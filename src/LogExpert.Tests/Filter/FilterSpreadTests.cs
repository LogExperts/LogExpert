using LogExpert.Core.Classes.Filter;

using NUnit.Framework;

namespace LogExpert.Tests.Filter;

[TestFixture]
public class FilterSpreadTests
{
    // Row format: hit line, spread before, spread behind, line count, already-taken history, expected lines.
    [TestCase(5, 0, 0, 100, new int[0], new[] { 5 }, TestName = "Expand_NoSpreadConfigured_ReturnsOnlyTheHitLine")]
    [TestCase(5, 0, 0, 100, new[] { 5 }, new[] { 5 }, TestName = "Expand_NoSpreadConfigured_IgnoresHistory_HistoricalQuirkPreserved")]
    [TestCase(0, 3, 2, 100, new int[0], new[] { 0, 1, 2 }, TestName = "Expand_HitAtLineZero_EmitsNoNegativeLines")]
    [TestCase(3, 5, 0, 100, new int[0], new[] { 0, 1, 2, 3 }, TestName = "Expand_BackSpreadReachingPastTopOfFile_ClampsAtLineZeroAndIncludesIt")]
    [TestCase(97, 0, 5, 100, new int[0], new[] { 97, 98, 99 }, TestName = "Expand_ForeSpreadReachingPastEndOfFile_ClampsAtLastLine")]
    [TestCase(99, 0, 3, 100, new int[0], new[] { 99 }, TestName = "Expand_HitAtLastLineWithForeSpread_ReturnsOnlyTheHit")]
    [TestCase(10, 1, 3, 100, new int[0], new[] { 9, 10, 11, 12, 13 }, TestName = "Expand_AsymmetricSpread_ReturnsBackLinesHitThenForeLines")]
    [TestCase(12, 2, 2, 100, new[] { 8, 9, 10, 11, 12 }, new[] { 13, 14 }, TestName = "Expand_OverlappingWithEarlierHit_SuppressesLinesAlreadyTaken")]
    [TestCase(5, 1, 1, 100, new[] { 5 }, new[] { 4, 6 }, TestName = "Expand_HitItselfAlreadyTaken_IsNotEmittedAgain")]
    public void Expand_Table (int lineNum, int spreadBefore, int spreadBehind, int lineCount, int[] alreadyTaken, int[] expected)
    {
        var result = FilterSpread.Expand(lineNum, spreadBefore, spreadBehind, lineCount, alreadyTaken);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void TrimHistory_LongerThanWindow_DropsOldestEntriesDownToWindowSize ()
    {
        // Window is 2 × 99 (the maximum configurable spread) — unifies the divergent 50/99 constants.
        var history = new List<int>(Enumerable.Range(0, 250));

        FilterSpread.TrimHistory(history);

        Assert.Multiple(() =>
        {
            Assert.That(history, Has.Count.EqualTo(198));
            Assert.That(history[0], Is.EqualTo(52));
            Assert.That(history[^1], Is.EqualTo(249));
        });
    }

    [Test]
    public void TrimHistory_WithinWindow_IsUnchanged ()
    {
        var history = new List<int>(Enumerable.Range(0, 198));

        FilterSpread.TrimHistory(history);

        Assert.That(history, Has.Count.EqualTo(198));
    }

    [Test]
    public void TrimHistory_WorksOnAnyIListImplementation ()
    {
        // The Filter Pipe history is an IList<int>, not a List<int> — the trim rule must not care.
        IList<int> history = [.. Enumerable.Range(0, 200)];

        FilterSpread.TrimHistory(history);

        Assert.Multiple(() =>
        {
            Assert.That(history, Has.Count.EqualTo(198));
            Assert.That(history[0], Is.EqualTo(2));
        });
    }

    [Test]
    public void ShiftLines_OnRollover_SubtractsOffsetFromEveryLine ()
    {
        var shifted = FilterSpread.ShiftLines([10, 20, 30], offset: 5);

        Assert.That(shifted, Is.EqualTo([5, 15, 25]));
    }

    [Test]
    public void ShiftLines_LinesRolledOffTheStart_AreDropped ()
    {
        // Offset 15 rolls lines 10 and 14 off the virtual file; 15 shifts to 0 and survives.
        var shifted = FilterSpread.ShiftLines([10, 14, 15, 40], offset: 15);

        Assert.That(shifted, Is.EqualTo([0, 25]));
    }

    [Test]
    public void RebuildHistory_ResultsLongerThanSpreadMax_TakesTheLastSpreadMaxLines ()
    {
        // The rollover rebuild caps the history at SPREAD_MAX (99), not the 2×99 trim
        // window — a preserved quirk of the original rollover code.
        var results = new List<int>(Enumerable.Range(0, 150));

        var history = FilterSpread.RebuildHistory(results);

        Assert.Multiple(() =>
        {
            Assert.That(history, Has.Count.EqualTo(99));
            Assert.That(history[0], Is.EqualTo(51));
            Assert.That(history[^1], Is.EqualTo(149));
        });
    }

    [Test]
    public void RebuildHistory_ResultsShorterThanSpreadMax_TakesAllOfThem ()
    {
        var history = FilterSpread.RebuildHistory([3, 4, 5]);

        Assert.That(history, Is.EqualTo([3, 4, 5]));
    }

    [Test]
    public void RebuildHistory_EmptyResults_ReturnsEmptyHistory ()
    {
        var history = FilterSpread.RebuildHistory([]);

        Assert.That(history, Is.Empty);
    }

    /// <summary>
    /// Equivalence guard: the single-threaded Log Window filter and the multi-threaded
    /// Core filter accumulate per-hit results through the same recipe
    /// (hit → Expand → append to results/history → TrimHistory). This pins that shared
    /// recipe's output for a hit sequence with overlapping spreads; a call site that
    /// deviates from the recipe no longer matches this specification. Expected values are
    /// a worked example: hits 5, 7 and 50 with spread 2/2 in a 100-line file.
    /// </summary>
    [Test]
    public void AccumulationRecipe_OverlappingHitSequence_ProducesPinnedResultHitAndHistoryLists ()
    {
        int[] hits = [5, 7, 50];
        List<int> resultLines = [];
        List<int> history = [];
        List<int> hitList = [];

        foreach (var hit in hits)
        {
            hitList.Add(hit);
            var expanded = FilterSpread.Expand(hit, spreadBefore: 2, spreadBehind: 2, lineCount: 100, alreadyTaken: history);
            resultLines.AddRange(expanded);
            history.AddRange(expanded);
            FilterSpread.TrimHistory(history);
        }

        Assert.Multiple(() =>
        {
            // Hit 5 contributes 3..7; hit 7 contributes only 8,9 (5..7 already taken); hit 50 contributes 48..52.
            Assert.That(resultLines, Is.EqualTo([3, 4, 5, 6, 7, 8, 9, 48, 49, 50, 51, 52]));
            Assert.That(hitList, Is.EqualTo([5, 7, 50]));
            Assert.That(history, Is.EqualTo(resultLines), "history below the window size mirrors the result lines");
        });
    }
}
