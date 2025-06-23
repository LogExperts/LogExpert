using System.Reflection;
using System.Runtime.Versioning;
using System.Text;

using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interface;
using LogExpert.Dialogs;
using LogExpert.UI.Extensions;
using LogExpert.UI.Extensions.Forms;

using NLog;

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

        ChangeTheme(Controls);

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

    #region ColorTheme

    [SupportedOSPlatform("windows")]
    public void ChangeTheme (Control.ControlCollection container)
    {
        ColorMode.LoadColorMode(ConfigManager.Settings.Preferences.DarkMode);
        NativeMethods.UseImmersiveDarkMode(Handle, ColorMode.DarkModeEnabled);

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

            if (component is MenuStrip menu)
            {
                foreach (ToolStripMenuItem item in menu.Items)
                {
                    item.ForeColor = ColorMode.ForeColor;
                    item.BackColor = ColorMode.BackgroundColor;

                    try
                    {
                        for (var x = 0; x < item.DropDownItems.Count; x++)
                        {
                            var children = item.DropDownItems[x];
                            children.ForeColor = ColorMode.ForeColor;
                            children.BackColor = ColorMode.MenuBackgroundColor;

                            if (children is ToolStripDropDownItem toolstripDropDownItem)
                            {
                                for (var y = 0; y < toolstripDropDownItem.DropDownItems.Count; y++)
                                {
                                    var subChildren = toolstripDropDownItem.DropDownItems[y];
                                    subChildren.ForeColor = ColorMode.ForeColor;
                                    subChildren.BackColor = ColorMode.MenuBackgroundColor;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "An error occured while applying style dynamically to all Controls under LogTabWindow:");
                    }
                }
            }
        }
        #endregion

        // Colors for selected menus
        mainMenuStrip.Renderer = new ExtendedMenuStripRenderer();

        // Dock special color
        dockPanel.DockBackColor = ColorMode.DockBackgroundColor;

        // Remove toolstrip bottom border
        buttonToolStrip.Renderer = new ToolStripRendererExtension();

        #region Tabs
        tabContextMenuStrip.Renderer = new ExtendedMenuStripRenderer();

        // Tabs menu
        for (var y = 0; y < tabContextMenuStrip.Items.Count; y++)
        {
            var item = tabContextMenuStrip.Items[y];
            item.ForeColor = ColorMode.ForeColor;
            item.BackColor = ColorMode.MenuBackgroundColor;
        }

        // Tabs line
        dockPanel.Theme.Skin.DockPaneStripSkin.ToolWindowGradient.DockStripGradient.StartColor = ColorMode.TabsBackgroundStripColor;
        dockPanel.Theme.Skin.DockPaneStripSkin.ToolWindowGradient.DockStripGradient.EndColor = ColorMode.TabsBackgroundStripColor;

        dockPanel.Theme.Skin.DockPaneStripSkin.DocumentGradient.DockStripGradient.StartColor = ColorMode.TabsBackgroundStripColor;
        dockPanel.Theme.Skin.DockPaneStripSkin.DocumentGradient.DockStripGradient.EndColor = ColorMode.TabsBackgroundStripColor;

        // Tabs
        dockPanel.Theme.Skin.DockPaneStripSkin.ToolWindowGradient.ActiveTabGradient.StartColor = ColorMode.ActiveTabColor;
        dockPanel.Theme.Skin.DockPaneStripSkin.ToolWindowGradient.ActiveTabGradient.EndColor = ColorMode.ActiveTabColor;
        dockPanel.Theme.Skin.DockPaneStripSkin.ToolWindowGradient.ActiveTabGradient.TextColor = ColorMode.ForeColor;

        dockPanel.Theme.Skin.DockPaneStripSkin.DocumentGradient.ActiveTabGradient.StartColor = ColorMode.ActiveTabColor;
        dockPanel.Theme.Skin.DockPaneStripSkin.DocumentGradient.ActiveTabGradient.EndColor = ColorMode.ActiveTabColor;
        dockPanel.Theme.Skin.DockPaneStripSkin.DocumentGradient.ActiveTabGradient.TextColor = ColorMode.ForeColor;

        dockPanel.Theme.Skin.DockPaneStripSkin.DocumentGradient.InactiveTabGradient.StartColor = ColorMode.InactiveTabColor;
        dockPanel.Theme.Skin.DockPaneStripSkin.DocumentGradient.InactiveTabGradient.EndColor = ColorMode.InactiveTabColor;
        dockPanel.Theme.Skin.DockPaneStripSkin.DocumentGradient.InactiveTabGradient.TextColor = ColorMode.ForeColor;
        #endregion Tabs
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
}