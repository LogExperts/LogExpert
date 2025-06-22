using System.Text;

using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArguments;
using LogExpert.Dialogs;
using LogExpert.UI.Entities;
using LogExpert.UI.Extensions;

namespace LogExpert.UI.Controls.LogWindow;

partial class LogWindow
{
    #region Public methods

    public void LoadFile (string fileName, EncodingOptions encodingOptions)
    {
        EnterLoadFileStatus();

        if (fileName != null)
        {
            FileName = fileName;
            EncodingOptions = encodingOptions;

            if (_logFileReader != null)
            {
                _logFileReader.StopMonitoringAsync();
                UnRegisterLogFileReaderEvents();
            }

            //
            // isUsingDefaultColumnizer is to enable automatically find the best columnizer.
            // When a new log file is opened, and no Columnizer can be chose by file mask,
            // this flag will enable find a columnizer automatically.
            // Current solution is not elegant.
            // Since the refactory will involving a lot of work, we can plan it in the future.
            // One possible solution is, using raw file stream to read the sample lines to help
            // the ColumnizerPicker to determine the priority.
            //
            var isUsingDefaultColumnizer = false;
            if (!LoadPersistenceOptions())
            {
                if (!IsTempFile)
                {
                    ILogLineColumnizer columnizer = FindColumnizer();
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
                        isUsingDefaultColumnizer = true;
                    }
                    PreSelectColumnizer(columnizer);
                }
                SetDefaultHighlightGroup();
            }

            // this may be set after loading persistence data
            if (_fileNames != null && IsMultiFile)
            {
                LoadFilesAsMulti(_fileNames, EncodingOptions);
                return;
            }

            _columnCache = new ColumnCache();

            try
            {
                _logFileReader = new(fileName, EncodingOptions, IsMultiFile, Preferences.BufferCount, Preferences.LinesPerBuffer, _multiFileOptions, PluginRegistry.PluginRegistry.Instance)
                {
                    UseNewReader = !Preferences.UseLegacyReader
                };
            }
            catch (LogFileException lfe)
            {
                _logger.Error(lfe);
                MessageBox.Show("Cannot load file\n" + lfe.Message, "LogExpert");
                _ = BeginInvoke(new FunctionWith1BoolParam(Close), true);
                _isLoadError = true;
                return;
            }

            if (CurrentColumnizer is ILogLineXmlColumnizer xmlColumnizer)
            {
                _logFileReader.IsXmlMode = true;
                _logFileReader.XmlLogConfig = xmlColumnizer.GetXmlLogConfiguration();
            }

            if (_forcedColumnizerForLoading != null)
            {
                CurrentColumnizer = _forcedColumnizerForLoading;
            }

            if (CurrentColumnizer is IPreProcessColumnizer processColumnizer)
            {
                _logFileReader.PreProcessColumnizer = processColumnizer;
            }
            else
            {
                _logFileReader.PreProcessColumnizer = null;
            }

            RegisterLogFileReaderEvents();
            _logger.Info($"Loading logfile: {fileName}");
            _logFileReader.StartMonitoring();

            if (isUsingDefaultColumnizer)
            {
                if (Preferences.AutoPick)
                {
                    ILogLineColumnizer newColumnizer = ColumnizerPicker.FindBetterColumnizer(FileName, _logFileReader, CurrentColumnizer, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

                    if (newColumnizer != null)
                    {
                        _logger.Debug($"Picked new columnizer '{newColumnizer}'");

                        PreSelectColumnizer(newColumnizer);
                    }
                }
            }
        }
    }

    public void LoadFilesAsMulti (string[] fileNames, EncodingOptions encodingOptions)
    {
        _logger.Info("Loading given files as MultiFile:");

        EnterLoadFileStatus();

        foreach (var name in fileNames)
        {
            _logger.Info("File: {0}", name);
        }

        if (_logFileReader != null)
        {
            _logFileReader.StopMonitoring();
            UnRegisterLogFileReaderEvents();
        }

        EncodingOptions = encodingOptions;
        _columnCache = new ColumnCache();

        _logFileReader = new(fileNames, EncodingOptions, Preferences.BufferCount, Preferences.LinesPerBuffer, _multiFileOptions, PluginRegistry.PluginRegistry.Instance)
        {
            UseNewReader = !Preferences.UseLegacyReader
        };

        RegisterLogFileReaderEvents();
        _logFileReader.StartMonitoring();
        FileName = fileNames[^1];
        _fileNames = fileNames;
        IsMultiFile = true;
        //if (this.isTempFile)
        //  this.Text = this.tempTitleName;
        //else
        //  this.Text = Util.GetNameFromPath(this.FileName);
    }

    public string SavePersistenceData (bool force)
    {
        if (!force)
        {
            if (!Preferences.SaveSessions)
            {
                return null;
            }
        }

        if (IsTempFile || _isLoadError)
        {
            return null;
        }

        try
        {
            PersistenceData persistenceData = GetPersistenceData();

            if (ForcedPersistenceFileName == null)
            {
                return Persister.SavePersistenceData(FileName, persistenceData, Preferences);
            }

            return Persister.SavePersistenceDataWithFixedName(ForcedPersistenceFileName, persistenceData);
        }
        catch (IOException ex)
        {
            _logger.Error(ex, "Error saving persistence: ");
        }
        catch (Exception e)
        {
            MessageBox.Show($"Unexpected error while saving persistence: {e.Message}");
        }

        return null;
    }

    public PersistenceData GetPersistenceData ()
    {
        PersistenceData persistenceData = new()
        {
            BookmarkList = _bookmarkProvider.BookmarkList,
            RowHeightList = _rowHeightList,
            MultiFile = IsMultiFile,
            MultiFilePattern = _multiFileOptions.FormatPattern,
            MultiFileMaxDays = _multiFileOptions.MaxDayTry,
            CurrentLine = dataGridView.CurrentCellAddress.Y,
            FirstDisplayedLine = dataGridView.FirstDisplayedScrollingRowIndex,
            FilterVisible = !splitContainerLogWindow.Panel2Collapsed,
            FilterAdvanced = !advancedFilterSplitContainer.Panel1Collapsed,
            FilterPosition = splitContainerLogWindow.SplitterDistance,
            FollowTail = _guiStateArgs.FollowTail,
            FileName = FileName,
            TabName = Text,
            SessionFileName = SessionFileName,
            ColumnizerName = CurrentColumnizer.GetName(),
            LineCount = _logFileReader.LineCount
        };

        _filterParams.IsFilterTail = filterTailCheckBox.Checked; // this option doesnt need a press on 'search'

        if (Preferences.SaveFilters)
        {
            List<FilterParams> filterList = [_filterParams];
            persistenceData.FilterParamsList = filterList;

            foreach (FilterPipe filterPipe in _filterPipeList)
            {
                FilterTabData data = new()
                {
                    PersistenceData = filterPipe.OwnLogWindow.GetPersistenceData(),
                    FilterParams = filterPipe.FilterParams
                };
                persistenceData.FilterTabDataList.Add(data);
            }
        }

        if (_currentHighlightGroup != null)
        {
            persistenceData.HighlightGroupName = _currentHighlightGroup.GroupName;
        }

        if (_fileNames != null && IsMultiFile)
        {
            persistenceData.MultiFileNames.AddRange(_fileNames);
        }

        //persistenceData.showBookmarkCommentColumn = this.bookmarkWindow.ShowBookmarkCommentColumn;
        persistenceData.FilterSaveListVisible = !highlightSplitContainer.Panel2Collapsed;
        persistenceData.Encoding = _logFileReader.CurrentEncoding;

        return persistenceData;
    }

