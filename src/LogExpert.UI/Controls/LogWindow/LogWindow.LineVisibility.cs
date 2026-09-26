using System.ComponentModel;
using System.Globalization;

using LogExpert.Core.Classes.Highlight;

namespace LogExpert.UI.Controls.LogWindow;

/// <summary>
/// Hide-line highlight rules. The main grid shows the rows of <see cref="_rowMap"/>; everything that talks
/// to the reader, bookmarks, timestamps or sessions uses original logical lines, converted with
/// <see cref="RowToLine"/> / <see cref="LineToRow"/>. The Window Filter grid is unaffected.
/// </summary>
internal partial class LogWindow
{
    private const int COLUMN_FINDER_HEIGHT = 28;
    private const int HIDDEN_LINES_BAR_HEIGHT = 24;

    private readonly Panel _hiddenLinesBar = new() { Name = "hiddenLinesBar", Dock = DockStyle.Top, Visible = false };
    // Not AutoSize: an auto-sized label keeps its text height and sits at the top instead of centering beside the check box.
    private readonly Label _hiddenLinesLabel = new() { Name = "hiddenLinesLabel", AutoSize = false, Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft };
    private readonly CheckBox _showHiddenLinesCheckBox = new() { Name = "showHiddenLinesCheckBox", AutoSize = true, Dock = DockStyle.Left };

    private LineVisibilityTracker _lineVisibility;

    // Only touched on the UI thread; worker threads read it once into a local.
    private volatile LineVisibilityMap _rowMap = LineVisibilityMap.Empty;
    private LineVisibilityMap _appliedTrackedMap = LineVisibilityMap.Empty;
    private bool _showHiddenLines;
    private bool _isLoadComplete;

    // A saved or reload position (original lines) waiting for the load's first scan.
    private (int CurrentLine, int FirstDisplayedLine)? _pendingPosition;

    /// <summary>Number of lines the active hide rules remove, whether or not the override shows them.</summary>
    internal int HiddenLineCount => _lineVisibility.Map.HiddenCount;

    /// <summary>The per-window "Show hidden lines" override. Transient: not saved in the Session File.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool ShowHiddenLines
    {
        get => _showHiddenLines;
        set
        {
            if (_showHiddenLines == value)
            {
                return;
            }

            _showHiddenLines = value;
            ApplyLineVisibility();
        }
    }

    internal Task WhenLineVisibilityIdle ()
    {
        return _lineVisibility.WhenIdle();
    }

    private void InitializeLineVisibility ()
    {
        _lineVisibility = new LineVisibilityTracker(line => _logFileReader?.GetLogLineMemory(line));
        _lineVisibility.Changed += OnLineVisibilityChanged;

        _showHiddenLinesCheckBox.Text = Resources.LogWindow_UI_CheckBox_ShowHiddenLines;
        _showHiddenLinesCheckBox.CheckedChanged += (_, _) => ShowHiddenLines = _showHiddenLinesCheckBox.Checked;
        _hiddenLinesBar.Padding = new Padding(4, 0, 4, 0);
        _hiddenLinesBar.Controls.Add(_showHiddenLinesCheckBox);
        _hiddenLinesBar.Controls.Add(_hiddenLinesLabel);

        // Row 0 hosts the column finder; the notice bar stacks above it.
        tableLayoutPanel1.Controls.Remove(columnFinderPanel);
        var topRow = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        columnFinderPanel.Dock = DockStyle.Fill;
        topRow.Controls.Add(columnFinderPanel);
        topRow.Controls.Add(_hiddenLinesBar);
        tableLayoutPanel1.Controls.Add(topRow, 0, 0);
    }

    /// <summary>The content is gone (loading, dead file): no rows, no scan, until the next load publishes.</summary>
    private void ResetLineVisibility ()
    {
        _isLoadComplete = false;
        _pendingPosition = null;
        _lineVisibility.Load(0, []);
        _rowMap = LineVisibilityMap.Empty;
        _appliedTrackedMap = _lineVisibility.Map;
        UpdateHiddenLinesBar();
    }

    private void UpdateTopRowHeight ()
    {
        var height = _guiStateArgs.ColumnFinderVisible ? COLUMN_FINDER_HEIGHT : 0;
        if (_hiddenLinesBar.Visible)
        {
            _hiddenLinesBar.Height = LogicalToDeviceUnits(HIDDEN_LINES_BAR_HEIGHT);
            height += _hiddenLinesBar.Height;
        }

        columnFinderPanel.Visible = _guiStateArgs.ColumnFinderVisible;
        tableLayoutPanel1.RowStyles[0].Height = height;
    }

