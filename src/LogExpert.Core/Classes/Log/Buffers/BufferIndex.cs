using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

using NLog;

namespace LogExpert.Core.Classes.Log.Buffers;

/*
 * !IMPORTANT
 * Before and after changes are made run the BufferIndexBenchmarks for a baseline, so no performance regression is introduced
 * If changes are made to this class, please also review BufferIndexSnapshot and BufferShiftTest to ensure consistency and correctness.
 */

/// <summary>
/// Thread-safe index that maps line numbers to <see cref="LogBuffer"/> instances with LRU eviction. This is the hot
/// path — every GetLogLine call goes through here. Has zero file-I/O dependencies. Constructable with only integers for
/// benchmarking.
/// </summary>
public sealed class BufferIndex : IDisposable
{
    private readonly int _maxBuffers;
    private readonly int _maxLinesPerBuffer;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    private readonly SortedList<int, LogBuffer> _bufferList = [];
    private readonly ConcurrentDictionary<int, LogBufferCacheEntry> _lruCacheDict;
    private readonly ThreadLocal<int> _lastBufferIndex = new(() => -1);

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private volatile bool _isLineCountDirty = true;
    private int _cachedLineCount;

    public BufferIndex (int maxBuffers, int maxLinesPerBuffer)
    {
        _maxBuffers = maxBuffers;
        _maxLinesPerBuffer = maxLinesPerBuffer;
        _lruCacheDict = new(Environment.ProcessorCount, maxBuffers + 1);
    }

    #region Hot Path Lookup

    /// <summary>
    /// 4-layer lookup. Caller must hold at least a read lock. Returns false if lineNum is out of range or the index is
    /// empty.
    /// </summary>
    public LogBufferEntry TryFindBuffer (int lineNum)
    {
        return TryFindBufferWithIndex(lineNum);
    }

    /// <summary>
    /// Core buffer lookup returning both buffer and index position. The caller MUST already hold a read,
    /// upgradeable-read, or write lock.
    /// </summary>
    internal LogBufferEntry GetBufferForLineWithIndex (int lineNum)
    {
        return TryFindBufferWithIndex(lineNum);
    }

    private LogBufferEntry TryFindBufferWithIndex (int lineNum)
    {
#if DEBUG
        Util.AssertTrue(_lock.IsReadLockHeld || _lock.IsUpgradeableReadLockHeld || _lock.IsWriteLockHeld, "No lock held for buffer list in TryFindBufferWithIndex");
        long startTime = Environment.TickCount;
#endif
        var arr = _bufferList.Values;
        var count = arr.Count;

        if (count == 0)
        {
            return new LogBufferEntry(null, -1, false);
        }

        // Layer 0: Last buffer cache — O(1) for sequential access
        var lastIdx = _lastBufferIndex.Value;
        if (lastIdx >= 0 && lastIdx < count)
        {
            var buf = arr[lastIdx];
            if ((uint)(lineNum - buf.StartLine) < (uint)buf.LineCount)
            {
                //dont UpdateLRUCache, the cache has not changed in layer 0
                return new LogBufferEntry(buf, lastIdx, true);
            }

            // Layer 1: Adjacent buffer prediction — O(1) for buffer boundary crossings
            if (lastIdx + 1 < count)
            {
                var next = arr[lastIdx + 1];
                if ((uint)(lineNum - next.StartLine) < (uint)next.LineCount)
                {
                    _lastBufferIndex.Value = lastIdx + 1;
                    UpdateLru(next);
                    return new LogBufferEntry(next, lastIdx + 1, true);
                }
            }

            if (lastIdx - 1 >= 0)
            {
                var prev = arr[lastIdx - 1];
                if ((uint)(lineNum - prev.StartLine) < (uint)prev.LineCount)
                {
                    _lastBufferIndex.Value = lastIdx - 1;
                    UpdateLru(prev);
                    return new LogBufferEntry(prev, lastIdx - 1, true);
                }
            }
        }

        // Layer 2: Direct mapping guess — O(1) speculative for uniform buffers
        var guess = lineNum / _maxLinesPerBuffer;
        if ((uint)guess < (uint)count)
        {
            var buf = arr[guess];
            if ((uint)(lineNum - buf.StartLine) < (uint)buf.LineCount)
            {
                _lastBufferIndex.Value = guess;
                UpdateLru(buf);
                return new LogBufferEntry(buf, guess, true);
            }
        }

        // Layer 3: Branchless binary search with power-of-two strides
        var step = HighestPowerOfTwo(count);
        var idx = (arr[step - 1].StartLine <= lineNum) ? count - step : 0;

        for (step >>= 1; step > 0; step >>= 1)
        {
            var probe = idx + step;
            if (probe < count && arr[probe].StartLine <= lineNum)
            {
                idx = probe;
            }
        }

        // idx is now the buffer index — verify bounds
        if (idx < count)
        {
            var buf = arr[idx];
            if ((uint)(lineNum - buf.StartLine) < (uint)buf.LineCount)
            {
                _lastBufferIndex.Value = idx;
                UpdateLru(buf);
                return new LogBufferEntry(buf, idx, true);
            }
        }
#if DEBUG
        long endTime = Environment.TickCount;
        _logger.Debug($"TryFindBufferWithIndex({lineNum}) duration: {endTime - startTime} ms.");
#endif
        return new LogBufferEntry(null, -1, false);
    }

