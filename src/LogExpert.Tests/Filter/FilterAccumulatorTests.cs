using LogExpert.Core.Classes.Filter;

using NUnit.Framework;

namespace LogExpert.Tests.Filter;

[TestFixture]
public class FilterAccumulatorTests
{
    /// <summary>
    /// The accumulator is the single home of the per-hit recipe (hit → Expand → append →
    /// TrimHistory). Expected values are the worked example pinned by
    /// <see cref="FilterSpreadTests"/>' accumulation-recipe guard: hits 5, 7 and 50 with
    /// spread 2/2 in a 100-line file.
    /// </summary>
    /// <summary>
    /// The tail filter path continues a full run: the accumulator adopts the window's canonical
    /// lists, so a history seeded by the run (here: the run already emitted 3..7 for a hit at 5)
    /// dedups subsequent tail hits exactly as if one accumulation had never stopped.
    /// </summary>
    [Test]
    public void AdoptedLists_HistorySeededByFullRun_DedupsContinuationHits ()
    {
        List<int> resultLines = [3, 4, 5, 6, 7];
        List<int> hitLines = [5];
        List<int> history = [3, 4, 5, 6, 7];
        var accumulator = new FilterAccumulator(resultLines, hitLines, history);

        var expansion = accumulator.AddHit(7, spreadBefore: 2, spreadBehind: 2, lineCount: 100);

        Assert.Multiple(() =>
        {
            // Hit 7 contributes only 8,9 — 5..7 were already taken by the seeded run.
            Assert.That(expansion, Is.EqualTo([8, 9]), "AddHit returns the hit's expansion (Filter Pipes write it out)");
            Assert.That(resultLines, Is.EqualTo([3, 4, 5, 6, 7, 8, 9]), "the adopted list instance is the accumulator's state");
            Assert.That(hitLines, Is.EqualTo([5, 7]));
        });
    }

    [Test]
    public void AddHit_OverlappingHitSequence_ProducesPinnedResultHitAndHistoryLists ()
    {
        var accumulator = new FilterAccumulator();

        _ = accumulator.AddHit(5, spreadBefore: 2, spreadBehind: 2, lineCount: 100);
        _ = accumulator.AddHit(7, spreadBefore: 2, spreadBehind: 2, lineCount: 100);
        _ = accumulator.AddHit(50, spreadBefore: 2, spreadBehind: 2, lineCount: 100);

        Assert.Multiple(() =>
        {
            // Hit 5 contributes 3..7; hit 7 contributes only 8,9 (5..7 already taken); hit 50 contributes 48..52.
            Assert.That(accumulator.ResultLines, Is.EqualTo([3, 4, 5, 6, 7, 8, 9, 48, 49, 50, 51, 52]));
            Assert.That(accumulator.HitLines, Is.EqualTo([5, 7, 50]));
            Assert.That(accumulator.History, Is.EqualTo(accumulator.ResultLines),
                "history below the window size mirrors the result lines");
        });
    }
}
