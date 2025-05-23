using LogExpert.Classes.Filter;
using LogExpert.Config;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Dialogs;
using LogExpert.Entities;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.Controls.LogTabWindow
{
    public partial class LogTabWindow
    {
        #region Public methods

        public LogWindow.LogWindow AddTempFileTab(string fileName, string title)
        {
            return AddFileTab(fileName, true, title, false, null);
        }

        public LogWindow.LogWindow AddFilterTab(FilterPipe pipe, string title, ILogLineColumnizer preProcessColumnizer)
        {
            LogWindow.LogWindow logWin = AddFileTab(pipe.FileName, true, title, false, preProcessColumnizer);
            if (pipe.FilterParams.SearchText.Length > 0)
            {
                ToolTip tip = new(components);
                tip.SetToolTip(logWin,
                    "Filter: \"" + pipe.FilterParams.SearchText + "\"" +
                    (pipe.FilterParams.IsInvert ? " (Invert match)" : "") +
                    (pipe.FilterParams.ColumnRestrict ? "\nColumn restrict" : "")
                );
                tip.AutomaticDelay = 10;
                tip.AutoPopDelay = 5000;
                LogWindowData data = logWin.Tag as LogWindowData;
                data.toolTip = tip;
            }

            return logWin;
        }

        public LogWindow.LogWindow AddFileTabDeferred(string givenFileName, bool isTempFile, string title, bool forcePersistenceLoading, ILogLineColumnizer preProcessColumnizer)
        {
            return AddFileTab(givenFileName, isTempFile, title, forcePersistenceLoading, preProcessColumnizer, true);
        }

        public LogWindow.LogWindow AddFileTab(string givenFileName, bool isTempFile, string title, bool forcePersistenceLoading, ILogLineColumnizer preProcessColumnizer, bool doNotAddToDockPanel = false)
        {
            string logFileName = FindFilenameForSettings(givenFileName);
            LogWindow.LogWindow win = FindWindowForFile(logFileName);
            if (win != null)
            {
                if (!isTempFile)
                {
                    AddToFileHistory(givenFileName);
                }

                SelectTab(win);
                return win;
            }

            EncodingOptions encodingOptions = new();
            FillDefaultEncodingFromSettings(encodingOptions);
            LogWindow.LogWindow logWindow = new(this, logFileName, isTempFile, forcePersistenceLoading);

            logWindow.GivenFileName = givenFileName;

            if (preProcessColumnizer != null)
            {
                logWindow.ForceColumnizerForLoading(preProcessColumnizer);
            }

            if (isTempFile)
            {
                logWindow.TempTitleName = title;
                encodingOptions.Encoding = new UnicodeEncoding(false, false);
            }

            AddLogWindow(logWindow, title, doNotAddToDockPanel);
            if (!isTempFile)
            {
                AddToFileHistory(givenFileName);
            }

            LogWindowData data = logWindow.Tag as LogWindowData;
            data.color = _defaultTabColor;
            SetTabColor(logWindow, _defaultTabColor);
            //data.tabPage.BorderColor = this.defaultTabBorderColor;
            if (!isTempFile)
            {
                foreach (ColorEntry colorEntry in ConfigManager.Settings.fileColors)
                {
                    if (colorEntry.FileName.ToLower().Equals(logFileName.ToLower()))
                    {
                        data.color = colorEntry.Color;
                        SetTabColor(logWindow, colorEntry.Color);
                        break;
                    }
                }
            }

            if (!isTempFile)
            {
                SetTooltipText(logWindow, logFileName);
            }

            if (givenFileName.EndsWith(".lxp"))
            {
                logWindow.ForcedPersistenceFileName = givenFileName;
            }

            // this.BeginInvoke(new LoadFileDelegate(logWindow.LoadFile), new object[] { logFileName, encoding });
            Task.Run(() => logWindow.LoadFile(logFileName, encodingOptions));
            return logWindow;
        }

        public LogWindow.LogWindow AddMultiFileTab(string[] fileNames)
        {
            if (fileNames.Length < 1)
            {
                return null;
            }

            LogWindow.LogWindow logWindow = new(this, fileNames[fileNames.Length - 1], false, false);
            AddLogWindow(logWindow, fileNames[fileNames.Length - 1], false);
            multiFileToolStripMenuItem.Checked = true;
            multiFileEnabledStripMenuItem.Checked = true;
            EncodingOptions encodingOptions = new();
            FillDefaultEncodingFromSettings(encodingOptions);
            BeginInvoke(new LoadMultiFilesDelegate(logWindow.LoadFilesAsMulti), fileNames, encodingOptions);
            AddToFileHistory(fileNames[0]);
            return logWindow;
        }

        public void LoadFiles(string[] fileNames)
        {
            Invoke(new AddFileTabsDelegate(AddFileTabs), [fileNames]);
        }

        public void OpenSearchDialog()
        {
            if (CurrentLogWindow == null)
            {
                return;
            }

            SearchDialog dlg = new();
            AddOwnedForm(dlg);
            dlg.TopMost = TopMost;
            SearchParams.HistoryList = ConfigManager.Settings.searchHistoryList;
            dlg.SearchParams = SearchParams;
            DialogResult res = dlg.ShowDialog();
            if (res == DialogResult.OK && dlg.SearchParams != null && !string.IsNullOrWhiteSpace(dlg.SearchParams.SearchText))
            {
                SearchParams = dlg.SearchParams;
                SearchParams.IsFindNext = false;
                CurrentLogWindow.StartSearch();
            }
        }

        public ILogLineColumnizer GetColumnizerHistoryEntry(string fileName)
        {
            ColumnizerHistoryEntry entry = FindColumnizerHistoryEntry(fileName);
            if (entry != null)
            {
                foreach (ILogLineColumnizer columnizer in PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers)
                {
                    if (columnizer.GetName().Equals(entry.ColumnizerName))
                    {
                        return columnizer;
                    }
                }

                ConfigManager.Settings.columnizerHistoryList.Remove(entry); // no valid name -> remove entry
            }

            return null;
        }

        public void SwitchTab(bool shiftPressed)
        {
            int index = dockPanel.Contents.IndexOf(dockPanel.ActiveContent);
            if (shiftPressed)
            {
                index--;
                if (index < 0)
                {
                    index = dockPanel.Contents.Count - 1;
                }

                if (index < 0)
                {
                    return;
                }
            }
            else
            {
                index++;
                if (index >= dockPanel.Contents.Count)
                {
                    index = 0;
                }
            }

            if (index < dockPanel.Contents.Count)
            {
                (dockPanel.Contents[index] as DockContent).Activate();
            }
        }

        public void ScrollAllTabsToTimestamp(DateTime timestamp, LogWindow.LogWindow senderWindow)
        {
            lock (_logWindowList)
            {
                foreach (LogWindow.LogWindow logWindow in _logWindowList)
                {
                    if (logWindow != senderWindow)
                    {
                        if (logWindow.ScrollToTimestamp(timestamp, false, false))
                        {
                            ShowLedPeak(logWindow);
                        }
                    }
                }
            }
        }

        public ILogLineColumnizer FindColumnizerByFileMask(string fileName)
        {
            foreach (ColumnizerMaskEntry entry in ConfigManager.Settings.Preferences.columnizerMaskList)
            {
                if (entry.mask != null)
                {
                    try
                    {
                        if (Regex.IsMatch(fileName, entry.mask))
                        {
                            ILogLineColumnizer columnizer = ColumnizerPicker.FindColumnizerByName(entry.columnizerName, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);
                            return columnizer;
                        }
                    }
                    catch (ArgumentException e)
                    {
                        _logger.Error(e, "RegEx-error while finding columnizer: ");
                        // occurs on invalid regex patterns
                    }
                }
            }

            return null;
        }

        public HighlightGroup FindHighlightGroupByFileMask(string fileName)
        {
            foreach (HighlightMaskEntry entry in ConfigManager.Settings.Preferences.highlightMaskList)
            {
                if (entry.mask != null)
                {
                    try
                    {
                        if (Regex.IsMatch(fileName, entry.mask))
                        {
                            HighlightGroup group = FindHighlightGroup(entry.highlightGroupName);
                            return group;
                        }
                    }
                    catch (ArgumentException e)
                    {
                        _logger.Error(e, "RegEx-error while finding columnizer: ");
                        // occurs on invalid regex patterns
                    }
                }
            }

            return null;
        }

        public void SelectTab(LogWindow.LogWindow logWindow)
        {
            logWindow.Activate();
        }

        public void SetForeground()
        {
            SetForegroundWindow(Handle);
            if (WindowState == FormWindowState.Minimized)
            {
                if (_wasMaximized)
                {
                    WindowState = FormWindowState.Maximized;
                }
                else
                {
                    WindowState = FormWindowState.Normal;
                }
            }
        }

        [DllImport("User32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);

        // called from LogWindow when follow tail was changed
        public void FollowTailChanged(LogWindow.LogWindow logWindow, bool isEnabled, bool offByTrigger)
        {
            LogWindowData data = logWindow.Tag as LogWindowData;
            if (data == null)
            {
                return;
            }

            if (isEnabled)
            {
                data.tailState = 0;
            }
            else
            {
                data.tailState = offByTrigger ? 2 : 1;
            }

            if (Preferences.showTailState)
            {
                Icon icon = GetIcon(data.diffSum, data);
                BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWindow, icon);
            }
        }

        public void NotifySettingsChanged(object sender, SettingsFlags flags)
        {
            if (sender != this)
            {
                NotifyWindowsForChangedPrefs(flags);
            }
        }

        public IList<WindowFileEntry> GetListOfOpenFiles()
        {
            IList<WindowFileEntry> list = new List<WindowFileEntry>();
            lock (_logWindowList)
            {
                foreach (LogWindow.LogWindow logWindow in _logWindowList)
                {
                    list.Add(new WindowFileEntry(logWindow));
                }
            }

            return list;
        }

        #endregion
    }
}