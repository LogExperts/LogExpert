using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace LogExpert.Core.Helpers;

/// <summary>
/// Resolves a Windows shortcut (<c>.lnk</c>) to the file it points at.
/// </summary>
/// <remarks>
/// <para>
/// A shortcut is a binary file, not a log: handing one to the reader shows its header bytes as log
/// lines. Callers that accept a path from the user — a drop, the Open dialog, the command line —
/// pass it through <see cref="Resolve"/> first, so everything downstream only ever sees the target.
/// </para>
/// <para>
/// The Shell Link binary format (MS-SHLLINK) is parsed here directly instead of through
/// <c>IShellLink</c>, because this class lives in the Core assembly, which carries no COM or
/// Windows-only dependency. Every unresolvable case — an unreadable or corrupt shortcut, a target
/// that no longer exists, a shortcut to a folder — returns the input unchanged, so the caller keeps
/// the behaviour it had before.
/// </para>
/// <para>
/// Only the extension is inspected for non-shortcut paths; nothing is read from disk unless the path
/// actually names an existing <c>.lnk</c>.
/// </para>
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

    private const int NETWORK_LINK_NET_NAME_OFFSET = 8;
    private const int NETWORK_LINK_NET_NAME_UNICODE_OFFSET = 20;
    private const int NETWORK_LINK_MIN_SIZE = 0x1C;

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
    private const uint COMMON_NETWORK_RELATIVE_LINK_AND_PATH_SUFFIX = 0x00000002;

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

    /// <summary>
    /// Encoding for the ANSI strings in the older parts of the format. Resolved lazily and only for
    /// shortcuts that are not flagged Unicode; the system ANSI code page keeps non-ASCII paths
    /// intact, and Latin1 is a sufficient fallback where no code pages are available.
    /// </summary>
    private static readonly Lazy<Encoding> _ansiEncoding = new(ResolveAnsiEncoding);

    #endregion

    #region Public methods

    /// <summary>
    /// Resolves <paramref name="fileName"/> to the file it points at, following chained shortcuts.
    /// </summary>
    /// <param name="fileName">A path that may or may not be a shortcut.</param>
    /// <returns>
    /// The target path when it resolves to an existing file; otherwise <paramref name="fileName"/>
    /// unchanged.
    /// </returns>
    public static string Resolve (string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return fileName;
        }

        var resolved = fileName;

        for (var depth = 0; depth < MAX_RESOLVE_DEPTH; depth++)
        {
            if (!IsShortcut(resolved))
            {
                return resolved;
            }

            var target = TryResolveSingle(resolved);
            if (target is null)
            {
                // Unresolvable shortcut: keep the current path, so the caller behaves as it did
                // before this class existed.
                return resolved;
            }

            resolved = target;
        }

        return resolved;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Whether <paramref name="path"/> names an existing file with the shortcut extension. Paths with
    /// any other extension never touch the disk.
    /// </summary>
    private static bool IsShortcut (string path)
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
    /// Reads one shortcut and returns its target, without following a shortcut target that is itself
    /// a shortcut.
    /// </summary>
    /// <returns>The first candidate that exists on disk, or <c>null</c>.</returns>
    private static string? TryResolveSingle (string shortcutPath)
    {
        byte[] bytes;

        try
        {
            bytes = File.ReadAllBytes(shortcutPath);
        }
        catch (Exception ex) when (ex is IOException
                                       or UnauthorizedAccessException
                                       or ArgumentException
                                       or NotSupportedException
                                       or System.Security.SecurityException)
        {
            return null;
        }

        var data = bytes.AsSpan();
        if (!IsShellLink(data))
        {
            return null;
        }

        var flags = BinaryPrimitives.ReadUInt32LittleEndian(data[LINK_FLAGS_OFFSET..]);
        var isUnicode = (flags & IS_UNICODE) != 0;
        var position = SHELL_LINK_HEADER_SIZE;
        var targets = new LinkTargets();

        if ((flags & HAS_LINK_TARGET_ID_LIST) != 0 && !TrySkipIdList(data, ref position))
        {
            return null;
        }

        if ((flags & HAS_LINK_INFO) != 0 && !TryReadLinkInfo(data, ref position, targets))
        {
            return null;
        }

        // StringData follows LinkInfo. Only RELATIVE_PATH is needed, but the entries before it have
        // variable length and have to be walked over to reach it.
        if ((flags & HAS_NAME) != 0)
        {
            _ = TryReadCountedString(data, ref position, isUnicode, out _);
        }

        if ((flags & HAS_RELATIVE_PATH) != 0
            && TryReadCountedString(data, ref position, isUnicode, out var relativePath))
        {
            targets.RelativePath = relativePath;
        }

        return FindExistingTarget(shortcutPath, targets);
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
    /// Reads <c>LinkInfo</c>: the local base path, the network path, and the common path suffix.
    /// </summary>
    private static bool TryReadLinkInfo (ReadOnlySpan<byte> data, ref int position, LinkTargets targets)
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

        if ((linkInfoFlags & COMMON_NETWORK_RELATIVE_LINK_AND_PATH_SUFFIX) != 0)
        {
            targets.NetworkPath = ReadNetworkPath(linkInfo, BinaryPrimitives.ReadUInt32LittleEndian(linkInfo[20..]));
        }

        position += (int)linkInfoSize;
        return true;
    }

    /// <summary>
    /// Reads the server name out of <c>CommonNetworkRelativeLink</c>. The offsets there are relative
    /// to that structure, not to <c>LinkInfo</c>.
    /// </summary>
    private static string? ReadNetworkPath (ReadOnlySpan<byte> linkInfo, uint offset)
    {
        if (offset == 0 || offset >= (uint)linkInfo.Length)
        {
            return null;
        }

        var start = (int)offset;
        if (start + NETWORK_LINK_MIN_SIZE > linkInfo.Length)
        {
            return null;
        }

        var networkLink = linkInfo[start..];
        var netNameOffset = BinaryPrimitives.ReadUInt32LittleEndian(networkLink[NETWORK_LINK_NET_NAME_OFFSET..]);
        var netNameUnicodeOffset = BinaryPrimitives.ReadUInt32LittleEndian(networkLink[NETWORK_LINK_NET_NAME_UNICODE_OFFSET..]);

        return ReadStringAt(networkLink, netNameUnicodeOffset, isUnicode: true)
               ?? ReadStringAt(networkLink, netNameOffset, isUnicode: false);
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
    /// Reads one <c>StringData</c> entry: a character count, the characters, then a terminator.
    /// </summary>
    private static bool TryReadCountedString (ReadOnlySpan<byte> data, ref int position, bool isUnicode, out string value)
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

        // Some writers omit the terminator on the last entry; clamp instead of failing the parse.
        position = Math.Min(position + (isUnicode ? 2 : 1), data.Length);
        return true;
    }

    /// <summary>
    /// Builds the candidate paths a shortcut may point at and returns the first one that exists.
    /// </summary>
    /// <remarks>
    /// Modern shortcuts usually fill <c>LocalBasePath</c> with the full path, but the field may also
    /// hold only the directory (with the rest in <c>CommonPathSuffix</c>), and shortcuts that were
    /// never saved with a volume fall back to a path relative to the shortcut's own directory. All
    /// of those are tried, most specific first.
    /// </remarks>
    private static string? FindExistingTarget (string shortcutPath, LinkTargets targets)
    {
        var shortcutDirectory = Path.GetDirectoryName(shortcutPath);
        var candidates = new List<string>(5);

        AddCandidate(candidates, targets.LocalBasePath);
        AddCandidate(candidates, JoinPath(targets.LocalBasePath, targets.CommonPathSuffix));
        AddCandidate(candidates, JoinPath(targets.NetworkPath, targets.CommonPathSuffix));
        AddCandidate(candidates, targets.NetworkPath);
        AddCandidate(candidates, targets.RelativePath);

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

            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static void AddCandidate (List<string> candidates, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string expanded;

        try
        {
            expanded = Environment.ExpandEnvironmentVariables(path);
        }
        catch (Exception ex) when (ex is ArgumentException or System.Security.SecurityException)
        {
            return;
        }

        if (expanded.Length > 0 && !candidates.Contains(expanded, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(expanded);
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
    /// The path fields a shortcut may carry, in the order they are parsed.
    /// </summary>
    private sealed class LinkTargets
    {
        public string? LocalBasePath { get; set; }

        public string? CommonPathSuffix { get; set; }

        public string? NetworkPath { get; set; }

        public string? RelativePath { get; set; }
    }

    #endregion
}
