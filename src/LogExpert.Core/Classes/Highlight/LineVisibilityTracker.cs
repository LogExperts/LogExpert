using ColumnizerLib;

namespace LogExpert.Core.Classes.Highlight;

/// <summary>
/// Keeps a Log Window's <see cref="LineVisibilityMap"/> up to date with the hide-line rules of its Highlight Group.
/// Lines are never read while the lock is held: a reader call may wait for the UI thread, which takes the lock too.
/// </summary>
public sealed class LineVisibilityTracker : IDisposable
{
    private const int READ_BATCH_SIZE = 256;

    private readonly Func<int, ITextValueMemory?> _getLine;
    private readonly Func<int, int, IDisposable?>? _pinRange;
    private readonly Lock _lock = new();

    private volatile LineVisibilityMap _map = LineVisibilityMap.Empty;
    private volatile HighlightEntry[]? _pendingRules;
    private volatile bool _loadPending;
    private HighlightEntry[] _rules = [];
    private CancellationTokenSource? _scanCts;
    private Task _scanTask = Task.CompletedTask;
    private int _generation;
    private int _version;
    private bool _disposed;

    /// <param name="getLine">Reads an original line; read live, since the reader is replaced on reload.</param>
    /// <param name="pinRange">Pins the buffers of an inclusive line range before it is read, so they can't be evicted mid-read.</param>
    public LineVisibilityTracker (Func<int, ITextValueMemory?> getLine, Func<int, int, IDisposable?>? pinRange = null)
    {
        ArgumentNullException.ThrowIfNull(getLine);
        _getLine = getLine;
        _pinRange = pinRange;
    }

    /// <summary>
    /// Raised on a worker thread when a background scan replaces the map, or when evaluation failed and every line
    /// was made visible (<see cref="LineVisibilityChangedEventArgs.Error"/> set).
    /// </summary>
    public event EventHandler<LineVisibilityChangedEventArgs>? Changed;

    public LineVisibilityMap Map => _map;

    public bool IsScanning => _pendingRules != null;

    /// <summary>The scan started by <see cref="Load"/> has not finished: the map does not reflect the rules yet.</summary>
    public bool IsLoadPending => _loadPending;

