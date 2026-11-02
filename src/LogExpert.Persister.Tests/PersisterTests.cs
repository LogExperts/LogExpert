using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;

namespace LogExpert.Persister.Tests;

[TestFixture]
public class PersisterTests
{
    private string _testDirectory;
    private string _applicationStartupPath;
    private string _sessionDirectory;
    private string _logFileName;

    [SetUp]
    public void Setup ()
    {
        // Create temporary test directory
        _testDirectory = Path.Join(Path.GetTempPath(), "LogExpertTests", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(_testDirectory);

        // Create a subdirectory to simulate application startup path
        _applicationStartupPath = Path.Join(_testDirectory, "ApplicationPath");
        _ = Directory.CreateDirectory(_applicationStartupPath);

        // Session directory: simulates the resolved session base directory
        // In portable mode this would be {AppDir}/configuration/sessions/
        // In normal mode this would be {AppDir}/sessionFiles/
        _sessionDirectory = Path.Join(_applicationStartupPath, "sessionFiles");

        // Create a test log file
        _logFileName = Path.Join(_testDirectory, "test.log");
        File.WriteAllText(_logFileName, "Test log content");
    }

    [TearDown]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Test")]
    public void TearDown ()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region SavePersistenceData Tests - ApplicationStartupDir Location

    [Test]
    public void SavePersistenceData_WithApplicationStartupDir_CreatesSessionDirectory ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName
        };

