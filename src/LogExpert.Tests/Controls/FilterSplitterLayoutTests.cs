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
    [TestCase(207, 300, 207)]
    [TestCase(310, 288, 288)]
    [TestCase(362, 310, 310)]
    [TestCase(414, 390, 390)]
    [TestCase(207, -10, 60)]
    [TestCase(207, 0, 60)]
    [TestCase(207, 30, 60)]
    public void FitInputWidth_ReservesSpaceBeforeAlignedColumn (int desired, int available, int expected)
    {
        Assert.That(FilterSplitterLayout.FitInputWidth(desired, available), Is.EqualTo(expected));
    }

    [TestCase(5, 35, 13, 16)]
    [TestCase(10, 70, 26, 32)]
    [TestCase(10, 70, 42, 24)]
    [TestCase(10, 70, 70, 10)]
    public void CenterVertically_UsesSharedRowCenter (int top, int rowHeight, int height, int expected)
    {
        Assert.That(FilterSplitterLayout.CenterVertically(top, rowHeight, height), Is.EqualTo(expected));
    }

    [TestCase(41, 566)]
    [TestCase(51, 556)]
    [TestCase(700, 0)]
    public void MoveTopPreservingBottom_ResizesPanel (int top, int expectedHeight)
    {
        var bounds = new Rectangle(3, 46, 1855, 561);
        var result = FilterSplitterLayout.MoveTopPreservingBottom(bounds, top);
        Assert.That(result, Is.EqualTo(new Rectangle(3, top, 1855, expectedHeight)));
    }

    [TestCase(160, 100, 25, 25, 74)]
    [TestCase(160, 300, 25, 25, 160)]
    public void TryClampSplitterDistance_AdvancedPanelFitsShortWindow (int desired, int height, int min1, int min2, int expected)
    {
        Assert.That(FilterSplitterLayout.TryClampSplitterDistance(desired, height, 1, min1, min2, out var distance), Is.True);
        Assert.That(distance, Is.EqualTo(expected));
    }

    [Test]
    public void TryClampSplitterDistance_AdvancedPanelCannotFit_SkipsAssignment ()
    {
        Assert.That(FilterSplitterLayout.TryClampSplitterDistance(160, 40, 1, 25, 25, out _), Is.False);
    }

    [Test]
    public void FitPanelMinimums_ReservesInputBeforeButtons ()
    {
        var minimums = FilterSplitterLayout.FitPanelMinimums(165, 726, 800, 4);
        Assert.That(minimums.Panel1, Is.EqualTo(165));
        Assert.That(minimums.Panel2, Is.EqualTo(631));
    }

    [TestCase(165, 726, 1855, 4, 165, 726)]
    [TestCase(265, 1452, 1200, 8, 265, 927)]
    [TestCase(165, 726, 100, 4, 96, 0)]
    [TestCase(165, 726, 0, 4, 0, 0)]
    public void FitPanelMinimums_BothMinimumsFitAvailableWidth (int desired1, int desired2, int width, int splitter, int expected1, int expected2)
    {
        var result = FilterSplitterLayout.FitPanelMinimums(desired1, desired2, width, splitter);
        Assert.That(result, Is.EqualTo((expected1, expected2)));
        Assert.That(result.Panel1 + result.Panel2, Is.LessThanOrEqualTo(Math.Max(0, width - splitter)));
    }

    [Test]
    public void MeasureGroups_IncludesWiderControlsAndGaps ()
    {
        var result = FilterSplitterLayout.MeasureGroups([(72, 17), (65, 80), (56, 17)], 6);
        Assert.That(result.Widths, Is.EqualTo(new[] { 72, 80, 56 }));
        Assert.That(result.TotalWidth, Is.EqualTo(232));
    }

    [Test]
    [Apartment(System.Threading.ApartmentState.STA)]
    public void PanelMinimums_CanBeAppliedDuringDpiChangeWithoutThrowing ()
    {
        using var split = new System.Windows.Forms.SplitContainer { Width = 800 };
        split.Panel1MinSize = 200;
        split.Panel2MinSize = 500;
        var minimums = FilterSplitterLayout.FitPanelMinimums(265, 1452, split.ClientSize.Width, split.SplitterWidth);
        Assert.DoesNotThrow(() =>
        {
            split.Panel1MinSize = 0;
            split.Panel2MinSize = 0;
            split.Panel2MinSize = minimums.Panel2;
            split.Panel1MinSize = minimums.Panel1;
        });
    }

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
