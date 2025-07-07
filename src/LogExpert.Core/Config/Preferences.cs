using System.Drawing;

using LogExpert.Core.Entities;
using LogExpert.Core.Enums;

namespace LogExpert.Core.Config;

[Serializable]
public class Preferences
{
    public List<HighlightGroup> HighlightGroupList { get; set; } = [];

    public bool PortableMode { get; set; }

    public bool ShowErrorMessageAllowOnlyOneInstances { get; set; }

    public int MaxLineLength { get; set; } = 20000;

    public bool AllowOnlyOneInstance { get; set; }

    public bool AskForClose { get; set; }

    public bool DarkMode { get; set; }

    public bool UseLegacyReader { get; set; }

    public List<ToolEntry> ToolEntries { get; set; } = [];

    public DragOrientationsEnum TimestampControlDragOrientation { get; set; } = DragOrientationsEnum.Horizontal;

    public bool TimestampControl { get; set; }

    public bool TimeSpreadTimeMode { get; set; }

    /// <summary>
    /// Save Directory of the last logfile
    /// </summary>
    public string SessionSaveDirectory { get; set; }

    public bool SaveFilters { get; set; } = true;

    public SessionSaveLocation SaveLocation { get; set; } = SessionSaveLocation.DocumentsDir;

    public bool SaveSessions { get; set; } = true;

    public bool SetLastColumnWidth { get; set; }

    public bool ShowBubbles { get; set; } = true;

    public bool ShowColumnFinder { get; set; }

    public Color ShowTailColor { get; set; } = Color.FromKnownColor(KnownColor.Blue);

    public bool ShowTailState { get; set; } = true;

    public bool ShowTimeSpread { get; set; }

    public Color TimeSpreadColor { get; set; } = Color.FromKnownColor(KnownColor.Gray);

    public bool IsAutoHideFilterList { get; set; }

    public bool IsFilterOnLoad { get; set; }

    public int LastColumnWidth { get; set; } = 2000;

    public int LinesPerBuffer { get; set; } = 500;

    public int MaximumFilterEntries { get; set; } = 30;

    public int MaximumFilterEntriesDisplayed { get; set; } = 20;

    public bool MaskPrio { get; set; }

    public bool AutoPick { get; set; }

    //TODO Refactor Enum
    public MultiFileOption MultiFileOption { get; set; }

    //TODO Refactor Class
    public MultiFileOptions MultiFileOptions { get; set; }

    public bool MultiThreadFilter { get; set; } = true;

    public bool OpenLastFiles { get; set; } = true;

    public int PollingInterval { get; set; } = 250;

    public bool ReverseAlpha { get; set; }

    public int BufferCount { get; set; } = 100;

    public List<ColumnizerMaskEntry> ColumnizerMaskList { get; set; } = [];

    public string DefaultEncoding { get; set; }

    public bool FilterSync { get; set; } = true;

    public bool FilterTail { get; set; } = true;

    public bool FollowTail { get; set; } = true;

    public string FontName { get; set; } = "Courier New";

    public float FontSize { get; set; } = 9;

    public List<HighlightMaskEntry> HighlightMaskList { get; set; } = [];
}