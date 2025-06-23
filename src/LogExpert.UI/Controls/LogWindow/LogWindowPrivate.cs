using System.Globalization;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

using LogExpert.Core.Callback;
using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArguments;
using LogExpert.Dialogs;
using LogExpert.Extensions;
using LogExpert.UI.Entities;

namespace LogExpert.UI.Controls.LogWindow;

partial class LogWindow
{
    #region Private Methods

    [SupportedOSPlatform("windows")]
    private void RegisterLogFileReaderEvents ()
    {
        _logFileReader.LoadFile += OnLogFileReaderLoadFile;
        _logFileReader.LoadingFinished += OnLogFileReaderFinishedLoading;
        _logFileReader.LoadingStarted += OnLogFileReaderLoadingStarted;
        _logFileReader.FileNotFound += OnLogFileReaderFileNotFound;
        _logFileReader.Respawned += OnLogFileReaderRespawned;
        // FileSizeChanged is not registered here because it's registered after loading has finished
    }

    [SupportedOSPlatform("windows")]
    private void UnRegisterLogFileReaderEvents ()
    {
        if (_logFileReader != null)
        {
            _logFileReader.LoadFile -= OnLogFileReaderLoadFile;
            _logFileReader.LoadingFinished -= OnLogFileReaderFinishedLoading;
            _logFileReader.LoadingStarted -= OnLogFileReaderLoadingStarted;
            _logFileReader.FileNotFound -= OnLogFileReaderFileNotFound;
            _logFileReader.Respawned -= OnLogFileReaderRespawned;
            _logFileReader.FileSizeChanged -= OnFileSizeChanged;
        }
    }

    [SupportedOSPlatform("windows")]
    private void CreateDefaultViewStyle ()
    {
        DataGridViewCellStyle dataGridViewCellStyleMainGrid = new();
        DataGridViewCellStyle dataGridViewCellStyleFilterGrid = new();

        dataGridViewCellStyleMainGrid.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyleMainGrid.BackColor = SystemColors.Window;
        dataGridViewCellStyleMainGrid.Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        dataGridViewCellStyleMainGrid.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyleMainGrid.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyleMainGrid.SelectionForeColor = SystemColors.HighlightText;

        Color highlightColor = SystemColors.Highlight;
        //Color is smaller than 128, means its darker
        var isDark = (highlightColor.R * 0.2126) + (highlightColor.G * 0.7152) + (highlightColor.B * 0.0722) < 255 / 2;

        if (isDark)
        {
            dataGridViewCellStyleMainGrid.SelectionForeColor = Color.White;
        }
        else
        {
            dataGridViewCellStyleMainGrid.SelectionForeColor = Color.Black;

        }

        dataGridViewCellStyleMainGrid.WrapMode = DataGridViewTriState.False;
        dataGridView.DefaultCellStyle = dataGridViewCellStyleMainGrid;

        dataGridViewCellStyleFilterGrid.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyleFilterGrid.BackColor = SystemColors.Window;
        dataGridViewCellStyleFilterGrid.Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        dataGridViewCellStyleFilterGrid.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyleFilterGrid.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyleFilterGrid.SelectionForeColor = SystemColors.HighlightText;

        if (isDark)
        {
            dataGridViewCellStyleFilterGrid.SelectionForeColor = Color.White;
        }
        else
        {
            dataGridViewCellStyleFilterGrid.SelectionForeColor = Color.Black;
        }

        dataGridViewCellStyleFilterGrid.WrapMode = DataGridViewTriState.False;
        filterGridView.DefaultCellStyle = dataGridViewCellStyleFilterGrid;
    }

