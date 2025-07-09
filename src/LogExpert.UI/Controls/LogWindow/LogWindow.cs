using System.ComponentModel;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

using LogExpert.Core.Callback;
using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Bookmark;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Log;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArguments;
using LogExpert.Core.Interface;
using LogExpert.Dialogs;
using LogExpert.Entities;
using LogExpert.Extensions;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Entities;
using LogExpert.UI.Extensions;
using LogExpert.UI.Interface;

using NLog;

using WeifenLuo.WinFormsUI.Docking;
//using static LogExpert.PluginRegistry.PluginRegistry; //TODO: Adjust the instance name so using static can be used.

namespace LogExpert.UI.Controls.LogWindow;

//TODO: Implemented 4 interfaces explicitly. Find them by searching: ILogWindow.<method name>
[SupportedOSPlatform("windows")]
internal partial class LogWindow : DockContent, ILogPaintContextUI, ILogView, ILogWindow
{
    #region Fields

    private const int SPREAD_MAX = 99;
    private const int PROGRESS_BAR_MODULO = 1000;
    private const int FILTER_ADVANCED_SPLITTER_DISTANCE = 110;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly Image _advancedButtonImage;

    private readonly object _bookmarkLock = new();
    private readonly BookmarkDataProvider _bookmarkProvider = new();

    private readonly IList<IBackgroundProcessCancelHandler> _cancelHandlerList = [];

    private readonly object _currentColumnizerLock = new();

    private readonly object _currentHighlightGroupLock = new();

    private readonly EventWaitHandle _externaLoadingFinishedEvent = new ManualResetEvent(false);

    private readonly IList<FilterPipe> _filterPipeList = [];
    private readonly Dictionary<Control, bool> _freezeStateMap = [];
    private readonly GuiStateArgs _guiStateArgs = new();

    private readonly List<int> _lineHashList = [];

    private readonly EventWaitHandle _loadingFinishedEvent = new ManualResetEvent(false);

    private readonly EventWaitHandle _logEventArgsEvent = new ManualResetEvent(false);

    private readonly List<LogEventArgs> _logEventArgsList = [];
    private readonly Task _logEventHandlerTask;
    //private readonly Thread _logEventHandlerThread;
    private readonly Image _panelCloseButtonImage;

    private readonly Image _panelOpenButtonImage;
    private readonly LogTabWindow.LogTabWindow _parentLogTabWin;

    private readonly ProgressEventArgs _progressEventArgs = new();
    private readonly object _reloadLock = new();
    private readonly Image _searchButtonImage;
    private readonly StatusLineEventArgs _statusEventArgs = new();

    private readonly object _tempHighlightEntryListLock = new();

    private readonly Task _timeShiftSyncTask;
    private readonly CancellationTokenSource cts = new();

    //private readonly Thread _timeShiftSyncThread;
    private readonly EventWaitHandle _timeShiftSyncTimerEvent = new ManualResetEvent(false);
    private readonly EventWaitHandle _timeShiftSyncWakeupEvent = new ManualResetEvent(false);

    private readonly TimeSpreadCalculator _timeSpreadCalc;

    private readonly object _timeSyncListLock = new();

    private ColumnCache _columnCache = new();

    private ILogLineColumnizer _currentColumnizer;

    //List<HilightEntry> currentHilightEntryList = new List<HilightEntry>();
    private HighlightGroup _currentHighlightGroup = new();

    private SearchParams _currentSearchParams;

    private string[] _fileNames;
    private List<int> _filterHitList = [];
    private FilterParams _filterParams = new();
    private int _filterPipeNameCounter;
    private List<int> _filterResultList = [];

    private ILogLineColumnizer _forcedColumnizer;
    private ILogLineColumnizer _forcedColumnizerForLoading;
    private bool _isDeadFile;
    private bool _isErrorShowing;
    private bool _isLoadError;
    private bool _isLoading;
    private bool _isMultiFile;
    private bool _isSearching;
    private bool _isTimestampDisplaySyncing;
    private List<int> _lastFilterLinesList = [];

    private int _lineHeight;

    private LogfileReader _logFileReader;
    private MultiFileOptions _multiFileOptions = new();
    private bool _noSelectionUpdates;
    private PatternArgs _patternArgs = new();
    private PatternWindow _patternWindow;

    private ReloadMemento _reloadMemento;
    private int _reloadOverloadCounter;
    private SortedList<int, RowHeightEntry> _rowHeightList = [];
    private int _selectedCol; // set by context menu event for column headers only
    private bool _shouldCallTimeSync;
    private bool _shouldCancel;
    private bool _shouldTimestampDisplaySyncingCancel;
    private bool _showAdvanced;
    private List<HighlightEntry> _tempHighlightEntryList = [];
    private int _timeShiftSyncLine;

    private bool _waitingForClose;

    #endregion

    #region cTor

