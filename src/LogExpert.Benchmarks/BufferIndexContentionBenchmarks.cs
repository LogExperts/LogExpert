using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

using ColumnizerLib;

using LogExpert.Benchmarks.Support;
using LogExpert.Core.Classes.Log.Buffers;

namespace LogExpert.Benchmarks;

/// <summary>
/// Measures ReaderWriterLockSlim contention under concurrent read load.
/// Compares single-threaded throughput against N concurrent readers
/// to determine if RWLS is a bottleneck worth optimizing.
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]  // Reports lock contention + thread pool stats
[RankColumn]
public class BufferIndexContentionBenchmarks : IDisposable
{
    private BufferIndex _index = null!;
    private int _totalLines;
    private bool _disposed;

    private const int BUFFERS = 10_000;
    private const int LINES_PER_BUFFER = 500;
    private const int READS_PER_TASK = 1_000;

    [GlobalSetup]
    public void Setup ()
    {
        _index = new BufferIndex(BUFFERS, LINES_PER_BUFFER);
        _totalLines = BUFFERS * LINES_PER_BUFFER;

        var fakeFileInfo = new FakeLogFileInfo();
        using var writeLock = _index.AcquireWriteLock();
        for (int i = 0; i < BUFFERS; i++)
        {
            var buffer = new LogBuffer(fakeFileInfo, LINES_PER_BUFFER)
            {
                StartLine = i * LINES_PER_BUFFER
            };
            for (int j = 0; j < LINES_PER_BUFFER; j++)
            {
                buffer.AddLine(
                    new LogLine($"line {i * LINES_PER_BUFFER + j}".AsMemory(),
                    i * LINES_PER_BUFFER + j), 0);
            }
            _index.Add(buffer);
        }
    }

    /// <summary>
    /// Single-threaded baseline: sequential reads under one read lock.
    /// This is the ideal throughput ceiling.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int SingleThreadedReads ()
    {
        int found = 0;
        using var readLock = _index.AcquireReadLock();
        var start = Math.Max(0, _totalLines - READS_PER_TASK);
        for (int i = start; i < _totalLines; i++)
        {
            if (_index.TryFindBuffer(i).Found)
            {
                found++;
            }
        }

        return found;
    }

    /// <summary>
    /// N concurrent readers each acquiring their own read lock.
    /// If RWLS has no contention, throughput ≈ N × single-threaded.
    /// </summary>
    [Benchmark]
    [Arguments(2)]
    [Arguments(4)]
    [Arguments(8)]
    [Arguments(12)]
    public int ConcurrentReads (int threadCount)
    {
        var total = 0;
        _ = Parallel.For(0, threadCount, _ =>
        {
            int found = 0;
            using var readLock = _index.AcquireReadLock();
            var start = Math.Max(0, _totalLines - READS_PER_TASK);
            for (int i = start; i < _totalLines; i++)
            {
                if (_index.TryFindBuffer(i).Found)
                {
                    found++;
                }
            }
            _ = Interlocked.Add(ref total, found);
        });
        return total;
    }

    /// <summary>
    /// Simulates production: N readers + 1 writer (tail-follow append).
    /// Writer acquires write lock briefly every ~1000 reads.
    /// This is the realistic contention scenario.
    /// </summary>
    [Benchmark]
    [Arguments(4)]
    [Arguments(8)]
    public int ConcurrentReadsWithWriter (int readerCount)
    {
        using var cts = new CancellationTokenSource();
        var total = 0;

        // Writer task: periodically takes write lock (simulates new buffer append)
        var writerTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                using var writeLock = _index.AcquireWriteLock();
                // Simulate brief write work (no actual mutation to keep state clean)
                Thread.SpinWait(100);
            }
        });

        // Reader tasks
        _ = Parallel.For(0, readerCount, _ =>
        {
            int found = 0;
            using var readLock = _index.AcquireReadLock();
            var start = Math.Max(0, _totalLines - READS_PER_TASK);
            for (int i = start; i < _totalLines; i++)
            {
                if (_index.TryFindBuffer(i).Found)
                {
                    found++;
                }
            }

            _ = Interlocked.Add(ref total, found);
        });

        cts.Cancel();
        writerTask.Wait();
        return total;
    }

    [GlobalCleanup]
    public void Cleanup () => _index.Dispose();

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