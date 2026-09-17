using System.Globalization;

using ColumnizerLib;

using LogExpert.Core.Callback;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Marker;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArguments;
using LogExpert.Core.Interfaces;

namespace LogExpert.UI.Controls.LogWindow;

internal partial class LogWindow
{
    private readonly MarkerIndex _highlightMarkers = new();
    private readonly MarkerIndex _searchMarkers = new();
    private readonly Lock _markerStateLock = new();
    private readonly MarkerBar _markerBar = new() { Name = "markerBar" };
    private readonly System.Windows.Forms.Timer _markerTimer = new() { Interval = 250 };
    private SearchParams? _markerSearch;
    private bool _searchMarkersCleared;
    private (bool Bar, bool Highlights, bool Bookmarks, bool Search, bool Filter) _markerVisibility;
    private MarkerScanSource _markerRebuild = MarkerScanSource.All;
    // Criteria/file generation, rendered data changes, and edits to the unfinished last line are distinct.
    private int _markerGeneration;
    private int _markerRevision;
    private int _markerContentRevision;
    private int _markerHighlightContentRevision = -1;
    private int _markerSearchContentRevision = -1;
    private int _markerTailPending;
    private MarkerFrame? _markerFrame;
    private MarkerSnapshot? _markerReportedHighlightError;
    private MarkerSnapshot? _markerReportedSearchError;
    private bool _markerRendering;
    private bool _markersDisposed;

    private void InitializeMarkerBar ()
    {
        tableLayoutPanel1.ColumnCount = 3;
        _ = tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
        tableLayoutPanel1.Controls.Add(_markerBar, 2, 1);
        _markerBar.LineSelected += OnMarkerLineSelected;
        _markerBar.ClearSearchRequested += OnClearSearchMarkers;
        _markerTimer.Tick += OnMarkerTimerTick;
        DpiChanged += (_, _) => ApplyMarkerPreferences();
        ApplyMarkerPreferences();
        _markerTimer.Start();
    }

    private void ApplyMarkerPreferences ()
    {
        var visibility = (Preferences.ShowMarkerBar, Preferences.ShowHighlightMarkers,
            Preferences.ShowBookmarkMarkers, Preferences.ShowSearchMarkers, Preferences.ShowFilterMarkers);
        if (_markerVisibility != visibility)
        {
            var sources = _markerVisibility.Bar != visibility.ShowMarkerBar ? MarkerScanSource.All
                : (_markerVisibility.Highlights != visibility.ShowHighlightMarkers ? MarkerScanSource.Highlights : MarkerScanSource.None)
                  | (_markerVisibility.Search != visibility.ShowSearchMarkers ? MarkerScanSource.Search : MarkerScanSource.None);
            _markerVisibility = visibility;
            InvalidateMarkerCriteria(sources);
            _markerBar.ClearBuckets();
        }

        tableLayoutPanel1.ColumnStyles[2].Width = visibility.ShowMarkerBar ? (int)Math.Round(28d * DeviceDpi / 96d) : 0;
        _markerBar.Visible = visibility.ShowMarkerBar;
        _markerBar.BackColor = dataGridView.BackgroundColor;
        _markerBar.ForeColor = dataGridView.ForeColor;
    }

    // May be called by the reader or tail worker: invalidate before any queued UI work.
    private void InvalidateMarkerCriteria (MarkerScanSource sources)
    {
        lock (_markerStateLock)
        {
            Interlocked.Increment(ref _markerGeneration);
            if ((sources & MarkerScanSource.Highlights) != 0)
            {
                _highlightMarkers.Reset(null, null);
            }

            if ((sources & MarkerScanSource.Search) != 0)
            {
                _searchMarkers.Reset(null, null);
            }

            _markerRebuild |= sources;
        }
        MarkMarkerDataChanged();
    }

    private void MarkMarkerDataChanged ()
    {
        Interlocked.Increment(ref _markerRevision);
    }

    private void TrackMarkerSearch (SearchParams search)
    {
        var sameCriteria = _markerSearch != null
            && _markerSearch.SearchText == search.SearchText
            && _markerSearch.IsRegex == search.IsRegex
            && _markerSearch.IsCaseSensitive == search.IsCaseSensitive;
        if (search.IsFindNext && sameCriteria)
        {
            return;
        }

        _markerSearch = new SearchParams();
        _markerSearch.CopyFrom(search);
        _searchMarkersCleared = false;
        InvalidateMarkerCriteria(MarkerScanSource.Search);
        _markerBar.ClearBuckets();
    }

    private void OnClearSearchMarkers (object? sender, EventArgs eventArgs)
    {
        _searchMarkersCleared = true;
        InvalidateMarkerCriteria(MarkerScanSource.Search);
        _markerBar.ClearBuckets();
    }

