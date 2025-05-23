using LogExpert.Classes;
using LogExpert.Classes.Filter;
using LogExpert.Classes.ILogLineColumnizerCallback;
using LogExpert.Classes.Log;
using LogExpert.Config;
using LogExpert.Core.Classes.Bookmark;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArgs;
using LogExpert.Core.Interface;
using LogExpert.Dialogs;
using LogExpert.Entities.EventArgs;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Extensions.Forms;

using NLog;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.Controls.LogWindow
{
    public partial class LogWindow : DockContent, ILogPaintContext, ILogView
    {
        #region Fields

        private const int SPREAD_MAX = 99;
        private const int PROGRESS_BAR_MODULO = 1000;
        private const int FILTER_ADVANCED_SPLITTER_DISTANCE = 150;
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly Image _advancedButtonImage;

        private readonly object _bookmarkLock = new();
        private readonly BookmarkDataProvider _bookmarkProvider = new();

        private readonly IList<BackgroundProcessCancelHandler> _cancelHandlerList = new List<BackgroundProcessCancelHandler>();

        private readonly object _currentColumnizerLock = new();

        private readonly object _currentHighlightGroupLock = new();

        private readonly EventWaitHandle _externaLoadingFinishedEvent = new ManualResetEvent(false);

        private readonly IList<FilterPipe> _filterPipeList = new List<FilterPipe>();
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
        private int _filterPipeNameCounter = 0;
        private List<int> _filterResultList = [];

        private EventWaitHandle _filterUpdateEvent = new ManualResetEvent(false);

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

        private int _lineHeight = 0;

        internal LogfileReader _logFileReader;
        private MultiFileOptions _multiFileOptions = new();
        private bool _noSelectionUpdates;
        private PatternArgs _patternArgs = new();
        private PatternWindow _patternWindow;

        private ReloadMemento _reloadMemento;
        private int _reloadOverloadCounter = 0;
        private SortedList<int, RowHeightEntry> _rowHeightList = [];
        private int _selectedCol = 0; // set by context menu event for column headers only
        private bool _shouldCallTimeSync;
        private bool _shouldCancel;
        private bool _shouldTimestampDisplaySyncingCancel;
        private bool _showAdvanced;
        private List<HighlightEntry> _tempHighlightEntryList = [];
        private int _timeShiftSyncLine = 0;

        private bool _waitingForClose;

        #endregion

        #region cTor

        public LogWindow(LogTabWindow.LogTabWindow parent, string fileName, bool isTempFile, bool forcePersistenceLoading)
        {
            SuspendLayout();

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            InitializeComponent();

            CreateDefaultViewStyle();

            columnNamesLabel.Text = ""; // no filtering on columns by default

            _parentLogTabWin = parent;
            IsTempFile = isTempFile;
            //Thread.CurrentThread.Name = "LogWindowThread";
            ColumnizerCallbackObject = new ColumnizerCallback(this);

            FileName = fileName;
            ForcePersistenceLoading = forcePersistenceLoading;

            dataGridView.CellValueNeeded += OnDataGridViewCellValueNeeded;
            dataGridView.CellPainting += OnDataGridView_CellPainting;

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
            foreach (string item in ConfigManager.Settings.filterHistoryList)
            {
                filterComboBox.Items.Add(item);
            }

            filterComboBox.DropDownHeight = filterComboBox.ItemHeight * ConfigManager.Settings.Preferences.maximumFilterEntriesDisplayed;
            AutoResizeFilterBox();

            filterRegexCheckBox.Checked = _filterParams.IsRegex;
            filterCaseSensitiveCheckBox.Checked = _filterParams.IsCaseSensitive;
            filterTailCheckBox.Checked = _filterParams.IsFilterTail;

            splitContainerLogWindow.Panel2Collapsed = true;
            advancedFilterSplitContainer.SplitterDistance = FILTER_ADVANCED_SPLITTER_DISTANCE;

            _timeShiftSyncTask = new Task(SyncTimestampDisplayWorker, cts.Token);
            _timeShiftSyncTask.Start();
            //_timeShiftSyncThread = new Thread(SyncTimestampDisplayWorker);
            //_timeShiftSyncThread.IsBackground = true;
            //_timeShiftSyncThread.Start();

            _logEventHandlerTask = new Task(LogEventWorker, cts.Token);
            _logEventHandlerTask.Start();
            //_logEventHandlerThread = new Thread(LogEventWorker);
            //_logEventHandlerThread.IsBackground = true;
            //_logEventHandlerThread.Start();

            //this.filterUpdateThread = new Thread(new ThreadStart(this.FilterUpdateWorker));
            //this.filterUpdateThread.Start();

            _advancedButtonImage = advancedButton.Image;
            _searchButtonImage = filterSearchButton.Image;
            filterSearchButton.Image = null;

            dataGridView.EditModeMenuStrip = editModeContextMenuStrip;
            markEditModeToolStripMenuItem.Enabled = true;

            _panelOpenButtonImage = Resources.Resources.Arrow_menu_open;
            _panelCloseButtonImage = Resources.Resources.Arrow_menu_close;

            Settings settings = ConfigManager.Settings;

            if (settings.appBounds.Right > 0)
            {
                Bounds = settings.appBounds;
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

            ChangeTheme(Controls);
        }

        #endregion

        #region ColorTheme
        public void ChangeTheme(Control.ControlCollection container)
        {
            #region ApplyColorToAllControls
            foreach (Control component in container)
            {
                if (component.Controls != null && component.Controls.Count > 0)
                {
                    ChangeTheme(component.Controls);
                    component.BackColor = ColorMode.BackgroundColor;
                    component.ForeColor = ColorMode.ForeColor;
                }
                else
                {
                    component.BackColor = ColorMode.BackgroundColor;
                    component.ForeColor = ColorMode.ForeColor;
                }

            }
            #endregion

            #region DataGridView

            // Main DataGridView
            dataGridView.BackgroundColor = ColorMode.DockBackgroundColor;
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = ColorMode.BackgroundColor;
            dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = ColorMode.ForeColor;
            dataGridView.EnableHeadersVisualStyles = false;

            // Filter dataGridView
            filterGridView.BackgroundColor = ColorMode.DockBackgroundColor;
            filterGridView.ColumnHeadersDefaultCellStyle.BackColor = ColorMode.BackgroundColor;
            filterGridView.ColumnHeadersDefaultCellStyle.ForeColor = ColorMode.ForeColor;
            filterGridView.EnableHeadersVisualStyles = false;

            // Colors for menu
            dataGridContextMenuStrip.Renderer = new ExtendedMenuStripRenderer();
            bookmarkContextMenuStrip.Renderer = new ExtendedMenuStripRenderer();
            columnContextMenuStrip.Renderer = new ExtendedMenuStripRenderer();
            editModeContextMenuStrip.Renderer = new ExtendedMenuStripRenderer();
            filterContextMenuStrip.Renderer = new ExtendedMenuStripRenderer();
            filterListContextMenuStrip.Renderer = new ExtendedMenuStripRenderer();

            foreach (ToolStripItem item in dataGridContextMenuStrip.Items)
            {
                item.ForeColor = ColorMode.ForeColor;
                item.BackColor = ColorMode.MenuBackgroundColor;
            }

            foreach (ToolStripItem item in bookmarkContextMenuStrip.Items)
            {
                item.ForeColor = ColorMode.ForeColor;
                item.BackColor = ColorMode.MenuBackgroundColor;
            }

            foreach (ToolStripItem item in columnContextMenuStrip.Items)
            {
                item.ForeColor = ColorMode.ForeColor;
                item.BackColor = ColorMode.MenuBackgroundColor;
            }

            foreach (ToolStripItem item in editModeContextMenuStrip.Items)
            {
                item.ForeColor = ColorMode.ForeColor;
                item.BackColor = ColorMode.MenuBackgroundColor;
            }

            foreach (ToolStripItem item in filterContextMenuStrip.Items)
            {
                item.ForeColor = ColorMode.ForeColor;
                item.BackColor = ColorMode.MenuBackgroundColor;
            }

            foreach (ToolStripItem item in filterListContextMenuStrip.Items)
            {
                item.ForeColor = ColorMode.ForeColor;
                item.BackColor = ColorMode.MenuBackgroundColor;
            }

            // Colors for menu
            filterContextMenuStrip.Renderer = new ExtendedMenuStripRenderer();

            for (var y = 0; y < filterContextMenuStrip.Items.Count; y++)
            {
                var item = filterContextMenuStrip.Items[y];
                item.ForeColor = ColorMode.ForeColor;
                item.BackColor = ColorMode.MenuBackgroundColor;
            }

            #endregion DataGridView

            filterComboBox.BackColor = ColorMode.DockBackgroundColor;
        }

        #endregion

        #region Delegates

        public delegate void BookmarkAddedEventHandler(object sender, EventArgs e);

        public delegate void BookmarkRemovedEventHandler(object sender, EventArgs e);

        public delegate void BookmarkTextChangedEventHandler(object sender, BookmarkEventArgs e);

        public delegate void ColumnizerChangedEventHandler(object sender, ColumnizerEventArgs e);

        public delegate void CurrentHighlightGroupChangedEventHandler(object sender, CurrentHighlightGroupChangedEventArgs e);

        public delegate void FileNotFoundEventHandler(object sender, EventArgs e);

        public delegate void FileRespawnedEventHandler(object sender, EventArgs e);

        public delegate void FileSizeChangedEventHandler(object sender, LogEventArgs e);

        public delegate void FilterListChangedEventHandler(object sender, FilterListChangedEventArgs e);

        // used for filterTab restore
        public delegate void FilterRestoreFx(LogWindow newWin, PersistenceData persistenceData);

        public delegate void GuiStateEventHandler(object sender, GuiStateArgs e);

        public delegate void ProgressBarEventHandler(object sender, ProgressEventArgs e);

        public delegate void RestoreFiltersFx(PersistenceData persistenceData);

        public delegate bool ScrollToTimestampFx(DateTime timestamp, bool roundToSeconds, bool triggerSyncCall);

        public delegate void StatusLineEventHandler(object sender, StatusLineEventArgs e);

        public delegate void SyncModeChangedEventHandler(object sender, SyncModeEventArgs e);

        public delegate void TailFollowedEventHandler(object sender, EventArgs e);

        #endregion

        #region Events

        public event FileSizeChangedEventHandler FileSizeChanged;

        public event ProgressBarEventHandler ProgressBarUpdate;

        public event StatusLineEventHandler StatusLineEvent;

        public event GuiStateEventHandler GuiStateUpdate;

        public event TailFollowedEventHandler TailFollowed;

        public event FileNotFoundEventHandler FileNotFound;

        public event FileRespawnedEventHandler FileRespawned;

        public event FilterListChangedEventHandler FilterListChanged;

        public event CurrentHighlightGroupChangedEventHandler CurrentHighlightGroupChanged;

        public event BookmarkAddedEventHandler BookmarkAdded;

        public event BookmarkRemovedEventHandler BookmarkRemoved;

        public event BookmarkTextChangedEventHandler BookmarkTextChanged;

        public event ColumnizerChangedEventHandler ColumnizerChanged;

        public event SyncModeChangedEventHandler SyncModeChanged;

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

        public bool ShowBookmarkBubbles
        {
            get => _guiStateArgs.ShowBookmarkBubbles;
            set
            {
                _guiStateArgs.ShowBookmarkBubbles = dataGridView.PaintWithOverlays = value;
                dataGridView.Refresh();
            }
        }

        public string FileName { get; private set; }

        public string SessionFileName { get; set; } = null;

        public bool IsMultiFile
        {
            get => _isMultiFile;
            private set => _guiStateArgs.IsMultiFileActive = _isMultiFile = value;
        }

        public bool IsTempFile { get; }

        public string TempTitleName { get; set; } = "";

        internal FilterPipe FilterPipe { get; set; } = null;

        public string Title
        {
            get
            {
                if (IsTempFile)
                {
                    return TempTitleName;
                }

                return FileName;
            }
        }

        public ColumnizerCallback ColumnizerCallbackObject { get; }

        public bool ForcePersistenceLoading { get; set; }

        public string ForcedPersistenceFileName { get; set; } = null;

        public Preferences Preferences => ConfigManager.Settings.Preferences;

        public string GivenFileName { get; set; } = null;

        public TimeSyncList TimeSyncList { get; private set; }

        public bool IsTimeSynced => TimeSyncList != null;

        protected EncodingOptions EncodingOptions { get; set; }

        public IBookmarkData BookmarkData => _bookmarkProvider;

        public Font MonospacedFont { get; private set; }

        public Font NormalFont { get; private set; }

        public Font BoldFont { get; private set; }

        #endregion

        #region Public methods

        public ILogLine GetLogLine(int lineNum)
        {
            return _logFileReader.GetLogLine(lineNum);
        }

        public Bookmark GetBookmarkForLine(int lineNum)
        {
            return _bookmarkProvider.GetBookmarkForLine(lineNum);
        }

        #endregion

        #region Internals

        internal IColumnizedLogLine GetColumnsForLine(int lineNumber)
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

        internal void RefreshAllGrids()
        {
            dataGridView.Refresh();
            filterGridView.Refresh();
        }

        internal void ChangeMultifileMask()
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

        internal void ToggleColumnFinder(bool show, bool setFocus)
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

        protected override string GetPersistString()
        {
            return "LogWindow#" + FileName;
        }

        #endregion

        private void OnButtonSizeChanged(object sender, EventArgs e)
        {
            if (sender is Button button && button.Image != null)
            {
                button.ImageAlign = ContentAlignment.MiddleCenter;
                button.Image = new Bitmap(button.Image, new Size(button.Size.Height, button.Size.Height));
            }
        }

        // used for external wait fx WaitForLoadFinished()

        private delegate void SelectLineFx(int line, bool triggerSyncCall);

        private Action<FilterParams, List<int>, List<int>, List<int>> FilterFxAction;
        //private delegate void FilterFx(FilterParams filterParams, List<int> filterResultLines, List<int> lastFilterResultLines, List<int> filterHitList);

        private delegate void UpdateProgressBarFx(int lineNum);

        private delegate void SetColumnizerFx(ILogLineColumnizer columnizer);

        private delegate void WriteFilterToTabFinishedFx(FilterPipe pipe, string namePrefix, PersistenceData persistenceData);

        private delegate void SetBookmarkFx(int lineNum, string comment);

        private delegate void FunctionWith1BoolParam(bool arg);

        private delegate void PatternStatisticFx(PatternArgs patternArgs);

        private delegate void ActionPluginExecuteFx(string keyword, string param, ILogExpertCallback callback, ILogLineColumnizer columnizer);

        private delegate void PositionAfterReloadFx(ReloadMemento reloadMemento);

        private delegate void AutoResizeColumnsFx(BufferedDataGridView gridView);

        private delegate bool BoolReturnDelegate();

        // =================== ILogLineColumnizerCallback ============================

#if DEBUG
        internal void DumpBufferInfo()
        {
            int currentLineNum = dataGridView.CurrentCellAddress.Y;
            _logFileReader.LogBufferInfoForLine(currentLineNum);
        }

        internal void DumpBufferDiagnostic()
        {
            _logFileReader.LogBufferDiagnostic();
        }
#endif
    }
}