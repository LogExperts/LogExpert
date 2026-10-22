using System.Reflection;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

/// <summary>
/// Unit tests for ConfigManager portable mode functionality.
/// Tests: ActiveConfigDir, ActiveSessionDir, PortableConfigDir, PortableSessionDir,
/// MigrateOldPortableLayout, CopyConfigToPortable, MoveConfigFromPortable.
/// </summary>
[TestFixture]
public class ConfigManagerPortableModeTests
{
    private string _testDir;
    private Configuration.ConfigManager _configManager;

    [SetUp]
    public void SetUp ()
    {
        // Create isolated test directory for each test
        _testDir = Path.Join(Path.GetTempPath(), "LogExpert_PortableTest_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(_testDir);

        // Initialize ConfigManager for testing
        _configManager = Configuration.ConfigManager.Instance;

        // Reset the singleton's initialization state using reflection
        ResetConfigManagerInitialization();

        _configManager.Initialize(_testDir, new Rectangle(0, 0, 1920, 1080));
    }

    [TearDown]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Test")]
    public void TearDown ()
    {
        // Reset portable mode to ensure isolation
        try
        {
            if (_configManager.Settings?.Preferences != null)
            {
                _configManager.Settings.Preferences.PortableMode = false;
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }

        // Cleanup test directory
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Helper Methods

    /// <summary>
    /// Resets ConfigManager singleton initialization state via reflection.
    /// </summary>
    private void ResetConfigManagerInitialization ()
    {
        var isInitializedField = typeof(Configuration.ConfigManager).GetField("_isInitialized", BindingFlags.NonPublic | BindingFlags.Instance);
        isInitializedField?.SetValue(_configManager, false);

        // Reset settings so they reload from the new path
        var settingsField = typeof(Configuration.ConfigManager).GetField("_settings", BindingFlags.NonPublic | BindingFlags.Instance);
        settingsField?.SetValue(_configManager, null);
    }

    /// <summary>
    /// Invokes a private instance method using reflection.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Unit Tests")]
    private void InvokePrivateInstanceMethod (string methodName, params object[] parameters)
    {
        MethodInfo? method = typeof(Configuration.ConfigManager).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new Exception($"Instance method {methodName} not found");

        _ = method.Invoke(_configManager, parameters);
    }

    /// <summary>
    /// Creates a marker file for portable mode detection.
    /// </summary>
    private void CreatePortableModeMarker (string directory)
    {
        _ = Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Join(directory, _configManager.PortableModeSettingsFileName),
            "{}");
    }

    /// <summary>
    /// Creates a settings.json file in the given directory.
    /// </summary>
    private static void CreateSettingsFile (string directory, string content = """{ "Preferences": { "FontName": "Courier New", "FontSize": 9 }, "FilterList": [], "SearchHistoryList": [] }""")
    {
        _ = Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Join(directory, "settings.json"), content);
    }

    #endregion

    #region PortableConfigDir Tests

    [Test]
    [Category("PortableMode")]
    [Description("PortableConfigDir should return {AppDir}/configuration/")]
    public void PortableConfigDir_ReturnsConfigurationSubdirectory ()
    {
        // Act
        var result = _configManager.PortableConfigDir;

        // Assert
        var expected = Path.Join(_testDir, "configuration");
        Assert.That(result, Is.EqualTo(expected), "PortableConfigDir should be {AppDir}/configuration/");
    }

    #endregion

    #region PortableSessionDir Tests

    [Test]
    [Category("PortableMode")]
    [Description("PortableSessionDir should return {AppDir}/configuration/sessions/")]
    public void PortableSessionDir_ReturnsSessionsSubdirectory ()
    {
        // Act
        var result = _configManager.PortableSessionDir;

        // Assert
        var expected = Path.Join(_testDir, "configuration", "sessions");
        Assert.That(result, Is.EqualTo(expected), "PortableSessionDir should be {AppDir}/configuration/sessions/");
    }

    #endregion

    #region ActiveConfigDir Tests

    [Test]
    [Category("PortableMode")]
    [Description("ActiveConfigDir should return ConfigDir when portable mode is off")]
    public void ActiveConfigDir_NormalMode_ReturnsConfigDir ()
    {
        // Arrange
        _configManager.Settings.Preferences.PortableMode = false;

        // Act
        var result = _configManager.ActiveConfigDir;

        // Assert
        Assert.That(result, Is.EqualTo(_configManager.ConfigDir), "ActiveConfigDir should be ConfigDir in normal mode");
    }

    [Test]
    [Category("PortableMode")]
    [Description("ActiveConfigDir should return PortableConfigDir when portable mode is on")]
    public void ActiveConfigDir_PortableMode_ReturnsPortableConfigDir ()
    {
        // Arrange
        _configManager.Settings.Preferences.PortableMode = true;

        // Act
        var result = _configManager.ActiveConfigDir;

        // Assert
        Assert.That(result, Is.EqualTo(_configManager.PortableConfigDir), "ActiveConfigDir should be PortableConfigDir in portable mode");
    }

    [Test]
    [Category("PortableMode")]
    [Description("ActiveConfigDir should toggle when PortableMode changes")]
    public void ActiveConfigDir_TogglesWithPortableMode ()
    {
        // Arrange & Act - Start in normal mode
        _configManager.Settings.Preferences.PortableMode = false;
        var normalDir = _configManager.ActiveConfigDir;

        // Switch to portable
        _configManager.Settings.Preferences.PortableMode = true;
        var portableDir = _configManager.ActiveConfigDir;

        // Switch back
        _configManager.Settings.Preferences.PortableMode = false;
        var backToNormal = _configManager.ActiveConfigDir;

        // Assert
        Assert.That(normalDir, Is.EqualTo(_configManager.ConfigDir));
        Assert.That(portableDir, Is.EqualTo(_configManager.PortableConfigDir));
        Assert.That(backToNormal, Is.EqualTo(normalDir), "Should return to normal ConfigDir");
        Assert.That(normalDir, Is.Not.EqualTo(portableDir), "Normal and portable dirs should differ");
    }

    #endregion

    #region ActiveSessionDir Tests

    [Test]
    [Category("PortableMode")]
    [Description("ActiveSessionDir should return {AppDir}/sessionFiles when portable mode is off")]
    public void ActiveSessionDir_NormalMode_ReturnsSessionFilesSubdirectory ()
    {
        // Arrange
        _configManager.Settings.Preferences.PortableMode = false;

        // Act
        var result = _configManager.ActiveSessionDir;

        // Assert
        var expected = Path.Join(_testDir, "sessionFiles");
        Assert.That(result, Is.EqualTo(expected), "ActiveSessionDir should be {AppDir}/sessionFiles in normal mode");
    }

    [Test]
    [Category("PortableMode")]
    [Description("ActiveSessionDir should return PortableSessionDir when portable mode is on")]
    public void ActiveSessionDir_PortableMode_ReturnsPortableSessionDir ()
    {
        // Arrange
        _configManager.Settings.Preferences.PortableMode = true;

        // Act
        var result = _configManager.ActiveSessionDir;

        // Assert
        Assert.That(result, Is.EqualTo(_configManager.PortableSessionDir), "ActiveSessionDir should be PortableSessionDir in portable mode");
    }

    [Test]
    [Category("PortableMode")]
    [Description("ActiveSessionDir should toggle when PortableMode changes")]
    public void ActiveSessionDir_TogglesWithPortableMode ()
    {
        // Arrange & Act - Start in normal mode
        _configManager.Settings.Preferences.PortableMode = false;
        var normalDir = _configManager.ActiveSessionDir;

        // Switch to portable
        _configManager.Settings.Preferences.PortableMode = true;
        var portableDir = _configManager.ActiveSessionDir;

        // Switch back
        _configManager.Settings.Preferences.PortableMode = false;
        var backToNormal = _configManager.ActiveSessionDir;

        // Assert
        var expectedNormal = Path.Join(_testDir, "sessionFiles");
        Assert.That(normalDir, Is.EqualTo(expectedNormal));
        Assert.That(portableDir, Is.EqualTo(_configManager.PortableSessionDir));
        Assert.That(backToNormal, Is.EqualTo(normalDir), "Should return to normal session dir");
        Assert.That(normalDir, Is.Not.EqualTo(portableDir), "Normal and portable session dirs should differ");
    }

    #endregion

    #region MigrateOldPortableLayout Tests

    [Test]
    [Category("PortableMode")]
    [Description("MigrateOldPortableLayout should move settings.json from app root to configuration/")]
    public void MigrateOldPortableLayout_MovesSettingsFromAppRoot ()
    {
        // Arrange - Create settings.json in app root (old location)
        File.WriteAllText(Path.Join(_testDir, "settings.json"), """{ "Preferences": { "FontName": "Test" }, "FilterList": [] }""");

        // Act
        InvokePrivateInstanceMethod("MigrateOldPortableLayout");

        // Assert
        Assert.That(File.Exists(Path.Join(_configManager.PortableConfigDir, "settings.json")), Is.True,
            "settings.json should be moved to configuration/");
        Assert.That(File.Exists(Path.Join(_testDir, "settings.json")), Is.False,
            "settings.json should no longer exist in app root");
    }

    [Test]
    [Category("PortableMode")]
    [Description("MigrateOldPortableLayout should move files from old portable/ directory")]
    public void MigrateOldPortableLayout_MovesFilesFromOldPortableDir ()
    {
        // Arrange - Create old portable directory with marker and config file
#pragma warning disable CS0618
        var oldPortableDir = _configManager.PortableModeDir;
#pragma warning restore CS0618
        _ = Directory.CreateDirectory(oldPortableDir);
        File.WriteAllText(Path.Join(oldPortableDir, "portableMode.json"), "{}");
        File.WriteAllText(Path.Join(oldPortableDir, "trusted-plugins.json"), "[]");

        // Act
        InvokePrivateInstanceMethod("MigrateOldPortableLayout");

        // Assert
        Assert.That(File.Exists(Path.Join(_configManager.PortableConfigDir, "portableMode.json")), Is.True,
            "portableMode.json should be migrated to configuration/");
        Assert.That(File.Exists(Path.Join(_configManager.PortableConfigDir, "trusted-plugins.json")), Is.True,
            "trusted-plugins.json should be migrated to configuration/");
    }

    [Test]
    [Category("PortableMode")]
    [Description("MigrateOldPortableLayout should move sessionFiles to configuration/sessions/")]
    public void MigrateOldPortableLayout_MovesSessionFiles ()
    {
        // Arrange - Create old sessionFiles directory
        var oldSessionDir = Path.Join(_testDir, "sessionFiles");
        _ = Directory.CreateDirectory(oldSessionDir);
        File.WriteAllText(Path.Join(oldSessionDir, "session1.lxp"), "{}");
        File.WriteAllText(Path.Join(oldSessionDir, "session2.lxp"), "{}");

        // Act
        InvokePrivateInstanceMethod("MigrateOldPortableLayout");

        // Assert
        Assert.That(File.Exists(Path.Join(_configManager.PortableSessionDir, "session1.lxp")), Is.True,
            "session1.lxp should be moved to configuration/sessions/");
        Assert.That(File.Exists(Path.Join(_configManager.PortableSessionDir, "session2.lxp")), Is.True,
            "session2.lxp should be moved to configuration/sessions/");
    }

    [Test]
    [Category("PortableMode")]
    [Description("MigrateOldPortableLayout should delete empty old portable directory")]
    public void MigrateOldPortableLayout_DeletesEmptyOldDir ()
    {
        // Arrange
#pragma warning disable CS0618
        var oldPortableDir = _configManager.PortableModeDir;
#pragma warning restore CS0618
        _ = Directory.CreateDirectory(oldPortableDir);
        File.WriteAllText(Path.Join(oldPortableDir, "portableMode.json"), "{}");

        // Act
        InvokePrivateInstanceMethod("MigrateOldPortableLayout");

        // Assert
        Assert.That(Directory.Exists(oldPortableDir), Is.False,
            "Empty old portable directory should be deleted after migration");
    }

    [Test]
    [Category("PortableMode")]
    [Description("MigrateOldPortableLayout should move subdirectories from old portable/")]
    public void MigrateOldPortableLayout_MovesSubdirectories ()
    {
        // Arrange - Create old portable/Plugins/ directory
#pragma warning disable CS0618
        var oldPortableDir = _configManager.PortableModeDir;
#pragma warning restore CS0618
        var oldPluginsDir = Path.Join(oldPortableDir, "Plugins");
        _ = Directory.CreateDirectory(oldPluginsDir);
        File.WriteAllText(Path.Join(oldPluginsDir, "MyPlugin.dll"), "fake dll");

        // Act
        InvokePrivateInstanceMethod("MigrateOldPortableLayout");

        // Assert
        var newPluginsDir = Path.Join(_configManager.PortableConfigDir, "Plugins");
        Assert.That(Directory.Exists(newPluginsDir), Is.True,
            "Plugins directory should be moved to configuration/Plugins/");
        Assert.That(File.Exists(Path.Join(newPluginsDir, "MyPlugin.dll")), Is.True,
            "Plugin files should be preserved");
    }

    [Test]
    [Category("PortableMode")]
    [Description("MigrateOldPortableLayout should move settings.json backup (.bak)")]
    public void MigrateOldPortableLayout_MovesSettingsBackup ()
    {
        // Arrange
        File.WriteAllText(Path.Join(_testDir, "settings.json.bak"), """{ "Preferences": {}, "FilterList": [] }""");

        // Act
        InvokePrivateInstanceMethod("MigrateOldPortableLayout");

        // Assert
        Assert.That(File.Exists(Path.Join(_configManager.PortableConfigDir, "settings.json.bak")), Is.True,
            "settings.json.bak should be moved to configuration/");
    }

    #endregion

    #region CopyConfigToPortable Tests

    [Test]
    [Category("PortableMode")]
    [Description("CopyConfigToPortable should create the portable configuration directory")]
    public void CopyConfigToPortable_CreatesPortableConfigDir ()
    {
        // Arrange - Ensure settings.json exists in normal location
        _ = Directory.CreateDirectory(_configManager.ConfigDir);
        CreateSettingsFile(_configManager.ConfigDir);

        // Act
        _configManager.CopyConfigToPortable();

        // Assert
        Assert.That(Directory.Exists(_configManager.PortableConfigDir), Is.True,
            "CopyConfigToPortable should create the portable configuration directory");
    }

    [Test]
    [Category("PortableMode")]
    [Description("CopyConfigToPortable should copy settings.json")]
    public void CopyConfigToPortable_CopiesSettingsFile ()
    {
        // Arrange
        var settingsContent = """{ "Preferences": { "FontName": "TestCopy" }, "FilterList": [] }""";
        CreateSettingsFile(_configManager.ConfigDir, settingsContent);

        // Act
        _configManager.CopyConfigToPortable();

        // Assert
        var portableSettingsFile = Path.Join(_configManager.PortableConfigDir, "settings.json");
        Assert.That(File.Exists(portableSettingsFile), Is.True, "settings.json should be copied");
        var content = File.ReadAllText(portableSettingsFile);
        Assert.That(content, Does.Contain("TestCopy"), "Copied file should have correct content");
    }

    [Test]
    [Category("PortableMode")]
    [Description("CopyConfigToPortable should copy trusted-plugins.json")]
    public void CopyConfigToPortable_CopiesTrustedPluginsFile ()
    {
        // Arrange
        _ = Directory.CreateDirectory(_configManager.ConfigDir);
        File.WriteAllText(
            Path.Join(_configManager.ConfigDir, "trusted-plugins.json"),
            """{ "TrustedPlugins": [] }""");

        // Act
        _configManager.CopyConfigToPortable();

        // Assert
        Assert.That(File.Exists(Path.Join(_configManager.PortableConfigDir, "trusted-plugins.json")), Is.True,
            "trusted-plugins.json should be copied");
    }

    [Test]
    [Category("PortableMode")]
    [Description("CopyConfigToPortable should copy Plugins subdirectory recursively")]
    public void CopyConfigToPortable_CopiesPluginsDirectory ()
    {
        // Arrange
        var pluginsDir = Path.Join(_configManager.ConfigDir, "Plugins");
        _ = Directory.CreateDirectory(pluginsDir);
        File.WriteAllText(Path.Join(pluginsDir, "TestPlugin.dll"), "fake dll");

        var subDir = Path.Join(pluginsDir, "SubDir");
        _ = Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Join(subDir, "SubFile.dll"), "fake dll 2");

        // Act
        _configManager.CopyConfigToPortable();

        // Assert
        var portablePluginsDir = Path.Join(_configManager.PortableConfigDir, "Plugins");
        Assert.That(File.Exists(Path.Join(portablePluginsDir, "TestPlugin.dll")), Is.True,
            "Plugin files should be copied");
        Assert.That(File.Exists(Path.Join(portablePluginsDir, "SubDir", "SubFile.dll")), Is.True,
            "Plugin subdirectory files should be copied recursively");
    }

