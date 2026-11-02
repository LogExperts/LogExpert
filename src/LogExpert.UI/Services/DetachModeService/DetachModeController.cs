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
    private readonly Func<FloatWindow, DetachFloatWindowHook> _attachCaptionHook;

    private DetachFloatWindowHook? _captionHook;
    private bool _filterInstalled;
    private bool _disposed;

    private LogWindow? _detachedWindow;

    private PlacementSnapshot _originalPlacement;
    private ChromeSnapshot? _originalChrome;

    public DetachModeController (
        Form mainForm,
        DockPanel dockPanel,
        ITabController tabController,
        Func<FloatWindow, DetachFloatWindowHook>? attachCaptionHook = null)
    {
        ArgumentNullException.ThrowIfNull(mainForm);
        ArgumentNullException.ThrowIfNull(dockPanel);
        ArgumentNullException.ThrowIfNull(tabController);

        _mainForm = mainForm;
        _dockPanel = dockPanel;
        _tabController = tabController;
        _messageFilter = new DetachMessageFilter(this);

        // Overridable so a test can fail the attach after the chrome has already been patched.
        _attachCaptionHook = attachCaptionHook ?? DetachFloatWindowHook.Attach;
    }

    public bool IsActive => _detachedWindow != null;

    public FloatWindow? DetachedFloat => _captionHook?.FloatWindow;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A mode that could not be entered has to be left again, whatever went wrong.")]
    public bool Enter ()
    {
        if (IsActive)
        {
            return true;
        }

        try
        {
            var active = _dockPanel.ActiveContent as LogWindow ?? _tabController.GetActiveWindow();

            if (active == null || active.IsDisposed)
            {
                return false;
            }

            if (!TryDetach(active))
            {
                // Hiding the main form without a float would leave no window to press F11 in.
                return false;
            }

            InstallFilter();
            _mainForm.Hide();
            return true;
        }
        catch (Exception)
        {
            // A main form that stayed visible with the mode still on could not be left with F11:
            // ProcessCmdKey would just re-enter the mode.
            Exit();
            return false;
        }
    }

    /// <summary>
    /// Leaves the mode: releases the float, restores the window and brings the main form back.
    /// </summary>
    /// <param name="restoreMainForm">
    /// Pass <c>false</c> when the caller is closing the main form: showing it again would make it
    /// flash during shutdown.
    /// </param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exit() runs from the message filter, from session load, from Enter()'s failure path and from Dispose().")]
    public void Exit (bool restoreMainForm = true)
    {
        var window = _detachedWindow;

        _detachedWindow = null;

        try
        {
            if (window != null && restoreMainForm)
            {
                // Show the owner first: assigning it to a hidden main form would hide the float
                // before its content is docked back.
                RestoreMainForm();
            }

            // Also releases a hook a half-entered mode left behind without a detached window.
            try
            {
                ReleaseFloatWindow();
            }
            catch (Exception)
            {
                // A hook that cannot be released must not keep the window floating as well.
            }

            RestoreWindow(window);
        }
        catch (Exception)
        {
            // The form was already brought back before the throw; the finally only has to clean up.
        }
        finally
        {
            try
            {
                UninstallFilter();
            }
            catch (Exception)
            {
                // A filter that cannot be removed is inert once the mode is off.
            }

            ClearSnapshots();
        }
    }

    private void ClearSnapshots ()
    {
        _originalPlacement = default;
        _originalChrome = null;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This only runs while a failure is already being handled.")]
    private void LeaveDetachedState (LogWindow? window)
    {
        _detachedWindow = null;

        // Show the form before the float takes its owner back, or the restored chrome hides it.
        RestoreMainForm();

        try
        {
            UninstallFilter();
        }
        catch (Exception)
        {
            // Best effort: the caller is already ending the mode.
        }

        try
        {
            ReleaseFloatWindow();
        }
        catch (Exception)
        {
            // The hook is dropped as the release starts, so a failed dispose cannot revive it.
        }

        // Only with a live snapshot: after a rollback the window is already back, and showing it
        // through the fallback would move a window that was originally floating or docked.
        if (_originalPlacement.DockState != DockState.Unknown)
        {
            try
            {
                RestoreWindow(window);
            }
            catch (Exception)
            {
                // A window that cannot be docked back stays where it is; the main form still returns.
            }
        }

        ClearSnapshots();
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
        if (_detachedWindow is not { } previous)
        {
            return;
        }

        SwitchTo(previous, () => shiftPressed
            ? _tabController.GetPreviousWindow(previous)
            : _tabController.GetNextWindow(previous));
    }

    public void OnWindowAdded (LogWindow window)
    {
        if (_detachedWindow is not { } previous || window.IsDisposed)
        {
            return;
        }

        SwitchTo(previous, () => window);
    }

    /// <summary>
    /// Swaps the detached window for another one. The current float is released before
    /// <paramref name="resolveNext"/> runs, because the next/previous walk needs it back in the tab
    /// strip.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A failed switch must end in a visible window rather than an exception.")]
    private void SwitchTo (LogWindow previous, Func<LogWindow?> resolveNext)
    {
        try
        {
            ReleaseAndRestore(previous);

            if (!TryDetach(resolveNext() ?? previous))
            {
                LeaveDetachedState(previous);
            }
        }
        catch (Exception)
        {
            // Nothing may escape while the main form is hidden.
            LeaveDetachedState(previous);
        }
    }

    // Puts the floating window back into the tab strip before the next one is detached: the float
    // is not part of the strip, so the walk needs the full order to stay stable.
    private void ReleaseAndRestore (LogWindow? window)
    {
        ReleaseFloatWindow();
        RestoreWindow(window);

        // The old placement is consumed: a later failure must not move the window a second time.
        ClearSnapshots();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A failed detach falls back to a visible window instead of propagating.")]
    private bool TryDetach (LogWindow window)
    {
        // Detach() snapshots afresh, so a throw before that point must not roll back to the old place.
        ClearSnapshots();

        try
        {
            return Detach(window);
        }
        catch (Exception)
        {
            // The caller ends the mode when this returns false.
            RollBackFailedDetach(window);
            return false;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The rollback runs while the main form is hidden and must not throw in turn.")]
    private void RollBackFailedDetach (LogWindow window)
    {
        // Detach() assigns this before Activate(), which can throw on a torn-down window.
        _detachedWindow = null;

        var floatWindow = _captionHook?.FloatWindow ?? FindFloatWindow(window);

        try
        {
            ReleaseFloatWindow();
        }
        catch (Exception)
        {
            // The hook is dropped as the release starts, so a failed dispose cannot revive it.
        }

        // Kept apart from the placement restore so a chrome failure cannot keep the window floating.
        try
        {
            // PatchFloatWindow() can run without a hook to release it, so nothing else would restore the chrome.
            RestorePatchedChrome(floatWindow);
        }
        catch (Exception)
        {
            // The chrome is cosmetic; docking the window back matters more.
        }

        if (_originalPlacement.DockState != DockState.Unknown)
        {
            try
            {
                RestoreWindow(window);
            }
            catch (Exception)
            {
                // A window that cannot be docked back stays where it is; the main form still returns.
            }
        }

        ClearSnapshots();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The detached window is already gone; nothing may escape into its dispose path.")]
    public void OnWindowDisposed (LogWindow window)
    {
        if (!IsActive || window != _detachedWindow)
        {
            return;
        }

        _detachedWindow = null;

        // Uninstall first and on its own: a filter left behind keeps this controller alive, and it
        // can never fire again once the mode is off.
        try
        {
            UninstallFilter();
        }
        catch (Exception)
        {
            // A filter that cannot be removed is inert while the mode is off.
        }

        try
        {
            ReleaseFloatWindow(restoreChrome: false);
        }
        catch (Exception)
        {
            // The float is being torn down with its content.
        }

        ClearSnapshots();
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
            // The suite refused to float the window; put it back instead of half-entering the mode.
            RollBackFailedDetach(window);
            return false;
        }

        // The snapshot has to exist before the first style change: everything after it can throw,
        // and only the snapshot can put the suite's chrome back.
        _originalChrome = CaptureChrome(floatWindow);
        PatchFloatWindow(floatWindow);

        var hook = _attachCaptionHook(floatWindow);
        hook.CaptionDoubleClicked += ToggleMaximize;
        _captionHook = hook;
        _detachedWindow = window;

        window.Activate();
        RepaintCaptionLater(floatWindow);
        return true;
    }

    private void RestoreWindow (LogWindow? window)
    {
        if (window == null || window.IsDisposed)
        {
            return;
        }

        if (_originalPlacement.DockState == DockState.Float)
        {
            // Was already floating; only the chrome was patched, and that is restored separately.
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

    private void RestorePatchedChrome (FloatWindow? floatWindow)
    {
        if (_originalChrome is not { } chrome)
        {
            return;
        }

        if (floatWindow is not { IsDisposed: false })
        {
            // The float is gone; there is nothing left to restore the chrome on.
            _originalChrome = null;
            return;
        }

        RestoreChrome(floatWindow, chrome);

        // Cleared only after the restore succeeded, so a throw leaves the snapshot in place.
        _originalChrome = null;
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

    private void ToggleMaximize (object? sender, EventArgs e)
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
        if (_captionHook is not { } hook)
        {
            return;
        }

        _captionHook = null;

        try
        {
            hook.CaptionDoubleClicked -= ToggleMaximize;
            hook.Dispose();
        }
        finally
        {
            // A hook that fails to dispose must not leave the patched chrome behind.
            if (restoreChrome)
            {
                RestorePatchedChrome(hook.FloatWindow);
            }
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is the last-resort path back to a visible window.")]
    private void RestoreMainForm ()
    {
        // Dispose() leaves the mode through Exit(); showing the form on that path would run while
        // the form is being torn down.
        if (_disposed || _mainForm.IsDisposed)
        {
            return;
        }

        try
        {
            _mainForm.Show();
            _mainForm.Activate();
        }
        catch (Exception)
        {
            // Nothing else can bring the form back; the caller is already on its fallback path.
        }
    }

    private FloatWindow? FindFloatWindow (LogWindow content)
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

        // Exit() is the single way out: it releases the float, uninstalls the filter and brings the
        // main form back.
        Exit();
    }

    private readonly record struct PlacementSnapshot (
        DockState DockState,
        DockPane? Pane,
        int ContentIndex);

    private readonly record struct ChromeSnapshot (
        Form? Owner,
        FormBorderStyle FormBorderStyle,
        FormWindowState WindowState,
        bool MaximizeBox,
        bool MinimizeBox,
        bool ShowInTaskbar,
        bool AllowEndUserDocking);
}
