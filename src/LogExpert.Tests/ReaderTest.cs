using LogExpert.Core.Classes.Log.Streamreaders;
using LogExpert.Core.Entities;

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

    #region TryReadLine line endings

    [Test]
    public void TryReadLine_CrLf_NoTrailingNewline_ReadsAllLines ()
    {
        // File with CRLF between lines but NO trailing newline on last line
        var content = "line1\r\nline2"u8.ToArray();
        using var stream = new MemoryStream(content);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

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
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

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
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

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
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

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
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

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
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

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
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

        Assert.That(reader.TryReadLine(out var line1), Is.True);
        Assert.That(line1.ToString(), Is.EqualTo("AB"));
        Assert.That(reader.Position, Is.EqualTo(4), "After 'AB\\r\\n', position should be 4");

        Assert.That(reader.TryReadLine(out var line2), Is.True);
        Assert.That(line2.ToString(), Is.EqualTo("CD"));
        // The last line has no newline, so only its content bytes count (the removed System reader added
        // a guessed newline length here, which broke seeking on buffer re-read).
        Assert.That(reader.Position, Is.EqualTo(6), "After 'CD' at EOF, position should be 6");
    }

    [Test]
    public void TryReadLine_SingleLine_NoNewline_ReadsLine ()
    {
        // Single line file with no newline at all
        var content = "onlyline"u8.ToArray();
        using var stream = new MemoryStream(content);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions(), 500);

        var lines = ReadAllLines(reader);

        Assert.That(lines, Has.Count.EqualTo(1), $"Expected 1 line, got: [{string.Join("|", lines)}]");
        Assert.That(lines[0], Is.EqualTo("onlyline"));
    }

    private static List<string> ReadAllLines (PositionAwareStreamReaderDirect reader)
    {
        List<string> lines = [];
        while (reader.TryReadLine(out var lineMemory))
        {
            lines.Add(lineMemory.ToString());
        }

        return lines;
    }

    #endregion
}