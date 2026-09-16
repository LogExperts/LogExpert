using ColumnizerLib;

using CsvColumnizer;

using LogExpert.Core.Entities;

using Moq;

using Newtonsoft.Json;

using NUnit.Framework;

using RegexColumnizer;

namespace LogExpert.Tests.UI;

[TestFixture]
public class MarkerColumnizerSnapshotTests
{
    [Test]
    public void CsvClone_SplitLinePreservesInitializedParseAfterOriginalConfigChanges ()
    {
        var directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Join(directory, "csvcolumnizer.json"), JsonConvert.SerializeObject(new
            {
                DelimiterChar = ",",
                EscapeChar = "\"",
                QuoteChar = "\"",
                CommentChar = "#",
                HasFieldNames = true,
                MinColumns = 0
            }));
            var original = new CsvColumnizer.CsvColumnizer();
            original.LoadConfig(directory);
            _ = original.PreProcessLine("name,age".AsMemory(), 0, 0);
            original.Selected(new Mock<ILogLineMemoryColumnizerCallback>().Object);

            var clone = (CsvColumnizer.CsvColumnizer)((ICloneable)original).Clone();
            File.WriteAllText(Path.Join(directory, "csvcolumnizer.json"), JsonConvert.SerializeObject(new
            {
                DelimiterChar = ";",
                EscapeChar = "\"",
                QuoteChar = "\"",
                CommentChar = "#",
                HasFieldNames = true,
                MinColumns = 0
            }));
            original.LoadConfig(directory);

            var result = clone.SplitLine(null, new CsvLogLine("Alice,42", 1));

            Assert.That(result.ColumnValues.Select(column => column.FullValue.ToString()), Is.EqualTo(["Alice", "42"]));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void RegexClone_SplitLinePreservesInitializedParseAfterOriginalConfigChanges ()
    {
        var directory = CreateTempDirectory();
        try
        {
            WriteRegexConfig(directory, "(?<name>[^:]+):(?<value>.+)");
            var original = new Regex1Columnizer();
            original.LoadConfig(directory);
            var clone = (BaseRegexColumnizer)((ICloneable)original).Clone();
            WriteRegexConfig(directory, "(?<other>.+)");
            original.LoadConfig(directory);

            var result = clone.SplitLine(new Mock<ILogLineMemoryColumnizerCallback>().Object, new LogLine("Alice:42", 1));

            Assert.That(result.ColumnValues.Select(column => column.FullValue.ToString()), Is.EqualTo(["Alice", "42"]));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Log4jClone_SplitLinePreservesInitializedParseAndTimeOffsetAfterOriginalConfigChanges ()
    {
        var directory = CreateTempDirectory();
        try
        {
            WriteLog4jConfig(directory, true);
            var original = new Log4jXmlColumnizer.Log4jXmlColumnizer();
            original.LoadConfig(directory);
            original.SetTimeOffset(123);
            var clone = (Log4jXmlColumnizer.Log4jXmlColumnizer)((ICloneable)original).Clone();
            WriteLog4jConfig(directory, false);
            original.LoadConfig(directory);

            var line = new LogLine("1700000000000�INFO�Logger�Thread�Class�Method�File�12�Message", 1);
            var result = clone.SplitLine(new Mock<ILogLineMemoryColumnizerCallback>().Object, line);

            Assert.Multiple(() =>
            {
                Assert.That(result.ColumnValues, Has.Length.EqualTo(9));
                Assert.That(result.ColumnValues[1].FullValue.ToString(), Is.EqualTo("INFO"));
                Assert.That(result.ColumnValues[8].FullValue.ToString(), Is.EqualTo("Message"));
                Assert.That(clone.GetTimeOffset(), Is.EqualTo(123));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateTempDirectory ()
    {
        var directory = Path.Join(Path.GetTempPath(), "LogExpertMarkerColumnizerTests", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteRegexConfig (string directory, string expression)
    {
        File.WriteAllText(Path.Join(directory, "Regex1Columnizer.json"), JsonConvert.SerializeObject(new
        {
            Expression = expression,
            Name = "Regex1",
            CustomName = "Regex1"
        }));
    }

    private static void WriteLog4jConfig (string directory, bool visible)
    {
        var names = new[] { "Timestamp", "Level", "Logger", "Thread", "Class", "Method", "File", "Line", "Message" };
        var columns = names
            .Select((name, index) => new Log4jXmlColumnizer.Log4jColumnEntry(name, index, 0) { Visible = visible });
        File.WriteAllText(Path.Join(directory, "log4jxmlcolumnizer.json"), JsonConvert.SerializeObject(new
        {
            columnNames = names,
            ColumnList = columns,
            LocalTimestamps = true
        }));
    }
}