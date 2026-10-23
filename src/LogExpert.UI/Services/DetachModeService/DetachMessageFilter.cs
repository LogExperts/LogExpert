using System.Runtime.Versioning;

namespace LogExpert.UI.Services.DetachModeService;

/// <summary>
/// Serves the keyboard of detach mode while the main window is hidden. The main form is not in the
/// focus chain then, so its <c>ProcessCmdKey</c> never fires and the keys are taken from the message
/// stream instead: F11 and ESC leave the mode, Ctrl+Tab / Ctrl+Shift+Tab move to the previous or
/// next log window.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class DetachMessageFilter : IMessageFilter
{
    private const int WM_KEYDOWN = 0x0100;
    private const int VK_F11 = 0x7A;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_TAB = 0x09;

    /// <summary>The lParam bit Windows sets on every WM_KEYDOWN but the first while a key is held.</summary>
    private const long AUTOREPEAT_FLAG = 1L << 30;

    private readonly DetachModeController _controller;

    // Test seams: both read live machine state a unit test cannot control (the keyboard modifiers
    // and the input focus), so their source is injectable. Production always uses the defaults.

    internal Func<Keys> _modifierKeys = static () => Control.ModifierKeys;

    // The detached float window is the active form while the mode lasts, so its active control is
    // where the keyboard is. Null when another window holds the focus.
    internal Func<bool> _isTextInputFocused = static () => Form.ActiveForm?.ActiveControl is TextBoxBase;

    public DetachMessageFilter (DetachModeController controller)
    {
        _controller = controller;
    }

    public bool PreFilterMessage (ref Message m)
    {
        if (m.Msg != WM_KEYDOWN || !_controller.IsActive)
        {
            return false;
        }

        switch (m.WParam.ToInt32())
        {
            case VK_F11:
                // Block the whole burst: acting on auto-repeat would flip the mode on every repeat.
                if ((m.LParam.ToInt64() & AUTOREPEAT_FLAG) == 0)
                {
                    _controller.Exit();
                }

                return true;

            case VK_ESCAPE:
                // The log window uses ESC to cancel a search or clear search highlighting, so leave
                // the mode only when the focus is not in a text input.
                if (_isTextInputFocused())
                {
                    return false;
                }

                _controller.Exit();
                return true;

            case VK_TAB when _modifierKeys().HasFlag(Keys.Control):
                _controller.SwitchWindow(_modifierKeys().HasFlag(Keys.Shift));
                return true;

            default:
                return false;
        }
    }
}
