using System.Globalization;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;
using LogExpert.Core.Interface;
using LogExpert.UI.Controls.LogTabWindow;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Extensions;

namespace LogExpert.Dialogs;

//TODO: This class should not knoow ConfigManager?
[SupportedOSPlatform("windows")]
internal partial class SettingsDialog : Form
{
    #region Fields

    private readonly Image _emptyImage = new Bitmap(16, 16);
    private readonly LogTabWindow _logTabWin;
    private const string DEFAULT_FONT_NAME = "Courier New";

    private ILogExpertPluginConfigurator _selectedPlugin;
    private ToolEntry _selectedTool;

    #endregion

    #region cTor

    private SettingsDialog (Preferences prefs, LogTabWindow logTabWin)
    {
        Preferences = prefs;
        _logTabWin = logTabWin; //TODO: uses only HighlightGroupList. Can we pass IList instead?
        InitializeComponent();
        LoadResources();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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

    #endregion

    #region Private Methods

    private void LoadResources ()
    {
        ApplyTextResources();
        ApplyToolTips();
        ApplyFormTitle();

        var textMap = new Dictionary<Control, string>
        {
            // General
            { labelWarningMaximumLineLength, Resources.SettingsDialog_UI_Label_WarningMaximumLineLength },
            { labelMaximumLineLength, Resources.SettingsDialog_UI_Label_MaximumLineLengthRestartRequired },
            { labelMaximumFilterEntriesDisplayed, Resources.SettingsDialog_UI_Label_MaximumFilterEntriesDisplayed },
            { labelMaximumFilterEntries, Resources.SettingsDialog_UI_Label_MaximumFilterEntries },
            { labelDefaultEncoding, Resources.SettingsDialog_UI_Label_DefaultEncoding },
            { groupBoxMisc, Resources.SettingsDialog_UI_GroupBox_Misc },
            { checkBoxShowErrorMessageOnlyOneInstance, Resources.SettingsDialog_UI_CheckBox_ShowErrorMessageOnlyOneInstance },
            { checkBoxColumnSize, Resources.SettingsDialog_UI_CheckBox_ColumnSize },
            { buttonTailColor, Resources.SettingsDialog_UI_Button_Color },
            { checkBoxTailState, Resources.SettingsDialog_UI_CheckBox_TailState },
            { checkBoxOpenLastFiles, Resources.SettingsDialog_UI_CheckBox_OpenLastFiles },
            { checkBoxSingleInstance, Resources.SettingsDialog_UI_CheckBox_SingleInstance },
            { checkBoxAskCloseTabs, Resources.SettingsDialog_UI_CheckBox_AskCloseTabs },
            { groupBoxDefaults, Resources.SettingsDialog_UI_GroupBox_Defaults },
            { checkBoxDarkMode, Resources.SettingsDialog_UI_CheckBox_DarkMode },
            { checkBoxFollowTail, Resources.SettingsDialog_UI_CheckBox_FollowTail },
            { checkBoxColumnFinder, Resources.SettingsDialog_UI_CheckBox_ColumnFinder },
            { checkBoxSyncFilter, Resources.SettingsDialog_UI_CheckBox_SyncFilter },
            { checkBoxFilterTail, Resources.SettingsDialog_UI_CheckBox_FilterTail },
            { groupBoxFont, Resources.SettingsDialog_UI_GroupBox_Font },
            { buttonChangeFont, Resources.SettingsDialog_UI_Button_ChangeFont },
            { labelFont, Resources.SettingsDialog_UI_Label_Font },

            // Tab Pages
            { tabPageViewSettings, Resources.SettingsDialog_UI_TabPage_ViewSettings },
            { tabPageTimeStampFeatures, Resources.SettingsDialog_UI_TabPage_TimestampFeatures },
            { tabPageExternalTools, Resources.SettingsDialog_UI_TabPage_ExternalTools },
            { tabPageColumnizers, Resources.SettingsDialog_UI_TabPage_Columnizers },
            { tabPageHighlightMask, Resources.SettingsDialog_UI_TabPage_Highlight },
            { tabPageMultiFile, Resources.SettingsDialog_UI_TabPage_MultiFile },
            { tabPagePlugins, Resources.SettingsDialog_UI_TabPage_Plugins },
            { tabPageSessions, Resources.SettingsDialog_UI_TabPage_Sessions },
            { tabPageMemory, Resources.SettingsDialog_UI_TabPage_Memory },

            // Timestamp/Time View
            { groupBoxTimeSpreadDisplay, Resources.SettingsDialog_UI_GroupBox_TimeSpreadDisplay },
            { groupBoxDisplayMode, Resources.SettingsDialog_UI_GroupBox_DisplayMode },
            { radioButtonLineView, Resources.SettingsDialog_UI_RadioButton_LineView },
            { radioButtonTimeView, Resources.SettingsDialog_UI_RadioButton_TimeView },
            { checkBoxReverseAlpha, Resources.SettingsDialog_UI_CheckBox_ReverseAlpha },
            { buttonTimespreadColor, Resources.SettingsDialog_UI_Button_TimespreadColor },
            { checkBoxTimeSpread, Resources.SettingsDialog_UI_CheckBox_TimeSpread },
            { groupBoxTimeStampNavigationControl, Resources.SettingsDialog_UI_GroupBox_TimestampNavigationControl },
            { checkBoxTimestamp, Resources.SettingsDialog_UI_CheckBox_Timestamp },

            // Mouse Drag
            { groupBoxMouseDragDefaults, Resources.SettingsDialog_UI_GroupBox_MouseDragDefault },
            { radioButtonVerticalMouseDragInverted, Resources.SettingsDialog_UI_RadioButton_VerticalMouseDragInverted },
            { radioButtonHorizMouseDrag, Resources.SettingsDialog_UI_RadioButton_HorizMouseDrag },
            { radioButtonVerticalMouseDrag, Resources.SettingsDialog_UI_RadioButton_VerticalMouseDrag },

            // Tools Section
            { labelToolsDescription, Resources.SettingsDialog_UI_Label_ToolsDescription },
            { buttonToolDelete, Resources.SettingsDialog_UI_Button_ToolDelete },
            { buttonToolAdd, Resources.SettingsDialog_UI_Button_ToolAdd },
            { buttonToolDown, Resources.SettingsDialog_UI_Button_ToolDown },
            { buttonToolUp, Resources.SettingsDialog_UI_Button_ToolUp },
            { groupBoxToolSettings, Resources.SettingsDialog_UI_GroupBox_ToolSettings },
            { labelWorkingDir, Resources.SettingsDialog_UI_Label_WorkingDir },
            { buttonWorkingDir, Resources.SettingsDialog_UI_Button_WorkingDir },
            { buttonIcon, Resources.SettingsDialog_UI_Button_Icon },
            { labelToolName, Resources.SettingsDialog_UI_Label_ToolName },
            { labelToolColumnizerForOutput, Resources.SettingsDialog_UI_Label_ToolColumnizerForOutput },
            { checkBoxSysout, Resources.SettingsDialog_UI_CheckBox_Sysout },
            { buttonArguments, Resources.SettingsDialog_UI_Button_Arguments },
            { labelTool, Resources.SettingsDialog_UI_Label_Tool },
            { buttonTool, Resources.SettingsDialog_UI_Button_Tool },
            { labelArguments, Resources.SettingsDialog_UI_Label_Arguments },

            // Columnizer/Highlight
            { checkBoxAutoPick, Resources.SettingsDialog_UI_CheckBox_AutoPick },
            { checkBoxMaskPrio, Resources.SettingsDialog_UI_CheckBox_MaskPrio },
            { buttonDelete, Resources.SettingsDialog_UI_Button_Delete },

            // MultiFile
            { groupBoxDefaultFileNamePattern, Resources.SettingsDialog_UI_GroupBox_DefaultFilenamePattern },
            { labelMaxDays, Resources.SettingsDialog_UI_Label_MaxDays },
            { labelPattern, Resources.SettingsDialog_UI_Label_Pattern },
            { labelHintMultiFile, Resources.SettingsDialog_UI_Label_HintMultiFile },
            { labelNoteMultiFile, Resources.SettingsDialog_UI_Label_NoteMultifile },
            { groupBoxWhenOpeningMultiFile, Resources.SettingsDialog_UI_GroupBox_WhenOpeningMultipleFiles },
            { radioButtonAskWhatToDo, Resources.SettingsDialog_UI_RadioButton_AskWhatToDo },
            { radioButtonTreatAllFilesAsOneMultifile, Resources.SettingsDialog_UI_RadioButton_TreatAllFilesAsOneMultiFile },
            { radioButtonLoadEveryFileIntoSeperatedTab, Resources.SettingsDialog_UI_RadioButton_LoadEveryFileIntoSeparateTab },

            // Plugin / Session / Memory
            { groupBoxPlugins, Resources.SettingsDialog_UI_GroupBox_Plugins },
            { groupBoxSettings, Resources.SettingsDialog_UI_GroupBox_Settings },
            { buttonConfigPlugin, Resources.SettingsDialog_UI_Button_ConfigurePlugin },
            { checkBoxPortableMode, Resources.SettingsDialog_UI_CheckBox_PortableMode },
            { checkBoxSaveFilter, Resources.SettingsDialog_UI_CheckBox_SaveFilter },
            { groupBoxPersistantFileLocation, Resources.SettingsDialog_UI_GroupBox_PersistenceFileLocation },
            { labelSessionSaveOwnDir, Resources.SettingsDialog_UI_Label_SessionSaveOwnDir },
            { buttonSessionSaveDir, Resources.SettingsDialog_UI_Button_SessionSaveDir },
            { radioButtonSessionSaveOwn, Resources.SettingsDialog_UI_RadioButton_SessionSaveOwn },
            { radioButtonsessionSaveDocuments, Resources.SettingsDialog_UI_RadioButton_SessionSaveDocuments },
            { radioButtonSessionSameDir, Resources.SettingsDialog_UI_RadioButton_SessionSameDir },
            { radioButtonSessionApplicationStartupDir, Resources.SettingsDialog_UI_RadioButton_SessionApplicationStartupDir },
            { checkBoxSaveSessions, Resources.SettingsDialog_UI_CheckBox_SaveSessions },
            { groupBoxCPUAndStuff, Resources.SettingsDialog_UI_GroupBox_CPUAndStuff },
            { checkBoxLegacyReader, Resources.SettingsDialog_UI_CheckBox_LegacyReader },
            { checkBoxMultiThread, Resources.SettingsDialog_UI_CheckBox_MultiThread },
            { labelFilePollingInterval, Resources.SettingsDialog_UI_Label_FilePollingInterval },
            { groupBoxLineBufferUsage, Resources.SettingsDialog_UI_GroupBox_LineBufferUsage },
            { labelInfo, Resources.SettingsDialog_UI_Label_Info },
            { labelNumberOfBlocks, Resources.SettingsDialog_UI_Label_NumberOfBlocks },
            { labelLinesPerBlock, Resources.SettingsDialog_UI_Label_LinesPerBlock },

            // Dialog buttons
            { buttonCancel, Resources.LogExpert_Common_UI_Button_Cancel },
            { buttonOk, Resources.LogExpert_Common_UI_Button_OK },
            { buttonExport, Resources.LogExpert_Common_UI_Button_Export },
            { buttonImport, Resources.LogExpert_Common_UI_Button_Import },
        };



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
        foreach (var entry in GetTextResourceMap())
        {
            entry.Key.Text = entry.Value;
        }

        dataGridViewTextBoxColumnFileMask.HeaderText = Resources.SettingsDialog_UI_DataGridViewTextBoxColumn_FileMask;
        dataGridViewComboBoxColumnColumnizer.HeaderText = Resources.SettingsDialog_UI_DataGridViewComboBoxColumn_Columnizer;
        dataGridViewTextBoxColumnFileName.HeaderText = Resources.SettingsDialog_UI_DataGridViewTextBoxColumn_FileName;
        dataGridViewComboBoxColumnHighlightGroup.HeaderText = Resources.SettingsDialog_UI_DataGridViewComboBoxColumn_HighlightGroup;
    }

    private void FillDialog ()
    {
        Preferences ??= new Preferences();

        if (Preferences.FontName == null)
        {
            Preferences.FontName = DEFAULT_FONT_NAME;
        }

        if (Math.Abs(Preferences.FontSize) < 0.1)
        {
            Preferences.FontSize = 9.0f;
        }

        FillPortableMode();

        checkBoxDarkMode.Checked = Preferences.DarkMode;
        checkBoxTimestamp.Checked = Preferences.TimestampControl;
        checkBoxSyncFilter.Checked = Preferences.FilterSync;
        checkBoxFilterTail.Checked = Preferences.FilterTail;
        checkBoxFollowTail.Checked = Preferences.FollowTail;

        radioButtonHorizMouseDrag.Checked = Preferences.TimestampControlDragOrientation == DragOrientationsEnum.Horizontal;
        radioButtonVerticalMouseDrag.Checked = Preferences.TimestampControlDragOrientation == DragOrientationsEnum.Vertical;
        radioButtonVerticalMouseDragInverted.Checked = Preferences.TimestampControlDragOrientation == DragOrientationsEnum.InvertedVertical;

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
                // intentionally left blank
                break;
            default:
                // intentionally left blank
                break;
        }

        //overwrite preferences save location in portable mode to always be application startup directory
        if (checkBoxPortableMode.Checked)
        {
            radioButtonSessionApplicationStartupDir.Checked = true;
        }

        upDownMaximumLineLength.Value = Preferences.MaxLineLength;

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

        comboBoxEncoding.SelectedItem = Encoding.GetEncoding(Preferences.DefaultEncoding);
        comboBoxLanguage.SelectedItem = CultureInfo.GetCultureInfo(Preferences.DefaultLanguage).Name;

        checkBoxMaskPrio.Checked = Preferences.MaskPrio;
        checkBoxAutoPick.Checked = Preferences.AutoPick;
        checkBoxAskCloseTabs.Checked = Preferences.AskForClose;
        checkBoxColumnFinder.Checked = Preferences.ShowColumnFinder;
        checkBoxLegacyReader.Checked = Preferences.UseLegacyReader;
        checkBoxShowErrorMessageOnlyOneInstance.Checked = Preferences.ShowErrorMessageAllowOnlyOneInstances;
    }

