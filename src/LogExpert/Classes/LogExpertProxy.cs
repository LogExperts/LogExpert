using LogExpert.Controls.LogTabWindow;
using LogExpert.Interface;

using NLog;

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LogExpert.Classes
{
    internal class LogExpertProxy : ILogExpertProxy
    {
        #region Fields

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        [NonSerialized] private readonly List<LogTabWindow> _windowList = [];

        [NonSerialized] private LogTabWindow _firstLogTabWindow;

        [NonSerialized] private int _logWindowIndex = 1;

        #endregion

        #region cTor

        public LogExpertProxy(LogTabWindow logTabWindow)
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


        public delegate void LastWindowClosedEventHandler(object sender, EventArgs e);

        #endregion

        #region Events

        public event LastWindowClosedEventHandler LastWindowClosed;

        #endregion

        #region Public methods

        public void LoadFiles(string[] fileNames)
        {
            _logger.Info("Loading files into existing LogTabWindow");
            LogTabWindow logWin = _windowList[^1];
            _ = logWin.Invoke(new MethodInvoker(logWin.SetForeground));
            logWin.LoadFiles(fileNames);
        }

        public void NewWindow(string[] fileNames)
        {
            if (_firstLogTabWindow.IsDisposed)
            {
                _logger.Warn("first GUI thread window is disposed. Setting a new one.");
                // may occur if a window is closed because of unhandled exception.
                // Determine a new 'firstWindow'. If no window is left, start a new one.
                RemoveWindow(_firstLogTabWindow);
                if (_windowList.Count == 0)
                {
                    _logger.Info("No windows left. New created window will be the new 'first' GUI window");
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
                _ = _firstLogTabWindow.Invoke(new NewWindowFx(NewWindowWorker), new object[] { fileNames });
            }
        }

        public void NewWindowOrLockedWindow(string[] fileNames)
        {
            foreach (LogTabWindow logWin in _windowList)
            {
                if (LogTabWindow.StaticData.CurrentLockedMainWindow == logWin)
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
            _logger.Info("Creating new LogTabWindow");
            LogTabWindow logWin = new(fileNames?.Length > 0 ? fileNames : null, _logWindowIndex++, true)
            {
                LogExpertProxy = this
            };
            AddWindow(logWin);
            logWin.Show();
            logWin.Activate();
        }


        public void WindowClosed(LogTabWindow logWin)
        {
            RemoveWindow(logWin);
            if (_windowList.Count == 0)
            {
                _logger.Info("Last LogTabWindow was closed");
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

        private void AddWindow(LogTabWindow window)
        {
            _logger.Info("Adding window to list");
            _windowList.Add(window);
        }

        private void RemoveWindow(LogTabWindow window)
        {
            _logger.Info("Removing window from list");
            _ = _windowList.Remove(window);
        }

        #endregion

        protected void OnLastWindowClosed()
        {
            LastWindowClosed?.Invoke(this, new EventArgs());
        }

        private delegate void NewWindowFx(string[] fileNames);
    }
}