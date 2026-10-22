using System.Globalization;

using LogExpert.Core.Classes.Persister;

namespace LogExpert.Persister.Tests;

/// <summary>
/// Unit tests for the Project File Validator implementation (Issue #514).
/// Tests validation logic for missing files in project/session loading.
/// Includes tests for SessionFileResolver, PersisterHelpers, and SessionPersister updates.
/// </summary>
[TestFixture]
public class SessionFileValidatorTests
{
    private string _testDirectory;
    private string _projectFile;
    private List<string> _testLogFiles;

    [SetUp]
    public void Setup ()
    {
        // Create temporary test directory
        _testDirectory = Path.Join(Path.GetTempPath(), "LogExpertTests", "SessionValidator", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(_testDirectory);

        // Initialize test log files list
        _testLogFiles = [];

        // Create a project file path (will be created in individual tests)
        _projectFile = Path.Join(_testDirectory, "test_project.lxj");

        // Initialize PluginRegistry for tests
        _ = PluginRegistry.PluginRegistry.Create(_testDirectory, 1000);
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

    #region Helper Methods

    /// <summary>
    /// Creates test log files with specified names.
    /// </summary>
    private void CreateTestLogFiles (params string[] fileNames)
    {
        foreach (var fileName in fileNames)
        {
            var filePath = Path.Join(_testDirectory, fileName);
            File.WriteAllText(filePath, $"Test log content for {fileName}");
            _testLogFiles.Add(filePath);
        }
    }

    /// <summary>
    /// Creates a test project file with specified log file references.
    /// </summary>
    private void CreateTestProjectFile (params string[] logFileNames)
    {
        var sessionData = new SessionData
        {
            FileNames = [.. logFileNames.Select(name => Path.Join(_testDirectory, name))],
            TabLayoutXml = "<layout><dockpanel>test</dockpanel></layout>"
        };

        SessionPersister.SaveSessionData(_projectFile, sessionData);
    }

    /// <summary>
    /// Creates a .lxp persistence file pointing to a log file.
    /// </summary>
    private void CreatePersistenceFile (string lxpFileName, string logFileName)
    {
        var lxpPath = Path.Join(_testDirectory, lxpFileName);
        var logPath = Path.Join(_testDirectory, logFileName);

        var persistenceData = new PersistenceData
        {
            FileName = logPath
        };

        // Use the correct namespace: LogExpert.Core.Classes.Persister.Persister
        _ = Core.Classes.Persister.Persister.SavePersistenceDataWithFixedName(lxpPath, persistenceData);
    }

    /// <summary>
    /// Deletes specified log files to simulate missing files.
    /// </summary>
    private void DeleteLogFiles (params string[] fileNames)
    {
        foreach (var filePath in fileNames.Select(fileName => Path.Join(_testDirectory, fileName)))
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    #endregion

    #region PersisterHelpers Tests

    [Test]
    public void PersisterHelpers_FindFilenameForSettings_RegularLogFile_ReturnsUnchanged ()
    {
        // Arrange
        CreateTestLogFiles("test.log");
        var logPath = Path.Join(_testDirectory, "test.log");

        // Act
        var result = PersisterHelpers.FindFilenameForSettings(logPath, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.EqualTo(logPath), "Regular log file should be returned unchanged");
    }

    [Test]
    public void PersisterHelpers_FindFilenameForSettings_LxpFile_ReturnsLogPath ()
    {
        // Arrange
        CreateTestLogFiles("actual.log");
        CreatePersistenceFile("settings.lxp", "actual.log");
        var lxpPath = Path.Join(_testDirectory, "settings.lxp");
        var expectedLogPath = Path.Join(_testDirectory, "actual.log");

        // Act
        var result = PersisterHelpers.FindFilenameForSettings(lxpPath, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.EqualTo(expectedLogPath), "Should resolve .lxp to actual log file");
    }

    [Test]
    public void PersisterHelpers_FindFilenameForSettings_NullFileName_ThrowsArgumentNullException ()
    {
        // Act & Assert - ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        _ = Assert.Throws<ArgumentNullException>(() =>
            PersisterHelpers.FindFilenameForSettings((string)null, PluginRegistry.PluginRegistry.Instance));
    }

    [Test]
    public void PersisterHelpers_FindFilenameForSettings_EmptyFileName_ThrowsArgumentException ()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() =>
            PersisterHelpers.FindFilenameForSettings(string.Empty, PluginRegistry.PluginRegistry.Instance));
    }

