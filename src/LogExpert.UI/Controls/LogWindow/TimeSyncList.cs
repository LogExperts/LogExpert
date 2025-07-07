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

    #region Delegates

    public delegate void WindowRemovedEventHandler (object sender, EventArgs e);

    #endregion

    #region Events

    public event WindowRemovedEventHandler WindowRemoved;

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
            logWindowList.Remove(logWindow);
        }

        OnWindowRemoved();
    }


    /// <summary>
    /// Scrolls all LogWindows to the given timestamp
    /// </summary>
    /// <param name="timestamp"></param>
    /// <param name="sender"></param>
    [SupportedOSPlatform("windows")]
    public void NavigateToTimestamp (DateTime timestamp, LogWindow sender)
    {
        CurrentTimestamp = timestamp;
        lock (logWindowList)
        {
            foreach (LogWindow logWindow in logWindowList)
            {
                if (sender != logWindow)
                {
                    logWindow.ScrollToTimestamp(timestamp, false, false);
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