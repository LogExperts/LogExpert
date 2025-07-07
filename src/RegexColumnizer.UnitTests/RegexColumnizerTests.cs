using LogExpert;
using Moq;
using NUnit.Framework;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]
namespace RegexColumnizer.UnitTests;

[TestFixture]
public class RegexColumnizerTests
{
    // The same amount of columns should be returned whether the line matches the regex or not.
    [TestCase("5 test message", @"^(?'time'[\d]+)\s+(?'Message'.+)$", 2)]
    [TestCase("Error in com.example.core", @"^(?'time'[\d]+)\s+(?'Message'.+)$", 2)]
    public void SplitLine_ColumnCountMatches(string lineToParse, string regex, int expectedNumberOfColumns)
    {
        Regex1Columnizer columnizer = CreateInitializedColumnizer(regex);

        TestLogLine testLogLine = new(4, lineToParse);
        IColumnizedLogLine parsedLogLine = columnizer.SplitLine(Mock.Of<ILogLineColumnizerCallback>(), testLogLine);

        Assert.That(expectedNumberOfColumns, Is.EqualTo(parsedLogLine.ColumnValues.Length));
    }

    //Using "" for empty string since string.Empty can't be passed to the TestCase attribute.
    [TestCase("5 test message", @"^(?'time'[\d]+)\s+(?'Message'.+)$", 0, "5")]
    [TestCase("5 test message", @"^(?'time'[\d]+)\s+(?'Message'.+)$", 1, "test message")]
    [TestCase("Error in com.example.core", @"^(?'time'[\d]+)\s+(?'Message'.+)$", 0, "")] // doesn't match regex so should be empty
    [TestCase("Error in com.example.core", @"^(?'time'[\d]+)\s+(?'Message'.+)$", 1, "Error in com.example.core")]
    public void SplitLine_ColumnValues(string lineToParse, string regex, int columnIndexToTest,
        string expectedColumnValue)
    {
        Regex1Columnizer columnizer = CreateInitializedColumnizer(regex);

        TestLogLine testLogLine = new(3, lineToParse);
        IColumnizedLogLine parsedLogLine = columnizer.SplitLine(Mock.Of<ILogLineColumnizerCallback>(), testLogLine);

        Assert.That(expectedColumnValue, Is.EqualTo(parsedLogLine.ColumnValues[columnIndexToTest].Text));
    }

    private Regex1Columnizer CreateInitializedColumnizer(string regex)
    {
        RegexColumnizerConfig columnizerConfig = new()
        {
            Expression = regex,
            Name = "Test regex"
        };

        Regex1Columnizer columnizer = new();
        //TODO this should be an internal function
        columnizer.Init(columnizerConfig);
        return columnizer;
    }

    private class TestLogLine : ILogLine
    {
        public TestLogLine(int lineNumber, string fullLine)
        {
            LineNumber = lineNumber;
            FullLine = fullLine;
        }

        public string FullLine { get; set; }

        public int LineNumber { get; set; }

        public string Text { get; set; }
    }
}
