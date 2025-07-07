using System.Runtime.Versioning;

using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;
using LogExpert.Core.Interface;
using LogExpert.UI.Entities;
using LogExpert.UI.Interface;

using NLog;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.Dialogs;

//TODO can be moved to Logexpert.UI if the PaintHelper has been refactored
[SupportedOSPlatform("windows")]
internal partial class BookmarkWindow : DockContent, ISharedToolWindow, IBookmarkView
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly object paintLock = new();

    private IBookmarkData _bookmarkData;
    private ILogPaintContextUI _logPaintContext;
    private ILogView _logView;

    #endregion

    #region cTor

    public BookmarkWindow ()
    {
        InitializeComponent();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        bookmarkDataGridView.CellValueNeeded += OnBoomarkDataGridViewCellValueNeeded;
        bookmarkDataGridView.CellPainting += OnBoomarkDataGridViewCellPainting;
    }

    #endregion

    #region Properties

    public bool LineColumnVisible
    {
        set => bookmarkDataGridView.Columns[2].Visible = value;
    }

    public bool ShowBookmarkCommentColumn
    {
        get => commentColumnCheckBox.Checked;
        set
        {
            commentColumnCheckBox.Checked = value;
            ShowCommentColumn(value);
        }
    }

    #endregion

    #region Public methods

    public void SetColumnizer (ILogLineColumnizer columnizer)
    {
        PaintHelper.SetColumnizer(columnizer, bookmarkDataGridView);

        if (bookmarkDataGridView.ColumnCount > 0)
        {
            bookmarkDataGridView.Columns[0].Width = 20;
        }

        DataGridViewTextBoxColumn commentColumn = new()
        {
            HeaderText = "Bookmark Comment",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Resizable = DataGridViewTriState.NotSet,
            DividerWidth = 1,
            ReadOnly = true,
            Width = 250,
            MinimumWidth = 130
        };

        bookmarkDataGridView.Columns.Insert(1, commentColumn);
        ShowCommentColumn(commentColumnCheckBox.Checked);
        ResizeColumns();
    }

    /// <summary>
    /// Called from LogWindow after reloading and when double clicking a header divider.
    /// </summary>
    public void ResizeColumns ()
    {
        // this.bookmarkDataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        for (var i = 2; i < bookmarkDataGridView.ColumnCount; ++i)
        {
            bookmarkDataGridView.AutoResizeColumn(i, DataGridViewAutoSizeColumnMode.DisplayedCells);
        }
    }

    public void UpdateView ()
    {
        bookmarkDataGridView.RowCount = _bookmarkData?.Bookmarks.Count ?? 0;
        ResizeColumns();
        bookmarkDataGridView.Refresh();
    }

    /// <summary>
    /// Called from LogWindow if the bookmark text was changed via popup window
    /// </summary>
    /// <param name="bookmark"></param>
    public void BookmarkTextChanged (Bookmark bookmark)
    {
        var rowIndex = bookmarkDataGridView.CurrentCellAddress.Y;

        if (rowIndex == -1)
        {
            return;
        }

        if (_bookmarkData.Bookmarks[rowIndex] == bookmark)
        {
            bookmarkTextBox.Text = bookmark.Text;
        }

        bookmarkDataGridView.Refresh();
    }

    public void SelectBookmark (int lineNum)
    {
        if (_bookmarkData.IsBookmarkAtLine(lineNum))
        {
            if (bookmarkDataGridView.Rows.GetRowCount(DataGridViewElementStates.None) < _bookmarkData.Bookmarks.Count)
            {
                // just for the case... There was an exception but I cannot find the cause
                UpdateView();
            }

            var row = _bookmarkData.GetBookmarkIndexForLine(lineNum);
            bookmarkDataGridView.CurrentCell = bookmarkDataGridView.Rows[row].Cells[0];
        }
    }

    public void SetBookmarkData (IBookmarkData bookmarkData)
    {
        _bookmarkData = bookmarkData;
        bookmarkDataGridView.RowCount = bookmarkData?.Bookmarks.Count ?? 0;
        HideIfNeeded();
    }

    public void PreferencesChanged (string fontName, float fontSize, bool setLastColumnWidth, int lastColumnWidth, SettingsFlags flags)
    {
        if ((flags & SettingsFlags.GuiOrColors) == SettingsFlags.GuiOrColors)
        {
            SetFont(fontName, fontSize);
            if (bookmarkDataGridView.Columns.Count > 1 && setLastColumnWidth)
            {
                bookmarkDataGridView.Columns[bookmarkDataGridView.Columns.Count - 1].MinimumWidth = lastColumnWidth;
            }

            PaintHelper.ApplyDataGridViewPrefs(bookmarkDataGridView, setLastColumnWidth, lastColumnWidth);
        }
    }

    public void SetCurrentFile (IFileViewContext ctx)
    {
        if (ctx != null)
        {
            _logger.Debug($"Current file changed to {ctx.LogView.FileName}");
            lock (paintLock)
            {
                _logView = ctx.LogView;
                _logPaintContext = (ILogPaintContextUI)ctx.LogPaintContext;
            }

            SetColumnizer(ctx.LogView.CurrentColumnizer);
        }
        else
        {
            _logView = null;
            _logPaintContext = null;
        }

        UpdateView();
    }

    public void FileChanged ()
    {
        // nothing to do
    }

    #endregion

    #region Overrides

    protected override string GetPersistString ()
    {
        return WindowTypes.BookmarkWindow.ToString();
    }

    protected override void OnPaint (PaintEventArgs e)
    {
        if (!splitContainer1.Visible)
        {
            e.Graphics.FillRectangle(SystemBrushes.ControlLight, ClientRectangle);

            StringFormat sf = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            e.Graphics.DrawString("No bookmarks in current file", SystemFonts.DialogFont, SystemBrushes.WindowText, ClientRectangle, sf);
        }
        else
        {
            base.OnPaint(e);
        }
    }

    #endregion

    #region Private Methods

    private void SetFont (string fontName, float fontSize)
    {
        Font font = new(new FontFamily(fontName), fontSize);
        bookmarkDataGridView.DefaultCellStyle.Font = font;
        bookmarkDataGridView.RowTemplate.Height = font.Height + 4;
        bookmarkDataGridView.Refresh();
    }

    private void CommentPainting (BufferedDataGridView gridView, DataGridViewCellPaintingEventArgs e)
    {
        if (e.State.HasFlag(DataGridViewElementStates.Selected))
        {
            using var brush = PaintHelper.GetBrushForFocusedControl(gridView.Focused, e.CellStyle.SelectionBackColor);
            e.Graphics.FillRectangle(brush, e.CellBounds);
        }
        else
        {
            e.CellStyle.BackColor = Color.White;
            e.PaintBackground(e.CellBounds, false);
        }

        e.PaintContent(e.CellBounds);
    }

    private void DeleteSelectedBookmarks ()
    {
        List<int> lineNumList = [];
        foreach (DataGridViewRow row in bookmarkDataGridView.SelectedRows)
        {
            if (row.Index != -1)
            {
                lineNumList.Add(_bookmarkData.Bookmarks[row.Index].LineNum);
            }
        }

        _logView?.DeleteBookmarks(lineNumList);
    }

    private static void InvalidateCurrentRow (BufferedDataGridView gridView)
    {
        if (gridView.CurrentCellAddress.Y > -1)
        {
            gridView.InvalidateRow(gridView.CurrentCellAddress.Y);
        }
    }

    private void CurrentRowChanged (int rowIndex)
    {
        if (rowIndex == -1)
        {
            // multiple selection or no selection at all
            bookmarkTextBox.Enabled = false;

            // disable the control first so that changes made to it won't propagate to the bookmark item
            bookmarkTextBox.Text = string.Empty;
        }
        else
        {
            bookmarkTextBox.Text = _bookmarkData.Bookmarks[rowIndex].Text;
            bookmarkTextBox.Enabled = true;
        }
    }

    private void ShowCommentColumn (bool show)
    {
        bookmarkDataGridView.Columns[1].Visible = show;
    }

    private void HideIfNeeded ()
    {
        splitContainer1.Visible = bookmarkDataGridView.RowCount > 0;
    }

    #endregion

    #region Events handler

    private void OnBoomarkDataGridViewCellPainting (object sender, DataGridViewCellPaintingEventArgs e)
    {
        if (_bookmarkData == null)
        {
            return;
        }

        lock (paintLock)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || _bookmarkData.Bookmarks.Count <= e.RowIndex)
                {
                    e.Handled = false;
                    return;
                }

                var lineNum = _bookmarkData.Bookmarks[e.RowIndex].LineNum;

                // if (e.ColumnIndex == 1)
                // {
                // CommentPainting(this.bookmarkDataGridView, lineNum, e);
                // }
                //{
                // else
                PaintHelper.CellPainting(_logPaintContext, bookmarkDataGridView.Focused, lineNum, e.ColumnIndex, e);
                //}
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }

    private void OnBoomarkDataGridViewCellValueNeeded (object sender, DataGridViewCellValueEventArgs e)
    {
        if (_bookmarkData == null)
        {
            return;
        }

        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _bookmarkData.Bookmarks.Count <= e.RowIndex)
        {
            e.Value = string.Empty;
            return;
        }

        var bookmarkForLine = _bookmarkData.Bookmarks[e.RowIndex];
        var lineNum = bookmarkForLine.LineNum;
        if (e.ColumnIndex == 1)
        {
            e.Value = bookmarkForLine.Text?.Replace('\n', ' ').Replace('\r', ' ');
        }
        else
        {
            var columnIndex = e.ColumnIndex > 1 ? e.ColumnIndex - 1 : e.ColumnIndex;
            e.Value = _logPaintContext.GetCellValue(lineNum, columnIndex);
        }
    }


    private void OnBoomarkDataGridViewMouseDoubleClick (object sender, MouseEventArgs e)
    {
        // if (this.bookmarkDataGridView.CurrentRow != null)
        // {
        // int lineNum = this.BookmarkList.Values[this.bookmarkDataGridView.CurrentRow.Index].LineNum;
        // this.logWindow.SelectLogLine(lineNum);
        // }
    }

    private void OnBoomarkDataGridViewColumnDividerDoubleClick (object sender,
        DataGridViewColumnDividerDoubleClickEventArgs e)
    {
        e.Handled = true;
        ResizeColumns();
    }

    private void OnBookmarkGridViewKeyDown (object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            if (bookmarkDataGridView.CurrentCellAddress.Y >= 0 &&
                bookmarkDataGridView.CurrentCellAddress.Y < _bookmarkData.Bookmarks.Count)
            {
                var lineNum = _bookmarkData.Bookmarks[bookmarkDataGridView.CurrentCellAddress.Y].LineNum;
                _logView.SelectLogLine(lineNum);
            }

            e.Handled = true;
        }

        if (e.KeyCode == Keys.Delete && e.Modifiers == Keys.None)
        {
            DeleteSelectedBookmarks();
        }

        if (e.KeyCode == Keys.Tab)
        {
            if (bookmarkDataGridView.Focused)
            {
                bookmarkTextBox.Focus();
                e.Handled = true;
            }
        }
    }

    private void OnBookmarkGridViewEnter (object sender, EventArgs e)
    {
        InvalidateCurrentRow(bookmarkDataGridView);
    }

    private void OnBookmarkGridViewLeave (object sender, EventArgs e)
    {
        InvalidateCurrentRow(bookmarkDataGridView);
    }

    private void OnDeleteBookmarksToolStripMenuItemClick (object sender, EventArgs e)
    {
        DeleteSelectedBookmarks();
    }

    private void OnBookmarkTextBoxTextChanged (object sender, EventArgs e)
    {
        if (!bookmarkTextBox.Enabled)
        {
            return; // ignore all changes done while the control is disabled
        }

        var rowIndex = bookmarkDataGridView.CurrentCellAddress.Y;
        if (rowIndex == -1)
        {
            return;
        }

        if (_bookmarkData.Bookmarks.Count <= rowIndex)
        {
            return;
        }

        var bookmark = _bookmarkData.Bookmarks[rowIndex];
        bookmark.Text = bookmarkTextBox.Text;
        _logView?.RefreshLogView();
    }

    private void OnBookmarkDataGridViewSelectionChanged (object sender, EventArgs e)
    {
        if (bookmarkDataGridView.SelectedRows.Count != 1
            || bookmarkDataGridView.SelectedRows[0].Index >= _bookmarkData.Bookmarks.Count)
        {
            CurrentRowChanged(-1);
        }
        else
        {
            CurrentRowChanged(bookmarkDataGridView.SelectedRows[0].Index);
        }
    }

    private void OnBookmarkDataGridViewPreviewKeyDown (object sender, PreviewKeyDownEventArgs e)
    {
        if (e.KeyCode == Keys.Tab)
        {
            e.IsInputKey = true;
        }
    }

    private void OnBookmarkDataGridViewCellToolTipTextNeeded (object sender,
        DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (e.ColumnIndex != 0 || e.RowIndex <= -1 || e.RowIndex >= _bookmarkData.Bookmarks.Count)
        {
            return;
        }

        var bookmark = _bookmarkData.Bookmarks[e.RowIndex];
        if (!string.IsNullOrEmpty(bookmark.Text))
        {
            e.ToolTipText = bookmark.Text;
            return;
        }
    }

    private void OnBookmarkDataGridViewCellDoubleClick (object sender, DataGridViewCellEventArgs e)
    {
        // Toggle bookmark when double-clicking on the first column
        if (e.ColumnIndex == 0 && e.RowIndex >= 0 && bookmarkDataGridView.CurrentRow != null)
        {
            var index = bookmarkDataGridView.CurrentRow.Index;
            var lineNum = _bookmarkData.Bookmarks[bookmarkDataGridView.CurrentRow.Index].LineNum;
            _bookmarkData.ToggleBookmark(lineNum);

            // we don't ask for confirmation if the bookmark has an associated comment...
            var boomarkCount = _bookmarkData.Bookmarks.Count;
            bookmarkDataGridView.RowCount = boomarkCount;

            if (index < boomarkCount)
            {
                bookmarkDataGridView.CurrentCell = bookmarkDataGridView.Rows[index].Cells[0];
            }
            else
            {
                if (boomarkCount > 0)
                {
                    bookmarkDataGridView.CurrentCell =
                        bookmarkDataGridView.Rows[boomarkCount - 1].Cells[0];
                }
            }

            if (boomarkCount > index)
            {
                CurrentRowChanged(index);
            }
            else
            {
                if (boomarkCount > 0)
                {
                    CurrentRowChanged(bookmarkDataGridView.RowCount - 1);
                }
                else
                {
                    CurrentRowChanged(-1);
                }
            }

            return;
        }

        if (bookmarkDataGridView.CurrentRow != null && e.RowIndex >= 0)
        {
            var lineNum = _bookmarkData.Bookmarks[bookmarkDataGridView.CurrentRow.Index].LineNum;
            _logView.SelectAndEnsureVisible(lineNum, true);
        }
    }

    private void OnRemoveCommentsToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (MessageBox.Show("Really remove bookmark comments for selected lines?", "LogExpert", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            foreach (DataGridViewRow row in bookmarkDataGridView.SelectedRows)
            {
                if (row.Index != -1)
                {
                    _bookmarkData.Bookmarks[row.Index].Text = string.Empty;
                }
            }

            bookmarkTextBox.Text = string.Empty;
            bookmarkDataGridView.Refresh();
            _logView.RefreshLogView();
        }
    }

    private void OnCommentColumnCheckBoxCheckedChanged (object sender, EventArgs e)
    {
        ShowCommentColumn(commentColumnCheckBox.Checked);
    }

    private void BookmarkWindow_ClientSizeChanged (object sender, EventArgs e)
    {
        if (Width > 0 && Height > 0)
        {
            if (Width > Height)
            {
                splitContainer1.Orientation = Orientation.Vertical;
                var distance = Width - 200;
                splitContainer1.SplitterDistance = distance > splitContainer1.Panel1MinSize
                    ? distance
                    : splitContainer1.Panel1MinSize;
            }
            else
            {
                splitContainer1.Orientation = Orientation.Horizontal;
                var distance = Height - 200;
                splitContainer1.SplitterDistance = distance > splitContainer1.Panel1MinSize
                    ? distance
                    : splitContainer1.Panel1MinSize;
            }
        }

        if (!splitContainer1.Visible)
        {
            // redraw the "no bookmarks" display
            Invalidate();
        }
    }

    private void OnBookmarkDataGridViewRowsAdded (object sender, DataGridViewRowsAddedEventArgs e)
    {
        HideIfNeeded();
    }

    private void OnBookmarkDataGridViewRowsRemoved (object sender, DataGridViewRowsRemovedEventArgs e)
    {
        HideIfNeeded();
    }

    private void OnBookmarkWindowSizeChanged (object sender, EventArgs e)
    {
        // if (!this.splitContainer1.Visible)
        // {
        // // redraw the "no bookmarks" display
        // Invalidate();
        // }
    }

    #endregion
}