    private void ConfigureMarkerIndexes ()
    {
        MarkerScanSource sources;
        int generation;
        lock (_markerStateLock)
        {
            sources = _markerRebuild;
            _markerRebuild = MarkerScanSource.None;
            generation = _markerGeneration;
        }
        if (sources == MarkerScanSource.None)
        {
            return;
        }

        var reader = _logFileReader;
        MarkerCriteria? highlightCriteria = null;
        Func<int, ILogLineMemory, IReadOnlyList<ITextValueMemory>>? columns = null;
        if ((sources & MarkerScanSource.Highlights) != 0 && _markerVisibility.Bar && _markerVisibility.Highlights && reader != null)
        {
            lock (_currentHighlightGroupLock)
            {
                highlightCriteria = MarkerCriteria.ForHighlights(_currentHighlightGroup.HighlightEntryList);
            }

            if (!highlightCriteria.IsEmpty)
            {
                columns = CaptureMarkerColumns(reader);
            }
        }

        var searchCriteria = _markerVisibility.Bar && _markerVisibility.Search && !_searchMarkersCleared && _markerSearch != null
            ? MarkerCriteria.ForSearch(_markerSearch, Color.DodgerBlue.ToArgb()) : null;
        lock (_markerStateLock)
        {
            if (generation != _markerGeneration)
            {
                _markerRebuild |= sources;
                return;
            }

            if ((sources & MarkerScanSource.Highlights) != 0)
            {
                _highlightMarkers.Reset(reader, highlightCriteria, columns);
                _markerHighlightContentRevision = -1;
                _markerReportedHighlightError = null;
            }

            if ((sources & MarkerScanSource.Search) != 0)
            {
                _searchMarkers.Reset(reader, searchCriteria);
                _markerSearchContentRevision = -1;
                _markerReportedSearchError = null;
            }
        }
    }

