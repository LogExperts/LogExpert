
using LogExpert.Core.Interface;

namespace LogExpert.Core.EventArguments;

//TODO: Move to UI
public class FilterListChangedEventArgs (ILogWindow logWindow) : EventArgs
{
    public ILogWindow LogWindow { get; } = logWindow;
}