    [Test]
    public void PersisterHelpers_FindFilenameForSettings_ListOfFiles_ResolvesAll ()
    {
        // Arrange
        CreateTestLogFiles("log1.log", "log2.log", "log3.log");
        var fileList = new List<string>
        {
            Path.Join(_testDirectory, "log1.log"),
            Path.Join(_testDirectory, "log2.log"),
            Path.Join(_testDirectory, "log3.log")
        };

        // Act - call the List overload explicitly
        var result = PersisterHelpers.FindFilenameForSettings(fileList.AsReadOnly(), PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Has.Count.EqualTo(3), "Should resolve all files");
        Assert.That(result[0], Does.EndWith("log1.log"));
        Assert.That(result[1], Does.EndWith("log2.log"));
        Assert.That(result[2], Does.EndWith("log3.log"));
    }

    [Test]
    public void PersisterHelpers_FindFilenameForSettings_MixedLxpAndLog_ResolvesBoth ()
    {
        // Arrange
        CreateTestLogFiles("direct.log", "referenced.log");
        CreatePersistenceFile("indirect.lxp", "referenced.log");

        var fileList = new List<string>
        {
            Path.Join(_testDirectory, "direct.log"),
            Path.Join(_testDirectory, "indirect.lxp")
        };

        // Act - call the List overload explicitly
        var result = PersisterHelpers.FindFilenameForSettings(fileList.AsReadOnly(), PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Does.EndWith("direct.log"), "Direct log should be unchanged");
        Assert.That(result[1], Does.EndWith("referenced.log"), ".lxp should resolve to referenced log");
    }

    [Test]
    public void PersisterHelpers_FindFilenameForSettings_CorruptedLxp_ReturnsLxpPath ()
    {
        // Arrange
        var lxpPath = Path.Join(_testDirectory, "corrupted.lxp");
        File.WriteAllText(lxpPath, "This is not valid XML");

        // Act
        var result = PersisterHelpers.FindFilenameForSettings(lxpPath, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.EqualTo(lxpPath), "Corrupted .lxp should return original path");
    }

    #endregion

    #region SessionFileResolver Tests

