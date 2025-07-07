using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;

using NUnit.Framework;

namespace LogExpert.Tests;

[TestFixture]
public class CSVColumnizerTest
{
    [TestCase(@".\TestData\organizations-10000.csv", new[] { "Index", "Organization Id", "Name", "Website", "Country", "Description", "Founded", "Industry", "Number of employees" })]
    [TestCase(@".\TestData\organizations-1000.csv", new[] { "Index", "Organization Id", "Name", "Website", "Country", "Description", "Founded", "Industry", "Number of employees" })]
    [TestCase(@".\TestData\people-10000.csv", new[] { "Index", "User Id", "First Name", "Last Name", "Sex", "Email", "Phone", "Date of birth", "Job Title" })]
    public void Instantiat_CSVFile_BuildCorrectColumnizer (string filename, string[] expectedHeaders)
    {
        CsvColumnizer.CsvColumnizer csvColumnizer = new();
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
        LogfileReader reader = new(path, new EncodingOptions(), false, 40, 50, new MultiFileOptions(), PluginRegistry.PluginRegistry.Instance);
        reader.ReadFiles();
        ILogLine line = reader.GetLogLine(0);
        IColumnizedLogLine logline = new ColumnizedLogLine();
        if (line != null)
        {
            logline = csvColumnizer.SplitLine(null, line);
        }
        var expectedResult = string.Join(",", expectedHeaders);
        Assert.That(logline.LogLine.FullLine, Is.EqualTo(expectedResult));
    }
}
