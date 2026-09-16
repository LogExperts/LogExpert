using ColumnizerLib;

using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Marker;

/// <summary>Owns a cancellable background index for one matching source in one Log Window.</summary>
public sealed class MarkerIndex : IDisposable
{
    private const int READ_BATCH_SIZE = 256;
    private const int MATCH_CHUNK_SIZE = 4096;
    private readonly Lock _gate = new();
    private ILogfileReader? _reader;
    private MarkerCriteria? _criteria;
    private Func<int, ILogLineMemory, IReadOnlyList<ITextValueMemory>>? _getColumns;
    private MarkerSnapshot _snapshot = new([], 0);
    private CancellationTokenSource? _scanCts;
    private Task? _work;
    private int _generation;
    private bool _disposed;

    public MarkerSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                return _snapshot;
            }
        }
    }

    public bool IsScanning
    {
        get
        {
            lock (_gate)
            {
                return _work != null;
            }
        }
    }

    public void Reset (ILogfileReader? reader, MarkerCriteria? criteria,
        Func<int, ILogLineMemory, IReadOnlyList<ITextValueMemory>>? getColumns = null)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _generation++;
            _scanCts?.Cancel();
            _scanCts = null;
            _work = null;
            _reader = reader;
            _criteria = criteria;
            _getColumns = getColumns;
            _snapshot = new MarkerSnapshot([], 0);
        }
    }

    public Task UpdateAsync (int lineCount, bool lastLineChanged = false)
    {
        lock (_gate)
        {
            if (_disposed || _reader == null || _criteria == null)
            {
                return Task.CompletedTask;
            }

            if (_work != null)
            {
                return _work;
            }

            lineCount = Math.Max(0, lineCount);
            if (lineCount < _snapshot.ScannedLineCount)
            {
                _snapshot = new MarkerSnapshot([], 0);
            }

            if (_snapshot.Error != null || (!lastLineChanged && lineCount == _snapshot.ScannedLineCount))
            {
                return Task.CompletedTask;
            }

            if (_criteria.IsEmpty)
            {
                _snapshot = new MarkerSnapshot([], lineCount);
                return Task.CompletedTask;
            }

            var reader = _reader;
            var criteria = _criteria;
            var columns = _getColumns;
            var previous = _snapshot;
            var generation = _generation;
            var cts = new CancellationTokenSource();
            _scanCts = cts;
            _work = Task.Run(() => Scan(reader, criteria, columns, previous, lineCount, generation, cts));
            return _work;
        }
    }

    public void Dispose ()
    {
        lock (_gate)
        {
            _disposed = true;
            _generation++;
            _scanCts?.Cancel();
            _scanCts = null;
            _work = null;
            _reader = null;
            _criteria = null;
            _getColumns = null;
            _snapshot = new MarkerSnapshot([], 0);
        }
    }

    private void Scan (ILogfileReader reader, MarkerCriteria criteria,
        Func<int, ILogLineMemory, IReadOnlyList<ITextValueMemory>>? columns,
        MarkerSnapshot previous, int lineCount, int generation, CancellationTokenSource cancellationSource)
    {
        using var scanCancellation = cancellationSource;
        var start = Math.Max(0, previous.ScannedLineCount - 1);
        List<MarkerLine[]> chunks = [];
        List<MarkerLine> pending = new(MATCH_CHUNK_SIZE);
        var scanned = start;
        Exception? error = null;
        try
        {
            // Retain completed chunks; only the last line can change during an append.
            foreach (var chunk in previous.Chunks)
            {
                if (chunk[^1].LineNumber < start)
                {
                    chunks.Add(chunk);
                }
                else
                {
                    pending.AddRange(chunk.TakeWhile(match => match.LineNumber < start));
                }
            }

            for (var batchStart = start; batchStart < lineCount;)
            {
                scanCancellation.Token.ThrowIfCancellationRequested();
                var batchEnd = (int)Math.Min((long)batchStart + READ_BATCH_SIZE, lineCount);
                // Pin before reading and release after this small batch, never for the whole file.
                using var pin = (reader as IBufferPinning)?.PinRange(batchStart, batchEnd - 1);
                for (var lineNumber = batchStart; lineNumber < batchEnd; lineNumber++)
                {
                    scanCancellation.Token.ThrowIfCancellationRequested();
                    var line = reader.GetLogLineMemory(lineNumber)
                        ?? throw new IOException();
                    var match = criteria.Match(lineNumber, line, columns, scanCancellation.Token);
                    if (match.HasValue)
                    {
                        pending.Add(match.Value);
                        if (pending.Count >= MATCH_CHUNK_SIZE)
                        {
                            chunks.Add(pending.ToArray());
                            pending.Clear();
                        }
                    }

                    scanned = lineNumber + 1;
                }

                batchStart = batchEnd;
            }
        }
        catch (OperationCanceledException) when (scanCancellation.IsCancellationRequested)
        {
            // Reset/disposal owns invalidation; the generation check below rejects this result.
        }
        catch (Exception exception)
        {
            // Background boundary: report regex, reader and columnizer failures as an outcome.
            error = exception;
        }
        finally
        {
            if (pending.Count > 0)
            {
                chunks.Add(pending.ToArray());
            }

            lock (_gate)
            {
                if (!_disposed && generation == _generation)
                {
                    _snapshot = new MarkerSnapshot(chunks.ToArray(), scanned, error);
                    _work = null;
                    _scanCts = null;
                }
            }
        }
    }
}