    public void Close (bool dontAsk)
    {
        Preferences.AskForClose = !dontAsk;
        Close();
    }

    public void CloseLogWindow ()
    {
        StopTimespreadThread();
        StopTimestampSyncThread();
        StopLogEventWorkerThread();
        _shouldCancel = true;

        if (_logFileReader != null)
        {
            UnRegisterLogFileReaderEvents();
            _logFileReader.StopMonitoringAsync();
            //this.logFileReader.DeleteAllContent();
        }

        if (_isLoading)
        {
            _waitingForClose = true;
        }

        if (IsTempFile)
        {
            _logger.Info("Deleting temp file {0}", FileName);

            try
            {
                File.Delete(FileName);
            }
            catch (IOException e)
            {
                _logger.Error(e, "Error while deleting temp file {0}: {1}", FileName, e);
            }
        }

        FilterPipe?.CloseAndDisconnect();
        DisconnectFilterPipes();
    }

    public void WaitForLoadingFinished ()
    {
        _externaLoadingFinishedEvent.WaitOne();
    }

    public void ForceColumnizer (ILogLineColumnizer columnizer)
    {
        //TODO this needs to be refactored
        var directory = ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir;

        _forcedColumnizer = ColumnizerPicker.CloneColumnizer(columnizer, directory);
        SetColumnizer(_forcedColumnizer);
    }

    public void ForceColumnizerForLoading (ILogLineColumnizer columnizer)
    {
        //TODO this needs to be refactored
        var directory = ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir;

        _forcedColumnizerForLoading = ColumnizerPicker.CloneColumnizer(columnizer, directory);
    }

    public void PreselectColumnizer (string columnizerName)
    {
        //TODO this needs to be refactored
        var directory = ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir;

        ILogLineColumnizer columnizer = ColumnizerPicker.FindColumnizerByName(columnizerName, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);
        PreSelectColumnizer(ColumnizerPicker.CloneColumnizer(columnizer, directory));
    }

    public void ColumnizerConfigChanged ()
    {
        SetColumnizerInternal(CurrentColumnizer);
    }

    public void SetColumnizer (ILogLineColumnizer columnizer, BufferedDataGridView gridView)
    {
        PaintHelper.SetColumnizer(columnizer, gridView);

        gridView.Refresh();
        AutoResizeColumns(gridView);
        ApplyFrozenState(gridView);
    }

    public IColumn GetCellValue (int rowIndex, int columnIndex)
    {
        if (columnIndex == 1)
        {
            return new Column
            {
                FullValue = (rowIndex + 1).ToString() // line number
            };
        }

        if (columnIndex == 0) // marker column
        {
            return Column.EmptyColumn;
        }

        try
        {
            IColumnizedLogLine cols = GetColumnsForLine(rowIndex);
            if (cols != null && cols.ColumnValues != null)
            {
                if (columnIndex <= cols.ColumnValues.Length + 1)
                {
                    IColumn value = cols.ColumnValues[columnIndex - 2];

                    if (value != null && value.DisplayValue != null)
                    {
                        return value;
                    }
                    return value;
                }

                if (columnIndex == 2)
                {
                    return cols.ColumnValues[^1];
                }

                return Column.EmptyColumn;
            }
        }
        catch
        {
            return Column.EmptyColumn;
        }

        return Column.EmptyColumn;
    }

