using System.Runtime.Versioning;

using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Interface;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Services.DetachModeService;

/// <summary>
/// Owns detach mode: F11 hides the main window and turns the active log window's float into a
/// stand-alone viewer window; leaving the mode puts the window back where it was and brings the
/// main window back.
/// </summary>
/// <remarks>
/// The detached window stays a regular DockPanel float — no content is moved out of the suite — so
/// layout, session persistence and the close button keep working untouched. Three things are added
/// on top while the mode lasts: the float's owner link to the main form is cut (a hidden owner would
/// hide the float with it, a closing one would take it down), the tool-window chrome is replaced by
/// a normal border with minimize/maximize buttons and a taskbar button, and a caption double click
/// maximizes/restores instead of docking the window back into the panel.
/// </remarks>
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

    /// <summary>The window currently detached, or null while the mode is off.</summary>
    private LogWindow _detachedWindow;

    /// <summary>
    /// The dock state the detached window came from. Only valid while the mode is on; a window
    /// switched away from is put back into this state before the next one is detached, so a single
    /// field is enough to restore every window on exit.
    /// </summary>
    private DockState _originalDockState = DockState.Unknown;

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

    /// <summary>True while a log window is detached from the main window.</summary>
    public bool IsActive => _detachedWindow != null;

    /// <summary>
    /// Detaches the active log window and hides the main window. Does nothing when no log window is
    /// open; returns whether the mode was entered.
    /// </summary>
    public bool Enter ()
    {
        if (IsActive)
        {
            return true;
        }

        // The DockPanel's own active content is what is really on screen; the shell's current window
        // can lag a step behind it during activation.
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

    /// <summary>
    /// Ends the mode: the detached window goes back to the dock state it came from and the main
    /// window becomes visible again. Safe to call when the detached window is already gone.
    /// </summary>
    public void Exit ()
    {
        if (!IsActive)
        {
            return;
        }

        var window = _detachedWindow;
        var originalDockState = _originalDockState;

        _detachedWindow = null;
        _originalDockState = DockState.Unknown;
        ReleaseFloatWindow();
        UninstallFilter();

        if (window is { IsDisposed: false } && originalDockState != DockState.Float)
        {
            window.Show(_dockPanel, ToRestorable(originalDockState));
        }

        RestoreMainForm();
    }

    /// <summary>
    /// Ctrl+Tab while detached: the tab strip is gone with the main window, so switching means
    /// replacing which window the detached float shows. The window that was detached returns to its
    /// original dock state (inside the hidden main window) and keeps it for the final restore.
    /// </summary>
    /// <param name="shiftPressed">True to move backwards in the tab order.</param>
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

        if (_detachedWindow is { IsDisposed: false } && _originalDockState != DockState.Float)
        {
            _detachedWindow.Show(_dockPanel, ToRestorable(_originalDockState));
        }

        Detach(target);
    }

    /// <summary>
    /// A window opened while detached (file drop, command line, session) becomes the detached one;
    /// the DockPanel has already activated it.
    /// </summary>
    /// <param name="window">The window that was just added.</param>
    public void OnWindowAdded (LogWindow window)
    {
        if (!IsActive || window == null || window.IsDisposed)
        {
            return;
        }

        if (_detachedWindow is { IsDisposed: false } && _originalDockState != DockState.Float)
        {
            _detachedWindow.Show(_dockPanel, ToRestorable(_originalDockState));
        }

        Detach(window);
    }

    /// <summary>
    /// Reacts to the detached window being closed through the float's X button: another window takes
    /// its place, or — when it was the last one — the mode ends and the main window comes back.
    /// </summary>
    /// <param name="window">The window being disposed.</param>
    public void OnWindowDisposed (LogWindow window)
    {
        if (!IsActive || window != _detachedWindow)
        {
            return;
        }

        _detachedWindow = null;
        _originalDockState = DockState.Unknown;
        ReleaseFloatWindow();

        var next = _tabController.GetPlacedWindows()
            .FirstOrDefault(candidate => candidate != window && !candidate.IsDisposed);

        if (next == null)
        {
            UninstallFilter();
            RestoreMainForm();
            return;
        }

        // The DockPanel is still tearing the closed window down here; detaching the next window
        // would re-enter that teardown.
        try
        {
            _mainForm.BeginInvoke(new Action(() =>
            {
                if (!IsActive && !next.IsDisposed)
                {
                    Detach(next);
                }
            }));
        }
        catch (InvalidOperationException)
        {
            // The main window lost its handle while closing; the shutdown sequence owns the rest.
        }
    }

    /// <summary>
    /// Turns <paramref name="window"/> into the detached one: floats it if needed, upgrades its
    /// float window to a normal window and hooks the caption double click.
    /// </summary>
    private void Detach (LogWindow window)
    {
        ReleaseFloatWindow();

        // Captured before Show(): afterwards the window reports Float.
        _originalDockState = window.DockState;

        if (window.DockState != DockState.Float)
        {
            window.Show(_dockPanel, DockState.Float);
        }

        var floatWindow = FindFloatWindow(window);

        if (floatWindow == null)
        {
            // The suite refused to float the window (should not happen for a log window); leaving
            // _detachedWindow unset keeps the mode off instead of a half-entered state.
            _originalDockState = DockState.Unknown;
            return;
        }

        PatchFloatWindow(floatWindow);
        _captionHook = DetachFloatWindowHook.Attach(floatWindow);
        _captionHook.CaptionDoubleClicked += ToggleMaximize;
        _detachedWindow = window;
        window.Activate();
    }

    /// <summary>
    /// Replaces the tool-window chrome with a normal window chrome. All style-affecting properties
    /// are set before the caption hook is attached: changing them recreates the window handle.
    /// </summary>
    private void PatchFloatWindow (FloatWindow floatWindow)
    {
        // The suite's SetText() makes the float share the detached log window's Icon object, and
        // the shell replaces that object on every tail refresh while disposing the old one - the
        // float then holds a disposed icon, and the handle recreation from the style changes below
        // reads it and throws. The main form's icon lives as long as the process, so use it.
        floatWindow.Icon = _mainForm.Icon;

        // A float is an owned window of the main form: a hidden owner hides it, a closing owner
        // closes it. Detached mode hides the main window, so the ownership is cut for the time
        // being. The suite recreates ownership on the next float anyway.
        floatWindow.Owner = null;
        floatWindow.FormBorderStyle = FormBorderStyle.Sizable;
        floatWindow.MaximizeBox = true;
        floatWindow.MinimizeBox = true;
        floatWindow.ShowInTaskbar = true;

        // The suite's caption left-button handler would start a dock drag; with end-user docking
        // off the caption falls through to the native move-drag and the double click arrives at the
        // hook as a normal window's caption message.
        floatWindow.AllowEndUserDocking = false;

        // .NET 10 applies the dark title bar once, when the window first becomes visible
        // (Control.SetVisibleCore). The style changes above recreated the window handle and the
        // DWM flag died with the old one, so it has to be set again here (newer WinForms builds
        // restore it themselves on handle recreation).
        if (Application.IsDarkModeEnabled && floatWindow.IsHandleCreated)
        {
            SetImmersiveDarkTitleBar(floatWindow.Handle, dark: true);
        }
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

        _captionHook.CaptionDoubleClicked -= ToggleMaximize;
        _captionHook.Dispose();
        _captionHook = null;
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

    /// <summary>
    /// Maps a recorded dock state to one <see cref="DockContent.Show(DockPanel, DockState)"/>
    /// accepts. A window recorded while hidden or not yet placed cannot come back that way and lands
    /// in the document area instead.
    /// </summary>
    private static DockState ToRestorable (DockState state)
    {
        return state is DockState.Unknown or DockState.Hidden ? DockState.Document : state;
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private static void SetImmersiveDarkTitleBar (IntPtr handle, bool dark)
    {
        int value = dark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute (IntPtr hwnd, int attribute, ref int value, int size);

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
}
