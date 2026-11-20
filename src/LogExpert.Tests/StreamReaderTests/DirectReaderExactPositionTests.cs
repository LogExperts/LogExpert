using System.Text;

using LogExpert.Core.Classes.Log.Streamreaders;
using LogExpert.Core.Entities;
using LogExpert.Core.Helpers;

using NUnit.Framework;

namespace LogExpert.Tests.StreamReaderTests;

// Issue #639: Position must equal the raw bytes consumed, whatever they are. Re-encoding decoded text
// counted each U+FFFD as 3 UTF-8 bytes, so a file with a binary block looked larger than it was.
[TestFixture]
public class DirectReaderExactPositionTests
{
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
        RawByteLineOracle.AssertMatches(data, Encoding.UTF8);
    }

    [Test]
    public void Position_InvalidUtf8_ContentShowsReplacementCharacters ()
    {
        byte[] data = [.. "abc"u8, 0x80, .. "def\n"u8];

        var lines = RawByteLineOracle.ReadAll(data, Encoding.UTF8);

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

        RawByteLineOracle.AssertMatches(data, Encoding.Unicode);
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void Position_Utf16WithSurrogatePairs_MatchesRawBytes ()
    {
        var data = Encoding.Unicode.GetBytes("plain\r\nemoji 🙂 and 𝄞\npair at end 🙂");

        RawByteLineOracle.AssertMatches(data, Encoding.Unicode);
    }

    [Test]
    public void Position_Utf16BigEndian_MatchesRawBytes ()
    {
        var data = Encoding.BigEndianUnicode.GetBytes("first\r\nsecond\nthird\rfourth");

        RawByteLineOracle.AssertMatches(data, Encoding.BigEndianUnicode);
    }

    [Test]
    public void Position_Gb2312WithInvalidLeadBytes_MatchesRawBytes ()
    {
        var gb2312 = EncodingRegistry.GetEncoding(EncodingRegistry.CODE_PAGE_GB2312);
        byte[] data = [.. gb2312.GetBytes("错误 "), 0x81, .. "\n"u8, 0xFF, .. gb2312.GetBytes("警告\n")];

        RawByteLineOracle.AssertMatches(data, gb2312);
    }

    #endregion

    #region Buffer boundaries

    // Boundaries cover the old 32 K char block and common byte buffer sizes.
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

            RawByteLineOracle.AssertMatches(data, Encoding.UTF8);
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
        using (var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions { Encoding = Encoding.UTF8 }, RawByteLineOracle.MAX_LINE_LENGTH))
        {
            var start = reader.Position;
            while (reader.TryReadLine(out var line))
            {
                expected.Add((start, line.ToString()));
                start = reader.Position;
            }
        }

        using var seekStream = new MemoryStream(data);
        using var seekReader = new PositionAwareStreamReaderDirect(seekStream, new EncodingOptions { Encoding = Encoding.UTF8 }, RawByteLineOracle.MAX_LINE_LENGTH);

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
        RawByteLineOracle.AssertMatches(TextWithBinaryBlock(lineCount: 3_000, binaryLength: 33_000, seed: 42), Encoding.UTF8);
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

        RawByteLineOracle.AssertMatches(data, Encoding.UTF8);
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
}