    [SupportedOSPlatform("windows")]
    private bool LoadPersistenceOptions ()
    {
        if (InvokeRequired)
        {
            return (bool)Invoke(new BoolReturnDelegate(LoadPersistenceOptions));
        }

        if (!Preferences.SaveSessions && ForcedPersistenceFileName == null)
        {
            return false;
        }

        try
        {
            PersistenceData persistenceData = ForcedPersistenceFileName == null
                ? Persister.LoadPersistenceDataOptionsOnly(FileName, Preferences)
                : Persister.LoadPersistenceDataOptionsOnlyFromFixedFile(ForcedPersistenceFileName);

            if (persistenceData == null)
            {
                _logger.Info($"No persistence data for {FileName} found.");
                return false;
            }

            IsMultiFile = persistenceData.MultiFile;
            _multiFileOptions = new MultiFileOptions
            {
                FormatPattern = persistenceData.MultiFilePattern,
                MaxDayTry = persistenceData.MultiFileMaxDays
            };

            if (string.IsNullOrEmpty(_multiFileOptions.FormatPattern))
            {
                _multiFileOptions = ObjectClone.Clone(Preferences.MultiFileOptions);
            }

            splitContainerLogWindow.SplitterDistance = persistenceData.FilterPosition;
            splitContainerLogWindow.Panel2Collapsed = !persistenceData.FilterVisible;
            ToggleHighlightPanel(persistenceData.FilterSaveListVisible);
            ShowAdvancedFilterPanel(persistenceData.FilterAdvanced);

            if (_reloadMemento == null)
            {
                PreselectColumnizer(persistenceData.ColumnizerName);
            }

            FollowTailChanged(persistenceData.FollowTail, false);
            if (persistenceData.TabName != null)
            {
                Text = persistenceData.TabName;
            }

            AdjustHighlightSplitterWidth();
            SetCurrentHighlightGroup(persistenceData.HighlightGroupName);

            if (persistenceData.MultiFileNames.Count > 0)
            {
                _logger.Info(CultureInfo.InvariantCulture, "Detected MultiFile name list in persistence options");
                _fileNames = new string[persistenceData.MultiFileNames.Count];
                persistenceData.MultiFileNames.CopyTo(_fileNames);
            }
            else
            {
                _fileNames = null;
            }

            //this.bookmarkWindow.ShowBookmarkCommentColumn = persistenceData.showBookmarkCommentColumn;
            SetExplicitEncoding(persistenceData.Encoding);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading persistence data: ");
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private void SetDefaultsFromPrefs ()
    {
        filterTailCheckBox.Checked = Preferences.FilterTail;
        syncFilterCheckBox.Checked = Preferences.FilterSync;
        FollowTailChanged(Preferences.FollowTail, false);
        _multiFileOptions = ObjectClone.Clone(Preferences.MultiFileOptions);
    }

    [SupportedOSPlatform("windows")]
    private void LoadPersistenceData ()
    {
        if (InvokeRequired)
        {
            Invoke(new MethodInvoker(LoadPersistenceData));
            return;
        }

        if (!Preferences.SaveSessions && !ForcePersistenceLoading && ForcedPersistenceFileName == null)
        {
            SetDefaultsFromPrefs();
            return;
        }

        if (IsTempFile)
        {
            SetDefaultsFromPrefs();
            return;
        }

        ForcePersistenceLoading = false; // force only 1 time (while session load)

        try
        {
            PersistenceData persistenceData = ForcedPersistenceFileName == null
                ? Persister.LoadPersistenceData(FileName, Preferences)
                : Persister.LoadPersistenceDataFromFixedFile(ForcedPersistenceFileName);

            if (persistenceData.LineCount > _logFileReader.LineCount)
            {
                // outdated persistence data (logfile rollover)
                // MessageBox.Show(this, "Persistence data for " + this.FileName + " is outdated. It was discarded.", "Log Expert");
                _logger.Info($"Persistence data for {FileName} is outdated. It was discarded.");
                _ = LoadPersistenceOptions();
                return;
            }

            _bookmarkProvider.SetBookmarks(persistenceData.BookmarkList);
            _rowHeightList = persistenceData.RowHeightList;
            try
            {
                if (persistenceData.CurrentLine >= 0 && persistenceData.CurrentLine < dataGridView.RowCount)
                {
                    SelectLine(persistenceData.CurrentLine, false, true);
                }
                else
                {
                    if (_logFileReader.LineCount > 0)
                    {
                        dataGridView.FirstDisplayedScrollingRowIndex = _logFileReader.LineCount - 1;
                        SelectLine(_logFileReader.LineCount - 1, false, true);
                    }
                }

                if (persistenceData.FirstDisplayedLine >= 0 &&
                    persistenceData.FirstDisplayedLine < dataGridView.RowCount)
                {
                    dataGridView.FirstDisplayedScrollingRowIndex = persistenceData.FirstDisplayedLine;
                }

                if (persistenceData.FollowTail)
                {
                    FollowTailChanged(persistenceData.FollowTail, false);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // FirstDisplayedScrollingRowIndex calculates sometimes the wrong scrolling ranges???
            }

            if (Preferences.SaveFilters)
            {
                RestoreFilters(persistenceData);
            }
        }
        catch (IOException ex)
        {
            SetDefaultsFromPrefs();
            _logger.Error(ex, "Error loading bookmarks: ");
        }
    }

    [SupportedOSPlatform("windows")]
    private void RestoreFilters (PersistenceData persistenceData)
    {
        if (persistenceData.FilterParamsList.Count > 0)
        {
            _filterParams = persistenceData.FilterParamsList[0];
            ReInitFilterParams(_filterParams);
        }

        ApplyFilterParams(); // re-loaded filter settingss
        BeginInvoke(new MethodInvoker(FilterSearch));
        try
        {
            splitContainerLogWindow.SplitterDistance = persistenceData.FilterPosition;
            splitContainerLogWindow.Panel2Collapsed = !persistenceData.FilterVisible;
        }
        catch (InvalidOperationException e)
        {
            _logger.Error(e, "Error setting splitter distance: ");
        }

        ShowAdvancedFilterPanel(persistenceData.FilterAdvanced);
        if (_filterPipeList.Count == 0) // don't restore if it's only a reload
        {
            RestoreFilterTabs(persistenceData);
        }
    }

    private void RestoreFilterTabs (PersistenceData persistenceData)
    {
        foreach (FilterTabData data in persistenceData.FilterTabDataList)
        {
            FilterParams persistFilterParams = data.FilterParams;
            ReInitFilterParams(persistFilterParams);
            List<int> filterResultList = [];
            //List<int> lastFilterResultList = new List<int>();
            List<int> filterHitList = [];
            Filter(persistFilterParams, filterResultList, _lastFilterLinesList, filterHitList);
            FilterPipe pipe = new(persistFilterParams.Clone(), this);
            WritePipeToTab(pipe, filterResultList, data.PersistenceData.TabName, data.PersistenceData);
        }
    }

    private void ReInitFilterParams (FilterParams filterParams)
    {
        filterParams.SearchText = filterParams.SearchText; // init "lowerSearchText"
        filterParams.RangeSearchText = filterParams.RangeSearchText; // init "lowerRangesearchText"
        filterParams.CurrentColumnizer = CurrentColumnizer;
        if (filterParams.IsRegex)
        {
            try
            {
                filterParams.CreateRegex();
            }
            catch (ArgumentException)
            {
                StatusLineError("Invalid regular expression");
            }
        }
    }

    private void EnterLoadFileStatus ()
    {
        _logger.Debug(CultureInfo.InvariantCulture, "EnterLoadFileStatus begin");

        if (InvokeRequired)
        {
            Invoke(new MethodInvoker(EnterLoadFileStatus));
            return;
        }

        _statusEventArgs.StatusText = "Loading file...";
        _statusEventArgs.LineCount = 0;
        _statusEventArgs.FileSize = 0;
        SendStatusLineUpdate();

        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = 0;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        _isLoading = true;
        _shouldCancel = true;
        ClearFilterList();
        ClearBookmarkList();
        dataGridView.ClearSelection();
        dataGridView.RowCount = 0;
        _logger.Debug(CultureInfo.InvariantCulture, "EnterLoadFileStatus end");
    }

    [SupportedOSPlatform("windows")]
    private void PositionAfterReload (ReloadMemento reloadMemento)
    {
        if (_reloadMemento.CurrentLine < dataGridView.RowCount && _reloadMemento.CurrentLine >= 0)
        {
            dataGridView.CurrentCell = dataGridView.Rows[_reloadMemento.CurrentLine].Cells[0];
        }

        if (_reloadMemento.FirstDisplayedLine < dataGridView.RowCount && _reloadMemento.FirstDisplayedLine >= 0)
        {
            dataGridView.FirstDisplayedScrollingRowIndex = _reloadMemento.FirstDisplayedLine;
        }
    }

    [SupportedOSPlatform("windows")]
    private void LogfileDead ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "File not found.");
        _isDeadFile = true;

        //this.logFileReader.FileSizeChanged -= this.FileSizeChangedHandler;
        //if (this.logFileReader != null)
        //  this.logFileReader.stopMonitoring();

        dataGridView.Enabled = false;
        dataGridView.RowCount = 0;
        _progressEventArgs.Visible = false;
        _progressEventArgs.Value = _progressEventArgs.MaxValue;
        SendProgressBarUpdate();
        _statusEventArgs.FileSize = 0;
        _statusEventArgs.LineCount = 0;
        _statusEventArgs.CurrentLineNum = 0;
        SendStatusLineUpdate();
        _shouldCancel = true;
        ClearFilterList();
        ClearBookmarkList();

        StatusLineText("File not found");
        OnFileNotFound(EventArgs.Empty);
    }

    [SupportedOSPlatform("windows")]
    private void LogfileRespawned ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "LogfileDead(): Reloading file because it has been respawned.");
        _isDeadFile = false;
        dataGridView.Enabled = true;
        StatusLineText("");
        OnFileRespawned(EventArgs.Empty);
        Reload();
    }

    [SupportedOSPlatform("windows")]
    private void SetGuiAfterLoading ()
    {
        if (Text.Length == 0)
        {
            Text = IsTempFile
                ? TempTitleName
                : Util.GetNameFromPath(FileName);
        }

        ShowBookmarkBubbles = Preferences.ShowBubbles;
        //if (this.forcedColumnizer == null)
        {
            ILogLineColumnizer columnizer;
            if (_forcedColumnizerForLoading != null)
            {
                columnizer = _forcedColumnizerForLoading;
                _forcedColumnizerForLoading = null;
            }
            else
            {
                columnizer = FindColumnizer();
                if (columnizer != null)
                {
                    if (_reloadMemento == null)
                    {
                        //TODO this needs to be refactored
                        var directory = ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir;

                        columnizer = ColumnizerPicker.CloneColumnizer(columnizer, directory);
                    }
                }
                else
                {
                    //TODO this needs to be refactored
                    var directory = ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir;

                    // Default Columnizers
                    columnizer = ColumnizerPicker.CloneColumnizer(ColumnizerPicker.FindColumnizer(FileName, _logFileReader, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers), directory);
                }
            }

            Invoke(new SetColumnizerFx(SetColumnizer), columnizer);
        }

        dataGridView.Enabled = true;
        DisplayCurrentFileOnStatusline();
        //this.guiStateArgs.FollowTail = this.Preferences.followTail;
        _guiStateArgs.MultiFileEnabled = !IsTempFile;
        _guiStateArgs.MenuEnabled = true;
        _guiStateArgs.CurrentEncoding = _logFileReader.CurrentEncoding;
        SendGuiStateUpdate();
        //if (this.dataGridView.RowCount > 0)
        //  SelectLine(this.dataGridView.RowCount - 1);
        //if (this.dataGridView.Columns.Count > 1)
        //{
        //  this.dataGridView.Columns[this.dataGridView.Columns.Count-1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        //  this.dataGridView.Columns[this.dataGridView.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
        //  AdjustMinimumGridWith();
        //}
        if (CurrentColumnizer.IsTimeshiftImplemented())
        {
            if (Preferences.TimestampControl)
            {
                SetTimestampLimits();
                SyncTimestampDisplay();
            }

            Settings settings = ConfigManager.Settings;
            ShowLineColumn(!settings.HideLineColumn);
        }

        ShowTimeSpread(Preferences.ShowTimeSpread && CurrentColumnizer.IsTimeshiftImplemented());
        locateLineInOriginalFileToolStripMenuItem.Enabled = FilterPipe != null;
    }

    private ILogLineColumnizer FindColumnizer ()
    {
        ILogLineColumnizer columnizer = Preferences.MaskPrio
            ? _parentLogTabWin.FindColumnizerByFileMask(Util.GetNameFromPath(FileName)) ?? _parentLogTabWin.GetColumnizerHistoryEntry(FileName)
            : _parentLogTabWin.GetColumnizerHistoryEntry(FileName) ?? _parentLogTabWin.FindColumnizerByFileMask(Util.GetNameFromPath(FileName));

        return columnizer;
    }

    private void ReloadNewFile ()
    {
        // prevent "overloads". May occur on very fast rollovers (next rollover before the file is reloaded)
        lock (_reloadLock)
        {
            _reloadOverloadCounter++;
            _logger.Info($"ReloadNewFile(): counter = {_reloadOverloadCounter}");
            if (_reloadOverloadCounter <= 1)
            {
                SavePersistenceData(false);
                _loadingFinishedEvent.Reset();
                _externaLoadingFinishedEvent.Reset();
                Thread reloadFinishedThread = new(ReloadFinishedThreadFx)
                {
                    IsBackground = true
                };
                reloadFinishedThread.Start();
                LoadFile(FileName, EncodingOptions);

                ClearBookmarkList();
                SavePersistenceData(false);

                //if (this.filterTailCheckBox.Checked)
                //{
                //  _logger.logDebug("Waiting for loading to be complete.");
                //  loadingFinishedEvent.WaitOne();
                //  _logger.logDebug("Refreshing filter view because of reload.");
                //  FilterSearch();
                //}
                //LoadFilterPipes();
            }
            else
            {
                _logger.Debug(CultureInfo.InvariantCulture, "Preventing reload because of recursive calls.");
            }

            _reloadOverloadCounter--;
        }
    }

    [SupportedOSPlatform("windows")]
    private void ReloadFinishedThreadFx ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "Waiting for loading to be complete.");
        _loadingFinishedEvent.WaitOne();
        _logger.Info(CultureInfo.InvariantCulture, "Refreshing filter view because of reload.");
        Invoke(new MethodInvoker(FilterSearch));
        LoadFilterPipes();
    }

    private void UpdateProgress (LoadFileEventArgs e)
    {
        try
        {
            if (e.ReadPos >= e.FileSize)
            {
                //_logger.Warn(CultureInfo.InvariantCulture, "UpdateProgress(): ReadPos (" + e.ReadPos + ") is greater than file size (" + e.FileSize + "). Aborting Update");
                return;
            }

            _statusEventArgs.FileSize = e.ReadPos;
            //this.progressEventArgs.Visible = true;
            _progressEventArgs.MaxValue = (int)e.FileSize;
            _progressEventArgs.Value = (int)e.ReadPos;
            SendProgressBarUpdate();
            SendStatusLineUpdate();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "UpdateProgress(): ");
        }
    }

    private void LoadingStarted (LoadFileEventArgs e)
    {
        try
        {
            _statusEventArgs.FileSize = e.ReadPos;
            _statusEventArgs.StatusText = "Loading " + Util.GetNameFromPath(e.FileName);
            _progressEventArgs.Visible = true;
            _progressEventArgs.MaxValue = (int)e.FileSize;
            _progressEventArgs.Value = (int)e.ReadPos;
            SendProgressBarUpdate();
            SendStatusLineUpdate();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "LoadingStarted(): ");
        }
    }

    private void LoadingFinished ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "File loading complete.");

        StatusLineText("");
        _logFileReader.FileSizeChanged += OnFileSizeChanged;
        _isLoading = false;
        _shouldCancel = false;
        dataGridView.SuspendLayout();
        dataGridView.RowCount = _logFileReader.LineCount;
        dataGridView.CurrentCellChanged += OnDataGridViewCurrentCellChanged;
        dataGridView.Enabled = true;
        dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        dataGridView.ResumeLayout();
        _progressEventArgs.Visible = false;
        _progressEventArgs.Value = _progressEventArgs.MaxValue;
        SendProgressBarUpdate();
        //if (this.logFileReader.LineCount > 0)
        //{
        //  this.dataGridView.FirstDisplayedScrollingRowIndex = this.logFileReader.LineCount - 1;
        //  SelectLine(this.logFileReader.LineCount - 1);
        //}
        _guiStateArgs.FollowTail = true;
        SendGuiStateUpdate();
        _statusEventArgs.LineCount = _logFileReader.LineCount;
        _statusEventArgs.FileSize = _logFileReader.FileSize;
        SendStatusLineUpdate();

        var setLastColumnWidth = _parentLogTabWin.Preferences.SetLastColumnWidth;
        var lastColumnWidth = _parentLogTabWin.Preferences.LastColumnWidth;
        var fontName = _parentLogTabWin.Preferences.FontName;
        var fontSize = _parentLogTabWin.Preferences.FontSize;

        PreferencesChanged(fontName, fontSize, setLastColumnWidth, lastColumnWidth, true, SettingsFlags.All);
        //LoadPersistenceData();
    }

    private void LogEventWorker ()
    {
        Thread.CurrentThread.Name = "LogEventWorker";
        while (true)
        {
            _logger.Debug(CultureInfo.InvariantCulture, "Waiting for signal");
            _logEventArgsEvent.WaitOne();
            _logger.Debug(CultureInfo.InvariantCulture, "Wakeup signal received.");
            while (true)
            {
                LogEventArgs e;
                var lastLineCount = 0;
                lock (_logEventArgsList)
                {
                    _logger.Info(CultureInfo.InvariantCulture, "{0} events in queue", _logEventArgsList.Count);
                    if (_logEventArgsList.Count == 0)
                    {
                        _logEventArgsEvent.Reset();
                        break;
                    }

                    e = _logEventArgsList[0];
                    _logEventArgsList.RemoveAt(0);
                }

                if (e.IsRollover)
                {
                    ShiftBookmarks(e.RolloverOffset);
                    ShiftRowHeightList(e.RolloverOffset);
                    ShiftFilterPipes(e.RolloverOffset);
                    lastLineCount = 0;
                }
                else
                {
                    if (e.LineCount < lastLineCount)
                    {
                        _logger.Error("Line count of event is: {0}, should be greater than last line count: {1}", e.LineCount, lastLineCount);
                    }
                }

                Invoke(UpdateGrid, [e]);
                CheckFilterAndHighlight(e);
                _timeSpreadCalc.SetLineCount(e.LineCount);
            }
        }
    }

    private void StopLogEventWorkerThread ()
    {
        _logEventArgsEvent.Set();
        cts.Cancel();
        //_logEventHandlerThread.Abort();
        //_logEventHandlerThread.Join();
    }

    private void OnFileSizeChanged (LogEventArgs e)
    {
        FileSizeChanged?.Invoke(this, e);
    }

    private void UpdateGrid (LogEventArgs e)
    {
        var oldRowCount = dataGridView.RowCount;
        var firstDisplayedLine = dataGridView.FirstDisplayedScrollingRowIndex;

        if (dataGridView.CurrentCellAddress.Y >= e.LineCount)
        {
            //this.dataGridView.Rows[this.dataGridView.CurrentCellAddress.Y].Selected = false;
            //this.dataGridView.CurrentCell = this.dataGridView.Rows[0].Cells[0];
        }

        try
        {
            if (dataGridView.RowCount > e.LineCount)
            {
                var currentLineNum = dataGridView.CurrentCellAddress.Y;
                dataGridView.RowCount = 0;
                dataGridView.RowCount = e.LineCount;
                if (_guiStateArgs.FollowTail == false)
                {
                    if (currentLineNum >= dataGridView.RowCount)
                    {
                        currentLineNum = dataGridView.RowCount - 1;
                    }

                    dataGridView.CurrentCell = dataGridView.Rows[currentLineNum].Cells[0];
                }
            }
            else
            {
                dataGridView.RowCount = e.LineCount;
            }

            _logger.Debug(CultureInfo.InvariantCulture, "UpdateGrid(): new RowCount={0}", dataGridView.RowCount);

            if (e.IsRollover)
            {
                // Multifile rollover
                // keep selection and view range, if no follow tail mode
                if (!_guiStateArgs.FollowTail)
                {
                    var currentLineNum = dataGridView.CurrentCellAddress.Y;
                    currentLineNum -= e.RolloverOffset;
                    if (currentLineNum < 0)
                    {
                        currentLineNum = 0;
                    }

                    _logger.Debug(CultureInfo.InvariantCulture, "UpdateGrid(): Rollover=true, Rollover offset={0}, currLineNum was {1}, new currLineNum={2}", e.RolloverOffset, dataGridView.CurrentCellAddress.Y, currentLineNum);
                    firstDisplayedLine -= e.RolloverOffset;
                    if (firstDisplayedLine < 0)
                    {
                        firstDisplayedLine = 0;
                    }

                    dataGridView.FirstDisplayedScrollingRowIndex = firstDisplayedLine;
                    dataGridView.CurrentCell = dataGridView.Rows[currentLineNum].Cells[0];
                    dataGridView.Rows[currentLineNum].Selected = true;
                }
            }

            _statusEventArgs.LineCount = e.LineCount;
            StatusLineFileSize(e.FileSize);

            if (!_isLoading)
            {
                if (oldRowCount == 0)
                {
                    AdjustMinimumGridWith();
                }

                //CheckFilterAndHighlight(e);
            }

            if (_guiStateArgs.FollowTail && dataGridView.RowCount > 0)
            {
                dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.RowCount - 1;
                OnTailFollowed(EventArgs.Empty);
            }

            if (Preferences.TimestampControl && !_isLoading)
            {
                SetTimestampLimits();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Fehler bei UpdateGrid(): ");
        }

        //this.dataGridView.Refresh();
        //this.dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
    }

    private void CheckFilterAndHighlight (LogEventArgs e)
    {
        var noLed = true;
        bool suppressLed;
        bool setBookmark;
        bool stopTail;
        string bookmarkComment;

        if (filterTailCheckBox.Checked || _filterPipeList.Count > 0)
        {
            var filterStart = e.PrevLineCount;
            if (e.IsRollover)
            {
                ShiftFilterLines(e.RolloverOffset);
                filterStart -= e.RolloverOffset;
            }

            var firstStopTail = true;
            ColumnizerCallback callback = new(this);
            var filterLineAdded = false;
            for (var i = filterStart; i < e.LineCount; ++i)
            {
                var line = _logFileReader.GetLogLine(i);
                //TODO: Why line can be equal null here? Need to understand all the situations and handle them correctlly. Prevent this from happening and replace the check with an exception throw
                if (line == null)
                {
                    return;
                }

                if (filterTailCheckBox.Checked)
                {
                    callback.SetLineNum(i);
                    if (Util.TestFilterCondition(_filterParams, line, callback))
                    {
                        //AddFilterLineFx addFx = new AddFilterLineFx(AddFilterLine);
                        //this.Invoke(addFx, new object[] { i, true });
                        filterLineAdded = true;
                        AddFilterLine(i, false, _filterParams, _filterResultList, _lastFilterLinesList,
                            _filterHitList);
                    }
                }

                //ProcessFilterPipeFx pipeFx = new ProcessFilterPipeFx(ProcessFilterPipes);
                //pipeFx.BeginInvoke(i, null, null);
                ProcessFilterPipes(i);

                IList<HighlightEntry> matchingList = FindMatchingHilightEntries(line);
                LaunchHighlightPlugins(matchingList, i);
                GetHilightActions(matchingList, out suppressLed, out stopTail, out setBookmark, out bookmarkComment);
                if (setBookmark)
                {
                    SetBookmarkFx fx = SetBookmarkFromTrigger;
                    fx.BeginInvoke(i, bookmarkComment, null, null);
                }

                if (stopTail && _guiStateArgs.FollowTail)
                {
                    var wasFollow = _guiStateArgs.FollowTail;
                    FollowTailChanged(false, true);
                    if (firstStopTail && wasFollow)
                    {
                        Invoke(new SelectLineFx(SelectAndEnsureVisible), [i, false]);
                        firstStopTail = false;
                    }
                }

                if (!suppressLed)
                {
                    noLed = false;
                }
            }

            if (filterLineAdded)
            {
                //AddFilterLineGuiUpdateFx addFx = new AddFilterLineGuiUpdateFx(AddFilterLineGuiUpdate);
                //this.Invoke(addFx);
                TriggerFilterLineGuiUpdate();
            }
        }
        else
        {
            var firstStopTail = true;
            var startLine = e.PrevLineCount;
            if (e.IsRollover)
            {
                ShiftFilterLines(e.RolloverOffset);
                startLine -= e.RolloverOffset;
            }

            for (var i = startLine; i < e.LineCount; ++i)
            {
                ILogLine line = _logFileReader.GetLogLine(i);
                if (line != null)
                {
                    IList<HighlightEntry> matchingList = FindMatchingHilightEntries(line);
                    LaunchHighlightPlugins(matchingList, i);
                    GetHilightActions(matchingList, out suppressLed, out stopTail, out setBookmark,
                        out bookmarkComment);
                    if (setBookmark)
                    {
                        SetBookmarkFx fx = SetBookmarkFromTrigger;
                        fx.BeginInvoke(i, bookmarkComment, null, null);
                    }

                    if (stopTail && _guiStateArgs.FollowTail)
                    {
                        var wasFollow = _guiStateArgs.FollowTail;
                        FollowTailChanged(false, true);
                        if (firstStopTail && wasFollow)
                        {
                            Invoke(new SelectLineFx(SelectAndEnsureVisible), [i, false]);
                            firstStopTail = false;
                        }
                    }

                    if (!suppressLed)
                    {
                        noLed = false;
                    }
                }
            }
        }

        if (!noLed)
        {
            OnFileSizeChanged(e);
        }
    }

    private void LaunchHighlightPlugins (IList<HighlightEntry> matchingList, int lineNum)
    {
        LogExpertCallback callback = new(this)
        {
            LineNum = lineNum
        };

        foreach (HighlightEntry entry in matchingList)
        {
            if (entry.IsActionEntry && entry.ActionEntry.PluginName != null)
            {
                IKeywordAction plugin = PluginRegistry.PluginRegistry.Instance.FindKeywordActionPluginByName(entry.ActionEntry.PluginName);
                if (plugin != null)
                {
                    ActionPluginExecuteFx fx = plugin.Execute;
                    fx.BeginInvoke(entry.SearchText, entry.ActionEntry.ActionParam, callback, CurrentColumnizer, null, null);
                }
            }
        }
    }

    private void PreSelectColumnizer (ILogLineColumnizer columnizer)
    {
        CurrentColumnizer = columnizer != null
            ? (_forcedColumnizerForLoading = columnizer)
            : (_forcedColumnizerForLoading = ColumnizerPicker.FindColumnizer(FileName, _logFileReader, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers));
    }

    private void SetColumnizer (ILogLineColumnizer columnizer)
    {
        columnizer = ColumnizerPicker.FindReplacementForAutoColumnizer(FileName, _logFileReader, columnizer, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

        var timeDiff = 0;
        if (CurrentColumnizer != null && CurrentColumnizer.IsTimeshiftImplemented())
        {
            timeDiff = CurrentColumnizer.GetTimeOffset();
        }

        SetColumnizerInternal(columnizer);

        if (CurrentColumnizer.IsTimeshiftImplemented())
        {
            CurrentColumnizer.SetTimeOffset(timeDiff);
        }
    }

    private void SetColumnizerInternal (ILogLineColumnizer columnizer)
    {
        _logger.Info(CultureInfo.InvariantCulture, "SetColumnizerInternal(): {0}", columnizer.GetName());

        ILogLineColumnizer oldColumnizer = CurrentColumnizer;
        var oldColumnizerIsXmlType = CurrentColumnizer is ILogLineXmlColumnizer;
        var oldColumnizerIsPreProcess = CurrentColumnizer is IPreProcessColumnizer;
        var mustReload = false;

        // Check if the filtered columns disappeared, if so must refresh the UI
        if (_filterParams.ColumnRestrict)
        {
            var newColumns = columnizer != null ? columnizer.GetColumnNames() : Array.Empty<string>();
            var colChanged = false;

            if (dataGridView.ColumnCount - 2 == newColumns.Length) // two first columns are 'marker' and 'line number'
            {
                for (var i = 0; i < newColumns.Length; i++)
                {
                    if (dataGridView.Columns[i].HeaderText != newColumns[i])
                    {
                        colChanged = true;
                        break; // one change is sufficient
                    }
                }
            }
            else
            {
                colChanged = true;
            }

            if (colChanged)
            {
                // Update UI
                columnNamesLabel.Text = CalculateColumnNames(_filterParams);
            }
        }

        Type oldColType = _filterParams.CurrentColumnizer?.GetType();
        Type newColType = columnizer?.GetType();

        if (oldColType != newColType && _filterParams.ColumnRestrict && _filterParams.IsFilterTail)
        {
            _filterParams.ColumnList.Clear();
        }

        if (CurrentColumnizer == null || CurrentColumnizer.GetType() != columnizer.GetType())
        {
            CurrentColumnizer = columnizer;
            _freezeStateMap.Clear();
            if (_logFileReader != null)
            {
                if (CurrentColumnizer is IPreProcessColumnizer)
                {
                    _logFileReader.PreProcessColumnizer = (IPreProcessColumnizer)CurrentColumnizer;
                }
                else
                {
                    _logFileReader.PreProcessColumnizer = null;
                }
            }

            // always reload when choosing XML columnizers
            if (_logFileReader != null && CurrentColumnizer is ILogLineXmlColumnizer)
            {
                //forcedColumnizer = currentColumnizer; // prevent Columnizer selection on SetGuiAfterReload()
                mustReload = true;
            }

            // Reload when choosing no XML columnizer but previous columnizer was XML
            if (_logFileReader != null && !(CurrentColumnizer is ILogLineXmlColumnizer) && oldColumnizerIsXmlType)
            {
                _logFileReader.IsXmlMode = false;
                //forcedColumnizer = currentColumnizer; // prevent Columnizer selection on SetGuiAfterReload()
                mustReload = true;
            }

            // Reload when previous columnizer was PreProcess and current is not, and vice versa.
            // When the current columnizer is a preProcess columnizer, reload in every case.
            if (CurrentColumnizer is IPreProcessColumnizer != oldColumnizerIsPreProcess ||
                CurrentColumnizer is IPreProcessColumnizer)
            {
                //forcedColumnizer = currentColumnizer; // prevent Columnizer selection on SetGuiAfterReload()
                mustReload = true;
            }
        }
        else
        {
            CurrentColumnizer = columnizer;
        }

        (oldColumnizer as IInitColumnizer)?.DeSelected(new ColumnizerCallback(this));

        (columnizer as IInitColumnizer)?.Selected(new ColumnizerCallback(this));

        SetColumnizer(columnizer, dataGridView);
        SetColumnizer(columnizer, filterGridView);
        _patternWindow?.SetColumnizer(columnizer);

        _guiStateArgs.TimeshiftPossible = columnizer.IsTimeshiftImplemented();
        SendGuiStateUpdate();

        if (_logFileReader != null)
        {
            dataGridView.RowCount = _logFileReader.LineCount;
        }

        if (_filterResultList != null)
        {
            filterGridView.RowCount = _filterResultList.Count;
        }

        if (mustReload)
        {
            Reload();
        }
        else
        {
            if (CurrentColumnizer.IsTimeshiftImplemented())
            {
                SetTimestampLimits();
                SyncTimestampDisplay();
            }

            Settings settings = ConfigManager.Settings;
            ShowLineColumn(!settings.HideLineColumn);
            ShowTimeSpread(Preferences.ShowTimeSpread && columnizer.IsTimeshiftImplemented());
        }

        if (!columnizer.IsTimeshiftImplemented() && IsTimeSynced)
        {
            FreeFromTimeSync();
        }

        columnComboBox.Items.Clear();

        foreach (var columnName in columnizer.GetColumnNames())
        {
            columnComboBox.Items.Add(columnName);
        }

        columnComboBox.SelectedIndex = 0;

        OnColumnizerChanged(CurrentColumnizer);
    }

    private void AutoResizeColumns (BufferedDataGridView gridView)
    {
        try
        {
            gridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            if (gridView.Columns.Count > 1 && Preferences.SetLastColumnWidth &&
                gridView.Columns[gridView.Columns.Count - 1].Width < Preferences.LastColumnWidth
            )
            {
                // It seems that using 'MinimumWidth' instead of 'Width' prevents the DataGridView's NullReferenceExceptions
                //gridView.Columns[gridView.Columns.Count - 1].Width = this.Preferences.lastColumnWidth;
                gridView.Columns[gridView.Columns.Count - 1].MinimumWidth = Preferences.LastColumnWidth;
            }
        }
        catch (NullReferenceException e)
        {
            // See https://connect.microsoft.com/VisualStudio/feedback/details/366943/autoresizecolumns-in-datagridview-throws-nullreferenceexception
            // possible solution => https://stackoverflow.com/questions/36287553/nullreferenceexception-when-trying-to-set-datagridview-column-width-brings-th
            // There are some rare situations with null ref exceptions when resizing columns and on filter finished
            // So catch them here. Better than crashing.
            _logger.Error(e, "Error while resizing columns: ");
        }
    }

    private void PaintCell (DataGridViewCellPaintingEventArgs e, BufferedDataGridView gridView, bool noBackgroundFill, HighlightEntry groundEntry)
    {
        PaintHighlightedCell(e, gridView, noBackgroundFill, groundEntry);
    }

    private void PaintHighlightedCell (DataGridViewCellPaintingEventArgs e, BufferedDataGridView gridView, bool noBackgroundFill, HighlightEntry groundEntry)
    {
        var column = e.Value as IColumn;

        column ??= Column.EmptyColumn;

        IList<HighlightMatchEntry> matchList = FindHighlightMatches(column);
        // too many entries per line seem to cause problems with the GDI
        while (matchList.Count > 50)
        {
            matchList.RemoveAt(50);
        }

        var he = new HighlightEntry
        {
            SearchText = column.DisplayValue,
            ForegroundColor = groundEntry?.ForegroundColor ?? Color.FromKnownColor(KnownColor.Black),
            BackgroundColor = groundEntry?.BackgroundColor ?? Color.Empty,
            IsWordMatch = true
        };

        HighlightMatchEntry hme = new()
        {
            StartPos = 0,
            Length = column.DisplayValue.Length,
            HighlightEntry = he
        };

        if (groundEntry != null)
        {
            hme.HighlightEntry.IsBold = groundEntry.IsBold;
        }

        matchList = MergeHighlightMatchEntries(matchList, hme);

        //var leftPad = e.CellStyle.Padding.Left;
        //RectangleF rect = new(e.CellBounds.Left + leftPad, e.CellBounds.Top, e.CellBounds.Width, e.CellBounds.Height);

        Rectangle borderWidths = PaintHelper.BorderWidths(e.AdvancedBorderStyle);
        Rectangle valBounds = e.CellBounds;
        valBounds.Offset(borderWidths.X, borderWidths.Y);
        valBounds.Width -= borderWidths.Right;
        valBounds.Height -= borderWidths.Bottom;
        if (e.CellStyle.Padding != Padding.Empty)
        {
            valBounds.Offset(e.CellStyle.Padding.Left, e.CellStyle.Padding.Top);
            valBounds.Width -= e.CellStyle.Padding.Horizontal;
            valBounds.Height -= e.CellStyle.Padding.Vertical;
        }

        TextFormatFlags flags =
                TextFormatFlags.Left
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPrefix
                | TextFormatFlags.PreserveGraphicsClipping
                | TextFormatFlags.NoPadding
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.TextBoxControl
            ;

        //          | TextFormatFlags.VerticalCenter
        //          | TextFormatFlags.TextBoxControl
        //          TextFormatFlags.SingleLine

        //TextRenderer.DrawText(e.Graphics, e.Value as String, e.CellStyle.Font, valBounds, Color.FromKnownColor(KnownColor.Black), flags);

        Point wordPos = valBounds.Location;
        Size proposedSize = new(valBounds.Width, valBounds.Height);

        Rectangle r = gridView.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
        e.Graphics.SetClip(e.CellBounds);

        foreach (HighlightMatchEntry matchEntry in matchList)
        {
            Font font = matchEntry != null && matchEntry.HighlightEntry.IsBold ? BoldFont : NormalFont;

            Brush bgBrush = matchEntry.HighlightEntry.BackgroundColor != Color.Empty
                ? new SolidBrush(matchEntry.HighlightEntry.BackgroundColor)
                : null;

            var matchWord = column.DisplayValue.Substring(matchEntry.StartPos, matchEntry.Length);
            Size wordSize = TextRenderer.MeasureText(e.Graphics, matchWord, font, proposedSize, flags);
            wordSize.Height = e.CellBounds.Height;
            Rectangle wordRect = new(wordPos, wordSize);

            Color foreColor = matchEntry.HighlightEntry.ForegroundColor;
            if ((e.State & DataGridViewElementStates.Selected) != DataGridViewElementStates.Selected)
            {
                if (!noBackgroundFill && bgBrush != null && !matchEntry.HighlightEntry.NoBackground)
                {
                    e.Graphics.FillRectangle(bgBrush, wordRect);
                }
            }

            TextRenderer.DrawText(e.Graphics, matchWord, font, wordRect, foreColor, flags);

            wordPos.Offset(wordSize.Width, 0);
            bgBrush?.Dispose();
        }
    }

    /// <summary>
    /// Builds a list of HilightMatchEntry objects. A HilightMatchEntry spans over a region that is painted with the same foreground and
    /// background colors.
    /// All regions which don't match a word-mode entry will be painted with the colors of a default entry (groundEntry). This is either the
    /// first matching non-word-mode highlight entry or a black-on-white default (if no matching entry was found).
    /// </summary>
    /// <param name="matchList">List of all highlight matches for the current cell</param>
    /// <param name="groundEntry">The entry that is used as the default.</param>
    /// <returns>List of HilightMatchEntry objects. The list spans over the whole cell and contains color infos for every substring.</returns>
    private IList<HighlightMatchEntry> MergeHighlightMatchEntries (IList<HighlightMatchEntry> matchList, HighlightMatchEntry groundEntry)
    {
        // Fill an area with lenth of whole text with a default hilight entry
        var entryArray = new HighlightEntry[groundEntry.Length];
        for (var i = 0; i < entryArray.Length; ++i)
        {
            entryArray[i] = groundEntry.HighlightEntry;
        }

        // "overpaint" with all matching word match enries
        // Non-word-mode matches will not overpaint because they use the groundEntry
        foreach (HighlightMatchEntry me in matchList)
        {
            var endPos = me.StartPos + me.Length;
            for (var i = me.StartPos; i < endPos; ++i)
            {
                if (me.HighlightEntry.IsWordMatch)
                {
                    entryArray[i] = me.HighlightEntry;
                }
                else
                {
                    //entryArray[i].ForegroundColor = me.HilightEntry.ForegroundColor;
                }
            }
        }

        // collect areas with same hilight entry and build new highlight match entries for it
        IList<HighlightMatchEntry> mergedList = [];

        if (entryArray.Length > 0)
        {
            HighlightEntry currentEntry = entryArray[0];
            var lastStartPos = 0;
            var pos = 0;

            for (; pos < entryArray.Length; ++pos)
            {
                if (entryArray[pos] != currentEntry)
                {
                    HighlightMatchEntry me = new()
                    {
                        StartPos = lastStartPos,
                        Length = pos - lastStartPos,
                        HighlightEntry = currentEntry
                    };

                    mergedList.Add(me);
                    currentEntry = entryArray[pos];
                    lastStartPos = pos;
                }
            }

            HighlightMatchEntry me2 = new()
            {
                StartPos = lastStartPos,
                Length = pos - lastStartPos,
                HighlightEntry = currentEntry
            };

            mergedList.Add(me2);
        }

        return mergedList;
    }

    /// <summary>
    /// Returns the first HilightEntry that matches the given line
    /// </summary>
    private HighlightEntry FindHilightEntry (ITextValue line)
    {
        return FindHighlightEntry(line, false);
    }

    private HighlightEntry FindFirstNoWordMatchHilightEntry (ITextValue line)
    {
        return FindHighlightEntry(line, true);
    }

    private bool CheckHighlightEntryMatch (HighlightEntry entry, ITextValue column)
    {
        if (entry.IsRegEx)
        {
            //Regex rex = new Regex(entry.SearchText, entry.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            if (entry.Regex.IsMatch(column.Text))
            {
                return true;
            }
        }
        else
        {
            if (entry.IsCaseSensitive)
            {
                if (column.Text.Contains(entry.SearchText, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            else
            {
                if (column.Text.ToUpperInvariant().Contains(entry.SearchText.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns all HilightEntry entries which matches the given line
    /// </summary>
    private IList<HighlightEntry> FindMatchingHilightEntries (ITextValue line)
    {
        IList<HighlightEntry> resultList = [];
        if (line != null)
        {
            lock (_currentHighlightGroupLock)
            {
                foreach (HighlightEntry entry in _currentHighlightGroup.HighlightEntryList)
                {
                    if (CheckHighlightEntryMatch(entry, line))
                    {
                        resultList.Add(entry);
                    }
                }
            }
        }

        return resultList;
    }

    private void GetHighlightEntryMatches (ITextValue line, IList<HighlightEntry> hilightEntryList, IList<HighlightMatchEntry> resultList)
    {
        foreach (HighlightEntry entry in hilightEntryList)
        {
            if (entry.IsWordMatch)
            {
                MatchCollection matches = entry.Regex.Matches(line.Text);
                foreach (Match match in matches)
                {
                    HighlightMatchEntry me = new()
                    {
                        HighlightEntry = entry,
                        StartPos = match.Index,
                        Length = match.Length
                    };

                    resultList.Add(me);
                }
            }
            else
            {
                if (CheckHighlightEntryMatch(entry, line))
                {
                    HighlightMatchEntry me = new()
                    {
                        HighlightEntry = entry,
                        StartPos = 0,
                        Length = line.Text.Length
                    };

                    resultList.Add(me);
                }
            }
        }
    }

    private void GetHilightActions (IList<HighlightEntry> matchingList, out bool noLed, out bool stopTail, out bool setBookmark, out string bookmarkComment)
    {
        noLed = stopTail = setBookmark = false;
        bookmarkComment = string.Empty;

        foreach (HighlightEntry entry in matchingList)
        {
            if (entry.IsLedSwitch)
            {
                noLed = true;
            }

            if (entry.IsSetBookmark)
            {
                setBookmark = true;
                if (!string.IsNullOrEmpty(entry.BookmarkComment))
                {
                    bookmarkComment += entry.BookmarkComment + "\r\n";
                }
            }

            if (entry.IsStopTail)
            {
                stopTail = true;
            }
        }

        bookmarkComment = bookmarkComment.TrimEnd(['\r', '\n']);
    }

    private void StopTimespreadThread ()
    {
        _timeSpreadCalc.Stop();
    }

    private void StopTimestampSyncThread ()
    {
        _shouldTimestampDisplaySyncingCancel = true;
        //_timeShiftSyncWakeupEvent.Set();
        //_timeShiftSyncThread.Abort();
        //_timeShiftSyncThread.Join();
        cts.Cancel();
    }

    [SupportedOSPlatform("windows")]
    private void SyncTimestampDisplay ()
    {
        if (CurrentColumnizer.IsTimeshiftImplemented())
        {
            if (dataGridView.CurrentRow != null)
            {
                SyncTimestampDisplay(dataGridView.CurrentRow.Index);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void SyncTimestampDisplay (int lineNum)
    {
        _timeShiftSyncLine = lineNum;
        _timeShiftSyncTimerEvent.Set();
        _timeShiftSyncWakeupEvent.Set();
    }

    [SupportedOSPlatform("windows")]
    private void SyncTimestampDisplayWorker ()
    {
        const int WAIT_TIME = 500;
        Thread.CurrentThread.Name = "SyncTimestampDisplayWorker";
        _shouldTimestampDisplaySyncingCancel = false;
        _isTimestampDisplaySyncing = true;

        while (!_shouldTimestampDisplaySyncingCancel)
        {
            _timeShiftSyncWakeupEvent.WaitOne();
            if (_shouldTimestampDisplaySyncingCancel)
            {
                return;
            }

            _timeShiftSyncWakeupEvent.Reset();

            while (!_shouldTimestampDisplaySyncingCancel)
            {
                var signaled = _timeShiftSyncTimerEvent.WaitOne(WAIT_TIME, true);
                _timeShiftSyncTimerEvent.Reset();
                if (!signaled)
                {
                    break;
                }
            }

            // timeout with no new Trigger -> update display
            var lineNum = _timeShiftSyncLine;
            if (lineNum >= 0 && lineNum < dataGridView.RowCount)
            {
                var refLine = lineNum;
                DateTime timeStamp = GetTimestampForLine(ref refLine, true);
                if (!timeStamp.Equals(DateTime.MinValue) && !_shouldTimestampDisplaySyncingCancel)
                {
                    _guiStateArgs.Timestamp = timeStamp;
                    SendGuiStateUpdate();
                    if (_shouldCallTimeSync)
                    {
                        refLine = lineNum;
                        DateTime exactTimeStamp = GetTimestampForLine(ref refLine, false);
                        SyncOtherWindows(exactTimeStamp);
                        _shouldCallTimeSync = false;
                    }
                }
            }

            // show time difference between 2 selected lines
            if (dataGridView.SelectedRows.Count == 2)
            {
                var row1 = dataGridView.SelectedRows[0].Index;
                var row2 = dataGridView.SelectedRows[1].Index;
                if (row1 > row2)
                {
                    (row2, row1) = (row1, row2);
                }

                var refLine = row1;
                DateTime timeStamp1 = GetTimestampForLine(ref refLine, false);
                refLine = row2;
                DateTime timeStamp2 = GetTimestampForLine(ref refLine, false);
                //TimeSpan span = TimeSpan.FromTicks(timeStamp2.Ticks - timeStamp1.Ticks);
                DateTime diff;
                if (timeStamp1.Ticks > timeStamp2.Ticks)
                {
                    diff = new DateTime(timeStamp1.Ticks - timeStamp2.Ticks);
                }
                else
                {
                    diff = new DateTime(timeStamp2.Ticks - timeStamp1.Ticks);
                }

                StatusLineText("Time diff is " + diff.ToString("HH:mm:ss.fff"));
            }
            else
            {
                if (!IsMultiFile && dataGridView.SelectedRows.Count == 1)
                {
                    StatusLineText(string.Empty);
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void SyncFilterGridPos ()
    {
        try
        {
            if (_filterResultList.Count > 0)
            {
                var index = _filterResultList.BinarySearch(dataGridView.CurrentRow.Index);
                if (index < 0)
                {
                    index = ~index;
                    if (index > 0)
                    {
                        --index;
                    }
                }

                if (filterGridView.Rows.GetRowCount(DataGridViewElementStates.None) > 0) // exception no rows
                {
                    filterGridView.CurrentCell = filterGridView.Rows[index].Cells[0];
                }
                else
                {
                    filterGridView.CurrentCell = null;
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "SyncFilterGridPos(): ");
        }
    }

    private void StatusLineFileSize (long size)
    {
        _statusEventArgs.FileSize = size;
        SendStatusLineUpdate();
    }

    [SupportedOSPlatform("windows")]
    private int Search (SearchParams searchParams)
    {
        if (searchParams.SearchText == null)
        {
            return -1;
        }

        var lineNum = searchParams.IsFromTop && !searchParams.IsFindNext
            ? 0
            : searchParams.CurrentLine;

        var lowerSearchText = searchParams.SearchText.ToLowerInvariant();
        var count = 0;
        var hasWrapped = false;

        while (true)
        {
            if ((searchParams.IsForward || searchParams.IsFindNext) && !searchParams.IsShiftF3Pressed)
            {
                if (lineNum >= _logFileReader.LineCount)
                {
                    if (hasWrapped)
                    {
                        StatusLineError("Not found: " + searchParams.SearchText);
                        return -1;
                    }

                    lineNum = 0;
                    count = 0;
                    hasWrapped = true;
                    StatusLineError("Started from beginning of file");
                }
            }
            else
            {
                if (lineNum < 0)
                {
                    if (hasWrapped)
                    {
                        StatusLineError("Not found: " + searchParams.SearchText);
                        return -1;
                    }

                    count = 0;
                    lineNum = _logFileReader.LineCount - 1;
                    hasWrapped = true;
                    StatusLineError("Started from end of file");
                }
            }

            ILogLine line = _logFileReader.GetLogLine(lineNum);
            if (line == null)
            {
                return -1;
            }

            if (searchParams.IsRegex)
            {
                Regex rex = new(searchParams.SearchText, searchParams.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                if (rex.IsMatch(line.FullLine))
                {
                    return lineNum;
                }
            }
            else
            {
                if (searchParams.IsCaseSensitive)
                {
                    if (line.FullLine.Contains(searchParams.SearchText, StringComparison.Ordinal))
                    {
                        return lineNum;
                    }
                }
                else
                {
                    if (line.FullLine.Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase))
                    {
                        return lineNum;
                    }
                }
            }

            if ((searchParams.IsForward || searchParams.IsFindNext) && !searchParams.IsShiftF3Pressed)
            {
                lineNum++;
            }
            else
            {
                lineNum--;
            }

            if (_shouldCancel)
            {
                return -1;
            }

            if (++count % PROGRESS_BAR_MODULO == 0)
            {
                try
                {
                    if (!Disposing)
                    {
                        Invoke(UpdateProgressBar, [count]);
                    }
                }
                catch (ObjectDisposedException ex) // can occur when closing the app while searching
                {
                    _logger.Warn(ex);
                }
            }
        }
    }

    private void ResetProgressBar ()
    {
        _progressEventArgs.Value = _progressEventArgs.MaxValue;
        _progressEventArgs.Visible = false;
        SendProgressBarUpdate();
    }

    [SupportedOSPlatform("windows")]
    private void SelectLine (int line, bool triggerSyncCall, bool shouldScroll)
    {
        try
        {
            _shouldCallTimeSync = triggerSyncCall;
            var wasCancelled = _shouldCancel;
            _shouldCancel = false;
            _isSearching = false;
            StatusLineText(string.Empty);
            _guiStateArgs.MenuEnabled = true;

            if (wasCancelled)
            {
                return;
            }

            if (line == -1)
            {
                // Hmm... is that experimental code from early days?
                MessageBox.Show(this, "Not found:", "Search result");
                return;
            }

            // Prevent ArgumentOutOfRangeException
            if (line >= dataGridView.Rows.GetRowCount(DataGridViewElementStates.None))
            {
                line = dataGridView.Rows.GetRowCount(DataGridViewElementStates.None) - 1;
            }

            dataGridView.Rows[line].Selected = true;

            if (shouldScroll)
            {
                dataGridView.CurrentCell = dataGridView.Rows[line].Cells[0];
                dataGridView.Focus();
            }
        }
        catch (ArgumentOutOfRangeException e)
        {
            _logger.Error(e, "Error while selecting line: ");
        }
        catch (IndexOutOfRangeException e)
        {
            // Occures sometimes (but cannot reproduce)
            _logger.Error(e, "Error while selecting line: ");
        }
    }

    [SupportedOSPlatform("windows")]
    private void StartEditMode ()
    {
        if (!dataGridView.CurrentCell.ReadOnly)
        {
            dataGridView.BeginEdit(false);
            if (dataGridView.EditingControl != null)
            {
                if (dataGridView.EditingControl is LogCellEditingControl editControl)
                {
                    editControl.KeyDown += OnEditControlKeyDown;
                    editControl.KeyPress += OnEditControlKeyPress;
                    editControl.KeyUp += OnEditControlKeyUp;
                    editControl.Click += OnEditControlClick;
                    dataGridView.CellEndEdit += OnDataGridViewCellEndEdit;
                    editControl.SelectionStart = 0;
                }
                else
                {
                    _logger.Warn(CultureInfo.InvariantCulture, "Edit control in logWindow was null");
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void UpdateEditColumnDisplay (DataGridViewTextBoxEditingControl editControl)
    {
        // prevents key events after edit mode has ended
        if (dataGridView.EditingControl != null)
        {
            var pos = editControl.SelectionStart + editControl.SelectionLength;
            StatusLineText("   " + pos);
            _logger.Debug(CultureInfo.InvariantCulture, "SelStart: {0}, SelLen: {1}", editControl.SelectionStart, editControl.SelectionLength);
        }
    }

    [SupportedOSPlatform("windows")]
    private void SelectPrevHighlightLine ()
    {
        var lineNum = dataGridView.CurrentCellAddress.Y;
        while (lineNum > 0)
        {
            lineNum--;
            ILogLine line = _logFileReader.GetLogLine(lineNum);
            if (line != null)
            {
                HighlightEntry entry = FindHilightEntry(line);
                if (entry != null)
                {
                    SelectLine(lineNum, false, true);
                    break;
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void SelectNextHighlightLine ()
    {
        var lineNum = dataGridView.CurrentCellAddress.Y;
        while (lineNum < _logFileReader.LineCount)
        {
            lineNum++;
            ILogLine line = _logFileReader.GetLogLine(lineNum);
            if (line != null)
            {
                HighlightEntry entry = FindHilightEntry(line);
                if (entry != null)
                {
                    SelectLine(lineNum, false, true);
                    break;
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private int FindNextBookmarkIndex (int lineNum)
    {
        if (lineNum >= dataGridView.RowCount)
        {
            lineNum = 0;
        }
        else
        {
            lineNum++;
        }

        return _bookmarkProvider.FindNextBookmarkIndex(lineNum);
    }

    [SupportedOSPlatform("windows")]
    private int FindPrevBookmarkIndex (int lineNum)
    {
        if (lineNum <= 0)
        {
            lineNum = dataGridView.RowCount - 1;
        }
        else
        {
            lineNum--;
        }

        return _bookmarkProvider.FindPrevBookmarkIndex(lineNum);
    }

    /**
   * Shift bookmarks after a logfile rollover
   */

    private void ShiftBookmarks (int offset)
    {
        _bookmarkProvider.ShiftBookmarks(offset);
        OnBookmarkRemoved();
    }

    private void ShiftRowHeightList (int offset)
    {
        SortedList<int, RowHeightEntry> newList = [];
        foreach (RowHeightEntry entry in _rowHeightList.Values)
        {
            var line = entry.LineNum - offset;
            if (line >= 0)
            {
                entry.LineNum = line;
                newList.Add(line, entry);
            }
        }

        _rowHeightList = newList;
    }

    private void ShiftFilterPipes (int offset)
    {
        lock (_filterPipeList)
        {
            foreach (FilterPipe pipe in _filterPipeList)
            {
                pipe.ShiftLineNums(offset);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void LoadFilterPipes ()
    {
        lock (_filterPipeList)
        {
            foreach (FilterPipe pipe in _filterPipeList)
            {
                pipe.RecreateTempFile();
            }
        }

        if (_filterPipeList.Count > 0)
        {
            for (var i = 0; i < dataGridView.RowCount; ++i)
            {
                ProcessFilterPipes(i);
            }
        }
    }

    private void DisconnectFilterPipes ()
    {
        lock (_filterPipeList)
        {
            foreach (FilterPipe pipe in _filterPipeList)
            {
                pipe.ClearLineList();
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void ApplyFilterParams ()
    {
        filterComboBox.Text = _filterParams.SearchText;
        filterCaseSensitiveCheckBox.Checked = _filterParams.IsCaseSensitive;
        filterRegexCheckBox.Checked = _filterParams.IsRegex;
        filterTailCheckBox.Checked = _filterParams.IsFilterTail;
        invertFilterCheckBox.Checked = _filterParams.IsInvert;
        filterKnobBackSpread.Value = _filterParams.SpreadBefore;
        filterKnobForeSpread.Value = _filterParams.SpreadBehind;
        rangeCheckBox.Checked = _filterParams.IsRangeSearch;
        columnRestrictCheckBox.Checked = _filterParams.ColumnRestrict;
        fuzzyKnobControl.Value = _filterParams.FuzzyValue;
        filterRangeComboBox.Text = _filterParams.RangeSearchText;
    }

    [SupportedOSPlatform("windows")]
    private void ResetFilterControls ()
    {
        filterComboBox.Text = "";
        filterCaseSensitiveCheckBox.Checked = false;
        filterRegexCheckBox.Checked = false;
        //this.filterTailCheckBox.Checked = this.Preferences.filterTail;
        invertFilterCheckBox.Checked = false;
        filterKnobBackSpread.Value = 0;
        filterKnobForeSpread.Value = 0;
        rangeCheckBox.Checked = false;
        columnRestrictCheckBox.Checked = false;
        fuzzyKnobControl.Value = 0;
        filterRangeComboBox.Text = "";
    }

    [SupportedOSPlatform("windows")]
    private void FilterSearch ()
    {
        if (filterComboBox.Text.Length == 0)
        {
            _filterParams.SearchText = string.Empty;
            _filterParams.IsRangeSearch = false;
            ClearFilterList();
            filterSearchButton.Image = null;
            ResetFilterControls();
            saveFilterButton.Enabled = false;
            return;
        }

        FilterSearch(filterComboBox.Text);
    }

    [SupportedOSPlatform("windows")]
    private async void FilterSearch (string text)
    {
        FireCancelHandlers(); // make sure that there's no other filter running (maybe from filter restore)

        _filterParams.SearchText = text;
        ConfigManager.Settings.FilterHistoryList.Remove(text);
        ConfigManager.Settings.FilterHistoryList.Insert(0, text);
        var maxHistory = ConfigManager.Settings.Preferences.MaximumFilterEntries;

        if (ConfigManager.Settings.FilterHistoryList.Count > maxHistory)
        {
            ConfigManager.Settings.FilterHistoryList.RemoveAt(filterComboBox.Items.Count - 1);
        }

        filterComboBox.Items.Clear();
        foreach (var item in ConfigManager.Settings.FilterHistoryList)
        {
            filterComboBox.Items.Add(item);
        }

        filterComboBox.Text = text;

        _filterParams.IsRangeSearch = rangeCheckBox.Checked;
        _filterParams.RangeSearchText = filterRangeComboBox.Text;
        if (_filterParams.IsRangeSearch)
        {
            ConfigManager.Settings.FilterRangeHistoryList.Remove(filterRangeComboBox.Text);
            ConfigManager.Settings.FilterRangeHistoryList.Insert(0, filterRangeComboBox.Text);
            if (ConfigManager.Settings.FilterRangeHistoryList.Count > maxHistory)
            {
                ConfigManager.Settings.FilterRangeHistoryList.RemoveAt(filterRangeComboBox.Items.Count - 1);
            }

            filterRangeComboBox.Items.Clear();
            foreach (var item in ConfigManager.Settings.FilterRangeHistoryList)
            {
                filterRangeComboBox.Items.Add(item);
            }
        }

        ConfigManager.Save(SettingsFlags.FilterHistory);

        _filterParams.IsCaseSensitive = filterCaseSensitiveCheckBox.Checked;
        _filterParams.IsRegex = filterRegexCheckBox.Checked;
        _filterParams.IsFilterTail = filterTailCheckBox.Checked;
        _filterParams.IsInvert = invertFilterCheckBox.Checked;
        if (_filterParams.IsRegex)
        {
            try
            {
                _filterParams.CreateRegex();
            }
            catch (ArgumentException)
            {
                StatusLineError("Invalid regular expression");
                return;
            }
        }

        _filterParams.FuzzyValue = fuzzyKnobControl.Value;
        _filterParams.SpreadBefore = filterKnobBackSpread.Value;
        _filterParams.SpreadBehind = filterKnobForeSpread.Value;
        _filterParams.ColumnRestrict = columnRestrictCheckBox.Checked;

        //ConfigManager.SaveFilterParams(this.filterParams);
        ConfigManager.Settings.FilterParams = _filterParams; // wozu eigentlich? sinnlos seit MDI?

        _shouldCancel = false;
        _isSearching = true;
        StatusLineText("Filtering... Press ESC to cancel");
        filterSearchButton.Enabled = false;
        ClearFilterList();

        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = dataGridView.RowCount;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        Settings settings = ConfigManager.Settings;

        //FilterFx fx = settings.preferences.multiThreadFilter ? MultiThreadedFilter : new FilterFx(Filter);
        FilterFxAction = settings.Preferences.MultiThreadFilter ? MultiThreadedFilter : Filter;

        //Task.Run(() => fx.Invoke(_filterParams, _filterResultList, _lastFilterLinesList, _filterHitList));
        var filterFxActionTask = Task.Run(() => Filter(_filterParams, _filterResultList, _lastFilterLinesList, _filterHitList));

        await filterFxActionTask;
        FilterComplete();

        //fx.BeginInvoke(_filterParams, _filterResultList, _lastFilterLinesList, _filterHitList, FilterComplete, null);
        CheckForFilterDirty();
    }

    private void MultiThreadedFilter (FilterParams filterParams, List<int> filterResultLines, List<int> lastFilterLinesList, List<int> filterHitList)
    {
        ColumnizerCallback callback = new(this);

        FilterStarter fs = new(callback, Environment.ProcessorCount + 2)
        {
            FilterHitList = _filterHitList,
            FilterResultLines = _filterResultList,
            LastFilterLinesList = _lastFilterLinesList
        };

        var cancelHandler = new FilterCancelHandler(fs);
        OnRegisterCancelHandler(cancelHandler);
        long startTime = Environment.TickCount;

        fs.DoFilter(filterParams, 0, _logFileReader.LineCount, FilterProgressCallback);

        long endTime = Environment.TickCount;

        _logger.Debug($"Multi threaded filter duration: {endTime - startTime} ms.");

        OnDeRegisterCancelHandler(cancelHandler);
        StatusLineText("Filter duration: " + (endTime - startTime) + " ms.");
    }

    private void FilterProgressCallback (int lineCount)
    {
        UpdateProgressBar(lineCount);
    }

    [SupportedOSPlatform("windows")]
    private void Filter (FilterParams filterParams, List<int> filterResultLines, List<int> lastFilterLinesList, List<int> filterHitList)
    {
        long startTime = Environment.TickCount;
        try
        {
            filterParams.Reset();
            var lineNum = 0;
            //AddFilterLineFx addFx = new AddFilterLineFx(AddFilterLine);
            ColumnizerCallback callback = new(this);
            while (true)
            {
                ILogLine line = _logFileReader.GetLogLine(lineNum);
                if (line == null)
                {
                    break;
                }

                callback.LineNum = lineNum;
                if (Util.TestFilterCondition(filterParams, line, callback))
                {
                    AddFilterLine(lineNum, false, filterParams, filterResultLines, lastFilterLinesList,
                        filterHitList);
                }

                lineNum++;
                if (lineNum % PROGRESS_BAR_MODULO == 0)
                {
                    UpdateProgressBar(lineNum);
                }

                if (_shouldCancel)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while filtering. Please report to developer: ");
            MessageBox.Show(null, $"Exception while filtering. Please report to developer: \n\n{ex}\n\n{ex.StackTrace}", "LogExpert");
        }

        long endTime = Environment.TickCount;

        _logger.Info($"Single threaded filter duration: {endTime - startTime} ms.");

        StatusLineText("Filter duration: " + (endTime - startTime) + " ms.");
    }

    /// <summary>
    ///  Returns a list with 'additional filter results'. This is the given line number
    ///  and (if back spread and/or fore spread is enabled) some additional lines.
    ///  This function doesn't check the filter condition!
    /// </summary>
    /// <param name="filterParams"></param>
    /// <param name="lineNum"></param>
    /// <param name="checkList"></param>
    /// <returns></returns>
    private IList<int> GetAdditionalFilterResults (FilterParams filterParams, int lineNum, IList<int> checkList)
    {
        IList<int> resultList = [];
        //string textLine = this.logFileReader.GetLogLine(lineNum);
        //ColumnizerCallback callback = new ColumnizerCallback(this);
        //callback.LineNum = lineNum;

        if (filterParams.SpreadBefore == 0 && filterParams.SpreadBehind == 0)
        {
            resultList.Add(lineNum);
            return resultList;
        }

        // back spread
        for (var i = filterParams.SpreadBefore; i > 0; --i)
        {
            if (lineNum - i > 0)
            {
                if (!resultList.Contains(lineNum - i) && !checkList.Contains(lineNum - i))
                {
                    resultList.Add(lineNum - i);
                }
            }
        }

        // direct filter hit
        if (!resultList.Contains(lineNum) && !checkList.Contains(lineNum))
        {
            resultList.Add(lineNum);
        }

        // after spread
        for (var i = 1; i <= filterParams.SpreadBehind; ++i)
        {
            if (lineNum + i < _logFileReader.LineCount)
            {
                if (!resultList.Contains(lineNum + i) && !checkList.Contains(lineNum + i))
                {
                    resultList.Add(lineNum + i);
                }
            }
        }

        return resultList;
    }

    [SupportedOSPlatform("windows")]
    private void AddFilterLine (int lineNum, bool immediate, FilterParams filterParams, List<int> filterResultLines, List<int> lastFilterLinesList, List<int> filterHitList)
    {
        int count;
        lock (_filterResultList)
        {
            filterHitList.Add(lineNum);
            IList<int> filterResult = GetAdditionalFilterResults(filterParams, lineNum, lastFilterLinesList);
            filterResultLines.AddRange(filterResult);
            count = filterResultLines.Count;
            lastFilterLinesList.AddRange(filterResult);
            if (lastFilterLinesList.Count > SPREAD_MAX * 2)
            {
                lastFilterLinesList.RemoveRange(0, lastFilterLinesList.Count - SPREAD_MAX * 2);
            }
        }

        if (immediate)
        {
            TriggerFilterLineGuiUpdate();
        }
        else if (lineNum % PROGRESS_BAR_MODULO == 0)
        {
            //FunctionWith1IntParam fx = new FunctionWith1IntParam(UpdateFilterCountLabel);
            //this.Invoke(fx, new object[] { count});
        }
    }

    [SupportedOSPlatform("windows")]
    private void TriggerFilterLineGuiUpdate ()
    {
        //lock (this.filterUpdateThread)
        //{
        //  this.filterEventCount++;
        //  this.filterUpdateEvent.Set();
        //}
        Invoke(new MethodInvoker(AddFilterLineGuiUpdate));
    }

    //private void FilterUpdateWorker()
    //{
    //  Thread.CurrentThread.Name = "FilterUpdateWorker";
    //  while (true)
    //  {
    //    this.filterUpdateEvent.WaitOne();
    //    lock (this.filterUpdateThread)
    //    {
    //      this.Invoke(new MethodInvoker(AddFilterLineGuiUpdate));
    //      this.filterUpdateEvent.Reset();
    //    }

    //    //_logger.logDebug("FilterUpdateWorker: Waiting for signal");
    //    //bool signaled = this.filterUpdateEvent.WaitOne(1000, false);

    //    //if (!signaled)
    //    //{
    //    //  lock (this.filterUpdateThread)
    //    //  {
    //    //    if (this.filterEventCount > 0)
    //    //    {
    //    //      this.filterEventCount = 0;
    //    //      _logger.logDebug("FilterUpdateWorker: Invoking GUI update because of wait timeout");
    //    //      this.Invoke(new MethodInvoker(AddFilterLineGuiUpdate));
    //    //    }
    //    //  }
    //    //}
    //    //else
    //    //{
    //    //  _logger.logDebug("FilterUpdateWorker: Wakeup signal received.");
    //    //  lock (this.filterUpdateThread)
    //    //  {
    //    //    _logger.logDebug("FilterUpdateWorker: event count: " + this.filterEventCount);
    //    //    if (this.filterEventCount > 100)
    //    //    {
    //    //      this.filterEventCount = 0;
    //    //      _logger.logDebug("FilterUpdateWorker: Invoking GUI update because of event count");
    //    //      this.Invoke(new MethodInvoker(AddFilterLineGuiUpdate));
    //    //    }
    //    //    this.filterUpdateEvent.Reset();
    //    //  }
    //    //}
    //  }
    //}

    //private void StopFilterUpdateWorkerThread()
    //{
    //  this.filterUpdateEvent.Set();
    //  this.filterUpdateThread.Abort();
    //  this.filterUpdateThread.Join();
    //}

    [SupportedOSPlatform("windows")]
    private void AddFilterLineGuiUpdate ()
    {
        try
        {
            lock (_filterResultList)
            {
                lblFilterCount.Text = "" + _filterResultList.Count;
                if (filterGridView.RowCount > _filterResultList.Count)
                {
                    filterGridView.RowCount = 0; // helps to prevent hang ?
                }

                filterGridView.RowCount = _filterResultList.Count;
                if (filterGridView.RowCount > 0)
                {
                    filterGridView.FirstDisplayedScrollingRowIndex = filterGridView.RowCount - 1;
                }

                if (filterGridView.RowCount == 1)
                {
                    // after a file reload adjusted column sizes anew when the first line arrives
                    //this.filterGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                    AutoResizeColumns(filterGridView);
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "AddFilterLineGuiUpdate(): ");
        }
    }

    private void UpdateProgressBar (int value)
    {
        _progressEventArgs.Value = value;
        if (value > _progressEventArgs.MaxValue)
        {
            // can occur if new lines will be added while filtering
            _progressEventArgs.MaxValue = value;
        }

        SendProgressBarUpdate();
    }

    [SupportedOSPlatform("windows")]
    private void FilterComplete ()
    {
        if (!IsDisposed && !_waitingForClose && !Disposing)
        {
            Invoke(new MethodInvoker(ResetStatusAfterFilter));
        }
    }

    [SupportedOSPlatform("windows")]
    private void FilterComplete (IAsyncResult result)
    {
        if (!IsDisposed && !_waitingForClose && !Disposing)
        {
            Invoke(new MethodInvoker(ResetStatusAfterFilter));
        }
    }

    [SupportedOSPlatform("windows")]
    private void ResetStatusAfterFilter ()
    {
        try
        {
            //StatusLineText("");
            _isSearching = false;
            _progressEventArgs.Value = _progressEventArgs.MaxValue;
            _progressEventArgs.Visible = false;
            SendProgressBarUpdate();
            filterGridView.RowCount = _filterResultList.Count;
            //this.filterGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            AutoResizeColumns(filterGridView);
            lblFilterCount.Text = "" + _filterResultList.Count;
            if (filterGridView.RowCount > 0)
            {
                filterGridView.Focus();
            }

            filterSearchButton.Enabled = true;
        }
        catch (NullReferenceException e)
        {
            // See https://connect.microsoft.com/VisualStudio/feedback/details/366943/autoresizecolumns-in-datagridview-throws-nullreferenceexception
            // There are some rare situations with null ref exceptions when resizing columns and on filter finished
            // So catch them here. Better than crashing.
            _logger.Error(e, "Error: ");
        }
    }

    [SupportedOSPlatform("windows")]
    private void ClearFilterList ()
    {
        try
        {
            //this.shouldCancel = true;
            lock (_filterResultList)
            {
                filterGridView.SuspendLayout();
                filterGridView.RowCount = 0;
                lblFilterCount.Text = "0";
                _filterResultList = [];
                _lastFilterLinesList = [];
                _filterHitList = [];
                //this.filterGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                filterGridView.ResumeLayout();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Wieder dieser sporadische Fehler: ");

            MessageBox.Show(null, ex.StackTrace, "Wieder dieser sporadische Fehler:");
        }
    }

    private void ClearBookmarkList ()
    {
        _bookmarkProvider.ClearAllBookmarks();
    }

    /**
   * Shift filter list line entries after a logfile rollover
   */
    [SupportedOSPlatform("windows")]
    private void ShiftFilterLines (int offset)
    {
        List<int> newFilterList = [];
        lock (_filterResultList)
        {
            foreach (var lineNum in _filterResultList)
            {
                var line = lineNum - offset;
                if (line >= 0)
                {
                    newFilterList.Add(line);
                }
            }

            _filterResultList = newFilterList;
        }

        newFilterList = [];
        foreach (var lineNum in _filterHitList)
        {
            var line = lineNum - offset;
            if (line >= 0)
            {
                newFilterList.Add(line);
            }
        }

        _filterHitList = newFilterList;

        var count = SPREAD_MAX;
        if (_filterResultList.Count < SPREAD_MAX)
        {
            count = _filterResultList.Count;
        }

        _lastFilterLinesList = _filterResultList.GetRange(_filterResultList.Count - count, count);

        //this.filterGridView.RowCount = this.filterResultList.Count;
        //this.filterCountLabel.Text = "" + this.filterResultList.Count;
        //this.BeginInvoke(new MethodInvoker(this.filterGridView.Refresh));
        //this.BeginInvoke(new MethodInvoker(AddFilterLineGuiUpdate));
        TriggerFilterLineGuiUpdate();
    }

    [SupportedOSPlatform("windows")]
    private void CheckForFilterDirty ()
    {
        if (IsFilterSearchDirty(_filterParams))
        {
            filterSearchButton.Image = _searchButtonImage;
            saveFilterButton.Enabled = false;
        }
        else
        {
            filterSearchButton.Image = null;
            saveFilterButton.Enabled = true;
        }
    }

    [SupportedOSPlatform("windows")]
    private bool IsFilterSearchDirty (FilterParams filterParams)
    {
        if (!filterParams.SearchText.Equals(filterComboBox.Text, StringComparison.Ordinal))
        {
            return true;
        }

        if (filterParams.IsRangeSearch != rangeCheckBox.Checked)
        {
            return true;
        }

        if (filterParams.IsRangeSearch && !filterParams.RangeSearchText.Equals(filterRangeComboBox.Text, StringComparison.Ordinal))
        {
            return true;
        }

        if (filterParams.IsRegex != filterRegexCheckBox.Checked)
        {
            return true;
        }

        if (filterParams.IsInvert != invertFilterCheckBox.Checked)
        {
            return true;
        }

        if (filterParams.SpreadBefore != filterKnobBackSpread.Value)
        {
            return true;
        }

        if (filterParams.SpreadBehind != filterKnobForeSpread.Value)
        {
            return true;
        }

        if (filterParams.FuzzyValue != fuzzyKnobControl.Value)
        {
            return true;
        }

        if (filterParams.ColumnRestrict != columnRestrictCheckBox.Checked)
        {
            return true;
        }

        if (filterParams.IsCaseSensitive != filterCaseSensitiveCheckBox.Checked)
        {
            return true;
        }

        return false;
    }

    [SupportedOSPlatform("windows")]
    private void AdjustMinimumGridWith ()
    {
        if (dataGridView.Columns.Count > 1)
        {
            //this.dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            AutoResizeColumns(dataGridView);

            var width = dataGridView.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
            var diff = dataGridView.Width - width;
            if (diff > 0)
            {
                diff -= dataGridView.RowHeadersWidth / 2;
                dataGridView.Columns[dataGridView.Columns.GetColumnCount(DataGridViewElementStates.None) - 1].Width += diff;
                filterGridView.Columns[filterGridView.Columns.GetColumnCount(DataGridViewElementStates.None) - 1].Width += diff;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void InvalidateCurrentRow (BufferedDataGridView gridView)
    {
        if (gridView.CurrentCellAddress.Y > -1)
        {
            gridView.InvalidateRow(gridView.CurrentCellAddress.Y);
        }
    }

    private void InvalidateCurrentRow ()
    {
        InvalidateCurrentRow(dataGridView);
        InvalidateCurrentRow(filterGridView);
    }

    [SupportedOSPlatform("windows")]
    private void DisplayCurrentFileOnStatusline ()
    {
        if (_logFileReader.IsMultiFile)
        {
            try
            {
                if (dataGridView.CurrentRow != null && dataGridView.CurrentRow.Index > -1)
                {
                    var fileName = _logFileReader.GetLogFileNameForLine(dataGridView.CurrentRow.Index);
                    if (fileName != null)
                    {
                        StatusLineText(Util.GetNameFromPath(fileName));
                    }
                }
            }
            catch (Exception)
            {
                // TODO: handle this concurrent situation better:
                // this.dataGridView.CurrentRow may be null even if checked before.
                // This can happen when MultiFile shift deselects the current row because there
                // are less lines after rollover than before.
                // access to dataGridView-Rows should be locked
            }
        }
    }

    private void UpdateSelectionDisplay ()
    {
        if (_noSelectionUpdates)
        {
            return;
        }
    }

    [SupportedOSPlatform("windows")]
    private void UpdateFilterHistoryFromSettings ()
    {
        ConfigManager.Settings.FilterHistoryList = ConfigManager.Settings.FilterHistoryList;
        filterComboBox.Items.Clear();
        foreach (var item in ConfigManager.Settings.FilterHistoryList)
        {
            filterComboBox.Items.Add(item);
        }

        filterRangeComboBox.Items.Clear();
        foreach (var item in ConfigManager.Settings.FilterRangeHistoryList)
        {
            filterRangeComboBox.Items.Add(item);
        }
    }

    private void StatusLineText (string text)
    {
        _statusEventArgs.StatusText = text;
        SendStatusLineUpdate();
    }

    private void StatusLineError (string text)
    {
        StatusLineText(text);
        _isErrorShowing = true;
    }

    private void RemoveStatusLineError ()
    {
        StatusLineText("");
        _isErrorShowing = false;
    }

    private void SendGuiStateUpdate ()
    {
        OnGuiState(_guiStateArgs);
    }

    private void SendProgressBarUpdate ()
    {
        OnProgressBarUpdate(_progressEventArgs);
    }

    private void SendStatusLineUpdate ()
    {
        OnStatusLine(_statusEventArgs);
    }

    [SupportedOSPlatform("windows")]
    private void ShowAdvancedFilterPanel (bool show)
    {
        if (show)
        {
            advancedButton.Text = "Hide advanced...";
            advancedButton.Image = null;
        }
        else
        {
            advancedButton.Text = "Show advanced...";
            CheckForAdvancedButtonDirty();
        }

        advancedFilterSplitContainer.Panel1Collapsed = !show;
        advancedFilterSplitContainer.SplitterDistance = FILTER_ADVANCED_SPLITTER_DISTANCE;
        _showAdvanced = show;
    }

    [SupportedOSPlatform("windows")]
    private void CheckForAdvancedButtonDirty ()
    {
        advancedButton.Image = IsAdvancedOptionActive() && !_showAdvanced
            ? _advancedButtonImage
            : null;
    }

    [SupportedOSPlatform("windows")]
    private void FilterToTab ()
    {
        filterSearchButton.Enabled = false;
        Task.Run(() => WriteFilterToTab());
    }

    [SupportedOSPlatform("windows")]
    private void WriteFilterToTab ()
    {
        FilterPipe pipe = new(_filterParams.Clone(), this);
        lock (_filterResultList)
        {
            var namePrefix = "->F";
            var title = IsTempFile
                ? TempTitleName + namePrefix + ++_filterPipeNameCounter
                : Util.GetNameFromPath(FileName) + namePrefix + ++_filterPipeNameCounter;

            WritePipeToTab(pipe, _filterResultList, title, null);
        }
    }

    [SupportedOSPlatform("windows")]
    private void WritePipeToTab (FilterPipe pipe, IList<int> lineNumberList, string name, PersistenceData persistenceData)
    {
        _logger.Info(CultureInfo.InvariantCulture, "WritePipeToTab(): {0} lines.", lineNumberList.Count);
        StatusLineText("Writing to temp file... Press ESC to cancel.");
        _guiStateArgs.MenuEnabled = false;
        SendGuiStateUpdate();
        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = lineNumberList.Count;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        Invoke(new MethodInvoker(SendProgressBarUpdate));
        _isSearching = true;
        _shouldCancel = false;

        lock (_filterPipeList)
        {
            _filterPipeList.Add(pipe);
        }

        pipe.Closed += OnPipeDisconnected;
        var count = 0;
        pipe.OpenFile();
        LogExpertCallback callback = new(this);
        foreach (var i in lineNumberList)
        {
            if (_shouldCancel)
            {
                break;
            }

            ILogLine line = _logFileReader.GetLogLine(i);
            if (CurrentColumnizer is ILogLineXmlColumnizer)
            {
                callback.LineNum = i;
                line = (CurrentColumnizer as ILogLineXmlColumnizer).GetLineTextForClipboard(line, callback);
            }

            pipe.WriteToPipe(line, i);
            if (++count % PROGRESS_BAR_MODULO == 0)
            {
                _progressEventArgs.Value = count;
                Invoke(new MethodInvoker(SendProgressBarUpdate));
            }
        }

        pipe.CloseFile();
        _logger.Info(CultureInfo.InvariantCulture, "WritePipeToTab(): finished");
        Invoke(new WriteFilterToTabFinishedFx(WriteFilterToTabFinished), pipe, name, persistenceData);
    }

    [SupportedOSPlatform("windows")]
    private void WriteFilterToTabFinished (FilterPipe pipe, string name, PersistenceData persistenceData)
    {
        _isSearching = false;
        if (!_shouldCancel)
        {
            var title = name;
            ILogLineColumnizer preProcessColumnizer = null;
            if (CurrentColumnizer is not ILogLineXmlColumnizer)
            {
                preProcessColumnizer = CurrentColumnizer;
            }

            LogWindow newWin = _parentLogTabWin.AddFilterTab(pipe, title, preProcessColumnizer);
            newWin.FilterPipe = pipe;
            pipe.OwnLogWindow = newWin;
            if (persistenceData != null)
            {
                Task.Run(() => FilterRestore(newWin, persistenceData));
            }
        }

        _progressEventArgs.Value = _progressEventArgs.MaxValue;
        _progressEventArgs.Visible = false;
        SendProgressBarUpdate();
        _guiStateArgs.MenuEnabled = true;
        SendGuiStateUpdate();
        StatusLineText("");
        filterSearchButton.Enabled = true;
    }

    /// <summary>
    /// Used to create a new tab and pipe the given content into it.
    /// </summary>
    /// <param name="lineEntryList"></param>
    /// <param name="title"></param>
    [SupportedOSPlatform("windows")]
    internal void WritePipeTab (IList<LineEntry> lineEntryList, string title)
    {
        FilterPipe pipe = new(new FilterParams(), this)
        {
            IsStopped = true
        };
        pipe.Closed += OnPipeDisconnected;
        pipe.OpenFile();
        foreach (LineEntry entry in lineEntryList)
        {
            pipe.WriteToPipe(entry.LogLine, entry.LineNum);
        }

        pipe.CloseFile();
        Invoke(new WriteFilterToTabFinishedFx(WriteFilterToTabFinished), [pipe, title, null]);
    }

    [SupportedOSPlatform("windows")]
    private void FilterRestore (LogWindow newWin, PersistenceData persistenceData)
    {
        newWin.WaitForLoadingFinished();
        ILogLineColumnizer columnizer = ColumnizerPicker.FindColumnizerByName(persistenceData.ColumnizerName,
            PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);
        if (columnizer != null)
        {
            SetColumnizerFx fx = newWin.ForceColumnizer;
            newWin.Invoke(fx, [columnizer]);
        }
        else
        {
            _logger.Warn($"FilterRestore(): Columnizer {persistenceData.ColumnizerName} not found");
        }

        newWin.BeginInvoke(new RestoreFiltersFx(newWin.RestoreFilters), [persistenceData]);
    }

    [SupportedOSPlatform("windows")]
    private void ProcessFilterPipes (int lineNum)
    {
        ILogLine searchLine = _logFileReader.GetLogLine(lineNum);
        if (searchLine == null)
        {
            return;
        }

        ColumnizerCallback callback = new(this)
        {
            LineNum = lineNum
        };
        IList<FilterPipe> deleteList = [];
        lock (_filterPipeList)
        {
            foreach (FilterPipe pipe in _filterPipeList)
            {
                if (pipe.IsStopped)
                {
                    continue;
                }

                //long startTime = Environment.TickCount;
                if (Util.TestFilterCondition(pipe.FilterParams, searchLine, callback))
                {
                    IList<int> filterResult =
                        GetAdditionalFilterResults(pipe.FilterParams, lineNum, pipe.LastLinesHistoryList);
                    pipe.OpenFile();
                    foreach (var line in filterResult)
                    {
                        pipe.LastLinesHistoryList.Add(line);
                        if (pipe.LastLinesHistoryList.Count > SPREAD_MAX * 2)
                        {
                            pipe.LastLinesHistoryList.RemoveAt(0);
                        }

                        ILogLine textLine = _logFileReader.GetLogLine(line);
                        var fileOk = pipe.WriteToPipe(textLine, line);
                        if (!fileOk)
                        {
                            deleteList.Add(pipe);
                        }
                    }

                    pipe.CloseFile();
                }

                //long endTime = Environment.TickCount;
                //_logger.logDebug("ProcessFilterPipes(" + lineNum + ") duration: " + ((endTime - startTime)));
            }
        }

        foreach (FilterPipe pipe in deleteList)
        {
            _filterPipeList.Remove(pipe);
        }
    }

    [SupportedOSPlatform("windows")]
    private void CopyMarkedLinesToClipboard ()
    {
        if (_guiStateArgs.CellSelectMode)
        {
            DataObject data = dataGridView.GetClipboardContent();
            Clipboard.SetDataObject(data);
        }
        else
        {
            List<int> lineNumList = [];
            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                if (row.Index != -1)
                {
                    lineNumList.Add(row.Index);
                }
            }

            lineNumList.Sort();
            StringBuilder clipText = new();
            LogExpertCallback callback = new(this);

            var xmlColumnizer = _currentColumnizer as ILogLineXmlColumnizer;

            foreach (var lineNum in lineNumList)
            {
                ILogLine line = _logFileReader.GetLogLine(lineNum);
                if (xmlColumnizer != null)
                {
                    callback.LineNum = lineNum;
                    line = xmlColumnizer.GetLineTextForClipboard(line, callback);
                }

                clipText.AppendLine(line.ToClipBoardText());
            }

            Clipboard.SetText(clipText.ToString());
        }
    }

    /// <summary>
    /// Set an Encoding which shall be used when loading a file. Used before a file is loaded.
    /// </summary>
    /// <param name="encoding"></param>
    private void SetExplicitEncoding (Encoding encoding)
    {
        EncodingOptions.Encoding = encoding;
    }

    [SupportedOSPlatform("windows")]
    private void ApplyDataGridViewPrefs (BufferedDataGridView dataGridView, bool setLastColumnWidth, int lastColumnWidth)
    {
        if (dataGridView.Columns.GetColumnCount(DataGridViewElementStates.None) > 1)
        {
            if (setLastColumnWidth)
            {
                dataGridView.Columns[dataGridView.Columns.GetColumnCount(DataGridViewElementStates.None) - 1].MinimumWidth = lastColumnWidth;
            }
            else
            {
                // Workaround for a .NET bug which brings the DataGridView into an unstable state (causing lots of NullReferenceExceptions).
                dataGridView.FirstDisplayedScrollingColumnIndex = 0;

                dataGridView.Columns[dataGridView.Columns.GetColumnCount(DataGridViewElementStates.None) - 1].MinimumWidth = 5; // default
            }
        }

        if (dataGridView.RowCount > 0)
        {
            dataGridView.UpdateRowHeightInfo(0, true);
        }

        dataGridView.Invalidate();
        dataGridView.Refresh();
        AutoResizeColumns(dataGridView);
    }

    [SupportedOSPlatform("windows")]
    private IList<int> GetSelectedContent ()
    {
        if (dataGridView.SelectionMode == DataGridViewSelectionMode.FullRowSelect)
        {
            List<int> lineNumList = [];

            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                if (row.Index != -1)
                {
                    lineNumList.Add(row.Index);
                }
            }

            lineNumList.Sort();
            return lineNumList;
        }

        return [];
    }

    /* ========================================================================
     * Timestamp stuff
     * =======================================================================*/

    [SupportedOSPlatform("windows")]
    private void SetTimestampLimits ()
    {
        if (!CurrentColumnizer.IsTimeshiftImplemented())
        {
            return;
        }

        var line = 0;
        _guiStateArgs.MinTimestamp = GetTimestampForLineForward(ref line, true);
        line = dataGridView.RowCount - 1;
        _guiStateArgs.MaxTimestamp = GetTimestampForLine(ref line, true);
        SendGuiStateUpdate();
    }

    private void AdjustHighlightSplitterWidth ()
    {
        //int size = this.editHighlightsSplitContainer.Panel2Collapsed ? 600 : 660;
        //int distance = this.highlightSplitContainer.Width - size;
        //if (distance < 10)
        //  distance = 10;
        //this.highlightSplitContainer.SplitterDistance = distance;
    }

    [SupportedOSPlatform("windows")]
    private void BookmarkComment (Bookmark bookmark)
    {
        BookmarkCommentDlg dlg = new()
        {
            Comment = bookmark.Text
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            bookmark.Text = dlg.Comment;
            dataGridView.Refresh();
            OnBookmarkTextChanged(bookmark);
        }
    }

    /// <summary>
    /// Indicates which columns we are filtering on
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    private string CalculateColumnNames (FilterParams filter)
    {
        var names = string.Empty;

        if (filter.ColumnRestrict)
        {
            foreach (var colIndex in filter.ColumnList)
            {
                if (colIndex < dataGridView.Columns.GetColumnCount(DataGridViewElementStates.None) - 2)
                {
                    if (names.Length > 0)
                    {
                        names += ", ";
                    }

                    names += dataGridView.Columns[2 + colIndex]
                        .HeaderText; // skip first two columns: marker + line number
                }
            }
        }

        return names;
    }

    [SupportedOSPlatform("windows")]
    private void ApplyFrozenState (BufferedDataGridView gridView)
    {
        SortedDictionary<int, DataGridViewColumn> dict = [];
        foreach (DataGridViewColumn col in gridView.Columns)
        {
            dict.Add(col.DisplayIndex, col);
        }

        foreach (DataGridViewColumn col in dict.Values)
        {
            col.Frozen = _freezeStateMap.ContainsKey(gridView) && _freezeStateMap[gridView];
            var sel = col.HeaderCell.Selected;
            if (col.Index == _selectedCol)
            {
                break;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void ShowTimeSpread (bool show)
    {
        if (show)
        {
            tableLayoutPanel1.ColumnStyles[1].Width = 16;
        }
        else
        {
            tableLayoutPanel1.ColumnStyles[1].Width = 0;
        }

        _timeSpreadCalc.Enabled = show;
    }

    [SupportedOSPlatform("windows")]
    protected internal void AddTempFileTab (string fileName, string title)
    {
        _parentLogTabWin.AddTempFileTab(fileName, title);
    }

    private void InitPatternWindow ()
    {
        //PatternStatistic(this.patternArgs);
        _patternWindow = new PatternWindow(this);
        _patternWindow.SetColumnizer(CurrentColumnizer);
        //this.patternWindow.SetBlockList(blockList);
        _patternWindow.SetFont(Preferences.FontName, Preferences.FontSize);
        _patternWindow.Fuzzy = _patternArgs.Fuzzy;
        _patternWindow.MaxDiff = _patternArgs.MaxDiffInBlock;
        _patternWindow.MaxMisses = _patternArgs.MaxMisses;
        _patternWindow.Weight = _patternArgs.MinWeight;
        //this.patternWindow.Show();
    }

    [SupportedOSPlatform("windows")]
    private void TestStatistic (PatternArgs patternArgs)
    {
        var beginLine = patternArgs.StartLine;
        _logger.Info($"TestStatistics() called with start line {beginLine}");

        _patternArgs = patternArgs;

        var num = beginLine + 1; //this.dataGridView.RowCount;

        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = dataGridView.RowCount;
        _progressEventArgs.Value = beginLine;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        PrepareDict();
        ResetCache(num);

        Dictionary<int, int> processedLinesDict = [];
        List<PatternBlock> blockList = [];
        var blockId = 0;
        _isSearching = true;
        _shouldCancel = false;
        var searchLine = -1;
        for (var i = beginLine; i < num && !_shouldCancel; ++i)
        {
            if (processedLinesDict.ContainsKey(i))
            {
                continue;
            }

            PatternBlock block;
            var maxBlockLen = patternArgs.EndLine - patternArgs.StartLine;
            //int searchLine = i + 1;
            _logger.Debug(CultureInfo.InvariantCulture, "TestStatistic(): i={0} searchLine={1}", i, searchLine);
            //bool firstBlock = true;
            searchLine++;
            UpdateProgressBar(searchLine);
            while (!_shouldCancel &&
                   (block =
                       DetectBlock(i, searchLine, maxBlockLen, _patternArgs.MaxDiffInBlock,
                           _patternArgs.MaxMisses,
                           processedLinesDict)) != null)
            {
                _logger.Debug(CultureInfo.InvariantCulture, "Found block: {0}", block);
                if (block.Weigth >= _patternArgs.MinWeight)
                {
                    //PatternBlock existingBlock = FindExistingBlock(block, blockList);
                    //if (existingBlock != null)
                    //{
                    //  if (block.weigth > existingBlock.weigth)
                    //  {
                    //    blockList.Remove(existingBlock);
                    //    blockList.Add(block);
                    //  }
                    //}
                    //else
                    {
                        blockList.Add(block);
                        AddBlockTargetLinesToDict(processedLinesDict, block);
                    }
                    block.BlockId = blockId;
                    //if (firstBlock)
                    //{
                    //  addBlockSrcLinesToDict(processedLinesDict, block);
                    //}
                    searchLine = block.TargetEnd + 1;
                }
                else
                {
                    searchLine = block.TargetStart + 1;
                }

                UpdateProgressBar(searchLine);
            }

            blockId++;
        }

        _isSearching = false;
        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = 0;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = false;
        SendProgressBarUpdate();
        //if (this.patternWindow.IsDisposed)
        //{
        //  this.Invoke(new MethodInvoker(CreatePatternWindow));
        //}
        _patternWindow.SetBlockList(blockList, _patternArgs);
        _logger.Info(CultureInfo.InvariantCulture, "TestStatistics() ended");
    }

    private void AddBlockTargetLinesToDict (Dictionary<int, int> dict, PatternBlock block)
    {
        foreach (var lineNum in block.TargetLines.Keys)
        {
            _ = dict.TryAdd(lineNum, lineNum);
        }
    }

    //Well keep this for the moment because there is some other commented code which calls this one
    private PatternBlock FindExistingBlock (PatternBlock block, List<PatternBlock> blockList)
    {
        foreach (PatternBlock searchBlock in blockList)
        {
            if (((block.StartLine > searchBlock.StartLine && block.StartLine < searchBlock.EndLine) ||
                 (block.EndLine > searchBlock.StartLine && block.EndLine < searchBlock.EndLine)) &&
                  block.StartLine != searchBlock.StartLine &&
                  block.EndLine != searchBlock.EndLine
            )
            {
                return searchBlock;
            }
        }

        return null;
    }

    private PatternBlock DetectBlock (int startNum, int startLineToSearch, int maxBlockLen, int maxDiffInBlock, int maxMisses, Dictionary<int, int> processedLinesDict)
    {
        var targetLine = FindSimilarLine(startNum, startLineToSearch, processedLinesDict);
        if (targetLine == -1)
        {
            return null;
        }

        PatternBlock block = new()
        {
            StartLine = startNum
        };
        var srcLine = block.StartLine;
        block.TargetStart = targetLine;
        var srcMisses = 0;
        block.SrcLines.Add(srcLine, srcLine);
        //block.targetLines.Add(targetLine, targetLine);
        var len = 0;
        QualityInfo qi = new()
        {
            Quality = block.Weigth
        };
        block.QualityInfoList[targetLine] = qi;

        while (!_shouldCancel)
        {
            srcLine++;
            len++;
            //if (srcLine >= block.targetStart)
            //  break;  // prevent to search in the own block
            if (maxBlockLen > 0 && len > maxBlockLen)
            {
                break;
            }

            var nextTargetLine = FindSimilarLine(srcLine, targetLine + 1, processedLinesDict);
            if (nextTargetLine > -1 && nextTargetLine - targetLine - 1 <= maxDiffInBlock)
            {
                block.Weigth += maxDiffInBlock - (nextTargetLine - targetLine - 1) + 1;
                block.EndLine = srcLine;
                //block.targetLines.Add(nextTargetLine, nextTargetLine);
                block.SrcLines.Add(srcLine, srcLine);
                if (nextTargetLine - targetLine > 1)
                {
                    var tempWeight = block.Weigth;
                    for (var tl = targetLine + 1; tl < nextTargetLine; ++tl)
                    {
                        qi = new QualityInfo
                        {
                            Quality = --tempWeight
                        };
                        block.QualityInfoList[tl] = qi;
                    }
                }

                targetLine = nextTargetLine;
                qi = new QualityInfo
                {
                    Quality = block.Weigth
                };
                block.QualityInfoList[targetLine] = qi;
            }
            else
            {
                srcMisses++;
                block.Weigth--;
                targetLine++;
                qi = new QualityInfo
                {
                    Quality = block.Weigth
                };
                block.QualityInfoList[targetLine] = qi;
                if (srcMisses > maxMisses)
                {
                    break;
                }
            }
        }

        block.TargetEnd = targetLine;
        qi = new QualityInfo
        {
            Quality = block.Weigth
        };

        block.QualityInfoList[targetLine] = qi;

        for (var k = block.TargetStart; k <= block.TargetEnd; ++k)
        {
            block.TargetLines.Add(k, k);
        }

        return block;
    }

    private void PrepareDict ()
    {
        _lineHashList.Clear();
        Regex regex = new("\\d");
        Regex regex2 = new("\\S");

        var num = _logFileReader.LineCount;
        for (var i = 0; i < num; ++i)
        {
            var msg = GetMsgForLine(i);
            if (msg != null)
            {
                msg = msg.ToLowerInvariant();
                msg = regex.Replace(msg, "0");
                msg = regex2.Replace(msg, " ");
                var chars = msg.ToCharArray();
                var value = 0;
                var numOfE = 0;
                var numOfA = 0;
                var numOfI = 0;
                foreach (var t in chars)
                {
                    value += t;
                    switch (t)
                    {
                        case 'e':
                            numOfE++;
                            break;
                        case 'a':
                            numOfA++;
                            break;
                        case 'i':
                            numOfI++;
                            break;
                    }
                }

                value += numOfE * 30;
                value += numOfA * 20;
                value += numOfI * 10;
                _lineHashList.Add(value);
            }
        }
    }

    private int FindSimilarLine (int srcLine, int startLine)
    {
        var value = _lineHashList[srcLine];

        var num = _lineHashList.Count;
        for (var i = startLine; i < num; ++i)
        {
            if (Math.Abs(_lineHashList[i] - value) < 3)
            {
                return i;
            }
        }

        return -1;
    }

    // int[,] similarCache;

    private void ResetCache (int num)
    {
        //this.similarCache = new int[num, num];
        //for (int i = 0; i < num; ++i)
        //{
        //  for (int j = 0; j < num; j++)
        //  {
        //    this.similarCache[i, j] = -1;
        //  }
        //}
    }

    private int FindSimilarLine (int srcLine, int startLine, Dictionary<int, int> processedLinesDict)
    {
        var threshold = _patternArgs.Fuzzy;

        var prepared = false;
        Regex regex = null;
        Regex regex2 = null;
        string msgToFind = null;
        CultureInfo culture = CultureInfo.CurrentCulture;

        var num = _logFileReader.LineCount;
        for (var i = startLine; i < num; ++i)
        {
            if (processedLinesDict.ContainsKey(i))
            {
                continue;
            }

            //if (this.similarCache[srcLine, i] != -1)
            //{
            //  if (this.similarCache[srcLine, i] < threshold)
            //  {
            //    return i;
            //  }
            //}
            //else
            {
                if (!prepared)
                {
                    msgToFind = GetMsgForLine(srcLine);
                    regex = new Regex("\\d");
                    regex2 = new Regex("\\W");
                    msgToFind = msgToFind.ToLower(culture);
                    msgToFind = regex.Replace(msgToFind, "0");
                    msgToFind = regex2.Replace(msgToFind, " ");
                    prepared = true;
                }

                var msg = GetMsgForLine(i);
                if (msg != null)
                {
                    msg = regex.Replace(msg, "0");
                    msg = regex2.Replace(msg, " ");
                    var lenDiff = Math.Abs(msg.Length - msgToFind.Length);
                    if (lenDiff > threshold)
                    {
                        //this.similarCache[srcLine, i] = lenDiff;
                        continue;
                    }

                    msg = msg.ToLower(culture);
                    var distance = Util.YetiLevenshtein(msgToFind, msg);
                    //this.similarCache[srcLine, i] = distance;
                    if (distance < threshold)
                    {
                        return i;
                    }
                }
            }
        }

        return -1;
    }

    private string GetMsgForLine (int i)
    {
        ILogLine line = _logFileReader.GetLogLine(i);
        ILogLineColumnizer columnizer = CurrentColumnizer;
        ColumnizerCallback callback = new(this);
        IColumnizedLogLine cols = columnizer.SplitLine(callback, line);
        return cols.ColumnValues.Last().FullValue;
    }

    [SupportedOSPlatform("windows")]
    private void ChangeRowHeight (bool decrease)
    {
        var rowNum = dataGridView.CurrentCellAddress.Y;
        if (rowNum < 0 || rowNum >= dataGridView.RowCount)
        {
            return;
        }

        if (decrease)
        {
            if (!_rowHeightList.TryGetValue(rowNum, out RowHeightEntry? entry))
            {
                return;
            }
            else
            {
                entry.Height -= _lineHeight;
                if (entry.Height <= _lineHeight)
                {
                    _rowHeightList.Remove(rowNum);
                }
            }
        }
        else
        {
            RowHeightEntry entry;
            if (!_rowHeightList.TryGetValue(rowNum, out RowHeightEntry? value))
            {
                entry = new RowHeightEntry
                {
                    LineNum = rowNum,
                    Height = _lineHeight
                };

                _rowHeightList[rowNum] = entry;
            }
            else
            {
                entry = value;
            }

            entry.Height += _lineHeight;
        }

        dataGridView.UpdateRowHeightInfo(rowNum, false);
        if (rowNum == dataGridView.RowCount - 1 && _guiStateArgs.FollowTail)
        {
            dataGridView.FirstDisplayedScrollingRowIndex = rowNum;
        }

        dataGridView.Refresh();
    }

    private int GetRowHeight (int rowNum)
    {
        return _rowHeightList.TryGetValue(rowNum, out RowHeightEntry? value)
            ? value.Height
            : _lineHeight;
    }

    private void AddBookmarkAtLineSilently (int lineNum)
    {
        if (!_bookmarkProvider.IsBookmarkAtLine(lineNum))
        {
            _bookmarkProvider.AddBookmark(new Bookmark(lineNum));
        }
    }

    [SupportedOSPlatform("windows")]
    private void AddBookmarkAndEditComment ()
    {
        var lineNum = dataGridView.CurrentCellAddress.Y;
        if (!_bookmarkProvider.IsBookmarkAtLine(lineNum))
        {
            ToggleBookmark();
        }

        BookmarkComment(_bookmarkProvider.GetBookmarkForLine(lineNum));
    }

    [SupportedOSPlatform("windows")]
    private void AddBookmarkComment (string text)
    {
        var lineNum = dataGridView.CurrentCellAddress.Y;
        Bookmark bookmark;
        if (!_bookmarkProvider.IsBookmarkAtLine(lineNum))
        {
            _bookmarkProvider.AddBookmark(bookmark = new Bookmark(lineNum));
        }
        else
        {
            bookmark = _bookmarkProvider.GetBookmarkForLine(lineNum);
        }

        bookmark.Text += text;
        dataGridView.Refresh();
        filterGridView.Refresh();
        OnBookmarkTextChanged(bookmark);
    }

    [SupportedOSPlatform("windows")]
    private void MarkCurrentFilterRange ()
    {
        _filterParams.RangeSearchText = filterRangeComboBox.Text;
        ColumnizerCallback callback = new(this);
        RangeFinder rangeFinder = new(_filterParams, callback);
        Core.Entities.Range range = rangeFinder.FindRange(dataGridView.CurrentCellAddress.Y);
        if (range != null)
        {
            SetCellSelectionMode(false);
            _noSelectionUpdates = true;
            for (var i = range.StartLine; i <= range.EndLine; ++i)
            {
                dataGridView.Rows[i].Selected = true;
            }

            _noSelectionUpdates = false;
            UpdateSelectionDisplay();
        }
    }

    [SupportedOSPlatform("windows")]
    private void RemoveTempHighlights ()
    {
        lock (_tempHighlightEntryListLock)
        {
            _tempHighlightEntryList.Clear();
        }

        RefreshAllGrids();
    }

    [SupportedOSPlatform("windows")]
    private void ToggleHighlightPanel (bool open)
    {
        highlightSplitContainer.Panel2Collapsed = !open;
        btnToggleHighlightPanel.Image = open
            ? new Bitmap(_panelCloseButtonImage, new Size(btnToggleHighlightPanel.Size.Height, btnToggleHighlightPanel.Size.Height))
            : new Bitmap(_panelOpenButtonImage, new Size(btnToggleHighlightPanel.Size.Height, btnToggleHighlightPanel.Size.Height));
    }

    [SupportedOSPlatform("windows")]
    private void SetBookmarksForSelectedFilterLines ()
    {
        lock (_filterResultList)
        {
            foreach (DataGridViewRow row in filterGridView.SelectedRows)
            {
                var lineNum = _filterResultList[row.Index];
                AddBookmarkAtLineSilently(lineNum);
            }
        }

        dataGridView.Refresh();
        filterGridView.Refresh();
        OnBookmarkAdded();
    }

    private void SetDefaultHighlightGroup ()
    {
        HighlightGroup group = _parentLogTabWin.FindHighlightGroupByFileMask(FileName);
        if (group != null)
        {
            SetCurrentHighlightGroup(group.GroupName);
        }
        else
        {
            SetCurrentHighlightGroup("[Default]");
        }
    }

    [SupportedOSPlatform("windows")]
    private void HandleChangedFilterOnLoadSetting ()
    {
        _parentLogTabWin.Preferences.IsFilterOnLoad = filterOnLoadCheckBox.Checked;
        _parentLogTabWin.Preferences.IsAutoHideFilterList = hideFilterListOnLoadCheckBox.Checked;
        OnFilterListChanged(this);
    }

    private void FireCancelHandlers ()
    {
        lock (_cancelHandlerList)
        {
            foreach (Core.Interface.IBackgroundProcessCancelHandler handler in _cancelHandlerList)
            {
                handler.EscapePressed();
            }
        }
    }

    private void SyncOtherWindows (DateTime timestamp)
    {
        lock (_timeSyncListLock)
        {
            TimeSyncList?.NavigateToTimestamp(timestamp, this);
        }
    }

    [SupportedOSPlatform("windows")]
    private void AddSlaveToTimesync (LogWindow slave)
    {
        lock (_timeSyncListLock)
        {
            if (TimeSyncList == null)
            {
                if (slave.TimeSyncList == null)
                {
                    TimeSyncList = new TimeSyncList();
                    TimeSyncList.AddWindow(this);
                }
                else
                {
                    TimeSyncList = slave.TimeSyncList;
                }

                var currentLineNum = dataGridView.CurrentCellAddress.Y;
                var refLine = currentLineNum;
                DateTime timeStamp = GetTimestampForLine(ref refLine, true);
                if (!timeStamp.Equals(DateTime.MinValue) && !_shouldTimestampDisplaySyncingCancel)
                {
                    TimeSyncList.CurrentTimestamp = timeStamp;
                }

                TimeSyncList.WindowRemoved += OnTimeSyncListWindowRemoved;
            }
        }

        slave.AddToTimeSync(this);
        OnSyncModeChanged();
    }

    private void FreeSlaveFromTimesync (LogWindow slave)
    {
        slave.FreeFromTimeSync();
    }

    private void OnSyncModeChanged ()
    {
        SyncModeChanged?.Invoke(this, new SyncModeEventArgs(IsTimeSynced));
    }

    [SupportedOSPlatform("windows")]
    private void AddSearchHitHighlightEntry (SearchParams para)
    {
        HighlightEntry he = new()
        {
            SearchText = para.SearchText,
            ForegroundColor = Color.Red,
            BackgroundColor = Color.Yellow,
            IsRegEx = para.IsRegex,
            IsCaseSensitive = para.IsCaseSensitive,
            IsLedSwitch = false,
            IsStopTail = false,
            IsSetBookmark = false,
            IsActionEntry = false,
            ActionEntry = null,
            IsWordMatch = true,
            IsSearchHit = true
        };

        lock (_tempHighlightEntryListLock)
        {
            _tempHighlightEntryList.Add(he);
        }

        RefreshAllGrids();
    }

    [SupportedOSPlatform("windows")]
    private void RemoveAllSearchHighlightEntries ()
    {
        lock (_tempHighlightEntryListLock)
        {
            List<HighlightEntry> newList = [];
            foreach (HighlightEntry he in _tempHighlightEntryList)
            {
                if (!he.IsSearchHit)
                {
                    newList.Add(he);
                }
            }

            _tempHighlightEntryList = newList;
        }

        RefreshAllGrids();
    }

    [SupportedOSPlatform("windows")]
    private DataGridViewColumn GetColumnByName (BufferedDataGridView dataGridView, string name)
    {
        foreach (DataGridViewColumn col in dataGridView.Columns)
        {
            if (col.HeaderText.Equals(name, StringComparison.Ordinal))
            {
                return col;
            }
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private void SelectColumn ()
    {
        var colName = columnComboBox.SelectedItem as string;
        DataGridViewColumn col = GetColumnByName(dataGridView, colName);
        if (col != null && !col.Frozen)
        {
            dataGridView.FirstDisplayedScrollingColumnIndex = col.Index;
            var currentLine = dataGridView.CurrentCellAddress.Y;
            if (currentLine >= 0)
            {
                dataGridView.CurrentCell = dataGridView.Rows[dataGridView.CurrentCellAddress.Y].Cells[col.Index];
            }
        }
    }

    #endregion

}