using LogExpert;
using LogExpert.Extensions;

using NUnit.Framework;

namespace ColumnizerLib.UnitTests.Extensions;

[TestFixture]

internal class LogLineExtensionsTests
{
    private class TestingLogLine : ILogLine
    {
        public string FullLine { get; set; }

        public int LineNumber { get; set; }

        public string Text { get; set; }
    }

    [Test]
    public void ToClipBoardText_ReturnsExpected()
    {
        var underTest = new TestingLogLine
        {
            FullLine = "a fullLine",
            LineNumber = 89,
            Text = "a text"
        };
        Assert.That(underTest.ToClipBoardText(), Is.EqualTo("\t90\ta fullLine"));
    }
}
