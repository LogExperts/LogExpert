using System.Runtime.Versioning;

using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Interface;
using LogExpert.UI.Services.FileOperationService;
using LogExpert.UI.Services.SessionHandlerService;

using Moq;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Services;

[TestFixture]
[Apartment(ApartmentState.STA)]
[SupportedOSPlatform("windows")]
internal class SessionHandlerTests : IDisposable
{
    private Mock<IPluginRegistry> _pluginRegistryMock = null!;
    private List<FileTabRequest> _addFileTabCalls = null!;
    private List<string> _callOrder = null!;
    private int _closeAllTabsCallCount;
    private Settings _settings;
    private Mock<IConfigManager> _configManagerMock;
    private LogWindow _stubLogWindow = null!;
    private SessionHandler _sut = null!;

    private bool _disposed;

    [SetUp]
    public void SetUp ()
    {
        // Ensure the static PluginRegistry singleton is initialized.
        // LogWindow's constructor accesses PluginRegistry.Instance.RegisteredColumnizers[0] directly.
        _ = PluginRegistry.PluginRegistry.Create(Path.GetTempPath(), 250);

        _pluginRegistryMock = new Mock<IPluginRegistry>();
        _configManagerMock = new Mock<IConfigManager>();
        _addFileTabCalls = [];
        _callOrder = [];
        _closeAllTabsCallCount = 0;

        _settings = new Settings();
        _ = _configManagerMock.Setup(cm => cm.Settings).Returns(_settings);

        _ = _pluginRegistryMock.Setup(pr => pr.RegisteredColumnizers).Returns([]);

        // Create a minimal stub LogWindow for the callback.
        // LogWindow requires an ILogWindowCoordinator
        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        _stubLogWindow = new LogWindow(
            coordinatorMock.Object,
            "stub.log",
            isTempFile: false,
            forcePersistenceLoading: false,
            configManager: _configManagerMock.Object,
            pluginRegistry: PluginRegistry.PluginRegistry.Instance);

        _sut = new SessionHandler(
            _pluginRegistryMock.Object,
            request =>
            {
                _addFileTabCalls.Add(request);
                _callOrder.Add($"addtab:{request.FileName}");
                return _stubLogWindow;
            },
            () =>
            {
                _closeAllTabsCallCount++;
                _callOrder.Add("close");
            });
    }

    [TearDown]
    public void TearDown ()
    {
        _stubLogWindow?.Dispose();
    }

    #region LoadSession — Phase 1 tests

    [Test]
    public void LoadProject_ValidProjectFile_ReturnsSuccess ()
    {
        // Arrange: create a real log file so validation passes
        var logFile = CreateTempLogFile();
        var tempFile = CreateTempProjectFile([logFile], layoutXml: null);

        try
        {
            // Act
            var outcome = _sut.LoadSession(tempFile);

            // Assert
            Assert.That(outcome.Status, Is.EqualTo(SessionLoadOutcome.LoadStatus.Success));
            Assert.That(outcome.SessionData, Is.Not.Null);
            Assert.That(outcome.SessionData!.FileNames, Has.Count.EqualTo(1));
            Assert.That(outcome.ErrorMessage, Is.Null);
            Assert.That(outcome.ValidationResult, Is.Null);
        }
        finally
        {
            File.Delete(logFile);
            File.Delete(tempFile);
        }
    }

    [Test]
    public void LoadProject_ValidProjectWithLayoutXml_HasLayoutDataIsTrue ()
    {
        // Arrange: create a real log file so validation passes
        var logFile = CreateTempLogFile();
        var tempFile = CreateTempProjectFile([logFile], layoutXml: "<DockPanel><Content/></DockPanel>");

        try
        {
            // Act
            var outcome = _sut.LoadSession(tempFile);

            // Assert
            Assert.That(outcome.Status, Is.EqualTo(SessionLoadOutcome.LoadStatus.Success));
            Assert.That(outcome.HasLayoutData, Is.True);
            Assert.That(outcome.LayoutXml, Is.EqualTo("<DockPanel><Content/></DockPanel>"));
        }
        finally
        {
            File.Delete(tempFile);
            File.Delete(logFile);
        }
    }

