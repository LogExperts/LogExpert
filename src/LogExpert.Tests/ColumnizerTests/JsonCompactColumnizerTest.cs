using System.Reflection;

using ColumnizerLib;

using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;

using NUnit.Framework;

namespace LogExpert.Tests.ColumnizerTests;

[TestFixture]
public class JsonCompactColumnizerTest
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

    // As long as the json file contains one of the pre-defined key, it's perfectly supported.
    [TestCase(@".\TestData\JsonCompactColumnizerTest_01.json", Priority.PerfectlySupport, ReaderType.System)]
    [TestCase(@".\TestData\JsonCompactColumnizerTest_02.json", Priority.PerfectlySupport, ReaderType.System)]
    [TestCase(@".\TestData\JsonCompactColumnizerTest_03.json", Priority.WellSupport, ReaderType.System)]
    public void GetPriority_HappyFile_PriorityMatches (string fileName, Priority priority, ReaderType readerType)
    {
        var jsonCompactColumnizer = new JsonCompactColumnizer.JsonCompactColumnizer();
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

        var result = jsonCompactColumnizer.GetPriority(path, loglines);
        Assert.That(result, Is.EqualTo(priority));
    }
}