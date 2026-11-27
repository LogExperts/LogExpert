namespace LogExpert.UI.Controls.LogWindow;

/// <summary>
/// Pure layout math for the filter row's split container.
/// Keeps the text filter (Panel1) from being grown so large that the Panel2 controls
/// ("Search", the filter checkboxes and the "Show advanced..." button) are pushed
/// outside the visible area of the application.
/// </summary>
internal static class FilterSplitterLayout
{
    public static int FitInputWidth (int desiredWidth, int availableWidth, int minimumWidth = 60)
    {
        return Math.Max(minimumWidth, Math.Min(desiredWidth, availableWidth));
    }

    public static (int Panel1, int Panel2) FitPanelMinimums (int desiredPanel1Minimum, int desiredPanel2Minimum, int containerWidth, int splitterWidth)
    {
        // Preserve the input minimum first. Below that width even the input must shrink;
        // the remaining space is all that can safely be reserved for the button panel.
        var available = Math.Max(0, containerWidth - splitterWidth);
        var panel1 = Math.Min(desiredPanel1Minimum, available);
        return (panel1, Math.Min(desiredPanel2Minimum, available - panel1));
    }

    public static (int[] Widths, int TotalWidth) MeasureGroups ((int LabelWidth, int ControlWidth)[] groups, int gap)
    {
        var widths = groups.Select(group => Math.Max(group.LabelWidth, group.ControlWidth)).ToArray();
        return (widths, widths.Sum() + (widths.Length + 1) * gap);
    }

    public static int CenterVertically (int rowTop, int rowHeight, int controlHeight)
    {
        return rowTop + (rowHeight - controlHeight) / 2;
    }

    public static Rectangle MoveTopPreservingBottom (Rectangle bounds, int top)
    {
        return new Rectangle(bounds.Left, top, bounds.Width, Math.Max(0, bounds.Bottom - top));
    }

    /// <summary>
    /// Clamps a desired splitter distance so that neither panel shrinks below its minimum size.
    /// </summary>
    /// <param name="desiredDistance">The requested distance (Panel1 width) in pixels.</param>
    /// <param name="containerWidth">Total width of the split container.</param>
    /// <param name="splitterWidth">Width of the splitter bar.</param>
    /// <param name="panel1MinSize">Minimum width of Panel1 (the text filter).</param>
    /// <param name="panel2MinSize">Minimum width of Panel2 (the buttons/checkboxes).</param>
    /// <param name="distance">The clamped distance to apply; only meaningful when this method returns <c>true</c>.</param>
    /// <returns>
    /// <c>true</c> when a distance honouring both minimums exists and <paramref name="distance"/> was set;
    /// <c>false</c> when the container is too narrow to honour both minimums at once. In that degenerate
    /// state there is no valid distance, and the caller must NOT assign one: WinForms'
    /// <c>SplitContainer.SplitterDistance</c> setter throws <see cref="InvalidOperationException"/> when the
    /// value cannot fit between <c>Panel1MinSize</c> and <c>Width - Panel2MinSize - SplitterWidth</c>.
    /// </returns>
    public static bool TryClampSplitterDistance (int desiredDistance, int containerWidth, int splitterWidth, int panel1MinSize, int panel2MinSize, out int distance)
    {
        var maxDistance = containerWidth - splitterWidth - panel2MinSize;
        if (maxDistance < panel1MinSize)
        {
            // Both minimums cannot fit (a narrow window forced the container below their sum).
            // No splitter distance is valid here; the caller leaves the splitter where it is.
            distance = panel1MinSize;
            return false;
        }

        distance = Math.Clamp(desiredDistance, panel1MinSize, maxDistance);
        return true;
    }

    /// <summary>
    /// Computes the minimum width Panel2 needs so that the rightmost left-anchored control
    /// (the "Show advanced..." button) never overlaps the right-anchored control next to it
    /// (the filter-count label) as the text filter is grown.
    /// </summary>
    /// <param name="rightmostControlRightEdge">Right edge (Left + Width) of the rightmost left-anchored control.</param>
    /// <param name="rightAnchoredControlWidth">Width of the right-anchored control.</param>
    /// <param name="gap">Desired gap in pixels between the two controls.</param>
    public static int RequiredPanel2Width (int rightmostControlRightEdge, int rightAnchoredControlWidth, int gap)
    {
        return rightmostControlRightEdge + gap + rightAnchoredControlWidth;
    }
}
