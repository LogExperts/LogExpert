using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

using ColumnizerLib;

using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;
using LogExpert.Core.EventArguments;
using LogExpert.Core.Interfaces;
using LogExpert.Dialogs;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Dialogs.Helpers;
using LogExpert.UI.Entities;
using LogExpert.UI.Extensions;
using LogExpert.UI.Extensions.LogWindow;
using LogExpert.UI.Services.FileOperationService;
using LogExpert.UI.Services.LedService;
using LogExpert.UI.Services.LogWindowCoordinatorService;
using LogExpert.UI.Services.MenuToolbarService;
using LogExpert.UI.Services.SessionHandlerService;
using LogExpert.UI.Services.TabControllerService;
using LogExpert.UI.Services.ToolLaunchService;
using LogExpert.UI.Services.ToolWindowCoordinatorService;

using NLog;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Controls.LogTabWindow;

// Data shared over all LogTabWindow instances
[SupportedOSPlatform("windows")]
internal partial class LogTabWindow : Form, ILogTabWindow
{
    #region Fields

    private const int MAX_COLUMNIZER_HISTORY = 40;
    //private const int MAX_COLOR_HISTORY = 40;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly Icon _deadIcon;
    private readonly LedIndicatorService _ledService;

    private readonly TabController _tabController;
    private readonly MenuToolbarController _menuToolbarController;
    private readonly LogWindowCoordinator _logWindowCoordinator;
    private readonly ToolWindowCoordinator _toolWindowCoordinator;
    private readonly FileOperationService _fileOperationService;
    private readonly SessionHandler _sessionHandler;
    private readonly ToolLaunchService _toolLaunchService;

    private bool _disposed;

    private readonly Color _defaultTabColor = Color.FromArgb(255, 192, 192, 192);

    private readonly int _instanceNumber;

    private readonly bool _showInstanceNumbers;

    private readonly string[] _startupFileNames;

    [SupportedOSPlatform("windows")]
    private readonly StringFormat _tabStringFormat = new();

    private LogWindow.LogWindow _currentLogWindow;

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

        ConfigureDockPanel();

        _tabController = new TabController(dockPanel);
        InitializeTabControllerEvents();

        _menuToolbarController = new MenuToolbarController();
        _menuToolbarController.InitializeMenus(mainMenuStrip, buttonToolStrip, externalToolsToolStrip, dragControlDateTime, checkBoxFollowTail);
        InitializeMenuToolbarControllerEvents();

        ApplyTextResources();

        ConfigManager = configManager;

        _toolWindowCoordinator = new ToolWindowCoordinator(configManager);

        _ledService = new LedIndicatorService();
        _ledService.Initialize(ConfigManager.Settings.Preferences.ShowTailColor);
        _ledService.IconChanged += OnLedIconChanged;
        _ledService.StartService();

        _deadIcon = _ledService.GetDeadIcon();

        _fileOperationService = new FileOperationService(configManager, _tabController, _ledService, PluginRegistry.PluginRegistry.Instance, CreateLogWindowFromRequest, () => Clipboard.ContainsText() ? Clipboard.GetText() : null, LoadSession);

        _fileOperationService.FileHistoryChanged += (_, _) => FillHistoryMenu();
        _fileOperationService.FileOpened += OnFileOperationServiceFileOpened;

        _sessionHandler = new SessionHandler(PluginRegistry.PluginRegistry.Instance, request => _fileOperationService.AddFileTab(request), CloseAllTabs);
        _toolLaunchService = new ToolLaunchService(PluginRegistry.PluginRegistry.Instance);

        _logWindowCoordinator = new LogWindowCoordinator(configManager, PluginRegistry.PluginRegistry.Instance, this, _tabController, _ledService, _fileOperationService);

        //Fix MainMenu and externalToolsToolStrip.Location, if the location has been changed in the designer
        mainMenuStrip.Location = new Point(0, 0);
        externalToolsToolStrip.Location = new Point(0, 54);

        _startupFileNames = fileNames;
        _instanceNumber = instanceNumber;
        _showInstanceNumbers = showInstanceNumbers;

        Load += OnLogTabWindowLoad;

        ConfigManager.ConfigChanged += OnConfigChanged;
        HighlightGroupList = configManager.Settings.Preferences.HighlightGroupList;

        _tabStringFormat.LineAlignment = StringAlignment.Center;
        _tabStringFormat.Alignment = StringAlignment.Near;

        ToolStripControlHost host = new(checkBoxFollowTail)
        {
            Padding = new Padding(20, 0, 0, 0),
            BackColor = Color.FromKnownColor(KnownColor.Transparent)
        };

        // Built here rather than declared in the designer so the menu and the Preferences combo box
        // cannot offer different encodings.
        EncodingMenuBuilder.Fill(encodingToolStripMenuItem, OnEncodingToolStripMenuItemClick);

        var index = buttonToolStrip.Items.IndexOfKey("toolStripButtonTail");

        if (index != -1)
        {
            buttonToolStrip.Items.RemoveAt(index);
            buttonToolStrip.Items.Insert(index, host);
        }

        dragControlDateTime.Visible = false;
        loadProgessBar.Visible = false;

        FormClosing += OnLogTabWindowFormClosing;

