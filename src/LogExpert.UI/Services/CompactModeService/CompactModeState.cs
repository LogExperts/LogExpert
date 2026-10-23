using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Services.CompactModeService;

/// <summary>
/// Everything compact mode has to give back when it ends: the chrome it hides, and the log windows
/// it took off screen with the dock state each was shown in. The whole snapshot has one lifetime —
/// it exists while the mode is active and disappears when the mode ends — so it lives in one type
/// rather than in a handful of fields that always travel together.
/// </summary>
internal sealed class CompactModeState
{
    public bool StatusStripVisible { get; init; }

    public bool TopToolStripPanelVisible { get; init; }

    public DocumentStyle DocumentStyle { get; init; }

    /// <summary>
    /// The log windows taken off screen, in the order the dock panel listed them when the mode
    /// started. Entries are added, never refreshed, and removed only when a window is closed.
    /// </summary>
    public List<CompactModeWindowEntry> Windows { get; } = [];
}
