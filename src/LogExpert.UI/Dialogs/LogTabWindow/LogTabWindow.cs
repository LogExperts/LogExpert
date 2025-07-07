using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;
using LogExpert.Core.EventArguments;
using LogExpert.Core.Interface;
using LogExpert.Dialogs;
using LogExpert.Entities;
using LogExpert.PluginRegistry.FileSystem;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Entities;
using LogExpert.UI.Extensions;
using LogExpert.UI.Extensions.LogWindow;

using NLog;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Controls.LogTabWindow;

// Data shared over all LogTabWindow instances
//TODO: Can we get rid of this class?
[SupportedOSPlatform("windows")]
internal partial class LogTabWindow : Form, ILogTabWindow
{
    #region Fields

    private const int MAX_COLUMNIZER_HISTORY = 40;
    private const int MAX_COLOR_HISTORY = 40;
    private const int DIFF_MAX = 100;
    private const int MAX_FILE_HISTORY = 10;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly Icon _deadIcon;

    private readonly Color _defaultTabColor = Color.FromArgb(255, 192, 192, 192);
    private readonly Brush _dirtyLedBrush;

    private readonly int _instanceNumber;
    private readonly Brush[] _ledBrushes = new Brush[5];
    private readonly Icon[,,,] _ledIcons = new Icon[6, 2, 4, 2];

    private readonly Rectangle[] _leds = new Rectangle[5];

    private readonly IList<LogWindow.LogWindow> _logWindowList = [];
    private readonly Brush _offLedBrush;
    private readonly bool _showInstanceNumbers;

    private readonly string[] _startupFileNames;

    private readonly EventWaitHandle _statusLineEventHandle = new AutoResetEvent(false);
    private readonly EventWaitHandle _statusLineEventWakeupHandle = new ManualResetEvent(false);
    private readonly Brush _syncLedBrush;

    [SupportedOSPlatform("windows")]
    private readonly StringFormat _tabStringFormat = new();

    private readonly Brush[] _tailLedBrush = new Brush[3];

    private BookmarkWindow _bookmarkWindow;

    private LogWindow.LogWindow _currentLogWindow;
    private bool _firstBookmarkWindowShow = true;

    private Thread _ledThread;

    //Settings settings;

    private bool _shouldStop;

    private bool _skipEvents;

    private bool _wasMaximized;

    #endregion

    #region cTor

    [SupportedOSPlatform("windows")]
    public LogTabWindow (string[] fileNames, int instanceNumber, bool showInstanceNumbers, IConfigManager configManager)
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();

        ConfigManager = configManager;

        //Fix MainMenu and externalToolsToolStrip.Location, if the location has unintentionally been changed in the designer
        mainMenuStrip.Location = new Point(0, 0);
        externalToolsToolStrip.Location = new Point(0, 54);

        _startupFileNames = fileNames;
        _instanceNumber = instanceNumber;
        _showInstanceNumbers = showInstanceNumbers;

        Load += OnLogTabWindowLoad;

        configManager.Instance.ConfigChanged += OnConfigChanged;
        HighlightGroupList = configManager.Settings.Preferences.HighlightGroupList;

        Rectangle led = new(0, 0, 8, 2);

        for (var i = 0; i < _leds.Length; ++i)
        {
            _leds[i] = led;
            led.Offset(0, led.Height + 0);
        }

        var grayAlpha = 50;

        _ledBrushes[0] = new SolidBrush(Color.FromArgb(255, 220, 0, 0));
        _ledBrushes[1] = new SolidBrush(Color.FromArgb(255, 220, 220, 0));
        _ledBrushes[2] = new SolidBrush(Color.FromArgb(255, 0, 220, 0));
        _ledBrushes[3] = new SolidBrush(Color.FromArgb(255, 0, 220, 0));
        _ledBrushes[4] = new SolidBrush(Color.FromArgb(255, 0, 220, 0));

        _offLedBrush = new SolidBrush(Color.FromArgb(grayAlpha, 160, 160, 160));

        _dirtyLedBrush = new SolidBrush(Color.FromArgb(255, 220, 0, 00));

        _tailLedBrush[0] = new SolidBrush(Color.FromArgb(255, 50, 100, 250)); // Follow tail: blue-ish
        _tailLedBrush[1] = new SolidBrush(Color.FromArgb(grayAlpha, 160, 160, 160)); // Don't follow tail: gray
        _tailLedBrush[2] = new SolidBrush(Color.FromArgb(255, 220, 220, 0)); // Stop follow tail (trigger): yellow-ish

        _syncLedBrush = new SolidBrush(Color.FromArgb(255, 250, 145, 30));

        CreateIcons();

        _tabStringFormat.LineAlignment = StringAlignment.Center;
        _tabStringFormat.Alignment = StringAlignment.Near;

        ToolStripControlHost host = new(checkBoxFollowTail);

        host.Padding = new Padding(20, 0, 0, 0);
        host.BackColor = Color.FromKnownColor(KnownColor.Transparent);

        var index = buttonToolStrip.Items.IndexOfKey("toolStripButtonTail");

        toolStripEncodingASCIIItem.Text = Encoding.ASCII.HeaderName;
        toolStripEncodingANSIItem.Text = Encoding.Default.HeaderName;
        toolStripEncodingISO88591Item.Text = Encoding.GetEncoding("iso-8859-1").HeaderName;
        toolStripEncodingUTF8Item.Text = Encoding.UTF8.HeaderName;
        toolStripEncodingUTF16Item.Text = Encoding.Unicode.HeaderName;

        if (index != -1)
        {
            buttonToolStrip.Items.RemoveAt(index);
            buttonToolStrip.Items.Insert(index, host);
        }

        dragControlDateTime.Visible = false;
        loadProgessBar.Visible = false;

        // get a reference to the current assembly
        var a = Assembly.GetExecutingAssembly();

        // get a list of resource names from the manifest
        var resNames = a.GetManifestResourceNames();

        Bitmap bmp = Resources.Deceased;
        _deadIcon = Icon.FromHandle(bmp.GetHicon());
        bmp.Dispose();
        Closing += OnLogTabWindowClosing;

