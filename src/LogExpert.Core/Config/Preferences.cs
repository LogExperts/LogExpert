using LogExpert.Core.Entities;
using LogExpert.Core.Enums;

using System.Drawing;

namespace LogExpert.Core.Config
{
    [Serializable]
    public class Preferences
    {
        #region Fields

        public bool allowOnlyOneInstance;

        public bool askForClose;

        public bool darkMode;

        public int bufferCount = 100;

        public List<ColumnizerMaskEntry> columnizerMaskList = [];

        public string defaultEncoding;

        public bool filterSync = true;

        public bool filterTail = true;

        public bool followTail = true;

        public string fontName = "Courier New";

        public float fontSize = 9;

        public List<HighlightMaskEntry> highlightMaskList = [];

        public List<HighlightGroup> HighlightGroupList { get; set; } = [];

        public bool isAutoHideFilterList;

        public bool isFilterOnLoad;

        public int lastColumnWidth = 2000;

        public int linesPerBuffer = 500;

        public int maximumFilterEntries = 30;

        public int maximumFilterEntriesDisplayed = 20;

        public bool maskPrio;

        public bool autoPick;

        //Refactor Enum
        public MultiFileOption multiFileOption;

        //Refactor class?
        public MultiFileOptions multiFileOptions;

        public bool multiThreadFilter = true;

        public bool openLastFiles = true;

        public int pollingInterval = 250;

        public bool reverseAlpha;

        public bool PortableMode { get; set; }

        /// <summary>
        /// Save Directory of the last logfile
        /// </summary>
        public string sessionSaveDirectory;

        public bool saveFilters = true;

        public SessionSaveLocation saveLocation = SessionSaveLocation.DocumentsDir;

        public bool saveSessions = true;

        public bool setLastColumnWidth;

        public bool showBubbles = true;

        public bool showColumnFinder;

        public Color showTailColor = Color.FromKnownColor(KnownColor.Blue);

        public bool showTailState = true;

        public bool showTimeSpread;

        public Color timeSpreadColor = Color.FromKnownColor(KnownColor.Gray);

        public bool timeSpreadTimeMode;

        public bool timestampControl = true;

        public DragOrientationsEnum timestampControlDragOrientation = DragOrientationsEnum.Horizontal;

        public List<ToolEntry> toolEntries = [];

        public bool useLegacyReader;

        public bool ShowErrorMessageAllowOnlyOneInstances { get; set; }

        public int MaxLineLength { get; set; } = 20000;

        #endregion
    }
}