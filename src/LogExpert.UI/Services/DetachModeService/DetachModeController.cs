using System.Runtime.Versioning;

using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Extensions;
using LogExpert.UI.Interface;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Services.DetachModeService;

/// <summary>
/// Owns detach mode: F11 hides the main window and keeps the active log window floating; leaving
/// the mode restores its original place and shows the main window again.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class DetachModeController : IDisposable
{
    private readonly Form _mainForm;
    private readonly DockPanel _dockPanel;
    private readonly ITabController _tabController;
    private readonly DetachMessageFilter _messageFilter;

    private DetachFloatWindowHook _captionHook;
    private bool _filterInstalled;
    private bool _disposed;

    private LogWindow _detachedWindow;

    private PlacementSnapshot _originalPlacement;
    private ChromeSnapshot _originalChrome;

    public DetachModeController (Form mainForm, DockPanel dockPanel, ITabController tabController)
    {
        ArgumentNullException.ThrowIfNull(mainForm);
        ArgumentNullException.ThrowIfNull(dockPanel);
        ArgumentNullException.ThrowIfNull(tabController);

        _mainForm = mainForm;
        _dockPanel = dockPanel;
        _tabController = tabController;
        _messageFilter = new DetachMessageFilter(this);
    }

    public bool IsActive => _detachedWindow != null;

    public FloatWindow DetachedFloat => _captionHook?.FloatWindow;

    public bool Enter ()
    {
        if (IsActive)
        {
            return true;
        }

        var active = _dockPanel.ActiveContent as LogWindow ?? _tabController.GetActiveWindow();

        if (active == null || active.IsDisposed)
        {
            return false;
        }

        if (!Detach(active))
        {
            // Hiding the main form without a float would leave no window to press F11 in.
            return false;
        }

        InstallFilter();
        _mainForm.Hide();
        return true;
    }

    public void Exit ()
    {
        if (!IsActive)
        {
            return;
        }

        var window = _detachedWindow;

        _detachedWindow = null;
        UninstallFilter();

        // Show the owner first: assigning it to a hidden main form would hide the float before its
        // content is docked back.
        RestoreMainForm();

        ReleaseFloatWindow();
        RestoreWindow(window);
        ClearOriginalPlacement();
    }

    private void ClearOriginalPlacement ()
    {
        _originalPlacement = default;
    }

    /// <summary>
    /// Ends the mode without a detached float, for the case where the suite cannot float a window:
    /// the main form has to come back even though no window can carry F11.
    /// </summary>
    private void LeaveDetachedState ()
    {
        _detachedWindow = null;
        UninstallFilter();
        ClearOriginalPlacement();
        RestoreMainForm();
    }

    private void RestoreMainFormLater ()
    {
        try
        {
            _mainForm.BeginInvoke(new Action(RestoreMainForm));
        }
        catch (InvalidOperationException)
        {
            RestoreMainForm();
        }
    }

    public void SwitchWindow (bool shiftPressed)
    {
        if (!IsActive)
        {
            return;
        }

        // Re-dock first: the floating window is not part of the tab strip, so the walk needs the
        // full order to stay stable.
        ReleaseFloatWindow();
        RestoreWindow(_detachedWindow);

        var target = shiftPressed
            ? _tabController.GetPreviousWindow(_detachedWindow)
            : _tabController.GetNextWindow(_detachedWindow);

        if (!Detach(target ?? _detachedWindow))
        {
            LeaveDetachedState();
        }
    }

    public void OnWindowAdded (LogWindow window)
    {
        if (!IsActive || window == null || window.IsDisposed)
        {
            return;
        }

        ReleaseFloatWindow();
        RestoreWindow(_detachedWindow);

        if (!Detach(window))
        {
            LeaveDetachedState();
        }
    }

    public void OnWindowDisposed (LogWindow window)
    {
        if (!IsActive || window != _detachedWindow)
        {
            return;
        }

        _detachedWindow = null;
        ReleaseFloatWindow(restoreChrome: false);
        UninstallFilter();
        ClearOriginalPlacement();
        RestoreMainFormLater();
    }

    private bool Detach (LogWindow window)
    {
        // Snapshot before Show(): the window reports Float afterwards.
        var pane = window.DockHandler.Pane;
        _originalPlacement = new PlacementSnapshot(window.DockState, pane, pane?.Contents.IndexOf(window) ?? -1);

        if (window.DockState != DockState.Float)
        {
            window.Show(_dockPanel, DockState.Float);
        }

        var floatWindow = FindFloatWindow(window);

        if (floatWindow == null)
        {
            // The suite refused to float the window; stay out of the mode instead of half-entering it.
            ClearOriginalPlacement();
            return false;
        }

        _originalChrome = CaptureChrome(floatWindow);
        PatchFloatWindow(floatWindow);
        _captionHook = DetachFloatWindowHook.Attach(floatWindow);
        _captionHook.CaptionDoubleClicked += ToggleMaximize;
        _detachedWindow = window;
        window.Activate();
        RepaintCaptionLater(floatWindow);
        return true;
    }

    private void RestoreWindow (LogWindow window)
    {
        if (window == null || window.IsDisposed)
        {
            return;
        }

        if (_originalPlacement.DockState == DockState.Float)
        {
            // Was already floating; only the chrome was patched.
            return;
        }

        if (_originalPlacement.Pane is { IsDisposed: false } pane && pane.DockState == _originalPlacement.DockState)
        {
            // Put it back into its original pane and tab index so split panes and tab order survive.
            window.Show(pane, null);

            if (_originalPlacement.ContentIndex >= 0 && _originalPlacement.ContentIndex < pane.Contents.Count)
            {
                pane.SetContentIndex(window, _originalPlacement.ContentIndex);
            }

            return;
        }

        // Pane disappeared (split closed, session load): fall back to dock-state-only placement.
        window.Show(_dockPanel, ToRestorable(_originalPlacement.DockState));
    }

    private void PatchFloatWindow (FloatWindow floatWindow)
    {
        // The suite shares the log window's Icon, whose handle is replaced on every tail refresh;
        // the main form's icon lives as long as the process.
        floatWindow.Icon = _mainForm.Icon;

        // A float is owned by the main form, which is hidden while detached: cut the ownership.
        floatWindow.Owner = null;
        floatWindow.FormBorderStyle = FormBorderStyle.Sizable;
        floatWindow.MaximizeBox = true;
        floatWindow.MinimizeBox = true;
        floatWindow.ShowInTaskbar = true;

        // The suite's caption handler would start a dock drag; leave the caption to the native move.
        floatWindow.AllowEndUserDocking = false;

        // The style changes above recreated the handle, so the dark title bar flag has to be set again.
        ApplyDarkTitleBar(floatWindow);
    }

    private static ChromeSnapshot CaptureChrome (FloatWindow floatWindow)
    {
        return new ChromeSnapshot(
            floatWindow.Owner,
            floatWindow.FormBorderStyle,
            floatWindow.WindowState,
            floatWindow.MaximizeBox,
            floatWindow.MinimizeBox,
            floatWindow.ShowInTaskbar,
            floatWindow.AllowEndUserDocking);
    }

    private static void RestoreChrome (FloatWindow floatWindow, ChromeSnapshot chrome)
    {
        floatWindow.Owner = chrome.Owner;
        floatWindow.FormBorderStyle = chrome.FormBorderStyle;
        floatWindow.MaximizeBox = chrome.MaximizeBox;
        floatWindow.MinimizeBox = chrome.MinimizeBox;
        floatWindow.ShowInTaskbar = chrome.ShowInTaskbar;
        floatWindow.AllowEndUserDocking = chrome.AllowEndUserDocking;

        // WinForms normalizes a maximized window as soon as the maximize box goes away, so the
        // state is applied last to bring back exactly what the suite had.
        floatWindow.WindowState = chrome.WindowState;

        // Restoring the styles recreated the handle: reapply the dark flag, then force a repaint.
        ApplyDarkTitleBar(floatWindow);
        RepaintCaptionLater(floatWindow);
    }

    private static void RepaintCaptionLater (FloatWindow floatWindow)
    {
        if (floatWindow.IsDisposed)
        {
            return;
        }

        try
        {
            floatWindow.BeginInvoke(new Action(() => RepaintCaption(floatWindow)));
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static void RepaintCaption (FloatWindow floatWindow)
    {
        if (floatWindow.IsDisposed)
        {
            return;
        }

        ApplyDarkTitleBar(floatWindow);

        if (floatWindow.WindowState != FormWindowState.Normal)
        {
            return;
        }

        var bounds = floatWindow.Bounds;

        floatWindow.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height + 1);
        floatWindow.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    private static void ApplyDarkTitleBar (FloatWindow floatWindow)
    {
        if (!Application.IsDarkModeEnabled || floatWindow is not { IsHandleCreated: true, IsDisposed: false })
        {
            return;
        }

        int value = 1;
        _ = NativeMethods.DwmSetWindowAttribute(floatWindow.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }

    private void ToggleMaximize (object sender, EventArgs e)
    {
        if (_captionHook == null || _captionHook.FloatWindow.IsDisposed)
        {
            return;
        }

        _captionHook.FloatWindow.WindowState = _captionHook.FloatWindow.WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
    }

    private void ReleaseFloatWindow (bool restoreChrome = true)
    {
        if (_captionHook == null)
        {
            return;
        }

        var floatWindow = _captionHook.FloatWindow;

        _captionHook.CaptionDoubleClicked -= ToggleMaximize;
        _captionHook.Dispose();
        _captionHook = null;

        if (restoreChrome && floatWindow is { IsDisposed: false })
        {
            // Restore the suite's tool-window chrome (owned border, no taskbar button, end-user docking).
            RestoreChrome(floatWindow, _originalChrome);
        }
    }

    private void InstallFilter ()
    {
        if (_filterInstalled)
        {
            return;
        }

        Application.AddMessageFilter(_messageFilter);
        _filterInstalled = true;
    }

    private void UninstallFilter ()
    {
        if (!_filterInstalled)
        {
            return;
        }

        Application.RemoveMessageFilter(_messageFilter);
        _filterInstalled = false;
    }

    private void RestoreMainForm ()
    {
        _mainForm.Show();
        _mainForm.Activate();
    }

    private FloatWindow FindFloatWindow (LogWindow content)
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

    private static DockState ToRestorable (DockState state)
    {
        return state is DockState.Unknown or DockState.Hidden ? DockState.Document : state;
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (IsActive)
        {
            try
            {
                Exit();
            }
            catch (ObjectDisposedException)
            {
                // Shutdown races: the panel or the window is already gone, nothing left to restore.
            }
        }

        ReleaseFloatWindow();
        UninstallFilter();
    }

    private readonly record struct PlacementSnapshot (
        DockState DockState,
        DockPane Pane,
        int ContentIndex);

    private readonly record struct ChromeSnapshot (
        Form Owner,
        FormBorderStyle FormBorderStyle,
        FormWindowState WindowState,
        bool MaximizeBox,
        bool MinimizeBox,
        bool ShowInTaskbar,
        bool AllowEndUserDocking);
}
