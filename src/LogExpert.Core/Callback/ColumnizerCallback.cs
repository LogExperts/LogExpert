using LogExpert.Core.Interface;

namespace LogExpert.Core.Callback;

public class ColumnizerCallback(ILogWindow logWindow) : ILogLineColumnizerCallback, IAutoLogLineColumnizerCallback, ICloneable
{
    #region Fields
    private readonly ILogWindow _logWindow = logWindow;

    #endregion

    #region Properties

    public int LineNum { get; set; }

    #endregion

    #region cTor

    private ColumnizerCallback(ColumnizerCallback original) : this(original._logWindow)
    {
        LineNum = original.LineNum;
    }

    #endregion

    #region Public methods

    public object Clone()
    {
        return new ColumnizerCallback(this);
    }

    public string GetFileName()
    {
        return _logWindow.GetCurrentFileName(LineNum);
    }

    public ILogLine GetLogLine(int lineNum)
    {
        return _logWindow.GetLine(lineNum);
    }

    public int GetLineCount()
    {
        return _logWindow.LogFileReader.LineCount;
    }

    public void SetLineNum(int lineNum)
    {
        LineNum = lineNum;
    }

    #endregion
}