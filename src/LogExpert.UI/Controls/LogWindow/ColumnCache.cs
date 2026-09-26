using ColumnizerLib;

using LogExpert.Core.Callback;
using LogExpert.Core.Classes.Log.Buffers;
using LogExpert.Core.Interfaces;

namespace LogExpert.UI.Controls.LogWindow;

internal class ColumnCache
{
    #region Fields

    private IColumnizedLogLineMemory _cachedColumns;
    private ILogLineMemoryColumnizer _lastColumnizer;
    private int _lastLineNumber = -1;

    // Prefetch state
    private ILogLineMemory[] _prefetchedLines;
    private int _prefetchStartLine = -1;
    private int _prefetchCount;
    private PinHandle? _pinHandle;

    // Sparse prefetch state (PrefetchLines)
    private Dictionary<int, ILogLineMemory>? _sparseLines;
    private int[] _sparseKey = [];
    private List<PinHandle> _sparsePinHandles = [];

    #endregion

    #region Internals

    /// <summary>
    /// Prefetch a range of lines in a single batch call.
    /// Call this before a paint cycle with the visible row range.
    /// Pin-before-read: pins are acquired BEFORE reading lines to prevent
    /// the GC eviction thread from returning char blocks between read and pin.
    /// </summary>
    internal void Prefetch (ILogfileReader logFileReader, int startLine, int count)
    {
        if (startLine == _prefetchStartLine && count == _prefetchCount)
        {
            return; // already prefetched this exact range
        }

        //Pin BEFORE reading — this should prevent GC thread from evicting buffers
        //between GetLogLineMemories() and PinRange().
        PinHandle? newHandle = null;
        if (logFileReader is IBufferPinning pinning)
        {
            newHandle = pinning.PinRange(startLine, startLine + count - 1);
        }

        var newLines = logFileReader.GetLogLineMemories(startLine, count);

        //Atomic swap: old handle released AFTER new is in place
        var oldHandles = _sparsePinHandles;
        var oldHandle = _pinHandle;
        _pinHandle = newHandle;
        _sparsePinHandles = [];
        _sparseLines = null;
        _prefetchedLines = newLines;
        _prefetchStartLine = startLine;
        _prefetchCount = newLines.Length;

        // Now safe to release old pins — new pins cover the new visible range
        oldHandle?.Dispose();
        DisposeAll(oldHandles);
    }

    /// <summary>
    /// Prefetch the given ascending, possibly non-contiguous lines (visible rows when hide-line rules skip lines).
    /// Each contiguous run is pinned before it is read, like <see cref="Prefetch"/>, so only buffers holding
    /// displayed lines are pinned.
    /// </summary>
    internal void PrefetchLines (ILogfileReader logFileReader, int[] lines)
    {
        if (_sparseLines != null && _prefetchStartLine == -1 && _sparseKey.AsSpan().SequenceEqual(lines))
        {
            return; // already prefetched these exact lines
        }

        List<PinHandle> newHandles = [];
        Dictionary<int, ILogLineMemory> newLines = new(lines.Length);
        var runStart = 0;
        for (var i = 1; i <= lines.Length; i++)
        {
            if (i < lines.Length && lines[i] == lines[i - 1] + 1)
            {
                continue;
            }

            var first = lines[runStart];
            var count = lines[i - 1] - first + 1;
            if (logFileReader is IBufferPinning pinning)
            {
                newHandles.Add(pinning.PinRange(first, first + count - 1));
            }

            var runLines = logFileReader.GetLogLineMemories(first, count);
            for (var j = 0; j < runLines.Length; j++)
            {
                newLines[first + j] = runLines[j];
            }

            runStart = i;
        }

        var oldHandle = _pinHandle;
        var oldHandles = _sparsePinHandles;
        _pinHandle = null;
        _sparsePinHandles = newHandles;
        _sparseLines = newLines;
        _sparseKey = lines;
        _prefetchedLines = null;
        _prefetchStartLine = -1;
        _prefetchCount = 0;

        oldHandle?.Dispose();
        DisposeAll(oldHandles);
    }

    /// <summary>
    /// Returns the prefetched line for the given line number if it falls within the
    /// currently pinned range. Returns null if not available.
    /// </summary>
    internal ILogLineMemory? GetPrefetchedLine (int lineNumber)
    {
        if (_sparseLines != null)
        {
            return _sparseLines.GetValueOrDefault(lineNumber);
        }

        return _prefetchedLines != null
            && lineNumber >= _prefetchStartLine
            && lineNumber < _prefetchStartLine + _prefetchCount
                ? _prefetchedLines[lineNumber - _prefetchStartLine]
                : null;
    }

    private static void DisposeAll (List<PinHandle> handles)
    {
        foreach (var handle in handles)
        {
            handle.Dispose();
        }
    }

    /// <summary>
    /// Invalidates the prefetch cache. Call on scroll, data change, or columnizer change.
    /// </summary>
    internal void InvalidatePrefetch ()
    {
        _pinHandle?.Dispose();
        _pinHandle = null;
        DisposeAll(_sparsePinHandles);
        _sparsePinHandles = [];
        _sparseLines = null;

        _prefetchedLines = null;
        _prefetchStartLine = -1;
        _prefetchCount = 0;
        _lastLineNumber = -1;
    }

    /// <summary>
    /// Forces the next <see cref="Prefetch"/> call to re-fetch and re-pin, without releasing
    /// current pins. Use this instead of <see cref="InvalidatePrefetch"/> when the visible data
    /// may be stale but the underlying buffers must remain pinned until replacement pins are acquired.
    /// </summary>
    internal void MarkPrefetchStale ()
    {
        _sparseLines = null;
        _prefetchStartLine = -1;
        _prefetchCount = 0;
        _lastLineNumber = -1;
        _cachedColumns = null;
    }

    internal IColumnizedLogLineMemory GetColumnsForLine (ILogfileReader logFileReader, int lineNumber, ILogLineMemoryColumnizer columnizer, ColumnizerCallback columnizerCallback)
    {
        // Re-fetch when:
        //   - the columnizer instance changed
        //   - we are asked for a different line number
        //   - the callback's line number is out of sync with the requested line
        //   - the cache is empty (null) — without this, a single null fetch for a given
        //     lineNumber poisons the cache and every subsequent call for that same line
        //     returns null. This is visible as a permanently blank row when the grid
        //     only displays a single row (e.g. CSV file with header + 1 data line and
        //     header dropped by the CsvColumnizer's PreProcessLine).
        if (_lastColumnizer != columnizer
            || _lastLineNumber != lineNumber
            || columnizerCallback.LineNum != lineNumber
            || _cachedColumns == null)
        {
            _lastColumnizer = columnizer;
            _lastLineNumber = lineNumber;

            var line = GetPrefetchedLine(lineNumber);

            // Fallback: read directly. This is safe because the caller (CellValueNeeded)
            // has already called Prefetch/PrefetchFilterVisibleLines which pins the relevant
            // buffers. Pinned buffers won't have their char blocks returned to the pool.
            line ??= logFileReader.GetLogLineMemoryWithWait(lineNumber).Result;

            if (line != null)
            {
                columnizerCallback.SetLineNum(lineNumber);
                _cachedColumns = columnizer.SplitLine(columnizerCallback, line);
            }
            else
            {
                _cachedColumns = null;
            }
        }

        return _cachedColumns;
    }

    #endregion
}