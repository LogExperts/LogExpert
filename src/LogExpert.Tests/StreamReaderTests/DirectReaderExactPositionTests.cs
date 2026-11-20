using System.Text;

using LogExpert.Core.Classes.Log.Streamreaders;
using LogExpert.Core.Entities;
using LogExpert.Core.Helpers;

using NUnit.Framework;

namespace LogExpert.Tests.StreamReaderTests;

/// <summary>
/// Issue #639: the byte <see cref="PositionAwareStreamReaderDirect.Position"/> must equal the raw bytes
/// consumed, whatever the bytes are. Re-encoding decoded text overcounts invalid sequences (each U+FFFD
/// re-encodes as 3 UTF-8 bytes), so a file with a binary block looked larger than it was and every
/// growth was mistaken for a recreated file. The oracle here is the raw byte stream itself: a line ends
/// at the next <c>\n</c>, <c>\r\n</c> or bare <c>\r</c> unit, and its text is that line's bytes decoded on
/// their own.
/// </summary>
[TestFixture]
public class DirectReaderExactPositionTests
{
    private const int MAX_LINE_LENGTH = 500_000;

    #region Invalid byte sequences

    private static IEnumerable<TestCaseData> InvalidUtf8Cases ()
    {
        yield return Case("StrayContinuationBytes", "abc", [0x80, 0x81], "def\nnext\n");
        yield return Case("TruncatedSequenceBeforeLf", "x", [0xE6, 0x97], "\ny\n");
        yield return Case("TruncatedSequenceBeforeCrLf", "x", [0xE6], "\r\ny");
        yield return Case("NulAndHighBytes", "a", [0x00, 0x00, 0xFF, 0xFE], "\nb\n");
        yield return Case("OverlongAndEncodedSurrogate", "", [0xC0, 0xAF, 0xED, 0xA0, 0x80], "\nok\n");
        yield return Case("TruncatedSequenceAtEof", "tail", [0xE6, 0x97], "");
        yield return Case("GenuineReplacementCharacter", "valid ", [0xEF, 0xBF, 0xBD], " stays 3 bytes\n");
        yield return Case("BareCrAfterInvalidByte", "", [0xFF], "\rz");
        yield return Case("InvalidLineOnly", "", [0xF5, 0xF6, 0xF7], "\n");
        yield return Case("FourByteSequenceCutShort", "emoji ", [0xF0, 0x9F, 0x99], "\nafter\n");

        static TestCaseData Case (string name, string before, byte[] invalid, string after)
        {
            var bytes = Encoding.UTF8.GetBytes(before).Concat(invalid).Concat(Encoding.UTF8.GetBytes(after)).ToArray();
            return new TestCaseData(bytes).SetName($"Position_InvalidUtf8_{name}_MatchesRawBytes");
        }
    }

    [TestCaseSource(nameof(InvalidUtf8Cases))]
    public void Position_InvalidUtf8_MatchesRawBytes (byte[] data)
    {
        AssertMatchesOracle(data, Encoding.UTF8);
    }

    [Test]
    public void Position_InvalidUtf8_ContentShowsReplacementCharacters ()
    {
        byte[] data = [.. "abc"u8, 0x80, .. "def\n"u8];

        var lines = ReadAll(data, Encoding.UTF8);

        Assert.That(lines.Select(line => line.Content), Is.EqualTo(new[] { "abc�def" }));
    }

    [Test]
    public void Position_InvalidUtf8LineLongerThanMaximum_TruncatesContentButCountsAllBytes ()
    {
        byte[] data = [.. Encoding.UTF8.GetBytes(new string('A', 50)), 0xFF, 0xFE, .. Encoding.UTF8.GetBytes(new string('B', 50)), .. "\nnext\n"u8];

        using var stream = new MemoryStream(data);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions { Encoding = Encoding.UTF8 }, 20);

