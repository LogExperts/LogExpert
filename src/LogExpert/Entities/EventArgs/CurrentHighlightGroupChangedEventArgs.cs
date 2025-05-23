using LogExpert.Controls.LogWindow;
using LogExpert.Core.Entities;

namespace LogExpert.Entities.EventArgs
{
    public class CurrentHighlightGroupChangedEventArgs(LogWindow logWindow, HighlightGroup currentGroup)
    {
        #region Properties

        public LogWindow LogWindow { get; } = logWindow;

        public HighlightGroup CurrentGroup { get; } = currentGroup;

        #endregion
    }
}