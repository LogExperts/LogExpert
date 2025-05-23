using LogExpert.Classes;
using LogExpert.Config;
using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;
using LogExpert.Core.EventArgs;
using LogExpert.Dialogs;
using LogExpert.PluginRegistry.FileSystem;
using LogExpert.UI.Dialogs;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.Controls.LogTabWindow
{
    public partial class LogTabWindow
    {
        #region Private Methods

        /// <summary>
        /// Creates a temp file with the text content of the clipboard and opens the temp file in a new tab.
        /// </summary>
        private void PasteFromClipboard()
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText();
                string fileName = Path.GetTempFileName();

                using (FileStream fStream = new(fileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (StreamWriter writer = new(fStream, Encoding.Unicode))
                {
                    writer.Write(text);
                    writer.Close();
                }

                string title = "Clipboard";
                LogWindow.LogWindow logWindow = AddTempFileTab(fileName, title);
                LogWindowData data = logWindow.Tag as LogWindowData;
                if (data != null)
                {
                    SetTooltipText(logWindow, "Pasted on " + DateTime.Now);
                }
            }
        }

        private void InitToolWindows()
        {
            InitBookmarkWindow();
        }

        private void DestroyToolWindows()
        {
            DestroyBookmarkWindow();
        }

        private void InitBookmarkWindow()
        {
            _bookmarkWindow = new BookmarkWindow
            {
                HideOnClose = true,
                ShowHint = DockState.DockBottom
            };

            _bookmarkWindow.PreferencesChanged(ConfigManager.Settings.Preferences, false, SettingsFlags.All);
            _bookmarkWindow.VisibleChanged += OnBookmarkWindowVisibleChanged;
            _firstBookmarkWindowShow = true;
        }

        private void DestroyBookmarkWindow()
        {
            _bookmarkWindow.HideOnClose = false;
            _bookmarkWindow.Close();
        }

        private void SaveLastOpenFilesList()
        {
            ConfigManager.Settings.lastOpenFilesList.Clear();
            foreach (DockContent content in dockPanel.Contents)
            {
                if (content is LogWindow.LogWindow logWin)
                {
                    if (!logWin.IsTempFile)
                    {
                        ConfigManager.Settings.lastOpenFilesList.Add(logWin.GivenFileName);
                    }
                }
            }
        }

        private void SaveWindowPosition()
        {
            SuspendLayout();
            if (WindowState == FormWindowState.Normal)
            {
                ConfigManager.Settings.appBounds = Bounds;
                ConfigManager.Settings.isMaximized = false;
            }
            else
            {
                ConfigManager.Settings.appBoundsFullscreen = Bounds;
                ConfigManager.Settings.isMaximized = true;
                WindowState = FormWindowState.Normal;
                ConfigManager.Settings.appBounds = Bounds;
            }

            ResumeLayout();
        }

        private void SetTooltipText(LogWindow.LogWindow logWindow, string logFileName)
        {
            logWindow.ToolTipText = logFileName;
        }

        private void FillDefaultEncodingFromSettings(EncodingOptions encodingOptions)
        {
            if (ConfigManager.Settings.Preferences.defaultEncoding != null)
            {
                try
                {
                    encodingOptions.DefaultEncoding = Encoding.GetEncoding(ConfigManager.Settings.Preferences.defaultEncoding);
                }
                catch (ArgumentException)
                {
                    _logger.Warn("Encoding " + ConfigManager.Settings.Preferences.defaultEncoding + " is not a valid encoding");
                    encodingOptions.DefaultEncoding = null;
                }
            }
        }

        private void AddFileTabs(string[] fileNames)
        {
            foreach (string fileName in fileNames)
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    if (fileName.EndsWith(".lxj"))
                    {
                        LoadProject(fileName, false);
                    }
                    else
                    {
                        AddFileTab(fileName, false, null, false, null);
                    }
                }
            }

            Activate();
        }

        private void AddLogWindow(LogWindow.LogWindow logWindow, string title, bool doNotAddToPanel)
        {
            logWindow.CloseButton = true;
            logWindow.TabPageContextMenuStrip = tabContextMenuStrip;
            SetTooltipText(logWindow, title);
            logWindow.DockAreas = DockAreas.Document | DockAreas.Float;

            if (!doNotAddToPanel)
            {
                logWindow.Show(dockPanel);
            }

            LogWindowData data = new();
            data.diffSum = 0;
            logWindow.Tag = data;
            lock (_logWindowList)
            {
                _logWindowList.Add(logWindow);
            }

            logWindow.FileSizeChanged += OnFileSizeChanged;
            logWindow.TailFollowed += OnTailFollowed;
            logWindow.Disposed += OnLogWindowDisposed;
            logWindow.FileNotFound += OnLogWindowFileNotFound;
            logWindow.FileRespawned += OnLogWindowFileRespawned;
            logWindow.FilterListChanged += OnLogWindowFilterListChanged;
            logWindow.CurrentHighlightGroupChanged += OnLogWindowCurrentHighlightGroupChanged;
            logWindow.SyncModeChanged += OnLogWindowSyncModeChanged;

            logWindow.Visible = true;
        }

        private void DisconnectEventHandlers(LogWindow.LogWindow logWindow)
        {
            logWindow.FileSizeChanged -= OnFileSizeChanged;
            logWindow.TailFollowed -= OnTailFollowed;
            logWindow.Disposed -= OnLogWindowDisposed;
            logWindow.FileNotFound -= OnLogWindowFileNotFound;
            logWindow.FileRespawned -= OnLogWindowFileRespawned;
            logWindow.FilterListChanged -= OnLogWindowFilterListChanged;
            logWindow.CurrentHighlightGroupChanged -= OnLogWindowCurrentHighlightGroupChanged;
            logWindow.SyncModeChanged -= OnLogWindowSyncModeChanged;

            LogWindowData data = logWindow.Tag as LogWindowData;
            //data.tabPage.MouseClick -= tabPage_MouseClick;
            //data.tabPage.TabDoubleClick -= tabPage_TabDoubleClick;
            //data.tabPage.ContextMenuStrip = null;
            //data.tabPage = null;
        }

        private void AddToFileHistory(string fileName)
        {
            bool FindName(string s)
            {
                return s.ToLower().Equals(fileName.ToLower());
            }

            int index = ConfigManager.Settings.fileHistoryList.FindIndex(FindName);

            if (index != -1)
            {
                ConfigManager.Settings.fileHistoryList.RemoveAt(index);
            }

            ConfigManager.Settings.fileHistoryList.Insert(0, fileName);

            while (ConfigManager.Settings.fileHistoryList.Count > MAX_FILE_HISTORY)
            {
                ConfigManager.Settings.fileHistoryList.RemoveAt(ConfigManager.Settings.fileHistoryList.Count - 1);
            }

            ConfigManager.Save(SettingsFlags.FileHistory);

            FillHistoryMenu();
        }

        private LogWindow.LogWindow FindWindowForFile(string fileName)
        {
            lock (_logWindowList)
            {
                foreach (LogWindow.LogWindow logWindow in _logWindowList)
                {
                    if (logWindow.FileName.ToLower().Equals(fileName.ToLower()))
                    {
                        return logWindow;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the file name is a settings file. If so, the contained logfile name
        /// is returned. If not, the given file name is returned unchanged.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string FindFilenameForSettings(string fileName)
        {
            if (fileName.EndsWith(".lxp"))
            {
                PersistenceData persistenceData = Persister.LoadOptionsOnly(fileName);
                if (persistenceData == null)
                {
                    return fileName;
                }

                if (!string.IsNullOrEmpty(persistenceData.fileName))
                {
                    IFileSystemPlugin fs = PluginRegistry.PluginRegistry.Instance.FindFileSystemForUri(persistenceData.fileName);
                    if (fs != null && !fs.GetType().Equals(typeof(LocalFileSystem)))
                    {
                        return persistenceData.fileName;
                    }

                    // On relative paths the URI check (and therefore the file system plugin check) will fail.
                    // So fs == null and fs == LocalFileSystem are handled here like normal files.
                    if (Path.IsPathRooted(persistenceData.fileName))
                    {
                        return persistenceData.fileName;
                    }

                    // handle relative paths in .lxp files
                    string dir = Path.GetDirectoryName(fileName);
                    return Path.Combine(dir, persistenceData.fileName);
                }
            }

            return fileName;
        }

        private void FillHistoryMenu()
        {
            ToolStripDropDown strip = new ToolStripDropDownMenu();

            foreach (string file in ConfigManager.Settings.fileHistoryList)
            {
                ToolStripItem item = new ToolStripMenuItem(file);
                strip.Items.Add(item);
            }

            strip.ItemClicked += OnHistoryItemClicked;
            strip.MouseUp += OnStripMouseUp;
            lastUsedToolStripMenuItem.DropDown = strip;
        }

        private void RemoveLogWindow(LogWindow.LogWindow logWindow)
        {
            lock (_logWindowList)
            {
                _logWindowList.Remove(logWindow);
            }

            DisconnectEventHandlers(logWindow);
        }

        private void RemoveAndDisposeLogWindow(LogWindow.LogWindow logWindow, bool dontAsk)
        {
            if (CurrentLogWindow == logWindow)
            {
                ChangeCurrentLogWindow(null);
            }

            lock (_logWindowList)
            {
                _logWindowList.Remove(logWindow);
            }

            logWindow.Close(dontAsk);
        }

        private void ShowHighlightSettingsDialog()
        {
            HighlightDialog dlg = new()
            {
                KeywordActionList = PluginRegistry.PluginRegistry.Instance.RegisteredKeywordActions,
                Owner = this,
                TopMost = TopMost,
                HighlightGroupList = HighlightGroupList,
                PreSelectedGroupName = groupsComboBoxHighlightGroups.Text
            };

            DialogResult res = dlg.ShowDialog();

            if (res == DialogResult.OK)
            {
                HighlightGroupList = dlg.HighlightGroupList;
                FillHighlightComboBox();
                ConfigManager.Settings.Preferences.HighlightGroupList = HighlightGroupList;
                ConfigManager.Save(SettingsFlags.HighlightSettings);
                OnHighlightSettingsChanged();
            }
        }

        private void FillHighlightComboBox()
        {
            string currentGroupName = groupsComboBoxHighlightGroups.Text;
            groupsComboBoxHighlightGroups.Items.Clear();
            foreach (HighlightGroup group in HighlightGroupList)
            {
                groupsComboBoxHighlightGroups.Items.Add(group.GroupName);
                if (group.GroupName.Equals(currentGroupName))
                {
                    groupsComboBoxHighlightGroups.Text = group.GroupName;
                }
            }
        }

        private void OpenFileDialog()
        {
            OpenFileDialog openFileDialog = new();

            if (CurrentLogWindow != null)
            {
                FileInfo info = new(CurrentLogWindow.FileName);
                openFileDialog.InitialDirectory = info.DirectoryName;
            }
            else
            {
                if (!string.IsNullOrEmpty(ConfigManager.Settings.lastDirectory))
                {
                    openFileDialog.InitialDirectory = ConfigManager.Settings.lastDirectory;
                }
                else
                {
                    try
                    {
                        openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }
                    catch (SecurityException e)
                    {
                        _logger.Warn(e, "Insufficient rights for GetFolderPath(): ");
                        // no initial directory if insufficient rights
                    }
                }
            }

            openFileDialog.Multiselect = true;

            if (DialogResult.OK == openFileDialog.ShowDialog(this))
            {
                FileInfo info = new(openFileDialog.FileName);
                if (info.Directory.Exists)
                {
                    ConfigManager.Settings.lastDirectory = info.DirectoryName;
                    ConfigManager.Save(SettingsFlags.FileHistory);
                }

                if (info.Exists)
                {
                    LoadFiles(openFileDialog.FileNames, false);
                }
            }
        }

        private void LoadFiles(string[] names, bool invertLogic)
        {
            Array.Sort(names);

            if (names.Length == 1)
            {
                if (names[0].EndsWith(".lxj"))
                {
                    LoadProject(names[0], true);
                    return;
                }

                AddFileTab(names[0], false, null, false, null);
                return;
            }

            MultiFileOption option = ConfigManager.Settings.Preferences.multiFileOption;
            if (option == MultiFileOption.Ask)
            {
                MultiLoadRequestDialog dlg = new();
                DialogResult res = dlg.ShowDialog();

                if (res == DialogResult.Yes)
                {
                    option = MultiFileOption.SingleFiles;
                }
                else if (res == DialogResult.No)
                {
                    option = MultiFileOption.MultiFile;
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (invertLogic)
                {
                    if (option == MultiFileOption.SingleFiles)
                    {
                        option = MultiFileOption.MultiFile;
                    }
                    else
                    {
                        option = MultiFileOption.SingleFiles;
                    }
                }
            }

            if (option == MultiFileOption.SingleFiles)
            {
                AddFileTabs(names);
            }
            else
            {
                AddMultiFileTab(names);
            }
        }

        private void SetColumnizerHistoryEntry(string fileName, ILogLineColumnizer columnizer)
        {
            ColumnizerHistoryEntry entry = FindColumnizerHistoryEntry(fileName);
            if (entry != null)
            {
                ConfigManager.Settings.columnizerHistoryList.Remove(entry);
            }

            ConfigManager.Settings.columnizerHistoryList.Add(new ColumnizerHistoryEntry(fileName, columnizer.GetName()));

            if (ConfigManager.Settings.columnizerHistoryList.Count > MAX_COLUMNIZER_HISTORY)
            {
                ConfigManager.Settings.columnizerHistoryList.RemoveAt(0);
            }
        }

        private ColumnizerHistoryEntry FindColumnizerHistoryEntry(string fileName)
        {
            foreach (ColumnizerHistoryEntry entry in ConfigManager.Settings.columnizerHistoryList)
            {
                if (entry.FileName.Equals(fileName))
                {
                    return entry;
                }
            }

            return null;
        }

        private void ToggleMultiFile()
        {
            if (CurrentLogWindow != null)
            {
                CurrentLogWindow.SwitchMultiFile(!CurrentLogWindow.IsMultiFile);
                multiFileToolStripMenuItem.Checked = CurrentLogWindow.IsMultiFile;
                multiFileEnabledStripMenuItem.Checked = CurrentLogWindow.IsMultiFile;
            }
        }

        private void ChangeCurrentLogWindow(LogWindow.LogWindow newLogWindow)
        {
            if (newLogWindow == _currentLogWindow)
            {
                return; // do nothing if wishing to set the same window
            }

            LogWindow.LogWindow oldLogWindow = _currentLogWindow;
            _currentLogWindow = newLogWindow;
            string titleName = _showInstanceNumbers ? "LogExpert #" + _instanceNumber : "LogExpert";

            if (oldLogWindow != null)
            {
                oldLogWindow.StatusLineEvent -= OnStatusLineEvent;
                oldLogWindow.ProgressBarUpdate -= OnProgressBarUpdate;
                oldLogWindow.GuiStateUpdate -= OnGuiStateUpdate;
                oldLogWindow.ColumnizerChanged -= OnColumnizerChanged;
                oldLogWindow.BookmarkAdded -= OnBookmarkAdded;
                oldLogWindow.BookmarkRemoved -= OnBookmarkRemoved;
                oldLogWindow.BookmarkTextChanged -= OnBookmarkTextChanged;
                DisconnectToolWindows(oldLogWindow);
            }

            if (newLogWindow != null)
            {
                newLogWindow.StatusLineEvent += OnStatusLineEvent;
                newLogWindow.ProgressBarUpdate += OnProgressBarUpdate;
                newLogWindow.GuiStateUpdate += OnGuiStateUpdate;
                newLogWindow.ColumnizerChanged += OnColumnizerChanged;
                newLogWindow.BookmarkAdded += OnBookmarkAdded;
                newLogWindow.BookmarkRemoved += OnBookmarkRemoved;
                newLogWindow.BookmarkTextChanged += OnBookmarkTextChanged;

                if (newLogWindow.IsTempFile)
                {
                    Text = titleName + @" - " + newLogWindow.TempTitleName;
                }
                else
                {
                    Text = titleName + @" - " + newLogWindow.FileName;
                }

                multiFileToolStripMenuItem.Checked = CurrentLogWindow.IsMultiFile;
                multiFileToolStripMenuItem.Enabled = true;
                multiFileEnabledStripMenuItem.Checked = CurrentLogWindow.IsMultiFile;
                cellSelectModeToolStripMenuItem.Checked = true;
                cellSelectModeToolStripMenuItem.Enabled = true;
                closeFileToolStripMenuItem.Enabled = true;
                searchToolStripMenuItem.Enabled = true;
                filterToolStripMenuItem.Enabled = true;
                goToLineToolStripMenuItem.Enabled = true;
                //ConnectToolWindows(newLogWindow);
            }
            else
            {
                Text = titleName;
                multiFileToolStripMenuItem.Checked = false;
                multiFileEnabledStripMenuItem.Checked = false;
                checkBoxFollowTail.Checked = false;
                mainMenuStrip.Enabled = true;
                timeshiftToolStripMenuItem.Enabled = false;
                timeshiftToolStripMenuItem.Checked = false;
                timeshiftMenuTextBox.Text = "";
                timeshiftMenuTextBox.Enabled = false;
                multiFileToolStripMenuItem.Enabled = false;
                cellSelectModeToolStripMenuItem.Checked = false;
                cellSelectModeToolStripMenuItem.Enabled = false;
                closeFileToolStripMenuItem.Enabled = false;
                searchToolStripMenuItem.Enabled = false;
                filterToolStripMenuItem.Enabled = false;
                goToLineToolStripMenuItem.Enabled = false;
                dragControlDateTime.Visible = false;
            }
        }

        private void ConnectToolWindows(LogWindow.LogWindow logWindow)
        {
            ConnectBookmarkWindow(logWindow);
        }

        private void ConnectBookmarkWindow(LogWindow.LogWindow logWindow)
        {
            FileViewContext ctx = new(logWindow, logWindow);
            _bookmarkWindow.SetBookmarkData(logWindow.BookmarkData);
            _bookmarkWindow.SetCurrentFile(ctx);
        }

        private void DisconnectToolWindows(LogWindow.LogWindow logWindow)
        {
            DisconnectBookmarkWindow(logWindow);
        }

        private void DisconnectBookmarkWindow(LogWindow.LogWindow logWindow)
        {
            _bookmarkWindow.SetBookmarkData(null);
            _bookmarkWindow.SetCurrentFile(null);
        }

        private void GuiStateUpdateWorker(GuiStateArgs e)
        {
            _skipEvents = true;
            checkBoxFollowTail.Checked = e.FollowTail;
            mainMenuStrip.Enabled = e.MenuEnabled;
            timeshiftToolStripMenuItem.Enabled = e.TimeshiftPossible;
            timeshiftToolStripMenuItem.Checked = e.TimeshiftEnabled;
            timeshiftMenuTextBox.Text = e.TimeshiftText;
            timeshiftMenuTextBox.Enabled = e.TimeshiftEnabled;
            multiFileToolStripMenuItem.Enabled = e.MultiFileEnabled; // disabled for temp files
            multiFileToolStripMenuItem.Checked = e.IsMultiFileActive;
            multiFileEnabledStripMenuItem.Checked = e.IsMultiFileActive;
            cellSelectModeToolStripMenuItem.Checked = e.CellSelectMode;
            RefreshEncodingMenuBar(e.CurrentEncoding);

            if (e.TimeshiftPossible && ConfigManager.Settings.Preferences.timestampControl)
            {
                dragControlDateTime.MinDateTime = e.MinTimestamp;
                dragControlDateTime.MaxDateTime = e.MaxTimestamp;
                dragControlDateTime.DateTime = e.Timestamp;
                dragControlDateTime.Visible = true;
                dragControlDateTime.Enabled = true;
                dragControlDateTime.Refresh();
            }
            else
            {
                dragControlDateTime.Visible = false;
                dragControlDateTime.Enabled = false;
            }

            toolStripButtonBubbles.Checked = e.ShowBookmarkBubbles;
            groupsComboBoxHighlightGroups.Text = e.HighlightGroupName;
            columnFinderToolStripMenuItem.Checked = e.ColumnFinderVisible;

            _skipEvents = false;
        }

        private void ProgressBarUpdateWorker(ProgressEventArgs e)
        {
            if (e.Value <= e.MaxValue && e.Value >= e.MinValue)
            {
                try
                {
                    loadProgessBar.Minimum = e.MinValue;
                    loadProgessBar.Maximum = e.MaxValue;
                    loadProgessBar.Value = e.Value;
                    loadProgessBar.Visible = e.Visible;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error during ProgressBarUpdateWorker value {0}, min {1}, max {2}, visible {3}", e.Value, e.MinValue, e.MaxValue, e.Visible);
                }

                Invoke(new MethodInvoker(statusStrip.Refresh));
            }
        }

        private void StatusLineEventWorker(StatusLineEventArgs e)
        {
            if (e != null)
            {
                //_logger.logDebug("StatusLineEvent: text = " + e.StatusText);
                labelStatus.Text = e.StatusText;
                labelStatus.Size = TextRenderer.MeasureText(labelStatus.Text, labelStatus.Font);
                labelLines.Text = $" {e.LineCount} lines";
                labelLines.Size = TextRenderer.MeasureText(labelLines.Text, labelLines.Font);
                labelSize.Text = Util.GetFileSizeAsText(e.FileSize);
                labelSize.Size = TextRenderer.MeasureText(labelSize.Text, labelSize.Font);
                labelCurrentLine.Text = $"Line: {e.CurrentLineNum}";
                labelCurrentLine.Size = TextRenderer.MeasureText(labelCurrentLine.Text, labelCurrentLine.Font);
                if (statusStrip.InvokeRequired)
                {
                    statusStrip.BeginInvoke(new MethodInvoker(delegate { statusStrip.Refresh(); }));
                }
                else
                {
                    statusStrip.Refresh();
                }
            }
        }

        // tailState: 0,1,2 = on/off/off by Trigger
        // syncMode: 0 = normal (no), 1 = time synced
        private Icon CreateLedIcon(int level, bool dirty, int tailState, int syncMode)
        {
            Rectangle iconRect = _leds[0];
            iconRect.Height = 16; // (DockPanel's damn hardcoded height) // this.leds[this.leds.Length - 1].Bottom;
            iconRect.Width = iconRect.Right + 6;
            Bitmap bmp = new(iconRect.Width, iconRect.Height);
            Graphics gfx = Graphics.FromImage(bmp);

            int offsetFromTop = 4;

            for (int i = 0; i < _leds.Length; ++i)
            {
                Rectangle ledRect = _leds[i];
                ledRect.Offset(0, offsetFromTop);

                if (level >= _leds.Length - i)
                {
                    gfx.FillRectangle(_ledBrushes[i], ledRect);
                }
                else
                {
                    gfx.FillRectangle(_offLedBrush, ledRect);
                }
            }

            int ledSize = 3;
            int ledGap = 1;
            Rectangle lastLed = _leds[^1];
            Rectangle dirtyLed = new(lastLed.Right + 2, lastLed.Bottom - ledSize, ledSize, ledSize);
            Rectangle tailLed = new(dirtyLed.Location, dirtyLed.Size);
            tailLed.Offset(0, -(ledSize + ledGap));
            Rectangle syncLed = new(tailLed.Location, dirtyLed.Size);
            syncLed.Offset(0, -(ledSize + ledGap));

            syncLed.Offset(0, offsetFromTop);
            tailLed.Offset(0, offsetFromTop);
            dirtyLed.Offset(0, offsetFromTop);

            if (dirty)
            {
                gfx.FillRectangle(_dirtyLedBrush, dirtyLed);
            }
            else
            {
                gfx.FillRectangle(_offLedBrush, dirtyLed);
            }

            // tailMode 4 means: don't show
            if (tailState < 3)
            {
                gfx.FillRectangle(_tailLedBrush[tailState], tailLed);
            }

            if (syncMode == 1)
            {
                gfx.FillRectangle(_syncLedBrush, syncLed);
            }
            //else
            //{
            //  gfx.FillRectangle(this.offLedBrush, syncLed);
            //}

            // see http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=345656
            // GetHicon() creates an unmanaged handle which must be destroyed. The Clone() workaround creates
            // a managed copy of icon. then the unmanaged win32 handle is destroyed
            IntPtr iconHandle = bmp.GetHicon();
            Icon icon = Icon.FromHandle(iconHandle).Clone() as Icon;
            Win32.DestroyIcon(iconHandle);

            gfx.Dispose();
            bmp.Dispose();
            return icon;
        }

        private void CreateIcons()
        {
            for (int syncMode = 0; syncMode <= 1; syncMode++) // LED indicating time synced tabs
            {
                for (int tailMode = 0; tailMode < 4; tailMode++)
                {
                    for (int i = 0; i < 6; ++i)
                    {
                        _ledIcons[i, 0, tailMode, syncMode] = CreateLedIcon(i, false, tailMode, syncMode);
                    }

                    for (int i = 0; i < 6; ++i)
                    {
                        _ledIcons[i, 1, tailMode, syncMode] = CreateLedIcon(i, true, tailMode, syncMode);
                    }
                }
            }
        }

        private void FileNotFound(LogWindow.LogWindow logWin)
        {
            LogWindowData data = logWin.Tag as LogWindowData;
            BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWin, _deadIcon);
            dragControlDateTime.Visible = false;
        }

        private void FileRespawned(LogWindow.LogWindow logWin)
        {
            LogWindowData data = logWin.Tag as LogWindowData;
            Icon icon = GetIcon(0, data);
            BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWin, icon);
        }

        private void ShowLedPeak(LogWindow.LogWindow logWin)
        {
            LogWindowData data = logWin.Tag as LogWindowData;
            lock (data)
            {
                data.diffSum = DIFF_MAX;
            }

            Icon icon = GetIcon(data.diffSum, data);
            BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWin, icon);
        }

        private int GetLevelFromDiff(int diff)
        {
            if (diff > 60)
            {
                diff = 60;
            }

            int level = diff / 10;
            if (diff > 0 && level == 0)
            {
                level = 2;
            }
            else if (level == 0)
            {
                level = 1;
            }

            return level - 1;
        }

        private void LedThreadProc()
        {
            Thread.CurrentThread.Name = "LED Thread";
            while (!_shouldStop)
            {
                try
                {
                    Thread.Sleep(200);
                }
                catch
                {
                    return;
                }

                lock (_logWindowList)
                {
                    foreach (LogWindow.LogWindow logWindow in _logWindowList)
                    {
                        LogWindowData data = logWindow.Tag as LogWindowData;
                        if (data.diffSum > 0)
                        {
                            data.diffSum -= 10;
                            if (data.diffSum < 0)
                            {
                                data.diffSum = 0;
                            }

                            Icon icon = GetIcon(data.diffSum, data);
                            BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWindow, icon);
                        }
                    }
                }
            }
        }

        private void SetTabIcon(LogWindow.LogWindow logWindow, Icon icon)
        {
            if (logWindow != null)
            {
                logWindow.Icon = icon;
                logWindow.DockHandler.Pane?.TabStripControl.Invalidate(false);
            }
        }

        private Icon GetIcon(int diff, LogWindowData data)
        {
            Icon icon =
                _ledIcons[
                    GetLevelFromDiff(diff), data.dirty ? 1 : 0, Preferences.showTailState ? data.tailState : 3,
                    data.syncMode
                ];
            return icon;
        }

        private void RefreshEncodingMenuBar(Encoding encoding)
        {
            toolStripEncodingASCIIItem.Checked = false;
            toolStripEncodingANSIItem.Checked = false;
            toolStripEncodingUTF8Item.Checked = false;
            toolStripEncodingUTF16Item.Checked = false;
            toolStripEncodingISO88591Item.Checked = false;

            if (encoding == null)
            {
                return;
            }

            if (encoding is ASCIIEncoding)
            {
                toolStripEncodingASCIIItem.Checked = true;
            }
            else if (encoding.Equals(Encoding.Default))
            {
                toolStripEncodingANSIItem.Checked = true;
            }
            else if (encoding is UTF8Encoding)
            {
                toolStripEncodingUTF8Item.Checked = true;
            }
            else if (encoding is UnicodeEncoding)
            {
                toolStripEncodingUTF16Item.Checked = true;
            }
            else if (encoding.Equals(Encoding.GetEncoding("iso-8859-1")))
            {
                toolStripEncodingISO88591Item.Checked = true;
            }

            toolStripEncodingANSIItem.Text = Encoding.Default.HeaderName;
        }

        private void OpenSettings(int tabToOpen)
        {
            SettingsDialog dlg = new(ConfigManager.Settings.Preferences, this, tabToOpen);
            dlg.TopMost = TopMost;

            if (DialogResult.OK == dlg.ShowDialog())
            {
                ConfigManager.Settings.Preferences = dlg.Preferences;
                ConfigManager.Save(SettingsFlags.Settings);
                NotifyWindowsForChangedPrefs(SettingsFlags.Settings);
            }
        }

        private void NotifyWindowsForChangedPrefs(SettingsFlags flags)
        {
            _logger.Info("The preferences have changed");
            ApplySettings(ConfigManager.Settings, flags);

            lock (_logWindowList)
            {
                foreach (LogWindow.LogWindow logWindow in _logWindowList)
                {
                    logWindow.PreferencesChanged(ConfigManager.Settings.Preferences, false, flags);
                }
            }

            _bookmarkWindow.PreferencesChanged(ConfigManager.Settings.Preferences, false, flags);

            HighlightGroupList = ConfigManager.Settings.Preferences.HighlightGroupList;
            if ((flags & SettingsFlags.HighlightSettings) == SettingsFlags.HighlightSettings)
            {
                OnHighlightSettingsChanged();
            }
        }

        private void ApplySettings(Settings settings, SettingsFlags flags)
        {
            if ((flags & SettingsFlags.WindowPosition) == SettingsFlags.WindowPosition)
            {
                TopMost = alwaysOnTopToolStripMenuItem.Checked = settings.alwaysOnTop;
                dragControlDateTime.DragOrientation = settings.Preferences.timestampControlDragOrientation;
                hideLineColumnToolStripMenuItem.Checked = settings.hideLineColumn;
            }

            if ((flags & SettingsFlags.FileHistory) == SettingsFlags.FileHistory)
            {
                FillHistoryMenu();
            }

            if ((flags & SettingsFlags.GuiOrColors) == SettingsFlags.GuiOrColors)
            {
                SetTabIcons(settings.Preferences);
            }

            if ((flags & SettingsFlags.ToolSettings) == SettingsFlags.ToolSettings)
            {
                FillToolLauncherBar();
            }

            if ((flags & SettingsFlags.HighlightSettings) == SettingsFlags.HighlightSettings)
            {
                FillHighlightComboBox();
            }
        }

        private void SetTabIcons(Preferences preferences)
        {
            _tailLedBrush[0] = new SolidBrush(preferences.showTailColor);
            CreateIcons();
            lock (_logWindowList)
            {
                foreach (LogWindow.LogWindow logWindow in _logWindowList)
                {
                    LogWindowData data = logWindow.Tag as LogWindowData;
                    Icon icon = GetIcon(data.diffSum, data);
                    BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWindow, icon);
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private void SetToolIcon(ToolEntry entry, ToolStripItem item)
        {
            Icon icon = Win32.LoadIconFromExe(entry.iconFile, entry.iconIndex);
            if (icon != null)
            {
                item.Image = icon.ToBitmap();
                if (item is ToolStripMenuItem)
                {
                    item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                }
                else
                {
                    item.DisplayStyle = ToolStripItemDisplayStyle.Image;
                }

                DestroyIcon(icon.Handle);
                icon.Dispose();
            }

            if (!string.IsNullOrEmpty(entry.cmd))
            {
                item.ToolTipText = entry.name;
            }
        }

        private void ToolButtonClick(ToolEntry toolEntry)
        {
            if (string.IsNullOrEmpty(toolEntry.cmd))
            {
                //TODO TabIndex => To Enum
                OpenSettings(2);
                return;
            }

            if (CurrentLogWindow != null)
            {
                ILogLine line = CurrentLogWindow.GetCurrentLine();
                ILogFileInfo info = CurrentLogWindow.GetCurrentFileInfo();
                if (line != null && info != null)
                {
                    ArgParser parser = new(toolEntry.args);
                    string argLine = parser.BuildArgs(line, CurrentLogWindow.GetRealLineNum() + 1, info, this);
                    if (argLine != null)
                    {
                        StartTool(toolEntry.cmd, argLine, toolEntry.sysout, toolEntry.columnizerName,
                            toolEntry.workingDir);
                    }
                }
            }
        }

        private void StartTool(string cmd, string args, bool sysoutPipe, string columnizerName, string workingDir)
        {
            if (string.IsNullOrEmpty(cmd))
            {
                return;
            }

            Process process = new();
            ProcessStartInfo startInfo = new(cmd, args);
            if (!Util.IsNull(workingDir))
            {
                startInfo.WorkingDirectory = workingDir;
            }

            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;

            if (sysoutPipe)
            {
                ILogLineColumnizer columnizer = ColumnizerPicker.DecideColumnizerByName(columnizerName, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

                _logger.Info("Starting external tool with sysout redirection: {0} {1}", cmd, args);
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                //process.OutputDataReceived += pipe.DataReceivedEventHandler;
                try
                {
                    process.Start();
                }
                catch (Win32Exception e)
                {
                    _logger.Error(e);
                    MessageBox.Show(e.Message);
                    return;
                }

                SysoutPipe pipe = new(process.StandardOutput);

                LogWindow.LogWindow logWin = AddTempFileTab(pipe.FileName,
                    CurrentLogWindow.IsTempFile
                        ? CurrentLogWindow.TempTitleName
                        : Util.GetNameFromPath(CurrentLogWindow.FileName) + "->E");
                logWin.ForceColumnizer(columnizer);

                process.Exited += pipe.ProcessExitedEventHandler;
                //process.BeginOutputReadLine();
            }
            else
            {
                _logger.Info("Starting external tool: {0} {1}", cmd, args);

                try
                {
                    startInfo.UseShellExecute = false;
                    process.Start();
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                    MessageBox.Show(e.Message);
                }
            }
        }

        private void CloseAllTabs()
        {
            IList<Form> closeList = [];
            lock (_logWindowList)
            {
                foreach (DockContent content in dockPanel.Contents)
                {
                    if (content is LogWindow.LogWindow window)
                    {
                        closeList.Add(window);
                    }
                }
            }

            foreach (Form form in closeList)
            {
                form.Close();
            }
        }

        private void SetTabColor(LogWindow.LogWindow logWindow, Color color)
        {
            //tabPage.BackLowColor = color;
            //tabPage.BackLowColorDisabled = Color.FromArgb(255,
            //  Math.Max(0, color.R - 50),
            //  Math.Max(0, color.G - 50),
            //  Math.Max(0, color.B - 50)
            //  );
        }

        private void LoadProject(string projectFileName, bool restoreLayout)
        {
            ProjectData projectData = ProjectPersister.LoadProjectData(projectFileName);
            bool hasLayoutData = projectData.tabLayoutXml != null;

            if (hasLayoutData && restoreLayout && _logWindowList.Count > 0)
            {
                ProjectLoadDlg dlg = new();
                if (DialogResult.Cancel != dlg.ShowDialog())
                {
                    switch (dlg.ProjectLoadResult)
                    {
                        case ProjectLoadDlgResult.IgnoreLayout:
                            hasLayoutData = false;
                            break;
                        case ProjectLoadDlgResult.CloseTabs:
                            CloseAllTabs();
                            break;
                        case ProjectLoadDlgResult.NewWindow:
                            LogExpertProxy.NewWindow([projectFileName]);
                            return;
                    }
                }
            }

            if (projectData != null)
            {
                foreach (string fileName in projectData.memberList)
                {
                    if (hasLayoutData)
                    {
                        AddFileTabDeferred(fileName, false, null, true, null);
                    }
                    else
                    {
                        AddFileTab(fileName, false, null, true, null);
                    }
                }

                if (hasLayoutData && restoreLayout)
                {
                    // Re-creating tool (non-document) windows is needed because the DockPanel control would throw strange errors
                    DestroyToolWindows();
                    InitToolWindows();
                    RestoreLayout(projectData.tabLayoutXml);
                }
            }
        }

        private void ApplySelectedHighlightGroup()
        {
            string groupName = groupsComboBoxHighlightGroups.Text;
            CurrentLogWindow?.SetCurrentHighlightGroup(groupName);
        }

        private void FillToolLauncherBar()
        {
            char[] labels =
            {
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
                'U', 'V', 'W', 'X', 'Y', 'Z'
            };
            toolsToolStripMenuItem.DropDownItems.Clear();
            toolsToolStripMenuItem.DropDownItems.Add(configureToolStripMenuItem);
            toolsToolStripMenuItem.DropDownItems.Add(configureToolStripSeparator);
            externalToolsToolStrip.Items.Clear();
            int num = 0;
            externalToolsToolStrip.SuspendLayout();
            foreach (ToolEntry tool in Preferences.toolEntries)
            {
                if (tool.isFavourite)
                {
                    ToolStripButton button = new("" + labels[num % 26])
                    {
                        Alignment = ToolStripItemAlignment.Left,
                        Tag = tool
                    };

                    SetToolIcon(tool, button);
                    externalToolsToolStrip.Items.Add(button);
                }

                num++;
                ToolStripMenuItem menuItem = new(tool.name);
                menuItem.Tag = tool;
                SetToolIcon(tool, menuItem);
                toolsToolStripMenuItem.DropDownItems.Add(menuItem);
            }

            externalToolsToolStrip.ResumeLayout();

            externalToolsToolStrip.Visible = num > 0; // do not show bar if no tool uses it
        }

        private void RunGC()
        {
            _logger.Info("Running GC. Used mem before: {0:N0}", GC.GetTotalMemory(false));
            GC.Collect();
            _logger.Info("GC done.    Used mem after:  {0:N0}", GC.GetTotalMemory(true));
        }

        private void DumpGCInfo()
        {
            _logger.Info("-------- GC info -----------\r\nUsed mem: {0:N0}", GC.GetTotalMemory(false));
            for (int i = 0; i < GC.MaxGeneration; ++i)
            {
                _logger.Info("Generation {0} collect count: {1}", i, GC.CollectionCount(i));
            }

            _logger.Info("----------------------------");
        }

        private void ThrowExceptionFx()
        {
            throw new Exception("This is a test exception thrown by an async delegate");
        }

        private void ThrowExceptionThreadFx()
        {
            throw new Exception("This is a test exception thrown by a background thread");
        }

        private string SaveLayout()
        {
            using MemoryStream memStream = new(2000);
            using StreamReader r = new(memStream);
            dockPanel.SaveAsXml(memStream, Encoding.UTF8, true);

            memStream.Seek(0, SeekOrigin.Begin);
            string resultXml = r.ReadToEnd();

            r.Close();

            return resultXml;
        }

        private void RestoreLayout(string layoutXml)
        {
            using MemoryStream memStream = new(2000);
            using StreamWriter w = new(memStream);
            w.Write(layoutXml);
            w.Flush();

            memStream.Seek(0, SeekOrigin.Begin);

            dockPanel.LoadFromXml(memStream, DeserializeDockContent, true);
        }

        private IDockContent DeserializeDockContent(string persistString)
        {
            if (persistString.Equals(WindowTypes.BookmarkWindow.ToString()))
            {
                return _bookmarkWindow;
            }

            if (persistString.StartsWith(WindowTypes.LogWindow.ToString()))
            {
                string fileName = persistString.Substring(WindowTypes.LogWindow.ToString().Length + 1);
                LogWindow.LogWindow win = FindWindowForFile(fileName);
                if (win != null)
                {
                    return win;
                }

                _logger.Warn("Layout data contains non-existing LogWindow for {0}", fileName);
            }

            return null;
        }

        private void OnHighlightSettingsChanged()
        {
            HighlightSettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}