using LogExpert.Core.Callback;
using LogExpert.Core.Classes.Log;

namespace LogExpert.UI.Controls.LogWindow;

internal class ColumnCache
{
    #region Fields

    private IColumnizedLogLine _cachedColumns;
    private ILogLineColumnizer _lastColumnizer;
    private int _lastLineNumber = -1;

    #endregion

    #region Internals

    internal IColumnizedLogLine GetColumnsForLine (LogfileReader logFileReader, int lineNumber, ILogLineColumnizer columnizer, ColumnizerCallback columnizerCallback)
    {
        if (_lastColumnizer != columnizer || (_lastLineNumber != lineNumber && _cachedColumns != null) || columnizerCallback.LineNum != lineNumber)
        {
            _lastColumnizer = columnizer;
            _lastLineNumber = lineNumber;
            ILogLine line = logFileReader.GetLogLineWithWait(lineNumber).Result;

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