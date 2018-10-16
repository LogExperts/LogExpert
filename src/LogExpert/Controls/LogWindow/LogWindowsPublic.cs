﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using LogExpert.Dialogs;

// using System.Linq;
namespace LogExpert
{
    public partial class LogWindow
    {
        #region Interface ILogPaintContext

        /**
       * Returns the first HilightEntry that matches the given line
       */
        public HilightEntry FindHilightEntry(ITextValue line, bool noWordMatches)
        {
            // first check the temp entries
            lock (tempHilightEntryListLock)
            {
                foreach (HilightEntry entry in tempHilightEntryList)
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

            lock (currentHighlightGroupLock)
            {
                foreach (HilightEntry entry in currentHighlightGroup.HilightEntryList)
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

        public IList<HilightMatchEntry> FindHilightMatches(ITextValue column)
        {
            IList<HilightMatchEntry> resultList = new List<HilightMatchEntry>();
            if (column != null)
            {
                lock (currentHighlightGroupLock)
                {
                    GetHighlightEntryMatches(column, currentHighlightGroup.HilightEntryList, resultList);
                }

                lock (tempHilightEntryList)
                {
                    GetHighlightEntryMatches(column, tempHilightEntryList, resultList);
                }
            }

            return resultList;
        }

        public IColumn GetCellValue(int rowIndex, int columnIndex)
        {
            if (columnIndex == 1)
            {
                return new Column
                {
                    FullValue = (rowIndex + 1).ToString() // line number
                };
            }

            if (columnIndex == 0)
            {
// marker column
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
                        return cols.ColumnValues[cols.ColumnValues.Length - 1];
                    }

                    return Column.EmptyColumn;
                    
                }
            }
            catch (Exception)
            {
                return Column.EmptyColumn;
            }

            return Column.EmptyColumn;
        }

        #endregion

        #region Interface ILogView

        public void DeleteBookmarks(List<int> lineNumList)
        {
            bool bookmarksPresent = false;
            foreach (int lineNum in lineNumList)
            {
                if (lineNum != -1)
                {
                    if (bookmarkProvider.IsBookmarkAtLine(lineNum) &&
                        bookmarkProvider.GetBookmarkForLine(lineNum).Text.Length > 0)
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

            bookmarkProvider.RemoveBookmarksForLines(lineNumList);
            OnBookmarkRemoved();
        }

        public void RefreshLogView()
        {
            RefreshAllGrids();
        }

        public void SelectAndEnsureVisible(int line, bool triggerSyncCall)
        {
            try
            {
                SelectLine(line, triggerSyncCall, false);

                // if (!this.dataGridView.CurrentRow.Displayed)
                if (line < dataGridView.FirstDisplayedScrollingRowIndex ||
                    line > dataGridView.FirstDisplayedScrollingRowIndex +
                    dataGridView.DisplayedRowCount(false))
                {
                    dataGridView.FirstDisplayedScrollingRowIndex = line;
                    for (int i = 0;
                        i < 8 && dataGridView.FirstDisplayedScrollingRowIndex > 0 &&
                        line < dataGridView.FirstDisplayedScrollingRowIndex +
                        dataGridView.DisplayedRowCount(false);
                        ++i)
                    {
                        dataGridView.FirstDisplayedScrollingRowIndex =
                            dataGridView.FirstDisplayedScrollingRowIndex - 1;
                    }

                    if (line >= dataGridView.FirstDisplayedScrollingRowIndex +
                        dataGridView.DisplayedRowCount(false))
                    {
                        dataGridView.FirstDisplayedScrollingRowIndex =
                            dataGridView.FirstDisplayedScrollingRowIndex + 1;
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

        public void SelectLogLine(int line)
        {
            Invoke(new SelectLineFx((line1, triggerSyncCall) => SelectLine(line1, triggerSyncCall, true)), line, true);
        }

        #endregion

        #region Public Methods

        public void AddBookmarkOverlays()
        {
            const int OVERSCAN = 20;
            int firstLine = dataGridView.FirstDisplayedScrollingRowIndex;
            if (firstLine < 0)
            {
                return;
            }

            firstLine -= OVERSCAN;
            if (firstLine < 0)
            {
                firstLine = 0;
            }

            int oversizeCount = OVERSCAN;

            for (int i = firstLine; i < dataGridView.RowCount; ++i)
            {
                if (!dataGridView.Rows[i].Displayed && i > dataGridView.FirstDisplayedScrollingRowIndex)
                {
                    if (oversizeCount-- < 0)
                    {
                        break;
                    }
                }

                if (bookmarkProvider.IsBookmarkAtLine(i))
                {
                    Bookmark bookmark = bookmarkProvider.GetBookmarkForLine(i);
                    if (bookmark.Text.Length > 0)
                    {
                        // BookmarkOverlay overlay = new BookmarkOverlay();
                        BookmarkOverlay overlay = bookmark.Overlay;
                        overlay.Bookmark = bookmark;

                        Rectangle r;
                        if (dataGridView.Rows[i].Displayed)
                        {
                            r = dataGridView.GetCellDisplayRectangle(0, i, false);
                        }
                        else
                        {
                            r = dataGridView.GetCellDisplayRectangle(0,
                                dataGridView.FirstDisplayedScrollingRowIndex, false);

// int count = i - this.dataGridView.FirstDisplayedScrollingRowIndex;
                            int heightSum = 0;
                            if (dataGridView.FirstDisplayedScrollingRowIndex < i)
                            {
                                for (int rn = dataGridView.FirstDisplayedScrollingRowIndex + 1; rn < i; ++rn)
                                {
                                    // Rectangle rr = this.dataGridView.GetCellDisplayRectangle(0, rn, false);
                                    // heightSum += rr.Height;
                                    heightSum += GetRowHeight(rn);
                                }

                                r.Offset(0, r.Height + heightSum);
                            }
                            else
                            {
                                for (int rn = dataGridView.FirstDisplayedScrollingRowIndex + 1; rn > i; --rn)
                                {
                                    // Rectangle rr = this.dataGridView.GetCellDisplayRectangle(0, rn, false);
                                    // heightSum += rr.Height;
                                    heightSum += GetRowHeight(rn);
                                }

                                r.Offset(0, -(r.Height + heightSum));
                            }

                            // r.Offset(0, this.dataGridView.DisplayRectangle.Height);
                        }

                        if (_logger.IsDebugEnabled)
                        {
                            _logger.Debug("AddBookmarkOverlay() r.Location={0}, width={1}, scroll_offset={2}", r.Location.X, r.Width, dataGridView.HorizontalScrollingOffset);
                        }

                        overlay.Position = r.Location - new Size(dataGridView.HorizontalScrollingOffset, 0);
                        overlay.Position = overlay.Position + new Size(10, r.Height / 2);
                        dataGridView.AddOverlay(overlay);
                    }
                }
            }
        }

        public void AddOtherWindowToTimesync(LogWindow other)
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

        public void AddToTimeSync(LogWindow master)
        {
            _logger.Info("Syncing window for {0} to {1}", Util.GetNameFromPath(FileName), Util.GetNameFromPath(master.FileName));
            lock (timeSyncListLock)
            {
                if (IsTimeSynced && master.TimeSyncList != TimeSyncList)
                {
// already synced but master has different sync list
                    FreeFromTimeSync();
                }

                TimeSyncList = master.TimeSyncList;
                TimeSyncList.AddWindow(this);
                ScrollToTimestamp(TimeSyncList.CurrentTimestamp, false, false);
            }

            OnSyncModeChanged();
        }

        public void AppFocusGained()
        {
            InvalidateCurrentRow(dataGridView);
        }

        public void AppFocusLost()
        {
            InvalidateCurrentRow(dataGridView);
        }

        public void CellPainting(DataGridView gridView, int rowIndex, DataGridViewCellPaintingEventArgs e)
        {
            if (rowIndex < 0 || e.ColumnIndex < 0)
            {
                e.Handled = false;
                return;
            }

            ILogLine line = logFileReader.GetLogLineWithWait(rowIndex);
            if (line != null)
            {
                HilightEntry entry = FindFirstNoWordMatchHilightEntry(line);
                e.Graphics.SetClip(e.CellBounds);
                if ((e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected)
                {
                    Color backColor = e.CellStyle.SelectionBackColor;
                    Brush brush;
                    if (gridView.Focused)
                    {
                        brush = new SolidBrush(e.CellStyle.SelectionBackColor);
                    }
                    else
                    {
                        Color color = Color.FromArgb(255, 170, 170, 170);
                        brush = new SolidBrush(color);
                    }

                    e.Graphics.FillRectangle(brush, e.CellBounds);
                    brush.Dispose();
                }
                else
                {
                    Color bgColor = Color.White;
                    if (!DebugOptions.disableWordHighlight)
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

                if (DebugOptions.disableWordHighlight)
                {
                    e.PaintContent(e.CellBounds);
                }
                else
                {
                    PaintCell(e, gridView, false, entry);
                }

                if (e.ColumnIndex == 0)
                {
                    if (bookmarkProvider.IsBookmarkAtLine(rowIndex))
                    {
                        Rectangle r; // = new Rectangle(e.CellBounds.Left + 2, e.CellBounds.Top + 2, 6, 6);
                        r = e.CellBounds;
                        r.Inflate(-2, -2);
                        Brush brush = new SolidBrush(BookmarkColor);
                        e.Graphics.FillRectangle(brush, r);
                        brush.Dispose();
                        Bookmark bookmark = bookmarkProvider.GetBookmarkForLine(rowIndex);
                        if (bookmark.Text.Length > 0)
                        {
                            StringFormat format = new StringFormat();
                            format.LineAlignment = StringAlignment.Center;
                            format.Alignment = StringAlignment.Center;
                            Brush brush2 = new SolidBrush(Color.FromArgb(255, 190, 100, 0));
                            Font font = new Font("Courier New", Preferences.fontSize, FontStyle.Bold);
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

        /// <summary>
        ///     Change the file encoding. May force a reload if byte count ot preamble lenght differs from previous used encoding.
        /// </summary>
        /// <param name="encoding"></param>
        public void ChangeEncoding(Encoding encoding)
        {
            logFileReader.ChangeEncoding(encoding);
            EncodingOptions.Encoding = encoding;
            if (guiStateArgs.CurrentEncoding.IsSingleByte != encoding.IsSingleByte ||
                guiStateArgs.CurrentEncoding.GetPreamble().Length != encoding.GetPreamble().Length)
            {
                Reload();
            }
            else
            {
                dataGridView.Refresh();
                SendGuiStateUpdate();
            }

            guiStateArgs.CurrentEncoding = logFileReader.CurrentEncoding;
        }

        public void Close(bool dontAsk)
        {
            Preferences.askForClose = !dontAsk;
            Close();
        }

        public void CloseLogWindow()
        {
            StopTimespreadThread();
            StopTimestampSyncThread();
            StopLogEventWorkerThread();
            statusLineTrigger.Stop();
            selectionChangedTrigger.Stop();

// StopFilterUpdateWorkerThread();
            shouldCancel = true;
            if (logFileReader != null)
            {
                UnRegisterLogFileReaderEvents();
                logFileReader.StopMonitoringAsync();

// this.logFileReader.DeleteAllContent();
            }

            if (isLoading)
            {
                waitingForClose = true;
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

            if (FilterPipe != null)
            {
                FilterPipe.CloseAndDisconnect();
            }

            DisconnectFilterPipes();
        }

        public void ColumnizerConfigChanged()
        {
            SetColumnizerInternal(CurrentColumnizer);
        }

        public void CopyMarkedLinesToTab()
        {
            if (dataGridView.SelectionMode == DataGridViewSelectionMode.FullRowSelect)
            {
                List<int> lineNumList = new List<int>();
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
                FilterPipe pipe = new FilterPipe(new FilterParams(), this);
                pipe.IsStopped = true;
                WritePipeToTab(pipe, lineNumList, Text + "->C", null);
            }
            else
            {
                string fileName = Path.GetTempFileName();
                FileStream fStream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                StreamWriter writer = new StreamWriter(fStream, Encoding.Unicode);

                DataObject data = dataGridView.GetClipboardContent();
                string text = data.GetText(TextDataFormat.Text);
                writer.Write(text);

                writer.Close();
                string title = Util.GetNameFromPath(FileName) + "->Clip";
                parentLogTabWin.AddTempFileTab(fileName, title);
            }
        }

        public void dataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            DataGridView gridView = (DataGridView)sender;
            CellPainting(gridView, e.RowIndex, e);
        }

        public void ExportBookmarkList()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Choose a file to save bookmarks into";
            dlg.AddExtension = true;
            dlg.DefaultExt = "csv";
            dlg.Filter = "CSV file (*.csv)|*.csv|Bookmark file (*.bmk)|*.bmk";
            dlg.FilterIndex = 1;
            dlg.FileName = Path.GetFileNameWithoutExtension(FileName);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    BookmarkExporter.ExportBookmarkList(bookmarkProvider.BookmarkList, FileName,
                        dlg.FileName);
                }
                catch (IOException e)
                {
                    _logger.Error(e);
                    MessageBox.Show("Error while exporting bookmark list: " + e.Message, "LogExpert");
                }
            }
        }

        public int FindTimestampLine(int lineNum, DateTime timestamp, bool roundToSeconds)
        {
            int foundLine =
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

        public int FindTimestampLine_Internal(int lineNum, int rangeStart, int rangeEnd, DateTime timestamp,
                                              bool roundToSeconds)
        {
            _logger.Debug("FindTimestampLine_Internal(): timestamp={0}, lineNum={1}, rangeStart={2}, rangeEnd={3}", timestamp, lineNum, rangeStart, rangeEnd);
            int refLine = lineNum;
            DateTime currentTimestamp = GetTimestampForLine(ref refLine, roundToSeconds);
            if (currentTimestamp.CompareTo(timestamp) == 0)
            {
                return lineNum;
            }

            if (timestamp < currentTimestamp)
            {
                // rangeStart = rangeStart;
                rangeEnd = lineNum;
            }
            else
            {
                rangeStart = lineNum;

// rangeEnd = rangeEnd;
            }

            if (rangeEnd - rangeStart <= 0)
            {
                return -lineNum;
            }

            lineNum = (rangeEnd - rangeStart) / 2 + rangeStart;

// prevent endless loop
            if (rangeEnd - rangeStart < 2)
            {
                currentTimestamp = GetTimestampForLine(ref rangeStart, roundToSeconds);
                if (currentTimestamp.CompareTo(timestamp) == 0)
                {
                    return rangeStart;
                }

                currentTimestamp = GetTimestampForLine(ref rangeEnd, roundToSeconds);
                if (currentTimestamp.CompareTo(timestamp) == 0)
                {
                    return rangeEnd;
                }

                return -lineNum;
            }

            return FindTimestampLine_Internal(lineNum, rangeStart, rangeEnd, timestamp, roundToSeconds);
        }

        public void FollowTailChanged(bool isChecked, bool byTrigger)
        {
            guiStateArgs.FollowTail = isChecked;

            if (guiStateArgs.FollowTail && logFileReader != null)
            {
                if (dataGridView.RowCount >= logFileReader.LineCount && logFileReader.LineCount > 0)
                {
                    dataGridView.FirstDisplayedScrollingRowIndex = logFileReader.LineCount - 1;
                }
            }

            BeginInvoke(new MethodInvoker(dataGridView.Refresh));

// this.dataGridView.Refresh();
            parentLogTabWin.FollowTailChanged(this, isChecked, byTrigger);
            SendGuiStateUpdate();
        }

        public void ForceColumnizer(ILogLineColumnizer columnizer)
        {
            forcedColumnizer = Util.CloneColumnizer(columnizer);
            SetColumnizer(forcedColumnizer);
        }

        public void ForceColumnizerForLoading(ILogLineColumnizer columnizer)
        {
            forcedColumnizerForLoading = Util.CloneColumnizer(columnizer);
        }

        public void FreeFromTimeSync()
        {
            lock (timeSyncListLock)
            {
                if (TimeSyncList != null)
                {
                    _logger.Info("De-Syncing window for {0}", Util.GetNameFromPath(FileName));
                    TimeSyncList.WindowRemoved -= timeSyncList_WindowRemoved;
                    TimeSyncList.RemoveWindow(this);
                    TimeSyncList = null;
                }
            }

            OnSyncModeChanged();
        }

        public ILogFileInfo GetCurrentFileInfo()
        {
            if (dataGridView.CurrentRow != null && dataGridView.CurrentRow.Index != -1)
            {
                return logFileReader.GetLogFileInfoForLine(dataGridView.CurrentRow.Index);
            }

            return null;
        }

        /// <summary>
        ///     zero-based
        /// </summary>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        public string GetCurrentFileName(int lineNum)
        {
            return logFileReader.GetLogFileNameForLine(lineNum);
        }

        public ILogLine GetCurrentLine()
        {
            if (dataGridView.CurrentRow != null && dataGridView.CurrentRow.Index != -1)
            {
                return logFileReader.GetLogLine(dataGridView.CurrentRow.Index);
            }

            return null;
        }

        public int GetCurrentLineNum()
        {
            if (dataGridView.CurrentRow == null)
            {
                return -1;
            }

            return dataGridView.CurrentRow.Index;
        }

        public ILogLine GetLine(int lineNum)
        {
            if (lineNum < 0 || lineNum >= logFileReader.LineCount)
            {
                return null;
            }

            return logFileReader.GetLogLine(lineNum);
        }

        public PersistenceData GetPersistenceData()
        {
            PersistenceData persistenceData = new PersistenceData();
            persistenceData.bookmarkList = bookmarkProvider.BookmarkList;
            persistenceData.rowHeightList = rowHeightList;
            persistenceData.multiFile = IsMultiFile;
            persistenceData.multiFilePattern = multifileOptions.FormatPattern;
            persistenceData.multiFileMaxDays = multifileOptions.MaxDayTry;
            persistenceData.currentLine = dataGridView.CurrentCellAddress.Y;
            persistenceData.firstDisplayedLine = dataGridView.FirstDisplayedScrollingRowIndex;
            persistenceData.filterVisible = !splitContainer1.Panel2Collapsed;
            persistenceData.filterAdvanced = !advancedFilterSplitContainer.Panel1Collapsed;
            persistenceData.filterPosition = splitContainer1.SplitterDistance;
            persistenceData.followTail = guiStateArgs.FollowTail;
            persistenceData.fileName = FileName;
            persistenceData.tabName = Text;
            persistenceData.sessionFileName = SessionFileName;
            persistenceData.columnizerName = CurrentColumnizer.GetName();
            persistenceData.lineCount = logFileReader.LineCount;
            filterParams.isFilterTail =
                filterTailCheckBox.Checked; // this option doesnt need a press on 'search'
            if (Preferences.saveFilters)
            {
                List<FilterParams> filterList = new List<FilterParams>();
                filterList.Add(filterParams);
                persistenceData.filterParamsList = filterList;

                foreach (FilterPipe filterPipe in filterPipeList)
                {
                    FilterTabData data = new FilterTabData();
                    data.persistenceData = filterPipe.OwnLogWindow.GetPersistenceData();
                    data.filterParams = filterPipe.FilterParams;
                    persistenceData.filterTabDataList.Add(data);
                }
            }

            if (currentHighlightGroup != null)
            {
                persistenceData.highlightGroupName = currentHighlightGroup.GroupName;
            }

            if (fileNames != null && IsMultiFile)
            {
                persistenceData.multiFileNames.AddRange(fileNames);
            }

            // persistenceData.showBookmarkCommentColumn = this.bookmarkWindow.ShowBookmarkCommentColumn;
            persistenceData.filterSaveListVisible = !highlightSplitContainer.Panel2Collapsed;
            persistenceData.encoding = logFileReader.CurrentEncoding;
            return persistenceData;
        }

        public int GetRealLineNum()
        {
            int lineNum = GetCurrentLineNum();
            if (lineNum == -1)
            {
                return -1;
            }

            return logFileReader.GetRealLineNumForVirtualLineNum(lineNum);
        }

        /**
       * Get the timestamp for the given line number. If the line
       * has no timestamp, the previous line will be checked until a
       * timestamp is found.
       */
        public DateTime GetTimestampForLine(ref int lineNum, bool roundToSeconds)
        {
            lock (currentColumnizerLock)
            {
                if (!CurrentColumnizer.IsTimeshiftImplemented())
                {
                    return DateTime.MinValue;
                }

                _logger.Debug("GetTimestampForLine({0}) enter", lineNum);
                DateTime timeStamp = DateTime.MinValue;
                bool lookBack = false;
                if (lineNum >= 0 && lineNum < dataGridView.RowCount)
                {
                    while (timeStamp.CompareTo(DateTime.MinValue) == 0 && lineNum >= 0)
                    {
                        if (isTimestampDisplaySyncing && shouldTimestampDisplaySyncingCancel)
                        {
                            return DateTime.MinValue;
                        }

                        lookBack = true;
                        ILogLine logLine = logFileReader.GetLogLine(lineNum);
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
        public DateTime GetTimestampForLineForward(ref int lineNum, bool roundToSeconds)
        {
            lock (currentColumnizerLock)
            {
                if (!CurrentColumnizer.IsTimeshiftImplemented())
                {
                    return DateTime.MinValue;
                }

                DateTime timeStamp = DateTime.MinValue;
                bool lookFwd = false;
                if (lineNum >= 0 && lineNum < dataGridView.RowCount)
                {
                    while (timeStamp.CompareTo(DateTime.MinValue) == 0 && lineNum < dataGridView.RowCount)
                    {
                        lookFwd = true;
                        ILogLine logLine = logFileReader.GetLogLine(lineNum);
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

        public void GotoLine(int line)
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

        public void HandleChangedFilterList()
        {
            Invoke(new MethodInvoker(HandleChangedFilterListWorker));
        }

        public void HandleChangedFilterListWorker()
        {
            int index = filterListBox.SelectedIndex;
            filterListBox.Items.Clear();
            foreach (FilterParams filterParam in ConfigManager.Settings.filterList)
            {
                filterListBox.Items.Add(filterParam);
            }

            filterListBox.Refresh();
            if (index >= 0 && index < filterListBox.Items.Count)
            {
                filterListBox.SelectedIndex = index;
            }

            filterOnLoadCheckBox.Checked = Preferences.isFilterOnLoad;
            hideFilterListOnLoadCheckBox.Checked = Preferences.isAutoHideFilterList;
        }

        public void ImportBookmarkList()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Choose a file to load bookmarks from";
            dlg.AddExtension = true;
            dlg.DefaultExt = "csv";
            dlg.DefaultExt = "csv";
            dlg.Filter = "CSV file (*.csv)|*.csv|Bookmark file (*.bmk)|*.bmk";
            dlg.FilterIndex = 1;
            dlg.FileName = Path.GetFileNameWithoutExtension(FileName);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // add to the existing bookmarks
                    SortedList<int, Bookmark> newBookmarks = new SortedList<int, Bookmark>();
                    BookmarkExporter.ImportBookmarkList(FileName, dlg.FileName, newBookmarks);

                    // Add (or replace) to existing bookmark list
                    bool bookmarkAdded = false;
                    foreach (Bookmark b in newBookmarks.Values)
                    {
                        if (!bookmarkProvider.BookmarkList.ContainsKey(b.LineNum))
                        {
                            bookmarkProvider.BookmarkList.Add(b.LineNum, b);
                            bookmarkAdded = true; // refresh the list only once at the end
                        }
                        else
                        {
                            Bookmark existingBookmark = bookmarkProvider.BookmarkList[b.LineNum];
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
                    MessageBox.Show("Error while importing bookmark list: " + e.Message, "LogExpert");
                }
            }
        }

        public bool IsAdvancedOptionActive()
        {
            return rangeCheckBox.Checked ||
                   fuzzyKnobControl.Value > 0 ||
                   filterKnobControl1.Value > 0 ||
                   filterKnobControl2.Value > 0 ||
                   invertFilterCheckBox.Checked ||
                   columnRestrictCheckBox.Checked;
        }

        public void JumpNextBookmark()
        {
            if (bookmarkProvider.Bookmarks.Count > 0)
            {
                if (filterGridView.Focused)
                {
                    int index = FindNextBookmarkIndex(filterResultList[filterGridView.CurrentCellAddress.Y]);
                    int startIndex = index;
                    bool wrapped = false;
                    while (true)
                    {
                        int lineNum = bookmarkProvider.Bookmarks[index].LineNum;
                        if (filterResultList.Contains(lineNum))
                        {
                            int filterLine = filterResultList.IndexOf(lineNum);
                            filterGridView.Rows[filterLine].Selected = true;
                            filterGridView.CurrentCell = filterGridView.Rows[filterLine].Cells[0];
                            break;
                        }

                        index++;
                        if (index > bookmarkProvider.Bookmarks.Count - 1)
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
                    int index = FindNextBookmarkIndex(dataGridView.CurrentCellAddress.Y);
                    if (index > bookmarkProvider.Bookmarks.Count - 1)
                    {
                        index = 0;
                    }

                    int lineNum = bookmarkProvider.Bookmarks[index].LineNum;
                    SelectLine(lineNum, true, true);
                }
            }
        }

        public void JumpPrevBookmark()
        {
            if (bookmarkProvider.Bookmarks.Count > 0)
            {
                if (filterGridView.Focused)
                {
                    // int index = this.bookmarkList.BinarySearch(this.filterResultList[this.filterGridView.CurrentCellAddress.Y]);
                    // if (index < 0)
                    // index = ~index;
                    // index--;
                    int index = FindPrevBookmarkIndex(filterResultList[filterGridView.CurrentCellAddress.Y]);
                    if (index < 0)
                    {
                        index = bookmarkProvider.Bookmarks.Count - 1;
                    }

                    int startIndex = index;
                    bool wrapped = false;
                    while (true)
                    {
                        int lineNum = bookmarkProvider.Bookmarks[index].LineNum;
                        if (filterResultList.Contains(lineNum))
                        {
                            int filterLine = filterResultList.IndexOf(lineNum);
                            filterGridView.Rows[filterLine].Selected = true;
                            filterGridView.CurrentCell = filterGridView.Rows[filterLine].Cells[0];
                            break;
                        }

                        index--;
                        if (index < 0)
                        {
                            index = bookmarkProvider.Bookmarks.Count - 1;
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
                    int index = FindPrevBookmarkIndex(dataGridView.CurrentCellAddress.Y);
                    if (index < 0)
                    {
                        index = bookmarkProvider.Bookmarks.Count - 1;
                    }

                    int lineNum = bookmarkProvider.Bookmarks[index].LineNum;
                    SelectLine(lineNum, false, true);
                }
            }
        }

        public void LoadFile(string fileName, EncodingOptions encodingOptions)
        {
            EnterLoadFileStatus();

            if (fileName != null)
            {
                FileName = fileName;
                EncodingOptions = encodingOptions;

                if (logFileReader != null)
                {
                    logFileReader.StopMonitoringAsync();
                    UnRegisterLogFileReaderEvents();
                }

                if (!LoadPersistenceOptions())
                {
                    if (!IsTempFile)
                    {
                        ILogLineColumnizer columnizer = FindColumnizer();
                        if (columnizer != null)
                        {
                            if (reloadMemento == null)
                            {
                                columnizer = Util.CloneColumnizer(columnizer);
                            }
                        }

                        PreSelectColumnizer(columnizer);
                    }

                    SetDefaultHighlightGroup();
                }

                // this may be set after loading persistence data
                if (fileNames != null && IsMultiFile)
                {
                    LoadFilesAsMulti(fileNames, EncodingOptions);
                    return;
                }

                columnCache = new ColumnCache();
                try
                {
                    logFileReader = new LogfileReader(fileName, EncodingOptions, IsMultiFile,
                        Preferences.bufferCount, Preferences.linesPerBuffer,
                        multifileOptions);
                    logFileReader.UseNewReader = !Preferences.useLegacyReader;
                }
                catch (LogFileException lfe)
                {
                    _logger.Error(lfe);
                    MessageBox.Show("Cannot load file\n" + lfe.Message, "LogExpert");
                    BeginInvoke(new FunctionWith1BoolParam(Close), true);
                    isLoadError = true;
                    return;
                }

                ILogLineXmlColumnizer xmlColumnizer = CurrentColumnizer as ILogLineXmlColumnizer;
                if (xmlColumnizer != null)
                {
                    logFileReader.IsXmlMode = true;
                    logFileReader.XmlLogConfig =
                        xmlColumnizer.GetXmlLogConfiguration();
                }

                if (forcedColumnizerForLoading != null)
                {
                    CurrentColumnizer = forcedColumnizerForLoading;
                }

                IPreProcessColumnizer processColumnizer = CurrentColumnizer as IPreProcessColumnizer;
                if (processColumnizer != null)
                {
                    logFileReader.PreProcessColumnizer = processColumnizer;
                }
                else
                {
                    logFileReader.PreProcessColumnizer = null;
                }

                RegisterLogFileReaderEvents();
                _logger.Info("Loading logfile: {0}", fileName);
                logFileReader.startMonitoring();
            }
        }

        public void LoadFilesAsMulti(string[] fileNames, EncodingOptions encodingOptions)
        {
            _logger.Info("Loading given files as MultiFile:");

            EnterLoadFileStatus();

            foreach (string name in fileNames)
            {
                _logger.Info("File: {0}", name);
            }

            if (logFileReader != null)
            {
                logFileReader.stopMonitoring();
                UnRegisterLogFileReaderEvents();
            }

            EncodingOptions = encodingOptions;
            columnCache = new ColumnCache();
            logFileReader = new LogfileReader(fileNames, EncodingOptions, Preferences.bufferCount,
                Preferences.linesPerBuffer, multifileOptions);
            logFileReader.UseNewReader = !Preferences.useLegacyReader;
            RegisterLogFileReaderEvents();
            logFileReader.startMonitoring();
            FileName = fileNames[fileNames.Length - 1];
            this.fileNames = fileNames;
            IsMultiFile = true;

// if (this.isTempFile)
            // this.Text = this.tempTitleName;
            // else
            // this.Text = Util.GetNameFromPath(this.FileName);
        }

        public void LogWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (isErrorShowing)
            {
                RemoveStatusLineError();
            }

            if (e.KeyCode == Keys.F3)
            {
                if (parentLogTabWin.SearchParams == null
                    || parentLogTabWin.SearchParams.searchText == null
                    || parentLogTabWin.SearchParams.searchText.Length == 0)
                {
                    return;
                }

                parentLogTabWin.SearchParams.isFindNext = true;
                parentLogTabWin.SearchParams.isShiftF3Pressed = (e.Modifiers & Keys.Shift) == Keys.Shift;
                StartSearch();
            }

            if (e.KeyCode == Keys.Escape)
            {
                if (isSearching)
                {
                    shouldCancel = true;
                }

                FireCancelHandlers();
                RemoveAllSearchHighlightEntries();
            }

            if (e.KeyCode == Keys.E && (e.Modifiers & Keys.Control) == Keys.Control)
            {
                StartEditMode();
            }

            if (e.KeyCode == Keys.Down && e.Modifiers == Keys.Alt)
            {
                int newLine = logFileReader.GetNextMultiFileLine(dataGridView.CurrentCellAddress.Y);
                if (newLine != -1)
                {
                    SelectLine(newLine, false, true);
                }

                e.Handled = true;
            }

            if (e.KeyCode == Keys.Up && e.Modifiers == Keys.Alt)
            {
                int newLine = logFileReader.GetPrevMultiFileLine(dataGridView.CurrentCellAddress.Y);
                if (newLine != -1)
                {
                    SelectLine(newLine - 1, false, true);
                }

                e.Handled = true;
            }

            if (e.KeyCode == Keys.Enter && dataGridView.Focused)
            {
                ChangeRowHeight(e.Shift);
                e.Handled = true;
            }

            if (e.KeyCode == Keys.Back && dataGridView.Focused)
            {
                ChangeRowHeight(true);
                e.Handled = true;
            }

            if (e.KeyCode == Keys.PageUp && e.Modifiers == Keys.Alt)
            {
                SelectPrevHighlightLine();
                e.Handled = true;
            }

            if (e.KeyCode == Keys.PageDown && e.Modifiers == Keys.Alt)
            {
                SelectNextHighlightLine();
                e.Handled = true;
            }

            if (e.KeyCode == Keys.T && (e.Modifiers & Keys.Control) == Keys.Control &&
                (e.Modifiers & Keys.Shift) == Keys.Shift)
            {
                FilterToTab();
            }
        }

        public void LogWindowActivated()
        {
            if (guiStateArgs.FollowTail && !isDeadFile)
            {
                OnTailFollowed(new EventArgs());
            }

            if (Preferences.timestampControl)
            {
                SetTimestampLimits();
                SyncTimestampDisplay();
            }

            dataGridView.Focus();

            SendGuiStateUpdate();
            SendStatusLineUpdate();
            SendProgressBarUpdate();
        }

        // =================================================================
        // Pattern statistics
        // =================================================================
        public void PatternStatistic()
        {
            InitPatternWindow();
        }

        public void PatternStatistic(PatternArgs patternArgs)
        {
            PatternStatisticFx fx = TestStatistic;
            fx.BeginInvoke(patternArgs, null, null);
        }

        public void PatternStatisticSelectRange(PatternArgs patternArgs)
        {
            if (dataGridView.SelectionMode == DataGridViewSelectionMode.FullRowSelect)
            {
                List<int> lineNumList = new List<int>();
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                {
                    if (row.Index != -1)
                    {
                        lineNumList.Add(row.Index);
                    }
                }

                lineNumList.Sort();
                patternArgs.startLine = lineNumList[0];
                patternArgs.endLine = lineNumList[lineNumList.Count - 1];
            }
            else
            {
                if (dataGridView.CurrentCellAddress.Y != -1)
                {
                    patternArgs.startLine = dataGridView.CurrentCellAddress.Y;
                }
                else
                {
                    patternArgs.startLine = 0;
                }

                patternArgs.endLine = dataGridView.RowCount - 1;
            }
        }

        public void PreferencesChanged(Preferences newPreferences, bool isLoadTime, SettingsFlags flags)
        {
            if ((flags & SettingsFlags.GuiOrColors) == SettingsFlags.GuiOrColors)
            {
                NormalFont = new Font(new FontFamily(newPreferences.fontName), newPreferences.fontSize);
                BoldFont = new Font(NormalFont, FontStyle.Bold);
                MonospacedFont = new Font("Courier New", Preferences.fontSize, FontStyle.Bold);

                int lineSpacing = NormalFont.FontFamily.GetLineSpacing(FontStyle.Regular);
                float lineSpacingPixel =
                    NormalFont.Size * lineSpacing / NormalFont.FontFamily.GetEmHeight(FontStyle.Regular);

                dataGridView.DefaultCellStyle.Font = NormalFont;
                filterGridView.DefaultCellStyle.Font = NormalFont;
                lineHeight = NormalFont.Height + 4;
                dataGridView.RowTemplate.Height = NormalFont.Height + 4;

                ShowBookmarkBubbles = Preferences.showBubbles;

                ApplyDataGridViewPrefs(dataGridView, newPreferences);
                ApplyDataGridViewPrefs(filterGridView, newPreferences);

                if (Preferences.timestampControl)
                {
                    SetTimestampLimits();
                    SyncTimestampDisplay();
                }

                if (isLoadTime)
                {
                    filterTailCheckBox.Checked = Preferences.filterTail;
                    syncFilterCheckBox.Checked = Preferences.filterSync;

// this.FollowTailChanged(this.Preferences.followTail, false);
                }

                timeSpreadCalc.TimeMode = Preferences.timeSpreadTimeMode;
                timeSpreadingControl1.ForeColor = Preferences.timeSpreadColor;
                timeSpreadingControl1.ReverseAlpha = Preferences.reverseAlpha;
                if (CurrentColumnizer.IsTimeshiftImplemented())
                {
                    timeSpreadingControl1.Invoke(new MethodInvoker(timeSpreadingControl1.Refresh));
                    ShowTimeSpread(Preferences.showTimeSpread);
                }

                ToggleColumnFinder(Preferences.showColumnFinder, false);
            }

            if ((flags & SettingsFlags.FilterList) == SettingsFlags.FilterList)
            {
                HandleChangedFilterList();
            }

            if ((flags & SettingsFlags.FilterHistory) == SettingsFlags.FilterHistory)
            {
                UpdateFilterHistoryFromSettings();
            }
        }

        public void PreselectColumnizer(string columnizerName)
        {
            ILogLineColumnizer columnizer = Util.FindColumnizerByName(columnizerName,
                PluginRegistry.GetInstance().RegisteredColumnizers);
            PreSelectColumnizer(Util.CloneColumnizer(columnizer));
        }

        public void Reload()
        {
            SavePersistenceData(false);

            reloadMemento = new ReloadMemento();
            reloadMemento.currentLine = dataGridView.CurrentCellAddress.Y;
            reloadMemento.firstDisplayedLine = dataGridView.FirstDisplayedScrollingRowIndex;
            forcedColumnizerForLoading = CurrentColumnizer;

            if (fileNames == null || !IsMultiFile)
            {
                LoadFile(FileName, EncodingOptions);
            }
            else
            {
                LoadFilesAsMulti(fileNames, EncodingOptions);
            }

// if (currentLine < this.dataGridView.RowCount && currentLine >= 0)
            // this.dataGridView.CurrentCell = this.dataGridView.Rows[currentLine].Cells[0];
            // if (firstDisplayedLine < this.dataGridView.RowCount && firstDisplayedLine >= 0)
            // this.dataGridView.FirstDisplayedScrollingRowIndex = firstDisplayedLine;

            // if (this.filterTailCheckBox.Checked)
            // {
            // _logger.logInfo("Refreshing filter view because of reload.");
            // FilterSearch();
            // }
        }

        public string SavePersistenceData(bool force)
        {
            if (!force)
            {
                if (!Preferences.saveSessions)
                {
                    return null;
                }
            }

            if (IsTempFile || isLoadError)
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

        public bool ScrollToTimestamp(DateTime timestamp, bool roundToSeconds, bool triggerSyncCall)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ScrollToTimestampFx(ScrollToTimestampWorker), timestamp, roundToSeconds, triggerSyncCall);
                return true;
            }

            return ScrollToTimestampWorker(timestamp, roundToSeconds, triggerSyncCall);
        }

        public bool ScrollToTimestampWorker(DateTime timestamp, bool roundToSeconds, bool triggerSyncCall)
        {
            bool hasScrolled = false;
            if (!CurrentColumnizer.IsTimeshiftImplemented() || dataGridView.RowCount == 0)
            {
                return false;
            }

            // this.Cursor = Cursors.WaitCursor;
            int currentLine = dataGridView.CurrentCellAddress.Y;
            if (currentLine < 0 || currentLine >= dataGridView.RowCount)
            {
                currentLine = 0;
            }

            int foundLine = FindTimestampLine(currentLine, timestamp, roundToSeconds);
            if (foundLine >= 0)
            {
                SelectAndEnsureVisible(foundLine, triggerSyncCall);
                hasScrolled = true;
            }

            // this.Cursor = Cursors.Default;
            return hasScrolled;
        }

        public void SetBookmarkFromTrigger(int lineNum, string comment)
        {
            lock (bookmarkLock)
            {
                ILogLine line = logFileReader.GetLogLine(lineNum);
                if (line == null)
                {
                    return;
                }

                ParamParser paramParser = new ParamParser(comment);
                try
                {
                    comment = paramParser.ReplaceParams(line, lineNum, FileName);
                }
                catch (ArgumentException)
                {
                    // occurs on invalid regex 
                }

                if (bookmarkProvider.IsBookmarkAtLine(lineNum))
                {
                    bookmarkProvider.RemoveBookmarkForLine(lineNum);
                }

                bookmarkProvider.AddBookmark(new Bookmark(lineNum, comment));
                OnBookmarkAdded();
            }
        }

        public void SetCellSelectionMode(bool isCellMode)
        {
            if (isCellMode)
            {
                dataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            }
            else
            {
                dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            }

            guiStateArgs.CellSelectMode = isCellMode;
        }

        public void SetColumnizer(ILogLineColumnizer columnizer, DataGridView gridView)
        {
            int rowCount = gridView.RowCount;
            int currLine = gridView.CurrentCellAddress.Y;
            int currFirstLine = gridView.FirstDisplayedScrollingRowIndex;

            gridView.Columns.Clear();

            DataGridViewTextBoxColumn markerColumn = new DataGridViewTextBoxColumn();
            markerColumn.HeaderText = string.Empty;
            markerColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
            markerColumn.Resizable = DataGridViewTriState.False;
            markerColumn.DividerWidth = 1;
            markerColumn.ReadOnly = true;
            markerColumn.HeaderCell.ContextMenuStrip = columnContextMenuStrip;
            gridView.Columns.Add(markerColumn);

            DataGridViewTextBoxColumn lineNumberColumn = new DataGridViewTextBoxColumn();
            lineNumberColumn.HeaderText = "Line";
            lineNumberColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
            lineNumberColumn.Resizable = DataGridViewTriState.NotSet;
            lineNumberColumn.DividerWidth = 1;
            lineNumberColumn.ReadOnly = true;
            lineNumberColumn.HeaderCell.ContextMenuStrip = columnContextMenuStrip;
            gridView.Columns.Add(lineNumberColumn);

            foreach (string colName in columnizer.GetColumnNames())
            {
                DataGridViewColumn titleColumn = new LogTextColumn();
                titleColumn.HeaderText = colName;
                titleColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
                titleColumn.Resizable = DataGridViewTriState.NotSet;
                titleColumn.DividerWidth = 1;
                titleColumn.HeaderCell.ContextMenuStrip = columnContextMenuStrip;
                gridView.Columns.Add(titleColumn);
            }

            columnNamesLabel.Text = CalculateColumnNames(filterParams);

            // gridView.Columns[gridView.Columns.Count - 1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            // gridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            gridView.RowCount = rowCount;
            if (currLine != -1)
            {
                gridView.CurrentCell = gridView.Rows[currLine].Cells[0];
            }

            if (currFirstLine != -1)
            {
                gridView.FirstDisplayedScrollingRowIndex = currFirstLine;
            }

            gridView.Refresh();
            AutoResizeColumns(gridView);
            ApplyFrozenState(gridView);
        }

        public void SetCurrentHighlightGroup(string groupName)
        {
            guiStateArgs.HighlightGroupName = groupName;
            lock (currentHighlightGroupLock)
            {
                currentHighlightGroup = parentLogTabWin.FindHighlightGroup(groupName);
                if (currentHighlightGroup == null)
                {
                    if (parentLogTabWin.HilightGroupList.Count > 0)
                    {
                        currentHighlightGroup = parentLogTabWin.HilightGroupList[0];
                    }
                    else
                    {
                        currentHighlightGroup = new HilightGroup();
                    }
                }

                guiStateArgs.HighlightGroupName = currentHighlightGroup.GroupName;
            }

            SendGuiStateUpdate();
            BeginInvoke(new MethodInvoker(RefreshAllGrids));
        }

        public void SetTimeshiftValue(string value)
        {
            guiStateArgs.TimeshiftText = value;
            if (CurrentColumnizer.IsTimeshiftImplemented())
            {
                try
                {
                    if (guiStateArgs.TimeshiftEnabled)
                    {
                        try
                        {
                            string text = guiStateArgs.TimeshiftText;
                            if (text.StartsWith("+"))
                            {
                                text = text.Substring(1);
                            }

                            TimeSpan timeSpan = TimeSpan.Parse(text);
                            int diff = (int)(timeSpan.Ticks / TimeSpan.TicksPerMillisecond);
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

        // =============== end of bookmark stuff ===================================
        public void ShowLineColumn(bool show)
        {
            dataGridView.Columns[1].Visible = show;
            filterGridView.Columns[1].Visible = show;
        }

        public void StartSearch()
        {
            guiStateArgs.MenuEnabled = false;
            GuiStateUpdate(this, guiStateArgs);
            SearchParams searchParams = parentLogTabWin.SearchParams;
            if ((searchParams.isForward || searchParams.isFindNext) && !searchParams.isShiftF3Pressed)
            {
                searchParams.currentLine = dataGridView.CurrentCellAddress.Y + 1;
            }
            else
            {
                searchParams.currentLine = dataGridView.CurrentCellAddress.Y - 1;
            }

            currentSearchParams = searchParams; // remember for async "not found" messages

            isSearching = true;
            shouldCancel = false;
            StatusLineText("Searching... Press ESC to cancel.");

            progressEventArgs.MinValue = 0;
            progressEventArgs.MaxValue = dataGridView.RowCount;
            progressEventArgs.Value = 0;
            progressEventArgs.Visible = true;
            SendProgressBarUpdate();

            SearchFx searchFx = Search;
            searchFx.BeginInvoke(searchParams, SearchComplete, null);

            RemoveAllSearchHighlightEntries();
            AddSearchHitHighlightEntry(searchParams);
        }

        public void SwitchMultiFile(bool enabled)
        {
            IsMultiFile = enabled;
            Reload();
        }

        public void TimeshiftEnabled(bool isEnabled, string shiftValue)
        {
            guiStateArgs.TimeshiftEnabled = isEnabled;
            SetTimestampLimits();
            SetTimeshiftValue(shiftValue);
        }

        public void ToggleBookmark()
        {
            DataGridView gridView;
            int lineNum;

            if (filterGridView.Focused)
            {
                gridView = filterGridView;
                if (gridView.CurrentCellAddress == null || gridView.CurrentCellAddress.Y == -1)
                {
                    return;
                }

                lineNum = filterResultList[gridView.CurrentCellAddress.Y];
            }
            else
            {
                gridView = dataGridView;
                if (gridView.CurrentCellAddress == null || gridView.CurrentCellAddress.Y == -1)
                {
                    return;
                }

                lineNum = dataGridView.CurrentCellAddress.Y;
            }

            ToggleBookmark(lineNum);
        }

        public void ToggleBookmark(int lineNum)
        {
            if (bookmarkProvider.IsBookmarkAtLine(lineNum))
            {
                Bookmark bookmark = bookmarkProvider.GetBookmarkForLine(lineNum);
                if (bookmark.Text != null && bookmark.Text.Length > 0)
                {
                    if (DialogResult.No ==
                        MessageBox.Show("There's a comment attached to the bookmark. Really remove the bookmark?",
                            "LogExpert", MessageBoxButtons.YesNo))
                    {
                        return;
                    }
                }

                bookmarkProvider.RemoveBookmarkForLine(lineNum);
            }
            else
            {
                bookmarkProvider.AddBookmark(new Bookmark(lineNum));
            }

            dataGridView.Refresh();
            filterGridView.Refresh();
            OnBookmarkAdded();
        }

        public void ToggleFilterPanel()
        {
            splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
            if (!splitContainer1.Panel2Collapsed)
            {
                filterComboBox.Focus();
            }
            else
            {
                dataGridView.Focus();
            }
        }

        public void WaitForLoadingFinished()
        {
            externaLoadingFinishedEvent.WaitOne();
        }

        #endregion
    }
}
