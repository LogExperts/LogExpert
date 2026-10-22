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
}