using System.Drawing.Drawing2D;
using System.Runtime.Versioning;

using LogExpert.Core.Config;

namespace LogExpert.UI.Entities;

[Flags]
internal enum SelectionEdges
{
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 4,
    Right = 8
}

/// <summary>Selection styling and external boundaries shared by all log grids.</summary>
[SupportedOSPlatform("windows")]
internal static class SelectionPainter
{
    public static SelectionCellStyle GetStyle (SelectionHighlightSettings settings, bool selected, bool focused, Color systemColor, bool darkMode)
    {
        var color = settings.CustomColor ?? (focused ? systemColor : darkMode ? Color.FromArgb(90, 90, 90) : Color.FromArgb(170, 170, 170));
        return new(selected && !settings.Outline, color, settings.CustomColor.HasValue);
    }

    public static SelectionEdges GetOutlineEdges (DataGridView grid, int row, int column)
    {
        if (!grid[column, row].Selected)
        {
            return SelectionEdges.None;
        }

        var previousRow = grid.Rows.GetPreviousRow(row, DataGridViewElementStates.Visible);
        var nextRow = grid.Rows.GetNextRow(row, DataGridViewElementStates.Visible);
        var previousColumn = grid.Columns.GetPreviousColumn(grid.Columns[column], DataGridViewElementStates.Visible, DataGridViewElementStates.None);
        var nextColumn = grid.Columns.GetNextColumn(grid.Columns[column], DataGridViewElementStates.Visible, DataGridViewElementStates.None);
        var edges = SelectionEdges.None;
        if (previousRow < 0 || !grid[column, previousRow].Selected)
        {
            edges |= SelectionEdges.Top;
        }

        if (nextRow < 0 || !grid[column, nextRow].Selected)
        {
            edges |= SelectionEdges.Bottom;
        }

        if (previousColumn == null || !grid[previousColumn.Index, row].Selected)
        {
            edges |= SelectionEdges.Left;
        }

        if (nextColumn == null || !grid[nextColumn.Index, row].Selected)
        {
            edges |= SelectionEdges.Right;
        }

        return edges;
    }

    public static void PaintOutline (DataGridView grid, PaintEventArgs e, SelectionHighlightSettings settings)
    {
        if (!settings.Outline || grid.RowCount == 0 || grid.ColumnCount == 0)
        {
            return;
        }

        var style = GetStyle(settings, true, grid.Focused, grid.DefaultCellStyle.SelectionBackColor, Application.IsDarkModeEnabled);
        using var pen = new Pen(style.Background, Math.Max(1, grid.DeviceDpi / 96f));
        var state = e.Graphics.Save();
        try
        {
            // Cell painting changes the graphics clip. Restore the update region before
            // drawing boundaries, and clip each cell to its actual visible portion.
            e.Graphics.SetClip(e.ClipRectangle);
            for (var row = grid.Rows.GetFirstRow(DataGridViewElementStates.Displayed); row >= 0;
                 row = grid.Rows.GetNextRow(row, DataGridViewElementStates.Displayed))
            {
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    if (!column.Visible)
                    {
                        continue;
                    }

                    var bounds = grid.GetCellDisplayRectangle(column.Index, row, false);
                    var visible = grid.GetCellDisplayRectangle(column.Index, row, true);
                    if (visible.IsEmpty || !visible.IntersectsWith(e.ClipRectangle))
                    {
                        continue;
                    }

                    var edges = GetOutlineEdges(grid, row, column.Index);
                    if (edges == SelectionEdges.None)
                    {
                        continue;
                    }

                    e.Graphics.SetClip(Rectangle.Intersect(visible, e.ClipRectangle), CombineMode.Replace);

                    var inset = pen.Width / 2;
                    var left = bounds.Left + inset;
                    var right = bounds.Right - inset;
                    var top = bounds.Top + inset;
                    var bottom = bounds.Bottom - inset;
                    if (edges.HasFlag(SelectionEdges.Top))
                    {
                        e.Graphics.DrawLine(pen, bounds.Left, top, bounds.Right, top);
                    }

                    if (edges.HasFlag(SelectionEdges.Bottom))
                    {
                        e.Graphics.DrawLine(pen, bounds.Left, bottom, bounds.Right, bottom);
                    }

                    if (edges.HasFlag(SelectionEdges.Left))
                    {
                        e.Graphics.DrawLine(pen, left, bounds.Top, left, bounds.Bottom);
                    }

                    if (edges.HasFlag(SelectionEdges.Right))
                    {
                        e.Graphics.DrawLine(pen, right, bounds.Top, right, bounds.Bottom);
                    }
                }
            }
        }
        finally
        {
            e.Graphics.Restore(state);
        }
    }
}

[SupportedOSPlatform("windows")]
internal readonly record struct SelectionCellStyle (bool FillBackground, Color Background, bool CustomColor)
{
    public Color Foreground (Color original) => !FillBackground ? original
        : CustomColor ? PaintHelper.GetForeColorBasedOnBackColor(Background)
        : original == Color.Black ? Color.White : original;

    public bool PaintBackground (DataGridViewCellPaintingEventArgs e)
    {
        if (!FillBackground)
        {
            return false;
        }

        using var brush = new SolidBrush(Background);
        e.Graphics.FillRectangle(brush, e.CellBounds);
        return true;
    }
}