    #endregion

    #region Navigation: multi-file traversal

    /// <summary>
    /// Finds the start line of the next file segment after <paramref name="lineNum"/>. Caller must hold at least a read
    /// lock.
    /// </summary>
    public (bool Found, int StartLine) TryGetNextFileStartLine (int lineNum)
    {
        var result = -1;

        var foundBufferEntry = TryFindBufferWithIndex(lineNum);
        if (!foundBufferEntry.Found)
        {
            return (foundBufferEntry.Found, result);
        }

        for (var i = foundBufferEntry.Index; i < _bufferList.Values.Count; ++i)
        {
            if (_bufferList.Values[i].FileInfo != foundBufferEntry.Buffer.FileInfo)
            {
                result = _bufferList.Values[i].StartLine;
                break;
            }
        }

        return (result != -1, result);
    }

    /// <summary>
    /// Finds the start line of the previous file segment before <paramref name="lineNum"/>. Caller must hold at least a
    /// read lock.
    /// </summary>
    public (bool Found, int StartLine) TryGetPrevFileStartLine (int lineNum)
    {
        var result = -1;

        var foundBufferEntry = TryFindBufferWithIndex(lineNum);

        if (!foundBufferEntry.Found)
        {
            return (foundBufferEntry.Found, result);
        }

        if (foundBufferEntry.Buffer != null && foundBufferEntry.Index != -1)
        {
            for (var i = foundBufferEntry.Index; i >= 0; --i)
            {
                if (_bufferList.Values[i].FileInfo != foundBufferEntry.Buffer.FileInfo)
                {
                    result = _bufferList.Values[i].StartLine + _bufferList.Values[i].LineCount;
                    break;
                }
            }
        }

        return (result != -1, result);
    }

    /// <summary>
    /// Finds the first buffer belonging to the same file as <paramref name="logBuffer"/>. Caller must hold at least a
    /// read lock.
    /// </summary>
    public LogBuffer? GetFirstBufferForFile (LogBuffer logBuffer, int index)
    {
        //maybe not necessary
        ArgumentNullException.ThrowIfNull(logBuffer, "GetFirstBufferForFile not possible: Buffer is NULL");

        if (index == -1)
        {
            return null;
        }

        var info = logBuffer.FileInfo;

        var resultBuffer = logBuffer;
        while (true)
        {
            index--;
            if (index < 0 || _bufferList.Values[index].FileInfo != info)
            {
                break;
            }

            resultBuffer = _bufferList.Values[index];
        }

        return resultBuffer;
    }

    #endregion

    #region Mutation — called during reads and rollover

