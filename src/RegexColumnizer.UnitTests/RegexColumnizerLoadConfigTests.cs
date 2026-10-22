using System.Runtime.Versioning;
using System.Windows.Forms;

using NUnit.Framework;

using RegexColumnizer;

[assembly: SupportedOSPlatform("windows")]
namespace LogExpert.RegexColumnizer.Tests;

[TestFixture]
public class RegexColumnizerLoadConfigTests
{
    private string _testDirectory;

    [SetUp]
    public void SetUp ()
    {
        _testDirectory = Path.Join(Path.GetTempPath(), $"LogExpertTest_{Guid.NewGuid()}");
        _ = Directory.CreateDirectory(_testDirectory);
    }

    [TearDown]
    public void TearDown ()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public void LoadConfig_JsonExists_LoadsFromJson ()
    {
        // Arrange
        string jsonPath = Path.Join(_testDirectory, "Regex1Columnizer.json");
        File.WriteAllText(jsonPath, @"{
  ""Name"": ""Test From JSON"",
  ""Expression"": ""^(?<col1>\\w+)\\s+(?<col2>\\w+)$""
}");

        var columnizer = new Regex1Columnizer();

        // Act
        columnizer.LoadConfig(_testDirectory);

        // Assert
        Assert.That(columnizer.GetName(), Is.EqualTo("Test From JSON"));
        Assert.That(columnizer.GetColumnCount(), Is.EqualTo(2));
        Assert.That(columnizer.GetColumnNames(), Is.EqualTo(["col1", "col2"]));
    }

