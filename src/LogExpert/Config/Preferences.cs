using LogExpert.Dialogs;
using LogExpert.Entities;

using System;
using System.Collections.Generic;
using System.Drawing;

namespace LogExpert.Config
{
    [Serializable]
    public class Preferences
    {
        #region Fields

        public bool allowOnlyOneInstance;

        public bool askForClose = false;

        public bool darkMode = false;

        public int bufferCount = 100;

        public List<ColumnizerMaskEntry> columnizerMaskList = [];

        public string defaultEncoding;

        public bool filterSync = true;

        public bool filterTail = true;

        public bool followTail = true;

        public string fontName = "Courier New";

        public float fontSize = 9;

        public List<HighlightMaskEntry> highlightMaskList = [];

        public bool isAutoHideFilterList = false;

        public bool isFilterOnLoad;

        public int lastColumnWidth = 2000;

        public int linesPerBuffer = 500;

        public int maximumFilterEntries = 30;

        public int maximumFilterEntriesDisplayed = 20;

        public bool maskPrio;

        public bool autoPick;

        public MultiFileOption multiFileOption;

        public MultiFileOptions multiFileOptions;

        public bool multiThreadFilter = true;

        public bool openLastFiles = true;

        public int pollingInterval = 250;

        public bool reverseAlpha = false;

        public bool PortableMode { get; set; }

        /// <summary>
        /// Save Directory of the last logfile
        /// </summary>
        public string sessionSaveDirectory = null;

        public bool saveFilters = true;

        public SessionSaveLocation saveLocation = SessionSaveLocation.DocumentsDir;

        public bool saveSessions = true;

        public bool setLastColumnWidth;

        public bool showBubbles = true;

        public bool showColumnFinder;

        public Color showTailColor = Color.FromKnownColor(KnownColor.Blue);

        public bool showTailState = true;

        public bool showTimeSpread = false;

        public Color timeSpreadColor = Color.FromKnownColor(KnownColor.Gray);

        public bool timeSpreadTimeMode;

        public bool timestampControl = true;

        public DateTimeDragControl.DragOrientations timestampControlDragOrientation = DateTimeDragControl.DragOrientations.Horizontal;

        public List<ToolEntry> toolEntries = [];

        public bool useLegacyReader;

        public bool ShowErrorMessageAllowOnlyOneInstances { get; set; }

        public int MaxLineLength { get; set; } = 20000;

        #endregion
    }
}