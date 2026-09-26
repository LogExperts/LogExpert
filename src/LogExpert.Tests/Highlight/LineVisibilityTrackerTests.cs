using ColumnizerLib;

using LogExpert.Core.Classes.Highlight;

using NUnit.Framework;

namespace LogExpert.Tests.Highlight;

[TestFixture]
public class LineVisibilityTrackerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static readonly HighlightEntry[] HideDebug = [new HighlightEntry { SearchText = "DEBUG", IsHideLine = true }];

    private List<string> _lines = null!;
    private ManualResetEventSlim _gate = null!;
    private LineVisibilityTracker _tracker = null!;
    private int _changedCount;

    [SetUp]
    public void SetUp ()
    {
        _lines = ["INFO a", "DEBUG b", "INFO c", "DEBUG d", "INFO e"];
        _gate = new ManualResetEventSlim(true);
        _changedCount = 0;
        _tracker = new LineVisibilityTracker(GetLine);
        _tracker.Changed += (_, _) => Interlocked.Increment(ref _changedCount);
    }

    [TearDown]
    public void TearDown ()
    {
        _gate.Set();
        _tracker.Dispose();
        _gate.Dispose();
    }

    private ITextValueMemory? GetLine (int lineNum)
    {
        _ = _gate.Wait(Timeout);
        lock (_lines)
        {
            return lineNum < _lines.Count ? new LogLine(_lines[lineNum], lineNum) : null;
        }
    }

    private void Idle ()
    {
        Assert.That(_tracker.WhenIdle().Wait(Timeout), Is.True, "scan did not finish");
    }

    private static int[] VisibleLines (LineVisibilityMap map)
    {
        return [.. Enumerable.Range(0, map.VisibleCount).Select(map.RowToLine)];
    }

    [Test]
    public void Load_WithHideRule_PublishesMapHidingMatchingLines ()
    {
        _tracker.Load(_lines.Count, HideDebug);
        Idle();

        Assert.That(VisibleLines(_tracker.Map), Is.EqualTo(new[] { 0, 2, 4 }));
        Assert.That(_tracker.Map.HiddenCount, Is.EqualTo(2));
        Assert.That(_changedCount, Is.EqualTo(1));
    }

    [Test]
    public void Load_UntilScanCompletes_EveryLineStaysVisible ()
    {
        _gate.Reset();

        _tracker.Load(_lines.Count, HideDebug);

        Assert.That(_tracker.Map.VisibleCount, Is.EqualTo(5));
        Assert.That(_tracker.IsScanning, Is.True);
        _gate.Set();
        Idle();
        Assert.That(_tracker.Map.VisibleCount, Is.EqualTo(3));
    }

    [Test]
    public void Load_WithoutHideRules_IsImmediatelyComplete ()
    {
        _tracker.Load(_lines.Count, [new HighlightEntry { SearchText = "DEBUG" }]);

        Assert.That(_tracker.IsScanning, Is.False);
        Assert.That(_tracker.Map.VisibleCount, Is.EqualTo(5));
    }

    [Test]
    public void Extend_EvaluatesAppendedLines_AsAnAppendOfTheCurrentMap ()
    {
        _tracker.Load(_lines.Count, HideDebug);
        Idle();
        var before = _tracker.Map;
        _lines.AddRange(["DEBUG f", "INFO g"]);

        var after = _tracker.Extend(7);

        Assert.That(VisibleLines(after), Is.EqualTo(new[] { 0, 2, 4, 6 }));
        Assert.That(after.IsAppendOf(before), Is.True);
        Assert.That(_tracker.Map, Is.SameAs(after));
    }

    [Test]
    public void Extend_ToAKnownLineCount_ChangesNothing ()
    {
        _tracker.Load(_lines.Count, HideDebug);
        Idle();
        var before = _tracker.Map;

        Assert.That(_tracker.Extend(3), Is.SameAs(before));
    }

    [Test]
    public void Extend_WhileScanPending_LinesAreCoveredByThePublishedMap ()
    {
        _gate.Reset();
        _tracker.Load(_lines.Count, HideDebug);
        _lines.Add("DEBUG f");

        var interim = Task.Run(() => _tracker.Extend(6));
        _gate.Set();
        Assert.That(interim.Wait(Timeout), Is.True);
        Idle();

        Assert.That(_tracker.Map.LineCount, Is.EqualTo(6));
        Assert.That(VisibleLines(_tracker.Map), Is.EqualTo(new[] { 0, 2, 4 }));
    }

    [Test]
    public void Rebuild_WhileScanPending_OnlyTheLatestRulesArePublished ()
    {
        _gate.Reset();
        _tracker.Load(_lines.Count, HideDebug);

        _tracker.Rebuild([new HighlightEntry { SearchText = "INFO", IsHideLine = true }]);
        _gate.Set();
        Idle();

        Assert.That(VisibleLines(_tracker.Map), Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void Rebuild_RemovingAllHideRules_ShowsEveryLine ()
    {
        _tracker.Load(_lines.Count, HideDebug);
        Idle();

        _tracker.Rebuild([]);
        Idle();

        Assert.That(_tracker.Map.HiddenCount, Is.Zero);
        Assert.That(_tracker.Map.VisibleCount, Is.EqualTo(5));
    }

    [Test]
    public void Rebuild_WithUnchangedHideRules_DoesNotRescan ()
    {
        _tracker.Load(_lines.Count, HideDebug);
        Idle();
        var before = _tracker.Map;

        _tracker.Rebuild([new HighlightEntry { SearchText = "DEBUG", IsHideLine = true, BackgroundColor = Color.Red }]);

        Assert.That(_tracker.IsScanning, Is.False);
        Assert.That(_tracker.Map, Is.SameAs(before));
        Assert.That(_changedCount, Is.EqualTo(1));
    }

    [Test]
    public void Rebuild_SnapshotsRules_LaterEditsDoNotLeakIntoTheScan ()
    {
        var rule = new HighlightEntry { SearchText = "DEBUG", IsHideLine = true };
        _gate.Reset();
        _tracker.Load(_lines.Count, [rule]);

        rule.SearchText = "INFO";
        _gate.Set();
        Idle();

        Assert.That(VisibleLines(_tracker.Map), Is.EqualTo(new[] { 0, 2, 4 }));
    }

    [Test]
    public void Shift_RenumbersTheCurrentMap ()
    {
        _tracker.Load(_lines.Count, HideDebug);
        Idle();

        var shifted = _tracker.Shift(2);

        Assert.That(shifted.LineCount, Is.EqualTo(3));
        Assert.That(VisibleLines(shifted), Is.EqualTo(new[] { 0, 2 }));
    }

    [Test]
    public void Shift_WhileScanPending_RestartsTheScanOnTheNewNumbering ()
    {
        _gate.Reset();
        _tracker.Load(_lines.Count, HideDebug);
        lock (_lines)
        {
            _lines.RemoveRange(0, 2);
        }

        _ = _tracker.Shift(2);
        _gate.Set();
        Idle();

        Assert.That(_tracker.Map.LineCount, Is.EqualTo(3));
        Assert.That(VisibleLines(_tracker.Map), Is.EqualTo(new[] { 0, 2 }));
    }

    [Test]
    public void Replace_ReevaluatesEveryLineOfTheNewContent ()
    {
        _tracker.Load(_lines.Count, HideDebug);
        Idle();
        lock (_lines)
        {
            _lines.Clear();
            _lines.AddRange(["DEBUG x", "INFO y"]);
        }

        var replaced = _tracker.Replace(2);

        Assert.That(VisibleLines(replaced), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void UnreadableLine_StaysVisible ()
    {
        _tracker.Load(7, HideDebug);
        Idle();

        Assert.That(_tracker.Map.LineCount, Is.EqualTo(7));
        Assert.That(VisibleLines(_tracker.Map), Is.EqualTo(new[] { 0, 2, 4, 5, 6 }));
    }

    [Test]
    public void ScanFailure_PublishesAllVisible_AndReportsTheError ()
    {
        LineVisibilityChangedEventArgs? reported = null;
        using var tracker = new LineVisibilityTracker(i => i == 3 ? throw new InvalidOperationException("boom") : new LogLine("DEBUG", i));
        tracker.Changed += (_, e) => reported = e;

        tracker.Load(5, HideDebug);
        Assert.That(tracker.WhenIdle().Wait(Timeout), Is.True);

        Assert.That(tracker.Map.HiddenCount, Is.Zero);
        Assert.That(tracker.Map.VisibleCount, Is.EqualTo(5));
        Assert.That(reported?.Error, Is.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void TailFailure_FallsBackToAllVisible ()
    {
        var fail = false;
        using var tracker = new LineVisibilityTracker(i => fail ? throw new InvalidOperationException("boom") : new LogLine(i % 2 == 0 ? "DEBUG" : "INFO", i));
        tracker.Load(4, HideDebug);
        Assert.That(tracker.WhenIdle().Wait(Timeout), Is.True);
        fail = true;

        var map = tracker.Extend(6);

        Assert.That(map.LineCount, Is.EqualTo(6));
        Assert.That(map.HiddenCount, Is.Zero);
    }

    [Test]
    public void Dispose_CancelsAPendingScan_WithoutPublishing ()
    {
        _gate.Reset();
        _tracker.Load(_lines.Count, HideDebug);

        _tracker.Dispose();
        _gate.Set();

        Assert.That(_tracker.WhenIdle().Wait(Timeout), Is.True);
        Assert.That(_changedCount, Is.Zero);
    }
}
