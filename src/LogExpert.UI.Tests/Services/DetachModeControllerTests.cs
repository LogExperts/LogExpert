using System.Runtime.Versioning;

using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Interface;
using LogExpert.UI.Services.DetachModeService;
using LogExpert.UI.Services.TabControllerService;

using Moq;

using NUnit.Framework;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Tests.Services;

/// <summary>
/// Unit tests for DetachModeController. Every test drives a real DockPanel holding real LogWindows,
/// because the behaviour under test is the dock-state bookkeeping and the float-window chrome the
/// DockPanel suite performs.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows")]
[Apartment(ApartmentState.STA)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "NUnit disposes the fields in TearDown.")]
internal class DetachModeControllerTests
{
    private Form _form;
    private DockPanel _dockPanel;
    private TabController _tabController;
    private DetachModeController _controller;

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

        _form.Controls.Add(_dockPanel);
        _form.Show();

        _tabController = new TabController(_dockPanel);
        _controller = new DetachModeController(_form, _dockPanel, _tabController);
    }

    [TearDown]
    public void TearDown ()
    {
        _controller?.Dispose();
        _tabController?.Dispose();
        _form?.Close();
        _form?.Dispose();
    }

    [Test]
    public void Enter_DetachesTheActiveWindowAsAWindowLikeFloat ()
    {
        // Arrange
        AddWindows("first.log", "second.log");
        var active = _dockPanel.ActiveContent as LogWindow;

        Assert.That(active, Is.Not.Null, "the dock panel should have an active document");

        // Act
        var entered = _controller.Enter();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(entered, Is.True);
            Assert.That(_controller.IsActive, Is.True);
            Assert.That(active.DockState, Is.EqualTo(DockState.Float));
            Assert.That(_form.Visible, Is.False, "the main window is hidden while detached");

            var floatWindow = FloatWindowOf(active);

            Assert.That(floatWindow, Is.Not.Null);
            Assert.That(floatWindow.Owner, Is.Null, "a hidden owner would take the float down with it");
            Assert.That(floatWindow.FormBorderStyle, Is.EqualTo(FormBorderStyle.Sizable));
            Assert.That(floatWindow.MaximizeBox, Is.True);
            Assert.That(floatWindow.MinimizeBox, Is.True);
            Assert.That(floatWindow.ShowInTaskbar, Is.True);
        });
    }

    [Test]
    public void Enter_GivesTheFloatItsOwnIconInsteadOfTheLogWindowSharedOne ()
    {
        // Arrange — the suite's SetText() assigns the log window's icon object to the float, but
        // the shell disposes and replaces that object on every tail refresh, which would leave the
        // float holding a disposed icon and crash the next handle recreation.
        var windows = AddWindows("first.log");
        windows[0].Icon = SystemIcons.Information;
        var active = _dockPanel.ActiveContent as LogWindow;

        Assert.That(active, Is.Not.Null);

        // Act
        _controller.Enter();

        // Assert
        var floatWindow = FloatWindowOf(active);

        Assert.Multiple(() =>
        {
            Assert.That(floatWindow, Is.Not.Null);
            Assert.That(floatWindow.Icon, Is.Not.EqualTo(windows[0].Icon), "the float must not share the log window's icon object");
        });
    }

    [Test]
    public void Enter_DoesNothingWhenNoWindowIsOpen ()
    {
        // Arrange — no log windows at all

        // Act
        var entered = _controller.Enter();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(entered, Is.False);
            Assert.That(_controller.IsActive, Is.False);
            Assert.That(_form.Visible, Is.True);
        });
    }

    [Test]
    public void Exit_RestoresTheWindowAndTheMainForm ()
    {
        // Arrange
        var windows = AddWindows("first.log", "second.log");
        var active = _dockPanel.ActiveContent as LogWindow;
        _controller.Enter();

        // Act
        _controller.Exit();

        // Assert — the window is back in the document pane, the others were never touched
        Assert.Multiple(() =>
        {
            Assert.That(_controller.IsActive, Is.False);
            Assert.That(_form.Visible, Is.True);
            Assert.That(active.DockState, Is.EqualTo(DockState.Document));
            Assert.That(windows.Count(window => window.DockState == DockState.Document), Is.EqualTo(windows.Count));
        });
    }

    [Test]
    public void Exit_KeepsAnAlreadyFloatingWindowFloating ()
    {
        // Arrange
        var windows = AddWindows("first.log", "floating.log");
        var floating = windows[1];

        floating.Show(_dockPanel, DockState.Float);
        Assert.That(_dockPanel.ActiveContent as LogWindow, Is.EqualTo(floating), "the float should be the active document");

        _controller.Enter();

        // Act
        _controller.Exit();

        // Assert — it came from a float, so it goes back to one
        Assert.That(floating.DockState, Is.EqualTo(DockState.Float));
    }

    [Test]
    public void SwitchWindow_MovesTheDetachedFloatThroughEveryWindow ()
    {
        // Arrange
        var windows = AddWindows("first.log", "second.log", "third.log");
        var start = _dockPanel.ActiveContent as LogWindow;

        Assert.That(start, Is.Not.Null, "the dock panel should have an active document");

        _controller.Enter();

        // Act & Assert — each switch detaches the next window and puts the previous one back
        var visited = new List<LogWindow>();

        for (var turn = 0; turn < windows.Count; turn++)
        {
            _controller.SwitchWindow(shiftPressed: false);

            var detached = windows.Single(window => window.DockState == DockState.Float);
            visited.Add(detached);

            Assert.That(windows.Count(window => window.DockState == DockState.Document), Is.EqualTo(windows.Count - 1));
            Assert.That(_form.Visible, Is.False);
        }

        Assert.Multiple(() =>
        {
            Assert.That(visited.Distinct().Count(), Is.EqualTo(windows.Count), "every window is reachable with Ctrl+Tab");
            Assert.That(visited[0], Is.Not.EqualTo(start), "the first switch moves off the window the mode started on");
            Assert.That(visited[^1], Is.EqualTo(start), "the cycle wraps around to where it started");
        });
    }

    [Test]
    public void OnWindowDisposed_LastWindow_EndsTheModeAndShowsTheMainForm ()
    {
        // Arrange
        var windows = AddWindows("only.log");
        var active = _dockPanel.ActiveContent as LogWindow;
        _controller.Enter();

        // Act — closing the detached window is deferred through BeginInvoke
        active.Dispose();
        _controller.OnWindowDisposed(active);
        Application.DoEvents();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_controller.IsActive, Is.False);
            Assert.That(_form.Visible, Is.True, "the last window is gone, so the main window has to come back");
        });
    }

    [Test]
    public void OnWindowDisposed_WithOtherWindows_EndsTheModeAndRestoresMainForm ()
    {
        // Arrange
        var windows = AddWindows("first.log", "second.log");
        var active = _dockPanel.ActiveContent as LogWindow;

        Assert.That(active, Is.Not.Null, "the dock panel should have an active document");

        _controller.Enter();

        // Act
        active.Dispose();
        _controller.OnWindowDisposed(active);
        Application.DoEvents();

        // Assert — closing the detached window ends detach mode and brings back the main form.
        // Other log windows remain where they were.
        var other = windows.Single(window => window != active);

        Assert.Multiple(() =>
        {
            Assert.That(_controller.IsActive, Is.False);
            Assert.That(_form.Visible, Is.True);
            Assert.That(other.DockState, Is.EqualTo(DockState.Document));
        });
    }

    [Test]
    public void OnWindowAdded_MakesTheNewWindowTheDetachedOne ()
    {
        // Arrange
        var windows = AddWindows("first.log");
        _controller.Enter();
        var detached = _dockPanel.ActiveContent as LogWindow;

        using var added = CreateLogWindow("added.log");
        _tabController.AddWindow(added, "added.log");

        // Act
        _controller.OnWindowAdded(added);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_controller.IsActive, Is.True);
            Assert.That(added.DockState, Is.EqualTo(DockState.Float));
            Assert.That(detached.DockState, Is.EqualTo(DockState.Document), "the previous detached window keeps its place in the main window");

            _controller.Exit();

            Assert.That(added.DockState, Is.EqualTo(DockState.Document), "the added window is restored like any other");
        });
    }

    [Test]
    public void PreFilterMessage_F11WhileDetached_LeavesTheMode ()
    {
        // Arrange
        AddWindows("first.log");
        _controller.Enter();

        var msg = new Message
        {
            Msg = 0x0100, // WM_KEYDOWN
            WParam = (IntPtr)0x7A, // VK_F11
            LParam = 0 // first press, no auto-repeat bit
        };

        // Act
        var swallowed = CreateFilter().PreFilterMessage(ref msg);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(swallowed, Is.True);
            Assert.That(_controller.IsActive, Is.False);
            Assert.That(_form.Visible, Is.True);
        });
    }

    [Test]
    public void PreFilterMessage_WhileNotDetached_PassesEveryKeyThrough ()
    {
        // Arrange — mode is off
        var msg = new Message
        {
            Msg = 0x0100,
            WParam = (IntPtr)0x7A, // VK_F11
            LParam = 0
        };

        // Act
        var swallowed = CreateFilter().PreFilterMessage(ref msg);

        // Assert
        Assert.That(swallowed, Is.False);
    }

    [Test]
    public void PreFilterMessage_F11AutoRepeat_IsSwallowedWithoutLeavingTheMode ()
    {
        // Arrange
        AddWindows("first.log");
        _controller.Enter();

        var msg = new Message
        {
            Msg = 0x0100,
            WParam = (IntPtr)0x7A, // VK_F11
            LParam = (IntPtr)(1L << 30) // auto-repeat bit set by Windows while the key is held
        };

        // Act
        var swallowed = CreateFilter().PreFilterMessage(ref msg);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(swallowed, Is.True, "the whole key burst is swallowed");
            Assert.That(_controller.IsActive, Is.True, "a repeat must not toggle the mode again");
        });
    }

    [Test]
    public void PreFilterMessage_Esc_PassesThroughAndStaysInMode ()
    {
        // Arrange — ESC is not handled by the filter at all; the grid or text input handles it
        AddWindows("first.log");
        _controller.Enter();
        var filter = CreateFilter();

        var msg = new Message
        {
            Msg = 0x0100,
            WParam = (IntPtr)0x1B, // VK_ESCAPE
            LParam = 0
        };

        // Act
        var swallowed = filter.PreFilterMessage(ref msg);

        // Assert — ESC passes through untouched
        Assert.Multiple(() =>
        {
            Assert.That(swallowed, Is.False);
            Assert.That(_controller.IsActive, Is.True);
        });
    }

    [Test]
    public void PreFilterMessage_F11WhileAnotherFormIsActive_DoesNotLeaveTheMode ()
    {
        // Arrange — a modal dialog or any other window holds the focus
        AddWindows("first.log");
        _controller.Enter();
        var filter = CreateFilter(activeFormGetter: () => _form);

        var msg = new Message
        {
            Msg = 0x0100,
            WParam = (IntPtr)0x7A, // VK_F11
            LParam = 0
        };

        // Act
        var swallowed = filter.PreFilterMessage(ref msg);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(swallowed, Is.False);
            Assert.That(_controller.IsActive, Is.True, "the key belongs to the form in front");
            Assert.That(_form.Visible, Is.False);
        });
    }

    [Test]
    public void PreFilterMessage_CtrlTabWhileDetached_SwitchesToTheNextWindow ()
    {
        // Arrange
        var windows = AddWindows("first.log", "second.log");
        _controller.Enter(); // detaches the active window, the last added one
        var filter = CreateFilter(() => Keys.Control);

        var msg = new Message
        {
            Msg = 0x0100,
            WParam = (IntPtr)0x09, // VK_TAB
            LParam = 0
        };

        // Act
        var swallowed = filter.PreFilterMessage(ref msg);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(swallowed, Is.True);
            Assert.That(_controller.IsActive, Is.True);
            Assert.That(windows[0].DockState, Is.EqualTo(DockState.Float), "the next window takes over");
            Assert.That(windows[1].DockState, Is.EqualTo(DockState.Document), "the previous detached window keeps its place");
        });
    }

    [Test]
    public void PreFilterMessage_CtrlShiftTabWhileDetached_SwitchesToThePreviousWindow ()
    {
        // Arrange
        var windows = AddWindows("first.log", "second.log", "third.log");
        _controller.Enter(); // detaches the active window, the last added one
        var filter = CreateFilter(() => Keys.Control | Keys.Shift);

        var msg = new Message
        {
            Msg = 0x0100,
            WParam = (IntPtr)0x09, // VK_TAB
            LParam = 0
        };

        // Act
        var swallowed = filter.PreFilterMessage(ref msg);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(swallowed, Is.True);
            Assert.That(_controller.IsActive, Is.True);
            Assert.That(windows[1].DockState, Is.EqualTo(DockState.Float), "the previous window takes over");
            Assert.That(windows[2].DockState, Is.EqualTo(DockState.Document), "the previous detached window keeps its place");
        });
    }

    [Test]
    public void PreFilterMessage_OtherKeysWhileDetached_PassThrough ()
    {
        // Arrange
        AddWindows("first.log");
        _controller.Enter();

        var msg = new Message
        {
            Msg = 0x0100,
            WParam = (IntPtr)0x41, // 'A' — not one of the detach mode keys
            LParam = 0
        };

        // Act
        var swallowed = CreateFilter().PreFilterMessage(ref msg);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(swallowed, Is.False);
            Assert.That(_controller.IsActive, Is.True);
        });
    }

    /// <summary>
    /// A fresh filter wired to the controller, so <c>PreFilterMessage</c> can be driven without a
    /// running message pump.
    /// </summary>
    private DetachMessageFilter CreateFilter (Func<Keys>? modifierKeysGetter = null, Func<Form>? activeFormGetter = null)
        => new(_controller, modifierKeysGetter, activeFormGetter ?? (() => _controller.DetachedFloat));

    private FloatWindow FloatWindowOf (LogWindow content)
    {
        foreach (FloatWindow floatWindow in _dockPanel.FloatWindows)
        {
            foreach (DockPane pane in floatWindow.NestedPanes)
            {
                if (pane.Contents.Contains(content))
                {
                    return floatWindow;
                }
            }
        }

        return null;
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

    private static LogWindow CreateLogWindow (string fileName)
    {
        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        _ = coordinatorMock.Setup(c => c.ResolveHighlightGroup(It.IsAny<string?>(), It.IsAny<string?>())).Returns(new HighlightGroup());
        _ = coordinatorMock.SetupGet(c => c.SearchParams).Returns(new SearchParams());

        var configManagerMock = new Mock<IConfigManager>();

        // Without these stubs the mock returns null and LogWindow's session save on Dispose
        // throws ArgumentNullException — surfaced as a modal MessageBox that blocks the test host.
        _ = configManagerMock.SetupGet(c => c.ActiveSessionDir).Returns(Path.Combine(Path.GetTempPath(), "LogExpert_DetachModeTests"));
        _ = configManagerMock.SetupGet(c => c.ActiveConfigDir).Returns(Path.Combine(Path.GetTempPath(), "LogExpert_DetachModeTests"));
        _ = configManagerMock.SetupGet(c => c.Settings).Returns(new Settings());

        var pluginRegistryMock = new Mock<IPluginRegistry>();
        _ = pluginRegistryMock.SetupGet(p => p.RegisteredColumnizers).Returns([new DefaultLogfileColumnizer()]);

        return new LogWindow(coordinatorMock.Object, fileName, false, false, configManagerMock.Object, pluginRegistryMock.Object);
    }
}
