using System.Runtime.Versioning;

using LogExpert.Dialogs;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Interface;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Services.CompactModeService;

/// <summary>
/// Owns compact mode: the chrome it hides, the one log window it keeps on screen, and how everything
/// is given back when the mode ends. Extracted from LogTabWindow so the transitions - enter, switch
/// window, close window, leave - can be exercised without the shell.
/// </summary>
/// <remarks>
/// The mode works from a snapshot taken on entry: every log window in the dock panel is recorded
/// with the dock state it had, and that record is never refreshed while the mode is active.
/// Switching windows only moves the "visible" marker and toggles visibility, so leaving the mode
/// restores the layout as it was, whatever the user did in between.
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed class CompactModeController
{
    private readonly Control _uiInvoker;
    private readonly DockPanel _dockPanel;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripContainer _toolStripContainer;
    private readonly DateTimeDragControl _timestampControl;
    private readonly ITabController _tabController;
    private readonly IToolWindowCoordinator _toolWindowCoordinator;

    /// <summary>
    /// The layout to give back when the mode ends. Non-null exactly while the mode is active: the
    /// snapshot is its own flag, which is what keeps "am I active" and "what do I restore" from
    /// ever disagreeing.
    /// </summary>
    private CompactModeState _restoreState;

    /// <summary>
    /// The one window currently on screen. Runtime state, not part of the snapshot: leaving the mode
    /// still restores every window to the state recorded on entry.
    /// </summary>
    private LogWindow _visibleWindow;

    public CompactModeController (
        Control uiInvoker,
        DockPanel dockPanel,
        StatusStrip statusStrip,
        ToolStripContainer toolStripContainer,
        DateTimeDragControl timestampControl,
        ITabController tabController,
        IToolWindowCoordinator toolWindowCoordinator)
    {
        ArgumentNullException.ThrowIfNull(uiInvoker);
        ArgumentNullException.ThrowIfNull(dockPanel);
        ArgumentNullException.ThrowIfNull(statusStrip);
        ArgumentNullException.ThrowIfNull(toolStripContainer);
        ArgumentNullException.ThrowIfNull(timestampControl);
        ArgumentNullException.ThrowIfNull(tabController);
        ArgumentNullException.ThrowIfNull(toolWindowCoordinator);

        _uiInvoker = uiInvoker;
        _dockPanel = dockPanel;
        _statusStrip = statusStrip;
        _toolStripContainer = toolStripContainer;
        _timestampControl = timestampControl;
        _tabController = tabController;
        _toolWindowCoordinator = toolWindowCoordinator;
    }

    /// <summary>
    /// Raised after entering and after leaving. Consumers update what the mode does not own: the
    /// menu check mark and the toolbar state of the active window.
    /// </summary>
    public event EventHandler CompactModeChanged;

    public bool IsActive => _restoreState != null;

    public void Toggle ()
    {
        if (IsActive)
        {
            Exit();
        }
        else
        {
            Enter();
        }
    }

    /// <summary>
    /// Hides the menu strip, toolbars, document tabs and status bar, leaving one log window on
    /// screen. The window keeps its title bar, size and position, so it stays movable, resizable and
    /// closable like any normal window.
    /// </summary>
    public void Enter ()
    {
        if (_restoreState != null)
        {
            return;
        }

        _restoreState = new CompactModeState
        {
            StatusStripVisible = _statusStrip.Visible,
            TopToolStripPanelVisible = _toolStripContainer.TopToolStripPanelVisible,
            DocumentStyle = _dockPanel.DocumentStyle
        };

        // The bookmark window docks below the documents and would take the space back.
        _toolWindowCoordinator.HideForCompactMode();

        // The DockPanel's own active content is what is really on screen; the shell's current
        // window can lag a step behind it during activation.
        var active = _dockPanel.ActiveContent as LogWindow ?? _tabController.GetActiveWindow();

        // DockPanelSuite offers no switch for hiding the document tab strip: the strip disappears
        // only for a document pane holding a single content while in SDI style. The other log
        // windows are therefore hidden for as long as the mode lasts.
        // Laid out once: every hidden window would otherwise re-layout the panes on its own.
        _dockPanel.SuspendLayout();

        try
        {
            if (active != null)
            {
                CaptureWindows(active);
            }

            _dockPanel.DocumentStyle = DocumentStyle.DockingSdi;

            if (active != null)
            {
                ShowAsActiveWindow(active);
            }
        }
        finally
        {
            _dockPanel.ResumeLayout(true, true);
        }

        _visibleWindow = active;

        _toolStripContainer.TopToolStripPanelVisible = false;
        _statusStrip.Visible = false;
        _timestampControl.Visible = false;

        CompactModeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gives the layout back: the document style and chrome recorded on entry, every window in the
    /// dock state it had, and the bookmark window through its coordinator.
    /// </summary>
    public void Exit ()
    {
        if (_restoreState == null)
        {
            return;
        }

        _dockPanel.SuspendLayout();

        try
        {
            _dockPanel.DocumentStyle = _restoreState.DocumentStyle;

            foreach (var entry in _restoreState.Windows)
            {
                if (!entry.Window.IsDisposed)
                {
                    entry.Window.Show(_dockPanel, CompactDockStates.ToRestored(entry.OriginalState, CompactDockStates.ActiveDisplayState));
                }
            }
        }
        finally
        {
            _dockPanel.ResumeLayout(true, true);
        }

        _toolStripContainer.TopToolStripPanelVisible = _restoreState.TopToolStripPanelVisible;
        _statusStrip.Visible = _restoreState.StatusStripVisible;

        _toolWindowCoordinator.RestoreAfterCompactMode(_dockPanel);

        var active = _visibleWindow;
        _visibleWindow = null;
        _restoreState = null;

        if (active != null && !active.IsDisposed)
        {
            active.Activate();
        }

        CompactModeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Ctrl+Tab inside compact mode: the tab strip is gone, so switching means swapping which
    /// window holds the document area.
    /// </summary>
    /// <param name="shiftPressed">True to move backwards in the tab order.</param>
    public void SwitchTab (bool shiftPressed)
    {
        if (_restoreState == null)
        {
            return;
        }

        var target = shiftPressed
            ? _tabController.GetPreviousWindow(_visibleWindow)
            : _tabController.GetNextWindow(_visibleWindow);

        if (target == null || target == _visibleWindow)
        {
            return;
        }

        ShowWindow(target);
    }

    /// <summary>
    /// Leaves <paramref name="target"/> as the only visible log window, hiding whichever window held
    /// the document area before. A window hidden earlier keeps the dock state it was recorded with,
    /// so switching through a float does not turn it into a docked document.
    /// </summary>
    /// <param name="target">The log window to show.</param>
    public void ShowWindow (LogWindow target)
    {
        if (_restoreState == null || target == null || target.IsDisposed || target == _visibleWindow)
        {
            return;
        }

        var current = _visibleWindow;
        _visibleWindow = target;

        // Recorded before hiding it: a window that entered the mode as a float or an auto-hide pane
        // must come back that way. Capture() keeps the first state it saw.
        Capture(target);

        _dockPanel.SuspendLayout();

        try
        {
            if (current != null && !current.IsDisposed)
            {
                current.Hide();
            }

            ShowAsActiveWindow(target);
        }
        finally
        {
            _dockPanel.ResumeLayout(true, true);
        }

        target.Activate();
    }

    /// <summary>
    /// Keeps a log window opened during compact mode as the visible one and hides the rest. The
    /// DockPanel has already activated the new window, and a second visible document would bring the
    /// tab strip back.
    /// </summary>
    /// <param name="window">The window that was just added.</param>
    public void OnWindowAdded (LogWindow window)
    {
        if (_restoreState == null || window == null || window.IsDisposed)
        {
            return;
        }

        _visibleWindow = window;

        Capture(window);

        // Materialised on purpose: hiding a window removes it from the panes being enumerated.
        // Visibility is not consulted: an inactive tab reports Visible == false although its tab is
        // on screen, and a second tab is exactly what the mode has to prevent.
        foreach (var other in _tabController.GetAllWindowsFromDockPanel())
        {
            if (other == null || other == window || other.IsDisposed)
            {
                continue;
            }

            Capture(other);
            other.Hide();
        }

        ShowAsActiveWindow(window);
        window.Activate();
    }

    /// <summary>
    /// Drops a closed window from the snapshot and, if it was the visible one, hands the document
    /// area to the next window still available.
    /// </summary>
    /// <param name="window">The window being disposed.</param>
    public void OnWindowDisposed (LogWindow window)
    {
        if (_restoreState == null || window == null)
        {
            return;
        }

        _restoreState.Windows.RemoveAll(entry => entry.Window == window);

        if (_visibleWindow != window)
        {
            return;
        }

        _visibleWindow = null;

        // Closing the only visible document while the others are hidden would leave an empty pane,
        // so the next hidden one takes its place.
        if (_restoreState.Windows.Count == 0 || !_uiInvoker.IsHandleCreated)
        {
            return;
        }

        var next = _restoreState.Windows[0].Window;

        // The DockPanel is still tearing the closed window down here; switching now would re-enter
        // that teardown.
        try
        {
            _uiInvoker.BeginInvoke(new Action(() =>
            {
                if (_restoreState != null && !next.IsDisposed)
                {
                    ShowWindow(next);
                }
            }));
        }
        catch (InvalidOperationException)
        {
            // The window lost its handle while closing (ObjectDisposedException derives from this as
            // well); there is nothing left to switch to.
        }
    }

    /// <summary>
    /// Records every log window the dock panel currently displays, and hides the ones that are not
    /// <paramref name="active"/>.
    /// </summary>
    /// <param name="active">The window to keep on screen.</param>
    private void CaptureWindows (LogWindow active)
    {
        _restoreState.Windows.Clear();

        // Materialised on purpose: hiding a window removes it from the panes being enumerated.
        foreach (var window in _tabController.GetAllWindowsFromDockPanel())
        {
            if (window == null || window.IsDisposed)
            {
                continue;
            }

            Capture(window);

            if (window != active)
            {
                window.Hide();
            }
        }
    }

    /// <summary>
    /// Records a window's dock state the first time it is seen. Later calls are no-ops, which is what
    /// keeps a window shown temporarily in the document area from overwriting its original state.
    /// </summary>
    /// <param name="window">The window to record.</param>
    private void Capture (LogWindow window)
    {
        if (_restoreState.Windows.Exists(entry => entry.Window == window))
        {
            return;
        }

        _restoreState.Windows.Add(new CompactModeWindowEntry(window, window.DockState));
    }

    /// <summary>
    /// Brings a window into the document area without re-laying out one that is already there.
    /// </summary>
    /// <param name="window">The window to show.</param>
    private void ShowAsActiveWindow (LogWindow window)
    {
        if (window.DockState != CompactDockStates.ActiveDisplayState)
        {
            window.Show(_dockPanel, CompactDockStates.ActiveDisplayState);
        }
    }
}
