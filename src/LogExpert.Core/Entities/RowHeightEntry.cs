namespace LogExpert.Core.Entities;

public class RowHeightEntry
{
    #region cTor

    public RowHeightEntry()
    {
        LineNum = 0;
        Height = 0;
    }

    public RowHeightEntry(int lineNum, int height)
    {
        LineNum = lineNum;
        Height = height;
    }

    #endregion

    #region Properties

    public int LineNum { get; set; }

    public int Height { get; set; }

    #endregion
}