        InitToolWindows();
    }

    [SupportedOSPlatform("windows")]
    private LogWindow.LogWindow CreateLogWindowFromRequest (FileTabRequest request, EncodingOptions encodingOptions)
    {
        LogWindow.LogWindow logWindow = new(
            _logWindowCoordinator,
            PersisterHelpers.FindFilenameForSettings(request.FileName, PluginRegistry.PluginRegistry.Instance),
            request.IsTempFile,
            request.ForcePersistenceLoading,
            ConfigManager,
            PluginRegistry.PluginRegistry.Instance)
        {
            GivenFileName = request.FileName
        };

        if (request.PreProcessColumnizer != null)
        {
            logWindow.ForceColumnizerForLoading(request.PreProcessColumnizer);
        }

        if (request.IsTempFile)
        {
            logWindow.TempTitleName = request.Title ?? string.Empty;
        }

        AddLogWindow(logWindow, request.Title, request.DoNotAddToDockPanel);
        return logWindow;
    }

    [SupportedOSPlatform("windows")]
    private void OnFileOperationServiceFileOpened (object? sender, FileOpenedEventArgs e)
    {
        if (e.LogWindow.Tag is LogWindowData data)
        {
            data.Color = _defaultTabColor;
        }

        if (!e.Request.IsTempFile)
        {
            SetTooltipText(e.LogWindow, e.ResolvedFileName);
        }

        // Filter tooltip setup
        if (e.FilterPipe != null && e.FilterPipe.FilterParams.SearchText?.Length > 0)
        {
            ToolTip tip = new(components);
            var isInvertText = e.FilterPipe.FilterParams.IsInvert ? Resources.LogTabWindow_UI_LogWindow_ToolTip_InvertMatch : string.Empty;
            var isColumnRestrictText = e.FilterPipe.FilterParams.ColumnRestrict ? Resources.LogTabWindow_UI_LogWindow_Tooltip_ColumnRestrict : string.Empty;
            tip.SetToolTip(e.LogWindow, string.Format(CultureInfo.InvariantCulture, Resources.LogTabWindow_UI_LogWindow_ToolTip_Filter, e.FilterPipe.FilterParams.SearchText, isInvertText, isColumnRestrictText));
            tip.AutomaticDelay = 10;
            tip.AutoPopDelay = 5000;
            if (e.LogWindow.Tag is LogWindowData filterData)
            {
                filterData.ToolTip = tip;
            }
        }

        // Multi-file loading (used starting in Phase 4)
        if (e.MultiFileNames != null && e.EncodingOptions != null)
        {
            multiFileToolStripMenuItem.Checked = true;
            multiFileEnabledStripMenuItem.Checked = true;
            _ = BeginInvoke(e.LogWindow.LoadFilesAsMulti, e.MultiFileNames, e.EncodingOptions);
        }
    }

    private void InitializeMenuToolbarControllerEvents ()
    {
        _menuToolbarController.HistoryItemClicked += OnMenuControllerHistoryItemClicked;
        _menuToolbarController.HistoryItemRemoveRequested += OnMenuControllerHistoryItemRemoveRequested;
        _menuToolbarController.HighlightGroupSelected += OnMenuControllerHighlightGroupSelected;
    }

    private void OnMenuControllerHighlightGroupSelected (object? sender, HighlightGroupSelectedEventArgs e)
    {
        CurrentLogWindow?.SetCurrentHighlightGroup(e.GroupName);
    }

    private void OnMenuControllerHistoryItemRemoveRequested (object? sender, HistoryItemClickedEventArgs e)
    {
        ConfigManager.RemoveFromFileHistory(e.FileName);
        FillHistoryMenu();
    }

    private void OnMenuControllerHistoryItemClicked (object? sender, HistoryItemClickedEventArgs e)
    {
        _ = _fileOperationService.AddFileTab(new FileTabRequest { FileName = e.FileName });
    }

    private void InitializeTabControllerEvents ()
    {
        _tabController.WindowAdded += OnTabControllerWindowAdded;
        _tabController.WindowRemoved += OnTabControllerWindowRemoved;
        _tabController.WindowActivated += OnTabControllerWindowActivated;
        _tabController.WindowClosing += OnTabControllerWindowClosing;
    }

    #endregion

    #region Properties

    [SupportedOSPlatform("windows")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public LogWindow.LogWindow CurrentLogWindow
    {
        get => _currentLogWindow;
        set => BindActiveLogWindow(value);
    }

    public SearchParams SearchParams => _logWindowCoordinator.SearchParams;

    public Preferences Preferences => ConfigManager.Settings.Preferences;

    //TODO: This needs to be changed, since its only using _configManager.Settings.Preferences.HighlightGroupList,
    //like the logwindowCoordinator, but its also used in the settingsDialog, this needs to be refactored
    public List<HighlightGroup> HighlightGroupList { get; private set; } = [];

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public ILogExpertProxy LogExpertProxy { get; set; }

    public IConfigManager ConfigManager { get; }

    #endregion

    #region Internals

    internal HighlightGroup FindHighlightGroup (string groupName)
    {
        return _logWindowCoordinator.ResolveHighlightGroup(groupName, null);
    }

    #endregion

    #region Public methods

    [SupportedOSPlatform("windows")]
    public LogWindow.LogWindow AddTempFileTab (string fileName, string title)
    {
        return _fileOperationService.AddTempFileTab(fileName, title);
    }

    private void ConfigureDockPanel ()
    {
        if (Application.IsDarkModeEnabled)
        {
            var darkTheme = new VS2015DarkTheme();
            dockPanel.Theme = darkTheme;
            dockPanel.DockBackColor = darkTheme.ColorPalette.MainWindowActive.Background;
            dockPanel.ActiveAutoHideContent = null;
            dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
            return;
        }

        var autoHideStripSkin1 = new AutoHideStripSkin();
        var dockPanelGradient1 = new DockPanelGradient();
        var tabGradient1 = new TabGradient();
        var dockPaneStripSkin1 = new DockPaneStripSkin();
        var dockPaneStripGradient1 = new DockPaneStripGradient();
        var tabGradient2 = new TabGradient();
        var dockPanelGradient2 = new DockPanelGradient();
        var tabGradient3 = new TabGradient();
        var dockPaneStripToolWindowGradient1 = new DockPaneStripToolWindowGradient();
        var tabGradient4 = new TabGradient();
        var tabGradient5 = new TabGradient();
        var dockPanelGradient3 = new DockPanelGradient();
        var tabGradient6 = new TabGradient();
        var tabGradient7 = new TabGradient();

        dockPanelGradient1.EndColor = SystemColors.Control;
        dockPanelGradient1.StartColor = SystemColors.Control;
        autoHideStripSkin1.DockStripGradient = dockPanelGradient1;
        tabGradient1.EndColor = SystemColors.Control;
        tabGradient1.StartColor = SystemColors.Control;
        tabGradient1.TextColor = SystemColors.ControlText;
        autoHideStripSkin1.TabGradient = tabGradient1;
        autoHideStripSkin1.TextFont = new Font("Segoe UI", 9F);
        tabGradient2.EndColor = SystemColors.Control;
        tabGradient2.StartColor = SystemColors.Control;
        tabGradient2.TextColor = SystemColors.ControlText;
        dockPaneStripGradient1.ActiveTabGradient = tabGradient2;
        dockPanelGradient2.EndColor = SystemColors.Control;
        dockPanelGradient2.StartColor = SystemColors.Control;
        dockPaneStripGradient1.DockStripGradient = dockPanelGradient2;
        tabGradient3.EndColor = SystemColors.ControlLight;
        tabGradient3.StartColor = SystemColors.ControlLight;
        tabGradient3.TextColor = SystemColors.ControlText;
        dockPaneStripGradient1.InactiveTabGradient = tabGradient3;
        dockPaneStripSkin1.DocumentGradient = dockPaneStripGradient1;
        dockPaneStripSkin1.TextFont = new Font("Segoe UI", 9F);
        tabGradient4.EndColor = SystemColors.ActiveCaption;
        tabGradient4.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
        tabGradient4.StartColor = SystemColors.GradientActiveCaption;
        tabGradient4.TextColor = SystemColors.ActiveCaptionText;
        dockPaneStripToolWindowGradient1.ActiveCaptionGradient = tabGradient4;
        tabGradient5.EndColor = SystemColors.Control;
        tabGradient5.StartColor = SystemColors.Control;
        tabGradient5.TextColor = SystemColors.ControlText;
        dockPaneStripToolWindowGradient1.ActiveTabGradient = tabGradient5;
        dockPanelGradient3.EndColor = SystemColors.ControlLight;
        dockPanelGradient3.StartColor = SystemColors.ControlLight;
        dockPaneStripToolWindowGradient1.DockStripGradient = dockPanelGradient3;
        tabGradient6.EndColor = SystemColors.InactiveCaption;
        tabGradient6.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
        tabGradient6.StartColor = SystemColors.GradientInactiveCaption;
        tabGradient6.TextColor = SystemColors.InactiveCaptionText;
        dockPaneStripToolWindowGradient1.InactiveCaptionGradient = tabGradient6;
        tabGradient7.EndColor = Color.Transparent;
        tabGradient7.StartColor = Color.Transparent;
        tabGradient7.TextColor = SystemColors.Control;
        dockPaneStripToolWindowGradient1.InactiveTabGradient = tabGradient7;
        dockPaneStripSkin1.ToolWindowGradient = dockPaneStripToolWindowGradient1;
        dockPanel.Theme = new VS2015LightTheme();
        dockPanel.Theme.Skin.DockPaneStripSkin = dockPaneStripSkin1;
        dockPanel.Theme.Skin.AutoHideStripSkin = autoHideStripSkin1;
        dockPanel.ActiveAutoHideContent = null;
        dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
    }

    private void ApplyTextResources ()
    {
        mainMenuStrip.Text = Resources.LogTabWindow_UI_MenuStrip_MainMenu;
        Text = Resources.LogExpert_Common_UI_Title_LogExpert;
        checkBoxHost.AccessibleName = Resources.LogTabWindow_UI_CheckBox_ToolTip_checkBoxHost;

        ApplyStatusStripResources();
        ApplyContextMenuResources();
        ApplyToolStripResources();
        ApplyTabContextMenuResources();

        ApplyToolTips();
    }

    private void ApplyTabContextMenuResources ()
    {
        closeThisTabToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_closeThisTabToolStripMenuItem;
        closeOtherTabsToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_closeOtherTabsToolStripMenuItem;
        closeAllTabsToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_closeAllTabsToolStripMenuItem;
        tabColorToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_tabColorToolStripMenuItem;
        tabRenameToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_tabRenameToolStripMenuItem;
        copyPathToClipboardToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_copyPathToClipboardToolStripMenuItem;
        findInExplorerToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_findInExplorerToolStripMenuItem;
        truncateFileToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_truncateFileToolStripMenuItem;
    }

    private void ApplyToolStripResources ()
    {
        checkBoxHost.Text = Resources.LogTabWindow_UI_CheckBox_Host;
        toolStripContainer.Text = Resources.LogTabWindow_UI_ToolStripContainer_toolStripContainer;
        toolStripButtonOpen.Text = Resources.LogTabWindow_UI_ToolStripButton_toolStripButtonOpen;
        toolStripButtonSearch.Text = Resources.LogTabWindow_UI_ToolStripButton_toolStripButtonSearch;
        toolStripButtonFilter.Text = Resources.LogTabWindow_UI_ToolStripButton_toolStripButtonFilter;
        toolStripButtonBookmark.Text = Resources.LogTabWindow_UI_ToolStripButton_toolStripButtonBookmark;
        toolStripButtonUp.Text = Resources.LogTabWindow_UI_ToolStripButton_toolStripButtonUp;
        toolStripButtonDown.Text = Resources.LogTabWindow_UI_ToolStripButton_toolStripButtonDown;
        toolStripButtonBubbles.Text = Resources.LogTabWindow_UI_ToolStripButton_toolStripButtonBubbles;
        toolStripButtonTail.Text = Resources.LogTabWindow_UI_ToolStripButton_toolStripButtonTail;
        checkBoxFollowTail.Text = Resources.LogTabWindow_UI_CheckBox_checkBoxFollowTail;
        pluginTrustManagementToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_Text_PluginTrustManagement;
    }

    private void ApplyContextMenuResources ()
    {
        //File menu
        fileToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_fileToolStripMenuItem;
        openToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_openToolStripMenuItem;
        openURIToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_openURIToolStripMenuItem;
        closeFileToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_closeFileToolStripMenuItem;
        reloadToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_reloadToolStripMenuItem;
        newFromClipboardToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_newFromClipboardToolStripMenuItem;
        multiFileToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_multiFileToolStripMenuItem;
        multiFileEnabledStripMenuItem.Text = Resources.LogTabWindow_UI_StripMenuItem_multiFileEnabledStripMenuItem;
        multifileMaskToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_multifileMaskToolStripMenuItem;
        loadSessionToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_loadProjectToolStripMenuItem;
        saveSessionToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_saveProjectToolStripMenuItem;
        exportBookmarksToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_exportBookmarksToolStripMenuItem;
        lastUsedToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_lastUsedToolStripMenuItem;
        exitToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_exitToolStripMenuItem;

        //View/Navigate menu
        viewNavigateToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_viewNavigateToolStripMenuItem;
        goToLineToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_goToLineToolStripMenuItem;
        searchToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_searchToolStripMenuItem;
        filterToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_filterToolStripMenuItem;
        bookmarksToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_bookmarksToolStripMenuItem;
        toggleBookmarkToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_toggleBookmarkToolStripMenuItem;
        jumpToNextToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_jumpToNextToolStripMenuItem;
        jumpToPrevToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_jumpToPrevToolStripMenuItem;
        showBookmarkListToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_showBookmarkListToolStripMenuItem;
        columnFinderToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_columnFinderToolStripMenuItem;
        // The rows below it are labelled with their encoding's header name, not from resources — an
        // encoding name is the same in every language.
        encodingToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_encodingToolStripMenuItem;
        timeshiftToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_timeshiftToolStripMenuItem;
        timeshiftToolStripTextBox.Text = Resources.LogTabWindow_UI_ToolStripTextBox_timeshiftToolStripTextBox;
        copyMarkedLinesIntoNewTabToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_copyMarkedLinesIntoNewTabToolStripMenuItem;

        //Options menu
        optionToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_optionToolStripMenuItem;
        columnizerToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_columnizerToolStripMenuItem;
        hilightingToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_hilightingToolStripMenuItem;
        settingsToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_settingsToolStripMenuItem;
        cellSelectModeToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_cellSelectModeToolStripMenuItem;
        alwaysOnTopToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_alwaysOnTopToolStripMenuItem;
        hideLineColumnToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_hideLineColumnToolStripMenuItem;
        lockInstanceToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_lockInstanceToolStripMenuItem;

        //Tools Menu
        toolsToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_toolsToolStripMenuItem;
        configureToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_configureToolStripMenuItem;

        //Help Menu
        helpToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_helpToolStripMenuItem;
        showHelpToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_showHelpToolStripMenuItem;
        aboutToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_aboutToolStripMenuItem;

        //Debug Menu
        debugToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_debugToolStripMenuItem;
        dumpLogBufferInfoToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_dumpLogBufferInfoToolStripMenuItem;
        dumpBufferDiagnosticToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_dumpBufferDiagnosticToolStripMenuItem;
        runGCToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_runGCToolStripMenuItem;
        gCInfoToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_gCInfoToolStripMenuItem;
        throwExceptionGUIThreadToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_throwExceptionGUIThreadToolStripMenuItem;
        throwExceptionbackgroundThToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_throwExceptionbackgroundThToolStripMenuItem;
        throwExceptionBackgroundThreadToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_throwExceptionBackgroundThreadToolStripMenuItem;
        loglevelToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_loglevelToolStripMenuItem;
        warnLogLevelToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_warnToolStripMenuItem;
        infoLogLevelToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_infoToolStripMenuItem;
        debugLogLevelToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_debugLogLevelToolStripMenuItem;
        disableWordHighlightModeToolStripMenuItem.Text = Resources.LogTabWindow_UI_ToolStripMenuItem_disableWordHighlightModeToolStripMenuItem;
    }

    private void ApplyStatusStripResources ()
    {
        labelLines.Text = Resources.LogTabWindow_UI_Label_labelLines;
        labelSize.Text = Resources.LogTabWindow_UI_Label_labelSize;
        labelCurrentLine.Text = Resources.LogTabWindow_UI_Label_labelCurrentLine;
        labelStatus.Text = Resources.LogTabWindow_UI_Label_labelStatus;
    }

    private void ApplyToolTips ()
    {
        //TODO use ToolTip class instead of ToolTipText
        pluginTrustManagementToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_PluginTrustManagement;
        timeshiftToolStripTextBox.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_timeshiftToolStripTextBox;
        openURIToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_openURIToolStripMenuItem;
        newFromClipboardToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_newFromClipboardToolStripMenuItem;
        multiFileToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_multiFileToolStripMenuItem;
        loadSessionToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_loadProjectToolStripMenuItem;
        saveSessionToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_saveProjectToolStripMenuItem;
        timeshiftToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_timeshiftToolStripMenuItem;
        copyMarkedLinesIntoNewTabToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_copyMarkedLinesIntoNewTabToolStripMenuItem;
        columnizerToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_columnizerToolStripMenuItem;
        cellSelectModeToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_cellSelectModeToolStripMenuItem;
        lockInstanceToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_lockInstanceToolStripMenuItem;
        toolsToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_toolsToolStripMenuItem;
        toolStripButtonSearch.ToolTipText = Resources.LogTabWindow_UI_ToolStripButton_ToolTip_toolStripButtonSearch;
        toolStripButtonOpen.ToolTipText = Resources.LogTabWindow_UI_ToolStripButton_ToolTip_toolStripButtonOpen;
        toolStripButtonDown.ToolTipText = Resources.LogTabWindow_UI_ToolStripButton_ToolTip_toolStripButtonDown;
        toolStripButtonUp.ToolTipText = Resources.LogTabWindow_UI_ToolStripButton_ToolTip_toolStripButtonUp;
        toolStripButtonBookmark.ToolTipText = Resources.LogTabWindow_UI_ToolStripButton_ToolTip_toolStripButtonBookmark;
        toolStripButtonFilter.ToolTipText = Resources.LogTabWindow_UI_ToolStripButton_ToolTip_toolStripButtonFilter;
        highlightGroupsToolStripComboBox.ToolTipText = Resources.LogTabWindow_UI_ToolStripComboBox_ToolTip_highlightGroupsToolStripComboBox;
        tabRenameToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_tabRenameToolStripMenuItem;
        closeAllTabsToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_closeAllTabsToolStripMenuItem;
        closeOtherTabsToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_closeOtherTabsToolStripMenuItem;
        tabColorToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_tabColorToolStripMenuItem;
        findInExplorerToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_findInExplorerToolStripMenuItem;
        copyPathToClipboardToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_copyPathToClipboardToolStripMenuItem;
        truncateFileToolStripMenuItem.ToolTipText = Resources.LogTabWindow_UI_ToolStripMenuItem_ToolTip_truncateFileToolStripMenuItem;
    }

    [SupportedOSPlatform("windows")]
    public void LoadFiles (string[] fileNames)
    {
        Invoke(() => _fileOperationService.AddFileTabs(fileNames));
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
            SearchParams.CopyFrom(dlg.SearchParams);
            SearchParams.IsFindNext = false;
            CurrentLogWindow.StartSearch();
        }
    }

    public void SwitchTab (bool shiftPressed)
    {
        if (shiftPressed)
        {
            _tabController.SwitchToPreviousWindow();
        }
        else
        {
            _tabController.SwitchToNextWindow();
        }
    }

    /// <summary>
    /// Handles the WindowActivated event from TabController. Updates CurrentLogWindow and connects tool windows to the
    /// newly activated window.
    /// </summary>
    /// <param name="sender">The TabController that raised the event</param>
    /// <param name="e">Event args containing the activated window and previous window</param>
    [SupportedOSPlatform("windows")]
    private void OnTabControllerWindowActivated (object sender, WindowActivatedEventArgs e)
    {
        var newWindow = e.Window;

        if (newWindow == _currentLogWindow)
        {
            return;
        }

        // Routes through BindActiveLogWindow: unbinds the previous window, binds this one, and
        // connects the tool windows. Must precede LogWindowActivated() below, which fires the
        // status-line, progress-bar and GUI-state updates into the subscriptions made here.
        CurrentLogWindow = newWindow;

        // Clear dirty state for the newly activated window
        if (newWindow?.Tag is LogWindowData data)
        {
            data.LedState.IsDirty = false;

            // Update the tab icon to reflect cleared dirty state
            var icon = GetLedIcon(data.LedState.DiffSum, data);
            _ = BeginInvoke(SetTabIcon, newWindow, icon);
        }

        // Notify the window it has been activated
        newWindow?.LogWindowActivated();
    }

    /// <summary>
    /// Handles the WindowAdded event from TabController. Performs additional setup for newly added windows that
    /// LogTabWindow needs.
    /// </summary>
    /// <param name="sender">The TabController that raised the event</param>
    /// <param name="e">Event args containing the added window and title</param>
    [SupportedOSPlatform("windows")]
    private void OnTabControllerWindowAdded (object sender, WindowAddedEventArgs e)
    {
        var logWindow = e.Window;

        if (logWindow.Tag is not LogWindowData)
        {
            LogWindowData data = new()
            {
                LedState = new LedState(),
                Color = _defaultTabColor
            };

            logWindow.Tag = data;
        }

        _ledService.RegisterWindow(logWindow);

        if (logWindow.Tag is LogWindowData ledData)
        {
            var icon = GetLedIcon(ledData.LedState.DiffSum, ledData);
            _ = BeginInvoke(SetTabIcon, logWindow, icon);
        }

        ConnectEventHandlers(logWindow);
    }

    /// <summary>
    /// Handles the WindowClosing event from TabController. Performs pre-close validation and cleanup. Can cancel the
    /// close operation.
    /// </summary>
    /// <param name="sender">The TabController that raised the event</param>
    /// <param name="e">Event args containing the window being closed and cancellation support</param>
    [SupportedOSPlatform("windows")]
    private void OnTabControllerWindowClosing (object sender, WindowClosingEventArgs e)
    {
        var logWindow = e.Window;
        var skipConfirmation = e.SkipConfirmation;

        if (_tabController.GetWindowCount() == 1 && !skipConfirmation)
        {
            //TODO Add logic to confirm closing the last tab if desired
        }

        if (logWindow.Tag is LogWindowData data)
        {
            data.ToolTip?.Hide(logWindow);
        }
    }

    /// <summary>
    /// Handles the WindowRemoved event from TabController. Cleans up resources and event subscriptions for the removed
    /// window.
    /// </summary>
    /// <param name="sender">The TabController that raised the event</param>
    /// <param name="e">Event args containing the removed window</param>
    [SupportedOSPlatform("windows")]
    private void OnTabControllerWindowRemoved (object sender, WindowRemovedEventArgs e)
    {
        var logWindow = e.Window;

        _ledService.UnregisterWindow(logWindow);

        DisconnectEventHandlers(logWindow);

        if (logWindow.Tag is LogWindowData data)
        {
            data.ToolTip?.Dispose();
            logWindow.Tag = null;
        }

        if (CurrentLogWindow == logWindow)
        {
            ChangeCurrentLogWindow(null);
        }
    }

    [SupportedOSPlatform("windows")]
    public void SetForeground ()
    {
        _ = Vanara.PInvoke.User32.SetForegroundWindow(Handle);
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = _wasMaximized
                ? FormWindowState.Maximized
                : FormWindowState.Normal;
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

        data.LedState.TailState = isEnabled
            ? TailFollowState.On
            : offByTrigger
                ? TailFollowState.Paused
                : TailFollowState.Off;

        if (Preferences.ShowTailState)
        {
            var icon = GetLedIcon(data.LedState.DiffSum, data);
            _ = BeginInvoke(SetTabIcon, logWindow, icon);
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

    #endregion

    #region Private Methods

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose (bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing && (components != null))
        {
            _ledService?.StopService();
            _ledService?.Dispose();
            components.Dispose();
            _tabStringFormat?.Dispose();
            _menuToolbarController?.Dispose();
            _toolWindowCoordinator?.Dispose();
            // Dispose TabController after FileOperationService is no longer reachable.
            // FileOperationService holds a reference to _tabController but does not own it;
            // after Dispose(), no caller invokes the service, so stale references are harmless.
            _tabController?.Dispose();
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Creates a temp file with the text content of the clipboard and opens the temp file in a new tab.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void PasteFromClipboard ()
    {
        var logWindow = _fileOperationService.PasteFromClipboard();
        if (logWindow?.Tag is LogWindowData)
        {
            SetTooltipText(logWindow, string.Format(CultureInfo.InvariantCulture, Resources.LogTabWindow_UI_LogWindow_Title_ToolTip_PastedOn, DateTime.Now));
        }
    }

    [SupportedOSPlatform("windows")]
    private void InitToolWindows ()
    {
        _toolWindowCoordinator.Initialize();
    }

    [SupportedOSPlatform("windows")]
    private void DestroyBookmarkWindow ()
    {
        _toolWindowCoordinator.Destroy();
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

    private static void SetTooltipText (LogWindow.LogWindow logWindow, string logFileName)
    {
        logWindow.ToolTipText = logFileName;
    }

    /// <summary>
    /// Adds a LogWindow to the tab system. Sets up window properties, delegates to TabController, and performs
    /// additional setup.
    /// </summary>
    /// <param name="logWindow">The window to add</param>
    /// <param name="title">Tab title</param>
    /// <param name="doNotAddToPanel">Skip adding to DockPanel (for deferred loading)</param>
    [SupportedOSPlatform("windows")]
    private void AddLogWindow (LogWindow.LogWindow logWindow, string title, bool doNotAddToPanel)
    {
        logWindow.CloseButton = true;
        logWindow.TabPageContextMenuStrip = tabContextMenuStrip;
        SetTooltipText(logWindow, title);
        logWindow.DockAreas = DockAreas.Document | DockAreas.Float;

        _tabController.AddWindow(logWindow, title, doNotAddToPanel);

        if (!doNotAddToPanel)
        {
            logWindow.Visible = true;
        }
    }

    private void ConnectEventHandlers (LogWindow.LogWindow logWindow)
    {
        logWindow.FileSizeChanged += OnFileSizeChanged;
        logWindow.TailFollowed += OnTailFollowed;
        logWindow.Disposed += OnLogWindowDisposed;
        logWindow.FileNotFound += OnLogWindowFileNotFound;
        logWindow.FileRespawned += OnLogWindowFileRespawned;
        logWindow.FilterListChanged += OnLogWindowFilterListChanged;
        logWindow.CurrentHighlightGroupChanged += OnLogWindowCurrentHighlightGroupChanged;
        logWindow.SyncModeChanged += OnLogWindowSyncModeChanged;
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
    }

    /// <summary>
    /// Subscribes to the events that drive the shell's singleton widgets — the status line, the
    /// progress bar and the GUI state. Only the <em>active</em> window may drive them, so unlike
    /// <see cref="ConnectEventHandlers"/> (which lives for as long as the window does) this pair is
    /// bound and unbound on every activation change.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void ConnectActiveWindow (LogWindow.LogWindow logWindow)
    {
        logWindow.StatusLineEvent += OnStatusLineEvent;
        logWindow.ProgressBarUpdate += OnProgressBarUpdate;
        logWindow.GuiStateUpdate += OnGuiStateUpdate;
    }

    /// <summary>
    /// Mirror of <see cref="ConnectActiveWindow"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void DisconnectActiveWindow (LogWindow.LogWindow logWindow)
    {
        logWindow.StatusLineEvent -= OnStatusLineEvent;
        logWindow.ProgressBarUpdate -= OnProgressBarUpdate;
        logWindow.GuiStateUpdate -= OnGuiStateUpdate;
    }

    [SupportedOSPlatform("windows")]
    private void FillHistoryMenu ()
    {
        _menuToolbarController.PopulateFileHistory(ConfigManager.Settings.FileHistoryList);
    }

    /// <summary>
    /// Removes a LogWindow from the tab system. Delegates to TabController for removal and cleanup.
    /// </summary>
    /// <param name="logWindow">The window to remove</param>
    [SupportedOSPlatform("windows")]
    private void RemoveLogWindow (LogWindow.LogWindow logWindow)
    {
        _tabController.RemoveWindow(logWindow);
    }

    [SupportedOSPlatform("windows")]
    private void RemoveAndDisposeLogWindow (LogWindow.LogWindow logWindow, bool dontAsk)
    {
        _tabController.RemoveWindow(logWindow);

        logWindow.Close(dontAsk);
    }

    [SupportedOSPlatform("windows")]
    private void ShowHighlightSettingsDialog ()
    {
        using HighlightDialog dlg = new(ConfigManager)
        {
            KeywordActionList = PluginRegistry.PluginRegistry.Instance.RegisteredKeywordActions,
            Owner = this,
            TopMost = TopMost,
            HighlightGroupList = HighlightGroupList,
            PreSelectedGroupName = highlightGroupsToolStripComboBox.Text
        };

        var res = dlg.ShowDialog();

        if (res == DialogResult.OK)
        {
            HighlightGroupList = dlg.HighlightGroupList;
            FillHighlightComboBox();
            ConfigManager.Settings.Preferences.HighlightGroupList = HighlightGroupList;
            ConfigManager.Settings.Preferences.SelectionHighlight = dlg.SelectionHighlight;
            ConfigManager.Save(SettingsFlags.HighlightSettings);
            OnHighlightSettingsChanged();
        }
    }

    [SupportedOSPlatform("windows")]
    private void FillHighlightComboBox ()
    {
        var groups = HighlightGroupList.Select(g => g.GroupName);
        var selected = highlightGroupsToolStripComboBox.Text;
        _menuToolbarController.UpdateHighlightGroups(groups, selected);
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
                    _logger.Warn(string.Format(CultureInfo.InvariantCulture, Resources.LogExpert_Common_Error_InsufficientRights_For_Parameter_ErrorMessage, nameof(Environment.GetFolderPath), e));
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
                var decision = _fileOperationService.LoadFilesWithOption(openFileDialog.FileNames, false);
                if (decision == MultiFileDecision.AskUser)
                {
                    MultiLoadRequestDialog dlg = new();
                    var res = dlg.ShowDialog();
                    var sortedNames = openFileDialog.FileNames;
                    Array.Sort(sortedNames);

                    if (res == DialogResult.Yes)
                    {
                        _fileOperationService.AddFileTabs(sortedNames);
                    }
                    else if (res == DialogResult.No)
                    {
                        _ = _fileOperationService.AddMultiFileTab(sortedNames);
                    }
                }
            }
        }
    }

    private void SetColumnizerHistoryEntry (string fileName, ILogLineMemoryColumnizer columnizer)
    {
        var entry = _logWindowCoordinator.FindColumnizerHistoryEntry(fileName);
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

        // Asymmetric on purpose: BindActiveLogWindow() owns connecting, because the connect must also
        // run when the guard above short-circuits on re-activation of the already-current tab. The
        // disconnect stays here, because ChangeCurrentLogWindow(null) — from OnTabControllerWindowRemoved
        // and OnLogWindowDisposed — is the only path that unbinds a removed active window, and there is
        // no activation to hang it off.
        if (oldLogWindow != null)
        {
            DisconnectActiveWindow(oldLogWindow);
            DisconnectToolWindows();
        }

        if (newLogWindow != null)
        {
            ConnectActiveWindow(newLogWindow);

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
            timeshiftToolStripTextBox.Text = string.Empty;
            timeshiftToolStripTextBox.Enabled = false;
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

    /// <summary>
    /// Binds the shell to its active Log Window: makes it current and connects the tool windows.
    /// Both activation sources — the TabController's WindowActivated event and DockPanelSuite's
    /// ActiveContentChanged — reach this through the <see cref="CurrentLogWindow"/> setter, so the
    /// binding cannot drift between them. (They still differ in what else they do on activation;
    /// only the binding is shared.)
    /// <para>
    /// Distinct from <see cref="ITabController.ActivateWindow"/>, which <em>causes</em> activation by
    /// bringing a tab to the front. This is the shell's <em>response</em> to it.
    /// </para>
    /// <para>
    /// <see cref="ChangeCurrentLogWindow"/> is guarded and no-ops when the window is already current;
    /// the tool-window connect is not, because re-activating the current tab must still re-seat the
    /// bookmark window's data. <see cref="ToolWindowCoordinator.Connect"/> disconnects any previous
    /// window first, so calling it repeatedly is safe.
    /// </para>
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void BindActiveLogWindow (LogWindow.LogWindow logWindow)
    {
        ChangeCurrentLogWindow(logWindow);

        if (logWindow != null)
        {
            ConnectToolWindows(logWindow);
        }
    }

    private void ConnectToolWindows (LogWindow.LogWindow logWindow)
    {
        _toolWindowCoordinator.Connect(logWindow);
    }

    private void DisconnectToolWindows ()
    {
        _toolWindowCoordinator.Disconnect();
    }

    [SupportedOSPlatform("windows")]
    private void GuiStateUpdateWorker (GuiStateEventArgs e)
    {
        _skipEvents = true;
        _menuToolbarController.UpdateGuiState(e, ConfigManager.Settings.Preferences.TimestampControl);
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
                _logger.Error(string.Format(CultureInfo.InvariantCulture, Resources.LogExpert_Common_Error_5Parameters_ErrorDuring0Value1Min2Max3Visible45, e.Value, e.MinValue, e.MaxValue, e.Visible, ex), ex);
            }

            _ = Invoke(new MethodInvoker(statusStrip.Refresh));
        }
    }

    [SupportedOSPlatform("windows")]
    private void StatusLineEventWorker (StatusLineEventArgs e)
    {
        if (e != null)
        {
#if DEBUG
            _logger.Debug("StatusLineEvent: text = " + e.StatusText);
#endif
            labelStatus.Text = e.StatusText;
            labelStatus.Size = TextRenderer.MeasureText(labelStatus.Text, labelStatus.Font);
            labelLines.Text = $"{e.LineCount} {Resources.LogTabWindow_StatusLineText_lowerCase_Lines}";
            labelLines.Size = TextRenderer.MeasureText(labelLines.Text, labelLines.Font);
            labelSize.Text = Util.GetFileSizeAsText(e.FileSize);
            labelSize.Size = TextRenderer.MeasureText(labelSize.Text, labelSize.Font);
            labelCurrentLine.Text = $"{Resources.LogTabWindow_StatusLineText_UpperCase_Lines} {e.CurrentLineNum}";
            labelCurrentLine.Size = TextRenderer.MeasureText(labelCurrentLine.Text, labelCurrentLine.Font);

            if (statusStrip.InvokeRequired)
            {
                _ = statusStrip.BeginInvoke(new MethodInvoker(statusStrip.Refresh));
            }
            else
            {
                statusStrip.Refresh();
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void FileNotFound (LogWindow.LogWindow logWin)
    {
        var data = logWin.Tag as LogWindowData;
        _ = BeginInvoke(SetTabIcon, logWin, _deadIcon);
        dragControlDateTime.Visible = false;
    }

    [SupportedOSPlatform("windows")]
    private void FileRespawned (LogWindow.LogWindow logWin)
    {
        var data = logWin.Tag as LogWindowData;
        data.LedState.DiffSum = 0;
        var icon = GetLedIcon(0, data);
        _ = BeginInvoke(SetTabIcon, logWin, icon);
    }

    [SupportedOSPlatform("windows")]
    private void SetTabIcon (LogWindow.LogWindow logWindow, Icon icon)
    {
        if (logWindow == null || logWindow.IsDisposed)
        {
            return;
        }

        if (icon == null)
        {
            logWindow.Icon = null;
            return;
        }

        try
        {
            //Accessing Handle makes sure it is not disposed,
            //if it is, the ObjectDisposedException is thrown
            _ = icon.Handle;
            logWindow.Icon = (Icon)icon.Clone();
        }
        catch (ObjectDisposedException)
        {
            //Icon Disposed
            return;
        }

        if (logWindow.Tag is LogWindowData data && data.OwnedIcon != null)
        {
            data.OwnedIcon.Dispose();
        }

        if (logWindow.Tag is LogWindowData logWindowData)
        {
            logWindowData.OwnedIcon = logWindow.Icon;
        }

        logWindow.DockHandler.Pane?.TabStripControl.Invalidate(false);

    }

    /// <summary>
    /// Gets the appropriate LED icon based on the difference sum and LED state.
    /// </summary>
    /// <param name="diffSum">The difference sum value used to determine the icon state.</param>
    /// <param name="data">The log window data containing the LED state information.</param>
    /// <returns>An <see cref="Icon"/> representing the current LED state.</returns>
    private Icon GetLedIcon (int diffSum, LogWindowData data)
    {
        return _ledService.GetIcon(diffSum, data.LedState);
    }

    [SupportedOSPlatform("windows")]
    private void RefreshEncodingMenuBar (Encoding encoding)
    {
        _menuToolbarController.UpdateEncodingMenu(encoding);
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
        ApplySettings(ConfigManager.Settings, flags);

        var setLastColumnWidth = ConfigManager.Settings.Preferences.SetLastColumnWidth;
        var lastColumnWidth = ConfigManager.Settings.Preferences.LastColumnWidth;

        foreach (var logWindow in _tabController.GetAllWindows())
        {
            logWindow.PreferencesChanged(ConfigManager.Settings.Preferences.Font, setLastColumnWidth, lastColumnWidth, false, flags);
        }

        _toolWindowCoordinator.ApplyPreferences(ConfigManager.Settings.Preferences.Font, setLastColumnWidth, lastColumnWidth, flags);

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
        _ledService.RegenerateIcons(preferences.ShowTailColor);

        foreach (var logWindow in _tabController.GetAllWindows())
        {
            var data = logWindow.Tag as LogWindowData;
            var icon = GetLedIcon(data.LedState.DiffSum, data);
            _ = BeginInvoke(SetTabIcon, logWindow, icon);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void SetToolIcon (ToolEntry entry, ToolStripItem item)
    {
        var icon = NativeMethods.LoadIconFromExe(entry.IconFile, entry.IconIndex);

        if (icon != null)
        {
            item.Image = icon.ToBitmap();

            item.DisplayStyle = item is ToolStripMenuItem
                ? ToolStripItemDisplayStyle.ImageAndText
                : ToolStripItemDisplayStyle.Image;

            _ = Vanara.PInvoke.User32.DestroyIcon(icon.Handle);
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

        ToolLaunchRequest request;

        if (CurrentLogWindow != null)
        {
            var line = CurrentLogWindow.GetCurrentLine();
            var info = CurrentLogWindow.GetCurrentFileInfo();
            if (line == null || info == null)
            {
                return;
            }

            ArgParser parser = new(toolEntry.Args);
            var argLine = parser.BuildArgs(line, CurrentLogWindow.GetRealLineNum() + 1, info, this);
            if (argLine == null)
            {
                return;
            }

            request = new ToolLaunchRequest
            {
                Cmd = toolEntry.Cmd,
                Args = argLine,
                SysoutPipe = toolEntry.Sysout,
                ColumnizerName = toolEntry.ColumnizerName,
                WorkingDir = toolEntry.WorkingDir
            };
        }
        else
        {
            if (toolEntry.Sysout)
            {
                _ = MessageBox.Show(Resources.LogTabWindow_UI_Message_NoLogfileWithSysOutPipeToolConfigured, Resources.LogExpert_Common_UI_Title_LogExpert);
            }

            request = new ToolLaunchRequest
            {
                Cmd = toolEntry.Cmd,
                Args = string.Empty,
                SysoutPipe = false,
                ColumnizerName = toolEntry.ColumnizerName,
                WorkingDir = toolEntry.WorkingDir
            };
        }

        var result = _toolLaunchService.Launch(request);

        if (result.HasError)
        {
            _ = MessageBox.Show(result.ErrorMessage, Resources.LogExpert_Common_UI_Title_LogExpert);
            return;
        }

        if (result.PipeFileName != null)
        {
            var title = CurrentLogWindow!.IsTempFile
                ? CurrentLogWindow.TempTitleName
                : $"{Util.GetNameFromPath(CurrentLogWindow.FileName)}{Resources.LogTabWindow_UI_LogWindow_Title_ExternalStartTool_Suffix}";

            var logWin = AddTempFileTab(result.PipeFileName, title);
            if (result.Columnizer != null)
            {
                logWin.ForceColumnizer(result.Columnizer);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void CloseAllTabs ()
    {
        _tabController.CloseAllWindows();
    }

    //TODO Reimplementation needs a new UI Framework since, DockpanelSuite has no easy way to change TabColor
    //private static void SetTabColor (LogWindow.LogWindow logWindow, Color color)
    //{
    //    //tabPage.BackLowColor = color;
    //    //tabPage.BackLowColorDisabled = Color.FromArgb(255,
    //    //  Math.Max(0, color.R - 50),
    //    //  Math.Max(0, color.G - 50),
    //    //  Math.Max(0, color.B - 50)
    //    //  );
    //}

    [SupportedOSPlatform("windows")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "no need for the other switch cases")]
    private void LoadSession (string sessionFileName, bool restoreLayout)
    {
        var outcome = _sessionHandler.LoadSession(sessionFileName);
        bool openedTabs = false;

        switch (outcome.Status)
        {
            case SessionLoadOutcome.LoadStatus.Error:
                {
                    ShowOkMessage(outcome.ErrorMessage ?? Resources.LogExpert_Common_UI_Title_Error,
                                  Resources.LoadSession_UI_Message_Error_Title_SessionLoadFailed,
                                  MessageBoxIcon.Error);
                    return;
                }
            case SessionLoadOutcome.LoadStatus.EmptySession:
                {
                    ShowOkMessage(outcome.ErrorMessage ?? Resources.LoadSession_UI_Message_Error_Title_SessionLoadFailed,
                                  Resources.LoadSession_UI_Message_Message_FilesForSessionCouldNotBeFound,
                                  MessageBoxIcon.Error);
                    return;
                }
            case SessionLoadOutcome.LoadStatus.NeedsIntervention:
                {
                    var (dialogResult, updateSessionFile, selectedAlternatives) =
                    MissingFilesDialog.ShowDialog(outcome.ValidationResult!, outcome.HasLayoutData);

                    if (dialogResult == MissingFilesDialogResult.Cancel)
                    {
                        return;
                    }

                    if (dialogResult == MissingFilesDialogResult.IgnoreLayout)
                    {
                        restoreLayout = false;
                    }

                    var resolution = new MissingFilesResolution
                    {
                        CloseAllTabs = dialogResult == MissingFilesDialogResult.CloseTabsAndRestoreLayout,
                        OpenInNewWindow = dialogResult == MissingFilesDialogResult.OpenInNewWindow,
                        UpdateSessionFile = updateSessionFile,
                        SelectedAlternatives = selectedAlternatives
                    };

                    // ContinueLoad closes existing tabs (when requested) before opening the
                    // new session windows, so no post-hoc CloseAllTabs() is needed here.
                    var interventionResult = _sessionHandler.ContinueLoad(outcome, resolution, restoreLayout);

                    if (updateSessionFile)
                    {
                        ShowOkMessage(Resources.LoadSession_UI_Message_Error_Message_UpdateSessionFile,
                                      Resources.LoadSession_UI_Message_Error_Title_UpdateSessionFile,
                                      MessageBoxIcon.Information);
                    }

                    if (interventionResult.OpenInNewWindowFiles is not null)
                    {
                        LogExpertProxy.NewWindow([.. interventionResult.OpenInNewWindowFiles]);
                        return;
                    }

                    openedTabs = interventionResult.OpenedTabs;
                    break;
                }
            case SessionLoadOutcome.LoadStatus.Success:
                {
                    openedTabs = _sessionHandler.ContinueLoad(outcome, null, restoreLayout).OpenedTabs;
                    break;
                }
        }

        if (restoreLayout && outcome.HasLayoutData && openedTabs)
        {
            _logger.Info("Restoring layout");
            DestroyBookmarkWindow();
            InitToolWindows();
            RestoreLayout(outcome.LayoutXml!);
        }
        else if (!openedTabs)
        {
            _logger.Warn("No files loaded, skipping layout restoration");
        }
    }

    private static void ShowOkMessage (string title, string message, MessageBoxIcon icon)
    {
        _ = MessageBox.Show(
            message,
            title,
            MessageBoxButtons.OK,
            icon);
    }

    [SupportedOSPlatform("windows")]
    private void ApplySelectedHighlightGroup ()
    {
        var groupName = highlightGroupsToolStripComboBox.Text;
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
        _ = toolsToolStripMenuItem.DropDownItems.Add(configureToolStripMenuItem);
        _ = toolsToolStripMenuItem.DropDownItems.Add(configureToolStripSeparator);
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
                _ = externalToolsToolStrip.Items.Add(button);
            }

            num++;
            ToolStripMenuItem menuItem = new(tool.Name)
            {
                Tag = tool
            };

            SetToolIcon(tool, menuItem);
            _ = toolsToolStripMenuItem.DropDownItems.Add(menuItem);
        }

        externalToolsToolStrip.ResumeLayout();

        externalToolsToolStrip.Visible = num > 0; // do not show bar if no tool uses it
    }

    private static void RunGC ()
    {
        _logger.Info($"Running GC. Used mem before: {GC.GetTotalMemory(false):N0}");
        GC.Collect();
        _logger.Info($"GC done.    Used mem after:  {GC.GetTotalMemory(true):N0}");
    }

    private static void DumpGCInfo ()
    {
        _logger.Info($"-------- GC info -----------\r\nUsed mem: {GC.GetTotalMemory(false):N0}");
        for (var i = 0; i < GC.MaxGeneration; ++i)
        {
            _logger.Info($"Generation {i} collect count: {GC.CollectionCount(i)}");
        }

        _logger.Info(CultureInfo.InvariantCulture, "----------------------------");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "For Debug Purposes")]
    private void ThrowExceptionFx ()
    {
        throw new Exception(Resources.LogTabWindow_ThrowTestException_ThisIsATestExceptionThrownByAnAsyncDelegate);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "For Debug Purposes")]
    private void ThrowExceptionThreadFx ()
    {
        throw new Exception(Resources.LogTabWindow_ThrowTestExceptionThread_ThisIsATestExceptionThrownByABackgroundThread);
    }

    private string SaveLayout ()
    {
        using MemoryStream memStream = new(2000);
        using StreamReader r = new(memStream);
        dockPanel.SaveAsXml(memStream, Encoding.UTF8, true);

        _ = memStream.Seek(0, SeekOrigin.Begin);
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

        _ = memStream.Seek(0, SeekOrigin.Begin);

        try
        {
            dockPanel.LoadFromXml(memStream, DeserializeDockContent, true);
        }
        catch (InvalidOperationException e)
        {
            _logger.Warn($"Layout restoration failed, showing windows with default layout: {e.Message}");
            ShowDeferredWindows();
        }
    }

    private void ShowDeferredWindows ()
    {
        foreach (var window in _tabController.GetAllWindows().Where(w => w.DockPanel == null))
        {
            window.Show(dockPanel);
        }
    }

    [SupportedOSPlatform("windows")]
    private IDockContent DeserializeDockContent (string persistString)
    {
        var toolContent = _toolWindowCoordinator.GetDockContent(persistString);
        if (toolContent != null)
        {
            return toolContent;
        }

        if (persistString.StartsWith(WindowTypes.LogWindow.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var fileName = persistString[(WindowTypes.LogWindow.ToString().Length + 1)..];
            var win = _fileOperationService.FindWindowForFile(fileName);
            if (win != null)
            {
                return win;
            }
        }

        return null;
    }

    private void OnHighlightSettingsChanged ()
    {
        _logWindowCoordinator.OnHighlightSettingsChanged();
    }

    #endregion

    #region Events handler

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

        var lastOpenFiles = ObjectClone.Clone(ConfigManager.Settings.LastOpenFilesList);
        _fileOperationService.LoadStartupFiles(lastOpenFiles, _startupFileNames);

        FillHighlightComboBox();
        FillToolLauncherBar();

        //TODO Change to Debug and true
#if !DEBUG
        debugToolStripMenuItem.Visible = false;
#endif
    }

    private void OnLogTabWindowFormClosing (object sender, CancelEventArgs e)
    {
        try
        {
            IList<LogWindow.LogWindow> deleteLogWindowList = [];
            ConfigManager.Settings.AlwaysOnTop = TopMost && ConfigManager.Settings.Preferences.AllowOnlyOneInstance;
            _fileOperationService.SaveLastOpenFilesList();

            foreach (var logWindow in _tabController.GetAllWindows())
            {
                RemoveAndDisposeLogWindow(logWindow, true);
            }

            DestroyBookmarkWindow();

            ConfigManager.ConfigChanged -= OnConfigChanged;

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
                foreach (var logWindow in _tabController.GetAllWindows())
                {
                    if (logWindow.CurrentColumnizer.GetType() != form.SelectedColumnizer.GetType())
                    {
                        //logWindow.SetColumnizer(form.SelectedColumnizer);
                        _ = logWindow.Invoke(logWindow.ForceColumnizer, form.SelectedColumnizer);
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
            else
            {
                if (CurrentLogWindow.CurrentColumnizer.GetType() != form.SelectedColumnizer.GetType())
                {
                    _ = CurrentLogWindow.Invoke(CurrentLogWindow.ForceColumnizer, form.SelectedColumnizer);
                    SetColumnizerHistoryEntry(CurrentLogWindow.FileName, form.SelectedColumnizer);
                }

                if (form.IsConfigPressed)
                {
                    foreach (var logWindow in _tabController.GetAllWindows())
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
        var s = "Dragging something over LogExpert. Formats: ";
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
        e.Effect = _fileOperationService.CanHandleDrop(e.Data)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnLogWindowDragDrop (object sender, DragEventArgs e)
    {
#if DEBUG
        var formats = e.Data.GetFormats();
        var s = "Dropped formats: ";
        foreach (var format in formats)
        {
            s += format;
            s += " , ";
        }

        s = s[..^3];
        _logger.Debug(s);
#endif

        if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] names)
        {
            // (shift pressed) https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.drageventargs.keystate
            var invertLogic = (e.KeyState & 4) == 4;
            var decision = _fileOperationService.LoadFilesWithOption(names, invertLogic);

            if (decision == MultiFileDecision.AskUser)
            {
                MultiLoadRequestDialog dlg = new();
                var res = dlg.ShowDialog();

                if (res == DialogResult.Yes)
                {
                    _fileOperationService.AddFileTabs(names);
                }
                else if (res == DialogResult.No)
                {
                    _ = _fileOperationService.AddMultiFileTab(names);
                }
            }

            e.Effect = DragDropEffects.Copy;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnTimeShiftToolStripMenuItemCheckStateChanged (object sender, EventArgs e)
    {
        if (!_skipEvents && CurrentLogWindow != null)
        {
            CurrentLogWindow.SetTimeshiftValue(timeshiftToolStripTextBox.Text);
            timeshiftToolStripTextBox.Enabled = timeshiftToolStripMenuItem.Checked;
            CurrentLogWindow.TimeshiftEnabled(timeshiftToolStripMenuItem.Checked,
                timeshiftToolStripTextBox.Text);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnAboutToolStripMenuItemClick (object sender, EventArgs e)
    {
        AboutBox aboutBox = new()
        {
            TopMost = TopMost
        };

        _ = aboutBox.ShowDialog();
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
    private void OnGuiStateUpdate (object sender, GuiStateEventArgs e)
    {
        _ = BeginInvoke(GuiStateUpdateWorker, e);
    }

    private void OnProgressBarUpdate (object sender, ProgressEventArgs e)
    {
        _ = Invoke(ProgressBarUpdateWorker, e);
    }

    private void OnStatusLineEvent (object sender, StatusLineEventArgs e)
    {
        if (InvokeRequired)
        {
            _ = BeginInvoke(() => StatusLineEventWorker(e));
            return;
        }

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
            CurrentLogWindow.SetTimeshiftValue(timeshiftToolStripTextBox.Text);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnAlwaysOnTopToolStripMenuItemClick (object sender, EventArgs e)
    {
        TopMost = alwaysOnTopToolStripMenuItem.Checked;
    }

    private void OnFileSizeChanged (object sender, LogEventArgs e)
    {
        if (sender is not LogWindow.LogWindow logWindow)
        {
            return;
        }

        if (logWindow.Tag is not LogWindowData)
        {
            return;
        }

        var diff = e.LineCount - e.PrevLineCount;
        if (diff < 0)
        {
            return;
        }

        _ledService.UpdateWindowActivity(logWindow, diff);
    }

    private void OnLogWindowFileNotFound (object sender, EventArgs e)
    {
        _ = Invoke(FileNotFound, sender);
    }

    private void OnLogWindowFileRespawned (object sender, EventArgs e)
    {
        _ = Invoke(FileRespawned, sender);
    }

    private void OnLogWindowFilterListChanged (object sender, FilterListChangedEventArgs e)
    {
        foreach (var logWindow in _tabController.GetAllWindows())
        {
            if (logWindow != e.LogWindow)
            {
                logWindow.HandleChangedFilterList();
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
                data.LedState.IsDirty = false;
                var icon = GetLedIcon(data.LedState.DiffSum, data);
                _ = BeginInvoke(SetTabIcon, (LogWindow.LogWindow)sender, icon);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnLogWindowSyncModeChanged (object sender, SyncModeEventArgs e)
    {
        if (!Disposing)
        {
            var data = ((LogWindow.LogWindow)sender).Tag as LogWindowData;
            data.LedState.SyncState = e.IsTimeSynced
                ? TimeSyncState.Synced
                : TimeSyncState.NotSynced;

            var icon = GetLedIcon(data.LedState.DiffSum, data);
            _ = BeginInvoke(SetTabIcon, (LogWindow.LogWindow)sender, icon);
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

    /// <summary>
    /// One handler for every row of the Encoding menu: the clicked row carries the encoding it stands
    /// for, so this does not know which encodings are offered.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void OnEncodingToolStripMenuItemClick (object sender, EventArgs e)
    {
        var encoding = EncodingMenuBuilder.EncodingOf(sender as ToolStripItem);

        if (encoding != null)
        {
            CurrentLogWindow?.ChangeEncoding(encoding);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnReloadToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (CurrentLogWindow != null)
        {
            var data = CurrentLogWindow.Tag as LogWindowData;
            var icon = GetLedIcon(0, data);
            _ = BeginInvoke(SetTabIcon, CurrentLogWindow, icon);
            CurrentLogWindow.Reload();
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnSettingsToolStripMenuItemClick (object sender, EventArgs e)
    {
        OpenSettings(0);
    }

    [SupportedOSPlatform("windows")]
    private void OnPluginTrustToolStripMenuItemClick (object sender, EventArgs e)
    {
        using var dialog = new PluginTrustDialog(this, ConfigManager);
        var result = dialog.ShowDialog();

        if (result == DialogResult.OK)
        {
            var restartPrompt = MessageBox.Show(
                Resources.LogTabWindow_UI_Message_PluginTrustConfigurationUpdate,
                Resources.LogTabWindow_UI_Title_RestartRecommended,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (restartPrompt == DialogResult.Yes)
            {
                Application.Restart();
            }
        }
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
        _ = CurrentLogWindow?.ScrollToTimestamp(dragControlDateTime.DateTime, true, true);
    }

    [SupportedOSPlatform("windows")]
    private void OnLogTabWindowDeactivate (object sender, EventArgs e)
    {
        CurrentLogWindow?.AppFocusLost();
    }

    [SupportedOSPlatform("windows")]
    private void OnLogTabWindowActivated (object sender, EventArgs e)
    {
        LogExpertProxy?.NotifyWindowActivated(this);
        CurrentLogWindow?.AppFocusGained();
    }

    [SupportedOSPlatform("windows")]
    private void OnShowBookmarkListToolStripMenuItemClick (object sender, EventArgs e)
    {
        _toolWindowCoordinator.ToggleBookmarkVisibility(dockPanel);
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
        Help.ShowHelp(this, Resources.LogTabWindow_HelpFile);
    }

    private void OnHideLineColumnToolStripMenuItemClick (object sender, EventArgs e)
    {
        ConfigManager.Settings.HideLineColumn = hideLineColumnToolStripMenuItem.Checked;

        foreach (var logWin in _tabController.GetAllWindows())
        {
            logWin.ShowLineColumn(!ConfigManager.Settings.HideLineColumn);
        }

        _toolWindowCoordinator.SetLineColumnVisible(!ConfigManager.Settings.HideLineColumn);
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
        var activeWindow = _tabController.GetActiveWindow();
        var closeList = _tabController.GetAllWindowsFromDockPanel()
                .Where(window => window != activeWindow)
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
        //Todo TabColoring must be reimplemented with a different UI Framework
        //var logWindow = dockPanel.ActiveContent as LogWindow.LogWindow;

        //if (logWindow.Tag is not LogWindowData data)
        //{
        //    return;
        //}

        //ColorDialog dlg = new()
        //{
        //    Color = data.Color
        //};

        //if (dlg.ShowDialog() == DialogResult.OK)
        //{
        //    data.Color = dlg.Color;
        //    //SetTabColor(logWindow, data.Color);
        //}

        //List<ColorEntry> delList = [];

        //foreach (var entry in ConfigManager.Settings.FileColors)
        //{
        //    if (entry.FileName.Equals(logWindow.FileName, StringComparison.Ordinal))
        //    {
        //        delList.Add(entry);
        //    }
        //}

        //foreach (var entry in delList)
        //{
        //    _ = ConfigManager.Settings.FileColors.Remove(entry);
        //}

        //ConfigManager.Settings.FileColors.Add(new ColorEntry(logWindow.FileName, dlg.Color));

        //while (ConfigManager.Settings.FileColors.Count > MAX_COLOR_HISTORY)
        //{
        //    ConfigManager.Settings.FileColors.RemoveAt(0);
        //}
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
    private void OnSaveSessionToolStripMenuItemClick (object sender, EventArgs e)
    {
        SaveFileDialog dlg = new()
        {
            DefaultExt = "lxj",
            Filter = string.Format(CultureInfo.InvariantCulture, Resources.LogTabWindow_UI_Session_Default_Filter, "(*.lxj)|*.lxj")
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var fileName = dlg.FileName;
            List<string> fileNames = [];

            foreach (var logWin in _tabController.GetAllWindowsFromDockPanel())
            {
                var persistenceFileName = logWin?.SavePersistenceDataAndReturnFileName(true);
                if (persistenceFileName != null)
                {
                    fileNames.Add(persistenceFileName);
                }
            }

            SessionData sessionData = new()
            {
                FileNames = fileNames,
                TabLayoutXml = SaveLayout()
            };

            if (!_sessionHandler.SaveSession(fileName, sessionData, out var errorMessage))
            {
                ShowOkMessage(errorMessage ?? Resources.LogExpert_Common_UI_Title_Error,
                              Resources.LogExpert_Common_UI_Title_Error,
                              MessageBoxIcon.Error);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnLoadSessionToolStripMenuItemClick (object sender, EventArgs e)
    {
        OpenFileDialog dlg = new()
        {
            DefaultExt = "lxj",
            Filter = string.Format(CultureInfo.InvariantCulture, Resources.LogTabWindow_UI_Session_Default_Filter, "(*.lxj)|*.lxj")
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var sessionFileName = dlg.FileName;
            LoadSession(sessionFileName, true);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnToolStripButtonBubblesClick (object sender, EventArgs e)
    {
        _ = CurrentLogWindow?.ShowBookmarkBubbles = toolStripButtonBubbles.Checked;
    }

    [SupportedOSPlatform("windows")]
    private void OnCopyPathToClipboardToolStripMenuItemClick (object sender, EventArgs e)
    {
        if (dockPanel.ActiveContent is not LogWindow.LogWindow logWindow)
        {
            return;
        }

        if (!ClipboardHelper.TrySetText(logWindow.Title))
        {
            _ = MessageBox.Show(this, Resources.LogExpert_Common_UI_Message_ClipboardInUse, Resources.LogExpert_Common_UI_Title_LogExpert, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnFindInExplorerToolStripMenuItemClick (object sender, EventArgs e)
    {
        var logWindow = dockPanel.ActiveContent as LogWindow.LogWindow;

        Process explorer = new();
        explorer.StartInfo.FileName = "explorer.exe";
        explorer.StartInfo.Arguments = "/e,/select," + logWindow.Title;
        explorer.StartInfo.UseShellExecute = false;
        _ = explorer.Start();
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "For Debug Purposes")]
    private void OnThrowExceptionGUIThreadToolStripMenuItemClick (object sender, EventArgs e)
    {
        throw new Exception(Resources.LogTabWindow_OnThrowTestExceptionGUIThread_ThisIsATestExceptionThrownByTheGUIThread);
    }

    private void OnThrowExceptionBackgroundThToolStripMenuItemClick (object sender, EventArgs e)
    {
        _ = Task.Run(ThrowExceptionFx);
    }

    private void OnThrowExceptionBackgroundThreadToolStripMenuItemClick (object sender, EventArgs e)
    {
        Thread thread = new(ThrowExceptionThreadFx)
        {
            IsBackground = true
        };

        thread.Start();
    }

    private void OnLedIconChanged (object sender, IconChangedEventArgs e)
    {
        SetTabIcon(e.Window, e.NewIcon);
    }

    private void OnWarnToolStripMenuItemClick (object sender, EventArgs e)
    {
        //_logger.GetLogger().LogLevel = _logger.Level.WARN;
    }

    private void OnInfoToolStripMenuItemClick (object sender, EventArgs e)
    {
        //_logger.Get_logger().LogLevel = _logger.Level.INFO;
    }

    private void OnDebugLogLevelToolStripMenuItemClick (object sender, EventArgs e)
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
                _fileOperationService.LoadFilesWithOption(new[] { dlg.Uri }, false);
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
            // Unlike OnTabControllerWindowActivated this has no already-current guard, so it also
            // fires when the current tab is re-activated. BindActiveLogWindow re-seats the tool
            // windows in that case even though the window itself does not change.
            CurrentLogWindow = window;
            CurrentLogWindow.LogWindowActivated();
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

    private class LogWindowData
    {
        #region Fields

        // public MdiTabControl.TabPage tabPage;

        public LedState LedState { get; set; } = new();

        public Color Color { get; set; } = Color.FromKnownColor(KnownColor.Gray);

        public ToolTip ToolTip { get; set; }

        public Icon OwnedIcon { get; set; }

        #endregion
    }
}
