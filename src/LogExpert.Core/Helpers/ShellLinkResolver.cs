using System.Buffers.Binary;
using System.Globalization;
using System.Text;

using NLog;

namespace LogExpert.Core.Helpers;

/// <summary>
/// Resolves a Windows shortcut (<c>.lnk</c>) to the local file it points at.
/// </summary>
/// <remarks>
/// The MS-SHLLINK format is parsed directly rather than through <c>IShellLink</c>, because this
/// lives in the Core assembly, which carries no COM. Only local targets are followed: UNC and
/// device paths are skipped, and anything unresolvable is returned unchanged.
/// </remarks>
public static class ShellLinkResolver
{
    #region Constants

    private const string SHORTCUT_EXTENSION = ".lnk";

    /// <summary>Size of <c>ShellLinkHeader</c>, and the value of its first field.</summary>
    private const int SHELL_LINK_HEADER_SIZE = 0x4C;

    /// <summary>Offset of <c>LinkFlags</c>: right after the header size and the link CLSID.</summary>
    private const int LINK_FLAGS_OFFSET = 20;

    /// <summary>Size of <c>LinkInfo</c> without the optional Unicode offsets.</summary>
    private const int LINK_INFO_MIN_SIZE = 0x1C;

    /// <summary><c>LinkInfoHeaderSize</c> of a <c>LinkInfo</c> that carries the Unicode offsets.</summary>
    private const int LINK_INFO_HEADER_SIZE_UNICODE = 0x24;

    /// <summary>
    /// Real shortcuts are a few KB. Anything larger is not one, and reading it whole would let a
    /// dropped file exhaust memory.
    /// </summary>
    private const int MAX_SHORTCUT_SIZE = 1024 * 1024;

    /// <summary>
    /// Upper bound for chained shortcuts. A shortcut pointing at a shortcut is legitimate; longer
    /// chains are either pathological or a loop.
    /// </summary>
    private const int MAX_RESOLVE_DEPTH = 8;

    // LinkFlags
    private const uint HAS_LINK_TARGET_ID_LIST = 0x00000001;
    private const uint HAS_LINK_INFO = 0x00000002;
    private const uint HAS_NAME = 0x00000004;
    private const uint HAS_RELATIVE_PATH = 0x00000008;
    private const uint IS_UNICODE = 0x00000080;

    // LinkInfoFlags
    private const uint VOLUME_ID_AND_LOCAL_BASE_PATH = 0x00000001;

    /// <summary>
    /// CLSID of a Shell Link: 00021401-0000-0000-C000-000000000046, in its on-disk byte order.
    /// </summary>
    private static ReadOnlySpan<byte> ShellLinkClsid =>
    [
        0x01, 0x14, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46
    ];

    #endregion

    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Encoding of the ANSI strings. Resolved lazily and only for shortcuts that are not flagged
    /// Unicode; Latin1 is a sufficient fallback where no code pages are available.
    /// </summary>
    private static readonly Lazy<Encoding> _ansiEncoding = new(ResolveAnsiEncoding);

    #endregion

    #region Public methods

    /// <summary>
    /// Resolves <paramref name="fileName"/> to the file it points at, following chained shortcuts.
    /// </summary>
    /// <param name="fileName">A path that may or may not be a shortcut.</param>
    /// <returns>
    /// The target path when the whole chain resolves to an existing local file; otherwise
    /// <paramref name="fileName"/> unchanged.
    /// </returns>
    public static string Resolve (string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return fileName;
        }

        var current = fileName;

        for (var depth = 0; depth < MAX_RESOLVE_DEPTH; depth++)
        {
            if (!IsShortcutFile(current))
            {
                return current;
            }

            var target = TryResolveSingle(current);
            if (target is null)
            {
                // Keep what the caller passed in, not a half-followed chain, so a broken second link
                // behaves exactly like a broken first one.
                _logger.Debug("Shortcut {Shortcut} is unresolvable, keeping {Original}", current, fileName);
                return fileName;
            }

            current = target;
        }

