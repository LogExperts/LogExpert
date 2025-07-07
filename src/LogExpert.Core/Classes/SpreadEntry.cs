namespace LogExpert.Core.Classes;

public class SpreadEntry
{
    #region Fields

    public int Diff { get; set; }

    public DateTime Timestamp { get; set; }

    public int LineNum { get; set; }

    public int Value { get; set; }

    #endregion

    #region cTor

    public SpreadEntry (int lineNum, int diff, DateTime timestamp)
    {
        LineNum = lineNum;
        Diff = diff;
        Timestamp = timestamp;
    }

    #endregion
}