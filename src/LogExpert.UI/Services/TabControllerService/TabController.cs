using System.Runtime.Versioning;

using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Entities;
using LogExpert.UI.Interface;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Services.TabControllerService;

[SupportedOSPlatform("windows")]
internal class TabController : ITabController
{
    private DockPanel _dockPanel;
    private readonly Dictionary<LogWindow, LogWindowMetadata> _windows;
    private readonly Lock _windowsLock = new();
    private LogWindow _activeWindow;
    private bool _disposed;
    private bool _initialized;

    public event EventHandler<WindowAddedEventArgs> WindowAdded;
    public event EventHandler<WindowRemovedEventArgs> WindowRemoved;
    public event EventHandler<WindowActivatedEventArgs> WindowActivated;
    public event EventHandler<WindowClosingEventArgs> WindowClosing;

    /// <summary>
    /// Creates a new TabController instance
    /// </summary>
    /// <param name="dockPanel">The DockPanel to manage tabs in</param>
    public TabController (DockPanel dockPanel)
    {
        _dockPanel = dockPanel ?? throw new ArgumentNullException(nameof(dockPanel));
        _windows = [];
        _initialized = true;

        // Subscribe to DockPanel events
        _dockPanel.ActiveContentChanged += OnDockPanelActiveContentChanged;
    }

    /// <summary>
    /// Creates a new TabController instance without a DockPanel
    /// Use InitializeDockPanel to set the DockPanel later
    /// </summary>
    public TabController ()
    {
        _windows = [];
        _initialized = false;
    }

    #region DockPanel Integration

    /// <summary>
    /// Initializes the TabController with a DockPanel
    /// Use this when the DockPanel is not available at construction time
    /// </summary>
    /// <param name="dockPanel">The DockPanel to manage tabs in</param>
    /// <exception cref="ArgumentNullException">If dockPanel is null</exception>
    /// <exception cref="InvalidOperationException">If already initialized</exception>
    public void InitializeDockPanel (DockPanel dockPanel)
    {
        ArgumentNullException.ThrowIfNull(dockPanel, nameof(dockPanel));

        if (_initialized)
        {
            throw new InvalidOperationException(Resources.TabController_Error_Message_AlreadInitialized);
        }

        _dockPanel = dockPanel;
        _dockPanel.ActiveContentChanged += OnDockPanelActiveContentChanged;
        _initialized = true;
    }

    private void OnDockPanelActiveContentChanged (object sender, EventArgs e)
    {
        if (_dockPanel.ActiveContent is LogWindow newWindow)
        {
            var previousWindow = _activeWindow;
            _activeWindow = newWindow;

            WindowActivated?.Invoke(this, new WindowActivatedEventArgs(newWindow, previousWindow));
        }
    }

    #endregion

    #region Window Management

    /// <summary>
    /// Adds a new LogWindow to the tab system
    /// </summary>
    /// <param name="window">Window to add</param>
    /// <param name="title">Tab title</param>
    /// <param name="doNotAddToDockPanel">Skip adding to DockPanel (for deferred loading)</param>
    /// <exception cref="ArgumentNullException">If window is null</exception>
    /// <exception cref="InvalidOperationException">If window already tracked or not initialized</exception>
    public void AddWindow (LogWindow window, string title, bool doNotAddToDockPanel = false)
    {
        ArgumentNullException.ThrowIfNull(window, nameof(window));

        if (!_initialized)
        {
            throw new InvalidOperationException(Resources.TabController_Error_Message_NotInitialized);
        }

        lock (_windowsLock)
        {
            if (_windows.ContainsKey(window))
            {
                throw new InvalidOperationException(Resources.TabController_Error_Message_WindowAlreadyTracked);
            }

            var metadata = new LogWindowMetadata
            {
                Window = window,
                Title = title,
                FileName = window.FileName,
                CreatedAt = DateTime.Now,
                IsTempFile = window.IsTempFile,
                TabColor = Color.Gray
            };

            _windows.Add(window, metadata);
        }

        if (!doNotAddToDockPanel)
        {
            window.Show(_dockPanel);
        }

        // Subscribe to window events
        window.Disposed += OnWindowDisposed;
        window.Activated += OnWindowActivated;

        WindowAdded?.Invoke(this, new WindowAddedEventArgs(window));
    }

