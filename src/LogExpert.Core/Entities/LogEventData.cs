namespace LogExpert.Core.Entities;

public class LogEventArgs : System.EventArgs
{
    #region Fields

    #endregion

    #region Properties

    public int RolloverOffset { get; set; }

    public bool IsRollover { get; set; }

    public long FileSize { get; set; }

    public int LineCount { get; set; }

    public int PrevLineCount { get; set; }

    public long PrevFileSize { get; set; }

    #endregion
}