    /// <summary>
    /// Adds a buffer to the index and updates LRU tracking. Caller must hold a write lock.
    /// </summary>
    public void Add (LogBuffer buffer)
    {
#if DEBUG
        _logger.Debug(CultureInfo.InvariantCulture, "AddBufferToList(): {0}/{1}/{2}", buffer.StartLine, buffer.LineCount, buffer.FileInfo.FullName);
#endif
        _bufferList[buffer.StartLine] = buffer;
        UpdateLru(buffer);
        _isLineCountDirty = true;
    }

    /// <summary>
    /// Removes a buffer by its start line key and LRU entry. Caller must hold a write lock.
    /// </summary>
    public bool Remove (LogBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer, "Remove not possible: Buffer is NULL");

        Debug.Assert(_lock.IsWriteLockHeld, "No writer lock for buffer list");
        _ = _lruCacheDict.TryRemove(buffer.StartLine, out _);
        _isLineCountDirty = true;
        return _bufferList.Remove(buffer.StartLine);
    }

    /// <summary>
    /// Atomically updates a buffer's start line in both the index and LRU cache. Used by ShiftBuffers during rollover.
    /// Caller must hold a write lock.
    /// </summary>
    public void UpdateStartLine (LogBuffer buffer, int newStartLine)
    {
        var hadCache = _lruCacheDict.TryRemove(buffer.StartLine, out var cacheEntry);

        _ = _bufferList.Remove(buffer.StartLine);
        buffer.StartLine = newStartLine;
        _bufferList[newStartLine] = buffer;

        if (hadCache)
        {
            _ = _lruCacheDict.TryAdd(buffer.StartLine, cacheEntry);
        }

        _isLineCountDirty = true;
    }

    /// <summary>
    /// Clears all buffers and LRU entries. Does NOT dispose buffer content. Caller must hold a write lock.
    /// </summary>
    public void Clear ()
    {
        _bufferList.Clear();
        _lruCacheDict.Clear();
        ResetThreadLocalCache();
        _isLineCountDirty = true;
    }

    #endregion

    #region LRU eviction

    /// <summary>
    /// Removes least-recently-used entries when cache exceeds max size. Evicts content but preserves metadata so
    /// buffers remain findable for re-read. Does NOT acquire _lock — only touches _lruCache (ConcurrentDictionary) and
    /// individual buffer SpinLocks.
    /// </summary>
    public void EvictLeastRecentlyUsed ()
    {
#if DEBUG
        long startTime = Environment.TickCount;
#endif
        _logger.Debug(CultureInfo.InvariantCulture, "Starting garbage collection");
        var threshold = 10;

        if (_lruCacheDict.Count - (_maxBuffers + threshold) > 0)
        {
            var diff = _lruCacheDict.Count - _maxBuffers;
#if DEBUG
            if (diff > 0)
            {
                _logger.Info(CultureInfo.InvariantCulture, "Removing {0} entries from LRU cache", diff);
            }
#endif
            // Snapshot values and sort by timestamp (ascending = least recently used first)
            var entries = _lruCacheDict.ToArray();
            Array.Sort(entries, static (a, b) => a.Value.LastUseTimeStamp.CompareTo(b.Value.LastUseTimeStamp));

            for (var i = 0; i < diff && i < entries.Length; ++i)
            {
                var kvp = entries[i];
                if (_lruCacheDict.TryRemove(kvp.Key, out var removed))
                {
                    // Skip pinned buffers — the UI is actively displaying their content.
                    // Re-add to LRU so they'll be reconsidered in a future eviction pass.
                    if (removed.LogBuffer.IsPinned)
                    {
                        _lruCacheDict.TryAdd(kvp.Key, removed);
                        continue;
                    }

                    var lockTaken = false;
                    try
                    {
                        removed.LogBuffer.AcquireContentLock(ref lockTaken);
                        // Evict content but preserve metadata (LineCount, StartLine, etc.)
                        // so the buffer remains findable in _bufferList lookups.
                        // Do NOT return to pool — the buffer is still referenced by _bufferList.
                        removed.LogBuffer.EvictContent();
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            removed.LogBuffer.ReleaseContentLock();
                        }
                    }
                }
            }
        }

