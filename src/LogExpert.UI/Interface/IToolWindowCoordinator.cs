using LogExpert.Core.Config;
using LogExpert.UI.Controls.LogWindow;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Interface;

/// <summary>
/// Coordinates tool window lifecycle and event relay for shared tool windows
/// (e.g., BookmarkWindow). Extracted from LogTabWindow to isolate tool-window
/// management concerns.
/// </summary>
internal interface IToolWindowCoordinator : IDisposable
{
    /// <summary>
    /// Creates the BookmarkWindow, wires initial events, applies preferences.
    /// </summary>
    void Initialize ();

    /// <summary>
    /// Destroys the BookmarkWindow and cleans up resources.
    /// </summary>
    void Destroy ();

    /// <summary>
    /// Connects the BookmarkWindow to the currently active LogWindow.
    /// Subscribes to bookmark and columnizer events for relay.
    /// </summary>
    void Connect (LogWindow logWindow);

    /// <summary>
    /// Disconnects the BookmarkWindow from the current LogWindow.
    /// Unsubscribes from all relayed events.
    /// </summary>
    void Disconnect ();

    /// <summary>
    /// Toggles BookmarkWindow visibility, handling the first-show DockPanel workaround.
    /// </summary>
    void ToggleBookmarkVisibility (DockPanel dockPanel);

    /// <summary>
    /// Returns the appropriate IDockContent for layout deserialization.
    /// Returns the BookmarkWindow if the persist string matches, null otherwise.
    /// </summary>
    IDockContent? GetDockContent (string persistString);

    /// <summary>
    /// Forwards preference changes to the BookmarkWindow.
    /// </summary>
    void ApplyPreferences (Font font, bool setLastColumnWidth, int lastColumnWidth, SettingsFlags flags);

    /// <summary>
    /// Sets the line column visibility on the BookmarkWindow.
    /// </summary>
    void SetLineColumnVisible (bool visible);
}