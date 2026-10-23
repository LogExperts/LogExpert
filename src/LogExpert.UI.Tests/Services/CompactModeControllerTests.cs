using System.Runtime.Versioning;

using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.Dialogs;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Interface;
using LogExpert.UI.Services.CompactModeService;
using LogExpert.UI.Services.TabControllerService;

using Moq;

using NUnit.Framework;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Tests.Services;

/// <summary>
/// Unit tests for CompactModeController. Every test drives a real DockPanel holding real LogWindows,
/// because the behaviour under test is the dock-state bookkeeping the DockPanel performs.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows")]
[Apartment(ApartmentState.STA)]
internal class CompactModeControllerTests
{
    private Form _form;
    private DockPanel _dockPanel;
    private StatusStrip _statusStrip;
    private ToolStripContainer _toolStripContainer;
    private DateTimeDragControl _timestampControl;
    private TabController _tabController;
    private Mock<IToolWindowCoordinator> _toolWindowCoordinatorMock;
    private CompactModeController _controller;

    [SetUp]
    public void Setup ()
    {
        _form = new Form();
        _dockPanel = new DockPanel
        {
            Dock = DockStyle.Fill,
            DocumentStyle = DocumentStyle.DockingWindow,
            Theme = new VS2015LightTheme()
        };

        _statusStrip = new StatusStrip();
        _toolStripContainer = new ToolStripContainer
        {
            Dock = DockStyle.Top,
            TopToolStripPanelVisible = true
        };
        _timestampControl = new DateTimeDragControl();

        _form.Controls.Add(_dockPanel);
        _form.Controls.Add(_toolStripContainer);
        _form.Controls.Add(_statusStrip);
        _form.Show();

        _tabController = new TabController(_dockPanel);
        _toolWindowCoordinatorMock = new Mock<IToolWindowCoordinator>();

        _controller = new CompactModeController(
            _form,
            _dockPanel,
            _statusStrip,
            _toolStripContainer,
            _timestampControl,
            _tabController,
            _toolWindowCoordinatorMock.Object);
    }

    [TearDown]
    public void TearDown ()
    {
        if (_controller.IsActive)
        {
            _controller.Exit();
        }

        _tabController?.Dispose();
        _statusStrip?.Dispose();
        _toolStripContainer?.Dispose();
        _timestampControl?.Dispose();
        _form?.Close();
        _form?.Dispose();
    }

    [Test]
    public void Enter_KeepsTheActiveWindowVisibleAndHidesTheOthers ()
    {
        // Arrange
        var windows = AddWindows("first.log", "second.log", "third.log");
        var active = _dockPanel.ActiveContent as LogWindow;

        Assert.That(active, Is.Not.Null, "the dock panel should have an active document");

        // Act
        _controller.Enter();

        // Assert
        Assert.That(_controller.IsActive, Is.True);
        Assert.That(active.Visible, Is.True);

        foreach (var window in windows.Where(window => window != active))
        {
            Assert.That(window.Visible, Is.False);
        }

        Assert.That(_dockPanel.DocumentStyle, Is.EqualTo(DocumentStyle.DockingSdi));
        Assert.That(_statusStrip.Visible, Is.False);
        Assert.That(_toolStripContainer.TopToolStripPanelVisible, Is.False);
        Assert.That(_timestampControl.Visible, Is.False);
    }

    [Test]
    public void Exit_GivesBackEveryWindowAndTheChrome ()
    {
        // Arrange
        var windows = AddWindows("first.log", "second.log", "third.log");
        _controller.Enter();

        // Act
        _controller.Exit();

        // Assert
        Assert.That(_controller.IsActive, Is.False);
        Assert.That(_dockPanel.DocumentStyle, Is.EqualTo(DocumentStyle.DockingWindow));
        Assert.That(_statusStrip.Visible, Is.True);
        Assert.That(_toolStripContainer.TopToolStripPanelVisible, Is.True);

        // Every window is back in the document pane, in its original order. Visibility is not the
        // measure here: an inactive tab reports Visible == false while its tab is on screen.
        Assert.That(_tabController.GetAllWindowsFromDockPanel(), Is.EqualTo(windows));
        Assert.That(windows.Select(window => window.DockState), Is.All.EqualTo(DockState.Document));
    }

    [Test]
    public void Exit_RestoresAFloatingWindowToItsFloatState ()
    {
        // Arrange
        var windows = AddWindows("first.log", "second.log");
        var floating = windows[1];

        floating.Show(_dockPanel, DockState.Float);
        Assert.That(floating.DockState, Is.EqualTo(DockState.Float));

        // Act
        _controller.Enter();
        _controller.Exit();

        // Assert
        Assert.That(floating.DockState, Is.EqualTo(DockState.Float));
    }

    [Test]
    public void ShowWindow_HidesTheCurrentWindowWithoutOverwritingItsOriginalState ()
    {
        // Arrange — the float window becomes the visible one and goes back into hiding twice
        var windows = AddWindows("first.log", "floating.log");
        var floating = windows[1];

        floating.Show(_dockPanel, DockState.Float);

        _controller.Enter();
        _controller.ShowWindow(floating);

        Assert.That(floating.Visible, Is.True);
        Assert.That(floating.DockState, Is.EqualTo(DockState.Document), "compact mode shows it in the document area");

        _controller.ShowWindow(windows[0]);

        // Act
        _controller.Exit();

        // Assert — the temporary document display must not survive the mode
        Assert.That(floating.DockState, Is.EqualTo(DockState.Float));
    }