    /// <summary>New content was loaded: every line is visible until the scan with <paramref name="entries"/> completes.</summary>
    public void Load (int lineCount, IEnumerable<HighlightEntry> entries)
    {
        var rules = Snapshot(entries);
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            SetStateLocked(LineVisibilityMap.Identity(lineCount), []);
            StartOrStopScanLocked(rules);
            _loadPending = rules.Length > 0;
        }
    }

    /// <summary>The Highlight Group or its entries changed. Unchanged hide rules keep the current map.</summary>
    public void Rebuild (IEnumerable<HighlightEntry> entries)
    {
        var rules = Snapshot(entries);
        LineVisibilityMap? cleared = null;
        lock (_lock)
        {
            if (_disposed || SameRules(_pendingRules ?? _rules, rules))
            {
                return;
            }

            StartOrStopScanLocked(rules);
            if (rules.Length == 0 && _map.HiddenCount > 0)
            {
                cleared = LineVisibilityMap.Identity(_map.LineCount);
                SetStateLocked(cleared, []);
            }
        }

        if (cleared != null)
        {
            Changed?.Invoke(this, new LineVisibilityChangedEventArgs(cleared, null));
        }
    }

    /// <summary>Tail path: evaluates lines appended up to <paramref name="lineCount"/>.</summary>
    public void Extend (int lineCount)
    {
        EvaluateAndCommit(lineCount, replace: false);
    }

    /// <summary>Tail path, rollover: the first <paramref name="offset"/> lines were dropped.</summary>
    public void Shift (int offset)
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                SetStateLocked(_map.Shift(offset), _rules);
                RestartPendingScanLocked();
            }
        }
    }

    /// <summary>Tail path, truncation: the content was replaced and is re-evaluated up to <paramref name="lineCount"/>.</summary>
    public void Replace (int lineCount)
    {
        EvaluateAndCommit(lineCount, replace: true);
    }

    /// <summary>
    /// Evaluates against a snapshot outside the lock, then commits only if the state is unchanged, retrying otherwise.
    /// <paramref name="replace"/> re-evaluates from the first line and restarts a pending scan on the new content.
    /// </summary>
    private void EvaluateAndCommit (int lineCount, bool replace)
    {
        while (true)
        {
            LineVisibilityMap from;
            HighlightEntry[] rules;
            int version;
            lock (_lock)
            {
                if (_disposed || (!replace && lineCount <= _map.LineCount))
                {
                    return;
                }

                (from, rules, version) = (replace ? LineVisibilityMap.Empty : _map, _rules, _version);
            }

            var (map, error) = TryEvaluate(from, rules, lineCount);
            lock (_lock)
            {
                if (_disposed)
                {
                    return;
                }

                if (version != _version)
                {
                    continue;
                }

                SetStateLocked(map, error == null ? rules : []);
                if (replace)
                {
                    RestartPendingScanLocked();
                }
            }

            if (error != null)
            {
                Changed?.Invoke(this, new LineVisibilityChangedEventArgs(map, error));
            }

            return;
        }
    }

    /// <summary>Completes once no scan is running (including scans restarted meanwhile).</summary>
    public async Task WhenIdle ()
    {
        while (true)
        {
            Task task;
            lock (_lock)
            {
                task = _scanTask;
            }

            await task.ConfigureAwait(false);

            lock (_lock)
            {
                if (task == _scanTask)
                {
                    return;
                }
            }
        }
    }

    public void Dispose ()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _generation++;
            _version++;
            _pendingRules = null;
            _loadPending = false;
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    private void SetStateLocked (LineVisibilityMap map, HighlightEntry[] rules)
    {
        _map = map;
        _rules = rules;
        _version++;
    }

    private void StartOrStopScanLocked (HighlightEntry[] rules)
    {
        _generation++;
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = null;
        _pendingRules = null;

        if (rules.Length == 0)
        {
            _loadPending = false;
            return;
        }

        var cts = new CancellationTokenSource();
        var generation = _generation;
        var lineCount = _map.LineCount;
        _scanCts = cts;
        _pendingRules = rules;
        _scanTask = Task.Run(() => Scan(rules, lineCount, generation, cts.Token), CancellationToken.None);
    }

    private void RestartPendingScanLocked ()
    {
        if (_pendingRules != null)
        {
            StartOrStopScanLocked(_pendingRules);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Any evaluation failure falls back to showing every line and is reported")]
    private void Scan (HighlightEntry[] rules, int lineCount, int generation, CancellationToken token)
    {
        LineVisibilityMap map;
        Exception? error = null;
        try
        {
            map = Evaluate(LineVisibilityMap.Empty, rules, lineCount, token);

            // Catch up with lines the tail appended while the scan ran, outside the lock, until none are left.
            while (true)
            {
                int known;
                lock (_lock)
                {
                    if (generation != _generation)
                    {
                        return;
                    }

                    known = _map.LineCount;
                    if (known <= map.LineCount)
                    {
                        SetStateLocked(map, rules);
                        _pendingRules = null;
                        _loadPending = false;
                        break;
                    }
                }

                map = Evaluate(map, rules, known, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                if (generation != _generation)
                {
                    return;
                }

                map = LineVisibilityMap.Identity(_map.LineCount);
                SetStateLocked(map, []);
                _pendingRules = null;
                _loadPending = false;
                error = ex;
            }
        }

        Changed?.Invoke(this, new LineVisibilityChangedEventArgs(map, error));
    }

    private (LineVisibilityMap Map, Exception? Error) TryEvaluate (LineVisibilityMap from, HighlightEntry[] rules, int lineCount)
    {
        try
        {
            return (Evaluate(from, rules, lineCount, CancellationToken.None), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Never publish a partial state: fall back to showing every line.
            return (LineVisibilityMap.Identity(lineCount), ex);
        }
    }

    private LineVisibilityMap Evaluate (LineVisibilityMap from, HighlightEntry[] rules, int lineCount, CancellationToken token)
    {
        if (lineCount <= from.LineCount)
        {
            return from;
        }

        List<int> hidden = [];
        if (rules.Length > 0)
        {
            for (var batchStart = from.LineCount; batchStart < lineCount; batchStart += READ_BATCH_SIZE)
            {
                var batchEnd = Math.Min(batchStart + READ_BATCH_SIZE, lineCount);
                using var pin = _pinRange?.Invoke(batchStart, batchEnd - 1);
                for (var i = batchStart; i < batchEnd; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (IsHiddenLine(rules, i))
                    {
                        hidden.Add(i);
                    }
                }
            }
        }

        return from.Append(lineCount, hidden);
    }

    private bool IsHiddenLine (HighlightEntry[] rules, int lineNum)
    {
        // An unreadable line cannot be classified; it stays visible.
        var line = _getLine(lineNum);
        return line != null && HighlightEvaluator.IsHidden(rules, line);
    }

    private static HighlightEntry[] Snapshot (IEnumerable<HighlightEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return [.. entries.Where(HighlightEvaluator.IsHideRule).Select(e => (HighlightEntry)e.Clone())];
    }

    private static bool SameRules (HighlightEntry[] current, HighlightEntry[] next)
    {
        return current.Length == next.Length
            && current.Zip(next).All(pair => string.Equals(pair.First.SearchText, pair.Second.SearchText, StringComparison.Ordinal)
                && pair.First.IsRegex == pair.Second.IsRegex
                && pair.First.IsCaseSensitive == pair.Second.IsCaseSensitive);
    }
}

public sealed class LineVisibilityChangedEventArgs (LineVisibilityMap map, Exception? error) : EventArgs
{
    public LineVisibilityMap Map { get; } = map;

    /// <summary>Set when evaluation failed; the map then shows every line.</summary>
    public Exception? Error { get; } = error;
}