        InitToolWindows();
    }

    #endregion

    #region Delegates

    private delegate void AddFileTabsDelegate (string[] fileNames);

    private delegate void ExceptionFx ();

    private delegate void FileNotFoundDelegate (LogWindow.LogWindow logWin);

    private delegate void FileRespawnedDelegate (LogWindow.LogWindow logWin);

    public delegate void HighlightSettingsChangedEventHandler (object sender, EventArgs e);

    private delegate void LoadMultiFilesDelegate (string[] fileName, EncodingOptions encodingOptions);

    private delegate void SetColumnizerFx (ILogLineColumnizer columnizer);

    private delegate void SetTabIconDelegate (LogWindow.LogWindow logWindow, Icon icon);

    #endregion

    #region Events

    public event HighlightSettingsChangedEventHandler HighlightSettingsChanged;

    #endregion

    #region Properties

    [SupportedOSPlatform("windows")]
    public LogWindow.LogWindow CurrentLogWindow
    {
        get => _currentLogWindow;
        set => ChangeCurrentLogWindow(value);
    }

    public SearchParams SearchParams { get; private set; } = new SearchParams();

    public Preferences Preferences => ConfigManager.Settings.Preferences;

    public List<HighlightGroup> HighlightGroupList { get; private set; } = [];

    //public Settings Settings
    //{
    //  get { return ConfigManager.Settings; }
    //}

    public ILogExpertProxy LogExpertProxy { get; set; }
    public IConfigManager ConfigManager { get; }

    #endregion

    #region Internals

    internal HighlightGroup FindHighlightGroup (string groupName)
    {
        lock (HighlightGroupList)
        {
            foreach (HighlightGroup group in HighlightGroupList)
            {
                if (group.GroupName.Equals(groupName, StringComparison.Ordinal))
                {
                    return group;
                }
            }

            return null;
        }
    }

    #endregion

    private class LogWindowData
    {
        #region Fields

        // public MdiTabControl.TabPage tabPage;

        public Color Color { get; set; } = Color.FromKnownColor(KnownColor.Gray);

        public int DiffSum { get; set; }

        public bool Dirty { get; set; }

        // tailState:
        /// <summary>
        /// 0 = on<br></br>
        /// 1 = off<br></br>
        /// 2 = off by Trigger<br></br>
        /// </summary>
        public int TailState { get; set; }

        public ToolTip ToolTip { get; set; }

        /// <summary>
        /// 0 = off<br></br>
        /// 1 = timeSynced
        /// </summary>
        public int SyncMode { get; set; }

        #endregion
    }

    #region Public methods

    [SupportedOSPlatform("windows")]
    public LogWindow.LogWindow AddTempFileTab (string fileName, string title)
    {
        return AddFileTab(fileName, true, title, false, null);
    }

    [SupportedOSPlatform("windows")]
    public LogWindow.LogWindow AddFilterTab (FilterPipe pipe, string title, ILogLineColumnizer preProcessColumnizer)
    {
        var logWin = AddFileTab(pipe.FileName, true, title, false, preProcessColumnizer);
        if (pipe.FilterParams.SearchText.Length > 0)
        {
            ToolTip tip = new(components);

            tip.SetToolTip(logWin,
                "Filter: \"" + pipe.FilterParams.SearchText + "\"" +
                (pipe.FilterParams.IsInvert ? " (Invert match)" : "") +
                (pipe.FilterParams.ColumnRestrict ? "\nColumn restrict" : "")
            );

            tip.AutomaticDelay = 10;
            tip.AutoPopDelay = 5000;
            var data = logWin.Tag as LogWindowData;
            data.ToolTip = tip;
        }

        return logWin;
    }

    [SupportedOSPlatform("windows")]
    public LogWindow.LogWindow AddFileTabDeferred (string givenFileName, bool isTempFile, string title, bool forcePersistenceLoading, ILogLineColumnizer preProcessColumnizer)
    {
        return AddFileTab(givenFileName, isTempFile, title, forcePersistenceLoading, preProcessColumnizer, true);
    }

    [SupportedOSPlatform("windows")]
    public LogWindow.LogWindow AddFileTab (string givenFileName, bool isTempFile, string title, bool forcePersistenceLoading, ILogLineColumnizer preProcessColumnizer, bool doNotAddToDockPanel = false)
    {
        var logFileName = FindFilenameForSettings(givenFileName);
        var win = FindWindowForFile(logFileName);
        if (win != null)
        {
            if (!isTempFile)
            {
                AddToFileHistory(givenFileName);
            }

            SelectTab(win);
            return win;
        }

        EncodingOptions encodingOptions = new();
        FillDefaultEncodingFromSettings(encodingOptions);
        LogWindow.LogWindow logWindow = new(this, logFileName, isTempFile, forcePersistenceLoading, ConfigManager)
        {
            GivenFileName = givenFileName
        };

        if (preProcessColumnizer != null)
        {
            logWindow.ForceColumnizerForLoading(preProcessColumnizer);
        }

        if (isTempFile)
        {
            logWindow.TempTitleName = title;
            encodingOptions.Encoding = new UnicodeEncoding(false, false);
        }

        AddLogWindow(logWindow, title, doNotAddToDockPanel);
        if (!isTempFile)
        {
            AddToFileHistory(givenFileName);
        }

        var data = logWindow.Tag as LogWindowData;
        data.Color = _defaultTabColor;
        SetTabColor(logWindow, _defaultTabColor);
        //data.tabPage.BorderColor = this.defaultTabBorderColor;
        if (!isTempFile)
        {
            foreach (var colorEntry in ConfigManager.Settings.FileColors)
            {
                if (colorEntry.FileName.ToUpperInvariant().Equals(logFileName.ToUpperInvariant(), StringComparison.Ordinal))
                {
                    data.Color = colorEntry.Color;
                    SetTabColor(logWindow, colorEntry.Color);
                    break;
                }
            }
        }

        if (!isTempFile)
        {
            SetTooltipText(logWindow, logFileName);
        }

        if (givenFileName.EndsWith(".lxp", StringComparison.Ordinal))
        {
            logWindow.ForcedPersistenceFileName = givenFileName;
        }

        // this.BeginInvoke(new LoadFileDelegate(logWindow.LoadFile), new object[] { logFileName, encoding });
        Task.Run(() => logWindow.LoadFile(logFileName, encodingOptions));
        return logWindow;
    }

    [SupportedOSPlatform("windows")]
    public LogWindow.LogWindow AddMultiFileTab (string[] fileNames)
    {
        if (fileNames.Length < 1)
        {
            return null;
        }

        LogWindow.LogWindow logWindow = new(this, fileNames[^1], false, false, ConfigManager);
        AddLogWindow(logWindow, fileNames[^1], false);
        multiFileToolStripMenuItem.Checked = true;
        multiFileEnabledStripMenuItem.Checked = true;
        EncodingOptions encodingOptions = new();
        FillDefaultEncodingFromSettings(encodingOptions);
        BeginInvoke(new LoadMultiFilesDelegate(logWindow.LoadFilesAsMulti), fileNames, encodingOptions);
        AddToFileHistory(fileNames[0]);
        return logWindow;
    }

    [SupportedOSPlatform("windows")]
    public void LoadFiles (string[] fileNames)
    {
        Invoke(new AddFileTabsDelegate(AddFileTabs), [fileNames]);
    }

    [SupportedOSPlatform("windows")]
    public void OpenSearchDialog ()
    {
        if (CurrentLogWindow == null)
        {
            return;
        }

        SearchDialog dlg = new();
        AddOwnedForm(dlg);
        dlg.TopMost = TopMost;
        SearchParams.HistoryList = ConfigManager.Settings.SearchHistoryList;
        dlg.SearchParams = SearchParams;
        var res = dlg.ShowDialog();
        if (res == DialogResult.OK && dlg.SearchParams != null && !string.IsNullOrWhiteSpace(dlg.SearchParams.SearchText))
        {
            SearchParams = dlg.SearchParams;
            SearchParams.IsFindNext = false;
            CurrentLogWindow.StartSearch();
        }
    }

    public ILogLineColumnizer GetColumnizerHistoryEntry (string fileName)
    {
        var entry = FindColumnizerHistoryEntry(fileName);
        if (entry != null)
        {
            foreach (var columnizer in PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers)
            {
                if (columnizer.GetName().Equals(entry.ColumnizerName, StringComparison.Ordinal))
                {
                    return columnizer;
                }
            }

            ConfigManager.Settings.ColumnizerHistoryList.Remove(entry); // no valid name -> remove entry
        }

        return null;
    }

    public void SwitchTab (bool shiftPressed)
    {
        var index = dockPanel.Contents.IndexOf(dockPanel.ActiveContent);
        if (shiftPressed)
        {
            index--;
            if (index < 0)
            {
                index = dockPanel.Contents.Count - 1;
            }

            if (index < 0)
            {
                return;
            }
        }
        else
        {
            index++;
            if (index >= dockPanel.Contents.Count)
            {
                index = 0;
            }
        }

        if (index < dockPanel.Contents.Count)
        {
            (dockPanel.Contents[index] as DockContent).Activate();
        }
    }

    public void ScrollAllTabsToTimestamp (DateTime timestamp, LogWindow.LogWindow senderWindow)
    {
        lock (_logWindowList)
        {
            foreach (var logWindow in _logWindowList)
            {
                if (logWindow != senderWindow)
                {
                    if (logWindow.ScrollToTimestamp(timestamp, false, false))
                    {
                        ShowLedPeak(logWindow);
                    }
                }
            }
        }
    }

    public ILogLineColumnizer FindColumnizerByFileMask (string fileName)
    {
        foreach (var entry in ConfigManager.Settings.Preferences.ColumnizerMaskList)
        {
            if (entry.Mask != null)
            {
                try
                {
                    if (Regex.IsMatch(fileName, entry.Mask))
                    {
                        var columnizer = ColumnizerPicker.FindColumnizerByName(entry.ColumnizerName, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);
                        return columnizer;
                    }
                }
                catch (ArgumentException e)
                {
                    _logger.Error(e, "RegEx-error while finding columnizer: ");
                    // occurs on invalid regex patterns
                }
            }
        }

        return null;
    }

    public HighlightGroup FindHighlightGroupByFileMask (string fileName)
    {
        foreach (var entry in ConfigManager.Settings.Preferences.HighlightMaskList)
        {
            if (entry.Mask != null)
            {
                try
                {
                    if (Regex.IsMatch(fileName, entry.Mask))
                    {
                        var group = FindHighlightGroup(entry.HighlightGroupName);
                        return group;
                    }
                }
                catch (ArgumentException e)
                {
                    _logger.Error(e, "RegEx-error while finding columnizer: ");
                    // occurs on invalid regex patterns
                }
            }
        }

        return null;
    }

    public void SelectTab (ILogWindow logWindow)
    {
        logWindow.Activate();
    }

    [SupportedOSPlatform("windows")]
    public void SetForeground ()
    {
        NativeMethods.SetForegroundWindow(Handle);
        if (WindowState == FormWindowState.Minimized)
        {
            if (_wasMaximized)
            {
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                WindowState = FormWindowState.Normal;
            }
        }
    }

    // called from LogWindow when follow tail was changed
    [SupportedOSPlatform("windows")]
    public void FollowTailChanged (LogWindow.LogWindow logWindow, bool isEnabled, bool offByTrigger)
    {
        if (logWindow.Tag is not LogWindowData data)
        {
            return;
        }

        if (isEnabled)
        {
            data.TailState = 0;
        }
        else
        {
            data.TailState = offByTrigger ? 2 : 1;
        }

        if (Preferences.ShowTailState)
        {
            var icon = GetIcon(data.DiffSum, data);
            BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWindow, icon);
        }
    }

    [SupportedOSPlatform("windows")]
    public void NotifySettingsChanged (object sender, SettingsFlags flags)
    {
        if (sender != this)
        {
            NotifyWindowsForChangedPrefs(flags);
        }
    }

    public IList<WindowFileEntry> GetListOfOpenFiles ()
    {
        IList<WindowFileEntry> list = [];
        lock (_logWindowList)
        {
            foreach (var logWindow in _logWindowList)
            {
                list.Add(new WindowFileEntry(logWindow));
            }
        }

        return list;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates a temp file with the text content of the clipboard and opens the temp file in a new tab.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void PasteFromClipboard ()
    {
        if (Clipboard.ContainsText())
        {
            var text = Clipboard.GetText();
            var fileName = Path.GetTempFileName();

            using (FileStream fStream = new(fileName, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (StreamWriter writer = new(fStream, Encoding.Unicode))
            {
                writer.Write(text);
                writer.Close();
            }

            var title = "Clipboard";
            var logWindow = AddTempFileTab(fileName, title);
            if (logWindow.Tag is LogWindowData data)
            {
                SetTooltipText(logWindow, "Pasted on " + DateTime.Now);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void InitToolWindows ()
    {
        InitBookmarkWindow();
    }

    [SupportedOSPlatform("windows")]
    private void DestroyToolWindows ()
    {
        DestroyBookmarkWindow();
    }

    [SupportedOSPlatform("windows")]
    private void InitBookmarkWindow ()
    {
        _bookmarkWindow = new BookmarkWindow
        {
            HideOnClose = true,
            ShowHint = DockState.DockBottom
        };

        var setLastColumnWidth = ConfigManager.Settings.Preferences.SetLastColumnWidth;
        var lastColumnWidth = ConfigManager.Settings.Preferences.LastColumnWidth;
        var fontName = ConfigManager.Settings.Preferences.FontName;
        var fontSize = ConfigManager.Settings.Preferences.FontSize;

        _bookmarkWindow.PreferencesChanged(fontName, fontSize, setLastColumnWidth, lastColumnWidth, SettingsFlags.All);
        _bookmarkWindow.VisibleChanged += OnBookmarkWindowVisibleChanged;
        _firstBookmarkWindowShow = true;
    }

    [SupportedOSPlatform("windows")]
    private void DestroyBookmarkWindow ()
    {
        _bookmarkWindow.HideOnClose = false;
        _bookmarkWindow.Close();
    }

    private void SaveLastOpenFilesList ()
    {
        ConfigManager.Settings.LastOpenFilesList.Clear();
        foreach (DockContent content in dockPanel.Contents)
        {
            if (content is LogWindow.LogWindow logWin)
            {
                if (!logWin.IsTempFile)
                {
                    ConfigManager.Settings.LastOpenFilesList.Add(logWin.GivenFileName);
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void SaveWindowPosition ()
    {
        SuspendLayout();
        if (WindowState == FormWindowState.Normal)
        {
            ConfigManager.Settings.AppBounds = Bounds;
            ConfigManager.Settings.IsMaximized = false;
        }
        else
        {
            ConfigManager.Settings.AppBoundsFullscreen = Bounds;
            ConfigManager.Settings.IsMaximized = true;
            WindowState = FormWindowState.Normal;
            ConfigManager.Settings.AppBounds = Bounds;
        }

        ResumeLayout();
    }

    private void SetTooltipText (LogWindow.LogWindow logWindow, string logFileName)
    {
        logWindow.ToolTipText = logFileName;
    }

    private void FillDefaultEncodingFromSettings (EncodingOptions encodingOptions)
    {
        if (ConfigManager.Settings.Preferences.DefaultEncoding != null)
        {
            try
            {
                encodingOptions.DefaultEncoding = Encoding.GetEncoding(ConfigManager.Settings.Preferences.DefaultEncoding);
            }
            catch (ArgumentException)
            {
                _logger.Warn(CultureInfo.InvariantCulture, "Encoding " + ConfigManager.Settings.Preferences.DefaultEncoding + " is not a valid encoding");
                encodingOptions.DefaultEncoding = null;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void AddFileTabs (string[] fileNames)
    {
        foreach (var fileName in fileNames)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                if (fileName.EndsWith(".lxj"))
                {
                    LoadProject(fileName, false);
                }
                else
                {
                    AddFileTab(fileName, false, null, false, null);
                }
            }
        }

        Activate();
    }

    [SupportedOSPlatform("windows")]
    private void AddLogWindow (LogWindow.LogWindow logWindow, string title, bool doNotAddToPanel)
    {
        logWindow.CloseButton = true;
        logWindow.TabPageContextMenuStrip = tabContextMenuStrip;
        SetTooltipText(logWindow, title);
        logWindow.DockAreas = DockAreas.Document | DockAreas.Float;

        if (!doNotAddToPanel)
        {
            logWindow.Show(dockPanel);
        }

        LogWindowData data = new()
        {
            DiffSum = 0
        };

        logWindow.Tag = data;

        lock (_logWindowList)
        {
            _logWindowList.Add(logWindow);
        }

        logWindow.FileSizeChanged += OnFileSizeChanged;
        logWindow.TailFollowed += OnTailFollowed;
        logWindow.Disposed += OnLogWindowDisposed;
        logWindow.FileNotFound += OnLogWindowFileNotFound;
        logWindow.FileRespawned += OnLogWindowFileRespawned;
        logWindow.FilterListChanged += OnLogWindowFilterListChanged;
        logWindow.CurrentHighlightGroupChanged += OnLogWindowCurrentHighlightGroupChanged;
        logWindow.SyncModeChanged += OnLogWindowSyncModeChanged;

        logWindow.Visible = true;
    }

    [SupportedOSPlatform("windows")]
    private void DisconnectEventHandlers (LogWindow.LogWindow logWindow)
    {
        logWindow.FileSizeChanged -= OnFileSizeChanged;
        logWindow.TailFollowed -= OnTailFollowed;
        logWindow.Disposed -= OnLogWindowDisposed;
        logWindow.FileNotFound -= OnLogWindowFileNotFound;
        logWindow.FileRespawned -= OnLogWindowFileRespawned;
        logWindow.FilterListChanged -= OnLogWindowFilterListChanged;
        logWindow.CurrentHighlightGroupChanged -= OnLogWindowCurrentHighlightGroupChanged;
        logWindow.SyncModeChanged -= OnLogWindowSyncModeChanged;

        var data = logWindow.Tag as LogWindowData;
        //data.tabPage.MouseClick -= tabPage_MouseClick;
        //data.tabPage.TabDoubleClick -= tabPage_TabDoubleClick;
        //data.tabPage.ContextMenuStrip = null;
        //data.tabPage = null;
    }

    [SupportedOSPlatform("windows")]
    private void AddToFileHistory (string fileName)
    {
        bool FindName (string s) => s.ToUpperInvariant().Equals(fileName.ToUpperInvariant(), StringComparison.Ordinal);

        var index = ConfigManager.Settings.FileHistoryList.FindIndex(FindName);

        if (index != -1)
        {
            ConfigManager.Settings.FileHistoryList.RemoveAt(index);
        }

        ConfigManager.Settings.FileHistoryList.Insert(0, fileName);

        while (ConfigManager.Settings.FileHistoryList.Count > MAX_FILE_HISTORY)
        {
            ConfigManager.Settings.FileHistoryList.RemoveAt(ConfigManager.Settings.FileHistoryList.Count - 1);
        }

        ConfigManager.Save(SettingsFlags.FileHistory);

        FillHistoryMenu();
    }

    [SupportedOSPlatform("windows")]
    private LogWindow.LogWindow FindWindowForFile (string fileName)
    {
        lock (_logWindowList)
        {
            foreach (var logWindow in _logWindowList)
            {
                if (logWindow.FileName.ToUpperInvariant().Equals(fileName.ToUpperInvariant(), StringComparison.Ordinal))
                {
                    return logWindow;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the file name is a settings file. If so, the contained logfile name
    /// is returned. If not, the given file name is returned unchanged.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private string FindFilenameForSettings (string fileName)
    {
        if (fileName.EndsWith(".lxp"))
        {
            var persistenceData = Persister.LoadOptionsOnly(fileName);
            if (persistenceData == null)
            {
                return fileName;
            }

            if (!string.IsNullOrEmpty(persistenceData.FileName))
            {
                var fs = PluginRegistry.PluginRegistry.Instance.FindFileSystemForUri(persistenceData.FileName);
                if (fs != null && !fs.GetType().Equals(typeof(LocalFileSystem)))
                {
                    return persistenceData.FileName;
                }

                // On relative paths the URI check (and therefore the file system plugin check) will fail.
                // So fs == null and fs == LocalFileSystem are handled here like normal files.
                if (Path.IsPathRooted(persistenceData.FileName))
                {
                    return persistenceData.FileName;
                }

                // handle relative paths in .lxp files
                var dir = Path.GetDirectoryName(fileName);
                return Path.Combine(dir, persistenceData.FileName);
            }
        }

        return fileName;
    }

    [SupportedOSPlatform("windows")]
    private void FillHistoryMenu ()
    {
        ToolStripDropDown strip = new ToolStripDropDownMenu();

        foreach (var file in ConfigManager.Settings.FileHistoryList)
        {
            ToolStripItem item = new ToolStripMenuItem(file);
            strip.Items.Add(item);
        }

        strip.ItemClicked += OnHistoryItemClicked;
        strip.MouseUp += OnStripMouseUp;
        lastUsedToolStripMenuItem.DropDown = strip;
    }

    [SupportedOSPlatform("windows")]
    private void RemoveLogWindow (LogWindow.LogWindow logWindow)
    {
        lock (_logWindowList)
        {
            _logWindowList.Remove(logWindow);
        }

        DisconnectEventHandlers(logWindow);
    }

    [SupportedOSPlatform("windows")]
    private void RemoveAndDisposeLogWindow (LogWindow.LogWindow logWindow, bool dontAsk)
    {
        if (CurrentLogWindow == logWindow)
        {
            ChangeCurrentLogWindow(null);
        }

        lock (_logWindowList)
        {
            _logWindowList.Remove(logWindow);
        }

        logWindow.Close(dontAsk);
    }

    [SupportedOSPlatform("windows")]
    private void ShowHighlightSettingsDialog ()
    {
        HighlightDialog dlg = new(ConfigManager)
        {
            KeywordActionList = PluginRegistry.PluginRegistry.Instance.RegisteredKeywordActions,
            Owner = this,
            TopMost = TopMost,
            HighlightGroupList = HighlightGroupList,
            PreSelectedGroupName = groupsComboBoxHighlightGroups.Text
        };

        var res = dlg.ShowDialog();

        if (res == DialogResult.OK)
        {
            HighlightGroupList = dlg.HighlightGroupList;
            FillHighlightComboBox();
            ConfigManager.Settings.Preferences.HighlightGroupList = HighlightGroupList;
            ConfigManager.Save(SettingsFlags.HighlightSettings);
            OnHighlightSettingsChanged();
        }
    }

    [SupportedOSPlatform("windows")]
    private void FillHighlightComboBox ()
    {
        var currentGroupName = groupsComboBoxHighlightGroups.Text;
        groupsComboBoxHighlightGroups.Items.Clear();
        foreach (var group in HighlightGroupList)
        {
            groupsComboBoxHighlightGroups.Items.Add(group.GroupName);
            if (group.GroupName.Equals(currentGroupName, StringComparison.Ordinal))
            {
                groupsComboBoxHighlightGroups.Text = group.GroupName;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OpenFileDialog ()
    {
        OpenFileDialog openFileDialog = new();

        if (CurrentLogWindow != null)
        {
            FileInfo info = new(CurrentLogWindow.FileName);
            openFileDialog.InitialDirectory = info.DirectoryName;
        }
        else
        {
            if (!string.IsNullOrEmpty(ConfigManager.Settings.LastDirectory))
            {
                openFileDialog.InitialDirectory = ConfigManager.Settings.LastDirectory;
            }
            else
            {
                try
                {
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }
                catch (SecurityException e)
                {
                    _logger.Warn(e, "Insufficient rights for GetFolderPath(): ");
                    // no initial directory if insufficient rights
                }
            }
        }

        openFileDialog.Multiselect = true;

        if (DialogResult.OK == openFileDialog.ShowDialog(this))
        {
            FileInfo info = new(openFileDialog.FileName);
            if (info.Directory.Exists)
            {
                ConfigManager.Settings.LastDirectory = info.DirectoryName;
                ConfigManager.Save(SettingsFlags.FileHistory);
            }

            if (info.Exists)
            {
                LoadFiles(openFileDialog.FileNames, false);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void LoadFiles (string[] names, bool invertLogic)
    {
        Array.Sort(names);

        if (names.Length == 1)
        {
            if (names[0].EndsWith(".lxj"))
            {
                LoadProject(names[0], true);
                return;
            }

            AddFileTab(names[0], false, null, false, null);
            return;
        }

        var option = ConfigManager.Settings.Preferences.MultiFileOption;
        if (option == MultiFileOption.Ask)
        {
            MultiLoadRequestDialog dlg = new();
            var res = dlg.ShowDialog();

            if (res == DialogResult.Yes)
            {
                option = MultiFileOption.SingleFiles;
            }
            else if (res == DialogResult.No)
            {
                option = MultiFileOption.MultiFile;
            }
            else
            {
                return;
            }
        }
        else
        {
            if (invertLogic)
            {
                option = option == MultiFileOption.SingleFiles
                    ? MultiFileOption.MultiFile
                    : MultiFileOption.SingleFiles;
            }
        }

        if (option == MultiFileOption.SingleFiles)
        {
            AddFileTabs(names);
        }
        else
        {
            AddMultiFileTab(names);
        }
    }

    private void SetColumnizerHistoryEntry (string fileName, ILogLineColumnizer columnizer)
    {
        var entry = FindColumnizerHistoryEntry(fileName);
        if (entry != null)
        {
            _ = ConfigManager.Settings.ColumnizerHistoryList.Remove(entry);

        }

        ConfigManager.Settings.ColumnizerHistoryList.Add(new ColumnizerHistoryEntry(fileName, columnizer.GetName()));

        if (ConfigManager.Settings.ColumnizerHistoryList.Count > MAX_COLUMNIZER_HISTORY)
        {
            ConfigManager.Settings.ColumnizerHistoryList.RemoveAt(0);
        }
    }

    private ColumnizerHistoryEntry FindColumnizerHistoryEntry (string fileName)
    {
        foreach (var entry in ConfigManager.Settings.ColumnizerHistoryList)
        {
            if (entry.FileName.Equals(fileName, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    [SupportedOSPlatform("windows")]
    private void ToggleMultiFile ()
    {
        if (CurrentLogWindow != null)
        {
            CurrentLogWindow.SwitchMultiFile(!CurrentLogWindow.IsMultiFile);
            multiFileToolStripMenuItem.Checked = CurrentLogWindow.IsMultiFile;
            multiFileEnabledStripMenuItem.Checked = CurrentLogWindow.IsMultiFile;
        }
    }

    [SupportedOSPlatform("windows")]
    private void ChangeCurrentLogWindow (LogWindow.LogWindow newLogWindow)
    {
        if (newLogWindow == _currentLogWindow)
        {
            return; // do nothing if wishing to set the same window
        }

        var oldLogWindow = _currentLogWindow;
        _currentLogWindow = newLogWindow;
        var titleName = _showInstanceNumbers ? "LogExpert #" + _instanceNumber : "LogExpert";

        if (oldLogWindow != null)
        {
            oldLogWindow.StatusLineEvent -= OnStatusLineEvent;
            oldLogWindow.ProgressBarUpdate -= OnProgressBarUpdate;
            oldLogWindow.GuiStateUpdate -= OnGuiStateUpdate;
            oldLogWindow.ColumnizerChanged -= OnColumnizerChanged;
            oldLogWindow.BookmarkAdded -= OnBookmarkAdded;
            oldLogWindow.BookmarkRemoved -= OnBookmarkRemoved;
            oldLogWindow.BookmarkTextChanged -= OnBookmarkTextChanged;
            DisconnectToolWindows(oldLogWindow);
        }

        if (newLogWindow != null)
        {
            newLogWindow.StatusLineEvent += OnStatusLineEvent;
            newLogWindow.ProgressBarUpdate += OnProgressBarUpdate;
            newLogWindow.GuiStateUpdate += OnGuiStateUpdate;
            newLogWindow.ColumnizerChanged += OnColumnizerChanged;
            newLogWindow.BookmarkAdded += OnBookmarkAdded;
            newLogWindow.BookmarkRemoved += OnBookmarkRemoved;
            newLogWindow.BookmarkTextChanged += OnBookmarkTextChanged;

            Text = newLogWindow.IsTempFile
                ? titleName + @" - " + newLogWindow.TempTitleName
                : titleName + @" - " + newLogWindow.FileName;

            multiFileToolStripMenuItem.Checked = CurrentLogWindow.IsMultiFile;
            multiFileToolStripMenuItem.Enabled = true;
            multiFileEnabledStripMenuItem.Checked = CurrentLogWindow.IsMultiFile;
            cellSelectModeToolStripMenuItem.Checked = true;
            cellSelectModeToolStripMenuItem.Enabled = true;
            closeFileToolStripMenuItem.Enabled = true;
            searchToolStripMenuItem.Enabled = true;
            filterToolStripMenuItem.Enabled = true;
            goToLineToolStripMenuItem.Enabled = true;
            //ConnectToolWindows(newLogWindow);
        }
        else
        {
            Text = titleName;
            multiFileToolStripMenuItem.Checked = false;
            multiFileEnabledStripMenuItem.Checked = false;
            checkBoxFollowTail.Checked = false;
            mainMenuStrip.Enabled = true;
            timeshiftToolStripMenuItem.Enabled = false;
            timeshiftToolStripMenuItem.Checked = false;
            timeshiftMenuTextBox.Text = "";
            timeshiftMenuTextBox.Enabled = false;
            multiFileToolStripMenuItem.Enabled = false;
            cellSelectModeToolStripMenuItem.Checked = false;
            cellSelectModeToolStripMenuItem.Enabled = false;
            closeFileToolStripMenuItem.Enabled = false;
            searchToolStripMenuItem.Enabled = false;
            filterToolStripMenuItem.Enabled = false;
            goToLineToolStripMenuItem.Enabled = false;
            dragControlDateTime.Visible = false;
        }
    }

    private void ConnectToolWindows (LogWindow.LogWindow logWindow)
    {
        ConnectBookmarkWindow(logWindow);
    }

    private void ConnectBookmarkWindow (LogWindow.LogWindow logWindow)
    {
        FileViewContext ctx = new(logWindow, logWindow);
        _bookmarkWindow.SetBookmarkData(logWindow.BookmarkData);
        _bookmarkWindow.SetCurrentFile(ctx);
    }

    private void DisconnectToolWindows (LogWindow.LogWindow logWindow)
    {
        DisconnectBookmarkWindow(logWindow);
    }

    private void DisconnectBookmarkWindow (LogWindow.LogWindow logWindow)
    {
        _bookmarkWindow.SetBookmarkData(null);
        _bookmarkWindow.SetCurrentFile(null);
    }

    [SupportedOSPlatform("windows")]
    private void GuiStateUpdateWorker (GuiStateArgs e)
    {
        _skipEvents = true;
        checkBoxFollowTail.Checked = e.FollowTail;
        mainMenuStrip.Enabled = e.MenuEnabled;
        timeshiftToolStripMenuItem.Enabled = e.TimeshiftPossible;
        timeshiftToolStripMenuItem.Checked = e.TimeshiftEnabled;
        timeshiftMenuTextBox.Text = e.TimeshiftText;
        timeshiftMenuTextBox.Enabled = e.TimeshiftEnabled;
        multiFileToolStripMenuItem.Enabled = e.MultiFileEnabled; // disabled for temp files
        multiFileToolStripMenuItem.Checked = e.IsMultiFileActive;
        multiFileEnabledStripMenuItem.Checked = e.IsMultiFileActive;
        cellSelectModeToolStripMenuItem.Checked = e.CellSelectMode;
        RefreshEncodingMenuBar(e.CurrentEncoding);

        if (e.TimeshiftPossible && ConfigManager.Settings.Preferences.TimestampControl)
        {
            dragControlDateTime.MinDateTime = e.MinTimestamp;
            dragControlDateTime.MaxDateTime = e.MaxTimestamp;
            dragControlDateTime.DateTime = e.Timestamp;
            dragControlDateTime.Visible = true;
            dragControlDateTime.Enabled = true;
            dragControlDateTime.Refresh();
        }
        else
        {
            dragControlDateTime.Visible = false;
            dragControlDateTime.Enabled = false;
        }

        toolStripButtonBubbles.Checked = e.ShowBookmarkBubbles;
        groupsComboBoxHighlightGroups.Text = e.HighlightGroupName;
        columnFinderToolStripMenuItem.Checked = e.ColumnFinderVisible;

        _skipEvents = false;
    }

    [SupportedOSPlatform("windows")]
    private void ProgressBarUpdateWorker (ProgressEventArgs e)
    {
        if (e.Value <= e.MaxValue && e.Value >= e.MinValue)
        {
            try
            {
                loadProgessBar.Minimum = e.MinValue;
                loadProgessBar.Maximum = e.MaxValue;
                loadProgessBar.Value = e.Value;
                loadProgessBar.Visible = e.Visible;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during ProgressBarUpdateWorker value {0}, min {1}, max {2}, visible {3}", e.Value, e.MinValue, e.MaxValue, e.Visible);
            }

            Invoke(new System.Windows.Forms.MethodInvoker(statusStrip.Refresh));
        }
    }

    [SupportedOSPlatform("windows")]
    //TODO Crossthread Exception when a log file has been filtered to a new tab!
    private void StatusLineEventWorker (StatusLineEventArgs e)
    {
        if (e != null)
        {
            //_logger.logDebug("StatusLineEvent: text = " + e.StatusText);
            labelStatus.Text = e.StatusText;
            labelStatus.Size = TextRenderer.MeasureText(labelStatus.Text, labelStatus.Font);
            labelLines.Text = $" {e.LineCount} lines";
            labelLines.Size = TextRenderer.MeasureText(labelLines.Text, labelLines.Font);
            labelSize.Text = Util.GetFileSizeAsText(e.FileSize);
            labelSize.Size = TextRenderer.MeasureText(labelSize.Text, labelSize.Font);
            labelCurrentLine.Text = $"Line: {e.CurrentLineNum}";
            labelCurrentLine.Size = TextRenderer.MeasureText(labelCurrentLine.Text, labelCurrentLine.Font);
            if (statusStrip.InvokeRequired)
            {
                statusStrip.BeginInvoke(new System.Windows.Forms.MethodInvoker(statusStrip.Refresh));
            }
            else
            {
                statusStrip.Refresh();
            }
        }
    }

    // tailState: 0,1,2 = on/off/off by Trigger
    // syncMode: 0 = normal (no), 1 = time synced
    [SupportedOSPlatform("windows")]
    private Icon CreateLedIcon (int level, bool dirty, int tailState, int syncMode)
    {
        var iconRect = _leds[0];
        iconRect.Height = 16; // (DockPanel's damn hardcoded height) // this.leds[this.leds.Length - 1].Bottom;
        iconRect.Width = iconRect.Right + 6;
        Bitmap bmp = new(iconRect.Width, iconRect.Height);
        var gfx = Graphics.FromImage(bmp);

        var offsetFromTop = 4;

        for (var i = 0; i < _leds.Length; ++i)
        {
            var ledRect = _leds[i];
            ledRect.Offset(0, offsetFromTop);

            if (level >= _leds.Length - i)
            {
                gfx.FillRectangle(_ledBrushes[i], ledRect);
            }
            else
            {
                gfx.FillRectangle(_offLedBrush, ledRect);
            }
        }

        var ledSize = 3;
        var ledGap = 1;
        var lastLed = _leds[^1];
        Rectangle dirtyLed = new(lastLed.Right + 2, lastLed.Bottom - ledSize, ledSize, ledSize);
        Rectangle tailLed = new(dirtyLed.Location, dirtyLed.Size);
        tailLed.Offset(0, -(ledSize + ledGap));
        Rectangle syncLed = new(tailLed.Location, dirtyLed.Size);
        syncLed.Offset(0, -(ledSize + ledGap));

        syncLed.Offset(0, offsetFromTop);
        tailLed.Offset(0, offsetFromTop);
        dirtyLed.Offset(0, offsetFromTop);

        if (dirty)
        {
            gfx.FillRectangle(_dirtyLedBrush, dirtyLed);
        }
        else
        {
            gfx.FillRectangle(_offLedBrush, dirtyLed);
        }

        // tailMode 4 means: don't show
        if (tailState < 3)
        {
            gfx.FillRectangle(_tailLedBrush[tailState], tailLed);
        }

        if (syncMode == 1)
        {
            gfx.FillRectangle(_syncLedBrush, syncLed);
        }
        //else
        //{
        //  gfx.FillRectangle(this.offLedBrush, syncLed);
        //}

        // see http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=345656
        // GetHicon() creates an unmanaged handle which must be destroyed. The Clone() workaround creates
        // a managed copy of icon. then the unmanaged win32 handle is destroyed
        var iconHandle = bmp.GetHicon();
        var icon = Icon.FromHandle(iconHandle).Clone() as Icon;
        NativeMethods.DestroyIcon(iconHandle);

        gfx.Dispose();
        bmp.Dispose();
        return icon;
    }

    [SupportedOSPlatform("windows")]
    private void CreateIcons ()
    {
        for (var syncMode = 0; syncMode <= 1; syncMode++) // LED indicating time synced tabs
        {
            for (var tailMode = 0; tailMode < 4; tailMode++)
            {
                for (var i = 0; i < 6; ++i)
                {
                    _ledIcons[i, 0, tailMode, syncMode] = CreateLedIcon(i, false, tailMode, syncMode);
                }

                for (var i = 0; i < 6; ++i)
                {
                    _ledIcons[i, 1, tailMode, syncMode] = CreateLedIcon(i, true, tailMode, syncMode);
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void FileNotFound (LogWindow.LogWindow logWin)
    {
        var data = logWin.Tag as LogWindowData;
        BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWin, _deadIcon);
        dragControlDateTime.Visible = false;
    }

    [SupportedOSPlatform("windows")]
    private void FileRespawned (LogWindow.LogWindow logWin)
    {
        var data = logWin.Tag as LogWindowData;
        var icon = GetIcon(0, data);
        BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWin, icon);
    }

    [SupportedOSPlatform("windows")]
    private void ShowLedPeak (LogWindow.LogWindow logWin)
    {
        var data = logWin.Tag as LogWindowData;
        lock (data)
        {
            data.DiffSum = DIFF_MAX;
        }

        var icon = GetIcon(data.DiffSum, data);
        BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWin, icon);
    }

    private int GetLevelFromDiff (int diff)
    {
        if (diff > 60)
        {
            diff = 60;
        }

        var level = diff / 10;
        if (diff > 0 && level == 0)
        {
            level = 2;
        }
        else if (level == 0)
        {
            level = 1;
        }

        return level - 1;
    }

    [SupportedOSPlatform("windows")]
    private void LedThreadProc ()
    {
        Thread.CurrentThread.Name = "LED Thread";
        while (!_shouldStop)
        {
            try
            {
                Thread.Sleep(200);
            }
            catch
            {
                return;
            }

            lock (_logWindowList)
            {
                foreach (var logWindow in _logWindowList)
                {
                    var data = logWindow.Tag as LogWindowData;
                    if (data.DiffSum > 0)
                    {
                        data.DiffSum -= 10;
                        if (data.DiffSum < 0)
                        {
                            data.DiffSum = 0;
                        }

                        var icon = GetIcon(data.DiffSum, data);
                        BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWindow, icon);
                    }
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void SetTabIcon (LogWindow.LogWindow logWindow, Icon icon)
    {
        if (logWindow != null)
        {
            logWindow.Icon = icon;
            logWindow.DockHandler.Pane?.TabStripControl.Invalidate(false);
        }
    }

    private Icon GetIcon (int diff, LogWindowData data)
    {
        var icon =
            _ledIcons[
                GetLevelFromDiff(diff), data.Dirty ? 1 : 0, Preferences.ShowTailState ? data.TailState : 3,
                data.SyncMode
            ];
        return icon;
    }

    [SupportedOSPlatform("windows")]
    private void RefreshEncodingMenuBar (Encoding encoding)
    {
        toolStripEncodingASCIIItem.Checked = false;
        toolStripEncodingANSIItem.Checked = false;
        toolStripEncodingUTF8Item.Checked = false;
        toolStripEncodingUTF16Item.Checked = false;
        toolStripEncodingISO88591Item.Checked = false;

        if (encoding == null)
        {
            return;
        }

        if (encoding is ASCIIEncoding)
        {
            toolStripEncodingASCIIItem.Checked = true;
        }
        else if (encoding.Equals(Encoding.Default))
        {
            toolStripEncodingANSIItem.Checked = true;
        }
        else if (encoding is UTF8Encoding)
        {
            toolStripEncodingUTF8Item.Checked = true;
        }
        else if (encoding is UnicodeEncoding)
        {
            toolStripEncodingUTF16Item.Checked = true;
        }
        else if (encoding.Equals(Encoding.GetEncoding("iso-8859-1")))
        {
            toolStripEncodingISO88591Item.Checked = true;
        }

        toolStripEncodingANSIItem.Text = Encoding.Default.HeaderName;
    }

    [SupportedOSPlatform("windows")]
    private void OpenSettings (int tabToOpen)
    {
        SettingsDialog dlg = new(ConfigManager.Settings.Preferences, this, tabToOpen, ConfigManager)
        {
            TopMost = TopMost
        };

        if (DialogResult.OK == dlg.ShowDialog())
        {
            ConfigManager.Settings.Preferences = dlg.Preferences;
            ConfigManager.Save(SettingsFlags.Settings);
            NotifyWindowsForChangedPrefs(SettingsFlags.Settings);
        }
    }

    [SupportedOSPlatform("windows")]
    private void NotifyWindowsForChangedPrefs (SettingsFlags flags)
    {
        _logger.Info(CultureInfo.InvariantCulture, "The preferences have changed");
        ApplySettings(ConfigManager.Settings, flags);

        var setLastColumnWidth = ConfigManager.Settings.Preferences.SetLastColumnWidth;
        var lastColumnWidth = ConfigManager.Settings.Preferences.LastColumnWidth;
        var fontName = ConfigManager.Settings.Preferences.FontName;
        var fontSize = ConfigManager.Settings.Preferences.FontSize;

        lock (_logWindowList)
        {
            foreach (var logWindow in _logWindowList)
            {
                logWindow.PreferencesChanged(fontName, fontSize, setLastColumnWidth, lastColumnWidth, false, flags);
            }
        }

        _bookmarkWindow.PreferencesChanged(fontName, fontSize, setLastColumnWidth, lastColumnWidth, flags);

        HighlightGroupList = ConfigManager.Settings.Preferences.HighlightGroupList;
        if ((flags & SettingsFlags.HighlightSettings) == SettingsFlags.HighlightSettings)
        {
            OnHighlightSettingsChanged();
        }
    }

    [SupportedOSPlatform("windows")]
    private void ApplySettings (Settings settings, SettingsFlags flags)
    {
        if ((flags & SettingsFlags.WindowPosition) == SettingsFlags.WindowPosition)
        {
            TopMost = alwaysOnTopToolStripMenuItem.Checked = settings.AlwaysOnTop;
            dragControlDateTime.DragOrientation = settings.Preferences.TimestampControlDragOrientation;
            hideLineColumnToolStripMenuItem.Checked = settings.HideLineColumn;
        }

        if ((flags & SettingsFlags.FileHistory) == SettingsFlags.FileHistory)
        {
            FillHistoryMenu();
        }

        if ((flags & SettingsFlags.GuiOrColors) == SettingsFlags.GuiOrColors)
        {
            SetTabIcons(settings.Preferences);
        }

        if ((flags & SettingsFlags.ToolSettings) == SettingsFlags.ToolSettings)
        {
            FillToolLauncherBar();
        }

        if ((flags & SettingsFlags.HighlightSettings) == SettingsFlags.HighlightSettings)
        {
            FillHighlightComboBox();
        }
    }

    [SupportedOSPlatform("windows")]
    private void SetTabIcons (Preferences preferences)
    {
        _tailLedBrush[0] = new SolidBrush(preferences.ShowTailColor);
        CreateIcons();
        lock (_logWindowList)
        {
            foreach (var logWindow in _logWindowList)
            {
                var data = logWindow.Tag as LogWindowData;
                var icon = GetIcon(data.DiffSum, data);
                BeginInvoke(new SetTabIconDelegate(SetTabIcon), logWindow, icon);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void SetToolIcon (ToolEntry entry, ToolStripItem item)
    {
        var icon = NativeMethods.LoadIconFromExe(entry.IconFile, entry.IconIndex);

        if (icon != null)
        {
            item.Image = icon.ToBitmap();

            item.DisplayStyle = item is ToolStripMenuItem
                ? ToolStripItemDisplayStyle.ImageAndText
                : ToolStripItemDisplayStyle.Image;

            NativeMethods.DestroyIcon(icon.Handle);
            icon.Dispose();
        }

        if (!string.IsNullOrEmpty(entry.Cmd))
        {
            item.ToolTipText = entry.Name;
        }
    }

    [SupportedOSPlatform("windows")]
    private void ToolButtonClick (ToolEntry toolEntry)
    {
        if (string.IsNullOrEmpty(toolEntry.Cmd))
        {
            //TODO TabIndex => To Enum
            OpenSettings(2);
            return;
        }

        if (CurrentLogWindow != null)
        {
            var line = CurrentLogWindow.GetCurrentLine();
            var info = CurrentLogWindow.GetCurrentFileInfo();
            if (line != null && info != null)
            {
                ArgParser parser = new(toolEntry.Args);
                var argLine = parser.BuildArgs(line, CurrentLogWindow.GetRealLineNum() + 1, info, this);
                if (argLine != null)
                {
                    StartTool(toolEntry.Cmd, argLine, toolEntry.Sysout, toolEntry.ColumnizerName, toolEntry.WorkingDir);
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void StartTool (string cmd, string args, bool sysoutPipe, string columnizerName, string workingDir)
    {
        if (string.IsNullOrEmpty(cmd))
        {
            return;
        }

        Process process = new();
        ProcessStartInfo startInfo = new(cmd, args);
        if (!Util.IsNull(workingDir))
        {
            startInfo.WorkingDirectory = workingDir;
        }

        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;

        if (sysoutPipe)
        {
            var columnizer = ColumnizerPicker.DecideColumnizerByName(columnizerName, PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

            _logger.Info(CultureInfo.InvariantCulture, "Starting external tool with sysout redirection: {0} {1}", cmd, args);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            //process.OutputDataReceived += pipe.DataReceivedEventHandler;
            try
            {
                _ = process.Start();
            }
            catch (Win32Exception e)
            {
                _logger.Error(e);
                MessageBox.Show(e.Message);
                return;
            }

            SysoutPipe pipe = new(process.StandardOutput);

            var logWin = AddTempFileTab(pipe.FileName,
                CurrentLogWindow.IsTempFile
                    ? CurrentLogWindow.TempTitleName
                    : Util.GetNameFromPath(CurrentLogWindow.FileName) + "->E");
            logWin.ForceColumnizer(columnizer);

            process.Exited += pipe.ProcessExitedEventHandler;
            //process.BeginOutputReadLine();
        }
        else
        {
            _logger.Info(CultureInfo.InvariantCulture, "Starting external tool: {0} {1}", cmd, args);

            try
            {
                startInfo.UseShellExecute = false;
                _ = process.Start();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                MessageBox.Show(e.Message);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void CloseAllTabs ()
    {
        IList<Form> closeList = [];
        lock (_logWindowList)
        {
            foreach (DockContent content in dockPanel.Contents)
            {
                if (content is LogWindow.LogWindow window)
                {
                    closeList.Add(window);
                }
            }
        }

        foreach (var form in closeList)
        {
            form.Close();
        }
    }

    //TODO Reimplement
    private void SetTabColor (LogWindow.LogWindow logWindow, Color color)
    {
        //tabPage.BackLowColor = color;
        //tabPage.BackLowColorDisabled = Color.FromArgb(255,
        //  Math.Max(0, color.R - 50),
        //  Math.Max(0, color.G - 50),
        //  Math.Max(0, color.B - 50)
        //  );
    }

    [SupportedOSPlatform("windows")]
    private void LoadProject (string projectFileName, bool restoreLayout)
    {
        var projectData = ProjectPersister.LoadProjectData(projectFileName);
        var hasLayoutData = projectData.TabLayoutXml != null;

        if (hasLayoutData && restoreLayout && _logWindowList.Count > 0)
        {
            ProjectLoadDlg dlg = new();
            if (DialogResult.Cancel != dlg.ShowDialog())
            {
                switch (dlg.ProjectLoadResult)
                {
                    case ProjectLoadDlgResult.IgnoreLayout:
                        hasLayoutData = false;
                        break;
                    case ProjectLoadDlgResult.CloseTabs:
                        CloseAllTabs();
                        break;
                    case ProjectLoadDlgResult.NewWindow:
                        LogExpertProxy.NewWindow([projectFileName]);
                        return;
                }
            }
        }

        if (projectData != null)
        {
            foreach (var fileName in projectData.MemberList)
            {
                if (hasLayoutData)
                {
                    AddFileTabDeferred(fileName, false, null, true, null);
                }
                else
                {
                    AddFileTab(fileName, false, null, true, null);
                }
            }

            if (hasLayoutData && restoreLayout)
            {
                // Re-creating tool (non-document) windows is needed because the DockPanel control would throw strange errors
                DestroyToolWindows();
                InitToolWindows();
                RestoreLayout(projectData.TabLayoutXml);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void ApplySelectedHighlightGroup ()
    {
        var groupName = groupsComboBoxHighlightGroups.Text;
        CurrentLogWindow?.SetCurrentHighlightGroup(groupName);
    }

    [SupportedOSPlatform("windows")]
    private void FillToolLauncherBar ()
    {
        char[] labels =
        [
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
            'U', 'V', 'W', 'X', 'Y', 'Z'
        ];
        toolsToolStripMenuItem.DropDownItems.Clear();
        toolsToolStripMenuItem.DropDownItems.Add(configureToolStripMenuItem);
        toolsToolStripMenuItem.DropDownItems.Add(configureToolStripSeparator);
        externalToolsToolStrip.Items.Clear();
        var num = 0;
        externalToolsToolStrip.SuspendLayout();
        foreach (var tool in Preferences.ToolEntries)
        {
            if (tool.IsFavourite)
            {
                ToolStripButton button = new("" + labels[num % 26])
                {
                    Alignment = ToolStripItemAlignment.Left,
                    Tag = tool
                };

                SetToolIcon(tool, button);
                externalToolsToolStrip.Items.Add(button);
            }

            num++;
            ToolStripMenuItem menuItem = new(tool.Name)
            {
                Tag = tool
            };

            SetToolIcon(tool, menuItem);
            toolsToolStripMenuItem.DropDownItems.Add(menuItem);
        }

        externalToolsToolStrip.ResumeLayout();

        externalToolsToolStrip.Visible = num > 0; // do not show bar if no tool uses it
    }

    private void RunGC ()
    {
        _logger.Info($"Running GC. Used mem before: {GC.GetTotalMemory(false):N0}");
        GC.Collect();
        _logger.Info($"GC done.    Used mem after:  {GC.GetTotalMemory(true):N0}");
    }

    private void DumpGCInfo ()
    {
        _logger.Info($"-------- GC info -----------\r\nUsed mem: {GC.GetTotalMemory(false):N0}");
        for (var i = 0; i < GC.MaxGeneration; ++i)
        {
            _logger.Info($"Generation {i} collect count: {GC.CollectionCount(i)}");
        }

        _logger.Info(CultureInfo.InvariantCulture, "----------------------------");
    }

    private void ThrowExceptionFx ()
    {
        throw new Exception("This is a test exception thrown by an async delegate");
    }

    private void ThrowExceptionThreadFx ()
    {
        throw new Exception("This is a test exception thrown by a background thread");
    }

    private string SaveLayout ()
    {
        using MemoryStream memStream = new(2000);
        using StreamReader r = new(memStream);
        dockPanel.SaveAsXml(memStream, Encoding.UTF8, true);

        memStream.Seek(0, SeekOrigin.Begin);
        var resultXml = r.ReadToEnd();

        r.Close();

        return resultXml;
    }

    [SupportedOSPlatform("windows")]
    private void RestoreLayout (string layoutXml)
    {
        using MemoryStream memStream = new(2000);
        using StreamWriter w = new(memStream);
        w.Write(layoutXml);
        w.Flush();

        memStream.Seek(0, SeekOrigin.Begin);

        dockPanel.LoadFromXml(memStream, DeserializeDockContent, true);
    }

    [SupportedOSPlatform("windows")]
    private IDockContent DeserializeDockContent (string persistString)
    {
        if (persistString.Equals(WindowTypes.BookmarkWindow.ToString(), StringComparison.Ordinal))
        {
            return _bookmarkWindow;
        }

        if (persistString.StartsWith(WindowTypes.LogWindow.ToString()))
        {
            var fileName = persistString[(WindowTypes.LogWindow.ToString().Length + 1)..];
            var win = FindWindowForFile(fileName);
            if (win != null)
            {
                return win;
            }

            _logger.Warn($"Layout data contains non-existing LogWindow for {fileName}");
        }

        return null;
    }

    private void OnHighlightSettingsChanged ()
    {
        HighlightSettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Events handler

    private void OnBookmarkWindowVisibleChanged (object sender, EventArgs e)
    {
        _firstBookmarkWindowShow = false;
    }

    private void OnLogTabWindowLoad (object sender, EventArgs e)
    {
        ApplySettings(ConfigManager.Settings, SettingsFlags.All);
        if (ConfigManager.Settings.IsMaximized)
        {
            Bounds = ConfigManager.Settings.AppBoundsFullscreen;
            WindowState = FormWindowState.Maximized;
            Bounds = ConfigManager.Settings.AppBounds;
        }
        else
        {
            if (ConfigManager.Settings.AppBounds.Right > 0)
            {
                Bounds = ConfigManager.Settings.AppBounds;
            }
        }

        if (ConfigManager.Settings.Preferences.OpenLastFiles && _startupFileNames == null)
        {
            var tmpList = ObjectClone.Clone(ConfigManager.Settings.LastOpenFilesList);

            foreach (var name in tmpList)
            {
                if (string.IsNullOrEmpty(name) == false)
                {
                    AddFileTab(name, false, null, false, null);
                }
            }
        }

        if (_startupFileNames != null)
        {
            LoadFiles(_startupFileNames, false);
        }

        _ledThread = new Thread(LedThreadProc)
        {
            IsBackground = true
        };
        _ledThread.Start();

        FillHighlightComboBox();
        FillToolLauncherBar();
#if !DEBUG
        debugToolStripMenuItem.Visible = false;
#endif
    }

    private void OnLogTabWindowClosing (object sender, CancelEventArgs e)
    {
        try
        {
            _shouldStop = true;
            _ = _statusLineEventHandle.Set();
            _ = _statusLineEventWakeupHandle.Set();
            _ledThread.Join();

            IList<LogWindow.LogWindow> deleteLogWindowList = [];
            ConfigManager.Settings.AlwaysOnTop = TopMost && ConfigManager.Settings.Preferences.AllowOnlyOneInstance;
            SaveLastOpenFilesList();

            foreach (var logWindow in _logWindowList.ToArray())
            {
                RemoveAndDisposeLogWindow(logWindow, true);
            }

            DestroyBookmarkWindow();

            ConfigManager.Instance.ConfigChanged -= OnConfigChanged;

            SaveWindowPosition();
            ConfigManager.Save(SettingsFlags.WindowPosition | SettingsFlags.FileHistory);
        }
        catch (Exception)
        {
            // ignore error (can occur then multipe instances are closed simultaneously or if the
            // window was not constructed completely because of errors)
        }
        finally
        {
            LogExpertProxy?.WindowClosed(this);
        }
    }

    private void OnStripMouseUp (object sender, MouseEventArgs e)
    {
        if (sender is ToolStripDropDown dropDown)
        {
            AddFileTab(dropDown.Text, false, null, false, null);
        }
    }

    private void OnHistoryItemClicked (object sender, ToolStripItemClickedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.ClickedItem.Text) == false)
        {
            AddFileTab(e.ClickedItem.Text, false, null, false, null);
        }
    }

    private void OnLogWindowDisposed (object sender, EventArgs e)
    {
        var logWindow = sender as LogWindow.LogWindow;

        if (sender == CurrentLogWindow)
        {
            ChangeCurrentLogWindow(null);
        }

        RemoveLogWindow(logWindow);

        logWindow.Tag = null;
    }

    private void OnExitToolStripMenuItemClick (object sender, EventArgs e)
    {
        Close();
    }

    private void OnSelectFilterToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (CurrentLogWindow == null)
        {
            return;
        }

        CurrentLogWindow.ColumnizerCallbackObject.LineNum = CurrentLogWindow.CurrentLineNum;
        FilterSelectorForm form = new(PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers, CurrentLogWindow.CurrentColumnizer, CurrentLogWindow.ColumnizerCallbackObject, ConfigManager)
        {
            Owner = this,
            TopMost = TopMost
        };
        var res = form.ShowDialog();

        if (res == DialogResult.OK)
        {
            if (form.ApplyToAll)
            {
                lock (_logWindowList)
                {
                    foreach (var logWindow in _logWindowList)
                    {
                        if (logWindow.CurrentColumnizer.GetType() != form.SelectedColumnizer.GetType())
                        {
                            //logWindow.SetColumnizer(form.SelectedColumnizer);
                            SetColumnizerFx fx = logWindow.ForceColumnizer;
                            logWindow.Invoke(fx, form.SelectedColumnizer);
                            SetColumnizerHistoryEntry(logWindow.FileName, form.SelectedColumnizer);
                        }
                        else
                        {
                            if (form.IsConfigPressed)
                            {
                                logWindow.ColumnizerConfigChanged();
                            }
                        }
                    }
                }
            }
            else
            {
                if (CurrentLogWindow.CurrentColumnizer.GetType() != form.SelectedColumnizer.GetType())
                {
                    SetColumnizerFx fx = CurrentLogWindow.ForceColumnizer;
                    CurrentLogWindow.Invoke(fx, form.SelectedColumnizer);
                    SetColumnizerHistoryEntry(CurrentLogWindow.FileName, form.SelectedColumnizer);
                }

                if (form.IsConfigPressed)
                {
                    lock (_logWindowList)
                    {
                        foreach (var logWindow in _logWindowList)
                        {
                            if (logWindow.CurrentColumnizer.GetType() == form.SelectedColumnizer.GetType())
                            {
                                logWindow.ColumnizerConfigChanged();
                            }
                        }
                    }
                }
            }
        }
    }

    private void OnGoToLineToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (CurrentLogWindow == null)
        {
            return;
        }

        GotoLineDialog dlg = new(this);
        var res = dlg.ShowDialog();
        if (res == DialogResult.OK)
        {
            var line = dlg.Line - 1;
            if (line >= 0)
            {
                CurrentLogWindow.GotoLine(line);
            }
        }
    }

    private void OnHighlightingToolStripMenuItemClick (object sender, EventArgs e)
    {
        ShowHighlightSettingsDialog();
    }

    private void OnSearchToolStripMenuItemClick (object sender, EventArgs e)
    {
        OpenSearchDialog();
    }

    private void OnOpenToolStripMenuItemClick (object sender, EventArgs e)
    {
        OpenFileDialog();
    }

    private void OnLogTabWindowDragEnter (object sender, DragEventArgs e)
    {
#if DEBUG
        var formats = e.Data.GetFormats();
        var s = "Dragging something over LogExpert. Formats:  ";
        foreach (var format in formats)
        {
            s += format;
            s += " , ";
        }

        s = s[..^3];
        _logger.Info(s);
#endif
    }

    private void OnLogWindowDragOver (object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.None;
        }
        else
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void OnLogWindowDragDrop (object sender, DragEventArgs e)
    {
#if DEBUG
        var formats = e.Data.GetFormats();
        var s = "Dropped formats:  ";
        foreach (var format in formats)
        {
            s += format;
            s += " , ";
        }

        s = s[..^3];
        _logger.Debug(s);
#endif

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var o = e.Data.GetData(DataFormats.FileDrop);
            if (o is string[] names)
            {
                // (shift pressed) https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.drageventargs.keystate
                LoadFiles(names, (e.KeyState & 4) == 4);
                e.Effect = DragDropEffects.Copy;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnTimeShiftToolStripMenuItemCheckStateChanged (object sender, EventArgs e)
    {
        if (!_skipEvents && CurrentLogWindow != null)
        {
            CurrentLogWindow.SetTimeshiftValue(timeshiftMenuTextBox.Text);
            timeshiftMenuTextBox.Enabled = timeshiftToolStripMenuItem.Checked;
            CurrentLogWindow.TimeshiftEnabled(timeshiftToolStripMenuItem.Checked,
                timeshiftMenuTextBox.Text);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnAboutToolStripMenuItemClick (object sender, EventArgs e)
    {
        AboutBox aboutBox = new()
        {
            TopMost = TopMost
        };

        aboutBox.ShowDialog();
    }

    private void OnFilterToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.ToggleFilterPanel();
    }

    [SupportedOSPlatform("windows")]
    private void OnMultiFileToolStripMenuItemClick (object sender, EventArgs e)
    {
        ToggleMultiFile();
        fileToolStripMenuItem.HideDropDown();
    }

    [SupportedOSPlatform("windows")]
    private void OnGuiStateUpdate (object sender, GuiStateArgs e)
    {
        BeginInvoke(GuiStateUpdateWorker, e);
    }

    private void OnColumnizerChanged (object sender, ColumnizerEventArgs e)
    {
        _bookmarkWindow?.SetColumnizer(e.Columnizer);
    }

    private void OnBookmarkAdded (object sender, EventArgs e)
    {
        _bookmarkWindow.UpdateView();
    }

    private void OnBookmarkTextChanged (object sender, BookmarkEventArgs e)
    {
        _bookmarkWindow.BookmarkTextChanged(e.Bookmark);
    }

    private void OnBookmarkRemoved (object sender, EventArgs e)
    {
        _bookmarkWindow.UpdateView();
    }

    private void OnProgressBarUpdate (object sender, ProgressEventArgs e)
    {
        Invoke(ProgressBarUpdateWorker, e);
    }

    private void OnStatusLineEvent (object sender, StatusLineEventArgs e)
    {
        StatusLineEventWorker(e);
    }

    private void OnFollowTailCheckBoxClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.FollowTailChanged(checkBoxFollowTail.Checked, false);
    }

    private void OnLogTabWindowKeyDown (object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.W && e.Control)
        {
            CurrentLogWindow?.Close();
        }
        else if (e.KeyCode == Keys.Tab && e.Control)
        {
            SwitchTab(e.Shift);
        }
        else
        {
            CurrentLogWindow?.OnLogWindowKeyDown(sender, e);
        }
    }

    private void OnCloseFileToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.Close();
    }

    [SupportedOSPlatform("windows")]
    private void OnCellSelectModeToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.SetCellSelectionMode(cellSelectModeToolStripMenuItem.Checked);
    }

    private void OnCopyMarkedLinesIntoNewTabToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.CopyMarkedLinesToTab();
    }

    private void OnTimeShiftMenuTextBoxKeyDown (object sender, KeyEventArgs e)
    {
        if (CurrentLogWindow == null)
        {
            return;
        }

        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            CurrentLogWindow.SetTimeshiftValue(timeshiftMenuTextBox.Text);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnAlwaysOnTopToolStripMenuItemClick (object sender, EventArgs e)
    {
        TopMost = alwaysOnTopToolStripMenuItem.Checked;
    }

    private void OnFileSizeChanged (object sender, LogEventArgs e)
    {
        if (sender.GetType().IsAssignableFrom(typeof(LogWindow.LogWindow)))
        {
            var diff = e.LineCount - e.PrevLineCount;
            if (diff < 0)
            {
                return;
            }

            if (((LogWindow.LogWindow)sender).Tag is LogWindowData data)
            {
                lock (data)
                {
                    data.DiffSum += diff;
                    if (data.DiffSum > DIFF_MAX)
                    {
                        data.DiffSum = DIFF_MAX;
                    }
                }

                //if (this.dockPanel.ActiveContent != null &&
                //    this.dockPanel.ActiveContent != sender || data.tailState != 0)
                if (CurrentLogWindow != null &&
                    CurrentLogWindow != sender || data.TailState != 0)
                {
                    data.Dirty = true;
                }
                var icon = GetIcon(diff, data);
                BeginInvoke(new SetTabIconDelegate(SetTabIcon), (LogWindow.LogWindow)sender, icon);
            }
        }
    }

    private void OnLogWindowFileNotFound (object sender, EventArgs e)
    {
        Invoke(new FileNotFoundDelegate(FileNotFound), sender);
    }

    private void OnLogWindowFileRespawned (object sender, EventArgs e)
    {
        Invoke(new FileRespawnedDelegate(FileRespawned), sender);
    }

    private void OnLogWindowFilterListChanged (object sender, FilterListChangedEventArgs e)
    {
        lock (_logWindowList)
        {
            foreach (var logWindow in _logWindowList)
            {
                if (logWindow != e.LogWindow)
                {
                    logWindow.HandleChangedFilterList();
                }
            }
        }
        ConfigManager.Save(SettingsFlags.FilterList);
    }

    private void OnLogWindowCurrentHighlightGroupChanged (object sender, CurrentHighlightGroupChangedEventArgs e)
    {
        OnHighlightSettingsChanged();
        ConfigManager.Settings.Preferences.HighlightGroupList = HighlightGroupList;
        ConfigManager.Save(SettingsFlags.HighlightSettings);
    }

    private void OnTailFollowed (object sender, EventArgs e)
    {
        if (dockPanel.ActiveContent == null)
        {
            return;
        }
        if (sender.GetType().IsAssignableFrom(typeof(LogWindow.LogWindow)))
        {
            if (dockPanel.ActiveContent == sender)
            {
                var data = ((LogWindow.LogWindow)sender).Tag as LogWindowData;
                data.Dirty = false;
                var icon = GetIcon(data.DiffSum, data);
                BeginInvoke(new SetTabIconDelegate(SetTabIcon), (LogWindow.LogWindow)sender, icon);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnLogWindowSyncModeChanged (object sender, SyncModeEventArgs e)
    {
        if (!Disposing)
        {
            var data = ((LogWindow.LogWindow)sender).Tag as LogWindowData;
            data.SyncMode = e.IsTimeSynced ? 1 : 0;
            var icon = GetIcon(data.DiffSum, data);
            BeginInvoke(new SetTabIconDelegate(SetTabIcon), (LogWindow.LogWindow)sender, icon);
        }
        else
        {
            _logger.Warn(CultureInfo.InvariantCulture, "Received SyncModeChanged event while disposing. Event ignored.");
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnToggleBookmarkToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.ToggleBookmark();
    }

    [SupportedOSPlatform("windows")]
    private void OnJumpToNextToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.JumpNextBookmark();
    }

    [SupportedOSPlatform("windows")]
    private void OnJumpToPrevToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.JumpPrevBookmark();
    }

    [SupportedOSPlatform("windows")]
    private void OnASCIIToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.ChangeEncoding(Encoding.ASCII);
    }

    [SupportedOSPlatform("windows")]
    private void OnANSIToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.ChangeEncoding(Encoding.Default);
    }

    [SupportedOSPlatform("windows")]
    private void OnUTF8ToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.ChangeEncoding(new UTF8Encoding(false));
    }

    [SupportedOSPlatform("windows")]
    private void OnUTF16ToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.ChangeEncoding(Encoding.Unicode);
    }

    [SupportedOSPlatform("windows")]
    private void OnISO88591ToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.ChangeEncoding(Encoding.GetEncoding("iso-8859-1"));
    }

    [SupportedOSPlatform("windows")]
    private void OnReloadToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (CurrentLogWindow != null)
        {
            var data = CurrentLogWindow.Tag as LogWindowData;
            var icon = GetIcon(0, data);
            BeginInvoke(new SetTabIconDelegate(SetTabIcon), CurrentLogWindow, icon);
            CurrentLogWindow.Reload();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnSettingsToolStripMenuItemClick (object sender, EventArgs e)
    {
        OpenSettings(0);
    }

    [SupportedOSPlatform("windows")]
    private void OnDateTimeDragControlValueDragged (object sender, EventArgs e)
    {
        if (CurrentLogWindow != null)
        {
            //this.CurrentLogWindow.ScrollToTimestamp(this.dateTimeDragControl.DateTime);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnDateTimeDragControlValueChanged (object sender, EventArgs e)
    {
        CurrentLogWindow?.ScrollToTimestamp(dragControlDateTime.DateTime, true, true);
    }

    [SupportedOSPlatform("windows")]
    private void OnLogTabWindowDeactivate (object sender, EventArgs e)
    {
        CurrentLogWindow?.AppFocusLost();
    }

    [SupportedOSPlatform("windows")]
    private void OnLogTabWindowActivated (object sender, EventArgs e)
    {
        CurrentLogWindow?.AppFocusGained();
    }

    [SupportedOSPlatform("windows")]
    private void OnShowBookmarkListToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (_bookmarkWindow.Visible)
        {
            _bookmarkWindow.Hide();
        }
        else
        {
            // strange: on very first Show() now bookmarks are displayed. after a hide it will work.
            if (_firstBookmarkWindowShow)
            {
                _bookmarkWindow.Show(dockPanel);
                _bookmarkWindow.Hide();
            }

            _bookmarkWindow.Show(dockPanel);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnToolStripButtonOpenClick (object sender, EventArgs e)
    {
        OpenFileDialog();
    }

    [SupportedOSPlatform("windows")]
    private void OnToolStripButtonSearchClick (object sender, EventArgs e)
    {
        OpenSearchDialog();
    }

    [SupportedOSPlatform("windows")]
    private void OnToolStripButtonFilterClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.ToggleFilterPanel();
    }

    [SupportedOSPlatform("windows")]
    private void OnToolStripButtonBookmarkClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.ToggleBookmark();
    }

    [SupportedOSPlatform("windows")]
    private void OnToolStripButtonUpClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.JumpPrevBookmark();
    }

    [SupportedOSPlatform("windows")]
    private void OnToolStripButtonDownClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.JumpNextBookmark();
    }

    [SupportedOSPlatform("windows")]
    private void OnShowHelpToolStripMenuItemClick (object sender, EventArgs e)
    {
        Help.ShowHelp(this, "LogExpert.chm");
    }

    private void OnHideLineColumnToolStripMenuItemClick (object sender, EventArgs e)
    {
        ConfigManager.Settings.HideLineColumn = hideLineColumnToolStripMenuItem.Checked;
        lock (_logWindowList)
        {
            foreach (var logWin in _logWindowList)
            {
                logWin.ShowLineColumn(!ConfigManager.Settings.HideLineColumn);
            }
        }
        _bookmarkWindow.LineColumnVisible = ConfigManager.Settings.HideLineColumn;
    }

    // ==================================================================
    // Tab context menu stuff
    // ==================================================================

    [SupportedOSPlatform("windows")]
    private void OnCloseThisTabToolStripMenuItemClick (object sender, EventArgs e)
    {
        (dockPanel.ActiveContent as LogWindow.LogWindow).Close();
    }

    [SupportedOSPlatform("windows")]
    private void OnCloseOtherTabsToolStripMenuItemClick (object sender, EventArgs e)
    {
        var closeList = dockPanel.Contents
                .OfType<LogWindow.LogWindow>()
                .Where(content => content != dockPanel.ActiveContent)
                .ToList();

        foreach (var logWindow in closeList)
        {
            logWindow.Close();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnCloseAllTabsToolStripMenuItemClick (object sender, EventArgs e)
    {
        CloseAllTabs();
    }

    [SupportedOSPlatform("windows")]
    private void OnTabColorToolStripMenuItemClick (object sender, EventArgs e)
    {
        var logWindow = dockPanel.ActiveContent as LogWindow.LogWindow;

        if (logWindow.Tag is not LogWindowData data)
        {
            return;
        }

        ColorDialog dlg = new()
        {
            Color = data.Color
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            data.Color = dlg.Color;
            SetTabColor(logWindow, data.Color);
        }

        List<ColorEntry> delList = [];

        foreach (var entry in ConfigManager.Settings.FileColors)
        {
            if (entry.FileName.Equals(logWindow.FileName, StringComparison.Ordinal))
            {
                delList.Add(entry);
            }
        }

        foreach (var entry in delList)
        {
            _ = ConfigManager.Settings.FileColors.Remove(entry);
        }

        ConfigManager.Settings.FileColors.Add(new ColorEntry(logWindow.FileName, dlg.Color));

        while (ConfigManager.Settings.FileColors.Count > MAX_COLOR_HISTORY)
        {
            ConfigManager.Settings.FileColors.RemoveAt(0);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnLogTabWindowSizeChanged (object sender, EventArgs e)
    {
        if (WindowState != FormWindowState.Minimized)
        {
            _wasMaximized = WindowState == FormWindowState.Maximized;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnSaveProjectToolStripMenuItemClick (object sender, EventArgs e)
    {
        SaveFileDialog dlg = new()
        {
            DefaultExt = "lxj",
            Filter = @"LogExpert session (*.lxj)|*.lxj"
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var fileName = dlg.FileName;
            List<string> fileNames = [];

            lock (_logWindowList)
            {
                foreach (var logWindow in dockPanel.Contents.OfType<LogWindow.LogWindow>())
                {
                    var persistenceFileName = logWindow?.SavePersistenceData(true);
                    if (persistenceFileName != null)
                    {
                        fileNames.Add(persistenceFileName);
                    }
                }
            }

            ProjectData projectData = new()
            {
                MemberList = fileNames,
                TabLayoutXml = SaveLayout()
            };
            ProjectPersister.SaveProjectData(fileName, projectData);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnLoadProjectToolStripMenuItemClick (object sender, EventArgs e)
    {
        OpenFileDialog dlg = new()
        {
            DefaultExt = "lxj",
            Filter = @"LogExpert sessions (*.lxj)|*.lxj"
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var projectFileName = dlg.FileName;
            LoadProject(projectFileName, true);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnToolStripButtonBubblesClick (object sender, EventArgs e)
    {
        if (CurrentLogWindow != null)
        {
            CurrentLogWindow.ShowBookmarkBubbles = toolStripButtonBubbles.Checked;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnCopyPathToClipboardToolStripMenuItemClick (object sender, EventArgs e)
    {
        var logWindow = dockPanel.ActiveContent as LogWindow.LogWindow;
        Clipboard.SetText(logWindow.Title);
    }

    private void OnFindInExplorerToolStripMenuItemClick (object sender, EventArgs e)
    {
        var logWindow = dockPanel.ActiveContent as LogWindow.LogWindow;

        Process explorer = new();
        explorer.StartInfo.FileName = "explorer.exe";
        explorer.StartInfo.Arguments = "/e,/select," + logWindow.Title;
        explorer.StartInfo.UseShellExecute = false;
        explorer.Start();
    }

    private void TruncateFileToolStripMenuItem_Click (object sender, EventArgs e)
    {
        CurrentLogWindow?.TryToTruncate();
    }

    private void OnExportBookmarksToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.ExportBookmarkList();
    }

    [SupportedOSPlatform("windows")]
    private void OnHighlightGroupsComboBoxDropDownClosed (object sender, EventArgs e)
    {
        ApplySelectedHighlightGroup();
    }

    [SupportedOSPlatform("windows")]
    private void OnHighlightGroupsComboBoxSelectedIndexChanged (object sender, EventArgs e)
    {
        ApplySelectedHighlightGroup();
    }

    [SupportedOSPlatform("windows")]
    private void OnHighlightGroupsComboBoxMouseUp (object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            ShowHighlightSettingsDialog();
        }
    }


    private void OnConfigChanged (object sender, ConfigChangedEventArgs e)
    {
        if (LogExpertProxy != null)
        {
            NotifySettingsChanged(null, e.Flags);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnDumpLogBufferInfoToolStripMenuItemClick (object sender, EventArgs e)
    {
#if DEBUG
        CurrentLogWindow?.DumpBufferInfo();
#endif
    }

    [SupportedOSPlatform("windows")]
    private void OnDumpBufferDiagnosticToolStripMenuItemClick (object sender, EventArgs e)
    {
#if DEBUG
        CurrentLogWindow?.DumpBufferDiagnostic();
#endif
    }

    private void OnRunGCToolStripMenuItemClick (object sender, EventArgs e)
    {
        RunGC();
    }

    private void OnGCInfoToolStripMenuItemClick (object sender, EventArgs e)
    {
        DumpGCInfo();
    }

    [SupportedOSPlatform("windows")]
    private void OnToolsToolStripMenuItemDropDownItemClicked (object sender, ToolStripItemClickedEventArgs e)
    {
        if (e.ClickedItem.Tag is ToolEntry tag)
        {
            ToolButtonClick(tag);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnExternalToolsToolStripItemClicked (object sender, ToolStripItemClickedEventArgs e)
    {
        ToolButtonClick(e.ClickedItem.Tag as ToolEntry);
    }

    [SupportedOSPlatform("windows")]
    private void OnConfigureToolStripMenuItemClick (object sender, EventArgs e)
    {
        OpenSettings(2);
    }

    private void OnThrowExceptionGUIThreadToolStripMenuItemClick (object sender, EventArgs e)
    {
        throw new Exception("This is a test exception thrown by the GUI thread");
    }

    private void OnThrowExceptionBackgroundThToolStripMenuItemClick (object sender, EventArgs e)
    {
        ExceptionFx fx = ThrowExceptionFx;
        fx.BeginInvoke(null, null);
    }

    private void OnThrowExceptionBackgroundThreadToolStripMenuItemClick (object sender, EventArgs e)
    {
        Thread thread = new(ThrowExceptionThreadFx)
        {
            IsBackground = true
        };

        thread.Start();
    }

    private void OnWarnToolStripMenuItemClick (object sender, EventArgs e)
    {
        //_logger.GetLogger().LogLevel = _logger.Level.WARN;
    }

    private void OnInfoToolStripMenuItemClick (object sender, EventArgs e)
    {
        //_logger.Get_logger().LogLevel = _logger.Level.INFO;
    }

    private void OnDebugToolStripMenuItemClick (object sender, EventArgs e)
    {
        //_logger.Get_logger().LogLevel = _logger.Level.DEBUG;
    }

    private void OnLogLevelToolStripMenuItemClick (object sender, EventArgs e)
    {
    }

    private void OnLogLevelToolStripMenuItemDropDownOpening (object sender, EventArgs e)
    {
        //warnToolStripMenuItem.Checked = _logger.Get_logger().LogLevel == _logger.Level.WARN;
        //infoToolStripMenuItem.Checked = _logger.Get_logger().LogLevel == _logger.Level.INFO;
        //debugToolStripMenuItem1.Checked = _logger.Get_logger().LogLevel == _logger.Level.DEBUG;
    }

    [SupportedOSPlatform("windows")]
    private void OnDisableWordHighlightModeToolStripMenuItemClick (object sender, EventArgs e)
    {
        DebugOptions.DisableWordHighlight = disableWordHighlightModeToolStripMenuItem.Checked;
        CurrentLogWindow?.RefreshAllGrids();
    }

    [SupportedOSPlatform("windows")]
    private void OnMultiFileMaskToolStripMenuItemClick (object sender, EventArgs e)
    {
        CurrentLogWindow?.ChangeMultifileMask();
    }

    [SupportedOSPlatform("windows")]
    private void OnMultiFileEnabledStripMenuItemClick (object sender, EventArgs e)
    {
        ToggleMultiFile();
    }

    [SupportedOSPlatform("windows")]
    private void OnLockInstanceToolStripMenuItemClick (object sender, EventArgs e)
    {
        AbstractLogTabWindow.StaticData.CurrentLockedMainWindow = lockInstanceToolStripMenuItem.Checked ? null : this;
    }

    [SupportedOSPlatform("windows")]
    private void OnOptionToolStripMenuItemDropDownOpening (object sender, EventArgs e)
    {
        lockInstanceToolStripMenuItem.Enabled = !ConfigManager.Settings.Preferences.AllowOnlyOneInstance;
        lockInstanceToolStripMenuItem.Checked = AbstractLogTabWindow.StaticData.CurrentLockedMainWindow == this;
    }

    [SupportedOSPlatform("windows")]
    private void OnFileToolStripMenuItemDropDownOpening (object sender, EventArgs e)
    {
        newFromClipboardToolStripMenuItem.Enabled = Clipboard.ContainsText();
    }

    [SupportedOSPlatform("windows")]
    private void OnNewFromClipboardToolStripMenuItemClick (object sender, EventArgs e)
    {
        PasteFromClipboard();
    }

    [SupportedOSPlatform("windows")]
    private void OnOpenURIToolStripMenuItemClick (object sender, EventArgs e)
    {
        OpenUriDialog dlg = new()
        {
            UriHistory = ConfigManager.Settings.UriHistoryList
        };

        if (DialogResult.OK == dlg.ShowDialog())
        {
            if (dlg.Uri.Trim().Length > 0)
            {
                ConfigManager.Settings.UriHistoryList = dlg.UriHistory;
                ConfigManager.Save(SettingsFlags.FileHistory);
                LoadFiles([dlg.Uri], false);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnColumnFinderToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (CurrentLogWindow != null && !_skipEvents)
        {
            CurrentLogWindow.ToggleColumnFinder(columnFinderToolStripMenuItem.Checked, true);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnDockPanelActiveContentChanged (object sender, EventArgs e)
    {
        if (dockPanel.ActiveContent is LogWindow.LogWindow window)
        {
            CurrentLogWindow = window;
            CurrentLogWindow.LogWindowActivated();
            ConnectToolWindows(CurrentLogWindow);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnTabRenameToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (CurrentLogWindow != null)
        {
            TabRenameDialog dlg = new()
            {
                TabName = CurrentLogWindow.Text
            };

            if (DialogResult.OK == dlg.ShowDialog())
            {
                CurrentLogWindow.Text = dlg.TabName;
            }

            dlg.Dispose();
        }
    }

    #endregion
}