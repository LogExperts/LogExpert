namespace LogExpert.Core.EventArguments;

public class SelectLineEventArgs(int line) : System.EventArgs
{
    #region Properties

    public int Line { get; } = line;

    #endregion
}