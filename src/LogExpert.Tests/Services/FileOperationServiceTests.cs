using System.Runtime.Versioning;
using System.Text;

using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.Log;
using LogExpert.Core.Classes.Log.ProgressReporters;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Interface;
using LogExpert.UI.Services.FileOperationService;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Services;

[TestFixture]
[Apartment(ApartmentState.STA)]
[SupportedOSPlatform("windows")]
internal class FileOperationServiceTests : IDisposable
{
    private Mock<IConfigManager> _configManagerMock;
    private Mock<ITabController> _tabControllerMock;
    private Mock<ILedIndicatorService> _ledServiceMock;
    private Mock<IPluginRegistry> _pluginRegistryMock;
    private Settings _settings;

    private List<(FileTabRequest Request, EncodingOptions Encoding)> _factoryCalls;
    private LogWindow _stubLogWindow;
    private Func<FileTabRequest, EncodingOptions, LogWindow> _factory;

    private string? _clipboardText;
    private List<(string FileName, bool IsSingleFileProject)> _projectCallbackCalls;

    private FileOperationService _sut;

    private bool _disposed;

    [OneTimeSetUp]
    public void OneTimeSetUp ()
    {
        var dir = Path.GetDirectoryName(typeof(FileOperationServiceTests).Assembly.Location)!;
        _ = PluginRegistry.PluginRegistry.Create(dir, 500);
    }

    [SetUp]
    public void Setup ()
    {
        _configManagerMock = new Mock<IConfigManager>();
        _tabControllerMock = new Mock<ITabController>();
        _ledServiceMock = new Mock<ILedIndicatorService>();
        _pluginRegistryMock = new Mock<IPluginRegistry>();

        _settings = new Settings();
        _ = _configManagerMock.Setup(cm => cm.Settings).Returns(_settings);
        _ = _configManagerMock.Setup(cm => cm.ClearLastOpenFilesList()).Callback(() => _settings.LastOpenFilesList.Clear());

        // Process-wide state: every test starts as if no window has saved its files yet
        FileOperationService.LastOpenFilesListReplaced = false;
        _ = _pluginRegistryMock.Setup(pr => pr.RegisteredColumnizers).Returns([]);

        _factoryCalls = [];
        _projectCallbackCalls = [];
        _clipboardText = null;

        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        _stubLogWindow = new LogWindow(coordinatorMock.Object, "stub.log", false, false, _configManagerMock.Object, PluginRegistry.PluginRegistry.Instance);

        _factory = (request, encoding) =>
        {
            _factoryCalls.Add((request, encoding));
            return _stubLogWindow;
        };

        // No existing windows by default
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns((LogWindow)null!);

        _sut = new FileOperationService(
            _configManagerMock.Object,
            _tabControllerMock.Object,
            _ledServiceMock.Object,
            _pluginRegistryMock.Object,
            _factory,
            () => _clipboardText,
            (fileName, isSingle) => _projectCallbackCalls.Add((fileName, isSingle)));
    }

    [TearDown]
    public void TearDown ()
    {
        _stubLogWindow?.Dispose();
    }

