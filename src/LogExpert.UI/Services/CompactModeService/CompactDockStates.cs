using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Services.CompactModeService;

/// <summary>
/// The dock-state rules compact mode applies, in one place: the single state a window is shown in
/// while the mode is active, and how a remembered state is given back when the mode ends. Both the
/// log windows and the bookmark tool window resolve their states here, so the three call sites that
/// used to map "hidden" differently cannot drift apart again.
/// </summary>
internal static class CompactDockStates
{
    /// <summary>
    /// The only state compact mode displays a log window in. The mode shows exactly one document
    /// and hides the tab strip: a float would come back as a separate window with its own caption,
    /// an auto-hide window would stay collapsed and leave the document area empty, and a hidden one
    /// has no visible pane to return to.
    /// </summary>
    public const DockState ActiveDisplayState = DockState.Document;

    /// <summary>
    /// Resolves the state a window is shown in when compact mode ends. A window whose remembered
    /// state is <see cref="DockState.Hidden"/> (hidden by the mode itself) or
    /// <see cref="DockState.Unknown"/> (never placed) has no pane to return to, so it falls back to
    /// <paramref name="fallback"/>; every other state is restored as it was.
    /// </summary>
    /// <param name="rememberedState">The state recorded before compact mode hid the window.</param>
    /// <param name="fallback">The state to use when the remembered one cannot be restored.</param>
    /// <returns>The state to show the window in.</returns>
    public static DockState ToRestored (DockState rememberedState, DockState fallback)
    {
        return rememberedState is DockState.Hidden or DockState.Unknown
            ? fallback
            : rememberedState;
    }
}
