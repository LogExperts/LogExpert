using System.Runtime.Versioning;

using LogExpert.UI.Extensions;

namespace LogExpert.UI.Services.DetachModeService;

/// <summary>
/// Serves the keyboard of detach mode while the main window is hidden. The main form is not in
/// the focus chain then, so its <c>ProcessCmdKey</c> never fires and the keys are taken from the
/// message stream instead: F11 leaves the mode, Ctrl+Tab / Ctrl+Shift+Tab move to the previous or
/// next log window.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class DetachMessageFilter : IMessageFilter
{
    private const int VK_F11 = 0x7A;
    private const int VK_TAB = 0x09;

    private readonly DetachModeController _controller;
    private readonly Func<Keys> _modifierKeysGetter;
    private readonly Func<Form> _activeFormGetter;

    public DetachMessageFilter (
        DetachModeController controller,
        Func<Keys>? modifierKeysGetter = null,
        Func<Form>? activeFormGetter = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        _controller = controller;
        _modifierKeysGetter = modifierKeysGetter ?? (() => Control.ModifierKeys);
        _activeFormGetter = activeFormGetter ?? (() => Form.ActiveForm);
    }

    public bool PreFilterMessage (ref Message m)
    {
        if (m.Msg != NativeMethods.WM_KEYDOWN || !_controller.IsActive)
        {
            return false;
        }

        // Only the detached float owns these keys; a modal dialog on top of it must keep F11 and
        // Ctrl+Tab for itself.
        if (_activeFormGetter() != _controller.DetachedFloat)
        {
            return false;
        }

        switch (m.WParam.ToInt32())
        {
            case VK_F11:
                // Block the whole burst: acting on auto-repeat would flip the mode on every repeat.
                if ((m.LParam.ToInt64() & NativeMethods.KEYDOWN_AUTOREPEAT_FLAG) == 0)
                {
                    _controller.Exit();
                }

                return true;

            case VK_TAB when _modifierKeysGetter().HasFlag(Keys.Control):
                _controller.SwitchWindow(_modifierKeysGetter().HasFlag(Keys.Shift));
                return true;

            default:
                return false;
        }
    }
}
