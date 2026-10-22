using System.Globalization;
using System.Linq;
using System.Threading;

using ColumnizerLib;

using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Timestamp;
using LogExpert.Core.Interfaces;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Timestamp;

/// <summary>
/// Tests for the Timestamp Locator seam. Every test runs against a fake
/// <see cref="ITimestampSource"/> over an in-memory line list — no WinForms type is instantiated,
/// which is the whole point of the extraction.
/// </summary>
[TestFixture]
public class TimestampLocatorTests
{
    /// <summary>A line with no parsable timestamp. The Columnizer contract for that is MinValue.</summary>
    private const string NoTime = "";

    [Test]
    public void FindBackward_LineHasATimestamp_ReturnsItAndTheSameLine ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00", "2026-01-01 10:00:01", "2026-01-01 10:00:02");

        var (timestamp, lineNumber) = locator.FindBackward(1, 3, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(At("2026-01-01 10:00:01")));
            Assert.That(lineNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void FindBackward_LineHasNoTimestamp_ScansBackToTheNearestLineThatDoes ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00", NoTime, NoTime);

        var (timestamp, lineNumber) = locator.FindBackward(2, 3, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(At("2026-01-01 10:00:00")));
            Assert.That(lineNumber, Is.EqualTo(0));
        });
    }

    [Test]
    public void FindBackward_NoLineInRangeHasATimestamp_ReturnsMinValue ()
    {
        var locator = LocatorOver(NoTime, NoTime, NoTime);

        var (timestamp, _) = locator.FindBackward(2, 3, roundToSeconds: false);

        Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
    }

    [Test]
    public void FindBackward_LineNumberAboveLineCount_ReturnsMinValueWithoutScanning ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00");

        var (timestamp, lineNumber) = locator.FindBackward(5, 1, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
            Assert.That(lineNumber, Is.EqualTo(5), "line number is returned unchanged when scanning never starts");
        });
    }

    [Test]
    public void FindBackward_NegativeLineNumber_ReturnsMinValueWithoutScanning ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00");

        var (timestamp, lineNumber) = locator.FindBackward(-1, 1, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
            Assert.That(lineNumber, Is.EqualTo(-1));
        });
    }

    [Test]
    public void FindBackward_LineCountIsZero_ReturnsMinValueWithoutScanning ()
    {
        var locator = LocatorOver();

        var (timestamp, lineNumber) = locator.FindBackward(0, 0, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
            Assert.That(lineNumber, Is.EqualTo(0));
        });
    }

    [Test]
    public void FindBackward_ReaderReturnsNullLine_ReturnsMinValueAndStops ()
    {
        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.LineCount).Returns(3);
        _ = readerMock.Setup(r => r.GetLogLineMemory(It.IsAny<int>())).Returns((ILogLineMemory)null!);

        var columnizerMock = new Mock<ILogLineMemoryColumnizer>();
        _ = columnizerMock.Setup(c => c.IsTimeshiftImplemented()).Returns(true);

        var sourceMock = new Mock<ITimestampSource>();
        _ = sourceMock.Setup(s => s.Reader).Returns(readerMock.Object);
        _ = sourceMock.Setup(s => s.Columnizer).Returns(columnizerMock.Object);
        _ = sourceMock.Setup(s => s.Callback).Returns(new RecordingCallback());
        _ = sourceMock.Setup(s => s.ColumnizerLock).Returns(new Lock());

        var locator = new TimestampLocator(sourceMock.Object);

        var (timestamp, lineNumber) = locator.FindBackward(2, 3, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
            Assert.That(lineNumber, Is.EqualTo(2));
        });
    }

    [Test]
    public void FindBackward_ColumnizerDoesNotImplementTimeshift_ReturnsMinValueWithoutTouchingTheReader ()
    {
        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.LineCount).Returns(3);

        var columnizerMock = new Mock<ILogLineMemoryColumnizer>();
        _ = columnizerMock.Setup(c => c.IsTimeshiftImplemented()).Returns(false);

        var sourceMock = new Mock<ITimestampSource>();
        _ = sourceMock.Setup(s => s.Reader).Returns(readerMock.Object);
        _ = sourceMock.Setup(s => s.Columnizer).Returns(columnizerMock.Object);
        _ = sourceMock.Setup(s => s.Callback).Returns(new RecordingCallback());
        _ = sourceMock.Setup(s => s.ColumnizerLock).Returns(new Lock());

        var locator = new TimestampLocator(sourceMock.Object);

        var (timestamp, lineNumber) = locator.FindBackward(1, 3, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
            Assert.That(lineNumber, Is.EqualTo(1));
        });
        readerMock.Verify(r => r.GetLogLineMemory(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public void FindBackward_RoundToSeconds_ZeroesTheMillisecondComponent ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00.750");

        var (timestamp, _) = locator.FindBackward(0, 1, roundToSeconds: true);

        Assert.That(timestamp, Is.EqualTo(At("2026-01-01 10:00:00.000")));
    }

    [Test]
    public void FindBackward_NotRoundToSeconds_PreservesTheMillisecondComponent ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00.750");

        var (timestamp, _) = locator.FindBackward(0, 1, roundToSeconds: false);

        Assert.That(timestamp, Is.EqualTo(At("2026-01-01 10:00:00.750")));
    }

    [Test]
    public void FindBackward_CancelledToken_StopsScanningWithoutTouchingTheReader ()
    {
        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.LineCount).Returns(2);

        var columnizerMock = new Mock<ILogLineMemoryColumnizer>();
        _ = columnizerMock.Setup(c => c.IsTimeshiftImplemented()).Returns(true);

        var sourceMock = new Mock<ITimestampSource>();
        _ = sourceMock.Setup(s => s.Reader).Returns(readerMock.Object);
        _ = sourceMock.Setup(s => s.Columnizer).Returns(columnizerMock.Object);
        _ = sourceMock.Setup(s => s.Callback).Returns(new RecordingCallback());
        _ = sourceMock.Setup(s => s.ColumnizerLock).Returns(new Lock());

        var locator = new TimestampLocator(sourceMock.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var (timestamp, lineNumber) = locator.FindBackward(1, 2, roundToSeconds: false, cts.Token);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
            Assert.That(lineNumber, Is.EqualTo(1));
        });
        readerMock.Verify(r => r.GetLogLineMemory(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public void FindBackward_PositionsTheCallbackOnEachLineItInspects ()
    {
        var callback = new RecordingCallback();
        var sourceMock = new Mock<ITimestampSource>();
        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.LineCount).Returns(3);
        _ = readerMock.Setup(r => r.GetLogLineMemory(It.IsAny<int>()))
            .Returns((int lineNum) => LineOf(lineNum == 0 ? "2026-01-01 10:00:00" : NoTime));

        var columnizerMock = new Mock<ILogLineMemoryColumnizer>();
        _ = columnizerMock.Setup(c => c.IsTimeshiftImplemented()).Returns(true);
        _ = columnizerMock.Setup(c => c.GetTimestamp(It.IsAny<ILogLineMemoryColumnizerCallback>(), It.IsAny<ILogLineMemory>()))
            .Returns((ILogLineMemoryColumnizerCallback _, ILogLineMemory logLine) => ParseOrMinValue(logLine));

        _ = sourceMock.Setup(s => s.Reader).Returns(readerMock.Object);
        _ = sourceMock.Setup(s => s.Columnizer).Returns(columnizerMock.Object);
        _ = sourceMock.Setup(s => s.Callback).Returns(callback);
        _ = sourceMock.Setup(s => s.ColumnizerLock).Returns(new Lock());

        var locator = new TimestampLocator(sourceMock.Object);

        _ = locator.FindBackward(2, 3, roundToSeconds: false);

        Assert.That(callback.PositionedAt, Is.EqualTo([2, 1, 0]));
    }

    [Test]
    public void FindForward_LineHasATimestamp_ReturnsItAndTheSameLine ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00", "2026-01-01 10:00:01", "2026-01-01 10:00:02");

        var (timestamp, lineNumber) = locator.FindForward(1, 3, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(At("2026-01-01 10:00:01")));
            Assert.That(lineNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void FindForward_LineHasNoTimestamp_ScansForwardToTheNearestLineThatDoes ()
    {
        var locator = LocatorOver(NoTime, NoTime, "2026-01-01 10:00:02");

        var (timestamp, lineNumber) = locator.FindForward(0, 3, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(At("2026-01-01 10:00:02")));
            Assert.That(lineNumber, Is.EqualTo(2));
        });
    }

    [Test]
    public void FindForward_NoLineInRangeHasATimestamp_ReturnsMinValue ()
    {
        var locator = LocatorOver(NoTime, NoTime, NoTime);

        var (timestamp, _) = locator.FindForward(0, 3, roundToSeconds: false);

        Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
    }

    [Test]
    public void FindForward_LineNumberAtOrAboveLineCount_ReturnsMinValueWithoutScanning ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00");

        var (timestamp, lineNumber) = locator.FindForward(1, 1, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
            Assert.That(lineNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void FindForward_NegativeLineNumber_ReturnsMinValueWithoutScanning ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00");

        var (timestamp, lineNumber) = locator.FindForward(-1, 1, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
            Assert.That(lineNumber, Is.EqualTo(-1));
        });
    }

    [Test]
    public void FindForward_RoundToSeconds_ZeroesTheMillisecondComponent ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00.750");

        var (timestamp, _) = locator.FindForward(0, 1, roundToSeconds: true);

        Assert.That(timestamp, Is.EqualTo(At("2026-01-01 10:00:00.000")));
    }

    [Test]
    public void FindForward_NotRoundToSeconds_PreservesTheMillisecondComponent ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00.750");

        var (timestamp, _) = locator.FindForward(0, 1, roundToSeconds: false);

        Assert.That(timestamp, Is.EqualTo(At("2026-01-01 10:00:00.750")));
    }

    [Test]
    public void FindForward_ReaderReturnsNullLine_ReturnsMinValueAndStops ()
    {
        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.LineCount).Returns(3);
        _ = readerMock.Setup(r => r.GetLogLineMemory(It.IsAny<int>())).Returns((ILogLineMemory)null!);

        var columnizerMock = new Mock<ILogLineMemoryColumnizer>();
        _ = columnizerMock.Setup(c => c.IsTimeshiftImplemented()).Returns(true);

        var sourceMock = new Mock<ITimestampSource>();
        _ = sourceMock.Setup(s => s.Reader).Returns(readerMock.Object);
        _ = sourceMock.Setup(s => s.Columnizer).Returns(columnizerMock.Object);
        _ = sourceMock.Setup(s => s.Callback).Returns(new RecordingCallback());
        _ = sourceMock.Setup(s => s.ColumnizerLock).Returns(new Lock());

        var locator = new TimestampLocator(sourceMock.Object);

        var (timestamp, lineNumber) = locator.FindForward(0, 3, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
            // Ported quirk, preserved verbatim: unlike FindBackward's early `return` on a null line,
            // the forward scan `break`s, so the post-loop "scanning moved" decrement still fires —
            // one below the starting line, not the starting line itself.
            Assert.That(lineNumber, Is.EqualTo(-1));
        });
    }

    [Test]
    public void FindForward_ColumnizerDoesNotImplementTimeshift_ReturnsMinValueWithoutTouchingTheReader ()
    {
        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.LineCount).Returns(3);

        var columnizerMock = new Mock<ILogLineMemoryColumnizer>();
        _ = columnizerMock.Setup(c => c.IsTimeshiftImplemented()).Returns(false);

        var sourceMock = new Mock<ITimestampSource>();
        _ = sourceMock.Setup(s => s.Reader).Returns(readerMock.Object);
        _ = sourceMock.Setup(s => s.Columnizer).Returns(columnizerMock.Object);
        _ = sourceMock.Setup(s => s.Callback).Returns(new RecordingCallback());
        _ = sourceMock.Setup(s => s.ColumnizerLock).Returns(new Lock());

        var locator = new TimestampLocator(sourceMock.Object);

        var (timestamp, lineNumber) = locator.FindForward(0, 3, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(DateTime.MinValue));
            Assert.That(lineNumber, Is.EqualTo(0));
        });
        readerMock.Verify(r => r.GetLogLineMemory(It.IsAny<int>()), Times.Never);
    }

    /// <summary>
    /// Regression seam for the fix made during the Timestamp Locator extraction: the original
    /// <c>GetTimestampForLineForward</c> never positioned the Columnizer callback before calling
    /// into the Columnizer, unlike its backward counterpart. No in-tree Columnizer reads
    /// <c>callback.LineNum</c> on this path, so it was latent rather than a live bug — but any
    /// third-party Columnizer, or Multi-File Mode's <c>GetFileName()</c> resolution, depends on it.
    /// </summary>
    [Test]
    public void FindForward_PositionsTheCallbackOnEachLineItInspects ()
    {
        var callback = new RecordingCallback();
        var sourceMock = new Mock<ITimestampSource>();
        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.LineCount).Returns(3);
        _ = readerMock.Setup(r => r.GetLogLineMemory(It.IsAny<int>()))
            .Returns((int lineNum) => LineOf(lineNum == 2 ? "2026-01-01 10:00:00" : NoTime));

        var columnizerMock = new Mock<ILogLineMemoryColumnizer>();
        _ = columnizerMock.Setup(c => c.IsTimeshiftImplemented()).Returns(true);
        _ = columnizerMock.Setup(c => c.GetTimestamp(It.IsAny<ILogLineMemoryColumnizerCallback>(), It.IsAny<ILogLineMemory>()))
            .Returns((ILogLineMemoryColumnizerCallback _, ILogLineMemory logLine) => ParseOrMinValue(logLine));

        _ = sourceMock.Setup(s => s.Reader).Returns(readerMock.Object);
        _ = sourceMock.Setup(s => s.Columnizer).Returns(columnizerMock.Object);
        _ = sourceMock.Setup(s => s.Callback).Returns(callback);
        _ = sourceMock.Setup(s => s.ColumnizerLock).Returns(new Lock());

        var locator = new TimestampLocator(sourceMock.Object);

        _ = locator.FindForward(0, 3, roundToSeconds: false);

        Assert.That(callback.PositionedAt, Is.EqualTo([0, 1, 2]));
    }

    [Test]
    public void FindLine_ExactHitAtTheMiddleLine_ReturnsThatLine ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00", "2026-01-01 10:00:01", "2026-01-01 10:00:02", "2026-01-01 10:00:03", "2026-01-01 10:00:04");

        var line = locator.FindLine(At("2026-01-01 10:00:02"), fromLine: 2, lineCount: 5, roundToSeconds: false);

        Assert.That(line, Is.EqualTo(2));
    }

    [Test]
    public void FindLine_ExactHitAtTheFirstLine_ReturnsLineZero ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00", "2026-01-01 10:00:01", "2026-01-01 10:00:02");

        var line = locator.FindLine(At("2026-01-01 10:00:00"), fromLine: 1, lineCount: 3, roundToSeconds: false);

        Assert.That(line, Is.EqualTo(0));
    }

    [Test]
    public void FindLine_ExactHitAtTheLastLine_ReturnsTheLastLine ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00", "2026-01-01 10:00:01", "2026-01-01 10:00:02");

        var line = locator.FindLine(At("2026-01-01 10:00:02"), fromLine: 1, lineCount: 3, roundToSeconds: false);

        Assert.That(line, Is.EqualTo(2));
    }

    /// <summary>
    /// Ported quirk, preserved rather than fixed (per the extraction ticket): the internal binary
    /// search signals a miss by returning the near-miss line number <em>negated</em>. When the
    /// near-miss line is 0, that negation is indistinguishable from an exact hit at line 0 —
    /// <c>-0 == 0</c> for a signed int. Searching before the very first line always converges the
    /// binary search on line 0 (the range can never go below it), so this case takes the
    /// <em>hit</em> branch of <see cref="TimestampLocator.FindLine"/>: it walks back (already at 0),
    /// then steps forward to the first line with a real timestamp — line 1 for this fixture. Not a
    /// bug introduced by the extraction; the original <c>FindTimestampLine</c> does the same walk
    /// starting from the same <c>-lineNum == 0</c> value.
    /// </summary>
    [Test]
    public void FindLine_TimestampBeforeTheFirstLine_IsIndistinguishableFromAHitAtLineZero_StepsForwardToLineOne ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00", "2026-01-01 10:00:01", "2026-01-01 10:00:02");

        var line = locator.FindLine(At("2025-01-01 00:00:00"), fromLine: 1, lineCount: 3, roundToSeconds: false);

        Assert.That(line, Is.EqualTo(1));
    }

    /// <summary>
    /// The scroll-to-nearest contract, regression-pinned after a smoke test caught it broken:
    /// on a miss, <see cref="TimestampLocator.FindLine"/> flips the internal negated near-miss back
    /// to a <em>positive</em> line number — the original <c>FindTimestampLine</c> ended with
    /// <c>return -foundLine</c>. Cross-window time-sync compares timestamps at millisecond
    /// precision, so an exact hit in another window's file is the rare case; if a miss stayed
    /// negative, "Scroll all tabs to current timestamp" and scrollbar time-sync would do nothing
    /// at all (the caller ignores negative lines) instead of scrolling to the nearest line.
    /// </summary>
    [Test]
    public void FindLine_NoExactMatchMidFile_ReturnsTheNearestLineAsAPositiveNumber ()
    {
        var locator = LocatorOver(
            "2026-01-01 10:00:00",
            "2026-01-01 10:00:01",
            "2026-01-01 10:00:02",
            "2026-01-01 10:00:03",
            "2026-01-01 10:00:04");

        var line = locator.FindLine(At("2026-01-01 10:00:02.500"), fromLine: 2, lineCount: 5, roundToSeconds: false);

        // The binary search converges on line 2 (10:00:02) for 10:00:02.500 — nearest, flipped positive.
        Assert.That(line, Is.EqualTo(2));
    }

    [Test]
    public void FindLine_TimestampAfterTheLastLine_ReturnsTheConvergedLineAsAPositiveNumber ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00", "2026-01-01 10:00:01", "2026-01-01 10:00:02");

        var line = locator.FindLine(At("2027-01-01 00:00:00"), fromLine: 1, lineCount: 3, roundToSeconds: false);

        // Ported quirk: the search reports the last midpoint it converged on (line 1), not the
        // truly nearest line (line 2). The original behaved identically; callers only need
        // "somewhere close, and scroll" — not exact nearest.
        Assert.That(line, Is.EqualTo(1));
    }

    [Test]
    public void FindLine_DuplicateTimestampAcrossARunOfLines_ReturnsTheFirstOfTheRun ()
    {
        var locator = LocatorOver(
            "2026-01-01 10:00:00",
            "2026-01-01 10:00:01",
            "2026-01-01 10:00:01",
            "2026-01-01 10:00:01",
            "2026-01-01 10:00:02");

        var line = locator.FindLine(At("2026-01-01 10:00:01"), fromLine: 2, lineCount: 5, roundToSeconds: false);

        Assert.That(line, Is.EqualTo(1));
    }

    [TestCase(0, TestName = "FindLine_RangeOfOneLine_FindsIt")]
    [TestCase(1, TestName = "FindLine_RangeOfTwoLines_FindsTheSecond")]
    public void FindLine_NarrowRange_StillFindsTheExactLine (int targetLine)
    {
        var lines = targetLine == 0 ? new[] { "2026-01-01 10:00:00" } : ["2026-01-01 10:00:00", "2026-01-01 10:00:01"];
        var locator = LocatorOver(lines);

        var line = locator.FindLine(At(lines[targetLine]), fromLine: 0, lineCount: lines.Length, roundToSeconds: false);

        Assert.That(line, Is.EqualTo(targetLine));
    }

    /// <summary>Every line's own timestamp, searched for, must resolve back to the first line
    /// carrying that timestamp — the monotonic-file property the binary search exists to serve.</summary>
    [Test]
    public void FindLine_MonotonicFile_EveryLineResolvesBackToItsOwnTimestamp ()
    {
        var lines = Enumerable.Range(0, 30).Select(i => At("2026-01-01 10:00:00").AddSeconds(i).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)).ToArray();
        var locator = LocatorOver(lines);

        Assert.Multiple(() =>
        {
            for (var i = 0; i < lines.Length; i++)
            {
                var line = locator.FindLine(At(lines[i]), fromLine: lines.Length / 2, lineCount: lines.Length, roundToSeconds: false);
                Assert.That(line, Is.EqualTo(i), $"line {i}");
            }
        });
    }

    /// <summary>
    /// Tests for the raw binary-search step, used directly (not through <see cref="TimestampLocator.FindLine"/>)
    /// by <c>TimeSpreadCalculator</c>, which does its own walk-back / sign handling for performance
    /// reasons. Ported from the original <c>FindTimestampLineInternal</c> — same near-miss-negated
    /// contract, minus the "walk back to the first occurrence" step <see cref="TimestampLocator.FindLine"/> adds.
    /// </summary>
    [Test]
    public void FindNearestLine_ExactHitInRange_ReturnsThatLine ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00", "2026-01-01 10:00:01", "2026-01-01 10:00:02", "2026-01-01 10:00:03", "2026-01-01 10:00:04");

        var line = locator.FindNearestLine(At("2026-01-01 10:00:03"), fromLine: 2, rangeStart: 2, rangeEnd: 4, lineCount: 5, roundToSeconds: false);

        Assert.That(line, Is.EqualTo(3));
    }

    [Test]
    public void FindNearestLine_NoExactMatchInRange_ReturnsANegatedNearMiss ()
    {
        var locator = LocatorOver("2026-01-01 10:00:00", "2026-01-01 10:00:01", "2026-01-01 10:00:02", "2026-01-01 10:00:03", "2026-01-01 10:00:04");

        var line = locator.FindNearestLine(At("2026-01-01 10:00:02.500"), fromLine: 2, rangeStart: 2, rangeEnd: 4, lineCount: 5, roundToSeconds: false);

        Assert.That(line, Is.LessThan(0));
    }

    /// <summary>
    /// Does <b>not</b> walk back to the first line of a duplicate-timestamp run — that is exactly
    /// the behaviour <see cref="TimestampLocator.FindLine"/> adds on top of this primitive.
    /// </summary>
    [Test]
    public void FindNearestLine_DuplicateTimestampRun_DoesNotWalkBackToTheFirstOccurrence ()
    {
        var locator = LocatorOver(
            "2026-01-01 10:00:00",
            "2026-01-01 10:00:01",
            "2026-01-01 10:00:01",
            "2026-01-01 10:00:01",
            "2026-01-01 10:00:02");

        var line = locator.FindNearestLine(At("2026-01-01 10:00:01"), fromLine: 2, rangeStart: 0, rangeEnd: 4, lineCount: 5, roundToSeconds: false);

        Assert.That(line, Is.EqualTo(2), "lands on whichever line the binary search first hits, not necessarily the first of the run");
    }

    #region Integration: real TimestampColumnizer over pipe-separated log lines

    /// <summary>
    /// End-to-end through the real <see cref="TimestampColumnizer"/> (not the stub) with lines
    /// shaped like a real pipe-separated log — the exact format a user reported dead time-sync on
    /// ("yyyy-MM-dd HH:mm:ss.fff | LEVEL | thread | source | message"). Proves the parse + lookup
    /// pipeline: if these pass, a dead sync in the app is a UI gate (timestamp-control preference,
    /// sync-group membership, per-window columnizer choice), not the locator or the columnizer.
    /// </summary>
    [Test]
    public void RealTimestampColumnizer_PipeSeparatedTraceLine_FindBackwardParsesTheTimestamp ()
    {
        var locator = RealColumnizerLocatorOver(
            "2022-03-21 11:34:34.491 | TRACE |   100 | CTI60Controller | <-- SendRadioInterfaceChanged",
            "2022-03-21 11:34:34.505 | DEBUG |    33 | PositionServiceListener | --> RadioInterfaceUpdate: message = [[; UpdateNumber=360]]",
            "2022-03-21 11:34:34.532 | TRACE |    21 | PositionServiceListener | ### ProcessRadioInterfaceUpdates: performing update");

        var (timestamp, lineNumber) = locator.FindBackward(1, 3, roundToSeconds: false);

        Assert.Multiple(() =>
        {
            Assert.That(timestamp, Is.EqualTo(At("2022-03-21 11:34:34.505")));
            Assert.That(lineNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void RealTimestampColumnizer_ExactMillisecondHit_FindLineReturnsTheLine ()
    {
        var locator = RealColumnizerLocatorOver(
            "2022-03-21 11:34:34.491 | TRACE |   100 | CTI60Controller | <-- SendRadioInterfaceChanged",
            "2022-03-21 11:34:34.505 | DEBUG |    33 | PositionServiceListener | --> RadioInterfaceUpdate",
            "2022-03-21 11:34:34.532 | TRACE |    21 | PositionServiceListener | ### ProcessRadioInterfaceUpdates");

        var line = locator.FindLine(At("2022-03-21 11:34:34.505"), fromLine: 0, lineCount: 3, roundToSeconds: false);

        Assert.That(line, Is.EqualTo(1));
    }

    [Test]
    public void RealTimestampColumnizer_MillisecondMiss_FindLineStillReturnsAScrollableLine ()
    {
        // The cross-window sync case: the source window's exact millisecond almost never exists
        // in the target file. The result must be positive (scrollable), never a negated miss.
        var locator = RealColumnizerLocatorOver(
            "2022-03-21 11:34:34.491 | TRACE |   100 | CTI60Controller | <-- SendRadioInterfaceChanged",
            "2022-03-21 11:34:34.532 | TRACE |    21 | PositionServiceListener | ### ProcessRadioInterfaceUpdates",
            "2022-03-21 11:34:35.104 | TRACE |    21 | Mixers | <-- Devices");

        var line = locator.FindLine(At("2022-03-21 11:34:34.505"), fromLine: 0, lineCount: 3, roundToSeconds: false);

        Assert.That(line, Is.InRange(0, 2));
    }

    private static TimestampLocator RealColumnizerLocatorOver (params string[] lines)
    {
        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.LineCount).Returns(lines.Length);
        _ = readerMock.Setup(r => r.GetLogLineMemory(It.IsAny<int>()))
            .Returns((int lineNum) => lineNum >= 0 && lineNum < lines.Length ? LineOf(lines[lineNum]) : null!);

        var sourceMock = new Mock<ITimestampSource>();
        _ = sourceMock.Setup(s => s.Reader).Returns(readerMock.Object);
        _ = sourceMock.Setup(s => s.Columnizer).Returns(new TimestampColumnizer());
        _ = sourceMock.Setup(s => s.Callback).Returns(new RecordingCallback());
        _ = sourceMock.Setup(s => s.ColumnizerLock).Returns(new Lock());

        return new TimestampLocator(sourceMock.Object);
    }

    #endregion

    #region Fake source over known lines

    private static TimestampLocator LocatorOver (params string[] lines) => new(SourceOver(lines));

    /// <summary>
    /// Builds a source whose Columnizer parses each line as an invariant date-time, or returns
    /// MinValue for <see cref="NoTime"/> — the same "no timestamp on this line" signal every real
    /// Columnizer gives.
    /// </summary>
    private static ITimestampSource SourceOver (params string[] lines)
    {
        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.LineCount).Returns(lines.Length);
        _ = readerMock.Setup(r => r.GetLogLineMemory(It.IsAny<int>()))
            .Returns((int lineNum) => lineNum >= 0 && lineNum < lines.Length ? LineOf(lines[lineNum]) : null!);

        var columnizerMock = new Mock<ILogLineMemoryColumnizer>();
        _ = columnizerMock.Setup(c => c.IsTimeshiftImplemented()).Returns(true);
        _ = columnizerMock.Setup(c => c.GetTimestamp(It.IsAny<ILogLineMemoryColumnizerCallback>(), It.IsAny<ILogLineMemory>()))
            .Returns((ILogLineMemoryColumnizerCallback _, ILogLineMemory logLine) => ParseOrMinValue(logLine));

        var sourceMock = new Mock<ITimestampSource>();
        _ = sourceMock.Setup(s => s.Reader).Returns(readerMock.Object);
        _ = sourceMock.Setup(s => s.Columnizer).Returns(columnizerMock.Object);
        _ = sourceMock.Setup(s => s.Callback).Returns(new RecordingCallback());
        _ = sourceMock.Setup(s => s.ColumnizerLock).Returns(new Lock());

        return sourceMock.Object;
    }

    private static DateTime ParseOrMinValue (ILogLineMemory logLine)
    {
        var text = logLine.FullLine.ToString();
        return text.Length == 0
            ? DateTime.MinValue
            : At(text);
    }

    /// <summary>Parses "yyyy-MM-dd HH:mm:ss" with an optional ".fff" fraction.</summary>
    private static DateTime At (string text) => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.None);

    private static ILogLineMemory LineOf (string text)
    {
        var mock = new Mock<ILogLineMemory>();
        _ = mock.Setup(l => l.FullLine).Returns(text.AsMemory());
        return mock.Object;
    }

    /// <summary>
    /// Records every line number the locator positioned it on, so tests can assert the callback is
    /// moved before each Columnizer call.
    /// </summary>
    private sealed class RecordingCallback : IPositionedColumnizerCallback
    {
        public List<int> PositionedAt { get; } = [];

        public int LineNum { get; private set; }

        public void SetLineNum (int lineNum)
        {
            LineNum = lineNum;
            PositionedAt.Add(lineNum);
        }

        public string GetFileName () => "fake.log";

        public int GetLineCount () => 0;

        public ILogLineMemory GetLogLineMemory (int lineNum) => null!;
    }

    #endregion
}
