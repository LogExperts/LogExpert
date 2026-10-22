using ColumnizerLib;

using LogExpert.Core.Callback;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Interfaces;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Filter;

/// <summary>
/// The Filter Engine contract: every fixture runs through every engine and must produce
/// byte-identical, sorted-ascending, duplicate-free ResultLines/HitLines and the same Outcome.
/// Serial and parallel are held to each other by the same table.
/// </summary>
[TestFixture]
public class FilterEngineEquivalenceTests
{
    private static IEnumerable<IFilterEngine> Engines ()
    {
        yield return new SerialFilterEngine();
        // Fixed chunk count so the chunk-boundary fixtures land deterministically on every machine.
        yield return new ParallelFilterEngine(chunkCount: 4);
    }

    [TestCaseSource(nameof(Engines))]
    public void Run_PlainTextHitsWithSpread_ReturnsCompletedSortedResults (IFilterEngine engine)
    {
        var callback = CallbackOf("ok0", "ERROR1", "ok2", "ok3", "ok4", "ERROR5", "ok6");
        var filterParams = ParamsOf("ERROR", spreadBefore: 1, spreadBehind: 1);

        var run = engine.Run(filterParams, callback, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(run.Outcome, Is.EqualTo(FilterRunOutcome.Completed));
            // Hit 1 contributes 0..2, hit 5 contributes 4..6.
            Assert.That(run.ResultLines, Is.EqualTo([0, 1, 2, 4, 5, 6]));
            Assert.That(run.HitLines, Is.EqualTo([1, 5]));
        });
    }

    /// <summary>
    /// 20 lines, chunk count 4 → parallel chunk boundaries at 5, 10, 15. Hits at 4 and 5 sit on
    /// either side of a boundary with spread 2 — each parallel worker starts with an empty dedup
    /// window, so the overlap (lines 3..7) is emitted by both workers and must be merged away to
    /// match the serial single-pass output.
    /// </summary>
    [TestCaseSource(nameof(Engines))]
    public void Run_SpreadOverlapStraddlingChunkBoundary_MatchesSerialOutput (IFilterEngine engine)
    {
        var lines = Enumerable.Range(0, 20).Select(i => i is 4 or 5 ? $"ERROR{i}" : $"ok{i}").ToArray();
        var callback = CallbackOf(lines);
        var filterParams = ParamsOf("ERROR", spreadBefore: 2, spreadBehind: 2);

        var run = engine.Run(filterParams, callback, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(run.Outcome, Is.EqualTo(FilterRunOutcome.Completed));
            // Hit 4 contributes 2..6, hit 5 contributes only 7 — one deduplicated span.
            Assert.That(run.ResultLines, Is.EqualTo([2, 3, 4, 5, 6, 7]));
            Assert.That(run.HitLines, Is.EqualTo([4, 5]));
        });
    }

    /// <summary>
    /// Range filter (begin/end markers) spanning parallel chunk boundaries: begin at 2, end at 12,
    /// chunks of 5. The worker owning the begin marker must overrun its chunk while in range; the
    /// mid-range workers see no marker and contribute nothing; the merge yields the serial range.
    /// </summary>
    [TestCaseSource(nameof(Engines))]
    public void Run_RangeFilterSpanningChunkBoundary_MatchesSerialOutput (IFilterEngine engine)
    {
        var lines = Enumerable.Range(0, 20)
            .Select(i => i == 2 ? "BEGIN here" : i == 12 ? "END here" : $"ok{i}")
            .ToArray();
        var callback = CallbackOf(lines);
        var filterParams = ParamsOf("BEGIN");
        filterParams.IsRangeSearch = true;
        filterParams.RangeSearchText = "END";

        var run = engine.Run(filterParams, callback, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(run.Outcome, Is.EqualTo(FilterRunOutcome.Completed));
            // Every line from the begin marker through the end marker, inclusive.
            Assert.That(run.HitLines, Is.EqualTo(Enumerable.Range(2, 11)));
            Assert.That(run.ResultLines, Is.EqualTo(Enumerable.Range(2, 11)));
        });
    }

    /// <summary>
    /// A throwing filter condition (column-restrict with no columnizer — the realistic
    /// "columnizer changed mid-flight" fault) is a narrated outcome, never a rethrow. This is the
    /// fixture that pins the parallel path's crash fix: today's MultiThreadedFilter would take the
    /// app down on this input.
    /// </summary>
    [TestCaseSource(nameof(Engines))]
    public void Run_ThrowingFilterCondition_ReturnsFailedWithError (IFilterEngine engine)
    {
        var callback = CallbackOf("ok0", "ok1");
        var filterParams = ParamsOf("ERROR");
        filterParams.ColumnRestrict = true;
        filterParams.ColumnList.Add(0);
        filterParams.CurrentColumnizer = null;

        var run = engine.Run(filterParams, callback, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(run.Outcome, Is.EqualTo(FilterRunOutcome.Failed));
            Assert.That(run.Error, Is.Not.Null);
        });
    }

    [TestCaseSource(nameof(Engines))]
    public void Run_CancelledToken_ReturnsCancelledWithoutThrowing (IFilterEngine engine)
    {
        var callback = CallbackOf("ERROR0", "ERROR1", "ERROR2");
        var filterParams = ParamsOf("ERROR");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var run = engine.Run(filterParams, callback, cts.Token);

        Assert.That(run.Outcome, Is.EqualTo(FilterRunOutcome.Cancelled));
    }

    /// <summary>
    /// Serial-only pin: the cancel check sits after the line is processed (matching the original
    /// loop), so a cancel requested before the first line still keeps that line's hit in the
    /// partial result.
    /// </summary>
    [Test]
    public void Run_Serial_CancelObservedAfterLineIsProcessed_KeepsFirstLinesHit ()
    {
        var callback = CallbackOf("ERROR0", "ERROR1", "ERROR2");
        var filterParams = ParamsOf("ERROR");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var run = new SerialFilterEngine().Run(filterParams, callback, cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(run.Outcome, Is.EqualTo(FilterRunOutcome.Cancelled));
            Assert.That(run.HitLines, Is.EqualTo([0]), "partial results stand on cancel");
        });
    }

    /// <summary>
    /// The engine snapshots FilterParams at entry: a caller mutating the shared instance while the
    /// run executes (filter-panel edits) cannot affect the run. The mutation is injected through the
    /// reader so it happens mid-run, synchronously.
    /// </summary>
    [TestCaseSource(nameof(Engines))]
    public void Run_ParamsMutatedMidRun_OutcomeUnaffected (IFilterEngine engine)
    {
        var filterParams = ParamsOf("ERROR");
        var callback = CallbackOf(["ok0", "ERROR1", "ok2", "ERROR3"],
            onLineRead: _ => filterParams.SearchText = "NEVERMATCHES");

        var run = engine.Run(filterParams, callback, CancellationToken.None);

        Assert.That(run.HitLines, Is.EqualTo([1, 3]),
            "the snapshot taken at entry governs the whole run");
    }

    /// <summary>
    /// The engine covers [0, LineCount-at-entry): lines appended while the run executes are not
    /// filtered — they belong to the tail path. Pins the deliberate serial change (the old loop
    /// chased EOF).
    /// </summary>
    [TestCaseSource(nameof(Engines))]
    public void Run_LinesAppendedMidRun_AreNotFiltered (IFilterEngine engine)
    {
        var lines = new List<string> { "ERROR0", "ok1", "ok2" };
        var callback = CallbackOf(lines,
            onLineRead: _ =>
            {
                if (lines.Count == 3)
                {
                    lines.Add("ERROR3");
                    lines.Add("ERROR4");
                }
            });
        var filterParams = ParamsOf("ERROR");

        var run = engine.Run(filterParams, callback, CancellationToken.None);

        Assert.That(run.HitLines, Is.EqualTo([0]),
            "lines appended after Run entry belong to the tail path");
    }

    [TestCaseSource(nameof(Engines))]
    public void Run_HitAtLineZeroWithBackSpread_ClampsAtStartAndKeepsLineZero (IFilterEngine engine)
    {
        var callback = CallbackOf("ERROR0", "ok1", "ok2");
        var filterParams = ParamsOf("ERROR", spreadBefore: 2, spreadBehind: 1);

        var run = engine.Run(filterParams, callback, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(run.ResultLines, Is.EqualTo([0, 1]), "line 0 is a valid hit; back spread clamps at the file start");
            Assert.That(run.HitLines, Is.EqualTo([0]));
        });
    }

    [TestCaseSource(nameof(Engines))]
    public void Run_HitAtLastLineWithForeSpread_ClampsAtEndOfFile (IFilterEngine engine)
    {
        var callback = CallbackOf("ok0", "ok1", "ERROR2");
        var filterParams = ParamsOf("ERROR", spreadBefore: 1, spreadBehind: 2);

        var run = engine.Run(filterParams, callback, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(run.ResultLines, Is.EqualTo([1, 2]), "fore spread clamps at the last line");
            Assert.That(run.HitLines, Is.EqualTo([2]));
        });
    }

    [TestCaseSource(nameof(Engines))]
    public void Run_NoHits_ReturnsCompletedEmptyLists (IFilterEngine engine)
    {
        var callback = CallbackOf("ok0", "ok1", "ok2");
        var filterParams = ParamsOf("ERROR");

        var run = engine.Run(filterParams, callback, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(run.Outcome, Is.EqualTo(FilterRunOutcome.Completed));
            Assert.That(run.ResultLines, Is.Empty);
            Assert.That(run.HitLines, Is.Empty);
        });
    }

    [TestCaseSource(nameof(Engines))]
    public void Run_EmptyFile_ReturnsCompletedEmptyLists (IFilterEngine engine)
    {
        var callback = CallbackOf();
        var filterParams = ParamsOf("ERROR");

        var run = engine.Run(filterParams, callback, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(run.Outcome, Is.EqualTo(FilterRunOutcome.Completed));
            Assert.That(run.ResultLines, Is.Empty);
            Assert.That(run.HitLines, Is.Empty);
        });
    }

    #region Fixture helpers

    private static ColumnizerCallback CallbackOf (params string[] lines)
    {
        return CallbackOf(lines.ToList(), onLineRead: null);
    }

    private static ColumnizerCallback CallbackOf (IList<string> lines, Action<int> onLineRead)
    {
        var source = new Mock<ILogLineSource>();
        _ = source.Setup(s => s.LineCount).Returns(() => lines.Count); // live — grows when a fixture appends
        _ = source.Setup(s => s.GetLineMemory(It.IsAny<int>()))
            .Returns((int lineNum) =>
            {
                onLineRead?.Invoke(lineNum);
                return lineNum >= 0 && lineNum < lines.Count ? LineOf(lines[lineNum]) : null!;
            });

        return new ColumnizerCallback(source.Object);
    }

    private static ILogLineMemory LineOf (string text)
    {
        var mock = new Mock<ILogLineMemory>();
        _ = mock.Setup(l => l.FullLine).Returns(text.AsMemory());
        return mock.Object;
    }

    private static FilterParams ParamsOf (string searchText, int spreadBefore = 0, int spreadBehind = 0)
    {
        var filterParams = new FilterParams
        {
            SearchText = searchText,
            SpreadBefore = spreadBefore,
            SpreadBehind = spreadBehind,
        };
        filterParams.Init();
        return filterParams;
    }

    #endregion
}
