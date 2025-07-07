using System.Runtime.Versioning;
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

    private ILogExpertPluginConfigurator _selectedPlugin;
    private ToolEntry _selectedTool;

    #endregion

    #region cTor

    private SettingsDialog (Preferences prefs, LogTabWindow logTabWin)
    {
        Preferences = prefs;
        _logTabWin = logTabWin; //TODO: uses only HighlightGroupList. Can we pass IList instead?
        InitializeComponent();

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

    private void FillDialog ()
    {
        Preferences ??= new Preferences();

        if (Preferences.FontName == null)
        {
            Preferences.FontName = "Courier New";
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
                }
                break;
            case SessionSaveLocation.SameDir:
                {
                    radioButtonSessionSameDir.Checked = true;
                }
                break;
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

        var temp = Encoding.GetEncoding(Preferences.DefaultEncoding);

        comboBoxEncoding.SelectedItem = Encoding.GetEncoding(Preferences.DefaultEncoding);
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
        labelFont.Text = Preferences.FontName + @" " + (int)Preferences.FontSize;
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

    private void OnBtnToolClickInternal (TextBox textBox)
    {
        OpenFileDialog dlg = new();
        dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        if (string.IsNullOrEmpty(textBox.Text) == false)
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

    private void OnBtnWorkingDirClick (TextBox textBox)
    {
        FolderBrowserDialog dlg = new()
        {
            RootFolder = Environment.SpecialFolder.MyComputer,
            Description = @"Select a working directory"
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

    private void FillColumnizerForToolsList (ComboBox comboBox, string columnizerName)
    {
        var selIndex = 0;
        comboBox.Items.Clear();
        IList<ILogLineColumnizer> columnizers = PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers;

        foreach (ILogLineColumnizer columnizer in columnizers)
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

        var textColumn = (DataGridViewTextBoxColumn)dataGridViewColumnizer.Columns[0];

        IList<ILogLineColumnizer> columnizers = PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers;

        foreach (ILogLineColumnizer columnizer in columnizers)
        {
            comboColumn.Items.Add(columnizer.GetName());
        }
        //comboColumn.DisplayMember = "Name";
        //comboColumn.ValueMember = "Columnizer";

        foreach (ColumnizerMaskEntry maskEntry in Preferences.ColumnizerMaskList)
        {
            DataGridViewRow row = new();
            row.Cells.Add(new DataGridViewTextBoxCell());
            DataGridViewComboBoxCell cell = new();

            foreach (ILogLineColumnizer logColumnizer in columnizers)
            {
                cell.Items.Add(logColumnizer.GetName());
            }

            row.Cells.Add(cell);
            row.Cells[0].Value = maskEntry.Mask;
            ILogLineColumnizer columnizer = ColumnizerPicker.DecideColumnizerByName(maskEntry.ColumnizerName,
                PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers);

            row.Cells[1].Value = columnizer.GetName();
            dataGridViewColumnizer.Rows.Add(row);
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
        var textColumn = (DataGridViewTextBoxColumn)dataGridViewHighlightMask.Columns[0];

        foreach (HighlightGroup group in (IList<HighlightGroup>)_logTabWin.HighlightGroupList)
        {
            comboColumn.Items.Add(group.GroupName);
        }

        foreach (HighlightMaskEntry maskEntry in Preferences.HighlightMaskList)
        {
            DataGridViewRow row = new();
            row.Cells.Add(new DataGridViewTextBoxCell());
            DataGridViewComboBoxCell cell = new();

            foreach (HighlightGroup group in (IList<HighlightGroup>)_logTabWin.HighlightGroupList)
            {
                cell.Items.Add(group.GroupName);
            }

            row.Cells.Add(cell);
            row.Cells[0].Value = maskEntry.Mask;

            HighlightGroup currentGroup = _logTabWin.FindHighlightGroup(maskEntry.HighlightGroupName);
            var highlightGroupList = _logTabWin.HighlightGroupList;
            currentGroup ??= highlightGroupList.Count > 0 ? highlightGroupList[0] : new HighlightGroup();

            row.Cells[1].Value = currentGroup.GroupName;
            dataGridViewHighlightMask.Rows.Add(row);
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
                ColumnizerMaskEntry entry = new();
                entry.Mask = (string)row.Cells[0].Value;
                entry.ColumnizerName = (string)row.Cells[1].Value;
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
                HighlightMaskEntry entry = new();
                entry.Mask = (string)row.Cells[0].Value;
                entry.HighlightGroupName = (string)row.Cells[1].Value;
                Preferences.HighlightMaskList.Add(entry);
            }
        }
    }

    private void FillPluginList ()
    {
        listBoxPlugin.Items.Clear();

        foreach (IContextMenuEntry entry in PluginRegistry.PluginRegistry.Instance.RegisteredContextMenuPlugins)
        {
            listBoxPlugin.Items.Add(entry);
            if (entry is ILogExpertPluginConfigurator configurator)
            {
                configurator.StartConfig();
            }
        }

        foreach (IKeywordAction entry in PluginRegistry.PluginRegistry.Instance.RegisteredKeywordActions)
        {
            listBoxPlugin.Items.Add(entry);
            if (entry is ILogExpertPluginConfigurator configurator)
            {
                configurator.StartConfig();
            }
        }

        foreach (IFileSystemPlugin entry in PluginRegistry.PluginRegistry.Instance.RegisteredFileSystemPlugins)
        {
            listBoxPlugin.Items.Add(entry);
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

        foreach (IContextMenuEntry entry in PluginRegistry.PluginRegistry.Instance.RegisteredContextMenuPlugins)
        {
            if (entry is ILogExpertPluginConfigurator configurator)
            {
                configurator.SaveConfig(checkBoxPortableMode.Checked ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir);
            }
        }

        foreach (IKeywordAction entry in PluginRegistry.PluginRegistry.Instance.RegisteredKeywordActions)
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

        foreach (ToolEntry tool in Preferences.ToolEntries)
        {
            listBoxTools.Items.Add(tool.Clone(), tool.IsFavourite);
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
            Icon icon = NativeMethods.LoadIconFromExe(_selectedTool.IconFile, _selectedTool.IconIndex);
            if (icon != null)
            {
                Image image = icon.ToBitmap();
                buttonIcon.Image = image;
                NativeMethods.DestroyIcon(icon.Handle);
                icon.Dispose();
            }
            else
            {
                buttonIcon.Image = _emptyImage;
            }
        }
    }

    private void FillEncodingList ()
    {
        comboBoxEncoding.Items.Clear();

        comboBoxEncoding.Items.Add(Encoding.ASCII);
        comboBoxEncoding.Items.Add(Encoding.Default);
        comboBoxEncoding.Items.Add(Encoding.GetEncoding("iso-8859-1"));
        comboBoxEncoding.Items.Add(Encoding.UTF8);
        comboBoxEncoding.Items.Add(Encoding.Unicode);
        comboBoxEncoding.Items.Add(CodePagesEncodingProvider.Instance.GetEncoding(1252));

        comboBoxEncoding.ValueMember = "HeaderName";
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

        if (radioButtonVerticalMouseDrag.Checked)
        {
            Preferences.TimestampControlDragOrientation = DragOrientationsEnum.Vertical;
        }
        else if (radioButtonVerticalMouseDragInverted.Checked)
        {
            Preferences.TimestampControlDragOrientation = DragOrientationsEnum.InvertedVertical;
        }
        else
        {
            Preferences.TimestampControlDragOrientation = DragOrientationsEnum.Horizontal;
        }

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

        if (radioButtonsessionSaveDocuments.Checked)
        {
            Preferences.SaveLocation = SessionSaveLocation.DocumentsDir;
        }
        else if (radioButtonSessionSaveOwn.Checked)
        {
            Preferences.SaveLocation = SessionSaveLocation.OwnDir;
        }
        else if (radioButtonSessionApplicationStartupDir.Checked)
        {
            Preferences.SaveLocation = SessionSaveLocation.ApplicationStartupDir;
        }
        else
        {
            Preferences.SaveLocation = SessionSaveLocation.SameDir;
        }

        Preferences.SaveFilters = checkBoxSaveFilter.Checked;
        Preferences.BufferCount = (int)upDownBlockCount.Value;
        Preferences.LinesPerBuffer = (int)upDownLinesPerBlock.Value;
        Preferences.PollingInterval = (int)upDownPollingInterval.Value;
        Preferences.MultiThreadFilter = checkBoxMultiThread.Checked;
        Preferences.DefaultEncoding = comboBoxEncoding.SelectedItem != null ? (comboBoxEncoding.SelectedItem as Encoding).HeaderName : Encoding.Default.HeaderName;
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
            dataGridViewColumnizer.EndEdit();
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
        dlg.Description = @"Choose folder for LogExpert's session files";

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
                        if (Directory.Exists(ConfigManager.PortableModeDir) == false)
                        {
                            Directory.CreateDirectory(ConfigManager.PortableModeDir);
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
            }

            switch (checkBoxPortableMode.CheckState)
            {
                case CheckState.Unchecked:
                    {
                        checkBoxPortableMode.Text = @"Activate Portable Mode";
                        Preferences.PortableMode = false;
                        break;
                    }

                case CheckState.Checked:
                    {
                        Preferences.PortableMode = true;
                        checkBoxPortableMode.Text = @"Deactivate Portable Mode";
                        break;
                    }
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show($@"Could not create / delete marker for Portable Mode: {exception}", @"Error", MessageBoxButtons.OK);
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
        listBoxTools.Items.Add(new ToolEntry());
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
        upDownMultifileDays.Enabled = pattern.Contains("$D", System.StringComparison.Ordinal);
    }

    [SupportedOSPlatform("windows")]
    private void OnBtnExportClick (object sender, EventArgs e)
    {
        SaveFileDialog dlg = new()
        {
            Title = @"Export Settings to file",
            DefaultExt = "json",
            AddExtension = true,
            Filter = @"Settings (*.json)|*.json|All files (*.*)|*.*"
        };

        DialogResult result = dlg.ShowDialog();

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
            catch (Exception ex)
            {
                MessageBox.Show(this, $@"Settings could not be imported: {ex}", @"LogExpert");
                return;
            }

            ConfigManager.Import(fileInfo, dlg.ImportFlags);
            Preferences = ConfigManager.Settings.Preferences;
            FillDialog();
            MessageBox.Show(this, @"Settings imported", @"LogExpert");
        }
    }

    #endregion
}
