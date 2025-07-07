using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;

using NUnit.Framework;

namespace LogExpert.Tests;

[TestFixture]
public class JsonCompactColumnizerTest
{
    [TestCase(@".\TestData\JsonCompactColumnizerTest_01.json", Priority.PerfectlySupport)]
    // As long as the json file contains one of the pre-defined key, it's perfectly supported.
    [TestCase(@".\TestData\JsonCompactColumnizerTest_02.json", Priority.PerfectlySupport)]
    [TestCase(@".\TestData\JsonCompactColumnizerTest_03.json", Priority.WellSupport)]
    public void GetPriority_HappyFile_PriorityMatches (string fileName, Priority priority)
    {
        var jsonCompactColumnizer = new JsonColumnizer.JsonCompactColumnizer();
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        LogfileReader logFileReader = new(path, new EncodingOptions(), false, 40, 50, new MultiFileOptions(), PluginRegistry.PluginRegistry.Instance);
        logFileReader.ReadFiles();
        List<ILogLine> loglines = new()
        {
            // Sampling a few lines to select the correct columnizer
            logFileReader.GetLogLine(0),
            logFileReader.GetLogLine(1),
            logFileReader.GetLogLine(2),
            logFileReader.GetLogLine(3),
            logFileReader.GetLogLine(4),
            logFileReader.GetLogLine(5),
            logFileReader.GetLogLine(25),
            logFileReader.GetLogLine(100),
            logFileReader.GetLogLine(200),
            logFileReader.GetLogLine(400)
        };

        var result = jsonCompactColumnizer.GetPriority(path, loglines);
        Assert.That(result, Is.EqualTo(priority));
    }
}
