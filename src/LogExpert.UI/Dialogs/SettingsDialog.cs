using System.ComponentModel;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

using ColumnizerLib;

using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;
using LogExpert.Core.Helpers;
using LogExpert.Core.Interfaces;
using LogExpert.UI.ControlCharDisplay;
using LogExpert.UI.Controls.LogTabWindow;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Dialogs.Helpers;
using LogExpert.UI.Entities;
using LogExpert.UI.Extensions;

namespace LogExpert.Dialogs;

//TODO: This class should not know ConfigManager, this needs to be refactored?
//TODO: This class should not be aware of LogTabWindow, only use HighlightGroupList. Refactor to pass IList instead of LogTabWindow?
[SupportedOSPlatform("windows")]
internal partial class SettingsDialog : Form
{
    #region Fields

    private readonly Image _emptyImage = new Bitmap(16, 16);
    private readonly Image _staleImage = SystemIcons.Exclamation.ToBitmap();
    private readonly LogTabWindow _logTabWin;
    private const float DEFAULT_FONT_SIZE = 9.0f;

    private ILogExpertPluginConfigurator _selectedPlugin;
    private ToolEntry _selectedTool;

    private Color _controlCharsForeColor;
    private Color _controlCharsBackColor;
    private readonly Dictionary<int, bool> _controlCharsEnabledByCp = new(33);

    // Codepoint set displayed in the grid (C0 + DEL, 33 rows).
    private static readonly int[] _allDisplayableControlCps = [.. Enumerable.Range(0, 32), 0x7F];

    // Friendly metadata for the grid; tooltip uses the formal Unicode name.
    private static readonly (string Abbr, string Name)[] _controlCharMeta =
    [
        ("NUL", "NULL"), ("SOH", "START OF HEADING"), ("STX", "START OF TEXT"),
        ("ETX", "END OF TEXT"), ("EOT", "END OF TRANSMISSION"), ("ENQ", "ENQUIRY"),
        ("ACK", "ACKNOWLEDGE"), ("BEL", "BELL"), ("BS", "BACKSPACE"),
        ("HT", "HORIZONTAL TABULATION"), ("LF", "LINE FEED"), ("VT", "VERTICAL TABULATION"),
        ("FF", "FORM FEED"), ("CR", "CARRIAGE RETURN"), ("SO", "SHIFT OUT"),
        ("SI", "SHIFT IN"), ("DLE", "DATA LINK ESCAPE"), ("DC1", "DEVICE CONTROL ONE"),
        ("DC2", "DEVICE CONTROL TWO"), ("DC3", "DEVICE CONTROL THREE"),
        ("DC4", "DEVICE CONTROL FOUR"), ("NAK", "NEGATIVE ACKNOWLEDGE"),
        ("SYN", "SYNCHRONOUS IDLE"), ("ETB", "END OF TRANSMISSION BLOCK"),
        ("CAN", "CANCEL"), ("EM", "END OF MEDIUM"), ("SUB", "SUBSTITUTE"),
        ("ESC", "ESCAPE"), ("FS", "FILE SEPARATOR"), ("GS", "GROUP SEPARATOR"),
        ("RS", "RECORD SEPARATOR"), ("US", "UNIT SEPARATOR"),
        // index 32 maps to 0x7F
        ("DEL", "DELETE"),
    ];

    #endregion

    #region cTor