    /// <summary>
    /// Removes a window from tracking (does not close it)
    /// </summary>
    /// <param name="window">Window to remove</param>
    public void RemoveWindow (LogWindow window)
    {
        if (window == null)
        {
            return;
        }

        lock (_windowsLock)
        {
            if (!_windows.Remove(window))
            {
                return;
            }
        }

        window.Disposed -= OnWindowDisposed;
        window.Activated -= OnWindowActivated;

        if (_activeWindow == window)
        {
            _activeWindow = null;
        }

        WindowRemoved?.Invoke(this, new WindowRemovedEventArgs(window));
    }

    /// <summary>
    /// Closes a window with optional confirmation
    /// </summary>
    /// <param name="window">Window to close</param>
    /// <param name="skipConfirmation">Skip user confirmation dialog</param>
    public void CloseWindow (LogWindow window, bool skipConfirmation = false)
    {
        if (window == null)
        {
            return;
        }

        var windowClosingEventArgs = new WindowClosingEventArgs(window, skipConfirmation);
        WindowClosing?.Invoke(this, windowClosingEventArgs);

        if (windowClosingEventArgs.Cancel)
        {
            return;
        }

        if (!window.IsDisposed && window.IsHandleCreated)
        {
            window.Icon = null;
        }

        window.Close(skipConfirmation);
        // Note: RemoveWindow will be called by OnWindowDisposed event handler
    }

    /// <summary>
    /// Closes all tracked windows
    /// </summary>
    public void CloseAllWindows ()
    {
        // Create a copy to avoid collection modification during iteration
        var windowsToClose = GetAllWindows();

        foreach (var window in windowsToClose)
        {
            CloseWindow(window, skipConfirmation: true);
        }
    }

    /// <summary>
    /// Closes all windows except the specified one
    /// </summary>
    /// <param name="window">Window to keep open</param>
    public void CloseAllExcept (LogWindow window)
    {
        var windowsToClose = GetAllWindows()
            .Where(w => w != window)
            .ToList();

        foreach (var win in windowsToClose)
        {
            CloseWindow(win, skipConfirmation: false);
        }
    }

    #endregion

    #region Window Activation

    /// <summary>
    /// Activates (brings to front) the specified window
    /// </summary>
    /// <param name="window">Window to activate</param>
    public void ActivateWindow (LogWindow window)
    {
        if (window == null)
        {
            return;
        }

        lock (_windowsLock)
        {
            if (!_windows.ContainsKey(window))
            {
                return; // Window not tracked
            }
        }

        // Activate the window - this will trigger OnDockPanelActiveContentChanged
        window.Activate();
    }

    /// <summary>
    /// Gets the currently active window
    /// </summary>
    /// <returns>The active LogWindow, or null if none is active</returns>
    public LogWindow GetActiveWindow ()
    {
        return _activeWindow;
    }

    /// <summary>
    /// Switches to the next window in the tab order (Ctrl+Tab behavior)
    /// </summary>
    public void SwitchToNextWindow ()
    {
        lock (_windowsLock)
        {
            if (_windows.Count == 0)
            {
                return;
            }

            var windows = _windows.Keys.ToList();
            var currentIndex = _activeWindow != null
                ? windows.IndexOf(_activeWindow)
                : -1;

            // Move forward, wrap around to beginning if at end
            var nextIndex = (currentIndex + 1) % windows.Count;

            windows[nextIndex].Activate();
        }
    }

