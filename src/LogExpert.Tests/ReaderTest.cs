using System.Text;

using LogExpert.Core.Classes.Log.Streamreaders;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;

using NUnit.Framework;

namespace LogExpert.Tests;

[TestFixture]
internal class ReaderTest
{
    [TearDown]
    public void TearDown ()
    {
    }

    [OneTimeSetUp]
    public void Boot ()
    {
    }

    #region GuessNewLineSequenceLength / TryReadLine tests

    [Test]
    public void TryReadLine_CrLf_NoTrailingNewline_ReadsAllLines ()
    {
        // File with CRLF between lines but NO trailing newline on last line
        var content = "line1\r\nline2"u8.ToArray();
        using var stream = new MemoryStream(content);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        var lines = ReadAllLines(reader);

        Assert.That(lines, Has.Count.EqualTo(2), $"Expected 2 lines, got: [{string.Join("|", lines)}]");
        Assert.That(lines[0], Is.EqualTo("line1"));
        Assert.That(lines[1], Is.EqualTo("line2"));
    }

    [Test]
    public void TryReadLine_CrLf_WithTrailingNewline_ReadsAllLines ()
    {
        // File with CRLF between lines AND trailing newline
        var content = "line1\r\nline2\r\n"u8.ToArray();
        using var stream = new MemoryStream(content);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        var lines = ReadAllLines(reader);

        Assert.That(lines, Has.Count.EqualTo(2), $"Expected 2 lines, got: [{string.Join("|", lines)}]");
        Assert.That(lines[0], Is.EqualTo("line1"));
        Assert.That(lines[1], Is.EqualTo("line2"));
    }

    [Test]
    public void TryReadLine_CrOnly_NoTrailingNewline_ReadsAllLines ()
    {
        // File with CR-only line endings (like old Mac), no trailing newline
        var content = "line1\rline2"u8.ToArray();
        using var stream = new MemoryStream(content);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        var lines = ReadAllLines(reader);

        Assert.That(lines, Has.Count.EqualTo(2), $"Expected 2 lines, got: [{string.Join("|", lines)}]");
        Assert.That(lines[0], Is.EqualTo("line1"));
        Assert.That(lines[1], Is.EqualTo("line2"));
    }

    [Test]
    public void TryReadLine_LfOnly_NoTrailingNewline_ReadsAllLines ()
    {
        // File with LF-only line endings (Unix), no trailing newline
        var content = "line1\nline2"u8.ToArray();
        using var stream = new MemoryStream(content);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        var lines = ReadAllLines(reader);

        Assert.That(lines, Has.Count.EqualTo(2), $"Expected 2 lines, got: [{string.Join("|", lines)}]");
        Assert.That(lines[0], Is.EqualTo("line1"));
        Assert.That(lines[1], Is.EqualTo("line2"));
    }

    [Test]
    public void TryReadLine_CrLf_ThreeLines_NoTrailingNewline_ReadsAllLines ()
    {
        // 3 lines with CRLF, no trailing newline
        var content = "header\r\ndata1\r\ndata2"u8.ToArray();
        using var stream = new MemoryStream(content);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        var lines = ReadAllLines(reader);

        Assert.That(lines, Has.Count.EqualTo(3), $"Expected 3 lines, got: [{string.Join("|", lines)}]");
        Assert.That(lines[0], Is.EqualTo("header"));
        Assert.That(lines[1], Is.EqualTo("data1"));
        Assert.That(lines[2], Is.EqualTo("data2"));
    }

    [Test]
    public void TryReadLine_CrLf_Position_TracksCorrectly ()
    {
        // Verify position tracking for CRLF file
        var content = "AB\r\nCD\r\n"u8.ToArray(); // 4 + 4 = 8 bytes
        using var stream = new MemoryStream(content);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        Assert.That(reader.TryReadLine(out var line1), Is.True);
        Assert.That(line1.ToString(), Is.EqualTo("AB"));
        Assert.That(reader.Position, Is.EqualTo(4), "After 'AB\\r\\n', position should be 4");

        Assert.That(reader.TryReadLine(out var line2), Is.True);
        Assert.That(line2.ToString(), Is.EqualTo("CD"));
        Assert.That(reader.Position, Is.EqualTo(8), "After 'CD\\r\\n', position should be 8");

        Assert.That(reader.TryReadLine(out _), Is.False, "Should be EOF");
    }

