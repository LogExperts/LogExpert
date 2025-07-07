namespace LogExpert.Core.EventArguments;

public class ContextMenuPluginEventArgs(IContextMenuEntry entry, IList<int> logLines, ILogLineColumnizer columnizer,
    ILogExpertCallback callback) : System.EventArgs
{

    #region Properties

    public IContextMenuEntry Entry { get; } = entry;

    public IList<int> LogLines { get; } = logLines;

    public ILogLineColumnizer Columnizer { get; } = columnizer;

    public ILogExpertCallback Callback { get; } = callback;

    #endregion
}