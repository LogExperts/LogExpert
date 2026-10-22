using System.Text;

namespace LogExpert.Tests.Helpers;

/// <summary>
/// Builds shortcut byte arrays for tests. Only the fields the resolver reads are written: header,
/// an optional LinkInfo with a local base path and common suffix, and the StringData entries for
/// NAME_STRING and RELATIVE_PATH.
/// </summary>
internal static class ShellLinkFixture
{
    private const int HEADER_SIZE = 0x4C;

    private const uint HAS_LINK_INFO = 0x00000002;
    private const uint HAS_NAME = 0x00000004;
    private const uint HAS_RELATIVE_PATH = 0x00000008;
    private const uint IS_UNICODE = 0x00000080;

    private static ReadOnlySpan<byte> Clsid =>
    [
        0x01, 0x14, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46
    ];

    /// <summary>
    /// Builds a shortcut. <paramref name="unicode"/> selects the Unicode encoding for both LinkInfo
    /// and StringData, as the IsUnicode flag does in the real format.
    /// </summary>
    internal static byte[] Build (
        string? localBasePath = null,
        string? commonPathSuffix = null,
        string? name = null,
        string? relativePath = null,
        bool unicode = true,
        bool corruptSignature = false)
    {
        var flags = unicode ? IS_UNICODE : 0u;

        if (name is not null)
        {
            flags |= HAS_NAME;
        }

        if (relativePath is not null)
        {
            flags |= HAS_RELATIVE_PATH;
        }

        byte[]? linkInfo = null;
        if (localBasePath is not null || commonPathSuffix is not null)
        {
            flags |= HAS_LINK_INFO;
            linkInfo = BuildLinkInfo(localBasePath ?? string.Empty, commonPathSuffix ?? string.Empty, unicode);
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.Unicode, true);

        writer.Write((uint)HEADER_SIZE);
        writer.Write(corruptSignature ? new byte[Clsid.Length] : Clsid.ToArray());
        writer.Write(flags);
        writer.Write(new byte[HEADER_SIZE - 24]);

        if (linkInfo is not null)
        {
            writer.Write(linkInfo);
        }

        if (name is not null)
        {
            WriteStringData(writer, name, unicode);
        }

        if (relativePath is not null)
        {
            WriteStringData(writer, relativePath, unicode);
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] BuildLinkInfo (string localBasePath, string commonPathSuffix, bool unicode)
    {
        // The ANSI copy is always present when VOLUME_ID_AND_LOCAL_BASE_PATH is set; the Unicode
        // one only when the header is large enough to carry its offsets.
        var headerSize = unicode ? 0x24 : 0x1C;
        var ansiLocal = Encoding.Latin1.GetBytes(localBasePath);
        var ansiSuffix = Encoding.Latin1.GetBytes(commonPathSuffix);

        var ansiLocalOffset = headerSize;
        var ansiSuffixOffset = ansiLocalOffset + ansiLocal.Length + 1;
        var unicodeLocalOffset = ansiSuffixOffset + ansiSuffix.Length + 1;
        var unicodeSuffixOffset = unicodeLocalOffset + ((localBasePath.Length + 1) * 2);

        var size = unicode
            ? unicodeSuffixOffset + ((commonPathSuffix.Length + 1) * 2)
            : ansiSuffixOffset + ansiSuffix.Length + 1;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.Unicode, true);

        writer.Write((uint)size);
        writer.Write((uint)headerSize);
        writer.Write(1u); // VOLUME_ID_AND_LOCAL_BASE_PATH
        writer.Write(0u); // VolumeIDOffset
        writer.Write((uint)ansiLocalOffset);
        writer.Write(0u); // CommonNetworkRelativeLinkOffset
        writer.Write((uint)ansiSuffixOffset);

        if (unicode)
        {
            writer.Write((uint)unicodeLocalOffset);
            writer.Write((uint)unicodeSuffixOffset);
        }

        writer.Write(ansiLocal);
        writer.Write((byte)0);
        writer.Write(ansiSuffix);
        writer.Write((byte)0);

        if (unicode)
        {
            writer.Write(Encoding.Unicode.GetBytes(localBasePath));
            writer.Write((ushort)0);
            writer.Write(Encoding.Unicode.GetBytes(commonPathSuffix));
            writer.Write((ushort)0);
        }

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// Writes one StringData entry: a character count, then the characters. MS-SHLLINK 2.4 puts no
    /// NULL terminator after them.
    /// </summary>
    private static void WriteStringData (BinaryWriter writer, string value, bool unicode)
    {
        writer.Write((ushort)value.Length);

        if (unicode)
        {
            writer.Write(Encoding.Unicode.GetBytes(value));
        }
        else
        {
            writer.Write(Encoding.Latin1.GetBytes(value));
        }
    }
}
