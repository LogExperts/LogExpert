using ColumnizerLib;

using LogExpert.Core.Callback;
using LogExpert.Core.Classes.Log.Buffers;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogWindow;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Controls;

[TestFixture]
public class ColumnCacheTests
{
    /// <summary>
    /// Regression test for a cache-poisoning bug in <see cref="ColumnCache.GetColumnsForLine"/>:
    /// when the underlying <see cref="ILogfileReader.GetLogLineMemoryWithWait"/> returned null
    /// (e.g. fast-fail timeout) for a given line, the cache stored that null and
    /// permanently returned null for every subsequent request of the same line number.
    /// This manifested in the GUI as a blank data row in a CSV file containing a header
    /// plus exactly one data line (header was dropped by CsvColumnizer's PreProcessLine,
    /// leaving a single grid row that was repeatedly requested for paint).
    /// The fix adds <c>_cachedColumns == null</c> to the re-fetch condition so a null
    /// result is never cached.
    /// </summary>
    [Test]
    public void GetColumnsForLine_NullThenValidLine_ReturnsColumnsOnSecondCall ()
    {
        const int lineNumber = 0;

        var validLine = new Mock<ILogLineMemory>().Object;
        var splitResult = new Mock<IColumnizedLogLineMemory>().Object;

        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock
            .SetupSequence(r => r.GetLogLineMemoryWithWait(lineNumber))
            .Returns(Task.FromResult<ILogLineMemory>(null))
            .Returns(Task.FromResult(validLine));

        var columnizerMock = new Mock<ILogLineMemoryColumnizer>();
        _ = columnizerMock
            .Setup(c => c.SplitLine(It.IsAny<ILogLineMemoryColumnizerCallback>(), validLine))
            .Returns(splitResult);

        var lineSourceMock = new Mock<ILogLineSource>();
        var callback = new ColumnizerCallback(lineSourceMock.Object);

        var cache = new ColumnCache();

        // First call: reader returns null -> cache must NOT store this null.
        var firstResult = cache.GetColumnsForLine(readerMock.Object, lineNumber, columnizerMock.Object, callback);
        Assert.That(firstResult, Is.Null, "First call should return null because the reader returned null.");

        // Second call for the SAME line: reader now returns a valid line.
        // Before the fix this would return the cached null and never call SplitLine.
        var secondResult = cache.GetColumnsForLine(readerMock.Object, lineNumber, columnizerMock.Object, callback);

        Assert.That(secondResult, Is.SameAs(splitResult), "Second call must re-fetch and return the freshly split columns instead of a cached null.");
        readerMock.Verify(r => r.GetLogLineMemoryWithWait(lineNumber), Times.Exactly(2));
        columnizerMock.Verify(c => c.SplitLine(It.IsAny<ILogLineMemoryColumnizerCallback>(), validLine), Times.Once);
    }

    /// <summary>
    /// Sanity check that a valid result IS cached: requesting the same line twice
    /// with a successful first fetch must not call the reader/columnizer a second time.
    /// </summary>
    [Test]
    public void GetColumnsForLine_SameLineTwice_UsesCachedValue ()
    {
        const int lineNumber = 0;

        var validLine = new Mock<ILogLineMemory>().Object;
        var splitResult = new Mock<IColumnizedLogLineMemory>().Object;

        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock
            .Setup(r => r.GetLogLineMemoryWithWait(lineNumber))
            .Returns(Task.FromResult(validLine));

        var columnizerMock = new Mock<ILogLineMemoryColumnizer>();
        _ = columnizerMock
            .Setup(c => c.SplitLine(It.IsAny<ILogLineMemoryColumnizerCallback>(), validLine))
            .Returns(splitResult);

        var lineSourceMock = new Mock<ILogLineSource>();
        var callback = new ColumnizerCallback(lineSourceMock.Object);

        var cache = new ColumnCache();

        var firstResult = cache.GetColumnsForLine(readerMock.Object, lineNumber, columnizerMock.Object, callback);
        // callback.LineNum is now equal to lineNumber (set by GetColumnsForLine), so the
        // second call should hit the cache.
        var secondResult = cache.GetColumnsForLine(readerMock.Object, lineNumber, columnizerMock.Object, callback);

        Assert.That(firstResult, Is.SameAs(splitResult));
        Assert.That(secondResult, Is.SameAs(splitResult));
        readerMock.Verify(r => r.GetLogLineMemoryWithWait(lineNumber), Times.Once);
        columnizerMock.Verify(c => c.SplitLine(It.IsAny<ILogLineMemoryColumnizerCallback>(), validLine), Times.Once);
    }

