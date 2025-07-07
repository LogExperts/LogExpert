using LogExpert.Config;
using LogExpert.Core.Interface;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Extensions.LogWindow;

using NLog;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace LogExpert.Classes;

internal class LogExpertProxy : ILogExpertProxy
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    [NonSerialized] private readonly List<ILogTabWindow> _windowList = [];

    [NonSerialized] private ILogTabWindow _firstLogTabWindow;

    [NonSerialized] private int _logWindowIndex = 1;

    #endregion

    #region cTor

    public LogExpertProxy(ILogTabWindow logTabWindow)
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

    public void LoadFiles(string[] fileNames)
    {
        _logger.Info(CultureInfo.InvariantCulture, "Loading files into existing LogTabWindow");
        ILogTabWindow logWin = _windowList[^1];
        _ = logWin.Invoke(new MethodInvoker(logWin.SetForeground));
        logWin.LoadFiles(fileNames);
    }

    public void NewWindow(string[] fileNames)
    {
        if (_firstLogTabWindow.IsDisposed)
        {
            _logger.Warn(CultureInfo.InvariantCulture, "first GUI thread window is disposed. Setting a new one.");
            // may occur if a window is closed because of unhandled exception.
            // Determine a new 'firstWindow'. If no window is left, start a new one.
            RemoveWindow(_firstLogTabWindow);
            if (_windowList.Count == 0)
            {
                _logger.Info(CultureInfo.InvariantCulture, "No windows left. New created window will be the new 'first' GUI window");
                LoadFiles(fileNames);
            }
            else
            {
                _firstLogTabWindow = _windowList[^1];
                NewWindow(fileNames);
            }
        }
        else
        {
            _ = _firstLogTabWindow.Invoke(new NewWindowFx(NewWindowWorker), [fileNames]);
        }
    }

    public void NewWindowOrLockedWindow(string[] fileNames)
    {
        foreach (var logWin in _windowList)
        {
            if (AbstractLogTabWindow.StaticData.CurrentLockedMainWindow == logWin)
            {
                _ = logWin.Invoke(new MethodInvoker(logWin.SetForeground));
                logWin.LoadFiles(fileNames);
                return;
            }
        }
        // No locked window was found --> create a new one
        NewWindow(fileNames);
    }


    public void NewWindowWorker(string[] fileNames)
    {
        _logger.Info(CultureInfo.InvariantCulture, "Creating new LogTabWindow");
        IConfigManager configManager = ConfigManager.Instance;
        ILogTabWindow logWin = AbstractLogTabWindow.Create(fileNames.Length > 0 ? fileNames : null, _logWindowIndex++, true, configManager);
        logWin.LogExpertProxy = this;
        AddWindow(logWin);
        logWin.Show();
        logWin.Activate();
    }


    public void WindowClosed(ILogTabWindow logWin)
    {
        RemoveWindow(logWin);
        if (_windowList.Count == 0)
        {
            _logger.Info(CultureInfo.InvariantCulture, "Last LogTabWindow was closed");
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

    public int GetLogWindowCount()
    {
        return _windowList.Count;
    }

    //public override object InitializeLifetimeService()
    //{
    //    return null;
    //}

    #endregion

    #region Private Methods

    private void AddWindow(ILogTabWindow window)
    {
        _logger.Info(CultureInfo.InvariantCulture, "Adding window to list");
        _windowList.Add(window);
    }

    private void RemoveWindow(ILogTabWindow window)
    {
        _logger.Info(CultureInfo.InvariantCulture, "Removing window from list");
        _ = _windowList.Remove(window);
    }

    #endregion

    protected void OnLastWindowClosed()
    {
        LastWindowClosed?.Invoke(this, new EventArgs());
    }

    private delegate void NewWindowFx(string[] fileNames);
}