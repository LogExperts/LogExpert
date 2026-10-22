using System.Reflection;

using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;

using NUnit.Framework;

namespace LogExpert.Tests.ColumnizerTests;

[TestFixture]
public class JsonColumnizerTest
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

    [TestCase(@".\TestData\JsonColumnizerTest_01.txt", "time @m level", ReaderType.System)]
    public void GetColumnNames_HappyFile_ColumnNameMatches (string fileName, string expectedHeaders, ReaderType readerType)
    {
        var jsonColumnizer = new JsonColumnizer.JsonColumnizer();
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, fileName);
        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), readerType, PluginRegistry.PluginRegistry.Instance, 500);
        reader.ReadFiles();

        var line = reader.GetLogLineMemory(0);
        if (line != null)
        {
            _ = jsonColumnizer.SplitLine(null, line);
        }

        line = reader.GetLogLineMemory(1);
        if (line != null)
        {
            _ = jsonColumnizer.SplitLine(null, line);
        }

        var columnHeaders = jsonColumnizer.GetColumnNames();
        var result = string.Join(" ", columnHeaders);
        Assert.That(expectedHeaders, Is.EqualTo(result));
    }
}
