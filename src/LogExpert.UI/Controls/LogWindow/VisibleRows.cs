using LogExpert.Core.Classes.Highlight;

namespace LogExpert.UI.Controls.LogWindow;

/// <summary>The selected and first displayed original lines of the main grid; -1 when there is none.</summary>
internal readonly record struct GridPosition (int CurrentLine, int FirstDisplayedLine);

/// <summary>
/// The rows of a Log Window's main grid after hiding: publishes a <see cref="LineVisibilityTracker"/>'s map to the grid
/// and keeps the selection and scroll position by original line. UI thread only, except <see cref="Map"/>.
/// </summary>
internal sealed class VisibleRows (DataGridView grid, LineVisibilityTracker tracker, Func<bool> isFollowTail, Action markPrefetchStale, Func<bool> hasRowHeights)
{
    // Other threads read it once into a local.
    private volatile LineVisibilityMap _map = LineVisibilityMap.Empty;
    private LineVisibilityMap _appliedTrackedMap = LineVisibilityMap.Empty;
    private GridPosition? _pendingPosition;

    /// <summary>The map the grid displays.</summary>
    public LineVisibilityMap Map => _map;

    /// <summary>The per-window "Show hidden lines" override; takes effect on the next <see cref="Publish"/>.</summary>
    public bool ShowHiddenLines { get; set; }

    public bool IsUpToDate => ReferenceEquals(_appliedTrackedMap, tracker.Map);

    /// <summary>The content is gone: no rows until the next load publishes.</summary>
    public void Reset ()
    {
        _pendingPosition = null;
        _map = LineVisibilityMap.Empty;
        _appliedTrackedMap = tracker.Map;
    }

    /// <summary>
    /// Switches the grid to the tracker's current map, with selected and first displayed lines moved up by
    /// <paramref name="rolloverOffset"/>; returns whether the displayed map changed.
    /// </summary>
    public bool Publish (int rolloverOffset)
    {
        var tracked = tracker.Map;
        var newMap = EffectiveMap(tracked);
        _appliedTrackedMap = tracked;

        var changed = !ReferenceEquals(newMap, _map);
        if (changed)
        {
            SetMap(newMap, rolloverOffset);
        }

        if (_pendingPosition is { } position && !tracker.IsLoadPending)
        {
            _pendingPosition = null;
            ApplyPosition(position, true);
        }

        return changed;
    }

    public GridPosition CurrentPosition ()
    {
        return new GridPosition(
            grid.CurrentRow == null ? -1 : _map.RowToLine(grid.CurrentRow.Index),
            _map.RowToLine(grid.FirstDisplayedScrollingRowIndex));
    }

    /// <summary>Restores a saved or reload position, deferred until the load's first scan has published the rows.</summary>
    public void RestorePosition (GridPosition position)
    {
        if (tracker.IsLoadPending && !ShowHiddenLines)
        {
            _pendingPosition = position;
            return;
        }

        ApplyPosition(position, true);
    }

    /// <summary>
    /// No rows while the load's first scan runs, the tracked map itself, or, with the override on, an identity map
    /// that keeps growing in place so tail appends stay appends.
    /// </summary>
    private LineVisibilityMap EffectiveMap (LineVisibilityMap tracked)
    {
        var current = _map;
        if (!ShowHiddenLines)
        {
            if (!tracker.IsLoadPending)
            {
                return tracked;
            }

            return current.LineCount == 0 ? current : LineVisibilityMap.Empty;
        }

        return current.HiddenCount == 0 && current.LineCount <= tracked.LineCount && tracked.IsAppendOf(_appliedTrackedMap)
            ? current.Append(tracked.LineCount, [])
            : LineVisibilityMap.Identity(tracked.LineCount);
    }

    /// <summary>An append only grows the row count; any other change rebuilds the rows and keeps the position.</summary>
    private void SetMap (LineVisibilityMap newMap, int rolloverOffset)
    {
        if (newMap.IsAppendOf(_map) && rolloverOffset == 0)
        {
            _map = newMap;
            grid.RowCount = newMap.VisibleCount;
            return;
        }

        var position = CurrentPosition();

        grid.RowCount = 0;
        markPrefetchStale();
        _map = newMap;
        grid.RowCount = newMap.VisibleCount;
        if (grid.RowCount == 0)
        {
            return;
        }

        if (hasRowHeights())
        {
            grid.UpdateRowHeightInfo(0, true);
        }

        ApplyPosition(Shifted(position, rolloverOffset), !isFollowTail());
    }

    /// <summary>Scrolls to and optionally selects a position; a hidden line resolves to the nearest visible row.</summary>
    private void ApplyPosition (GridPosition position, bool select)
    {
        var currentRow = position.CurrentLine >= 0 ? _map.NearestRow(position.CurrentLine) : -1;
        if (select && currentRow >= 0)
        {
            grid.CurrentCell = grid.Rows[currentRow].Cells[0];
            grid.Rows[currentRow].Selected = true;
        }

        var firstRow = position.FirstDisplayedLine >= 0 ? _map.NearestRow(position.FirstDisplayedLine) : -1;
        if (firstRow >= 0)
        {
            grid.FirstDisplayedScrollingRowIndex = firstRow;
        }
    }

    private static GridPosition Shifted (GridPosition position, int offset)
    {
        return new GridPosition(
            position.CurrentLine < 0 ? -1 : Math.Max(0, position.CurrentLine - offset),
            position.FirstDisplayedLine < 0 ? -1 : Math.Max(0, position.FirstDisplayedLine - offset));
    }
}