    [Test]
    public void TryReadLine_CrLf_NoTrailingNewline_Position_TracksCorrectly ()
    {
        // Verify position tracking for CRLF file without trailing newline
        var content = "AB\r\nCD"u8.ToArray(); // 4 + 2 = 6 bytes
        using var stream = new MemoryStream(content);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        Assert.That(reader.TryReadLine(out var line1), Is.True);
        Assert.That(line1.ToString(), Is.EqualTo("AB"));
        Assert.That(reader.Position, Is.EqualTo(4), "After 'AB\\r\\n', position should be 4");

        Assert.That(reader.TryReadLine(out var line2), Is.True);
        Assert.That(line2.ToString(), Is.EqualTo("CD"));
        // Last line has no newline, but current implementation adds _newLineSequenceLength anyway.
        // BUG: position is 8 (adds 2 for nonexistent CRLF), should be 6.
        // This causes incorrect seeking on buffer re-read.
        Assert.That(reader.Position, Is.EqualTo(8), "Known issue: overcounts by newline length on last line without trailing newline");
    }

    [Test]
    public void TryReadLine_SingleLine_NoNewline_ReadsLine ()
    {
        // Single line file with no newline at all
        var content = "onlyline"u8.ToArray();
        using var stream = new MemoryStream(content);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 500);

        var lines = ReadAllLines(reader);

        Assert.That(lines, Has.Count.EqualTo(1), $"Expected 1 line, got: [{string.Join("|", lines)}]");
        Assert.That(lines[0], Is.EqualTo("onlyline"));
    }

    private static List<string> ReadAllLines (PositionAwareStreamReaderSystem reader)
    {
        List<string> lines = [];
        while (reader.TryReadLine(out var lineMemory))
        {
            lines.Add(lineMemory.ToString());
        }

        return lines;
    }

    #endregion

    //TODO reimplement
    private void CompareReaderImplementationsInternal (string fileName, Encoding enc, int maxPosition)
    {
        var path = Environment.CurrentDirectory + "\\data\\";
        EncodingOptions encOpts = new()
        {
            Encoding = enc
        };

        using Stream s1 = new FileStream(path + fileName, FileMode.Open, FileAccess.Read);
        using Stream s2 = new FileStream(path + fileName, FileMode.Open, FileAccess.Read);
        using ILogStreamReader r1 = new PositionAwareStreamReaderLegacy(s1, encOpts, 500);
        using ILogStreamReader r2 = new PositionAwareStreamReaderSystem(s2, encOpts, 500);
        for (var lineNum = 0; ; lineNum++)
        {
            var line1 = r1.ReadLine();
            var line2 = r2.ReadLine();
            if (line1 == null && line2 == null)
            {
                break;
            }

            Assert.That(line1, Is.EqualTo(line2), "File " + fileName);

            if (r1.Position != maxPosition)
            {
                Assert.That(r2.Position, Is.EqualTo(r1.Position), "Line " + lineNum + ", File: " + fileName);
            }
            else
            {
                //Its desired that the position of the new implementation is 2 bytes ahead to fix the problem of empty lines every time a new line is appended.
                Assert.That(r2.Position - 2, Is.EqualTo(r1.Position), "Line " + lineNum + ", File: " + fileName);
            }
        }
    }

    //TODO find out why it does not work with appveyor, but works fine with normal environment!
    //[Test]
    //[TestCase("50 MB.txt", "Windows-1252", 50000000)]
    //[TestCase("50 MB UTF16.txt", "Unicode", 49999998)]
    //[TestCase("50 MB UTF8.txt", "UTF-8", 50000000)]
    //public void CompareReaderImplementations(string fileName, string encoding, int maxPosition)
    //{
    //    CompareReaderImplementationsInternal(fileName, Encoding.GetEncoding(encoding), maxPosition);
    //}
}