using System.Text;

namespace LogExpert.Core.EventArguments;

public class GuiStateArgs : System.EventArgs
{
    #region Properties

    public bool TimeshiftEnabled { get; set; }

    public bool FollowTail { get; set; }

    public bool MenuEnabled { get; set; } = true;

    public string TimeshiftText { get; set; } = string.Empty;

    public bool TimeshiftPossible { get; set; }

    public bool MultiFileEnabled { get; set; } = true;

    public bool FilterEnabled { get; set; } = true;

    public bool CellSelectMode { get; set; }

    public Encoding CurrentEncoding { get; set; }

    public DateTime Timestamp { get; set; }

    public DateTime MinTimestamp { get; set; }

    public DateTime MaxTimestamp { get; set; }

    public bool ShowBookmarkBubbles { get; set; }

    public bool IsMultiFileActive { get; set; }

    public bool ShowHiddenLines { get; set; } = true;

    public string HighlightGroupName { get; set; } = string.Empty;

    public bool ColumnFinderVisible { get; set; }

    #endregion
}