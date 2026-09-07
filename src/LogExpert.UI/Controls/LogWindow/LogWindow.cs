using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

using ColumnizerLib;
using ColumnizerLib.Extensions;

using LogExpert.Audio;
using LogExpert.Core.Callback;
using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Bookmark;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Log;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Classes.Search;
using LogExpert.Core.Classes.Tail;
using LogExpert.Core.Classes.Timestamp;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArguments;
using LogExpert.Core.Interfaces;
using LogExpert.Dialogs;
using LogExpert.UI.ControlCharDisplay;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Entities;
using LogExpert.UI.Extensions;
using LogExpert.UI.Interface;

using NLog;

using Vanara.Extensions;

using WeifenLuo.WinFormsUI.Docking;
//using static LogExpert.PluginRegistry.PluginRegistry;
//TODO: Adjust the instance name so using static can be used.

namespace LogExpert.UI.Controls.LogWindow;

[SupportedOSPlatform("windows")]
internal partial class LogWindow : DockContent, ILogPaintContextUI, ILogView, ILogWindow, ITimestampSource, ITailFollowSink
{
    #region Fields

    private const int PROGRESS_BAR_MODULO = 1000;
    private const int FILTER_ADVANCED_SPLITTER_DISTANCE = 110;
    private const int FILTER_PANEL2_CONTROL_GAP = 6;
    private const int WAIT_TIME = 500;
    private const int OVERSCAN = 20;
    private const int WORKER_SHUTDOWN_TIMEOUT = 2000; // ms to wait for a worker task to drain during teardown before giving up
    private const string FONT_COURIER_NEW = "Courier New";
    private const string FONT_VERDANA = "Verdana";
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly Image _advancedButtonImage;

    private readonly BookmarkDataProvider _bookmarkProvider = new();

    private readonly IList<IBackgroundProcessCancelHandler> _cancelHandlerList = [];

    private readonly Lock _currentColumnizerLock = new();

    private readonly Lock _currentHighlightGroupLock = new();

    private readonly EventWaitHandle _externaLoadingFinishedEvent = new ManualResetEvent(false);

    private readonly IList<FilterPipe> _filterPipeList = [];
    private readonly Dictionary<Control, bool> _freezeStateMap = [];
    private readonly GuiStateEventArgs _guiStateArgs = new();

    private readonly EventWaitHandle _loadingFinishedEvent = new ManualResetEvent(false);

    private readonly TailFollowEngine _tailFollowEngine;

    private readonly Image _panelCloseButtonImage;

    private readonly Image _panelOpenButtonImage;
    private readonly ILogWindowCoordinator _logWindowCoordinator;
    private readonly IPluginRegistry _pluginRegistry;

    private readonly ProgressEventArgs _progressEventArgs = new();
    private readonly Lock _reloadLock = new();
    private readonly Image _searchButtonImage;
    private readonly StatusLineEventArgs _statusEventArgs = new();

    private readonly Lock _tempHighlightEntryListLock = new();

    private readonly Task _timeShiftSyncTask;

    private readonly CancellationTokenSource _cts = new();
    private CancellationTokenSource _highlightBookmarkScanCts;

    //private readonly Thread _timeShiftSyncThread;
    private readonly EventWaitHandle _timeShiftSyncTimerEvent = new ManualResetEvent(false);
    private readonly EventWaitHandle _timeShiftSyncWakeupEvent = new ManualResetEvent(false);

    private readonly TimeSpreadCalculator _timeSpreadCalc;
    private readonly TimestampLocator _timestampLocator;

    private readonly Lock _timeSyncListLock = new();

    private ColumnCache _columnCache = new();
    private ColumnCache _filterColumnCache = new();

    private readonly StringFormat _format = new()
    {
        LineAlignment = StringAlignment.Center,
        Alignment = StringAlignment.Center
    };

    //List<HilightEntry> currentHilightEntryList = new List<HilightEntry>();
    private HighlightGroup _currentHighlightGroup = new();

    private SearchParams _currentSearchParams;
    private CancellationTokenSource? _searchCts;

    private string[] _fileNames;
    private List<int> _filterHitList = [];
    private FilterParams _filterParams = new();
    private int _filterPipeNameCounter;
    private List<int> _filterResultList = [];

    private ILogLineMemoryColumnizer _forcedColumnizer;
    private ILogLineMemoryColumnizer _forcedColumnizerForLoading;

    private bool _isDeadFile;
    private bool _isErrorShowing;
    private bool _isLoadError;
    private bool _isLoading;
    private bool _isSearching;

    private List<int> _lastFilterLinesList = [];

    private int _lineHeight;

    private LogfileReader _logFileReader;
    private MultiFileOptions _multiFileOptions = new();
    private bool _noSelectionUpdates;

    private ReloadMemento _reloadMemento;
    private int _reloadOverloadCounter;
    private SortedList<int, RowHeightEntry> _rowHeightList = [];
    private int _selectedCol; // set by context menu event for column headers only

    /// <summary>The deserialize-once result of the Session File read, kept for the window-open
    /// in progress. Written by the read at the start of each open (or direct multi-file load),
    /// applied by the two apply phases (<see cref="LoadPersistenceOptions"/> pre-load,
    /// <see cref="LoadPersistenceData"/> post-load).</summary>
    private SessionSnapshot _sessionSnapshot;
    private bool _shouldCallTimeSync;
    /// <summary>Window-lifetime cancellation: per-job token sources link to it, so closing the
    /// window cancels every running job. Live jobs are also registered in the cancel-handler
    /// registry, which ESC and reload fan out through.</summary>
    private readonly CancellationTokenSource _windowCts = new();
    private bool _isClosing; // set once CloseLogWindow starts tearing down; suppresses grid CellValueNeeded callbacks
    private bool _shouldTimestampDisplaySyncingCancel;
    private bool _showAdvanced;
    private List<HighlightEntry> _tempHighlightEntryList = [];
    private int _timeShiftSyncLine;

    private bool _waitingForClose;

    #endregion

    #region cTor

