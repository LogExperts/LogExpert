using ColumnizerLib;

using LogExpert.Core.Classes.Search;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Search;

[TestFixture]
public class LogSearcherTests
{
    [Test]
    public void Find_ForwardFromStart_ReturnsFirstHitAfterCurrentLine ()
    {
        var reader = ReaderOf("alpha", "bravo", "target", "delta", "target");
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, CurrentLine = 0 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 2, Wrapped: false)));
    }

    // Truth table for the effective-direction rule the Log Window triplicated:
    // forward when (IsForward || IsFindNext), reversed by Shift+F3. Pins today's semantics,
    // including the quirk that F3 (find next) on a backward search goes forward.
    [TestCase(true, false, false, SearchDirection.Forward, TestName = "ResolveDirection_Forward_IsForward")]
    [TestCase(true, true, false, SearchDirection.Forward, TestName = "ResolveDirection_Forward_IsForwardAndFindNext")]
    [TestCase(false, true, false, SearchDirection.Forward, TestName = "ResolveDirection_Forward_FindNextOverridesBackward")]
    [TestCase(false, false, false, SearchDirection.Backward, TestName = "ResolveDirection_Backward_NeitherForwardNorFindNext")]
    [TestCase(true, false, true, SearchDirection.Backward, TestName = "ResolveDirection_Backward_ShiftF3ReversesForward")]
    [TestCase(true, true, true, SearchDirection.Backward, TestName = "ResolveDirection_Backward_ShiftF3ReversesFindNext")]
    [TestCase(false, true, true, SearchDirection.Backward, TestName = "ResolveDirection_Backward_ShiftF3ReversesBackwardFindNext")]
    [TestCase(false, false, true, SearchDirection.Backward, TestName = "ResolveDirection_Backward_ShiftF3OnBackwardStaysBackward")]
    public void ResolveDirection_Table (bool isForward, bool isFindNext, bool isShiftF3Pressed, SearchDirection expected)
    {
        var searchParams = new SearchParams { IsForward = isForward, IsFindNext = isFindNext, IsShiftF3Pressed = isShiftF3Pressed };

        Assert.That(LogSearcher.ResolveDirection(searchParams), Is.EqualTo(expected));
    }

    [Test]
    public void Find_Backward_ReturnsNearestHitBeforeCurrentLine ()
    {
        var reader = ReaderOf("target", "bravo", "target", "delta", "echo");
        var searchParams = new SearchParams { SearchText = "target", IsForward = false, CurrentLine = 4 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 2, Wrapped: false)));
    }

    [Test]
    public void Find_ForwardPastEndOfFile_WrapsToStartAndFindsEarlierHit ()
    {
        var reader = ReaderOf("alpha", "target", "bravo", "charlie");
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, CurrentLine = 2 };
        var progress = new ProgressRecorder();

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None, progress);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 1, Wrapped: true)));
            Assert.That(progress.Reports.Select(r => r.Wrap), Does.Contain(SearchWrap.ToStart),
                "the wrap event must reach the progress sink so the control can show the notice mid-search");
        });
    }

    [Test]
    public void Find_BackwardPastTopOfFile_WrapsToEndAndFindsLaterHit ()
    {
        var reader = ReaderOf("alpha", "bravo", "charlie", "target");
        var searchParams = new SearchParams { SearchText = "target", IsForward = false, CurrentLine = 1 };
        var progress = new ProgressRecorder();

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None, progress);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 3, Wrapped: true)));
            Assert.That(progress.Reports.Select(r => r.Wrap), Does.Contain(SearchWrap.ToEnd));
        });
    }

    [Test]
    public void Find_WrappedFullCircleWithoutMatch_ReturnsNotFoundWrapped ()
    {
        var reader = ReaderOf("alpha", "bravo", "charlie");
        var searchParams = new SearchParams { SearchText = "missing", IsForward = true, CurrentLine = 1 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.NotFound, -1, Wrapped: true)));
    }

    [Test]
    public void Find_BackwardWrappedFullCircleWithoutMatch_ReturnsNotFoundWrapped ()
    {
        var reader = ReaderOf("alpha", "bravo", "charlie");
        var searchParams = new SearchParams { SearchText = "missing", IsForward = false, CurrentLine = 1 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.NotFound, -1, Wrapped: true)));
    }

    [Test]
    public void Find_StartPositionPastEndOfFile_WrapsImmediatelyAndScansFromTop ()
    {
        var reader = ReaderOf("target", "alpha", "bravo");
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, CurrentLine = 99 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 0, Wrapped: true)));
    }

    [Test]
    public void Find_WrapEvent_IsReportedBeforeThePostWrapHitIsReturned ()
    {
        var reader = ReaderOf("target", "alpha", "bravo");
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, CurrentLine = 1 };
        var scannedAtWrap = -1;
        var progress = new ProgressRecorder();
        progress.OnWrap = () => scannedAtWrap = progress.Reports.Count;

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None, progress);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(SearchOutcome.Found));
            Assert.That(scannedAtWrap, Is.GreaterThanOrEqualTo(0), "wrap must be reported during the run, not inferred afterwards");
        });
    }

    [Test]
    public void Find_CaseInsensitive_MatchesDifferentCasing ()
    {
        var reader = ReaderOf("alpha", "some Target here");
        var searchParams = new SearchParams { SearchText = "TARGET", IsForward = true, CurrentLine = 0, IsCaseSensitive = false };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 1, Wrapped: false)));
    }

    [Test]
    public void Find_CaseSensitive_SkipsWrongCasingAndFindsExactMatch ()
    {
        var reader = ReaderOf("target wrong case: ERROR", "exact: error");
        var searchParams = new SearchParams { SearchText = "error", IsForward = true, CurrentLine = 0, IsCaseSensitive = true };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 1, Wrapped: false)));
    }

    [Test]
    public void Find_FromTop_StartsAtLineZeroRegardlessOfCurrentLine ()
    {
        var reader = ReaderOf("target", "alpha", "bravo", "charlie");
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, IsFromTop = true, CurrentLine = 3 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 0, Wrapped: false)),
            "from-top must scan from line 0 without needing to wrap");
    }

    [Test]
    public void Find_FromTopWithFindNext_ContinuesFromCurrentLineInstead ()
    {
        // Today's rule: F3 (find next) overrides the from-top start so repeat-search advances.
        var reader = ReaderOf("target", "alpha", "target", "charlie");
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, IsFromTop = true, IsFindNext = true, CurrentLine = 1 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 2, Wrapped: false)));
    }

    [Test]
    public void Find_HitOnLastLine_IsFound ()
    {
        var reader = ReaderOf("alpha", "bravo", "target");
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, CurrentLine = 0 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 2, Wrapped: false)));
    }

    [Test]
    public void Find_HitOnLineZeroBackward_IsFound ()
    {
        var reader = ReaderOf("target", "alpha", "bravo");
        var searchParams = new SearchParams { SearchText = "target", IsForward = false, CurrentLine = 2 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 0, Wrapped: false)));
    }

    [Test]
    public void Find_Regex_MatchesByPattern ()
    {
        var reader = ReaderOf("alpha", "code err42 raised", "bravo");
        var searchParams = new SearchParams { SearchText = @"err\d+", IsRegex = true, IsForward = true, CurrentLine = 0 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 1, Wrapped: false)));
    }

    [Test]
    public void Find_CaseSensitiveRegex_SkipsWrongCasing ()
    {
        var reader = ReaderOf("ERR1", "err2");
        var searchParams = new SearchParams { SearchText = @"err\d", IsRegex = true, IsCaseSensitive = true, IsForward = true, CurrentLine = 0 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 1, Wrapped: false)));
    }

    [Test]
    public void Find_CaseInsensitiveRegex_MatchesWrongCasing ()
    {
        var reader = ReaderOf("ERR1", "err2");
        var searchParams = new SearchParams { SearchText = @"err\d", IsRegex = true, IsCaseSensitive = false, IsForward = true, CurrentLine = 0 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 0, Wrapped: false)));
    }

    [Test]
    public void Find_InvalidRegex_ReturnsInvalidPatternWithoutReadingAnyLine ()
    {
        var readerMock = ReaderMockOf("alpha", "bravo");
        var searchParams = new SearchParams { SearchText = "(unclosed", IsRegex = true, IsForward = true, CurrentLine = 0 };

        var result = LogSearcher.Find(searchParams, readerMock.Object, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.InvalidPattern, -1, Wrapped: false)));
        readerMock.Verify(r => r.GetLogLineMemory(It.IsAny<int>()), Times.Never,
            "an uncompilable pattern must fail before any line is read");
    }

    [Test]
    public void Find_ShiftF3OnFindNext_ScansBackwardThroughTheSeam ()
    {
        // Forward would find line 3; Shift+F3 must reverse the find-next and reach line 0.
        var reader = ReaderOf("target", "alpha", "bravo", "target");
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, IsFindNext = true, IsShiftF3Pressed = true, CurrentLine = 2 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 0, Wrapped: false)));
    }

    [Test]
    public void Find_CancelledTokenWithMatchOnCurrentLine_StillReturnsTheHit ()
    {
        // Pins the original loop's ordering: the match test runs before the cancel check.
        var reader = ReaderOf("target", "alpha");
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, CurrentLine = 0 };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = LogSearcher.Find(searchParams, reader, cts.Token);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 0, Wrapped: false)));
    }

    [Test]
    public void Find_CancelledToken_ReturnsCancelled ()
    {
        var reader = ReaderOf("alpha", "bravo", "target");
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, CurrentLine = 0 };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = LogSearcher.Find(searchParams, reader, cts.Token);

        Assert.That(result.Outcome, Is.EqualTo(SearchOutcome.Cancelled));
    }

    [TestCase(null, TestName = "Find_NullSearchText_ReturnsNotFound")]
    [TestCase("", TestName = "Find_EmptySearchText_ReturnsNotFound")]
    public void Find_MissingSearchText_ReturnsNotFound (string? searchText)
    {
        var reader = ReaderOf("alpha", "bravo");
        var searchParams = new SearchParams { SearchText = searchText!, IsForward = true, CurrentLine = 0 };

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.NotFound, -1, Wrapped: false)));
    }

    [Test]
    public void Find_ReaderReturnsNullLine_EndsAsNotFound ()
    {
        // The reader contract: null for unavailable lines (deleted file, timeout). Never throw.
        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.LineCount).Returns(5);
        _ = readerMock.Setup(r => r.GetLogLineMemory(It.IsAny<int>())).Returns((ILogLineMemory)null!);
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, CurrentLine = 0 };

        var result = LogSearcher.Find(searchParams, readerMock.Object, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(SearchOutcome.NotFound));
    }

    [Test]
    public void Find_MutatingSearchParamsMidRun_DoesNotAffectTheRunningSearch ()
    {
        // The shared coordinator SearchParams is mutated by F3 while a search runs; Find
        // must operate on a snapshot taken at entry.
        var searchParams = new SearchParams { SearchText = "target", IsForward = true, CurrentLine = 0 };

        var mock = new Mock<ILogfileReader>();
        _ = mock.Setup(r => r.LineCount).Returns(3);
        _ = mock.Setup(r => r.GetLogLineMemory(It.IsAny<int>()))
            .Returns((int lineNum) =>
            {
                searchParams.SearchText = "changed-mid-run";
                searchParams.IsForward = false;
                return LineOf(lineNum == 2 ? "target" : "alpha");
            });

        var result = LogSearcher.Find(searchParams, mock.Object, CancellationToken.None);

        Assert.That(result, Is.EqualTo(new SearchResult(SearchOutcome.Found, 2, Wrapped: false)));
    }

    [Test]
    public void Find_LongScan_ReportsProgressEveryThousandLinesAndResetsOnWrap ()
    {
        var lines = Enumerable.Repeat("filler", 2500).ToArray();
        var reader = ReaderOf(lines);
        var searchParams = new SearchParams { SearchText = "missing", IsForward = true, CurrentLine = 0 };
        var progress = new ProgressRecorder();

        var result = LogSearcher.Find(searchParams, reader, CancellationToken.None, progress);

        var scanReports = progress.Reports.Where(r => r.Wrap == SearchWrap.None).Select(r => r.LinesScanned);
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(SearchOutcome.NotFound));
            // Segment counter resets on wrap, matching a progress bar whose maximum is the line count.
            Assert.That(scanReports, Is.EqualTo([1000, 2000, 1000, 2000]));
        });
    }

    #region Fake reader over known lines

    private static ILogfileReader ReaderOf (params string[] lines) => ReaderMockOf(lines).Object;

    private static Mock<ILogfileReader> ReaderMockOf (params string[] lines)
    {
        var mock = new Mock<ILogfileReader>();
        _ = mock.Setup(r => r.LineCount).Returns(lines.Length);
        _ = mock.Setup(r => r.GetLogLineMemory(It.IsAny<int>()))
            .Returns((int lineNum) => lineNum >= 0 && lineNum < lines.Length ? LineOf(lines[lineNum]) : null!);
        return mock;
    }

    private static ILogLineMemory LineOf (string text)
    {
        var mock = new Mock<ILogLineMemory>();
        _ = mock.Setup(l => l.FullLine).Returns(text.AsMemory());
        return mock.Object;
    }

    /// <summary>Synchronous recorder — System.Progress posts asynchronously, which tests must not rely on.</summary>
    private sealed class ProgressRecorder : IProgress<SearchProgress>
    {
        public List<SearchProgress> Reports { get; } = [];

        public Action? OnWrap { get; set; }

        public void Report (SearchProgress value)
        {
            Reports.Add(value);
            if (value.Wrap != SearchWrap.None)
            {
                OnWrap?.Invoke();
            }
        }
    }

    #endregion
}