    /// <summary>
    /// Regression test for #634. The follow-tail path (LogWindow.UpdateGrid) deliberately calls
    /// <see cref="ColumnCache.MarkPrefetchStale"/> instead of <see cref="ColumnCache.InvalidatePrefetch"/>
    /// so the currently-visible buffers stay pinned across the tail refresh. If the pins were released,
    /// the background LRU-eviction thread could return their char blocks to the ArrayPool mid-paint,
    /// producing blank/stale tail rows (the Release-only follow-tail failure). This locks the contract:
    /// MarkPrefetchStale keeps pins; only InvalidatePrefetch releases them.
    /// </summary>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void MarkPrefetchStale_KeepsBuffersPinned_While_InvalidateReleasesThem ()
    {
        var fileInfo = new Mock<ILogFileInfo>();
        _ = fileInfo.Setup(f => f.FullName).Returns("fake.log");
        _ = fileInfo.Setup(f => f.FileName).Returns("fake.log");

        var buffer = new LogBuffer(fileInfo.Object, 10) { StartLine = 0, StartPos = 0 };
        buffer.AddLine(new LogLine("tail".AsMemory(), 0), 0);

        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.GetLogLineMemories(0, 1)).Returns(new ILogLineMemory[1]);
        _ = readerMock.As<IBufferPinning>()
            .Setup(p => p.PinRange(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(() =>
            {
                buffer.Pin();
                return new PinHandle([buffer]);
            });

        var cache = new ColumnCache();

        cache.Prefetch(readerMock.Object, 0, 1);
        Assert.That(buffer.IsPinned, Is.True, "Prefetch must pin the visible range.");

        cache.MarkPrefetchStale();
        Assert.That(buffer.IsPinned, Is.True, "MarkPrefetchStale must NOT release pins — follow-tail relies on this (#634).");

        cache.InvalidatePrefetch();
        Assert.That(buffer.IsPinned, Is.False, "InvalidatePrefetch must release the pins.");
    }

    /// <summary>
    /// Hide-line rules (#338) make visible rows non-contiguous: only the runs of displayed lines are pinned and read,
    /// never the gap between them, and replacing the prefetch releases the previous pins.
    /// </summary>
    [Test]
    public void PrefetchLines_PinsAndReadsOnlyTheRunsOfDisplayedLines ()
    {
        var fileInfo = new Mock<ILogFileInfo>();
        _ = fileInfo.Setup(f => f.FullName).Returns("fake.log");
        var buffers = new List<LogBuffer>();
        var readerMock = new Mock<ILogfileReader>();
        _ = readerMock.Setup(r => r.GetLogLineMemories(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int start, int count) => [.. Enumerable.Range(start, count).Select(i => (ILogLineMemory)new LogLine($"line {i}", i))]);
        _ = readerMock.As<IBufferPinning>()
            .Setup(p => p.PinRange(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int start, int _) =>
            {
                var buffer = new LogBuffer(fileInfo.Object, 10) { StartLine = start };
                buffer.Pin();
                buffers.Add(buffer);
                return new PinHandle([buffer]);
            });
        var cache = new ColumnCache();

        cache.PrefetchLines(readerMock.Object, [2, 3, 1000, 1001, 1002]);

        readerMock.As<IBufferPinning>().Verify(p => p.PinRange(2, 3), Times.Once);
        readerMock.As<IBufferPinning>().Verify(p => p.PinRange(1000, 1002), Times.Once);
        readerMock.Verify(r => r.GetLogLineMemories(2, 2), Times.Once);
        readerMock.Verify(r => r.GetLogLineMemories(1000, 3), Times.Once);
        Assert.That(cache.GetPrefetchedLine(1001)?.FullLine.ToString(), Is.EqualTo("line 1001"));
        Assert.That(cache.GetPrefetchedLine(500), Is.Null);

        cache.Prefetch(readerMock.Object, 0, 1);
        Assert.That(buffers.Take(2).All(b => !b.IsPinned), Is.True, "the sparse pins must be released once replaced");
        Assert.That(cache.GetPrefetchedLine(1001), Is.Null);
    }
}