    [Test]
    public void AddFileTab_NewFile_InvokesFactory_ReturnsLogWindow ()
    {
        // Arrange
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns((LogWindow)null!);

        var request = new FileTabRequest { FileName = "test.log" };

        // Act
        var result = _sut.AddFileTab(request);

        // Assert
        Assert.That(result, Is.SameAs(_stubLogWindow));
        Assert.That(_factoryCalls, Has.Count.EqualTo(1));
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo("test.log"));
    }

    [Test]
    public void AddFileTab_DuplicateFile_ActivatesExisting_DoesNotCallFactory ()
    {
        // Arrange
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns(_stubLogWindow);

        var request = new FileTabRequest { FileName = "test.log" };

        // Act
        var result = _sut.AddFileTab(request);

        // Assert
        Assert.That(result, Is.SameAs(_stubLogWindow));
        Assert.That(_factoryCalls, Is.Empty, "Factory should not be called for duplicate files");
        _tabControllerMock.Verify(tc => tc.ActivateWindow(_stubLogWindow), Times.Once);
    }

    [Test]
    public void AddFileTab_NonTempFile_AddsToHistory ()
    {
        // Arrange
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns((LogWindow)null!);

        var request = new FileTabRequest { FileName = "test.log", IsTempFile = false };

        // Act
        _ = _sut.AddFileTab(request);

        // Assert
        _configManagerMock.Verify(cm => cm.AddToFileHistory("test.log"), Times.Once);
    }

    [Test]
    public void AddFileTab_TempFile_DoesNotAddToHistory ()
    {
        // Arrange
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns((LogWindow)null!);

        var request = new FileTabRequest { FileName = "temp.log", IsTempFile = true };

        // Act
        _ = _sut.AddFileTab(request);

        // Assert
        _configManagerMock.Verify(cm => cm.AddToFileHistory(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void AddFileTab_LxpSuffix_SetsForcedPersistenceFileName ()
    {
        // Arrange
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns((LogWindow)null!);

        var request = new FileTabRequest { FileName = "session.lxp" };

        // Act
        var result = _sut.AddFileTab(request);

        // Assert
        Assert.That(result.ForcedPersistenceFileName, Is.EqualTo("session.lxp"));
    }

    [Test]
    public void AddFileTab_RaisesFileOpenedEvent ()
    {
        // Arrange
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns((LogWindow)null!);

        FileOpenedEventArgs? receivedArgs = null;
        _sut.FileOpened += (_, args) => receivedArgs = args;

        var request = new FileTabRequest { FileName = "test.log" };

        // Act
        _ = _sut.AddFileTab(request);

        // Assert
        Assert.That(receivedArgs, Is.Not.Null);
        Assert.That(receivedArgs!.LogWindow, Is.SameAs(_stubLogWindow));
        Assert.That(receivedArgs.Request, Is.SameAs(request));
        Assert.That(receivedArgs.ResolvedFileName, Is.Not.Null.And.Not.Empty);
        Assert.That(receivedArgs.EncodingOptions, Is.Not.Null);
    }

    [Test]
    public void AddFileTab_TempFile_SetsUnicodeEncoding ()
    {
        // Arrange
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns((LogWindow)null!);

        var request = new FileTabRequest { FileName = "temp.log", IsTempFile = true };

        // Act
        _ = _sut.AddFileTab(request);

        // Assert
        Assert.That(_factoryCalls, Has.Count.EqualTo(1));
        Assert.That(_factoryCalls[0].Encoding.Encoding, Is.InstanceOf<UnicodeEncoding>());
    }

    [Test]
    public void AddFileTab_DuplicateNonTemp_StillAddsToHistory ()
    {
        // Arrange — duplicate detected
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns(_stubLogWindow);

        var request = new FileTabRequest { FileName = "test.log", IsTempFile = false };

        // Act
        _ = _sut.AddFileTab(request);

        // Assert — history is still updated even for duplicates
        _configManagerMock.Verify(cm => cm.AddToFileHistory("test.log"), Times.Once);
    }

    [Test]
    public void AddToFileHistory_CallsConfigManager_RaisesEvent ()
    {
        // Arrange
        var eventRaised = false;
        _sut.FileHistoryChanged += (_, _) => eventRaised = true;

        // Act
        _sut.AddToFileHistory("test.log");

        // Assert
        _configManagerMock.Verify(cm => cm.AddToFileHistory("test.log"), Times.Once);
        Assert.That(eventRaised, Is.True);
    }

    [Test]
    public void FindWindowForFile_DelegatesToTabController ()
    {
        // Arrange
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName("test.log"))
            .Returns(_stubLogWindow);

        // Act
        var result = _sut.FindWindowForFile("test.log");

        // Assert
        Assert.That(result, Is.SameAs(_stubLogWindow));
        _tabControllerMock.Verify(tc => tc.FindWindowByFileName("test.log"), Times.Once);
    }

    [Test]
    public void FindWindowForFile_NoMatch_ReturnsNull ()
    {
        // Arrange
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns((LogWindow)null!);

        // Act
        var result = _sut.FindWindowForFile("nonexistent.log");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void AddFileTab_ValidDefaultEncoding_SetsDefaultEncoding ()
    {
        // Arrange
        _settings.Preferences.DefaultEncoding = "utf-8";
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns((LogWindow)null!);

        var request = new FileTabRequest { FileName = "test.log" };

        // Act
        _ = _sut.AddFileTab(request);

        // Assert
        Assert.That(_factoryCalls[0].Encoding.DefaultEncoding, Is.Not.Null);
        Assert.That(_factoryCalls[0].Encoding.DefaultEncoding.WebName, Is.EqualTo("utf-8"));
    }

    /// <summary>
    /// The headline bug: .NET does not ship the legacy Windows code pages, and registration of
    /// <c>CodePagesEncodingProvider</c> used to happen only in the Preferences dialog constructor. A user
    /// who picked Windows-1252 and restarted without reopening Preferences got their choice silently
    /// discarded here, because this method swallows the resolve failure and leaves DefaultEncoding null.
    /// </summary>
    [Test]
    [TestCase("windows-1250", 1250)]
    [TestCase("windows-1252", 1252)]
    [TestCase("gb2312", 936)]
    public void AddFileTab_LegacyCodePageDefaultEncoding_ResolvesWithoutOpeningPreferences (string encodingName, int expectedCodePage)
    {
        // Arrange
        _settings.Preferences.DefaultEncoding = encodingName;
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns((LogWindow)null!);

        var request = new FileTabRequest { FileName = "test.log" };

        // Act
        _ = _sut.AddFileTab(request);

        // Assert
        Assert.That(_factoryCalls[0].Encoding.DefaultEncoding, Is.Not.Null, "the configured code page was discarded");
        Assert.That(_factoryCalls[0].Encoding.DefaultEncoding.CodePage, Is.EqualTo(expectedCodePage));
    }

    [Test]
    public void AddFileTab_InvalidDefaultEncoding_DefaultEncodingRemainsNull ()
    {
        // Arrange
        _settings.Preferences.DefaultEncoding = "not-a-real-encoding-xxxxx";
        _ = _tabControllerMock
            .Setup(tc => tc.FindWindowByFileName(It.IsAny<string>()))
            .Returns((LogWindow)null!);

        var request = new FileTabRequest { FileName = "test.log" };

        // Act
        _ = _sut.AddFileTab(request);

        // Assert — invalid encoding should be caught and DefaultEncoding left null
        Assert.That(_factoryCalls[0].Encoding.DefaultEncoding, Is.Null);
    }

    [Test]
    public void AddFilterTab_CreatesTabWithTempFile ()
    {
        // Arrange
        var filterParams = new FilterParams { SearchText = "error" };
        var logWindowMock = new Mock<ILineSelectable>();
        using var pipe = new FilterPipe(filterParams, logWindowMock.Object);

        // Act
        var result = _sut.AddFilterTab(pipe, "Filter: error", null);

        // Assert
        Assert.That(result, Is.SameAs(_stubLogWindow));
        Assert.That(_factoryCalls, Has.Count.EqualTo(1));
        Assert.That(_factoryCalls[0].Request.IsTempFile, Is.True);
        Assert.That(_factoryCalls[0].Request.Title, Is.EqualTo("Filter: error"));
    }

    [Test]
    public void AddFilterTab_WithSearchText_RaisesFileOpenedWithFilterPipe ()
    {
        // Arrange
        var filterParams = new FilterParams { SearchText = "error" };
        var logWindowMock = new Mock<ILineSelectable>();
        using var pipe = new FilterPipe(filterParams, logWindowMock.Object);

        var fileOpenedArgs = new List<FileOpenedEventArgs>();
        _sut.FileOpened += (_, args) => fileOpenedArgs.Add(args);

        // Act
        _ = _sut.AddFilterTab(pipe, "Filter: error", null);

        // Assert — two FileOpened events: one from AddFileTab, one with FilterPipe
        Assert.That(fileOpenedArgs, Has.Count.EqualTo(2));

        // First event: from AddFileTab (no FilterPipe)
        Assert.That(fileOpenedArgs[0].FilterPipe, Is.Null);

        // Second event: carries the FilterPipe
        Assert.That(fileOpenedArgs[1].FilterPipe, Is.SameAs(pipe));
        Assert.That(fileOpenedArgs[1].LogWindow, Is.SameAs(_stubLogWindow));
    }

    [Test]
    public void AddFilterTab_EmptySearchText_DoesNotRaiseSecondFileOpened ()
    {
        // Arrange
        var filterParams = new FilterParams { SearchText = "" };
        var logWindowMock = new Mock<ILineSelectable>();
        using var pipe = new FilterPipe(filterParams, logWindowMock.Object);

        var fileOpenedArgs = new List<FileOpenedEventArgs>();
        _sut.FileOpened += (_, args) => fileOpenedArgs.Add(args);

        // Act
        _ = _sut.AddFilterTab(pipe, "Filter: empty", null);

        // Assert — only one event from AddFileTab, no second event for empty search
        Assert.That(fileOpenedArgs, Has.Count.EqualTo(1));
        Assert.That(fileOpenedArgs[0].FilterPipe, Is.Null);
    }

    [Test]
    public void AddFilterTab_NullSearchText_DoesNotRaiseSecondFileOpened ()
    {
        // Arrange
        var filterParams = new FilterParams { SearchText = null };
        var logWindowMock = new Mock<ILineSelectable>();
        using var pipe = new FilterPipe(filterParams, logWindowMock.Object);

        var fileOpenedArgs = new List<FileOpenedEventArgs>();
        _sut.FileOpened += (_, args) => fileOpenedArgs.Add(args);

        // Act
        _ = _sut.AddFilterTab(pipe, "Filter: null", null);

        // Assert
        Assert.That(fileOpenedArgs, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddFilterTab_SetsFileNameFromPipe ()
    {
        // Arrange
        var filterParams = new FilterParams { SearchText = "warn" };
        var logWindowMock = new Mock<ILineSelectable>();
        using var pipe = new FilterPipe(filterParams, logWindowMock.Object);

        // Act
        _ = _sut.AddFilterTab(pipe, "Filter: warn", null);

        // Assert — request FileName should come from the pipe's temp file
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo(pipe.FileName));
    }

    [Test]
    public void AddTempFileTab_CreatesTabWithIsTempFileTrue ()
    {
        // Act
        var result = _sut.AddTempFileTab("temp.log", "Temp Tab");

        // Assert
        Assert.That(result, Is.SameAs(_stubLogWindow));
        Assert.That(_factoryCalls, Has.Count.EqualTo(1));
        Assert.That(_factoryCalls[0].Request.IsTempFile, Is.True);
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo("temp.log"));
        Assert.That(_factoryCalls[0].Request.Title, Is.EqualTo("Temp Tab"));
    }

    [Test]
    public void AddTempFileTab_DoesNotAddToHistory ()
    {
        // Act
        _ = _sut.AddTempFileTab("temp.log", "Temp Tab");

        // Assert — temp files must not be added to file history
        _configManagerMock.Verify(cm => cm.AddToFileHistory(It.IsAny<string>()), Times.Never);
    }

    [TestCase(MultiFileDecision.SingleFiles)]
    [TestCase(MultiFileDecision.MultiFile)]
    [TestCase(MultiFileDecision.Cancel)]
    public void LoadDroppedFiles_Ask_UsesOnlyTheConfirmedSelection (MultiFileDecision choice)
    {
        _settings.Preferences.MultiFileOption = MultiFileOption.Ask;
        string[] selected = [@"C:\logs\nested\b.log", @"C:\logs\a.log"];
        string[]? combined = null;
        _sut.FileOpened += (_, e) => combined = e.MultiFileNames;

        _sut.LoadDroppedFiles(selected, invertLogic: true, () => choice);

        string[] expected = [@"C:\logs\a.log", @"C:\logs\nested\b.log"];
        if (choice == MultiFileDecision.SingleFiles)
        {
            Assert.That(_factoryCalls.Select(call => call.Request.FileName), Is.EqualTo(expected));
        }
        else if (choice == MultiFileDecision.MultiFile)
        {
            Assert.That(combined, Is.EqualTo(expected));
        }
        else
        {
            Assert.That(_factoryCalls, Is.Empty);
        }
    }

    [TestCase(-1, false, 2)]
    [TestCase(0, false, 1)]
    [TestCase(1, false, 1)]
    [TestCase(0, true, 1)]
    [TestCase(1, true, 1)]
    public void LoadDroppedFiles_CombinedChoice_ReadsAccessibleSelectedFiles (int unavailableIndex, bool lockFile, int expectedLines)
    {
        var directory = Path.Join(Path.GetTempPath(), "LogExpertDropRouting", Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        var first = Path.Join(directory, "a.log");
        var second = Path.Join(directory, "b.txt");
        File.WriteAllText(first, "first\n");
        File.WriteAllText(second, "second\n");
        _settings.Preferences.MultiFileOption = MultiFileOption.Ask;
        var lineCount = -1;
        FileStream? lockedFile = null;
        _sut.FileOpened += (_, e) =>
        {
            if (e.MultiFileNames == null)
            {
                return;
            }
            // Exercise the same reader endpoint used by LogWindow.LoadFilesAsMulti.
            using var reader = new LogfileReader(e.MultiFileNames,
                new EncodingOptions { Encoding = Encoding.UTF8 }, 40, 50, new MultiFileOptions(),
                ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500, NullProgressReporter.Instance, useExplicitFileList: true);
            reader.ReadFiles();
            lineCount = reader.GetLogLineMemories(0, 10).Length;
        };
        try
        {
            if (unavailableIndex >= 0)
            {
                var unavailableFile = unavailableIndex == 0 ? first : second;
                if (lockFile)
                {
                    lockedFile = new FileStream(unavailableFile, FileMode.Open, FileAccess.Read, FileShare.None);
                }
                else
                {
                    File.Delete(unavailableFile);
                }
            }
            _sut.LoadDroppedFiles([first, second], false, () => MultiFileDecision.MultiFile);
            Assert.That(lineCount, Is.EqualTo(expectedLines));
        }
        finally
        {
            lockedFile?.Dispose();
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void LoadDroppedFiles_UnavailableFile_DoesNotBlockRemainingSelection (bool accessDenied)
    {
        _settings.Preferences.MultiFileOption = MultiFileOption.SingleFiles;
        var service = new FileOperationService(_configManagerMock.Object, _tabControllerMock.Object,
            _ledServiceMock.Object, _pluginRegistryMock.Object,
            (request, encoding) => request.FileName == "a.log"
                ? throw (accessDenied ? new UnauthorizedAccessException() : new IOException())
                : _factory(request, encoding), () => null, (_, _) => { });

        service.LoadDroppedFiles(["a.log", "b.log"], false, () => MultiFileDecision.Cancel);

        Assert.That(_factoryCalls.Select(call => call.Request.FileName), Is.EqualTo(new[] { "b.log" }));
    }

    [Test]
    public void LoadDroppedFiles_CombinedSessionAndLog_DoesNotOpenUnselectedRotations ()
    {
        var directory = Path.Join(Path.GetTempPath(), "LogExpertDropRouting", Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        var log = Path.Join(directory, "b.log");
        var session = Path.Join(directory, "session.lxj");
        File.WriteAllText(log, "selected\n");
        File.WriteAllText(log + ".1", "unselected\n");
        _settings.Preferences.MultiFileOption = MultiFileOption.Ask;
        string[] observed = [];
        _sut.FileOpened += (_, e) =>
        {
            using var reader = new LogfileReader(e.MultiFileNames!,
                new EncodingOptions { Encoding = Encoding.UTF8 }, 40, 50, new MultiFileOptions(),
                ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500, NullProgressReporter.Instance, useExplicitFileList: true);
            reader.ReadFiles();
            observed = reader.GetLogLineMemories(0, 10).Select(line => line.FullLine.ToString()).ToArray();
        };
        try
        {
            _sut.LoadDroppedFiles([session, log], false, () => MultiFileDecision.MultiFile);
            Assert.That(observed, Is.EqualTo(new[] { "selected" }));
            Assert.That(_projectCallbackCalls.Select(call => call.FileName), Is.EqualTo(new[] { session }));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public void LoadDroppedFiles_CombinedSelectionAfterTruncation_KeepsOnlySelectedPaths ()
    {
        var directory = Path.Join(Path.GetTempPath(), "LogExpertDropRouting", Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        var first = Path.Join(directory, "a.log");
        var second = Path.Join(directory, "b.log");
        File.WriteAllText(first, "first\n");
        File.WriteAllText(second, "second line before truncation\n");
        File.WriteAllText(second + ".1", "unselected\n");
        _settings.Preferences.MultiFileOption = MultiFileOption.MultiFile;
        string[] observed = [];
        string[] paths = [];
        _sut.FileOpened += (_, e) =>
        {
            using var reader = new LogfileReader(e.MultiFileNames!,
                new EncodingOptions { Encoding = Encoding.UTF8 }, 40, 50, new MultiFileOptions(),
                ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500, NullProgressReporter.Instance, useExplicitFileList: true);
            reader.ReadFiles();
            File.WriteAllText(second, "short\n");
            reader.ShiftBuffers();
            paths = reader.GetLogFileInfoList().Select(info => info.FullName).ToArray();
            observed = reader.GetLogLineMemories(0, 10).Select(line => line.FullLine.ToString()).ToArray();
        };
        try
        {
            _sut.LoadDroppedFiles([first, second], false, () => MultiFileDecision.Cancel);
            Assert.Multiple(() =>
            {
                Assert.That(paths, Is.EqualTo(new[] { first, second }));
                Assert.That(observed, Is.EqualTo(new[] { "first", "short" }));
            });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public void LoadFilesWithOption_SingleFile_CallsAddFileTab ()
    {
        // Arrange
        var fileNames = new[] { "test.log" };

        // Act
        var decision = _sut.LoadFilesWithOption(fileNames, false);

        // Assert
        Assert.That(decision, Is.EqualTo(MultiFileDecision.SingleFiles));
        Assert.That(_factoryCalls, Has.Count.EqualTo(1));
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo("test.log"));
    }

    [Test]
    public void LoadFilesWithOption_SingleLxjFile_CallsProjectCallback ()
    {
        // Arrange
        var fileNames = new[] { "project.lxj" };

        // Act
        var decision = _sut.LoadFilesWithOption(fileNames, false);

        // Assert
        Assert.That(decision, Is.EqualTo(MultiFileDecision.Cancel));
        Assert.That(_projectCallbackCalls, Has.Count.EqualTo(1));
        Assert.That(_projectCallbackCalls[0].FileName, Is.EqualTo("project.lxj"));
        Assert.That(_projectCallbackCalls[0].IsSingleFileProject, Is.True);
        Assert.That(_factoryCalls, Is.Empty, "Should not call AddFileTab for .lxj files");
    }

    [Test]
    public void LoadFilesWithOption_MultiFile_SingleFilesPreference_CallsAddFileTabs ()
    {
        // Arrange
        _settings.Preferences.MultiFileOption = MultiFileOption.SingleFiles;
        var fileNames = new[] { "b.log", "a.log" };

        // Act
        var decision = _sut.LoadFilesWithOption(fileNames, false);

        // Assert
        Assert.That(decision, Is.EqualTo(MultiFileDecision.SingleFiles));
        // AddFileTabs calls AddFileTab for each — 2 factory calls
        Assert.That(_factoryCalls, Has.Count.EqualTo(2));
    }

    [Test]
    public void LoadFilesWithOption_MultiFile_MultiFilePreference_CallsAddMultiFileTab ()
    {
        // Arrange
        _settings.Preferences.MultiFileOption = MultiFileOption.MultiFile;
        var fileNames = new[] { "a.log", "b.log" };

        // Act
        var decision = _sut.LoadFilesWithOption(fileNames, false);

        // Assert
        Assert.That(decision, Is.EqualTo(MultiFileDecision.MultiFile));
        // AddMultiFileTab calls the factory once
        Assert.That(_factoryCalls, Has.Count.EqualTo(1));
    }

    [Test]
    public void LoadFilesWithOption_AskPreference_ReturnsAskUser ()
    {
        // Arrange
        _settings.Preferences.MultiFileOption = MultiFileOption.Ask;
        var fileNames = new[] { "a.log", "b.log" };

        // Act
        var decision = _sut.LoadFilesWithOption(fileNames, false);

        // Assert
        Assert.That(decision, Is.EqualTo(MultiFileDecision.AskUser));
        Assert.That(_factoryCalls, Is.Empty, "Should not create any tabs when decision is AskUser");
    }

    [Test]
    public void LoadFilesWithOption_InvertLogic_FlipsDecision ()
    {
        // Arrange — preference is SingleFiles, invert should flip to MultiFile
        _settings.Preferences.MultiFileOption = MultiFileOption.SingleFiles;
        var fileNames = new[] { "a.log", "b.log" };

        // Act
        var decision = _sut.LoadFilesWithOption(fileNames, invertLogic: true);

        // Assert — inverted: SingleFiles → MultiFile
        Assert.That(decision, Is.EqualTo(MultiFileDecision.MultiFile));
    }

    [Test]
    public void LoadFilesWithOption_InvertLogic_MultiFileToSingleFiles ()
    {
        // Arrange — preference is MultiFile, invert should flip to SingleFiles
        _settings.Preferences.MultiFileOption = MultiFileOption.MultiFile;
        var fileNames = new[] { "a.log", "b.log" };

        // Act
        var decision = _sut.LoadFilesWithOption(fileNames, invertLogic: true);

        // Assert — inverted: MultiFile → SingleFiles
        Assert.That(decision, Is.EqualTo(MultiFileDecision.SingleFiles));
    }

    [Test]
    public void LoadFilesWithOption_SortsFileNames ()
    {
        // Arrange
        _settings.Preferences.MultiFileOption = MultiFileOption.SingleFiles;
        var fileNames = new[] { "c.log", "a.log", "b.log" };

        // Act
        _ = _sut.LoadFilesWithOption(fileNames, false);

        // Assert — files should be sorted; factory calls reflect sorted order
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo("a.log"));
        Assert.That(_factoryCalls[1].Request.FileName, Is.EqualTo("b.log"));
        Assert.That(_factoryCalls[2].Request.FileName, Is.EqualTo("c.log"));
    }

    [Test]
    public void AddMultiFileTab_EmptyArray_ReturnsNull ()
    {
        // Act
        var result = _sut.AddMultiFileTab([]);

        // Assert
        Assert.That(result, Is.Null);
        Assert.That(_factoryCalls, Is.Empty);
    }

    [Test]
    public void AddMultiFileTab_CreatesWindow_AddsToHistory ()
    {
        // Arrange
        var fileNames = new[] { "first.log", "second.log" };

        // Act
        var result = _sut.AddMultiFileTab(fileNames);

        // Assert
        Assert.That(result, Is.SameAs(_stubLogWindow));
        Assert.That(_factoryCalls, Has.Count.EqualTo(1));
        // The request FileName should be the last file in the array
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo("second.log"));
        // History should contain the first file
        _configManagerMock.Verify(cm => cm.AddToFileHistory("first.log"), Times.Once);
    }

    [Test]
    public void AddMultiFileTab_RaisesFileOpened_WithMultiFileNames ()
    {
        // Arrange
        var fileNames = new[] { "a.log", "b.log" };
        FileOpenedEventArgs? receivedArgs = null;
        _sut.FileOpened += (_, args) => receivedArgs = args;

        // Act
        _ = _sut.AddMultiFileTab(fileNames);

        // Assert
        Assert.That(receivedArgs, Is.Not.Null);
        Assert.That(receivedArgs!.MultiFileNames, Is.EqualTo(fileNames));
        Assert.That(receivedArgs.EncodingOptions, Is.Not.Null);
    }

    [Test]
    public void AddFileTabs_SkipsEmptyStrings ()
    {
        // Arrange
        var fileNames = new[] { "a.log", "", "b.log", null! };

        // Act
        _sut.AddFileTabs(fileNames);

        // Assert — only non-empty names should trigger AddFileTab
        Assert.That(_factoryCalls, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddFileTabs_RoutesLxjToProjectCallback ()
    {
        // Arrange
        var fileNames = new[] { "project.lxj" };

        // Act
        _sut.AddFileTabs(fileNames);

        // Assert
        Assert.That(_projectCallbackCalls, Has.Count.EqualTo(1));
        Assert.That(_projectCallbackCalls[0].FileName, Is.EqualTo("project.lxj"));
        Assert.That(_projectCallbackCalls[0].IsSingleFileProject, Is.False);
        Assert.That(_factoryCalls, Is.Empty);
    }

    [Test]
    public void AddFileTabs_RoutesNonLxjToAddFileTab ()
    {
        // Arrange
        var fileNames = new[] { "app.log", "server.log" };

        // Act
        _sut.AddFileTabs(fileNames);

        // Assert
        Assert.That(_factoryCalls, Has.Count.EqualTo(2));
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo("app.log"));
        Assert.That(_factoryCalls[1].Request.FileName, Is.EqualTo("server.log"));
    }

    [Test]
    public void AddFileTabs_MixedLxjAndLog_RoutesSeparately ()
    {
        // Arrange
        var fileNames = new[] { "app.log", "project.lxj", "server.log" };

        // Act
        _sut.AddFileTabs(fileNames);

        // Assert
        Assert.That(_factoryCalls, Has.Count.EqualTo(2));
        Assert.That(_projectCallbackCalls, Has.Count.EqualTo(1));
    }

    [Test]
    public void PasteFromClipboard_NullClipboard_ReturnsNull ()
    {
        // Arrange
        _clipboardText = null;

        // Act
        var result = _sut.PasteFromClipboard();

        // Assert
        Assert.That(result, Is.Null);
        Assert.That(_factoryCalls, Is.Empty);
    }

    [Test]
    public void PasteFromClipboard_EmptyClipboard_ReturnsNull ()
    {
        // Arrange
        _clipboardText = "";

        // Act
        var result = _sut.PasteFromClipboard();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void PasteFromClipboard_CreatesFileAndTab ()
    {
        // Arrange
        _clipboardText = "line1\nline2\nline3";

        // Act
        var result = _sut.PasteFromClipboard();

        // Assert
        Assert.That(result, Is.SameAs(_stubLogWindow));
        Assert.That(_factoryCalls, Has.Count.EqualTo(1));
        Assert.That(_factoryCalls[0].Request.IsTempFile, Is.True);
        Assert.That(_factoryCalls[0].Request.Title, Is.EqualTo("Clipboard"));

        // Verify the temp file was actually created with content
        var tempFileName = _factoryCalls[0].Request.FileName;
        Assert.That(File.Exists(tempFileName), Is.True);

        var content = File.ReadAllText(tempFileName, Encoding.Unicode);
        Assert.That(content, Is.EqualTo("line1\nline2\nline3"));

        // Cleanup temp file
        File.Delete(tempFileName);
    }

    [Test]
    public void CanHandleDrop_FileDrop_ReturnsTrue ()
    {
        // Arrange
        var dataObjectMock = new Mock<IDataObject>();
        _ = dataObjectMock.Setup(d => d.GetDataPresent(DataFormats.FileDrop)).Returns(true);

        // Act
        var result = _sut.CanHandleDrop(dataObjectMock.Object);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanHandleDrop_NonFileDrop_ReturnsFalse ()
    {
        // Arrange
        var dataObjectMock = new Mock<IDataObject>();
        _ = dataObjectMock.Setup(d => d.GetDataPresent(DataFormats.FileDrop)).Returns(false);

        // Act
        var result = _sut.CanHandleDrop(dataObjectMock.Object);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CanHandleDrop_NullData_ReturnsFalse ()
    {
        // Act
        var result = _sut.CanHandleDrop(null!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void LoadStartupFiles_WithStartupFiles_LoadsStartupFiles_IgnoresLastOpen ()
    {
        // Arrange
        var lastOpenFiles = new List<string> { "old1.log", "old2.log" };
        var startupFiles = new[] { "startup.log" };

        // Act
        _sut.LoadStartupFiles(lastOpenFiles, startupFiles);

        // Assert — startup files are loaded (via LoadFilesWithOption → AddFileTab)
        Assert.That(_factoryCalls, Has.Count.EqualTo(1));
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo("startup.log"));
    }

    [Test]
    public void LoadStartupFiles_NoStartupFiles_OpenLastFilesEnabled_LoadsLastOpenFiles ()
    {
        // Arrange
        _settings.Preferences.OpenLastFiles = true;
        var lastOpenFiles = new List<string> { "file1.log", "file2.log" };

        // Act
        _sut.LoadStartupFiles(lastOpenFiles, null);

        // Assert
        Assert.That(_factoryCalls, Has.Count.EqualTo(2));
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo("file1.log"));
        Assert.That(_factoryCalls[1].Request.FileName, Is.EqualTo("file2.log"));
    }

    [Test]
    public void LoadStartupFiles_NoStartupFiles_OpenLastFilesDisabled_DoesNothing ()
    {
        // Arrange
        _settings.Preferences.OpenLastFiles = false;
        var lastOpenFiles = new List<string> { "file1.log" };

        // Act
        _sut.LoadStartupFiles(lastOpenFiles, null);

        // Assert
        Assert.That(_factoryCalls, Is.Empty);
        _configManagerMock.Verify(cm => cm.ClearLastOpenFilesList(), Times.Never);
    }

    [Test]
    public void LoadStartupFiles_EmptyStartupArray_TreatsAsNoStartupFiles ()
    {
        // Arrange
        _settings.Preferences.OpenLastFiles = true;
        var lastOpenFiles = new List<string> { "file1.log" };
        var startupFiles = Array.Empty<string>();

        // Act
        _sut.LoadStartupFiles(lastOpenFiles, startupFiles);

        // Assert — empty startup array should fall through to last-open-files path
        Assert.That(_factoryCalls, Has.Count.EqualTo(1));
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo("file1.log"));
    }

    [Test]
    public void LoadStartupFiles_KeepsLastOpenFilesList ()
    {
        // Arrange
        _settings.Preferences.OpenLastFiles = true;
        _settings.LastOpenFilesList.Add("file1.log");
        var lastOpenFiles = new List<string> { "file1.log" };

        // Act
        _sut.LoadStartupFiles(lastOpenFiles, null);

        // Assert — restoring tabs must not empty the persisted list: SaveLastOpenFilesList owns it,
        // so a crash mid-session still leaves the files to restore next time
        _configManagerMock.Verify(cm => cm.ClearLastOpenFilesList(), Times.Never);
        Assert.That(_settings.LastOpenFilesList, Is.EqualTo(new[] { "file1.log" }));
    }

    [Test]
    public void LoadStartupFiles_SkipsEmptyNamesInLastOpenFiles ()
    {
        // Arrange
        _settings.Preferences.OpenLastFiles = true;
        var lastOpenFiles = new List<string> { "file1.log", "", "file2.log", null! };

        // Act
        _sut.LoadStartupFiles(lastOpenFiles, null);

        // Assert — only non-empty names should trigger AddFileTab
        Assert.That(_factoryCalls, Has.Count.EqualTo(2));
    }

    [Test]
    public void SaveLastOpenFilesList_SkipsTempFiles ()
    {
        // Arrange
        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        using var tempWindow = new LogWindow(coordinatorMock.Object, "temp.log", true, false, _configManagerMock.Object, PluginRegistry.PluginRegistry.Instance);
        tempWindow.GivenFileName = "temp.log";

        _ = _tabControllerMock
            .Setup(tc => tc.GetAllWindowsFromDockPanel())
            .Returns(new List<LogWindow> { tempWindow }.AsReadOnly());

        // Act
        _sut.SaveLastOpenFilesList();

        // Assert — temp files should be skipped
        Assert.That(_settings.LastOpenFilesList, Is.Empty);
    }

    [Test]
    public void SaveLastOpenFilesList_AddsGivenFileNameToConfig ()
    {
        // Arrange
        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        using var normalWindow = new LogWindow(coordinatorMock.Object, "app.log", false, false, _configManagerMock.Object, PluginRegistry.PluginRegistry.Instance);
        normalWindow.GivenFileName = "app.log";

        _ = _tabControllerMock
            .Setup(tc => tc.GetAllWindowsFromDockPanel())
            .Returns(new List<LogWindow> { normalWindow }.AsReadOnly());

        // Act
        _sut.SaveLastOpenFilesList();

        // Assert
        Assert.That(_settings.LastOpenFilesList, Has.Count.EqualTo(1));
        Assert.That(_settings.LastOpenFilesList[0], Is.EqualTo("app.log"));
    }

    [Test]
    public void SaveLastOpenFilesList_MultipleMixed_OnlySavesNonTemp ()
    {
        // Arrange
        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        using var normalWindow = new LogWindow(coordinatorMock.Object, "app.log", false, false, _configManagerMock.Object, PluginRegistry.PluginRegistry.Instance);
        normalWindow.GivenFileName = "app.log";
        using var tempWindow = new LogWindow(coordinatorMock.Object, "filter.tmp", true, false, _configManagerMock.Object, PluginRegistry.PluginRegistry.Instance);
        tempWindow.GivenFileName = "filter.tmp";
        using var normalWindow2 = new LogWindow(coordinatorMock.Object, "server.log", false, false, _configManagerMock.Object, PluginRegistry.PluginRegistry.Instance);
        normalWindow2.GivenFileName = "server.log";

        _ = _tabControllerMock
            .Setup(tc => tc.GetAllWindowsFromDockPanel())
            .Returns(new List<LogWindow> { normalWindow, tempWindow, normalWindow2 }.AsReadOnly());

        // Act
        _sut.SaveLastOpenFilesList();

        // Assert
        Assert.That(_settings.LastOpenFilesList, Has.Count.EqualTo(2));
        Assert.That(_settings.LastOpenFilesList, Does.Contain("app.log"));
        Assert.That(_settings.LastOpenFilesList, Does.Contain("server.log"));
    }

    [Test]
    public void SaveLastOpenFilesList_ReplacesStaleEntries ()
    {
        // Arrange — a list left over from an earlier session, e.g. because LogExpert was
        // started with command-line files and so never cleared it during startup
        _settings.LastOpenFilesList.Add("old1.log");
        _settings.LastOpenFilesList.Add("old2.log");

        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        using var normalWindow = new LogWindow(coordinatorMock.Object, "current.log", false, false, _configManagerMock.Object, PluginRegistry.PluginRegistry.Instance);
        normalWindow.GivenFileName = "current.log";

        _ = _tabControllerMock
            .Setup(tc => tc.GetAllWindowsFromDockPanel())
            .Returns(new List<LogWindow> { normalWindow }.AsReadOnly());

        // Act
        _sut.SaveLastOpenFilesList();

        // Assert — the saved list holds the current tabs only, not the stale ones
        Assert.That(_settings.LastOpenFilesList, Is.EqualTo(new[] { "current.log" }));
    }

    [Test]
    public void SaveLastOpenFilesList_SecondClosingWindow_AppendsToTheFirstWindowsEntries ()
    {
        // Arrange — two LogTabWindows of one session, each with its own tab controller but
        // sharing the config manager
        _settings.LastOpenFilesList.Add("old.log");

        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        using var firstWindow = new LogWindow(coordinatorMock.Object, "first.log", false, false, _configManagerMock.Object, PluginRegistry.PluginRegistry.Instance);
        firstWindow.GivenFileName = "first.log";
        using var secondWindow = new LogWindow(coordinatorMock.Object, "second.log", false, false, _configManagerMock.Object, PluginRegistry.PluginRegistry.Instance);
        secondWindow.GivenFileName = "second.log";

        var firstService = CreateServiceForWindows(firstWindow);
        var secondService = CreateServiceForWindows(secondWindow);

        // Act — the first window to close drops the stale entry, the second one adds to it
        firstService.SaveLastOpenFilesList();
        secondService.SaveLastOpenFilesList();

        // Assert
        Assert.That(_settings.LastOpenFilesList, Is.EqualTo(new[] { "first.log", "second.log" }));
    }

    [Test]
    public void SaveLastOpenFilesList_NoFilesToSave_ClearsStaleEntries ()
    {
        // Arrange — only a temp file is open, so nothing gets saved
        _settings.LastOpenFilesList.Add("old1.log");

        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        using var tempWindow = new LogWindow(coordinatorMock.Object, "temp.log", true, false, _configManagerMock.Object, PluginRegistry.PluginRegistry.Instance);
        tempWindow.GivenFileName = "temp.log";

        _ = _tabControllerMock
            .Setup(tc => tc.GetAllWindowsFromDockPanel())
            .Returns(new List<LogWindow> { tempWindow }.AsReadOnly());

        // Act
        _sut.SaveLastOpenFilesList();

        // Assert
        Assert.That(_settings.LastOpenFilesList, Is.Empty);
    }

    [Test]
    public void AddFileTabDeferred_SetsDoNotAddToDockPanelTrue ()
    {
        // Act
        var result = _sut.AddFileTabDeferred("deferred.log", false, "Deferred", true, null);

        // Assert
        Assert.That(result, Is.SameAs(_stubLogWindow));
        Assert.That(_factoryCalls, Has.Count.EqualTo(1));
        Assert.That(_factoryCalls[0].Request.DoNotAddToDockPanel, Is.True);
        Assert.That(_factoryCalls[0].Request.ForcePersistenceLoading, Is.True);
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo("deferred.log"));
        Assert.That(_factoryCalls[0].Request.Title, Is.EqualTo("Deferred"));
    }

    [Test]
    public void AddFileTabDeferred_TempFile_SetsIsTempFileTrue ()
    {
        // Act
        _ = _sut.AddFileTabDeferred("temp.log", true, "Temp", false, null);

        // Assert
        Assert.That(_factoryCalls[0].Request.IsTempFile, Is.True);
    }

    [Test]
    public void LoadFiles_DelegatesToAddFileTabs ()
    {
        // Arrange
        var fileNames = new[] { "a.log", "b.log" };

        // Act
        _sut.LoadFiles(fileNames);

        // Assert — LoadFiles just calls AddFileTabs
        Assert.That(_factoryCalls, Has.Count.EqualTo(2));
        Assert.That(_factoryCalls[0].Request.FileName, Is.EqualTo("a.log"));
        Assert.That(_factoryCalls[1].Request.FileName, Is.EqualTo("b.log"));
    }

    /// <summary>
    /// Builds a second service on the shared config manager, standing in for another LogTabWindow
    /// of the same session with its own tab controller.
    /// </summary>
    private FileOperationService CreateServiceForWindows (params LogWindow[] windows)
    {
        var tabControllerMock = new Mock<ITabController>();
        _ = tabControllerMock
            .Setup(tc => tc.GetAllWindowsFromDockPanel())
            .Returns(windows.ToList().AsReadOnly());

        return new FileOperationService(
            _configManagerMock.Object,
            tabControllerMock.Object,
            _ledServiceMock.Object,
            _pluginRegistryMock.Object,
            _factory,
            () => _clipboardText,
            (fileName, isSingle) => _projectCallbackCalls.Add((fileName, isSingle)));
    }

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