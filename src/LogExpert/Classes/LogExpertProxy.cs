using System.Runtime.Versioning;
using System.Windows.Forms;

using LogExpert.Configuration;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Extensions.LogWindow;

using NLog;

namespace LogExpert.Classes;

internal class LogExpertProxy : ILogExpertProxy
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    [NonSerialized] private readonly List<ILogTabWindow> _windowList = [];

    [NonSerialized] private ILogTabWindow _firstLogTabWindow;

    [NonSerialized] private ILogTabWindow _mostRecentActiveWindow; // Track most recently activated window

    [NonSerialized] private int _logWindowIndex = 1;

    #endregion

    #region cTor

    public LogExpertProxy (ILogTabWindow logTabWindow)
    {
        AddWindow(logTabWindow);
        logTabWindow.LogExpertProxy = this;
        _firstLogTabWindow = logTabWindow;
    }

    #endregion

    #region Delegates

    //public void BroadcastSettingsChanged(Object cookie)
    //{
    //  lock (this.windowList)
    //  {
    //    foreach (LogTabWindow logTabWindow in this.windowList)
    //    {
    //      logTabWindow.NotifySettingsChanged(cookie);
    //    }
    //  }
    //}

    #endregion

    #region Events

    public event EventHandler<EventArgs> LastWindowClosed;

    #endregion

    #region Public methods

    public void LoadFiles (string[] fileNames, int? targetLine = null)
    {
        // Use most recently ACTIVATED window, fallback to most recently created
        var logWin = _mostRecentActiveWindow ?? _windowList[^1];
        _logger.Info($"Loading files in {(_mostRecentActiveWindow != null ? "most recently activated" : "most recently created")} window");
        _ = logWin.Invoke(new MethodInvoker(logWin.SetForeground));
        logWin.LoadFiles(fileNames, targetLine);
    }

    /// <summary>
    /// Notifies the proxy that a window has been activated by the user.
    /// This is used to track which window should receive new files when "Allow Only One Instance" is enabled.
    /// </summary>
    /// <param name="window">The window that was activated</param>
    public void NotifyWindowActivated (ILogTabWindow window)
    {
        _mostRecentActiveWindow = window;
        _logger.Debug($"Most recent active window updated: {window}");
    }

    [SupportedOSPlatform("windows")]
    public void NewWindow (string[] fileNames, int? targetLine = null)
    {
        if (_firstLogTabWindow.IsDisposed)
        {
            _logger.Warn("### NewWindow: first GUI thread window is disposed. Setting a new one.");
            // may occur if a window is closed because of unhandled exception.
            // Determine a new 'firstWindow'. If no window is left, start a new one.
            RemoveWindow(_firstLogTabWindow);
            if (_windowList.Count == 0)
            {
                _logger.Info("### NewWindow: No windows left. New created window will be the new 'first' GUI window");
                LoadFiles(fileNames, targetLine);
            }
            else
            {
                _firstLogTabWindow = _windowList[^1];
                NewWindow(fileNames, targetLine);
            }
        }
        else
        {
            _ = _firstLogTabWindow.Invoke(new NewWindowFx(NewWindowWorker), [fileNames, targetLine]);
        }
    }

    [SupportedOSPlatform("windows")]
    public void NewWindowOrLockedWindow (string[] fileNames, int? targetLine = null)
    {
        // Lock Instance has priority
        // Check for locked window first
        foreach (var logWin in _windowList)
        {
            if (AbstractLogTabWindow.StaticData.CurrentLockedMainWindow == logWin)
            {
                _logger.Info("Loading files in locked window");
                _ = logWin.Invoke(new MethodInvoker(logWin.SetForeground));
                logWin.LoadFiles(fileNames, targetLine);
                return;
            }
        }

        // No locked window found
        // Load in most recent window (not new window)
        _logger.Info("No locked window, loading files in most recent window");
        LoadFiles(fileNames, targetLine); // Uses most recent window
    }

    [SupportedOSPlatform("windows")]
    public void NewWindowWorker (string[] fileNames, int? targetLine = null)
    {
        IConfigManager configManager = ConfigManager.Instance;
        var logWin = AbstractLogTabWindow.Create(fileNames.Length > 0
                                                    ? fileNames
                                                    : null,
                                                    _logWindowIndex++,
                                                    true,
                                                    configManager,
                                                    targetLine);
        logWin.LogExpertProxy = this;
        AddWindow(logWin);
        logWin.Show();
        logWin.Activate();
    }

    public void WindowClosed (ILogTabWindow logWin)
    {
        RemoveWindow(logWin);
        if (_windowList.Count == 0)
        {
            PluginRegistry.PluginRegistry.Instance.CleanupPlugins();
            OnLastWindowClosed();
        }
        else
        {
            if (_firstLogTabWindow == logWin)
            {
                // valid firstLogTabWindow is needed for the Invoke()-Calls in NewWindow()
                _firstLogTabWindow = _windowList[^1];
            }
        }
    }

    public int GetLogWindowCount ()
    {
        return _windowList.Count;
    }

    //public override object InitializeLifetimeService()
    //{
    //    return null;
    //}

    #endregion

    #region Private Methods

    private void AddWindow (ILogTabWindow window)
    {
        _windowList.Add(window);
    }

    private void RemoveWindow (ILogTabWindow window)
    {
        _ = _windowList.Remove(window);
    }

    #endregion

    protected void OnLastWindowClosed ()
    {
        LastWindowClosed?.Invoke(this, new EventArgs());
    }

    private delegate void NewWindowFx (string[] fileNames, int? targetLine = null);
}