#if DEBUG
        if (_lruCacheDict.Count - (_maxBuffers + threshold) > 0)
        {
            long endTime = Environment.TickCount;
            _logger.Info(CultureInfo.InvariantCulture, "Garbage collector time: " + (endTime - startTime) + " ms.");
        }
#endif
    }

    /// <summary>
    /// Pins all buffers that cover the specified line range. Returns a <see cref="PinHandle"/>
    /// that unpins them on dispose. Caller must hold at least a read lock.
    /// </summary>
    public PinHandle PinRange (int startLine, int endLine)
    {
        var pinned = new List<LogBuffer>();
        var line = startLine;

        while (line <= endLine)
        {
            var entry = TryFindBuffer(line);
            if (!entry.Found || entry.Buffer is null)
            {
                break;
            }

            entry.Buffer.Pin();
            pinned.Add(entry.Buffer);

            // Jump to next buffer's start line to avoid redundant lookups
            line = entry.Buffer.StartLine + entry.Buffer.LineCount;
        }

        return new PinHandle(pinned);
    }

    /// <summary>
    /// Atomically clears the index and returns all LRU-tracked buffers to the pool. Clears the index FIRST under the
    /// caller's write lock, THEN returns buffers to pool. This prevents a race where concurrent readers find buffers
    /// that have been returned to the pool. Caller must hold a write lock.
    /// </summary>
    public void ClearLru (LogBufferPool pool)
    {
        _logger.Info(CultureInfo.InvariantCulture, "Clearing LRU cache.");

        // 1. Collect buffer references before clearing
        var toReturn = new List<LogBuffer>(_lruCacheDict.Count);
        foreach (var entry in _lruCacheDict.Values)
        {
            toReturn.Add(entry.LogBuffer);
        }

        // 2. Clear index FIRST — no concurrent reader can find these after this
        _bufferList.Clear();
        _lruCacheDict.Clear();
        _isLineCountDirty = true;
        ResetThreadLocalCache();

        // 3. Now safe to return to pool
        foreach (var entry in toReturn)
        {
            var lockTaken = false;
            try
            {
                entry.AcquireContentLock(ref lockTaken);
                pool.Return(entry);
            }
            finally
            {
                if (lockTaken)
                {
                    entry.ReleaseContentLock();
                }
            }
        }

        _logger.Info(CultureInfo.InvariantCulture, "Clearing done.");
    }

    #endregion

    /// <summary>
    /// Gets the number of buffers.
    /// </summary>
    public int BufferCount => _bufferList.Count;

    /// <summary>
    /// Returns the buffer at the specified positional index. Caller must hold at least a read lock.
    /// </summary>
    public LogBuffer GetBufferAt (int index) => _bufferList.GetValueAtIndex(index);

    /// <summary>
    /// Returns the last buffer in the index (highest start line). Caller must hold at least a read lock.
    /// </summary>
    public LogBuffer GetLastBuffer () => _bufferList.GetValueAtIndex(_bufferList.Count - 1);

    /// <summary>
    /// Returns an enumerable collection of all log buffers managed by the current instance.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{LogBuffer}"/> containing each <see cref="LogBuffer"/> in the collection. The
    /// enumeration reflects the current state of the buffers at the time of the call.
    /// </returns>
    public IEnumerable<LogBuffer> EnumerateBuffers () { return [.. _bufferList.Values]; }

    /// <summary>
    /// Total lines across all buffers. Recalculated on demand when dirty. Caller must hold at least a read lock.
    /// </summary>
    public int TotalLineCount
    {
        get
        {
            if (_isLineCountDirty)
            {
                var total = 0;
                foreach (var buffer in _bufferList.Values)
                {
                    total += buffer.LineCount;
                }

                _cachedLineCount = total;
                _isLineCountDirty = false;
            }

            return _cachedLineCount;
        }
    }

    public void MarkLineCountDirty () => _isLineCountDirty = true;

    /// <summary>
    /// Gets the number of items currently stored in the least recently used (LRU) cache.
    /// </summary>
    public int LruCacheCount => _lruCacheDict.Count;

    #region Lock management — using-scoped only

    public ReadLockScope AcquireReadLock () => new(_lock);

    public WriteLockScope AcquireWriteLock () => new(_lock);

    public UpgradeableReadLockScope AcquireUpgradeableReadLock () => new(_lock);

    #endregion

    #region Diagnostics

    /// <summary>
    /// Creates an immutable point-in-time capture of the index state. Acquires its own read lock internally.
    /// </summary>
    public BufferIndexSnapshot CreateSnapshot ()
    {
        using var _ = AcquireReadLock();

        var buffers = new List<BufferIndexSnapshot.BufferInfo>(_bufferList.Count);

        foreach (var b in _bufferList.Values)
        {
            buffers.Add(new BufferIndexSnapshot.BufferInfo
                (
                    b.StartLine,
                    b.LineCount,
                    b.StartPos,
                    b.Size,
                    b.IsDisposed,
                    b.FileInfo.FullName
                ));
        }

        return new BufferIndexSnapshot
        {
            BufferCount = _bufferList.Count,
            TotalLineCount = TotalLineCount,
            LruCacheCount = _lruCacheDict.Count,
            Buffers = buffers
        };
    }

    #endregion

    #region Internal Helpers

    public void ResetThreadLocalCache () => _lastBufferIndex.Value = -1;

    private void UpdateLru (LogBuffer logBuffer)
    {
        var cacheEntry = _lruCacheDict.GetOrAdd(
            logBuffer.StartLine,
            static (_, buf) => new LogBufferCacheEntry { LogBuffer = buf },
            logBuffer);

        cacheEntry.Touch();
    }

    private static int HighestPowerOfTwo (int n) => 1 << (31 - int.LeadingZeroCount(n));

    public void Dispose ()
    {
        _lastBufferIndex.Dispose();
        _lock.Dispose();
    }

    #endregion
}

