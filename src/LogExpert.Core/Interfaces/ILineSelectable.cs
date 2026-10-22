namespace LogExpert.Core.Interfaces;

/// <summary>
/// A Log Window in which a line can be selected programmatically.
/// </summary>
/// <remarks>
/// This is the role a <see cref="Classes.Filter.FilterPipe"/> keeps for its origin window:
/// "Locate line in original file" selects the original line there.
/// </remarks>
public interface ILineSelectable
{
    /// <summary>
    /// Selects the specified line in the log view and optionally scrolls to make it visible.
    /// </summary>
    /// <param name="lineNum">The zero-based line number to select.</param>
    /// <param name="triggerSyncCall">
    /// If <c>true</c>, triggers timestamp synchronization with other log windows
    /// that are in sync mode.
    /// </param>
    /// <param name="shouldScroll">
    /// If <c>true</c>, scrolls the view to ensure the selected line is visible.
    /// </param>
    void SelectLine (int lineNum, bool triggerSyncCall, bool shouldScroll);
}
