using ColumnizerLib;

namespace LogExpert.Core.EventArguments;

public class ColumnizerEventArgs (ILogLineMemoryColumnizer columnizer) : EventArgs
{
    #region Properties

    public ILogLineMemoryColumnizer Columnizer { get; } = columnizer;

    #endregion
}