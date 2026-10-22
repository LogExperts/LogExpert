using System.Text;

using LogExpert.Core.Classes.Log.Streamreaders;
using LogExpert.Core.Entities;
using LogExpert.Core.Helpers;
using LogExpert.Core.Interfaces;

using NUnit.Framework;

namespace LogExpert.Tests.StreamReaderTests;

[TestFixture]
public class LogStreamReaderTest
{
    [Test]
    [TestCase("Line 1\nLine 2\nLine 3", 3)]
    [TestCase("Line 1\nLine 2\nLine 3\n", 3)]
    [TestCase("Line 1\r\nLine 2\r\nLine 3", 3)]
    [TestCase("Line 1\r\nLine 2\r\nLine 3\r\n", 3)]
    [TestCase("Line 1\rLine 2\rLine 3", 3)]
    [TestCase("Line 1\rLine 2\rLine 3\r", 3)]
    public void ReadLinesWithSystemNewLine (string text, int expectedLines)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);
        var lineCount = 0;
        while (true)
        {
            var line = reader.ReadLine();
            if (line == null)
            {
                break;
            }

            lineCount += 1;

            Assert.That(line.StartsWith($"Line {lineCount}", StringComparison.OrdinalIgnoreCase), $"Invalid line: {line}");
        }

        Assert.That(expectedLines, Is.EqualTo(lineCount), $"Unexpected lines:\n{text}");
    }

    [Test]
    [TestCase("\n\n\n", 3)]
    [TestCase("\r\n\r\n\r\n", 3)]
    [TestCase("\r\r\r", 3)]
    public void CountLinesWithSystemNewLine (string text, int expectedLines)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);
        var lineCount = 0;
        while (reader.ReadLine() != null)
        {
            lineCount += 1;
        }

        Assert.That(expectedLines, Is.EqualTo(lineCount), $"Unexpected lines:\n{text}");
    }

    [Test]
    [TestCase("Line 1\nLine 2\nLine 3", 3)]
    [TestCase("Line 1\nLine 2\nLine 3\n", 3)]
    [TestCase("Line 1\r\nLine 2\r\nLine 3", 3)]
    [TestCase("Line 1\r\nLine 2\r\nLine 3\r\n", 3)]
    [TestCase("Line 1\rLine 2\rLine 3", 3)]
    [TestCase("Line 1\rLine 2\rLine 3\r", 3)]
    public void ReadLinesWithLegacyNewLine (string text, int expectedLines)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
        using var reader = new PositionAwareStreamReaderLegacy(stream, new EncodingOptions(), 500);
        var lineCount = 0;
        while (true)
        {
            var line = reader.ReadLine();
            if (line == null)
            {
                break;
            }

            lineCount += 1;

            Assert.That(line.StartsWith($"Line {lineCount}", StringComparison.OrdinalIgnoreCase), $"Invalid line: {line}");
        }

        Assert.That(expectedLines, Is.EqualTo(lineCount), $"Unexpected lines:\n{text}");
    }
    [Test]
    [TestCase("\n\n\n", 3)]
    [TestCase("\r\n\r\n\r\n", 3)]
    [TestCase("\r\r\r", 3)]
    public void CountLinesWithLegacyNewLine (string text, int expectedLines)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
        using var reader = new PositionAwareStreamReaderLegacy(stream, new EncodingOptions(), 500);
        var lineCount = 0;
        while (reader.ReadLine() != null)
        {
            lineCount += 1;
        }

        Assert.That(expectedLines, Is.EqualTo(lineCount), $"Unexpected lines:\n{text}");
    }

    [Test]
    [TestCase("Line 1\nLine 2\nLine 3", 3)]
    [TestCase("Line 1\nLine 2\nLine 3\n", 3)]
    [TestCase("Line 1\r\nLine 2\r\nLine 3", 3)]
    [TestCase("Line 1\r\nLine 2\r\nLine 3\r\n", 3)]
    [TestCase("Line 1\rLine 2\rLine 3", 3)]
    [TestCase("Line 1\rLine 2\rLine 3\r", 3)]
    public void TryReadLine_ReturnsCorrectContent_SystemReader (string text, int expectedLines)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);
        var lineCount = 0;
        while (reader.TryReadLine(out var lineMemory))
        {
            lineCount++;
            var line = lineMemory.Span.ToString();
            Assert.That(line.StartsWith($"Line {lineCount}", StringComparison.OrdinalIgnoreCase), $"Invalid line: {line}");
        }

        Assert.That(lineCount, Is.EqualTo(expectedLines));
    }

    [Test]
    [TestCase("\n\n\n", 3)]
    [TestCase("\r\n\r\n\r\n", 3)]
    [TestCase("\r\r\r", 3)]
    public void TryReadLine_CountsEmptyLines_SystemReader (string text, int expectedLines)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);
        var lineCount = 0;
        while (reader.TryReadLine(out _))
        {
            lineCount++;
        }

        Assert.That(lineCount, Is.EqualTo(expectedLines));
    }

    [Test]
    public void TryReadLine_TracksPositionCorrectly_SystemReader ()
    {
        var text = "Line 1\nLine 2\nLine 3\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        Assert.That(reader.TryReadLine(out var line1), Is.True);
        Assert.That(line1.Span.ToString(), Is.EqualTo("Line 1"));
        Assert.That(reader.Position, Is.EqualTo(7)); // "Line 1\n" = 7 bytes

        Assert.That(reader.TryReadLine(out var line2), Is.True);
        Assert.That(line2.Span.ToString(), Is.EqualTo("Line 2"));
        Assert.That(reader.Position, Is.EqualTo(14));

        Assert.That(reader.TryReadLine(out var line3), Is.True);
        Assert.That(line3.Span.ToString(), Is.EqualTo("Line 3"));
        Assert.That(reader.Position, Is.EqualTo(21));

        Assert.That(reader.TryReadLine(out _), Is.False); // EOF
    }

    [Test]
    public void TryReadLine_ReturnsBlockBackedMemory_NotStringBacked ()
    {
        var text = "Hello World\nSecond Line\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        Assert.That(reader.TryReadLine(out var lineMemory), Is.True);

        // Verify the memory is backed by a char[] from the block allocator, not a string.
        // MemoryMarshal.TryGetArray succeeds for array-backed Memory but fails for string-backed Memory.
        var success = System.Runtime.InteropServices.MemoryMarshal.TryGetArray(lineMemory, out var segment);
        Assert.That(success, Is.True, "Memory should be backed by a char[] (block-allocated), not a string");
        Assert.That(segment.Array, Is.Not.Null);
    }

    [Test]
    public void TryReadLine_DetachCharBlocks_ReturnsFilledBlocks_AfterReading ()
    {
        var text = "Line 1\nLine 2\nLine 3\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        while (reader.TryReadLine(out _))
        {
            // Intentionally empty: consume all lines to advance reader state.
        }

        // Reading rented at least one block, exposed only through the seam.
        Assert.That(reader.DetachCharBlocks(), Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void TryReadLine_DetachCharBlocks_TransfersOwnership ()
    {
        var text = "Line 1\nLine 2\nLine 3\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        while (reader.TryReadLine(out _))
        {
            // Intentionally empty: consume all lines to advance reader state.
        }

        var blocks = reader.DetachCharBlocks();
        Assert.That(blocks, Has.Count.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void TryReadLine_MatchesReadLine_ContentAndPosition ()
    {
        // Verify TryReadLine produces identical content and position tracking as ReadLine
        var text = "2026-04-23 12:00:00 [INFO] Thread-1 SomeClass - Message 1\n" +
                   "2026-04-23 12:00:01 [WARN] Thread-2 OtherClass - Message 2\n" +
                   "Short\n" +
                   "\n" + // empty line
                   "Last line";

        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var readLineReader = new PositionAwareStreamReaderSystem(stream1, new EncodingOptions(), 500);
        using var tryReadLineReader = new PositionAwareStreamReaderSystem(stream2, new EncodingOptions(), 500);

        while (true)
        {
            var stringLine = readLineReader.ReadLine();
            var tryResult = tryReadLineReader.TryReadLine(out var memoryLine);

            if (stringLine == null)
            {
                Assert.That(tryResult, Is.False, "TryReadLine should return false at EOF when ReadLine returns null");
                break;
            }

            Assert.That(tryResult, Is.True);
            Assert.That(memoryLine.Span.ToString(), Is.EqualTo(stringLine), "Content mismatch between ReadLine and TryReadLine");
            Assert.That(tryReadLineReader.Position, Is.EqualTo(readLineReader.Position), "Position mismatch between ReadLine and TryReadLine");
        }
    }

    [Test]
    public void TryReadLine_RespectsMaximumLineLength ()
    {
        var longLine = new string('X', 1000);
        var text = $"{longLine}\nShort\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        Assert.That(reader.TryReadLine(out var lineMemory), Is.True);
        Assert.That(lineMemory.Length, Is.EqualTo(500), "Line should be truncated to MaximumLineLength");

        Assert.That(reader.TryReadLine(out var line2), Is.True);
        Assert.That(line2.Span.ToString(), Is.EqualTo("Short"));
    }

    [Test]
    public void TryReadLine_UTF8_MultiByteCharacters ()
    {
        // Japanese characters: 3 bytes each in UTF-8
        var text = "日本語テスト\nLine 2\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions { Encoding = Encoding.UTF8 }, 500);

        Assert.That(reader.TryReadLine(out var line1), Is.True);
        Assert.That(line1.Span.ToString(), Is.EqualTo("日本語テスト"));
        Assert.That(reader.Position, Is.EqualTo(Encoding.UTF8.GetByteCount("日本語テスト\n")));

        Assert.That(reader.TryReadLine(out var line2), Is.True);
        Assert.That(line2.Span.ToString(), Is.EqualTo("Line 2"));
    }

    [Test]
    [TestCase("Line 1\nLine 2\nLine 3", 3)]
    [TestCase("Line 1\r\nLine 2\r\nLine 3", 3)]
    [TestCase("Line 1\rLine 2\rLine 3", 3)]
    public void TryReadLine_LegacyReader_ReadsAllLines (string text, int expectedLines)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
        using var reader = new PositionAwareStreamReaderLegacy(stream, new EncodingOptions(), 500);

        var memoryReader = reader as ILogStreamReaderMemory;
        Assert.That(memoryReader, Is.Not.Null, "Legacy reader must implement ILogStreamReaderMemory");

        var lineCount = 0;
        while (memoryReader!.TryReadLine(out var lineMemory))
        {
            lineCount++;
            Assert.That(lineMemory.Span.ToString(), Does.StartWith($"Line {lineCount}"));
        }

        Assert.That(lineCount, Is.EqualTo(expectedLines));
    }

    /// <summary>
    /// The legacy reader advances its position per character read, by a per-encoding step
    /// (<c>GetPosIncPrecomputed</c>). GB2312 (issue #688) is the first offered encoding that is neither
    /// single-byte nor Unicode — one byte per ASCII character, two per Chinese one — so a step of "1
    /// byte unless UTF-8 or UTF-16" drifted the position on the first Chinese character and every
    /// subsequent line started at the wrong offset.
    /// </summary>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void TryReadLine_LegacyReader_Gb2312_TracksTheBytePositionPerLine ()
    {
        var gb2312 = EncodingRegistry.GetEncoding(EncodingRegistry.CODE_PAGE_GB2312);
        string[] lines = ["错误: 连接失败", "INFO 启动完成", "plain ascii", "警告"];
        var text = string.Join("\n", lines) + "\n";

        using var stream = new MemoryStream(gb2312.GetBytes(text));
        using var reader = new PositionAwareStreamReaderLegacy(stream, new EncodingOptions { Encoding = gb2312 }, 500);

        var expectedPosition = 0;

        Assert.Multiple(() =>
        {
            foreach (var line in lines)
            {
                expectedPosition += gb2312.GetByteCount(line + "\n");

                Assert.That(reader.TryReadLine(out var lineMemory), Is.True);
                Assert.That(lineMemory.Span.ToString(), Is.EqualTo(line));
                Assert.That(reader.Position, Is.EqualTo(expectedPosition), $"position drifted after '{line}'");
            }
        });
    }

    /// <summary>
    /// A step of 0 means "measure the character", which is the only correct answer for a variable-width
    /// encoding. Pinned per offered encoding so a newly offered one cannot silently get a fixed step.
    /// </summary>
    [Test]
    public void GetPosIncPrecomputed_IsAFixedStepOnlyForFixedWidthEncodings ()
    {
        Assert.Multiple(() =>
        {
            foreach (var encoding in EncodingRegistry.OfferedEncodings)
            {
                var step = PositionAwareStreamReaderBase.GetPosIncPrecomputed(encoding);

                if (step != 0)
                {
                    Assert.That(
                        encoding.GetByteCount("a"),
                        Is.EqualTo(step),
                        $"'{encoding.HeaderName}' is credited a fixed {step} byte(s) per character");
                    Assert.That(
                        encoding.IsSingleByte || encoding is UnicodeEncoding,
                        Is.True,
                        $"'{encoding.HeaderName}' is variable-width, so its step has to be measured");
                }
            }
        });
    }
}
