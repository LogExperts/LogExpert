using LogExpert.UI.Controls.LogWindow;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Services.CompactModeService;

/// <summary>
/// One log window hidden for the duration of compact mode, with the dock state it was shown in when
/// the mode started. The state is recorded once and never refreshed while the mode is active, so a
/// window shown temporarily in the document area still returns to its float or auto-hide pane.
/// </summary>
/// <param name="window">The hidden log window.</param>
/// <param name="originalState">The dock state the window had before compact mode hid it.</param>
internal sealed class CompactModeWindowEntry (LogWindow window, DockState originalState)
{
    public LogWindow Window { get; } = window;

    public DockState OriginalState { get; } = originalState;
}
