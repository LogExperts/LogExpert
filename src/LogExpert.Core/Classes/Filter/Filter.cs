using LogExpert.Core.Callback;
using LogExpert.Core.Classes;

using NLog;

namespace LogExpert.Core.Classes.Filter;

internal delegate void FilterFx (FilterParams filterParams, List<int> filterResultLines, List<int> lastFilterResultLines, List<int> filterHitList);

internal class Filter
{
    #region Fields

    private const int PROGRESS_BAR_MODULO = 1000;
    private const int SPREAD_MAX = 50;
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
    public bool ShouldCancel { get; set; }

    #endregion

    #region Public methods

    public int DoFilter (FilterParams filterParams, int startLine, int maxCount, ProgressCallback progressCallback)
    {
        return DoFilter(filterParams, startLine, maxCount, FilterResultLines, LastFilterLinesList, FilterHitList, progressCallback);
    }

    #endregion

    #region Private Methods

    private int DoFilter (FilterParams filterParams, int startLine, int maxCount, List<int> filterResultLines, List<int> lastFilterLinesList, List<int> filterHitList, ProgressCallback progressCallback)
    {
        var lineNum = startLine;
        var count = 0;
        var callbackCounter = 0;

        try
        {
            filterParams.Reset();

            while ((count++ < maxCount || filterParams.IsInRange) && !ShouldCancel)
            {
                if (lineNum >= _callback.GetLineCount())
                {
                    return count;
                }

                var line = _callback.GetLogLine(lineNum);

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
        var filterResult = GetAdditionalFilterResults(filterParams, lineNum, lastFilterLinesList);

        filterResultLines.AddRange(filterResult);

        lastFilterLinesList.AddRange(filterResult);

        if (lastFilterLinesList.Count > SPREAD_MAX * 2)
        {
            lastFilterLinesList.RemoveRange(0, lastFilterLinesList.Count - SPREAD_MAX * 2);
        }
    }

    /// <summary>
    ///  Returns a list with 'additional filter results'. This is the given line number
    ///  and (if back spread and/or fore spread is enabled) some additional lines.
    ///  This function doesn't check the filter condition!
    /// </summary>
    /// <param name="filterParams"></param>
    /// <param name="lineNum"></param>
    /// <param name="checkList"></param>
    /// <returns></returns>
    private IList<int> GetAdditionalFilterResults (FilterParams filterParams, int lineNum, IList<int> checkList)
    {
        IList<int> resultList = [];

        if (filterParams.SpreadBefore == 0 && filterParams.SpreadBehind == 0)
        {
            resultList.Add(lineNum);
            return resultList;
        }

        // back spread
        for (var i = filterParams.SpreadBefore; i > 0; --i)
        {
            if (lineNum - i > 0)
            {
                if (!resultList.Contains(lineNum - i) && !checkList.Contains(lineNum - i))
                {
                    resultList.Add(lineNum - i);
                }
            }
        }
        // direct filter hit
        if (!resultList.Contains(lineNum) && !checkList.Contains(lineNum))
        {
            resultList.Add(lineNum);
        }
        // after spread
        for (var i = 1; i <= filterParams.SpreadBehind; ++i)
        {
            if (lineNum + i < _callback.GetLineCount())
            {
                if (!resultList.Contains(lineNum + i) && !checkList.Contains(lineNum + i))
                {
                    resultList.Add(lineNum + i);
                }
            }
        }

        return resultList;
    }

    #endregion
}