    public void CellPainting (BufferedDataGridView gridView, int rowIndex, DataGridViewCellPaintingEventArgs e)
    {
        if (rowIndex < 0 || e.ColumnIndex < 0)
        {
            e.Handled = false;
            //return;
        }

        //ILogLine line = _logFileReader.GetLogLineWithWait(rowIndex).Result;
        ILogLine line = null;
        if (line != null)
        {
            HighlightEntry entry = FindFirstNoWordMatchHilightEntry(line);
            e.Graphics.SetClip(e.CellBounds);

            if ((e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected)
            {
                Color backColor = ColorMode.BackgroundColor;

                Brush brush;

                if (gridView.Focused)
                {
                    brush = new SolidBrush(e.CellStyle.SelectionBackColor);
                }
                else
                {
                    Color color = backColor;
                    brush = new SolidBrush(color);
                }

                e.Graphics.FillRectangle(brush, e.CellBounds);
                brush.Dispose();
            }
            else
            {
                Color bgColor = ColorMode.DockBackgroundColor;

                if (!DebugOptions.DisableWordHighlight)
                {
                    if (entry != null)
                    {
                        bgColor = entry.BackgroundColor;
                    }
                }
                else
                {
                    if (entry != null)
                    {
                        bgColor = entry.BackgroundColor;
                    }
                }

                e.CellStyle.BackColor = bgColor;

                e.PaintBackground(e.ClipBounds, false);
            }

            if (DebugOptions.DisableWordHighlight)
            {
                e.PaintContent(e.CellBounds);
            }
            else
            {
                PaintCell(e, gridView, false, entry);
            }

            if (e.ColumnIndex == 0)
            {
                if (_bookmarkProvider.IsBookmarkAtLine(rowIndex))
                {
                    Rectangle r; // = new Rectangle(e.CellBounds.Left + 2, e.CellBounds.Top + 2, 6, 6);
                    r = e.CellBounds;
                    r.Inflate(-2, -2);
                    Brush brush = new SolidBrush(BookmarkColor);
                    e.Graphics.FillRectangle(brush, r);
                    brush.Dispose();
                    Bookmark bookmark = _bookmarkProvider.GetBookmarkForLine(rowIndex);

                    if (bookmark.Text.Length > 0)
                    {
                        StringFormat format = new()
                        {
                            LineAlignment = StringAlignment.Center,
                            Alignment = StringAlignment.Center
                        };
                        Brush brush2 = new SolidBrush(Color.FromArgb(255, 190, 100, 0));
                        Font font = new("Courier New", Preferences.FontSize, FontStyle.Bold);
                        e.Graphics.DrawString("i", font, brush2, new RectangleF(r.Left, r.Top, r.Width, r.Height),
                            format);
                        font.Dispose();
                        brush2.Dispose();
                    }
                }
            }

            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
            e.Handled = true;
        }
    }

    public void OnDataGridView_CellPainting (object sender, DataGridViewCellPaintingEventArgs e)
    {
        var gridView = (BufferedDataGridView)sender;
        CellPainting(gridView, e.RowIndex, e);
    }

    /// <summary>
    /// Returns the first HilightEntry that matches the given line
    /// </summary>
    /// <param name="line"></param>
    /// <param name="noWordMatches"></param>
    /// <returns></returns>
    public HighlightEntry FindHighlightEntry (ITextValue line, bool noWordMatches)
    {
        // first check the temp entries
        lock (_tempHighlightEntryListLock)
        {
            foreach (HighlightEntry entry in _tempHighlightEntryList)
            {
                if (noWordMatches && entry.IsWordMatch)
                {
                    continue;
                }

                if (CheckHighlightEntryMatch(entry, line))
                {
                    return entry;
                }
            }
        }

        lock (_currentHighlightGroupLock)
        {
            foreach (HighlightEntry entry in _currentHighlightGroup.HighlightEntryList)
            {
                if (noWordMatches && entry.IsWordMatch)
                {
                    continue;
                }
                if (CheckHighlightEntryMatch(entry, line))
                {
                    return entry;
                }
            }
            return null;
        }
    }

    public IList<HighlightMatchEntry> FindHighlightMatches (ITextValue column)
    {
        IList<HighlightMatchEntry> resultList = [];

        if (column != null)
        {
            lock (_currentHighlightGroupLock)
            {
                GetHighlightEntryMatches(column, _currentHighlightGroup.HighlightEntryList, resultList);
            }

            lock (_tempHighlightEntryList)
            {
                GetHighlightEntryMatches(column, _tempHighlightEntryList, resultList);
            }
        }

        return resultList;
    }

    public void FollowTailChanged (bool isChecked, bool byTrigger)
    {
        _guiStateArgs.FollowTail = isChecked;

        if (_guiStateArgs.FollowTail && _logFileReader != null)
        {
            if (dataGridView.RowCount >= _logFileReader.LineCount && _logFileReader.LineCount > 0)
            {
                dataGridView.FirstDisplayedScrollingRowIndex = _logFileReader.LineCount - 1;
            }
        }
        BeginInvoke(new MethodInvoker(dataGridView.Refresh));
        //this.dataGridView.Refresh();
        _parentLogTabWin.FollowTailChanged(this, isChecked, byTrigger);
        SendGuiStateUpdate();
    }

    public void TryToTruncate ()
    {
        try
        {
            if (LockFinder.CheckIfFileIsLocked(Title))
            {
                var name = LockFinder.FindLockedProcessName(Title);
                StatusLineText($"Truncate failed: file is locked by {name}");
            }
            else
            {
                File.WriteAllText(Title, "");
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Unexpected issue truncating file: {ex.Message}");
            StatusLineText("Unexpected issue truncating file");
            throw;
        }
    }

    public void GotoLine (int line)
    {
        if (line >= 0)
        {
            if (line < dataGridView.RowCount)
            {
                SelectLine(line, false, true);
            }
            else
            {
                SelectLine(dataGridView.RowCount - 1, false, true);
            }
            dataGridView.Focus();
        }
    }

    public void StartSearch ()
    {
        _guiStateArgs.MenuEnabled = false;
        GuiStateUpdate(this, _guiStateArgs);
        SearchParams searchParams = _parentLogTabWin.SearchParams;

        if ((searchParams.IsForward || searchParams.IsFindNext) && !searchParams.IsShiftF3Pressed)
        {
            searchParams.CurrentLine = dataGridView.CurrentCellAddress.Y + 1;
        }
        else
        {
            searchParams.CurrentLine = dataGridView.CurrentCellAddress.Y - 1;
        }

        _currentSearchParams = searchParams; // remember for async "not found" messages

        _isSearching = true;
        _shouldCancel = false;
        StatusLineText("Searching... Press ESC to cancel.");

        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = dataGridView.RowCount;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        Task.Run(() => Search(searchParams)).ContinueWith(SearchComplete);

        RemoveAllSearchHighlightEntries();
        AddSearchHitHighlightEntry(searchParams);
    }

    private void SearchComplete (Task<int> task)
    {
        if (Disposing)
        {
            return;
        }

        try
        {
            Invoke(new MethodInvoker(ResetProgressBar));
            var line = task.Result;
            _guiStateArgs.MenuEnabled = true;
            GuiStateUpdate(this, _guiStateArgs);
            if (line == -1)
            {
                return;
            }

            dataGridView.Invoke(new SelectLineFx((line1, triggerSyncCall) => SelectLine(line1, triggerSyncCall, true)), line, true);
        }
        catch (Exception ex) // in the case the windows is already destroyed
        {
            _logger.Warn(ex);
        }
    }

    public void SelectLogLine (int line)
    {
        Invoke(new SelectLineFx((line1, triggerSyncCall) => SelectLine(line1, triggerSyncCall, true)), line, true);
    }

    public void SelectAndEnsureVisible (int line, bool triggerSyncCall)
    {
        try
        {
            SelectLine(line, triggerSyncCall, false);

            //if (!this.dataGridView.CurrentRow.Displayed)
            if (line < dataGridView.FirstDisplayedScrollingRowIndex || line > dataGridView.FirstDisplayedScrollingRowIndex + dataGridView.DisplayedRowCount(false))
            {
                dataGridView.FirstDisplayedScrollingRowIndex = line;
                for (var i = 0; i < 8 && dataGridView.FirstDisplayedScrollingRowIndex > 0 && line < dataGridView.FirstDisplayedScrollingRowIndex + dataGridView.DisplayedRowCount(false); ++i)
                {
                    dataGridView.FirstDisplayedScrollingRowIndex -= 1;
                }

                if (line >= dataGridView.FirstDisplayedScrollingRowIndex + dataGridView.DisplayedRowCount(false))
                {
                    dataGridView.FirstDisplayedScrollingRowIndex += 1;
                }
            }

            dataGridView.CurrentCell = dataGridView.Rows[line].Cells[0];
        }
        catch (Exception e)
        {
            // In rare situations there seems to be an invalid argument exceptions (or something like this). Concrete location isn't visible in stack
            // trace because use of Invoke(). So catch it, and log (better than crashing the app).
            _logger.Error(e);
        }
    }

    public void OnLogWindowKeyDown (object sender, KeyEventArgs e)
    {
        if (_isErrorShowing)
        {
            RemoveStatusLineError();
        }

        switch (e.KeyCode)
        {
            case Keys.F3 when _parentLogTabWin.SearchParams?.SearchText == null || _parentLogTabWin.SearchParams.SearchText.Length == 0:
                {
                    return;
                }
            case Keys.F3:
                {
                    _parentLogTabWin.SearchParams.IsFindNext = true;
                    _parentLogTabWin.SearchParams.IsShiftF3Pressed = (e.Modifiers & Keys.Shift) == Keys.Shift;
                    StartSearch();
                    break;
                }
            case Keys.Escape:
                {
                    if (_isSearching)
                    {
                        _shouldCancel = true;
                    }

                    FireCancelHandlers();
                    RemoveAllSearchHighlightEntries();
                    break;
                }
            case Keys.E when (e.Modifiers & Keys.Control) == Keys.Control:
                {
                    StartEditMode();

                    break;
                }
            case Keys.Down when e.Modifiers == Keys.Alt:
                {
                    var newLine = _logFileReader.GetNextMultiFileLine(dataGridView.CurrentCellAddress.Y);

                    if (newLine != -1)
                    {
                        SelectLine(newLine, false, true);
                    }

                    e.Handled = true;

                    break;
                }
            case Keys.Up when e.Modifiers == Keys.Alt:
                {
                    var newLine = _logFileReader.GetPrevMultiFileLine(dataGridView.CurrentCellAddress.Y);

                    if (newLine != -1)
                    {
                        SelectLine(newLine - 1, false, true);
                    }

                    e.Handled = true;

                    break;
                }
            case Keys.Enter when dataGridView.Focused:
                {
                    ChangeRowHeight(e.Shift);
                    e.Handled = true;

                    break;
                }
            case Keys.Back when dataGridView.Focused:
                {
                    ChangeRowHeight(true);
                    e.Handled = true;

                    break;
                }
            case Keys.PageUp when e.Modifiers == Keys.Alt:
                {
                    SelectPrevHighlightLine();
                    e.Handled = true;

                    break;
                }
            case Keys.PageDown when e.Modifiers == Keys.Alt:
                {
                    SelectNextHighlightLine();
                    e.Handled = true;

                    break;
                }
            case Keys.T when (e.Modifiers & Keys.Control) == Keys.Control && (e.Modifiers & Keys.Shift) == Keys.Shift:
                {
                    FilterToTab();
                    break;
                }
        }
    }

    public void AddBookmarkOverlays ()
    {
        const int OVERSCAN = 20;

        var firstLine = dataGridView.FirstDisplayedScrollingRowIndex;
        if (firstLine < 0)
        {
            return;
        }

        firstLine -= OVERSCAN;
        if (firstLine < 0)
        {
            firstLine = 0;
        }

        var oversizeCount = OVERSCAN;

        for (var i = firstLine; i < dataGridView.RowCount; ++i)
        {
            if (!dataGridView.Rows[i].Displayed && i > dataGridView.FirstDisplayedScrollingRowIndex)
            {
                if (oversizeCount-- < 0)
                {
                    break;
                }
            }

            if (_bookmarkProvider.IsBookmarkAtLine(i))
            {
                Bookmark bookmark = _bookmarkProvider.GetBookmarkForLine(i);
                if (bookmark.Text.Length > 0)
                {
                    //BookmarkOverlay overlay = new BookmarkOverlay();
                    BookmarkOverlay overlay = bookmark.Overlay;
                    overlay.Bookmark = bookmark;

                    Rectangle r;
                    if (dataGridView.Rows[i].Displayed)
                    {
                        r = dataGridView.GetCellDisplayRectangle(0, i, false);
                    }
                    else
                    {
                        r = dataGridView.GetCellDisplayRectangle(0, dataGridView.FirstDisplayedScrollingRowIndex, false);
                        //int count = i - this.dataGridView.FirstDisplayedScrollingRowIndex;
                        var heightSum = 0;
                        if (dataGridView.FirstDisplayedScrollingRowIndex < i)
                        {
                            for (var rn = dataGridView.FirstDisplayedScrollingRowIndex + 1; rn < i; ++rn)
                            {
                                //Rectangle rr = this.dataGridView.GetCellDisplayRectangle(0, rn, false);
                                //heightSum += rr.Height;
                                heightSum += GetRowHeight(rn);
                            }

                            r.Offset(0, r.Height + heightSum);
                        }
                        else
                        {
                            for (var rn = dataGridView.FirstDisplayedScrollingRowIndex + 1; rn > i; --rn)
                            {
                                //Rectangle rr = this.dataGridView.GetCellDisplayRectangle(0, rn, false);
                                //heightSum += rr.Height;
                                heightSum += GetRowHeight(rn);
                            }

                            r.Offset(0, -(r.Height + heightSum));
                        }
                        //r.Offset(0, this.dataGridView.DisplayRectangle.Height);
                    }

                    if (_logger.IsDebugEnabled)
                    {
                        _logger.Debug($"AddBookmarkOverlay() r.Location={r.Location.X}, width={r.Width}, scroll_offset={dataGridView.HorizontalScrollingOffset}");
                    }

                    overlay.Position = r.Location - new Size(dataGridView.HorizontalScrollingOffset, 0);
                    overlay.Position += new Size(10, r.Height / 2);
                    dataGridView.AddOverlay(overlay);
                }
            }
        }
    }

    public void ToggleBookmark ()
    {
        BufferedDataGridView gridView;
        int lineNum;

        if (filterGridView.Focused)
        {
            gridView = filterGridView;
            if (gridView.CurrentCellAddress.Y == -1)
            {
                return;
            }

            lineNum = _filterResultList[gridView.CurrentCellAddress.Y];
        }
        else
        {
            gridView = dataGridView;
            if (gridView.CurrentCellAddress.Y == -1)
            {
                return;
            }
            lineNum = dataGridView.CurrentCellAddress.Y;
        }

        ToggleBookmark(lineNum);
    }

    public void ToggleBookmark (int lineNum)
    {
        if (_bookmarkProvider.IsBookmarkAtLine(lineNum))
        {
            Bookmark bookmark = _bookmarkProvider.GetBookmarkForLine(lineNum);

            if (string.IsNullOrEmpty(bookmark.Text) == false)
            {
                if (DialogResult.No == MessageBox.Show("There's a comment attached to the bookmark. Really remove the bookmark?", "LogExpert", MessageBoxButtons.YesNo))
                {
                    return;
                }
            }
            _bookmarkProvider.RemoveBookmarkForLine(lineNum);
        }
        else
        {
            _bookmarkProvider.AddBookmark(new Bookmark(lineNum));
        }
        dataGridView.Refresh();
        filterGridView.Refresh();
        OnBookmarkAdded();
    }

    public void SetBookmarkFromTrigger (int lineNum, string comment)
    {
        lock (_bookmarkLock)
        {
            ILogLine line = _logFileReader.GetLogLine(lineNum);
            if (line == null)
            {
                return;
            }
            var paramParser = new ParamParser(comment);
            try
            {
                comment = paramParser.ReplaceParams(line, lineNum, FileName);
            }
            catch (ArgumentException)
            {
                // occurs on invalid regex
            }
            if (_bookmarkProvider.IsBookmarkAtLine(lineNum))
            {
                _bookmarkProvider.RemoveBookmarkForLine(lineNum);
            }
            _bookmarkProvider.AddBookmark(new Bookmark(lineNum, comment));
            OnBookmarkAdded();
        }
    }

    public void JumpNextBookmark ()
    {
        if (_bookmarkProvider.Bookmarks.Count > 0)
        {
            if (filterGridView.Focused)
            {
                var index = FindNextBookmarkIndex(_filterResultList[filterGridView.CurrentCellAddress.Y]);
                var startIndex = index;
                var wrapped = false;
                while (true)
                {
                    var lineNum = _bookmarkProvider.Bookmarks[index].LineNum;
                    if (_filterResultList.Contains(lineNum))
                    {
                        var filterLine = _filterResultList.IndexOf(lineNum);
                        filterGridView.Rows[filterLine].Selected = true;
                        filterGridView.CurrentCell = filterGridView.Rows[filterLine].Cells[0];
                        break;
                    }
                    index++;
                    if (index > _bookmarkProvider.Bookmarks.Count - 1)
                    {
                        index = 0;
                        wrapped = true;
                    }
                    if (index >= startIndex && wrapped)
                    {
                        break;
                    }
                }
            }
            else
            {
                var index = FindNextBookmarkIndex(dataGridView.CurrentCellAddress.Y);
                if (index > _bookmarkProvider.Bookmarks.Count - 1)
                {
                    index = 0;
                }

                var lineNum = _bookmarkProvider.Bookmarks[index].LineNum;
                SelectLine(lineNum, true, true);
            }
        }
    }

    public void JumpPrevBookmark ()
    {
        if (_bookmarkProvider.Bookmarks.Count > 0)
        {
            if (filterGridView.Focused)
            {
                //int index = this.bookmarkList.BinarySearch(this.filterResultList[this.filterGridView.CurrentCellAddress.Y]);
                //if (index < 0)
                //  index = ~index;
                //index--;
                var index = FindPrevBookmarkIndex(_filterResultList[filterGridView.CurrentCellAddress.Y]);
                if (index < 0)
                {
                    index = _bookmarkProvider.Bookmarks.Count - 1;
                }
                var startIndex = index;
                var wrapped = false;
                while (true)
                {
                    var lineNum = _bookmarkProvider.Bookmarks[index].LineNum;
                    if (_filterResultList.Contains(lineNum))
                    {
                        var filterLine = _filterResultList.IndexOf(lineNum);
                        filterGridView.Rows[filterLine].Selected = true;
                        filterGridView.CurrentCell = filterGridView.Rows[filterLine].Cells[0];
                        break;
                    }
                    index--;
                    if (index < 0)
                    {
                        index = _bookmarkProvider.Bookmarks.Count - 1;
                        wrapped = true;
                    }
                    if (index <= startIndex && wrapped)
                    {
                        break;
                    }
                }
            }
            else
            {
                var index = FindPrevBookmarkIndex(dataGridView.CurrentCellAddress.Y);
                if (index < 0)
                {
                    index = _bookmarkProvider.Bookmarks.Count - 1;
                }

                var lineNum = _bookmarkProvider.Bookmarks[index].LineNum;
                SelectLine(lineNum, false, true);
            }
        }
    }

    public void DeleteBookmarks (List<int> lineNumList)
    {
        var bookmarksPresent = false;
        foreach (var lineNum in lineNumList)
        {
            if (lineNum != -1)
            {
                if (_bookmarkProvider.IsBookmarkAtLine(lineNum) &&
                    _bookmarkProvider.GetBookmarkForLine(lineNum).Text.Length > 0)
                {
                    bookmarksPresent = true;
                }
            }
        }
        if (bookmarksPresent)
        {
            if (
                MessageBox.Show("There are some comments in the bookmarks. Really remove bookmarks?", "LogExpert",
                    MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }
        }
        _bookmarkProvider.RemoveBookmarksForLines(lineNumList);
        OnBookmarkRemoved();
    }

    public void SetTimeshiftValue (string value)
    {
        _guiStateArgs.TimeshiftText = value;
        if (CurrentColumnizer.IsTimeshiftImplemented())
        {
            try
            {
                if (_guiStateArgs.TimeshiftEnabled)
                {
                    try
                    {
                        var text = _guiStateArgs.TimeshiftText;
                        if (text.StartsWith("+"))
                        {
                            text = text.Substring(1);
                        }
                        var timeSpan = TimeSpan.Parse(text);
                        var diff = (int)(timeSpan.Ticks / TimeSpan.TicksPerMillisecond);
                        CurrentColumnizer.SetTimeOffset(diff);
                    }
                    catch (Exception)
                    {
                        CurrentColumnizer.SetTimeOffset(0);
                    }
                }
                else
                {
                    CurrentColumnizer.SetTimeOffset(0);
                }
                dataGridView.Refresh();
                filterGridView.Refresh();
                if (CurrentColumnizer.IsTimeshiftImplemented())
                {
                    SetTimestampLimits();
                    SyncTimestampDisplay();
                }
            }
            catch (FormatException ex)
            {
                _logger.Error(ex);
            }
        }
    }

    public void ToggleFilterPanel ()
    {
        splitContainerLogWindow.Panel2Collapsed = !splitContainerLogWindow.Panel2Collapsed;
        if (!splitContainerLogWindow.Panel2Collapsed)
        {
            filterComboBox.Focus();
        }
        else
        {
            dataGridView.Focus();
        }
    }

    public void LogWindowActivated ()
    {
        if (_guiStateArgs.FollowTail && !_isDeadFile)
        {
            OnTailFollowed(EventArgs.Empty);
        }

        if (Preferences.TimestampControl)
        {
            SetTimestampLimits();
            SyncTimestampDisplay();
        }

        dataGridView.Focus();

        SendGuiStateUpdate();
        SendStatusLineUpdate();
        SendProgressBarUpdate();
    }

    public void SetCellSelectionMode (bool isCellMode)
    {
        if (isCellMode)
        {
            //possible performance issue, see => https://docs.microsoft.com/en-us/dotnet/desktop/winforms/controls/best-practices-for-scaling-the-windows-forms-datagridview-control?view=netframeworkdesktop-4.8#using-the-selected-cells-rows-and-columns-collections-efficiently
            dataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;
        }
        else
        {
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        _guiStateArgs.CellSelectMode = isCellMode;
    }

    public void TimeshiftEnabled (bool isEnabled, string shiftValue)
    {
        _guiStateArgs.TimeshiftEnabled = isEnabled;
        SetTimestampLimits();
        SetTimeshiftValue(shiftValue);
    }

    public void CopyMarkedLinesToTab ()
    {
        if (dataGridView.SelectionMode == DataGridViewSelectionMode.FullRowSelect)
        {
            var lineNumList = new List<int>();
            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                if (row.Index != -1)
                {
                    lineNumList.Add(row.Index);
                }
            }

            lineNumList.Sort();
            // create dummy FilterPipe for connecting line numbers to original window
            // setting IsStopped to true prevents further filter processing
            var pipe = new FilterPipe(new FilterParams(), this)
            {
                IsStopped = true
            };
            WritePipeToTab(pipe, lineNumList, Text + "->C", null);
        }
        else
        {
            var fileName = Path.GetTempFileName();
            var fStream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read);
            var writer = new StreamWriter(fStream, Encoding.Unicode);

            DataObject data = dataGridView.GetClipboardContent();
            var text = data.GetText(TextDataFormat.Text);
            writer.Write(text);

            writer.Close();
            var title = Util.GetNameFromPath(FileName) + "->Clip";
            _parentLogTabWin.AddTempFileTab(fileName, title);
        }
    }

    /// <summary>
    /// Change the file encoding. May force a reload if byte count ot preamble lenght differs from previous used encoding.
    /// </summary>
    /// <param name="encoding"></param>
    public void ChangeEncoding (Encoding encoding)
    {
        _logFileReader.ChangeEncoding(encoding);
        EncodingOptions.Encoding = encoding;
        if (_guiStateArgs.CurrentEncoding.IsSingleByte != encoding.IsSingleByte ||
            _guiStateArgs.CurrentEncoding.GetPreamble().Length != encoding.GetPreamble().Length)
        {
            Reload();
        }
        else
        {
            dataGridView.Refresh();
            SendGuiStateUpdate();
        }
        _guiStateArgs.CurrentEncoding = _logFileReader.CurrentEncoding;
    }

    public void Reload ()
    {
        SavePersistenceData(false);

        _reloadMemento = new ReloadMemento
        {
            CurrentLine = dataGridView.CurrentCellAddress.Y,
            FirstDisplayedLine = dataGridView.FirstDisplayedScrollingRowIndex
        };
        _forcedColumnizerForLoading = CurrentColumnizer;

        if (_fileNames == null || !IsMultiFile)
        {
            LoadFile(FileName, EncodingOptions);
        }
        else
        {
            LoadFilesAsMulti(_fileNames, EncodingOptions);
        }
        //if (currentLine < this.dataGridView.RowCount && currentLine >= 0)
        //  this.dataGridView.CurrentCell = this.dataGridView.Rows[currentLine].Cells[0];
        //if (firstDisplayedLine < this.dataGridView.RowCount && firstDisplayedLine >= 0)
        //  this.dataGridView.FirstDisplayedScrollingRowIndex = firstDisplayedLine;

        //if (this.filterTailCheckBox.Checked)
        //{
        //  _logger.logInfo("Refreshing filter view because of reload.");
        //  FilterSearch();
        //}
    }

    public void PreferencesChanged (string fontName, float fontSize, bool setLastColumnWidth, int lastColumnWidth, bool isLoadTime, SettingsFlags flags)
    {
        if ((flags & SettingsFlags.GuiOrColors) == SettingsFlags.GuiOrColors)
        {
            NormalFont = new Font(new FontFamily(fontName), fontSize);
            BoldFont = new Font(NormalFont, FontStyle.Bold);
            MonospacedFont = new Font("Courier New", Preferences.FontSize, FontStyle.Bold);

            var lineSpacing = NormalFont.FontFamily.GetLineSpacing(FontStyle.Regular);
            var lineSpacingPixel = NormalFont.Size * lineSpacing / NormalFont.FontFamily.GetEmHeight(FontStyle.Regular);

            dataGridView.DefaultCellStyle.Font = NormalFont;
            filterGridView.DefaultCellStyle.Font = NormalFont;
            _lineHeight = NormalFont.Height + 4;
            dataGridView.RowTemplate.Height = NormalFont.Height + 4;

            ShowBookmarkBubbles = Preferences.ShowBubbles;

            ApplyDataGridViewPrefs(dataGridView, setLastColumnWidth, lastColumnWidth);
            ApplyDataGridViewPrefs(filterGridView, setLastColumnWidth, lastColumnWidth);

            if (Preferences.TimestampControl)
            {
                SetTimestampLimits();
                SyncTimestampDisplay();
            }

            if (isLoadTime)
            {
                filterTailCheckBox.Checked = Preferences.FilterTail;
                syncFilterCheckBox.Checked = Preferences.FilterSync;
                //this.FollowTailChanged(this.Preferences.followTail, false);
            }

            _timeSpreadCalc.TimeMode = Preferences.TimeSpreadTimeMode;
            timeSpreadingControl.ForeColor = Preferences.TimeSpreadColor;
            timeSpreadingControl.ReverseAlpha = Preferences.ReverseAlpha;

            if (CurrentColumnizer.IsTimeshiftImplemented())
            {
                timeSpreadingControl.Invoke(new MethodInvoker(timeSpreadingControl.Refresh));
                ShowTimeSpread(Preferences.ShowTimeSpread);
            }

            ToggleColumnFinder(Preferences.ShowColumnFinder, false);
        }

        if ((flags & SettingsFlags.FilterList) == SettingsFlags.FilterList)
        {
            HandleChangedFilterList();
        }

        if ((flags & SettingsFlags.FilterHistory) == SettingsFlags.FilterHistory)
        {
            UpdateFilterHistoryFromSettings();

            if (isLoadTime)
            {
                AutoResizeFilterBox();
            }
        }
    }

    public bool ScrollToTimestamp (DateTime timestamp, bool roundToSeconds, bool triggerSyncCall)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new ScrollToTimestampFx(ScrollToTimestampWorker), timestamp, roundToSeconds, triggerSyncCall);
            return true;
        }

