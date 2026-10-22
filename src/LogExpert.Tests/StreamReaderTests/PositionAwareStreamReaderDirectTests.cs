using System.Globalization;
using System.Text;

using LogExpert.Core.Classes.Log.Streamreaders;
using LogExpert.Core.Entities;
using LogExpert.Core.Helpers;

using NUnit.Framework;

namespace LogExpert.Tests.StreamReaderTests;

[TestFixture]
public class PositionAwareStreamReaderDirectTests
{
    private static MemoryStream CreateStream (string text, Encoding? encoding = null)
    {
        return new MemoryStream((encoding ?? Encoding.UTF8).GetBytes(text));
    }

    #region Basic line reading

    [Test]
    [TestCase("Line 1\nLine 2\nLine 3", 3)]
    [TestCase("Line 1\nLine 2\nLine 3\n", 3)]
    [TestCase("Line 1\r\nLine 2\r\nLine 3", 3)]
    [TestCase("Line 1\r\nLine 2\r\nLine 3\r\n", 3)]
    public void TryReadLine_CorrectLineCount (string text, int expectedLines)
    {
        using var stream = CreateStream(text);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);
        var count = 0;
        while (reader.TryReadLine(out _))
        {
            count++;
        }

        Assert.That(count, Is.EqualTo(expectedLines));
    }

    [Test]
    [TestCase("Line 1\nLine 2\nLine 3")]
    [TestCase("Line 1\r\nLine 2\r\nLine 3")]
    public void TryReadLine_CorrectContent (string text)
    {
        using var stream = CreateStream(text);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

        Assert.That(reader.TryReadLine(out var line1), Is.True);
        Assert.That(line1.Span.ToString(), Is.EqualTo("Line 1"));

        Assert.That(reader.TryReadLine(out var line2), Is.True);
        Assert.That(line2.Span.ToString(), Is.EqualTo("Line 2"));

        Assert.That(reader.TryReadLine(out var line3), Is.True);
        Assert.That(line3.Span.ToString(), Is.EqualTo("Line 3"));

        Assert.That(reader.TryReadLine(out _), Is.False);
    }

    [Test]
    public void TryReadLine_EmptyFile_ReturnsFalse ()
    {
        using var stream = CreateStream("");
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

        Assert.That(reader.TryReadLine(out _), Is.False);
    }

    [Test]
    public void TryReadLine_EmptyLines_Preserved ()
    {
        using var stream = CreateStream("\n\n\n");
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

        var count = 0;
        while (reader.TryReadLine(out var line))
        {
            Assert.That(line.Length, Is.EqualTo(0));
            count++;
        }

        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void TryReadLine_NoTrailingNewline_EmitsLastLine ()
    {
        using var stream = CreateStream("line1\nline2");
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

        Assert.That(reader.TryReadLine(out var line1), Is.True);
        Assert.That(line1.Span.ToString(), Is.EqualTo("line1"));

        Assert.That(reader.TryReadLine(out var line2), Is.True);
        Assert.That(line2.Span.ToString(), Is.EqualTo("line2"));

        Assert.That(reader.TryReadLine(out _), Is.False);
    }

    #endregion

    #region MaximumLineLength

    [Test]
    public void TryReadLine_LongLine_Truncated ()
    {
        var longLine = new string('x', 1000);
        using var stream = CreateStream(longLine + "\nshort\n");
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 100);

        Assert.That(reader.TryReadLine(out var line1), Is.True);
        Assert.That(line1.Length, Is.EqualTo(100));

        Assert.That(reader.TryReadLine(out var line2), Is.True);
        Assert.That(line2.Span.ToString(), Is.EqualTo("short"));
    }

    #endregion

    #region Cross-block boundary

    [Test]
    public void TryReadLine_LineCrossesBlockBoundary_Correct ()
    {
        // Create content that forces a line to cross the internal block boundary.
        // Use a known block size and a line longer than remaining space.
        var sb = new StringBuilder();
        // Fill near the block boundary with short lines, then one long line
        for (var i = 0; i < 400; i++)
        {
            _ = sb.AppendLine(new string('A', 80)); // ~81 chars per line with \n
        }
        // At this point we're ~32,400 chars in. Add a long line that crosses the boundary.
        var crossingLine = new string('B', 500);
        _ = sb.AppendLine(crossingLine);
        _ = sb.AppendLine("final line");

        var text = sb.ToString();
        using var stream = CreateStream(text);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 1000);

        // Read all lines and verify the crossing line
        var lineNum = 0;
        var foundCrossing = false;
        while (reader.TryReadLine(out ReadOnlyMemory<char> line))
        {
            if (lineNum >= 400 && line.Length == 500)
            {
                Assert.That(line.Span.ToString(), Is.EqualTo(crossingLine));
                foundCrossing = true;
            }

            lineNum++;
        }

        Assert.That(foundCrossing, Is.True, "Should have found the 500-char crossing line");
    }

    #endregion

    #region Position parity with System reader

    [Test]
    [TestCase("Line 1\nLine 2\nLine 3\n")]
    [TestCase("Line 1\r\nLine 2\r\nLine 3\r\n")]
    [TestCase("Short\nMedium line here\nA much longer line with more content\n")]
    public void Position_MatchesSystemReader_UTF8 (string text)
    {
        ComparePositions(text, Encoding.UTF8);
    }

    [Test]
    [TestCase("Line 1\nLine 2\nLine 3\n")]
    public void Position_MatchesSystemReader_ASCII (string text)
    {
        ComparePositions(text, Encoding.ASCII);
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void Position_MatchesSystemReader_UTF8_MultibyteChars ()
    {
        var text = "Héllo wörld\nCafé résumé\nこんにちは\n";
        ComparePositions(text, Encoding.UTF8);
    }

    /// <summary>
    /// GB2312 (issue #688) is the first offered encoding that is neither single-byte nor Unicode: two
    /// bytes per Chinese character, one per ASCII character, mixed within a line. The direct reader
    /// advances its byte position by <c>Encoding.GetByteCount</c> over the decoded chars, so a wrong
    /// assumption there would drift the position from the second line on — and every buffer boundary,
    /// rollover check and .lxp scroll position is computed from it.
    /// </summary>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void Position_MatchesSystemReader_Gb2312_MixedAsciiAndChinese ()
    {
        var text = "2026-07-30 错误: 连接失败\nINFO 启动完成\nplain ascii line\n警告\n";
        ComparePositions(text, EncodingRegistry.GetEncoding(EncodingRegistry.CODE_PAGE_GB2312));
    }

    private static void ComparePositions (string text, Encoding encoding)
    {
        var bytes = encoding.GetBytes(text);

        using var s1 = new MemoryStream(bytes);
        using var s2 = new MemoryStream(bytes);
        using var systemReader = new PositionAwareStreamReaderSystem(s1, new EncodingOptions { Encoding = encoding }, 500);
        using var directReader = new PositionAwareStreamReaderDirect(s2, new EncodingOptions { Encoding = encoding }, 500);

        var lineNum = 0;
        while (true)
        {
            var systemHasLine = systemReader.TryReadLine(out var systemLine);
            var directHasLine = directReader.TryReadLine(out var directLine);

            Assert.That(directHasLine, Is.EqualTo(systemHasLine), $"Line {lineNum}: EOF mismatch");

            if (!systemHasLine)
            {
                break;
            }

            Assert.That(directLine.Span.ToString(), Is.EqualTo(systemLine.Span.ToString()),
                $"Line {lineNum}: content mismatch");
            Assert.That(directReader.Position, Is.EqualTo(systemReader.Position),
                $"Line {lineNum}: position mismatch (System={systemReader.Position}, Direct={directReader.Position})");

            lineNum++;
        }
    }

    #endregion

    #region Content parity with System reader

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Unit Tests")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "Unit Tests")]
    public void ContentParity_LargeFile ()
    {
        var sb = new StringBuilder();
        var rng = new Random(42);
        for (var i = 0; i < 10_000; i++)
        {
            _ = sb.Append("2026-04-23 12:00:00.");
            _ = sb.Append(i % 1000);
            _ = sb.Append(" [INFO] Thread-");
            _ = sb.Append(rng.Next(1, 32));
            _ = sb.Append(" SomeClass - Log message number ");
            _ = sb.AppendLine(i.ToString());
        }

        var text = sb.ToString();
        var bytes = Encoding.UTF8.GetBytes(text);

        using var s1 = new MemoryStream(bytes);
        using var s2 = new MemoryStream(bytes);
        using var systemReader = new PositionAwareStreamReaderSystem(s1, new EncodingOptions { Encoding = Encoding.UTF8 }, 500);
        using var directReader = new PositionAwareStreamReaderDirect(s2, new EncodingOptions { Encoding = Encoding.UTF8 }, 500);

        var lineNum = 0;
        while (systemReader.TryReadLine(out var systemLine))
        {
            Assert.That(directReader.TryReadLine(out var directLine), Is.True, $"Direct reader ended early at line {lineNum}");
            Assert.That(directLine.Span.ToString(), Is.EqualTo(systemLine.Span.ToString()), $"Line {lineNum}: content mismatch");
            lineNum++;
        }

        Assert.That(directReader.TryReadLine(out _), Is.False, "Direct reader has extra lines");
        Assert.That(lineNum, Is.EqualTo(10_000));
    }

    #endregion

    #region DetachCharBlocks

    [Test]
    public void DetachCharBlocks_ReturnsCompletedBlocks ()
    {
        // Create enough content to fill multiple blocks
        var sb = new StringBuilder();
        for (var i = 0; i < 1000; i++)
        {
            _ = sb.AppendLine(new string('X', 80));
        }

        using var stream = CreateStream(sb.ToString());
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

        // Read all lines
        while (reader.TryReadLine(out _))
        {
            // Intentionally empty: consume all lines to advance reader state.
        }

        var blocks = reader.DetachCharBlocks();
        Assert.That(blocks.Count, Is.GreaterThan(0), "Should have completed blocks to detach");

        // Second detach should be empty
        var blocks2 = reader.DetachCharBlocks();
        Assert.That(blocks2.Count, Is.EqualTo(0));
    }

    #endregion

    #region Lines longer than BLOCK_SIZE (32768 chars)

    private const int TEST_TIMEOUT_MS = 5000;

    private static void RunWithTimeout (Action action, int timeoutMs = TEST_TIMEOUT_MS)
    {
        var task = Task.Run(action);
        Assert.That(task.Wait(timeoutMs), Is.True,
            "Test timed out — reader is likely in an infinite loop growing buffers");
    }

    /// <summary>
    /// Reproduces the bug: a single line longer than the internal BLOCK_SIZE (32768 chars)
    /// causes the reader to loop endlessly, growing buffers without making progress.
    /// The user's real-world case: a TSMP XML line with thousands of tetraTalkgroup entries (~100K+ chars).
    /// </summary>
    [Test]
    public void TryReadLine_LineLongerThanBlockSize_ReadsAllLines ()
    {
        RunWithTimeout(() =>
        {
            // Simulate the real scenario: normal log lines, then one massive XML line, then more normal lines
            const int longLineLength = 100_000; // well over BLOCK_SIZE (32768)
            var sb = new StringBuilder();

            // 10 normal lines before the long one
            for (var i = 0; i < 10; i++)
            {
                _ = sb.AppendLine(CultureInfo.InvariantCulture, $"2026-05-06 12:00:00.{i:D3} [INFO] Normal log line {i}");
            }

            // The problematic line: simulates a massive XML payload (no internal newlines)
            _ = sb.Append("<protocol><talkgroupList>");
            _ = sb.Append(new string('X', longLineLength - 50)); // fill to target length
            _ = sb.AppendLine("</talkgroupList></protocol>");

            // 10 normal lines after the long one
            for (var i = 0; i < 10; i++)
            {
                _ = sb.AppendLine(CultureInfo.InvariantCulture, $"2026-05-06 12:00:01.{i:D3} [INFO] After long line {i}");
            }

            var text = sb.ToString();
            using var stream = CreateStream(text);
            using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), longLineLength + 100);

            var lines = new List<string>();
            while (reader.TryReadLine(out var line))
            {
                lines.Add(line.Span.ToString());
            }

            Assert.That(lines.Count, Is.EqualTo(21), "Should read all 21 lines (10 + 1 long + 10)");
            Assert.That(lines[0], Does.StartWith("2026-05-06 12:00:00.000"));
            Assert.That(lines[10], Does.StartWith("<protocol><talkgroupList>"));
            Assert.That(lines[10].Length, Is.GreaterThan(longLineLength - 50));
            Assert.That(lines[20], Does.StartWith("2026-05-06 12:00:01.009"));
        });
    }

    [Test]
    public void TryReadLine_LineExactlyAtBlockSize_ReadsCorrectly ()
    {
        RunWithTimeout(() =>
        {
            // Line of exactly 32768 chars (the BLOCK_SIZE boundary)
            const int lineLength = 32_768;
            var longLine = new string('Z', lineLength);
            var text = $"before\n{longLine}\nafter\n";

            using var stream = CreateStream(text);
            using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), lineLength + 100);

            Assert.That(reader.TryReadLine(out var line1), Is.True);
            Assert.That(line1.Span.ToString(), Is.EqualTo("before"));

            Assert.That(reader.TryReadLine(out var line2), Is.True);
            Assert.That(line2.Length, Is.EqualTo(lineLength));

            Assert.That(reader.TryReadLine(out var line3), Is.True);
            Assert.That(line3.Span.ToString(), Is.EqualTo("after"));

            Assert.That(reader.TryReadLine(out _), Is.False);
        });
    }

    [Test]
    public void TryReadLine_LineSlightlyOverBlockSize_ReadsCorrectly ()
    {
        RunWithTimeout(() =>
        {
            // Line just 1 char over the block boundary
            const int lineLength = 32_769; // BLOCK_SIZE + 1
            var longLine = new string('Y', lineLength);
            var text = $"before\n{longLine}\nafter\n";

            using var stream = CreateStream(text);
            using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), lineLength + 100);

            Assert.That(reader.TryReadLine(out var line1), Is.True);
            Assert.That(line1.Span.ToString(), Is.EqualTo("before"));

            Assert.That(reader.TryReadLine(out var line2), Is.True);
            Assert.That(line2.Length, Is.EqualTo(lineLength));

            Assert.That(reader.TryReadLine(out var line3), Is.True);
            Assert.That(line3.Span.ToString(), Is.EqualTo("after"));

            Assert.That(reader.TryReadLine(out _), Is.False);
        });
    }

    [Test]
    public void TryReadLine_LineMultipleTimesBlockSize_ReadsCorrectly ()
    {
        RunWithTimeout(() =>
        {
            // Line that spans multiple block doublings: 200K chars (~6x BLOCK_SIZE)
            const int lineLength = 200_000;
            var longLine = new string('W', lineLength);
            var text = $"first\n{longLine}\nlast\n";

            using var stream = CreateStream(text);
            using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), lineLength + 100);

            Assert.That(reader.TryReadLine(out var line1), Is.True);
            Assert.That(line1.Span.ToString(), Is.EqualTo("first"));

            Assert.That(reader.TryReadLine(out var line2), Is.True);
            Assert.That(line2.Length, Is.EqualTo(lineLength));
            Assert.That(line2.Span[0], Is.EqualTo('W'));
            Assert.That(line2.Span[lineLength - 1], Is.EqualTo('W'));

            Assert.That(reader.TryReadLine(out var line3), Is.True);
            Assert.That(line3.Span.ToString(), Is.EqualTo("last"));

            Assert.That(reader.TryReadLine(out _), Is.False);
        });
    }

    [Test]
    public void TryReadLine_LineLongerThanBlockSize_PositionParityWithSystemReader ()
    {
        RunWithTimeout(() =>
        {
            // Verify byte positions match between Direct and System readers for long lines
            const int lineLength = 50_000;
            var text = $"short\n{new string('Q', lineLength)}\nend\n";
            var bytes = Encoding.UTF8.GetBytes(text);

            using var s1 = new MemoryStream(bytes);
            using var s2 = new MemoryStream(bytes);
            // Note: System reader has a pre-existing bug in CharBlockAllocator.ReturnAll() for
            // oversized lines (allocates with new[] but tries to return to ArrayPool). Suppress
            // disposal to avoid that unrelated failure.
            var systemReader = new PositionAwareStreamReaderSystem(s1, new EncodingOptions { Encoding = Encoding.UTF8 }, lineLength + 100);
            using var directReader = new PositionAwareStreamReaderDirect(s2, new EncodingOptions { Encoding = Encoding.UTF8 }, lineLength + 100);

            try
            {
                var lineNum = 0;
                while (true)
                {
                    var systemHasLine = systemReader.TryReadLine(out var systemLine);
                    var directHasLine = directReader.TryReadLine(out var directLine);

                    Assert.That(directHasLine, Is.EqualTo(systemHasLine), $"Line {lineNum}: EOF mismatch");
                    if (!systemHasLine)
                    {
                        break;
                    }

                    Assert.That(directLine.Span.ToString(), Is.EqualTo(systemLine.Span.ToString()), $"Line {lineNum}: content mismatch");
                    Assert.That(directReader.Position, Is.EqualTo(systemReader.Position), $"Line {lineNum}: position mismatch (System={systemReader.Position}, Direct={directReader.Position})");

                    lineNum++;
                }

                Assert.That(lineNum, Is.EqualTo(3));
            }
            finally
            {
                // Don't call Dispose on systemReader — CharBlockAllocator has a pre-existing
                // bug returning oversized (new[]) arrays to ArrayPool.
                // The GC will reclaim the memory.
            }
        });
    }

    [Test]
    public void TryReadLine_LineLongerThanBlockSize_WithMaxLineLengthTruncation ()
    {
        RunWithTimeout(() =>
        {
            // Long line that exceeds MaximumLineLength — should truncate but not hang
            const int lineLength = 100_000;
            const int maxLineLength = 20_000; // typical LogExpert setting
            var longLine = new string('T', lineLength);
            var text = $"before\n{longLine}\nafter\n";

            using var stream = CreateStream(text);
            using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), maxLineLength);

            Assert.That(reader.TryReadLine(out var line1), Is.True);
            Assert.That(line1.Span.ToString(), Is.EqualTo("before"));

            Assert.That(reader.TryReadLine(out var line2), Is.True);
            Assert.That(line2.Length, Is.EqualTo(maxLineLength), "Long line should be truncated to MaximumLineLength");

            Assert.That(reader.TryReadLine(out var line3), Is.True);
            Assert.That(line3.Span.ToString(), Is.EqualTo("after"));

            Assert.That(reader.TryReadLine(out _), Is.False);
        });
    }

    [Test]
    public void TryReadLine_LineLongerThanBlockSize_DetachCharBlocksAfterEachLine ()
    {
        RunWithTimeout(() =>
        {
            // Simulates the real LogfileReader pattern: DetachBlocks() is called after reading lines.
            // This tests the interaction between grown buffers and DetachBlocks.
            const int lineLength = 100_000;
            var longLine = new string('D', lineLength);
            var text = $"line1\n{longLine}\nline3\n";

            using var stream = CreateStream(text);
            using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), lineLength + 100);

            Assert.That(reader.TryReadLine(out var line1), Is.True);
            Assert.That(line1.Span.ToString(), Is.EqualTo("line1"));
            var blocks1 = reader.DetachCharBlocks();
            Assert.That(blocks1.Count, Is.GreaterThan(0));

            Assert.That(reader.TryReadLine(out var line2), Is.True);
            Assert.That(line2.Length, Is.EqualTo(lineLength));
            var blocks2 = reader.DetachCharBlocks();
            Assert.That(blocks2.Count, Is.GreaterThan(0));

            Assert.That(reader.TryReadLine(out var line3), Is.True);
            Assert.That(line3.Span.ToString(), Is.EqualTo("line3"));

            Assert.That(reader.TryReadLine(out _), Is.False);
        }, 10000);
    }

    [Test]
    public void TryReadLine_LineLongerThanBlockSize_DetachCharBlocksWithLargeTail ()
    {
        RunWithTimeout(() =>
        {
            // KEY BUG TEST: After reading a long line, the internal buffer has grown (e.g. 128K).
            // If the file has lots of data AFTER the long line, the "tail" (unscanned data after
            // the long line's \n) can exceed BLOCK_SIZE. DetachBlocks() rents only BLOCK_SIZE
            // for the carry-over buffer, causing a buffer overflow / ArgumentOutOfRangeException.
            //
            // To trigger this: line length of ~65536 forces buffer to grow to 131072 (via
            // ArrayPool bucket). After reading the line, available space reads ~65K of subsequent
            // data into the buffer. After _scanOffset advances past the line, tail > BLOCK_SIZE.
            const int longLineLength = 65_536; // Forces buffer growth to 131072
            var sb = new StringBuilder();

            // One short line first (so DetachBlocks resets to fresh BLOCK_SIZE before long line)
            _ = sb.AppendLine("prefix");

            // The long line — forces buffer to grow to 128K+
            _ = sb.Append(new string('L', longLineLength));
            _ = sb.Append('\n');

            // Lots of short lines AFTER — ensures the stream has data to fill buffer tail
            // We need > 32768 chars of data after the long line's \n
            for (var i = 0; i < 1500; i++)
            {
                _ = sb.AppendLine(CultureInfo.InvariantCulture, $"[{i:D4}] padding-line-to-fill-buffer-tail-past-BLOCK_SIZE-boundary");
            }

            var text = sb.ToString();
            using var stream = CreateStream(text);
            using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), longLineLength + 100);

            // Read and detach the prefix (resets reader to fresh BLOCK_SIZE buffer)
            Assert.That(reader.TryReadLine(out var line1), Is.True);
            Assert.That(line1.Span.ToString(), Is.EqualTo("prefix"));
            _ = reader.DetachCharBlocks();

            // Read the long line — this grows the buffer to 131072
            Assert.That(reader.TryReadLine(out var line2), Is.True);
            Assert.That(line2.Length, Is.EqualTo(longLineLength));

            // THIS IS THE CRITICAL CALL: DetachBlocks must handle tail > BLOCK_SIZE
            var blocks = reader.DetachCharBlocks();
            Assert.That(blocks.Count, Is.GreaterThan(0));

            // Verify we can still read subsequent lines correctly
            var count = 0;
            while (reader.TryReadLine(out var line))
            {
                count++;
                if (count == 1)
                {
                    Assert.That(line.Span.ToString(), Does.StartWith("[0000]"));
                }
            }

            Assert.That(count, Is.EqualTo(1500), "All lines after the long line should be readable");
        }, 10000);
    }

    #endregion

    #region Line ending handling (bare CR, mixed)

    private static List<string> ReadAllLines (string text, Encoding? encoding = null, int maximumLineLength = 500)
    {
        var enc = encoding ?? Encoding.UTF8;
        using var stream = CreateStream(text, enc);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions { Encoding = enc }, maximumLineLength);
        var result = new List<string>();
        while (reader.TryReadLine(out var line))
        {
            result.Add(line.Span.ToString());
        }

        return result;
    }

    private static List<(string Content, long Position)> ReadAllWithPositions (string text, Encoding? encoding = null)
    {
        var enc = encoding ?? Encoding.UTF8;
        using var stream = CreateStream(text, enc);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions { Encoding = enc }, 500);
        var result = new List<(string, long)>();
        while (reader.TryReadLine(out var line))
        {
            result.Add((line.Span.ToString(), reader.Position));
        }

        return result;
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void TryReadLine_BareCarriageReturn_SplitsLines ()
    {
        var lines = ReadAllLines("Line 1\rLine 2\rLine 3");
        Assert.That(lines, Is.EqualTo(["Line 1", "Line 2", "Line 3"]));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void TryReadLine_MixedLineEndings_SplitsLines ()
    {
        // \r\n, \n, bare \r all in one file
        var lines = ReadAllLines("a\r\nb\nc\rd\n");
        Assert.That(lines, Is.EqualTo(["a", "b", "c", "d"]));
    }

    [Test]
    [TestCase("\r\r\r", 3)]
    [TestCase("\r\n\r\n\r\n", 3)]
    [TestCase("\n\n\n", 3)]
    public void TryReadLine_RepeatedTerminators_YieldEmptyLines (string text, int expectedCount)
    {
        var lines = ReadAllLines(text);
        Assert.That(lines.Count, Is.EqualTo(expectedCount));
        Assert.That(lines, Has.All.Empty);
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void TryReadLine_TrailingBareCR_NoSpuriousEmptyLine ()
    {
        var lines = ReadAllLines("abc\r");
        Assert.That(lines, Is.EqualTo(["abc"]));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void TryReadLine_TrailingCrLf_NoSpuriousEmptyLine ()
    {
        var lines = ReadAllLines("abc\r\n");
        Assert.That(lines, Is.EqualTo(["abc"]));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void TryReadLine_LfThenTrailingCr_YieldsEmptyLineBetween ()
    {
        // "abc\n\r": \n terminates "abc", then a standalone \r yields an empty final line
        var lines = ReadAllLines("abc\n\r");
        Assert.That(lines, Is.EqualTo(["abc", ""]));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void Position_BareCarriageReturn_ExactBytes ()
    {
        // "abc\rdef" UTF-8: "abc"(3) + \r(1) = 4; "def"(3) at EOF = 7
        var result = ReadAllWithPositions("abc\rdef");
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(("abc", 4L)));
        Assert.That(result[1], Is.EqualTo(("def", 7L)));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void Position_MixedLineEndings_ExactBytes ()
    {
        // "a\r\nb\nc\rd\n": a+\r\n=3, b+\n=5, c+\r=7, d+\n=9
        var result = ReadAllWithPositions("a\r\nb\nc\rd\n");
        Assert.That(result.Select(r => r.Position).ToArray(), Is.EqualTo([3L, 5L, 7L, 9L]));
        Assert.That(result.Select(r => r.Content).ToArray(), Is.EqualTo(["a", "b", "c", "d"]));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void Position_MixedLineEndings_Multibyte_ExactBytes ()
    {
        // "Héllo\rwörld\n" UTF-8: "Héllo"=6 bytes + \r(1) = 7; "wörld"=6 bytes + \n(1) = 14
        var result = ReadAllWithPositions("Héllo\rwörld\n");
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(("Héllo", 7L)));
        Assert.That(result[1], Is.EqualTo(("wörld", 14L)));
    }

    [Test]
    public void TryReadLine_BareCrAtBlockBoundary_SplitsCorrectly ()
    {
        // \r lands exactly at the last char of the first 32 KB block, followed by more data.
        const int blockSize = 32_768;
        var firstLine = new string('A', blockSize - 1); // \r will be char at index blockSize-1 (last in block)
        var text = firstLine + "\r" + "rest\nlast\n";

        var lines = ReadAllLines(text, maximumLineLength: blockSize + 100);
        Assert.That(lines.Count, Is.EqualTo(3));
        Assert.That(lines[0], Is.EqualTo(firstLine));
        Assert.That(lines[1], Is.EqualTo("rest"));
        Assert.That(lines[2], Is.EqualTo("last"));
    }

    [Test]
    public void TryReadLine_CrLfStraddlesBlockBoundary_SplitsCorrectly ()
    {
        // \r is the last char of the first block, \n is the first char of the next block.
        const int blockSize = 32_768;
        var firstLine = new string('A', blockSize - 1);
        var text = firstLine + "\r\n" + "rest\nlast\n";

        var lines = ReadAllLines(text, maximumLineLength: blockSize + 100);
        Assert.That(lines.Count, Is.EqualTo(3));
        Assert.That(lines[0], Is.EqualTo(firstLine));
        Assert.That(lines[1], Is.EqualTo("rest"));
        Assert.That(lines[2], Is.EqualTo("last"));
    }

    #endregion
}