    private Func<int, ILogLineMemory, IReadOnlyList<ITextValueMemory>> CaptureMarkerColumns (ILogfileReader reader)
    {
        var template = CurrentColumnizer;
        var snapshot = (template as IColumnizerSnapshotMemory)?.CreateSnapshot();
        var directory = ConfigManager.ActiveConfigDir;
        var offset = template.IsTimeshiftImplemented() ? template.GetTimeOffset() : 0;
        var callback = new ColumnizerCallback(new MarkerLineSource(reader, FileName));
        var parser = new Lazy<ILogLineMemoryColumnizer>(() =>
        {
            // Initialization may read file headers. Run it on the discovery worker, never the UI thread.
            var clone = snapshot ?? ColumnizerPicker.CloneMemoryColumnizer(template, directory)
                ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    Resources.Columnizer_SnapshotUnavailable, template.GetName()));
            if (snapshot == null)
            {
                (clone as IInitColumnizerMemory)?.Selected(callback);
            }

            if (clone.IsTimeshiftImplemented())
            {
                clone.SetTimeOffset(offset);
            }

            return clone;
        });
        return (lineNumber, line) =>
        {
            callback.LineNum = lineNumber;
            return parser.Value.SplitLine(callback, line).ColumnValues
                .Select(column => (ITextValueMemory)new LogLine(column.Text.ToString(), lineNumber)).ToArray();
        };
    }

    private async void OnMarkerTimerTick (object? sender, EventArgs eventArgs)
    {
        if (_markersDisposed || _isClosing || IsDisposed || _markerRendering)
        {
            return;
        }

        if (_isLoading || _isDeadFile || Volatile.Read(ref _markerTailPending) != 0)
        {
            _markerBar.ClearBuckets();
            return;
        }

        _markerRendering = true;
        try
        {
            ConfigureMarkerIndexes();
            if (!_markerVisibility.Bar || _logFileReader == null)
            {
                return;
            }

            _markerBar.TopInset = dataGridView.ColumnHeadersVisible ? dataGridView.ColumnHeadersHeight : 0;
            _markerBar.BottomInset = SystemInformation.HorizontalScrollBarHeight;
            var lineCount = _logFileReader.LineCount;
            var contentRevision = Volatile.Read(ref _markerContentRevision);
            if (!_highlightMarkers.IsScanning)
            {
                _ = _highlightMarkers.UpdateAsync(lineCount, contentRevision != _markerHighlightContentRevision);
                _markerHighlightContentRevision = contentRevision;
            }

            if (!_searchMarkers.IsScanning)
            {
                _ = _searchMarkers.UpdateAsync(lineCount, contentRevision != _markerSearchContentRevision);
                _markerSearchContentRevision = contentRevision;
            }

            MarkerSnapshot highlights;
            MarkerSnapshot search;
            int generation;
            lock (_markerStateLock)
            {
                // A snapshot and its file/criteria generation must be captured atomically with invalidation.
                generation = _markerGeneration;
                highlights = _highlightMarkers.Snapshot;
                search = _searchMarkers.Snapshot;
            }
            var discovering = _highlightMarkers.IsScanning || _searchMarkers.IsScanning;
            _markerBar.SetDiscovering(discovering);
            ReportMarkerError(highlights, ref _markerReportedHighlightError);
            ReportMarkerError(search, ref _markerReportedSearchError);
            var height = _markerBar.BucketHeight;
            var revision = Volatile.Read(ref _markerRevision);
            var foregroundArgb = _markerBar.ForeColor.ToArgb();
            var frame = new MarkerFrame(generation, revision, lineCount, height, foregroundArgb, highlights, search);
            if (_markerFrame == frame)
            {
                return;
            }

            var visibility = _markerVisibility;
            // Capture bookmarks on the UI thread: legacy CSV import also writes the provider there.
            var bookmarks = visibility.Bookmarks ? _bookmarkProvider.GetBookmarkLineNumbers() : [];
            // Copy only existing hit numbers under short locks. Pixel aggregation never runs on the UI thread.
            var buckets = await Task.Run(() =>
            {
                int[] filterHits;
                lock (_filterHitLock)
                {
                    filterHits = visibility.Filter ? _filterHitList.ToArray() : [];
                }

                return new Dictionary<MarkerCategory, IReadOnlyList<MarkerBucket>>
                {
                    [MarkerCategory.Highlights] = MarkerBucket.Aggregate(highlights.Matches, lineCount, height, foregroundArgb),
                    [MarkerCategory.Bookmarks] = MarkerBucket.Aggregate(bookmarks.Select(line => new MarkerLine(line, Color.OrangeRed.ToArgb())), lineCount, height, foregroundArgb),
                    [MarkerCategory.Search] = MarkerBucket.Aggregate(search.Matches, lineCount, height, foregroundArgb),
                    [MarkerCategory.Filter] = MarkerBucket.Aggregate(filterHits.Order().Select(line => new MarkerLine(line, Color.MediumSeaGreen.ToArgb())), lineCount, height, foregroundArgb)
                };
            }).ConfigureAwait(true);
            lock (_markerStateLock)
            {
                if (_markersDisposed || _isClosing || IsDisposed || generation != _markerGeneration
                    || Volatile.Read(ref _markerTailPending) != 0 || _markerRebuild != MarkerScanSource.None
                    || foregroundArgb != _markerBar.ForeColor.ToArgb()
                    || revision != Volatile.Read(ref _markerRevision))
                {
                    return;
                }

                _markerFrame = frame;
                _markerBar.SetBuckets(buckets, height, discovering);
            }
        }
        catch (Exception exception)
        {
            if (!_markersDisposed && !_isClosing && !IsDisposed)
            {
                _logger.Warn(exception, "Marker update failed");
                StatusLineError(string.Format(CultureInfo.CurrentCulture, Resources.MarkerBar_ScanFailed, exception.Message));
            }
        }
        finally
        {
            _markerRendering = false;
        }
    }

    private void ReportMarkerError (MarkerSnapshot snapshot, ref MarkerSnapshot? reported)
    {
        if (snapshot.Error != null && !ReferenceEquals(snapshot, reported))
        {
            reported = snapshot;
            _logger.Warn(snapshot.Error, "Marker discovery failed");
            StatusLineError(string.Format(CultureInfo.CurrentCulture, Resources.MarkerBar_ScanFailed, snapshot.Error.Message));
        }
    }

    private void OnMarkerLineSelected (object? sender, SelectLineEventArgs eventArgs)
    {
        if (_markerFrame?.Generation == Volatile.Read(ref _markerGeneration)
            && eventArgs.Line >= 0 && eventArgs.Line < dataGridView.RowCount)
        {
            RequestGotoLine(eventArgs.Line + 1);
        }
    }

    private void DisposeMarkers ()
    {
        if (_markersDisposed)
        {
            return;
        }

        _markersDisposed = true;
        _markerTimer.Stop();
        _markerTimer.Dispose();
        _highlightMarkers.Dispose();
        _searchMarkers.Dispose();
        _markerFrame = null;
        _markerReportedHighlightError = null;
        _markerReportedSearchError = null;
        if (!_markerBar.IsDisposed)
        {
            _markerBar.ClearBuckets();
        }
    }

    [Flags]
    private enum MarkerScanSource
    {
        None = 0,
        Highlights = 1,
        Search = 2,
        All = Highlights | Search
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