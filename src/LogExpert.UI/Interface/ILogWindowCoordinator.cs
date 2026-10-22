using ColumnizerLib;

using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Entities;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Entities;

namespace LogExpert.UI.Interface;

/// <summary>
/// Coordinates workspace-level operations for LogWindow instances.
/// Replaces the concrete LogTabWindow reference that LogWindow previously held.
/// </summary>
internal interface ILogWindowCoordinator
{
    /// <summary>
    /// Resolves the appropriate highlight group using a 4-tier fallback chain:
    /// 1. File-mask regex match (if fileName is provided)<br></br>
    /// 2. Name match (if groupName is provided)<br></br>
    /// 3. First group in the list<br></br>
    /// 4. New empty group<br></br>
    /// Never returns null.
    /// </summary>
    HighlightGroup ResolveHighlightGroup (string? groupName, string? fileName);

    /// <summary>
    /// Raised after highlight settings have been changed (e.g., after settings dialog closes).
    /// Subscribers should re-resolve their highlight groups.
    /// </summary>
    event EventHandler HighlightSettingsChanged;

    /// <summary>
    /// Resolves the appropriate columnizer for the given file name.
    /// Honours the configured <see cref="LogExpert.Core.Config.ColumnizerSelectionPriority"/>.
    /// Cleans up stale history entries. Returns null when no source produced a match.
    /// </summary>
    ILogLineMemoryColumnizer? ResolveColumnizer (string fileName);

    /// <summary>
    /// Returns the mask-list columnizer that matches <paramref name="shortFileName"/>, ignoring history,
    /// persistence, and AutoPick. Used by the per-file load path when the user has opted in to
    /// <c>MaskOverridesPersistence</c>. Returns null when no mask matches or the matching entry's
    /// columnizer is not registered.
    /// </summary>
    ILogLineMemoryColumnizer? TryGetMaskColumnizer (string shortFileName);

    /// <summary>
    /// Shared search parameters across all tabs.
    /// All tabs read/write from the same instance.
    /// </summary>
    SearchParams SearchParams { get; }

    /// <summary>
    /// Creates a new filter result tab. Transitionally delegates to the main form.
    /// </summary>
    LogWindow AddFilterTab (FilterPipe pipe, string title, ILogLineMemoryColumnizer? preProcessColumnizer);

    /// <summary>
    /// Creates a new temporary file tab. Transitionally delegates to the main form.
    /// </summary>
    LogWindow AddTempFileTab (string fileName, string title);

    /// <summary>
    /// Scrolls all tabs (except sender) to the given timestamp and updates LED activity.
    /// </summary>
    void ScrollAllTabsToTimestamp (DateTime timestamp, LogWindow sender);

    /// <summary>
    /// Flashes the given window's tab LED to signal it was scrolled by time-sync.
    /// Same activity signal that <see cref="ScrollAllTabsToTimestamp"/> raises for the tabs it
    /// scrolls; selection-driven sync (via <c>TimeSyncList</c>) reports through here so both sync
    /// paths are visually identical. Safe to call from a worker thread — the LED service marshals
    /// icon updates to the UI thread itself.
    /// </summary>
    void IndicateTimeSyncActivity (LogWindow logWindow);

    /// <summary>
    /// Returns the list of all currently open log files.
    /// </summary>
    IList<WindowFileEntry> GetOpenFiles ();

    /// <summary>
    /// Activates the tab containing the specified LogWindow.
    /// </summary>
    void SelectTab (LogWindow logWindow);

    /// <summary>
    /// Notifies the LED indicator service about follow-tail state changes.
    /// </summary>
    void NotifyFollowTailChanged (LogWindow logWindow, bool isEnabled, bool offByTrigger);
}
