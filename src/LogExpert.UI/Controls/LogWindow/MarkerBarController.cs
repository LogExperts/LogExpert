using System.Globalization;

using ColumnizerLib;

using LogExpert.Core.Callback;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Marker;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArguments;
using LogExpert.Core.Interfaces;

using NLog;

namespace LogExpert.UI.Controls.LogWindow;

/// <summary>What a <see cref="MarkerBarController"/> reads from, and reports to, its Log Window.</summary>
internal interface IMarkerBarHost
{
    ILogfileReader? Reader { get; }

    string FileName { get; }

    ILogLineMemoryColumnizer Columnizer { get; }

    string ConfigDir { get; }

    /// <summary>Loading or dead file: the bar shows nothing.</summary>
    bool IsContentUnavailable { get; }

    bool IsClosed { get; }

    int ColumnHeaderHeight { get; }

    /// <summary>The line visibility tracker's line count; clicks beyond it are ignored.</summary>
    int NavigableLineCount { get; }

    /// <summary>Taken under the window's highlight-group lock.</summary>
    MarkerCriteria GetHighlightCriteria ();

    /// <summary>Called on the UI thread.</summary>
    int[] GetBookmarkLineNumbers ();

    /// <summary>Called on a worker thread.</summary>
    int[] GetFilterHits ();

    void StatusLineError (string text);

    void RequestGotoLine (int targetLine);
}

[Flags]
internal enum MarkerScanSource
{
    None = 0,
    Highlights = 1,
    Search = 2,
    All = Highlights | Search
}