        // Act
        var savedFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, _sessionDirectory);

        // Assert
        Assert.That(Directory.Exists(_sessionDirectory), Is.True, "Session directory should be created");
        Assert.That(savedFileName, Does.StartWith(_sessionDirectory), "Saved file should be in session directory");
    }

    [Test]
    public void SavePersistenceData_WithApplicationStartupDir_SavesFileWithCorrectName ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName,
            CurrentLine = 42,
            FollowTail = true
        };

        // Act
        var savedFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, _sessionDirectory);

        // Assert
        Assert.That(File.Exists(savedFileName), Is.True, "Persistence file should exist");
        Assert.That(savedFileName, Does.EndWith(".lxp"), "Persistence file should have .lxp extension");
    }

    [Test]
    public void SavePersistenceData_WithApplicationStartupDir_FileContainsCorrectData ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName,
            CurrentLine = 42,
            FollowTail = true,
            FilterVisible = true
        };

        // Act
        var savedFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, _sessionDirectory);

        // Assert
        var savedContent = File.ReadAllText(savedFileName);
        Assert.That(savedContent, Does.Contain("\"CurrentLine\": 42"), "Should contain CurrentLine value");
        Assert.That(savedContent, Does.Contain("\"FollowTail\": true"), "Should contain FollowTail value");
        Assert.That(savedContent, Does.Contain("\"FilterVisible\": true"), "Should contain FilterVisible value");
    }

    [Test]
    public void SavePersistenceData_WithApplicationStartupDir_NullApplicationStartupPath_ThrowsArgumentNullException ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName
        };

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, null));
    }

    #endregion

    #region SavePersistenceData Tests - Other Locations

    [Test]
    public void SavePersistenceData_WithSameDir_DoesNotUseApplicationStartupPath ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.SameDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName
        };

        // Act
        var savedFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, _applicationStartupPath);

        // Assert
        Assert.That(savedFileName, Does.Not.Contain(_applicationStartupPath), "Should not use applicationStartupPath for SameDir location");
        Assert.That(savedFileName, Does.StartWith(_testDirectory), "Should save in same directory as log file");
    }

    [Test]
    public void SavePersistenceData_WithDocumentsDir_DoesNotUseApplicationStartupPath ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.DocumentsDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName
        };

        // Act
        var savedFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, _applicationStartupPath);

        // Assert
        Assert.That(savedFileName, Does.Not.Contain(_applicationStartupPath), "Should not use applicationStartupPath for DocumentsDir location");
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        Assert.That(savedFileName, Does.StartWith(documentsPath), "Should save in Documents directory");
    }

    [Test]
    public void SavePersistenceData_WithOwnDir_DoesNotUseApplicationStartupPath ()
    {
        // Arrange
        var customDirectory = Path.Join(_testDirectory, "CustomSessionDir");
        _ = Directory.CreateDirectory(customDirectory);

        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.OwnDir,
            SessionSaveDirectory = customDirectory
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName
        };

        // Act
        var savedFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, _applicationStartupPath);

        // Assert
        Assert.That(savedFileName, Does.Not.Contain(_applicationStartupPath), "Should not use applicationStartupPath for OwnDir location");
        Assert.That(savedFileName, Does.StartWith(customDirectory), "Should save in custom directory");
    }

    #endregion

    #region LoadPersistenceData Tests

    [Test]
    public void LoadPersistenceData_WithApplicationStartupDir_LoadsCorrectFile ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var originalData = new PersistenceData
        {
            FileName = _logFileName,
            CurrentLine = 123,
            FollowTail = true,
            FilterVisible = false
        };

        // Save data first
        _ = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, originalData, preferences, _sessionDirectory);

        // Act
        var loadedData = Core.Classes.Persister.Persister.LoadPersistenceData(_logFileName, preferences, _sessionDirectory);

        // Assert
        Assert.That(loadedData, Is.Not.Null, "Should load persistence data");
        Assert.That(loadedData.CurrentLine, Is.EqualTo(123), "Should load correct CurrentLine");
        Assert.That(loadedData.FollowTail, Is.True, "Should load correct FollowTail");
        Assert.That(loadedData.FilterVisible, Is.False, "Should load correct FilterVisible");
    }

    [Test]
    public void LoadPersistenceData_WithApplicationStartupDir_FileNotExists_ReturnsNull ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var nonExistentFile = Path.Join(_testDirectory, "nonexistent.log");

        // Act
        var loadedData = Core.Classes.Persister.Persister.LoadPersistenceData(nonExistentFile, preferences, _sessionDirectory);

        // Assert
        Assert.That(loadedData, Is.Null, "Should return null when file doesn't exist");
    }

    [Test]
    public void LoadPersistenceData_WithApplicationStartupDir_NullApplicationStartupPath_ThrowsArgumentNullException ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => Core.Classes.Persister.Persister.LoadPersistenceData(_logFileName, preferences, null));
    }

    #endregion

    #region LoadPersistenceDataOptionsOnly Tests

    [Test]
    public void LoadPersistenceDataOptionsOnly_WithApplicationStartupDir_LoadsCorrectData ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var originalData = new PersistenceData
        {
            FileName = _logFileName,
            MultiFile = true,
            MultiFilePattern = "*.log",
            FilterAdvanced = true
        };

        // Save data first
        _ = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, originalData, preferences, _sessionDirectory);

        // Act
        var loadedData = Core.Classes.Persister.Persister.LoadPersistenceDataOptionsOnly(_logFileName, preferences, _sessionDirectory);

        // Assert
        Assert.That(loadedData, Is.Not.Null, "Should load persistence data");
        Assert.That(loadedData.MultiFile, Is.True, "Should load correct MultiFile");
        Assert.That(loadedData.MultiFilePattern, Is.EqualTo("*.log"), "Should load correct MultiFilePattern");
        Assert.That(loadedData.FilterAdvanced, Is.True, "Should load correct FilterAdvanced");
    }

    [Test]
    public void LoadPersistenceDataOptionsOnly_WithApplicationStartupDir_NullApplicationStartupPath_ThrowsArgumentNullException ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => Core.Classes.Persister.Persister.LoadPersistenceDataOptionsOnly(_logFileName, preferences, null));
    }

    #endregion

    #region Round-trip Tests

    [Test]
    public void RoundTrip_WithApplicationStartupDir_PreservesAllData ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var originalData = new PersistenceData
        {
            FileName = _logFileName,
            CurrentLine = 999,
            FirstDisplayedLine = 500,
            FollowTail = true,
            FilterVisible = true,
            FilterAdvanced = false,
            FilterPosition = 300,
            TabName = "Test Tab",
            MultiFile = true,
            MultiFilePattern = "test*.log",
            MultiFileMaxDays = 7,
            MultiFileMaxFileAgeDays = 5,
            LineCount = 1000,
            CellSelectMode = true
        };

        // Act
        _ = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, originalData, preferences, _sessionDirectory);
        var loadedData = Core.Classes.Persister.Persister.LoadPersistenceData(_logFileName, preferences, _sessionDirectory);

        // Assert
        Assert.That(loadedData, Is.Not.Null, "Should load data");
        Assert.That(loadedData.CurrentLine, Is.EqualTo(originalData.CurrentLine), "CurrentLine should match");
        Assert.That(loadedData.FirstDisplayedLine, Is.EqualTo(originalData.FirstDisplayedLine), "FirstDisplayedLine should match");
        Assert.That(loadedData.FollowTail, Is.EqualTo(originalData.FollowTail), "FollowTail should match");
        Assert.That(loadedData.FilterVisible, Is.EqualTo(originalData.FilterVisible), "FilterVisible should match");
        Assert.That(loadedData.FilterAdvanced, Is.EqualTo(originalData.FilterAdvanced), "FilterAdvanced should match");
        Assert.That(loadedData.FilterPosition, Is.EqualTo(originalData.FilterPosition), "FilterPosition should match");
        Assert.That(loadedData.TabName, Is.EqualTo(originalData.TabName), "TabName should match");
        Assert.That(loadedData.MultiFile, Is.EqualTo(originalData.MultiFile), "MultiFile should match");
        Assert.That(loadedData.MultiFilePattern, Is.EqualTo(originalData.MultiFilePattern), "MultiFilePattern should match");
        Assert.That(loadedData.MultiFileMaxDays, Is.EqualTo(originalData.MultiFileMaxDays), "MultiFileMaxDays should match");
        Assert.That(loadedData.MultiFileMaxFileAgeDays, Is.EqualTo(originalData.MultiFileMaxFileAgeDays),
            "MultiFileMaxFileAgeDays should match");
        Assert.That(loadedData.LineCount, Is.EqualTo(originalData.LineCount), "LineCount should match");
        Assert.That(loadedData.CellSelectMode, Is.EqualTo(originalData.CellSelectMode), "CellSelectMode should match");
    }

    [Test]
    public void RoundTrip_SwitchingBetweenLocations_WorksCorrectly ()
    {
        // Arrange
        var appDirPreferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var sameDirPreferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.SameDir
        };

        var testData = new PersistenceData
        {
            FileName = _logFileName,
            CurrentLine = 42
        };

        // Act - Save to ApplicationStartupDir
        var appDirFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, testData, appDirPreferences, _sessionDirectory);

        // Save to SameDir
        var sameDirFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, testData, sameDirPreferences, _sessionDirectory);

        // Load from ApplicationStartupDir
        var loadedFromAppDir = Core.Classes.Persister.Persister.LoadPersistenceData(_logFileName, appDirPreferences, _sessionDirectory);

        // Load from SameDir
        var loadedFromSameDir = Core.Classes.Persister.Persister.LoadPersistenceData(_logFileName, sameDirPreferences, _sessionDirectory);

        // Assert
        Assert.That(appDirFileName, Is.Not.EqualTo(sameDirFileName), "Files should be in different locations");
        Assert.That(loadedFromAppDir, Is.Not.Null, "Should load from app dir");
        Assert.That(loadedFromSameDir, Is.Not.Null, "Should load from same dir");
        Assert.That(loadedFromAppDir.CurrentLine, Is.EqualTo(42), "App dir data should match");
        Assert.That(loadedFromSameDir.CurrentLine, Is.EqualTo(42), "Same dir data should match");
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public void SavePersistenceData_WithApplicationStartupDir_EmptyPath_ThrowsArgumentException ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName
        };

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, string.Empty));
    }

    [Test]
    public void SavePersistenceData_WithApplicationStartupDir_WhitespacePath_ThrowsArgumentException ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName
        };

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, "   "));
    }

    [Test]
    public void SavePersistenceData_WithApplicationStartupDir_SpecialCharactersInPath_HandlesCorrectly ()
    {
        // Arrange
        var specialPath = Path.Join(_testDirectory, "Special Path With Spaces & Symbols");
        _ = Directory.CreateDirectory(specialPath);

        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName,
            CurrentLine = 100
        };

        // Act
        var savedFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, specialPath);
        var loadedData = Core.Classes.Persister.Persister.LoadPersistenceData(_logFileName, preferences, specialPath);

        // Assert
        Assert.That(File.Exists(savedFileName), Is.True, "Should handle special characters in path");
        Assert.That(loadedData, Is.Not.Null, "Should load from path with special characters");
        Assert.That(loadedData.CurrentLine, Is.EqualTo(100), "Data should be preserved");
    }

    [Test]
    public void SavePersistenceData_WithApplicationStartupDir_UnicodeCharactersInPath_HandlesCorrectly ()
    {
        // Arrange
        var unicodePath = Path.Join(_testDirectory, "Пути_日本語_Ελληνικά");
        _ = Directory.CreateDirectory(unicodePath);

        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName,
            CurrentLine = 200
        };

        // Act
        var savedFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, unicodePath);
        var loadedData = Core.Classes.Persister.Persister.LoadPersistenceData(_logFileName, preferences, unicodePath);

        // Assert
        Assert.That(File.Exists(savedFileName), Is.True, "Should handle unicode characters in path");
        Assert.That(loadedData, Is.Not.Null, "Should load from path with unicode characters");
        Assert.That(loadedData.CurrentLine, Is.EqualTo(200), "Data should be preserved");
    }

    [Test]
    public void SavePersistenceData_WithApplicationStartupDir_LongPath_HandlesCorrectly ()
    {
        // Arrange - Create a deep directory structure
        var longPath = _applicationStartupPath;
        for (int i = 0; i < 10; i++)
        {
            longPath = Path.Join(longPath, $"SubDirectory{i}");
        }

        _ = Directory.CreateDirectory(longPath);

        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName,
            CurrentLine = 300
        };

        // Act
        var savedFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, longPath);
        var loadedData = Core.Classes.Persister.Persister.LoadPersistenceData(_logFileName, preferences, longPath);

        // Assert
        Assert.That(File.Exists(savedFileName), Is.True, "Should handle long paths");
        Assert.That(loadedData, Is.Not.Null, "Should load from long path");
        Assert.That(loadedData.CurrentLine, Is.EqualTo(300), "Data should be preserved");
    }

    #endregion

    #region Backward Compatibility Tests

    [Test]
    public void LoadPersistenceData_WithoutApplicationStartupPath_StillWorksForOtherLocations ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.SameDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName,
            CurrentLine = 50
        };

        // Act - Save with SameDir (should not use applicationStartupPath)
        _ = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, _applicationStartupPath);
        var loadedData = Core.Classes.Persister.Persister.LoadPersistenceData(_logFileName, preferences, _applicationStartupPath);

        // Assert
        Assert.That(loadedData, Is.Not.Null, "Should work for non-ApplicationStartupDir locations");
        Assert.That(loadedData.CurrentLine, Is.EqualTo(50), "Data should be preserved");
    }

    #endregion

    #region Concurrency Tests

    [Test]
    public void SavePersistenceData_ConcurrentSaves_ToApplicationStartupDir_HandlesCorrectly ()
    {
        // Arrange
        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var tasks = new List<Task<string>>();
        var logFiles = new List<string>();

        // Create multiple log files
        for (int i = 0; i < 5; i++)
        {
            var logFile = Path.Join(_testDirectory, $"concurrent_test_{i}.log");
            File.WriteAllText(logFile, $"Test log {i}");
            logFiles.Add(logFile);
        }

        // Act - Save persistence data concurrently
        foreach (var logFile in logFiles)
        {
            var index = logFiles.IndexOf(logFile);
            tasks.Add(Task.Run(() =>
            {
                var data = new PersistenceData
                {
                    FileName = logFile,
                    CurrentLine = index * 100
                };
                return Core.Classes.Persister.Persister.SavePersistenceData(logFile, data, preferences, _sessionDirectory);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - Verify all files were saved correctly
        for (int i = 0; i < logFiles.Count; i++)
        {
            var loadedData = Core.Classes.Persister.Persister.LoadPersistenceData(logFiles[i], preferences, _sessionDirectory);
            Assert.That(loadedData, Is.Not.Null, $"Should load data for file {i}");
            Assert.That(loadedData.CurrentLine, Is.EqualTo(i * 100), $"Data should match for file {i}");
        }
    }

    #endregion

    #region Directory Creation Tests

    [Test]
    public void SavePersistenceData_SessionBaseDirectoryNotExists_CreatesDirectory ()
    {
        // Arrange
        var nonExistentSessionDir = Path.Join(_testDirectory, "NonExistentSessionDir");
        // Don't create the directory - let Persister create it

        var preferences = new Preferences
        {
            SaveLocation = SessionSaveLocation.ApplicationStartupDir
        };

        var persistenceData = new PersistenceData
        {
            FileName = _logFileName,
            CurrentLine = 77
        };

        // Act
        var savedFileName = Core.Classes.Persister.Persister.SavePersistenceData(_logFileName, persistenceData, preferences, nonExistentSessionDir);

        // Assert
        Assert.That(Directory.Exists(nonExistentSessionDir), Is.True, "Should create session base directory");
        Assert.That(File.Exists(savedFileName), Is.True, "Should create persistence file");
    }

    #endregion
}
