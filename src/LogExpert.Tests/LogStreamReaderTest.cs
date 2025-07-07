using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;

using NUnit.Framework;

using System.IO;
using System.Text;

namespace LogExpert.Tests;

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
    public void ReadLinesWithSystemNewLine(string text, int expectedLines)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions());
        var lineCount = 0;
        while (true)
        {
            var line = reader.ReadLine();
            if (line == null)
            {
                break;
            }

            lineCount += 1;

            Assert.That(line.StartsWith($"Line {lineCount}"), $"Invalid line: {line}");
        }

        Assert.That(expectedLines, Is.EqualTo(lineCount), $"Unexpected lines:\n{text}");
    }

    [Test]
    [TestCase("\n\n\n", 3)]
    [TestCase("\r\n\r\n\r\n", 3)]
    [TestCase("\r\r\r", 3)]
    public void CountLinesWithSystemNewLine(string text, int expectedLines)
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions());
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
    public void ReadLinesWithLegacyNewLine(string text, int expectedLines)
    {
        using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(text)))
        using (var reader = new PositionAwareStreamReaderLegacy(stream, new EncodingOptions()))
        {
            var lineCount = 0;
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }

                lineCount += 1;

                Assert.That(line.StartsWith($"Line {lineCount}"), $"Invalid line: {line}");
            }

            Assert.That(expectedLines, Is.EqualTo(lineCount), $"Unexpected lines:\n{text}");
        }
    }
    [Test]
    [TestCase("\n\n\n", 3)]
    [TestCase("\r\n\r\n\r\n", 3)]
    [TestCase("\r\r\r", 3)]
    public void CountLinesWithLegacyNewLine(string text, int expectedLines)
    {
        using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(text)))
        using (var reader = new PositionAwareStreamReaderLegacy(stream, new EncodingOptions()))
        {
            var lineCount = 0;
            while (reader.ReadLine() != null)
            {
                lineCount += 1;
            }


            Assert.That(expectedLines, Is.EqualTo(lineCount), $"Unexpected lines:\n{text}");
        }
    }
}