    private void UpdateHiddenLinesBar ()
    {
        var hidden = HiddenLineCount;
        _hiddenLinesLabel.Text = string.Format(CultureInfo.CurrentCulture, Resources.LogWindow_UI_Label_HiddenLines, hidden);
        _hiddenLinesLabel.Width = _hiddenLinesLabel.PreferredWidth + LogicalToDeviceUnits(12);
        _showHiddenLinesCheckBox.Checked = _showHiddenLines;

        var visible = hidden > 0 || _showHiddenLines;
        if (_hiddenLinesBar.Visible != visible)
        {
            _hiddenLinesBar.Visible = visible;
            UpdateTopRowHeight();
        }
    }

    private List<Core.Classes.Highlight.HighlightEntry> CurrentHighlightEntries ()
    {
        lock (_currentHighlightGroupLock)
        {
            return [.. _currentHighlightGroup.HighlightEntryList];
        }
    }

    private void RebuildLineVisibility ()
    {
        if (!_isLoading && _logFileReader != null)
        {
            _lineVisibility.Rebuild(CurrentHighlightEntries());
        }
    }

    private void OnLineVisibilityChanged (object? sender, LineVisibilityChangedEventArgs e)
    {
        if (_isClosing || IsDisposed || Disposing)
        {
            return;
        }

        try
        {
            // Marshals through a parent's handle when this (background) tab has none yet.
            _ = BeginInvoke(() =>
            {
                if (e.Error != null)
                {
                    _logger.Warn(e.Error, "Hide rules failed");
                    StatusLineError(string.Format(CultureInfo.CurrentCulture, Resources.LogWindow_UI_StatusLineError_HideRulesFailed, e.Error.Message));
                }

                ApplyLineVisibility();
            });
        }
        catch (InvalidOperationException)
        {
            // No handle anywhere up the chain yet: OnHandleCreated applies the current map.
        }
    }

    protected override void OnHandleCreated (EventArgs e)
    {
        base.OnHandleCreated(e);
        if (_lineVisibility != null && !ReferenceEquals(_appliedTrackedMap, _lineVisibility.Map))
        {
            _ = BeginInvoke(ApplyLineVisibility);
        }
    }

    /// <summary>
    /// The map the grid should display for <paramref name="tracked"/>: no rows while the load's first scan runs, the
    /// tracked map itself, or, with the override on, an identity map that keeps growing in place so tail appends
    /// stay appends.
    /// </summary>
    private LineVisibilityMap EffectiveMap (LineVisibilityMap tracked)
    {
        var current = _rowMap;
        if (!_showHiddenLines)
        {
            if (!_lineVisibility.IsLoadPending)
            {
                return tracked;
            }

            return current.LineCount == 0 ? current : LineVisibilityMap.Empty;
        }

        return current.HiddenCount == 0 && current.LineCount <= tracked.LineCount && tracked.IsAppendOf(_appliedTrackedMap)
            ? current.Append(tracked.LineCount, [])
            : LineVisibilityMap.Identity(tracked.LineCount);
    }

    /// <summary>
    /// Publishes the tracker's current map to the grid (UI thread). Row count and mapping change together; the
    /// selected original line is kept, or moved to the next (else previous) visible line.
    /// </summary>
    private void ApplyLineVisibility ()
    {
        if (_isLoading || _isClosing || _logFileReader == null || IsDisposed)
        {
            UpdateHiddenLinesBar();
            return;
        }

        if (PublishTrackedMap(0) && _guiStateArgs.FollowTail && dataGridView.RowCount > 0)
        {
            _columnCache.MarkPrefetchStale();
            dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.RowCount - 1;
        }

        dataGridView.Invalidate();
        UpdateLineNavigationReadiness();
    }

    /// <summary>Switches the grid to the tracker's current map; returns whether the displayed map changed.</summary>
    private bool PublishTrackedMap (int rolloverOffset)
    {
        var tracked = _lineVisibility.Map;
        var newMap = EffectiveMap(tracked);
        _appliedTrackedMap = tracked;

        var changed = !ReferenceEquals(newMap, _rowMap);
        if (changed)
        {
            SetRowMap(newMap, rolloverOffset);
        }

        if (_pendingPosition is { } position && !_lineVisibility.IsLoadPending)
        {
            _pendingPosition = null;
            ApplyPosition(position.CurrentLine, position.FirstDisplayedLine);
        }

        UpdateHiddenLinesBar();
        return changed;
    }