    private SettingsDialog (Preferences prefs, LogTabWindow logTabWin)
    {
        SuspendLayout();

        Preferences = prefs;
        _logTabWin = logTabWin; //TODO: uses only HighlightGroupList. Can we pass IList instead?

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();
        InitializeMarkerBar();

        var darkMode = Application.IsDarkModeEnabled;
        PaintHelper.ApplyTabControlTheme(tabControlSettings, darkMode);
        PaintHelper.ApplyGridViewTheme(dataGridViewColumnizer, darkMode);
        PaintHelper.ApplyGridViewTheme(dataGridViewHighlightMask, darkMode);
        PaintHelper.ApplyGridViewTheme(dataGridViewControlChars, darkMode);

        dataGridViewImageColumnColumnizerStale.CellTemplate = new EmptyImageCell();

        LoadResources();

        ResumeLayout();
    }


    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _staleImage?.Dispose();
            _emptyImage?.Dispose();
        }

        base.Dispose(disposing);
    }

    public SettingsDialog (Preferences prefs, LogTabWindow logTabWin, int tabToOpen, IConfigManager configManager) : this(prefs, logTabWin)
    {
        tabControlSettings.SelectedIndex = tabToOpen;
        ConfigManager = configManager;
    }

    #endregion

    #region Properties

    public Preferences Preferences { get; private set; }

    private IConfigManager ConfigManager { get; }

    /// <summary>
    /// The encoding the dialog falls back to when the persisted name is unusable — on both the way in
    /// (nothing to select) and the way out (nothing selected). Has to be one of
    /// <see cref="EncodingRegistry.OfferedEncodings"/>, otherwise the combo box shows a blank selection
    /// and OK writes back a value the list never offered.
    /// </summary>
    internal static Encoding FallbackEncoding { get; } = Encoding.UTF8;

    #endregion

    #region Private Methods

    private void LoadResources ()
    {
        ApplyTextResources();
        ApplyToolTips();
        ApplyFormTitle();

        // Form title
        Text = Resources.SettingsDialog_Form_Text;
    }

    private void ApplyFormTitle ()
    {
        Text = Resources.SettingsDialog_Form_Text;
    }

    private void ApplyToolTips ()
    {
        foreach (var entry in GetToolTipMap())
        {
            toolTip.SetToolTip(entry.Key, entry.Value);
        }
    }

    private void ApplyTextResources ()
    {
        var map = ResourceHelper.GenerateTextMapFromNaming(this, nameof(SettingsDialog), "UI");

        // Add exceptions or unrelated entries manually:
        map[buttonCancel] = Resources.LogExpert_Common_UI_Button_Cancel;
        map[buttonOk] = Resources.LogExpert_Common_UI_Button_OK;
        map[buttonExport] = Resources.LogExpert_Common_UI_Button_Export;
        map[buttonImport] = Resources.LogExpert_Common_UI_Button_Import;

        foreach (var entry in map)
        {
            entry.Key.Text = entry.Value;
        }

        dataGridViewTextBoxColumnFileMask.HeaderText = Resources.SettingsDialog_UI_DataGridViewTextBoxColumn_FileMask;
        dataGridViewComboBoxColumnColumnizer.HeaderText = Resources.SettingsDialog_UI_DataGridViewComboBoxColumn_Columnizer;
        dataGridViewComboBoxColumnColumnizerMaskType.HeaderText = Resources.SettingsDialog_UI_DataGridView_columnHeaderColumnizerMaskType;
        dataGridViewComboBoxColumnColumnizerMaskType.ToolTipText = Resources.SettingsDialog_UI_DataGridView_columnTooltipColumnizerMaskType;
        dataGridViewTextBoxColumnFileName.HeaderText = Resources.SettingsDialog_UI_DataGridViewTextBoxColumn_FileName;
        dataGridViewComboBoxColumnHighlightGroup.HeaderText = Resources.SettingsDialog_UI_DataGridViewComboBoxColumn_HighlightGroup;
    }

    private void FillDialog ()
    {
        Preferences ??= new Preferences();

        FillPortableMode();

        FillColorModeList();
        checkBoxTimestamp.Checked = Preferences.TimestampControl;
        checkBoxSyncFilter.Checked = Preferences.FilterSync;
        checkBoxFilterTail.Checked = Preferences.FilterTail;
        checkBoxFollowTail.Checked = Preferences.FollowTail;

        radioButtonHorizMouseDrag.Checked = Preferences.TimestampControlDragOrientation == DragOrientations.Horizontal;
        radioButtonVerticalMouseDrag.Checked = Preferences.TimestampControlDragOrientation == DragOrientations.Vertical;
        radioButtonVerticalMouseDragInverted.Checked = Preferences.TimestampControlDragOrientation == DragOrientations.InvertedVertical;

        checkBoxSingleInstance.Checked = Preferences.AllowOnlyOneInstance;
        checkBoxOpenLastFiles.Checked = Preferences.OpenLastFiles;
        checkBoxTailState.Checked = Preferences.ShowTailState;
        checkBoxColumnSize.Checked = Preferences.SetLastColumnWidth;
        cpDownColumnWidth.Enabled = Preferences.SetLastColumnWidth;

        if (Preferences.LastColumnWidth != 0)
        {
            if (Preferences.LastColumnWidth < cpDownColumnWidth.Minimum)
            {
                Preferences.LastColumnWidth = (int)cpDownColumnWidth.Minimum;
            }

            if (Preferences.LastColumnWidth > cpDownColumnWidth.Maximum)
            {
                Preferences.LastColumnWidth = (int)cpDownColumnWidth.Maximum;
            }

            cpDownColumnWidth.Value = Preferences.LastColumnWidth;
        }

        checkBoxTimeSpread.Checked = Preferences.ShowTimeSpread;
        checkBoxReverseAlpha.Checked = Preferences.ReverseAlpha;

        radioButtonTimeView.Checked = Preferences.TimeSpreadTimeMode;
        radioButtonLineView.Checked = !Preferences.TimeSpreadTimeMode;

        checkBoxSaveSessions.Checked = Preferences.SaveSessions;

        switch (Preferences.SaveLocation)
        {
            case SessionSaveLocation.OwnDir:
                {
                    radioButtonSessionSaveOwn.Checked = true;
                    break;
                }
            case SessionSaveLocation.SameDir:
                {
                    radioButtonSessionSameDir.Checked = true;
                    break;
                }

            case SessionSaveLocation.DocumentsDir:
                {
                    radioButtonsessionSaveDocuments.Checked = true;
                    break;
                }
            case SessionSaveLocation.ApplicationStartupDir:
                {
                    radioButtonSessionApplicationStartupDir.Checked = true;
                    break;
                }
            case SessionSaveLocation.LoadedSessionFile:
            default:
                // intentionally left blank
                break;
        }

        //overwrite preferences save location in portable mode to always be application startup directory
        if (checkBoxPortableMode.Checked)
        {
            radioButtonSessionApplicationStartupDir.Checked = true;
        }

        //Keep Order or, exception is thrown with upDownMaxDisplayLength.Value because its bigger then maximum
        upDownMaximumLineLength.Value = Preferences.MaxLineLength;

        upDownMaxDisplayLength.Maximum = Math.Min(upDownMaxDisplayLength.Maximum, upDownMaximumLineLength.Value);
        upDownMaxDisplayLength.Value = Math.Min(Preferences.MaxDisplayLength, (int)upDownMaxDisplayLength.Maximum);

        upDownMaximumFilterEntriesDisplayed.Value = Preferences.MaximumFilterEntriesDisplayed;
        upDownMaximumFilterEntries.Value = Preferences.MaximumFilterEntries;

        labelSessionSaveOwnDir.Text = Preferences.SessionSaveDirectory ?? string.Empty;
        checkBoxSaveFilter.Checked = Preferences.SaveFilters;
        upDownBlockCount.Value = Preferences.BufferCount;
        upDownLinesPerBlock.Value = Preferences.LinesPerBuffer;
        upDownPollingInterval.Value = Preferences.PollingInterval;
        checkBoxMultiThread.Checked = Preferences.MultiThreadFilter;

        dataGridViewColumnizer.DataError += OnDataGridViewColumnizerDataError;

        FillColumnizerList();
        FillPluginList();
        DisplayFontName();
        FillHighlightMaskList();
        FillToolListbox();
        FillMultifileSettings();
        FillEncodingList();
        FillLanguageList();
        FillReaderTypeList();
        FillControlCharsTab();
        FillMarkerBarTab();

        comboBoxEncoding.SelectedItem = EncodingRegistry.GetEncoding(Preferences.DefaultEncoding, FallbackEncoding);
        comboBoxLanguage.SelectedItem = CultureInfo.GetCultureInfo(Preferences.DefaultLanguage).Name;

        switch (Preferences.ColumnizerSelectionPriority)
        {
            case ColumnizerSelectionPriority.MaskThenHistory:
                radioColumnizerPriorityMaskThenHistory.Checked = true;
                break;
            case ColumnizerSelectionPriority.MaskOverridesPersistence:
                radioColumnizerPriorityMaskOverridesPersistence.Checked = true;
                break;
            case ColumnizerSelectionPriority.HistoryThenMask:
            default:
                radioColumnizerPriorityHistoryThenMask.Checked = true;
                break;
        }

        checkBoxAutoPick.Checked = Preferences.AutoPick;
        checkBoxAskCloseTabs.Checked = Preferences.AskForClose;
        checkBoxColumnFinder.Checked = Preferences.ShowColumnFinder;

        checkBoxShowErrorMessageOnlyOneInstance.Checked = Preferences.ShowErrorMessageAllowOnlyOneInstances;
    }

    private void FillReaderTypeList ()
    {
        foreach (var readerType in Enum.GetValues<ReaderType>())
        {
            if (!comboBoxReaderType.Items.Contains(readerType))
            {
                _ = comboBoxReaderType.Items.Add(readerType);
            }
        }

        comboBoxReaderType.SelectedItem = Preferences.ReaderType;
    }

    private void FillColorModeList ()
    {
        foreach (var colorMode in Enum.GetValues<ColorMode>().Where(cm => !comboBoxColorMode.Items.Contains(cm)))
        {
            _ = comboBoxColorMode.Items.Add(colorMode);
        }

        comboBoxColorMode.SelectedItem = Preferences.ColorMode;
    }

    internal void FillPortableMode ()
    {
        // Detach the handler while syncing the checkbox from preferences: CheckedChanged also
        // fires on programmatic changes, and the handler runs the full activation flow
        // (question dialog, marker file) which must only happen on a user toggle (issue #658).
        checkBoxPortableMode.CheckedChanged -= OnPortableModeCheckedChanged;
        checkBoxPortableMode.CheckState = Preferences.PortableMode ? CheckState.Checked : CheckState.Unchecked;
        SetPortableModeCheckBoxText();
        checkBoxPortableMode.CheckedChanged += OnPortableModeCheckedChanged;
    }

    private void SetPortableModeCheckBoxText ()
    {
        checkBoxPortableMode.Text = Preferences.PortableMode
            ? Resources.SettingsDialog_UI_DeActivatePortableMode
            : Resources.SettingsDialog_UI_ActivatePortableMode;
    }

    private void DisplayFontName ()
    {
        var font = Preferences.Font ?? new Font(FontFamily.GenericMonospace, DEFAULT_FONT_SIZE);
        var style = font.Style == FontStyle.Regular ? string.Empty : $" {font.Style}";
        labelFont.Text = $"{font.Name} {font.Size}{style}";
        labelFont.Font = font;
    }

    private void SaveMultifileData ()
    {
        if (radioButtonLoadEveryFileIntoSeperatedTab.Checked)
        {
            Preferences.MultiFileOption = MultiFileOption.SingleFiles;
        }

        if (radioButtonTreatAllFilesAsOneMultifile.Checked)
        {
            Preferences.MultiFileOption = MultiFileOption.MultiFile;
        }

        if (radioButtonAskWhatToDo.Checked)
        {
            Preferences.MultiFileOption = MultiFileOption.Ask;
        }

        Preferences.MultiFileOptions.FormatPattern = textBoxMultifilePattern.Text;
        Preferences.MultiFileOptions.MaxDayTry = (int)upDownMultifileDays.Value;
    }

    private void FillColumnizerForToolsList ()
    {
        if (_selectedTool != null)
        {
            FillColumnizerForToolsList(comboBoxColumnizer, _selectedTool.ColumnizerName);
        }
    }

    private static void FillColumnizerForToolsList (ComboBox comboBox, string columnizerName)
    {
        var selIndex = 0;
        comboBox.Items.Clear();
        var columnizers = PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers;

        foreach (var columnizer in columnizers)
        {
            var index = comboBox.Items.Add(columnizer.GetName());
            if (columnizer.GetName().Equals(columnizerName, StringComparison.Ordinal))
            {
                selIndex = index;
            }
        }

        comboBox.SelectedIndex = selIndex;
    }

    private void FillColumnizerList ()
    {
        dataGridViewColumnizer.Rows.Clear();

        var comboColumn = (DataGridViewComboBoxColumn)dataGridViewColumnizer.Columns[3];
        comboColumn.Items.Clear();

        var typeColumn = (DataGridViewComboBoxColumn)dataGridViewColumnizer.Columns[2];
        typeColumn.ValueType = typeof(MaskType);

        if (typeColumn.Items.Count == 0)
        {
            _ = typeColumn.Items.Add(MaskType.Glob);
            _ = typeColumn.Items.Add(MaskType.Regex);
        }

        var columnizers = PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers;
        var columnizerLookup = new HashSet<string>(StringComparer.Ordinal);

        foreach (var columnizer in columnizers)
        {
            var name = columnizer.GetName();
            _ = comboColumn.Items.Add(name);
            _ = columnizerLookup.Add(name);
        }

        foreach (var maskEntry in Preferences.ColumnizerMaskList)
        {
            int rowIndex = dataGridViewColumnizer.Rows.Add();
            var row = dataGridViewColumnizer.Rows[rowIndex];

            row.Cells[1].Value = maskEntry.Mask;
            row.Cells[2].Value = maskEntry.Type;

            if (columnizerLookup.Contains(maskEntry.ColumnizerName))
            {
                row.Cells[3].Value = maskEntry.ColumnizerName;
                row.Cells[0].Value = _emptyImage;
            }
            else
            {
                // Stale entry — the columnizer is not registered. Mark the row but keep the original name
                // (a future re-install of the plugin will resurrect the entry).
                row.Cells[3].Value = maskEntry.ColumnizerName;
                row.Cells[0].Value = _staleImage;
                row.Cells[0].ToolTipText = string.Format(CultureInfo.CurrentCulture,
                    Resources.SettingsDialog_UI_DataGridView_columnTooltipColumnizerMaskStale,
                    maskEntry.ColumnizerName);
            }
        }
    }

    private void FillHighlightMaskList ()
    {
        dataGridViewHighlightMask.Rows.Clear();

        var comboColumn = (DataGridViewComboBoxColumn)dataGridViewHighlightMask.Columns[1];
        comboColumn.Items.Clear();

        //TODO Remove if not necessary
        //var textColumn = (DataGridViewTextBoxColumn)dataGridViewHighlightMask.Columns[0];

        foreach (var group in (IList<HighlightGroup>)_logTabWin.HighlightGroupList)
        {
            _ = comboColumn.Items.Add(group.GroupName);
        }

        foreach (var maskEntry in Preferences.HighlightMaskList)
        {
            DataGridViewRow row = new();
            _ = row.Cells.Add(new DataGridViewTextBoxCell());
            DataGridViewComboBoxCell cell = new();

            foreach (var group in (IList<HighlightGroup>)_logTabWin.HighlightGroupList)
            {
                _ = cell.Items.Add(group.GroupName);
            }

            _ = row.Cells.Add(cell);
            row.Cells[0].Value = maskEntry.Mask;

            //var currentGroup = _logTabWin.FindHighlightGroup(maskEntry.HighlightGroupName);
            var highlightGroupList = _logTabWin.HighlightGroupList;
            var currentGroup = highlightGroupList.Count > 0 ? highlightGroupList[0] : new HighlightGroup();

            row.Cells[1].Value = currentGroup.GroupName;
            _ = dataGridViewHighlightMask.Rows.Add(row);
        }

        var count = dataGridViewHighlightMask.RowCount;

        if (count > 0 && !dataGridViewHighlightMask.Rows[count - 1].IsNewRow)
        {
            var comboCell = (DataGridViewComboBoxCell)dataGridViewHighlightMask.Rows[count - 1].Cells[1];
            comboCell.Value = comboCell.Items[0];
        }
    }

    private void SaveColumnizerList ()
    {
        Preferences.ColumnizerMaskList.Clear();

        foreach (DataGridViewRow row in dataGridViewColumnizer.Rows)
        {
            if (!row.IsNewRow)
            {
                var type = row.Cells[2].Value is MaskType maskType
                    ? maskType
                    : MaskType.Glob;

                ColumnizerMaskEntry entry = new()
                {
                    Mask = (string)row.Cells[1].Value,
                    Type = type,
                    ColumnizerName = (string)row.Cells[3].Value
                };

                Preferences.ColumnizerMaskList.Add(entry);
            }
        }
    }

    private void SaveHighlightMaskList ()
    {
        Preferences.HighlightMaskList.Clear();

        foreach (DataGridViewRow row in dataGridViewHighlightMask.Rows)
        {
            if (!row.IsNewRow)
            {
                HighlightMaskEntry entry = new()
                {
                    Mask = (string)row.Cells[0].Value,
                    HighlightGroupName = (string)row.Cells[1].Value
                };

                Preferences.HighlightMaskList.Add(entry);
            }
        }
    }

    private void FillPluginList ()
    {
        listBoxPlugin.Items.Clear();

        foreach (var entry in PluginRegistry.PluginRegistry.Instance.RegisteredContextMenuPlugins)
        {
            _ = listBoxPlugin.Items.Add(entry);
            if (entry is ILogExpertPluginConfigurator configurator)
            {
                configurator.StartConfig();
            }
        }

        foreach (var entry in PluginRegistry.PluginRegistry.Instance.RegisteredKeywordActions)
        {
            _ = listBoxPlugin.Items.Add(entry);
            if (entry is ILogExpertPluginConfigurator configurator)
            {
                configurator.StartConfig();
            }
        }

        foreach (var entry in PluginRegistry.PluginRegistry.Instance.RegisteredFileSystemPlugins)
        {
            _ = listBoxPlugin.Items.Add(entry);
            if (entry is ILogExpertPluginConfigurator configurator)
            {
                configurator.StartConfig();
            }
        }

        buttonConfigPlugin.Enabled = false;
    }

    private void SavePluginSettings ()
    {
        _selectedPlugin?.HideConfigForm();

        foreach (var entry in PluginRegistry.PluginRegistry.Instance.RegisteredContextMenuPlugins)
        {
            if (entry is ILogExpertPluginConfigurator configurator)
            {
                configurator.SaveConfig(ConfigManager.ActiveConfigDir);
            }
        }

        foreach (var entry in PluginRegistry.PluginRegistry.Instance.RegisteredKeywordActions)
        {
            if (entry is ILogExpertPluginConfigurator configurator)
            {
                configurator.SaveConfig(ConfigManager.ActiveConfigDir);
            }
        }
    }

    private void FillToolListbox ()
    {
        listBoxTools.Items.Clear();

        foreach (var tool in Preferences.ToolEntries)
        {
            _ = listBoxTools.Items.Add(tool.Clone(), tool.IsFavourite);
        }

        if (listBoxTools.Items.Count > 0)
        {
            listBoxTools.SelectedIndex = 0;
        }
    }

    private void FillMultifileSettings ()
    {
        switch (Preferences.MultiFileOption)
        {
            case MultiFileOption.SingleFiles:
                {
                    radioButtonLoadEveryFileIntoSeperatedTab.Checked = true;
                    break;
                }
            case MultiFileOption.MultiFile:
                {
                    radioButtonTreatAllFilesAsOneMultifile.Checked = true;
                    break;
                }
            case MultiFileOption.Ask:
                {
                    radioButtonAskWhatToDo.Checked = true;
                    break;
                }
            default:
                //intentionally left blank
                break;
        }

        textBoxMultifilePattern.Text = Preferences.MultiFileOptions.FormatPattern;
        upDownMultifileDays.Value = Preferences.MultiFileOptions.MaxDayTry;
    }

    private void GetToolListBoxData ()
    {
        GetCurrentToolValues();
        Preferences.ToolEntries.Clear();

        for (var i = 0; i < listBoxTools.Items.Count; ++i)
        {
            Preferences.ToolEntries.Add(listBoxTools.Items[i] as ToolEntry);
            (listBoxTools.Items[i] as ToolEntry).IsFavourite = listBoxTools.GetItemChecked(i);
        }
    }

    private void GetCurrentToolValues ()
    {
        if (_selectedTool != null)
        {
            _selectedTool.Name = string.IsNullOrWhiteSpace(textBoxToolName.Text) ? textBoxTool.Text : textBoxToolName.Text;
            _selectedTool.Cmd = textBoxTool.Text;
            _selectedTool.Args = textBoxArguments.Text;
            _selectedTool.ColumnizerName = comboBoxColumnizer.Text;
            _selectedTool.Sysout = checkBoxSysout.Checked;
            _selectedTool.WorkingDir = textBoxWorkingDir.Text;
        }
    }

    private void ShowCurrentToolValues ()
    {
        if (_selectedTool != null)
        {
            textBoxToolName.Text = _selectedTool.Name;
            textBoxTool.Text = _selectedTool.Cmd;
            textBoxArguments.Text = _selectedTool.Args;
            comboBoxColumnizer.Text = _selectedTool.ColumnizerName;
            checkBoxSysout.Checked = _selectedTool.Sysout;
            comboBoxColumnizer.Enabled = _selectedTool.Sysout;
            textBoxWorkingDir.Text = _selectedTool.WorkingDir;
        }
    }

    private void DisplayCurrentIcon ()
    {
        if (_selectedTool != null)
        {
            var icon = NativeMethods.LoadIconFromExe(_selectedTool.IconFile, _selectedTool.IconIndex);
            if (icon != null)
            {
                Image image = icon.ToBitmap();
                buttonIcon.Image = image;
                _ = Vanara.PInvoke.User32.DestroyIcon(icon.Handle);
                icon.Dispose();
            }
            else
            {
                buttonIcon.Image = _emptyImage;
            }
        }
    }

    /// <summary>
    /// Populates the encoding combo box from <see cref="EncodingRegistry.OfferedEncodings"/> — the same
    /// list the per-file View → Encoding menu is built from. The value member is set to a specific
    /// header name defined in the resources; WinForms falls back to it for the display text, so each row
    /// reads as its <see cref="Encoding.HeaderName"/>.
    /// </summary>
    private void FillEncodingList ()
    {
        comboBoxEncoding.Items.Clear();

        foreach (var encoding in EncodingRegistry.OfferedEncodings)
        {
            _ = comboBoxEncoding.Items.Add(encoding);
        }

        comboBoxEncoding.ValueMember = Resources.SettingsDialog_UI_ComboBox_Encoding_ValueMember_HeaderName;
    }

    /// <summary>
    /// Populates the language selection list with available language options.
    /// </summary>
    /// <remarks>
    /// Clears any existing items in the language selection list and adds predefined language options. Currently, it
    /// includes English (United States) and German (Germany).
    /// </remarks>
    private void FillLanguageList ()
    {
        comboBoxLanguage.Items.Clear();

        _ = comboBoxLanguage.Items.Add(CultureInfo.GetCultureInfo("en-US").Name); // Add English as default
        _ = comboBoxLanguage.Items.Add(CultureInfo.GetCultureInfo("de-DE").Name);
        _ = comboBoxLanguage.Items.Add(CultureInfo.GetCultureInfo("zh-CN").Name);
    }

    #endregion

    #region Events handler

    private void OnSettingsDialogLoad (object sender, EventArgs e)
    {
        FillDialog();
    }

    private void OnBtnChangeFontClick (object sender, EventArgs e)
    {
        var currentFont = Preferences.Font ?? new Font(FontFamily.GenericMonospace, DEFAULT_FONT_SIZE);

        using FontDialog dlg = new()
        {
            ShowEffects = true,
            AllowVerticalFonts = false,
            AllowScriptChange = false,
            Font = currentFont
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var converter = TypeDescriptor.GetConverter(typeof(Font));
            var selected = (Font)dlg.Font.Clone();

            Preferences.Font?.Dispose();
            Preferences.Font = selected;
            Preferences.FontString = converter.ConvertToInvariantString(selected);
        }

        DisplayFontName();
    }

    private void OnBtnOkClick (object sender, EventArgs e)
    {
        Preferences.TimestampControl = checkBoxTimestamp.Checked;
        Preferences.FilterSync = checkBoxSyncFilter.Checked;
        Preferences.FilterTail = checkBoxFilterTail.Checked;
        Preferences.FollowTail = checkBoxFollowTail.Checked;

        Preferences.TimestampControlDragOrientation = radioButtonVerticalMouseDrag.Checked
            ? DragOrientations.Vertical
            : radioButtonVerticalMouseDragInverted.Checked
                ? DragOrientations.InvertedVertical
                : DragOrientations.Horizontal;

        SaveColumnizerList();

        Preferences.ColumnizerSelectionPriority = radioColumnizerPriorityMaskOverridesPersistence.Checked
            ? ColumnizerSelectionPriority.MaskOverridesPersistence
            : radioColumnizerPriorityMaskThenHistory.Checked
                ? ColumnizerSelectionPriority.MaskThenHistory
                : ColumnizerSelectionPriority.HistoryThenMask;
        Preferences.AutoPick = checkBoxAutoPick.Checked;
        Preferences.AskForClose = checkBoxAskCloseTabs.Checked;
        Preferences.AllowOnlyOneInstance = checkBoxSingleInstance.Checked;
        Preferences.OpenLastFiles = checkBoxOpenLastFiles.Checked;
        Preferences.ShowTailState = checkBoxTailState.Checked;
        Preferences.SetLastColumnWidth = checkBoxColumnSize.Checked;
        Preferences.LastColumnWidth = (int)cpDownColumnWidth.Value;
        Preferences.ShowTimeSpread = checkBoxTimeSpread.Checked;
        Preferences.ReverseAlpha = checkBoxReverseAlpha.Checked;
        Preferences.TimeSpreadTimeMode = radioButtonTimeView.Checked;

        Preferences.SaveSessions = checkBoxSaveSessions.Checked;
        Preferences.SessionSaveDirectory = labelSessionSaveOwnDir.Text;

        Preferences.SaveLocation = radioButtonsessionSaveDocuments.Checked
            ? SessionSaveLocation.DocumentsDir
            : radioButtonSessionSaveOwn.Checked
                ? SessionSaveLocation.OwnDir
                : radioButtonSessionApplicationStartupDir.Checked
                    ? SessionSaveLocation.ApplicationStartupDir
                    : SessionSaveLocation.SameDir;

        Preferences.SaveFilters = checkBoxSaveFilter.Checked;
        Preferences.BufferCount = (int)upDownBlockCount.Value;
        Preferences.LinesPerBuffer = (int)upDownLinesPerBlock.Value;
        Preferences.PollingInterval = (int)upDownPollingInterval.Value;
        Preferences.MultiThreadFilter = checkBoxMultiThread.Checked;
        Preferences.DefaultEncoding = comboBoxEncoding.SelectedItem is Encoding selectedEncoding ? selectedEncoding.HeaderName : FallbackEncoding.HeaderName;
        Preferences.DefaultLanguage = comboBoxLanguage.SelectedItem != null ? (comboBoxLanguage.SelectedItem as string) : CultureInfo.GetCultureInfo("en-US").Name;
        Preferences.ShowColumnFinder = checkBoxColumnFinder.Checked;
        Preferences.ReaderType = comboBoxReaderType.SelectedItem != null ? (ReaderType)comboBoxReaderType.SelectedItem : ReaderType.SystemDirect;

        Preferences.MaximumFilterEntries = (int)upDownMaximumFilterEntries.Value;
        Preferences.MaximumFilterEntriesDisplayed = (int)upDownMaximumFilterEntriesDisplayed.Value;
        Preferences.ShowErrorMessageAllowOnlyOneInstances = checkBoxShowErrorMessageOnlyOneInstance.Checked;
        Preferences.ColorMode = comboBoxColorMode.SelectedItem is ColorMode colorMode ? colorMode : ColorMode.Light;
        Preferences.MaxLineLength = (int)upDownMaximumLineLength.Value;
        Preferences.MaxDisplayLength = Math.Min((int)upDownMaxDisplayLength.Value, (int)upDownMaximumLineLength.Value);

        SavePluginSettings();
        SaveHighlightMaskList();
        GetToolListBoxData();
        SaveMultifileData();
        SaveControlCharsTab();
        SaveMarkerBarTab();
    }

    private void OnBtnToolClick (object sender, EventArgs e)
    {
        using OpenFileDialog dlg = new()
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (!string.IsNullOrEmpty(textBoxTool.Text))
        {
            FileInfo info = new(textBoxTool.Text);
            if (info.Directory != null && info.Directory.Exists)
            {
                dlg.InitialDirectory = info.DirectoryName;
            }
        }

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            textBoxTool.Text = dlg.FileName;
        }
    }

    private void OnBtnArgClick (object sender, EventArgs e)
    {
        using ToolArgsDialog dlg = new(_logTabWin, this)
        {
            Arg = textBoxArguments.Text
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            textBoxArguments.Text = dlg.Arg;
        }
    }

    /// <summary>
    /// Adds default values to the Columnizer Grid when a new row is created. The default mask type is set to "Glob",
    /// and if there are any registered columnizers, the first one in the list is selected as the default columnizer for
    /// the new row.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnDataGridViewColumnizerDefaultValuesNeeded (object sender, DataGridViewRowEventArgs e)
    {
        e.Row.Cells[2].Value = MaskType.Glob;

        var columnizers = PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers;
        if (columnizers.Count > 0)
        {
            e.Row.Cells[3].Value = columnizers[0].GetName();
        }
    }

    private void OnBtnDeleteClick (object sender, EventArgs e)
    {
        if (dataGridViewColumnizer.CurrentRow != null && !dataGridViewColumnizer.CurrentRow.IsNewRow)
        {
            var index = dataGridViewColumnizer.CurrentRow.Index;
            _ = dataGridViewColumnizer.EndEdit();
            dataGridViewColumnizer.Rows.RemoveAt(index);
        }
    }

    private void OnDataGridViewColumnizerDataError (object sender, DataGridViewDataErrorEventArgs e)
    {
        e.Cancel = true;
    }

    private void OnDataGridViewColumnizerCurrentCellDirtyStateChanged (object sender, EventArgs e)
    {
        if (dataGridViewColumnizer.IsCurrentCellDirty)
        {
            _ = dataGridViewColumnizer.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void OnChkBoxSysoutCheckedChanged (object sender, EventArgs e)
    {
        comboBoxColumnizer.Enabled = checkBoxSysout.Checked;
    }

    private void OnBtnTailColorClick (object sender, EventArgs e)
    {
        ColorDialog dlg = new()
        {
            Color = Preferences.ShowTailColor
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Preferences.ShowTailColor = dlg.Color;
        }
    }

    private void OnChkBoxColumnSizeCheckedChanged (object sender, EventArgs e)
    {
        cpDownColumnWidth.Enabled = checkBoxColumnSize.Checked;
    }

    private void OnBtnTimespreadColorClick (object sender, EventArgs e)
    {
        ColorDialog dlg = new()
        {
            Color = Preferences.TimeSpreadColor
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Preferences.TimeSpreadColor = dlg.Color;
        }
    }

    private void OnListBoxPluginSelectedIndexChanged (object sender, EventArgs e)
    {
        _selectedPlugin?.HideConfigForm();

        var selectedPlugin = listBoxPlugin.SelectedItem;

        if (selectedPlugin != null)
        {
            if (selectedPlugin is ILogExpertPluginConfigurator pluginConfigurator)
            {
                _selectedPlugin = pluginConfigurator;

                if (_selectedPlugin.HasEmbeddedForm())
                {
                    buttonConfigPlugin.Enabled = false;
                    buttonConfigPlugin.Visible = false;
                    _selectedPlugin.ShowConfigForm(panelPlugin);
                }
                else
                {
                    buttonConfigPlugin.Enabled = true;
                    buttonConfigPlugin.Visible = true;
                }
            }
        }
        else
        {
            buttonConfigPlugin.Enabled = false;
            buttonConfigPlugin.Visible = true;
        }
    }

    private void OnBtnSessionSaveDirClick (object sender, EventArgs e)
    {
        FolderBrowserDialog dlg = new();

        if (Preferences.SessionSaveDirectory != null)
        {
            dlg.SelectedPath = Preferences.SessionSaveDirectory;
        }

        dlg.ShowNewFolderButton = true;
        dlg.Description = Resources.SettingsDialog_UI_FolderBrowser_folderBrowserSessionSaveDir;

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            labelSessionSaveOwnDir.Text = dlg.SelectedPath;
        }
    }

    private void OnPortableModeCheckedChanged (object sender, EventArgs e)
    {
        try
        {
            switch (checkBoxPortableMode.CheckState)
            {
                case CheckState.Checked:
                    {
                        try
                        {
                            // Create new portable configuration directory
                            _ = Directory.CreateDirectory(ConfigManager.PortableConfigDir);

                            // Create marker file
                            var markerPath = Path.Join(ConfigManager.PortableConfigDir, ConfigManager.PortableModeSettingsFileName);
                            if (!File.Exists(markerPath))
                            {
                                using (File.Create(markerPath))
                                { }
                            }

                            Preferences.PortableMode = true;
                            SetPortableModeCheckBoxText();

                            // Ask user if they want to copy existing settings
                            var result = MessageBox.Show(
                                Resources.SettingsDialog_UI_PortableMode_CopySettingsQuestion,
                                Resources.SettingsDialog_UI_PortableMode_Title,
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (result == DialogResult.Yes)
                            {
                                ConfigManager.CopyConfigToPortable();
                            }
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                        {
                            _ = MessageBox.Show(
                                string.Format(CultureInfo.CurrentCulture,
                                    Resources.SettingsDialog_UI_PortableMode_ActivationError, ex.Message),
                                Resources.LogExpert_Common_UI_Title_LogExpert,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                            checkBoxPortableMode.Checked = false;
                            Preferences.PortableMode = false;
                        }

                        break;
                    }
                case CheckState.Unchecked:
                    {
                        Preferences.PortableMode = false;
                        SetPortableModeCheckBoxText();

                        // Ask user if they want to move settings back
                        var result = MessageBox.Show(
                            Resources.SettingsDialog_UI_PortableMode_MoveSettingsQuestion,
                            Resources.SettingsDialog_UI_PortableMode_Title,
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            try
                            {
                                ConfigManager.MoveConfigFromPortable();
                            }
                            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                            {
                                _ = MessageBox.Show(
                                    string.Format(CultureInfo.CurrentCulture,
                                        Resources.SettingsDialog_UI_PortableMode_MigrationError, ex.Message),
                                    Resources.LogExpert_Common_UI_Title_LogExpert,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                            }
                        }

                        // Delete marker file
                        var markerPath = Path.Join(ConfigManager.PortableConfigDir, ConfigManager.PortableModeSettingsFileName);
                        if (File.Exists(markerPath))
                        {
                            File.Delete(markerPath);
                        }

                        break;
                    }
                case CheckState.Indeterminate:
                    //intentionally left blank
                    break;
                default:
                    //intentionally left blank
                    break;
            }
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException
                                                    or IOException
                                                    or ArgumentException
                                                    or ArgumentNullException
                                                    or PathTooLongException
                                                    or DirectoryNotFoundException
                                                    or NotSupportedException)
        {
            _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.SettingsDialog_UI_CouldNotCreatePortableMode, exception), Resources.LogExpert_Common_UI_Title_Error, MessageBoxButtons.OK);
        }
    }

    private void OnBtnConfigPluginClick (object sender, EventArgs e)
    {
        if (!_selectedPlugin.HasEmbeddedForm())
        {
            _selectedPlugin.ShowConfigDialog(this);
        }
    }

    private void OnNumericUpDown1ValueChanged (object sender, EventArgs e)
    {
        //TODO implement
    }

    private void OnListBoxToolSelectedIndexChanged (object sender, EventArgs e)
    {
        GetCurrentToolValues();
        _selectedTool = listBoxTools.SelectedItem as ToolEntry;
        ShowCurrentToolValues();
        listBoxTools.Refresh();
        FillColumnizerForToolsList();
        DisplayCurrentIcon();
    }

    private void OnBtnToolUpClick (object sender, EventArgs e)
    {
        var i = listBoxTools.SelectedIndex;

        if (i > 0)
        {
            var isChecked = listBoxTools.GetItemChecked(i);
            var item = listBoxTools.Items[i];
            listBoxTools.Items.RemoveAt(i);

            i--;

            listBoxTools.Items.Insert(i, item);
            listBoxTools.SelectedIndex = i;
            listBoxTools.SetItemChecked(i, isChecked);
        }
    }

    private void OnBtnToolDownClick (object sender, EventArgs e)
    {
        var i = listBoxTools.SelectedIndex;

        if (i < listBoxTools.Items.Count - 1)
        {
            var isChecked = listBoxTools.GetItemChecked(i);
            var item = listBoxTools.Items[i];
            listBoxTools.Items.RemoveAt(i);

            i++;

            listBoxTools.Items.Insert(i, item);
            listBoxTools.SelectedIndex = i;
            listBoxTools.SetItemChecked(i, isChecked);
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnBtnToolAddClick (object sender, EventArgs e)
    {
        _ = listBoxTools.Items.Add(new ToolEntry());
        listBoxTools.SelectedIndex = listBoxTools.Items.Count - 1;
    }

    [SupportedOSPlatform("windows")]
    private void OnToolDeleteButtonClick (object sender, EventArgs e)
    {
        var i = listBoxTools.SelectedIndex;

        if (i < listBoxTools.Items.Count && i >= 0)
        {
            listBoxTools.Items.RemoveAt(i);
            if (i < listBoxTools.Items.Count)
            {
                listBoxTools.SelectedIndex = i;
            }
            else
            {
                if (listBoxTools.Items.Count > 0)
                {
                    listBoxTools.SelectedIndex = listBoxTools.Items.Count - 1;
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnBtnToolIconClick (object sender, EventArgs e)
    {
        if (_selectedTool != null)
        {
            var iconFile = _selectedTool.IconFile;

            if (string.IsNullOrWhiteSpace(iconFile))
            {
                iconFile = textBoxTool.Text;
            }

            ChooseIconDlg dlg = new(iconFile);

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _selectedTool.IconFile = dlg.FileName;
                _selectedTool.IconIndex = dlg.IconIndex;
                DisplayCurrentIcon();
            }
        }
    }

    private void OnBtnCancelClick (object sender, EventArgs e)
    {
        _selectedPlugin?.HideConfigForm();
    }

    private void OnBtnWorkingDirClick (object sender, EventArgs e)
    {
        using FolderBrowserDialog dlg = new()
        {
            RootFolder = Environment.SpecialFolder.MyComputer,
            Description = Resources.SettingsDialog_UI_FolderBrowser_folderBrowserWorkingDir
        };

        if (!string.IsNullOrEmpty(textBoxWorkingDir.Text))
        {
            DirectoryInfo info = new(textBoxWorkingDir.Text);
            if (info.Exists)
            {
                dlg.SelectedPath = info.FullName;
            }
        }

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            textBoxWorkingDir.Text = dlg.SelectedPath;
        }
    }

    [SupportedOSPlatform("windows")]
    private void OnMultiFilePatternTextChanged (object sender, EventArgs e)
    {
        var pattern = textBoxMultifilePattern.Text;
        upDownMultifileDays.Enabled = pattern.Contains("$D", StringComparison.Ordinal);
    }

    [SupportedOSPlatform("windows")]
    private void OnBtnExportClick (object sender, EventArgs e)
    {
        SaveFileDialog dlg = new()
        {
            Title = @Resources.SettingsDialog_UI_Title_ExportSettings,
            DefaultExt = "json",
            AddExtension = true,
            Filter = string.Format(CultureInfo.InvariantCulture, Resources.SettingsDialog_UI_Filter_ExportSettings, "(*.json)|*.json", "(*.*)|*.*")
        };

        var result = dlg.ShowDialog();

        if (result == DialogResult.OK)
        {
            FileInfo fileInfo = new(dlg.FileName);
            ConfigManager.Export(fileInfo);
        }
    }

    /// <summary>
    /// Import settings from file
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnBtnImportClick (object sender, EventArgs e)
    {
        ImportSettingsDialog dlg = new(ExportImportFlags.All);

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            if (string.IsNullOrWhiteSpace(dlg.FileName))
            {
                return;
            }

            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(dlg.FileName);
            }
            catch (Exception ex) when (ex is ArgumentException
                                          or ArgumentNullException
                                          or PathTooLongException
                                          or SecurityException
                                          or NotSupportedException
                                          or UnauthorizedAccessException)
            {
                _ = MessageBox.Show(this, string.Format(CultureInfo.InvariantCulture, Resources.SettingsDialog_UI_Error_SettingsCouldNotBeImported, ex), Resources.LogExpert_Common_UI_Title_Error);
                return;
            }

            ImportResult importResult = ConfigManager.Import(fileInfo, dlg.ImportFlags);

            if (!importResult.Success)
            {
                if (importResult.RequiresUserConfirmation)
                {
                    var confirmResult = MessageBox.Show(
                        this,
                        importResult.ConfirmationMessage,
                        importResult.ConfirmationTitle,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);

                    if (confirmResult == DialogResult.Yes)
                    {
                        // User confirmed, retry import without validation
                        _ = ConfigManager.Import(fileInfo, dlg.ImportFlags);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    _ = MessageBox.Show(
                        this,
                        importResult.ErrorMessage,
                        importResult.ErrorTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }
            }

            Preferences = ConfigManager.Settings.Preferences;
            FillDialog();
            _ = MessageBox.Show(this, Resources.SettingsDialog_UI_SettingsImported, Resources.LogExpert_Common_UI_Title_LogExpert);
        }
    }

    private void OnUpDownMaxDisplayLengthValueChanged (object sender, EventArgs e)
    {
        // Ensure MaxDisplayLength doesn't exceed MaxLineLength
        if (upDownMaxDisplayLength.Value > upDownMaximumLineLength.Value)
        {
            upDownMaxDisplayLength.Value = upDownMaximumLineLength.Value;
        }
    }

    private void OnUpDownMaximumLineLengthValueChanged (object sender, EventArgs e)
    {
        // When MaxLineLength changes, update the maximum allowed for MaxDisplayLength
        upDownMaxDisplayLength.Maximum = Math.Min(1000000, upDownMaximumLineLength.Value);

        // If current MaxDisplayLength exceeds new MaxLineLength, adjust it
        if (upDownMaxDisplayLength.Value > upDownMaximumLineLength.Value)
        {
            upDownMaxDisplayLength.Value = upDownMaximumLineLength.Value;
        }
    }

    #endregion

    #region Resources Map

    /// <summary>
    /// Creates a mapping of UI controls to their corresponding tooltip text.
    /// </summary>
    /// <remarks>
    /// This method initializes a dictionary with predefined tooltips for specific UI controls. Additional tooltips can
    /// be added to the dictionary as needed.
    /// </remarks>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> where the keys are <see cref="Control"/> objects and the values are
    /// strings representing the tooltip text for each control.
    /// </returns>
    private Dictionary<Control, string> GetToolTipMap ()
    {
        return new Dictionary<Control, string>
        {
            { comboBoxLanguage, Resources.SettingsDialog_UI_ComboBox_ToolTip_toolTipLanguage },
            { comboBoxEncoding, Resources.SettingsDialog_UI_ComboBox_ToolTip_toolTipEncoding },
            { checkBoxPortableMode, Resources.SettingsDialog_UI_CheckBox_ToolTip_toolTipPortableMode },
            { radioButtonSessionApplicationStartupDir, Resources.SettingsDialog_UI_RadioButton_ToolTip_toolTipSessionApplicationStartupDir },
            { comboBoxReaderType, Resources.SettingsDialog_UI_CheckBox_ToolTip_toolTipReaderTyp }
        };
    }

    #endregion

    #region Control Chars Tab

    private void FillControlCharsTab ()
    {
        var s = Preferences.ControlCharSettings ??= new ControlCharSettings();

        checkBoxControlCharsEnable.Checked = s.Substitute;
        checkBoxControlCharsCopyDisplayedForm.Checked = s.CopyDisplayedForm;
        checkBoxControlCharsBold.Checked = s.Bold;
        checkBoxControlCharsItalic.Checked = s.Italic;
        _controlCharsForeColor = s.ForeColor == Color.Empty ? Color.Gray : s.ForeColor;
        _controlCharsBackColor = s.BackColor;

        switch (s.Style)
        {
            case ControlCharStyle.Caret:
                {
                    radioButtonControlCharStyleCaret.Checked = true;
                    break;
                }
            case ControlCharStyle.CEscape:
                {
                    radioButtonControlCharStyleCEscape.Checked = true;
                    break;
                }
            case ControlCharStyle.Abbreviation:
                {
                    radioButtonControlCharStyleAbbreviation.Checked = true;
                    break;
                }
            case ControlCharStyle.Iso2047:
                {
                    radioButtonControlCharStyleIso2047.Checked = true;
                    break;
                }
            case ControlCharStyle.ControlPictures:
            default:
                {
                    radioButtonControlCharStyleControlPictures.Checked = true;
                    break;
                }
        }

        _controlCharsEnabledByCp.Clear();
        var enabled = s.EnabledCodepoints ?? [];
        foreach (var cp in _allDisplayableControlCps)
        {
            _controlCharsEnabledByCp[cp] = enabled.Contains(cp);
        }

        PopulateControlCharsGrid();
        UpdateColorButtons();
        UpdateSampleAndPreview();
        UpdateHintVisibility();
    }

    private void SaveControlCharsTab ()
    {
        var s = Preferences.ControlCharSettings ??= new ControlCharSettings();

        s.Substitute = checkBoxControlCharsEnable.Checked;
        s.CopyDisplayedForm = checkBoxControlCharsCopyDisplayedForm.Checked;
        s.Bold = checkBoxControlCharsBold.Checked;
        s.Italic = checkBoxControlCharsItalic.Checked;
        s.ForeColor = _controlCharsForeColor;
        s.BackColor = _controlCharsBackColor;
        s.Style = GetSelectedStyle();

        var newSet = new HashSet<int>();
        foreach (var kvp in _controlCharsEnabledByCp.Where(kvp => kvp.Value))
        {
            _ = newSet.Add(kvp.Key);
        }

        s.EnabledCodepoints = newSet;
    }

    private void PopulateControlCharsGrid ()
    {
        var style = GetSelectedStyle();
        dataGridViewControlChars.SuspendLayout();
        dataGridViewControlChars.Rows.Clear();

        for (var i = 0; i < _allDisplayableControlCps.Length; i++)
        {
            var cp = _allDisplayableControlCps[i];
            var meta = _controlCharMeta[i];
            var preview = ControlCharStyleFormatter.Format(cp, style);
            var caret = cp == 0x7F ? "^?" : "^" + (char)(cp + 0x40);
            var rowIndex = dataGridViewControlChars.Rows.Add(
                _controlCharsEnabledByCp.TryGetValue(cp, out var on) && on,
                "0x" + cp.ToString("X2", CultureInfo.InvariantCulture),
                meta.Abbr,
                caret,
                preview);

            dataGridViewControlChars.Rows[rowIndex].Tag = cp;
            dataGridViewControlChars.Rows[rowIndex].Cells[columnControlCharAbbr.Index].ToolTipText = meta.Name;
        }

        dataGridViewControlChars.ResumeLayout();
    }

    private void OnControlCharsGridCellValueChanged (object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != columnControlCharEnabled.Index)
        {
            return;
        }

        var row = dataGridViewControlChars.Rows[e.RowIndex];
        if (row.Tag is int cp && row.Cells[columnControlCharEnabled.Index].Value is bool checkedValue)
        {
            _controlCharsEnabledByCp[cp] = checkedValue;
        }
    }

    private void OnControlCharStyleChanged (object sender, EventArgs e)
    {
        if (sender is RadioButton rb && rb.Checked)
        {
            PopulateControlCharsGrid();
            UpdateSampleAndPreview();
        }
    }

    private void OnControlCharsEnableChanged (object sender, EventArgs e) => UpdateHintVisibility();

    private void UpdateHintVisibility ()
    {
        labelControlCharsHint.Visible = !checkBoxControlCharsEnable.Checked;
    }

    private void ApplyPreset (IReadOnlySet<int> preset)
    {
        foreach (var cp in _allDisplayableControlCps)
        {
            _controlCharsEnabledByCp[cp] = preset.Contains(cp);
        }

        PopulateControlCharsGrid();
    }

    private ControlCharStyle GetSelectedStyle ()
    {
        return radioButtonControlCharStyleCaret.Checked
            ? ControlCharStyle.Caret
            : radioButtonControlCharStyleCEscape.Checked
                ? ControlCharStyle.CEscape
                : radioButtonControlCharStyleAbbreviation.Checked
                    ? ControlCharStyle.Abbreviation
                    : radioButtonControlCharStyleIso2047.Checked
                        ? ControlCharStyle.Iso2047
                        : ControlCharStyle.ControlPictures;
    }

    private void OnControlCharsPresetAllClick (object sender, EventArgs e) => ApplyPreset(ControlCharPresetProvider.All);

    private void OnControlCharsPresetNoneClick (object sender, EventArgs e) => ApplyPreset(ControlCharPresetProvider.None);

    private void OnControlCharsPresetNonWhitespaceClick (object sender, EventArgs e) => ApplyPreset(ControlCharPresetProvider.NonWhitespaceDefaults);

    private void OnControlCharsGridCurrentCellDirtyStateChanged (object sender, EventArgs e)
    {
        if (dataGridViewControlChars.IsCurrentCellDirty)
        {
            _ = dataGridViewControlChars.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void OnControlCharsBackColorClearClick (object sender, EventArgs e)
    {
        _controlCharsBackColor = Color.Empty;
        UpdateColorButtons();
        UpdateSampleAndPreview();
    }

    private void OnControlCharsBoldChanged (object sender, EventArgs e) => UpdateSampleAndPreview();

    private void OnControlCharsItalicChanged (object sender, EventArgs e) => UpdateSampleAndPreview();

    private void OnControlCharsForeColorClick (object sender, EventArgs e)
    {
        using var dlg = new ColorDialog { Color = _controlCharsForeColor == Color.Empty ? Color.Gray : _controlCharsForeColor };

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _controlCharsForeColor = dlg.Color;
            UpdateColorButtons();
            UpdateSampleAndPreview();
        }
    }

    private void OnControlCharsBackColorClick (object sender, EventArgs e)
    {
        using var dlg = new ColorDialog { Color = _controlCharsBackColor == Color.Empty ? Color.White : _controlCharsBackColor };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _controlCharsBackColor = dlg.Color;
            UpdateColorButtons();
            UpdateSampleAndPreview();
        }
    }

    private void UpdateColorButtons ()
    {
        buttonControlCharsForeColor.BackColor = _controlCharsForeColor == Color.Empty ? Color.Gray : _controlCharsForeColor;
        buttonControlCharsBackColor.BackColor = _controlCharsBackColor == Color.Empty ? SystemColors.Control : _controlCharsBackColor;
    }

    private void UpdateSampleAndPreview ()
    {
        var style = GetSelectedStyle();
        // Sample renders 0x01 SOH so all styles look distinct.
        var sample = ControlCharStyleFormatter.Format(0x01, style);
        labelControlCharsSample.Text = "abc" + sample + "def";
        labelControlCharsSample.ForeColor = _controlCharsForeColor == Color.Empty ? Color.Gray : _controlCharsForeColor;
        labelControlCharsSample.BackColor = _controlCharsBackColor == Color.Empty ? SystemColors.Control : _controlCharsBackColor;

        var fontStyle = FontStyle.Regular;
        if (checkBoxControlCharsBold.Checked)
        {
            fontStyle |= FontStyle.Bold;
        }

        if (checkBoxControlCharsItalic.Checked)
        {
            fontStyle |= FontStyle.Italic;
        }

        labelControlCharsSample.Font = new Font(FontFamily.GenericMonospace, 12f, fontStyle);
    }

    #endregion
}
