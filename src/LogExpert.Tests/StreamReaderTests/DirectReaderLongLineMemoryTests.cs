using System.Buffers;
using System.Numerics;
using System.Text;

using LogExpert.Core.Classes.Log.Streamreaders;
using LogExpert.Core.Entities;
using LogExpert.Core.Helpers;

using NUnit.Framework;

namespace LogExpert.Tests.StreamReaderTests;

// Issue #758: memory used to read one line must be bounded by the maximum line length, not by the
// line's raw length. A 50 MB run without terminators used to grow the byte buffer to the whole run
// and hand a 64 M char block to the LogBuffer.
[TestFixture]
public class DirectReaderLongLineMemoryTests
{
    private const int MAX_LINE_LENGTH = 20_000;
    private const int RUN_LENGTH = 50 * 1024 * 1024;
    private const int BLOCK_SIZE = 32_768;
    private const int BYTE_BUFFER_SIZE = 65_536;
    private const int GB18030_CODE_PAGE = 54_936;

    // A small multiple of the maximum line length (decoded chars plus the bytes behind them) and the read buffer.
    private const long ALLOCATION_BOUND = (16L * MAX_LINE_LENGTH * sizeof(char)) + (4L * BYTE_BUFFER_SIZE);

    private static readonly string[] _runEncodings = ["utf-8", "utf-16"];

    #region Long run without terminators

    [TestCaseSource(nameof(_runEncodings))]
    public void LongRun_AllocationsStayBounded (string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        var data = TextAroundRun(encoding, RandomRun(encoding, RUN_LENGTH, seed: 758));

        using var stream = new MemoryStream(data, writable: false);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions { Encoding = encoding }, MAX_LINE_LENGTH);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var lineCount = 0;
        while (reader.TryReadLine(out _))
        {
            lineCount++;
        }

        var blocks = reader.DetachCharBlocks();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        ReturnBlocks(blocks);

