using BenchmarkDotNet.Attributes;

using ColumnizerLib;

using LogExpert.Benchmarks.Support;
using LogExpert.Core.Classes.Log.Buffers;

namespace LogExpert.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class BufferIndexBenchmarks : IDisposable
{
    private BufferIndex _index = null!;
    private int _totalLines;

    private bool _disposed;

    [Params(100, 1_000, 10_000)]
    public int BufferCount { get; set; }

    private const int LINES_PER_BUFFER = 500;

    [GlobalSetup]
    public void Setup ()
    {
        _index = new BufferIndex(BufferCount, LINES_PER_BUFFER);
        _totalLines = BufferCount * LINES_PER_BUFFER;

        var fakeFileInfo = new FakeLogFileInfo();

        using (var writeLock = _index.AcquireWriteLock())
        {
            for (int i = 0; i < BufferCount; i++)
            {
                var buffer = new LogBuffer(fakeFileInfo, LINES_PER_BUFFER)
                {
                    StartLine = i * LINES_PER_BUFFER
                };

                for (int j = 0; j < LINES_PER_BUFFER; j++)
                {
                    buffer.AddLine(new LogLine($"line {i * LINES_PER_BUFFER + j}".AsMemory(), i * LINES_PER_BUFFER + j), 0);
                }

                _index.Add(buffer);
            }
        }

        // Validate setup
        var snapshot = _index.CreateSnapshot();
        if (snapshot.BufferCount != BufferCount)
        {
            throw new InvalidOperationException($"Setup failed: expected {BufferCount} buffers, got {snapshot.BufferCount}");
        }
    }

    [GlobalCleanup]
    public void Cleanup () => _index.Dispose();

    /// <summary>
    /// Simulates tail-follow: reading the last 1000 lines sequentially.
    /// Should hit Layer 0 (thread-local cache) ~99% of the time.
    /// </summary>
    [Benchmark(Baseline = true)]
    public LogBuffer? SequentialAccess ()
    {
        using var readlock = _index.AcquireReadLock();
        LogBuffer? last = null;
        var start = Math.Max(0, _totalLines - 1000);
        for (int i = start; i < _totalLines; i++)
        {
            var logBufferEntry = _index.TryFindBuffer(i);
            if (logBufferEntry.Found)
            {
                last = logBufferEntry.Buffer;
            }
        }

        return last;
    }

    /// <summary>
    /// Simulates search/goto: deterministic stride across the full file.
    /// Co-prime stride visits buffers in non-sequential, non-repeating order.
    /// Exercises Layers 2 and 3 heavily.
    /// </summary>
    [Benchmark]
    public LogBuffer? StrideAccess ()
    {
        using var readLock = _index.AcquireReadLock();
        LogBuffer? last = null;
        var stride = _totalLines / 3 + 1;
        var lineNum = 0;
        for (int i = 0; i < 1000; i++)
        {
            var logBufferEntry = _index.TryFindBuffer(lineNum);
            if (logBufferEntry.Found)
            {
                last = logBufferEntry.Buffer;
            }

            lineNum = (lineNum + stride) % _totalLines;
        }

        return last;
    }

    /// <summary>
    /// Worst case for Layer 0: always crossing buffer boundaries.
    /// Exercises Layer 1 (adjacent prediction).
    /// </summary>
    [Benchmark]
    public LogBuffer? BoundaryAccess ()
    {
        using var readLock = _index.AcquireReadLock();
        LogBuffer? last = null;

        for (int i = 0; i < 1000; i++)
        {
            int lineNum = i * (_totalLines / 1000);
            var logBufferEntry = _index.TryFindBuffer(lineNum);
            if (logBufferEntry.Found)
            {
                last = logBufferEntry.Buffer;
            }
        }

        return last;
    }

    /// <summary>
    /// Simulates UI scrolling: page-sized jumps forward through the file.
    /// 50-line pages with 3x page jumps (fast scroll drag).
    /// Exercises Layer 0 within pages and Layers 1-2 on transitions.
    /// </summary>
    [Benchmark]
    public LogBuffer? ScrollAccess ()
    {
        using var readLock = _index.AcquireReadLock();
        LogBuffer? last = null;
        const int pageSize = 50;
        const int pageJump = pageSize * 3;
        var pageStart = 0;

        for (int page = 0; page < 20 && pageStart < _totalLines; page++)
        {
            var pageEnd = Math.Min(pageStart + pageSize, _totalLines);
            for (int line = pageStart; line < pageEnd; line++)
            {
                var logBufferEntry = _index.TryFindBuffer(line);
                if (logBufferEntry.Found)
                {
                    last = logBufferEntry.Buffer;
                }
            }

            pageStart += pageJump;
        }

        return last;
    }

    /// <summary>
    /// Measures LRU eviction cost at current scale.
    /// </summary>
    [Benchmark]
    public void EvictAndRepopulate ()
    {
        _index.EvictLeastRecentlyUsed();
    }

    public void Dispose ()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _index?.Dispose();
            }

            _disposed = true;
        }
    }
}