        Assert.That(reader.TryReadLine(out var first), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(first.ToString(), Is.EqualTo(new string('A', 20)));
            Assert.That(reader.Position, Is.EqualTo(103));
        });

        Assert.That(reader.TryReadLine(out var second), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(second.ToString(), Is.EqualTo("next"));
            Assert.That(reader.Position, Is.EqualTo(data.Length));
        });
    }

    [Test]
    public void Position_Utf16WithLoneSurrogateAndOddTrailingByte_MatchesRawBytes ()
    {
        byte[] data = [.. Encoding.Unicode.GetBytes("ab"), 0x00, 0xD8, .. Encoding.Unicode.GetBytes("\r\ncd\n"), 0x41];

        AssertMatchesOracle(data, Encoding.Unicode);
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void Position_Utf16WithSurrogatePairs_MatchesRawBytes ()
    {
        var data = Encoding.Unicode.GetBytes("plain\r\nemoji 🙂 and 𝄞\npair at end 🙂");

        AssertMatchesOracle(data, Encoding.Unicode);
    }

    [Test]
    public void Position_Utf16BigEndian_MatchesRawBytes ()
    {
        var data = Encoding.BigEndianUnicode.GetBytes("first\r\nsecond\nthird\rfourth");

        AssertMatchesOracle(data, Encoding.BigEndianUnicode);
    }

    [Test]
    public void Position_Gb2312WithInvalidLeadBytes_MatchesRawBytes ()
    {
        var gb2312 = EncodingRegistry.GetEncoding(EncodingRegistry.CODE_PAGE_GB2312);
        byte[] data = [.. gb2312.GetBytes("错误 "), 0x81, .. "\n"u8, 0xFF, .. gb2312.GetBytes("警告\n")];

        AssertMatchesOracle(data, gb2312);
    }

    #endregion

    #region Buffer boundaries

    /// <summary>
    /// A multi-byte character, an invalid sequence or a <c>\r\n</c> pair that straddles the reader's
    /// internal buffer must not shift the position. The boundaries cover the old 32 K char block and
    /// common byte buffer sizes, on both short-line and single-long-line layouts.
    /// </summary>
    [Test]
    public void Position_SequenceStraddlesBufferBoundary_MatchesRawBytes (
        [Values(16_383, 16_384, 32_767, 32_768, 65_535, 65_536, 131_071, 131_072)] int boundary,
        [Values("ValidThreeByte", "InvalidTruncated", "CrLf", "Emoji")] string kind,
        [Values(true, false)] bool shortLines)
    {
        byte[] sequence = kind switch
        {
            "ValidThreeByte" => [0xE4, 0xB8, 0x96],
            "InvalidTruncated" => [0xE4, 0xB8],
            "CrLf" => [0x0D, 0x0A],
            _ => [0xF0, 0x9F, 0x99, 0x82]
        };

        for (var startOffset = 1; startOffset < sequence.Length + 1; startOffset++)
        {
            var prefix = Filler(boundary - startOffset, shortLines);
            byte[] data = [.. prefix, .. sequence, .. "after\nlast"u8];

            AssertMatchesOracle(data, Encoding.UTF8);
        }

        static byte[] Filler (int length, bool shortLines)
        {
            var filler = new byte[length];
            for (var i = 0; i < length; i++)
            {
                filler[i] = shortLines && i % 100 == 99 ? (byte)'\n' : (byte)'A';
            }

            return filler;
        }
    }

    #endregion

    #region Seeking

    [Test]
    public void Position_RecordedLineStarts_SeekBackToTheSameLines ()
    {
        var data = TextWithBinaryBlock(lineCount: 3_000, binaryLength: 33_000, seed: 639);

        var expected = new List<(long Start, string Content)>();
        using (var stream = new MemoryStream(data))
        using (var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions { Encoding = Encoding.UTF8 }, MAX_LINE_LENGTH))
        {
            var start = reader.Position;
            while (reader.TryReadLine(out var line))
            {
                expected.Add((start, line.ToString()));
                start = reader.Position;
            }
        }

        using var seekStream = new MemoryStream(data);
        using var seekReader = new PositionAwareStreamReaderDirect(seekStream, new EncodingOptions { Encoding = Encoding.UTF8 }, MAX_LINE_LENGTH);

        // Walk backwards so every seek moves the stream and discards what the reader had buffered.
        for (var index = expected.Count - 1; index >= 0; index -= 37)
        {
            seekReader.Position = expected[index].Start;

            Assert.That(seekReader.TryReadLine(out var line), Is.True, $"Line {index}: nothing read after seek");
            Assert.That(line.ToString(), Is.EqualTo(expected[index].Content), $"Line {index}: content after seek");
        }
    }

    #endregion

    #region Generated input

    [Test]
    public void Position_TextWithBinaryBlock_MatchesRawBytes ()
    {
        AssertMatchesOracle(TextWithBinaryBlock(lineCount: 3_000, binaryLength: 33_000, seed: 42), Encoding.UTF8);
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Unit Tests")]
    public void Position_RandomBytes_MatchesRawBytes ([Range(0, 49)] int seed)
    {
        var random = new Random(seed);
        var data = new byte[random.Next(1, 200_000)];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = random.Next(10) switch
            {
                0 => (byte)'\n',
                1 => (byte)'\r',
                2 or 3 => (byte)random.Next(0x80, 0x100),
                _ => (byte)random.Next(0x20, 0x7F)
            };
        }

        AssertMatchesOracle(data, Encoding.UTF8);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Unit Tests")]
    private static byte[] TextWithBinaryBlock (int lineCount, int binaryLength, int seed)
    {
        var random = new Random(seed);
        using var stream = new MemoryStream();

        for (var i = 0; i < lineCount; i++)
        {
            if (i == lineCount / 2)
            {
                var binary = new byte[binaryLength];
                random.NextBytes(binary);
                stream.Write(binary);
            }

            stream.Write(Encoding.UTF8.GetBytes($"06/23/2026 00:00:00,MXS SEP26,3Minute,Info,OnVarUpdate(),\"Größe {i} 日志\"\r\n"));
        }

        return stream.ToArray();
    }

    #endregion

    #region Oracle

    private static void AssertMatchesOracle (byte[] data, Encoding encoding)
    {
        var expected = ExpectedLines(data, encoding);
        var actual = ReadAll(data, encoding);

        Assert.That(actual.Count, Is.EqualTo(expected.Count), "Line count");
        for (var i = 0; i < expected.Count; i++)
        {
            if (actual[i] != expected[i])
            {
                Assert.Fail($"Line {i}: expected end {expected[i].End} \"{Shorten(expected[i].Content)}\", got end {actual[i].End} \"{Shorten(actual[i].Content)}\"");
            }
        }
    }

    private static List<(string Content, long End)> ReadAll (byte[] data, Encoding encoding)
    {
        using var stream = new MemoryStream(data);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions { Encoding = encoding }, MAX_LINE_LENGTH);

        var lines = new List<(string, long)>();
        while (reader.TryReadLine(out var line))
        {
            lines.Add((line.ToString(), reader.Position));
        }

        return lines;
    }

    /// <summary>
    /// Splits the raw bytes at <c>\n</c>, <c>\r\n</c> and bare <c>\r</c> code units of the encoding and
    /// decodes each line's bytes on its own. A trailing line without terminator is a line; an empty
    /// remainder after the last terminator is not.
    /// </summary>
    private static List<(string Content, long End)> ExpectedLines (byte[] data, Encoding encoding)
    {
        var cr = encoding.GetBytes("\r");
        var lf = encoding.GetBytes("\n");
        var unit = lf.Length;

        var lines = new List<(string, long)>();
        var lineStart = 0;
        var position = 0;

        while (position + unit <= data.Length)
        {
            var span = data.AsSpan(position, unit);
            if (span.SequenceEqual(lf) || span.SequenceEqual(cr))
            {
                var terminatorLength = unit;
                if (span.SequenceEqual(cr) && position + (2 * unit) <= data.Length && data.AsSpan(position + unit, unit).SequenceEqual(lf))
                {
                    terminatorLength = 2 * unit;
                }

                lines.Add((encoding.GetString(data, lineStart, position - lineStart), position + terminatorLength));
                position += terminatorLength;
                lineStart = position;
            }
            else
            {
                position += unit;
            }
        }

        if (lineStart < data.Length)
        {
            lines.Add((encoding.GetString(data, lineStart, data.Length - lineStart), data.Length));
        }

        return lines;
    }

    private static string Shorten (string text) => text.Length <= 60 ? text : $"{text[..60]}… ({text.Length} chars)";

    #endregion
}