        Assert.Multiple(() =>
        {
            Assert.That(lineCount, Is.EqualTo(5));
            Assert.That(allocated, Is.LessThan(ALLOCATION_BOUND), "Bytes allocated while reading");
        });
    }

    [TestCaseSource(nameof(_runEncodings))]
    public void LongRun_DetachedBlocksAreNotOversized (string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        var data = TextAroundRun(encoding, RandomRun(encoding, RUN_LENGTH, seed: 758));

        using var stream = new MemoryStream(data, writable: false);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions { Encoding = encoding }, MAX_LINE_LENGTH);

        var blocks = new List<char[]>();
        while (reader.TryReadLine(out _))
        {
            blocks.AddRange(reader.DetachCharBlocks());
        }

        var largest = blocks.Max(block => block.Length);
        ReturnBlocks(blocks);

        Assert.That(largest, Is.LessThanOrEqualTo(LargestAllowedBlock(MAX_LINE_LENGTH)));
    }

    [TestCaseSource(nameof(_runEncodings))]
    public void LongRun_TextAndPositionsMatchRawBytes (string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        var run = RandomRun(encoding, RUN_LENGTH, seed: 758);
        var data = TextAroundRun(encoding, run);

        using var stream = new MemoryStream(data, writable: false);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions { Encoding = encoding }, MAX_LINE_LENGTH);

        var lines = new List<(string Content, long End)>();
        while (reader.TryReadLine(out var line))
        {
            lines.Add((line.ToString(), reader.Position));
        }

        var runStart = encoding.GetByteCount("first\nsecond\r\n");
        var runEnd = runStart + run.Length + encoding.GetByteCount("\r\n");

        Assert.That(lines.Select(line => line.Content).ToArray(), Is.EqualTo(new[]
        {
            "first",
            "second",
            encoding.GetString(run)[..MAX_LINE_LENGTH],
            "after",
            "last"
        }));
        Assert.That(lines.Select(line => line.End).ToArray(), Is.EqualTo(new long[]
        {
            encoding.GetByteCount("first\n"),
            runStart,
            runEnd,
            runEnd + encoding.GetByteCount("after\n"),
            data.Length
        }));
    }

    #endregion

    #region Cut points and terminators at refill boundaries

    // Lines of 1 MB, so the cut happens while the reader still has to refill.
    [Test]
    public void LongLine_CutInsideMultiByteSequence_MatchesFullDecodeTruncated (
        [Values(0, 1, 2, 3)] int asciiPrefix,
        [Values(0x65E5, 0x1F642, 0xC4)] int codePoint,
        [Values(7, 100, 20_000)] int maxLineLength)
    {
        var character = char.ConvertFromUtf32(codePoint);
        var line = new string('A', asciiPrefix) + string.Concat(Enumerable.Repeat(character, 1_000_000 / character.Length / 3));
        var data = Encoding.UTF8.GetBytes($"head\n{line}\nnext\n{line}");

        RawByteLineOracle.AssertMatches(data, Encoding.UTF8, maxLineLength);
    }

    [Test]
    public void LongLine_InvalidSequenceAtCut_MatchesFullDecodeTruncated ([Values(0, 1, 2)] int asciiPrefix, [Values(7, 100)] int maxLineLength)
    {
        byte[] pattern = [0xE6, 0x97, 0x41, 0xF0, 0x9F, 0x99, 0xFF];
        var data = new List<byte>(Enumerable.Repeat((byte)'A', asciiPrefix));
        while (data.Count < 300_000)
        {
            data.AddRange(pattern);
        }

        data.AddRange("\nend"u8.ToArray());

        RawByteLineOracle.AssertMatches([.. data], Encoding.UTF8, maxLineLength);
    }

    [Test]
    public void LongLine_Utf16SurrogatePairsAtCut_MatchesFullDecodeTruncated ([Values(0, 1)] int asciiPrefix, [Values(7, 100, 20_000)] int maxLineLength)
    {
        var line = new string('A', asciiPrefix) + string.Concat(Enumerable.Repeat("🙂", 300_000));

        RawByteLineOracle.AssertMatches(Encoding.Unicode.GetBytes($"{line}\r\n{line}"), Encoding.Unicode, maxLineLength);
    }

    // GB18030 has 4-byte sequences; a cut one may decode to a char per byte.
    [Test]
    public void LongLine_Gb18030FourByteSequenceAtCut_MatchesFullDecodeTruncated (
        [Values(0, 1, 2, 3)] int asciiPrefix,
        [Values(true, false)] bool broken,
        [Values(1, 7, 100)] int maxLineLength)
    {
        byte[] pattern = broken ? [0x81, 0x30, 0x81, 0x41] : [0x81, 0x30, 0x81, 0x30];
        var data = new List<byte>(Enumerable.Repeat((byte)'A', asciiPrefix));
        while (data.Count < 300_000)
        {
            data.AddRange(pattern);
        }

        data.AddRange("\nend"u8.ToArray());

        RawByteLineOracle.AssertMatches([.. data], EncodingRegistry.GetEncoding(GB18030_CODE_PAGE), maxLineLength);
    }

    // The \r is the last byte of a buffer fill, so its partner arrives with the next refill.
    [Test]
    public void LongLine_CrAtRefillBoundary_IsClassifiedCorrectly (
        [Values(BYTE_BUFFER_SIZE - 1, (2 * BYTE_BUFFER_SIZE) - 1, (3 * BYTE_BUFFER_SIZE) - 1)] int crOffset,
        [Values("\n", "Y\n", "\r\n")] string afterCr,
        [Values(100, 20_000)] int maxLineLength)
    {
        byte[] data = [.. Enumerable.Repeat((byte)'X', crOffset), (byte)'\r', .. Encoding.UTF8.GetBytes(afterCr), .. "tail"u8];

        RawByteLineOracle.AssertMatches(data, Encoding.UTF8, maxLineLength);
    }

    [Test]
    public void LongLine_Utf16CrAtRefillBoundary_IsClassifiedCorrectly ([Values("\n", "Y")] string afterCr)
    {
        var data = Encoding.Unicode.GetBytes(new string('X', (BYTE_BUFFER_SIZE / 2) - 1) + "\r" + afterCr + "tail");

        RawByteLineOracle.AssertMatches(data, Encoding.Unicode, 100);
    }

    #endregion

    #region Generated input

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Unit Tests")]
    public void LongLines_RandomBytes_MatchFullDecodeTruncated (
        [Range(0, 9)] int seed,
        [Values("utf-8", "utf-16", "utf-16BE", "utf-32", "gb2312", "gb18030")] string encodingName,
        [Values(1, 7, 1_000)] int maxLineLength)
    {
        var encoding = encodingName switch
        {
            "gb2312" => EncodingRegistry.GetEncoding(EncodingRegistry.CODE_PAGE_GB2312),
            "gb18030" => EncodingRegistry.GetEncoding(GB18030_CODE_PAGE),
            _ => Encoding.GetEncoding(encodingName)
        };
        var random = new Random(seed);
        var data = RandomRun(encoding, random.Next(100_000, 400_000), seed);

        // Rare terminators, so most lines span several refills.
        var terminator = encoding.GetBytes(random.Next(3) switch { 0 => "\n", 1 => "\r", _ => "\r\n" });
        for (var i = 0; i < 4; i++)
        {
            var offset = random.Next(data.Length - terminator.Length);
            terminator.CopyTo(data, offset - (offset % terminator.Length));
        }

        RawByteLineOracle.AssertMatches(data, encoding, maxLineLength);
    }

    #endregion

    #region Helpers

    private static int LargestAllowedBlock (int maxLineLength) => Math.Max(BLOCK_SIZE, (int)BitOperations.RoundUpToPowerOf2((uint)maxLineLength));

    private static byte[] TextAroundRun (Encoding encoding, byte[] run)
    {
        return [.. encoding.GetBytes("first\nsecond\r\n"), .. run, .. encoding.GetBytes("\r\nafter\nlast")];
    }

    // Random code units, none of them \r or \n.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Unit Tests")]
    private static byte[] RandomRun (Encoding encoding, int length, int seed)
    {
        var random = new Random(seed);
        var run = new byte[length];
        random.NextBytes(run);

        var unitSize = encoding.GetByteCount("\n");
        var lf = encoding.GetBytes("\n");
        var cr = encoding.GetBytes("\r");
        for (var i = 0; i + unitSize <= run.Length; i += unitSize)
        {
            var unit = run.AsSpan(i, unitSize);
            if (unit.SequenceEqual(lf) || unit.SequenceEqual(cr))
            {
                unit[0] ^= 0x40;
            }
        }

        return run;
    }

    private static void ReturnBlocks (List<char[]> blocks)
    {
        foreach (var block in blocks)
        {
            ArrayPool<char>.Shared.Return(block);
        }
    }

    #endregion
}