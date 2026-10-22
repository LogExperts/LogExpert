using ColumnizerLib;

using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Callback;

public class ColumnizerCallbackMemory (ILogLineSource lineSource) : ILogLineMemoryColumnizerCallback, IAutoLogLineMemoryColumnizerCallback, ICloneable
{
    #region Fields
    private readonly ILogLineSource _lineSource = lineSource;

    #endregion

    #region Properties

    public int LineNum { get; set; }

    #endregion

    #region cTor

    private ColumnizerCallbackMemory (ColumnizerCallbackMemory original) : this(original._lineSource)
    {
        LineNum = original.LineNum;
    }

    #endregion

    #region Public methods

    public object Clone ()
    {
        return new ColumnizerCallbackMemory(this);
    }

    public string GetFileName ()
    {
        return _lineSource.GetCurrentFileName(LineNum);
    }

    public int GetLineCount ()
    {
        return _lineSource.LineCount;
    }

    public void SetLineNum (int lineNum)
    {
        LineNum = lineNum;
    }

    public ILogLineMemory GetLogLineMemory (int lineNum)
    {
        return _lineSource.GetLineMemory(lineNum);
    }

    #endregion
}