    [SupportedOSPlatform("windows")]
    public LogWindow (LogTabWindow.LogTabWindow parent, string fileName, bool isTempFile, bool forcePersistenceLoading, IConfigManager configManager)
    {
        SuspendLayout();

        //HighDPI Functionality must be called before all UI Elements are initialized, to make sure they work as intended
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();

        CreateDefaultViewStyle();

        columnNamesLabel.Text = string.Empty; // no filtering on columns by default

        _parentLogTabWin = parent;
        IsTempFile = isTempFile;
        ConfigManager = configManager; //TODO: This should be changed to DI
        //Thread.CurrentThread.Name = "LogWindowThread";
        ColumnizerCallbackObject = new ColumnizerCallback(this);

        FileName = fileName;
        ForcePersistenceLoading = forcePersistenceLoading;

        dataGridView.CellValueNeeded += OnDataGridViewCellValueNeeded;
        dataGridView.CellPainting += OnDataGridViewCellPainting;

        filterGridView.CellValueNeeded += OnFilterGridViewCellValueNeeded;
        filterGridView.CellPainting += OnFilterGridViewCellPainting;
        filterListBox.DrawMode = DrawMode.OwnerDrawVariable;
        filterListBox.MeasureItem += MeasureItem;

        Closing += OnLogWindowClosing;
        Disposed += OnLogWindowDisposed;
        Load += OnLogWindowLoad;

        _timeSpreadCalc = new TimeSpreadCalculator(this);
        timeSpreadingControl.TimeSpreadCalc = _timeSpreadCalc;
        timeSpreadingControl.LineSelected += OnTimeSpreadingControlLineSelected;
        tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Absolute;
        tableLayoutPanel1.ColumnStyles[1].Width = 20;
        tableLayoutPanel1.ColumnStyles[0].SizeType = SizeType.Percent;
        tableLayoutPanel1.ColumnStyles[0].Width = 100;

        _parentLogTabWin.HighlightSettingsChanged += OnParentHighlightSettingsChanged;
        SetColumnizer(PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers[0]);

        _patternArgs.MaxMisses = 5;
        _patternArgs.MinWeight = 1;
        _patternArgs.MaxDiffInBlock = 5;
        _patternArgs.Fuzzy = 5;

        //InitPatternWindow();

        //this.toolwinTabControl.TabPages.Add(this.patternWindow);
        //this.toolwinTabControl.TabPages.Add(this.bookmarkWindow);

        _filterParams = new FilterParams();
        foreach (var item in configManager.Settings.FilterHistoryList)
        {
            _ = filterComboBox.Items.Add(item);
        }

        filterComboBox.DropDownHeight = filterComboBox.ItemHeight * configManager.Settings.Preferences.MaximumFilterEntriesDisplayed;
        AutoResizeFilterBox();

        filterRegexCheckBox.Checked = _filterParams.IsRegex;
        filterCaseSensitiveCheckBox.Checked = _filterParams.IsCaseSensitive;
        filterTailCheckBox.Checked = _filterParams.IsFilterTail;

        splitContainerLogWindow.Panel2Collapsed = true;
        advancedFilterSplitContainer.SplitterDistance = FILTER_ADVANCED_SPLITTER_DISTANCE;

        _timeShiftSyncTask = new Task(SyncTimestampDisplayWorker, cts.Token);
        _timeShiftSyncTask.Start();

        _logEventHandlerTask = new Task(LogEventWorker, cts.Token);
        _logEventHandlerTask.Start();

        //this.filterUpdateThread = new Thread(new ThreadStart(this.FilterUpdateWorker));
        //this.filterUpdateThread.Start();

        _advancedButtonImage = advancedButton.Image;
        _searchButtonImage = filterSearchButton.Image;
        filterSearchButton.Image = null;

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
        filterKnobBackSpread.MinValue = 0;
        filterKnobBackSpread.MaxValue = SPREAD_MAX;
        filterKnobBackSpread.ValueChanged += OnFilterKnobControlValueChanged;
        filterKnobForeSpread.MinValue = 0;
        filterKnobForeSpread.MaxValue = SPREAD_MAX;
        filterKnobForeSpread.ValueChanged += OnFilterKnobControlValueChanged;
        fuzzyKnobControl.MinValue = 0;
        fuzzyKnobControl.MaxValue = 10;
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
    public delegate void FilterRestoreFx (LogWindow newWin, PersistenceData persistenceData);

    public delegate void RestoreFiltersFx (PersistenceData persistenceData);

    public delegate bool ScrollToTimestampFx (DateTime timestamp, bool roundToSeconds, bool triggerSyncCall);

    public delegate void TailFollowedEventHandler (object sender, EventArgs e);

    #endregion

    #region Events

    public event EventHandler<LogEventArgs> FileSizeChanged;

    public event EventHandler<ProgressEventArgs> ProgressBarUpdate;

    public event EventHandler<StatusLineEventArgs> StatusLineEvent;

    public event EventHandler<GuiStateArgs> GuiStateUpdate;

    public event TailFollowedEventHandler TailFollowed;

    public event EventHandler<EventArgs> FileNotFound;

    public event EventHandler<EventArgs> FileRespawned;

    public event EventHandler<FilterListChangedEventArgs> FilterListChanged;

    public event EventHandler<CurrentHighlightGroupChangedEventArgs> CurrentHighlightGroupChanged;

    public event EventHandler<EventArgs> BookmarkAdded;

    public event EventHandler<EventArgs> BookmarkRemoved;

    public event EventHandler<BookmarkEventArgs> BookmarkTextChanged;

    public event EventHandler<ColumnizerEventArgs> ColumnizerChanged;

    public event EventHandler<SyncModeEventArgs> SyncModeChanged;

    #endregion

    #region Properties

    public Color BookmarkColor { get; set; } = Color.FromArgb(165, 200, 225);

    public ILogLineColumnizer CurrentColumnizer
    {
        get => _currentColumnizer;
        private set
        {
            lock (_currentColumnizerLock)
            {
                _currentColumnizer = value;
                _logger.Debug($"Setting columnizer {_currentColumnizer.GetName()} ");
            }
        }
    }

    [SupportedOSPlatform("windows")]
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

    public string SessionFileName { get; set; }

    public bool IsMultiFile
    {
        get => _isMultiFile;
        private set => _guiStateArgs.IsMultiFileActive = _isMultiFile = value;
    }

    public bool IsTempFile { get; }

    private readonly IConfigManager ConfigManager;

    public string TempTitleName { get; set; } = "";

    internal FilterPipe FilterPipe { get; set; }

    public string Title => IsTempFile
                ? TempTitleName
                : FileName;

    public ColumnizerCallback ColumnizerCallbackObject { get; }

    public bool ForcePersistenceLoading { get; set; }

    public string ForcedPersistenceFileName { get; set; }

    public Preferences Preferences => _parentLogTabWin.Preferences;

    public string GivenFileName { get; set; }

    public TimeSyncList TimeSyncList { get; private set; }

    public bool IsTimeSynced => TimeSyncList != null;

    protected EncodingOptions EncodingOptions { get; set; }

    public IBookmarkData BookmarkData => _bookmarkProvider;

    public Font MonospacedFont { get; private set; }

    public Font NormalFont { get; private set; }

    public Font BoldFont { get; private set; }

    LogfileReader ILogWindow.LogFileReader => _logFileReader;

    //public event EventHandler<EventArgs> ILogWindow.FileSizeChanged
    //{
    //    add => FileSizeChanged += new EventHandler<LogEventArgs>(value);
    //    remove => FileSizeChanged -= new EventHandler<LogEventArgs>(value);
    //}

    //event EventHandler ILogWindow.TailFollowed
    //{
    //    add => TailFollowed += new TailFollowedEventHandler(value);
    //    remove => TailFollowed -= new TailFollowedEventHandler(value);
    //}

    #endregion

    #region Public methods

    public ILogLine GetLogLine (int lineNum)
    {
        return _logFileReader.GetLogLine(lineNum);
    }

    public ILogLine GetLogLineWithWait (int lineNum)
    {
        return _logFileReader.GetLogLineWithWait(lineNum).Result;
    }

    public Bookmark GetBookmarkForLine (int lineNum)
    {
        return _bookmarkProvider.GetBookmarkForLine(lineNum);
    }

    #endregion

    #region Internals

    internal IColumnizedLogLine GetColumnsForLine (int lineNumber)
    {
        return _columnCache.GetColumnsForLine(_logFileReader, lineNumber, CurrentColumnizer, ColumnizerCallbackObject);

        //string line = this.logFileReader.GetLogLine(lineNumber);
        //if (line != null)
        //{
        //  string[] cols;
        //  this.columnizerCallback.LineNum = lineNumber;
        //  cols = this.CurrentColumnizer.SplitLine(this.columnizerCallback, line);
        //  return cols;
        //}
        //else
        //{
        //  return null;
        //}
    }

    [SupportedOSPlatform("windows")]
    internal void RefreshAllGrids ()
    {
        dataGridView.Refresh();
        filterGridView.Refresh();
    }

    [SupportedOSPlatform("windows")]
    internal void ChangeMultifileMask ()
    {
        MultiFileMaskDialog dlg = new(this, FileName)
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
                columnComboBox.Focus();
            }
        }
        else
        {
            dataGridView.Focus();
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

    private Action<FilterParams, List<int>, List<int>, List<int>> FilterFxAction;
    //private delegate void FilterFx(FilterParams filterParams, List<int> filterResultLines, List<int> lastFilterResultLines, List<int> filterHitList);

    private delegate void UpdateProgressBarFx (int lineNum);

    private delegate void SetColumnizerFx (ILogLineColumnizer columnizer);

    private delegate void WriteFilterToTabFinishedFx (FilterPipe pipe, string namePrefix, PersistenceData persistenceData);

    private delegate void SetBookmarkFx (int lineNum, string comment);

    private delegate void FunctionWith1BoolParam (bool arg);

    private delegate void PatternStatisticFx (PatternArgs patternArgs);

    private delegate void ActionPluginExecuteFx (string keyword, string param, ILogExpertCallback callback, ILogLineColumnizer columnizer);

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
    void ILogWindow.SelectLine (int lineNum, bool triggerSyncCall, bool shouldScroll)
    {
        SelectLine(lineNum, triggerSyncCall, shouldScroll);
    }

    [SupportedOSPlatform("windows")]
    void ILogWindow.AddTempFileTab (string fileName, string title)
    {
        AddTempFileTab(fileName, title);
    }

    [SupportedOSPlatform("windows")]
    void ILogWindow.WritePipeTab (IList<LineEntry> lineEntryList, string title)
    {
        WritePipeTab(lineEntryList, title);
    }

    #region Event Handlers

    [SupportedOSPlatform("windows")]
    private void AutoResizeFilterBox ()
    {
        filterSplitContainer.SplitterDistance = filterComboBox.Left + filterComboBox.GetMaxTextWidth();
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

    protected void OnGuiState (GuiStateArgs e)
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

    protected void OnBookmarkAdded ()
    {
        BookmarkAdded?.Invoke(this, EventArgs.Empty);
    }

    protected void OnBookmarkRemoved ()
    {
        BookmarkRemoved?.Invoke(this, EventArgs.Empty);
    }

    protected void OnBookmarkTextChanged (Bookmark bookmark)
    {
        BookmarkTextChanged?.Invoke(this, new BookmarkEventArgs(bookmark));
    }

    protected void OnColumnizerChanged (ILogLineColumnizer columnizer)
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
            _cancelHandlerList.Remove(handler);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnLogWindowLoad (object sender, EventArgs e)
    {
        var setLastColumnWidth = _parentLogTabWin.Preferences.SetLastColumnWidth;
        var lastColumnWidth = _parentLogTabWin.Preferences.LastColumnWidth;
        var fontName = _parentLogTabWin.Preferences.FontName;
        var fontSize = _parentLogTabWin.Preferences.FontSize;

        PreferencesChanged(fontName, fontSize, setLastColumnWidth, lastColumnWidth, true, SettingsFlags.GuiOrColors);
    }

    [SupportedOSPlatform("windows")]
    private void OnLogWindowDisposed (object sender, EventArgs e)
    {
        _waitingForClose = true;
        _parentLogTabWin.HighlightSettingsChanged -= OnParentHighlightSettingsChanged;
        _logFileReader?.DeleteAllContent();

        FreeFromTimeSync();
    }

    [SupportedOSPlatform("windows")]
    private void OnLogFileReaderLoadingStarted (object sender, LoadFileEventArgs e)
    {
        Invoke(LoadingStarted, e);
    }

    [SupportedOSPlatform("windows")]
    private void OnLogFileReaderFinishedLoading (object sender, EventArgs e)
    {
        //Thread.CurrentThread.Name = "FinishedLoading event thread";
        _logger.Info(CultureInfo.InvariantCulture, "Finished loading.");
        _isLoading = false;
        _isDeadFile = false;
        if (!_waitingForClose)
        {
            Invoke(new MethodInvoker(LoadingFinished));
            Invoke(new MethodInvoker(LoadPersistenceData));
            Invoke(new MethodInvoker(SetGuiAfterLoading));
            _loadingFinishedEvent.Set();
            _externaLoadingFinishedEvent.Set();
            _timeSpreadCalc.SetLineCount(_logFileReader.LineCount);

            if (_reloadMemento != null)
            {
                Invoke(new PositionAfterReloadFx(PositionAfterReload), _reloadMemento);
            }

            if (filterTailCheckBox.Checked)
            {
                _logger.Info(CultureInfo.InvariantCulture, "Refreshing filter view because of reload.");
                Invoke(new MethodInvoker(FilterSearch)); // call on proper thread
            }

            HandleChangedFilterList();
        }

        _reloadMemento = null;
    }

    [SupportedOSPlatform("windows")]
    private void OnLogFileReaderFileNotFound (object sender, EventArgs e)
    {
        if (!IsDisposed && !Disposing)
        {
            _logger.Info(CultureInfo.InvariantCulture, "Handling file not found event.");
            _isDeadFile = true;
            BeginInvoke(new MethodInvoker(LogfileDead));
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnLogFileReaderRespawned (object sender, EventArgs e)
    {
        BeginInvoke(new MethodInvoker(LogfileRespawned));
    }

    [SupportedOSPlatform("windows")]
    private void OnLogWindowClosing (object sender, CancelEventArgs e)
    {
        if (Preferences.AskForClose)
        {
            if (MessageBox.Show("Sure to close?", "LogExpert", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }
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
            _logger.Info(CultureInfo.InvariantCulture, "File created anew.");

            // File was new created (e.g. rollover)
            _isDeadFile = false;
            UnRegisterLogFileReaderEvents();
            dataGridView.CurrentCellChanged -= OnDataGridViewCurrentCellChanged;
            MethodInvoker invoker = ReloadNewFile;
            BeginInvoke(invoker);
            //Thread loadThread = new Thread(new ThreadStart(ReloadNewFile));
            //loadThread.Start();
            _logger.Debug(CultureInfo.InvariantCulture, "Reloading invoked.");
        }
        else if (_isLoading)
        {
            BeginInvoke(UpdateProgress, e);
        }
    }

    private void OnFileSizeChanged (object sender, LogEventArgs e)
    {
        //OnFileSizeChanged(e);  // now done in UpdateGrid()
        _logger.Info(CultureInfo.InvariantCulture, "Got FileSizeChanged event. prevLines:{0}, curr lines: {1}", e.PrevLineCount, e.LineCount);

        // - now done in the thread that works on the event args list
        //if (e.IsRollover)
        //{
        //  ShiftBookmarks(e.RolloverOffset);
        //  ShiftFilterPipes(e.RolloverOffset);
        //}

        //UpdateGridCallback callback = new UpdateGridCallback(UpdateGrid);
        //this.BeginInvoke(callback, new object[] { e });
        lock (_logEventArgsList)
        {
            _logEventArgsList.Add(e);
            _ = _logEventArgsEvent.Set();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewCellValueNeeded (object sender, DataGridViewCellValueEventArgs e)
    {
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

    [SupportedOSPlatform("windows")]
    private void OnDataGridViewCellValuePushed (object sender, DataGridViewCellValueEventArgs e)
    {
        if (!CurrentColumnizer.IsTimeshiftImplemented())
        {
            return;
        }

        var line = _logFileReader.GetLogLine(e.RowIndex);
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
        //string oldValue = (string) this.dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
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

            //MethodInvoker invoker = new MethodInvoker(DisplayCurrentFileOnStatusline);
            //invoker.BeginInvoke(null, null);
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
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _filterResultList.Count <= e.RowIndex)
        {
            e.Value = string.Empty;
            return;
        }

        var lineNum = _filterResultList[e.RowIndex];
        e.Value = GetCellValue(lineNum, e.ColumnIndex);
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
        BeginInvoke(fx, filterGridView);
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
            _logger.Debug(CultureInfo.InvariantCulture, "Selection changed trigger");
            selCount = dataGridView.SelectedRows.Count;
            if (selCount > 1)
            {
                StatusLineText(selCount + " selected lines");
            }
            else
            {
                if (IsMultiFile)
                {
                    MethodInvoker invoker = DisplayCurrentFileOnStatusline;
                    invoker.BeginInvoke(null, null);
                }
                else
                {
                    StatusLineText("");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in selectionChangedTrigger_Signal selcount {0}", selCount);
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
                _filterPipeList.Remove((FilterPipe)sender);
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
                if (splitContainer.Orientation.Equals(Orientation.Vertical))
                {
                    if (e.X > 0 && e.X < splitContainer.Width)
                    {
                        splitContainer.SplitterDistance = e.X;
                        splitContainer.Refresh();
                    }
                }
                else
                {
                    if (e.Y > 0 && e.Y < splitContainer.Height)
                    {
                        splitContainer.SplitterDistance = e.Y;
                        splitContainer.Refresh();
                    }
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

        var refLineNum = lineNum;

        copyToTabToolStripMenuItem.Enabled = dataGridView.SelectedCells.Count > 0;
        scrollAllTabsToTimestampToolStripMenuItem.Enabled = CurrentColumnizer.IsTimeshiftImplemented()
                                                            &&
                                                            GetTimestampForLine(ref refLineNum, false) !=
                                                            DateTime.MinValue;

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
        if (PluginRegistry.PluginRegistry.Instance.RegisteredContextMenuPlugins.Count > 0)
        {
            var lines = GetSelectedContent();
            foreach (var entry in PluginRegistry.PluginRegistry.Instance.RegisteredContextMenuPlugins)
            {
                LogExpertCallback callback = new(this);
                var menuText = entry.GetMenuText(lines.Count, CurrentColumnizer, callback.GetLogLine(lines[0]));

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

        markCurrentFilterRangeToolStripMenuItem.Enabled = string.IsNullOrEmpty(filterRangeComboBox.Text) == false;

        if (CurrentColumnizer.IsTimeshiftImplemented())
        {
            var list = _parentLogTabWin.GetListOfOpenFiles();
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
            menuArgs.Entry.MenuSelected(logLines.Count, menuArgs.Columnizer, menuArgs.Callback.GetLogLine(logLines[0]));
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
            }
        }
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
                var lineNum = currentLine;
                var timeStamp = GetTimestampForLine(ref lineNum, false);
                if (timeStamp.Equals(DateTime.MinValue)) // means: invalid
                {
                    return;
                }

                _parentLogTabWin.ScrollAllTabsToTimestamp(timeStamp, this);
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
                FilterPipe.LogWindow.SelectLine(lineNum, false, true);
                _parentLogTabWin.SelectTab(FilterPipe.LogWindow);
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
        columnButton.Enabled = columnRestrictCheckBox.Checked;
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
        _filterParams.CurrentColumnizer = _currentColumnizer;
        FilterColumnChooser chooser = new(_filterParams);
        if (chooser.ShowDialog() == DialogResult.OK)
        {
            columnNamesLabel.Text = CalculateColumnNames(_filterParams);

            //CheckForFilterDirty(); //!!!GBro: Indicate to redo the search if search columns were changed
            filterSearchButton.Image = _searchButtonImage;
            saveFilterButton.Enabled = false;
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
            freezeLeftColumnsUntilHereToolStripMenuItem.Text = "Frozen";
        }
        else
        {
            if (ctl is BufferedDataGridView)
            {
                freezeLeftColumnsUntilHereToolStripMenuItem.Text = $"Freeze left columns until here ({gridView.Columns[_selectedCol].HeaderText})";
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
        if (col != null)
        {
            col.DisplayIndex = gridView.Columns.Count - 1;
        }
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
            col.DisplayIndex = col.DisplayIndex + 1;
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
                IsRegEx = false,
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

            dataGridView.CancelEdit();
            dataGridView.EndEdit();
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
                IsRegEx = false,
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

            dataGridView.CancelEdit();
            dataGridView.EndEdit();
            RefreshAllGrids();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnEditModeCopyToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (dataGridView.EditingControl is DataGridViewTextBoxEditingControl ctl)
        {
            if (Util.IsNull(ctl.SelectedText) == false)
            {
                Clipboard.SetText(ctl.SelectedText);
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
        var index = filterListBox.SelectedIndex;
        if (index >= 0)
        {
            var filterParams = (FilterParams)filterListBox.Items[index];
            ConfigManager.Settings.FilterList.Remove(filterParams);
            OnFilterListChanged(this);
            if (filterListBox.Items.Count > 0)
            {
                filterListBox.SelectedIndex = filterListBox.Items.Count - 1;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterUpButtonClick (object sender, EventArgs e)
    {
        var i = filterListBox.SelectedIndex;
        if (i > 0)
        {
            var filterParams = (FilterParams)filterListBox.Items[i];
            ConfigManager.Settings.FilterList.RemoveAt(i);
            i--;
            ConfigManager.Settings.FilterList.Insert(i, filterParams);
            OnFilterListChanged(this);
            filterListBox.SelectedIndex = i;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterDownButtonClick (object sender, EventArgs e)
    {
        var i = filterListBox.SelectedIndex;
        if (i < 0)
        {
            return;
        }

        if (i < filterListBox.Items.Count - 1)
        {
            var filterParams = (FilterParams)filterListBox.Items[i];
            ConfigManager.Settings.FilterList.RemoveAt(i);
            i++;
            ConfigManager.Settings.FilterList.Insert(i, filterParams);
            OnFilterListChanged(this);
            filterListBox.SelectedIndex = i;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnFilterListBoxMouseDoubleClick (object sender, MouseEventArgs e)
    {
        if (filterListBox.SelectedIndex >= 0)
        {
            var filterParams = (FilterParams)filterListBox.Items[filterListBox.SelectedIndex];
            var newParams = filterParams.Clone();
            //newParams.historyList = ConfigManager.Settings.filterHistoryList;
            _filterParams = newParams;
            ReInitFilterParams(_filterParams);
            ApplyFilterParams();
            CheckForAdvancedButtonDirty();
            CheckForFilterDirty();
            filterSearchButton.Image = _searchButtonImage;
            saveFilterButton.Enabled = false;
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
            var filterParams = (FilterParams)filterListBox.Items[e.Index];
            Rectangle rectangle = new(0, e.Bounds.Top, e.Bounds.Width, e.Bounds.Height);

            using var brush = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? new SolidBrush(filterListBox.BackColor)
                : new SolidBrush(filterParams.Color);

            e.Graphics.DrawString(filterParams.SearchText, e.Font, brush, new PointF(rectangle.Left, rectangle.Top));
            e.DrawFocusRectangle();
        }
    }

    [SupportedOSPlatform("windows")]
    // Color for filter list entry
    private void OnColorToolStripMenuItemClick (object sender, EventArgs e)
    {
        var i = filterListBox.SelectedIndex;
        if (i < filterListBox.Items.Count && i >= 0)
        {
            var filterParams = (FilterParams)filterListBox.Items[i];
            ColorDialog dlg = new()
            {
                CustomColors = [filterParams.Color.ToArgb()],
                Color = filterParams.Color
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                filterParams.Color = dlg.Color;
                filterListBox.Refresh();
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
        fuzzyKnobControl.Enabled = !filterRegexCheckBox.Checked;
        fuzzyLabel.Enabled = !filterRegexCheckBox.Checked;
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
        e.ItemHeight = filterListBox.Font.Height;
    }

    #endregion

    #region Private Methods

    [SupportedOSPlatform("windows")]
    private void CreateDefaultViewStyle ()
    {
        dataGridView.DefaultCellStyle = PaintHelper.GetDataGridViewCellStyle();
        filterGridView.DefaultCellStyle = PaintHelper.GetDataGridViewCellStyle();
        dataGridView.RowsDefaultCellStyle = PaintHelper.GetDataGridDefaultRowStyle();
        filterGridView.RowsDefaultCellStyle = PaintHelper.GetDataGridDefaultRowStyle();
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

    [SupportedOSPlatform("windows")]
    private bool LoadPersistenceOptions ()
    {
        if (InvokeRequired)
        {
            return (bool)Invoke(new BoolReturnDelegate(LoadPersistenceOptions));
        }

        if (!Preferences.SaveSessions && ForcedPersistenceFileName == null)
        {
            return false;
        }

        try
        {
            var persistenceData = ForcedPersistenceFileName == null
                ? Persister.LoadPersistenceDataOptionsOnly(FileName, Preferences)
                : Persister.LoadPersistenceDataOptionsOnlyFromFixedFile(ForcedPersistenceFileName);

            if (persistenceData == null)
            {
                _logger.Info($"No persistence data for {FileName} found.");
                return false;
            }

            IsMultiFile = persistenceData.MultiFile;
            _multiFileOptions = new MultiFileOptions
            {
                FormatPattern = persistenceData.MultiFilePattern,
                MaxDayTry = persistenceData.MultiFileMaxDays
            };

            if (string.IsNullOrEmpty(_multiFileOptions.FormatPattern))
            {
                _multiFileOptions = ObjectClone.Clone(Preferences.MultiFileOptions);
            }

            splitContainerLogWindow.SplitterDistance = persistenceData.FilterPosition;
            splitContainerLogWindow.Panel2Collapsed = !persistenceData.FilterVisible;
            ToggleHighlightPanel(persistenceData.FilterSaveListVisible);
            ShowAdvancedFilterPanel(persistenceData.FilterAdvanced);

            if (_reloadMemento == null)
            {
                PreselectColumnizer(persistenceData.ColumnizerName);
            }

            FollowTailChanged(persistenceData.FollowTail, false);
            if (persistenceData.TabName != null)
            {
                Text = persistenceData.TabName;
            }

            AdjustHighlightSplitterWidth();
            SetCurrentHighlightGroup(persistenceData.HighlightGroupName);

            if (persistenceData.MultiFileNames.Count > 0)
            {
                _logger.Info(CultureInfo.InvariantCulture, "Detected MultiFile name list in persistence options");
                _fileNames = new string[persistenceData.MultiFileNames.Count];
                persistenceData.MultiFileNames.CopyTo(_fileNames);
            }
            else
            {
                _fileNames = null;
            }

            //this.bookmarkWindow.ShowBookmarkCommentColumn = persistenceData.showBookmarkCommentColumn;
            SetExplicitEncoding(persistenceData.Encoding);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading persistence data: ");
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

    [SupportedOSPlatform("windows")]
    private void LoadPersistenceData ()
    {
        if (InvokeRequired)
        {
            Invoke(new MethodInvoker(LoadPersistenceData));
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
            var persistenceData = ForcedPersistenceFileName == null
                ? Persister.LoadPersistenceData(FileName, Preferences)
                : Persister.LoadPersistenceDataFromFixedFile(ForcedPersistenceFileName);

            if (persistenceData.LineCount > _logFileReader.LineCount)
            {
                // outdated persistence data (logfile rollover)
                // MessageBox.Show(this, "Persistence data for " + this.FileName + " is outdated. It was discarded.", "Log Expert");
                _logger.Info($"Persistence data for {FileName} is outdated. It was discarded.");
                _ = LoadPersistenceOptions();
                return;
            }

            _bookmarkProvider.SetBookmarks(persistenceData.BookmarkList);
            _rowHeightList = persistenceData.RowHeightList;
            try
            {
                if (persistenceData.CurrentLine >= 0 && persistenceData.CurrentLine < dataGridView.RowCount)
                {
                    SelectLine(persistenceData.CurrentLine, false, true);
                }
                else
                {
                    if (_logFileReader.LineCount > 0)
                    {
                        dataGridView.FirstDisplayedScrollingRowIndex = _logFileReader.LineCount - 1;
                        SelectLine(_logFileReader.LineCount - 1, false, true);
                    }
                }

                if (persistenceData.FirstDisplayedLine >= 0 &&
                    persistenceData.FirstDisplayedLine < dataGridView.RowCount)
                {
                    dataGridView.FirstDisplayedScrollingRowIndex = persistenceData.FirstDisplayedLine;
                }

                if (persistenceData.FollowTail)
                {
                    FollowTailChanged(persistenceData.FollowTail, false);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // FirstDisplayedScrollingRowIndex calculates sometimes the wrong scrolling ranges???
            }

            if (Preferences.SaveFilters)
            {
                RestoreFilters(persistenceData);
            }
        }
        catch (IOException ex)
        {
            SetDefaultsFromPrefs();
            _logger.Error(ex, "Error loading bookmarks: ");
        }
    }

    [SupportedOSPlatform("windows")]
    private void RestoreFilters (PersistenceData persistenceData)
    {
        if (persistenceData.FilterParamsList.Count > 0)
        {
            _filterParams = persistenceData.FilterParamsList[0];
            ReInitFilterParams(_filterParams);
        }

        ApplyFilterParams(); // re-loaded filter settingss
        BeginInvoke(new MethodInvoker(FilterSearch));
        try
        {
            splitContainerLogWindow.SplitterDistance = persistenceData.FilterPosition;
            splitContainerLogWindow.Panel2Collapsed = !persistenceData.FilterVisible;
        }
        catch (InvalidOperationException e)
        {
            _logger.Error(e, "Error setting splitter distance: ");
        }

        ShowAdvancedFilterPanel(persistenceData.FilterAdvanced);
        if (_filterPipeList.Count == 0) // don't restore if it's only a reload
        {
            RestoreFilterTabs(persistenceData);
        }
    }

    private void RestoreFilterTabs (PersistenceData persistenceData)
    {
        foreach (var data in persistenceData.FilterTabDataList)
        {
            var persistFilterParams = data.FilterParams;
            ReInitFilterParams(persistFilterParams);
            List<int> filterResultList = [];
            //List<int> lastFilterResultList = new List<int>();
            List<int> filterHitList = [];
            Filter(persistFilterParams, filterResultList, _lastFilterLinesList, filterHitList);
            FilterPipe pipe = new(persistFilterParams.Clone(), this);
            WritePipeToTab(pipe, filterResultList, data.PersistenceData.TabName, data.PersistenceData);
        }
    }

    private void ReInitFilterParams (FilterParams filterParams)
    {
        filterParams.SearchText = filterParams.SearchText; // init "lowerSearchText"
        filterParams.RangeSearchText = filterParams.RangeSearchText; // init "lowerRangesearchText"
        filterParams.CurrentColumnizer = CurrentColumnizer;
        if (filterParams.IsRegex)
        {
            try
            {
                filterParams.CreateRegex();
            }
            catch (ArgumentException)
            {
                StatusLineError("Invalid regular expression");
            }
        }
    }

    private void EnterLoadFileStatus ()
    {
        _logger.Debug(CultureInfo.InvariantCulture, "EnterLoadFileStatus begin");

        if (InvokeRequired)
        {
            Invoke(new MethodInvoker(EnterLoadFileStatus));
            return;
        }

        _statusEventArgs.StatusText = "Loading file...";
        _statusEventArgs.LineCount = 0;
        _statusEventArgs.FileSize = 0;
        SendStatusLineUpdate();

        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = 0;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        _isLoading = true;
        _shouldCancel = true;
        ClearFilterList();
        ClearBookmarkList();
        dataGridView.ClearSelection();
        dataGridView.RowCount = 0;
        _logger.Debug(CultureInfo.InvariantCulture, "EnterLoadFileStatus end");
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
        _logger.Info(CultureInfo.InvariantCulture, "File not found.");
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
        _shouldCancel = true;
        ClearFilterList();
        ClearBookmarkList();

        StatusLineText("File not found");
        OnFileNotFound(EventArgs.Empty);
    }

    [SupportedOSPlatform("windows")]
    private void LogfileRespawned ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "LogfileDead(): Reloading file because it has been respawned.");
        _isDeadFile = false;
        dataGridView.Enabled = true;
        StatusLineText("");
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
            ILogLineColumnizer columnizer;
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
                        //TODO this needs to be refactored
                        var directory = ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir;

                        columnizer = ColumnizerPicker.CloneColumnizer(columnizer, directory);
                    }
                }
                else
                {
                    //TODO this needs to be refactored
                    var directory = ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir;

                    // Default Columnizers
                    columnizer = ColumnizerPicker.CloneColumnizer(ColumnizerPicker.FindColumnizer(FileName, _logFileReader, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers), directory);
                }
            }

            Invoke(new SetColumnizerFx(SetColumnizer), columnizer);
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

    private ILogLineColumnizer FindColumnizer ()
    {
        var columnizer = Preferences.MaskPrio
            ? _parentLogTabWin.FindColumnizerByFileMask(Util.GetNameFromPath(FileName)) ?? _parentLogTabWin.GetColumnizerHistoryEntry(FileName)
            : _parentLogTabWin.GetColumnizerHistoryEntry(FileName) ?? _parentLogTabWin.FindColumnizerByFileMask(Util.GetNameFromPath(FileName));

        return columnizer;
    }

    private void ReloadNewFile ()
    {
        // prevent "overloads". May occur on very fast rollovers (next rollover before the file is reloaded)
        lock (_reloadLock)
        {
            _reloadOverloadCounter++;
            _logger.Info($"ReloadNewFile(): counter = {_reloadOverloadCounter}");
            if (_reloadOverloadCounter <= 1)
            {
                SavePersistenceData(false);
                _loadingFinishedEvent.Reset();
                _externaLoadingFinishedEvent.Reset();
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
                //  _logger.logDebug("Waiting for loading to be complete.");
                //  loadingFinishedEvent.WaitOne();
                //  _logger.logDebug("Refreshing filter view because of reload.");
                //  FilterSearch();
                //}
                //LoadFilterPipes();
            }
            else
            {
                _logger.Debug(CultureInfo.InvariantCulture, "Preventing reload because of recursive calls.");
            }

            _reloadOverloadCounter--;
        }
    }

    [SupportedOSPlatform("windows")]
    private void ReloadFinishedThreadFx ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "Waiting for loading to be complete.");
        _loadingFinishedEvent.WaitOne();
        _logger.Info(CultureInfo.InvariantCulture, "Refreshing filter view because of reload.");
        Invoke(new MethodInvoker(FilterSearch));
        LoadFilterPipes();
    }

    private void UpdateProgress (LoadFileEventArgs e)
    {
        try
        {
            if (e.ReadPos >= e.FileSize)
            {
                //_logger.Warn(CultureInfo.InvariantCulture, "UpdateProgress(): ReadPos (" + e.ReadPos + ") is greater than file size (" + e.FileSize + "). Aborting Update");
                return;
            }

            _statusEventArgs.FileSize = e.ReadPos;
            //this.progressEventArgs.Visible = true;
            _progressEventArgs.MaxValue = (int)e.FileSize;
            _progressEventArgs.Value = (int)e.ReadPos;
            SendProgressBarUpdate();
            SendStatusLineUpdate();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "UpdateProgress(): ");
        }
    }

    private void LoadingStarted (LoadFileEventArgs e)
    {
        try
        {
            _statusEventArgs.FileSize = e.ReadPos;
            _statusEventArgs.StatusText = "Loading " + Util.GetNameFromPath(e.FileName);
            _progressEventArgs.Visible = true;
            _progressEventArgs.MaxValue = (int)e.FileSize;
            _progressEventArgs.Value = (int)e.ReadPos;
            SendProgressBarUpdate();
            SendStatusLineUpdate();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "LoadingStarted(): ");
        }
    }

    private void LoadingFinished ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "File loading complete.");

        StatusLineText("");
        _logFileReader.FileSizeChanged += OnFileSizeChanged;
        _isLoading = false;
        _shouldCancel = false;
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

        var setLastColumnWidth = _parentLogTabWin.Preferences.SetLastColumnWidth;
        var lastColumnWidth = _parentLogTabWin.Preferences.LastColumnWidth;
        var fontName = _parentLogTabWin.Preferences.FontName;
        var fontSize = _parentLogTabWin.Preferences.FontSize;

        PreferencesChanged(fontName, fontSize, setLastColumnWidth, lastColumnWidth, true, SettingsFlags.All);
        //LoadPersistenceData();
    }

    private void LogEventWorker ()
    {
        Thread.CurrentThread.Name = "LogEventWorker";
        while (true)
        {
            _logger.Debug(CultureInfo.InvariantCulture, "Waiting for signal");
            _logEventArgsEvent.WaitOne();
            _logger.Debug(CultureInfo.InvariantCulture, "Wakeup signal received.");
            while (true)
            {
                LogEventArgs e;
                var lastLineCount = 0;
                lock (_logEventArgsList)
                {
                    _logger.Info(CultureInfo.InvariantCulture, "{0} events in queue", _logEventArgsList.Count);
                    if (_logEventArgsList.Count == 0)
                    {
                        _logEventArgsEvent.Reset();
                        break;
                    }

                    e = _logEventArgsList[0];
                    _logEventArgsList.RemoveAt(0);
                }

                if (e.IsRollover)
                {
                    ShiftBookmarks(e.RolloverOffset);
                    ShiftRowHeightList(e.RolloverOffset);
                    ShiftFilterPipes(e.RolloverOffset);
                    lastLineCount = 0;
                }
                else
                {
                    if (e.LineCount < lastLineCount)
                    {
                        _logger.Error("Line count of event is: {0}, should be greater than last line count: {1}", e.LineCount, lastLineCount);
                    }
                }

                Invoke(UpdateGrid, [e]);
                CheckFilterAndHighlight(e);
                _timeSpreadCalc.SetLineCount(e.LineCount);
            }
        }
    }

    private void StopLogEventWorkerThread ()
    {
        _logEventArgsEvent.Set();
        cts.Cancel();
        //_logEventHandlerThread.Abort();
        //_logEventHandlerThread.Join();
    }

    private void OnFileSizeChanged (LogEventArgs e)
    {
        FileSizeChanged?.Invoke(this, e);
    }

    private void UpdateGrid (LogEventArgs e)
    {
        var oldRowCount = dataGridView.RowCount;
        var firstDisplayedLine = dataGridView.FirstDisplayedScrollingRowIndex;

        if (dataGridView.CurrentCellAddress.Y >= e.LineCount)
        {
            //this.dataGridView.Rows[this.dataGridView.CurrentCellAddress.Y].Selected = false;
            //this.dataGridView.CurrentCell = this.dataGridView.Rows[0].Cells[0];
        }

        try
        {
            if (dataGridView.RowCount > e.LineCount)
            {
                var currentLineNum = dataGridView.CurrentCellAddress.Y;
                dataGridView.RowCount = 0;
                dataGridView.RowCount = e.LineCount;
                if (_guiStateArgs.FollowTail == false)
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
                dataGridView.RowCount = e.LineCount;
            }

            _logger.Debug(CultureInfo.InvariantCulture, "UpdateGrid(): new RowCount={0}", dataGridView.RowCount);

            if (e.IsRollover)
            {
                // Multifile rollover
                // keep selection and view range, if no follow tail mode
                if (!_guiStateArgs.FollowTail)
                {
                    var currentLineNum = dataGridView.CurrentCellAddress.Y;
                    currentLineNum -= e.RolloverOffset;
                    if (currentLineNum < 0)
                    {
                        currentLineNum = 0;
                    }

                    _logger.Debug(CultureInfo.InvariantCulture, "UpdateGrid(): Rollover=true, Rollover offset={0}, currLineNum was {1}, new currLineNum={2}", e.RolloverOffset, dataGridView.CurrentCellAddress.Y, currentLineNum);
                    firstDisplayedLine -= e.RolloverOffset;
                    if (firstDisplayedLine < 0)
                    {
                        firstDisplayedLine = 0;
                    }

                    dataGridView.FirstDisplayedScrollingRowIndex = firstDisplayedLine;
                    dataGridView.CurrentCell = dataGridView.Rows[currentLineNum].Cells[0];
                    dataGridView.Rows[currentLineNum].Selected = true;
                }
            }

            _statusEventArgs.LineCount = e.LineCount;
            StatusLineFileSize(e.FileSize);

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
                dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.RowCount - 1;
                OnTailFollowed(EventArgs.Empty);
            }

            if (Preferences.TimestampControl && !_isLoading)
            {
                SetTimestampLimits();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Fehler bei UpdateGrid(): ");
        }

        //this.dataGridView.Refresh();
        //this.dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
    }

    private void CheckFilterAndHighlight (LogEventArgs e)
    {
        var noLed = true;
        bool suppressLed;
        bool setBookmark;
        bool stopTail;
        string bookmarkComment;

        if (filterTailCheckBox.Checked || _filterPipeList.Count > 0)
        {
            var filterStart = e.PrevLineCount;
            if (e.IsRollover)
            {
                ShiftFilterLines(e.RolloverOffset);
                filterStart -= e.RolloverOffset;
            }

            var firstStopTail = true;
            ColumnizerCallback callback = new(this);
            var filterLineAdded = false;
            for (var i = filterStart; i < e.LineCount; ++i)
            {
                var line = _logFileReader.GetLogLine(i);
                if (line == null)
                {
                    return;
                }

                if (filterTailCheckBox.Checked)
                {
                    callback.SetLineNum(i);
                    if (Util.TestFilterCondition(_filterParams, line, callback))
                    {
                        //AddFilterLineFx addFx = new AddFilterLineFx(AddFilterLine);
                        //this.Invoke(addFx, new object[] { i, true });
                        filterLineAdded = true;
                        AddFilterLine(i, false, _filterParams, _filterResultList, _lastFilterLinesList,
                            _filterHitList);
                    }
                }

                //ProcessFilterPipeFx pipeFx = new ProcessFilterPipeFx(ProcessFilterPipes);
                //pipeFx.BeginInvoke(i, null, null);
                ProcessFilterPipes(i);

                var matchingList = FindMatchingHilightEntries(line);
                LaunchHighlightPlugins(matchingList, i);
                GetHighlightActions(matchingList, out suppressLed, out stopTail, out setBookmark, out bookmarkComment);
                if (setBookmark)
                {
                    SetBookmarkFx fx = SetBookmarkFromTrigger;
                    fx.BeginInvoke(i, bookmarkComment, null, null);
                }

                if (stopTail && _guiStateArgs.FollowTail)
                {
                    var wasFollow = _guiStateArgs.FollowTail;
                    FollowTailChanged(false, true);
                    if (firstStopTail && wasFollow)
                    {
                        Invoke(new SelectLineFx(SelectAndEnsureVisible), [i, false]);
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
                //AddFilterLineGuiUpdateFx addFx = new AddFilterLineGuiUpdateFx(AddFilterLineGuiUpdate);
                //this.Invoke(addFx);
                TriggerFilterLineGuiUpdate();
            }
        }
        else
        {
            var firstStopTail = true;
            var startLine = e.PrevLineCount;
            if (e.IsRollover)
            {
                ShiftFilterLines(e.RolloverOffset);
                startLine -= e.RolloverOffset;
            }

            for (var i = startLine; i < e.LineCount; ++i)
            {
                var line = _logFileReader.GetLogLine(i);
                if (line != null)
                {
                    var matchingList = FindMatchingHilightEntries(line);
                    LaunchHighlightPlugins(matchingList, i);
                    GetHighlightActions(matchingList, out suppressLed, out stopTail, out setBookmark,
                        out bookmarkComment);
                    if (setBookmark)
                    {
                        SetBookmarkFx fx = SetBookmarkFromTrigger;
                        fx.BeginInvoke(i, bookmarkComment, null, null);
                    }

                    if (stopTail && _guiStateArgs.FollowTail)
                    {
                        var wasFollow = _guiStateArgs.FollowTail;
                        FollowTailChanged(false, true);
                        if (firstStopTail && wasFollow)
                        {
                            Invoke(new SelectLineFx(SelectAndEnsureVisible), [i, false]);
                            firstStopTail = false;
                        }
                    }

                    if (!suppressLed)
                    {
                        noLed = false;
                    }
                }
            }
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
                var plugin = PluginRegistry.PluginRegistry.Instance.FindKeywordActionPluginByName(entry.ActionEntry.PluginName);
                if (plugin != null)
                {
                    ActionPluginExecuteFx fx = plugin.Execute;
                    fx.BeginInvoke(entry.SearchText, entry.ActionEntry.ActionParam, callback, CurrentColumnizer, null, null);
                }
            }
        }
    }

    private void PreSelectColumnizer (ILogLineColumnizer columnizer)
    {
        CurrentColumnizer = columnizer != null
            ? (_forcedColumnizerForLoading = columnizer)
            : (_forcedColumnizerForLoading = ColumnizerPicker.FindColumnizer(FileName, _logFileReader, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers));
    }

    private void SetColumnizer (ILogLineColumnizer columnizer)
    {
        columnizer = ColumnizerPicker.FindReplacementForAutoColumnizer(FileName, _logFileReader, columnizer, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

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

    private void SetColumnizerInternal (ILogLineColumnizer columnizer)
    {
        _logger.Info(CultureInfo.InvariantCulture, "SetColumnizerInternal(): {0}", columnizer.GetName());

        var oldColumnizer = CurrentColumnizer;
        var oldColumnizerIsXmlType = CurrentColumnizer is ILogLineXmlColumnizer;
        var oldColumnizerIsPreProcess = CurrentColumnizer is IPreProcessColumnizer;
        var mustReload = false;

        // Check if the filtered columns disappeared, if so must refresh the UI
        if (_filterParams.ColumnRestrict)
        {
            var newColumns = columnizer != null ? columnizer.GetColumnNames() : Array.Empty<string>();
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

            if (_logFileReader != null)
            {
                _logFileReader.PreProcessColumnizer = CurrentColumnizer is IPreProcessColumnizer columnizer1
                    ? columnizer1
                    : null;
            }

            // always reload when choosing XML columnizers
            if (_logFileReader != null && CurrentColumnizer is ILogLineXmlColumnizer)
            {
                //forcedColumnizer = currentColumnizer; // prevent Columnizer selection on SetGuiAfterReload()
                mustReload = true;
            }

            // Reload when choosing no XML columnizer but previous columnizer was XML
            if (_logFileReader != null && !(CurrentColumnizer is ILogLineXmlColumnizer) && oldColumnizerIsXmlType)
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

        (oldColumnizer as IInitColumnizer)?.DeSelected(new ColumnizerCallback(this));

        (columnizer as IInitColumnizer)?.Selected(new ColumnizerCallback(this));

        SetColumnizer(columnizer, dataGridView);
        SetColumnizer(columnizer, filterGridView);
        _patternWindow?.SetColumnizer(columnizer);

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
            columnComboBox.Items.Add(columnName);
        }

        columnComboBox.SelectedIndex = 0;

        OnColumnizerChanged(CurrentColumnizer);
    }

    private void AutoResizeColumns (BufferedDataGridView gridView)
    {
        try
        {
            gridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
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
            _logger.Error(e, "Error while resizing columns: ");
        }
    }

    private void PaintCell (DataGridViewCellPaintingEventArgs e, HighlightEntry groundEntry)
    {
        PaintHighlightedCell(e, groundEntry);
    }

    private void PaintHighlightedCell (DataGridViewCellPaintingEventArgs e, HighlightEntry groundEntry)
    {
        var column = e.Value as IColumn;

        column ??= Column.EmptyColumn;

        var matchList = FindHighlightMatches(column);
        // too many entries per line seem to cause problems with the GDI
        while (matchList.Count > 50)
        {
            matchList.RemoveAt(50);
        }

        var he = new HighlightEntry
        {
            SearchText = column.DisplayValue,
            ForegroundColor = groundEntry?.ForegroundColor ?? Color.FromKnownColor(KnownColor.Black),
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

        matchList = MergeHighlightMatchEntries(matchList, hme);

        //var leftPad = e.CellStyle.Padding.Left;
        //RectangleF rect = new(e.CellBounds.Left + leftPad, e.CellBounds.Top, e.CellBounds.Width, e.CellBounds.Height);

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

        //          | TextFormatFlags.VerticalCenter
        //          | TextFormatFlags.TextBoxControl
        //          TextFormatFlags.SingleLine

        //TextRenderer.DrawText(e.Graphics, e.Value as String, e.CellStyle.Font, valBounds, Color.FromKnownColor(KnownColor.Black), flags);

        var wordPos = valBounds.Location;
        Size proposedSize = new(valBounds.Width, valBounds.Height);

        e.Graphics.SetClip(e.CellBounds);

        foreach (var matchEntry in matchList)
        {
            var font = matchEntry != null && matchEntry.HighlightEntry.IsBold ? BoldFont : NormalFont;

            using var bgBrush = matchEntry.HighlightEntry.BackgroundColor != Color.Empty
                ? new SolidBrush(matchEntry.HighlightEntry.BackgroundColor)
                : null;

            var matchWord = column.DisplayValue.Substring(matchEntry.StartPos, matchEntry.Length);
            var wordSize = TextRenderer.MeasureText(e.Graphics, matchWord, font, proposedSize, flags);
            wordSize.Height = e.CellBounds.Height;
            Rectangle wordRect = new(wordPos, wordSize);

            var foreColor = matchEntry.HighlightEntry.ForegroundColor;
            if (e.State.HasFlag(DataGridViewElementStates.Selected))
            {
                if (foreColor.Equals(Color.Black))
                {
                    foreColor = Color.White;
                }
            }
            else
            {
                if (bgBrush != null && !matchEntry.HighlightEntry.NoBackground)
                {
                    e.Graphics.FillRectangle(bgBrush, wordRect);
                }
            }

            TextRenderer.DrawText(e.Graphics, matchWord, font, wordRect, foreColor, flags);
            wordPos.Offset(wordSize.Width, 0);
        }
    }

    /// <summary>
    /// Builds a list of HilightMatchEntry objects. A HilightMatchEntry spans over a region that is painted with the same foreground and
    /// background colors.
    /// All regions which don't match a word-mode entry will be painted with the colors of a default entry (groundEntry). This is either the
    /// first matching non-word-mode highlight entry or a black-on-white default (if no matching entry was found).
    /// </summary>
    /// <param name="matchList">List of all highlight matches for the current cell</param>
    /// <param name="groundEntry">The entry that is used as the default.</param>
    /// <returns>List of HighlightMatchEntry objects. The list spans over the whole cell and contains color infos for every substring.</returns>
    private IList<HighlightMatchEntry> MergeHighlightMatchEntries (IList<HighlightMatchEntry> matchList, HighlightMatchEntry groundEntry)
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
    /// Returns the first HilightEntry that matches the given line
    /// </summary>
    private HighlightEntry FindHilightEntry (ITextValue line)
    {
        return FindHighlightEntry(line, false);
    }

    private HighlightEntry FindFirstNoWordMatchHilightEntry (ITextValue line)
    {
        return FindHighlightEntry(line, true);
    }

    private bool CheckHighlightEntryMatch (HighlightEntry entry, ITextValue column)
    {
        if (entry.IsRegEx)
        {
            //Regex rex = new Regex(entry.SearchText, entry.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            if (entry.Regex.IsMatch(column.Text))
            {
                return true;
            }
        }
        else
        {
            if (entry.IsCaseSensitive)
            {
                if (column.Text.Contains(entry.SearchText, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            else
            {
                if (column.Text.ToUpperInvariant().Contains(entry.SearchText.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns all HilightEntry entries which matches the given line
    /// </summary>
    private IList<HighlightEntry> FindMatchingHilightEntries (ITextValue line)
    {
        IList<HighlightEntry> resultList = [];
        if (line != null)
        {
            lock (_currentHighlightGroupLock)
            {
                foreach (var entry in _currentHighlightGroup.HighlightEntryList)
                {
                    if (CheckHighlightEntryMatch(entry, line))
                    {
                        resultList.Add(entry);
                    }
                }
            }
        }

        return resultList;
    }

    private void GetHighlightEntryMatches (ITextValue line, IList<HighlightEntry> hilightEntryList, IList<HighlightMatchEntry> resultList)
    {
        foreach (var entry in hilightEntryList)
        {
            if (entry.IsWordMatch)
            {
                var matches = entry.Regex.Matches(line.Text);
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
                if (CheckHighlightEntryMatch(entry, line))
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

    private void GetHighlightActions (IList<HighlightEntry> matchingList, out bool noLed, out bool stopTail, out bool setBookmark, out string bookmarkComment)
    {
        noLed = stopTail = setBookmark = false;
        bookmarkComment = string.Empty;

        foreach (var entry in matchingList)
        {
            if (entry.IsLedSwitch)
            {
                noLed = true;
            }

            if (entry.IsSetBookmark)
            {
                setBookmark = true;
                if (!string.IsNullOrEmpty(entry.BookmarkComment))
                {
                    bookmarkComment += entry.BookmarkComment + "\r\n";
                }
            }

            if (entry.IsStopTail)
            {
                stopTail = true;
            }
        }

        bookmarkComment = bookmarkComment.TrimEnd(['\r', '\n']);
    }

    private void StopTimespreadThread ()
    {
        _timeSpreadCalc.Stop();
    }

    private void StopTimestampSyncThread ()
    {
        _shouldTimestampDisplaySyncingCancel = true;
        //_timeShiftSyncWakeupEvent.Set();
        //_timeShiftSyncThread.Abort();
        //_timeShiftSyncThread.Join();
        cts.Cancel();
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
        const int WAIT_TIME = 500;
        Thread.CurrentThread.Name = "SyncTimestampDisplayWorker";
        _shouldTimestampDisplaySyncingCancel = false;
        _isTimestampDisplaySyncing = true;

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
                var refLine = lineNum;
                var timeStamp = GetTimestampForLine(ref refLine, true);
                if (!timeStamp.Equals(DateTime.MinValue) && !_shouldTimestampDisplaySyncingCancel)
                {
                    _guiStateArgs.Timestamp = timeStamp;
                    SendGuiStateUpdate();
                    if (_shouldCallTimeSync)
                    {
                        refLine = lineNum;
                        var exactTimeStamp = GetTimestampForLine(ref refLine, false);
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

                var refLine = row1;
                var timeStamp1 = GetTimestampForLine(ref refLine, false);
                refLine = row2;
                var timeStamp2 = GetTimestampForLine(ref refLine, false);
                //TimeSpan span = TimeSpan.FromTicks(timeStamp2.Ticks - timeStamp1.Ticks);
                var diff = timeStamp1.Ticks > timeStamp2.Ticks
                    ? new DateTime(timeStamp1.Ticks - timeStamp2.Ticks)
                    : new DateTime(timeStamp2.Ticks - timeStamp1.Ticks);

                StatusLineText($"Time diff is {diff:HH:mm:ss.fff}");
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
            _logger.Error(e, "SyncFilterGridPos(): ");
        }
    }

    private void StatusLineFileSize (long size)
    {
        _statusEventArgs.FileSize = size;
        SendStatusLineUpdate();
    }

    [SupportedOSPlatform("windows")]
    private int Search (SearchParams searchParams)
    {
        if (searchParams.SearchText == null)
        {
            return -1;
        }

        var lineNum = searchParams.IsFromTop && !searchParams.IsFindNext
            ? 0
            : searchParams.CurrentLine;

        var lowerSearchText = searchParams.SearchText.ToLowerInvariant();
        var count = 0;
        var hasWrapped = false;

        while (true)
        {
            if ((searchParams.IsForward || searchParams.IsFindNext) && !searchParams.IsShiftF3Pressed)
            {
                if (lineNum >= _logFileReader.LineCount)
                {
                    if (hasWrapped)
                    {
                        StatusLineError("Not found: " + searchParams.SearchText);
                        return -1;
                    }

                    lineNum = 0;
                    count = 0;
                    hasWrapped = true;
                    StatusLineError("Started from beginning of file");
                }
            }
            else
            {
                if (lineNum < 0)
                {
                    if (hasWrapped)
                    {
                        StatusLineError("Not found: " + searchParams.SearchText);
                        return -1;
                    }

                    count = 0;
                    lineNum = _logFileReader.LineCount - 1;
                    hasWrapped = true;
                    StatusLineError("Started from end of file");
                }
            }

            var line = _logFileReader.GetLogLine(lineNum);
            if (line == null)
            {
                return -1;
            }

            if (searchParams.IsRegex)
            {
                Regex rex = new(searchParams.SearchText, searchParams.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                if (rex.IsMatch(line.FullLine))
                {
                    return lineNum;
                }
            }
            else
            {
                if (searchParams.IsCaseSensitive)
                {
                    if (line.FullLine.Contains(searchParams.SearchText, StringComparison.Ordinal))
                    {
                        return lineNum;
                    }
                }
                else
                {
                    if (line.FullLine.Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase))
                    {
                        return lineNum;
                    }
                }
            }

            if ((searchParams.IsForward || searchParams.IsFindNext) && !searchParams.IsShiftF3Pressed)
            {
                lineNum++;
            }
            else
            {
                lineNum--;
            }

            if (_shouldCancel)
            {
                return -1;
            }

            if (++count % PROGRESS_BAR_MODULO == 0)
            {
                try
                {
                    if (!Disposing)
                    {
                        Invoke(UpdateProgressBar, [count]);
                    }
                }
                catch (ObjectDisposedException ex) // can occur when closing the app while searching
                {
                    _logger.Warn(ex);
                }
            }
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
            var wasCancelled = _shouldCancel;
            _shouldCancel = false;
            _isSearching = false;
            StatusLineText(string.Empty);
            _guiStateArgs.MenuEnabled = true;

            if (wasCancelled)
            {
                return;
            }

            if (lineNum == -1)
            {
                // Hmm... is that experimental code from early days?
                MessageBox.Show(this, "Not found:", "Search result");
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
            _logger.Error(e, "Error while selecting line: ");
        }
        catch (IndexOutOfRangeException e)
        {
            // Occures sometimes (but cannot reproduce)
            _logger.Error(e, "Error while selecting line: ");
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
                else
                {
                    _logger.Warn(CultureInfo.InvariantCulture, "Edit control in logWindow was null");
                }
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
            StatusLineText("   " + pos);
            _logger.Debug(CultureInfo.InvariantCulture, "SelStart: {0}, SelLen: {1}", editControl.SelectionStart, editControl.SelectionLength);
        }
    }

    [SupportedOSPlatform("windows")]
    private void SelectPrevHighlightLine ()
    {
        var lineNum = dataGridView.CurrentCellAddress.Y;
        while (lineNum > 0)
        {
            lineNum--;
            var line = _logFileReader.GetLogLine(lineNum);
            if (line != null)
            {
                var entry = FindHilightEntry(line);
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
            var line = _logFileReader.GetLogLine(lineNum);
            if (line != null)
            {
                var entry = FindHilightEntry(line);
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
        OnBookmarkRemoved();
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
        filterKnobBackSpread.Value = _filterParams.SpreadBefore;
        filterKnobForeSpread.Value = _filterParams.SpreadBehind;
        rangeCheckBox.Checked = _filterParams.IsRangeSearch;
        columnRestrictCheckBox.Checked = _filterParams.ColumnRestrict;
        fuzzyKnobControl.Value = _filterParams.FuzzyValue;
        filterRangeComboBox.Text = _filterParams.RangeSearchText;
    }

    [SupportedOSPlatform("windows")]
    private void ResetFilterControls ()
    {
        filterComboBox.Text = "";
        filterCaseSensitiveCheckBox.Checked = false;
        filterRegexCheckBox.Checked = false;
        //this.filterTailCheckBox.Checked = this.Preferences.filterTail;
        invertFilterCheckBox.Checked = false;
        filterKnobBackSpread.Value = 0;
        filterKnobForeSpread.Value = 0;
        rangeCheckBox.Checked = false;
        columnRestrictCheckBox.Checked = false;
        fuzzyKnobControl.Value = 0;
        filterRangeComboBox.Text = "";
    }

    [SupportedOSPlatform("windows")]
    private void FilterSearch ()
    {
        if (filterComboBox.Text.Length == 0)
        {
            _filterParams.SearchText = string.Empty;
            _filterParams.IsRangeSearch = false;
            ClearFilterList();
            filterSearchButton.Image = null;
            ResetFilterControls();
            saveFilterButton.Enabled = false;
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
                StatusLineError("Invalid regular expression");
                return;
            }
        }

        _filterParams.FuzzyValue = fuzzyKnobControl.Value;
        _filterParams.SpreadBefore = filterKnobBackSpread.Value;
        _filterParams.SpreadBehind = filterKnobForeSpread.Value;
        _filterParams.ColumnRestrict = columnRestrictCheckBox.Checked;

        //ConfigManager.SaveFilterParams(this.filterParams);
        ConfigManager.Settings.FilterParams = _filterParams; // wozu eigentlich? sinnlos seit MDI?

        _shouldCancel = false;
        _isSearching = true;
        StatusLineText("Filtering... Press ESC to cancel");
        filterSearchButton.Enabled = false;
        ClearFilterList();

        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = dataGridView.RowCount;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        var settings = ConfigManager.Settings;

        //FilterFx fx = settings.preferences.multiThreadFilter ? MultiThreadedFilter : new FilterFx(Filter);
        FilterFxAction = settings.Preferences.MultiThreadFilter ? MultiThreadedFilter : Filter;

        //Task.Run(() => fx.Invoke(_filterParams, _filterResultList, _lastFilterLinesList, _filterHitList));
        var filterFxActionTask = Task.Run(() => Filter(_filterParams, _filterResultList, _lastFilterLinesList, _filterHitList));

        await filterFxActionTask;
        FilterComplete();

        //fx.BeginInvoke(_filterParams, _filterResultList, _lastFilterLinesList, _filterHitList, FilterComplete, null);
        CheckForFilterDirty();
    }

    private void MultiThreadedFilter (FilterParams filterParams, List<int> filterResultLines, List<int> lastFilterLinesList, List<int> filterHitList)
    {
        ColumnizerCallback callback = new(this);

        FilterStarter fs = new(callback, Environment.ProcessorCount + 2)
        {
            FilterHitList = _filterHitList,
            FilterResultLines = _filterResultList,
            LastFilterLinesList = _lastFilterLinesList
        };

        var cancelHandler = new FilterCancelHandler(fs);
        OnRegisterCancelHandler(cancelHandler);
        long startTime = Environment.TickCount;

        fs.DoFilter(filterParams, 0, _logFileReader.LineCount, FilterProgressCallback);

        long endTime = Environment.TickCount;

        _logger.Debug($"Multi threaded filter duration: {endTime - startTime} ms.");

        OnDeRegisterCancelHandler(cancelHandler);
        StatusLineText("Filter duration: " + (endTime - startTime) + " ms.");
    }

    private void FilterProgressCallback (int lineCount)
    {
        UpdateProgressBar(lineCount);
    }

    [SupportedOSPlatform("windows")]
    private void Filter (FilterParams filterParams, List<int> filterResultLines, List<int> lastFilterLinesList, List<int> filterHitList)
    {
        long startTime = Environment.TickCount;
        try
        {
            filterParams.Reset();
            var lineNum = 0;
            //AddFilterLineFx addFx = new AddFilterLineFx(AddFilterLine);
            ColumnizerCallback callback = new(this);
            while (true)
            {
                var line = _logFileReader.GetLogLine(lineNum);
                if (line == null)
                {
                    break;
                }

                callback.LineNum = lineNum;
                if (Util.TestFilterCondition(filterParams, line, callback))
                {
                    AddFilterLine(lineNum, false, filterParams, filterResultLines, lastFilterLinesList,
                        filterHitList);
                }

                lineNum++;
                if (lineNum % PROGRESS_BAR_MODULO == 0)
                {
                    UpdateProgressBar(lineNum);
                }

                if (_shouldCancel)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while filtering. Please report to developer: ");
            MessageBox.Show(null, $"Exception while filtering. Please report to developer: \n\n{ex}\n\n{ex.StackTrace}", "LogExpert");
        }

        long endTime = Environment.TickCount;

        _logger.Info($"Single threaded filter duration: {endTime - startTime} ms.");

        StatusLineText("Filter duration: " + (endTime - startTime) + " ms.");
    }

    /// <summary>
    ///  Returns a list with 'additional filter results'. This is the given line number
    ///  and (if back spread and/or fore spread is enabled) some additional lines.
    ///  This function doesn't check the filter condition!
    /// </summary>
    /// <param name="filterParams"></param>
    /// <param name="lineNum"></param>
    /// <param name="checkList"></param>
    /// <returns></returns>
    private IList<int> GetAdditionalFilterResults (FilterParams filterParams, int lineNum, IList<int> checkList)
    {
        IList<int> resultList = [];
        //string textLine = this.logFileReader.GetLogLine(lineNum);
        //ColumnizerCallback callback = new ColumnizerCallback(this);
        //callback.LineNum = lineNum;

        if (filterParams.SpreadBefore == 0 && filterParams.SpreadBehind == 0)
        {
            resultList.Add(lineNum);
            return resultList;
        }

        // back spread
        for (var i = filterParams.SpreadBefore; i > 0; --i)
        {
            if (lineNum - i > 0)
            {
                if (!resultList.Contains(lineNum - i) && !checkList.Contains(lineNum - i))
                {
                    resultList.Add(lineNum - i);
                }
            }
        }

        // direct filter hit
        if (!resultList.Contains(lineNum) && !checkList.Contains(lineNum))
        {
            resultList.Add(lineNum);
        }

        // after spread
        for (var i = 1; i <= filterParams.SpreadBehind; ++i)
        {
            if (lineNum + i < _logFileReader.LineCount)
            {
                if (!resultList.Contains(lineNum + i) && !checkList.Contains(lineNum + i))
                {
                    resultList.Add(lineNum + i);
                }
            }
        }

        return resultList;
    }

    [SupportedOSPlatform("windows")]
    private void AddFilterLine (int lineNum, bool immediate, FilterParams filterParams, List<int> filterResultLines, List<int> lastFilterLinesList, List<int> filterHitList)
    {
        int count;
        lock (_filterResultList)
        {
            filterHitList.Add(lineNum);
            var filterResult = GetAdditionalFilterResults(filterParams, lineNum, lastFilterLinesList);
            filterResultLines.AddRange(filterResult);
            count = filterResultLines.Count;
            lastFilterLinesList.AddRange(filterResult);
            if (lastFilterLinesList.Count > SPREAD_MAX * 2)
            {
                lastFilterLinesList.RemoveRange(0, lastFilterLinesList.Count - SPREAD_MAX * 2);
            }
        }

        if (immediate)
        {
            TriggerFilterLineGuiUpdate();
        }
        else if (lineNum % PROGRESS_BAR_MODULO == 0)
        {
            //FunctionWith1IntParam fx = new FunctionWith1IntParam(UpdateFilterCountLabel);
            //this.Invoke(fx, new object[] { count});
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
        Invoke(new MethodInvoker(AddFilterLineGuiUpdate));
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
                lblFilterCount.Text = "" + _filterResultList.Count;
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
            _logger.Error(e, "AddFilterLineGuiUpdate(): ");
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
            Invoke(new MethodInvoker(ResetStatusAfterFilter));
        }
    }

    [SupportedOSPlatform("windows")]
    private void FilterComplete (IAsyncResult result)
    {
        if (!IsDisposed && !_waitingForClose && !Disposing)
        {
            Invoke(new MethodInvoker(ResetStatusAfterFilter));
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
            lblFilterCount.Text = "" + _filterResultList.Count;
            if (filterGridView.RowCount > 0)
            {
                filterGridView.Focus();
            }

            filterSearchButton.Enabled = true;
        }
        catch (NullReferenceException e)
        {
            // See https://connect.microsoft.com/VisualStudio/feedback/details/366943/autoresizecolumns-in-datagridview-throws-nullreferenceexception
            // There are some rare situations with null ref exceptions when resizing columns and on filter finished
            // So catch them here. Better than crashing.
            _logger.Error(e, "Error: ");
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
                lblFilterCount.Text = "0";
                _filterResultList = [];
                _lastFilterLinesList = [];
                _filterHitList = [];
                //this.filterGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                filterGridView.ResumeLayout();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Wieder dieser sporadische Fehler: ");

            MessageBox.Show(null, ex.StackTrace, "Wieder dieser sporadische Fehler:");
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
        List<int> newFilterList = [];
        lock (_filterResultList)
        {
            foreach (var lineNum in _filterResultList)
            {
                var line = lineNum - offset;
                if (line >= 0)
                {
                    newFilterList.Add(line);
                }
            }

            _filterResultList = newFilterList;
        }

        newFilterList = [];
        foreach (var lineNum in _filterHitList)
        {
            var line = lineNum - offset;
            if (line >= 0)
            {
                newFilterList.Add(line);
            }
        }

        _filterHitList = newFilterList;

        var count = SPREAD_MAX;
        if (_filterResultList.Count < SPREAD_MAX)
        {
            count = _filterResultList.Count;
        }

        _lastFilterLinesList = _filterResultList.GetRange(_filterResultList.Count - count, count);

        //this.filterGridView.RowCount = this.filterResultList.Count;
        //this.filterCountLabel.Text = "" + this.filterResultList.Count;
        //this.BeginInvoke(new MethodInvoker(this.filterGridView.Refresh));
        //this.BeginInvoke(new MethodInvoker(AddFilterLineGuiUpdate));
        TriggerFilterLineGuiUpdate();
    }

    [SupportedOSPlatform("windows")]
    private void CheckForFilterDirty ()
    {
        if (IsFilterSearchDirty(_filterParams))
        {
            filterSearchButton.Image = _searchButtonImage;
            saveFilterButton.Enabled = false;
        }
        else
        {
            filterSearchButton.Image = null;
            saveFilterButton.Enabled = true;
        }
    }

    [SupportedOSPlatform("windows")]
    private bool IsFilterSearchDirty (FilterParams filterParams)
    {
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

        if (filterParams.SpreadBefore != filterKnobBackSpread.Value)
        {
            return true;
        }

        if (filterParams.SpreadBehind != filterKnobForeSpread.Value)
        {
            return true;
        }

        if (filterParams.FuzzyValue != fuzzyKnobControl.Value)
        {
            return true;
        }

        if (filterParams.ColumnRestrict != columnRestrictCheckBox.Checked)
        {
            return true;
        }

        return filterParams.IsCaseSensitive != filterCaseSensitiveCheckBox.Checked;
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
    private void InvalidateCurrentRow (BufferedDataGridView gridView)
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
            filterComboBox.Items.Add(item);
        }

        filterRangeComboBox.Items.Clear();
        foreach (var item in ConfigManager.Settings.FilterRangeHistoryList)
        {
            filterRangeComboBox.Items.Add(item);
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
        StatusLineText("");
        _isErrorShowing = false;
    }

    private void SendGuiStateUpdate ()
    {
        OnGuiState(_guiStateArgs);
    }

    private void SendProgressBarUpdate ()
    {
        OnProgressBarUpdate(_progressEventArgs);
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
            advancedButton.Text = "Hide advanced...";
            advancedButton.Image = null;
        }
        else
        {
            advancedButton.Text = "Show advanced...";
            CheckForAdvancedButtonDirty();
        }

        advancedFilterSplitContainer.Panel1Collapsed = !show;
        advancedFilterSplitContainer.SplitterDistance = FILTER_ADVANCED_SPLITTER_DISTANCE;
        _showAdvanced = show;
    }

    [SupportedOSPlatform("windows")]
    private void CheckForAdvancedButtonDirty ()
    {
        advancedButton.Image = IsAdvancedOptionActive() && !_showAdvanced
            ? _advancedButtonImage
            : null;
    }

    [SupportedOSPlatform("windows")]
    private void FilterToTab ()
    {
        filterSearchButton.Enabled = false;
        Task.Run(() => WriteFilterToTab());
    }

    [SupportedOSPlatform("windows")]
    private void WriteFilterToTab ()
    {
        FilterPipe pipe = new(_filterParams.Clone(), this);
        lock (_filterResultList)
        {
            var namePrefix = "->F";
            var title = IsTempFile
                ? TempTitleName + namePrefix + ++_filterPipeNameCounter
                : Util.GetNameFromPath(FileName) + namePrefix + ++_filterPipeNameCounter;

            WritePipeToTab(pipe, _filterResultList, title, null);
        }
    }

    [SupportedOSPlatform("windows")]
    private void WritePipeToTab (FilterPipe pipe, List<int> lineNumberList, string name, PersistenceData persistenceData)
    {
        _logger.Info(CultureInfo.InvariantCulture, "WritePipeToTab(): {0} lines.", lineNumberList.Count);
        StatusLineText("Writing to temp file... Press ESC to cancel.");
        _guiStateArgs.MenuEnabled = false;
        SendGuiStateUpdate();
        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = lineNumberList.Count;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        Invoke(new MethodInvoker(SendProgressBarUpdate));
        _isSearching = true;
        _shouldCancel = false;

        lock (_filterPipeList)
        {
            _filterPipeList.Add(pipe);
        }

        pipe.Closed += OnPipeDisconnected;
        var count = 0;
        pipe.OpenFile();
        LogExpertCallback callback = new(this);
        foreach (var i in lineNumberList)
        {
            if (_shouldCancel)
            {
                break;
            }

            var line = _logFileReader.GetLogLine(i);
            if (CurrentColumnizer is ILogLineXmlColumnizer)
            {
                callback.LineNum = i;
                line = (CurrentColumnizer as ILogLineXmlColumnizer).GetLineTextForClipboard(line, callback);
            }

            pipe.WriteToPipe(line, i);
            if (++count % PROGRESS_BAR_MODULO == 0)
            {
                _progressEventArgs.Value = count;
                Invoke(new MethodInvoker(SendProgressBarUpdate));
            }
        }

        pipe.CloseFile();
        _logger.Info(CultureInfo.InvariantCulture, "WritePipeToTab(): finished");
        Invoke(new WriteFilterToTabFinishedFx(WriteFilterToTabFinished), pipe, name, persistenceData);
    }

    [SupportedOSPlatform("windows")]
    private void WriteFilterToTabFinished (FilterPipe pipe, string name, PersistenceData persistenceData)
    {
        _isSearching = false;
        if (!_shouldCancel)
        {
            var title = name;
            ILogLineColumnizer preProcessColumnizer = null;
            if (CurrentColumnizer is not ILogLineXmlColumnizer)
            {
                preProcessColumnizer = CurrentColumnizer;
            }

            var newWin = _parentLogTabWin.AddFilterTab(pipe, title, preProcessColumnizer);
            newWin.FilterPipe = pipe;
            pipe.OwnLogWindow = newWin;
            if (persistenceData != null)
            {
                Task.Run(() => FilterRestore(newWin, persistenceData));
            }
        }

        _progressEventArgs.Value = _progressEventArgs.MaxValue;
        _progressEventArgs.Visible = false;
        SendProgressBarUpdate();
        _guiStateArgs.MenuEnabled = true;
        SendGuiStateUpdate();
        StatusLineText("");
        filterSearchButton.Enabled = true;
    }

    /// <summary>
    /// Used to create a new tab and pipe the given content into it.
    /// </summary>
    /// <param name="lineEntryList"></param>
    /// <param name="title"></param>
    [SupportedOSPlatform("windows")]
    internal void WritePipeTab (IList<LineEntry> lineEntryList, string title)
    {
        FilterPipe pipe = new(new FilterParams(), this)
        {
            IsStopped = true
        };

        pipe.Closed += OnPipeDisconnected;
        pipe.OpenFile();
        foreach (var entry in lineEntryList)
        {
            pipe.WriteToPipe(entry.LogLine, entry.LineNum);
        }

        pipe.CloseFile();
        Invoke(new WriteFilterToTabFinishedFx(WriteFilterToTabFinished), [pipe, title, null]);
    }

    [SupportedOSPlatform("windows")]
    private void FilterRestore (LogWindow newWin, PersistenceData persistenceData)
    {
        newWin.WaitForLoadingFinished();
        var columnizer = ColumnizerPicker.FindColumnizerByName(persistenceData.ColumnizerName,
            PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);
        if (columnizer != null)
        {
            SetColumnizerFx fx = newWin.ForceColumnizer;
            newWin.Invoke(fx, [columnizer]);
        }
        else
        {
            _logger.Warn($"FilterRestore(): Columnizer {persistenceData.ColumnizerName} not found");
        }

        newWin.BeginInvoke(new RestoreFiltersFx(newWin.RestoreFilters), [persistenceData]);
    }

    [SupportedOSPlatform("windows")]
    private void ProcessFilterPipes (int lineNum)
    {
        var searchLine = _logFileReader.GetLogLine(lineNum);
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

                //long startTime = Environment.TickCount;
                if (Util.TestFilterCondition(pipe.FilterParams, searchLine, callback))
                {
                    var filterResult =
                        GetAdditionalFilterResults(pipe.FilterParams, lineNum, pipe.LastLinesHistoryList);
                    pipe.OpenFile();
                    foreach (var line in filterResult)
                    {
                        pipe.LastLinesHistoryList.Add(line);
                        if (pipe.LastLinesHistoryList.Count > SPREAD_MAX * 2)
                        {
                            pipe.LastLinesHistoryList.RemoveAt(0);
                        }

                        var textLine = _logFileReader.GetLogLine(line);
                        var fileOk = pipe.WriteToPipe(textLine, line);
                        if (!fileOk)
                        {
                            deleteList.Add(pipe);
                        }
                    }

                    pipe.CloseFile();
                }

                //long endTime = Environment.TickCount;
                //_logger.logDebug("ProcessFilterPipes(" + lineNum + ") duration: " + ((endTime - startTime)));
            }
        }

        foreach (var pipe in deleteList)
        {
            _filterPipeList.Remove(pipe);
        }
    }

    [SupportedOSPlatform("windows")]
    private void CopyMarkedLinesToClipboard ()
    {
        if (_guiStateArgs.CellSelectMode)
        {
            var data = dataGridView.GetClipboardContent();
            Clipboard.SetDataObject(data);
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

            var xmlColumnizer = _currentColumnizer as ILogLineXmlColumnizer;

            foreach (var lineNum in lineNumList)
            {
                var line = _logFileReader.GetLogLine(lineNum);
                if (xmlColumnizer != null)
                {
                    callback.LineNum = lineNum;
                    line = xmlColumnizer.GetLineTextForClipboard(line, callback);
                }

                clipText.AppendLine(line.ToClipBoardText());
            }

            Clipboard.SetText(clipText.ToString());
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
    private IList<int> GetSelectedContent ()
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

        var line = 0;
        _guiStateArgs.MinTimestamp = GetTimestampForLineForward(ref line, true);
        line = dataGridView.RowCount - 1;
        _guiStateArgs.MaxTimestamp = GetTimestampForLine(ref line, true);
        SendGuiStateUpdate();
    }

    private void AdjustHighlightSplitterWidth ()
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
        var names = string.Empty;

        if (filter.ColumnRestrict)
        {
            foreach (var colIndex in filter.ColumnList)
            {
                if (colIndex < dataGridView.Columns.GetColumnCount(DataGridViewElementStates.None) - 2)
                {
                    if (names.Length > 0)
                    {
                        names += ", ";
                    }

                    names += dataGridView.Columns[2 + colIndex]
                        .HeaderText; // skip first two columns: marker + line number
                }
            }
        }

        return names;
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
            col.Frozen = _freezeStateMap.ContainsKey(gridView) && _freezeStateMap[gridView];
            var sel = col.HeaderCell.Selected;
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
        _ = _parentLogTabWin.AddTempFileTab(fileName, title);
    }

    private void InitPatternWindow ()
    {
        //PatternStatistic(this.patternArgs);
        _patternWindow = new PatternWindow(this);
        _patternWindow.SetColumnizer(CurrentColumnizer);
        //this.patternWindow.SetBlockList(blockList);
        _patternWindow.SetFont(Preferences.FontName, Preferences.FontSize);
        _patternWindow.Fuzzy = _patternArgs.Fuzzy;
        _patternWindow.MaxDiff = _patternArgs.MaxDiffInBlock;
        _patternWindow.MaxMisses = _patternArgs.MaxMisses;
        _patternWindow.Weight = _patternArgs.MinWeight;
        //this.patternWindow.Show();
    }

    [SupportedOSPlatform("windows")]
    private void TestStatistic (PatternArgs patternArgs)
    {
        var beginLine = patternArgs.StartLine;
        _logger.Info($"TestStatistics() called with start line {beginLine}");

        _patternArgs = patternArgs;

        var num = beginLine + 1; //this.dataGridView.RowCount;

        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = dataGridView.RowCount;
        _progressEventArgs.Value = beginLine;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        PrepareDict();
        ResetCache(num);

        Dictionary<int, int> processedLinesDict = [];
        List<PatternBlock> blockList = [];
        var blockId = 0;
        _isSearching = true;
        _shouldCancel = false;
        var searchLine = -1;
        for (var i = beginLine; i < num && !_shouldCancel; ++i)
        {
            if (processedLinesDict.ContainsKey(i))
            {
                continue;
            }

            PatternBlock block;
            var maxBlockLen = patternArgs.EndLine - patternArgs.StartLine;
            //int searchLine = i + 1;
            _logger.Debug(CultureInfo.InvariantCulture, "TestStatistic(): i={0} searchLine={1}", i, searchLine);
            //bool firstBlock = true;
            searchLine++;
            UpdateProgressBar(searchLine);
            while (!_shouldCancel &&
                   (block =
                       DetectBlock(i, searchLine, maxBlockLen, _patternArgs.MaxDiffInBlock,
                           _patternArgs.MaxMisses,
                           processedLinesDict)) != null)
            {
                _logger.Debug(CultureInfo.InvariantCulture, "Found block: {0}", block);
                if (block.Weigth >= _patternArgs.MinWeight)
                {
                    //PatternBlock existingBlock = FindExistingBlock(block, blockList);
                    //if (existingBlock != null)
                    //{
                    //  if (block.weigth > existingBlock.weigth)
                    //  {
                    //    blockList.Remove(existingBlock);
                    //    blockList.Add(block);
                    //  }
                    //}
                    //else
                    {
                        blockList.Add(block);
                        AddBlockTargetLinesToDict(processedLinesDict, block);
                    }
                    block.BlockId = blockId;
                    //if (firstBlock)
                    //{
                    //  addBlockSrcLinesToDict(processedLinesDict, block);
                    //}
                    searchLine = block.TargetEnd + 1;
                }
                else
                {
                    searchLine = block.TargetStart + 1;
                }

                UpdateProgressBar(searchLine);
            }

            blockId++;
        }

        _isSearching = false;
        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = 0;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = false;
        SendProgressBarUpdate();
        //if (this.patternWindow.IsDisposed)
        //{
        //  this.Invoke(new MethodInvoker(CreatePatternWindow));
        //}
        _patternWindow.SetBlockList(blockList, _patternArgs);
        _logger.Info(CultureInfo.InvariantCulture, "TestStatistics() ended");
    }

    private void AddBlockTargetLinesToDict (Dictionary<int, int> dict, PatternBlock block)
    {
        foreach (var lineNum in block.TargetLines.Keys)
        {
            _ = dict.TryAdd(lineNum, lineNum);
        }
    }

    //Well keep this for the moment because there is some other commented code which calls this one
    private PatternBlock FindExistingBlock (PatternBlock block, List<PatternBlock> blockList)
    {
        foreach (var searchBlock in blockList)
        {
            if (((block.StartLine > searchBlock.StartLine && block.StartLine < searchBlock.EndLine) ||
                 (block.EndLine > searchBlock.StartLine && block.EndLine < searchBlock.EndLine)) &&
                  block.StartLine != searchBlock.StartLine &&
                  block.EndLine != searchBlock.EndLine
            )
            {
                return searchBlock;
            }
        }

        return null;
    }

    private PatternBlock DetectBlock (int startNum, int startLineToSearch, int maxBlockLen, int maxDiffInBlock, int maxMisses, Dictionary<int, int> processedLinesDict)
    {
        var targetLine = FindSimilarLine(startNum, startLineToSearch, processedLinesDict);
        if (targetLine == -1)
        {
            return null;
        }

        PatternBlock block = new()
        {
            StartLine = startNum
        };

        var srcLine = block.StartLine;
        block.TargetStart = targetLine;
        var srcMisses = 0;
        block.SrcLines.Add(srcLine, srcLine);
        //block.targetLines.Add(targetLine, targetLine);
        var len = 0;
        QualityInfo qi = new()
        {
            Quality = block.Weigth
        };
        block.QualityInfoList[targetLine] = qi;

        while (!_shouldCancel)
        {
            srcLine++;
            len++;
            //if (srcLine >= block.targetStart)
            //  break;  // prevent to search in the own block
            if (maxBlockLen > 0 && len > maxBlockLen)
            {
                break;
            }

            var nextTargetLine = FindSimilarLine(srcLine, targetLine + 1, processedLinesDict);
            if (nextTargetLine > -1 && nextTargetLine - targetLine - 1 <= maxDiffInBlock)
            {
                block.Weigth += maxDiffInBlock - (nextTargetLine - targetLine - 1) + 1;
                block.EndLine = srcLine;
                //block.targetLines.Add(nextTargetLine, nextTargetLine);
                block.SrcLines.Add(srcLine, srcLine);
                if (nextTargetLine - targetLine > 1)
                {
                    var tempWeight = block.Weigth;
                    for (var tl = targetLine + 1; tl < nextTargetLine; ++tl)
                    {
                        qi = new QualityInfo
                        {
                            Quality = --tempWeight
                        };
                        block.QualityInfoList[tl] = qi;
                    }
                }

                targetLine = nextTargetLine;
                qi = new QualityInfo
                {
                    Quality = block.Weigth
                };
                block.QualityInfoList[targetLine] = qi;
            }
            else
            {
                srcMisses++;
                block.Weigth--;
                targetLine++;
                qi = new QualityInfo
                {
                    Quality = block.Weigth
                };
                block.QualityInfoList[targetLine] = qi;
                if (srcMisses > maxMisses)
                {
                    break;
                }
            }
        }

        block.TargetEnd = targetLine;
        qi = new QualityInfo
        {
            Quality = block.Weigth
        };

        block.QualityInfoList[targetLine] = qi;

        for (var k = block.TargetStart; k <= block.TargetEnd; ++k)
        {
            block.TargetLines.Add(k, k);
        }

        return block;
    }

    private void PrepareDict ()
    {
        _lineHashList.Clear();
        Regex regex = new("\\d");
        Regex regex2 = new("\\S");

        var num = _logFileReader.LineCount;
        for (var i = 0; i < num; ++i)
        {
            var msg = GetMsgForLine(i);
            if (msg != null)
            {
                msg = msg.ToLowerInvariant();
                msg = regex.Replace(msg, "0");
                msg = regex2.Replace(msg, " ");
                var chars = msg.ToCharArray();
                var value = 0;
                var numOfE = 0;
                var numOfA = 0;
                var numOfI = 0;
                foreach (var t in chars)
                {
                    value += t;
                    switch (t)
                    {
                        case 'e':
                            numOfE++;
                            break;
                        case 'a':
                            numOfA++;
                            break;
                        case 'i':
                            numOfI++;
                            break;
                    }
                }

                value += numOfE * 30;
                value += numOfA * 20;
                value += numOfI * 10;
                _lineHashList.Add(value);
            }
        }
    }

    private int FindSimilarLine (int srcLine, int startLine)
    {
        var value = _lineHashList[srcLine];

        var num = _lineHashList.Count;
        for (var i = startLine; i < num; ++i)
        {
            if (Math.Abs(_lineHashList[i] - value) < 3)
            {
                return i;
            }
        }

        return -1;
    }

    // int[,] similarCache;

    private void ResetCache (int num)
    {
        //this.similarCache = new int[num, num];
        //for (int i = 0; i < num; ++i)
        //{
        //  for (int j = 0; j < num; j++)
        //  {
        //    this.similarCache[i, j] = -1;
        //  }
        //}
    }

    private int FindSimilarLine (int srcLine, int startLine, Dictionary<int, int> processedLinesDict)
    {
        var threshold = _patternArgs.Fuzzy;

        var prepared = false;
        Regex regex = null;
        Regex regex2 = null;
        string msgToFind = null;
        var culture = CultureInfo.CurrentCulture;

        var num = _logFileReader.LineCount;
        for (var i = startLine; i < num; ++i)
        {
            if (processedLinesDict.ContainsKey(i))
            {
                continue;
            }

            //if (this.similarCache[srcLine, i] != -1)
            //{
            //  if (this.similarCache[srcLine, i] < threshold)
            //  {
            //    return i;
            //  }
            //}
            //else
            {
                if (!prepared)
                {
                    msgToFind = GetMsgForLine(srcLine);
                    regex = new Regex("\\d");
                    regex2 = new Regex("\\W");
                    msgToFind = msgToFind.ToLower(culture);
                    msgToFind = regex.Replace(msgToFind, "0");
                    msgToFind = regex2.Replace(msgToFind, " ");
                    prepared = true;
                }

                var msg = GetMsgForLine(i);
                if (msg != null)
                {
                    msg = regex.Replace(msg, "0");
                    msg = regex2.Replace(msg, " ");
                    var lenDiff = Math.Abs(msg.Length - msgToFind.Length);
                    if (lenDiff > threshold)
                    {
                        //this.similarCache[srcLine, i] = lenDiff;
                        continue;
                    }

                    msg = msg.ToLower(culture);
                    var distance = Util.YetiLevenshtein(msgToFind, msg);
                    //this.similarCache[srcLine, i] = distance;
                    if (distance < threshold)
                    {
                        return i;
                    }
                }
            }
        }

        return -1;
    }

    private string GetMsgForLine (int i)
    {
        var line = _logFileReader.GetLogLine(i);
        var columnizer = CurrentColumnizer;
        ColumnizerCallback callback = new(this);
        var cols = columnizer.SplitLine(callback, line);
        return cols.ColumnValues.Last().FullValue;
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
                    _rowHeightList.Remove(rowNum);
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
        OnBookmarkAdded();
    }

    private void SetDefaultHighlightGroup ()
    {
        var group = _parentLogTabWin.FindHighlightGroupByFileMask(FileName);
        if (group != null)
        {
            SetCurrentHighlightGroup(group.GroupName);
        }
        else
        {
            SetCurrentHighlightGroup("[Default]");
        }
    }

    [SupportedOSPlatform("windows")]
    private void HandleChangedFilterOnLoadSetting ()
    {
        _parentLogTabWin.Preferences.IsFilterOnLoad = filterOnLoadCheckBox.Checked;
        _parentLogTabWin.Preferences.IsAutoHideFilterList = hideFilterListOnLoadCheckBox.Checked;
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
            TimeSyncList?.NavigateToTimestamp(timestamp, this);
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
                var refLine = currentLineNum;
                var timeStamp = GetTimestampForLine(ref refLine, true);
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

    private void FreeSlaveFromTimesync (LogWindow slave)
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
            IsRegEx = para.IsRegex,
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
    private DataGridViewColumn GetColumnByName (BufferedDataGridView dataGridView, string name)
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
                            //TODO this needs to be refactored
                            var directory = ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir;

                            columnizer = ColumnizerPicker.CloneColumnizer(columnizer, directory);
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

            // this may be set after loading persistence data
            if (_fileNames != null && IsMultiFile)
            {
                LoadFilesAsMulti(_fileNames, EncodingOptions);
                return;
            }

            _columnCache = new ColumnCache();

            try
            {
                _logFileReader = new(fileName, EncodingOptions, IsMultiFile, Preferences.BufferCount, Preferences.LinesPerBuffer, _multiFileOptions, !Preferences.UseLegacyReader, PluginRegistry.PluginRegistry.Instance);
            }
            catch (LogFileException lfe)
            {
                _logger.Error(lfe);
                MessageBox.Show($"Cannot load file\n{lfe.Message}", "LogExpert");
                _ = BeginInvoke(new FunctionWith1BoolParam(Close), true);
                _isLoadError = true;
                return;
            }

            if (CurrentColumnizer is ILogLineXmlColumnizer xmlColumnizer)
            {
                _logFileReader.IsXmlMode = true;
                _logFileReader.XmlLogConfig = xmlColumnizer.GetXmlLogConfiguration();
            }

            if (_forcedColumnizerForLoading != null)
            {
                CurrentColumnizer = _forcedColumnizerForLoading;
            }

            _logFileReader.PreProcessColumnizer = CurrentColumnizer is IPreProcessColumnizer processColumnizer ? processColumnizer : null;

            RegisterLogFileReaderEvents();
            _logger.Info($"Loading logfile: {fileName}");
            _logFileReader.StartMonitoring();

            if (isUsingDefaultColumnizer)
            {
                if (Preferences.AutoPick)
                {
                    var newColumnizer = ColumnizerPicker.FindBetterColumnizer(FileName, _logFileReader, CurrentColumnizer, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

                    if (newColumnizer != null)
                    {
                        _logger.Debug($"Picked new columnizer '{newColumnizer}'");

                        PreSelectColumnizer(newColumnizer);
                    }
                }
            }
        }
    }

    public void LoadFilesAsMulti (string[] fileNames, EncodingOptions encodingOptions)
    {
        _logger.Info("Loading given files as MultiFile:");

        EnterLoadFileStatus();

        foreach (var name in fileNames)
        {
            _logger.Info($"File: {name}");
        }

        if (_logFileReader != null)
        {
            _logFileReader.StopMonitoring();
            UnRegisterLogFileReaderEvents();
        }

        EncodingOptions = encodingOptions;
        _columnCache = new ColumnCache();

        _logFileReader = new(fileNames, EncodingOptions, Preferences.BufferCount, Preferences.LinesPerBuffer, _multiFileOptions, !Preferences.UseLegacyReader, PluginRegistry.PluginRegistry.Instance);

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

    public string SavePersistenceData (bool force)
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
            var persistenceData = GetPersistenceData();

            return ForcedPersistenceFileName == null
                ? Persister.SavePersistenceData(FileName, persistenceData, Preferences)
                : Persister.SavePersistenceDataWithFixedName(ForcedPersistenceFileName, persistenceData);
        }
        catch (IOException ex)
        {
            _logger.Error(ex, "Error saving persistence: ");
        }
        catch (Exception e)
        {
            MessageBox.Show($"Unexpected error while saving persistence: {e.Message}");
        }

        return null;
    }

    public PersistenceData GetPersistenceData ()
    {
        PersistenceData persistenceData = new()
        {
            BookmarkList = _bookmarkProvider.BookmarkList,
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
            FileName = FileName,
            TabName = Text,
            SessionFileName = SessionFileName,
            ColumnizerName = CurrentColumnizer.GetName(),
            LineCount = _logFileReader.LineCount
        };

        _filterParams.IsFilterTail = filterTailCheckBox.Checked; // this option doesnt need a press on 'search'

        if (Preferences.SaveFilters)
        {
            List<FilterParams> filterList = [_filterParams];
            persistenceData.FilterParamsList = filterList;

            foreach (var filterPipe in _filterPipeList)
            {
                FilterTabData data = new()
                {
                    PersistenceData = filterPipe.OwnLogWindow.GetPersistenceData(),
                    FilterParams = filterPipe.FilterParams
                };
                persistenceData.FilterTabDataList.Add(data);
            }
        }

        if (_currentHighlightGroup != null)
        {
            persistenceData.HighlightGroupName = _currentHighlightGroup.GroupName;
        }

        if (_fileNames != null && IsMultiFile)
        {
            persistenceData.MultiFileNames.AddRange(_fileNames);
        }

        //persistenceData.showBookmarkCommentColumn = this.bookmarkWindow.ShowBookmarkCommentColumn;
        persistenceData.FilterSaveListVisible = !highlightSplitContainer.Panel2Collapsed;
        persistenceData.Encoding = _logFileReader.CurrentEncoding;

        return persistenceData;
    }

    public void Close (bool dontAsk)
    {
        Preferences.AskForClose = !dontAsk;
        Close();
    }

    public void CloseLogWindow ()
    {
        StopTimespreadThread();
        StopTimestampSyncThread();
        StopLogEventWorkerThread();
        _shouldCancel = true;

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
            _logger.Info($"Deleting temp file {FileName}");

            try
            {
                File.Delete(FileName);
            }
            catch (IOException e)
            {
                _logger.Error(e, $"Error while deleting temp file {FileName}: {e}");
            }
        }

        FilterPipe?.CloseAndDisconnect();
        DisconnectFilterPipes();
    }

    public void WaitForLoadingFinished ()
    {
        _externaLoadingFinishedEvent.WaitOne();
    }

    public void ForceColumnizer (ILogLineColumnizer columnizer)
    {
        //TODO this needs to be refactored
        var directory = ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir;

        _forcedColumnizer = ColumnizerPicker.CloneColumnizer(columnizer, directory);
        SetColumnizer(_forcedColumnizer);
    }

    public void ForceColumnizerForLoading (ILogLineColumnizer columnizer)
    {
        //TODO this needs to be refactored
        var directory = ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir;

        _forcedColumnizerForLoading = ColumnizerPicker.CloneColumnizer(columnizer, directory);
    }

    public void PreselectColumnizer (string columnizerName)
    {
        //TODO this needs to be refactored
        var directory = ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir;

        var columnizer = ColumnizerPicker.FindColumnizerByName(columnizerName, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);
        PreSelectColumnizer(ColumnizerPicker.CloneColumnizer(columnizer, directory));
    }

    public void ColumnizerConfigChanged ()
    {
        SetColumnizerInternal(CurrentColumnizer);
    }

    public void SetColumnizer (ILogLineColumnizer columnizer, BufferedDataGridView gridView)
    {
        PaintHelper.SetColumnizer(columnizer, gridView);

        gridView.Refresh();
        AutoResizeColumns(gridView);
        ApplyFrozenState(gridView);
    }

    public IColumn GetCellValue (int rowIndex, int columnIndex)
    {
        if (columnIndex == 1)
        {
            return new Column
            {
                FullValue = $"{rowIndex + 1}" // line number
            };
        }

        if (columnIndex == 0) // marker column
        {
            return Column.EmptyColumn;
        }

        try
        {
            var cols = GetColumnsForLine(rowIndex);
            if (cols != null && cols.ColumnValues != null)
            {
                if (columnIndex <= cols.ColumnValues.Length + 1)
                {
                    var value = cols.ColumnValues[columnIndex - 2];

                    return value != null && value.DisplayValue != null
                        ? value
                        : value;
                }

                return columnIndex == 2
                    ? cols.ColumnValues[^1]
                    : Column.EmptyColumn;
            }
        }
        catch
        {
            return Column.EmptyColumn;
        }

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

        var line = _logFileReader.GetLogLineWithWait(rowIndex).Result;

        if (line != null)
        {
            var entry = FindFirstNoWordMatchHilightEntry(line);
            e.Graphics.SetClip(e.CellBounds);

            if (e.State.HasFlag(DataGridViewElementStates.Selected))
            {
                using var brush = PaintHelper.GetBrushForFocusedControl(focused, e.CellStyle.SelectionBackColor);
                e.Graphics.FillRectangle(brush, e.CellBounds);
            }
            else
            {
                e.CellStyle.BackColor = PaintHelper.GetBackColorFromHighlightEntry(entry);
                e.PaintBackground(e.ClipBounds, false);
            }

            if (DebugOptions.DisableWordHighlight)
            {
                e.PaintContent(e.CellBounds);
            }
            else
            {
                PaintCell(e, entry);
            }

            if (columnIndex == 0)
            {
                if (_bookmarkProvider.IsBookmarkAtLine(rowIndex))
                {
                    //keeping this comment, because it's the original code
                    // = new Rectangle(e.CellBounds.Left + 2, e.CellBounds.Top + 2, 6, 6);
                    var rect = e.CellBounds;
                    rect.Inflate(-2, -2);
                    using var brush = new SolidBrush(BookmarkColor);
                    e.Graphics.FillRectangle(brush, rect);

                    var bookmark = _bookmarkProvider.GetBookmarkForLine(rowIndex);

                    if (bookmark.Text.Length > 0)
                    {
                        StringFormat format = new()
                        {
                            LineAlignment = StringAlignment.Center,
                            Alignment = StringAlignment.Center
                        };

                        //Todo Add this as a Settings Option
                        var fontName = isFilteredGridView ? "Verdana" : "Courier New";
                        var stringToDraw = isFilteredGridView ? "!" : "i";

                        using var brush2 = new SolidBrush(Color.FromArgb(255, 190, 100, 0)); //dark orange
                        using var font = new Font(fontName, Preferences.FontSize, FontStyle.Bold);
                        e.Graphics.DrawString(stringToDraw, font, brush2, new RectangleF(rect.Left, rect.Top, rect.Width, rect.Height), format);
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
    public HighlightEntry FindHighlightEntry (ITextValue line, bool noWordMatches)
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

                if (CheckHighlightEntryMatch(entry, line))
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

                if (CheckHighlightEntryMatch(entry, line))
                {
                    return entry;
                }
            }

            return null;
        }
    }

    public IList<HighlightMatchEntry> FindHighlightMatches (ITextValue line)
    {
        IList<HighlightMatchEntry> resultList = [];

        if (line != null)
        {
            lock (_currentHighlightGroupLock)
            {
                GetHighlightEntryMatches(line, _currentHighlightGroup.HighlightEntryList, resultList);
            }

            lock (_tempHighlightEntryList)
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
                dataGridView.FirstDisplayedScrollingRowIndex = _logFileReader.LineCount - 1;
            }
        }

        BeginInvoke(new MethodInvoker(dataGridView.Refresh));
        //this.dataGridView.Refresh();
        _parentLogTabWin.FollowTailChanged(this, isChecked, byTrigger);
        SendGuiStateUpdate();
    }

    public void TryToTruncate ()
    {
        try
        {
            if (LockFinder.CheckIfFileIsLocked(Title))
            {
                var name = LockFinder.FindLockedProcessName(Title);
                StatusLineText($"Truncate failed: file is locked by {name}");
            }
            else
            {
                File.WriteAllText(Title, "");
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Unexpected issue truncating file: {ex.Message}");
            StatusLineText("Unexpected issue truncating file");
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
        var searchParams = _parentLogTabWin.SearchParams;

        searchParams.CurrentLine = (searchParams.IsForward || searchParams.IsFindNext) && !searchParams.IsShiftF3Pressed
            ? dataGridView.CurrentCellAddress.Y + 1
            : dataGridView.CurrentCellAddress.Y - 1;

        _currentSearchParams = searchParams; // remember for async "not found" messages

        _isSearching = true;
        _shouldCancel = false;
        StatusLineText("Searching... Press ESC to cancel.");

        _progressEventArgs.MinValue = 0;
        _progressEventArgs.MaxValue = dataGridView.RowCount;
        _progressEventArgs.Value = 0;
        _progressEventArgs.Visible = true;
        SendProgressBarUpdate();

        Task.Run(() => Search(searchParams)).ContinueWith(SearchComplete);

        RemoveAllSearchHighlightEntries();
        AddSearchHitHighlightEntry(searchParams);
    }

    private void SearchComplete (Task<int> task)
    {
        if (Disposing)
        {
            return;
        }

        try
        {
            Invoke(new MethodInvoker(ResetProgressBar));
            var line = task.Result;
            _guiStateArgs.MenuEnabled = true;
            GuiStateUpdate(this, _guiStateArgs);
            if (line == -1)
            {
                return;
            }

            dataGridView.Invoke(new SelectLineFx((line1, triggerSyncCall) => SelectLine(line1, triggerSyncCall, true)), line, true);
        }
        catch (Exception ex) // in the case the windows is already destroyed
        {
            _logger.Warn(ex);
        }
    }

    public void SelectLogLine (int line)
    {
        Invoke(new SelectLineFx((line1, triggerSyncCall) => SelectLine(line1, triggerSyncCall, true)), line, true);
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
            _logger.Error(e);
        }
    }

    public void OnLogWindowKeyDown (object sender, KeyEventArgs e)
    {
        if (_isErrorShowing)
        {
            RemoveStatusLineError();
        }

        switch (e.KeyCode)
        {
            case Keys.F3 when _parentLogTabWin.SearchParams?.SearchText == null || _parentLogTabWin.SearchParams.SearchText.Length == 0:
                {
                    return;
                }
            case Keys.F3:
                {
                    _parentLogTabWin.SearchParams.IsFindNext = true;
                    _parentLogTabWin.SearchParams.IsShiftF3Pressed = (e.Modifiers & Keys.Shift) == Keys.Shift;
                    StartSearch();
                    break;
                }
            case Keys.Escape:
                {
                    if (_isSearching)
                    {
                        _shouldCancel = true;
                    }

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
        const int OVERSCAN = 20;

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
                        _logger.Debug($"AddBookmarkOverlay() r.Location={r.Location.X}, width={r.Width}, scroll_offset={dataGridView.HorizontalScrollingOffset}");
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

            if (string.IsNullOrEmpty(bookmark.Text) == false)
            {
                if (DialogResult.No == MessageBox.Show("There's a comment attached to the bookmark. Really remove the bookmark?", "LogExpert", MessageBoxButtons.YesNo))
                {
                    return;
                }
            }
            _bookmarkProvider.RemoveBookmarkForLine(lineNum);
        }
        else
        {
            _bookmarkProvider.AddBookmark(new Bookmark(lineNum));
        }
        dataGridView.Refresh();
        filterGridView.Refresh();
        OnBookmarkAdded();
    }

    public void SetBookmarkFromTrigger (int lineNum, string comment)
    {
        lock (_bookmarkLock)
        {
            var line = _logFileReader.GetLogLine(lineNum);
            if (line == null)
            {
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
                _bookmarkProvider.RemoveBookmarkForLine(lineNum);
            }
            _bookmarkProvider.AddBookmark(new Bookmark(lineNum, comment));
            OnBookmarkAdded();
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
                MessageBox.Show("There are some comments in the bookmarks. Really remove bookmarks?", "LogExpert",
                    MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }
        }
        _bookmarkProvider.RemoveBookmarksForLines(lineNumList);
        OnBookmarkRemoved();
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
                        if (text.StartsWith("+"))
                        {
                            text = text.Substring(1);
                        }
                        var timeSpan = TimeSpan.Parse(text);
                        var diff = (int)(timeSpan.Ticks / TimeSpan.TicksPerMillisecond);
                        CurrentColumnizer.SetTimeOffset(diff);
                    }
                    catch (Exception)
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
            catch (FormatException ex)
            {
                _logger.Error(ex);
            }
        }
    }

    public void ToggleFilterPanel ()
    {
        splitContainerLogWindow.Panel2Collapsed = !splitContainerLogWindow.Panel2Collapsed;
        if (!splitContainerLogWindow.Panel2Collapsed)
        {
            filterComboBox.Focus();
        }
        else
        {
            dataGridView.Focus();
        }
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

        dataGridView.Focus();

        SendGuiStateUpdate();
        SendStatusLineUpdate();
        SendProgressBarUpdate();
    }

    public void SetCellSelectionMode (bool isCellMode)
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
            WritePipeToTab(pipe, lineNumList, Text + "->C", null);
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
            var title = Util.GetNameFromPath(FileName) + "->Clip";
            _parentLogTabWin.AddTempFileTab(fileName, title);
        }
    }

    /// <summary>
    /// Change the file encoding. May force a reload if byte count ot preamble lenght differs from previous used encoding.
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
        //if (currentLine < this.dataGridView.RowCount && currentLine >= 0)
        //  this.dataGridView.CurrentCell = this.dataGridView.Rows[currentLine].Cells[0];
        //if (firstDisplayedLine < this.dataGridView.RowCount && firstDisplayedLine >= 0)
        //  this.dataGridView.FirstDisplayedScrollingRowIndex = firstDisplayedLine;

        //if (this.filterTailCheckBox.Checked)
        //{
        //  _logger.logInfo("Refreshing filter view because of reload.");
        //  FilterSearch();
        //}
    }

    public void PreferencesChanged (string fontName, float fontSize, bool setLastColumnWidth, int lastColumnWidth, bool isLoadTime, SettingsFlags flags)
    {
        if ((flags & SettingsFlags.GuiOrColors) == SettingsFlags.GuiOrColors)
        {
            NormalFont = new Font(new FontFamily(fontName), fontSize);
            BoldFont = new Font(NormalFont, FontStyle.Bold);
            MonospacedFont = new Font("Courier New", Preferences.FontSize, FontStyle.Bold);

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
                timeSpreadingControl.Invoke(new MethodInvoker(timeSpreadingControl.Refresh));
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
            BeginInvoke(new ScrollToTimestampFx(ScrollToTimestampWorker), timestamp, roundToSeconds, triggerSyncCall);
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
        var foundLine = FindTimestampLine(currentLine, timestamp, roundToSeconds);
        if (foundLine >= 0)
        {
            SelectAndEnsureVisible(foundLine, triggerSyncCall);
            hasScrolled = true;
        }
        //this.Cursor = Cursors.Default;
        return hasScrolled;
    }

    public int FindTimestampLine (int lineNum, DateTime timestamp, bool roundToSeconds)
    {
        var foundLine = FindTimestampLineInternal(lineNum, 0, dataGridView.RowCount - 1, timestamp, roundToSeconds);
        if (foundLine >= 0)
        {
            // go backwards to the first occurence of the hit
            var foundTimestamp = GetTimestampForLine(ref foundLine, roundToSeconds);
            while (foundTimestamp.CompareTo(timestamp) == 0 && foundLine >= 0)
            {
                foundLine--;
                foundTimestamp = GetTimestampForLine(ref foundLine, roundToSeconds);
            }
            if (foundLine < 0)
            {
                return 0;
            }

            foundLine++;
            GetTimestampForLineForward(ref foundLine, roundToSeconds); // fwd to next valid timestamp
            return foundLine;
        }

        return -foundLine;
    }

    public int FindTimestampLineInternal (int lineNum, int rangeStart, int rangeEnd, DateTime timestamp, bool roundToSeconds)
    {
        _logger.Debug($"FindTimestampLine_Internal(): timestamp={timestamp}, lineNum={lineNum}, rangeStart={rangeStart}, rangeEnd={rangeEnd}");
        var refLine = lineNum;
        var currentTimestamp = GetTimestampForLine(ref refLine, roundToSeconds);
        if (currentTimestamp.CompareTo(timestamp) == 0)
        {
            return lineNum;
        }

        if (timestamp < currentTimestamp)
        {
            //rangeStart = rangeStart;
            rangeEnd = lineNum;
        }
        else
        {
            rangeStart = lineNum;
            //rangeEnd = rangeEnd;
        }

        if (rangeEnd - rangeStart <= 0)
        {
            return -lineNum;
        }

        lineNum = ((rangeEnd - rangeStart) / 2) + rangeStart;
        // prevent endless loop
        if (rangeEnd - rangeStart < 2)
        {
            currentTimestamp = GetTimestampForLine(ref rangeStart, roundToSeconds);
            if (currentTimestamp.CompareTo(timestamp) == 0)
            {
                return rangeStart;
            }

            currentTimestamp = GetTimestampForLine(ref rangeEnd, roundToSeconds);

            return currentTimestamp.CompareTo(timestamp) == 0
                ? rangeEnd
                : -lineNum;
        }

        return FindTimestampLineInternal(lineNum, rangeStart, rangeEnd, timestamp, roundToSeconds);
    }

    /**
   * Get the timestamp for the given line number. If the line
   * has no timestamp, the previous line will be checked until a
   * timestamp is found.
   */
    public DateTime GetTimestampForLine (ref int lineNum, bool roundToSeconds)
    {
        lock (_currentColumnizerLock)
        {
            if (!CurrentColumnizer.IsTimeshiftImplemented())
            {
                return DateTime.MinValue;
            }

            _logger.Debug($"GetTimestampForLine({lineNum}) enter");
            var timeStamp = DateTime.MinValue;
            var lookBack = false;
            if (lineNum >= 0 && lineNum < dataGridView.RowCount)
            {
                while (timeStamp.CompareTo(DateTime.MinValue) == 0 && lineNum >= 0)
                {
                    if (_isTimestampDisplaySyncing && _shouldTimestampDisplaySyncingCancel)
                    {
                        return DateTime.MinValue;
                    }

                    lookBack = true;
                    var logLine = _logFileReader.GetLogLine(lineNum);
                    if (logLine == null)
                    {
                        return DateTime.MinValue;
                    }

                    ColumnizerCallbackObject.LineNum = lineNum;
                    timeStamp = CurrentColumnizer.GetTimestamp(ColumnizerCallbackObject, logLine);
                    if (roundToSeconds)
                    {
                        timeStamp = timeStamp.Subtract(TimeSpan.FromMilliseconds(timeStamp.Millisecond));
                    }

                    lineNum--;
                }
            }

            if (lookBack)
            {
                lineNum++;
            }

            _logger.Debug($"GetTimestampForLine() leave with lineNum={lineNum}");
            return timeStamp;
        }
    }

    /**
   * Get the timestamp for the given line number. If the line
   * has no timestamp, the next line will be checked until a
   * timestamp is found.
   */
    public DateTime GetTimestampForLineForward (ref int lineNum, bool roundToSeconds)
    {
        lock (_currentColumnizerLock)
        {
            if (!CurrentColumnizer.IsTimeshiftImplemented())
            {
                return DateTime.MinValue;
            }

            var timeStamp = DateTime.MinValue;
            var lookFwd = false;
            if (lineNum >= 0 && lineNum < dataGridView.RowCount)
            {
                while (timeStamp.CompareTo(DateTime.MinValue) == 0 && lineNum < dataGridView.RowCount)
                {
                    lookFwd = true;
                    var logLine = _logFileReader.GetLogLine(lineNum);

                    if (logLine == null)
                    {
                        timeStamp = DateTime.MinValue;
                        break;
                    }

                    timeStamp = CurrentColumnizer.GetTimestamp(ColumnizerCallbackObject, logLine);

                    if (roundToSeconds)
                    {
                        timeStamp = timeStamp.Subtract(TimeSpan.FromMilliseconds(timeStamp.Millisecond));
                    }

                    lineNum++;
                }
            }

            if (lookFwd)
            {
                lineNum--;
            }

            return timeStamp;
        }
    }

    public void AppFocusLost ()
    {
        InvalidateCurrentRow(dataGridView);
    }

    public void AppFocusGained ()
    {
        InvalidateCurrentRow(dataGridView);
    }

    public ILogLine GetCurrentLine ()
    {
        return dataGridView.CurrentRow != null && dataGridView.CurrentRow.Index != -1
            ? _logFileReader.GetLogLine(dataGridView.CurrentRow.Index)
            : null;
    }

    public ILogLine GetLine (int lineNum)
    {
        return lineNum < 0 || _logFileReader == null || lineNum >= _logFileReader.LineCount
            ? null
            : _logFileReader.GetLogLine(lineNum);
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

    // =================================================================
    // Pattern statistics
    // =================================================================

    public void PatternStatistic ()
    {
        InitPatternWindow();
    }

    public void PatternStatisticSelectRange (PatternArgs patternArgs)
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
            patternArgs.StartLine = lineNumList[0];
            patternArgs.EndLine = lineNumList[^1];
        }
        else
        {
            patternArgs.StartLine = dataGridView.CurrentCellAddress.Y != -1
                ? dataGridView.CurrentCellAddress.Y
                : 0;
            patternArgs.EndLine = dataGridView.RowCount - 1;
        }
    }

    public void PatternStatistic (PatternArgs patternArgs)
    {
        var fx = new PatternStatisticFx(TestStatistic);
        fx.BeginInvoke(patternArgs, null, null);
    }

    public void ExportBookmarkList ()
    {
        SaveFileDialog dlg = new()
        {
            Title = "Choose a file to save bookmarks into",
            AddExtension = true,
            DefaultExt = "csv",
            Filter = "CSV file (*.csv)|*.csv|Bookmark file (*.bmk)|*.bmk",
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
                _logger.Error(e);
                MessageBox.Show("Error while exporting bookmark list: " + e.Message, "LogExpert");
            }
        }
    }

    public void ImportBookmarkList ()
    {
        OpenFileDialog dlg = new()
        {
            Title = "Choose a file to load bookmarks from",
            AddExtension = true,
            DefaultExt = "csv",
            Filter = "CSV file (*.csv)|*.csv|Bookmark file (*.bmk)|*.bmk",
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
                    if (!_bookmarkProvider.BookmarkList.ContainsKey(b.LineNum))
                    {
                        _bookmarkProvider.BookmarkList.Add(b.LineNum, b);
                        bookmarkAdded = true; // refresh the list only once at the end
                    }
                    else
                    {
                        var existingBookmark = _bookmarkProvider.BookmarkList[b.LineNum];
                        existingBookmark.Text =
                            b.Text; // replace existing bookmark for that line, preserving the overlay
                        OnBookmarkTextChanged(b);
                    }
                }

                // Refresh the lists
                if (bookmarkAdded)
                {
                    OnBookmarkAdded();
                }
                dataGridView.Refresh();
                filterGridView.Refresh();
            }
            catch (IOException e)
            {
                _logger.Error(e);
                MessageBox.Show($"Error while importing bookmark list: {e.Message}", "LogExpert");
            }
        }
    }

    public bool IsAdvancedOptionActive ()
    {
        return rangeCheckBox.Checked ||
               fuzzyKnobControl.Value > 0 ||
               filterKnobBackSpread.Value > 0 ||
               filterKnobForeSpread.Value > 0 ||
               invertFilterCheckBox.Checked ||
               columnRestrictCheckBox.Checked;
    }

    public void HandleChangedFilterList ()
    {
        Invoke(new MethodInvoker(HandleChangedFilterListWorker));
    }

    public void HandleChangedFilterListWorker ()
    {
        var index = filterListBox.SelectedIndex;
        filterListBox.Items.Clear();
        foreach (var filterParam in ConfigManager.Settings.FilterList)
        {
            filterListBox.Items.Add(filterParam);
        }
        filterListBox.Refresh();
        if (index >= 0 && index < filterListBox.Items.Count)
        {
            filterListBox.SelectedIndex = index;
        }
        filterOnLoadCheckBox.Checked = Preferences.IsFilterOnLoad;
        hideFilterListOnLoadCheckBox.Checked = Preferences.IsAutoHideFilterList;
    }

    public void SetCurrentHighlightGroup (string groupName)
    {
        _guiStateArgs.HighlightGroupName = groupName;
        lock (_currentHighlightGroupLock)
        {
            _currentHighlightGroup = _parentLogTabWin.FindHighlightGroup(groupName);

            _currentHighlightGroup ??= _parentLogTabWin.HighlightGroupList.Count > 0
                ? _parentLogTabWin.HighlightGroupList[0]
                : new HighlightGroup();

            _guiStateArgs.HighlightGroupName = _currentHighlightGroup.GroupName;
        }

        SendGuiStateUpdate();
        BeginInvoke(new MethodInvoker(RefreshAllGrids));
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
        _logger.Info($"Syncing window for {Util.GetNameFromPath(FileName)} to {Util.GetNameFromPath(master.FileName)}");
        lock (_timeSyncListLock)
        {
            if (IsTimeSynced && master.TimeSyncList != TimeSyncList)
            // already synced but master has different sync list
            {
                FreeFromTimeSync();
            }

            TimeSyncList = master.TimeSyncList;
            TimeSyncList.AddWindow(this);
            ScrollToTimestamp(TimeSyncList.CurrentTimestamp, false, false);
        }

        OnSyncModeChanged();
    }

    public void FreeFromTimeSync ()
    {
        lock (_timeSyncListLock)
        {
            if (TimeSyncList != null)
            {
                _logger.Info($"De-Syncing window for {Util.GetNameFromPath(FileName)}");
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