    [Test]
    public void LoadConfig_XmlExists_LoadsFromXml ()
    {
        // Arrange
        string xmlPath = Path.Join(_testDirectory, "Regex1Columnizer.xml");
        File.WriteAllText(xmlPath, @"<?xml version=""1.0""?>
<RegexColumnizerConfig xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Expression>^(?&lt;text&gt;.*)$</Expression>
  <Name>Test From XML</Name>
</RegexColumnizerConfig>");

        var columnizer = new Regex1Columnizer();

        // Act
        columnizer.LoadConfig(_testDirectory);

        // Assert
        Assert.That(columnizer.GetName(), Is.EqualTo("Test From XML"));
        Assert.That(columnizer.GetColumnCount(), Is.EqualTo(1));
    }

    [Test]
    public void LoadConfig_NoConfigExists_UsesDefaults ()
    {
        // Arrange
        var columnizer = new Regex1Columnizer();

        // Act
        columnizer.LoadConfig(_testDirectory);

        // Assert
        Assert.That(columnizer.GetName(), Is.EqualTo("Regex1"));
        Assert.That(columnizer.GetColumnCount(), Is.GreaterThan(0));
    }

    [Test]
    public void LoadConfig_BothJsonAndXmlExist_PrefersJson ()
    {
        // Arrange
        string jsonPath = Path.Join(_testDirectory, "Regex1Columnizer.json");
        string xmlPath = Path.Join(_testDirectory, "Regex1Columnizer.xml");

        File.WriteAllText(jsonPath, @"{
  ""Name"": ""From JSON"",
  ""Expression"": ""(?<json>.*)""
}");

        File.WriteAllText(xmlPath, @"<?xml version=""1.0""?>
<RegexColumnizerConfig>
  <Expression>(?&lt;xml&gt;.*)</Expression>
  <Name>From XML</Name>
</RegexColumnizerConfig>");

        var columnizer = new Regex1Columnizer();

        // Act
        columnizer.LoadConfig(_testDirectory);

        // Assert
        Assert.That(columnizer.GetName(), Is.EqualTo("From JSON"));
        Assert.That(columnizer.GetColumnNames(), Is.EqualTo(["json"]));
    }

    [Test]
    public void LoadConfig_CorruptJsonFile_FallsBackToDefault ()
    {
        // Arrange
        string jsonPath = Path.Join(_testDirectory, "Regex1Columnizer.json");
        File.WriteAllText(jsonPath, "{ this is not valid json }");

        var errors = new List<(string Message, string Title, MessageBoxIcon Icon)>();
        var columnizer = new Regex1Columnizer
        {
            ShowError = (message, title, icon) => errors.Add((message, title, icon))
        };

        // Act - Should not throw, should fall back to defaults
        Assert.DoesNotThrow(() => columnizer.LoadConfig(_testDirectory));

        // Assert - error was reported (dialog would have shown in release)
        Assert.That(errors, Has.Count.EqualTo(1));

        // Assert - and should have fallen back to defaults
        Assert.That(columnizer.GetName(), Is.EqualTo("Regex1"));
    }

    [Test]
    public void LoadConfig_NullJsonContent_FallsBackToDefault ()
    {
        // Arrange
        string jsonPath = Path.Join(_testDirectory, "Regex1Columnizer.json");
        File.WriteAllText(jsonPath, "null");

        var columnizer = new Regex1Columnizer();

        // Act - CRITICAL-02 has been fixed with null coalescing operator
        Assert.DoesNotThrow(() => columnizer.LoadConfig(_testDirectory));

        // Assert - Should use default configuration
        Assert.That(columnizer.GetName(), Is.EqualTo("Regex1"));
        Assert.That(columnizer.GetColumnCount(), Is.GreaterThan(0));
    }

    [Test]
    public void LoadConfig_EmptyJsonObject_LoadsWithDefaults ()
    {
        // Arrange
        string jsonPath = Path.Join(_testDirectory, "Regex1Columnizer.json");
        File.WriteAllText(jsonPath, "{}");

        var columnizer = new Regex1Columnizer();

        // Act
        columnizer.LoadConfig(_testDirectory);

        // Assert - Should use default expression
        Assert.That(columnizer.GetColumnCount(), Is.GreaterThan(0));
    }

    [Test]
    public void LoadConfig_InvalidRegexInJson_HandlesGracefully ()
    {
        // Arrange
        string jsonPath = Path.Join(_testDirectory, "Regex1Columnizer.json");
        File.WriteAllText(jsonPath, @"{
  ""Name"": ""Invalid Regex"",
  ""Expression"": ""(?<unclosed""
}");

        var columnizer = new Regex1Columnizer();

        // Act - Should not throw
        Assert.DoesNotThrow(() => columnizer.LoadConfig(_testDirectory));

        // Assert - Regex should be null, but columnizer should still work
        Assert.That(columnizer.GetName(), Is.EqualTo("Invalid Regex"));
    }

    [Test]
    public void LoadConfig_DifferentColumnizerInstances_LoadSeparateConfigs ()
    {
        // Arrange
        string json1Path = Path.Join(_testDirectory, "Regex1Columnizer.json");
        string json2Path = Path.Join(_testDirectory, "Regex2Columnizer.json");

        File.WriteAllText(json1Path, @"{
  ""Name"": ""Config1"",
  ""Expression"": ""(?<col1>.*)""
}");

        File.WriteAllText(json2Path, @"{
  ""Name"": ""Config2"",
  ""Expression"": ""(?<col2>.*)""
}");

        var columnizer1 = new Regex1Columnizer();
        var columnizer2 = new Regex2Columnizer();

        // Act
        columnizer1.LoadConfig(_testDirectory);
        columnizer2.LoadConfig(_testDirectory);

        // Assert
        Assert.That(columnizer1.GetName(), Is.EqualTo("Config1"));
        Assert.That(columnizer2.GetName(), Is.EqualTo("Config2"));
    }

    [Test]
    public void LoadConfig_ComplexRegexWithMultipleGroups_ParsesCorrectly ()
    {
        // Arrange
        string jsonPath = Path.Join(_testDirectory, "Regex1Columnizer.json");
        File.WriteAllText(jsonPath, @"{
  ""Name"": ""Apache Log Parser"",
  ""Expression"": ""^(?<ip>\\S+)\\s+\\S+\\s+(?<user>\\S+)\\s+\\[(?<datetime>[^\\]]+)\\]\\s+\""(?<method>\\S+)\\s+(?<path>\\S+)\\s+(?<protocol>\\S+)\""\\s+(?<status>\\d+)\\s+(?<size>\\d+)""
}");

        var columnizer = new Regex1Columnizer();

        // Act
        columnizer.LoadConfig(_testDirectory);

        // Assert - 8 columns total (first unnamed group creates numbered group that gets skipped)
        Assert.That(columnizer.GetColumnCount(), Is.EqualTo(8));
        var expectedColumns = new[] { "ip", "user", "datetime", "method", "path", "protocol", "status", "size" };
        Assert.That(columnizer.GetColumnNames(), Is.EqualTo(expectedColumns));
    }

    [Test]
    public void LoadConfig_CalledMultipleTimes_ReloadsConfiguration ()
    {
        // Arrange
        string jsonPath = Path.Join(_testDirectory, "Regex1Columnizer.json");
        File.WriteAllText(jsonPath, @"{
  ""Name"": ""First Config"",
  ""Expression"": ""(?<col1>.*)""
}");

        var columnizer = new Regex1Columnizer();
        columnizer.LoadConfig(_testDirectory);

        Assert.That(columnizer.GetName(), Is.EqualTo("First Config"));

        // Modify config file
        File.WriteAllText(jsonPath, @"{
  ""Name"": ""Second Config"",
  ""Expression"": ""(?<col2>.*)""
}");

        // Act
        columnizer.LoadConfig(_testDirectory);

        // Assert
        Assert.That(columnizer.GetName(), Is.EqualTo("Second Config"));
        Assert.That(columnizer.GetColumnNames(), Is.EqualTo(["col2"]));
    }
}