    [SupportedOSPlatform("windows")]
    public LogWindow (ILogWindowCoordinator logWindowCoordinator, string fileName, bool isTempFile, bool forcePersistenceLoading, IConfigManager configManager, IPluginRegistry pluginRegistry)
    {
        SuspendLayout();

        //HighDPI Functionality must be called before all UI Elements are initialized, to make sure they work as intended
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();

        SetResources();

        CreateDefaultViewStyle();

        columnNamesLabel.Text = string.Empty; // no filtering on columns by default

        _logWindowCoordinator = logWindowCoordinator;
        IsTempFile = isTempFile;
        ConfigManager = configManager; //TODO: This should be changed to DI
        _pluginRegistry = pluginRegistry;
        //Thread.CurrentThread.Name = "LogWindowThread";
        ColumnizerCallbackObject = new ColumnizerCallback(this);
        _timestampLocator = new TimestampLocator(this);

        FileName = fileName;
        ForcePersistenceLoading = forcePersistenceLoading;

        dataGridView.CellValueNeeded += OnDataGridViewCellValueNeeded;
        dataGridView.CellPainting += OnDataGridViewCellPainting;

        filterGridView.CellValueNeeded += OnFilterGridViewCellValueNeeded;
        filterGridView.CellPainting += OnFilterGridViewCellPainting;
        listBoxFilter.DrawMode = DrawMode.OwnerDrawVariable;
        listBoxFilter.MeasureItem += MeasureItem;

        FormClosing += OnLogWindowClosing;
        Disposed += OnLogWindowDisposed;
        Load += OnLogWindowLoad;

        _timeSpreadCalc = new TimeSpreadCalculator(_timestampLocator, this);
        timeSpreadingControl.TimeSpreadCalc = _timeSpreadCalc;
        timeSpreadingControl.LineSelected += OnTimeSpreadingControlLineSelected;
        tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Absolute;
        tableLayoutPanel1.ColumnStyles[1].Width = 20;
        tableLayoutPanel1.ColumnStyles[0].SizeType = SizeType.Percent;
        tableLayoutPanel1.ColumnStyles[0].Width = 100;

        _logWindowCoordinator.HighlightSettingsChanged += OnParentHighlightSettingsChanged;
        SetColumnizer(_pluginRegistry.RegisteredColumnizers[0]);

        //this.toolwinTabControl.TabPages.Add(this.bookmarkWindow);

        _filterParams = new FilterParams();
        foreach (var item in configManager.Settings.FilterHistoryList)
        {
            _ = filterComboBox.Items.Add(item);
        }

        filterComboBox.DropDownHeight = filterComboBox.ItemHeight * configManager.Settings.Preferences.MaximumFilterEntriesDisplayed;

        // Keep Panel2 wide enough that "Show advanced..." (btnAdvanced, the rightmost left-anchored
        // control) never overlaps the right-anchored filter-count label when the text filter grows.
        filterSplitContainer.Panel2MinSize = FilterSplitterLayout.RequiredPanel2Width(btnAdvanced.Right, lblFilterCount.Width, FILTER_PANEL2_CONTROL_GAP);
        AutoResizeFilterBox();

        filterRegexCheckBox.Checked = _filterParams.IsRegex;
        filterCaseSensitiveCheckBox.Checked = _filterParams.IsCaseSensitive;
        filterTailCheckBox.Checked = _filterParams.IsFilterTail;

        splitContainerLogWindow.Panel2Collapsed = true;
        advancedFilterSplitContainer.SplitterDistance = FILTER_ADVANCED_SPLITTER_DISTANCE;

        _timeShiftSyncTask = Task.Factory.StartNew(SyncTimestampDisplayWorker, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        _tailFollowEngine = new TailFollowEngine(this);

        //this.filterUpdateThread = new Thread(new ThreadStart(this.FilterUpdateWorker));
        //this.filterUpdateThread.Start();

        _advancedButtonImage = btnAdvanced.Image;
        _searchButtonImage = btnfilterSearch.Image;
        btnfilterSearch.Image = null;

        dataGridView.EditModeMenuStrip = editModeContextMenuStrip;
        markEditModeToolStripMenuItem.Enabled = true;

        _panelOpenButtonImage = Resources.Arrow_menu_open;
        _panelCloseButtonImage = Resources.Arrow_menu_close;

        var settings = configManager.Settings;

        if (settings.AppBounds.Right > 0)
        {
            Bounds = settings.AppBounds;
        }

        _waitingForClose = false;
        dataGridView.Enabled = false;
        dataGridView.ColumnDividerDoubleClick += OnDataGridViewColumnDividerDoubleClick;
        ShowAdvancedFilterPanel(false);
        knobControlFilterBackSpread.MinValue = 0;
        knobControlFilterBackSpread.MaxValue = FilterSpread.SPREAD_MAX;
        knobControlFilterBackSpread.ValueChanged += OnFilterKnobControlValueChanged;
        knobControlFilterForeSpread.MinValue = 0;
        knobControlFilterForeSpread.MaxValue = FilterSpread.SPREAD_MAX;
        knobControlFilterForeSpread.ValueChanged += OnFilterKnobControlValueChanged;
        knobControlFuzzy.MinValue = 0;
        knobControlFuzzy.MaxValue = 10;
        //PreferencesChanged(settings.preferences, true);
        AdjustHighlightSplitterWidth();
        ToggleHighlightPanel(false); // hidden

        _bookmarkProvider.BookmarkAdded += OnBookmarkProviderBookmarkAdded;
        _bookmarkProvider.BookmarkRemoved += OnBookmarkProviderBookmarkRemoved;
        _bookmarkProvider.AllBookmarksRemoved += OnBookmarkProviderAllBookmarksRemoved;

        ResumeLayout();
    }

    #endregion

    #region Delegates

    // used for filterTab restore
    public delegate void FilterRestoreFx (LogWindow newWin, SessionSnapshot snapshot);

    public delegate void RestoreFiltersFx (SessionSnapshot snapshot);

    public delegate bool ScrollToTimestampFx (DateTime timestamp, bool roundToSeconds, bool triggerSyncCall);

    #endregion

    #region Events

    public event EventHandler<LogEventArgs> FileSizeChanged;

    public event EventHandler<ProgressEventArgs> ProgressBarUpdate;

    public event EventHandler<StatusLineEventArgs> StatusLineEvent;

    public event EventHandler<GuiStateEventArgs> GuiStateUpdate;

    public event EventHandler<EventArgs> TailFollowed;

    public event EventHandler<EventArgs> FileNotFound;

    public event EventHandler<EventArgs> FileRespawned;

    public event EventHandler<FilterListChangedEventArgs> FilterListChanged;

    public event EventHandler<CurrentHighlightGroupChangedEventArgs> CurrentHighlightGroupChanged;

    /// <summary>
    /// Raised when this window's bookmark set changes — added or removed. Consumers only ever
    /// re-read the set, so add and remove are deliberately not distinguished here. The Core-level
    /// <see cref="BookmarkDataProvider.BookmarkAdded"/> / <see cref="BookmarkDataProvider.BookmarkRemoved"/>
    /// do keep them apart.
    /// </summary>
    public event EventHandler<EventArgs> BookmarksChanged;

    public event EventHandler<BookmarkEventArgs> BookmarkTextChanged;

    public event EventHandler<ColumnizerEventArgs> ColumnizerChanged;

    public event EventHandler<SyncModeEventArgs> SyncModeChanged;

    #endregion

    #region Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color BookmarkColor { get; set; } = Color.FromArgb(165, 200, 225);

    /// <summary>
    /// Color used to paint the bookmark marker in column 0 for auto-generated (highlight-triggered) bookmarks. Uses a
    /// lighter/more desaturated shade to distinguish from manual bookmarks.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color AutoBookmarkColor { get; set; } = Color.FromArgb(180, 210, 180);

    public ILogLineMemoryColumnizer CurrentColumnizer
    {
        get;
        private set
        {
            lock (_currentColumnizerLock)
            {
                field = value;
                _logger.Debug($"Setting columnizer {field.GetName()}");
            }
        }
    }

    [SupportedOSPlatform("windows")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ShowBookmarkBubbles
    {
        get => _guiStateArgs.ShowBookmarkBubbles;
        set
        {
            _guiStateArgs.ShowBookmarkBubbles = dataGridView.PaintWithOverlays = value;
            dataGridView.Refresh();
        }
    }

    public int CurrentLineNum => dataGridView.CurrentRow == null
            ? -1
            : dataGridView.CurrentRow.Index;

    public string FileName { get; private set; }

    public bool IsMultiFile
    {
        get;
        private set => _guiStateArgs.IsMultiFileActive = field = value;
    }

    public bool IsTempFile { get; }

    private readonly IConfigManager ConfigManager;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string TempTitleName { get; set; } = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    internal FilterPipe FilterPipe { get; set; }

    public string Title => IsTempFile
                ? TempTitleName
                : FileName;

    public ColumnizerCallback ColumnizerCallbackObject { get; }

    #region ITimestampSource

    // Read live rather than captured: _logFileReader is reassigned on load/reload/rollover, and
    // CurrentColumnizer is swapped by its own setter whenever the user picks a different one.
    // TimestampLocator (and TimeSpreadCalculator through it) must see the current instance of each.
    ILogfileReader ITimestampSource.Reader => _logFileReader;
    ILogLineMemoryColumnizer ITimestampSource.Columnizer => CurrentColumnizer;
    IPositionedColumnizerCallback ITimestampSource.Callback => ColumnizerCallbackObject;
    Lock ITimestampSource.ColumnizerLock => _currentColumnizerLock;

    #endregion

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ForcePersistenceLoading { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string ForcedPersistenceFileName { get; set; }

    public Preferences Preferences => ConfigManager.Settings.Preferences;

    SelectionHighlightSettings ILogPaintContextUI.SelectionHighlight => Preferences.SelectionHighlight;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string GivenFileName { get; set; }

    public TimeSyncList TimeSyncList { get; private set; }

    public bool IsTimeSynced => TimeSyncList != null;

    protected EncodingOptions EncodingOptions { get; set; }

    public IBookmarkData BookmarkData => _bookmarkProvider;

    public Font MonospacedFont { get; private set; }

    public Font NormalFont { get; private set; }

    public Font BoldFont { get; private set; }

    int ILogLineSource.LineCount => _logFileReader.LineCount;

    #endregion

    #region Public methods

    public ILogLineMemory GetLogLineMemory (int lineNum)
    {
        return _logFileReader.GetLogLineMemory(lineNum);
    }

    public Bookmark GetBookmarkForLine (int lineNum)
    {
        return _bookmarkProvider.GetBookmarkForLine(lineNum);
    }

    #endregion

    #region Internals

    #region Apply Resources

    private void SetResources ()
    {
        ApplyButtonResources();
        ApplyLabelResources();
        ApplyCheckBoxResources();
        ApplyToolStripMenuItemResources();
        ApplyToolTipsResources();
        ApplyResourceImages();
    }

    private void ApplyResourceImages ()
    {
        pnlProFilterLabel.BackgroundImage = Resources.Pro_Filter;
        btnFilterDown.BackgroundImage = Resources.ArrowDown;
        btnFilterUp.BackgroundImage = Resources.ArrowUp;
        btnToggleHighlightPanel.Image = Resources.Arrow_menu_open;
    }

    private void ApplyCheckBoxResources ()
    {
        columnRestrictCheckBox.Text = Resources.LogWindow_UI_CheckBox_ColumnRestrict;
        invertFilterCheckBox.Text = Resources.LogWindow_UI_CheckBox_InvertMatch;
        rangeCheckBox.Text = Resources.LogWindow_UI_CheckBox_RangeSearch;
        hideFilterListOnLoadCheckBox.Text = Resources.LogWindow_UI_CheckBox_AutoHide;
        filterOnLoadCheckBox.Text = Resources.LogWindow_UI_CheckBox_FilterOnLoad;
        syncFilterCheckBox.Text = Resources.LogWindow_UI_CheckBox_FilterSync;
        filterTailCheckBox.Text = Resources.LogWindow_UI_CheckBox_FilterTail;
        filterRegexCheckBox.Text = Resources.LogWindow_UI_CheckBox_FilterRegex;
        filterCaseSensitiveCheckBox.Text = Resources.LogWindow_UI_CheckBox_FilterCaseSensitive;
    }

    private void ApplyLabelResources ()
    {
        lblColumnName.Text = Resources.LogWindow_UI_Label_ColumnName;
        columnNamesLabel.Text = Resources.LogWindow_UI_Label_ColumnNames;
        lblfuzzy.Text = Resources.LogWindow_UI_Label_Fuzzyness;
        lblBackSpread.Text = Resources.LogWindow_UI_Label_BackSpread;
        lblForeSpread.Text = Resources.LogWindow_UI_Label_ForeSpread;
        lblTextFilter.Text = Resources.LogWindow_UI_Label_TextFilter;
        lblFilterCount.Text = Resources.LogWindow_UI_Common_ZeroValue;
    }

    private void ApplyButtonResources ()
    {
        btnColumn.Text = Resources.LogWindow_UI_Button_Column;
        btnColumn.AutoSize = true;
        btnColumn.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        btnFilterToTab.Text = Resources.LogWindow_UI_Button_FilterToTab;
        btnFilterToTab.AutoSize = true;
        btnFilterToTab.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        bntSaveFilter.Text = Resources.LogWindow_UI_Button_SaveFilter;
        bntSaveFilter.AutoSize = true;
        bntSaveFilter.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        btnDeleteFilter.Text = Resources.LogWindow_UI_Button_Delete;
        btnDeleteFilter.AutoSize = true;
        btnDeleteFilter.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        btnAdvanced.Text = Resources.LogWindow_UI_Button_ShowAdvanced;
        btnAdvanced.AutoSize = true;
        btnAdvanced.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        btnfilterSearch.Text = Resources.LogWindow_UI_Button_Search;
        btnfilterSearch.AutoSize = true;
        btnfilterSearch.AutoSizeMode = AutoSizeMode.GrowAndShrink;

    }

    private void ApplyToolStripMenuItemResources ()
    {
        copyToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_CopyToClipboard;
        copyToTabToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_CopyToNewTab;
        copyToTabToolStripMenuItem.ToolTipText = Resources.LogWindow_UI_ToolStripMenuItem_ToolTip_CopyToNewTab;
        scrollAllTabsToTimestampToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_ScrollAllTabsToCurrentTimestamp;
        scrollAllTabsToTimestampToolStripMenuItem.ToolTipText = Resources.LogWindow_UI_ToolStripMenuItem_ToolTip_ScrollAllTabsToCurrentTimestamp;
        syncTimestampsToToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_TimeSyncedFiles;
        freeThisWindowFromTimeSyncToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_FreeThisWindowFromTimeSync;
        locateLineInOriginalFileToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_LocateFilteredLineInOriginalFile;
        toggleBoomarkToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_ToggleBoomark;
        bookmarkCommentToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_BookmarkComment;
        bookmarkCommentToolStripMenuItem.ToolTipText = Resources.LogWindow_UI_ToolStripMenuItem_ToolTip_BookmarkComment;
        markEditModeToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_MarkEditMode;
        tempHighlightsToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_TempHighlights;
        removeAllToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_RemoveAll;
        makePermanentToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_MakeAllPermanent;
        markCurrentFilterRangeToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_MarkCurrentFilterRange;
        setBookmarksOnSelectedLinesToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_SetBookmarksOnSelectedLines;
        filterToTabToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_FilterToNewTab;
        markFilterHitsInLogViewToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_MarkFilterHitsInLogView;
        colorToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_Color;
        freezeLeftColumnsUntilHereToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_FreezeLeftColumnsUntilHere;
        moveToLastColumnToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_MoveToLastColumn;
        moveToLastColumnToolStripMenuItem.ToolTipText = Resources.LogWindow_UI_ToolStripMenuItem_ToolTip_MoveToLastColumn;
        moveLeftToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_MoveLeft;
        moveRightToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_MoveRight;
        hideColumnToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_HideColumn;
        hideColumnToolStripMenuItem.ToolTipText = Resources.LogWindow_UI_ToolStripMenuItem_ToolTip_HideColumn;
        restoreColumnsToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_RestoreColumns;
        allColumnsToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_ScrollToColumn;
        editModecopyToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_Copy;
        highlightSelectionInLogFileToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_HighlightSelectionInLogFileFullLine;
        highlightSelectionInLogFilewordModeToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_HighlightSelectionInLogFileWordMode;
        filterForSelectionToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_FilterForSelection;
        setSelectedTextAsBookmarkCommentToolStripMenuItem.Text = Resources.LogWindow_UI_ToolStripMenuItem_SetSelectedTextAsBookmarkComment;
    }

    private void ApplyToolTipsResources ()
    {
        helpToolTip.AutoPopDelay = 5000; //this is in ms, 5000ms =  5 seconds
        helpToolTip.SetToolTip(btnColumn, Resources.LogWindow_UI_Button_ToolTip_Column);
        helpToolTip.SetToolTip(columnRestrictCheckBox, Resources.LogWindow_UI_CheckBox_ToolTip_ColumnRestrict);
        helpToolTip.SetToolTip(knobControlFuzzy, Resources.LogWindow_UI_KnobControl_Fuzzy);
        helpToolTip.SetToolTip(invertFilterCheckBox, Resources.LogWindow_UI_CheckBox_ToolTip_InvertMatch);
        helpToolTip.SetToolTip(knobControlFilterBackSpread, Resources.LogWindow_UI_KnobControl_FilterBackSpread);
        helpToolTip.SetToolTip(knobControlFilterForeSpread, Resources.LogWindow_UI_KnobControl_FilterForeSpread);
        helpToolTip.SetToolTip(rangeCheckBox, Resources.LogWindow_UI_CheckBox_ToolTip_RangeSearch);
        helpToolTip.SetToolTip(filterRangeComboBox, Resources.LogWindow_UI_ComboBox_ToolTip_FilterRange);
        helpToolTip.SetToolTip(btnFilterToTab, Resources.LogWindow_UI_Button_ToolTip_FilterToTab);
        helpToolTip.SetToolTip(btnToggleHighlightPanel, Resources.LogWindow_UI_Button_ToolTip_ToggleHighlightPanel);
        helpToolTip.SetToolTip(filterOnLoadCheckBox, Resources.LogWindow_UI_CheckBox_ToolTip_FilterOnLoad);
        helpToolTip.SetToolTip(hideFilterListOnLoadCheckBox, Resources.LogWindow_UI_CheckBox_ToolTip_AutoHide);
        helpToolTip.SetToolTip(btnFilterDown, Resources.LogWindow_UI_Button_ToolTip_FilterDown);
        helpToolTip.SetToolTip(btnFilterUp, Resources.LogWindow_UI_Button_ToolTip_FilterUp);
        helpToolTip.SetToolTip(listBoxFilter, Resources.LogWindow_UI_ListBox_ToolTip_Filter);
        helpToolTip.SetToolTip(filterComboBox, Resources.LogWindow_UI_ComboBox_ToolTip_Filter);
        helpToolTip.SetToolTip(btnAdvanced, Resources.LogWindow_UI_Button_ToolTip_ShowAdvanced);
        helpToolTip.SetToolTip(syncFilterCheckBox, Resources.LogWindow_UI_CheckBox_ToolTip_FilterSync);
        helpToolTip.SetToolTip(filterTailCheckBox, Resources.LogWindow_UI_CheckBox_ToolTip_FilterTail);
        helpToolTip.SetToolTip(filterRegexCheckBox, Resources.LogWindow_UI_CheckBox_ToolTip_FilterRegex);
        helpToolTip.SetToolTip(filterCaseSensitiveCheckBox, Resources.LogWindow_UI_CheckBox_ToolTip_FilterCaseSensitive);
        helpToolTip.SetToolTip(btnfilterSearch, Resources.LogWindow_UI_Button_ToolTip_Search);
        helpToolTip.SetToolTip(columnComboBox, Resources.LogWindow_UI_ColumnComboBox_ToolTip);
    }

    #endregion

    [SupportedOSPlatform("windows")]
    internal void RefreshAllGrids ()
    {
        dataGridView.Refresh();
        filterGridView.Refresh();
    }

    [SupportedOSPlatform("windows")]
    internal void ChangeMultifileMask ()
    {
        MultiFileMaskDialog dlg = new(FileName)
        {
            Owner = this,
            MaxDays = _multiFileOptions.MaxDayTry,
            FileNamePattern = _multiFileOptions.FormatPattern
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _multiFileOptions.FormatPattern = dlg.FileNamePattern;
            _multiFileOptions.MaxDayTry = dlg.MaxDays;
            if (IsMultiFile)
            {
                Reload();
            }
        }
    }

    [SupportedOSPlatform("windows")]
    internal void ToggleColumnFinder (bool show, bool setFocus)
    {
        _guiStateArgs.ColumnFinderVisible = show;

        if (show)
        {
            columnComboBox.AutoCompleteMode = AutoCompleteMode.Suggest;
            columnComboBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            columnComboBox.AutoCompleteCustomSource = [.. CurrentColumnizer.GetColumnNames()];

            if (setFocus)
            {
                _ = columnComboBox.Focus();
            }
        }
        else
        {
            _ = dataGridView.Focus();
        }

        tableLayoutPanel1.RowStyles[0].Height = show ? 28 : 0;
    }

    #endregion

    #region Overrides

    protected override string GetPersistString ()
    {
        return "LogWindow#" + FileName;
    }

    #endregion

    [SupportedOSPlatform("windows")]
    private void OnButtonSizeChanged (object sender, EventArgs e)
    {
        if (sender is Button button && button.Image != null)
        {
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.Image = new Bitmap(button.Image, new Size(button.Size.Height, button.Size.Height));
        }
    }

    // used for external wait fx WaitForLoadFinished()

    private delegate void SelectLineFx (int line, bool triggerSyncCall);


    private delegate void SetColumnizerFx (ILogLineMemoryColumnizer columnizer);


    private delegate void SetBookmarkFx (int lineNum, string comment);

    private delegate void FunctionWith1BoolParam (bool arg);

    private delegate void ActionPluginExecuteFx (string keyword, string param, ILogExpertCallbackMemory callback, ILogLineMemoryColumnizer columnizer);

    private delegate void PositionAfterReloadFx (ReloadMemento reloadMemento);

    private delegate void AutoResizeColumnsFx (BufferedDataGridView gridView);

    private delegate bool BoolReturnDelegate ();

    // =================== ILogLineColumnizerCallback ============================

#if DEBUG
    [SupportedOSPlatform("windows")]
    internal void DumpBufferInfo ()
    {
        var currentLineNum = dataGridView.CurrentCellAddress.Y;
        _logFileReader.LogBufferInfoForLine(currentLineNum);
    }

    internal void DumpBufferDiagnostic ()
    {
        _logFileReader.LogBufferDiagnostic();
    }
#endif

    [SupportedOSPlatform("windows")]
    void ILineSelectable.SelectLine (int lineNum, bool triggerSyncCall, bool shouldScroll)
    {
        SelectLine(lineNum, triggerSyncCall, shouldScroll);
    }

    #region Event Handlers

    [SupportedOSPlatform("windows")]
    private void AutoResizeFilterBox ()
    {
        var desired = filterComboBox.Left + filterComboBox.GetMaxTextWidth();
        if (FilterSplitterLayout.TryClampSplitterDistance(desired, filterSplitContainer.Width, filterSplitContainer.SplitterWidth, filterSplitContainer.Panel1MinSize, filterSplitContainer.Panel2MinSize, out var distance))
        {
            filterSplitContainer.SplitterDistance = distance;
        }
    }

    #region Events handler

    protected void OnProgressBarUpdate (ProgressEventArgs e)
    {
        ProgressBarUpdate?.Invoke(this, e);
    }

    protected void OnStatusLine (StatusLineEventArgs e)
    {
        StatusLineEvent?.Invoke(this, e);
    }

    protected void OnGuiState (GuiStateEventArgs e)
    {
        GuiStateUpdate?.Invoke(this, e);
    }

    protected void OnTailFollowed (EventArgs e)
    {
        TailFollowed?.Invoke(this, e);
    }

    protected void OnFileNotFound (EventArgs e)
    {
        FileNotFound?.Invoke(this, e);
    }

    protected void OnFileRespawned (EventArgs e)
    {
        FileRespawned?.Invoke(this, e);
    }

    protected void OnFilterListChanged (LogWindow source)
    {
        FilterListChanged?.Invoke(this, new FilterListChangedEventArgs(source));
    }

    protected void OnCurrentHighlightListChanged ()
    {
        CurrentHighlightGroupChanged?.Invoke(this, new CurrentHighlightGroupChangedEventArgs(this, _currentHighlightGroup));
    }

    //TODO Double Check why the bookmark Providers have the Event and the LogWindow, and if it still necessary
    protected void OnBookmarksChanged ()
    {
        BookmarksChanged?.Invoke(this, EventArgs.Empty);
    }

    protected void OnBookmarkTextChanged (Bookmark bookmark)
    {
        BookmarkTextChanged?.Invoke(this, new BookmarkEventArgs(bookmark));
    }

    protected void OnColumnizerChanged (ILogLineMemoryColumnizer columnizer)
    {
        ColumnizerChanged?.Invoke(this, new ColumnizerEventArgs(columnizer));
    }

    protected void OnRegisterCancelHandler (IBackgroundProcessCancelHandler handler)
    {
        lock (_cancelHandlerList)
        {
            _cancelHandlerList.Add(handler);
        }
    }

    protected void OnDeRegisterCancelHandler (IBackgroundProcessCancelHandler handler)
    {
        lock (_cancelHandlerList)
        {
            _ = _cancelHandlerList.Remove(handler);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnLogWindowLoad (object sender, EventArgs e)
    {
        var setLastColumnWidth = Preferences.SetLastColumnWidth;
        var lastColumnWidth = Preferences.LastColumnWidth;

        PreferencesChanged(Preferences.Font, setLastColumnWidth, lastColumnWidth, true, SettingsFlags.GuiOrColors);
    }

    [SupportedOSPlatform("windows")]
    private void OnLogWindowDisposed (object sender, EventArgs e)
    {
        _waitingForClose = true;
        _logWindowCoordinator.HighlightSettingsChanged -= OnParentHighlightSettingsChanged;
        _logFileReader?.DeleteAllContent();

        FreeFromTimeSync();
    }

    [SupportedOSPlatform("windows")]
    private void OnLogFileReaderLoadingStarted (object sender, LoadFileEventArgs e)
    {
        _ = Invoke(LoadingStarted, e);
    }

    [SupportedOSPlatform("windows")]
    private void OnLogFileReaderFinishedLoading (object sender, EventArgs e)
    {
        //Thread.CurrentThread.Name = "FinishedLoading event thread";
        //_logger.Info($"Finished loading.");
        _isLoading = false;
        _isDeadFile = false;
        if (!_waitingForClose)
        {
            _ = Invoke(new MethodInvoker(LoadingFinished));
            _ = Invoke(new MethodInvoker(LoadPersistenceData));
            _ = Invoke(new MethodInvoker(SetGuiAfterLoading));
            _ = _loadingFinishedEvent.Set();
            _ = _externaLoadingFinishedEvent.Set();
            _timeSpreadCalc.SetLineCount(_logFileReader.LineCount);

            if (_reloadMemento != null)
            {
                _ = Invoke(new PositionAfterReloadFx(PositionAfterReload), _reloadMemento);
            }

            if (filterTailCheckBox.Checked)
            {
                //_logger.Info("Refreshing filter view because of reload.");
                _ = Invoke(new MethodInvoker(FilterSearch)); // call on proper thread
            }

            HandleChangedFilterList();

            _ = Invoke(new MethodInvoker(RunHighlightBookmarkScan));
        }

        _reloadMemento = null;
    }

    [SupportedOSPlatform("windows")]
    private void OnLogFileReaderFileNotFound (object sender, EventArgs e)
    {
        if (!IsDisposed && !Disposing)
        {
            //_logger.Info($"Handling file not found event.");
            _isDeadFile = true;
            _ = BeginInvoke(new MethodInvoker(LogfileDead));
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnLogFileReaderRespawned (object sender, EventArgs e)
    {
        _ = BeginInvoke(new MethodInvoker(LogfileRespawned));
    }

    [SupportedOSPlatform("windows")]
    private void OnLogWindowClosing (object sender, CancelEventArgs e)
    {
        if (Preferences.AskForClose && MessageBox.Show(Resources.LogWindow_UI_SureToClose, Resources.LogExpert_Common_UI_Title_LogExpert, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
        {
            e.Cancel = true;
            return;
        }

        SavePersistenceData(false);
        CloseLogWindow();
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewColumnDividerDoubleClick (object sender, DataGridViewColumnDividerDoubleClickEventArgs e)
    {
        e.Handled = true;
        AutoResizeColumns(dataGridView);
    }

    /**
   * Event handler for the Load event from LogfileReader
   */
    [SupportedOSPlatform("windows")]
    private void OnLogFileReaderLoadFile (object sender, LoadFileEventArgs e)
    {
        if (e.NewFile)
        {
            // File was new created (e.g. rollover)
            _isDeadFile = false;
            UnRegisterLogFileReaderEvents();
            dataGridView.CurrentCellChanged -= OnDataGridViewCurrentCellChanged;
            MethodInvoker invoker = ReloadNewFile;
            _ = BeginInvoke(invoker);
        }
        else if (_isLoading)
        {
            _ = BeginInvoke(UpdateProgress, e);
        }
    }

    private void OnFileSizeChanged (object sender, LogEventArgs e)
    {
        _tailFollowEngine.Post(e);
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewCellValueNeeded (object sender, DataGridViewCellValueEventArgs e)
    {
        if (_isClosing)
        {
            e.Value = Column.EmptyColumn;
            return;
        }

        PrefetchVisibleLines();

        var startCount = CurrentColumnizer?.GetColumnCount() ?? 0;
        e.Value = GetCellValue(e.RowIndex, e.ColumnIndex);

        // The new column could be find dynamically.
        // Only support add new columns for now.
        // TODO: Support reload all columns?
        if (CurrentColumnizer != null && CurrentColumnizer.GetColumnCount() > startCount)
        {
            for (var i = startCount; i < CurrentColumnizer.GetColumnCount(); i++)
            {
                var colName = CurrentColumnizer.GetColumnNames()[i];
                _ = dataGridView.Columns.Add(PaintHelper.CreateTitleColumn(colName));
            }
        }
    }

    private void PrefetchVisibleLines ()
    {
        if (_logFileReader == null)
        {
            return;
        }

        var firstVisible = dataGridView.FirstDisplayedScrollingRowIndex;
        var visibleCount = dataGridView.DisplayedRowCount(includePartialRow: true);

        if (firstVisible >= 0 && visibleCount > 0)
        {
            _columnCache.Prefetch(_logFileReader, firstVisible, visibleCount);
        }
    }

    private void PrefetchFilterVisibleLines ()
    {
        if (_logFileReader == null || _filterResultList == null || _filterResultList.Count == 0)
        {
            return;
        }

        var firstVisible = filterGridView.FirstDisplayedScrollingRowIndex;
        var visibleCount = filterGridView.DisplayedRowCount(includePartialRow: true);

        if (firstVisible < 0 || visibleCount <= 0)
        {
            return;
        }

        // The filter grid maps grid rows -> original line numbers.
        // We need to pin the actual line buffers for the visible filter rows.
        var endVisible = Math.Min(firstVisible + visibleCount, _filterResultList.Count);
        var span = CollectionsMarshal.AsSpan(_filterResultList)[firstVisible..endVisible];

        var minLine = span[0];
        var maxLine = minLine;

        for (int i = 0; i < span.Length; i++)
        {
            int lineNum = span[i];

            if (lineNum < minLine)
            {
                minLine = lineNum;
            }
            else if (lineNum > maxLine)
            {
                maxLine = lineNum;
            }
        }

        // Prefetch the tight range covering only the visible filter rows.
        // For sorted filter results this is the same as before but
        // bounded to actual visible rows, not the entire result set.
        _filterColumnCache.Prefetch(_logFileReader, minLine, maxLine - minLine + 1);
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewCellValuePushed (object sender, DataGridViewCellValueEventArgs e)
    {
        if (!CurrentColumnizer.IsTimeshiftImplemented())
        {
            return;
        }

        var line = _logFileReader.GetLogLineMemory(e.RowIndex);
        var offset = CurrentColumnizer.GetTimeOffset();
        CurrentColumnizer.SetTimeOffset(0);
        ColumnizerCallbackObject.SetLineNum(e.RowIndex);
        var cols = CurrentColumnizer.SplitLine(ColumnizerCallbackObject, line);
        CurrentColumnizer.SetTimeOffset(offset);
        if (cols.ColumnValues.Length <= e.ColumnIndex - 2)
        {
            return;
        }

        var oldValue = cols.ColumnValues[e.ColumnIndex - 2].FullValue;
        var newValue = (string)e.Value;

        CurrentColumnizer.PushValue(ColumnizerCallbackObject, e.ColumnIndex - 2, newValue, oldValue);
        dataGridView.Refresh();

        TimeSpan timeSpan = new(CurrentColumnizer.GetTimeOffset() * TimeSpan.TicksPerMillisecond);
        var span = timeSpan.ToString();
        var index = span.LastIndexOf('.');
        if (index > 0)
        {
            span = span[..(index + 4)];
        }

        SetTimeshiftValue(span);
        SendGuiStateUpdate();
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewRowHeightInfoNeeded (object sender, DataGridViewRowHeightInfoNeededEventArgs e)
    {
        e.Height = GetRowHeight(e.RowIndex);
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewCurrentCellChanged (object sender, EventArgs e)
    {
        if (dataGridView.CurrentRow != null)
        {
            _statusEventArgs.CurrentLineNum = dataGridView.CurrentRow.Index + 1;
            SendStatusLineUpdate();
            if (syncFilterCheckBox.Checked)
            {
                SyncFilterGridPos();
            }

            if (CurrentColumnizer.IsTimeshiftImplemented() && Preferences.TimestampControl)
            {
                SyncTimestampDisplay();
            }
        }
    }

    private void OnDataGridViewCellEndEdit (object sender, DataGridViewCellEventArgs e)
    {
        StatusLineText(string.Empty);
    }

    [SupportedOSPlatform("windows")]
    private void OnEditControlKeyUp (object sender, KeyEventArgs e)
    {
        UpdateEditColumnDisplay((DataGridViewTextBoxEditingControl)sender);
    }

    [SupportedOSPlatform("windows")]
    private void OnEditControlKeyPress (object sender, KeyPressEventArgs e)
    {
        UpdateEditColumnDisplay((DataGridViewTextBoxEditingControl)sender);
    }

    [SupportedOSPlatform("windows")]
    private void OnEditControlClick (object sender, EventArgs e)
    {
        UpdateEditColumnDisplay((DataGridViewTextBoxEditingControl)sender);
    }

    [SupportedOSPlatform("windows")]
    private void OnEditControlKeyDown (object sender, KeyEventArgs e)
    {
        UpdateEditColumnDisplay((DataGridViewTextBoxEditingControl)sender);
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewPaint (object sender, PaintEventArgs e)
    {
        if (ShowBookmarkBubbles)
        {
            AddBookmarkOverlays();
        }
    }

    // ======================================================================================
    // Filter Grid stuff
    // ======================================================================================

    [SupportedOSPlatform("windows")]
    private void OnFilterSearchButtonClick (object sender, EventArgs e)
    {
        FilterSearch();
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterGridViewCellPainting (object sender, DataGridViewCellPaintingEventArgs e)
    {
        var gridView = (BufferedDataGridView)sender;
        CellPainting(gridView.Focused, e.RowIndex, e.ColumnIndex, true, e);
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterGridViewCellValueNeeded (object sender, DataGridViewCellValueEventArgs e)
    {
        if (_isClosing || e.RowIndex < 0 || e.ColumnIndex < 0 || _filterResultList.Count <= e.RowIndex)
        {
            e.Value = string.Empty;
            return;
        }

        PrefetchFilterVisibleLines();

        var lineNum = _filterResultList[e.RowIndex];
        e.Value = GetFilterCellValue(lineNum, e.ColumnIndex);
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterGridViewRowHeightInfoNeeded (object sender, DataGridViewRowHeightInfoNeededEventArgs e)
    {
        e.Height = _lineHeight;
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterComboBoxKeyDown (object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            FilterSearch();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterGridViewColumnDividerDoubleClick (object sender,
        DataGridViewColumnDividerDoubleClickEventArgs e)
    {
        e.Handled = true;
        AutoResizeColumnsFx fx = AutoResizeColumns;
        _ = BeginInvoke(fx, filterGridView);
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterGridViewCellDoubleClick (object sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex == 0)
        {
            ToggleBookmark();
            return;
        }

        if (filterGridView.CurrentRow != null && e.RowIndex >= 0)
        {
            var lineNum = _filterResultList[filterGridView.CurrentRow.Index];
            SelectAndEnsureVisible(lineNum, true);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnRangeCheckBoxCheckedChanged (object sender, EventArgs e)
    {
        filterRangeComboBox.Enabled = rangeCheckBox.Checked;
        CheckForFilterDirty();
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewScroll (object sender, ScrollEventArgs e)
    {
        if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
        {
            if (dataGridView.DisplayedRowCount(false) + dataGridView.FirstDisplayedScrollingRowIndex >= dataGridView.RowCount)
            {
                //this.guiStateArgs.FollowTail = true;
                if (!_guiStateArgs.FollowTail)
                {
                    FollowTailChanged(true, false);
                }

                OnTailFollowed(EventArgs.Empty);
            }
            else
            {
                //this.guiStateArgs.FollowTail = false;
                if (_guiStateArgs.FollowTail)
                {
                    FollowTailChanged(false, false);
                }
            }

            SendGuiStateUpdate();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterGridViewKeyDown (object sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Enter:
                {
                    if (filterGridView.CurrentCellAddress.Y >= 0 && filterGridView.CurrentCellAddress.Y < _filterResultList.Count)
                    {
                        var lineNum = _filterResultList[filterGridView.CurrentCellAddress.Y];
                        SelectLine(lineNum, false, true);
                        e.Handled = true;
                    }

                    break;
                }
            case Keys.Tab when e.Modifiers == Keys.None:
                _ = dataGridView.Focus();
                e.Handled = true;
                break;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewKeyDown (object sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Tab when e.Modifiers == Keys.None:
                {
                    _ = filterGridView.Focus();
                    e.Handled = true;
                    break;
                }
        }

        //switch (e.KeyCode)
        //{
        //    case Keys.Tab when e.Modifiers == Keys.Control:
        //        //this.parentLogTabWin.SwitchTab(e.Shift);
        //        break;
        //}

        _shouldCallTimeSync = true;
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewPreviewKeyDown (object sender, PreviewKeyDownEventArgs e)
    {
        if (e.KeyCode == Keys.Tab && e.Control)
        {
            e.IsInputKey = true;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewCellContentDoubleClick (object sender, DataGridViewCellEventArgs e)
    {
        if (dataGridView.CurrentCell != null)
        {
            _ = dataGridView.BeginEdit(false);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnSyncFilterCheckBoxCheckedChanged (object sender, EventArgs e)
    {
        if (syncFilterCheckBox.Checked)
        {
            SyncFilterGridPos();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewLeave (object sender, EventArgs e)
    {
        InvalidateCurrentRow(dataGridView);
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewEnter (object sender, EventArgs e)
    {
        InvalidateCurrentRow(dataGridView);
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterGridViewEnter (object sender, EventArgs e)
    {
        InvalidateCurrentRow(filterGridView);
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterGridViewLeave (object sender, EventArgs e)
    {
        InvalidateCurrentRow(filterGridView);
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewResize (object sender, EventArgs e)
    {
        if (_logFileReader != null && dataGridView.RowCount > 0 && _guiStateArgs.FollowTail)
        {
            dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.RowCount - 1;
        }
    }

    private void OnDataGridViewSelectionChanged (object sender, EventArgs e)
    {
        UpdateSelectionDisplay();
    }

    [SupportedOSPlatform("windows")]
    private void OnSelectionChangedTriggerSignal (object sender, EventArgs e)
    {
        var selCount = 0;
        try
        {
            //_logger.Debug("OnSelectionChangedTriggerSignal: Selection changed trigger");
            selCount = dataGridView.SelectedRows.Count;
            if (selCount > 1)
            {
                StatusLineText(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_StatusLineText_SelCountSelectedLines, selCount));
            }
            else
            {
                if (IsMultiFile)
                {
                    //MethodInvoker invoker = DisplayCurrentFileOnStatusline;
                    _ = Task.Run(DisplayCurrentFileOnStatusline);
                    //_ = invoker.BeginInvoke(null, null);
                }
                else
                {
                    StatusLineText(string.Empty);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in selectionChangedTrigger_Signal selcount {selCount}, Exception: {ex}");
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterKnobControlValueChanged (object sender, EventArgs e)
    {
        CheckForFilterDirty();
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterToTabButtonClick (object sender, EventArgs e)
    {
        FilterToTab();
    }

    private void OnPipeDisconnected (object sender, EventArgs e)
    {
        if (sender.GetType() == typeof(FilterPipe))
        {
            lock (_filterPipeList)
            {
                _ = _filterPipeList.Remove((FilterPipe)sender);
                if (_filterPipeList.Count == 0)
                // reset naming counter to 0 if no more open filter tabs for this source window
                {
                    _filterPipeNameCounter = 0;
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnAdvancedButtonClick (object sender, EventArgs e)
    {
        _showAdvanced = !_showAdvanced;
        ShowAdvancedFilterPanel(_showAdvanced);
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterSplitContainerMouseDown (object sender, MouseEventArgs e)
    {
        ((SplitContainer)sender).IsSplitterFixed = true;
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterSplitContainerMouseUp (object sender, MouseEventArgs e)
    {
        ((SplitContainer)sender).IsSplitterFixed = false;
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterSplitContainerMouseMove (object sender, MouseEventArgs e)
    {
        var splitContainer = (SplitContainer)sender;
        if (splitContainer.IsSplitterFixed)
        {
            if (e.Button.Equals(MouseButtons.Left))
            {
                var isVertical = splitContainer.Orientation.Equals(Orientation.Vertical);
                var desired = isVertical ? e.X : e.Y;
                var containerSize = isVertical ? splitContainer.Width : splitContainer.Height;

                if (FilterSplitterLayout.TryClampSplitterDistance(desired, containerSize, splitContainer.SplitterWidth, splitContainer.Panel1MinSize, splitContainer.Panel2MinSize, out var distance))
                {
                    splitContainer.SplitterDistance = distance;
                    splitContainer.Refresh();
                }
            }
            else
            {
                splitContainer.IsSplitterFixed = false;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterSplitContainerMouseDoubleClick (object sender, MouseEventArgs e)
    {
        AutoResizeFilterBox();
    }

    #region Context Menu

    [SupportedOSPlatform("windows")]
    private void OnDataGridContextMenuStripOpening (object sender, CancelEventArgs e)
    {
        var lineNum = -1;
        if (dataGridView.CurrentRow != null)
        {
            lineNum = dataGridView.CurrentRow.Index;
        }

        if (lineNum == -1)
        {
            return;
        }

        var (timeStamp, lastLineNumber) = GetTimestampForLine(lineNum, false);
        lineNum = lastLineNumber;

        copyToTabToolStripMenuItem.Enabled = dataGridView.SelectedCells.Count > 0;
        scrollAllTabsToTimestampToolStripMenuItem.Enabled = CurrentColumnizer.IsTimeshiftImplemented()
                                                            && timeStamp != DateTime.MinValue;

        locateLineInOriginalFileToolStripMenuItem.Enabled = IsTempFile &&
                                                            FilterPipe != null &&
                                                            FilterPipe.GetOriginalLineNum(lineNum) != -1;

        markEditModeToolStripMenuItem.Enabled = !dataGridView.CurrentCell.ReadOnly;

        // Remove all "old" plugin entries
        var index = dataGridContextMenuStrip.Items.IndexOf(pluginSeparator);

        if (index > 0)
        {
            for (var i = index + 1; i < dataGridContextMenuStrip.Items.Count;)
            {
                dataGridContextMenuStrip.Items.RemoveAt(i);
            }
        }

        // Add plugin entries
        var isAdded = false;
        if (_pluginRegistry.RegisteredContextMenuPlugins.Count > 0)
        {
            var lines = GetSelectedContent();
            foreach (var entry in _pluginRegistry.RegisteredContextMenuPlugins)
            {
                LogExpertCallback callback = new(this);
                var menuText = entry.GetMenuText(lines.Count, CurrentColumnizer, callback.GetLogLineMemory(lines[0]));

                if (menuText != null)
                {
                    var disabled = menuText.StartsWith('_');
                    if (disabled)
                    {
                        menuText = menuText[1..];
                    }

                    var item = dataGridContextMenuStrip.Items.Add(menuText, null, OnHandlePluginContextMenu);
                    item.Tag = new ContextMenuPluginEventArgs(entry, lines, CurrentColumnizer, callback);
                    item.Enabled = !disabled;
                    isAdded = true;
                }
            }
        }

        pluginSeparator.Visible = isAdded;

        // enable/disable Temp Highlight item
        tempHighlightsToolStripMenuItem.Enabled = _tempHighlightEntryList.Count > 0;

        markCurrentFilterRangeToolStripMenuItem.Enabled = !string.IsNullOrEmpty(filterRangeComboBox.Text);

        if (CurrentColumnizer.IsTimeshiftImplemented())
        {
            var list = _logWindowCoordinator.GetOpenFiles();
            syncTimestampsToToolStripMenuItem.Enabled = true;
            syncTimestampsToToolStripMenuItem.DropDownItems.Clear();
            EventHandler ev = OnHandleSyncContextMenu;
            Font italicFont = new(syncTimestampsToToolStripMenuItem.Font.FontFamily, syncTimestampsToToolStripMenuItem.Font.Size, FontStyle.Italic);

            foreach (var fileEntry in list)
            {
                if (fileEntry.LogWindow != this)
                {
                    var item = syncTimestampsToToolStripMenuItem.DropDownItems.Add(fileEntry.Title, null, ev) as ToolStripMenuItem;
                    item.Tag = fileEntry;
                    item.Checked = TimeSyncList != null && TimeSyncList.Contains(fileEntry.LogWindow);
                    if (fileEntry.LogWindow.TimeSyncList != null && !fileEntry.LogWindow.TimeSyncList.Contains(this))
                    {
                        item.Font = italicFont;
                        item.ForeColor = Color.Blue;
                    }

                    item.Enabled = fileEntry.LogWindow.CurrentColumnizer.IsTimeshiftImplemented();
                }
            }
        }
        else
        {
            syncTimestampsToToolStripMenuItem.Enabled = false;
        }

        freeThisWindowFromTimeSyncToolStripMenuItem.Enabled = TimeSyncList != null &&
                                                              TimeSyncList.Count > 1;
    }

    [SupportedOSPlatform("windows")]
    private void OnHandlePluginContextMenu (object sender, EventArgs args)
    {
        if (sender is ToolStripItem item)
        {
            var menuArgs = item.Tag as ContextMenuPluginEventArgs;
            var logLines = menuArgs.LogLines;
            menuArgs.Entry.MenuSelected(logLines.Count, menuArgs.Columnizer, menuArgs.Callback.GetLogLineMemory(logLines[0]));
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnHandleSyncContextMenu (object sender, EventArgs args)
    {
        if (sender is ToolStripItem item)
        {
            var entry = item.Tag as WindowFileEntry;

            if (TimeSyncList != null && TimeSyncList.Contains(entry.LogWindow))
            {
                FreeSlaveFromTimesync(entry.LogWindow);
            }
            else
            //AddSlaveToTimesync(entry.LogWindow);
            {
                AddOtherWindowToTimesync(entry.LogWindow);
                ShowTimeSyncSettingsHintOnce();
            }
        }
    }

    /// <summary>
    /// Selection-driven sync (mouse click / arrow keys) only runs when the timestamp control is
    /// enabled — the entry point is gated on <c>Preferences.TimestampControl</c>, which defaults to
    /// off. Users who time-sync files with it off see the explicit menu command work but selection
    /// do nothing, which reads as a broken feature. Shown at most once per window, at the moment
    /// sync is switched on from that window's context menu.
    /// </summary>
    private bool _timeSyncSettingsHintShown;

    private void ShowTimeSyncSettingsHintOnce ()
    {
        if (_timeSyncSettingsHintShown || Preferences.TimestampControl)
        {
            return;
        }

        _timeSyncSettingsHintShown = true;
        _ = MessageBox.Show(this, Resources.LogWindow_UI_TimeSync_TimestampControlHint,
            Resources.LogExpert_Common_UI_Title_LogExpert, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    [SupportedOSPlatform("windows")]
    private void OnCopyToolStripMenuItemClick (object sender, EventArgs e)
    {
        CopyMarkedLinesToClipboard();
    }

    private void OnCopyToTabToolStripMenuItemClick (object sender, EventArgs e)
    {
        CopyMarkedLinesToTab();
    }

    [SupportedOSPlatform("windows")]
    private void OnScrollAllTabsToTimestampToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (CurrentColumnizer.IsTimeshiftImplemented())
        {
            var currentLine = dataGridView.CurrentCellAddress.Y;
            if (currentLine > 0 && currentLine < dataGridView.RowCount)
            {
                var (timeStamp, _) = GetTimestampForLine(currentLine, false);
                if (timeStamp.Equals(DateTime.MinValue)) // means: invalid
                {
                    return;
                }

                _logWindowCoordinator.ScrollAllTabsToTimestamp(timeStamp, this);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnLocateLineInOriginalFileToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (dataGridView.CurrentRow != null && FilterPipe != null)
        {
            var lineNum = FilterPipe.GetOriginalLineNum(dataGridView.CurrentRow.Index);
            if (lineNum != -1)
            {
                FilterPipe.OriginWindow.SelectLine(lineNum, false, true);
                _logWindowCoordinator.SelectTab(FilterPipe.OriginWindow as LogWindow);
            }
        }
    }

    private void OnToggleBoomarkToolStripMenuItemClick (object sender, EventArgs e)
    {
        ToggleBookmark();
    }

    [SupportedOSPlatform("windows")]
    private void OnMarkEditModeToolStripMenuItemClick (object sender, EventArgs e)
    {
        StartEditMode();
    }

    private void OnLogWindowSizeChanged (object sender, EventArgs e)
    {
        //AdjustMinimumGridWith();
        AdjustHighlightSplitterWidth();
    }

    #region BookMarkList

    [SupportedOSPlatform("windows")]
    private void OnColumnRestrictCheckBoxCheckedChanged (object sender, EventArgs e)
    {
        btnColumn.Enabled = columnRestrictCheckBox.Checked;
        if (columnRestrictCheckBox.Checked) // disable when nothing to filter
        {
            columnNamesLabel.Visible = true;
            _filterParams.ColumnRestrict = true;
            columnNamesLabel.Text = CalculateColumnNames(_filterParams);
        }
        else
        {
            columnNamesLabel.Visible = false;
        }

        CheckForFilterDirty();
    }

    [SupportedOSPlatform("windows")]
    private void OnColumnButtonClick (object sender, EventArgs e)
    {
        _filterParams.CurrentColumnizer = CurrentColumnizer;
        FilterColumnChooser chooser = new(_filterParams);
        if (chooser.ShowDialog() == DialogResult.OK)
        {
            columnNamesLabel.Text = CalculateColumnNames(_filterParams);

            //CheckForFilterDirty(); //!!!GBro: Indicate to redo the search if search columns were changed
            btnfilterSearch.Image = _searchButtonImage;
            bntSaveFilter.Enabled = false;
        }
    }

    #endregion

    #region Column Header Context Menu

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewCellContextMenuStripNeeded (object sender, DataGridViewCellContextMenuStripNeededEventArgs e)
    {
        if (e.RowIndex >= 0 && e.RowIndex < dataGridView.RowCount && !dataGridView.Rows[e.RowIndex].Selected)
        {
            SelectLine(e.RowIndex, false, true);
        }
        else if (e.RowIndex < 0)
        {
            e.ContextMenuStrip = columnContextMenuStrip;
        }

        if (e.ContextMenuStrip == columnContextMenuStrip)
        {
            _selectedCol = e.ColumnIndex;
        }
    }

    //private void boomarkDataGridView_CellContextMenuStripNeeded(object sender, DataGridViewCellContextMenuStripNeededEventArgs e)
    //{
    //  if (e.RowIndex > 0 && e.RowIndex < this.boomarkDataGridView.RowCount
    //      && !this.boomarkDataGridView.Rows[e.RowIndex].Selected)
    //  {
    //    this.boomarkDataGridView.Rows[e.RowIndex].Selected = true;
    //    this.boomarkDataGridView.CurrentCell = this.boomarkDataGridView.Rows[e.RowIndex].Cells[0];
    //  }
    //  if (e.ContextMenuStrip == this.columnContextMenuStrip)
    //  {
    //    this.selectedCol = e.ColumnIndex;
    //  }
    //}

    [SupportedOSPlatform("windows")]
    private void OnFilterGridViewCellContextMenuStripNeeded (object sender, DataGridViewCellContextMenuStripNeededEventArgs e)
    {
        if (e.ContextMenuStrip == columnContextMenuStrip)
        {
            _selectedCol = e.ColumnIndex;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnColumnContextMenuStripOpening (object sender, CancelEventArgs e)
    {
        var ctl = columnContextMenuStrip.SourceControl;
        var gridView = ctl as BufferedDataGridView;
        var frozen = false;
        if (_freezeStateMap.TryGetValue(ctl, out var value))
        {
            frozen = value;
        }

        freezeLeftColumnsUntilHereToolStripMenuItem.Checked = frozen;

        if (frozen)
        {
            freezeLeftColumnsUntilHereToolStripMenuItem.Text = Resources.LogWindow_UI_Text_Frozen;
        }
        else
        {
            if (ctl is BufferedDataGridView)
            {
                freezeLeftColumnsUntilHereToolStripMenuItem.Text = string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_Text_FreezeLeftColumnsUntilHereGridViewColumns_selectedColHeaderText, gridView.Columns[_selectedCol].HeaderText);
            }
        }

        var col = gridView.Columns[_selectedCol];
        moveLeftToolStripMenuItem.Enabled = col != null && col.DisplayIndex > 0;
        moveRightToolStripMenuItem.Enabled = col != null && col.DisplayIndex < gridView.Columns.Count - 1;

        if (gridView.Columns.Count - 1 > _selectedCol)
        {
            //        DataGridViewColumn colRight = gridView.Columns[this.selectedCol + 1];
            var colRight = gridView.Columns.GetNextColumn(col, DataGridViewElementStates.None, DataGridViewElementStates.None);
            moveRightToolStripMenuItem.Enabled = colRight != null && colRight.Frozen == col.Frozen;
        }

        if (_selectedCol > 0)
        {
            //DataGridViewColumn colLeft = gridView.Columns[this.selectedCol - 1];
            var colLeft = gridView.Columns.GetPreviousColumn(col, DataGridViewElementStates.None, DataGridViewElementStates.None);

            moveLeftToolStripMenuItem.Enabled = colLeft != null && colLeft.Frozen == col.Frozen;
        }

        var colLast = gridView.Columns[gridView.Columns.Count - 1];
        moveToLastColumnToolStripMenuItem.Enabled = colLast != null && colLast.Frozen == col.Frozen;

        // Fill context menu with column names
        //
        EventHandler ev = OnHandleColumnItemContextMenu;
        allColumnsToolStripMenuItem.DropDownItems.Clear();
        foreach (DataGridViewColumn column in gridView.Columns)
        {
            if (column.HeaderText.Length > 0)
            {
                var item = allColumnsToolStripMenuItem.DropDownItems.Add(column.HeaderText, null, ev) as ToolStripMenuItem;
                item.Tag = column;
                item.Enabled = !column.Frozen;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnHandleColumnItemContextMenu (object sender, EventArgs args)
    {
        if (sender is ToolStripItem item)
        {
            var column = item.Tag as DataGridViewColumn;
            column.Visible = true;
            column.DataGridView.FirstDisplayedScrollingColumnIndex = column.Index;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFreezeLeftColumnsUntilHereToolStripMenuItemClick (object sender, EventArgs e)
    {
        var ctl = columnContextMenuStrip.SourceControl;
        var frozen = false;

        if (_freezeStateMap.TryGetValue(ctl, out var value))
        {
            frozen = value;
        }

        frozen = !frozen;
        _freezeStateMap[ctl] = frozen;

        if (ctl is BufferedDataGridView gridView)
        {
            ApplyFrozenState(gridView);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnMoveToLastColumnToolStripMenuItemClick (object sender, EventArgs e)
    {
        var gridView = columnContextMenuStrip.SourceControl as BufferedDataGridView;
        var col = gridView.Columns[_selectedCol];
        _ = col?.DisplayIndex = gridView.Columns.Count - 1;
    }

    [SupportedOSPlatform("windows")]
    private void OnMoveLeftToolStripMenuItemClick (object sender, EventArgs e)
    {
        var gridView = columnContextMenuStrip.SourceControl as BufferedDataGridView;
        var col = gridView.Columns[_selectedCol];
        if (col != null && col.DisplayIndex > 0)
        {
            col.DisplayIndex -= 1;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnMoveRightToolStripMenuItemClick (object sender, EventArgs e)
    {
        var gridView = columnContextMenuStrip.SourceControl as BufferedDataGridView;
        var col = gridView.Columns[_selectedCol];
        if (col != null && col.DisplayIndex < gridView.Columns.Count - 1)
        {
            col.DisplayIndex++;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnHideColumnToolStripMenuItemClick (object sender, EventArgs e)
    {
        var gridView = columnContextMenuStrip.SourceControl as BufferedDataGridView;
        var col = gridView.Columns[_selectedCol];
        col.Visible = false;
    }

    [SupportedOSPlatform("windows")]
    private void OnRestoreColumnsToolStripMenuItemClick (object sender, EventArgs e)
    {
        var gridView = columnContextMenuStrip.SourceControl as BufferedDataGridView;
        foreach (DataGridViewColumn col in gridView.Columns)
        {
            col.Visible = true;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnTimeSpreadingControlLineSelected (object sender, SelectLineEventArgs e)
    {
        SelectLine(e.Line, false, true);
    }

    [SupportedOSPlatform("windows")]
    private void OnBookmarkCommentToolStripMenuItemClick (object sender, EventArgs e)
    {
        AddBookmarkAndEditComment();
    }

    [SupportedOSPlatform("windows")]
    private void OnHighlightSelectionInLogFileToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (dataGridView.EditingControl is DataGridViewTextBoxEditingControl ctl)
        {
            var he = new HighlightEntry()
            {
                SearchText = ctl.SelectedText,
                ForegroundColor = Color.Red,
                BackgroundColor = Color.Yellow,
                IsRegex = false,
                IsCaseSensitive = true,
                IsLedSwitch = false,
                IsSetBookmark = false,
                IsActionEntry = false,
                ActionEntry = null,
                IsWordMatch = false
            };

            lock (_tempHighlightEntryListLock)
            {
                _tempHighlightEntryList.Add(he);
            }

            _ = dataGridView.CancelEdit();
            _ = dataGridView.EndEdit();
            RefreshAllGrids();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnHighlightSelectionInLogFilewordModeToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (dataGridView.EditingControl is DataGridViewTextBoxEditingControl ctl)
        {
            HighlightEntry he = new()
            {
                SearchText = ctl.SelectedText,
                ForegroundColor = Color.Red,
                BackgroundColor = Color.Yellow,
                IsRegex = false,
                IsCaseSensitive = true,
                IsLedSwitch = false,
                IsStopTail = false,
                IsSetBookmark = false,
                IsActionEntry = false,
                ActionEntry = null,
                IsWordMatch = true
            };

            lock (_tempHighlightEntryListLock)
            {
                _tempHighlightEntryList.Add(he);
            }

            _ = dataGridView.CancelEdit();
            _ = dataGridView.EndEdit();
            RefreshAllGrids();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnEditModeCopyToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (dataGridView.EditingControl is DataGridViewTextBoxEditingControl ctl)
        {
            if (!string.IsNullOrEmpty(ctl.SelectedText) && !ClipboardHelper.TrySetText(ctl.SelectedText))
            {
                StatusLineError(Resources.LogExpert_Common_UI_Message_ClipboardInUse);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnRemoveAllToolStripMenuItemClick (object sender, EventArgs e)
    {
        RemoveTempHighlights();
    }

    [SupportedOSPlatform("windows")]
    private void OnMakePermanentToolStripMenuItemClick (object sender, EventArgs e)
    {
        lock (_tempHighlightEntryListLock)
        {
            lock (_currentHighlightGroupLock)
            {
                _currentHighlightGroup.HighlightEntryList.AddRange(_tempHighlightEntryList);
                RemoveTempHighlights();
                OnCurrentHighlightListChanged();
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnMarkCurrentFilterRangeToolStripMenuItemClick (object sender, EventArgs e)
    {
        MarkCurrentFilterRange();
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterForSelectionToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (dataGridView.EditingControl is DataGridViewTextBoxEditingControl ctl)
        {
            splitContainerLogWindow.Panel2Collapsed = false;
            ResetFilterControls();
            FilterSearch(ctl.SelectedText);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnSetSelectedTextAsBookmarkCommentToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (dataGridView.EditingControl is DataGridViewTextBoxEditingControl ctl)
        {
            AddBookmarkComment(ctl.SelectedText);
        }
    }

    private void OnDataGridViewCellClick (object sender, DataGridViewCellEventArgs e)
    {
        _shouldCallTimeSync = true;
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewCellDoubleClick (object sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex == 0)
        {
            ToggleBookmark();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewOverlayDoubleClicked (object sender, OverlayEventArgs e)
    {
        BookmarkComment(e.BookmarkOverlay.Bookmark);
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterRegexCheckBoxMouseUp (object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            RegexHelperDialog dlg = new()
            {
                ExpressionHistoryList = ConfigManager.Settings.RegexHistory.ExpressionHistoryList,
                TesttextHistoryList = ConfigManager.Settings.RegexHistory.TesttextHistoryList,
                Owner = this,
                CaseSensitive = filterCaseSensitiveCheckBox.Checked,
                Pattern = filterComboBox.Text
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ConfigManager.Settings.RegexHistory.ExpressionHistoryList = dlg.ExpressionHistoryList;
                ConfigManager.Settings.RegexHistory.TesttextHistoryList = dlg.TesttextHistoryList;

                filterCaseSensitiveCheckBox.Checked = dlg.CaseSensitive;
                filterComboBox.Text = dlg.Pattern;

                ConfigManager.Save(SettingsFlags.RegexHistory);
            }
        }
    }

    #endregion

    #region Filter-Highlight

    [SupportedOSPlatform("windows")]
    private void OnToggleHighlightPanelButtonClick (object sender, EventArgs e)
    {
        ToggleHighlightPanel(highlightSplitContainer.Panel2Collapsed);
    }

    private void OnSaveFilterButtonClick (object sender, EventArgs e)
    {
        var newParams = _filterParams.Clone();
        newParams.Color = Color.FromKnownColor(KnownColor.Black);
        ConfigManager.Settings.FilterList.Add(newParams);
        OnFilterListChanged(this);
    }

    [SupportedOSPlatform("windows")]
    private void OnDeleteFilterButtonClick (object sender, EventArgs e)
    {
        var index = listBoxFilter.SelectedIndex;
        if (index >= 0)
        {
            var filterParams = (FilterParams)listBoxFilter.Items[index];
            _ = ConfigManager.Settings.FilterList.Remove(filterParams);
            OnFilterListChanged(this);
            if (listBoxFilter.Items.Count > 0)
            {
                listBoxFilter.SelectedIndex = listBoxFilter.Items.Count - 1;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterUpButtonClick (object sender, EventArgs e)
    {
        var i = listBoxFilter.SelectedIndex;
        if (i > 0)
        {
            var filterParams = (FilterParams)listBoxFilter.Items[i];
            ConfigManager.Settings.FilterList.RemoveAt(i);
            i--;
            ConfigManager.Settings.FilterList.Insert(i, filterParams);
            OnFilterListChanged(this);
            listBoxFilter.SelectedIndex = i;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterDownButtonClick (object sender, EventArgs e)
    {
        var i = listBoxFilter.SelectedIndex;
        if (i < 0)
        {
            return;
        }

        if (i < listBoxFilter.Items.Count - 1)
        {
            var filterParams = (FilterParams)listBoxFilter.Items[i];
            ConfigManager.Settings.FilterList.RemoveAt(i);
            i++;
            ConfigManager.Settings.FilterList.Insert(i, filterParams);
            OnFilterListChanged(this);
            listBoxFilter.SelectedIndex = i;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterListBoxMouseDoubleClick (object sender, MouseEventArgs e)
    {
        if (listBoxFilter.SelectedIndex >= 0)
        {
            var filterParams = (FilterParams)listBoxFilter.Items[listBoxFilter.SelectedIndex];
            var newParams = filterParams.Clone();
            //newParams.historyList = ConfigManager.Settings.filterHistoryList;
            _filterParams = newParams;
            ReInitFilterParams(_filterParams);
            ApplyFilterParams();
            CheckForAdvancedButtonDirty();
            CheckForFilterDirty();
            btnfilterSearch.Image = _searchButtonImage;
            bntSaveFilter.Enabled = false;
            if (hideFilterListOnLoadCheckBox.Checked)
            {
                ToggleHighlightPanel(false);
            }

            if (filterOnLoadCheckBox.Checked)
            {
                FilterSearch();
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterListBoxDrawItem (object sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index >= 0)
        {
            var filterParams = (FilterParams)listBoxFilter.Items[e.Index];
            Rectangle rectangle = new(0, e.Bounds.Top, e.Bounds.Width, e.Bounds.Height);

            using var brush = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? new SolidBrush(listBoxFilter.BackColor)
                : new SolidBrush(filterParams.Color);

            e.Graphics.DrawString(filterParams.SearchText, e.Font, brush, new PointF(rectangle.Left, rectangle.Top));
            e.DrawFocusRectangle();
        }
    }

    [SupportedOSPlatform("windows")]
    // Color for filter list entry
    private void OnColorToolStripMenuItemClick (object sender, EventArgs e)
    {
        var i = listBoxFilter.SelectedIndex;
        if (i < listBoxFilter.Items.Count && i >= 0)
        {
            var filterParams = (FilterParams)listBoxFilter.Items[i];
            ColorDialog dlg = new()
            {
                CustomColors = [filterParams.Color.ToArgb()],
                Color = filterParams.Color
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                filterParams.Color = dlg.Color;
                listBoxFilter.Refresh();
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterCaseSensitiveCheckBoxCheckedChanged (object sender, EventArgs e)
    {
        CheckForFilterDirty();
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterRegexCheckBoxCheckedChanged (object sender, EventArgs e)
    {
        knobControlFuzzy.Enabled = !filterRegexCheckBox.Checked;
        lblfuzzy.Enabled = !filterRegexCheckBox.Checked;
        CheckForFilterDirty();
    }

    [SupportedOSPlatform("windows")]
    private void OnInvertFilterCheckBoxCheckedChanged (object sender, EventArgs e)
    {
        CheckForFilterDirty();
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterRangeComboBoxTextChanged (object sender, EventArgs e)
    {
        CheckForFilterDirty();
    }

    [SupportedOSPlatform("windows")]
    private void OnFuzzyKnobControlValueChanged (object sender, EventArgs e)
    {
        CheckForFilterDirty();
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterComboBoxTextChanged (object sender, EventArgs e)
    {
        CheckForFilterDirty();
    }

    [SupportedOSPlatform("windows")]
    private void OnSetBookmarksOnSelectedLinesToolStripMenuItemClick (object sender, EventArgs e)
    {
        SetBookmarksForSelectedFilterLines();
    }

    private void OnParentHighlightSettingsChanged (object sender, EventArgs e)
    {
        dataGridView.SelectionHighlight = Preferences.SelectionHighlight;
        filterGridView.SelectionHighlight = Preferences.SelectionHighlight;
        var groupName = _guiStateArgs.HighlightGroupName;
        SetCurrentHighlightGroup(groupName);
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterOnLoadCheckBoxMouseClick (object sender, MouseEventArgs e)
    {
        HandleChangedFilterOnLoadSetting();
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterOnLoadCheckBoxKeyPress (object sender, KeyPressEventArgs e)
    {
        HandleChangedFilterOnLoadSetting();
    }

    [SupportedOSPlatform("windows")]
    private void OnHideFilterListOnLoadCheckBoxMouseClick (object sender, MouseEventArgs e)
    {
        HandleChangedFilterOnLoadSetting();
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterToTabToolStripMenuItemClick (object sender, EventArgs e)
    {
        FilterToTab();
    }

    private void OnTimeSyncListWindowRemoved (object sender, EventArgs e)
    {
        var syncList = sender as TimeSyncList;
        lock (_timeSyncListLock)
        {
            if (syncList.Count == 0 || (syncList.Count == 1 && syncList.Contains(this)))
            {
                if (syncList == TimeSyncList)
                {
                    TimeSyncList = null;
                    OnSyncModeChanged();
                }
            }
        }
    }

    private void OnFreeThisWindowFromTimeSyncToolStripMenuItemClick (object sender, EventArgs e)
    {
        FreeFromTimeSync();
    }

    [SupportedOSPlatform("windows")]
    private void OnSplitContainerSplitterMoved (object sender, SplitterEventArgs e)
    {
        advancedFilterSplitContainer.SplitterDistance = FILTER_ADVANCED_SPLITTER_DISTANCE;
    }

    [SupportedOSPlatform("windows")]
    private void OnMarkFilterHitsInLogViewToolStripMenuItemClick (object sender, EventArgs e)
    {
        SearchParams p = new()
        {
            SearchText = _filterParams.SearchText,
            IsRegex = _filterParams.IsRegex,
            IsCaseSensitive = _filterParams.IsCaseSensitive
        };

        AddSearchHitHighlightEntry(p);
    }

    [SupportedOSPlatform("windows")]
    private void OnColumnComboBoxSelectionChangeCommitted (object sender, EventArgs e)
    {
        SelectColumn();
    }

    [SupportedOSPlatform("windows")]
    private void OnColumnComboBoxKeyDown (object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SelectColumn();
            _ = dataGridView.Focus();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnColumnComboBoxPreviewKeyDown (object sender, PreviewKeyDownEventArgs e)
    {
        if (e.KeyCode == Keys.Down && e.Modifiers == Keys.Alt)
        {
            columnComboBox.DroppedDown = true;
        }

        if (e.KeyCode == Keys.Enter)
        {
            e.IsInputKey = true;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnBookmarkProviderBookmarkRemoved (object sender, EventArgs e)
    {
        if (!_isLoading)
        {
            dataGridView.Refresh();
            filterGridView.Refresh();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnBookmarkProviderBookmarkAdded (object sender, EventArgs e)
    {
        if (!_isLoading)
        {
            dataGridView.Refresh();
            filterGridView.Refresh();
        }
    }

    private void OnBookmarkProviderAllBookmarksRemoved (object sender, EventArgs e)
    {
        // nothing
    }

    private void OnLogWindowLeave (object sender, EventArgs e)
    {
        InvalidateCurrentRow();
    }

    private void OnLogWindowEnter (object sender, EventArgs e)
    {
        InvalidateCurrentRow();
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewRowUnshared (object sender, DataGridViewRowEventArgs e)
    {
        //if (_logger.IsTraceEnabled)
        //{
        //    _logger.Trace($"Row unshared line {e.Row.Cells[1].Value}");
        //}
    }

    #endregion

    #endregion

    #endregion

    [SupportedOSPlatform("windows")]
    private void MeasureItem (object sender, MeasureItemEventArgs e)
    {
        e.ItemHeight = listBoxFilter.Font.Height;
    }

    #endregion

    #region Private Methods

    [SupportedOSPlatform("windows")]
    private void CreateDefaultViewStyle ()
    {
        var darkMode = Application.IsDarkModeEnabled;
        dataGridView.DefaultCellStyle = PaintHelper.GetDataGridViewCellStyle(darkMode);
        filterGridView.DefaultCellStyle = PaintHelper.GetDataGridViewCellStyle(darkMode);
        dataGridView.RowsDefaultCellStyle = PaintHelper.GetDataGridDefaultRowStyle(darkMode);
        filterGridView.RowsDefaultCellStyle = PaintHelper.GetDataGridDefaultRowStyle(darkMode);
        PaintHelper.ApplyGridViewTheme(dataGridView, darkMode);
        PaintHelper.ApplyGridViewTheme(filterGridView, darkMode);
    }

    [SupportedOSPlatform("windows")]
    private void RegisterLogFileReaderEvents ()
    {
        _logFileReader.LoadFile += OnLogFileReaderLoadFile;
        _logFileReader.LoadingFinished += OnLogFileReaderFinishedLoading;
        _logFileReader.LoadingStarted += OnLogFileReaderLoadingStarted;
        _logFileReader.FileNotFound += OnLogFileReaderFileNotFound;
        _logFileReader.Respawned += OnLogFileReaderRespawned;
        // FileSizeChanged is not registered here because it's registered after loading has finished
    }

    [SupportedOSPlatform("windows")]
    private void UnRegisterLogFileReaderEvents ()
    {
        if (_logFileReader != null)
        {
            _logFileReader.LoadFile -= OnLogFileReaderLoadFile;
            _logFileReader.LoadingFinished -= OnLogFileReaderFinishedLoading;
            _logFileReader.LoadingStarted -= OnLogFileReaderLoadingStarted;
            _logFileReader.FileNotFound -= OnLogFileReaderFileNotFound;
            _logFileReader.Respawned -= OnLogFileReaderRespawned;
            _logFileReader.FileSizeChanged -= OnFileSizeChanged;
        }
    }

    /// <summary>
    /// The single Session File read of a window open: deserializes the <c>.lxp</c> once (via
    /// <see cref="Persister"/>) and decomposes it into the Session Snapshot both apply phases
    /// work from. Returns null when there is nothing to load or the read fails (log + defaults,
    /// silent — the load error contract).
    /// </summary>
    private SessionSnapshot ReadSessionSnapshot (string fileName)
    {
        // Union of both apply phases' gates: the pre-load phase needs SaveSessions or a forced
        // file name, the post-load phase additionally honors the session-load one-shot flag.
        if (!Preferences.SaveSessions && !ForcePersistenceLoading && ForcedPersistenceFileName == null)
        {
            return null;
        }

        try
        {
            var persistenceData = ForcedPersistenceFileName == null
                ? Persister.LoadPersistenceData(fileName, Preferences, ConfigManager.ActiveSessionDir)
                : Persister.LoadPersistenceDataFromFixedFile(ForcedPersistenceFileName);

            return persistenceData == null ? null : SessionFileComposer.Decompose(persistenceData);
        }
        catch (Exception e) when (e is IOException or
                                       UnauthorizedAccessException or
                                       NotSupportedException or
                                       InvalidOperationException or
                                       ArgumentException)
        {
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(ReadSessionSnapshot), e));
            return null;
        }
    }

    /// <summary>
    /// The pre-load apply phase of the Session Snapshot: options that must be in place before
    /// the file content loads (encoding, multi-file, columnizer-by-name, tab name, highlight
    /// group, panel layout). The return value gates columnizer auto-detection in
    /// <see cref="LoadFile"/>, as before. Also performs the window open's single Session File
    /// read, keeping the snapshot for the post-load phase.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private bool LoadPersistenceOptions ()
    {
        if (InvokeRequired)
        {
            return (bool)Invoke(new BoolReturnDelegate(LoadPersistenceOptions));
        }

        _sessionSnapshot = ReadSessionSnapshot(FileName);

        if (!Preferences.SaveSessions && ForcedPersistenceFileName == null)
        {
            // The snapshot may still exist for the post-load phase (session load with
            // SaveSessions off) — but this phase does not apply it, as before.
            return false;
        }

        if (_sessionSnapshot == null)
        {
            //_logger.Info($"No persistence data for {FileName} found.");
            return false;
        }

        try
        {
            var snapshot = _sessionSnapshot;

            IsMultiFile = snapshot.MultiFile;
            _multiFileOptions = new MultiFileOptions
            {
                FormatPattern = snapshot.MultiFilePattern,
                MaxDayTry = snapshot.MultiFileMaxDays
            };

            if (string.IsNullOrEmpty(_multiFileOptions.FormatPattern))
            {
                _multiFileOptions = ObjectClone.Clone(Preferences.MultiFileOptions);
            }

            splitContainerLogWindow.SplitterDistance = snapshot.FilterPosition;
            splitContainerLogWindow.Panel2Collapsed = !snapshot.FilterVisible;
            ToggleHighlightPanel(snapshot.FilterSaveListVisible);
            ShowAdvancedFilterPanel(snapshot.FilterAdvanced);

            if (_reloadMemento == null)
            {
                PreSelectColumnizerByName(snapshot.Columnizer?.GetName());
            }

            if (snapshot.TabName != null)
            {
                Text = snapshot.TabName;
            }

            AdjustHighlightSplitterWidth();
            SetCurrentHighlightGroup(snapshot.HighlightGroupName);

            SetCellSelectionMode(snapshot.CellSelectMode, true);

            if (snapshot.MultiFileNames.Count > 0)
            {
                //_logger.Info($"Detected MultiFile name list in persistence options");
                _fileNames = new string[snapshot.MultiFileNames.Count];
                snapshot.MultiFileNames.CopyTo(_fileNames);
            }
            else
            {
                _fileNames = null;
            }

            SetExplicitEncoding(snapshot.Encoding);
            return true;
        }
        catch (Exception e)
        {
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(LoadPersistenceOptions), e));
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private void SetDefaultsFromPrefs ()
    {
        filterTailCheckBox.Checked = Preferences.FilterTail;
        syncFilterCheckBox.Checked = Preferences.FilterSync;
        FollowTailChanged(Preferences.FollowTail, false);
        _multiFileOptions = ObjectClone.Clone(Preferences.MultiFileOptions);
    }

    /// <summary>
    /// The post-load apply phase of the Session Snapshot: content-dependent state that needs the
    /// loaded file (bookmarks, row heights, scroll position, follow-tail, filters, filter tabs).
    /// Works from the snapshot kept by the window open's single read — no re-deserialization.
    /// Skipped entirely when the Rollover staleness rule fires; the pre-load options stay
    /// applied.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void LoadPersistenceData ()
    {
        if (InvokeRequired)
        {
            _ = Invoke(new MethodInvoker(LoadPersistenceData));
            return;
        }

        if (!Preferences.SaveSessions && !ForcePersistenceLoading && ForcedPersistenceFileName == null)
        {
            SetDefaultsFromPrefs();
            return;
        }

        if (IsTempFile)
        {
            SetDefaultsFromPrefs();
            return;
        }

        ForcePersistenceLoading = false; // force only 1 time (while session load)

        try
        {
            var snapshot = _sessionSnapshot;

            if (snapshot == null)
            {
                _logger.Info($"No persistence data for {FileName} found.");
                return;
            }

            if (SessionFileComposer.IsStale(snapshot, _logFileReader.LineCount))
            {
                // Outdated persistence data (logfile rollover): the content-dependent state is
                // discarded; the pre-load options are already applied and stay.
                return;
            }

            _bookmarkProvider.SetBookmarks(snapshot.BookmarkList);
            _rowHeightList = snapshot.RowHeightList;
            try
            {
                if (snapshot.CurrentLine >= 0 && snapshot.CurrentLine < dataGridView.RowCount)
                {
                    SelectLine(snapshot.CurrentLine, false, true);
                }
                else
                {
                    if (_logFileReader.LineCount > 0)
                    {
                        dataGridView.FirstDisplayedScrollingRowIndex = _logFileReader.LineCount - 1;
                        SelectLine(_logFileReader.LineCount - 1, false, true);
                    }
                }

                if (snapshot.FirstDisplayedLine >= 0 &&
                    snapshot.FirstDisplayedLine < dataGridView.RowCount)
                {
                    dataGridView.FirstDisplayedScrollingRowIndex = snapshot.FirstDisplayedLine;
                }

                // Applied once, here (was double-applied: always pre-load, here only when true).
                // Applying true after positioning keeps the jump-to-tail semantics.
                FollowTailChanged(snapshot.FollowTail, false);
            }
            catch (ArgumentOutOfRangeException)
            {
                // FirstDisplayedScrollingRowIndex calculates sometimes the wrong scrolling ranges???
            }

            if (Preferences.SaveFilters)
            {
                RestoreFilters(snapshot);
            }
        }
        catch (IOException e)
        {
            SetDefaultsFromPrefs();
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(LoadPersistenceData), e));
        }
    }

    // Note: the panel layout fields (FilterPosition, FilterVisible, FilterAdvanced) are no
    // longer re-applied here — they are pre-load fields, applied once in LoadPersistenceOptions
    // (one field, one phase). Restored filter-tab child windows get them via
    // ApplyRestoredFilterTabSnapshot instead.
    [SupportedOSPlatform("windows")]
    private void RestoreFilters (SessionSnapshot snapshot)
    {
        if (snapshot.FilterParams != null)
        {
            _filterParams = snapshot.FilterParams;
            ReInitFilterParams(_filterParams);
        }

        ApplyFilterParams(); // re-loaded filter settings
        _ = BeginInvoke(new MethodInvoker(FilterSearch));

        if (_filterPipeList.Count == 0) // don't restore if it's only a reload
        {
            RestoreFilterTabs(snapshot);
        }
    }

    private void RestoreFilterTabs (SessionSnapshot snapshot)
    {
        foreach (var tab in snapshot.FilterTabs)
        {
            var persistFilterParams = tab.FilterParams;
            ReInitFilterParams(persistFilterParams);
            // Each restored tab runs its own filter with fresh spread history — its content no
            // longer depends on the window's shared history state at restore time.
            var filterRun = new SerialFilterEngine().Run(persistFilterParams, new ColumnizerCallback(this), CancellationToken.None);
            FilterPipe pipe = new(persistFilterParams.Clone(), this);
            WritePipeToTab(pipe, [.. filterRun.ResultLines], tab.Snapshot.TabName, tab.Snapshot);
        }
    }

    /// <summary>
    /// Applies a restored filter-tab child window's snapshot. A child window never runs the two
    /// window-open apply phases against its own snapshot, so the panel layout it would get
    /// pre-load is applied here, once, followed by its filters (and its own filter tabs,
    /// recursively).
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void ApplyRestoredFilterTabSnapshot (SessionSnapshot snapshot)
    {
        try
        {
            splitContainerLogWindow.SplitterDistance = snapshot.FilterPosition;
            splitContainerLogWindow.Panel2Collapsed = !snapshot.FilterVisible;
        }
        catch (InvalidOperationException e)
        {
            _logger.Error($"Error setting splitter distance: {e}");
        }

        ShowAdvancedFilterPanel(snapshot.FilterAdvanced);
        RestoreFilters(snapshot);
    }

    private void ReInitFilterParams (FilterParams filterParams)
    {
        filterParams.CurrentColumnizer = CurrentColumnizer;

        if (filterParams.IsRegex)
        {
            try
            {
                filterParams.CreateRegex();
            }
            catch (ArgumentException)
            {
                StatusLineError(Resources.LogWindow_UI_StatusLineError_InvalidRegularExpression);
            }
        }
    }

    private void EnterLoadFileStatus ()
    {
        if (InvokeRequired)
        {
            _ = Invoke(new MethodInvoker(EnterLoadFileStatus));
            return;
        }

        _statusEventArgs.StatusText = Resources.LogWindow_UI_StatusText_LoadingFile;
        _statusEventArgs.LineCount = 0;
        _statusEventArgs.FileSize = 0;
        SendStatusLineUpdate();

        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = 0;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        _isLoading = true;
        FireCancelHandlers(); // reload cancels the jobs of the old content, not the window lifetime
        _searchCts?.Cancel();
        ClearFilterList();
        ClearBookmarkList();
        dataGridView.ClearSelection();
        dataGridView.RowCount = 0;
    }

    [SupportedOSPlatform("windows")]
    private void PositionAfterReload (ReloadMemento reloadMemento)
    {
        if (_reloadMemento.CurrentLine < dataGridView.RowCount && _reloadMemento.CurrentLine >= 0)
        {
            dataGridView.CurrentCell = dataGridView.Rows[_reloadMemento.CurrentLine].Cells[0];
        }

        if (_reloadMemento.FirstDisplayedLine < dataGridView.RowCount && _reloadMemento.FirstDisplayedLine >= 0)
        {
            dataGridView.FirstDisplayedScrollingRowIndex = _reloadMemento.FirstDisplayedLine;
        }
    }

    [SupportedOSPlatform("windows")]
    private void LogfileDead ()
    {
        _isDeadFile = true;

        //this.logFileReader.FileSizeChanged -= this.FileSizeChangedHandler;
        //if (this.logFileReader != null)
        //  this.logFileReader.stopMonitoring();

        dataGridView.Enabled = false;
        dataGridView.RowCount = 0;
        _progressEventArgs.Visible = false;
        _progressEventArgs.Value = _progressEventArgs.MaxValue;
        SendProgressBarUpdate();
        _statusEventArgs.FileSize = 0;
        _statusEventArgs.LineCount = 0;
        _statusEventArgs.CurrentLineNum = 0;
        SendStatusLineUpdate();
        FireCancelHandlers(); // a dead file may respawn — cancel its jobs, keep the window lifetime
        _searchCts?.Cancel();
        ClearFilterList();
        ClearBookmarkList();

        StatusLineText(Resources.LogWindow_UI_StatusLineText_FileNotFound);
        OnFileNotFound(EventArgs.Empty);
    }

    [SupportedOSPlatform("windows")]
    private void LogfileRespawned ()
    {
        //_logger.Info($"Reloading file because it has been respawned.");
        _isDeadFile = false;
        dataGridView.Enabled = true;
        StatusLineText(string.Empty);
        OnFileRespawned(EventArgs.Empty);
        Reload();
    }

    [SupportedOSPlatform("windows")]
    private void SetGuiAfterLoading ()
    {
        if (Text.Length == 0)
        {
            Text = IsTempFile
                ? TempTitleName
                : Util.GetNameFromPath(FileName);
        }

        ShowBookmarkBubbles = Preferences.ShowBubbles;
        //if (this.forcedColumnizer == null)
        {
            ILogLineMemoryColumnizer columnizer;
            if (_forcedColumnizerForLoading != null)
            {
                columnizer = _forcedColumnizerForLoading;
                _forcedColumnizerForLoading = null;
            }
            else
            {
                columnizer = FindColumnizer();
                if (columnizer != null)
                {
                    if (_reloadMemento == null)
                    {
                        columnizer = ColumnizerPicker.CloneMemoryColumnizer(columnizer, ConfigManager.ActiveConfigDir);
                    }
                }
                else
                {
                    // Default Columnizers
                    columnizer = ColumnizerPicker.CloneMemoryColumnizer(ColumnizerPicker.FindMemoryColumnizer(FileName, _logFileReader, _pluginRegistry.RegisteredColumnizers), ConfigManager.ActiveConfigDir);
                }
            }

            _ = Invoke(new SetColumnizerFx(SetColumnizer), columnizer);
        }

        dataGridView.Enabled = true;
        DisplayCurrentFileOnStatusline();
        //this.guiStateArgs.FollowTail = this.Preferences.followTail;
        _guiStateArgs.MultiFileEnabled = !IsTempFile;
        _guiStateArgs.MenuEnabled = true;
        _guiStateArgs.CurrentEncoding = _logFileReader.CurrentEncoding;
        SendGuiStateUpdate();
        //if (this.dataGridView.RowCount > 0)
        //  SelectLine(this.dataGridView.RowCount - 1);
        //if (this.dataGridView.Columns.Count > 1)
        //{
        //  this.dataGridView.Columns[this.dataGridView.Columns.Count-1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        //  this.dataGridView.Columns[this.dataGridView.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet;
        //  AdjustMinimumGridWith();
        //}
        if (CurrentColumnizer.IsTimeshiftImplemented())
        {
            if (Preferences.TimestampControl)
            {
                SetTimestampLimits();
                SyncTimestampDisplay();
            }

            var settings = ConfigManager.Settings;
            ShowLineColumn(!settings.HideLineColumn);
        }

        ShowTimeSpread(Preferences.ShowTimeSpread && CurrentColumnizer.IsTimeshiftImplemented());
        locateLineInOriginalFileToolStripMenuItem.Enabled = FilterPipe != null;
    }

    private ILogLineMemoryColumnizer FindColumnizer ()
    {
        return _logWindowCoordinator.ResolveColumnizer(FileName);
    }

    private void ReloadNewFile ()
    {
        // prevent "overloads". May occur on very fast rollovers (next rollover before the file is reloaded)
        lock (_reloadLock)
        {
            _reloadOverloadCounter++;
            //_logger.Info($"ReloadNewFile(): counter = {_reloadOverloadCounter}");
            if (_reloadOverloadCounter <= 1)
            {
                SavePersistenceData(false);
                _ = _loadingFinishedEvent.Reset();
                _ = _externaLoadingFinishedEvent.Reset();
                Thread reloadFinishedThread = new(ReloadFinishedThreadFx)
                {
                    IsBackground = true
                };
                reloadFinishedThread.Start();
                LoadFile(FileName, EncodingOptions);

                ClearBookmarkList();
                SavePersistenceData(false);

                //if (this.filterTailCheckBox.Checked)
                //{
                //  _logger.Debug($"Waiting for loading to be complete.");
                //  loadingFinishedEvent.WaitOne();
                //  _logger.Debug($"Refreshing filter view because of reload.");
                //  FilterSearch();
                //}
                //LoadFilterPipes();
            }
            //else
            //{
            //    //_logger.Debug($"Preventing reload because of recursive calls.");
            //}

            _reloadOverloadCounter--;
        }
    }

    [SupportedOSPlatform("windows")]
    private void ReloadFinishedThreadFx ()
    {
        //_logger.Info($"Waiting for loading to be complete.");
        _ = _loadingFinishedEvent.WaitOne();
        //_logger.Info("Refreshing filter view because of reload.");
        _ = Invoke(new MethodInvoker(FilterSearch));
        LoadFilterPipes();
    }

    private void UpdateProgress (LoadFileEventArgs loadFileEventArgs)
    {
        try
        {
            if (loadFileEventArgs.ReadPos >= loadFileEventArgs.FileSize)
            {
                //_logger.Warn(CultureInfo.InvariantCulture, "UpdateProgress(): ReadPos (" + e.ReadPos + ") is greater than file size (" + e.FileSize + "). Aborting Update");
                return;
            }

            _statusEventArgs.FileSize = loadFileEventArgs.ReadPos;
            //this.progressEventArgs.Visible = true;
            _progressEventArgs.MaxValue = (int)loadFileEventArgs.FileSize;
            _progressEventArgs.Value = (int)loadFileEventArgs.ReadPos;
            SendProgressBarUpdate();
            SendStatusLineUpdate();
        }
        catch (Exception e)
        {
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(UpdateProgress), e));
        }
    }

    private void LoadingStarted (LoadFileEventArgs loadFileEventArgs)
    {
        try
        {
            _statusEventArgs.FileSize = loadFileEventArgs.ReadPos;
            _statusEventArgs.StatusText = string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_StatusText_LoadingWithParameter, Util.GetNameFromPath(loadFileEventArgs.FileName));
            _progressEventArgs.Visible = true;
            _progressEventArgs.MaxValue = (int)loadFileEventArgs.FileSize;
            _progressEventArgs.Value = (int)loadFileEventArgs.ReadPos;
            SendProgressBarUpdate();
            SendStatusLineUpdate();
        }
        catch (Exception e)
        {
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(LoadingStarted), e));
        }
    }

    private void LoadingFinished ()
    {
        //_logger.Info($"File loading complete.");

        StatusLineText(string.Empty);
        _logFileReader.FileSizeChanged += OnFileSizeChanged;
        _isLoading = false;
        dataGridView.SuspendLayout();
        dataGridView.RowCount = _logFileReader.LineCount;
        dataGridView.CurrentCellChanged += OnDataGridViewCurrentCellChanged;
        dataGridView.Enabled = true;
        dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        dataGridView.ResumeLayout();
        _progressEventArgs.Visible = false;
        _progressEventArgs.Value = _progressEventArgs.MaxValue;
        SendProgressBarUpdate();
        //if (this.logFileReader.LineCount > 0)
        //{
        //  this.dataGridView.FirstDisplayedScrollingRowIndex = this.logFileReader.LineCount - 1;
        //  SelectLine(this.logFileReader.LineCount - 1);
        //}
        _guiStateArgs.FollowTail = true;
        SendGuiStateUpdate();
        _statusEventArgs.LineCount = _logFileReader.LineCount;
        _statusEventArgs.FileSize = _logFileReader.FileSize;
        SendStatusLineUpdate();

        var setLastColumnWidth = Preferences.SetLastColumnWidth;
        var lastColumnWidth = Preferences.LastColumnWidth;

        PreferencesChanged(Preferences.Font, setLastColumnWidth, lastColumnWidth, true, SettingsFlags.All);
        //LoadPersistenceData();
    }

    // ITailFollowSink — the rendering side of the Tail Follow Engine seam. All four members run
    // on the engine's worker thread; marshalling to the UI thread happens inside the callbacks.

    bool ITailFollowSink.IsAbandoned => IsDisposed || Disposing || _waitingForClose;

    void ITailFollowSink.OnRolloverShift (int rolloverOffset)
    {
        ShiftBookmarks(rolloverOffset);
        ShiftRowHeightList(rolloverOffset);
        ShiftFilterPipes(rolloverOffset);
    }

    void ITailFollowSink.OnTailLines (LogEventArgs e)
    {
        _ = BeginInvoke(UpdateGrid, [e]);
        CheckFilterAndHighlight(e);
    }

    void ITailFollowSink.OnLineCountChanged (int lineCount)
    {
        _timeSpreadCalc.SetLineCount(lineCount);
    }


    /// <summary>
    /// Waits (briefly) for a long-running worker task to drain during teardown. Workers observe cancellation via
    /// <see cref="_cts"/> plus their wake-up events; the bounded timeout guards against a worker blocked in I/O so
    /// closing the window can never hang. Faults are swallowed because this only runs while tearing down.
    /// </summary>
    private static void JoinWorker (Task worker)
    {
        try
        {
            _ = worker.Wait(WORKER_SHUTDOWN_TIMEOUT);
        }
        catch (AggregateException)
        {
            // A worker threw on its way out; nothing actionable during teardown.
        }
    }

    private void OnFileSizeChanged (LogEventArgs logEventArgs)
    {
        FileSizeChanged?.Invoke(this, logEventArgs);
    }

    private void UpdateGrid (LogEventArgs logEventArgs)
    {
        var oldRowCount = dataGridView.RowCount;
        var firstDisplayedLine = dataGridView.FirstDisplayedScrollingRowIndex;

        if (dataGridView.CurrentCellAddress.Y >= logEventArgs.LineCount)
        {
            //this.dataGridView.Rows[this.dataGridView.CurrentCellAddress.Y].Selected = false;
            //this.dataGridView.CurrentCell = this.dataGridView.Rows[0].Cells[0];
        }

        try
        {
            if (dataGridView.RowCount > logEventArgs.LineCount)
            {
                var currentLineNum = dataGridView.CurrentCellAddress.Y;
                dataGridView.RowCount = 0;
                dataGridView.RowCount = logEventArgs.LineCount;
                if (!_guiStateArgs.FollowTail)
                {
                    if (currentLineNum >= dataGridView.RowCount)
                    {
                        currentLineNum = dataGridView.RowCount - 1;
                    }

                    dataGridView.CurrentCell = dataGridView.Rows[currentLineNum].Cells[0];
                }
            }
            else
            {
                dataGridView.RowCount = logEventArgs.LineCount;
            }

            //_logger.Debug($"UpdateGrid(): new RowCount={dataGridView.RowCount}");

            if (logEventArgs.IsRollover)
            {
                // Multifile rollover
                // keep selection and view range, if no follow tail mode
                if (!_guiStateArgs.FollowTail)
                {
                    var currentLineNum = dataGridView.CurrentCellAddress.Y;
                    currentLineNum -= logEventArgs.RolloverOffset;
                    if (currentLineNum < 0)
                    {
                        currentLineNum = 0;
                    }

                    //_logger.Debug($"UpdateGrid(): Rollover=true, Rollover offset={logEventArgs.RolloverOffset}, currLineNum was {dataGridView.CurrentCellAddress.Y}, new currLineNum={currentLineNum}");
                    firstDisplayedLine -= logEventArgs.RolloverOffset;
                    if (firstDisplayedLine < 0)
                    {
                        firstDisplayedLine = 0;
                    }

                    dataGridView.FirstDisplayedScrollingRowIndex = firstDisplayedLine;
                    dataGridView.CurrentCell = dataGridView.Rows[currentLineNum].Cells[0];
                    dataGridView.Rows[currentLineNum].Selected = true;
                }
            }

            _statusEventArgs.LineCount = logEventArgs.LineCount;
            StatusLineFileSize(logEventArgs.FileSize);

            if (!_isLoading)
            {
                if (oldRowCount == 0)
                {
                    AdjustMinimumGridWith();
                }

                //CheckFilterAndHighlight(e);
            }

            if (_guiStateArgs.FollowTail && dataGridView.RowCount > 0)
            {
                // Mark prefetch stale so the next paint re-fetches fresh data.
                // Do NOT call InvalidatePrefetch() here — it unpins buffers, creating a
                // window where the GC thread can evict and return blocks to the pool.
                _columnCache.MarkPrefetchStale();
                dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.RowCount - 1;
                OnTailFollowed(EventArgs.Empty);
            }

            if (Preferences.TimestampControl && !_isLoading)
            {
                SetTimestampLimits();
            }
        }
        catch (Exception e)
        {
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(UpdateGrid), e));
        }

        //this.dataGridView.Refresh();
        //this.dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
    }

    private void CheckFilterAndHighlight (LogEventArgs e)
    {
        // The Tail trigger path: the one loop that evaluates highlight entries against
        // newly appended lines. Side-effecting triggers (Audio Alert, Set Bookmark, Stop Tail) fire
        // here and only here — the bulk scanner (HighlightBookmarkScanner) has no access to them.
        var doFilter = filterTailCheckBox.Checked || _filterPipeList.Count > 0;
        var noLed = true;
        var firstStopTail = true;
        var filterLineAdded = false;

        var startLine = e.PrevLineCount;
        if (e.IsRollover)
        {
            ShiftFilterLines(e.RolloverOffset);
            startLine -= e.RolloverOffset;
        }

        ColumnizerCallback callback = new(this);

        for (var i = startLine; i < e.LineCount; ++i)
        {
            var line = _logFileReader.GetLogLineMemory(i);
            if (line == null)
            {
                // End of available lines — stop scanning, but still flush the work already done below.
                break;
            }

            if (doFilter)
            {
                if (filterTailCheckBox.Checked)
                {
                    callback.SetLineNum(i);
                    if (Util.TestFilterCondition(_filterParams, line, callback))
                    {
                        filterLineAdded = true;
                        AddFilterLine(i, false);
                    }
                }

                ProcessFilterPipes(i);
            }

            var matchingList = FindMatchingHighlightEntries(line);
            LaunchHighlightPlugins(matchingList, i);
            var (suppressLed, stopTail, setBookmark, bookmarkComment) = HighlightEvaluator.GetTriggerActions(matchingList);
            SafeTriggerAudioAlert(matchingList);
            if (setBookmark)
            {
                var capturedLineNum = i;
                var capturedComment = bookmarkComment;
                _ = BeginInvoke(() => SetBookmarkFromTrigger(capturedLineNum, capturedComment));
            }

            if (stopTail && _guiStateArgs.FollowTail)
            {
                FollowTailChanged(false, true);

                // Scroll to the triggering line once per batch, even if follow-tail gets
                // re-enabled while the batch is still being processed.
                if (firstStopTail)
                {
                    var capturedLineNum = i;
                    _ = BeginInvoke(() => SelectAndEnsureVisible(capturedLineNum, false));
                    firstStopTail = false;
                }
            }

            if (!suppressLed)
            {
                noLed = false;
            }
        }

        if (filterLineAdded)
        {
            TriggerFilterLineGuiUpdate();
        }

        if (!noLed)
        {
            OnFileSizeChanged(e);
        }
    }

    private void LaunchHighlightPlugins (IList<HighlightEntry> matchingList, int lineNum)
    {
        LogExpertCallback callback = new(this)
        {
            LineNum = lineNum
        };

        foreach (var entry in matchingList)
        {
            if (entry.IsActionEntry && entry.ActionEntry.PluginName != null)
            {
                var plugin = _pluginRegistry.FindKeywordActionPluginByName(entry.ActionEntry.PluginName);
                if (plugin != null)
                {
                    //ActionPluginExecuteFx fx = plugin.Execute;
                    _ = Task.Run(() => plugin.Execute(entry.SearchText, entry.ActionEntry.ActionParam, callback, CurrentColumnizer));
                    //_ = fx.BeginInvoke(entry.SearchText, entry.ActionEntry.ActionParam, callback, CurrentColumnizer, null, null);
                }
            }
        }
    }

    private void SetColumnizer (ILogLineMemoryColumnizer columnizer)
    {
        columnizer = ColumnizerPicker.FindReplacementForAutoMemoryColumnizer(FileName, _logFileReader, columnizer, _pluginRegistry.RegisteredColumnizers);

        var timeDiff = 0;
        if (CurrentColumnizer != null && CurrentColumnizer.IsTimeshiftImplemented())
        {
            timeDiff = CurrentColumnizer.GetTimeOffset();
        }

        SetColumnizerInternal(columnizer);

        if (CurrentColumnizer.IsTimeshiftImplemented())
        {
            CurrentColumnizer.SetTimeOffset(timeDiff);
        }
    }

    private void SetColumnizerInternal (ILogLineMemoryColumnizer columnizer)
    {
        //_logger.Info($"SetColumnizerInternal(): {columnizer.GetName()}");

        var oldColumnizer = CurrentColumnizer;
        var oldColumnizerIsXmlType = CurrentColumnizer is ILogLineMemoryXmlColumnizer;
        var oldColumnizerIsPreProcess = CurrentColumnizer is IPreProcessColumnizerMemory;
        var mustReload = false;

        // Check if the filtered columns disappeared, if so must refresh the UI
        if (_filterParams.ColumnRestrict)
        {
            var newColumns = columnizer != null ? columnizer.GetColumnNames() : [];
            var colChanged = false;

            if (dataGridView.ColumnCount - 2 == newColumns.Length) // two first columns are 'marker' and 'line number'
            {
                for (var i = 0; i < newColumns.Length; i++)
                {
                    if (dataGridView.Columns[i].HeaderText != newColumns[i])
                    {
                        colChanged = true;
                        break; // one change is sufficient
                    }
                }
            }
            else
            {
                colChanged = true;
            }

            if (colChanged)
            {
                // Update UI
                columnNamesLabel.Text = CalculateColumnNames(_filterParams);
            }
        }

        var oldColType = _filterParams.CurrentColumnizer?.GetType();
        var newColType = columnizer?.GetType();

        if (oldColType != newColType && _filterParams.ColumnRestrict && _filterParams.IsFilterTail)
        {
            _filterParams.ColumnList.Clear();
        }

        if (CurrentColumnizer == null || CurrentColumnizer.GetType() != columnizer.GetType())
        {
            CurrentColumnizer = columnizer;
            _freezeStateMap.Clear();

            _ = _logFileReader?.PreProcessColumnizer = CurrentColumnizer is IPreProcessColumnizerMemory columnizer1
                    ? columnizer1
                    : null;

            // always reload when choosing XML columnizers
            if (_logFileReader != null && CurrentColumnizer is ILogLineMemoryXmlColumnizer)
            {
                //forcedColumnizer = currentColumnizer; // prevent Columnizer selection on SetGuiAfterReload()
                mustReload = true;
            }

            // Reload when choosing no XML columnizer but previous columnizer was XML
            if (_logFileReader != null && CurrentColumnizer is not ILogLineMemoryXmlColumnizer && oldColumnizerIsXmlType)
            {
                _logFileReader.IsXmlMode = false;
                //forcedColumnizer = currentColumnizer; // prevent Columnizer selection on SetGuiAfterReload()
                mustReload = true;
            }

            // Reload when previous columnizer was PreProcess and current is not, and vice versa.
            // When the current columnizer is a preProcess columnizer, reload in every case.
            if ((CurrentColumnizer is IPreProcessColumnizer) != oldColumnizerIsPreProcess ||
                CurrentColumnizer is IPreProcessColumnizer)
            {
                //forcedColumnizer = currentColumnizer; // prevent Columnizer selection on SetGuiAfterReload()
                mustReload = true;
            }
        }
        else
        {
            CurrentColumnizer = columnizer;
        }

        (oldColumnizer as IInitColumnizerMemory)?.DeSelected(new ColumnizerCallbackMemory(this));

        (columnizer as IInitColumnizerMemory)?.Selected(new ColumnizerCallbackMemory(this));

        SetColumnizer(columnizer, dataGridView);
        SetColumnizer(columnizer, filterGridView);

        _guiStateArgs.TimeshiftPossible = columnizer.IsTimeshiftImplemented();
        SendGuiStateUpdate();

        if (_logFileReader != null)
        {
            dataGridView.RowCount = _logFileReader.LineCount;
        }

        if (_filterResultList != null)
        {
            filterGridView.RowCount = _filterResultList.Count;
        }

        if (mustReload)
        {
            Reload();
        }
        else
        {
            if (CurrentColumnizer.IsTimeshiftImplemented())
            {
                SetTimestampLimits();
                SyncTimestampDisplay();
            }

            var settings = ConfigManager.Settings;
            ShowLineColumn(!settings.HideLineColumn);
            ShowTimeSpread(Preferences.ShowTimeSpread && columnizer.IsTimeshiftImplemented());
        }

        if (!columnizer.IsTimeshiftImplemented() && IsTimeSynced)
        {
            FreeFromTimeSync();
        }

        columnComboBox.Items.Clear();

        foreach (var columnName in columnizer.GetColumnNames())
        {
            _ = columnComboBox.Items.Add(columnName);
        }

        if (columnComboBox.Items.Count > 0)
        {
            columnComboBox.SelectedIndex = 0;
        }

        OnColumnizerChanged(CurrentColumnizer);
    }

    private void AutoResizeColumns (BufferedDataGridView gridView)
    {
        try
        {
            gridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            AdjustColumnWidthsForControlCharSubstitution(gridView);
            if (gridView.Columns.Count > 1 && Preferences.SetLastColumnWidth &&
                gridView.Columns[gridView.Columns.Count - 1].Width < Preferences.LastColumnWidth
            )
            {
                // It seems that using 'MinimumWidth' instead of 'Width' prevents the DataGridView's NullReferenceExceptions
                //gridView.Columns[gridView.Columns.Count - 1].Width = this.Preferences.lastColumnWidth;
                gridView.Columns[gridView.Columns.Count - 1].MinimumWidth = Preferences.LastColumnWidth;
            }
        }
        catch (NullReferenceException e)
        {
            // See https://connect.microsoft.com/VisualStudio/feedback/details/366943/autoresizecolumns-in-datagridview-throws-nullreferenceexception
            // possible solution => https://stackoverflow.com/questions/36287553/nullreferenceexception-when-trying-to-set-datagridview-column-width-brings-th
            // There are some rare situations with null ref exceptions when resizing columns and on filter finished
            // So catch them here. Better than crashing.
            _logger.Error($"Error while resizing columns: {e}");
        }
    }

    /// <summary>
    /// DataGridView's built-in auto-resize measures the cell <c>Value</c> (raw text). When
    /// control-character substitution is enabled, substituted glyphs render wider than the
    /// original 1-character control bytes, so columns under-measure and clip. This routine
    /// walks the displayed rows, measures the rendered (post-substitution) string for each
    /// cell, and grows the column width when needed.
    /// </summary>
    private void AdjustColumnWidthsForControlCharSubstitution (BufferedDataGridView gridView)
    {
        var settings = Preferences.ControlCharSettings;
        if (settings is null || !settings.Substitute || settings.EnabledCodepoints is null || settings.EnabledCodepoints.Count == 0)
        {
            return;
        }

        int firstRow = gridView.FirstDisplayedScrollingRowIndex;
        if (firstRow < 0)
        {
            return;
        }

        int displayed = gridView.DisplayedRowCount(true);
        if (displayed <= 0)
        {
            return;
        }

        int lastRow = Math.Min(firstRow + displayed, gridView.RowCount) - 1;

        for (int colIndex = 0; colIndex < gridView.ColumnCount; colIndex++)
        {
            var gridColumn = gridView.Columns[colIndex];
            int requiredWidth = gridColumn.Width;

            for (int rowIndex = firstRow; rowIndex <= lastRow; rowIndex++)
            {
                var cellValue = gridView.Rows[rowIndex].Cells[colIndex].Value;
                if (cellValue is not IColumnMemory mem || mem.DisplayValue.IsEmpty)
                {
                    continue;
                }

                if (!ControlCharRenderer.HasAnyEnabledCodepoint(mem.DisplayValue.Span, settings.EnabledCodepoints))
                {
                    continue;
                }

                var rendered = ControlCharRenderer.Render(mem.DisplayValue.ToString(), settings);
                var sb = new System.Text.StringBuilder(mem.DisplayValue.Length + 8);
                foreach (var seg in rendered)
                {
                    _ = sb.Append(seg.RenderedText);
                }

                var size = TextRenderer.MeasureText(sb.ToString(), NormalFont);
                // Match the padding DataGridView uses for DisplayedCells auto-size.
                int candidate = size.Width + 9;
                if (candidate > requiredWidth)
                {
                    requiredWidth = candidate;
                }
            }

            if (requiredWidth > gridColumn.Width)
            {
                gridColumn.Width = requiredWidth;
            }
        }
    }

    private void PaintCell (DataGridViewCellPaintingEventArgs e, HighlightEntry groundEntry, SelectionCellStyle selection)
    {
        PaintHighlightedCell(e, groundEntry, selection);
    }

    private void PaintHighlightedCell (DataGridViewCellPaintingEventArgs e, HighlightEntry groundEntry, SelectionCellStyle selection)
    {
        var column = e.Value as IColumnMemory;

        column ??= Column.EmptyColumn;

        var matchList = FindHighlightMatches(column);
        // too many entries per line seem to cause problems with the GDI
        while (matchList.Count > 50)
        {
            matchList.RemoveAt(50);
        }

        var he = new HighlightEntry
        {
            SearchText = column.DisplayValue.ToString(),
            ForegroundColor = PaintHelper.GetForeColorFromHighlightEntry(groundEntry, Application.IsDarkModeEnabled),
            BackgroundColor = groundEntry?.BackgroundColor ?? Color.Empty,
            IsWordMatch = true
        };

        HighlightMatchEntry hme = new()
        {
            StartPos = 0,
            Length = column.DisplayValue.Length,
            HighlightEntry = he
        };

        if (groundEntry != null)
        {
            hme.HighlightEntry.IsBold = groundEntry.IsBold;
        }

        // Decide path: take the cheap legacy merge when substitution is disabled or the cell
        // contains no enabled control character (the dominant case). Otherwise build
        // render segments and combine them with the highlight match list.
        var controlCharSettings = Preferences.ControlCharSettings ?? new Core.Config.ControlCharSettings();
        bool useSubstitutionPath = controlCharSettings.Substitute
            && ControlCharRenderer.HasAnyEnabledCodepoint(column.DisplayValue.Span, controlCharSettings.EnabledCodepoints);

        IReadOnlyList<PaintSegment> paintSegments;
        if (useSubstitutionPath)
        {
            var rawText = column.DisplayValue.ToString();
            var renderSegments = ControlCharRenderer.Render(rawText, controlCharSettings);
            paintSegments = SubstitutedHighlightSegmenter.Combine(
                renderSegments,
                matchList,
                hme.HighlightEntry,
                controlCharSettings);
        }
        else
        {
            paintSegments = ToPaintSegments(MergeHighlightMatchEntries(matchList, hme), column.DisplayValue);
        }

        var borderWidths = PaintHelper.BorderWidths(e.AdvancedBorderStyle);
        var valBounds = e.CellBounds;
        valBounds.Offset(borderWidths.X, borderWidths.Y);
        valBounds.Width -= borderWidths.Right;
        valBounds.Height -= borderWidths.Bottom;
        if (e.CellStyle.Padding != Padding.Empty)
        {
            valBounds.Offset(e.CellStyle.Padding.Left, e.CellStyle.Padding.Top);
            valBounds.Width -= e.CellStyle.Padding.Horizontal;
            valBounds.Height -= e.CellStyle.Padding.Vertical;
        }

        var flags =
                TextFormatFlags.Left
                | TextFormatFlags.SingleLine
                | TextFormatFlags.NoPrefix
                | TextFormatFlags.PreserveGraphicsClipping
                | TextFormatFlags.NoPadding
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.TextBoxControl;

        var wordPos = valBounds.Location;
        Size proposedSize = new(valBounds.Width, valBounds.Height);

        e.Graphics.SetClip(e.CellBounds);

        foreach (var segment in paintSegments)
        {
            var font = segment.IsBold ? BoldFont : NormalFont;
            // Italic only applies to substituted glyphs; raw runs never set it.
            if (segment.IsItalic)
            {
                font = new Font(font, font.Style | FontStyle.Italic);
            }

            using var bgBrush = segment.BackColor != Color.Empty
                ? new SolidBrush(segment.BackColor)
                : null;

            var wordSize = TextRenderer.MeasureText(e.Graphics, segment.RenderedText, font, proposedSize, flags);
            wordSize.Height = e.CellBounds.Height;
            Rectangle wordRect = new(wordPos, wordSize);

            var foreColor = selection.Foreground(segment.ForeColor);
            if (!selection.FillBackground)
            {
                if (bgBrush != null && !segment.NoBackground)
                {
                    e.Graphics.FillRectangle(bgBrush, wordRect);
                }
            }

            TextRenderer.DrawText(e.Graphics, segment.RenderedText, font, wordRect, foreColor, flags);
            wordPos.Offset(wordSize.Width, 0);

            if (segment.IsItalic)
            {
                font.Dispose();
            }
        }
    }

    /// <summary>
    /// Adapter that converts the legacy merged highlight-match list (produced by
    /// <see cref="MergeHighlightMatchEntries"/>) into a <see cref="PaintSegment"/> list so
    /// the unified paint loop can render it without a separate code path.
    /// </summary>
    private static IReadOnlyList<PaintSegment> ToPaintSegments (IList<HighlightMatchEntry> mergedMatches, ReadOnlyMemory<char> raw)
    {
        var result = new List<PaintSegment>(mergedMatches.Count);
        foreach (var me in mergedMatches)
        {
            result.Add(new PaintSegment(
                RenderedText: raw.Slice(me.StartPos, me.Length).ToString(),
                ForeColor: me.HighlightEntry.ForegroundColor,
                BackColor: me.HighlightEntry.BackgroundColor,
                IsBold: me.HighlightEntry.IsBold,
                IsItalic: false,
                NoBackground: me.HighlightEntry.NoBackground,
                IsSubstituted: false));
        }

        return result;
    }

    /// <summary>
    /// Builds a list of HighlightMatchEntry objects. A HighlightMatchEntry spans over a region that is painted with the
    /// same foreground and background colors. All regions which don't match a word-mode entry will be painted with the
    /// colors of a default entry (groundEntry). This is either the first matching non-word-mode highlight entry or a
    /// black-on-white default (if no matching entry was found).
    /// </summary>
    /// <param name="matchList">List of all highlight matches for the current cell</param>
    /// <param name="groundEntry">The entry that is used as the default.</param>
    /// <returns>
    /// List of HighlightMatchEntry objects. The list spans over the whole cell and contains color infos for every
    /// substring.
    /// </returns>
    private static IList<HighlightMatchEntry> MergeHighlightMatchEntries (IList<HighlightMatchEntry> matchList, HighlightMatchEntry groundEntry)
    {
        // Fill an area with lenth of whole text with a default hilight entry
        var entryArray = new HighlightEntry[groundEntry.Length];
        for (var i = 0; i < entryArray.Length; ++i)
        {
            entryArray[i] = groundEntry.HighlightEntry;
        }

        // "overpaint" with all matching word match enries
        // Non-word-mode matches will not overpaint because they use the groundEntry
        foreach (var me in matchList)
        {
            var endPos = me.StartPos + me.Length;
            for (var i = me.StartPos; i < endPos; ++i)
            {
                if (me.HighlightEntry.IsWordMatch)
                {
                    entryArray[i] = me.HighlightEntry;
                }
                else
                {
                    //entryArray[i].ForegroundColor = me.HilightEntry.ForegroundColor;
                }
            }
        }

        // collect areas with same hilight entry and build new highlight match entries for it
        IList<HighlightMatchEntry> mergedList = [];

        if (entryArray.Length > 0)
        {
            var currentEntry = entryArray[0];
            var lastStartPos = 0;
            var pos = 0;

            for (; pos < entryArray.Length; ++pos)
            {
                if (entryArray[pos] != currentEntry)
                {
                    HighlightMatchEntry me = new()
                    {
                        StartPos = lastStartPos,
                        Length = pos - lastStartPos,
                        HighlightEntry = currentEntry
                    };

                    mergedList.Add(me);
                    currentEntry = entryArray[pos];
                    lastStartPos = pos;
                }
            }

            HighlightMatchEntry me2 = new()
            {
                StartPos = lastStartPos,
                Length = pos - lastStartPos,
                HighlightEntry = currentEntry
            };

            mergedList.Add(me2);
        }

        return mergedList;
    }

    /// <summary>
    /// Returns the first HighlightEntry that matches the given line
    /// </summary>
    private HighlightEntry FindHighlightEntry (ITextValueMemory line)
    {
        return FindHighlightEntry(line, false);
    }

    private HighlightEntry FindFirstNoWordMatchHighlightEntry (ITextValueMemory line)
    {
        return FindHighlightEntry(line, true);
    }

    /// <summary>
    /// Returns all HighlightEntry entries which matches the given line
    /// </summary>
    private IList<HighlightEntry> FindMatchingHighlightEntries (ITextValueMemory line)
    {
        lock (_currentHighlightGroupLock)
        {
            return HighlightEvaluator.FindMatchingEntries(_currentHighlightGroup.HighlightEntryList, line);
        }
    }

    private static void GetHighlightEntryMatches (ITextValueMemory line, IList<HighlightEntry> hilightEntryList, IList<HighlightMatchEntry> resultList)
    {
        foreach (var entry in hilightEntryList)
        {
            if (entry.IsWordMatch)
            {
                var matches = entry.Regex.Matches(line.Text.ToString());
                foreach (Match match in matches)
                {
                    HighlightMatchEntry me = new()
                    {
                        HighlightEntry = entry,
                        StartPos = match.Index,
                        Length = match.Length
                    };

                    resultList.Add(me);
                }
            }
            else
            {
                if (HighlightEvaluator.IsMatch(entry, line))
                {
                    HighlightMatchEntry me = new()
                    {
                        HighlightEntry = entry,
                        StartPos = 0,
                        Length = line.Text.Length
                    };

                    resultList.Add(me);
                }
            }
        }
    }

    /// <summary>
    /// Fires an audio alert for the first matching highlight entry that has
    /// <see cref="HighlightEntry.AlertOnHit"/> enabled. Iteration stops after the
    /// first such entry; the process-wide cooldown maintained by
    /// <see cref="AudioPlayer"/> would suppress subsequent plays anyway.
    /// Called only from the tail trigger path.
    /// </summary>
    private static volatile bool _audioAlertsUnavailable;

    /// <summary>
    /// Guarded entry point for <see cref="TriggerAudioAlert"/>. Isolating the audio call in a
    /// separate method keeps the audio assembly (LogExpert.Audio/NAudio) off the JIT path of
    /// <see cref="CheckFilterAndHighlight"/>, and catches a failed lazy load of that assembly so a
    /// missing/broken audio dependency degrades to "no audio alerts" instead of throwing on the
    /// worker thread and stopping follow-tail (#634). After one failure audio alerts stay disabled.
    /// </summary>
    private static void SafeTriggerAudioAlert (IList<HighlightEntry> matchingList)
    {
        if (_audioAlertsUnavailable)
        {
            return;
        }

        try
        {
            TriggerAudioAlert(matchingList);
        }
        catch (Exception ex) when (ex is FileNotFoundException or
                                         FileLoadException or
                                         BadImageFormatException or
                                         TypeLoadException or
                                         DllNotFoundException)
        {
            _audioAlertsUnavailable = true;
            _logger.Warn(ex, "### SafeTriggerAudioAlert: Audio alerts disabled: the audio component could not be loaded (e.g. LogExpert.Audio/NAudio missing from this installation).");
        }
    }

    private static void TriggerAudioAlert (IList<HighlightEntry> matchingList)
    {
        if (matchingList == null || matchingList.Count == 0)
        {
            return;
        }

        foreach (var entry in matchingList.Where(entry => entry.AlertOnHit))
        {
            _ = AudioPlayer.PlayThrottled(entry.SoundFilePath, entry.CooldownSeconds);
            break;
        }
    }

    private void StopTimespreadThread ()
    {
        _timeSpreadCalc.Stop();
    }

    private void StopTimestampSyncThread ()
    {
        _shouldTimestampDisplaySyncingCancel = true;
        _ = _timeShiftSyncWakeupEvent.Set(); // wake the worker if it is parked so it observes the cancel flag and exits
        _cts.Cancel();
        JoinWorker(_timeShiftSyncTask);
    }

    [SupportedOSPlatform("windows")]
    private void SyncTimestampDisplay ()
    {
        if (CurrentColumnizer.IsTimeshiftImplemented())
        {
            if (dataGridView.CurrentRow != null)
            {
                SyncTimestampDisplay(dataGridView.CurrentRow.Index);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void SyncTimestampDisplay (int lineNum)
    {
        _timeShiftSyncLine = lineNum;
        _ = _timeShiftSyncTimerEvent.Set();
        _ = _timeShiftSyncWakeupEvent.Set();
    }

    [SupportedOSPlatform("windows")]
    private void SyncTimestampDisplayWorker ()
    {
        Thread.CurrentThread.Name = "SyncTimestampDisplayWorker";
        _shouldTimestampDisplaySyncingCancel = false;

        while (!_shouldTimestampDisplaySyncingCancel)
        {
            _ = _timeShiftSyncWakeupEvent.WaitOne();
            if (_shouldTimestampDisplaySyncingCancel)
            {
                return;
            }

            _ = _timeShiftSyncWakeupEvent.Reset();

            while (!_shouldTimestampDisplaySyncingCancel)
            {
                var signaled = _timeShiftSyncTimerEvent.WaitOne(WAIT_TIME, true);
                _ = _timeShiftSyncTimerEvent.Reset();
                if (!signaled)
                {
                    break;
                }
            }

            // timeout with no new Trigger -> update display
            var lineNum = _timeShiftSyncLine;
            if (lineNum >= 0 && lineNum < dataGridView.RowCount)
            {
                var (timeStamp, lineNumber) = GetTimestampForLine(lineNum, true);
                lineNum = lineNumber;
                if (!timeStamp.Equals(DateTime.MinValue) && !_shouldTimestampDisplaySyncingCancel)
                {
                    _guiStateArgs.Timestamp = timeStamp;
                    SendGuiStateUpdate();
                    if (_shouldCallTimeSync)
                    {
                        var (exactTimeStamp, _) = GetTimestampForLine(lineNum, false);
                        SyncOtherWindows(exactTimeStamp);
                        _shouldCallTimeSync = false;
                    }
                }
            }

            // show time difference between 2 selected lines
            if (dataGridView.SelectedRows.Count == 2)
            {
                var row1 = dataGridView.SelectedRows[0].Index;
                var row2 = dataGridView.SelectedRows[1].Index;
                if (row1 > row2)
                {
                    (row2, row1) = (row1, row2);
                }

                var (timeStamp1, _) = GetTimestampForLine(row1, false);
                var (timeStamp2, _) = GetTimestampForLine(row2, false);
                //TimeSpan span = TimeSpan.FromTicks(timeStamp2.Ticks - timeStamp1.Ticks);
                var diff = timeStamp1.Ticks > timeStamp2.Ticks
                    ? new DateTime(timeStamp1.Ticks - timeStamp2.Ticks)
                    : new DateTime(timeStamp2.Ticks - timeStamp1.Ticks);

                StatusLineText(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_StatusLineText_TimeDiff, $"{diff:HH:mm:ss.fff}"));
            }
            else
            {
                if (!IsMultiFile && dataGridView.SelectedRows.Count == 1)
                {
                    StatusLineText(string.Empty);
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void SyncFilterGridPos ()
    {
        try
        {
            if (_filterResultList.Count > 0)
            {
                var index = _filterResultList.BinarySearch(dataGridView.CurrentRow.Index);
                if (index < 0)
                {
                    index = ~index;
                    if (index > 0)
                    {
                        --index;
                    }
                }

                filterGridView.CurrentCell = filterGridView.Rows.GetRowCount(DataGridViewElementStates.None) > 0
                    ? filterGridView.Rows[index].Cells[0]
                    : null;
            }
        }
        catch (Exception e)
        {
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(SyncFilterGridPos), e));
        }
    }

    private void StatusLineFileSize (long size)
    {
        _statusEventArgs.FileSize = size;
        SendStatusLineUpdate();
    }

    /// <summary>
    /// Narrates a running search's progress: wrap notices on the status line (mid-search,
    /// like the old in-loop reporting) and scanned-line counts to the progress bar.
    /// Called synchronously on the search's background thread.
    /// </summary>
    private void ReportSearchProgress (SearchProgress searchProgress)
    {
        switch (searchProgress.Wrap)
        {
            case SearchWrap.ToStart:
                StatusLineError(Resources.LogWindow_UI_StatusLineError_StartedFromBeginningOfFile);
                return;
            case SearchWrap.ToEnd:
                StatusLineError(Resources.LogWindow_UI_StatusLineError_StartedFromEndOfFile);
                return;
        }

        try
        {
            if (!Disposing)
            {
                _ = Invoke(UpdateProgressBar, [searchProgress.LinesScanned]);
            }
        }
        catch (ObjectDisposedException ex) // can occur when closing the app while searching
        {
            _logger.Warn(ex);
        }
    }

    /// <summary>Relays reports synchronously — System.Progress would post them asynchronously.</summary>
    private sealed class SynchronousProgress<T> (Action<T> handler) : IProgress<T>
    {
        public void Report (T value)
        {
            handler(value);
        }
    }

    private void ResetProgressBar ()
    {
        _progressEventArgs.Value = _progressEventArgs.MaxValue;
        _progressEventArgs.Visible = false;
        SendProgressBarUpdate();
    }

    [SupportedOSPlatform("windows")]
    private void SelectLine (int lineNum, bool triggerSyncCall, bool shouldScroll)
    {
        try
        {
            _shouldCallTimeSync = triggerSyncCall;

            if (lineNum < 0)
            {
                return;
            }

            // Prevent ArgumentOutOfRangeException
            if (lineNum >= dataGridView.Rows.GetRowCount(DataGridViewElementStates.None))
            {
                lineNum = dataGridView.Rows.GetRowCount(DataGridViewElementStates.None) - 1;
            }

            dataGridView.Rows[lineNum].Selected = true;

            if (shouldScroll)
            {
                dataGridView.CurrentCell = dataGridView.Rows[lineNum].Cells[0];
                _ = dataGridView.Focus();
            }
        }
        catch (ArgumentOutOfRangeException e)
        {
            _logger.Error($"### SelectLine: Error while selecting line: {e}");
        }
        catch (IndexOutOfRangeException e)
        {
            // Occurs sometimes (but cannot reproduce)
            _logger.Error($"### SelectLine: Error while selecting line: {e}");
        }
    }

    [SupportedOSPlatform("windows")]
    private void StartEditMode ()
    {
        if (!dataGridView.CurrentCell.ReadOnly)
        {
            _ = dataGridView.BeginEdit(false);
            if (dataGridView.EditingControl != null)
            {
                if (dataGridView.EditingControl is LogCellEditingControl editControl)
                {
                    editControl.KeyDown += OnEditControlKeyDown;
                    editControl.KeyPress += OnEditControlKeyPress;
                    editControl.KeyUp += OnEditControlKeyUp;
                    editControl.Click += OnEditControlClick;
                    dataGridView.CellEndEdit += OnDataGridViewCellEndEdit;
                    editControl.SelectionStart = 0;
                }
                //else
                //{
                //    _logger.Warn($"Edit control in logWindow was null");
                //}
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void UpdateEditColumnDisplay (DataGridViewTextBoxEditingControl editControl)
    {
        // prevents key events after edit mode has ended
        if (dataGridView.EditingControl != null)
        {
            var pos = editControl.SelectionStart + editControl.SelectionLength;
            StatusLineText(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_StatusLineText_UpdateEditColumnDisplay, pos));
            //_logger.Debug($"### UpdateEditColumnDisplay: SelStart: {editControl.SelectionStart}, SelLen: {editControl.SelectionLength}"));
        }
    }

    [SupportedOSPlatform("windows")]
    private void SelectPrevHighlightLine ()
    {
        var lineNum = dataGridView.CurrentCellAddress.Y;
        while (lineNum > 0)
        {
            lineNum--;
            var line = _logFileReader.GetLogLineMemory(lineNum);
            if (line != null)
            {
                var entry = FindHighlightEntry(line);
                if (entry != null)
                {
                    SelectLine(lineNum, false, true);
                    break;
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void SelectNextHighlightLine ()
    {
        var lineNum = dataGridView.CurrentCellAddress.Y;
        while (lineNum < _logFileReader.LineCount)
        {
            lineNum++;
            var line = _logFileReader.GetLogLineMemory(lineNum);
            if (line != null)
            {
                var entry = FindHighlightEntry(line);
                if (entry != null)
                {
                    SelectLine(lineNum, false, true);
                    break;
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private int FindNextBookmarkIndex (int lineNum)
    {
        if (lineNum >= dataGridView.RowCount)
        {
            lineNum = 0;
        }
        else
        {
            lineNum++;
        }

        return _bookmarkProvider.FindNextBookmarkIndex(lineNum);
    }

    [SupportedOSPlatform("windows")]
    private int FindPrevBookmarkIndex (int lineNum)
    {
        if (lineNum <= 0)
        {
            lineNum = dataGridView.RowCount - 1;
        }
        else
        {
            lineNum--;
        }

        return _bookmarkProvider.FindPrevBookmarkIndex(lineNum);
    }

    /**
   * Shift bookmarks after a logfile rollover
   */
    private void ShiftBookmarks (int offset)
    {
        _bookmarkProvider.ShiftBookmarks(offset);
        OnBookmarksChanged();
    }

    private void ShiftRowHeightList (int offset)
    {
        SortedList<int, RowHeightEntry> newList = [];
        foreach (var entry in _rowHeightList.Values)
        {
            var line = entry.LineNum - offset;
            if (line >= 0)
            {
                entry.LineNum = line;
                newList.Add(line, entry);
            }
        }

        _rowHeightList = newList;
    }

    private void ShiftFilterPipes (int offset)
    {
        lock (_filterPipeList)
        {
            foreach (var pipe in _filterPipeList)
            {
                pipe.ShiftLineNums(offset);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void LoadFilterPipes ()
    {
        lock (_filterPipeList)
        {
            foreach (var pipe in _filterPipeList)
            {
                pipe.RecreateTempFile();
            }
        }

        if (_filterPipeList.Count > 0)
        {
            for (var i = 0; i < dataGridView.RowCount; ++i)
            {
                ProcessFilterPipes(i);
            }
        }
    }

    private void DisconnectFilterPipes ()
    {
        lock (_filterPipeList)
        {
            foreach (var pipe in _filterPipeList)
            {
                pipe.ClearLineList();
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void ApplyFilterParams ()
    {
        filterComboBox.Text = _filterParams.SearchText;
        filterCaseSensitiveCheckBox.Checked = _filterParams.IsCaseSensitive;
        filterRegexCheckBox.Checked = _filterParams.IsRegex;
        filterTailCheckBox.Checked = _filterParams.IsFilterTail;
        invertFilterCheckBox.Checked = _filterParams.IsInvert;
        knobControlFilterBackSpread.Value = _filterParams.SpreadBefore;
        knobControlFilterForeSpread.Value = _filterParams.SpreadBehind;
        rangeCheckBox.Checked = _filterParams.IsRangeSearch;
        columnRestrictCheckBox.Checked = _filterParams.ColumnRestrict;
        knobControlFuzzy.Value = _filterParams.FuzzyValue;
        filterRangeComboBox.Text = _filterParams.RangeSearchText;
    }

    [SupportedOSPlatform("windows")]
    private void ResetFilterControls ()
    {
        filterComboBox.Text = string.Empty;
        filterCaseSensitiveCheckBox.Checked = false;
        filterRegexCheckBox.Checked = false;
        //this.filterTailCheckBox.Checked = this.Preferences.filterTail;
        invertFilterCheckBox.Checked = false;
        knobControlFilterBackSpread.Value = 0;
        knobControlFilterForeSpread.Value = 0;
        rangeCheckBox.Checked = false;
        columnRestrictCheckBox.Checked = false;
        knobControlFuzzy.Value = 0;
        filterRangeComboBox.Text = string.Empty;
    }

    [SupportedOSPlatform("windows")]
    private void FilterSearch ()
    {
        if (filterComboBox.Text.Length == 0)
        {
            _filterParams.SearchText = string.Empty;
            _filterParams.IsRangeSearch = false;
            ClearFilterList();
            btnfilterSearch.Image = null;
            ResetFilterControls();
            bntSaveFilter.Enabled = false;
            return;
        }

        FilterSearch(filterComboBox.Text);
    }

    [SupportedOSPlatform("windows")]
    private async void FilterSearch (string text)
    {
        FireCancelHandlers(); // make sure that there's no other filter running (maybe from filter restore)

        _filterParams.SearchText = text;
        _ = ConfigManager.Settings.FilterHistoryList.Remove(text);
        ConfigManager.Settings.FilterHistoryList.Insert(0, text);
        var maxHistory = ConfigManager.Settings.Preferences.MaximumFilterEntries;

        if (ConfigManager.Settings.FilterHistoryList.Count > maxHistory)
        {
            ConfigManager.Settings.FilterHistoryList.RemoveAt(filterComboBox.Items.Count - 1);
        }

        filterComboBox.Items.Clear();
        foreach (var item in ConfigManager.Settings.FilterHistoryList)
        {
            _ = filterComboBox.Items.Add(item);
        }

        filterComboBox.Text = text;

        _filterParams.IsRangeSearch = rangeCheckBox.Checked;
        _filterParams.RangeSearchText = filterRangeComboBox.Text;
        if (_filterParams.IsRangeSearch)
        {
            _ = ConfigManager.Settings.FilterRangeHistoryList.Remove(filterRangeComboBox.Text);
            ConfigManager.Settings.FilterRangeHistoryList.Insert(0, filterRangeComboBox.Text);
            if (ConfigManager.Settings.FilterRangeHistoryList.Count > maxHistory)
            {
                ConfigManager.Settings.FilterRangeHistoryList.RemoveAt(filterRangeComboBox.Items.Count - 1);
            }

            filterRangeComboBox.Items.Clear();
            foreach (var item in ConfigManager.Settings.FilterRangeHistoryList)
            {
                _ = filterRangeComboBox.Items.Add(item);
            }
        }

        ConfigManager.Save(SettingsFlags.FilterHistory);

        _filterParams.IsCaseSensitive = filterCaseSensitiveCheckBox.Checked;
        _filterParams.IsRegex = filterRegexCheckBox.Checked;
        _filterParams.IsFilterTail = filterTailCheckBox.Checked;
        _filterParams.IsInvert = invertFilterCheckBox.Checked;
        if (_filterParams.IsRegex)
        {
            try
            {
                _filterParams.CreateRegex();
            }
            catch (ArgumentException)
            {
                StatusLineError(Resources.LogWindow_UI_StatusLineError_InvalidRegularExpression);
                return;
            }
        }

        _filterParams.FuzzyValue = knobControlFuzzy.Value;
        _filterParams.SpreadBefore = knobControlFilterBackSpread.Value;
        _filterParams.SpreadBehind = knobControlFilterForeSpread.Value;
        _filterParams.ColumnRestrict = columnRestrictCheckBox.Checked;

        //ConfigManager.SaveFilterParams(this.filterParams);
        ConfigManager.Settings.FilterParams = _filterParams; // wozu eigentlich? sinnlos seit MDI?

        _isSearching = true;
        StatusLineText(Resources.LogWindow_UI_StatusLineText_FilterSearch_Filtering);
        btnfilterSearch.Enabled = false;
        ClearFilterList();

        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = dataGridView.RowCount;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        IFilterEngine engine = ConfigManager.Settings.Preferences.MultiThreadFilter
            ? new ParallelFilterEngine()
            : new SerialFilterEngine();

        ColumnizerCallback callback = new(this);
        var progress = new SynchronousProgress<int>(UpdateProgressBar);

        // ESC reaches the run through the cancel-handler registry; the engines observe the token.
        using var filterCts = CancellationTokenSource.CreateLinkedTokenSource(_windowCts.Token);
        var cancelHandler = new FilterRunCancelHandler(filterCts);
        OnRegisterCancelHandler(cancelHandler);

        long startTime = Environment.TickCount;
        FilterRun filterRun;
        try
        {
            filterRun = await Task.Run(() => engine.Run(_filterParams, callback, filterCts.Token, progress)).ConfigureAwait(false);
        }
        finally
        {
            OnDeRegisterCancelHandler(cancelHandler);
        }

        long endTime = Environment.TickCount;

        lock (_filterResultList)
        {
            _filterHitList.AddRange(filterRun.HitLines);
            _filterResultList.AddRange(filterRun.ResultLines);
            _lastFilterLinesList.AddRange(filterRun.History);
            FilterSpread.TrimHistory(_lastFilterLinesList);
        }

        // Closing the window cancels the run (linked token), so this continuation also fires
        // mid-close — with the handle already destroyed, narration and BeginInvoke would throw.
        // _isClosing (set before the cancel) is the discriminator, NOT IsHandleCreated: a session-
        // restored background tab runs this with its own handle not yet created, marshaling
        // through the parent — and must still reach FilterComplete to populate the filter grid.
        if (IsDisposed || Disposing || _waitingForClose || _isClosing)
        {
            return;
        }

        if (filterRun.Outcome == FilterRunOutcome.Failed)
        {
            StatusLineError(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_StatusLineError_FilterFailed, filterRun.Error?.Message));
        }
        else
        {
            StatusLineText(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_StatusLineText_Filter_FilterDurationMs, endTime - startTime));
        }

        FilterComplete();

        //This needs to be invoked, because there is a potential CrossThreadException
        _ = BeginInvoke(CheckForFilterDirty);
    }

    /// <summary>Bridges the ESC cancel-handler registry to a filter run's token.</summary>
    private sealed class FilterRunCancelHandler (CancellationTokenSource cts) : IBackgroundProcessCancelHandler
    {
        public void EscapePressed ()
        {
            cts.Cancel();
        }
    }

    [SupportedOSPlatform("windows")]
    private void AddFilterLine (int lineNum, bool immediate)
    {
        lock (_filterResultList)
        {
            // Adopts the window's canonical lists per call — ClearFilterList/ShiftFilterLines
            // replace the instances, so the accumulator must not outlive them.
            new FilterAccumulator(_filterResultList, _filterHitList, _lastFilterLinesList)
                .AddHit(lineNum, _filterParams.SpreadBefore, _filterParams.SpreadBehind, _logFileReader.LineCount);
        }

        if (immediate)
        {
            TriggerFilterLineGuiUpdate();
        }
    }

    [SupportedOSPlatform("windows")]
    private void TriggerFilterLineGuiUpdate ()
    {
        //lock (this.filterUpdateThread)
        //{
        //  this.filterEventCount++;
        //  this.filterUpdateEvent.Set();
        //}
        _ = Invoke(new MethodInvoker(AddFilterLineGuiUpdate));
    }

    //private void FilterUpdateWorker()
    //{
    //  Thread.CurrentThread.Name = "FilterUpdateWorker";
    //  while (true)
    //  {
    //    this.filterUpdateEvent.WaitOne();
    //    lock (this.filterUpdateThread)
    //    {
    //      this.Invoke(new MethodInvoker(AddFilterLineGuiUpdate));
    //      this.filterUpdateEvent.Reset();
    //    }

    //    //_logger.logDebug("FilterUpdateWorker: Waiting for signal");
    //    //bool signaled = this.filterUpdateEvent.WaitOne(1000, false);

    //    //if (!signaled)
    //    //{
    //    //  lock (this.filterUpdateThread)
    //    //  {
    //    //    if (this.filterEventCount > 0)
    //    //    {
    //    //      this.filterEventCount = 0;
    //    //      _logger.logDebug("FilterUpdateWorker: Invoking GUI update because of wait timeout");
    //    //      this.Invoke(new MethodInvoker(AddFilterLineGuiUpdate));
    //    //    }
    //    //  }
    //    //}
    //    //else
    //    //{
    //    //  _logger.logDebug("FilterUpdateWorker: Wakeup signal received.");
    //    //  lock (this.filterUpdateThread)
    //    //  {
    //    //    _logger.logDebug("FilterUpdateWorker: event count: " + this.filterEventCount);
    //    //    if (this.filterEventCount > 100)
    //    //    {
    //    //      this.filterEventCount = 0;
    //    //      _logger.logDebug("FilterUpdateWorker: Invoking GUI update because of event count");
    //    //      this.Invoke(new MethodInvoker(AddFilterLineGuiUpdate));
    //    //    }
    //    //    this.filterUpdateEvent.Reset();
    //    //  }
    //    //}
    //  }
    //}

    //private void StopFilterUpdateWorkerThread()
    //{
    //  this.filterUpdateEvent.Set();
    //  this.filterUpdateThread.Abort();
    //  this.filterUpdateThread.Join();
    //}

    [SupportedOSPlatform("windows")]
    private void AddFilterLineGuiUpdate ()
    {
        try
        {
            lock (_filterResultList)
            {
                lblFilterCount.Text = string.Empty + _filterResultList.Count;
                if (filterGridView.RowCount > _filterResultList.Count)
                {
                    filterGridView.RowCount = 0; // helps to prevent hang ?
                }

                filterGridView.RowCount = _filterResultList.Count;
                if (filterGridView.RowCount > 0)
                {
                    filterGridView.FirstDisplayedScrollingRowIndex = filterGridView.RowCount - 1;
                }

                if (filterGridView.RowCount == 1)
                {
                    // after a file reload adjusted column sizes anew when the first line arrives
                    //this.filterGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                    AutoResizeColumns(filterGridView);
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(AddFilterLineGuiUpdate), e));
        }
    }

    private void UpdateProgressBar (int value)
    {
        _progressEventArgs.Value = value;
        if (value > _progressEventArgs.MaxValue)
        {
            // can occur if new lines will be added while filtering
            _progressEventArgs.MaxValue = value;
        }

        SendProgressBarUpdate();
    }

    [SupportedOSPlatform("windows")]
    private void FilterComplete ()
    {
        if (!IsDisposed && !_waitingForClose && !Disposing)
        {
            _ = Invoke(new MethodInvoker(ResetStatusAfterFilter));
        }
    }

    [SupportedOSPlatform("windows")]
    private void ResetStatusAfterFilter ()
    {
        try
        {
            //StatusLineText("");
            _isSearching = false;
            _progressEventArgs.Value = _progressEventArgs.MaxValue;
            _progressEventArgs.Visible = false;
            SendProgressBarUpdate();
            filterGridView.RowCount = _filterResultList.Count;
            //this.filterGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            AutoResizeColumns(filterGridView);
            lblFilterCount.Text = string.Empty + _filterResultList.Count;
            if (filterGridView.RowCount > 0)
            {
                _ = filterGridView.Focus();
            }

            btnfilterSearch.Enabled = true;
        }
        catch (NullReferenceException e)
        {
            // See https://connect.microsoft.com/VisualStudio/feedback/details/366943/autoresizecolumns-in-datagridview-throws-nullreferenceexception
            // There are some rare situations with null ref exceptions when resizing columns and on filter finished
            // So catch them here. Better than crashing.
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(ResetStatusAfterFilter), e));
        }
    }

    [SupportedOSPlatform("windows")]
    private void ClearFilterList ()
    {
        try
        {
            //this.shouldCancel = true;
            lock (_filterResultList)
            {
                filterGridView.SuspendLayout();
                filterGridView.RowCount = 0;
                lblFilterCount.Text = Resources.LogWindow_UI_Common_ZeroValue;
                _filterResultList = [];
                _lastFilterLinesList = [];
                _filterHitList = [];
                //this.filterGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                filterGridView.ResumeLayout();
            }
        }
        catch (Exception e)
        {
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(ClearFilterList), e));

            _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_Error_ClearFilterList_WhileClearingFilterList, e), Resources.LogExpert_Common_UI_Title_Error);
        }
    }

    private void ClearBookmarkList ()
    {
        _bookmarkProvider.ClearAllBookmarks();
    }

    /**
   * Shift filter list line entries after a logfile rollover
   */
    [SupportedOSPlatform("windows")]
    private void ShiftFilterLines (int offset)
    {
        lock (_filterResultList)
        {
            _filterResultList = FilterSpread.ShiftLines(_filterResultList, offset);
        }

        _filterHitList = FilterSpread.ShiftLines(_filterHitList, offset);
        _lastFilterLinesList = FilterSpread.RebuildHistory(_filterResultList);

        TriggerFilterLineGuiUpdate();
    }

    [SupportedOSPlatform("windows")]
    private void CheckForFilterDirty ()
    {
        if (IsFilterSearchDirty(_filterParams))
        {
            btnfilterSearch.Image = _searchButtonImage;
            bntSaveFilter.Enabled = false;
        }
        else
        {
            btnfilterSearch.Image = null;
            bntSaveFilter.Enabled = true;
        }
    }

    [SupportedOSPlatform("windows")]
    private bool IsFilterSearchDirty (FilterParams filterParams)
    {
        if (filterParams == null || filterParams.SearchText == null)
        {
            return true;
        }

        if (!filterParams.SearchText.Equals(filterComboBox.Text, StringComparison.Ordinal))
        {
            return true;
        }

        if (filterParams.IsRangeSearch != rangeCheckBox.Checked)
        {
            return true;
        }

        if (filterParams.IsRangeSearch && !filterParams.RangeSearchText.Equals(filterRangeComboBox.Text, StringComparison.Ordinal))
        {
            return true;
        }

        if (filterParams.IsRegex != filterRegexCheckBox.Checked)
        {
            return true;
        }

        if (filterParams.IsInvert != invertFilterCheckBox.Checked)
        {
            return true;
        }

        if (filterParams.SpreadBefore != knobControlFilterBackSpread.Value)
        {
            return true;
        }

        if (filterParams.SpreadBehind != knobControlFilterForeSpread.Value)
        {
            return true;
        }

        return filterParams.FuzzyValue != knobControlFuzzy.Value ||
            filterParams.ColumnRestrict != columnRestrictCheckBox.Checked ||
            filterParams.IsCaseSensitive != filterCaseSensitiveCheckBox.Checked;
    }

    [SupportedOSPlatform("windows")]
    private void AdjustMinimumGridWith ()
    {
        if (dataGridView.Columns.Count > 1)
        {
            //this.dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            AutoResizeColumns(dataGridView);

            var width = dataGridView.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
            var diff = dataGridView.Width - width;
            if (diff > 0)
            {
                diff -= dataGridView.RowHeadersWidth / 2;
                dataGridView.Columns[dataGridView.Columns.GetColumnCount(DataGridViewElementStates.None) - 1].Width += diff;
                filterGridView.Columns[filterGridView.Columns.GetColumnCount(DataGridViewElementStates.None) - 1].Width += diff;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static void InvalidateCurrentRow (BufferedDataGridView gridView)
    {
        if (gridView.CurrentCellAddress.Y > -1)
        {
            gridView.InvalidateRow(gridView.CurrentCellAddress.Y);
        }
    }

    private void InvalidateCurrentRow ()
    {
        InvalidateCurrentRow(dataGridView);
        InvalidateCurrentRow(filterGridView);
    }

    [SupportedOSPlatform("windows")]
    private void DisplayCurrentFileOnStatusline ()
    {
        if (_logFileReader.IsMultiFile)
        {
            try
            {
                if (dataGridView.CurrentRow != null && dataGridView.CurrentRow.Index > -1)
                {
                    var fileName = _logFileReader.GetLogFileNameForLine(dataGridView.CurrentRow.Index);
                    if (fileName != null)
                    {
                        StatusLineText(Util.GetNameFromPath(fileName));
                    }
                }
            }
            catch (Exception)
            {
                // TODO: handle this concurrent situation better:
                // this.dataGridView.CurrentRow may be null even if checked before.
                // This can happen when MultiFile shift deselects the current row because there
                // are less lines after rollover than before.
                // access to dataGridView-Rows should be locked
            }
        }
    }

    private void UpdateSelectionDisplay ()
    {
        if (_noSelectionUpdates)
        {
            return;
        }
    }

    [SupportedOSPlatform("windows")]
    private void UpdateFilterHistoryFromSettings ()
    {
        ConfigManager.Settings.FilterHistoryList = ConfigManager.Settings.FilterHistoryList;
        filterComboBox.Items.Clear();
        foreach (var item in ConfigManager.Settings.FilterHistoryList)
        {
            _ = filterComboBox.Items.Add(item);
        }

        filterRangeComboBox.Items.Clear();
        foreach (var item in ConfigManager.Settings.FilterRangeHistoryList)
        {
            _ = filterRangeComboBox.Items.Add(item);
        }
    }

    private void StatusLineText (string text)
    {
        _statusEventArgs.StatusText = text;
        SendStatusLineUpdate();
    }

    private void StatusLineError (string text)
    {
        StatusLineText(text);
        _isErrorShowing = true;
    }

    private void RemoveStatusLineError ()
    {
        StatusLineText(string.Empty);
        _isErrorShowing = false;
    }

    private void SendGuiStateUpdate ()
    {
        OnGuiState(_guiStateArgs);
    }

    private void SendProgressBarUpdate ()
    {
        OnProgressBarUpdate(new ProgressEventArgs
        {
            MinValue = _progressEventArgs.MinValue,
            MaxValue = _progressEventArgs.MaxValue,
            Value = _progressEventArgs.Value,
            Visible = _progressEventArgs.Visible
        });
    }

    private void SendStatusLineUpdate ()
    {
        OnStatusLine(_statusEventArgs);
    }

    [SupportedOSPlatform("windows")]
    private void ShowAdvancedFilterPanel (bool show)
    {
        if (show)
        {
            btnAdvanced.Text = Resources.LogWindow_UI_Text_ShowAdvancedFilterPanel_HideAdvanced;
            btnAdvanced.Image = null;
        }
        else
        {
            btnAdvanced.Text = Resources.LogWindow_UI_Text_ShowAdvancedFilterPanel_ShowAdvanced;
            CheckForAdvancedButtonDirty();
        }

        advancedFilterSplitContainer.Panel1Collapsed = !show;
        advancedFilterSplitContainer.SplitterDistance = FILTER_ADVANCED_SPLITTER_DISTANCE;
        _showAdvanced = show;
    }

    [SupportedOSPlatform("windows")]
    private void CheckForAdvancedButtonDirty ()
    {
        btnAdvanced.Image = IsAdvancedOptionActive() && !_showAdvanced
            ? _advancedButtonImage
            : null;
    }

    [SupportedOSPlatform("windows")]
    private void FilterToTab ()
    {
        btnfilterSearch.Enabled = false;
        _ = Task.Run(WriteFilterToTab);
    }

    [SupportedOSPlatform("windows")]
    private void WriteFilterToTab ()
    {
        FilterPipe pipe = new(_filterParams.Clone(), this);
        lock (_filterResultList)
        {
            var namePrefix = Resources.LogWindow_UI_WriteFilterToTab_NamePrefix_ForFilter;
            var title = IsTempFile
                ? TempTitleName + namePrefix + ++_filterPipeNameCounter
                : Util.GetNameFromPath(FileName) + namePrefix + ++_filterPipeNameCounter;

            WritePipeToTab(pipe, _filterResultList, title, null);
        }
    }

    [SupportedOSPlatform("windows")]
    private void WritePipeToTab (FilterPipe pipe, List<int> lineNumberList, string name, SessionSnapshot snapshot)
    {
        StatusLineText(Resources.LogWindow_UI_StatusLineText_WritePipeToTab_WritingToTempFile);
        _guiStateArgs.MenuEnabled = false;
        SendGuiStateUpdate();
        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = lineNumberList.Count;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        _ = Invoke(new MethodInvoker(SendProgressBarUpdate));
        _isSearching = true;

        // Per-job token, linked to the window's lifetime; ESC reaches it via the registry.
        using var pipeCts = CancellationTokenSource.CreateLinkedTokenSource(_windowCts.Token);
        var cancelHandler = new FilterRunCancelHandler(pipeCts);
        OnRegisterCancelHandler(cancelHandler);

        lock (_filterPipeList)
        {
            _filterPipeList.Add(pipe);
        }

        pipe.Closed += OnPipeDisconnected;
        var count = 0;
        pipe.OpenFile();
        LogExpertCallback callback = new(this);

        try
        {
            foreach (var i in lineNumberList)
            {
                if (pipeCts.Token.IsCancellationRequested)
                {
                    break;
                }

                var line = _logFileReader.GetLogLineMemory(i);
                if (CurrentColumnizer is ILogLineMemoryXmlColumnizer)
                {
                    callback.LineNum = i;
                    line = (CurrentColumnizer as ILogLineMemoryXmlColumnizer).GetLineTextForClipboard(line, callback);
                }

                _ = pipe.WriteToPipe(line, i);
                if (++count % PROGRESS_BAR_MODULO == 0)
                {
                    _progressEventArgs.Value = count;
                    _ = Invoke(new MethodInvoker(SendProgressBarUpdate));
                }
            }
        }
        finally
        {
            OnDeRegisterCancelHandler(cancelHandler);
        }

        pipe.CloseFile();
        var wasCancelled = pipeCts.IsCancellationRequested;

        // Same close-mid-job hazard as the filter run's epilogue: skip the UI finish when the
        // window went away while writing (_isClosing, not IsHandleCreated — see FilterSearch).
        if (IsDisposed || Disposing || _waitingForClose || _isClosing)
        {
            return;
        }

        Invoke(() => WriteFilterToTabFinished(pipe, name, snapshot, wasCancelled));
    }

    [SupportedOSPlatform("windows")]
    private void WriteFilterToTabFinished (FilterPipe pipe, string name, SessionSnapshot snapshot, bool wasCancelled)
    {
        _isSearching = false;
        if (!wasCancelled)
        {
            var title = name;
            ILogLineMemoryColumnizer preProcessColumnizer = null;
            if (CurrentColumnizer is not ILogLineMemoryXmlColumnizer)
            {
                preProcessColumnizer = CurrentColumnizer;
            }

            var newWin = _logWindowCoordinator.AddFilterTab(pipe, title, preProcessColumnizer);
            newWin.FilterPipe = pipe;
            pipe.ResultWindow = newWin;
            if (snapshot != null)
            {
                _ = Task.Run(() => FilterRestore(newWin, snapshot));
            }
        }

        _progressEventArgs.Value = _progressEventArgs.MaxValue;
        _progressEventArgs.Visible = false;
        SendProgressBarUpdate();
        _guiStateArgs.MenuEnabled = true;
        SendGuiStateUpdate();
        StatusLineText("");
        btnfilterSearch.Enabled = true;
    }

    /// <summary>
    /// Used to create a new tab and pipe the given content into it.
    /// </summary>
    /// <param name="lineEntryList"></param>
    /// <param name="title"></param>
    [SupportedOSPlatform("windows")]
    internal void WritePipeTab (IList<LineEntryMemory> lineEntryList, string title)
    {
        FilterPipe pipe = new(new FilterParams(), this)
        {
            IsStopped = true
        };

        pipe.Closed += OnPipeDisconnected;
        pipe.OpenFile();
        foreach (var entry in lineEntryList)
        {
            _ = pipe.WriteToPipe(entry.LogLine, entry.LineNum);
        }

        pipe.CloseFile();
        Invoke(() => WriteFilterToTabFinished(pipe, title, null, wasCancelled: false));
    }

    [SupportedOSPlatform("windows")]
    private static void FilterRestore (LogWindow newWin, SessionSnapshot snapshot)
    {
        newWin.WaitForLoadingFinished();
        var columnizer = ColumnizerPicker.FindMemoryColumnizerByName(snapshot.Columnizer.GetName(), newWin._pluginRegistry.RegisteredColumnizers);

        if (columnizer != null)
        {
            SetColumnizerFx fx = newWin.ForceColumnizer;
            _ = newWin.Invoke(fx, [columnizer]);
        }

        _ = newWin.BeginInvoke(new RestoreFiltersFx(newWin.ApplyRestoredFilterTabSnapshot), [snapshot]);
    }

    [SupportedOSPlatform("windows")]
    private void ProcessFilterPipes (int lineNum)
    {
        var searchLine = _logFileReader.GetLogLineMemory(lineNum);
        if (searchLine == null)
        {
            return;
        }

        ColumnizerCallback callback = new(this)
        {
            LineNum = lineNum
        };

        IList<FilterPipe> deleteList = [];
        lock (_filterPipeList)
        {
            foreach (var pipe in _filterPipeList)
            {
                if (pipe.IsStopped)
                {
                    continue;
                }

                if (Util.TestFilterCondition(pipe.FilterParams, searchLine, callback))
                {
                    // Adopts the pipe's history; result/hit lists are not tracked per pipe.
                    var filterResult = new FilterAccumulator([], [], pipe.LastLinesHistoryList)
                        .AddHit(lineNum, pipe.FilterParams.SpreadBefore, pipe.FilterParams.SpreadBehind, _logFileReader.LineCount);
                    pipe.OpenFile();

                    foreach (var line in filterResult)
                    {
                        var textLine = _logFileReader.GetLogLineMemory(line);
                        var fileOk = pipe.WriteToPipe(textLine, line);
                        if (!fileOk)
                        {
                            deleteList.Add(pipe);
                        }
                    }

                    pipe.CloseFile();
                }
            }
        }

        foreach (var pipe in deleteList)
        {
            _ = _filterPipeList.Remove(pipe);
        }
    }

    [SupportedOSPlatform("windows")]
    private void CopyMarkedLinesToClipboard ()
    {
        var clipboardSettings = Preferences.ControlCharSettings ?? new ControlCharSettings();
        bool transformDisplayedForm = clipboardSettings.Substitute && clipboardSettings.CopyDisplayedForm;

        if (_guiStateArgs.CellSelectMode)
        {
            var data = dataGridView.GetClipboardContent();

            // Replace the UnicodeText payload with the substituted form. Default
            // EnabledCodepoints exclude TAB/LF/CR so the grid's cell/line separators
            // are preserved; users who opt in to those codepoints will see those
            // separators substituted too.
            if (transformDisplayedForm && data is not null && data.TryGetData<string>(DataFormats.UnicodeText, out var unicodeText))
            {
                var transformed = SubstitutedClipboardBuilder.Build(unicodeText.AsSpan(), 0, unicodeText.Length, clipboardSettings);
                data = new DataObject(DataFormats.UnicodeText, transformed);
            }

            if (!ClipboardHelper.TrySetDataObject(data))
            {
                StatusLineError(Resources.LogExpert_Common_UI_Message_ClipboardInUse);
            }
        }
        else
        {
            List<int> lineNumList = [];
            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                if (row.Index != -1)
                {
                    lineNumList.Add(row.Index);
                }
            }

            lineNumList.Sort();
            StringBuilder clipText = new();
            LogExpertCallback callback = new(this);

            foreach (var lineNum in lineNumList)
            {
                var line = _logFileReader.GetLogLineMemory(lineNum);
                if (CurrentColumnizer is ILogLineMemoryXmlColumnizer xmlColumnizer)
                {
                    callback.LineNum = lineNum;
                    line = xmlColumnizer.GetLineTextForClipboard(line, callback);
                }

                if (transformDisplayedForm)
                {
                    var rawLine = line.FullLine;
                    var substituted = SubstitutedClipboardBuilder.Build(
                        rawLine.Span, 0, rawLine.Length, clipboardSettings);
                    _ = clipText.Append('\t')
                        .Append(line.LineNumber + 1)
                        .Append('\t')
                        .AppendLine(substituted);
                }
                else
                {
                    _ = clipText.AppendLine(line.ToClipBoardText());
                }
            }

            if (!ClipboardHelper.TrySetDataObject(clipText.ToString()))
            {
                StatusLineError(Resources.LogExpert_Common_UI_Message_ClipboardInUse);
            }
        }
    }

    /// <summary>
    /// Set an Encoding which shall be used when loading a file. Used before a file is loaded.
    /// </summary>
    /// <param name="encoding"></param>
    private void SetExplicitEncoding (Encoding encoding)
    {
        EncodingOptions.Encoding = encoding;
    }

    [SupportedOSPlatform("windows")]
    private void ApplyDataGridViewPrefs (BufferedDataGridView dataGridView, bool setLastColumnWidth, int lastColumnWidth)
    {
        if (dataGridView.Columns.GetColumnCount(DataGridViewElementStates.None) > 1)
        {
            if (setLastColumnWidth)
            {
                dataGridView.Columns[dataGridView.Columns.GetColumnCount(DataGridViewElementStates.None) - 1].MinimumWidth = lastColumnWidth;
            }
            else
            {
                // Workaround for a .NET bug which brings the DataGridView into an unstable state (causing lots of NullReferenceExceptions).
                dataGridView.FirstDisplayedScrollingColumnIndex = 0;

                dataGridView.Columns[dataGridView.Columns.GetColumnCount(DataGridViewElementStates.None) - 1].MinimumWidth = 5; // default
            }
        }

        if (dataGridView.RowCount > 0)
        {
            dataGridView.UpdateRowHeightInfo(0, true);
        }

        dataGridView.Invalidate();
        dataGridView.Refresh();
        AutoResizeColumns(dataGridView);
    }

    [SupportedOSPlatform("windows")]
    private List<int> GetSelectedContent ()
    {
        if (dataGridView.SelectionMode == DataGridViewSelectionMode.FullRowSelect)
        {
            List<int> lineNumList = [];

            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                if (row.Index != -1)
                {
                    lineNumList.Add(row.Index);
                }
            }

            lineNumList.Sort();
            return lineNumList;
        }

        return [];
    }

    /* ========================================================================
     * Timestamp stuff
     * =======================================================================*/

    [SupportedOSPlatform("windows")]
    private void SetTimestampLimits ()
    {
        if (!CurrentColumnizer.IsTimeshiftImplemented())
        {
            return;
        }

        (_guiStateArgs.MinTimestamp, _) = _timestampLocator.FindForward(0, _logFileReader.LineCount, true);
        (_guiStateArgs.MaxTimestamp, _) = GetTimestampForLine(dataGridView.RowCount - 1, true);
        SendGuiStateUpdate();
    }

    //TODO Reimplement
    private static void AdjustHighlightSplitterWidth ()
    {
        //int size = this.editHighlightsSplitContainer.Panel2Collapsed ? 600 : 660;
        //int distance = this.highlightSplitContainer.Width - size;
        //if (distance < 10)
        //  distance = 10;
        //this.highlightSplitContainer.SplitterDistance = distance;
    }

    [SupportedOSPlatform("windows")]
    private void BookmarkComment (Bookmark bookmark)
    {
        BookmarkCommentDlg dlg = new()
        {
            Comment = bookmark.Text
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            bookmark.Text = dlg.Comment;
            dataGridView.Refresh();
            OnBookmarkTextChanged(bookmark);
        }
    }

    /// <summary>
    /// Indicates which columns we are filtering on
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    private string CalculateColumnNames (FilterParams filter)
    {
        var names = new StringBuilder();

        if (filter.ColumnRestrict)
        {
            foreach (var colIndex in filter.ColumnList)
            {
                if (colIndex < dataGridView.Columns.GetColumnCount(DataGridViewElementStates.None) - 2)
                {
                    if (names.Length > 0)
                    {
                        _ = names.Append(", ");
                    }

                    // skip first two columns: marker + line number
                    _ = names.Append(dataGridView.Columns[2 + colIndex].HeaderText);
                }
            }
        }

        return names.ToString();
    }

    [SupportedOSPlatform("windows")]
    private void ApplyFrozenState (BufferedDataGridView gridView)
    {
        SortedDictionary<int, DataGridViewColumn> dict = [];
        foreach (DataGridViewColumn col in gridView.Columns)
        {
            dict.Add(col.DisplayIndex, col);
        }

        foreach (var col in dict.Values)
        {
            col.Frozen = _freezeStateMap.TryGetValue(gridView, out bool isFrozen) && isFrozen;

            if (col.Index == _selectedCol)
            {
                break;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void ShowTimeSpread (bool show)
    {
        tableLayoutPanel1.ColumnStyles[1].Width = show
            ? 16
            : 0;

        _timeSpreadCalc.Enabled = show;
    }

    [SupportedOSPlatform("windows")]
    protected internal void AddTempFileTab (string fileName, string title)
    {
        _ = _logWindowCoordinator.AddTempFileTab(fileName, title);
    }

    [SupportedOSPlatform("windows")]
    private void ChangeRowHeight (bool decrease)
    {
        var rowNum = dataGridView.CurrentCellAddress.Y;
        if (rowNum < 0 || rowNum >= dataGridView.RowCount)
        {
            return;
        }

        if (decrease)
        {
            if (!_rowHeightList.TryGetValue(rowNum, out var entry))
            {
                return;
            }
            else
            {
                entry.Height -= _lineHeight;
                if (entry.Height <= _lineHeight)
                {
                    _ = _rowHeightList.Remove(rowNum);
                }
            }
        }
        else
        {
            RowHeightEntry entry;
            if (!_rowHeightList.TryGetValue(rowNum, out var value))
            {
                entry = new RowHeightEntry
                {
                    LineNum = rowNum,
                    Height = _lineHeight
                };

                _rowHeightList[rowNum] = entry;
            }
            else
            {
                entry = value;
            }

            entry.Height += _lineHeight;
        }

        dataGridView.UpdateRowHeightInfo(rowNum, false);
        if (rowNum == dataGridView.RowCount - 1 && _guiStateArgs.FollowTail)
        {
            dataGridView.FirstDisplayedScrollingRowIndex = rowNum;
        }

        dataGridView.Refresh();
    }

    private int GetRowHeight (int rowNum)
    {
        return _rowHeightList.TryGetValue(rowNum, out var value)
            ? value.Height
            : _lineHeight;
    }

    private void AddBookmarkAtLineSilently (int lineNum)
    {
        if (!_bookmarkProvider.IsBookmarkAtLine(lineNum))
        {
            _bookmarkProvider.AddBookmark(new Bookmark(lineNum));
        }
    }

    [SupportedOSPlatform("windows")]
    private void AddBookmarkAndEditComment ()
    {
        var lineNum = dataGridView.CurrentCellAddress.Y;
        if (!_bookmarkProvider.IsBookmarkAtLine(lineNum))
        {
            ToggleBookmark();
        }

        BookmarkComment(_bookmarkProvider.GetBookmarkForLine(lineNum));
    }

    [SupportedOSPlatform("windows")]
    private void AddBookmarkComment (string text)
    {
        var lineNum = dataGridView.CurrentCellAddress.Y;
        Bookmark bookmark;

        if (!_bookmarkProvider.IsBookmarkAtLine(lineNum))
        {
            _bookmarkProvider.AddBookmark(bookmark = new Bookmark(lineNum));
        }
        else
        {
            bookmark = _bookmarkProvider.GetBookmarkForLine(lineNum);
        }

        bookmark.Text += text;
        dataGridView.Refresh();
        filterGridView.Refresh();
        OnBookmarkTextChanged(bookmark);
    }

    [SupportedOSPlatform("windows")]
    private void MarkCurrentFilterRange ()
    {
        _filterParams.RangeSearchText = filterRangeComboBox.Text;
        ColumnizerCallback callback = new(this);
        RangeFinder rangeFinder = new(_filterParams, callback);
        var range = rangeFinder.FindRange(dataGridView.CurrentCellAddress.Y);
        if (range != null)
        {
            SetCellSelectionMode(false);
            _noSelectionUpdates = true;
            for (var i = range.StartLine; i <= range.EndLine; ++i)
            {
                dataGridView.Rows[i].Selected = true;
            }

            _noSelectionUpdates = false;
            UpdateSelectionDisplay();
        }
    }

    [SupportedOSPlatform("windows")]
    private void RemoveTempHighlights ()
    {
        lock (_tempHighlightEntryListLock)
        {
            _tempHighlightEntryList.Clear();
        }

        RefreshAllGrids();
    }

    [SupportedOSPlatform("windows")]
    private void ToggleHighlightPanel (bool open)
    {
        highlightSplitContainer.Panel2Collapsed = !open;
        btnToggleHighlightPanel.Image = open
            ? new Bitmap(_panelCloseButtonImage, new Size(btnToggleHighlightPanel.Size.Height, btnToggleHighlightPanel.Size.Height))
            : new Bitmap(_panelOpenButtonImage, new Size(btnToggleHighlightPanel.Size.Height, btnToggleHighlightPanel.Size.Height));
    }

    [SupportedOSPlatform("windows")]
    private void SetBookmarksForSelectedFilterLines ()
    {
        lock (_filterResultList)
        {
            foreach (DataGridViewRow row in filterGridView.SelectedRows)
            {
                var lineNum = _filterResultList[row.Index];
                AddBookmarkAtLineSilently(lineNum);
            }
        }

        dataGridView.Refresh();
        filterGridView.Refresh();
        OnBookmarksChanged();
    }

    private void SetDefaultHighlightGroup ()
    {
        var group = _logWindowCoordinator.ResolveHighlightGroup(null, FileName);
        //Resources.HighlightDialog_UI_DefaultGroupName
        SetCurrentHighlightGroup(group.GroupName);
    }

    [SupportedOSPlatform("windows")]
    private void HandleChangedFilterOnLoadSetting ()
    {
        Preferences.IsFilterOnLoad = filterOnLoadCheckBox.Checked;
        Preferences.IsAutoHideFilterList = hideFilterListOnLoadCheckBox.Checked;
        OnFilterListChanged(this);
    }

    private void FireCancelHandlers ()
    {
        lock (_cancelHandlerList)
        {
            foreach (var handler in _cancelHandlerList)
            {
                handler.EscapePressed();
            }
        }
    }

    private void SyncOtherWindows (DateTime timestamp)
    {
        lock (_timeSyncListLock)
        {
            TimeSyncList?.NavigateToTimestamp(timestamp, this, _logWindowCoordinator.IndicateTimeSyncActivity);
        }
    }

    [SupportedOSPlatform("windows")]
    private void AddSlaveToTimesync (LogWindow slave)
    {
        lock (_timeSyncListLock)
        {
            if (TimeSyncList == null)
            {
                if (slave.TimeSyncList == null)
                {
                    TimeSyncList = new TimeSyncList();
                    TimeSyncList.AddWindow(this);
                }
                else
                {
                    TimeSyncList = slave.TimeSyncList;
                }

                var currentLineNum = dataGridView.CurrentCellAddress.Y;
                var (timeStamp, _) = GetTimestampForLine(currentLineNum, true);
                if (!timeStamp.Equals(DateTime.MinValue) && !_shouldTimestampDisplaySyncingCancel)
                {
                    TimeSyncList.CurrentTimestamp = timeStamp;
                }

                TimeSyncList.WindowRemoved += OnTimeSyncListWindowRemoved;
            }
        }

        slave.AddToTimeSync(this);
        OnSyncModeChanged();
    }

    private static void FreeSlaveFromTimesync (LogWindow slave)
    {
        slave.FreeFromTimeSync();
    }

    private void OnSyncModeChanged ()
    {
        SyncModeChanged?.Invoke(this, new SyncModeEventArgs(IsTimeSynced));
    }

    [SupportedOSPlatform("windows")]
    private void AddSearchHitHighlightEntry (SearchParams para)
    {
        HighlightEntry he = new()
        {
            SearchText = para.SearchText,
            ForegroundColor = Color.Red,
            BackgroundColor = Color.Yellow,
            IsRegex = para.IsRegex,
            IsCaseSensitive = para.IsCaseSensitive,
            IsLedSwitch = false,
            IsStopTail = false,
            IsSetBookmark = false,
            IsActionEntry = false,
            ActionEntry = null,
            IsWordMatch = true,
            IsSearchHit = true
        };

        lock (_tempHighlightEntryListLock)
        {
            _tempHighlightEntryList.Add(he);
        }

        RefreshAllGrids();
    }

    [SupportedOSPlatform("windows")]
    private void RemoveAllSearchHighlightEntries ()
    {
        lock (_tempHighlightEntryListLock)
        {
            List<HighlightEntry> newList = [];
            foreach (var he in _tempHighlightEntryList)
            {
                if (!he.IsSearchHit)
                {
                    newList.Add(he);
                }
            }

            _tempHighlightEntryList = newList;
        }

        RefreshAllGrids();
    }

    [SupportedOSPlatform("windows")]
    private static DataGridViewColumn GetColumnByName (BufferedDataGridView dataGridView, string name)
    {
        foreach (DataGridViewColumn col in dataGridView.Columns)
        {
            if (col.HeaderText.Equals(name, StringComparison.Ordinal))
            {
                return col;
            }
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private void SelectColumn ()
    {
        var colName = columnComboBox.SelectedItem as string;
        var col = GetColumnByName(dataGridView, colName);
        if (col != null && !col.Frozen)
        {
            dataGridView.FirstDisplayedScrollingColumnIndex = col.Index;
            var currentLine = dataGridView.CurrentCellAddress.Y;
            if (currentLine >= 0)
            {
                dataGridView.CurrentCell = dataGridView.Rows[dataGridView.CurrentCellAddress.Y].Cells[col.Index];
            }
        }
    }

    #endregion

    #region Public methods

    public void LoadFile (string fileName, EncodingOptions encodingOptions)
    {
        EnterLoadFileStatus();

        if (fileName != null)
        {
            FileName = fileName;
            EncodingOptions = encodingOptions;

            if (_logFileReader != null)
            {
                _logFileReader.StopMonitoringAsync();
                UnRegisterLogFileReaderEvents();
            }

            //
            // isUsingDefaultColumnizer is to enable automatically find the best columnizer.
            // When a new log file is opened, and no Columnizer can be chose by file mask,
            // this flag will enable find a columnizer automatically.
            // Current solution is not elegant.
            // Since the refactory will involving a lot of work, we can plan it in the future.
            // One possible solution is, using raw file stream to read the sample lines to help
            // the ColumnizerPicker to determine the priority.
            //
            var isUsingDefaultColumnizer = false;
            if (!LoadPersistenceOptions())
            {
                if (!IsTempFile)
                {
                    var columnizer = FindColumnizer();
                    if (columnizer != null)
                    {
                        if (_reloadMemento == null)
                        {
                            columnizer = ColumnizerPicker.CloneMemoryColumnizer(columnizer, ConfigManager.ActiveConfigDir);
                        }
                    }
                    else
                    {
                        isUsingDefaultColumnizer = true;
                    }

                    PreSelectColumnizer(columnizer);
                }

                SetDefaultHighlightGroup();
            }
            else if (ConfigManager.Settings.Preferences.ColumnizerSelectionPriority == Core.Config.ColumnizerSelectionPriority.MaskOverridesPersistence
                     && !IsTempFile)
            {
                // The user opted in to mask-overrides-persistence: the .lxp file's bookmarks, filters,
                // highlight group, encoding etc. still apply, but the columnizer is replaced by the
                // mask list's match (if any).
                var shortName = Util.GetNameFromPath(fileName);
                var maskColumnizer = _logWindowCoordinator.TryGetMaskColumnizer(shortName);
                if (maskColumnizer != null)
                {
                    if (_reloadMemento == null)
                    {
                        maskColumnizer = ColumnizerPicker.CloneMemoryColumnizer(maskColumnizer, ConfigManager.ActiveConfigDir);
                    }

                    PreSelectColumnizer(maskColumnizer);
                }
            }

            // this may be set after loading persistence data
            if (_fileNames != null && IsMultiFile)
            {
                StartMultiFileReader(_fileNames, EncodingOptions);
                return;
            }

            _columnCache = new ColumnCache();
            _filterColumnCache = new ColumnCache();

            try
            {
                _logFileReader = new(fileName, EncodingOptions, IsMultiFile, Preferences.BufferCount, Preferences.LinesPerBuffer, _multiFileOptions, Preferences.ReaderType, _pluginRegistry, ConfigManager.Settings.Preferences.MaxLineLength);
            }
            catch (LogFileException lfe)
            {
                _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(LoadFile), lfe));
                _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_LoadFile_CannotLoadFile, lfe.Message), Resources.LogExpert_Common_UI_Title_LogExpert);
                _ = BeginInvoke(new FunctionWith1BoolParam(Close), true);
                _isLoadError = true;
                return;
            }

            if (CurrentColumnizer is ILogLineMemoryXmlColumnizer xmlColumnizer)
            {
                _logFileReader.IsXmlMode = true;
                _logFileReader.XmlLogConfig = xmlColumnizer.GetXmlLogConfiguration();
            }

            if (_forcedColumnizerForLoading != null)
            {
                CurrentColumnizer = _forcedColumnizerForLoading;
            }

            _logFileReader.PreProcessColumnizer = CurrentColumnizer is IPreProcessColumnizerMemory processColumnizer ? processColumnizer : null;

            RegisterLogFileReaderEvents();
            //_logger.Info($"Loading logfile: {fileName}");
            _logFileReader.StartMonitoring();

            if (isUsingDefaultColumnizer)
            {
                if (Preferences.AutoPick)
                {
                    var newColumnizer = ColumnizerPicker.FindBetterMemoryColumnizer(FileName, _logFileReader, CurrentColumnizer, _pluginRegistry.RegisteredColumnizers);

                    if (newColumnizer != null)
                    {
                        //_logger.Debug($"Picked new columnizer {newColumnizer.GetName()}");

                        PreSelectColumnizer(newColumnizer);
                    }
                }
            }
        }
    }

    public void LoadFilesAsMulti (string[] fileNames, EncodingOptions encodingOptions)
    {
        // A direct multi-file load (new multi tab, multi-file reload) never runs the LoadFile
        // flow, so the window open's single Session File read happens here. LoadFile's own
        // multi-file branch calls StartMultiFileReader directly — its read already happened.
        _sessionSnapshot = ReadSessionSnapshot(fileNames[^1]);

        StartMultiFileReader(fileNames, encodingOptions);
    }

    private void StartMultiFileReader (string[] fileNames, EncodingOptions encodingOptions)
    {
        EnterLoadFileStatus();

        //foreach (var name in fileNames)
        //{
        //    //_logger.Info($"LoadFilesAsMulti: File: {name}");
        //}

        if (_logFileReader != null)
        {
            _logFileReader.StopMonitoring();
            UnRegisterLogFileReaderEvents();
        }

        EncodingOptions = encodingOptions;
        _columnCache = new ColumnCache();
        _filterColumnCache = new ColumnCache();

        _logFileReader = new(fileNames, EncodingOptions, Preferences.BufferCount, Preferences.LinesPerBuffer, _multiFileOptions, Preferences.ReaderType, _pluginRegistry, ConfigManager.Settings.Preferences.MaxLineLength);

        RegisterLogFileReaderEvents();
        _logFileReader.StartMonitoring();
        FileName = fileNames[^1];
        _fileNames = fileNames;
        IsMultiFile = true;
        //if (this.isTempFile)
        //  this.Text = this.tempTitleName;
        //else
        //  this.Text = Util.GetNameFromPath(this.FileName);
    }

    //TODO move to Persister class
    public string SavePersistenceDataAndReturnFileName (bool force)
    {
        if (!force)
        {
            if (!Preferences.SaveSessions)
            {
                return null;
            }
        }

        if (IsTempFile || _isLoadError)
        {
            return null;
        }

        try
        {
            // Relocated from the old composing getter (the composer must stay side-effect-free):
            // this option doesn't need a press on 'search'. The write must precede the gather,
            // which is what copies _filterParams into the snapshot.
            _filterParams.IsFilterTail = filterTailCheckBox.Checked;

            var persistenceData = SessionFileComposer.Compose(GatherSessionSnapshot());

            return ForcedPersistenceFileName == null
                ? Persister.SavePersistenceData(FileName, persistenceData, Preferences, ConfigManager.ActiveSessionDir)
                : Persister.SavePersistenceDataWithFixedName(ForcedPersistenceFileName, persistenceData);
        }
        catch (IOException e)
        {
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(SavePersistenceDataAndReturnFileName), e));
        }
        catch (Exception e)
        {
            _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_SavePersistenceData_ErrorWhileSaving, e), Resources.LogExpert_Common_UI_Title_Error);
        }

        return null;
    }

    public void SavePersistenceData (bool force)
    {
        _ = SavePersistenceDataAndReturnFileName(force);
    }

    public SessionSnapshot GatherSessionSnapshot ()
    {
        // Filter out auto-generated bookmarks — they are transient and will be re-generated on load
        SortedList<int, Bookmark> manualBookmarks = [];
        foreach (var kvp in _bookmarkProvider.BookmarkList)
        {
            if (!kvp.Value.IsAutoGenerated)
            {
                manualBookmarks.Add(kvp.Key, kvp.Value);
            }
        }

        SessionSnapshot snapshot = new()
        {
            BookmarkList = manualBookmarks,
            RowHeightList = _rowHeightList,
            MultiFile = IsMultiFile,
            MultiFilePattern = _multiFileOptions.FormatPattern,
            MultiFileMaxDays = _multiFileOptions.MaxDayTry,
            CurrentLine = dataGridView.CurrentCellAddress.Y,
            FirstDisplayedLine = dataGridView.FirstDisplayedScrollingRowIndex,
            FilterVisible = !splitContainerLogWindow.Panel2Collapsed,
            FilterAdvanced = !advancedFilterSplitContainer.Panel1Collapsed,
            FilterPosition = splitContainerLogWindow.SplitterDistance,
            FollowTail = _guiStateArgs.FollowTail,
            CellSelectMode = _guiStateArgs.CellSelectMode,
            FileName = FileName,
            TabName = Text,
            Columnizer = CurrentColumnizer,
            LineCount = _logFileReader != null ? _logFileReader.LineCount : 0
        };

        if (Preferences.SaveFilters)
        {
            // The window's own filter, not the global filter list: a Session File is per-window
            // state, and the global list is never restored from one anyway. Clone() is a shallow
            // copy (ColumnList and the columnizer stay shared) — enough here, because compose and
            // serialize run synchronously right after this gather.
            snapshot.FilterParams = _filterParams.Clone();

            foreach (var filterPipe in _filterPipeList)
            {
                snapshot.FilterTabs.Add(new FilterTabSnapshot
                {
                    FilterParams = filterPipe.FilterParams,
                    Snapshot = filterPipe.ResultWindow.GatherSessionSnapshot(),
                });
            }
        }

        if (_currentHighlightGroup != null)
        {
            snapshot.HighlightGroupName = _currentHighlightGroup.GroupName;
        }

        if (_fileNames != null && IsMultiFile)
        {
            snapshot.MultiFileNames.AddRange(_fileNames);
        }

        snapshot.FilterSaveListVisible = !highlightSplitContainer.Panel2Collapsed;
        snapshot.Encoding = _logFileReader?.CurrentEncoding;

        return snapshot;
    }

    public void Close (bool dontAsk)
    {
        Preferences.AskForClose = !dontAsk;
        Close();
    }

    public void CloseLogWindow ()
    {
        _isClosing = true;

        CancelHighlightBookmarkScan();
        StopTimespreadThread();
        StopTimestampSyncThread();
        _tailFollowEngine.Stop();
        _windowCts.Cancel(); // window lifetime ends: every linked job token cancels
        _searchCts?.Cancel();

        // Jobs cancelled by the close skip their UI epilogues (the handle is going away), so the
        // closing window clears the shared status line and progress bar itself, while its events
        // are still wired to the parent.
        StatusLineText(string.Empty);
        _progressEventArgs.Visible = false;
        _progressEventArgs.Value = _progressEventArgs.MaxValue;
        SendProgressBarUpdate();

        if (_logFileReader != null)
        {
            UnRegisterLogFileReaderEvents();
            _logFileReader.StopMonitoringAsync();
            //this.logFileReader.DeleteAllContent();
        }

        if (_isLoading)
        {
            _waitingForClose = true;
        }

        if (IsTempFile)
        {
            //_logger.Info($"Deleting temp file {FileName}");

            try
            {
                File.Delete(FileName);
            }
            catch (IOException e)
            {
                _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(CloseLogWindow), e));
            }
        }

        FilterPipe?.CloseAndDisconnect();
        DisconnectFilterPipes();
        ClearAndDisposeGrids();
    }

    /// <summary>
    /// Dispose and clear the DataGridViews
    /// </summary>
    private void ClearAndDisposeGrids ()
    {
        dataGridView.Rows.Clear();
        dataGridView.Dispose();

        filterGridView.Rows.Clear();
        filterGridView.Dispose();
    }

    public void WaitForLoadingFinished ()
    {
        _ = _externaLoadingFinishedEvent.WaitOne();
    }

    public void ForceColumnizer (ILogLineMemoryColumnizer columnizer)
    {
        _forcedColumnizer = ColumnizerPicker.CloneMemoryColumnizer(columnizer, ConfigManager.ActiveConfigDir);
        SetColumnizer(_forcedColumnizer);
    }

    public void ForceColumnizerForLoading (ILogLineMemoryColumnizer columnizer)
    {
        _forcedColumnizerForLoading = ColumnizerPicker.CloneMemoryColumnizer(columnizer, ConfigManager.ActiveConfigDir);
    }

    private void PreSelectColumnizer (ILogLineMemoryColumnizer columnizer)
    {
        CurrentColumnizer = columnizer != null
            ? (_forcedColumnizerForLoading = columnizer)
            : (_forcedColumnizerForLoading = ColumnizerPicker.FindMemoryColumnizer(FileName, _logFileReader, _pluginRegistry.RegisteredColumnizers));
    }

    public void PreSelectColumnizerByName (string columnizerName)
    {
        var columnizer = ColumnizerPicker.FindMemoryColumnizerByName(columnizerName, _pluginRegistry.RegisteredColumnizers);
        PreSelectColumnizer(ColumnizerPicker.CloneMemoryColumnizer(columnizer, ConfigManager.ActiveConfigDir));
    }

    public void ColumnizerConfigChanged ()
    {
        SetColumnizerInternal(CurrentColumnizer);
    }

    public void SetColumnizer (ILogLineMemoryColumnizer columnizer, BufferedDataGridView gridView)
    {
        PaintHelper.SetColumnizer(columnizer, gridView);

        gridView.Refresh();
        AutoResizeColumns(gridView);
        ApplyFrozenState(gridView);
    }

    public IColumnMemory GetCellValue (int rowIndex, int columnIndex)
    {
        if (columnIndex == 1)
        {
            return new Column
            {
                FullValue = $"{rowIndex + 1}".AsMemory() // line number
            };
        }

        if (columnIndex == 0) // marker column
        {
            return Column.EmptyColumn;
        }

        try
        {
            var cols = _columnCache.GetColumnsForLine(_logFileReader, rowIndex, CurrentColumnizer, ColumnizerCallbackObject);
            if (cols != null && cols.ColumnValues != null)
            {
                if (columnIndex <= cols.ColumnValues.Length + 1)
                {
                    var value = cols.ColumnValues[columnIndex - 2];

                    return value != null && !value.DisplayValue.IsEmpty
                        ? value
                        : Column.EmptyColumn;
                }

                return columnIndex == 2
                    ? cols.ColumnValues[^1]
                    : Column.EmptyColumn;
            }
        }
#if DEBUG
        catch (IndexOutOfRangeException ex)
        {

            _logger.Warn(ex, "Failed to get cell value due to index error. rowIndex={RowIndex}, columnIndex={ColumnIndex}", rowIndex, columnIndex);
            return Column.EmptyColumn;
        }
#else
        catch (IndexOutOfRangeException)
        {
            return Column.EmptyColumn;

        }
#endif
#if DEBUG
        catch (ArgumentOutOfRangeException ex)
        {

            _logger.Warn(ex, "Failed to get cell value due to argument range error. rowIndex={RowIndex}, columnIndex={ColumnIndex}", rowIndex, columnIndex);
            return Column.EmptyColumn;
        }
#else
        catch (ArgumentOutOfRangeException)
        {
            return Column.EmptyColumn;
        }
#endif
#if DEBUG
        catch (NullReferenceException ex)
        {

            _logger.Warn(ex, "Failed to get cell value due to null state. rowIndex={RowIndex}, columnIndex={ColumnIndex}", rowIndex, columnIndex);
            return Column.EmptyColumn;
        }
#else
        catch (NullReferenceException)
        {
            return Column.EmptyColumn;
        }
#endif
        return Column.EmptyColumn;
    }

    /// <summary>
    /// Filter grid variant of GetCellValue that uses _filterColumnCache (which pins
    /// the filter's visible buffers) instead of _columnCache (which pins the main grid's range).
    /// </summary>
    private IColumnMemory GetFilterCellValue (int rowIndex, int columnIndex)
    {
        if (columnIndex == 1)
        {
            return new Column
            {
                FullValue = $"{rowIndex + 1}".AsMemory() // line number
            };
        }

        if (columnIndex == 0)
        {
            return Column.EmptyColumn;
        }

        try
        {
            var cols = _filterColumnCache.GetColumnsForLine(_logFileReader, rowIndex, CurrentColumnizer, ColumnizerCallbackObject);
            if (cols != null && cols.ColumnValues != null)
            {
                if (columnIndex <= cols.ColumnValues.Length + 1)
                {
                    var value = cols.ColumnValues[columnIndex - 2];

                    return value != null && !value.DisplayValue.IsEmpty
                        ? value
                        : Column.EmptyColumn;
                }

                return columnIndex == 2
                    ? cols.ColumnValues[^1]
                    : Column.EmptyColumn;
            }
        }
#if DEBUG
        catch (IndexOutOfRangeException ex)
        {

            _logger.Warn(ex, "Failed to get filter cell value due to index error. rowIndex={RowIndex}, columnIndex={ColumnIndex}", rowIndex, columnIndex);
            return Column.EmptyColumn;
        }
#else
        catch (IndexOutOfRangeException)
        {
            return Column.EmptyColumn;
        }
#endif
#if DEBUG
        catch (ArgumentOutOfRangeException ex)
        {

            _logger.Warn(ex, "Failed to get filter cell value due to argument range error. rowIndex={RowIndex}, columnIndex={ColumnIndex}", rowIndex, columnIndex);
            return Column.EmptyColumn;
        }
#else
        catch (ArgumentOutOfRangeException)
        {
            return Column.EmptyColumn;
        }
#endif
#if DEBUG
        catch (NullReferenceException ex)
        {
            _logger.Warn(ex, "Failed to get filter cell value due to null state. rowIndex={RowIndex}, columnIndex={ColumnIndex}", rowIndex, columnIndex);
            return Column.EmptyColumn;
        }
#else
        catch (NullReferenceException)
        {
            return Column.EmptyColumn;
        }
#endif

        return Column.EmptyColumn;
    }

    public void CellPainting (bool focused, int rowIndex, int columnIndex, bool isFilteredGridView, DataGridViewCellPaintingEventArgs e)
    {
        if (rowIndex < 0 || columnIndex < 0 || (isFilteredGridView && _filterResultList.Count <= rowIndex))
        {
            e.Handled = false;
            return;
        }

        if (isFilteredGridView)
        {
            rowIndex = _filterResultList[rowIndex];
        }

        // Ensure prefetch is current — CellPainting fires BEFORE CellValueNeeded on scroll jumps,
        // so the prefetch may still be at the old range. Without this, GetPrefetchedLine returns null
        // and the fallback (GetLogLineMemoryWithWait) fetches an unprotected line whose backing block
        // can be returned to the shared ArrayPool and rented by another window's reader.
        if (!isFilteredGridView)
        {
            PrefetchVisibleLines();
        }
        else
        {
            PrefetchFilterVisibleLines();
        }

        // Use only prefetched (pinned) data — no unprotected fallback
        ILogLineMemory line = !isFilteredGridView
            ? _columnCache.GetPrefetchedLine(rowIndex)
            : _filterColumnCache.GetPrefetchedLine(rowIndex);

        if (line == null)
        {
            // Fallback: prefetch a single-row range covering the requested row and retry.
            // This handles the case where CellPainting runs before the grid's layout has
            // populated FirstDisplayedScrollingRowIndex / DisplayedRowCount (common for
            // very small files with only one visible row), and PrefetchVisibleLines
            // therefore short-circuits without pinning anything.
            var targetCache = !isFilteredGridView ? _columnCache : _filterColumnCache;
            targetCache.Prefetch(_logFileReader, rowIndex, 1);
            line = targetCache.GetPrefetchedLine(rowIndex);
        }

        if (line == null)
        {
            _logger.Warn("CellPainting: null line for rowIndex={0}, isFilteredGridView={1}", rowIndex, isFilteredGridView);

            // Paint an empty cell with proper colors to prevent white-on-white default rendering
            e.Graphics.SetClip(e.CellBounds);
            using (var brush = new SolidBrush(e.CellStyle.BackColor))
            {
                e.Graphics.FillRectangle(brush, e.CellBounds);
            }

            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
            e.Handled = true;
            return;
        }

        {
            var entry = FindFirstNoWordMatchHighlightEntry(line);
            e.Graphics.SetClip(e.CellBounds);

            var selection = SelectionPainter.GetStyle(Preferences.SelectionHighlight,
                e.State.HasFlag(DataGridViewElementStates.Selected), focused, e.CellStyle.SelectionBackColor, Application.IsDarkModeEnabled);
            if (!selection.PaintBackground(e))
            {
                e.CellStyle.BackColor = PaintHelper.GetBackColorFromHighlightEntry(entry, Application.IsDarkModeEnabled);
                e.PaintBackground(e.ClipBounds, false);
            }

            if (DebugOptions.DisableWordHighlight)
            {
                e.CellStyle.SelectionForeColor = selection.Foreground(e.CellStyle.ForeColor);
                e.PaintContent(e.CellBounds);
            }
            else
            {
                PaintCell(e, entry, selection);
            }

            if (columnIndex == 0)
            {
                if (_bookmarkProvider.IsBookmarkAtLine(rowIndex))
                {
                    //keeping this comment, because it's the original code
                    // = new Rectangle(e.CellBounds.Left + 2, e.CellBounds.Top + 2, 6, 6);
                    var rect = e.CellBounds;
                    rect.Inflate(-2, -2);
                    var bookmark = _bookmarkProvider.GetBookmarkForLine(rowIndex);
                    var bookmarkColor = bookmark.IsAutoGenerated ? AutoBookmarkColor : BookmarkColor;
                    using var brush = new SolidBrush(bookmarkColor);
                    e.Graphics.FillRectangle(brush, rect);

                    if (bookmark.Text.Length > 0)
                    {
                        //Todo Add this as a Settings Option
                        var fontName = isFilteredGridView ? FONT_VERDANA : FONT_COURIER_NEW;
                        var stringToDraw = isFilteredGridView ? "!" : "i";

                        using var brush2 = new SolidBrush(Color.FromArgb(255, 190, 100, 0)); //dark orange
                        using var font = new Font(fontName, Preferences.Font.Size, FontStyle.Bold);
                        e.Graphics.DrawString(stringToDraw, font, brush2, new RectangleF(rect.Left, rect.Top, rect.Width, rect.Height), _format);
                    }
                }
            }

            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
            e.Handled = true;
        }
    }

    public void OnDataGridViewCellPainting (object sender, DataGridViewCellPaintingEventArgs e)
    {
        var gridView = (BufferedDataGridView)sender;
        CellPainting(gridView.Focused, e.RowIndex, e.ColumnIndex, false, e);
    }

    /// <summary>
    /// Returns the first HilightEntry that matches the given line
    /// </summary>
    /// <param name="line"></param>
    /// <param name="noWordMatches"></param>
    /// <returns></returns>
    public HighlightEntry FindHighlightEntry (ITextValueMemory line, bool noWordMatches)
    {
        // first check the temp entries
        lock (_tempHighlightEntryListLock)
        {
            foreach (var entry in _tempHighlightEntryList)
            {
                if (noWordMatches && entry.IsWordMatch)
                {
                    continue;
                }

                if (HighlightEvaluator.IsMatch(entry, line))
                {
                    return entry;
                }
            }
        }

        lock (_currentHighlightGroupLock)
        {
            foreach (var entry in _currentHighlightGroup.HighlightEntryList)
            {
                if (noWordMatches && entry.IsWordMatch)
                {
                    continue;
                }

                if (HighlightEvaluator.IsMatch(entry, line))
                {
                    return entry;
                }
            }

            return null;
        }
    }

    public IList<HighlightMatchEntry> FindHighlightMatches (ITextValueMemory line)
    {
        IList<HighlightMatchEntry> resultList = [];

        if (line != null)
        {
            lock (_currentHighlightGroupLock)
            {
                GetHighlightEntryMatches(line, _currentHighlightGroup.HighlightEntryList, resultList);
            }

            lock (_tempHighlightEntryListLock)
            {
                GetHighlightEntryMatches(line, _tempHighlightEntryList, resultList);
            }
        }

        return resultList;
    }

    public void FollowTailChanged (bool isChecked, bool byTrigger)
    {
        _guiStateArgs.FollowTail = isChecked;

        if (_guiStateArgs.FollowTail && _logFileReader != null)
        {
            if (dataGridView.RowCount >= _logFileReader.LineCount && _logFileReader.LineCount > 0)
            {
                // Mark stale instead of invalidating — keeps old buffers pinned until
                // the next Prefetch atomically swaps in new pins.
                _columnCache.MarkPrefetchStale();
                dataGridView.FirstDisplayedScrollingRowIndex = _logFileReader.LineCount - 1;
            }
        }

        _ = BeginInvoke(new MethodInvoker(dataGridView.Refresh));
        _logWindowCoordinator.NotifyFollowTailChanged(this, isChecked, byTrigger);
        SendGuiStateUpdate();
    }

    public void TryToTruncate ()
    {
        try
        {
            if (LockFinder.CheckIfFileIsLocked(Title))
            {
                var name = LockFinder.FindLockedProcessName(Title);
                StatusLineText(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_StatusLineText_TruncateFailedFileIsLockedByName, name));
            }
            else
            {
                File.WriteAllText(Title, string.Empty);
            }
        }
        catch (Exception)
        {
            //_logger.Warn($"Unexpected issue truncating file: {ex.Message}");
            StatusLineText(Resources.LogWindow_UI_StatusLineText_UnexpectedIssueTruncatingFile);
            throw;
        }
    }

    public void GotoLine (int line)
    {
        if (line >= 0)
        {
            if (line < dataGridView.RowCount)
            {
                SelectLine(line, false, true);
            }
            else
            {
                SelectLine(dataGridView.RowCount - 1, false, true);
            }

            _ = dataGridView.Focus();
        }
    }

    public void StartSearch ()
    {
        _guiStateArgs.MenuEnabled = false;
        GuiStateUpdate(this, _guiStateArgs);
        var searchParams = _logWindowCoordinator.SearchParams;

        searchParams.CurrentLine = LogSearcher.ResolveDirection(searchParams) == SearchDirection.Forward
            ? dataGridView.CurrentCellAddress.Y + 1
            : dataGridView.CurrentCellAddress.Y - 1;

        _currentSearchParams = searchParams; // remember for async "not found" messages

        _isSearching = true;
        StatusLineText(Resources.LogWindow_UI_StatusLineText_SearchingPressESCToCancel);

        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = dataGridView.RowCount;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        // Replace the token source only after a consumed cancellation; a still-running previous
        // search keeps sharing it, so one ESC cancels every running search.
        if (_searchCts == null || _searchCts.IsCancellationRequested)
        {
            _searchCts?.Dispose();
            _searchCts = CancellationTokenSource.CreateLinkedTokenSource(_windowCts.Token);
        }

        var cancellationToken = _searchCts.Token;
        var progress = new SynchronousProgress<SearchProgress>(ReportSearchProgress);

        _ = Task.Run(() => LogSearcher.Find(searchParams, _logFileReader, cancellationToken, progress), CancellationToken.None)
            .ContinueWith(SearchComplete, TaskScheduler.Default);

        RemoveAllSearchHighlightEntries();
        AddSearchHitHighlightEntry(searchParams);
    }

    private void SearchComplete (Task<SearchResult> task)
    {
        if (Disposing)
        {
            return;
        }

        try
        {
            _ = Invoke(new MethodInvoker(ResetProgressBar));
            var result = task.Result;
            _isSearching = false; // was SelectLine's job when it doubled as the search epilogue
            _guiStateArgs.MenuEnabled = true;
            GuiStateUpdate(this, _guiStateArgs);

            switch (result.Outcome)
            {
                case SearchOutcome.Found:
                    StatusLineText(string.Empty);
                    _ = dataGridView.Invoke(new SelectLineFx((line1, triggerSyncCall) => SelectLine(line1, triggerSyncCall, true)), result.LineNumber, true);
                    break;

                case SearchOutcome.NotFound:
                    StatusLineError(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_StatusLineError_NotFound, _currentSearchParams.SearchText));
                    break;

                case SearchOutcome.InvalidPattern:
                    StatusLineError(Resources.LogWindow_UI_StatusLineError_InvalidRegularExpression);
                    break;

                default: // Cancelled — clear the busy status
                    StatusLineText(string.Empty);
                    break;
            }
        }
        catch (Exception ex) // in the case the windows is already destroyed
        {
            _logger.Warn(ex);
        }
    }

    public void SelectLogLine (int lineNumber)
    {
        _ = Invoke(new SelectLineFx((line1, triggerSyncCall) => SelectLine(line1, triggerSyncCall, true)), lineNumber, true);
    }

    public void SelectAndEnsureVisible (int line, bool triggerSyncCall)
    {
        try
        {
            SelectLine(line, triggerSyncCall, false);

            //if (!this.dataGridView.CurrentRow.Displayed)
            if (line < dataGridView.FirstDisplayedScrollingRowIndex || line > dataGridView.FirstDisplayedScrollingRowIndex + dataGridView.DisplayedRowCount(false))
            {
                dataGridView.FirstDisplayedScrollingRowIndex = line;
                for (var i = 0; i < 8 && dataGridView.FirstDisplayedScrollingRowIndex > 0 && line < dataGridView.FirstDisplayedScrollingRowIndex + dataGridView.DisplayedRowCount(false); ++i)
                {
                    dataGridView.FirstDisplayedScrollingRowIndex -= 1;
                }

                if (line >= dataGridView.FirstDisplayedScrollingRowIndex + dataGridView.DisplayedRowCount(false))
                {
                    dataGridView.FirstDisplayedScrollingRowIndex += 1;
                }
            }

            dataGridView.CurrentCell = dataGridView.Rows[line].Cells[0];
        }
        catch (Exception e)
        {
            // In rare situations there seems to be an invalid argument exceptions (or something like this). Concrete location isn't visible in stack
            // trace because use of Invoke(). So catch it, and log (better than crashing the app).
            _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(SelectAndEnsureVisible), e));
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Only Add if a new Key is introduced")]
    public void OnLogWindowKeyDown (object sender, KeyEventArgs e)
    {
        if (_isErrorShowing)
        {
            RemoveStatusLineError();
        }

        switch (e.KeyCode)
        {
            case Keys.F3 when _logWindowCoordinator.SearchParams?.SearchText == null || _logWindowCoordinator.SearchParams.SearchText.Length == 0:
                {
                    return;
                }
            case Keys.F3:
                {
                    _logWindowCoordinator.SearchParams.IsFindNext = true;
                    _logWindowCoordinator.SearchParams.IsShiftF3Pressed = (e.Modifiers & Keys.Shift) == Keys.Shift;
                    StartSearch();
                    break;
                }
            case Keys.Escape:
                {
                    if (_isSearching)
                    {
                        _searchCts?.Cancel();
                    }

                    CancelHighlightBookmarkScan();
                    FireCancelHandlers();
                    RemoveAllSearchHighlightEntries();


                    break;
                }
            case Keys.E when (e.Modifiers & Keys.Control) == Keys.Control:
                {
                    StartEditMode();

                    break;
                }
            case Keys.Down when e.Modifiers == Keys.Alt:
                {
                    var newLine = _logFileReader.GetNextMultiFileLine(dataGridView.CurrentCellAddress.Y);

                    if (newLine != -1)
                    {
                        SelectLine(newLine, false, true);
                    }

                    e.Handled = true;

                    break;
                }
            case Keys.Up when e.Modifiers == Keys.Alt:
                {
                    var newLine = _logFileReader.GetPrevMultiFileLine(dataGridView.CurrentCellAddress.Y);

                    if (newLine != -1)
                    {
                        SelectLine(newLine - 1, false, true);
                    }

                    e.Handled = true;

                    break;
                }
            case Keys.Enter when dataGridView.Focused:
                {
                    ChangeRowHeight(e.Shift);
                    e.Handled = true;

                    break;
                }
            case Keys.Back when dataGridView.Focused:
                {
                    ChangeRowHeight(true);
                    e.Handled = true;

                    break;
                }
            case Keys.PageUp when e.Modifiers == Keys.Alt:
                {
                    SelectPrevHighlightLine();
                    e.Handled = true;

                    break;
                }
            case Keys.PageDown when e.Modifiers == Keys.Alt:
                {
                    SelectNextHighlightLine();
                    e.Handled = true;

                    break;
                }
            case Keys.T when (e.Modifiers & Keys.Control) == Keys.Control && (e.Modifiers & Keys.Shift) == Keys.Shift:
                {
                    FilterToTab();
                    break;
                }
        }
    }

    public void AddBookmarkOverlays ()
    {
        var firstLine = dataGridView.FirstDisplayedScrollingRowIndex;
        if (firstLine < 0)
        {
            return;
        }

        firstLine -= OVERSCAN;
        if (firstLine < 0)
        {
            firstLine = 0;
        }

        var oversizeCount = OVERSCAN;

        for (var i = firstLine; i < dataGridView.RowCount; ++i)
        {
            if (!dataGridView.Rows[i].Displayed && i > dataGridView.FirstDisplayedScrollingRowIndex)
            {
                if (oversizeCount-- < 0)
                {
                    break;
                }
            }

            if (_bookmarkProvider.IsBookmarkAtLine(i))
            {
                var bookmark = _bookmarkProvider.GetBookmarkForLine(i);
                if (bookmark.Text.Length > 0)
                {
                    //BookmarkOverlay overlay = new BookmarkOverlay();
                    var overlay = bookmark.Overlay;
                    overlay.Bookmark = bookmark;

                    Rectangle r;
                    if (dataGridView.Rows[i].Displayed)
                    {
                        r = dataGridView.GetCellDisplayRectangle(0, i, false);
                    }
                    else
                    {
                        r = dataGridView.GetCellDisplayRectangle(0, dataGridView.FirstDisplayedScrollingRowIndex, false);
                        //int count = i - this.dataGridView.FirstDisplayedScrollingRowIndex;
                        var heightSum = 0;
                        if (dataGridView.FirstDisplayedScrollingRowIndex < i)
                        {
                            for (var rn = dataGridView.FirstDisplayedScrollingRowIndex + 1; rn < i; ++rn)
                            {
                                //Rectangle rr = this.dataGridView.GetCellDisplayRectangle(0, rn, false);
                                //heightSum += rr.Height;
                                heightSum += GetRowHeight(rn);
                            }

                            r.Offset(0, r.Height + heightSum);
                        }
                        else
                        {
                            for (var rn = dataGridView.FirstDisplayedScrollingRowIndex + 1; rn > i; --rn)
                            {
                                //Rectangle rr = this.dataGridView.GetCellDisplayRectangle(0, rn, false);
                                //heightSum += rr.Height;
                                heightSum += GetRowHeight(rn);
                            }

                            r.Offset(0, -(r.Height + heightSum));
                        }
                        //r.Offset(0, this.dataGridView.DisplayRectangle.Height);
                    }

                    if (_logger.IsDebugEnabled)
                    {
                        _logger.Debug($"### AddBookmarkOverlay: r.Location={r.Location.X}, width={r.Width}, scroll_offset={dataGridView.HorizontalScrollingOffset}");
                    }

                    overlay.Position = r.Location - new Size(dataGridView.HorizontalScrollingOffset, 0);
                    overlay.Position += new Size(10, r.Height / 2);
                    dataGridView.AddOverlay(overlay);
                }
            }
        }
    }

    public void ToggleBookmark ()
    {
        BufferedDataGridView gridView;
        int lineNum;

        if (filterGridView.Focused)
        {
            gridView = filterGridView;
            if (gridView.CurrentCellAddress.Y == -1)
            {
                return;
            }

            lineNum = _filterResultList[gridView.CurrentCellAddress.Y];
        }
        else
        {
            gridView = dataGridView;
            if (gridView.CurrentCellAddress.Y == -1)
            {
                return;
            }

            lineNum = dataGridView.CurrentCellAddress.Y;
        }

        ToggleBookmark(lineNum);
    }

    public void ToggleBookmark (int lineNum)
    {
        if (_bookmarkProvider.IsBookmarkAtLine(lineNum))
        {
            var bookmark = _bookmarkProvider.GetBookmarkForLine(lineNum);

            // If it's an auto-generated bookmark, convert to manual instead of removing
            if (bookmark.IsAutoGenerated)
            {
                _ = _bookmarkProvider.ConvertToManualBookmark(lineNum);
            }
            else
            {
                if (!string.IsNullOrEmpty(bookmark.Text))
                {
                    if (MessageBox.Show(Resources.LogWindow_UI_ToggleBookmark_ThereCommentAttachedRemoveIt, Resources.LogExpert_Common_UI_Title_LogExpert, MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                }

                _bookmarkProvider.RemoveBookmarkForLine(lineNum);
            }
        }
        else
        {
            _bookmarkProvider.AddBookmark(new Bookmark(lineNum));
        }

        RefreshAllGrids();
        OnBookmarksChanged();
    }

    public void SetBookmarkFromTrigger (int lineNum, string comment)
    {
        var line = _logFileReader.GetLogLineMemory(lineNum);

        if (line == null)
        {
#if DEBUG
            _logger.Warn($"SetBookmarkFromTrigger: line {lineNum} returned null, bookmark not set");
#endif
            return;
        }

        var paramParser = new ParamParser(comment);

        try
        {
            comment = paramParser.ReplaceParams(line, lineNum, FileName);
        }
        catch (ArgumentException)
        {
            // occurs on invalid regex
        }

        if (_bookmarkProvider.IsBookmarkAtLine(lineNum))
        {
            var existing = _bookmarkProvider.GetBookmarkForLine(lineNum);

            // Don't overwrite manual bookmarks with auto-generated ones
            if (!existing.IsAutoGenerated)
            {
                return;
            }

            _bookmarkProvider.RemoveBookmarkForLine(lineNum);
        }

        _bookmarkProvider.AddBookmark(Bookmark.CreateAutoGenerated(lineNum, comment, GetSourceHighlightTextForLine(lineNum)));
        OnBookmarksChanged();
    }

    /// <summary>
    /// Returns the SearchText of the first matching highlight entry with IsSetBookmark for the given line. Used to set
    /// SourceHighlightText on trigger-created bookmarks.
    /// </summary>
    private string GetSourceHighlightTextForLine (int lineNum)
    {
        var line = _logFileReader.GetLogLineMemory(lineNum);

        if (line == null)
        {
            return string.Empty;
        }

        var matchingList = FindMatchingHighlightEntries(line);

        foreach (var entry in matchingList)
        {
            if (entry.IsSetBookmark)
            {
                return entry.SearchText;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Cancels any in-progress highlight bookmark scan, removes existing auto-generated bookmarks, and starts a new
    /// background scan based on the current highlight group.
    /// </summary>
    private void RunHighlightBookmarkScan ()
    {
        // Guard: don't scan during loading or if reader is not available
        if (_isLoading || _logFileReader == null)
        {
            return;
        }

        // Cancel any in-progress scan
        CancelHighlightBookmarkScan();

        // Step 1: Remove previous auto-generated bookmarks
        _bookmarkProvider.RemoveAutoGeneratedBookmarks();
        RefreshAllGrids();

        // Step 2: Get current highlight entries (snapshot under lock)
        List<HighlightEntry> entries;
        lock (_currentHighlightGroupLock)
        {
            entries = [.. _currentHighlightGroup.HighlightEntryList];
        }

        // Step 3: Early exit if no entries have IsSetBookmark
        if (!entries.Any(e => e.IsSetBookmark))
        {
            return;
        }

        // Step 4: Start background scan
        var cts = new CancellationTokenSource();
        _highlightBookmarkScanCts = cts;
        var lineCount = _logFileReader.LineCount;
        var fileName = FileName;

        StatusLineText(Resources.LogWindow_UI_StatusLineText_ScanningBookmarks);
        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = lineCount;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        var progress = new Progress<int>(OnHighlightBookmarkScanProgress);
        _ = Task.Run(() => ExecuteHighlightBookmarkScan(lineCount, entries, fileName, progress, cts));
    }

    private void ExecuteHighlightBookmarkScan (int lineCount, List<HighlightEntry> entries, string fileName, IProgress<int> progress, CancellationTokenSource cts)
    {
        using (cts)
        {
            try
            {
                var bookmarks = HighlightBookmarkScanner.Scan(lineCount, _logFileReader.GetLogLineMemory, entries, fileName, PROGRESS_BAR_MODULO, progress, cts.Token);

                // Marshal bookmark additions to UI thread
                if (!cts.Token.IsCancellationRequested && IsHandleCreated && !IsDisposed)
                {
                    _ = BeginInvoke(() =>
                    {
                        _ = _bookmarkProvider.AddBookmarks(bookmarks);

                        RefreshAllGrids();

                        _progressEventArgs.Visible = false;
                        SendProgressBarUpdate();
                        StatusLineText(string.Empty);
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Scan was cancelled — clean up on UI thread
                if (IsHandleCreated && !IsDisposed)
                {
                    _ = BeginInvoke(() =>
                    {
                        _progressEventArgs.Visible = false;
                        SendProgressBarUpdate();
                        StatusLineText(string.Empty);
                    });
                }
            }
            finally
            {
                // Only dispose if this is still the active CTS
                if (_highlightBookmarkScanCts == cts)
                {
                    _highlightBookmarkScanCts = null;
                }
            }
        }
    }

    private void OnHighlightBookmarkScanProgress (int currentLine)
    {
        try
        {
            if (_highlightBookmarkScanCts is not { } cts)
            {
                return;
            }

            if (cts.Token.IsCancellationRequested)
            {
                StatusLineText(Resources.LogWindow_UI_StatusLineText_ScanningBookmarksEnded);
                return;
            }
        }
        catch (ObjectDisposedException)
        {
            // CTS was disposed after task completed — progress callback is stale
            return;
        }

        if (IsHandleCreated && !IsDisposed)
        {
            _ = BeginInvoke(() =>
            {
                _progressEventArgs.Value = currentLine;
                SendProgressBarUpdate();

                var lineCount = _logFileReader?.LineCount ?? 0;
                if (lineCount > 0)
                {
                    var pct = (int)((long)currentLine * 100 / lineCount);
                    StatusLineText(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_StatusLineText_ScanningBookmarksPct, pct));
                }
            });
        }
    }

    /// <summary>
    /// Cancels any currently running highlight bookmark scan.
    /// </summary>
    private void CancelHighlightBookmarkScan ()
    {
        var cts = _highlightBookmarkScanCts;
        if (cts != null)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed — ignore
            }
        }
    }

    public void JumpNextBookmark ()
    {
        if (_bookmarkProvider.Bookmarks.Count > 0)
        {
            if (filterGridView.Focused)
            {
                var index = FindNextBookmarkIndex(_filterResultList[filterGridView.CurrentCellAddress.Y]);
                var startIndex = index;
                var wrapped = false;
                while (true)
                {
                    var lineNum = _bookmarkProvider.Bookmarks[index].LineNum;
                    if (_filterResultList.Contains(lineNum))
                    {
                        var filterLine = _filterResultList.IndexOf(lineNum);
                        filterGridView.Rows[filterLine].Selected = true;
                        filterGridView.CurrentCell = filterGridView.Rows[filterLine].Cells[0];
                        break;
                    }

                    index++;
                    if (index > _bookmarkProvider.Bookmarks.Count - 1)
                    {
                        index = 0;
                        wrapped = true;
                    }

                    if (index >= startIndex && wrapped)
                    {
                        break;
                    }
                }
            }
            else
            {
                var index = FindNextBookmarkIndex(dataGridView.CurrentCellAddress.Y);
                if (index > _bookmarkProvider.Bookmarks.Count - 1)
                {
                    index = 0;
                }

                var lineNum = _bookmarkProvider.Bookmarks[index].LineNum;
                SelectLine(lineNum, true, true);
            }
        }
    }

    public void JumpPrevBookmark ()
    {
        if (_bookmarkProvider.Bookmarks.Count > 0)
        {
            if (filterGridView.Focused)
            {
                //int index = this.bookmarkList.BinarySearch(this.filterResultList[this.filterGridView.CurrentCellAddress.Y]);
                //if (index < 0)
                //  index = ~index;
                //index--;
                var index = FindPrevBookmarkIndex(_filterResultList[filterGridView.CurrentCellAddress.Y]);
                if (index < 0)
                {
                    index = _bookmarkProvider.Bookmarks.Count - 1;
                }

                var startIndex = index;
                var wrapped = false;

                while (true)
                {
                    var lineNum = _bookmarkProvider.Bookmarks[index].LineNum;

                    if (_filterResultList.Contains(lineNum))
                    {
                        var filterLine = _filterResultList.IndexOf(lineNum);
                        filterGridView.Rows[filterLine].Selected = true;
                        filterGridView.CurrentCell = filterGridView.Rows[filterLine].Cells[0];
                        break;
                    }

                    index--;

                    if (index < 0)
                    {
                        index = _bookmarkProvider.Bookmarks.Count - 1;
                        wrapped = true;
                    }

                    if (index <= startIndex && wrapped)
                    {
                        break;
                    }
                }
            }
            else
            {
                var index = FindPrevBookmarkIndex(dataGridView.CurrentCellAddress.Y);
                if (index < 0)
                {
                    index = _bookmarkProvider.Bookmarks.Count - 1;
                }

                var lineNum = _bookmarkProvider.Bookmarks[index].LineNum;
                SelectLine(lineNum, false, true);
            }
        }
    }

    public void DeleteBookmarks (List<int> lineNumList)
    {
        var bookmarksPresent = false;
        foreach (var lineNum in lineNumList)
        {
            if (lineNum != -1)
            {
                if (_bookmarkProvider.IsBookmarkAtLine(lineNum) &&
                    _bookmarkProvider.GetBookmarkForLine(lineNum).Text.Length > 0)
                {
                    bookmarksPresent = true;
                }
            }
        }

        if (bookmarksPresent)
        {
            if (
                MessageBox.Show(
                    Resources.LogWindow_UI_ThereAreSomeCommentsInTheBookmarksReallyRemoveBookmarks,
                    Resources.LogExpert_Common_UI_Title_LogExpert,
                    MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }
        }

        _bookmarkProvider.RemoveBookmarksForLines(lineNumList);
        OnBookmarksChanged();
    }

    public void SetTimeshiftValue (string value)
    {
        _guiStateArgs.TimeshiftText = value;
        if (CurrentColumnizer.IsTimeshiftImplemented())
        {
            try
            {
                if (_guiStateArgs.TimeshiftEnabled)
                {
                    try
                    {
                        var text = _guiStateArgs.TimeshiftText;
                        if (text.StartsWith('+'))
                        {
                            text = text[1..];
                        }

                        var timeSpan = TimeSpan.Parse(text, CultureInfo.InvariantCulture);
                        var diff = (int)(timeSpan.Ticks / TimeSpan.TicksPerMillisecond);
                        CurrentColumnizer.SetTimeOffset(diff);
                    }
                    catch (Exception ex) when (ex is FormatException
                                            or ArgumentOutOfRangeException
                                            or OverflowException)
                    {
                        CurrentColumnizer.SetTimeOffset(0);
                    }
                }
                else
                {
                    CurrentColumnizer.SetTimeOffset(0);
                }

                dataGridView.Refresh();
                filterGridView.Refresh();
                if (CurrentColumnizer.IsTimeshiftImplemented())
                {
                    SetTimestampLimits();
                    SyncTimestampDisplay();
                }
            }
            catch (FormatException e)
            {
                _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.Logger_Error_In_Function, nameof(SetTimeshiftValue), e));
            }
        }
    }

    public void ToggleFilterPanel ()
    {
        splitContainerLogWindow.Panel2Collapsed = !splitContainerLogWindow.Panel2Collapsed;
        _ = !splitContainerLogWindow.Panel2Collapsed
            ? filterComboBox.Focus()
            : dataGridView.Focus();
    }

    public void LogWindowActivated ()
    {
        if (_guiStateArgs.FollowTail && !_isDeadFile)
        {
            OnTailFollowed(EventArgs.Empty);
        }

        if (Preferences.TimestampControl)
        {
            SetTimestampLimits();
            SyncTimestampDisplay();
        }

        _ = dataGridView.Focus();

        SendGuiStateUpdate();
        SendStatusLineUpdate();
        SendProgressBarUpdate();
    }

    public void SetCellSelectionMode (bool isCellMode, bool updateGUI = false)
    {
        if (isCellMode)
        {
            //possible performance issue, see => https://docs.microsoft.com/en-us/dotnet/desktop/winforms/controls/best-practices-for-scaling-the-windows-forms-datagridview-control?view=netframeworkdesktop-4.8#using-the-selected-cells-rows-and-columns-collections-efficiently
            dataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;
        }
        else
        {
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        _guiStateArgs.CellSelectMode = isCellMode;

        if (updateGUI)
        {
            SendGuiStateUpdate();
        }
    }

    public void TimeshiftEnabled (bool isEnabled, string shiftValue)
    {
        _guiStateArgs.TimeshiftEnabled = isEnabled;
        SetTimestampLimits();
        SetTimeshiftValue(shiftValue);
    }

    public void CopyMarkedLinesToTab ()
    {
        if (dataGridView.SelectionMode == DataGridViewSelectionMode.FullRowSelect)
        {
            var lineNumList = new List<int>();
            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                if (row.Index != -1)
                {
                    lineNumList.Add(row.Index);
                }
            }

            lineNumList.Sort();
            // create dummy FilterPipe for connecting line numbers to original window
            // setting IsStopped to true prevents further filter processing
            var pipe = new FilterPipe(new FilterParams(), this)
            {
                IsStopped = true
            };

            var name = string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_CopyMarkedLinesToTab_Copy, Text);
            WritePipeToTab(pipe, lineNumList, name, null);
        }
        else
        {
            var fileName = Path.GetTempFileName();
            var fStream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read);
            var writer = new StreamWriter(fStream, Encoding.Unicode);

            var data = dataGridView.GetClipboardContent();
            var text = data.GetText(TextDataFormat.Text);
            writer.Write(text);

            writer.Close();
            var title = string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_CopyMarkedLinesToTab_Clip, Util.GetNameFromPath(FileName));
            _ = _logWindowCoordinator.AddTempFileTab(fileName, title);
        }
    }

    /// <summary>
    /// Change the file encoding. May force a reload if byte count ot preamble lenght differs from previous used
    /// encoding.
    /// </summary>
    /// <param name="encoding"></param>
    public void ChangeEncoding (Encoding encoding)
    {
        _logFileReader.ChangeEncoding(encoding);
        EncodingOptions.Encoding = encoding;
        if (_guiStateArgs.CurrentEncoding.IsSingleByte != encoding.IsSingleByte ||
            _guiStateArgs.CurrentEncoding.GetPreamble().Length != encoding.GetPreamble().Length)
        {
            Reload();
        }
        else
        {
            dataGridView.Refresh();
            SendGuiStateUpdate();
        }

        _guiStateArgs.CurrentEncoding = _logFileReader.CurrentEncoding;
    }

    public void Reload ()
    {
        SavePersistenceData(false);

        _reloadMemento = new ReloadMemento
        {
            CurrentLine = dataGridView.CurrentCellAddress.Y,
            FirstDisplayedLine = dataGridView.FirstDisplayedScrollingRowIndex
        };

        _forcedColumnizerForLoading = CurrentColumnizer;

        if (_fileNames == null || !IsMultiFile)
        {
            LoadFile(FileName, EncodingOptions);
        }
        else
        {
            LoadFilesAsMulti(_fileNames, EncodingOptions);
        }
    }

    public void PreferencesChanged (Font font, bool setLastColumnWidth, int lastColumnWidth, bool isLoadTime, SettingsFlags flags)
    {
        dataGridView.SelectionHighlight = Preferences.SelectionHighlight;
        filterGridView.SelectionHighlight = Preferences.SelectionHighlight;
        if ((flags & SettingsFlags.GuiOrColors) == SettingsFlags.GuiOrColors)
        {
            font ??= Preferences.Font ?? new Font(FontFamily.GenericMonospace, 9f);

            NormalFont = font;
            BoldFont = new Font(NormalFont, FontStyle.Bold);
            MonospacedFont = new Font(FONT_COURIER_NEW, NormalFont.Size, FontStyle.Bold);

            var lineSpacing = NormalFont.FontFamily.GetLineSpacing(FontStyle.Regular);
            var lineSpacingPixel = NormalFont.Size * lineSpacing / NormalFont.FontFamily.GetEmHeight(FontStyle.Regular);

            dataGridView.DefaultCellStyle.Font = NormalFont;
            filterGridView.DefaultCellStyle.Font = NormalFont;
            _lineHeight = NormalFont.Height + 4;
            dataGridView.RowTemplate.Height = NormalFont.Height + 4;

            ShowBookmarkBubbles = Preferences.ShowBubbles;

            ApplyDataGridViewPrefs(dataGridView, setLastColumnWidth, lastColumnWidth);
            ApplyDataGridViewPrefs(filterGridView, setLastColumnWidth, lastColumnWidth);

            if (Preferences.TimestampControl)
            {
                SetTimestampLimits();
                SyncTimestampDisplay();
            }

            if (isLoadTime)
            {
                filterTailCheckBox.Checked = Preferences.FilterTail;
                syncFilterCheckBox.Checked = Preferences.FilterSync;
                //this.FollowTailChanged(this.Preferences.followTail, false);
            }

            _timeSpreadCalc.TimeMode = Preferences.TimeSpreadTimeMode;
            timeSpreadingControl.ForeColor = Preferences.TimeSpreadColor;
            timeSpreadingControl.ReverseAlpha = Preferences.ReverseAlpha;

            if (CurrentColumnizer.IsTimeshiftImplemented())
            {
                timeSpreadingControl.Refresh();
                ShowTimeSpread(Preferences.ShowTimeSpread);
            }

            ToggleColumnFinder(Preferences.ShowColumnFinder, false);
        }

        if ((flags & SettingsFlags.FilterList) == SettingsFlags.FilterList)
        {
            HandleChangedFilterList();
        }

        if ((flags & SettingsFlags.FilterHistory) == SettingsFlags.FilterHistory)
        {
            UpdateFilterHistoryFromSettings();

            if (isLoadTime)
            {
                AutoResizeFilterBox();
            }
        }
    }

    public bool ScrollToTimestamp (DateTime timestamp, bool roundToSeconds, bool triggerSyncCall)
    {
        if (InvokeRequired)
        {
            _ = BeginInvoke(new ScrollToTimestampFx(ScrollToTimestampWorker), timestamp, roundToSeconds, triggerSyncCall);
            return true;
        }

        return ScrollToTimestampWorker(timestamp, roundToSeconds, triggerSyncCall);
    }

    public bool ScrollToTimestampWorker (DateTime timestamp, bool roundToSeconds, bool triggerSyncCall)
    {
        var hasScrolled = false;
        if (!CurrentColumnizer.IsTimeshiftImplemented() || dataGridView.RowCount == 0)
        {
            return false;
        }

        //this.Cursor = Cursors.WaitCursor;
        var currentLine = dataGridView.CurrentCellAddress.Y;
        if (currentLine < 0 || currentLine >= dataGridView.RowCount)
        {
            currentLine = 0;
        }

        var foundLine = _timestampLocator.FindLine(timestamp, currentLine, _logFileReader.LineCount, roundToSeconds, _cts.Token);
        if (foundLine >= 0)
        {
            SelectAndEnsureVisible(foundLine, triggerSyncCall);
            hasScrolled = true;
        }

        //this.Cursor = Cursors.Default;
        return hasScrolled;
    }

    /// <summary>
    /// Gets the timestamp for the line at or before <paramref name="lastLineNum"/>, searching
    /// backward through the file for one if that line has none. Delegates to
    /// <see cref="TimestampLocator.FindBackward"/>.
    /// </summary>
    /// <remarks>
    /// Cancels via <see cref="_cts"/>, not <see cref="_windowCts"/>: this mirrors the original
    /// bail-out (<c>_isTimestampDisplaySyncing &amp;&amp; _shouldTimestampDisplaySyncingCancel</c>),
    /// which every caller of this method observed. <c>_cts</c> is cancelled in
    /// <see cref="StopTimestampSyncThread"/> — before it blocks on <see cref="JoinWorker"/> — while
    /// <c>_windowCts</c> is only cancelled afterward, in <see cref="CloseLogWindow"/>. Using
    /// <c>_windowCts</c> here would leave an in-progress scan unable to observe cancellation for the
    /// whole of that teardown wait.
    /// </remarks>
    private (DateTime timeStamp, int lastLineNumber) GetTimestampForLine (int lastLineNum, bool roundToSeconds)
    {
        return _timestampLocator.FindBackward(lastLineNum, _logFileReader.LineCount, roundToSeconds, _cts.Token);
    }

    public void AppFocusLost ()
    {
        InvalidateCurrentRow(dataGridView);
    }

    public void AppFocusGained ()
    {
        InvalidateCurrentRow(dataGridView);
    }

    public ILogLineMemory GetCurrentLine ()
    {
        return dataGridView.CurrentRow != null && dataGridView.CurrentRow.Index != -1
            ? _logFileReader.GetLogLineMemory(dataGridView.CurrentRow.Index)
            : null;
    }

    public ILogLineMemory GetLineMemory (int lineNum)
    {
        return lineNum < 0 || _logFileReader == null || lineNum >= _logFileReader.LineCount
            ? null
            : _logFileReader.GetLogLineMemory(lineNum);
    }

    public int GetRealLineNum ()
    {
        var lineNum = CurrentLineNum;
        return lineNum == -1
            ? -1
            : _logFileReader.GetRealLineNumForVirtualLineNum(lineNum);
    }

    public ILogFileInfo GetCurrentFileInfo ()
    {
        return dataGridView.CurrentRow != null && dataGridView.CurrentRow.Index != -1
            ? _logFileReader.GetLogFileInfoForLine(dataGridView.CurrentRow.Index)
            : null;
    }

    /// <summary>
    /// zero-based
    /// </summary>
    /// <param name="lineNum"></param>
    /// <returns></returns>
    public string GetCurrentFileName (int lineNum)
    {
        return _logFileReader.GetLogFileNameForLine(lineNum);
    }

    // =============== end of bookmark stuff ===================================

    public void ShowLineColumn (bool show)
    {
        dataGridView.Columns[1].Visible = show;
        filterGridView.Columns[1].Visible = show;
    }

    public void ExportBookmarkList ()
    {
        SaveFileDialog dlg = new()
        {
            Title = Resources.LogWindow_UI_Title_ExportBookMarkList,
            AddExtension = true,
            DefaultExt = "csv",
            Filter = Resources.LogWindow_UI_ImportExportBookmarkList_Filter,
            FilterIndex = 1,
            FileName = Path.GetFileNameWithoutExtension(FileName)
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            try
            {
                BookmarkExporter.ExportBookmarkList(_bookmarkProvider.BookmarkList, FileName,
                    dlg.FileName);
            }
            catch (IOException e)
            {
                _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_ErrorWhileExportingBookmarkList, e.Message), Resources.LogExpert_Common_UI_Title_LogExpert);
            }
        }

        dlg.Dispose();
    }

    public void ImportBookmarkList ()
    {
        OpenFileDialog dlg = new()
        {
            Title = Resources.LogWindow_UI_Title_ImportBookmarkList,
            AddExtension = true,
            DefaultExt = "csv",
            Filter = Resources.LogWindow_UI_ImportExportBookmarkList_Filter,
            FilterIndex = 1,
            FileName = Path.GetFileNameWithoutExtension(FileName)
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            try
            {
                // add to the existing bookmarks
                SortedList<int, Bookmark> newBookmarks = [];
                BookmarkExporter.ImportBookmarkList(FileName, dlg.FileName, newBookmarks);

                // Add (or replace) to existing bookmark list
                var bookmarkAdded = false;
                foreach (var b in newBookmarks.Values)
                {
                    if (_bookmarkProvider.BookmarkList.TryGetValue(b.LineNum, out Bookmark? existingBookmark))
                    {
                        // replace existing bookmark for that line, preserving the overlay
                        existingBookmark.Text = b.Text;
                        OnBookmarkTextChanged(b);
                    }
                    else
                    {
                        _bookmarkProvider.BookmarkList.Add(b.LineNum, b);
                        bookmarkAdded = true; // refresh the list only once at the end
                    }
                }

                // Refresh the lists
                if (bookmarkAdded)
                {
                    OnBookmarksChanged();
                }

                RefreshAllGrids();
            }
            catch (IOException e)
            {
                _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.LogWindow_UI_ErrorWhileImportingBookmarkList, e.Message), Resources.LogExpert_Common_UI_Title_LogExpert);
            }
        }

        dlg.Dispose();
    }

    public bool IsAdvancedOptionActive ()
    {
        return rangeCheckBox.Checked ||
               knobControlFuzzy.Value > 0 ||
               knobControlFilterBackSpread.Value > 0 ||
               knobControlFilterForeSpread.Value > 0 ||
               invertFilterCheckBox.Checked ||
               columnRestrictCheckBox.Checked;
    }

    public void HandleChangedFilterList ()
    {
        _ = Invoke(new MethodInvoker(HandleChangedFilterListWorker));
    }

    public void HandleChangedFilterListWorker ()
    {
        var index = listBoxFilter.SelectedIndex;
        listBoxFilter.Items.Clear();
        foreach (var filterParam in ConfigManager.Settings.FilterList)
        {
            _ = listBoxFilter.Items.Add(filterParam);
        }

        listBoxFilter.Refresh();
        if (index >= 0 && index < listBoxFilter.Items.Count)
        {
            listBoxFilter.SelectedIndex = index;
        }

        filterOnLoadCheckBox.Checked = Preferences.IsFilterOnLoad;
        hideFilterListOnLoadCheckBox.Checked = Preferences.IsAutoHideFilterList;
    }

    public void SetCurrentHighlightGroup (string groupName)
    {
        _guiStateArgs.HighlightGroupName = groupName;

        lock (_currentHighlightGroupLock)
        {
            _currentHighlightGroup = _logWindowCoordinator.ResolveHighlightGroup(groupName, null);
            _guiStateArgs.HighlightGroupName = _currentHighlightGroup.GroupName;
        }

        SendGuiStateUpdate();

        if (IsHandleCreated)
        {
            //NOTE: Possible double refresh of AllGrids, maybe not necessary if only will be called once
            _ = BeginInvoke(new MethodInvoker(RefreshAllGrids));
            _ = BeginInvoke(new MethodInvoker(RunHighlightBookmarkScan));
        }
    }

    public void SwitchMultiFile (bool enabled)
    {
        IsMultiFile = enabled;
        Reload();
    }

    public void AddOtherWindowToTimesync (LogWindow other)
    {
        if (other.IsTimeSynced)
        {
            if (IsTimeSynced)
            {
                other.FreeFromTimeSync();
                AddSlaveToTimesync(other);
            }
            else
            {
                AddToTimeSync(other);
            }
        }
        else
        {
            AddSlaveToTimesync(other);
        }
    }

    public void AddToTimeSync (LogWindow master)
    {
        //if (_logger.IsInfoEnabled)
        //{
        //    _logger.Info($"Syncing window for {Util.GetNameFromPath(FileName)} to {Util.GetNameFromPath(master.FileName)}");
        //}

        lock (_timeSyncListLock)
        {
            if (IsTimeSynced && master.TimeSyncList != TimeSyncList)
            // already synced but master has different sync list
            {
                FreeFromTimeSync();
            }

            TimeSyncList = master.TimeSyncList;
            TimeSyncList.AddWindow(this);
            _ = ScrollToTimestamp(TimeSyncList.CurrentTimestamp, false, false);
        }

        OnSyncModeChanged();
    }

    public void FreeFromTimeSync ()
    {
        lock (_timeSyncListLock)
        {
            if (TimeSyncList != null)
            {
                //if (_logger.IsInfoEnabled)
                //{
                //    _logger.Info($"De-Syncing window for {Util.GetNameFromPath(FileName)}");
                //}

                TimeSyncList.WindowRemoved -= OnTimeSyncListWindowRemoved;
                TimeSyncList.RemoveWindow(this);
                TimeSyncList = null;
            }
        }

        OnSyncModeChanged();
    }

    public void RefreshLogView ()
    {
        RefreshAllGrids();
    }

    #endregion
}
