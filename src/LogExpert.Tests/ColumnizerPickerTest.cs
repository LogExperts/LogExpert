using JsonColumnizer;

using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests;

/// <summary>
/// Summary description for AutoColumnizerTest
/// </summary>
[TestFixture]
public class ColumnizerPickerTest
{
    [TestCase("Square Bracket Columnizer", "30/08/2018 08:51:42.712 [TRACE]    [a] hello", "30/08/2018 08:51:42.712 [DATAIO]   [b] world", null, null, null)]
    [TestCase("Square Bracket Columnizer", "30/08/2018 08:51:42.712 [TRACE]     hello", "30/08/2018 08:51:42.712 [DATAIO][]    world", null, null, null)]
    [TestCase("Square Bracket Columnizer", "", "30/08/2018 08:51:42.712 [TRACE]    hello", "30/08/2018 08:51:42.712 [TRACE]    hello", "[DATAIO][b][c] world", null)]
    [TestCase("Timestamp Columnizer", "30/08/2018 08:51:42.712 no bracket 1", "30/08/2018 08:51:42.712 no bracket 2", "30/08/2018 08:51:42.712 [TRACE]    with bracket 1", "30/08/2018 08:51:42.712 [TRACE]    with bracket 2", "no bracket 3")]
    public void FindColumnizer_ReturnCorrectColumnizer (string expectedColumnizerName, string line0, string line1, string line2, string line3, string line4)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test");

        Mock<IAutoLogLineColumnizerCallback> autoLogLineColumnizerCallbackMock = new();

        autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLine(0)).Returns(new TestLogLine()
        {
            FullLine = line0,
            LineNumber = 0
        });

        autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLine(1)).Returns(new TestLogLine()
        {
            FullLine = line1,
            LineNumber = 1
        });

        autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLine(2)).Returns(new TestLogLine()
        {
            FullLine = line2,
            LineNumber = 2
        });

        autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLine(3)).Returns(new TestLogLine()
        {
            FullLine = line3,
            LineNumber = 3
        });
        autoLogLineColumnizerCallbackMock.Setup(a => a.GetLogLine(4)).Returns(new TestLogLine()
        {
            FullLine = line4,
            LineNumber = 4
        });

        var result = ColumnizerPicker.FindColumnizer(path, autoLogLineColumnizerCallbackMock.Object, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

        Assert.That(result.GetName(), Is.EqualTo(expectedColumnizerName));
    }


    [TestCase(@".\TestData\JsonColumnizerTest_01.txt", typeof(JsonCompactColumnizer))]
    [TestCase(@".\TestData\SquareBracketColumnizerTest_02.txt", typeof(SquareBracketColumnizer))]
    public void FindReplacementForAutoColumnizer_ValidTextFile_ReturnCorrectColumnizer (
        string fileName, Type columnizerType)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        LogfileReader reader = new(path, new EncodingOptions(), false, 40, 50, new MultiFileOptions(), PluginRegistry.PluginRegistry.Instance);
        reader.ReadFiles();

        Mock<ILogLineColumnizer> autoColumnizer = new();
        autoColumnizer.Setup(a => a.GetName()).Returns("Auto Columnizer");

        // TODO: When DI container is ready, we can mock this set up.
        PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers.Add(new JsonCompactColumnizer());
        var result = ColumnizerPicker.FindReplacementForAutoColumnizer(fileName, reader, autoColumnizer.Object, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

        Assert.That(columnizerType, Is.EqualTo(result.GetType()));
    }

    [TestCase(@".\TestData\FileNotExists.txt", typeof(DefaultLogfileColumnizer))]
    public void DecideColumnizerByName_WhenReaderIsNotReady_ReturnCorrectColumnizer (
        string fileName, Type columnizerType)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

        // TODO: When DI container is ready, we can mock this set up.
        PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers.Add(new JsonCompactColumnizer());
        var result = ColumnizerPicker.DecideColumnizerByName(fileName, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

        Assert.That(columnizerType, Is.EqualTo(result.GetType()));
    }

    [TestCase(@"Invalid Name", typeof(DefaultLogfileColumnizer))]
    [TestCase(@"JSON Columnizer", typeof(JsonColumnizer.JsonColumnizer))]
    public void DecideColumnizerByName_ValidTextFile_ReturnCorrectColumnizer (
        string columnizerName, Type columnizerType)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, columnizerName);

        // TODO: When DI container is ready, we can mock this set up.
        PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers.Add(new JsonColumnizer.JsonColumnizer());

        var result = ColumnizerPicker.DecideColumnizerByName(columnizerName, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

        Assert.That(columnizerType, Is.EqualTo(result.GetType()));
    }

    private class TestLogLine : ILogLine
    {
        public string Text => FullLine;
        public string FullLine { get; set; }
        public int LineNumber { get; set; }
    }
}