using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;

using NUnit.Framework;

namespace LogExpert.Tests;

[TestFixture]
public class JsonColumnizerTest
{
    [TestCase(@".\TestData\JsonColumnizerTest_01.txt", "time @m level")]
    public void GetColumnNames_HappyFile_ColumnNameMatches (string fileName, string expectedHeaders)
    {
        var jsonColumnizer = new JsonColumnizer.JsonColumnizer();
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), false, PluginRegistry.PluginRegistry.Instance);
        reader.ReadFiles();

        ILogLine line = reader.GetLogLine(0);
        if (line != null)
        {
            jsonColumnizer.SplitLine(null, line);
        }

        line = reader.GetLogLine(1);
        if (line != null)
        {
            jsonColumnizer.SplitLine(null, line);
        }

        var columnHeaders = jsonColumnizer.GetColumnNames();
        var result = string.Join(" ", columnHeaders);
        Assert.That(expectedHeaders, Is.EqualTo(result));
    }
}