    [Test]
    public void ShowWindow_BringsAnAutoHideWindowIntoTheDocumentArea ()
    {
        // Arrange
        var windows = AddWindows("first.log", "autoHide.log");
        var autoHide = windows[1];

        autoHide.Show(_dockPanel, DockState.DockBottomAutoHide);

        _controller.Enter();

        // Act
        _controller.ShowWindow(autoHide);

        // Assert — an auto-hide window would stay collapsed and leave the document area empty
        Assert.That(autoHide.Visible, Is.True);
        Assert.That(autoHide.DockState, Is.EqualTo(DockState.Document));

        // Act
        _controller.Exit();

        // Assert — leaving the mode restores the auto-hide pane
        Assert.That(autoHide.DockState, Is.EqualTo(DockState.DockBottomAutoHide));
    }

    [Test]
    public void SwitchTab_CyclesThroughEveryWindow ()
    {
        // Arrange
        var windows = AddWindows("first.log", "second.log", "third.log");
        var start = _dockPanel.ActiveContent as LogWindow;

        Assert.That(start, Is.Not.Null, "the dock panel should have an active document");

        _controller.Enter();

        // Act
        var visited = new List<LogWindow>();

        for (var turn = 0; turn < windows.Count; turn++)
        {
            _controller.SwitchTab(shiftPressed: false);

            // Assert — one window on screen at any time, and the first switch leaves the start
            Assert.That(_controller.IsActive, Is.True);
            Assert.That(windows.Count(window => window.Visible), Is.EqualTo(1));

            var shown = VisibleWindow(windows);

            if (turn == 0)
            {
                Assert.That(shown, Is.Not.EqualTo(start), "the first switch moves off the window the mode started on");
            }

            visited.Add(shown);
        }

        // Assert — every window is reachable and the cycle wraps around
        Assert.That(visited.Distinct().Count(), Is.EqualTo(windows.Count), "every window is reachable with Ctrl+Tab");
        Assert.That(VisibleWindow(windows), Is.EqualTo(start), "the cycle wraps around to where it started");
    }

    [Test]
    public void OnWindowAdded_KeepsTheNewWindowAsTheOnlyVisibleOne ()
    {
        // Arrange
        var windows = AddWindows("first.log", "second.log");
        _controller.Enter();

        // Act
        using var added = CreateLogWindow("added.log");
        _tabController.AddWindow(added, "added.log");
        _controller.OnWindowAdded(added);

        // Assert
        Assert.That(added.Visible, Is.True);
        Assert.That(windows.Select(window => window.Visible), Is.All.False);

        // Act
        _controller.Exit();

        // Assert — every window comes back, the one opened during the mode included
        Assert.That(_tabController.GetAllWindowsFromDockPanel(), Is.EquivalentTo([.. windows, added]));
    }

    [Test]
    public void OnWindowDisposed_HandsTheDocumentAreaToTheNextWindow ()
    {
        // Arrange
        var windows = AddWindows("first.log", "second.log", "third.log");
        var active = _dockPanel.ActiveContent as LogWindow;
        _controller.Enter();

        var others = windows.Where(window => window != active).ToList();

        // Act — closing the visible window is deferred through BeginInvoke
        active.Dispose();
        _controller.OnWindowDisposed(active);
        Application.DoEvents();

        // Assert
        Assert.That(others.Count(window => window.Visible), Is.EqualTo(1));
    }

    [Test]
    public void Enter_DoesNotHideAnythingWhenNoWindowIsOnScreen ()
    {
        // Arrange — no log windows at all
        // Act
        _controller.Enter();

        // Assert — the chrome is still hidden, but no window was touched
        Assert.That(_controller.IsActive, Is.True);
        Assert.That(_statusStrip.Visible, Is.False);

        // Act
        _controller.Exit();

        // Assert
        Assert.That(_statusStrip.Visible, Is.True);
    }

    private List<LogWindow> AddWindows (params string[] fileNames)
    {
        var windows = fileNames.Select(CreateLogWindow).ToList();

        foreach (var (window, fileName) in windows.Zip(fileNames))
        {
            _tabController.AddWindow(window, fileName);
        }

        return windows;
    }

    private static LogWindow VisibleWindow (IEnumerable<LogWindow> windows)
    {
        return windows.SingleOrDefault(window => window.Visible);
    }

    private static LogWindow CreateLogWindow (string fileName)
    {
        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        _ = coordinatorMock.Setup(c => c.ResolveHighlightGroup(It.IsAny<string?>(), It.IsAny<string?>())).Returns(new HighlightGroup());
        _ = coordinatorMock.SetupGet(c => c.SearchParams).Returns(new SearchParams());

        var configManagerMock = new Mock<IConfigManager>();
        _ = configManagerMock.SetupGet(c => c.Settings).Returns(new Settings());

        var pluginRegistryMock = new Mock<IPluginRegistry>();
        _ = pluginRegistryMock.SetupGet(p => p.RegisteredColumnizers).Returns([new DefaultLogfileColumnizer()]);

        return new LogWindow(coordinatorMock.Object, fileName, false, false, configManagerMock.Object, pluginRegistryMock.Object);
    }
}
