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

    private DockState _originalDockState = DockState.Unknown;
    private DockPane _originalPane;
    private int _originalContentIndex;

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

        Detach(active);
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
        ReleaseFloatWindow();
        UninstallFilter();

        RestoreWindow(window);
        RestoreMainForm();
    }

    public void SwitchWindow (bool shiftPressed)
    {
        if (!IsActive)
        {
            return;
        }

        var target = shiftPressed
            ? _tabController.GetPreviousWindow(_detachedWindow)
            : _tabController.GetNextWindow(_detachedWindow);

        if (target == null || target == _detachedWindow)
        {
            return;
        }

        RestoreWindow(_detachedWindow);
        Detach(target);
    }

    public void OnWindowAdded (LogWindow window)
    {
        if (!IsActive || window == null || window.IsDisposed)
        {
            return;
        }

        RestoreWindow(_detachedWindow);
        Detach(window);
    }

    public void OnWindowDisposed (LogWindow window)
    {
        if (!IsActive || window != _detachedWindow)
        {
            return;
        }

        Exit();
    }

    private void Detach (LogWindow window)
    {
        ReleaseFloatWindow();

        // Snapshot before Show(): the window reports Float afterwards.
        _originalDockState = window.DockState;
        _originalPane = window.DockHandler.Pane;
        _originalContentIndex = _originalPane?.Contents.IndexOf(window) ?? -1;

        if (window.DockState != DockState.Float)
        {
            window.Show(_dockPanel, DockState.Float);
        }

        var floatWindow = FindFloatWindow(window);

        if (floatWindow == null)
        {
            // The suite refused to float the window; stay out of the mode instead of half-entering it.
            _originalDockState = DockState.Unknown;
            _originalPane = null;
            return;
        }

        _originalChrome = CaptureChrome(floatWindow);
        PatchFloatWindow(floatWindow);
        _captionHook = DetachFloatWindowHook.Attach(floatWindow);
        _captionHook.CaptionDoubleClicked += ToggleMaximize;
        _detachedWindow = window;
        window.Activate();
    }

    private void RestoreWindow (LogWindow window)
    {
        if (window == null || window.IsDisposed)
        {
            return;
        }

        if (_originalDockState == DockState.Float)
        {
            // Was already floating; only the chrome was patched.
            return;
        }

        if (_originalPane != null && !_originalPane.IsDisposed && _originalPane.DockState == _originalDockState)
        {
            // Put it back into its original pane and tab index so split panes and tab order survive.
            window.Show(_originalPane, null);

            if (_originalContentIndex >= 0 && _originalContentIndex < _originalPane.Contents.Count)
            {
                _originalPane.SetContentIndex(window, _originalContentIndex);
            }

            return;
        }

        // Pane disappeared (split closed, session load): fall back to dock-state-only placement.
        window.Show(_dockPanel, ToRestorable(_originalDockState));
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
        if (Application.IsDarkModeEnabled && floatWindow.IsHandleCreated)
        {
            SetImmersiveDarkTitleBar(floatWindow.Handle, dark: true);
        }
    }

    private static ChromeSnapshot CaptureChrome (FloatWindow floatWindow)
    {
        return new ChromeSnapshot(
            floatWindow.Owner,
            floatWindow.FormBorderStyle,
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

    private void ReleaseFloatWindow ()
    {
        if (_captionHook == null)
        {
            return;
        }

        var floatWindow = _captionHook.FloatWindow;

        _captionHook.CaptionDoubleClicked -= ToggleMaximize;
        _captionHook.Dispose();
        _captionHook = null;

        // Restore the suite's tool-window chrome (owned border, no taskbar button, end-user docking).
        if (floatWindow is { IsDisposed: false })
        {
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

    private static void SetImmersiveDarkTitleBar (IntPtr handle, bool dark)
    {
        int value = dark ? 1 : 0;
        _ = NativeMethods.DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }

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

    private readonly record struct ChromeSnapshot (
        Form Owner,
        FormBorderStyle FormBorderStyle,
        bool MaximizeBox,
        bool MinimizeBox,
        bool ShowInTaskbar,
        bool AllowEndUserDocking);
}
