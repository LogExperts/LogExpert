using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Services.TabControllerService;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Interface;

/// <summary>
/// Controls the management of LogWindow tabs in the application.
/// Provides methods for adding, removing, activating, and navigating between log windows.
/// </summary>
internal interface ITabController : IDisposable
{
    /// <summary>
    /// Adds a new LogWindow to the controller.
    /// </summary>
    /// <param name="window">The LogWindow instance to add.</param>
    /// <param name="title">The title to display on the tab.</param>
    /// <param name="doNotAddToDockPanel">If true, the window is tracked but not added to the DockPanel.</param>
    void AddWindow (LogWindow window, string title, bool doNotAddToDockPanel = false);

    /// <summary>
    /// Removes a LogWindow from the controller without closing it.
    /// </summary>
    /// <param name="window">The LogWindow instance to remove.</param>
    void RemoveWindow (LogWindow window);

    /// <summary>
    /// Closes a LogWindow, optionally prompting for confirmation if there are unsaved changes.
    /// </summary>
    /// <param name="window">The LogWindow instance to close.</param>
    /// <param name="skipConfirmation">If true, closes without prompting for confirmation.</param>
    void CloseWindow (LogWindow window, bool skipConfirmation = false);

    /// <summary>
    /// Closes all LogWindows managed by the controller.
    /// </summary>
    void CloseAllWindows ();

    /// <summary>
    /// Closes all LogWindows except the specified window.
    /// </summary>
    /// <param name="window">The LogWindow to keep open.</param>
    void CloseAllExcept (LogWindow window);

    /// <summary>
    /// Activates and brings focus to the specified LogWindow.
    /// </summary>
    /// <param name="window">The LogWindow to activate.</param>
    void ActivateWindow (LogWindow window);

    /// <summary>
    /// Gets the currently active LogWindow.
    /// </summary>
    /// <returns>The active LogWindow, or null if no window is active.</returns>
    LogWindow GetActiveWindow ();

    /// <summary>
    /// Switches focus to the next LogWindow in the tab order.
    /// </summary>
    void SwitchToNextWindow ();

    /// <summary>
    /// Switches focus to the previous LogWindow in the tab order.
    /// </summary>
    void SwitchToPreviousWindow ();

    /// <summary>
    /// Resolves the LogWindow that follows <paramref name="current"/> in the tab order, without
    /// activating it.
    /// </summary>
    /// <param name="current">
    /// The window to move on from. Pass <c>null</c> to use the active window.
    /// </param>
    /// <returns>The next LogWindow, or <c>null</c> when no window is tracked.</returns>
    LogWindow GetNextWindow (LogWindow current = null);

    /// <summary>
    /// Resolves the LogWindow that precedes <paramref name="current"/> in the tab order, without
    /// activating it.
    /// </summary>
    /// <param name="current">
    /// The window to move back from. Pass <c>null</c> to use the active window.
    /// </param>
    /// <returns>The previous LogWindow, or <c>null</c> when no window is tracked.</returns>
    LogWindow GetPreviousWindow (LogWindow current = null);

    /// <summary>
    /// Finds a LogWindow by its associated file name.
    /// </summary>
    /// <param name="fileName">The file name to search for.</param>
    /// <returns>The LogWindow associated with the file name, or null if not found.</returns>
    LogWindow FindWindowByFileName (string fileName);

    /// <summary>
    /// Gets all LogWindows explicitly tracked by the controller.
    /// </summary>
    /// <returns>A read-only list of all tracked LogWindows.</returns>
    IReadOnlyList<LogWindow> GetAllWindows ();

    /// <summary>
    /// Gets all tracked LogWindows that are (or were) placed in the dock panel, in tracking order.
    /// Windows a session layout restore has not placed yet (<see cref="DockState.Unknown"/>) are
    /// excluded. Hidden windows are included.
    /// </summary>
    /// <remarks>
    /// This is the query for operations that must cover every window an ongoing session should
    /// keep — saving a session, closing every other tab — including the ones compact mode hides.
    /// </remarks>
    /// <returns>A read-only list of the placed LogWindows.</returns>
    IReadOnlyList<LogWindow> GetPlacedWindows ();

    /// <summary>
    /// Gets all LogWindow instances from the DockPanel's Contents collection.
    /// This returns windows that are currently displayed in the DockPanel,
    /// which may include windows not explicitly tracked by TabController
    /// (e.g., windows restored from layout serialization).
    /// </summary>
    /// <remarks>
    /// Only displayed windows are returned: a window hidden with <see cref="Control.Hide"/> drops
    /// out of the result. Use <see cref="GetPlacedWindows"/> when hidden windows must be included.
    /// </remarks>
    /// <returns>Read-only list of all displayed LogWindows in the DockPanel.</returns>
    IReadOnlyList<LogWindow> GetAllWindowsFromDockPanel ();

    /// <summary>
    /// Gets the total number of LogWindows managed by the controller.
    /// </summary>
    /// <returns>The count of tracked LogWindows.</returns>
    int GetWindowCount ();

    /// <summary>
    /// Checks if the specified LogWindow is managed by the controller.
    /// </summary>
    /// <param name="window">The LogWindow to check.</param>
    /// <returns>True if the window is tracked by the controller; otherwise, false.</returns>
    bool HasWindow (LogWindow window);

    /// <summary>
    /// Initializes the controller with the specified DockPanel for window management.
    /// </summary>
    /// <param name="dockPanel">The DockPanel to use for displaying LogWindows.</param>
    void InitializeDockPanel (DockPanel dockPanel);

    /// <summary>
    /// Occurs when a new LogWindow is added to the controller.
    /// </summary>
    event EventHandler<WindowAddedEventArgs> WindowAdded;

    /// <summary>
    /// Occurs when a LogWindow is removed from the controller.
    /// </summary>
    event EventHandler<WindowRemovedEventArgs> WindowRemoved;

    /// <summary>
    /// Occurs when a LogWindow is activated.
    /// </summary>
    event EventHandler<WindowActivatedEventArgs> WindowActivated;

    /// <summary>
    /// Occurs when a LogWindow is about to close.
    /// </summary>
    event EventHandler<WindowClosingEventArgs> WindowClosing;
}
