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

    // Issue #713: a nonzero time offset must shift only the date and time columns and keep
    // the bracket fields and the message.
    [TestCase("2022-03-21 11:34:34.505[INFO][Worker]Message", 0, "2022-03-21", "11:34:34.505")]
    [TestCase("2022-03-21 11:34:34.505[INFO][Worker]Message", 123, "2022-03-21", "11:34:34.628")]
    [TestCase("2022-03-21 11:34:34.505[INFO][Worker]Message", -1505, "2022-03-21", "11:34:33.000")]
    [TestCase("2022-03-21 23:59:59.900[INFO][Worker]Message", 200, "2022-03-22", "00:00:00.100")]
    [TestCase("2022-03-21 00:00:00.100[INFO][Worker]Message", -200, "2022-03-20", "23:59:59.900")]
    public void SplitLine_TimeOffset_KeepsBracketFieldsAndMessage (string text, int offset, string expectedDate, string expectedTime)
    {
        var line = new LogLine(text, 0);
        SquareBracketColumnizer squareBracketColumnizer = new();
        _ = squareBracketColumnizer.GetPriority("square.log", [line]);

        squareBracketColumnizer.SetTimeOffset(offset);
        var columns = squareBracketColumnizer.SplitLine(null, line).ColumnValues.Select(c => c.FullValue.ToString());

        Assert.That(columns, Is.EqualTo(new[] { expectedDate, expectedTime, "[INFO]", "[Worker]", "Message" }));
    }

    [Test]
    public void SplitLine_TimeOffsetResetToZero_RestoresOriginalColumns ()
    {
        var line = new LogLine("2022-03-21 11:34:34.505[INFO][Worker]Message", 0);
        SquareBracketColumnizer squareBracketColumnizer = new();
        _ = squareBracketColumnizer.GetPriority("square.log", [line]);

        squareBracketColumnizer.SetTimeOffset(123);
        _ = squareBracketColumnizer.SplitLine(null, line);
        squareBracketColumnizer.SetTimeOffset(0);
        var columns = squareBracketColumnizer.SplitLine(null, line).ColumnValues.Select(c => c.FullValue.ToString());

        Assert.Multiple(() =>
        {
            Assert.That(columns, Is.EqualTo(new[] { "2022-03-21", "11:34:34.505", "[INFO]", "[Worker]", "Message" }));
            Assert.That(line.FullLine.ToString(), Is.EqualTo("2022-03-21 11:34:34.505[INFO][Worker]Message"));
        });
    }
}