    private void FillPortableMode ()
    {
        checkBoxPortableMode.CheckState = Preferences.PortableMode ? CheckState.Checked : CheckState.Unchecked;
    }

    private void DisplayFontName ()
    {
        labelFont.Text = $"{Preferences.FontName} {(int)Preferences.FontSize}";
        labelFont.Font = new Font(new FontFamily(Preferences.FontName), Preferences.FontSize);
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

    private static void OnBtnToolClickInternal (TextBox textBox)
    {
        OpenFileDialog dlg = new()
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (!string.IsNullOrEmpty(textBox.Text))
        {
            FileInfo info = new(textBox.Text);
            if (info.Directory != null && info.Directory.Exists)
            {
                dlg.InitialDirectory = info.DirectoryName;
            }
        }

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            textBox.Text = dlg.FileName;
        }
    }

    //TODO: what is the purpose of this method?
    private void OnBtnArgsClickInternal (TextBox textBox)
    {
        ToolArgsDialog dlg = new(_logTabWin, this)
        {
            Arg = textBox.Text
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            textBox.Text = dlg.Arg;
        }
    }

    private static void OnBtnWorkingDirClick (TextBox textBox)
    {
        FolderBrowserDialog dlg = new()
        {
            RootFolder = Environment.SpecialFolder.MyComputer,
            Description = Resources.SettingsDialog_UI_FolderBrowser_WorkingDir
        };

        if (!string.IsNullOrEmpty(textBox.Text))
        {
            DirectoryInfo info = new(textBox.Text);
            if (info.Exists)
            {
                dlg.SelectedPath = info.FullName;
            }
        }

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            textBox.Text = dlg.SelectedPath;
        }
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

