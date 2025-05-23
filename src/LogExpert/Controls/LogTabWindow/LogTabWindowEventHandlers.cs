using LogExpert.Config;
using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArgs;
using LogExpert.Dialogs;
using LogExpert.Entities.EventArgs;
using LogExpert.UI.Dialogs;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.Controls.LogTabWindow
{
    public partial class LogTabWindow
    {
        #region Events handler

        private void OnBookmarkWindowVisibleChanged(object sender, EventArgs e)
        {
            _firstBookmarkWindowShow = false;
        }

        private void OnLogTabWindowLoad(object sender, EventArgs e)
        {
            ApplySettings(ConfigManager.Settings, SettingsFlags.All);
            if (ConfigManager.Settings.isMaximized)
            {
                Bounds = ConfigManager.Settings.appBoundsFullscreen;
                WindowState = FormWindowState.Maximized;
                Bounds = ConfigManager.Settings.appBounds;
            }
            else
            {
                if (ConfigManager.Settings.appBounds.Right > 0)
                {
                    Bounds = ConfigManager.Settings.appBounds;
                }
            }

            if (ConfigManager.Settings.Preferences.openLastFiles && _startupFileNames == null)
            {
                List<string> tmpList = ObjectClone.Clone(ConfigManager.Settings.lastOpenFilesList);

                foreach (string name in tmpList)
                {
                    if (string.IsNullOrEmpty(name) == false)
                    {
                        AddFileTab(name, false, null, false, null);
                    }
                }
            }
            if (_startupFileNames != null)
            {
                LoadFiles(_startupFileNames, false);
            }
            _ledThread = new Thread(LedThreadProc);
            _ledThread.IsBackground = true;
            _ledThread.Start();

            FillHighlightComboBox();
            FillToolLauncherBar();
#if !DEBUG
            debugToolStripMenuItem.Visible = false;
#endif
        }

        private void OnLogTabWindowClosing(object sender, CancelEventArgs e)
        {
            try
            {
                _shouldStop = true;
                _statusLineEventHandle.Set();
                _statusLineEventWakeupHandle.Set();
                _ledThread.Join();

                IList<LogWindow.LogWindow> deleteLogWindowList = new List<LogWindow.LogWindow>();
                ConfigManager.Settings.alwaysOnTop = TopMost && ConfigManager.Settings.Preferences.allowOnlyOneInstance;
                SaveLastOpenFilesList();

                foreach (LogWindow.LogWindow logWindow in _logWindowList)
                {
                    deleteLogWindowList.Add(logWindow);
                }

                foreach (LogWindow.LogWindow logWindow in deleteLogWindowList)
                {
                    RemoveAndDisposeLogWindow(logWindow, true);
                }

                DestroyBookmarkWindow();

                ConfigManager.Instance.ConfigChanged -= OnConfigChanged;

                SaveWindowPosition();
                ConfigManager.Save(SettingsFlags.WindowPosition | SettingsFlags.FileHistory);
            }
            catch (Exception)
            {
                // ignore error (can occur then multipe instances are closed simultaneously or if the
                // window was not constructed completely because of errors)
            }
            finally
            {
                LogExpertProxy?.WindowClosed(this);
            }
        }

        private void OnStripMouseUp(object sender, MouseEventArgs e)
        {
            if (sender is ToolStripDropDown dropDown)
            {
                AddFileTab(dropDown.Text, false, null, false, null);
            }
        }

        private void OnHistoryItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ClickedItem.Text) == false)
            {
                AddFileTab(e.ClickedItem.Text, false, null, false, null);
            }
        }

        private void OnLogWindowDisposed(object sender, EventArgs e)
        {
            LogWindow.LogWindow logWindow = sender as LogWindow.LogWindow;

            if (sender == CurrentLogWindow)
            {
                ChangeCurrentLogWindow(null);
            }

            RemoveLogWindow(logWindow);

            logWindow.Tag = null;
        }

        private void OnExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            Close();
        }

        private void OnSelectFilterToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (CurrentLogWindow == null)
            {
                return;
            }

            CurrentLogWindow.ColumnizerCallbackObject.LineNum = CurrentLogWindow.GetCurrentLineNum();
            FilterSelectorForm form = new(PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers, CurrentLogWindow.CurrentColumnizer, CurrentLogWindow.ColumnizerCallbackObject);
            form.Owner = this;
            form.TopMost = TopMost;
            DialogResult res = form.ShowDialog();

            if (res == DialogResult.OK)
            {
                if (form.ApplyToAll)
                {
                    lock (_logWindowList)
                    {
                        foreach (LogWindow.LogWindow logWindow in _logWindowList)
                        {
                            if (logWindow.CurrentColumnizer.GetType() != form.SelectedColumnizer.GetType())
                            {
                                //logWindow.SetColumnizer(form.SelectedColumnizer);
                                SetColumnizerFx fx = logWindow.ForceColumnizer;
                                logWindow.Invoke(fx, form.SelectedColumnizer);
                                SetColumnizerHistoryEntry(logWindow.FileName, form.SelectedColumnizer);
                            }
                            else
                            {
                                if (form.IsConfigPressed)
                                {
                                    logWindow.ColumnizerConfigChanged();
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (CurrentLogWindow.CurrentColumnizer.GetType() != form.SelectedColumnizer.GetType())
                    {
                        SetColumnizerFx fx = CurrentLogWindow.ForceColumnizer;
                        CurrentLogWindow.Invoke(fx, form.SelectedColumnizer);
                        SetColumnizerHistoryEntry(CurrentLogWindow.FileName, form.SelectedColumnizer);
                    }

                    if (form.IsConfigPressed)
                    {
                        lock (_logWindowList)
                        {
                            foreach (LogWindow.LogWindow logWindow in _logWindowList)
                            {
                                if (logWindow.CurrentColumnizer.GetType() == form.SelectedColumnizer.GetType())
                                {
                                    logWindow.ColumnizerConfigChanged();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnGoToLineToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (CurrentLogWindow == null)
            {
                return;
            }

            GotoLineDialog dlg = new(this);
            DialogResult res = dlg.ShowDialog();
            if (res == DialogResult.OK)
            {
                int line = dlg.Line - 1;
                if (line >= 0)
                {
                    CurrentLogWindow.GotoLine(line);
                }
            }
        }

        private void OnHighlightingToolStripMenuItemClick(object sender, EventArgs e)
        {
            ShowHighlightSettingsDialog();
        }

        private void OnSearchToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenSearchDialog();
        }

        private void OnOpenToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenFileDialog();
        }

        private void OnLogTabWindowDragEnter(object sender, DragEventArgs e)
        {
#if DEBUG
            string[] formats = e.Data.GetFormats();
            string s = "Dragging something over LogExpert. Formats:  ";
            foreach (string format in formats)
            {
                s += format;
                s += " , ";
            }
            s = s.Substring(0, s.Length - 3);
            _logger.Info(s);
#endif
        }

        private void OnLogWindowDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void OnLogWindowDragDrop(object sender, DragEventArgs e)
        {
#if DEBUG
            string[] formats = e.Data.GetFormats();
            string s = "Dropped formats:  ";
            foreach (string format in formats)
            {
                s += format;
                s += " , ";
            }
            s = s[..^3];
            _logger.Debug(s);
#endif

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                object o = e.Data.GetData(DataFormats.FileDrop);
                if (o is string[] names)
                {
                    LoadFiles(names, (e.KeyState & 4) == 4); // (shift pressed?)
                    e.Effect = DragDropEffects.Copy;
                }
            }
        }

        private void OnTimeShiftToolStripMenuItemCheckStateChanged(object sender, EventArgs e)
        {
            if (!_skipEvents && CurrentLogWindow != null)
            {
                CurrentLogWindow.SetTimeshiftValue(timeshiftMenuTextBox.Text);
                timeshiftMenuTextBox.Enabled = timeshiftToolStripMenuItem.Checked;
                CurrentLogWindow.TimeshiftEnabled(timeshiftToolStripMenuItem.Checked,
                    timeshiftMenuTextBox.Text);
            }
        }

        private void OnAboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            AboutBox aboutBox = new();
            aboutBox.TopMost = TopMost;
            aboutBox.ShowDialog();
        }

        private void OnFilterToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.ToggleFilterPanel();
        }

        private void OnMultiFileToolStripMenuItemClick(object sender, EventArgs e)
        {
            ToggleMultiFile();
            fileToolStripMenuItem.HideDropDown();
        }

        private void OnGuiStateUpdate(object sender, GuiStateArgs e)
        {
            BeginInvoke(GuiStateUpdateWorker, e);
        }

        private void OnColumnizerChanged(object sender, ColumnizerEventArgs e)
        {
            _bookmarkWindow?.SetColumnizer(e.Columnizer);
        }

        private void OnBookmarkAdded(object sender, EventArgs e)
        {
            _bookmarkWindow.UpdateView();
        }

        private void OnBookmarkTextChanged(object sender, BookmarkEventArgs e)
        {
            _bookmarkWindow.BookmarkTextChanged(e.Bookmark);
        }

        private void OnBookmarkRemoved(object sender, EventArgs e)
        {
            _bookmarkWindow.UpdateView();
        }

        private void OnProgressBarUpdate(object sender, ProgressEventArgs e)
        {
            Invoke(ProgressBarUpdateWorker, e);
        }

        private void OnStatusLineEvent(object sender, StatusLineEventArgs e)
        {
            StatusLineEventWorker(e);
        }

        private void OnFollowTailCheckBoxClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.FollowTailChanged(checkBoxFollowTail.Checked, false);
        }

        private void OnLogTabWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W && e.Control)
            {
                CurrentLogWindow?.Close();
            }
            else if (e.KeyCode == Keys.Tab && e.Control)
            {
                SwitchTab(e.Shift);
            }
            else
            {
                CurrentLogWindow?.OnLogWindowKeyDown(sender, e);
            }
        }

        private void OnCloseFileToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.Close();
        }

        private void OnCellSelectModeToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.SetCellSelectionMode(cellSelectModeToolStripMenuItem.Checked);
        }

        private void OnCopyMarkedLinesIntoNewTabToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.CopyMarkedLinesToTab();
        }

        private void OnTimeShiftMenuTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (CurrentLogWindow == null)
            {
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                CurrentLogWindow.SetTimeshiftValue(timeshiftMenuTextBox.Text);
            }
        }

        private void OnAlwaysOnTopToolStripMenuItemClick(object sender, EventArgs e)
        {
            TopMost = alwaysOnTopToolStripMenuItem.Checked;
        }

        private void OnFileSizeChanged(object sender, LogEventArgs e)
        {
            if (sender.GetType().IsAssignableFrom(typeof(LogWindow.LogWindow)))
            {
                int diff = e.LineCount - e.PrevLineCount;
                if (diff < 0)
                {
                    return;
                }

                if (((LogWindow.LogWindow)sender).Tag is LogWindowData data)
                {
                    lock (data)
                    {
                        data.diffSum += diff;
                        if (data.diffSum > DIFF_MAX)
                        {
                            data.diffSum = DIFF_MAX;
                        }
                    }

                    //if (this.dockPanel.ActiveContent != null &&
                    //    this.dockPanel.ActiveContent != sender || data.tailState != 0)
                    if (CurrentLogWindow != null &&
                        CurrentLogWindow != sender || data.tailState != 0)
                    {
                        data.dirty = true;
                    }
                    Icon icon = GetIcon(diff, data);
                    BeginInvoke(new SetTabIconDelegate(SetTabIcon), (LogWindow.LogWindow)sender, icon);
                }
            }
        }

        private void OnLogWindowFileNotFound(object sender, EventArgs e)
        {
            Invoke(new FileNotFoundDelegate(FileNotFound), sender);
        }

        private void OnLogWindowFileRespawned(object sender, EventArgs e)
        {
            Invoke(new FileRespawnedDelegate(FileRespawned), sender);
        }

        private void OnLogWindowFilterListChanged(object sender, FilterListChangedEventArgs e)
        {
            lock (_logWindowList)
            {
                foreach (LogWindow.LogWindow logWindow in _logWindowList)
                {
                    if (logWindow != e.LogWindow)
                    {
                        logWindow.HandleChangedFilterList();
                    }
                }
            }
            ConfigManager.Save(SettingsFlags.FilterList);
        }

        private void OnLogWindowCurrentHighlightGroupChanged(object sender, CurrentHighlightGroupChangedEventArgs e)
        {
            OnHighlightSettingsChanged();
            ConfigManager.Settings.Preferences.HighlightGroupList = HighlightGroupList;
            ConfigManager.Save(SettingsFlags.HighlightSettings);
        }

        private void OnTailFollowed(object sender, EventArgs e)
        {
            if (dockPanel.ActiveContent == null)
            {
                return;
            }
            if (sender.GetType().IsAssignableFrom(typeof(LogWindow.LogWindow)))
            {
                if (dockPanel.ActiveContent == sender)
                {
                    LogWindowData data = ((LogWindow.LogWindow)sender).Tag as LogWindowData;
                    data.dirty = false;
                    Icon icon = GetIcon(data.diffSum, data);
                    BeginInvoke(new SetTabIconDelegate(SetTabIcon), (LogWindow.LogWindow)sender, icon);
                }
            }
        }

        private void OnLogWindowSyncModeChanged(object sender, SyncModeEventArgs e)
        {
            if (!Disposing)
            {
                LogWindowData data = ((LogWindow.LogWindow)sender).Tag as LogWindowData;
                data.syncMode = e.IsTimeSynced ? 1 : 0;
                Icon icon = GetIcon(data.diffSum, data);
                BeginInvoke(new SetTabIconDelegate(SetTabIcon), (LogWindow.LogWindow)sender, icon);
            }
            else
            {
                _logger.Warn("Received SyncModeChanged event while disposing. Event ignored.");
            }
        }

        private void OnToggleBookmarkToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.ToggleBookmark();
        }

        private void OnJumpToNextToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.JumpNextBookmark();
        }

        private void OnJumpToPrevToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.JumpPrevBookmark();
        }

        private void OnASCIIToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.ChangeEncoding(Encoding.ASCII);
        }

        private void OnANSIToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.ChangeEncoding(Encoding.Default);
        }

        private void OnUTF8ToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.ChangeEncoding(new UTF8Encoding(false));
        }

        private void OnUTF16ToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.ChangeEncoding(Encoding.Unicode);
        }

        private void OnISO88591ToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.ChangeEncoding(Encoding.GetEncoding("iso-8859-1"));
        }

        private void OnReloadToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (CurrentLogWindow != null)
            {
                LogWindowData data = CurrentLogWindow.Tag as LogWindowData;
                Icon icon = GetIcon(0, data);
                BeginInvoke(new SetTabIconDelegate(SetTabIcon), CurrentLogWindow, icon);
                CurrentLogWindow.Reload();
            }
        }

        private void OnSettingsToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenSettings(0);
        }

        private void OnDateTimeDragControlValueDragged(object sender, EventArgs e)
        {
            if (CurrentLogWindow != null)
            {
                //this.CurrentLogWindow.ScrollToTimestamp(this.dateTimeDragControl.DateTime);
            }
        }

        private void OnDateTimeDragControlValueChanged(object sender, EventArgs e)
        {
            CurrentLogWindow?.ScrollToTimestamp(dragControlDateTime.DateTime, true, true);
        }

        private void OnLogTabWindowDeactivate(object sender, EventArgs e)
        {
            CurrentLogWindow?.AppFocusLost();
        }

        private void OnLogTabWindowActivated(object sender, EventArgs e)
        {
            CurrentLogWindow?.AppFocusGained();
        }

        private void OnShowBookmarkListToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (_bookmarkWindow.Visible)
            {
                _bookmarkWindow.Hide();
            }
            else
            {
                // strange: on very first Show() now bookmarks are displayed. after a hide it will work.
                if (_firstBookmarkWindowShow)
                {
                    _bookmarkWindow.Show(dockPanel);
                    _bookmarkWindow.Hide();
                }

                _bookmarkWindow.Show(dockPanel);
            }
        }

        private void OnToolStripButtonOpenClick(object sender, EventArgs e)
        {
            OpenFileDialog();
        }

        private void OnToolStripButtonSearchClick(object sender, EventArgs e)
        {
            OpenSearchDialog();
        }

        private void OnToolStripButtonFilterClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.ToggleFilterPanel();
        }

        private void OnToolStripButtonBookmarkClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.ToggleBookmark();
        }

        private void OnToolStripButtonUpClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.JumpPrevBookmark();
        }

        private void OnToolStripButtonDownClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.JumpNextBookmark();
        }

        private void OnShowHelpToolStripMenuItemClick(object sender, EventArgs e)
        {
            Help.ShowHelp(this, "LogExpert.chm");
        }

        private void OnHideLineColumnToolStripMenuItemClick(object sender, EventArgs e)
        {
            ConfigManager.Settings.hideLineColumn = hideLineColumnToolStripMenuItem.Checked;
            lock (_logWindowList)
            {
                foreach (LogWindow.LogWindow logWin in _logWindowList)
                {
                    logWin.ShowLineColumn(!ConfigManager.Settings.hideLineColumn);
                }
            }
            _bookmarkWindow.LineColumnVisible = ConfigManager.Settings.hideLineColumn;
        }

        // ==================================================================
        // Tab context menu stuff
        // ==================================================================

        private void OnCloseThisTabToolStripMenuItemClick(object sender, EventArgs e)
        {
            (dockPanel.ActiveContent as LogWindow.LogWindow).Close();
        }

        private void OnCloseOtherTabsToolStripMenuItemClick(object sender, EventArgs e)
        {
            IList<Form> closeList = new List<Form>();
            lock (_logWindowList)
            {
                foreach (DockContent content in dockPanel.Contents)
                {
                    if (content != dockPanel.ActiveContent && content is LogWindow.LogWindow)
                    {
                        closeList.Add(content as Form);
                    }
                }
            }
            foreach (Form form in closeList)
            {
                form.Close();
            }
        }

        private void OnCloseAllTabsToolStripMenuItemClick(object sender, EventArgs e)
        {
            CloseAllTabs();
        }

        private void OnTabColorToolStripMenuItemClick(object sender, EventArgs e)
        {
            LogWindow.LogWindow logWindow = dockPanel.ActiveContent as LogWindow.LogWindow;

            LogWindowData data = logWindow.Tag as LogWindowData;

            if (data == null)
            {
                return;
            }

            ColorDialog dlg = new();
            dlg.Color = data.color;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                data.color = dlg.Color;
                SetTabColor(logWindow, data.color);
            }
            List<ColorEntry> delList = [];

            foreach (ColorEntry entry in ConfigManager.Settings.fileColors)
            {
                if (entry.FileName.ToLower().Equals(logWindow.FileName.ToLower()))
                {
                    delList.Add(entry);
                }
            }

            foreach (ColorEntry entry in delList)
            {
                ConfigManager.Settings.fileColors.Remove(entry);
            }
            ConfigManager.Settings.fileColors.Add(new ColorEntry(logWindow.FileName, dlg.Color));

            while (ConfigManager.Settings.fileColors.Count > MAX_COLOR_HISTORY)
            {
                ConfigManager.Settings.fileColors.RemoveAt(0);
            }
        }

        private void OnLogTabWindowSizeChanged(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                _wasMaximized = WindowState == FormWindowState.Maximized;
            }
        }

        private void OnSaveProjectToolStripMenuItemClick(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new();
            dlg.DefaultExt = "lxj";
            dlg.Filter = @"LogExpert session (*.lxj)|*.lxj";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string fileName = dlg.FileName;
                List<string> fileNames = [];

                lock (_logWindowList)
                {
                    foreach (DockContent content in dockPanel.Contents)
                    {
                        LogWindow.LogWindow logWindow = content as LogWindow.LogWindow;
                        string persistenceFileName = logWindow?.SavePersistenceData(true);
                        if (persistenceFileName != null)
                        {
                            fileNames.Add(persistenceFileName);
                        }
                    }
                }

                ProjectData projectData = new();
                projectData.memberList = fileNames;
                projectData.tabLayoutXml = SaveLayout();
                ProjectPersister.SaveProjectData(fileName, projectData);
            }
        }

        private void OnLoadProjectToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new();
            dlg.DefaultExt = "lxj";
            dlg.Filter = @"LogExpert sessions (*.lxj)|*.lxj";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string projectFileName = dlg.FileName;
                LoadProject(projectFileName, true);
            }
        }

        private void OnToolStripButtonBubblesClick(object sender, EventArgs e)
        {
            if (CurrentLogWindow != null)
            {
                CurrentLogWindow.ShowBookmarkBubbles = toolStripButtonBubbles.Checked;
            }
        }

        private void OnCopyPathToClipboardToolStripMenuItemClick(object sender, EventArgs e)
        {
            LogWindow.LogWindow logWindow = dockPanel.ActiveContent as LogWindow.LogWindow;
            Clipboard.SetText(logWindow.Title);
        }

        private void OnFindInExplorerToolStripMenuItemClick(object sender, EventArgs e)
        {
            LogWindow.LogWindow logWindow = dockPanel.ActiveContent as LogWindow.LogWindow;

            Process explorer = new();
            explorer.StartInfo.FileName = "explorer.exe";
            explorer.StartInfo.Arguments = "/e,/select," + logWindow.Title;
            explorer.StartInfo.UseShellExecute = false;
            explorer.Start();
        }

        private void OnExportBookmarksToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.ExportBookmarkList();
        }

        private void OnHighlightGroupsComboBoxDropDownClosed(object sender, EventArgs e)
        {
            ApplySelectedHighlightGroup();
        }

        private void OnHighlightGroupsComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySelectedHighlightGroup();
        }

        private void OnHighlightGroupsComboBoxMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ShowHighlightSettingsDialog();
            }
        }


        private void OnConfigChanged(object sender, ConfigChangedEventArgs e)
        {
            if (LogExpertProxy != null)
            {
                NotifySettingsChanged(null, e.Flags);
            }
        }

        private void OnDumpLogBufferInfoToolStripMenuItemClick(object sender, EventArgs e)
        {
#if DEBUG
            CurrentLogWindow?.DumpBufferInfo();
#endif
        }

        private void OnDumpBufferDiagnosticToolStripMenuItemClick(object sender, EventArgs e)
        {
#if DEBUG
            CurrentLogWindow?.DumpBufferDiagnostic();
#endif
        }

        private void OnRunGCToolStripMenuItemClick(object sender, EventArgs e)
        {
            RunGC();
        }

        private void OnGCInfoToolStripMenuItemClick(object sender, EventArgs e)
        {
            DumpGCInfo();
        }

        private void OnToolsToolStripMenuItemDropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Tag is ToolEntry tag)
            {
                ToolButtonClick(tag);
            }
        }

        private void OnExternalToolsToolStripItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolButtonClick(e.ClickedItem.Tag as ToolEntry);
        }

        private void OnConfigureToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenSettings(2);
        }

        private void OnThrowExceptionGUIThreadToolStripMenuItemClick(object sender, EventArgs e)
        {
            throw new Exception("This is a test exception thrown by the GUI thread");
        }

        private void OnThrowExceptionBackgroundThToolStripMenuItemClick(object sender, EventArgs e)
        {
            ExceptionFx fx = ThrowExceptionFx;
            fx.BeginInvoke(null, null);
        }

        private void OnThrowExceptionBackgroundThreadToolStripMenuItemClick(object sender, EventArgs e)
        {
            Thread thread = new(ThrowExceptionThreadFx);
            thread.IsBackground = true;
            thread.Start();
        }

        private void OnWarnToolStripMenuItemClick(object sender, EventArgs e)
        {
            //_logger.GetLogger().LogLevel = _logger.Level.WARN;
        }

        private void OnInfoToolStripMenuItemClick(object sender, EventArgs e)
        {
            //_logger.Get_logger().LogLevel = _logger.Level.INFO;
        }

        private void OnDebugToolStripMenuItemClick(object sender, EventArgs e)
        {
            //_logger.Get_logger().LogLevel = _logger.Level.DEBUG;
        }

        private void OnLogLevelToolStripMenuItemClick(object sender, EventArgs e)
        {
        }

        private void OnLogLevelToolStripMenuItemDropDownOpening(object sender, EventArgs e)
        {
            //warnToolStripMenuItem.Checked = _logger.Get_logger().LogLevel == _logger.Level.WARN;
            //infoToolStripMenuItem.Checked = _logger.Get_logger().LogLevel == _logger.Level.INFO;
            //debugToolStripMenuItem1.Checked = _logger.Get_logger().LogLevel == _logger.Level.DEBUG;
        }

        private void OnDisableWordHighlightModeToolStripMenuItemClick(object sender, EventArgs e)
        {
            DebugOptions.DisableWordHighlight = disableWordHighlightModeToolStripMenuItem.Checked;
            CurrentLogWindow?.RefreshAllGrids();
        }

        private void OnMultiFileMaskToolStripMenuItemClick(object sender, EventArgs e)
        {
            CurrentLogWindow?.ChangeMultifileMask();
        }

        private void OnMultiFileEnabledStripMenuItemClick(object sender, EventArgs e)
        {
            ToggleMultiFile();
        }

        private void OnLockInstanceToolStripMenuItemClick(object sender, EventArgs e)
        {
            StaticData.CurrentLockedMainWindow = lockInstanceToolStripMenuItem.Checked ? null : this;
        }

        private void OnOptionToolStripMenuItemDropDownOpening(object sender, EventArgs e)
        {
            lockInstanceToolStripMenuItem.Enabled = !ConfigManager.Settings.Preferences.allowOnlyOneInstance;
            lockInstanceToolStripMenuItem.Checked = StaticData.CurrentLockedMainWindow == this;
        }

        private void OnFileToolStripMenuItemDropDownOpening(object sender, EventArgs e)
        {
            newFromClipboardToolStripMenuItem.Enabled = Clipboard.ContainsText();
        }

        private void OnNewFromClipboardToolStripMenuItemClick(object sender, EventArgs e)
        {
            PasteFromClipboard();
        }

        private void OnOpenURIToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenUriDialog dlg = new()
            {
                UriHistory = ConfigManager.Settings.uriHistoryList
            };

            if (DialogResult.OK == dlg.ShowDialog())
            {
                if (dlg.Uri.Trim().Length > 0)
                {
                    ConfigManager.Settings.uriHistoryList = dlg.UriHistory;
                    ConfigManager.Save(SettingsFlags.FileHistory);
                    LoadFiles([dlg.Uri], false);
                }
            }
        }

        private void OnColumnFinderToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (CurrentLogWindow != null && !_skipEvents)
            {
                CurrentLogWindow.ToggleColumnFinder(columnFinderToolStripMenuItem.Checked, true);
            }
        }

        private void OnDockPanelActiveContentChanged(object sender, EventArgs e)
        {
            if (dockPanel.ActiveContent is LogWindow.LogWindow window)
            {
                CurrentLogWindow = window;
                CurrentLogWindow.LogWindowActivated();
                ConnectToolWindows(CurrentLogWindow);
            }
        }

        private void OnTabRenameToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (CurrentLogWindow != null)
            {
                TabRenameDialog dlg = new()
                {
                    TabName = CurrentLogWindow.Text
                };
                if (DialogResult.OK == dlg.ShowDialog())
                {
                    CurrentLogWindow.Text = dlg.TabName;
                }
                dlg.Dispose();
            }
        }

        #endregion
    }
}