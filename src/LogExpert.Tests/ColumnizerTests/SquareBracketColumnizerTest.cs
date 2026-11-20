using System.Reflection;

using ColumnizerLib;

using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;

using NUnit.Framework;

namespace LogExpert.Tests.ColumnizerTests;

[TestFixture]
public class SquareBracketColumnizerTest
{
    [SetUp]
    public void Setup ()
    {
        // Reset singleton for testing (same pattern as PluginRegistryTests)
        ResetPluginRegistrySingleton();

        // Initialize plugin registry with proper test directory
        var testDataPath = Path.Join(Path.GetTempPath(), "LogExpertTests", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(testDataPath);

        var pluginRegistry = PluginRegistry.PluginRegistry.Create(testDataPath, 250);

        // Verify the local file system plugin is registered
        var localPlugin = pluginRegistry.FindFileSystemForUri(@"C:\test.txt");
        Assert.That(localPlugin, Is.Not.Null, "Local file system plugin not registered!");
    }

    [TearDown]
    public void TearDown ()
    {
        ResetPluginRegistrySingleton();
    }

    /// <summary>
    /// Uses reflection to reset the singleton instance for testing.
    /// This ensures each test starts with a fresh PluginRegistry state.
    /// </summary>
    private static void ResetPluginRegistrySingleton ()
    {
        var instanceField = typeof(PluginRegistry.PluginRegistry).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        instanceField?.SetValue(null, null);
    }

    [TestCase(@".\TestData\SquareBracketColumnizerTest_01.txt", 5, ReaderType.System)]
    [TestCase(@".\TestData\SquareBracketColumnizerTest_02.txt", 5, ReaderType.System)]
    [TestCase(@".\TestData\SquareBracketColumnizerTest_03.txt", 6, ReaderType.System)]
    [TestCase(@".\TestData\SquareBracketColumnizerTest_05.txt", 3, ReaderType.System)]
    public void GetPriority_HappyFile_ColumnCountMatches (string fileName, int count, ReaderType readerType)
    {
        SquareBracketColumnizer squareBracketColumnizer = new();
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, fileName);

        LogfileReader logFileReader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), readerType, PluginRegistry.PluginRegistry.Instance, 500);
        logFileReader.ReadFiles();
        List<ILogLineMemory> loglines =
        [
            // Sampling a few lines to select the correct columnizer
            logFileReader.GetLogLineMemory(0),
            logFileReader.GetLogLineMemory(1),
            logFileReader.GetLogLineMemory(2),
            logFileReader.GetLogLineMemory(3),
            logFileReader.GetLogLineMemory(4),
            logFileReader.GetLogLineMemory(5),
            logFileReader.GetLogLineMemory(25),
            logFileReader.GetLogLineMemory(100),
            logFileReader.GetLogLineMemory(200),
            logFileReader.GetLogLineMemory(400)
        ];

        _ = squareBracketColumnizer.GetPriority(path, loglines);
        Assert.That(count, Is.EqualTo(squareBracketColumnizer.GetColumnCount()));
    }

    // Issue #460: a bracketed field containing nested brackets (e.g. "[ProgramName/MethodName[41]]")
    // must stay in a single column instead of being split at the first inner ']'.
    [Test]
    public void SplitLine_NestedBrackets_KeepsInnerBracketsInOneColumn ()
    {
        SquareBracketColumnizer squareBracketColumnizer = new();
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\SquareBracketColumnizerTest_06.txt");

        LogfileReader logFileReader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        logFileReader.ReadFiles();

        List<ILogLineMemory> loglines =
        [
            logFileReader.GetLogLineMemory(0),
            logFileReader.GetLogLineMemory(1)
        ];

        _ = squareBracketColumnizer.GetPriority(path, loglines);

        var line = logFileReader.GetLogLineMemory(0);
        var columns = squareBracketColumnizer.SplitLine(null, line);

        Assert.Multiple(() =>
        {
            Assert.That(columns.ColumnValues[0].FullValue.ToString(), Is.EqualTo("[WARN ]"));
            Assert.That(columns.ColumnValues[1].FullValue.ToString(), Is.EqualTo("[18/08-06:06:18.436]"));
            Assert.That(columns.ColumnValues[2].FullValue.ToString(), Is.EqualTo("[ProgramName/MethodName[41]]"));
            Assert.That(columns.ColumnValues[^1].FullValue.ToString().Trim(), Is.EqualTo("About to run."));
        });
    }

    private static IEnumerable<string> GetColumnTexts (IColumnizedLogLineMemory result) =>
        result.ColumnValues.Select(c => c.FullValue.ToString());

    // Issue #713: applying a time offset must shift only the timestamp and preserve bracket fields and message.
    [Test]
    public void SplitLine_TimeOffset_PreservesBracketFieldsAndMessage ()
    {
        var line = new LogLine("2022-03-21 11:34:34.505[INFO][Worker]Message", 0);
        SquareBracketColumnizer columnizer = new();
        _ = columnizer.GetPriority("square.log", [line]);

        columnizer.SetTimeOffset(0);
        var zeroColumns = columnizer.SplitLine(null, line);

        columnizer.SetTimeOffset(123);
        var positiveColumns = columnizer.SplitLine(null, line);

        columnizer.SetTimeOffset(-506);
        var negativeColumns = columnizer.SplitLine(null, line);

        columnizer.SetTimeOffset(0);
        var resetColumns = columnizer.SplitLine(null, line);

        Assert.Multiple(() =>
        {
            Assert.That(GetColumnTexts(zeroColumns), Is.EqualTo(new[] { "2022-03-21", "11:34:34.505", "[INFO]", "[Worker]", "Message" }));
            Assert.That(GetColumnTexts(positiveColumns), Is.EqualTo(new[] { "2022-03-21", "11:34:34.628", "[INFO]", "[Worker]", "Message" }));
            Assert.That(GetColumnTexts(negativeColumns), Is.EqualTo(new[] { "2022-03-21", "11:34:33.999", "[INFO]", "[Worker]", "Message" }));
            Assert.That(GetColumnTexts(resetColumns), Is.EqualTo(new[] { "2022-03-21", "11:34:34.505", "[INFO]", "[Worker]", "Message" }));
            Assert.That(line.FullLine.ToString(), Is.EqualTo("2022-03-21 11:34:34.505[INFO][Worker]Message"));
        });
    }

    // Issue #713: midnight rollover under positive and negative time offsets must shift the date and preserve remaining columns.
    [Test]
    public void SplitLine_TimeOffsetCrossesMidnight_ShiftsDateAndPreservesRemainingColumns ()
    {
        var forwardLine = new LogLine("2022-03-21 23:59:59.900[INFO][Worker]Message", 0);
        var backwardLine = new LogLine("2022-03-22 00:00:00.100[INFO][Worker]Message", 0);
        SquareBracketColumnizer columnizer = new();
        _ = columnizer.GetPriority("square.log", [forwardLine, backwardLine]);

        columnizer.SetTimeOffset(200);
        var forwardColumns = columnizer.SplitLine(null, forwardLine);

        columnizer.SetTimeOffset(-200);
        var backwardColumns = columnizer.SplitLine(null, backwardLine);

        Assert.Multiple(() =>
        {
            Assert.That(GetColumnTexts(forwardColumns), Is.EqualTo(new[] { "2022-03-22", "00:00:00.100", "[INFO]", "[Worker]", "Message" }));
            Assert.That(GetColumnTexts(backwardColumns), Is.EqualTo(new[] { "2022-03-21", "23:59:59.900", "[INFO]", "[Worker]", "Message" }));
        });
    }

    // Issue #713: nested brackets (Issue #460) must remain a single column when a nonzero time offset is applied.
    [Test]
    public void SplitLine_TimeOffsetWithNestedBrackets_KeepsInnerBracketsInOneColumn ()
    {
        var line = new LogLine("2022-03-21 11:34:34.505[INFO][ProgramName/MethodName[41]]Message", 0);
        SquareBracketColumnizer columnizer = new();
        _ = columnizer.GetPriority("square.log", [line]);

        columnizer.SetTimeOffset(123);
        var columns = columnizer.SplitLine(null, line);

        Assert.That(GetColumnTexts(columns), Is.EqualTo(new[] { "2022-03-21", "11:34:34.628", "[INFO]", "[ProgramName/MethodName[41]]", "Message" }));
    }
}