/// <summary>
/// Coordinates a Log Window's <see cref="MarkerBar"/>: configures the highlight and Log Search <see cref="MarkerIndex"/>es,
/// and every 250 ms takes their snapshots and renders a frame unless the file, criteria or data changed meanwhile.
/// UI thread only, except <see cref="InvalidateCriteria"/>, <see cref="MarkDataChanged"/>, <see cref="MarkContentChanged"/>,
/// <see cref="BeginRollover"/> and <see cref="EndRollover"/>.
/// </summary>
internal sealed class MarkerBarController : IDisposable
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly IMarkerBarHost _host;
    private readonly MarkerIndex _highlightMarkers = new();
    private readonly MarkerIndex _searchMarkers = new();
    private readonly Lock _stateLock = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 250 };
    private SearchParams? _search;
    private bool _searchMarkersCleared;
    private (bool Bar, bool Highlights, bool Bookmarks, bool Search, bool Filter) _visibility;
    private MarkerScanSource _rebuild = MarkerScanSource.All;
    // Criteria/file generation, rendered data changes, and edits to the unfinished last line are distinct.
    private int _generation;
    private int _revision;
    private int _contentRevision;
    private int _highlightContentRevision = -1;
    private int _searchContentRevision = -1;
    private int _rolloverPending;
    private MarkerFrame? _frame;
    private MarkerSnapshot? _reportedHighlightError;
    private MarkerSnapshot? _reportedSearchError;
    private bool _rendering;
    private bool _disposed;

    public MarkerBarController (IMarkerBarHost host)
    {
        _host = host;
        Bar.LineSelected += OnLineSelected;
        Bar.ClearSearchRequested += OnClearSearchRequested;
        _timer.Tick += OnTimerTick;
    }

    public MarkerBar Bar { get; } = new() { Name = "markerBar" };

    public void Start ()
    {
        _timer.Start();
    }

    public void ApplyPreferences (Preferences preferences, Color backColor, Color foreColor)
    {
        var visibility = (preferences.ShowMarkerBar, preferences.ShowHighlightMarkers,
            preferences.ShowBookmarkMarkers, preferences.ShowSearchMarkers, preferences.ShowFilterMarkers);
        if (_visibility != visibility)
        {
            var sources = _visibility.Bar != visibility.ShowMarkerBar ? MarkerScanSource.All
                : (_visibility.Highlights != visibility.ShowHighlightMarkers ? MarkerScanSource.Highlights : MarkerScanSource.None)
                  | (_visibility.Search != visibility.ShowSearchMarkers ? MarkerScanSource.Search : MarkerScanSource.None);
            _visibility = visibility;
            InvalidateCriteria(sources);
            Bar.ClearBuckets();
        }

        Bar.Visible = visibility.ShowMarkerBar;
        Bar.BackColor = backColor;
        Bar.ForeColor = foreColor;
    }

    // May be called by the reader or tail worker: invalidate before any queued UI work.
    public void InvalidateCriteria (MarkerScanSource sources)
    {
        lock (_stateLock)
        {
            Interlocked.Increment(ref _generation);
            if ((sources & MarkerScanSource.Highlights) != 0)
            {
                _highlightMarkers.Reset(null, null);
            }

            if ((sources & MarkerScanSource.Search) != 0)
            {
                _searchMarkers.Reset(null, null);
            }

            _rebuild |= sources;
        }
        MarkDataChanged();
    }

    /// <summary>Invalidates every source and blanks the bar until the next frame.</summary>
    public void Clear ()
    {
        InvalidateCriteria(MarkerScanSource.All);
        Bar.ClearBuckets();
    }

    public void MarkDataChanged ()
    {
        Interlocked.Increment(ref _revision);
    }

    /// <summary>The line count changed: the unfinished last line may have been edited.</summary>
    public void MarkContentChanged ()
    {
        Interlocked.Increment(ref _contentRevision);
        MarkDataChanged();
    }

    /// <summary>A rollover or truncation is pending; the bar stays blank until the matching <see cref="EndRollover"/>.</summary>
    public void BeginRollover ()
    {
        Interlocked.Increment(ref _rolloverPending);
        InvalidateCriteria(MarkerScanSource.All);
    }

    public void EndRollover ()
    {
        Interlocked.Decrement(ref _rolloverPending);
    }

    public void TrackSearch (SearchParams search)
    {
        var sameCriteria = _search != null
            && _search.SearchText == search.SearchText
            && _search.IsRegex == search.IsRegex
            && _search.IsCaseSensitive == search.IsCaseSensitive;
        if (search.IsFindNext && sameCriteria)
        {
            return;
        }

        _search = new SearchParams();
        _search.CopyFrom(search);
        _searchMarkersCleared = false;
        InvalidateCriteria(MarkerScanSource.Search);
        Bar.ClearBuckets();
    }

    /// <summary>One timer tick: rebuilds invalidated indexes, advances their scans and renders a frame if anything changed.</summary>
    internal async Task UpdateAsync ()
    {
        if (_disposed || _host.IsClosed || _rendering)
        {
            return;
        }

        if (_host.IsContentUnavailable || Volatile.Read(ref _rolloverPending) != 0)
        {
            Bar.ClearBuckets();
            return;
        }

        _rendering = true;
        try
        {
            ConfigureIndexes();
            if (!_visibility.Bar || _host.Reader == null)
            {
                return;
            }

            Bar.TopInset = _host.ColumnHeaderHeight;
            Bar.BottomInset = SystemInformation.HorizontalScrollBarHeight;
            var lineCount = _host.Reader.LineCount;
            var contentRevision = Volatile.Read(ref _contentRevision);
            if (!_highlightMarkers.IsScanning)
            {
                _ = _highlightMarkers.UpdateAsync(lineCount, contentRevision != _highlightContentRevision);
                _highlightContentRevision = contentRevision;
            }

            if (!_searchMarkers.IsScanning)
            {
                _ = _searchMarkers.UpdateAsync(lineCount, contentRevision != _searchContentRevision);
                _searchContentRevision = contentRevision;
            }

            MarkerSnapshot highlights;
            MarkerSnapshot search;
            int generation;
            lock (_stateLock)
            {
                // A snapshot and its file/criteria generation must be captured atomically with invalidation.
                generation = _generation;
                highlights = _highlightMarkers.Snapshot;
                search = _searchMarkers.Snapshot;
            }
            var discovering = _highlightMarkers.IsScanning || _searchMarkers.IsScanning;
            Bar.SetDiscovering(discovering);
            ReportError(highlights, ref _reportedHighlightError);
            ReportError(search, ref _reportedSearchError);
            var height = Bar.BucketHeight;
            var revision = Volatile.Read(ref _revision);
            var foregroundArgb = Bar.ForeColor.ToArgb();
            var frame = new MarkerFrame(generation, revision, lineCount, height, foregroundArgb, highlights, search);
            if (_frame == frame)
            {
                return;
            }

            var visibility = _visibility;
            // Capture bookmarks on the UI thread: legacy CSV import also writes the provider there.
            var bookmarks = visibility.Bookmarks ? _host.GetBookmarkLineNumbers() : [];
            // Copy only existing hit numbers under short locks. Pixel aggregation never runs on the UI thread.
            var buckets = await Task.Run(() =>
            {
                var filterHits = visibility.Filter ? _host.GetFilterHits() : [];

                return new Dictionary<MarkerCategory, IReadOnlyList<MarkerBucket>>
                {
                    [MarkerCategory.Highlights] = MarkerBucket.Aggregate(highlights.Matches, lineCount, height, foregroundArgb),
                    [MarkerCategory.Bookmarks] = MarkerBucket.Aggregate(bookmarks.Select(line => new MarkerLine(line, Color.OrangeRed.ToArgb())), lineCount, height, foregroundArgb),
                    [MarkerCategory.Search] = MarkerBucket.Aggregate(search.Matches, lineCount, height, foregroundArgb),
                    [MarkerCategory.Filter] = MarkerBucket.Aggregate(filterHits.Order().Select(line => new MarkerLine(line, Color.MediumSeaGreen.ToArgb())), lineCount, height, foregroundArgb)
                };
            }).ConfigureAwait(true);
            lock (_stateLock)
            {
                if (_disposed || _host.IsClosed || generation != _generation
                    || Volatile.Read(ref _rolloverPending) != 0 || _rebuild != MarkerScanSource.None
                    || foregroundArgb != Bar.ForeColor.ToArgb()
                    || revision != Volatile.Read(ref _revision))
                {
                    return;
                }

                _frame = frame;
                Bar.SetBuckets(buckets, height, discovering);
            }
        }
        catch (Exception exception)
        {
            if (!_disposed && !_host.IsClosed)
            {
                _logger.Warn(exception, "Marker update failed");
                _host.StatusLineError(string.Format(CultureInfo.CurrentCulture, Resources.MarkerBar_ScanFailed, exception.Message));
            }
        }
        finally
        {
            _rendering = false;
        }
    }

    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
        _highlightMarkers.Dispose();
        _searchMarkers.Dispose();
        _frame = null;
        _reportedHighlightError = null;
        _reportedSearchError = null;
        if (!Bar.IsDisposed)
        {
            Bar.ClearBuckets();
        }
    }

    private void OnClearSearchRequested (object? sender, EventArgs eventArgs)
    {
        _searchMarkersCleared = true;
        InvalidateCriteria(MarkerScanSource.Search);
        Bar.ClearBuckets();
    }

    private void ConfigureIndexes ()
    {
        MarkerScanSource sources;
        int generation;
        lock (_stateLock)
        {
            sources = _rebuild;
            _rebuild = MarkerScanSource.None;
            generation = _generation;
        }
        if (sources == MarkerScanSource.None)
        {
            return;
        }

        var reader = _host.Reader;
        MarkerCriteria? highlightCriteria = null;
        Func<int, ILogLineMemory, IReadOnlyList<ITextValueMemory>>? columns = null;
        if ((sources & MarkerScanSource.Highlights) != 0 && _visibility.Bar && _visibility.Highlights && reader != null)
        {
            highlightCriteria = _host.GetHighlightCriteria();
            if (!highlightCriteria.IsEmpty)
            {
                columns = CaptureColumns(reader);
            }
        }

        var searchCriteria = _visibility.Bar && _visibility.Search && !_searchMarkersCleared && _search != null
            ? MarkerCriteria.ForSearch(_search, Color.DodgerBlue.ToArgb()) : null;
        lock (_stateLock)
        {
            if (generation != _generation)
            {
                _rebuild |= sources;
                return;
            }

            if ((sources & MarkerScanSource.Highlights) != 0)
            {
                _highlightMarkers.Reset(reader, highlightCriteria, columns);
                _highlightContentRevision = -1;
                _reportedHighlightError = null;
            }

            if ((sources & MarkerScanSource.Search) != 0)
            {
                _searchMarkers.Reset(reader, searchCriteria);
                _searchContentRevision = -1;
                _reportedSearchError = null;
            }
        }
    }

    private Func<int, ILogLineMemory, IReadOnlyList<ITextValueMemory>> CaptureColumns (ILogfileReader reader)
    {
        var template = _host.Columnizer;
        var snapshot = (template as IColumnizerSnapshotMemory)?.CreateSnapshot();
        var directory = _host.ConfigDir;
        var offset = template.IsTimeshiftImplemented() ? template.GetTimeOffset() : 0;
        var callback = new ColumnizerCallback(new MarkerLineSource(reader, _host.FileName));
        var parser = new Lazy<ILogLineMemoryColumnizer>(() =>
        {
            // Initialization may read file headers. Run it on the discovery worker, never the UI thread.
            var workerColumnizer = snapshot ?? ColumnizerPicker.CloneMemoryColumnizer(template, directory)
                ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    Resources.Columnizer_SnapshotUnavailable, template.GetName()));
            if (snapshot == null)
            {
                (workerColumnizer as IInitColumnizerMemory)?.Selected(callback);
            }

            if (workerColumnizer.IsTimeshiftImplemented())
            {
                workerColumnizer.SetTimeOffset(offset);
            }

            return workerColumnizer;
        });
        return (lineNumber, line) =>
        {
            callback.LineNum = lineNumber;
            return parser.Value.SplitLine(callback, line).ColumnValues
                .Select(column => (ITextValueMemory)new LogLine(column.Text.ToString(), lineNumber)).ToArray();
        };
    }

    private async void OnTimerTick (object? sender, EventArgs eventArgs)
    {
        await UpdateAsync().ConfigureAwait(true);
    }

    private void ReportError (MarkerSnapshot snapshot, ref MarkerSnapshot? reported)
    {
        if (snapshot.Error != null && !ReferenceEquals(snapshot, reported))
        {
            reported = snapshot;
            _logger.Warn(snapshot.Error, "Marker discovery failed");
            _host.StatusLineError(string.Format(CultureInfo.CurrentCulture, Resources.MarkerBar_ScanFailed, snapshot.Error.Message));
        }
    }

    private void OnLineSelected (object? sender, SelectLineEventArgs eventArgs)
    {
        // The tracker's map, not the displayed one: that is empty during the first scan, when clicks must be queued.
        if (_frame?.Generation == Volatile.Read(ref _generation)
            && eventArgs.Line >= 0 && eventArgs.Line < _host.NavigableLineCount)
        {
            _host.RequestGotoLine(eventArgs.Line + 1);
        }
    }

    private sealed record MarkerFrame (int Generation, int Revision, int LineCount, int Height, int ForegroundArgb,
        MarkerSnapshot Highlights, MarkerSnapshot Search);

    private sealed class MarkerLineSource (ILogfileReader reader, string fileName) : ILogLineSource
    {
        public int LineCount => reader.LineCount;

        public ILogLineMemory GetLineMemory (int lineNum)
        {
            return reader.GetLogLineMemory(lineNum);
        }

        public string GetCurrentFileName (int lineNum)
        {
            return reader is IMultiFileNavigation navigation ? navigation.GetLogFileNameForLine(lineNum) : fileName;
        }
    }
}
