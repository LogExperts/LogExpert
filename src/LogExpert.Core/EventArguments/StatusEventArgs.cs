namespace LogExpert.Core.EventArguments;

public class StatusLineEventArgs : System.EventArgs
{
    #region Properties

    public long FileSize { get; set; }

    public string StatusText { get; set; } = string.Empty;

    public int LineCount { get; set; }

    public int CurrentLineNum { get; set; }

    #endregion

    #region Public methods

    public StatusLineEventArgs Clone()
    {
        StatusLineEventArgs e = new()
        {
            StatusText = StatusText,
            CurrentLineNum = CurrentLineNum,
            LineCount = LineCount,
            FileSize = FileSize
        };
        return e;
    }

    #endregion
}