        _logger.Warn("Shortcut chain starting at {Shortcut} exceeds {Depth} links, keeping the original path", fileName, MAX_RESOLVE_DEPTH);
        return fileName;
    }

    #endregion

    #region Internal parsing core

    /// <summary>
    /// Parses the path fields out of shortcut bytes. Separate from <see cref="Resolve"/> and free of
    /// I/O so tests can feed crafted shortcuts in.
    /// </summary>
    internal static bool TryParseTargets (ReadOnlySpan<byte> data, out ShortcutTargets targets)
    {
        targets = new ShortcutTargets();

        if (!IsShellLink(data))
        {
            return false;
        }

        var flags = BinaryPrimitives.ReadUInt32LittleEndian(data[LINK_FLAGS_OFFSET..]);
        var isUnicode = (flags & IS_UNICODE) != 0;
        var position = SHELL_LINK_HEADER_SIZE;

        if ((flags & HAS_LINK_TARGET_ID_LIST) != 0 && !TrySkipIdList(data, ref position))
        {
            return false;
        }

        if ((flags & HAS_LINK_INFO) != 0 && !TryReadLinkInfo(data, ref position, targets))
        {
            return false;
        }

        // StringData follows LinkInfo, one entry per flag, in a fixed order. Only RELATIVE_PATH is
        // needed, but NAME_STRING has to be stepped over to reach it.
        if ((flags & HAS_NAME) != 0 && !TryReadStringData(data, ref position, isUnicode, out _))
        {
            return false;
        }

        if ((flags & HAS_RELATIVE_PATH) != 0)
        {
            if (!TryReadStringData(data, ref position, isUnicode, out var relativePath))
            {
                return false;
            }

            targets.RelativePath = relativePath;
        }

        return true;
    }

    /// <summary>
    /// Turns the parsed fields into absolute paths, most specific first, dropping anything that is
    /// not on this machine. No file is touched.
    /// </summary>
    internal static IReadOnlyList<string> BuildCandidatePaths (string shortcutPath, ShortcutTargets targets)
    {
        var shortcutDirectory = Path.GetDirectoryName(shortcutPath);
        var candidates = new List<string>(3);

        AddCandidate(candidates, targets.LocalBasePath);
        AddCandidate(candidates, JoinPath(targets.LocalBasePath, targets.CommonPathSuffix));
        AddCandidate(candidates, targets.RelativePath);

        var result = new List<string>(candidates.Count);

        foreach (var candidate in candidates)
        {
            var path = candidate;

            if (!Path.IsPathRooted(path))
            {
                if (shortcutDirectory is null)
                {
                    continue;
                }

                path = Path.Join(shortcutDirectory, path);
            }

            try
            {
                path = Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException
                                           or NotSupportedException
                                           or PathTooLongException
                                           or System.Security.SecurityException)
            {
                continue;
            }

            if (IsLocalFilePath(path))
            {
                result.Add(path);
            }
            else
            {
                _logger.Debug("Not following non-local shortcut target {Target}", path);
            }
        }

        return result;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Whether <paramref name="path"/> names an existing file with the shortcut extension. Paths with
    /// any other extension never touch the disk.
    /// </summary>
    private static bool IsShortcutFile (string path)
    {
        if (!Path.GetExtension(path).Equals(SHORTCUT_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            return File.Exists(path);
        }
        catch (Exception ex) when (ex is IOException
                                       or UnauthorizedAccessException
                                       or ArgumentException
                                       or NotSupportedException
                                       or System.Security.SecurityException)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads one shortcut and returns its target, without following a target that is itself a
    /// shortcut.
    /// </summary>
    /// <returns>The first candidate that exists on disk, or <c>null</c>.</returns>
    private static string? TryResolveSingle (string shortcutPath)
    {
        var data = TryReadShortcut(shortcutPath);
        if (data is null)
        {
            return null;
        }

        if (!TryParseTargets(data, out var targets))
        {
            _logger.Warn("Ignoring {Shortcut}: not a valid shortcut", shortcutPath);
            return null;
        }

        return FindExistingTarget(shortcutPath, targets);
    }

    private static byte[]? TryReadShortcut (string shortcutPath)
    {
        try
        {
            using var stream = new FileStream(shortcutPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            if (stream.Length > MAX_SHORTCUT_SIZE)
            {
                _logger.Warn("Ignoring {Shortcut}: {Size} bytes is above the {Limit} byte shortcut limit", shortcutPath, stream.Length, MAX_SHORTCUT_SIZE);
                return null;
            }

            var bytes = new byte[stream.Length];
            stream.ReadExactly(bytes);
            return bytes;
        }
        catch (Exception ex) when (ex is IOException
                                       or UnauthorizedAccessException
                                       or ArgumentException
                                       or NotSupportedException
                                       or System.Security.SecurityException)
        {
            _logger.Debug(ex, "Could not read shortcut {Shortcut}", shortcutPath);
            return null;
        }
    }

    private static bool IsShellLink (ReadOnlySpan<byte> data)
    {
        if (data.Length < SHELL_LINK_HEADER_SIZE)
        {
            return false;
        }

        if (BinaryPrimitives.ReadUInt32LittleEndian(data) != SHELL_LINK_HEADER_SIZE)
        {
            return false;
        }

        return data.Slice(4, ShellLinkClsid.Length).SequenceEqual(ShellLinkClsid);
    }

    /// <summary>
    /// Steps over <c>LinkTargetIDList</c>, a size-prefixed blob of shell item identifiers.
    /// </summary>
    private static bool TrySkipIdList (ReadOnlySpan<byte> data, ref int position)
    {
        if (position + 2 > data.Length)
        {
            return false;
        }

        var idListSize = BinaryPrimitives.ReadUInt16LittleEndian(data[position..]);
        position += 2 + idListSize;
        return position <= data.Length;
    }

    /// <summary>
    /// Reads the local base path and the common path suffix out of <c>LinkInfo</c>. The network part
    /// is deliberately not read: its targets are never followed.
    /// </summary>
    private static bool TryReadLinkInfo (ReadOnlySpan<byte> data, ref int position, ShortcutTargets targets)
    {
        if (position + LINK_INFO_MIN_SIZE > data.Length)
        {
            return false;
        }

        var linkInfoSize = BinaryPrimitives.ReadUInt32LittleEndian(data[position..]);
        if (linkInfoSize < LINK_INFO_MIN_SIZE || (long)position + linkInfoSize > data.Length)
        {
            return false;
        }

        var linkInfo = data.Slice(position, (int)linkInfoSize);
        var linkInfoHeaderSize = BinaryPrimitives.ReadUInt32LittleEndian(linkInfo[4..]);
        var linkInfoFlags = BinaryPrimitives.ReadUInt32LittleEndian(linkInfo[8..]);
        var hasUnicodeOffsets = linkInfoHeaderSize >= LINK_INFO_HEADER_SIZE_UNICODE
                                && linkInfoSize >= LINK_INFO_HEADER_SIZE_UNICODE;

        var localBasePathOffset = BinaryPrimitives.ReadUInt32LittleEndian(linkInfo[16..]);
        var commonPathSuffixOffset = BinaryPrimitives.ReadUInt32LittleEndian(linkInfo[24..]);

        // The Unicode strings win when present: the ANSI ones are a lossy rendering of them.
        var localBasePathUnicodeOffset = hasUnicodeOffsets
            ? BinaryPrimitives.ReadUInt32LittleEndian(linkInfo[28..])
            : 0;

        var commonPathSuffixUnicodeOffset = hasUnicodeOffsets
            ? BinaryPrimitives.ReadUInt32LittleEndian(linkInfo[32..])
            : 0;

        if ((linkInfoFlags & VOLUME_ID_AND_LOCAL_BASE_PATH) != 0)
        {
            targets.LocalBasePath = ReadStringAt(linkInfo, localBasePathUnicodeOffset, isUnicode: true)
                                    ?? ReadStringAt(linkInfo, localBasePathOffset, isUnicode: false);
        }

        targets.CommonPathSuffix = ReadStringAt(linkInfo, commonPathSuffixUnicodeOffset, isUnicode: true)
                                   ?? ReadStringAt(linkInfo, commonPathSuffixOffset, isUnicode: false);

        position += (int)linkInfoSize;
        return true;
    }

    /// <summary>
    /// Reads a NUL-terminated string at <paramref name="offset"/> inside <paramref name="data"/>.
    /// </summary>
    private static string? ReadStringAt (ReadOnlySpan<byte> data, uint offset, bool isUnicode)
    {
        if (offset == 0 || offset >= (uint)data.Length)
        {
            return null;
        }

        return TryReadNullTerminatedString(data, (int)offset, isUnicode, out var value) && value.Length > 0
            ? value
            : null;
    }

    private static bool TryReadNullTerminatedString (ReadOnlySpan<byte> data, int offset, bool isUnicode, out string value)
    {
        value = string.Empty;

        if (offset < 0 || offset >= data.Length)
        {
            return false;
        }

        var slice = data[offset..];

        if (isUnicode)
        {
            for (var index = 0; index + 1 < slice.Length; index += 2)
            {
                if (slice[index] == 0 && slice[index + 1] == 0)
                {
                    value = Encoding.Unicode.GetString(slice[..index]);
                    return true;
                }
            }

            return false;
        }

        var terminator = slice.IndexOf((byte)0);
        if (terminator < 0)
        {
            return false;
        }

        value = _ansiEncoding.Value.GetString(slice[..terminator]);
        return true;
    }

    /// <summary>
    /// Reads one <c>StringData</c> entry: a character count followed by that many characters, with
    /// no terminator (MS-SHLLINK 2.4).
    /// </summary>
    private static bool TryReadStringData (ReadOnlySpan<byte> data, ref int position, bool isUnicode, out string value)
    {
        value = string.Empty;

        if (position + 2 > data.Length)
        {
            return false;
        }

        var characterCount = BinaryPrimitives.ReadUInt16LittleEndian(data[position..]);
        position += 2;

        var byteCount = isUnicode ? characterCount * 2 : characterCount;
        if (position + byteCount > data.Length)
        {
            return false;
        }

        value = isUnicode
            ? Encoding.Unicode.GetString(data.Slice(position, byteCount))
            : _ansiEncoding.Value.GetString(data.Slice(position, byteCount));

        position += byteCount;
        return true;
    }

    /// <summary>
    /// Returns the first candidate path that exists as a file.
    /// </summary>
    private static string? FindExistingTarget (string shortcutPath, ShortcutTargets targets)
    {
        foreach (var candidate in BuildCandidatePaths(shortcutPath, targets))
        {
            try
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (Exception ex) when (ex is IOException
                                           or UnauthorizedAccessException
                                           or ArgumentException
                                           or NotSupportedException
                                           or System.Security.SecurityException)
            {
                _logger.Debug(ex, "Could not look up shortcut target {Target}", candidate);
            }
        }

        return null;
    }

    /// <summary>
    /// Whether <paramref name="path"/> stays on this machine. <c>\\host\share</c> would make Windows
    /// authenticate to that host, handing it the user's NetNTLM hash, and <c>\\.\</c> or
    /// <c>\\?\</c> reach devices and pipes that can block.
    /// </summary>
    private static bool IsLocalFilePath (string path)
    {
        return !path.StartsWith(@"\\", StringComparison.Ordinal);
    }

    private static void AddCandidate (List<string> candidates, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!candidates.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(path);
        }
    }

    /// <summary>
    /// Joins a base path and a suffix, tolerating a separator on either side.
    /// </summary>
    private static string? JoinPath (string? basePath, string? suffix)
    {
        if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(suffix))
        {
            return null;
        }

        return Path.Join(
            basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            suffix.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private static Encoding ResolveAnsiEncoding ()
    {
        try
        {
            // InstalledUICulture, not CurrentCulture: the UI language the user picked is applied to
            // CurrentCulture at startup, which would otherwise report the wrong ANSI code page.
            var codePage = CultureInfo.InstalledUICulture.TextInfo.ANSICodePage;
            if (codePage > 0)
            {
                return EncodingRegistry.GetEncoding(codePage);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            // No code pages are available; Latin1 still decodes the ASCII paths this branch sees.
        }

        return Encoding.Latin1;
    }

    #endregion

    #region Data types

    /// <summary>
    /// The path fields a shortcut carries, as parsed. A plain holder: resolving them against the
    /// file system is <see cref="Resolve"/>'s job.
    /// </summary>
    internal sealed class ShortcutTargets
    {
        public string? LocalBasePath { get; set; }

        public string? CommonPathSuffix { get; set; }

        public string? RelativePath { get; set; }
    }

    #endregion
}
