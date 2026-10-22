using System.Buffers;

using ColumnizerLib;

using NLog;

namespace LogExpert.Core.Classes.Log.Buffers;

public class LogBuffer
{
    #region Fields

    private SpinLock _contentLock = new(enableThreadOwnerTracking: false);
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

#if DEBUG
    private readonly List<long> _filePositions; // file position for every line
#endif

    private LogLine[] _lineArray;
    private int _lineArrayLength; // capacity of the rented array
    private List<char[]> _charBlocks;

    private int _pinCount;

    private int MAX_LINES = 500;

    #endregion

    #region cTor

    // Don't use a primary constructor here: field initializers (like MAX_LINES) run before primary constructor parameters are assigned,
    // so MAX_LINES would always be set to its default value before the constructor body can assign it. Use a regular constructor instead.
    public LogBuffer (ILogFileInfo fileInfo, int maxLines)
    {
        FileInfo = fileInfo;
        MAX_LINES = maxLines;
        _lineArray = ArrayPool<LogLine>.Shared.Rent(maxLines);
        _lineArrayLength = _lineArray.Length;
#if DEBUG
        _filePositions = new(MAX_LINES);
#endif
    }

    #endregion

    #region Properties

    /// <summary>
    /// Returns true if any component has pinned this buffer to prevent eviction.
    /// </summary>
    public bool IsPinned => Volatile.Read(ref _pinCount) > 0;

    public long StartPos { set; get; }

    public long Size
    {
        set
        {
            field = value;
#if DEBUG
            if (_filePositions.Count > 0)
            {
                if (field < _filePositions[^1] - StartPos)
                {
                    _logger.Error("### LogBuffer: LogBuffer overall Size must be greater than last line file position!");
                }
            }
#endif
        }
        get;
    }

    public int EndLine => StartLine + LineCount;

    public int StartLine { set; get; }

    public int LineCount { get; private set; }

    public bool IsDisposed { get; private set; }

    public ILogFileInfo FileInfo { get; set; }

    public int DroppedLinesCount { get; set; }

