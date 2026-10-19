using System.Reflection;

using ColumnizerLib;

using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.PluginRegistry.FileSystem;

using NUnit.Framework;

namespace LogExpert.Tests;

[TestFixture]
internal class RolloverHandlerTest : RolloverHandlerTestBase
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

    [Test]
    [TestCase("*$J(.)", 66)]
    public void TestFilenameListWithAppendedIndex (string format, int retries)
    {
        MultiFileOptions options = new()
        {
            FormatPattern = format,
            MaxDayTry = retries
        };

        var files = CreateTestFilesWithoutDate();

        var firstFile = files.Last.Value;

        ILogFileInfo info = new LogFileInfo(new Uri(firstFile));
        RolloverFilenameHandler handler = new(info, options);
        var fileList = handler.GetNameList(PluginRegistry.PluginRegistry.Instance);

        Assert.That(fileList, Is.EqualTo(files));

        Cleanup();
    }

    [Test]
    [TestCase("*$D(YYYY-mm-DD)_$I.log", 3)]
    public void TestFilenameListWithDate (string format, int retries)
    {
        MultiFileOptions options = new()
        {
            FormatPattern = format,
            MaxDayTry = retries
        };

        var files = CreateTestFilesWithDate();

        var firstFile = files.Last.Value;

        ILogFileInfo info = new LogFileInfo(new Uri(firstFile));
        RolloverFilenameHandler handler = new(info, options);
        var fileList = handler.GetNameList(PluginRegistry.PluginRegistry.Instance);

        Assert.That(fileList, Is.EqualTo(files));

        Cleanup();
    }
}