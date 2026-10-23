using System.Runtime.Versioning;

namespace LogExpert.UI.Services.DetachModeService;

/// <summary>
/// Win32 subclass for a detached <see cref="WeifenLuo.WinFormsUI.Docking.FloatWindow"/>. The DockPanel
/// suite handles a caption double click by docking the window back into the panel (and its public
/// <c>DoubleClickTitleBarToDock</c> switch does not disable that), so the message has to be caught
/// before it reaches the suite: this hook turns a caption double click into a maximize/restore
/// toggle instead.
/// </summary>
/// <remarks>
/// Subclassing replaces the window procedure, so every message the hook does not intercept is
/// forwarded to the procedure captured before <see cref="NativeWindow.AssignHandle"/> - that is the
/// WinForms procedure which finally reaches the suite's <c>FloatWindow.WndProc</c>. Messages
/// swallowed here never arrive in the suite.
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed class DetachFloatWindowHook : NativeWindow, IDisposable
{
    private const int GWL_WNDPROC = -4;
    private const int WM_NCLBUTTONDBLCLK = 0x00A3;

    private readonly WeifenLuo.WinFormsUI.Docking.FloatWindow _floatWindow;
    private readonly IntPtr _originalProc;
    private bool _disposed;

    /// <summary>Raised when the user double clicks the caption of the hooked window.</summary>
    public event EventHandler CaptionDoubleClicked;

    private DetachFloatWindowHook (WeifenLuo.WinFormsUI.Docking.FloatWindow floatWindow)
    {
        _floatWindow = floatWindow;

        // Must be captured before AssignHandle replaces the procedure with this hook.
        _originalProc = GetWindowProc(floatWindow.Handle);
        AssignHandle(floatWindow.Handle);
    }

    /// <summary>
    /// Starts watching the caption of <paramref name="floatWindow"/>. Call this only after every
    /// style-changing property (border style, taskbar flag) has been applied: those recreate the
    /// window handle, and the hook would be detached by the recreation.
    /// </summary>
    public static DetachFloatWindowHook Attach (WeifenLuo.WinFormsUI.Docking.FloatWindow floatWindow)
    {
        ArgumentNullException.ThrowIfNull(floatWindow);

        if (!floatWindow.IsHandleCreated)
        {
            throw new InvalidOperationException("The float window handle must be created before hooking its caption.");
        }

        return new DetachFloatWindowHook(floatWindow);
    }

    /// <summary>The window this hook is attached to.</summary>
    public WeifenLuo.WinFormsUI.Docking.FloatWindow FloatWindow => _floatWindow;

    protected override void WndProc (ref Message m)
    {
        if (m.Msg == WM_NCLBUTTONDBLCLK)
        {
            // Swallowed on purpose: both the suite (dock back) and plain DefWindowProc (maximize)
            // have a fixed idea of what a caption double click means. The controller decides.
            CaptionDoubleClicked?.Invoke(this, EventArgs.Empty);
            return;
        }

        m.Result = CallWindowProc(_originalProc, m.HWnd, (uint)m.Msg, m.WParam, m.LParam);
    }

    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // ReleaseHandle restores the window procedure that AssignHandle replaced.
        if (Handle != IntPtr.Zero)
        {
            ReleaseHandle();
        }

        CaptionDoubleClicked = null;
    }

    private static IntPtr GetWindowProc (IntPtr handle)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(handle, GWL_WNDPROC)
            : new IntPtr(GetWindowLong32(handle, GWL_WNDPROC));
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr64 (IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong32 (IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc (IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