    public int PrevBuffersDroppedLinesSum { get; set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Increments the pin count. While pinned, the buffer will not be evicted by the LRU garbage collector. Each call
    /// to Pin() must be balanced by a call to Unpin().
    /// </summary>
    public void Pin ()
    {
        _ = Interlocked.Increment(ref _pinCount);
    }

    /// <summary>
    /// Decrements the pin count. When the count reaches zero, the buffer becomes eligible for eviction.
    /// </summary>
    public void Unpin ()
    {
#if DEBUG
        var newCount = Interlocked.Decrement(ref _pinCount);
        if (newCount < 0)
        {
            _logger.Warn("Unpin underflow: _pinCount went to {0}. Unbalanced Pin/Unpin calls.", newCount);
        }
#else
        Interlocked.Decrement(ref _pinCount);
#endif
    }

    /// <summary>
    /// Adds a log line to the internal collection at the specified file position.
    /// </summary>
    /// <remarks>
    /// If the internal collection has reached its maximum capacity, the log line is not added. In debug builds, an
    /// error is logged when this occurs.
    /// </remarks>
    /// <param name="lineMemory">The log line to add to the collection.</param>
    /// <param name="filePos">The file position associated with the log line.</param>
    public void AddLine (LogLine lineMemory, long filePos)
    {
        if (LineCount < _lineArrayLength)
        {
            _lineArray[LineCount] = lineMemory;
            LineCount++;
        }
#if DEBUG
        else
        {
            _logger.Error("AddLine overflow: LineCount={0} >= _lineArrayLength={1}", LineCount, _lineArrayLength);
        }
#endif

#if DEBUG
        _filePositions.Add(filePos);
#endif
        IsDisposed = false;
    }

    /// <summary>
    /// Removes all log lines from the current collection, resetting its state for reuse.
    /// </summary>
    /// <remarks>
    /// After calling this method, the collection will be empty and ready to accept new log lines. Any resources
    /// associated with the previous log lines are released. This method is typically used to clear the log data before
    /// loading new content or starting a new logging session.
    /// </remarks>
    public void ClearLines ()
    {
        if (_lineArray == null)
        {
            _lineArray = ArrayPool<LogLine>.Shared.Rent(MAX_LINES);
            _lineArrayLength = _lineArray.Length;
        }
        else
        {
            Array.Clear(_lineArray, 0, LineCount);
        }

        ReturnCharBlocks();

        LineCount = 0;
#if DEBUG
        _filePositions.Clear();
#endif
    }

    /// <summary>
    /// Prepares the buffer for reuse from the pool.
    /// </summary>
    public void Reinitialise (ILogFileInfo fileInfo, int maxLines)
    {
        ReturnCharBlocks();

        FileInfo = fileInfo;
        MAX_LINES = maxLines;
        StartLine = 0;
        StartPos = 0;
        Size = 0;
        LineCount = 0;
        DroppedLinesCount = 0;
        PrevBuffersDroppedLinesSum = 0;
        IsDisposed = false;
        _pinCount = 0;
        _lineArray = ArrayPool<LogLine>.Shared.Rent(maxLines);
        _lineArrayLength = _lineArray.Length;
#if DEBUG
        _filePositions.Clear();
        DisposeCount = 0;
#endif
    }

    /// <summary>
    /// Evicts the buffer content to free memory while preserving metadata (LineCount, StartLine, StartPos, Size). The
    /// buffer remains findable in buffer list lookups and can be re-read from disk when accessed.
    /// </summary>
    public void EvictContent ()
    {
        if (_lineArray != null)
        {
            Array.Clear(_lineArray, 0, LineCount);
            ArrayPool<LogLine>.Shared.Return(_lineArray);
            _lineArray = null;
        }

        ReturnCharBlocks();

        //! Do NOT zero LineCount — it is needed for buffer lookup in GetBufferForLineWithIndex.
        //! Do NOT zero StartLine, StartPos, Size — they are needed for re-reading from disk.
        IsDisposed = true;
#if DEBUG
        DisposeCount++;
#endif
    }

    /// <summary>
    /// Fully disposes the buffer content and resets all metadata. Used when the buffer is being returned to the pool or
    /// completely removed from the buffer list.
    /// </summary>
    public void DisposeContent ()
    {
        if (_lineArray != null)
        {
            Array.Clear(_lineArray, 0, LineCount);
            ArrayPool<LogLine>.Shared.Return(_lineArray);
            _lineArray = null;
            LineCount = 0;
        }

        ReturnCharBlocks();

        IsDisposed = true;
#if DEBUG
        DisposeCount++;
#endif
    }

    /// <summary>
    /// Retrieves the log line at the specified index within the current memory block.
    /// </summary>
    /// <param name="num">
    /// The zero-based index of the log line to retrieve. Must be greater than or equal to 0 and less than the total
    /// number of lines.
    /// </param>
    /// <returns>
    /// The <see cref="LogLine"/> at the specified index if it exists; otherwise, <see langword="null"/>.
    /// </returns>
    public LogLine? GetLineMemoryOfBlock (int num)
    {
        return num < LineCount && num >= 0
        ? _lineArray[num]
        : null;
    }

    /// <summary>
    /// Acquires the content lock. The caller MUST call <see cref="ReleaseContentLock"/> in a finally block.
    /// </summary>
    public void AcquireContentLock (ref bool lockTaken)
    {
        _contentLock.Enter(ref lockTaken);
    }

    /// <summary>
    /// Releases the content lock previously acquired via <see cref="AcquireContentLock"/>.
    /// </summary>
    public void ReleaseContentLock ()
    {
        _contentLock.Exit(useMemoryBarrier: false);
    }

    /// <summary>
    /// Attaches pooled char[] blocks that back the ReadOnlyMemory in this buffer's LogLine entries. These blocks will
    /// be returned to ArrayPool when the buffer is evicted or disposed. New blocks are MERGED with existing ones —
    /// never replace — because the buffer's existing LogLine entries still reference the old blocks (e.g., during tail
    /// mode where multiple read sessions append lines to the same buffer).
    /// </summary>
    public void AttachCharBlocks (List<char[]> blocks)
    {
        if (_charBlocks is null)
        {
            _charBlocks = blocks;
        }
        else
        {
            _charBlocks.AddRange(blocks);
        }
    }

    #endregion

#if DEBUG

    public long DisposeCount { get; private set; }

    public long GetFilePosForLineOfBlock (int line)
    {
        return line >= 0 && line < _filePositions.Count
            ? _filePositions[line]
            : -1;
    }

#endif

    #region Private Methods

    /// <summary>
    /// Releases references to the character block buffers used by this instance, allowing them to be garbage collected
    /// when no longer in use.
    /// </summary>
    /// <remarks>
    /// If the buffer is pinned, this method only drops the reference without returning the blocks to the array pool, as
    /// external consumers may still hold references. This helps prevent premature reuse of buffers that may still be
    /// accessed elsewhere.
    /// </remarks>
    private void ReturnCharBlocks ()
    {
        if (_charBlocks is null)
        {
            return;
        }

        if (IsPinned)
        {
            // Buffer is pinned — UI still holds ReadOnlyMemory<char> slices into these blocks.
            // Don't return to ArrayPool; just drop the reference. GC will collect them
            // once all UI references (ColumnCache, DataGridView) are released.
            _charBlocks = null;
            return;
        }

        foreach (var block in _charBlocks)
        {
            ArrayPool<char>.Shared.Return(block);
        }

        _charBlocks = null;
    }

    #endregion
}