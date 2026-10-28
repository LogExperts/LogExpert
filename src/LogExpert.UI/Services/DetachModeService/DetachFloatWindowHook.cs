using System.Runtime.Versioning;

namespace LogExpert.UI.Services.DetachModeService;

/// <summary>
/// Win32 subclass for a detached <see cref="WeifenLuo.WinFormsUI.Docking.FloatWindow"/>: it turns a
/// caption double click into a maximize/restore toggle, because the suite docks the window back on
/// that message.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class DetachFloatWindowHook : NativeWindow, IDisposable
{
    private const int WM_NCLBUTTONDBLCLK = 0x00A3;

    private readonly WeifenLuo.WinFormsUI.Docking.FloatWindow _floatWindow;
    private bool _disposed;

    /// <summary>Raised when the user double clicks the caption of the hooked window.</summary>
    public event EventHandler CaptionDoubleClicked;

    private DetachFloatWindowHook (WeifenLuo.WinFormsUI.Docking.FloatWindow floatWindow)
    {
        _floatWindow = floatWindow;
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

        base.WndProc(ref m);
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
}
