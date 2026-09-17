using ColumnizerLib;

using CsvColumnizer;

using LogExpert.Core.Classes.Columnizer;
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
    public void CsvSnapshot_SplitLinePreservesInitializedParseAfterOriginalConfigChanges ()
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

            var snapshot = ((IColumnizerSnapshotMemory)original).CreateSnapshot();
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

            var result = snapshot.SplitLine(null, new CsvLogLine("Alice,42", 1));

            Assert.That(result.ColumnValues.Select(column => column.FullValue.ToString()), Is.EqualTo(["Alice", "42"]));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void RegexSnapshot_SplitLinePreservesInitializedParseAfterOriginalConfigChanges ()
    {
        var directory = CreateTempDirectory();
        try
        {
            WriteRegexConfig(directory, "(?<name>[^:]+):(?<value>.+)");
            var original = new Regex1Columnizer();
            original.LoadConfig(directory);
            var snapshot = ((IColumnizerSnapshotMemory)original).CreateSnapshot();
            WriteRegexConfig(directory, "(?<other>.+)");
            original.LoadConfig(directory);

            var result = snapshot.SplitLine(new Mock<ILogLineMemoryColumnizerCallback>().Object, new LogLine("Alice:42", 1));

            Assert.That(result.ColumnValues.Select(column => column.FullValue.ToString()), Is.EqualTo(["Alice", "42"]));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Log4jSnapshot_SplitLinePreservesInitializedParseAndTimeOffsetAfterOriginalConfigChanges ()
    {
        var directory = CreateTempDirectory();
        try
        {
            WriteLog4jConfig(directory, true);
            var original = new Log4jXmlColumnizer.Log4jXmlColumnizer();
            original.LoadConfig(directory);
            original.SetTimeOffset(123);
            var snapshot = ((IColumnizerSnapshotMemory)original).CreateSnapshot();
            WriteLog4jConfig(directory, false);
            original.LoadConfig(directory);

            var line = new LogLine("1700000000000�INFO�Logger�Thread�Class�Method�File�12�Message", 1);
            var result = snapshot.SplitLine(new Mock<ILogLineMemoryColumnizerCallback>().Object, line);

            Assert.Multiple(() =>
            {
                Assert.That(result.ColumnValues, Has.Length.EqualTo(9));
                Assert.That(result.ColumnValues[1].FullValue.ToString(), Is.EqualTo("INFO"));
                Assert.That(result.ColumnValues[8].FullValue.ToString(), Is.EqualTo("Message"));
                Assert.That(snapshot.GetTimeOffset(), Is.EqualTo(123));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SquareBracketSnapshot_PreservesDetectedLayoutAndTimeOffsetAfterOriginalChanges ()
    {
        var line = new LogLine("2022-03-21 11:34:34.505[one][two][three][four][five][six]Message", 0);
        var original = new SquareBracketColumnizer();
        _ = original.GetPriority("square.log", new ILogLineMemory[] { line });
        original.SetTimeOffset(123);
        var snapshot = ((IColumnizerSnapshotMemory)original).CreateSnapshot();
        _ = original.GetPriority("other.log", new ILogLineMemory[] { new LogLine("[Other]Changed", 0) });
        original.SetTimeOffset(456);

        var result = snapshot.SplitLine(null, line);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.GetColumnNames(), Is.EqualTo(new[] { "Date", "Time", "Level", "Source", "Source1", "Source2", "Source3", "Source4", "Message" }));
            Assert.That(result.ColumnValues, Has.Length.EqualTo(9));
            Assert.That(result.ColumnValues[0].FullValue.ToString(), Is.EqualTo("2022-03-21"));
            Assert.That(result.ColumnValues[1].FullValue.ToString(), Is.EqualTo("11:34:34.628"));
            Assert.That(snapshot.GetTimeOffset(), Is.EqualTo(123));
        });
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