using System.Globalization;

using LogExpert.Core.Callback;
using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Entities;

using NLog;

using Range = LogExpert.Core.Entities.Range;

namespace LogExpert.UI.Controls.LogWindow;

/// <summary>
/// Delivers the range (from..to) that matches the current range filter settings starting from a given line.
/// </summary>
internal class RangeFinder(FilterParams filterParams, ColumnizerCallback callback)
{
    #region Fields

    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly FilterParams _filterParams = filterParams.CloneWithCurrentColumnizer();

    #endregion

    #region Public methods

    public Range FindRange(int startLine)
    {
        _logger.Info($"Starting range search for {_filterParams.SearchText} ... {_filterParams.RangeSearchText}");

        if (_filterParams.RangeSearchText == null || _filterParams.RangeSearchText.Trim().Length == 0)
        {
            _logger.Info(CultureInfo.InvariantCulture, "Range search text not set. Cancelling range search.");
            return null;
        }
        if (_filterParams.SearchText == null || _filterParams.SearchText.Trim().Length == 0)
        {
            _logger.Info(CultureInfo.InvariantCulture, "Search text not set. Cancelling range search.");
            return null;
        }

        _filterParams.IsRangeSearch = false;
        _filterParams.IsInRange = false;

        var lineCount = callback.GetLineCount();
        var lineNum = startLine;
        var foundStartLine = false;

        Range range = new();
        FilterParams tmpParam = _filterParams.CloneWithCurrentColumnizer();

        tmpParam.SearchText = _filterParams.RangeSearchText;

        // search backward for starting keyword
        var line = callback.GetLogLine(lineNum);

        while (lineNum >= 0)
        {
            callback.LineNum = lineNum;

            if (Util.TestFilterCondition(_filterParams, line, callback))
            {
                foundStartLine = true;
                break;
            }
            lineNum--;
            line = callback.GetLogLine(lineNum);

            if (lineNum < 0 || Util.TestFilterCondition(tmpParam, line, callback)) // do not crash on Ctrl+R when there is not start line found
            {
                // lower range bound found --> we are not in between a valid range
                break;
            }
        }

        if (!foundStartLine)
        {
            _logger.Info(CultureInfo.InvariantCulture, "Range start not found");
            return null;
        }

        range.StartLine = lineNum;
        _filterParams.IsRangeSearch = true;
        _filterParams.IsInRange = true;
        lineNum++;

        while (lineNum < lineCount)
        {
            line = callback.GetLogLine(lineNum);
            callback.LineNum = lineNum;
            if (!Util.TestFilterCondition(_filterParams, line, callback))
            {
                break;
            }
            lineNum++;
        }
        lineNum--;
        range.EndLine = lineNum;

        _logger.Info($"Range search finished. Found {range.EndLine - range.StartLine} lines");

        return range;
    }

    #endregion
}