    /// <summary>
    /// Switches to the previous window in the tab order (Ctrl+Shift+Tab behavior)
    /// </summary>
    public void SwitchToPreviousWindow ()
    {
        lock (_windowsLock)
        {
            if (_windows.Count == 0)
            {
                return;
            }

            var windows = _windows.Keys.ToList();
            var currentIndex = _activeWindow != null
                ? windows.IndexOf(_activeWindow)
                : 0;

            // Move backward, wrap around to end if at beginning
            var previousIndex = currentIndex - 1;
            if (previousIndex < 0)
            {
                previousIndex = windows.Count - 1;
            }

            windows[previousIndex].Activate();
        }
    }

    /// <summary>
    /// Event handler for when a window is activated directly (not via DockPanel)
    /// </summary>
    private void OnWindowActivated (object sender, EventArgs e)
    {
        if (sender is LogWindow window)
        {
            var previousWindow = _activeWindow;

            // Only update and raise event if the window actually changed
            if (_activeWindow != window)
            {
                _activeWindow = window;
                WindowActivated?.Invoke(this, new WindowActivatedEventArgs(window, previousWindow));
            }
        }
    }

    #endregion

    #region Window Queries

    /// <summary>
    /// Finds a window by its file name (case-insensitive)
    /// </summary>
    /// <param name="fileName">File name to search for</param>
    /// <returns>The matching LogWindow, or null if not found</returns>
    public LogWindow FindWindowByFileName (string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        lock (_windowsLock)
        {
            return _windows
                .Where(kvp => kvp.Value.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets all tracked windows as a read-only list
    /// </summary>
    /// <returns>Read-only list of all LogWindows</returns>
    public IReadOnlyList<LogWindow> GetAllWindows ()
    {
        lock (_windowsLock)
        {
            return _windows.Keys.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the count of tracked windows
    /// </summary>
    /// <returns>Number of tracked windows</returns>
    public int GetWindowCount ()
    {
        lock (_windowsLock)
        {
            return _windows.Count;
        }
    }

    /// <summary>
    /// Checks if a window is currently being tracked
    /// </summary>
    /// <param name="window">Window to check</param>
    /// <returns>True if window is tracked, false otherwise</returns>
    public bool HasWindow (LogWindow window)
    {
        if (window == null)
        {
            return false;
        }

        lock (_windowsLock)
        {
            return _windows.ContainsKey(window);
        }
    }

    #endregion

    #region Event Handlers

    private void OnWindowDisposed (object sender, EventArgs e)
    {
        if (sender is LogWindow window)
        {
            RemoveWindow(window);
        }
    }

    #endregion

    #region Disposal

    public void Dispose ()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Unsubscribe from DockPanel
            if (_dockPanel != null)
            {
                _dockPanel.ActiveContentChanged -= OnDockPanelActiveContentChanged;
            }

            // Unsubscribe from all windows
            lock (_windowsLock)
            {
                foreach (var window in _windows.Keys)
                {
                    if (!window.IsDisposed && window.IsHandleCreated)
                    {
                        window.Icon = null;
                    }

                    window.Disposed -= OnWindowDisposed;
                    window.Activated -= OnWindowActivated;
                }

                _windows.Clear();
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// Gets all LogWindow instances currently held by the DockPanel's panes, in the order their tab strips display
    /// them. Panes of every dock state are enumerated, so floating windows are included.
    /// </summary>
    /// <returns>Read-only list of all LogWindows in the DockPanel</returns>
    public IReadOnlyList<LogWindow> GetAllWindowsFromDockPanel ()
    {
        if (!_initialized || _dockPanel == null)
        {
            return [];
        }

        var windows = new List<LogWindow>();

        foreach (DockPane pane in _dockPanel.Panes)
        {
            var displayingContents = pane.DisplayingContents;

            // Use 'for' instead of 'foreach': DisplayingContents exposes displayed tabs through Count and its indexer.
            // 'foreach' uses the inherited ReadOnlyCollection enumerator and does not return displayed tabs.
            for (int index = 0; index < displayingContents.Count; index++)
            {
                if (displayingContents[index] is LogWindow logWindow)
                {
                    windows.Add(logWindow);
                }
            }
        }

        return windows.AsReadOnly();
    }

    #endregion
}
