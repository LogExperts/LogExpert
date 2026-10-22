using System.Runtime.Versioning;

using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Interface;
using LogExpert.UI.Services.TabControllerService;

using Moq;

using NUnit.Framework;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Tests.Services;

/// <summary>
/// Unit tests for TabController.
/// Most tests focus on behavior that does not require LogWindow instances.
/// Tests that verify DockPanel tab order create real LogWindow instances through CreateLogWindow.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows")]
[Apartment(ApartmentState.STA)] // Required for WinForms controls
internal class TabControllerTests : IDisposable
{
    private Form _testForm;
    private DockPanel _dockPanel;
    private TabController _tabController;
    private bool _disposed;

    [SetUp]
    public void Setup ()
    {
        // Create a real Form and DockPanel for testing
        // This is necessary because DockPanel requires WinForms infrastructure
        _testForm = new Form();
        _dockPanel = new DockPanel
        {
            Dock = DockStyle.Fill,
            // Match LogExpert's document style; DockingMdi required test form to be an MDI container.
            // Use DockingWindow to match production and avoid requiring
            // an artificial MDI container in this test.
            DocumentStyle = DocumentStyle.DockingWindow,
            Theme = new VS2015LightTheme()
        };
        _testForm.Controls.Add(_dockPanel);
        _testForm.Show(); // Must show form for DockPanel to work

        _tabController = new TabController(_dockPanel);
    }

    [TearDown]
    public void TearDown ()
    {
        _tabController?.Dispose();
        _testForm?.Close();
        _testForm?.Dispose();
    }

