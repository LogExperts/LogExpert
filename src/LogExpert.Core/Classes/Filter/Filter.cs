using LogExpert.Core.Callback;

using NLog;

namespace LogExpert.Core.Classes.Filter;

internal delegate void FilterFx (FilterParams filterParams, List<int> filterResultLines, List<int> lastFilterResultLines, List<int> filterHitList);

internal class Filter
{
    #region Fields

    private const int PROGRESS_BAR_MODULO = 1000;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly ColumnizerCallback _callback;

    #endregion

    #region cTor

    //TODO Is the callback needed? (https://github.com/LogExperts/LogExpert/issues/401)
    public Filter (ColumnizerCallback callback)
    {
        _callback = callback;
        FilterResultLines = [];
        LastFilterLinesList = [];
        FilterHitList = [];
    }

    #endregion

    #region Properties

    public List<int> FilterResultLines { get; }
    public List<int> LastFilterLinesList { get; }
    public List<int> FilterHitList { get; }

    #endregion

    #region Public methods

    public int DoFilter (FilterParams filterParams, int startLine, int maxCount, ProgressCallback progressCallback, CancellationToken cancellationToken)
    {
        return DoFilter(filterParams, startLine, maxCount, FilterResultLines, LastFilterLinesList, FilterHitList, progressCallback, cancellationToken);
    }

    #endregion

    #region Private Methods

    private int DoFilter (FilterParams filterParams, int startLine, int maxCount, List<int> filterResultLines, List<int> lastFilterLinesList, List<int> filterHitList, ProgressCallback progressCallback, CancellationToken cancellationToken)
    {
        var lineNum = startLine;
        var count = 0;
        var callbackCounter = 0;

        try
        {
            filterParams.Reset();

            while ((count++ < maxCount || filterParams.IsInRange) && !cancellationToken.IsCancellationRequested)
            {
                if (lineNum >= _callback.GetLineCount())
                {
                    return count;
                }

                var line = _callback.GetLogLineMemory(lineNum);

                if (line == null)
                {
                    return count;
                }

                _callback.SetLineNum(lineNum);

                if (Util.TestFilterCondition(filterParams, line, _callback))
                {
                    AddFilterLine(lineNum, filterParams, filterResultLines, lastFilterLinesList, filterHitList);
                }

                lineNum++;
                callbackCounter++;

                if (lineNum % PROGRESS_BAR_MODULO == 0)
                {
                    progressCallback(callbackCounter);
                    callbackCounter = 0;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while filtering. Please report to developer");
            throw;
            //TODO: This information should be handled from the LogExpert project and not from LogExpert.Core.
            //MessageBox.Show(null,
            //    "Exception while filtering. Please report to developer: \n\n" + ex + "\n\n" + ex.StackTrace,
            //    "LogExpert");
        }

        return count;
    }

    private void AddFilterLine (int lineNum, FilterParams filterParams, List<int> filterResultLines, List<int> lastFilterLinesList, List<int> filterHitList)
    {
        filterHitList.Add(lineNum);
        var filterResult = FilterSpread.Expand(lineNum, filterParams.SpreadBefore, filterParams.SpreadBehind, _callback.GetLineCount(), lastFilterLinesList);

        filterResultLines.AddRange(filterResult);

        lastFilterLinesList.AddRange(filterResult);

        FilterSpread.TrimHistory(lastFilterLinesList);
    }

    #endregion
}