#region Lock scope structs

public readonly ref struct ReadLockScope
{
    private readonly ReaderWriterLockSlim _lock;

    public ReadLockScope (ReaderWriterLockSlim rwLock)
    {
        _lock = rwLock;
        if (!_lock.TryEnterReadLock(TimeSpan.FromSeconds(10)))
        {
            //_logger.Warn("Reader lock wait timed out, forcing entry");
            _lock.EnterReadLock();
        }
    }

    public void Dispose () => _lock.ExitReadLock();

}

public readonly ref struct WriteLockScope
{
    private readonly ReaderWriterLockSlim _lock;

    public WriteLockScope (ReaderWriterLockSlim rwLock)
    {
        _lock = rwLock;
        if (!_lock.TryEnterWriteLock(TimeSpan.FromSeconds(10)))
        {
            //_logger.Warn("Writer lock wait timed out, forcing entry");
            _lock.EnterWriteLock();
        }
    }

    public void Dispose () => _lock.ExitWriteLock();
}

public readonly ref struct UpgradeableReadLockScope
{
    private readonly ReaderWriterLockSlim _lock;

    public UpgradeableReadLockScope (ReaderWriterLockSlim rwLock)
    {
        _lock = rwLock;
        if (!_lock.TryEnterUpgradeableReadLock(TimeSpan.FromSeconds(10)))
        {
            //_logger.Warn("Upgradeable read lock timed out, forcing entry");
            _lock.EnterUpgradeableReadLock();
        }
    }

    public WriteLockUpgradeScope UpgradeToWrite () => new(_lock);

    public void Dispose () => _lock.ExitUpgradeableReadLock();
}

public readonly ref struct WriteLockUpgradeScope
{
    private readonly ReaderWriterLockSlim _lock;

    public WriteLockUpgradeScope (ReaderWriterLockSlim rwls)
    {
        _lock = rwls;
        if (!_lock.TryEnterWriteLock(TimeSpan.FromSeconds(10)))
        {
            //_logger.Warn("Writer lock upgrade timed out, forcing entry");
            _lock.EnterWriteLock();
        }
    }

    public void Dispose () => _lock.ExitWriteLock();
}

#endregion

public readonly struct LogBufferEntry (LogBuffer? buffer, int index, bool found)
{
    public LogBuffer? Buffer { get; } = buffer;

    public int Index { get; } = index;

    public bool Found { get; } = found;
}