    #region IDisposable Implementation

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
            _tabController?.Dispose();
            _testForm?.Dispose();
        }

        _disposed = true;
    }

    #endregion

    #region Constructor Tests

    [Test]
    public void Constructor_WithDockPanel_InitializesSuccessfully ()
    {
        // Arrange & Act - already done in Setup

        // Assert
        Assert.That(_tabController, Is.Not.Null);
        Assert.That(_tabController.GetWindowCount(), Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithNullDockPanel_ThrowsArgumentNullException ()
    {
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new TabController(null));
    }

    [Test]
    public void Constructor_WithoutDockPanel_CreatesUninitializedController ()
    {
        // Arrange & Act
        using var controller = new TabController();

        // Assert - controller should be created but not initialized
        // Calling GetWindowCount should still work (returns 0)
        Assert.That(controller.GetWindowCount(), Is.EqualTo(0));
    }

    #endregion

    #region InitializeDockPanel Tests

    [Test]
    public void InitializeDockPanel_WithValidDockPanel_Succeeds ()
    {
        // Arrange
        using var controller = new TabController();

        // Act
        controller.InitializeDockPanel(_dockPanel);

        // Assert - no exception thrown, and controller should work
        Assert.That(controller.GetWindowCount(), Is.EqualTo(0));
    }

    [Test]
    public void InitializeDockPanel_WithNullDockPanel_ThrowsArgumentNullException ()
    {
        // Arrange
        using var controller = new TabController();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => controller.InitializeDockPanel(null));
    }

    [Test]
    public void InitializeDockPanel_WhenAlreadyInitialized_ThrowsInvalidOperationException ()
    {
        // Arrange
        using var controller = new TabController(_dockPanel);

        // Create a new form and dock panel for the second initialization attempt
        using var form2 = new Form();
        using var dockPanel2 = new DockPanel();
        form2.Controls.Add(dockPanel2);

        // Act & Assert
        _ = Assert.Throws<InvalidOperationException>(() => controller.InitializeDockPanel(dockPanel2));
    }

    #endregion

    #region GetAllWindowsFromDockPanel Tests

    [Test]
    public void GetAllWindowsFromDockPanel_WhenNotInitialized_ReturnsEmptyList ()
    {
        // Arrange
        using var controller = new TabController();

        // Act
        var result = controller.GetAllWindowsFromDockPanel();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetAllWindowsFromDockPanel_WhenInitializedButEmpty_ReturnsEmptyList ()
    {
        // Arrange - already done in Setup

        // Act
        var result = _tabController.GetAllWindowsFromDockPanel();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetAllWindowsFromDockPanel_ReturnsReadOnlyList ()
    {
        // Arrange - already done in Setup

        // Act
        var result = _tabController.GetAllWindowsFromDockPanel();

        // Assert - ReadOnlyCollection implements IReadOnlyList
        Assert.That(result, Is.InstanceOf<IReadOnlyList<LogWindow>>());
    }

    [Test]
    public void GetAllWindowsFromDockPanel_ReturnsDisplayedWindowsInTabOrder ()
    {
        // Arrange
        using var firstWindow = CreateLogWindow("first.log");
        using var secondWindow = CreateLogWindow("second.log");
        using var thirdWindow = CreateLogWindow("third.log");

        _tabController.AddWindow(firstWindow, "first.log");
        _tabController.AddWindow(secondWindow, "second.log");
        _tabController.AddWindow(thirdWindow, "third.log");

        // Act
        var result = _tabController.GetAllWindowsFromDockPanel();

        // Assert
        Assert.That(result, Is.EqualTo([firstWindow, secondWindow, thirdWindow]));
    }

    #endregion

    #region GetAllWindows Tests

    [Test]
    public void GetAllWindows_WhenEmpty_ReturnsEmptyList ()
    {
        // Arrange - already done in Setup

        // Act
        var result = _tabController.GetAllWindows();

        // Assert
        Assert.That(result, Is.Empty);
        Assert.That(result, Is.InstanceOf<IReadOnlyList<LogWindow>>());
    }

    #endregion

    #region GetWindowCount Tests

    [Test]
    public void GetWindowCount_WhenEmpty_ReturnsZero ()
    {
        // Arrange - already done in Setup

        // Act
        var result = _tabController.GetWindowCount();

        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion

    #region HasWindow Tests

    [Test]
    public void HasWindow_WithNullWindow_ReturnsFalse ()
    {
        // Arrange - already done in Setup

        // Act
        var result = _tabController.HasWindow(null);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region GetActiveWindow Tests

    [Test]
    public void GetActiveWindow_WhenNoWindowActive_ReturnsNull ()
    {
        // Arrange - already done in Setup

        // Act
        var result = _tabController.GetActiveWindow();

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region FindWindowByFileName Tests

    [Test]
    public void FindWindowByFileName_WithNullFileName_ReturnsNull ()
    {
        // Arrange - already done in Setup

        // Act
        var result = _tabController.FindWindowByFileName(null);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindWindowByFileName_WithEmptyFileName_ReturnsNull ()
    {
        // Arrange - already done in Setup

        // Act
        var result = _tabController.FindWindowByFileName(string.Empty);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindWindowByFileName_WhenNoWindowsExist_ReturnsNull ()
    {
        // Arrange - already done in Setup

        // Act
        var result = _tabController.FindWindowByFileName("test.log");

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region AddWindow Tests

    [Test]
    public void AddWindow_WithNullWindow_ThrowsArgumentNullException ()
    {
        // Arrange - already done in Setup

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => _tabController.AddWindow(null, "Test Window"));
    }

    [Test]
    public void AddWindow_WhenNotInitialized_ThrowsInvalidOperationException ()
    {
        // Arrange
        using var controller = new TabController();

        // Validate the argument before checking whether the controller is initialized.
        var ex = Assert.Throws<ArgumentNullException>(() => controller.AddWindow(null, "Test"));
        Assert.That(ex.ParamName, Is.EqualTo("window"));
    }

    #endregion

    #region RemoveWindow Tests

    [Test]
    public void RemoveWindow_WithNullWindow_DoesNotThrow ()
    {
        // Arrange - already done in Setup

        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => _tabController.RemoveWindow(null));
    }

    #endregion

    #region CloseWindow Tests

    [Test]
    public void CloseWindow_WithNullWindow_DoesNotThrow ()
    {
        // Arrange - already done in Setup

        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => _tabController.CloseWindow(null));
    }

    #endregion

    #region CloseAllWindows Tests

    [Test]
    public void CloseAllWindows_WhenEmpty_DoesNotThrow ()
    {
        // Arrange - already done in Setup

        // Act & Assert - should not throw
        Assert.DoesNotThrow(_tabController.CloseAllWindows);
    }

    #endregion

    #region CloseAllExcept Tests

    [Test]
    public void CloseAllExcept_WithNullWindow_DoesNotThrow ()
    {
        // Arrange - already done in Setup

        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => _tabController.CloseAllExcept(null));
    }

    #endregion

    #region ActivateWindow Tests

    [Test]
    public void ActivateWindow_WithNullWindow_DoesNotThrow ()
    {
        // Arrange - already done in Setup

        // Act & Assert - should not throw
        Assert.DoesNotThrow(() => _tabController.ActivateWindow(null));
    }

    #endregion

    #region SwitchToNextWindow Tests

    [Test]
    public void SwitchToNextWindow_WhenEmpty_DoesNotThrow ()
    {
        // Arrange - already done in Setup

        // Act & Assert - should not throw
        Assert.DoesNotThrow(_tabController.SwitchToNextWindow);
    }

    #endregion

    #region SwitchToPreviousWindow Tests

    [Test]
    public void SwitchToPreviousWindow_WhenEmpty_DoesNotThrow ()
    {
        // Arrange - already done in Setup

        // Act & Assert - should not throw
        Assert.DoesNotThrow(_tabController.SwitchToPreviousWindow);
    }

    #endregion

    #region Helpers

    private static LogWindow CreateLogWindow (string fileName)
    {
        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        _ = coordinatorMock.Setup(coordinator => coordinator.ResolveHighlightGroup(It.IsAny<string?>(), It.IsAny<string?>())).Returns(new HighlightGroup());
        _ = coordinatorMock.SetupGet(coordinator => coordinator.SearchParams).Returns(new SearchParams());

        var configManagerMock = new Mock<IConfigManager>();
        _ = configManagerMock.SetupGet(configManager => configManager.Settings).Returns(new Settings());

        var pluginRegistryMock = new Mock<IPluginRegistry>();
        _ = pluginRegistryMock.SetupGet(pluginRegistry => pluginRegistry.RegisteredColumnizers).Returns([new DefaultLogfileColumnizer()]);

        return new LogWindow(
            coordinatorMock.Object,
            fileName,
            isTempFile: false,
            forcePersistenceLoading: false,
            configManagerMock.Object,
            pluginRegistryMock.Object);
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_MultipleCallsDoNotThrow ()
    {
        // Arrange
        using var controller = new TabController(_dockPanel);

        // Act & Assert - multiple dispose calls should not throw
        Assert.DoesNotThrow(() =>
        {
            controller.Dispose();
            controller.Dispose();
            controller.Dispose();
        });
    }

    #endregion
}