        //ILogLineColumnizer columnizer = Util.FindColumnizerByName(columnizerName, this.logTabWin.RegisteredColumnizers);
        //if (columnizer == null)
        //  columnizer = this.logTabWin.RegisteredColumnizers[0];
        comboBox.SelectedIndex = selIndex;
    }

    private void FillColumnizerList ()
    {
        dataGridViewColumnizer.Rows.Clear();

        var comboColumn = (DataGridViewComboBoxColumn)dataGridViewColumnizer.Columns[1];
        comboColumn.Items.Clear();

        //var textColumn = (DataGridViewTextBoxColumn)dataGridViewColumnizer.Columns[0];

        var columnizers = PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers;

        foreach (var columnizer in columnizers)
        {
            _ = comboColumn.Items.Add(columnizer.GetName());
        }
        //comboColumn.DisplayMember = "Name";
        //comboColumn.ValueMember = "Columnizer";

        foreach (var maskEntry in Preferences.ColumnizerMaskList)
        {
            DataGridViewRow row = new();
            _ = row.Cells.Add(new DataGridViewTextBoxCell());
            DataGridViewComboBoxCell cell = new();

            foreach (var logColumnizer in columnizers)
            {
                _ = cell.Items.Add(logColumnizer.GetName());
            }

            _ = row.Cells.Add(cell);
            row.Cells[0].Value = maskEntry.Mask;
            var columnizer = ColumnizerPicker.DecideColumnizerByName(maskEntry.ColumnizerName,
                PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

            row.Cells[1].Value = columnizer.GetName();
            _ = dataGridViewColumnizer.Rows.Add(row);
        }

        var count = dataGridViewColumnizer.RowCount;

        if (count > 0 && !dataGridViewColumnizer.Rows[count - 1].IsNewRow)
        {
            var comboCell = (DataGridViewComboBoxCell)dataGridViewColumnizer.Rows[count - 1].Cells[1];
            comboCell.Value = comboCell.Items[0];
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

            var currentGroup = _logTabWin.FindHighlightGroup(maskEntry.HighlightGroupName);
            var highlightGroupList = _logTabWin.HighlightGroupList;
            currentGroup ??= highlightGroupList.Count > 0 ? highlightGroupList[0] : new HighlightGroup();

            row.Cells[1].Value = currentGroup.GroupName;
            _ = dataGridViewHighlightMask.Rows.Add(row);
        }

        var count = dataGridViewHighlightMask.RowCount;

        if (count > 0 && !dataGridViewHighlightMask.Rows[count - 1].IsNewRow)
        {
            var comboCell =
                (DataGridViewComboBoxCell)dataGridViewHighlightMask.Rows[count - 1].Cells[1];
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
                ColumnizerMaskEntry entry = new()
                {
                    Mask = (string)row.Cells[0].Value,
                    ColumnizerName = (string)row.Cells[1].Value
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
                configurator.SaveConfig(checkBoxPortableMode.Checked ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir);
            }
        }

        foreach (var entry in PluginRegistry.PluginRegistry.Instance.RegisteredKeywordActions)
        {
            if (entry is ILogExpertPluginConfigurator configurator)
            {
                configurator.SaveConfig(checkBoxPortableMode.Checked ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir);
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

        textBoxMultifilePattern.Text = Preferences.MultiFileOptions.FormatPattern; //TODO: Impport settings file throws an exception. Fix or I caused it?
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
            _selectedTool.Name = Util.IsNullOrSpaces(textBoxToolName.Text) ? textBoxTool.Text : textBoxToolName.Text;
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
                _ = NativeMethods.DestroyIcon(icon.Handle);
                icon.Dispose();
            }
            else
            {
                buttonIcon.Image = _emptyImage;
            }
        }
    }

    /// <summary>
    /// Populates the encoding list in the combo box with a predefined set of character encodings.
    /// </summary>
    /// <remarks>This method clears any existing items in the combo box and adds a selection of common
    /// encodings, including ASCII, Default (UTF-8), ISO-8859-1, UTF-8, Unicode, and Windows-1252. The value member of the combo
    /// box is set to a specific header name defined in the resources.</remarks>
    private void FillEncodingList ()
    {
        comboBoxEncoding.Items.Clear();

        _ = comboBoxEncoding.Items.Add(Encoding.ASCII);
        _ = comboBoxEncoding.Items.Add(Encoding.Default);
        _ = comboBoxEncoding.Items.Add(Encoding.GetEncoding("iso-8859-1"));
        _ = comboBoxEncoding.Items.Add(Encoding.UTF8);
        _ = comboBoxEncoding.Items.Add(Encoding.Unicode);
        _ = comboBoxEncoding.Items.Add(CodePagesEncodingProvider.Instance.GetEncoding(1252));

        comboBoxEncoding.ValueMember = Resources.SettingsDialog_UI_ComboBox_Encoding_ValueMember_HeaderName;
    }

    /// <summary>
    /// Populates the language selection list with available language options.
    /// </summary>
    /// <remarks>Clears any existing items in the language selection list and adds predefined language
    /// options. Currently, it includes English (United States) and German (Germany).</remarks>
    private void FillLanguageList ()
    {
        comboBoxLanguage.Items.Clear();

        _ = comboBoxLanguage.Items.Add(CultureInfo.GetCultureInfo("en-US").Name); // Add English as default
        _ = comboBoxLanguage.Items.Add(CultureInfo.GetCultureInfo("de-DE").Name);
    }

    #endregion

    #region Events handler

    private void OnSettingsDialogLoad (object sender, EventArgs e)
    {
        FillDialog();
    }

    private void OnBtnChangeFontClick (object sender, EventArgs e)
    {
        FontDialog dlg = new()
        {
            ShowEffects = false,
            AllowVerticalFonts = false,
            AllowScriptChange = false,
            Font = new Font(new FontFamily(Preferences.FontName), Preferences.FontSize)
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Preferences.FontSize = dlg.Font.Size;
            Preferences.FontName = dlg.Font.FontFamily.Name;
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
            ? DragOrientationsEnum.Vertical
            : radioButtonVerticalMouseDragInverted.Checked
                ? DragOrientationsEnum.InvertedVertical
                : DragOrientationsEnum.Horizontal;

        SaveColumnizerList();

        Preferences.MaskPrio = checkBoxMaskPrio.Checked;
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
        Preferences.DefaultEncoding = comboBoxEncoding.SelectedItem != null ? (comboBoxEncoding.SelectedItem as Encoding).HeaderName : Encoding.Default.HeaderName;
        Preferences.DefaultLanguage = comboBoxLanguage.SelectedItem != null ? (comboBoxLanguage.SelectedItem as string) : CultureInfo.GetCultureInfo("en-US").Name;
        Preferences.ShowColumnFinder = checkBoxColumnFinder.Checked;
        Preferences.UseLegacyReader = checkBoxLegacyReader.Checked;

        Preferences.MaximumFilterEntries = (int)upDownMaximumFilterEntries.Value;
        Preferences.MaximumFilterEntriesDisplayed = (int)upDownMaximumFilterEntriesDisplayed.Value;
        Preferences.ShowErrorMessageAllowOnlyOneInstances = checkBoxShowErrorMessageOnlyOneInstance.Checked;
        Preferences.DarkMode = checkBoxDarkMode.Checked;

        SavePluginSettings();
        SaveHighlightMaskList();
        GetToolListBoxData();
        SaveMultifileData();
    }

    private void OnBtnToolClick (object sender, EventArgs e)
    {
        OnBtnToolClickInternal(textBoxTool);
    }

    //TODO: what is the purpose of this click?
    private void OnBtnArgClick (object sender, EventArgs e)
    {
        OnBtnArgsClickInternal(textBoxArguments);
    }

    //TODO Remove or refactor this function
    private void OnDataGridViewColumnizerRowsAdded (object sender, DataGridViewRowsAddedEventArgs e)
    {
        var comboCell = (DataGridViewComboBoxCell)dataGridViewColumnizer.Rows[e.RowIndex].Cells[1];
        if (comboCell.Items.Count > 0)
        {
            //        comboCell.Value = comboCell.Items[0];
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

        var o = listBoxPlugin.SelectedItem;

        if (o != null)
        {
            _selectedPlugin = o as ILogExpertPluginConfigurator;

            if (o is ILogExpertPluginConfigurator)
            {
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
        dlg.Description = Resources.SettingsDialog_UI_FolderBrowser_SessionSaveDir;

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
                case CheckState.Checked when !File.Exists(ConfigManager.PortableModeDir + Path.DirectorySeparatorChar + ConfigManager.PortableModeSettingsFileName):
                    {
                        if (!Directory.Exists(ConfigManager.PortableModeDir))
                        {
                            _ = Directory.CreateDirectory(ConfigManager.PortableModeDir);
                        }

                        using (File.Create(ConfigManager.PortableModeDir + Path.DirectorySeparatorChar + ConfigManager.PortableModeSettingsFileName))
                        {
                            break;
                        }
                    }
                case CheckState.Unchecked when File.Exists(ConfigManager.PortableModeDir + Path.DirectorySeparatorChar + ConfigManager.PortableModeSettingsFileName):
                    {
                        File.Delete(ConfigManager.PortableModeDir + Path.DirectorySeparatorChar + ConfigManager.PortableModeSettingsFileName);
                        break;
                    }

                case CheckState.Unchecked:
                    //intentionally left blank
                    break;
                case CheckState.Checked:
                    //intentionally left blank
                    break;
                case CheckState.Indeterminate:
                    //intentionally left blank
                    break;
                default:
                    //intentionally left blank
                    break;
            }

            switch (checkBoxPortableMode.CheckState)
            {
                case CheckState.Unchecked:
                    {
                        checkBoxPortableMode.Text = Resources.SettingsDialog_UI_ActivatePortableMode;
                        Preferences.PortableMode = false;
                        break;
                    }
                case CheckState.Checked:
                    {
                        Preferences.PortableMode = true;
                        checkBoxPortableMode.Text = Resources.SettingsDialog_UI_DeActivatePortableMode;
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
            _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.SettingsDialog_UI_CouldNotCreatePortableMode, exception), Resources.Title_LogExpert_Error, MessageBoxButtons.OK);
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
    private void OnBtnIconClick (object sender, EventArgs e)
    {
        if (_selectedTool != null)
        {
            var iconFile = _selectedTool.IconFile;

            if (Util.IsNullOrSpaces(iconFile))
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
        OnBtnWorkingDirClick(textBoxWorkingDir);
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
    ///
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
                _ = MessageBox.Show(this, string.Format(CultureInfo.InvariantCulture, Resources.SettingsDialog_UI_Error_SettingsCouldNotBeImported, ex), Resources.Title_LogExpert_Error);
                return;
            }

            ConfigManager.Import(fileInfo, dlg.ImportFlags);
            Preferences = ConfigManager.Settings.Preferences;
            FillDialog();
            _ = MessageBox.Show(this, Resources.SettingsDialog_UI_SettingsImported, Resources.Title_LogExpert);
        }
    }

    #endregion

    #region Resourse Map

    /// <summary>
    /// Creates a mapping between UI controls and their corresponding text resources.
    /// </summary>
    /// <remarks>This method is used to associate UI controls with their respective text resources,
    /// facilitating localization and dynamic text updates within the application.</remarks>
    /// <returns>A <see cref="Dictionary{TKey, TValue}"/> where the key is a <see cref="Control"/> and the value is a <see
    /// cref="string"/> representing the text resource associated with the control.</returns>
    private Dictionary<Control, string> GetTextResourceMap ()
    {
        return new Dictionary<Control, string>
        {
            // General
            { labelWarningMaximumLineLength, Resources.SettingsDialog_UI_Label_WarningMaximumLineLength },
            { labelMaximumLineLength, Resources.SettingsDialog_UI_Label_MaximumLineLengthRestartRequired },
            { labelMaximumFilterEntriesDisplayed, Resources.SettingsDialog_UI_Label_MaximumFilterEntriesDisplayed },
            { labelMaximumFilterEntries, Resources.SettingsDialog_UI_Label_MaximumFilterEntries },
            { labelDefaultEncoding, Resources.SettingsDialog_UI_Label_DefaultEncoding },
            { groupBoxMisc, Resources.SettingsDialog_UI_GroupBox_Misc },
            { checkBoxShowErrorMessageOnlyOneInstance, Resources.SettingsDialog_UI_CheckBox_ShowErrorMessageOnlyOneInstance },
            { checkBoxColumnSize, Resources.SettingsDialog_UI_CheckBox_ColumnSize },
            { buttonTailColor, Resources.SettingsDialog_UI_Button_Color },
            { checkBoxTailState, Resources.SettingsDialog_UI_CheckBox_TailState },
            { checkBoxOpenLastFiles, Resources.SettingsDialog_UI_CheckBox_OpenLastFiles },
            { checkBoxSingleInstance, Resources.SettingsDialog_UI_CheckBox_SingleInstance },
            { checkBoxAskCloseTabs, Resources.SettingsDialog_UI_CheckBox_AskCloseTabs },
            { groupBoxDefaults, Resources.SettingsDialog_UI_GroupBox_Defaults },
            { checkBoxDarkMode, Resources.SettingsDialog_UI_CheckBox_DarkMode },
            { checkBoxFollowTail, Resources.SettingsDialog_UI_CheckBox_FollowTail },
            { checkBoxColumnFinder, Resources.SettingsDialog_UI_CheckBox_ColumnFinder },
            { checkBoxSyncFilter, Resources.SettingsDialog_UI_CheckBox_SyncFilter },
            { checkBoxFilterTail, Resources.SettingsDialog_UI_CheckBox_FilterTail },
            { groupBoxFont, Resources.SettingsDialog_UI_GroupBox_Font },
            { buttonChangeFont, Resources.SettingsDialog_UI_Button_ChangeFont },
            { labelFont, Resources.SettingsDialog_UI_Label_Font },

            // Tab Pages
            { tabPageViewSettings, Resources.SettingsDialog_UI_TabPage_ViewSettings },
            { tabPageTimeStampFeatures, Resources.SettingsDialog_UI_TabPage_TimestampFeatures },
            { tabPageExternalTools, Resources.SettingsDialog_UI_TabPage_ExternalTools },
            { tabPageColumnizers, Resources.SettingsDialog_UI_TabPage_Columnizers },
            { tabPageHighlightMask, Resources.SettingsDialog_UI_TabPage_Highlight },
            { tabPageMultiFile, Resources.SettingsDialog_UI_TabPage_MultiFile },
            { tabPagePlugins, Resources.SettingsDialog_UI_TabPage_Plugins },
            { tabPageSessions, Resources.SettingsDialog_UI_TabPage_Sessions },
            { tabPageMemory, Resources.SettingsDialog_UI_TabPage_Memory },

            // Timestamp/Time View
            { groupBoxTimeSpreadDisplay, Resources.SettingsDialog_UI_GroupBox_TimeSpreadDisplay },
            { groupBoxDisplayMode, Resources.SettingsDialog_UI_GroupBox_DisplayMode },
            { radioButtonLineView, Resources.SettingsDialog_UI_RadioButton_LineView },
            { radioButtonTimeView, Resources.SettingsDialog_UI_RadioButton_TimeView },
            { checkBoxReverseAlpha, Resources.SettingsDialog_UI_CheckBox_ReverseAlpha },
            { buttonTimespreadColor, Resources.SettingsDialog_UI_Button_TimespreadColor },
            { checkBoxTimeSpread, Resources.SettingsDialog_UI_CheckBox_TimeSpread },
            { groupBoxTimeStampNavigationControl, Resources.SettingsDialog_UI_GroupBox_TimestampNavigationControl },
            { checkBoxTimestamp, Resources.SettingsDialog_UI_CheckBox_Timestamp },

            // Mouse Drag
            { groupBoxMouseDragDefaults, Resources.SettingsDialog_UI_GroupBox_MouseDragDefault },
            { radioButtonVerticalMouseDragInverted, Resources.SettingsDialog_UI_RadioButton_VerticalMouseDragInverted },
            { radioButtonHorizMouseDrag, Resources.SettingsDialog_UI_RadioButton_HorizMouseDrag },
            { radioButtonVerticalMouseDrag, Resources.SettingsDialog_UI_RadioButton_VerticalMouseDrag },

            // Tools Section
            { labelToolsDescription, Resources.SettingsDialog_UI_Label_ToolsDescription },
            { buttonToolDelete, Resources.SettingsDialog_UI_Button_ToolDelete },
            { buttonToolAdd, Resources.SettingsDialog_UI_Button_ToolAdd },
            { buttonToolDown, Resources.SettingsDialog_UI_Button_ToolDown },
            { buttonToolUp, Resources.SettingsDialog_UI_Button_ToolUp },
            { groupBoxToolSettings, Resources.SettingsDialog_UI_GroupBox_ToolSettings },
            { labelWorkingDir, Resources.SettingsDialog_UI_Label_WorkingDir },
            { buttonWorkingDir, Resources.SettingsDialog_UI_Button_WorkingDir },
            { buttonIcon, Resources.SettingsDialog_UI_Button_Icon },
            { labelToolName, Resources.SettingsDialog_UI_Label_ToolName },
            { labelToolColumnizerForOutput, Resources.SettingsDialog_UI_Label_ToolColumnizerForOutput },
            { checkBoxSysout, Resources.SettingsDialog_UI_CheckBox_Sysout },
            { buttonArguments, Resources.SettingsDialog_UI_Button_Arguments },
            { labelTool, Resources.SettingsDialog_UI_Label_Tool },
            { buttonTool, Resources.SettingsDialog_UI_Button_Tool },
            { labelArguments, Resources.SettingsDialog_UI_Label_Arguments },

            // Columnizer/Highlight
            { checkBoxAutoPick, Resources.SettingsDialog_UI_CheckBox_AutoPick },
            { checkBoxMaskPrio, Resources.SettingsDialog_UI_CheckBox_MaskPrio },
            { buttonDelete, Resources.SettingsDialog_UI_Button_Delete },

            // MultiFile
            { groupBoxDefaultFileNamePattern, Resources.SettingsDialog_UI_GroupBox_DefaultFilenamePattern },
            { labelMaxDays, Resources.SettingsDialog_UI_Label_MaxDays },
            { labelPattern, Resources.SettingsDialog_UI_Label_Pattern },
            { labelHintMultiFile, Resources.SettingsDialog_UI_Label_HintMultiFile },
            { labelNoteMultiFile, Resources.SettingsDialog_UI_Label_NoteMultifile },
            { groupBoxWhenOpeningMultiFile, Resources.SettingsDialog_UI_GroupBox_WhenOpeningMultipleFiles },
            { radioButtonAskWhatToDo, Resources.SettingsDialog_UI_RadioButton_AskWhatToDo },
            { radioButtonTreatAllFilesAsOneMultifile, Resources.SettingsDialog_UI_RadioButton_TreatAllFilesAsOneMultiFile },
            { radioButtonLoadEveryFileIntoSeperatedTab, Resources.SettingsDialog_UI_RadioButton_LoadEveryFileIntoSeparateTab },

            // Plugin / Session / Memory
            { groupBoxPlugins, Resources.SettingsDialog_UI_GroupBox_Plugins },
            { groupBoxSettings, Resources.SettingsDialog_UI_GroupBox_Settings },
            { buttonConfigPlugin, Resources.SettingsDialog_UI_Button_ConfigurePlugin },
            { checkBoxPortableMode, Resources.SettingsDialog_UI_CheckBox_PortableMode },
            { checkBoxSaveFilter, Resources.SettingsDialog_UI_CheckBox_SaveFilter },
            { groupBoxPersistantFileLocation, Resources.SettingsDialog_UI_GroupBox_PersistenceFileLocation },
            { labelSessionSaveOwnDir, Resources.SettingsDialog_UI_Label_SessionSaveOwnDir },
            { buttonSessionSaveDir, Resources.SettingsDialog_UI_Button_SessionSaveDir },
            { radioButtonSessionSaveOwn, Resources.SettingsDialog_UI_RadioButton_SessionSaveOwn },
            { radioButtonsessionSaveDocuments, Resources.SettingsDialog_UI_RadioButton_SessionSaveDocuments },
            { radioButtonSessionSameDir, Resources.SettingsDialog_UI_RadioButton_SessionSameDir },
            { radioButtonSessionApplicationStartupDir, Resources.SettingsDialog_UI_RadioButton_SessionApplicationStartupDir },
            { checkBoxSaveSessions, Resources.SettingsDialog_UI_CheckBox_SaveSessions },
            { groupBoxCPUAndStuff, Resources.SettingsDialog_UI_GroupBox_CPUAndStuff },
            { checkBoxLegacyReader, Resources.SettingsDialog_UI_CheckBox_LegacyReader },
            { checkBoxMultiThread, Resources.SettingsDialog_UI_CheckBox_MultiThread },
            { labelFilePollingInterval, Resources.SettingsDialog_UI_Label_FilePollingInterval },
            { groupBoxLineBufferUsage, Resources.SettingsDialog_UI_GroupBox_LineBufferUsage },
            { labelInfo, Resources.SettingsDialog_UI_Label_Info },
            { labelNumberOfBlocks, Resources.SettingsDialog_UI_Label_NumberOfBlocks },
            { labelLinesPerBlock, Resources.SettingsDialog_UI_Label_LinesPerBlock },

            // Dialog buttons
            { buttonCancel, Resources.LogExpert_Common_UI_Button_Cancel },
            { buttonOk, Resources.LogExpert_Common_UI_Button_OK },
            { buttonExport, Resources.LogExpert_Common_UI_Button_Export },
            { buttonImport, Resources.LogExpert_Common_UI_Button_Import },
        };
    }

    /// <summary>
    /// Creates a mapping of UI controls to their corresponding tooltip text.
    /// </summary>
    /// <remarks>This method initializes a dictionary with predefined tooltips for specific UI controls.
    /// Additional tooltips can be added to the dictionary as needed.</remarks>
    /// <returns>A <see cref="Dictionary{TKey, TValue}"/> where the keys are <see cref="Control"/> objects and the values are
    /// strings representing the tooltip text for each control.</returns>
    private Dictionary<Control, string> GetToolTipMap ()
    {
        return new Dictionary<Control, string>
        {
            { comboBoxLanguage, Resources.SettingsDialog_UI_ComboBox_ToolTip_Language },
            { comboBoxEncoding, Resources.SettingsDialog_UI_ComboBox_ToolTip_Encoding },
            { checkBoxPortableMode, Resources.SettingsDialog_UI_CheckBox_ToolTip_PortableMode },
            { radioButtonSessionApplicationStartupDir, Resources.SettingsDialog_UI_RadioButton_ToolTip_SessionApplicationStartupDir },
            { checkBoxLegacyReader, Resources.SettingsDialog_UI_CheckBox_ToolTip_LegacyReader }
        };
    }

    #endregion
}