    [Test]
    public void ProjectFileResolver_ResolveProjectFiles_AllLogFiles_ReturnsUnchanged ()
    {
        // Arrange
        CreateTestLogFiles("file1.log", "file2.log");
        var sessionData = new SessionData
        {
            FileNames =
            [
                Path.Join(_testDirectory, "file1.log"),
                Path.Join(_testDirectory, "file2.log")
            ]
        };

        // Act
        var result = SessionFileResolver.ResolveSessionFiles(sessionData, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].LogFile, Does.EndWith("file1.log"));
        Assert.That(result[0].OriginalFile, Does.EndWith("file1.log"));
        Assert.That(result[1].LogFile, Does.EndWith("file2.log"));
        Assert.That(result[1].OriginalFile, Does.EndWith("file2.log"));
    }

    [Test]
    public void ProjectFileResolver_ResolveProjectFiles_WithLxpFiles_ResolvesToLogs ()
    {
        // Arrange
        CreateTestLogFiles("actual1.log", "actual2.log");
        CreatePersistenceFile("settings1.lxp", "actual1.log");
        CreatePersistenceFile("settings2.lxp", "actual2.log");

        var sessionData = new SessionData
        {
            FileNames =
            [
                Path.Join(_testDirectory, "settings1.lxp"),
                Path.Join(_testDirectory, "settings2.lxp")
            ]
        };

        // Act
        var result = SessionFileResolver.ResolveSessionFiles(sessionData, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].LogFile, Does.EndWith("actual1.log"), "Should resolve to actual log");
        Assert.That(result[0].OriginalFile, Does.EndWith("settings1.lxp"), "Should preserve original .lxp");
        Assert.That(result[1].LogFile, Does.EndWith("actual2.log"));
        Assert.That(result[1].OriginalFile, Does.EndWith("settings2.lxp"));
    }

    [Test]
    public void ProjectFileResolver_ResolveProjectFiles_MixedFiles_ResolvesProperly ()
    {
        // Arrange
        CreateTestLogFiles("direct.log", "referenced.log");
        CreatePersistenceFile("indirect.lxp", "referenced.log");

        var sessionData = new SessionData
        {
            FileNames =
            [
                Path.Join(_testDirectory, "direct.log"),
                Path.Join(_testDirectory, "indirect.lxp")
            ]
        };

        // Act
        var result = SessionFileResolver.ResolveSessionFiles(sessionData, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].LogFile, Does.EndWith("direct.log"));
        Assert.That(result[0].OriginalFile, Does.EndWith("direct.log"));
        Assert.That(result[1].LogFile, Does.EndWith("referenced.log"));
        Assert.That(result[1].OriginalFile, Does.EndWith("indirect.lxp"));
    }

    [Test]
    public void ProjectFileResolver_ResolveProjectFiles_NullProjectData_ThrowsArgumentNullException ()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            SessionFileResolver.ResolveSessionFiles(null, PluginRegistry.PluginRegistry.Instance));
    }

    [Test]
    public void ProjectFileResolver_ResolveProjectFiles_EmptyProject_ReturnsEmptyList ()
    {
        // Arrange
        var sessionData = new SessionData
        {
            FileNames = []
        };

        // Act
        var result = SessionFileResolver.ResolveSessionFiles(sessionData, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.Empty, "Empty project should return empty list");
    }

    [Test]
    public void ProjectFileResolver_ResolveProjectFiles_ReturnsReadOnlyCollection ()
    {
        // Arrange
        CreateTestLogFiles("test.log");
        var sessionData = new SessionData
        {
            FileNames = [Path.Join(_testDirectory, "test.log")]
        };

        // Act
        var result = SessionFileResolver.ResolveSessionFiles(sessionData, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<(string, string)>>());
    }

    #endregion

    #region SessionLoadResult Tests

    [Test]
    public void ProjectLoadResult_HasValidFiles_AllFilesValid_ReturnsTrue ()
    {
        // Arrange
        var sessionData = new SessionData();
        var validationResult = new SessionValidationResult();
        validationResult.ValidFiles.Add("file1.log");
        validationResult.ValidFiles.Add("file2.log");

        var result = new SessionLoadResult
        {
            SessionData = sessionData,
            ValidationResult = validationResult
        };

        // Act
        var hasValidFiles = result.HasValidFiles;

        // Assert
        Assert.That(hasValidFiles, Is.True, "Should have valid files");
    }

    [Test]
    public void ProjectLoadResult_HasValidFiles_NoValidFiles_ReturnsFalse ()
    {
        // Arrange
        var sessionData = new SessionData();
        var validationResult = new SessionValidationResult();
        validationResult.MissingFiles.Add("file1.log");
        validationResult.MissingFiles.Add("file2.log");

        var result = new SessionLoadResult
        {
            SessionData = sessionData,
            ValidationResult = validationResult
        };

        // Act
        var hasValidFiles = result.HasValidFiles;

        // Assert
        Assert.That(hasValidFiles, Is.False, "Should not have valid files");
    }

    [Test]
    public void ProjectLoadResult_HasValidFiles_SomeValidFiles_ReturnsTrue ()
    {
        // Arrange
        var sessionData = new SessionData();
        var validationResult = new SessionValidationResult();
        validationResult.ValidFiles.Add("file1.log");
        validationResult.MissingFiles.Add("file2.log");
        validationResult.MissingFiles.Add("file3.log");

        var result = new SessionLoadResult
        {
            SessionData = sessionData,
            ValidationResult = validationResult
        };

        // Act
        var hasValidFiles = result.HasValidFiles;

        // Assert
        Assert.That(hasValidFiles, Is.True, "Should have at least one valid file");
    }

    [Test]
    public void ProjectLoadResult_RequiresUserIntervention_AllFilesValid_ReturnsFalse ()
    {
        // Arrange
        var sessionData = new SessionData();
        var validationResult = new SessionValidationResult();
        validationResult.ValidFiles.Add("file1.log");
        validationResult.ValidFiles.Add("file2.log");

        var result = new SessionLoadResult
        {
            SessionData = sessionData,
            ValidationResult = validationResult
        };

        // Act
        var requiresIntervention = result.RequiresUserIntervention;

        // Assert
        Assert.That(requiresIntervention, Is.False, "Should not require user intervention");
    }

    [Test]
    public void ProjectLoadResult_RequiresUserIntervention_SomeMissingFiles_ReturnsTrue ()
    {
        // Arrange
        var sessionData = new SessionData();
        var validationResult = new SessionValidationResult();
        validationResult.ValidFiles.Add("file1.log");
        validationResult.MissingFiles.Add("file2.log");

        var result = new SessionLoadResult
        {
            SessionData = sessionData,
            ValidationResult = validationResult
        };

        // Act
        var requiresIntervention = result.RequiresUserIntervention;

        // Assert
        Assert.That(requiresIntervention, Is.True, "Should require user intervention");
    }

    [Test]
    public void ProjectLoadResult_LogToOriginalFileMapping_StoresMapping ()
    {
        // Arrange
        var mapping = new Dictionary<string, string>
        {
            ["C:\\logs\\actual.log"] = "C:\\settings\\config.lxp",
            ["C:\\logs\\direct.log"] = "C:\\logs\\direct.log"
        };

        var result = new SessionLoadResult
        {
            LogToOriginalFileMapping = mapping
        };

        // Act & Assert
        Assert.That(result.LogToOriginalFileMapping, Has.Count.EqualTo(2));
        Assert.That(result.LogToOriginalFileMapping["C:\\logs\\actual.log"], Is.EqualTo("C:\\settings\\config.lxp"));
        Assert.That(result.LogToOriginalFileMapping["C:\\logs\\direct.log"], Is.EqualTo("C:\\logs\\direct.log"));
    }

    #endregion

    #region SessionValidationResult Tests

    [Test]
    public void ProjectValidationResult_HasMissingFiles_WithMissingFiles_ReturnsTrue ()
    {
        // Arrange
        var result = new SessionValidationResult();
        result.ValidFiles.Add("file1.log");
        result.MissingFiles.Add("file2.log");

        // Act
        var hasMissing = result.HasMissingFiles;

        // Assert
        Assert.That(hasMissing, Is.True, "Should have missing files");
    }

    [Test]
    public void ProjectValidationResult_HasMissingFiles_WithoutMissingFiles_ReturnsFalse ()
    {
        // Arrange
        var result = new SessionValidationResult();
        result.ValidFiles.Add("file1.log");
        result.ValidFiles.Add("file2.log");

        // Act
        var hasMissing = result.HasMissingFiles;

        // Assert
        Assert.That(hasMissing, Is.False, "Should not have missing files");
    }

    #endregion

    #region SessionPersister.LoadSessionData - All Files Valid

    [Test]
    public void LoadProjectData_AllFilesExist_ReturnsSuccessResult ()
    {
        // Arrange
        CreateTestLogFiles("log1.log", "log2.log", "log3.log");
        CreateTestProjectFile("log1.log", "log2.log", "log3.log");

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.SessionData, Is.Not.Null, "SessionData should not be null");
        Assert.That(result.ValidationResult, Is.Not.Null, "ValidationResult should not be null");
        Assert.That(result.HasValidFiles, Is.True, "Should have valid files");
        Assert.That(result.RequiresUserIntervention, Is.False, "Should not require intervention");
        Assert.That(result.ValidationResult.ValidFiles.Count, Is.EqualTo(3), "Should have 3 valid files");
        Assert.That(result.ValidationResult.MissingFiles.Count, Is.EqualTo(0), "Should have 0 missing files");
    }

    [Test]
    public void LoadProjectData_AllFilesExist_ProjectDataContainsCorrectFiles ()
    {
        // Arrange
        CreateTestLogFiles("alpha.log", "beta.log", "gamma.log");
        CreateTestProjectFile("alpha.log", "beta.log", "gamma.log");

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        var fileNames = result.SessionData.FileNames.Select(Path.GetFileName).ToList();
        Assert.That(fileNames, Does.Contain("alpha.log"), "Should contain alpha.log");
        Assert.That(fileNames, Does.Contain("beta.log"), "Should contain beta.log");
        Assert.That(fileNames, Does.Contain("gamma.log"), "Should contain gamma.log");
    }

    [Test]
    public void LoadProjectData_AllFilesExist_PreservesTabLayoutXml ()
    {
        // Arrange
        CreateTestLogFiles("test.log");
        CreateTestProjectFile("test.log");

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result.SessionData.TabLayoutXml, Is.Not.Null.And.Not.Empty, "TabLayoutXml should be preserved");
        Assert.That(result.SessionData.TabLayoutXml, Does.Contain("<layout>"), "Should contain layout XML");
    }

    [Test]
    public void LoadProjectData_WithLxpFiles_ResolvesToActualLogs ()
    {
        // Arrange
        CreateTestLogFiles("actual1.log", "actual2.log");
        CreatePersistenceFile("settings1.lxp", "actual1.log");
        CreatePersistenceFile("settings2.lxp", "actual2.log");

        // Create project referencing .lxp files
        var sessionData = new SessionData
        {
            FileNames =
            [
                Path.Join(_testDirectory, "settings1.lxp"),
                Path.Join(_testDirectory, "settings2.lxp")
            ]
        };
        SessionPersister.SaveSessionData(_projectFile, sessionData);

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ValidationResult.ValidFiles.Count, Is.EqualTo(2), "Should validate actual log files");
        var fileNames = result.SessionData.FileNames.Select(Path.GetFileName).ToList();
        Assert.That(fileNames, Does.Contain("actual1.log"), "Should contain resolved log file");
        Assert.That(fileNames, Does.Contain("actual2.log"), "Should contain resolved log file");
    }

    [Test]
    public void LoadProjectData_WithLxpFiles_PreservesMapping ()
    {
        // Arrange
        CreateTestLogFiles("actual.log");
        CreatePersistenceFile("settings.lxp", "actual.log");

        var sessionData = new SessionData
        {
            FileNames = [Path.Join(_testDirectory, "settings.lxp")]
        };
        SessionPersister.SaveSessionData(_projectFile, sessionData);

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result.LogToOriginalFileMapping, Is.Not.Null);
        Assert.That(result.LogToOriginalFileMapping, Has.Count.EqualTo(1));
        var actualLogPath = Path.Join(_testDirectory, "actual.log");
        var lxpPath = Path.Join(_testDirectory, "settings.lxp");
        Assert.That(result.LogToOriginalFileMapping[actualLogPath], Is.EqualTo(lxpPath));
    }

    #endregion

    #region SessionPersister.LoadSessionData - Some Files Missing

    [Test]
    public void LoadProjectData_SomeFilesMissing_ReturnsPartialSuccessResult ()
    {
        // Arrange
        CreateTestLogFiles("exists1.log", "exists2.log", "missing.log");
        DeleteLogFiles("missing.log"); // Delete to simulate missing
        CreateTestProjectFile("exists1.log", "exists2.log", "missing.log");

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.HasValidFiles, Is.True, "Should have some valid files");
        Assert.That(result.RequiresUserIntervention, Is.True, "Should require user intervention");
        Assert.That(result.ValidationResult.ValidFiles.Count, Is.EqualTo(2), "Should have 2 valid files");
        Assert.That(result.ValidationResult.MissingFiles.Count, Is.EqualTo(1), "Should have 1 missing file");
    }

    [Test]
    public void LoadProjectData_SomeFilesMissing_ValidFilesListIsCorrect ()
    {
        // Arrange
        CreateTestLogFiles("valid1.log", "valid2.log", "invalid.log");
        DeleteLogFiles("invalid.log");
        CreateTestProjectFile("valid1.log", "valid2.log", "invalid.log");

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        var validFileNames = result.ValidationResult.ValidFiles.Select(Path.GetFileName).ToList();
        Assert.That(validFileNames, Does.Contain("valid1.log"), "Should contain valid1.log");
        Assert.That(validFileNames, Does.Contain("valid2.log"), "Should contain valid2.log");
        Assert.That(validFileNames, Does.Not.Contain("invalid.log"), "Should not contain invalid.log");
    }

    [Test]
    public void LoadProjectData_SomeFilesMissing_MissingFilesListIsCorrect ()
    {
        // Arrange
        CreateTestLogFiles("present.log", "absent1.log", "absent2.log");
        DeleteLogFiles("absent1.log", "absent2.log");
        CreateTestProjectFile("present.log", "absent1.log", "absent2.log");

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        var missingFileNames = result.ValidationResult.MissingFiles.Select(Path.GetFileName).ToList();
        Assert.That(missingFileNames, Does.Contain("absent1.log"), "Should contain absent1.log");
        Assert.That(missingFileNames, Does.Contain("absent2.log"), "Should contain absent2.log");
        Assert.That(missingFileNames, Does.Not.Contain("present.log"), "Should not contain present.log");
    }

    [Test]
    public void LoadProjectData_MajorityFilesMissing_StillReturnsValidFiles ()
    {
        // Arrange
        CreateTestLogFiles("only_valid.log", "missing1.log", "missing2.log", "missing3.log", "missing4.log");
        DeleteLogFiles("missing1.log", "missing2.log", "missing3.log", "missing4.log");
        CreateTestProjectFile("only_valid.log", "missing1.log", "missing2.log", "missing3.log", "missing4.log");

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result.HasValidFiles, Is.True, "Should have at least one valid file");
        Assert.That(result.ValidationResult.ValidFiles.Count, Is.EqualTo(1), "Should have 1 valid file");
        Assert.That(result.ValidationResult.MissingFiles.Count, Is.EqualTo(4), "Should have 4 missing files");
    }

    [Test]
    public void LoadProjectData_LxpReferencingMissingLog_ReportsLogAsMissing ()
    {
        // Arrange
        CreateTestLogFiles("missing.log");
        CreatePersistenceFile("settings.lxp", "missing.log");
        DeleteLogFiles("missing.log");

        var sessionData = new SessionData
        {
            FileNames = [Path.Join(_testDirectory, "settings.lxp")]
        };
        SessionPersister.SaveSessionData(_projectFile, sessionData);

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result.ValidationResult.MissingFiles.Count, Is.EqualTo(1), "Should report missing log file");
        Assert.That(result.ValidationResult.MissingFiles[0], Does.EndWith("missing.log"));
    }

    #endregion

    #region SessionPersister.LoadSessionData - All Files Missing

    [Test]
    public void LoadProjectData_AllFilesMissing_ReturnsFailureResult ()
    {
        // Arrange
        CreateTestLogFiles("missing1.log", "missing2.log");
        DeleteLogFiles("missing1.log", "missing2.log");
        CreateTestProjectFile("missing1.log", "missing2.log");

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.HasValidFiles, Is.False, "Should not have valid files");
        Assert.That(result.ValidationResult.ValidFiles.Count, Is.EqualTo(0), "Should have 0 valid files");
        Assert.That(result.ValidationResult.MissingFiles.Count, Is.EqualTo(2), "Should have 2 missing files");
    }

    [Test]
    public void LoadProjectData_AllFilesMissing_MissingFilesListComplete ()
    {
        // Arrange
        CreateTestLogFiles("gone1.log", "gone2.log", "gone3.log");
        DeleteLogFiles("gone1.log", "gone2.log", "gone3.log");
        CreateTestProjectFile("gone1.log", "gone2.log", "gone3.log");

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result.ValidationResult.MissingFiles.Count, Is.EqualTo(3), "Should have 3 missing files");
        var missingFileNames = result.ValidationResult.MissingFiles.Select(Path.GetFileName).ToList();
        Assert.That(missingFileNames, Does.Contain("gone1.log"));
        Assert.That(missingFileNames, Does.Contain("gone2.log"));
        Assert.That(missingFileNames, Does.Contain("gone3.log"));
    }

    #endregion

    #region SessionPersister.LoadSessionData - Empty/Invalid Projects

    [Test]
    public void LoadProjectData_EmptyProject_ReturnsEmptyResult ()
    {
        // Arrange
        CreateTestProjectFile(); // Empty project with no files

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.SessionData.FileNames, Is.Empty, "FileNames should be empty");
        Assert.That(result.ValidationResult.ValidFiles, Is.Empty, "ValidFiles should be empty");
        Assert.That(result.ValidationResult.MissingFiles, Is.Empty, "MissingFiles should be empty");
    }

    [Test]
    public void LoadProjectData_NonExistentProjectFile_ReturnsNull ()
    {
        // Arrange
        var nonExistentProject = Path.Join(_testDirectory, "does_not_exist.lxj");

        // Act
        var result = SessionPersister.LoadSessionData(nonExistentProject, PluginRegistry.PluginRegistry.Instance);

        // Assert
        // FIXED: Now returns empty result instead of null when file doesn't exist
        Assert.That(result, Is.Not.Null, "Result should not be null even for non-existent file");
        Assert.That(result.SessionData, Is.Not.Null, "SessionData should be initialized");
    }

    [Test]
    public void LoadProjectData_CorruptedProjectFile_ThrowsJsonReaderException ()
    {
        // Arrange
        var corruptedProject = Path.Join(_testDirectory, "corrupted.lxj");
        File.WriteAllText(corruptedProject, "This is not valid XML or JSON");

        // Act & Assert - JsonReaderException is not caught, so it propagates
        _ = Assert.Throws<Newtonsoft.Json.JsonReaderException>(() =>
            SessionPersister.LoadSessionData(corruptedProject, PluginRegistry.PluginRegistry.Instance));
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [Test]
    public void LoadProjectData_DuplicateFileReferences_HandlesCorrectly ()
    {
        // Arrange
        CreateTestLogFiles("duplicate.log");
        var sessionData = new SessionData
        {
            FileNames =
            [
                Path.Join(_testDirectory, "duplicate.log"),
                Path.Join(_testDirectory, "duplicate.log"),
                Path.Join(_testDirectory, "duplicate.log")
            ]
        };
        SessionPersister.SaveSessionData(_projectFile, sessionData);

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.HasValidFiles, Is.True, "Should have valid files");
        // Validation should handle duplicates gracefully
    }

    [Test]
    public void LoadProjectData_FilesWithSpecialCharacters_ValidatesCorrectly ()
    {
        // Arrange
        CreateTestLogFiles("file with spaces.log", "file-with-dashes.log", "file_with_underscores.log");
        CreateTestProjectFile("file with spaces.log", "file-with-dashes.log", "file_with_underscores.log");

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.ValidationResult.ValidFiles.Count, Is.EqualTo(3), "Should validate all files with special characters");
    }

    [Test]
    public void LoadProjectData_VeryLargeProject_ValidatesEfficiently ()
    {
        // Arrange
        const int fileCount = 100;
        var fileNames = new List<string>();

        for (int i = 0; i < fileCount; i++)
        {
            var fileName = $"log_{i:D4}.log";
            fileNames.Add(fileName);
        }

        CreateTestLogFiles([.. fileNames]);
        CreateTestProjectFile([.. fileNames]);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);
        stopwatch.Stop();

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.ValidationResult.ValidFiles.Count, Is.EqualTo(fileCount), $"Should validate all {fileCount} files");
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(60000), "Should complete validation in reasonable time");
    }

    #endregion

    #region Performance and Stress Tests

    [Test]
    public void LoadProjectData_ManyMissingFiles_HandledCorrectly ()
    {
        // This exercises the many-missing-files path (each missing file triggers an alternative-path
        // search). We assert correctness only and deliberately avoid a wall-clock budget: the work is
        // filesystem-bound, so timings are dominated by antivirus/EDR scanning, disk-cache state and
        // machine load, none of which are code defects. The per-missing-file cost is linear, so there is
        // no super-linear blow-up for a timer to catch here; efficiency is guaranteed structurally by
        // enumerating the session directory once per session rather than once per missing file.
        const int totalFiles = 50;
        var fileNames = new List<string>();

        // Create only first 10 files, rest will be missing
        for (int i = 0; i < 10; i++)
        {
            var fileName = $"exists_{i}.log";
            fileNames.Add(fileName);
            CreateTestLogFiles(fileName);
        }

        for (int i = 10; i < totalFiles; i++)
        {
            fileNames.Add($"missing_{i}.log");
        }

        CreateTestProjectFile([.. fileNames]);

        // Act
        var result = SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result, Is.Not.Null, "Result should not be null");
        Assert.That(result.ValidationResult.ValidFiles.Count, Is.EqualTo(10), "Should have 10 valid files");
        Assert.That(result.ValidationResult.MissingFiles.Count, Is.EqualTo(40), "Should have 40 missing files");
    }

    #endregion

    #region Null and Exception Handling

    [Test]
    public void LoadProjectData_NullProjectFile_ThrowsArgumentNullException ()
    {
        // Act & Assert - File.ReadAllText throws ArgumentNullException for null path
        _ = Assert.Throws<ArgumentNullException>(() =>
            SessionPersister.LoadSessionData(null, PluginRegistry.PluginRegistry.Instance));
    }

    [Test]
    public void LoadProjectData_EmptyProjectFile_ThrowsArgumentException ()
    {
        // Act & Assert - File.ReadAllText throws ArgumentException for empty string
        _ = Assert.Throws<ArgumentException>(() =>
            SessionPersister.LoadSessionData(string.Empty, PluginRegistry.PluginRegistry.Instance));
    }

    [Test]
    public void LoadProjectData_NullPluginRegistry_ThrowsArgumentNullException ()
    {
        // Arrange
        CreateTestProjectFile("test.log");

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            SessionPersister.LoadSessionData(_projectFile, null));
    }

    #endregion

    #region Backward Compatibility

    [Test]
    public void LoadProjectData_LegacyProjectFormat_ThrowsJsonReaderException ()
    {
        // Arrange
        CreateTestLogFiles("legacy.log");

        // Create a legacy format project file (XML)
        var legacyXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<LogExpertProject>
  <Files>
    <member fileName=""{0}"" />
  </Files>
</LogExpertProject>";

        var legacyContent = string.Format(CultureInfo.InvariantCulture, legacyXml, Path.Join(_testDirectory, "legacy.log"));
        File.WriteAllText(_projectFile, legacyContent);

        // Act & Assert - JsonReaderException is not caught, so XML fallback doesn't trigger
        _ = Assert.Throws<Newtonsoft.Json.JsonReaderException>(() =>
            SessionPersister.LoadSessionData(_projectFile, PluginRegistry.PluginRegistry.Instance));
    }

    #endregion
}