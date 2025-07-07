namespace LogExpert.Core.Entities;

public class Range
{
    #region Fields

    #endregion

    #region cTor

    public Range()
    {
    }

    public Range(int startLine, int endLine)
    {
        StartLine = startLine;
        EndLine = endLine;
    }

    #endregion

    #region Properties

    public int StartLine { get; set; }

    public int EndLine { get; set; }

    #endregion
}