        return ScrollToTimestampWorker(timestamp, roundToSeconds, triggerSyncCall);
    }

    public bool ScrollToTimestampWorker (DateTime timestamp, bool roundToSeconds, bool triggerSyncCall)
    {
        var hasScrolled = false;
        if (!CurrentColumnizer.IsTimeshiftImplemented() || dataGridView.RowCount == 0)
        {
            return false;
        }

        //this.Cursor = Cursors.WaitCursor;
        var currentLine = dataGridView.CurrentCellAddress.Y;
        if (currentLine < 0 || currentLine >= dataGridView.RowCount)
        {
            currentLine = 0;
        }
        var foundLine = FindTimestampLine(currentLine, timestamp, roundToSeconds);
        if (foundLine >= 0)
        {
            SelectAndEnsureVisible(foundLine, triggerSyncCall);
            hasScrolled = true;
        }
        //this.Cursor = Cursors.Default;
        return hasScrolled;
    }

    public int FindTimestampLine (int lineNum, DateTime timestamp, bool roundToSeconds)
    {
        var foundLine =
            FindTimestampLine_Internal(lineNum, 0, dataGridView.RowCount - 1, timestamp, roundToSeconds);
        if (foundLine >= 0)
        {
            // go backwards to the first occurence of the hit
            DateTime foundTimestamp = GetTimestampForLine(ref foundLine, roundToSeconds);
            while (foundTimestamp.CompareTo(timestamp) == 0 && foundLine >= 0)
            {
                foundLine--;
                foundTimestamp = GetTimestampForLine(ref foundLine, roundToSeconds);
            }
            if (foundLine < 0)
            {
                return 0;
            }

            foundLine++;
            GetTimestampForLineForward(ref foundLine, roundToSeconds); // fwd to next valid timestamp
            return foundLine;
        }

        return -foundLine;
    }

    public int FindTimestampLine_Internal (int lineNum, int rangeStart, int rangeEnd, DateTime timestamp,
        bool roundToSeconds)
    {
        _logger.Debug("FindTimestampLine_Internal(): timestamp={0}, lineNum={1}, rangeStart={2}, rangeEnd={3}", timestamp, lineNum, rangeStart, rangeEnd);
        var refLine = lineNum;
        DateTime currentTimestamp = GetTimestampForLine(ref refLine, roundToSeconds);
        if (currentTimestamp.CompareTo(timestamp) == 0)
        {
            return lineNum;
        }

        if (timestamp < currentTimestamp)
        {
            //rangeStart = rangeStart;
            rangeEnd = lineNum;
        }
        else
        {
            rangeStart = lineNum;
            //rangeEnd = rangeEnd;
        }

        if (rangeEnd - rangeStart <= 0)
        {
            return -lineNum;
        }

        lineNum = ((rangeEnd - rangeStart) / 2) + rangeStart;
        // prevent endless loop
        if (rangeEnd - rangeStart < 2)
        {
            currentTimestamp = GetTimestampForLine(ref rangeStart, roundToSeconds);
            if (currentTimestamp.CompareTo(timestamp) == 0)
            {
                return rangeStart;
            }

            currentTimestamp = GetTimestampForLine(ref rangeEnd, roundToSeconds);

            return currentTimestamp.CompareTo(timestamp) == 0
                ? rangeEnd
                : -lineNum;
        }

        return FindTimestampLine_Internal(lineNum, rangeStart, rangeEnd, timestamp, roundToSeconds);
    }

    /**
   * Get the timestamp for the given line number. If the line
   * has no timestamp, the previous line will be checked until a
   * timestamp is found.
   */
    public DateTime GetTimestampForLine (ref int lineNum, bool roundToSeconds)
    {
        lock (_currentColumnizerLock)
        {
            if (!CurrentColumnizer.IsTimeshiftImplemented())
            {
                return DateTime.MinValue;
            }

            _logger.Debug($"GetTimestampForLine({lineNum}) enter");
            DateTime timeStamp = DateTime.MinValue;
            var lookBack = false;
            if (lineNum >= 0 && lineNum < dataGridView.RowCount)
            {
                while (timeStamp.CompareTo(DateTime.MinValue) == 0 && lineNum >= 0)
                {
                    if (_isTimestampDisplaySyncing && _shouldTimestampDisplaySyncingCancel)
                    {
                        return DateTime.MinValue;
                    }

                    lookBack = true;
                    ILogLine logLine = _logFileReader.GetLogLine(lineNum);
                    if (logLine == null)
                    {
                        return DateTime.MinValue;
                    }

                    ColumnizerCallbackObject.LineNum = lineNum;
                    timeStamp = CurrentColumnizer.GetTimestamp(ColumnizerCallbackObject, logLine);
                    if (roundToSeconds)
                    {
                        timeStamp = timeStamp.Subtract(TimeSpan.FromMilliseconds(timeStamp.Millisecond));
                    }

                    lineNum--;
                }
            }

            if (lookBack)
            {
                lineNum++;
            }

            _logger.Debug("GetTimestampForLine() leave with lineNum={0}", lineNum);
            return timeStamp;
        }
    }

    /**
   * Get the timestamp for the given line number. If the line
   * has no timestamp, the next line will be checked until a
   * timestamp is found.
   */
    public DateTime GetTimestampForLineForward (ref int lineNum, bool roundToSeconds)
    {
        lock (_currentColumnizerLock)
        {
            if (!CurrentColumnizer.IsTimeshiftImplemented())
            {
                return DateTime.MinValue;
            }

            DateTime timeStamp = DateTime.MinValue;
            var lookFwd = false;
            if (lineNum >= 0 && lineNum < dataGridView.RowCount)
            {
                while (timeStamp.CompareTo(DateTime.MinValue) == 0 && lineNum < dataGridView.RowCount)
                {
                    lookFwd = true;
                    ILogLine logLine = _logFileReader.GetLogLine(lineNum);
                    if (logLine == null)
                    {
                        timeStamp = DateTime.MinValue;
                        break;
                    }
                    timeStamp = CurrentColumnizer.GetTimestamp(ColumnizerCallbackObject, logLine);
                    if (roundToSeconds)
                    {
                        timeStamp = timeStamp.Subtract(TimeSpan.FromMilliseconds(timeStamp.Millisecond));
                    }
                    lineNum++;
                }
            }
            if (lookFwd)
            {
                lineNum--;
            }
            return timeStamp;
        }
    }

    public void AppFocusLost ()
    {
        InvalidateCurrentRow(dataGridView);
    }

    public void AppFocusGained ()
    {
        InvalidateCurrentRow(dataGridView);
    }

    public ILogLine GetCurrentLine ()
    {
        if (dataGridView.CurrentRow != null && dataGridView.CurrentRow.Index != -1)
        {
            return _logFileReader.GetLogLine(dataGridView.CurrentRow.Index);
        }
        return null;
    }

    public ILogLine GetLine (int lineNum)
    {
        if (lineNum < 0 || _logFileReader == null || lineNum >= _logFileReader.LineCount)
        {
            return null;
        }
        return _logFileReader.GetLogLine(lineNum);
    }

    public int GetCurrentLineNum ()
    {
        if (dataGridView.CurrentRow == null)
        {
            return -1;
        }
        return dataGridView.CurrentRow.Index;
    }

    public int GetRealLineNum ()
    {
        var lineNum = GetCurrentLineNum();
        if (lineNum == -1)
        {
            return -1;
        }
        return _logFileReader.GetRealLineNumForVirtualLineNum(lineNum);
    }

    public ILogFileInfo GetCurrentFileInfo ()
    {
        if (dataGridView.CurrentRow != null && dataGridView.CurrentRow.Index != -1)
        {
            return _logFileReader.GetLogFileInfoForLine(dataGridView.CurrentRow.Index);
        }
        return null;
    }

    /// <summary>
    /// zero-based
    /// </summary>
    /// <param name="lineNum"></param>
    /// <returns></returns>
    public string GetCurrentFileName (int lineNum)
    {
        return _logFileReader.GetLogFileNameForLine(lineNum);
    }

    // =============== end of bookmark stuff ===================================

    public void ShowLineColumn (bool show)
    {
        dataGridView.Columns[1].Visible = show;
        filterGridView.Columns[1].Visible = show;
    }

    // =================================================================
    // Pattern statistics
    // =================================================================

    public void PatternStatistic ()
    {
        InitPatternWindow();
    }

    public void PatternStatisticSelectRange (PatternArgs patternArgs)
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
            patternArgs.StartLine = lineNumList[0];
            patternArgs.EndLine = lineNumList[^1];
        }
        else
        {
            if (dataGridView.CurrentCellAddress.Y != -1)
            {
                patternArgs.StartLine = dataGridView.CurrentCellAddress.Y;
            }
            else
            {
                patternArgs.StartLine = 0;
            }
            patternArgs.EndLine = dataGridView.RowCount - 1;
        }
    }

    public void PatternStatistic (PatternArgs patternArgs)
    {
        var fx = new PatternStatisticFx(TestStatistic);
        fx.BeginInvoke(patternArgs, null, null);
    }

    public void ExportBookmarkList ()
    {
        SaveFileDialog dlg = new()
        {
            Title = "Choose a file to save bookmarks into",
            AddExtension = true,
            DefaultExt = "csv",
            Filter = "CSV file (*.csv)|*.csv|Bookmark file (*.bmk)|*.bmk",
            FilterIndex = 1,
            FileName = Path.GetFileNameWithoutExtension(FileName)
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            try
            {
                BookmarkExporter.ExportBookmarkList(_bookmarkProvider.BookmarkList, FileName,
                    dlg.FileName);
            }
            catch (IOException e)
            {
                _logger.Error(e);
                MessageBox.Show("Error while exporting bookmark list: " + e.Message, "LogExpert");
            }
        }
    }

    public void ImportBookmarkList ()
    {
        OpenFileDialog dlg = new()
        {
            Title = "Choose a file to load bookmarks from",
            AddExtension = true,
            DefaultExt = "csv",
            Filter = "CSV file (*.csv)|*.csv|Bookmark file (*.bmk)|*.bmk",
            FilterIndex = 1,
            FileName = Path.GetFileNameWithoutExtension(FileName)
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            try
            {
                // add to the existing bookmarks
                SortedList<int, Bookmark> newBookmarks = [];
                BookmarkExporter.ImportBookmarkList(FileName, dlg.FileName, newBookmarks);

                // Add (or replace) to existing bookmark list
                var bookmarkAdded = false;
                foreach (Bookmark b in newBookmarks.Values)
                {
                    if (!_bookmarkProvider.BookmarkList.ContainsKey(b.LineNum))
                    {
                        _bookmarkProvider.BookmarkList.Add(b.LineNum, b);
                        bookmarkAdded = true; // refresh the list only once at the end
                    }
                    else
                    {
                        Bookmark existingBookmark = _bookmarkProvider.BookmarkList[b.LineNum];
                        existingBookmark.Text =
                            b.Text; // replace existing bookmark for that line, preserving the overlay
                        OnBookmarkTextChanged(b);
                    }
                }

                // Refresh the lists
                if (bookmarkAdded)
                {
                    OnBookmarkAdded();
                }
                dataGridView.Refresh();
                filterGridView.Refresh();
            }
            catch (IOException e)
            {
                _logger.Error(e);
                MessageBox.Show($"Error while importing bookmark list: {e.Message}", "LogExpert");
            }
        }
    }

    public bool IsAdvancedOptionActive ()
    {
        return rangeCheckBox.Checked ||
               fuzzyKnobControl.Value > 0 ||
               filterKnobBackSpread.Value > 0 ||
               filterKnobForeSpread.Value > 0 ||
               invertFilterCheckBox.Checked ||
               columnRestrictCheckBox.Checked;
    }

    public void HandleChangedFilterList ()
    {
        Invoke(new MethodInvoker(HandleChangedFilterListWorker));
    }

    public void HandleChangedFilterListWorker ()
    {
        var index = filterListBox.SelectedIndex;
        filterListBox.Items.Clear();
        foreach (FilterParams filterParam in ConfigManager.Settings.FilterList)
        {
            filterListBox.Items.Add(filterParam);
        }
        filterListBox.Refresh();
        if (index >= 0 && index < filterListBox.Items.Count)
        {
            filterListBox.SelectedIndex = index;
        }
        filterOnLoadCheckBox.Checked = Preferences.IsFilterOnLoad;
        hideFilterListOnLoadCheckBox.Checked = Preferences.IsAutoHideFilterList;
    }

    public void SetCurrentHighlightGroup (string groupName)
    {
        _guiStateArgs.HighlightGroupName = groupName;
        lock (_currentHighlightGroupLock)
        {
            _currentHighlightGroup = _parentLogTabWin.FindHighlightGroup(groupName);
            if (_currentHighlightGroup == null)
            {
                if (_parentLogTabWin.HighlightGroupList.Count > 0)
                {
                    _currentHighlightGroup = _parentLogTabWin.HighlightGroupList[0];
                }
                else
                {
                    _currentHighlightGroup = new HighlightGroup();
                }
            }
            _guiStateArgs.HighlightGroupName = _currentHighlightGroup.GroupName;
        }
        SendGuiStateUpdate();
        BeginInvoke(new MethodInvoker(RefreshAllGrids));
    }

    public void SwitchMultiFile (bool enabled)
    {
        IsMultiFile = enabled;
        Reload();
    }

    public void AddOtherWindowToTimesync (LogWindow other)
    {
        if (other.IsTimeSynced)
        {
            if (IsTimeSynced)
            {
                other.FreeFromTimeSync();
                AddSlaveToTimesync(other);
            }
            else
            {
                AddToTimeSync(other);
            }
        }
        else
        {
            AddSlaveToTimesync(other);
        }
    }

    public void AddToTimeSync (LogWindow master)
    {
        _logger.Info("Syncing window for {0} to {1}", Util.GetNameFromPath(FileName), Util.GetNameFromPath(master.FileName));
        lock (_timeSyncListLock)
        {
            if (IsTimeSynced && master.TimeSyncList != TimeSyncList)
            // already synced but master has different sync list
            {
                FreeFromTimeSync();
            }
            TimeSyncList = master.TimeSyncList;
            TimeSyncList.AddWindow(this);
            ScrollToTimestamp(TimeSyncList.CurrentTimestamp, false, false);
        }
        OnSyncModeChanged();
    }

    public void FreeFromTimeSync ()
    {
        lock (_timeSyncListLock)
        {
            if (TimeSyncList != null)
            {
                _logger.Info("De-Syncing window for {0}", Util.GetNameFromPath(FileName));
                TimeSyncList.WindowRemoved -= OnTimeSyncListWindowRemoved;
                TimeSyncList.RemoveWindow(this);
                TimeSyncList = null;
            }
        }
        OnSyncModeChanged();
    }

    public void RefreshLogView ()
    {
        RefreshAllGrids();
    }

    #endregion
}
