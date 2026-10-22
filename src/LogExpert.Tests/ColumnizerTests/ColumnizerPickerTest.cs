using System.Reflection;

using ColumnizerLib;

using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.ColumnizerTests;

/// <summary>
/// Summary description for AutoColumnizerTest
/// </summary>
[TestFixture]
public class ColumnizerPickerTest
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

    [TestCase("Square Bracket Columnizer", "30/08/2018 08:51:42.712 [TRACE]    [a] hello", "30/08/2018 08:51:42.712 [DATAIO]   [b] world", null, null, null)]
    [TestCase("Square Bracket Columnizer", "30/08/2018 08:51:42.712 [TRACE]     hello", "30/08/2018 08:51:42.712 [DATAIO][]    world", null, null, null)]
    [TestCase("Square Bracket Columnizer", "", "30/08/2018 08:51:42.712 [TRACE]    hello", "30/08/2018 08:51:42.712 [TRACE]    hello", "[DATAIO][b][c] world", null)]
    [TestCase("Timestamp Columnizer", "30/08/2018 08:51:42.712 no bracket 1", "30/08/2018 08:51:42.712 no bracket 2", "30/08/2018 08:51:42.712 [TRACE]    with bracket 1", "30/08/2018 08:51:42.712 [TRACE]    with bracket 2", "no bracket 3")]
    public void FindColumnizer_ReturnCorrectColumnizer (string expectedColumnizerName, string line0, string line1, string line2, string line3, string line4)
    {
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "test");

        Mock<IAutoLogLineMemoryColumnizerCallback> autoLogLineColumnizerCallbackMock = new();

        // Mock GetLogLineMemory() which returns ILogLineMemory
        _ = autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLineMemory(0)).Returns(new TestLogLineMemory()
        {
            FullLine = line0?.AsMemory() ?? ReadOnlyMemory<char>.Empty,
            LineNumber = 0
        });

        _ = autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLineMemory(1)).Returns(new TestLogLineMemory()
        {
            FullLine = line1?.AsMemory() ?? ReadOnlyMemory<char>.Empty,
            LineNumber = 1
        });

        _ = autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLineMemory(2)).Returns(new TestLogLineMemory()
        {
            FullLine = line2?.AsMemory() ?? ReadOnlyMemory<char>.Empty,
            LineNumber = 2
        });

        _ = autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLineMemory(3)).Returns(new TestLogLineMemory()
        {
            FullLine = line3?.AsMemory() ?? ReadOnlyMemory<char>.Empty,
            LineNumber = 3
        });

        _ = autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLineMemory(4)).Returns(new TestLogLineMemory()
        {
            FullLine = line4?.AsMemory() ?? ReadOnlyMemory<char>.Empty,
            LineNumber = 4
        });

        // Mock for additional sampled lines that ColumnizerPicker checks
        _ = autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLineMemory(5)).Returns((ILogLineMemory)null);
        _ = autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLineMemory(25)).Returns((ILogLineMemory)null);
        _ = autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLineMemory(100)).Returns((ILogLineMemory)null);
        _ = autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLineMemory(200)).Returns((ILogLineMemory)null);
        _ = autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLineMemory(400)).Returns((ILogLineMemory)null);

        var result = ColumnizerPicker.FindMemoryColumnizer(path, autoLogLineColumnizerCallbackMock.Object, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

        Assert.That(result.GetName(), Is.EqualTo(expectedColumnizerName));
    }

    [TestCase(@".\TestData\JsonColumnizerTest_01.txt", typeof(JsonCompactColumnizer.JsonCompactColumnizer), ReaderType.System)]
    [TestCase(@".\TestData\SquareBracketColumnizerTest_02.txt", typeof(SquareBracketColumnizer), ReaderType.System)]
    public void FindReplacementForAutoColumnizer_ValidTextFile_ReturnCorrectColumnizer (string fileName, Type columnizerType, ReaderType readerType)
    {
        var pluginRegistry = PluginRegistry.PluginRegistry.Instance;

        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, fileName);
        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), readerType, pluginRegistry, 500);
        reader.ReadFiles();

        Mock<ILogLineMemoryColumnizer> autoColumnizer = new();
        _ = autoColumnizer.Setup(a => a.GetName()).Returns("Auto Columnizer");

        // TODO: When DI container is ready, we can mock this set up.
        PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers.Add(new JsonCompactColumnizer.JsonCompactColumnizer());
        var result = ColumnizerPicker.FindReplacementForAutoMemoryColumnizer(fileName, reader, autoColumnizer.Object, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

        Assert.That(columnizerType, Is.EqualTo(result.GetType()));
    }

    [TestCase(@".\TestData\FileNotExists.txt", typeof(DefaultLogfileColumnizer))]
    public void DecideColumnizerByName_WhenReaderIsNotReady_ReturnCorrectColumnizer (string fileName, Type columnizerType)
    {
        // TODO: When DI container is ready, we can mock this set up.
        PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers.Add(new JsonCompactColumnizer.JsonCompactColumnizer());
        var result = ColumnizerPicker.DecideMemoryColumnizerByName(fileName, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

        Assert.That(columnizerType, Is.EqualTo(result.GetType()));
    }

    [TestCase(@"Invalid Name", typeof(DefaultLogfileColumnizer))]
    [TestCase(@"JSON Columnizer", typeof(JsonColumnizer.JsonColumnizer))]
    public void DecideColumnizerByName_ValidTextFile_ReturnCorrectColumnizer (string columnizerName, Type columnizerType)
    {
        // TODO: When DI container is ready, we can mock this set up.
        PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers.Add(new JsonColumnizer.JsonColumnizer());

        var result = ColumnizerPicker.DecideMemoryColumnizerByName(columnizerName, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

        Assert.That(columnizerType, Is.EqualTo(result.GetType()));
    }

    /// <summary>
    /// Test helper class that implements ILogLineMemory for mocking log lines.
    /// </summary>
    private class TestLogLineMemory : ILogLineMemory
    {
        public ReadOnlyMemory<char> FullLine { get; set; }

        public int LineNumber { get; set; }

        // Explicit implementation for ITextValueMemory.Text (ReadOnlyMemory<char> version)
        ReadOnlyMemory<char> ITextValueMemory.Text => FullLine;
    }
}