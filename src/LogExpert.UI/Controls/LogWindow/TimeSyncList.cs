using System.Runtime.Versioning;

namespace LogExpert.UI.Controls.LogWindow;

/// <summary>
/// Holds all windows which are in sync via timestamp
/// </summary>
internal class TimeSyncList
{
    #region Fields

    private readonly IList<LogWindow> logWindowList = [];

    #endregion

    #region Events

    public event EventHandler<EventArgs> WindowRemoved;

    #endregion

    #region Properties

    public DateTime CurrentTimestamp { get; set; }

    [SupportedOSPlatform("windows")]
    public int Count => logWindowList.Count;

    #endregion

    #region Public methods

    [SupportedOSPlatform("windows")]
    public void AddWindow (LogWindow logWindow)
    {
        lock (logWindowList)
        {
            if (!logWindowList.Contains(logWindow))
            {
                logWindowList.Add(logWindow);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    public void RemoveWindow (LogWindow logWindow)
    {
        lock (logWindowList)
        {
            _ = logWindowList.Remove(logWindow);
        }

        OnWindowRemoved();
    }


    /// <summary>
    /// Scrolls all LogWindows to the given timestamp
    /// </summary>
    /// <param name="timestamp"></param>
    /// <param name="sender"></param>
    /// <param name="onScrolled">
    /// Invoked for each window that accepted the scroll (LED signalling). When this is called from
    /// the sender's sync worker thread, a cross-thread scroll is dispatched via BeginInvoke and
    /// reports acceptance, not completion — the callback then covers every sync-group member,
    /// which is the intended "these tabs are moving" signal.
    /// </param>
    [SupportedOSPlatform("windows")]
    public void NavigateToTimestamp (DateTime timestamp, LogWindow sender, Action<LogWindow> onScrolled = null)
    {
        CurrentTimestamp = timestamp;
        lock (logWindowList)
        {
            foreach (var logWindow in logWindowList)
            {
                if (sender != logWindow && logWindow.ScrollToTimestamp(timestamp, false, false))
                {
                    onScrolled?.Invoke(logWindow);
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    public bool Contains (LogWindow logWindow)
    {
        return logWindowList.Contains(logWindow);
    }

    #endregion

    #region Private Methods

    private void OnWindowRemoved ()
    {
        WindowRemoved?.Invoke(this, new EventArgs());
    }

    #endregion
}