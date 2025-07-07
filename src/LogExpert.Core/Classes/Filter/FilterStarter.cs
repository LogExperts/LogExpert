using System.Globalization;

using LogExpert.Core.Callback;

using NLog;

namespace LogExpert.Core.Classes.Filter;

public delegate void ProgressCallback (int lineCount);

public class FilterStarter
{
    #region Fields

    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    private readonly ColumnizerCallback _callback;
    private readonly SortedDictionary<int, int> _filterHitDict;
    private readonly List<Filter> _filterReadyList;
    private readonly SortedDictionary<int, int> _filterResultDict;

    private readonly List<Filter> _filterWorkerList;

    private readonly SortedDictionary<int, int> _lastFilterLinesDict;

    private ProgressCallback _progressCallback;
    private int _progressLineCount;
    private bool _shouldStop;

    #endregion

    #region cTor

    public FilterStarter (ColumnizerCallback callback, int minThreads)
    {
        _callback = callback;
        FilterResultLines = [];
        LastFilterLinesList = [];
        FilterHitList = [];
        _filterReadyList = [];
        _filterWorkerList = [];
        _filterHitDict = [];
        _filterResultDict = [];
        _lastFilterLinesDict = [];
        ThreadCount = Environment.ProcessorCount * 4;
        ThreadCount = minThreads;
        ThreadPool.GetMinThreads(out _, out var completion);
        ThreadPool.SetMinThreads(minThreads, completion);
        ThreadPool.GetMaxThreads(out _, out _);
    }

    #endregion

    #region Properties

    public List<int> FilterResultLines { get; set; }

    public List<int> LastFilterLinesList { get; set; }

    public List<int> FilterHitList { get; set; }

    public int ThreadCount { get; set; }

    #endregion

    #region Public methods

    public async void DoFilter (FilterParams filterParams, int startLine, int maxCount, ProgressCallback progressCallback)
    {
        FilterResultLines.Clear();
        LastFilterLinesList.Clear();
        FilterHitList.Clear();
        _filterHitDict.Clear();
        _filterReadyList.Clear();
        _filterResultDict.Clear();
        _lastFilterLinesDict.Clear();
        _filterWorkerList.Clear();
        _shouldStop = false;

        var interval = maxCount / ThreadCount;

        if (interval < 1)
        {
            interval = 1;
        }

        var workStartLine = startLine;
        _progressLineCount = 0;
        _progressCallback = progressCallback;
        while (workStartLine < startLine + maxCount)
        {
            if (workStartLine + interval > maxCount)
            {
                interval = maxCount - workStartLine;
                if (interval == 0)
                {
                    break;
                }
            }
            _logger.Info(CultureInfo.InvariantCulture, "FilterStarter starts worker for line {0}, lineCount {1}", workStartLine, interval);

            var filter = await Task.Run(() => DoWork(filterParams, workStartLine, interval, ThreadProgressCallback)).ConfigureAwait(false);
            FilterDoneCallback(filter);
            workStartLine += interval;
        }

        MergeResults();
    }

    /// <summary>
    /// Requests the FilterStarter to stop all filter threads. Call this from another thread (e.g. GUI). The function returns
    /// immediately without waiting for filter end.
    /// </summary>
    public void CancelFilter ()
    {
        _shouldStop = true;
        lock (_filterWorkerList)
        {
            _logger.Info(CultureInfo.InvariantCulture, "Filter cancel requested. Stopping all {0} threads.", _filterWorkerList.Count);
            foreach (var filter in _filterWorkerList)
            {
                filter.ShouldCancel = true;
            }
        }
    }

    #endregion

    #region Private Methods

    private void ThreadProgressCallback (int lineCount)
    {
        var count = Interlocked.Add(ref _progressLineCount, lineCount);
        _progressCallback(count);
    }

    private Filter DoWork (FilterParams filterParams, int startLine, int maxCount, ProgressCallback progressCallback)
    {
        _logger.Info(CultureInfo.InvariantCulture, "Started Filter worker [{0}] for line {1}", Environment.CurrentManagedThreadId, startLine);

        // Give every thread own copies of ColumnizerCallback and FilterParams, because the state of the objects changes while filtering
        var threadFilterParams = filterParams.CloneWithCurrentColumnizer();
        Filter filter = new((ColumnizerCallback)_callback.Clone());
        lock (_filterWorkerList)
        {
            _filterWorkerList.Add(filter);
        }

        if (!_shouldStop)
        {

            _ = filter.DoFilter(threadFilterParams, startLine, maxCount, progressCallback);
            _logger.Info(CultureInfo.InvariantCulture, "Filter worker [{0}] for line {1} has completed.", Environment.CurrentManagedThreadId, startLine);

            lock (_filterReadyList)
            {
                _filterReadyList.Add(filter);
            }
        }

        return filter;
    }

    private void FilterDoneCallback (Filter filter)
    {
        lock (_filterReadyList)
        {
            _filterReadyList.Add(filter);
        }
    }

    private void MergeResults ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "Merging filter results.");
        foreach (var filter in _filterReadyList)
        {
            foreach (var lineNum in filter.FilterHitList)
            {
                if (!_filterHitDict.ContainsKey(lineNum))
                {
                    _filterHitDict.Add(lineNum, lineNum);
                }
            }

            foreach (var lineNum in filter.FilterResultLines)
            {
                if (!_filterResultDict.ContainsKey(lineNum))
                {
                    _filterResultDict.Add(lineNum, lineNum);
                }
            }

            foreach (var lineNum in filter.LastFilterLinesList)
            {
                if (!_lastFilterLinesDict.ContainsKey(lineNum))
                {
                    _lastFilterLinesDict.Add(lineNum, lineNum);
                }
            }
        }

        FilterHitList.AddRange(_filterHitDict.Keys);
        FilterResultLines.AddRange(_filterResultDict.Keys);
        LastFilterLinesList.AddRange(_lastFilterLinesDict.Keys);
        _logger.Info(CultureInfo.InvariantCulture, "Merging done.");
    }

    #endregion
}