    [Test]
    [Category("PortableMode")]
    [Description("CopyConfigToPortable should copy .dat/.cfg config files")]
    public void CopyConfigToPortable_CopiesColumnizerConfigFiles ()
    {
        // Arrange
        _ = Directory.CreateDirectory(_configManager.ConfigDir);
        File.WriteAllText(Path.Join(_configManager.ConfigDir, "MyColumnizer.cfg"), "config data");
        File.WriteAllText(Path.Join(_configManager.ConfigDir, "SomeData.dat"), "data content");

        // Act
        _configManager.CopyConfigToPortable();

        // Assert
        Assert.That(File.Exists(Path.Join(_configManager.PortableConfigDir, "MyColumnizer.cfg")), Is.True,
            ".cfg files should be copied");
        Assert.That(File.Exists(Path.Join(_configManager.PortableConfigDir, "SomeData.dat")), Is.True,
            ".dat files should be copied");
    }

    [Test]
    [Category("PortableMode")]
    [Description("CopyConfigToPortable with no source files should not throw")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Test")]
    public void CopyConfigToPortable_NoSourceFiles_DoesNotThrow ()
    {
        // Arrange - ConfigDir might not exist or be empty
        // (don't create any files)

        // Act & Assert
        Assert.DoesNotThrow(_configManager.CopyConfigToPortable, "Should not throw when no source files exist");
    }

    #endregion

    #region MoveConfigFromPortable Tests

    [Test]
    [Category("PortableMode")]
    [Description("MoveConfigFromPortable should move config files back to ConfigDir")]
    public void MoveConfigFromPortable_MovesConfigFilesToNormalLocation ()
    {
        // Arrange - Create files in portable config dir
        _ = Directory.CreateDirectory(_configManager.PortableConfigDir);
        File.WriteAllText(Path.Join(_configManager.PortableConfigDir, "settings.json"),
            """{ "Preferences": { "FontName": "Portable" }, "FilterList": [] }""");
        File.WriteAllText(Path.Join(_configManager.PortableConfigDir, "trusted-plugins.json"), "[]");
        // Keep marker file (it should be skipped)
        CreatePortableModeMarker(_configManager.PortableConfigDir);

        // Act
        _configManager.MoveConfigFromPortable();

        // Assert
        Assert.That(File.Exists(Path.Join(_configManager.ConfigDir, "settings.json")), Is.True,
            "settings.json should be moved to ConfigDir");
        Assert.That(File.Exists(Path.Join(_configManager.ConfigDir, "trusted-plugins.json")), Is.True,
            "trusted-plugins.json should be moved to ConfigDir");
    }

    [Test]
    [Category("PortableMode")]
    [Description("MoveConfigFromPortable should skip the portableMode.json marker file")]
    public void MoveConfigFromPortable_SkipsMarkerFile ()
    {
        // Arrange
        _ = Directory.CreateDirectory(_configManager.PortableConfigDir);
        CreatePortableModeMarker(_configManager.PortableConfigDir);
        File.WriteAllText(Path.Join(_configManager.PortableConfigDir, "settings.json"), "{}");

        // Act
        _configManager.MoveConfigFromPortable();

        // Assert
        Assert.That(
            File.Exists(Path.Join(_configManager.ConfigDir, _configManager.PortableModeSettingsFileName)),
            Is.False,
            "portableMode.json marker should NOT be moved to ConfigDir");
    }

    [Test]
    [Category("PortableMode")]
    [Description("MoveConfigFromPortable should move Plugins directory")]
    public void MoveConfigFromPortable_MovesPluginsDirectory ()
    {
        // Arrange
        var portablePluginsDir = Path.Join(_configManager.PortableConfigDir, "Plugins");
        _ = Directory.CreateDirectory(portablePluginsDir);
        File.WriteAllText(Path.Join(portablePluginsDir, "TestPlugin.dll"), "fake");

        // Act
        _configManager.MoveConfigFromPortable();

        // Assert
        var normalPluginsDir = Path.Join(_configManager.ConfigDir, "Plugins");
        Assert.That(File.Exists(Path.Join(normalPluginsDir, "TestPlugin.dll")), Is.True,
            "Plugins directory should be moved to ConfigDir");
    }

    [Test]
    [Category("PortableMode")]
    [Description("MoveConfigFromPortable should move session files to Documents/LogExpert")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Tests")]
    public void MoveConfigFromPortable_MovesSessionFiles ()
    {
        // Arrange
        _ = Directory.CreateDirectory(_configManager.PortableSessionDir);
        File.WriteAllText(Path.Join(_configManager.PortableSessionDir, "session.lxp"), "{}");

        // Act
        _configManager.MoveConfigFromPortable();

        // Assert
        var docsSessionDir = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LogExpert");
        Assert.That(File.Exists(Path.Join(docsSessionDir, "session.lxp")), Is.True,
            "Session files should be moved to Documents/LogExpert/");

        // Cleanup: remove the file we created in Documents
        try
        {
            File.Delete(Path.Join(docsSessionDir, "session.lxp"));
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Test]
    [Category("PortableMode")]
    [Description("MoveConfigFromPortable should overwrite existing files in ConfigDir")]
    public void MoveConfigFromPortable_OverwritesExistingFiles ()
    {
        // Arrange
        _ = Directory.CreateDirectory(_configManager.ConfigDir);
        File.WriteAllText(Path.Join(_configManager.ConfigDir, "trusted-plugins.json"), "old content");

        _ = Directory.CreateDirectory(_configManager.PortableConfigDir);
        File.WriteAllText(Path.Join(_configManager.PortableConfigDir, "trusted-plugins.json"), "new content");

        // Act
        _configManager.MoveConfigFromPortable();

        // Assert
        var content = File.ReadAllText(Path.Join(_configManager.ConfigDir, "trusted-plugins.json"));
        Assert.That(content, Is.EqualTo("new content"), "Should overwrite existing file with portable version");
    }

    [Test]
    [Category("PortableMode")]
    [Description("MoveConfigFromPortable should clean up empty portable config directory")]
    public void MoveConfigFromPortable_CleansUpEmptyPortableDir ()
    {
        // Arrange - Create portable dir with only one file to move
        _ = Directory.CreateDirectory(_configManager.PortableConfigDir);
        File.WriteAllText(Path.Join(_configManager.PortableConfigDir, "test.cfg"), "data");

        // Act
        _configManager.MoveConfigFromPortable();

        // Assert
        Assert.That(Directory.Exists(_configManager.PortableConfigDir), Is.False,
            "Empty portable configuration directory should be deleted after migration");
    }

    #endregion

    #region Helper Method Tests

    [Test]
    [Category("PortableMode")]
    [Description("MoveFileIfExists should move a file and delete source")]
    public void MoveFileIfExists_MovesFile ()
    {
        // Arrange
        var source = Path.Join(_testDir, "source.txt");
        var target = Path.Join(_testDir, "target.txt");
        File.WriteAllText(source, "test content");

        // Act
        var method = typeof(Configuration.ConfigManager).GetMethod("MoveFileIfExists", BindingFlags.NonPublic | BindingFlags.Static);
        _ = (method?.Invoke(null, [source, target]));

        // Assert
        Assert.That(File.Exists(target), Is.True, "Target file should exist");
        Assert.That(File.Exists(source), Is.False, "Source file should be deleted");
        Assert.That(File.ReadAllText(target), Is.EqualTo("test content"), "Content should be preserved");
    }

    [Test]
    [Category("PortableMode")]
    [Description("MoveFileIfExists with non-existent source should do nothing")]
    public void MoveFileIfExists_NonExistentSource_DoesNothing ()
    {
        // Arrange
        var source = Path.Join(_testDir, "does_not_exist.txt");
        var target = Path.Join(_testDir, "target.txt");

        // Act
        var method = typeof(Configuration.ConfigManager).GetMethod("MoveFileIfExists", BindingFlags.NonPublic | BindingFlags.Static);
        _ = (method?.Invoke(null, [source, target]));

        // Assert
        Assert.That(File.Exists(target), Is.False, "Target should not be created");
    }

    [Test]
    [Category("PortableMode")]
    [Description("CopyFileIfExists should copy file preserving source")]
    public void CopyFileIfExists_CopiesFile ()
    {
        // Arrange
        var source = Path.Join(_testDir, "source.txt");
        var target = Path.Join(_testDir, "target.txt");
        File.WriteAllText(source, "copy me");

        // Act
        var method = typeof(Configuration.ConfigManager).GetMethod("CopyFileIfExists", BindingFlags.NonPublic | BindingFlags.Static);
        _ = (method?.Invoke(null, [source, target]));

        // Assert
        Assert.That(File.Exists(source), Is.True, "Source should still exist");
        Assert.That(File.Exists(target), Is.True, "Target should be created");
        Assert.That(File.ReadAllText(target), Is.EqualTo("copy me"));
    }

    [Test]
    [Category("PortableMode")]
    [Description("CopyFileIfNotExists should not overwrite existing target")]
    public void CopyFileIfNotExists_DoesNotOverwrite ()
    {
        // Arrange
        var source = Path.Join(_testDir, "source.txt");
        var target = Path.Join(_testDir, "target.txt");
        File.WriteAllText(source, "new content");
        File.WriteAllText(target, "existing content");

        // Act
        var method = typeof(Configuration.ConfigManager).GetMethod("CopyFileIfNotExists", BindingFlags.NonPublic | BindingFlags.Static);
        _ = (method?.Invoke(null, [source, target]));

        //Assert
        Assert.That(File.ReadAllText(target), Is.EqualTo("existing content"),
            "Existing target should not be overwritten");
    }

    [Test]
    [Category("PortableMode")]
    [Description("CopyDirectoryRecursive should copy all files and subdirectories")]
    public void CopyDirectoryRecursive_CopiesEverything ()
    {
        // Arrange
        var sourceDir = Path.Join(_testDir, "sourceDir");
        var targetDir = Path.Join(_testDir, "targetDir");
        _ = Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Join(sourceDir, "file1.txt"), "content1");

        var subDir = Path.Join(sourceDir, "sub");
        _ = Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Join(subDir, "file2.txt"), "content2");

        // Act
        var method = typeof(Configuration.ConfigManager).GetMethod("CopyDirectoryRecursive", BindingFlags.NonPublic | BindingFlags.Static);
        _ = (method?.Invoke(null, [sourceDir, targetDir]));

        // Assert
        Assert.That(File.Exists(Path.Join(targetDir, "file1.txt")), Is.True);
        Assert.That(File.Exists(Path.Join(targetDir, "sub", "file2.txt")), Is.True);
        Assert.That(File.ReadAllText(Path.Join(targetDir, "file1.txt")), Is.EqualTo("content1"));
        Assert.That(File.ReadAllText(Path.Join(targetDir, "sub", "file2.txt")), Is.EqualTo("content2"));
    }

    #endregion

    #region Load Detection Tests

    [Test]
    [Category("PortableMode")]
    [Description("Load should detect new portable layout when portableMode.json exists in configuration/")]
    public void Load_NewPortableLayout_DetectedCorrectly ()
    {
        // Arrange - Create new portable layout
        CreatePortableModeMarker(_configManager.PortableConfigDir);
        CreateSettingsFile(_configManager.PortableConfigDir);

        // Reset settings to force reload
        ResetConfigManagerInitialization();
        _configManager.Initialize(_testDir, new Rectangle(0, 0, 1920, 1080));

        // Act - Access Settings to trigger Load()
        var settings = _configManager.Settings;

        // Assert
        Assert.That(settings, Is.Not.Null, "Settings should be loaded from new portable layout");
    }

    [Test]
    [Category("PortableMode")]
    [Description("Load should detect old portable layout and trigger migration")]
    public void Load_OldPortableLayout_TriggersMigration ()
    {
        // Arrange - Create old portable layout
#pragma warning disable CS0618
        var oldPortableDir = _configManager.PortableModeDir;
#pragma warning restore CS0618
        CreatePortableModeMarker(oldPortableDir);
        // Put settings.json in app root (old layout)
        File.WriteAllText(Path.Join(_testDir, "settings.json"),
            """{ "Preferences": { "FontName": "OldLayout" }, "FilterList": [] }""");

        // Reset settings to force reload
        ResetConfigManagerInitialization();
        _configManager.Initialize(_testDir, new Rectangle(0, 0, 1920, 1080));

        // Act - Access Settings to trigger Load() which should migrate
        var settings = _configManager.Settings;

        // Assert
        Assert.That(settings, Is.Not.Null, "Settings should load after migration");
        // After migration, settings should be in the new configuration/ directory
        Assert.That(File.Exists(Path.Join(_configManager.PortableConfigDir, "settings.json")), Is.True,
            "settings.json should be migrated to configuration/ directory");
    }

    #endregion

    #region End-to-End Portable Mode Toggle Tests

    [Test]
    [Category("PortableMode")]
    [Description("Full cycle: activate portable mode, copy config, verify, deactivate")]
    public void PortableMode_FullToggleCycle_WorksCorrectly ()
    {
        // Arrange - Create some config in normal location
        _ = Directory.CreateDirectory(_configManager.ConfigDir);
        File.WriteAllText(Path.Join(_configManager.ConfigDir, "trusted-plugins.json"), """{ "test": true }""");

        // Step 1: Activate portable mode and copy config
        _configManager.CopyConfigToPortable();
        _configManager.Settings.Preferences.PortableMode = true;

        // Verify config was copied
        Assert.That(File.Exists(Path.Join(_configManager.PortableConfigDir, "trusted-plugins.json")), Is.True,
            "Config should be copied to portable directory");
        Assert.That(_configManager.ActiveConfigDir, Is.EqualTo(_configManager.PortableConfigDir),
            "ActiveConfigDir should point to portable dir");

        // Step 2: Deactivate portable mode and move config back
        _configManager.Settings.Preferences.PortableMode = false;
        _configManager.MoveConfigFromPortable();

        // Verify config was moved back
        Assert.That(_configManager.ActiveConfigDir, Is.EqualTo(_configManager.ConfigDir),
            "ActiveConfigDir should point back to ConfigDir");
    }

    #endregion
}