    [Test]
    public void LoadProject_ValidProjectWithoutLayoutXml_HasLayoutDataIsFalse ()
    {
        // Arrange
        var tempFile = CreateTempProjectFile(["C:\\logs\\app.log"], layoutXml: null);

        try
        {
            // Act
            var outcome = _sut.LoadSession(tempFile);

            // Assert
            Assert.That(outcome.HasLayoutData, Is.False);
            Assert.That(outcome.LayoutXml, Is.Null);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void LoadProject_ProjectWithMissingFiles_ReturnsNeedsIntervention ()
    {
        // Arrange: use file paths that do not exist on disk
        var tempFile = CreateTempProjectFile(
            ["C:\\nonexistent\\path\\missing.log", "C:\\also\\missing.log"],
            layoutXml: null);

        try
        {
            // Act
            var outcome = _sut.LoadSession(tempFile);

            // Assert
            Assert.That(outcome.Status, Is.EqualTo(SessionLoadOutcome.LoadStatus.NeedsIntervention));
            Assert.That(outcome.SessionData, Is.Not.Null);
            Assert.That(outcome.ValidationResult, Is.Not.Null);
            Assert.That(outcome.ValidationResult!.HasMissingFiles, Is.True);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void LoadProject_CorruptFile_ReturnsError ()
    {
        // Arrange: write garbage content
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "{{{{not json not xml}}}}");

        try
        {
            // Act
            var outcome = _sut.LoadSession(tempFile);

            // Assert
            Assert.That(outcome.Status, Is.EqualTo(SessionLoadOutcome.LoadStatus.Error));
            Assert.That(outcome.ErrorMessage, Is.Not.Null.And.Not.Empty);
            Assert.That(outcome.SessionData, Is.Null);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void LoadProject_NonexistentFile_ReturnsError ()
    {
        // Arrange
        var fakePath = Path.Join(Path.GetTempPath(), Guid.NewGuid() + ".lxj");

        // Act
        var outcome = _sut.LoadSession(fakePath);

        // Assert
        Assert.That(outcome.Status, Is.EqualTo(SessionLoadOutcome.LoadStatus.Error));
        Assert.That(outcome.ErrorMessage, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void LoadProject_EmptyFileList_ReturnsEmptyProject ()
    {
        // Arrange: valid JSON but with zero files
        var tempFile = CreateTempProjectFile([], layoutXml: null);

        try
        {
            // Act
            var outcome = _sut.LoadSession(tempFile);

            // Assert
            Assert.That(outcome.Status, Is.EqualTo(SessionLoadOutcome.LoadStatus.EmptySession));
            Assert.That(outcome.ErrorMessage, Is.Not.Null.And.Not.Empty);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void LoadProject_DoesNotInvokeAddFileTabCallback ()
    {
        // Arrange
        var tempFile = CreateTempProjectFile(["C:\\logs\\app.log"], layoutXml: null);

        try
        {
            // Act
            _ = _sut.LoadSession(tempFile);

            // Assert — phase 1 must never open tabs
            Assert.That(_addFileTabCalls, Is.Empty);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region ContinueLoad tests

    [Test]
    public void ContinueLoad_SuccessNoLayout_CallsAddFileTabWithDoNotAddToDockPanelFalse ()
    {
        // Arrange
        var outcome = CreateSuccessOutcome(["C:\\logs\\app.log"], layoutXml: null);

        // Act
        var result = _sut.ContinueLoad(outcome, resolution: null, restoreLayout: true);

        // Assert
        Assert.That(result.OpenedTabs, Is.True);
        Assert.That(_addFileTabCalls, Has.Count.EqualTo(1));
        Assert.That(_addFileTabCalls[0].DoNotAddToDockPanel, Is.False);
        Assert.That(_addFileTabCalls[0].FileName, Is.EqualTo("C:\\logs\\app.log"));
        Assert.That(_addFileTabCalls[0].ForcePersistenceLoading, Is.True);
    }

    [Test]
    public void ContinueLoad_SuccessWithLayout_CallsAddFileTabWithDoNotAddToDockPanelTrue ()
    {
        // Arrange
        var outcome = CreateSuccessOutcome(["C:\\logs\\app.log"], layoutXml: "<DockPanel/>");

        // Act
        var result = _sut.ContinueLoad(outcome, resolution: null, restoreLayout: true);

        // Assert
        Assert.That(result.OpenedTabs, Is.True);
        Assert.That(_addFileTabCalls, Has.Count.EqualTo(1));
        Assert.That(_addFileTabCalls[0].DoNotAddToDockPanel, Is.True);
    }

    [Test]
    public void ContinueLoad_SuccessWithLayoutButRestoreLayoutFalse_DoNotAddToDockPanelIsFalse ()
    {
        // Arrange
        var outcome = CreateSuccessOutcome(["C:\\logs\\app.log"], layoutXml: "<DockPanel/>");

        // Act
        _ = _sut.ContinueLoad(outcome, resolution: null, restoreLayout: false);

        // Assert
        Assert.That(_addFileTabCalls[0].DoNotAddToDockPanel, Is.False);
    }

    [Test]
    public void ContinueLoad_MultipleFiles_CallsAddFileTabForEachFile ()
    {
        // Arrange
        var outcome = CreateSuccessOutcome(["C:\\logs\\a.log", "C:\\logs\\b.log", "C:\\logs\\c.log"], layoutXml: null);

        // Act
        var result = _sut.ContinueLoad(outcome, resolution: null, restoreLayout: true);

        // Assert
        Assert.That(result.OpenedTabs, Is.True);
        Assert.That(_addFileTabCalls, Has.Count.EqualTo(3));
        Assert.That(_addFileTabCalls[0].FileName, Is.EqualTo("C:\\logs\\a.log"));
        Assert.That(_addFileTabCalls[1].FileName, Is.EqualTo("C:\\logs\\b.log"));
        Assert.That(_addFileTabCalls[2].FileName, Is.EqualTo("C:\\logs\\c.log"));
    }

    [Test]
    public void ContinueLoad_WithAlternatives_ReplacesFilePathsBeforeOpening ()
    {
        // Arrange
        var outcome = CreateOutcome(
            SessionLoadOutcome.LoadStatus.NeedsIntervention,
            ["C:\\old\\missing.log", "C:\\logs\\existing.log"],
            layoutXml: null);

        var resolution = new MissingFilesResolution
        {
            SelectedAlternatives = new Dictionary<string, string>
            {
                ["C:\\old\\missing.log"] = "D:\\new\\found.log"
            }
        };

        // Act
        _ = _sut.ContinueLoad(outcome, resolution, restoreLayout: true);

        // Assert
        Assert.That(_addFileTabCalls, Has.Count.EqualTo(2));
        Assert.That(_addFileTabCalls[0].FileName, Is.EqualTo("D:\\new\\found.log"));
        Assert.That(_addFileTabCalls[1].FileName, Is.EqualTo("C:\\logs\\existing.log"));
    }

    [Test]
    public void ContinueLoad_DuplicateFileWithAlternative_ReplacesBothOccurrences ()
    {
        // Arrange
        var outcome = CreateOutcome(
            SessionLoadOutcome.LoadStatus.NeedsIntervention,
            ["C:\\missing.log", "C:\\missing.log"],
            layoutXml: null);

        var resolution = new MissingFilesResolution
        {
            SelectedAlternatives = new Dictionary<string, string>
            {
                ["C:\\missing.log"] = "D:\\found.log"
            }
        };

        // Act
        _ = _sut.ContinueLoad(outcome, resolution, restoreLayout: true);

        // Assert
        Assert.That(_addFileTabCalls, Has.Count.EqualTo(2));
        Assert.That(_addFileTabCalls[0].FileName, Is.EqualTo("D:\\found.log"));
        Assert.That(_addFileTabCalls[1].FileName, Is.EqualTo("D:\\found.log"));
    }

    [Test]
    public void ContinueLoad_CloseAllTabs_ClosesExistingTabsBeforeOpeningNewWindows ()
    {
        var outcome = CreateOutcome(
            SessionLoadOutcome.LoadStatus.NeedsIntervention,
            ["C:\\logs\\app.log"],
            layoutXml: "<DockPanel/>");

        var resolution = new MissingFilesResolution { CloseAllTabs = true };

        // Act
        var result = _sut.ContinueLoad(outcome, resolution, restoreLayout: true);

        // Assert
        Assert.That(_closeAllTabsCallCount, Is.EqualTo(1), "close must be invoked exactly once");
        Assert.That(_callOrder, Is.EqualTo(["close", "addtab:C:\\logs\\app.log"]),
                    "existing tabs must be closed before the new windows are opened");
        Assert.That(result.OpenedTabs, Is.True);
        Assert.That(result.OpenInNewWindowFiles, Is.Null);
    }

    [Test]
    public void ContinueLoad_OpenInNewWindow_DoesNotOpenTabs_ReturnsFileNames ()
    {
        // Arrange
        var outcome = CreateOutcome(
            SessionLoadOutcome.LoadStatus.NeedsIntervention,
            ["C:\\logs\\app.log"],
            layoutXml: null);

        var resolution = new MissingFilesResolution { OpenInNewWindow = true };

        // Act
        var result = _sut.ContinueLoad(outcome, resolution, restoreLayout: true);

        // Assert
        Assert.That(result.OpenedTabs, Is.False);
        Assert.That(result.OpenInNewWindowFiles, Is.Not.Null);
        Assert.That(result.OpenInNewWindowFiles, Has.Length.EqualTo(1));
        Assert.That(_addFileTabCalls, Is.Empty, "OpenInNewWindow must not open tabs");
        Assert.That(_closeAllTabsCallCount, Is.EqualTo(0), "OpenInNewWindow must not close existing tabs");
    }

    [Test]
    public void ContinueLoad_OpenInNewWindowWithAlternatives_AppliesAlternativesThenResolves ()
    {
        // Arrange
        var outcome = CreateOutcome(
            SessionLoadOutcome.LoadStatus.NeedsIntervention,
            ["C:\\old\\missing.log", "C:\\logs\\existing.log"],
            layoutXml: null);

        var resolution = new MissingFilesResolution
        {
            OpenInNewWindow = true,
            SelectedAlternatives = new Dictionary<string, string>
            {
                ["C:\\old\\missing.log"] = "D:\\new\\found.log"
            }
        };

        // Act
        var result = _sut.ContinueLoad(outcome, resolution, restoreLayout: true);

        // Assert
        Assert.That(result.OpenInNewWindowFiles, Is.Not.Null);
        Assert.That(result.OpenInNewWindowFiles, Has.Length.EqualTo(2));
        // First file should be the alternative, second should be unchanged
        // Exact values depend on PersisterHelpers.FindFilenameForSettings resolution
        Assert.That(result.OpenInNewWindowFiles![0], Does.Contain("found.log"));
    }

    [Test]
    public void ContinueLoad_UpdateSessionFileTrue_SavesAmendedProject ()
    {
        // Arrange: create a real temp .lxj so we can verify it was updated
        var tempFile = CreateTempProjectFile(["C:\\old\\missing.log"], layoutXml: "<Layout/>");

        try
        {
            var outcome = new SessionLoadOutcome
            {
                Status = SessionLoadOutcome.LoadStatus.NeedsIntervention,
                SessionData = new SessionData
                {
                    FileNames = ["C:\\old\\missing.log"],
                    TabLayoutXml = "<Layout/>",
                    SessionFilePath = tempFile
                },
                LayoutXml = "<Layout/>"
            };

            var resolution = new MissingFilesResolution
            {
                UpdateSessionFile = true,
                SelectedAlternatives = new Dictionary<string, string>
                {
                    ["C:\\old\\missing.log"] = "D:\\new\\found.log"
                }
            };

            // Act
            _ = _sut.ContinueLoad(outcome, resolution, restoreLayout: false);

            // Assert: reload the file and check the path was updated
            var reloaded = SessionPersister.LoadSessionData(tempFile, _pluginRegistryMock.Object);
            Assert.That(reloaded?.SessionData?.FileNames, Has.Some.Contains("found.log"));
            // Layout XML must be preserved
            Assert.That(reloaded?.SessionData?.TabLayoutXml, Is.EqualTo("<Layout/>"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void ContinueLoad_UpdateSessionFileFalse_DoesNotSave ()
    {
        // Arrange
        var tempFile = CreateTempProjectFile(["C:\\old\\missing.log"], layoutXml: null);

        try
        {
            var originalContent = File.ReadAllText(tempFile);

            var outcome = new SessionLoadOutcome
            {
                Status = SessionLoadOutcome.LoadStatus.NeedsIntervention,
                SessionData = new SessionData
                {
                    FileNames = ["C:\\old\\missing.log"],
                    SessionFilePath = tempFile
                }
            };

            var resolution = new MissingFilesResolution
            {
                UpdateSessionFile = false,
                SelectedAlternatives = new Dictionary<string, string>
                {
                    ["C:\\old\\missing.log"] = "D:\\new\\found.log"
                }
            };

            // Act
            _ = _sut.ContinueLoad(outcome, resolution, restoreLayout: false);

            // Assert: file content unchanged
            Assert.That(File.ReadAllText(tempFile), Is.EqualTo(originalContent));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void ContinueLoad_NullResolution_DoesNotCloseExistingTabs ()
    {
        // Arrange
        var outcome = CreateSuccessOutcome(["C:\\logs\\app.log"], layoutXml: null);

        // Act
        var result = _sut.ContinueLoad(outcome, resolution: null, restoreLayout: true);

        // Assert
        Assert.That(_closeAllTabsCallCount, Is.EqualTo(0), "existing tabs must not be closed when no resolution is given");
        Assert.That(result.OpenInNewWindowFiles, Is.Null);
    }

    [Test]
    public void ContinueLoad_CloseAllTabsFalse_DoesNotCloseExistingTabs ()
    {
        // Arrange
        var outcome = CreateOutcome(
            SessionLoadOutcome.LoadStatus.NeedsIntervention,
            ["C:\\logs\\app.log"],
            layoutXml: "<DockPanel/>");

        var resolution = new MissingFilesResolution { CloseAllTabs = false };

        // Act
        _ = _sut.ContinueLoad(outcome, resolution, restoreLayout: true);

        // Assert
        Assert.That(_closeAllTabsCallCount, Is.EqualTo(0));
        Assert.That(_addFileTabCalls, Has.Count.EqualTo(1));
    }

    #endregion

    #region SaveSession tests

    [Test]
    public void SaveProject_ValidData_ReturnsTrueAndNullError ()
    {
        // Arrange
        var tempFile = Path.Join(Path.GetTempPath(), Guid.NewGuid() + ".lxj");
        var sessionData = new SessionData
        {
            FileNames = ["C:\\logs\\app.log"],
            TabLayoutXml = "<Layout/>"
        };

        try
        {
            // Act
            var success = _sut.SaveSession(tempFile, sessionData, out var errorMessage);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(errorMessage, Is.Null);
            Assert.That(File.Exists(tempFile), Is.True);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Test]
    public void SaveProject_NullPath_ReturnsFalseWithErrorMessage ()
    {
        // Note: SessionPersister.SaveSessionData swallows IOException/UnauthorizedAccessException/
        // JsonSerializationException. Only uncaught exceptions (e.g. ArgumentNullException from
        // File.WriteAllText(null,...)) propagate to SessionHandler.SaveSession's catch block.
        var sessionData = new SessionData
        {
            FileNames = ["C:\\logs\\app.log"]
        };

        // Act
        var success = _sut.SaveSession(null!, sessionData, out var errorMessage);

        // Assert
        Assert.That(success, Is.False);
        Assert.That(errorMessage, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region Helpers

    private static SessionLoadOutcome CreateSuccessOutcome (List<string> fileNames, string? layoutXml)
    {
        return CreateOutcome(SessionLoadOutcome.LoadStatus.Success, fileNames, layoutXml);
    }

    private static SessionLoadOutcome CreateOutcome (SessionLoadOutcome.LoadStatus status, List<string> fileNames, string? layoutXml)
    {
        return new SessionLoadOutcome
        {
            Status = status,
            SessionData = new SessionData
            {
                FileNames = fileNames,
                TabLayoutXml = layoutXml!
            },
            LayoutXml = layoutXml
        };
    }

    private static string CreateTempLogFile ()
    {
        var tempPathFile = Path.GetTempFileName();
        var logFile = Path.ChangeExtension(tempPathFile, ".log")!;
        File.WriteAllText(logFile, "test log line");
        File.Delete(tempPathFile);

        return logFile;
    }

    /// <summary>
    /// Creates a temporary .lxj project file with the given file names and optional layout XML. Returns the path to the
    /// temp file.
    /// </summary>
    private static string CreateTempProjectFile (List<string> fileNames, string? layoutXml)
    {
        var sessionData = new SessionData
        {
            FileNames = fileNames,
            TabLayoutXml = layoutXml!
        };

        var tempFile = Path.Join(Path.GetTempPath(), Guid.NewGuid() + ".lxj");
        SessionPersister.SaveSessionData(tempFile, sessionData);
        return tempFile;
    }

    #endregion

    public void Dispose ()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _stubLogWindow?.Dispose();
        }

        _disposed = true;
    }
}