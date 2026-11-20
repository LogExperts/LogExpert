using System.Text;

using LogExpert.Core.Classes.Log.Streamreaders;
using LogExpert.Core.Entities;

using NUnit.Framework;

namespace LogExpert.Tests.StreamReaderTests;

// The raw bytes as oracle for the Direct reader (ADR 0010): a line ends at the next \n, \r\n or bare
// \r code unit of the encoding, its text is that line's bytes decoded on their own, and the position
// after it is the byte offset after its terminator.
internal static class RawByteLineOracle
{
    public const int MAX_LINE_LENGTH = 500_000;

    public static void AssertMatches (byte[] data, Encoding encoding)
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

    public static List<(string Content, long End)> ReadAll (byte[] data, Encoding encoding)
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

    // A trailing line without terminator is a line; an empty remainder after the last terminator is not.
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
}