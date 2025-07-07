namespace LogExpert.Core.EventArguments;

public class ColumnizerEventArgs(ILogLineColumnizer columnizer) : System.EventArgs
{
    #region Properties

    public ILogLineColumnizer Columnizer { get; } = columnizer;

    #endregion
}