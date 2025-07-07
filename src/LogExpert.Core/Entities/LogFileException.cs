namespace LogExpert.Core.Entities;

public class LogFileException : Exception
{
    #region cTor

    public LogFileException (string msg)
        : base(msg)
    {
    }

    public LogFileException (string msg, Exception inner)
        : base(msg, inner)
    {
    }

    public LogFileException ()
    {
    }

    #endregion
}