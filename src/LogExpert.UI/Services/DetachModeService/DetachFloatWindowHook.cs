using System.Runtime.Versioning;

using Vanara.PInvoke;

namespace LogExpert.UI.Services.DetachModeService;

/// <summary>
/// Win32 subclass for a detached <see cref="WeifenLuo.WinFormsUI.Docking.FloatWindow"/>: it turns a
/// caption double click into a maximize/restore toggle, because the suite docks the window back on
/// that message.
/// </summary>
/// <remarks>
/// Messages the hook does not intercept are forwarded to the procedure captured before
/// <see cref="NativeWindow.AssignHandle"/>.
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed class DetachFloatWindowHook : NativeWindow, IDisposable
{
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
    /// Starts watching the caption of <paramref name="floatWindow"/>. Call this after every
    /// style-changing property: those recreate the handle and would detach the hook.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Internal invariant message, not user-facing.")]
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
            // Swallowed on purpose: both the suite and DefWindowProc act on a caption double click.
            CaptionDoubleClicked?.Invoke(this, EventArgs.Empty);
            return;
        }

        m.Result = User32.CallWindowProc(_originalProc, m.HWnd, (uint)m.Msg, m.WParam, m.LParam);
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
        return User32.GetWindowLongPtr(handle, User32.WindowLongFlags.GWL_WNDPROC);
    }
}
