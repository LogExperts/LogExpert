using System.Reflection;

using LogExpert.Core.Config;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

[TestFixture]
public class ClearFileHistoryTests
{
    private string _testDir;
    private Configuration.ConfigManager _configManager;
    private object? _originalSettings;
    private bool _originalIsInitialized;
    private string _originalConfigDir;

    [SetUp]
    public void SetUp ()
    {
        _configManager = Configuration.ConfigManager.Instance;
        _originalIsInitialized = GetPrivateField("_isInitialized") is true;
        _originalSettings = GetPrivateField("_settings");
        _originalConfigDir = _configManager.ConfigDir;

        _testDir = Path.Join(Path.GetTempPath(), "LogExpert_ClearHistoryTest_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(_testDir);

        ResetConfigManagerInitialization();
        _configManager.Initialize(_testDir, new Rectangle(0, 0, 1920, 1080));
        _configManager.ConfigDir = _testDir;
    }

    [TearDown]
    public void TearDown ()
    {
        SetPrivateField("_isInitialized", _originalIsInitialized);
        SetPrivateField("_settings", _originalSettings);
        _configManager.ConfigDir = _originalConfigDir;

        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch (IOException)
        {
            // Ignore IO errors during cleanup
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore access errors during cleanup
        }
    }

    [Test]
    public void ClearFileHistory_RemovesAllEntries ()
    {
        _configManager.AddToFileHistory(@"C:\logs\one.log");
        _configManager.AddToFileHistory(@"C:\logs\two.log");
        Assert.That(_configManager.Settings.FileHistoryList, Has.Count.EqualTo(2));

        _configManager.ClearFileHistory();

        Assert.That(_configManager.Settings.FileHistoryList, Is.Empty);
    }

    [Test]
    public void ClearFileHistory_WritesEmptyListToSettingsFile ()
    {
        _configManager.AddToFileHistory(@"C:\logs\one.log");

        _configManager.ClearFileHistory();

        var settingsFile = Path.Join(_configManager.ActiveConfigDir, "settings.json");
        Assert.That(File.Exists(settingsFile), Is.True);

        var saved = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFile));
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved.FileHistoryList, Is.Empty);
    }

    [Test]
    public void ClearFileHistory_LeavesOtherHistoryListsUntouched ()
    {
        _configManager.AddToFileHistory(@"C:\logs\one.log");
        _configManager.Settings.LastOpenFilesList.Add(@"C:\logs\one.log");
        _configManager.Settings.SearchHistoryList.Add("error");
        _configManager.Settings.FilterHistoryList.Add("WARN");
        _configManager.Settings.FilterRangeHistoryList.Add("10");
        _configManager.Settings.UriHistoryList.Add("https://example.org/one.log");
        _configManager.Settings.RegexHistory.ExpressionHistoryList.Add(@"^\d+");
        _configManager.Settings.RegexHistory.TesttextHistoryList.Add("42");
        _configManager.Settings.ColumnizerHistoryList.Add(new ColumnizerHistoryEntry(@"C:\logs\one.log", "JsonColumnizer"));

        _configManager.ClearFileHistory();

        Assert.Multiple(() =>
        {
            Assert.That(_configManager.Settings.FileHistoryList, Is.Empty);
            Assert.That(_configManager.Settings.LastOpenFilesList, Has.Count.EqualTo(1));
            Assert.That(_configManager.Settings.SearchHistoryList, Has.Count.EqualTo(1));
            Assert.That(_configManager.Settings.FilterHistoryList, Has.Count.EqualTo(1));
            Assert.That(_configManager.Settings.FilterRangeHistoryList, Has.Count.EqualTo(1));
            Assert.That(_configManager.Settings.UriHistoryList, Has.Count.EqualTo(1));
            Assert.That(_configManager.Settings.RegexHistory.ExpressionHistoryList, Has.Count.EqualTo(1));
            Assert.That(_configManager.Settings.RegexHistory.TesttextHistoryList, Has.Count.EqualTo(1));
            Assert.That(_configManager.Settings.ColumnizerHistoryList, Has.Count.EqualTo(1));
        });
    }

    /// <summary>
    /// Resets the singleton so each test loads its settings from its own temp directory.
    /// </summary>
    private void ResetConfigManagerInitialization ()
    {
        SetPrivateField("_isInitialized", false);
        SetPrivateField("_settings", null);
    }

    private object? GetPrivateField (string fieldName)
    {
        return Field(fieldName).GetValue(_configManager);
    }

    private void SetPrivateField (string fieldName, object? value)
    {
        Field(fieldName).SetValue(_configManager, value);
    }

    private static FieldInfo Field (string fieldName)
    {
        return typeof(Configuration.ConfigManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"ConfigManager.{fieldName} is gone; update this fixture's reset.");
    }
}
