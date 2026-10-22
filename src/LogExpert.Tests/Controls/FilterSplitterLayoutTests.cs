using LogExpert.UI.Controls.LogWindow;

using NUnit.Framework;

namespace LogExpert.Tests.Controls;

/// <summary>
/// Tests for the splitter-distance clamping used by the filter row's split container
/// (see issue #560). The text filter (Panel1) must not be growable to a size that pushes
/// the Panel2 controls ("Search", checkboxes, "Show advanced...") outside the app.
/// </summary>
[TestFixture]
public class FilterSplitterLayoutTests
{
    [Test]
    public void TryClampSplitterDistance_DesiredWithinBounds_ReturnedUnchanged ()
    {
        var ok = FilterSplitterLayout.TryClampSplitterDistance(
            desiredDistance: 600,
            containerWidth: 1855,
            splitterWidth: 4,
            panel1MinSize: 200,
            panel2MinSize: 660,
            out var distance);

        Assert.That(ok, Is.True);
        Assert.That(distance, Is.EqualTo(600));
    }

    [Test]
    public void TryClampSplitterDistance_DesiredTooLarge_ClampedSoPanel2KeepsMinWidth ()
    {
        // 1855 - 4 (splitter) - 660 (panel2 min) = 1191 is the furthest the splitter may go.
        var ok = FilterSplitterLayout.TryClampSplitterDistance(
            desiredDistance: 1800,
            containerWidth: 1855,
            splitterWidth: 4,
            panel1MinSize: 200,
            panel2MinSize: 660,
            out var distance);

        Assert.That(ok, Is.True);
        Assert.That(distance, Is.EqualTo(1191));
    }

    [Test]
    public void TryClampSplitterDistance_DesiredTooSmall_ClampedUpToPanel1MinWidth ()
    {
        var ok = FilterSplitterLayout.TryClampSplitterDistance(
            desiredDistance: 50,
            containerWidth: 1855,
            splitterWidth: 4,
            panel1MinSize: 200,
            panel2MinSize: 660,
            out var distance);

        Assert.That(ok, Is.True);
        Assert.That(distance, Is.EqualTo(200));
    }

    [Test]
    public void TryClampSplitterDistance_ContainerTooSmallForBothMinimums_ReturnsFalse ()
    {
        // 700 - 4 - 660 = 36, which is below panel1MinSize: no distance honours both minimums,
        // so the caller must skip assignment. Returning a value here is what previously made the
        // SplitContainer.SplitterDistance setter throw on a narrow window.
        var ok = FilterSplitterLayout.TryClampSplitterDistance(
            desiredDistance: 500,
            containerWidth: 700,
            splitterWidth: 4,
            panel1MinSize: 200,
            panel2MinSize: 660,
            out _);

        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryClampSplitterDistance_NarrowWindowRegression_ReturnsFalseInsteadOfThrowingValue ()
    {
        // Regression for the #560 follow-up crash: with the real filter-row minimums (Panel2 = 726),
        // shrinking the LogExpert window forces the Dock=Fill container below ~730px. The old
        // ClampSplitterDistance returned Panel1MinSize (200), and assigning it threw
        // InvalidOperationException ("SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize").
        var ok = FilterSplitterLayout.TryClampSplitterDistance(
            desiredDistance: 5000,
            containerWidth: 684,
            splitterWidth: 4,
            panel1MinSize: 200,
            panel2MinSize: 726,
            out _);

        Assert.That(ok, Is.False);
    }

    [Test]
    public void RequiredPanel2Width_LeavesRoomForRightAnchoredControlAfterRightmostControl ()
    {
        // "Show advanced..." ends at x=649; the right-anchored filter-count label is 71 wide.
        // Panel2 must be at least wide enough that the label starts after the button (+ gap),
        // otherwise the two overlap as the text filter is grown (issue #560 follow-up).
        var result = FilterSplitterLayout.RequiredPanel2Width(
            rightmostControlRightEdge: 649,
            rightAnchoredControlWidth: 71,
            gap: 6);

        Assert.That(result, Is.EqualTo(726));
    }
}