    /// <summary>
    /// Restores a saved or reload position (original lines; a hidden one resolves to the nearest visible line),
    /// deferred until the load's first scan has published the rows.
    /// </summary>
    private void RestorePosition (int currentLine, int firstDisplayedLine)
    {
        if (_lineVisibility.IsLoadPending && !_showHiddenLines)
        {
            _pendingPosition = (currentLine, firstDisplayedLine);
            return;
        }

        ApplyPosition(currentLine, firstDisplayedLine);
    }

    private void ApplyPosition (int currentLine, int firstDisplayedLine)
    {
        var currentRow = currentLine >= 0 ? _rowMap.NearestRow(currentLine) : -1;
        if (currentRow >= 0)
        {
            SelectRow(currentRow, false, true);
        }

        var firstRow = firstDisplayedLine >= 0 ? _rowMap.NearestRow(firstDisplayedLine) : -1;
        if (firstRow >= 0)
        {
            dataGridView.FirstDisplayedScrollingRowIndex = firstRow;
        }
    }

    /// <summary>
    /// Switches the grid to <paramref name="newMap"/>. An append only grows the row count; any other change
    /// rebuilds the rows and restores the selection and scroll position by original line, after moving those
    /// lines up by <paramref name="rolloverOffset"/>.
    /// </summary>
    private void SetRowMap (LineVisibilityMap newMap, int rolloverOffset)
    {
        var oldMap = _rowMap;
        if (newMap.IsAppendOf(oldMap) && rolloverOffset == 0)
        {
            _rowMap = newMap;
            dataGridView.RowCount = newMap.VisibleCount;
            return;
        }

        var currentLine = oldMap.RowToLine(dataGridView.CurrentCellAddress.Y);
        var firstLine = oldMap.RowToLine(dataGridView.FirstDisplayedScrollingRowIndex);
        var hadCurrentLine = currentLine >= 0;

        dataGridView.RowCount = 0;
        _columnCache.MarkPrefetchStale();
        _rowMap = newMap;
        dataGridView.RowCount = newMap.VisibleCount;
        if (_rowHeightList.Count > 0 && dataGridView.RowCount > 0)
        {
            dataGridView.UpdateRowHeightInfo(0, true);
        }

        if (dataGridView.RowCount == 0)
        {
            return;
        }

        if (firstLine >= 0)
        {
            var firstRow = newMap.NearestRow(Math.Max(0, firstLine - rolloverOffset));
            if (firstRow >= 0)
            {
                dataGridView.FirstDisplayedScrollingRowIndex = firstRow;
            }
        }

        if (hadCurrentLine && !_guiStateArgs.FollowTail)
        {
            var row = newMap.NearestRow(Math.Max(0, currentLine - rolloverOffset));
            if (row >= 0)
            {
                dataGridView.CurrentCell = dataGridView.Rows[row].Cells[0];
                dataGridView.Rows[row].Selected = true;
            }
        }
    }

    /// <summary>Original line displayed in a main-grid row, or -1.</summary>
    private int RowToLine (int row)
    {
        return _rowMap.RowToLine(row);
    }

    /// <summary>Main-grid row of an original line, or -1 when it is hidden or out of range.</summary>
    private int LineToRow (int line)
    {
        return _rowMap.LineToRow(line);
    }

    /// <summary>
    /// Explicit navigation to an original line: a line beyond the end resolves to the last row; a hidden line turns
    /// on "Show hidden lines" so that exact line can be selected. Returns the row, or -1.
    /// </summary>
    private int RevealLine (int line)
    {
        if (line < 0)
        {
            return -1;
        }

        if (line >= _rowMap.LineCount)
        {
            return dataGridView.RowCount - 1;
        }

        if (_rowMap.IsHidden(line))
        {
            ShowHiddenLines = true;
        }

        return LineToRow(line);
    }

    /// <summary>Line navigation may run once the file is loaded and the first visibility scan is published.</summary>
    private void UpdateLineNavigationReadiness ()
    {
        if (_isLoadComplete && !_isReadyForLineNavigation && !_lineVisibility.IsScanning)
        {
            _isReadyForLineNavigation = true;
            ApplyPendingLineNavigation();
        }
    }
}
