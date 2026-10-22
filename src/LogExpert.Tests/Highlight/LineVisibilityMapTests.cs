using LogExpert.Core.Classes.Highlight;

using NUnit.Framework;

namespace LogExpert.Tests.Highlight;

[TestFixture]
public class LineVisibilityMapTests
{
    private static LineVisibilityMap Map (int lineCount, params int[] hidden)
    {
        return LineVisibilityMap.Empty.Append(lineCount, hidden);
    }

    [Test]
    public void RowToLine_SkipsHiddenLines ()
    {
        var map = Map(6, 1, 2, 4);

        Assert.That(map.VisibleCount, Is.EqualTo(3));
        Assert.That(map.HiddenCount, Is.EqualTo(3));
        Assert.That(Enumerable.Range(0, 3).Select(map.RowToLine), Is.EqualTo(new[] { 0, 3, 5 }));
    }

    [Test]
    public void LineToRow_VisibleLine_ReturnsItsRow_HiddenLine_ReturnsMinusOne ()
    {
        var map = Map(6, 1, 2, 4);

        Assert.That(map.LineToRow(0), Is.EqualTo(0));
        Assert.That(map.LineToRow(3), Is.EqualTo(1));
        Assert.That(map.LineToRow(5), Is.EqualTo(2));
        Assert.That(map.LineToRow(1), Is.EqualTo(-1));
        Assert.That(map.LineToRow(4), Is.EqualTo(-1));
    }

    [TestCase(-1)]
    [TestCase(6)]
    public void OutOfRange_ReturnsMinusOne (int index)
    {
        var map = Map(6, 1);

        Assert.That(map.LineToRow(index), Is.EqualTo(-1));
        Assert.That(map.RowToLine(index), Is.EqualTo(-1));
    }

    [Test]
    public void HiddenAtStartAndEnd_MapsMiddleOnly ()
    {
        var map = Map(5, 0, 1, 4);

        Assert.That(map.VisibleCount, Is.EqualTo(2));
        Assert.That(map.RowToLine(0), Is.EqualTo(2));
        Assert.That(map.RowToLine(1), Is.EqualTo(3));
        Assert.That(map.RowToLine(2), Is.EqualTo(-1));
    }

    [Test]
    public void AllLinesHidden_HasNoRows ()
    {
        var map = Map(3, 0, 1, 2);

        Assert.That(map.VisibleCount, Is.Zero);
        Assert.That(map.RowToLine(0), Is.EqualTo(-1));
        Assert.That(map.NearestRow(1), Is.EqualTo(-1));
    }

    [Test]
    public void Identity_MapsEveryLineToItself ()
    {
        var map = LineVisibilityMap.Identity(4);

        Assert.That(map.VisibleCount, Is.EqualTo(4));
        Assert.That(map.HiddenCount, Is.Zero);
        Assert.That(map.RowToLine(3), Is.EqualTo(3));
        Assert.That(map.LineToRow(2), Is.EqualTo(2));
    }

    [Test]
    public void NearestRow_HiddenLine_PrefersNextVisible_ThenPrevious ()
    {
        var map = Map(8, 2, 3, 6, 7);

        Assert.That(map.NearestRow(4), Is.EqualTo(2), "visible line keeps its own row");
        Assert.That(map.NearestRow(2), Is.EqualTo(2), "hidden line → next visible line 4");
        Assert.That(map.NearestRow(6), Is.EqualTo(3), "no later visible line → previous visible line 5");
        Assert.That(map.NearestRow(100), Is.EqualTo(3), "beyond the end → last visible row");
    }

    [Test]
    public void Append_ExtendsWithNewHiddenLines_AndKeepsOldMapUnchanged ()
    {
        var before = Map(4, 1);

        var after = before.Append(7, [4, 6]);

        Assert.That(before.LineCount, Is.EqualTo(4));
        Assert.That(before.VisibleCount, Is.EqualTo(3));
        Assert.That(after.LineCount, Is.EqualTo(7));
        Assert.That(Enumerable.Range(0, after.VisibleCount).Select(after.RowToLine), Is.EqualTo(new[] { 0, 2, 3, 5 }));
        Assert.That(after.IsAppendOf(before), Is.True);
    }

    [Test]
    public void Append_TwiceFromSameBase_DoesNotCorruptEitherBranch ()
    {
        var baseMap = Map(2);
        var first = baseMap.Append(4, [2]);
        var second = baseMap.Append(4, [3]);

        Assert.That(first.IsHidden(2), Is.True);
        Assert.That(first.IsHidden(3), Is.False);
        Assert.That(second.IsHidden(2), Is.False);
        Assert.That(second.IsHidden(3), Is.True);
    }

    [Test]
    public void Shift_DropsLeadingLines_AndRenumbersTheRest ()
    {
        var map = Map(10, 1, 5, 8);

        var shifted = map.Shift(3);

        Assert.That(shifted.LineCount, Is.EqualTo(7));
        Assert.That(shifted.IsHidden(2), Is.True, "old line 5");
        Assert.That(shifted.IsHidden(5), Is.True, "old line 8");
        Assert.That(shifted.HiddenCount, Is.EqualTo(2));
        Assert.That(shifted.IsAppendOf(map), Is.False);
    }
}
