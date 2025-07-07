using LogExpert.Core.Entities;
using LogExpert.Core.Interface;

namespace LogExpert.Core.EventArguments;

public class CurrentHighlightGroupChangedEventArgs(ILogWindow logWindow, HighlightGroup currentGroup)
{
    #region Properties

    public ILogWindow LogWindow { get; } = logWindow;

    public HighlightGroup CurrentGroup { get; } = currentGroup;

    #endregion
}