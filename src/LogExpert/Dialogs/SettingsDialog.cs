using LogExpert.Classes;
using LogExpert.Classes.Columnizer;
using LogExpert.Config;
using LogExpert.Controls.LogTabWindow;
using LogExpert.Entities;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace LogExpert.Dialogs
{
    internal partial class SettingsDialog : Form
    {
        #region Fields

        private readonly Image _emptyImage = new Bitmap(16, 16);
        private readonly LogTabWindow _logTabWin;

        private ILogExpertPluginConfigurator _selectedPlugin;
        private ToolEntry _selectedTool;

        #endregion

        #region cTor

        public SettingsDialog(Preferences prefs, LogTabWindow logTabWin)
        {
            Preferences = prefs;
            _logTabWin = logTabWin;
            InitializeComponent();

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public SettingsDialog(Preferences prefs, LogTabWindow logTabWin, int tabToOpen) : this(prefs, logTabWin)
        {
            tabControlSettings.SelectedIndex = tabToOpen;
        }

        #endregion

        #region Properties

        public Preferences Preferences { get; private set; }

        #endregion

        #region Private Methods

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private void FillDialog()
        {
            Preferences ??= new Preferences();

            if (Preferences.fontName == null)
            {
                Preferences.fontName = "Courier New";
            }

            if (Math.Abs(Preferences.fontSize) < 0.1)
            {
                Preferences.fontSize = 9.0f;
            }

            FillPortableMode();

            checkBoxDarkMode.Checked = Preferences.darkMode;
            checkBoxTimestamp.Checked = Preferences.timestampControl;
            checkBoxSyncFilter.Checked = Preferences.filterSync;
            checkBoxFilterTail.Checked = Preferences.filterTail;
            checkBoxFollowTail.Checked = Preferences.followTail;

            radioButtonHorizMouseDrag.Checked = Preferences.timestampControlDragOrientation == DateTimeDragControl.DragOrientations.Horizontal;
            radioButtonVerticalMouseDrag.Checked = Preferences.timestampControlDragOrientation == DateTimeDragControl.DragOrientations.Vertical;
            radioButtonVerticalMouseDragInverted.Checked = Preferences.timestampControlDragOrientation == DateTimeDragControl.DragOrientations.InvertedVertical;

            checkBoxSingleInstance.Checked = Preferences.allowOnlyOneInstance;
            checkBoxOpenLastFiles.Checked = Preferences.openLastFiles;
            checkBoxTailState.Checked = Preferences.showTailState;
            checkBoxColumnSize.Checked = Preferences.setLastColumnWidth;
            cpDownColumnWidth.Enabled = Preferences.setLastColumnWidth;

            if (Preferences.lastColumnWidth != 0)
            {
                if (Preferences.lastColumnWidth < cpDownColumnWidth.Minimum)
                {
                    Preferences.lastColumnWidth = (int)cpDownColumnWidth.Minimum;
                }

                if (Preferences.lastColumnWidth > cpDownColumnWidth.Maximum)
                {
                    Preferences.lastColumnWidth = (int)cpDownColumnWidth.Maximum;
                }

                cpDownColumnWidth.Value = Preferences.lastColumnWidth;
            }

            checkBoxTimeSpread.Checked = Preferences.showTimeSpread;
            checkBoxReverseAlpha.Checked = Preferences.reverseAlpha;

            radioButtonTimeView.Checked = Preferences.timeSpreadTimeMode;
            radioButtonLineView.Checked = !Preferences.timeSpreadTimeMode;

            checkBoxSaveSessions.Checked = Preferences.saveSessions;

            switch (Preferences.saveLocation)
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

            upDownMaximumFilterEntriesDisplayed.Value = Preferences.maximumFilterEntriesDisplayed;
            upDownMaximumFilterEntries.Value = Preferences.maximumFilterEntries;

            labelSessionSaveOwnDir.Text = Preferences.sessionSaveDirectory ?? string.Empty;
            checkBoxSaveFilter.Checked = Preferences.saveFilters;
            upDownBlockCount.Value = Preferences.bufferCount;
            upDownLinesPerBlock.Value = Preferences.linesPerBuffer;
            upDownPollingInterval.Value = Preferences.pollingInterval;
            checkBoxMultiThread.Checked = Preferences.multiThreadFilter;

            dataGridViewColumnizer.DataError += OnDataGridViewColumnizerDataError;

            FillColumnizerList();
            FillPluginList();
            DisplayFontName();
            FillHighlightMaskList();
            FillToolListbox();
            FillMultifileSettings();
            FillEncodingList();

            var temp = Encoding.GetEncoding(Preferences.defaultEncoding);

            comboBoxEncoding.SelectedItem = Encoding.GetEncoding(Preferences.defaultEncoding);
            checkBoxMaskPrio.Checked = Preferences.maskPrio;
            checkBoxAutoPick.Checked = Preferences.autoPick;
            checkBoxAskCloseTabs.Checked = Preferences.askForClose;
            checkBoxColumnFinder.Checked = Preferences.showColumnFinder;
            checkBoxLegacyReader.Checked = Preferences.useLegacyReader;
            checkBoxShowErrorMessageOnlyOneInstance.Checked = Preferences.ShowErrorMessageAllowOnlyOneInstances;
        }

        private void FillPortableMode()
        {
            checkBoxPortableMode.CheckState = Preferences.PortableMode ? CheckState.Checked : CheckState.Unchecked;
        }

        private void DisplayFontName()
        {
            labelFont.Text = Preferences.fontName + @" " + (int)Preferences.fontSize;
            labelFont.Font = new Font(new FontFamily(Preferences.fontName), Preferences.fontSize);
        }

        private void SaveMultifileData()
        {
            if (radioButtonLoadEveryFileIntoSeperatedTab.Checked)
            {
                Preferences.multiFileOption = MultiFileOption.SingleFiles;
            }

            if (radioButtonTreatAllFilesAsOneMultifile.Checked)
            {
                Preferences.multiFileOption = MultiFileOption.MultiFile;
            }

            if (radioButtonAskWhatToDo.Checked)
            {
                Preferences.multiFileOption = MultiFileOption.Ask;
            }

            Preferences.multiFileOptions.FormatPattern = textBoxMultifilePattern.Text;
            Preferences.multiFileOptions.MaxDayTry = (int)upDownMultifileDays.Value;
        }

        private void OnBtnToolClickInternal(TextBox textBox)
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

        private void OnBtnArgsClickInternal(TextBox textBox)
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

        private void OnBtnWorkingDirClick(TextBox textBox)
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

        private void FillColumnizerForToolsList()
        {
            if (_selectedTool != null)
            {
                FillColumnizerForToolsList(comboBoxColumnizer, _selectedTool.columnizerName);
            }
        }

        private void FillColumnizerForToolsList(ComboBox comboBox, string columnizerName)
        {
            int selIndex = 0;
            comboBox.Items.Clear();
            IList<ILogLineColumnizer> columnizers = PluginRegistry.Instance.RegisteredColumnizers;

            foreach (ILogLineColumnizer columnizer in columnizers)
            {
                int index = comboBox.Items.Add(columnizer.GetName());
                if (columnizer.GetName().Equals(columnizerName))
                {
                    selIndex = index;
                }
            }

            //ILogLineColumnizer columnizer = Util.FindColumnizerByName(columnizerName, this.logTabWin.RegisteredColumnizers);
            //if (columnizer == null)
            //  columnizer = this.logTabWin.RegisteredColumnizers[0];
            comboBox.SelectedIndex = selIndex;
        }

        private void FillColumnizerList()
        {
            dataGridViewColumnizer.Rows.Clear();

            DataGridViewComboBoxColumn comboColumn = (DataGridViewComboBoxColumn)dataGridViewColumnizer.Columns[1];
            comboColumn.Items.Clear();

            DataGridViewTextBoxColumn textColumn = (DataGridViewTextBoxColumn)dataGridViewColumnizer.Columns[0];

            IList<ILogLineColumnizer> columnizers = PluginRegistry.Instance.RegisteredColumnizers;

            foreach (ILogLineColumnizer columnizer in columnizers)
            {
                comboColumn.Items.Add(columnizer.GetName());
            }
            //comboColumn.DisplayMember = "Name";
            //comboColumn.ValueMember = "Columnizer";

            foreach (ColumnizerMaskEntry maskEntry in Preferences.columnizerMaskList)
            {
                DataGridViewRow row = new();
                row.Cells.Add(new DataGridViewTextBoxCell());
                DataGridViewComboBoxCell cell = new();

                foreach (ILogLineColumnizer logColumnizer in columnizers)
                {
                    cell.Items.Add(logColumnizer.GetName());
                }

                row.Cells.Add(cell);
                row.Cells[0].Value = maskEntry.mask;
                ILogLineColumnizer columnizer = ColumnizerPicker.DecideColumnizerByName(maskEntry.columnizerName,
                    PluginRegistry.Instance.RegisteredColumnizers);

                row.Cells[1].Value = columnizer.GetName();
                dataGridViewColumnizer.Rows.Add(row);
            }

            int count = dataGridViewColumnizer.RowCount;

            if (count > 0 && !dataGridViewColumnizer.Rows[count - 1].IsNewRow)
            {
                DataGridViewComboBoxCell comboCell = (DataGridViewComboBoxCell)dataGridViewColumnizer.Rows[count - 1].Cells[1];
                comboCell.Value = comboCell.Items[0];
            }
        }

        private void FillHighlightMaskList()
        {
            dataGridViewHighlightMask.Rows.Clear();

            DataGridViewComboBoxColumn comboColumn = (DataGridViewComboBoxColumn)dataGridViewHighlightMask.Columns[1];
            comboColumn.Items.Clear();

            //TODO Remove if not necessary
            DataGridViewTextBoxColumn textColumn = (DataGridViewTextBoxColumn)dataGridViewHighlightMask.Columns[0];

            foreach (HilightGroup group in (IList<HilightGroup>)_logTabWin.HilightGroupList)
            {
                comboColumn.Items.Add(group.GroupName);
            }

            foreach (HighlightMaskEntry maskEntry in Preferences.highlightMaskList)
            {
                DataGridViewRow row = new();
                row.Cells.Add(new DataGridViewTextBoxCell());
                DataGridViewComboBoxCell cell = new();

                foreach (HilightGroup group in (IList<HilightGroup>)_logTabWin.HilightGroupList)
                {
                    cell.Items.Add(group.GroupName);
                }

                row.Cells.Add(cell);
                row.Cells[0].Value = maskEntry.mask;

                HilightGroup currentGroup = _logTabWin.FindHighlightGroup(maskEntry.highlightGroupName);
                currentGroup ??= ((IList<HilightGroup>)_logTabWin.HilightGroupList)[0];
                currentGroup ??= new HilightGroup();

                row.Cells[1].Value = currentGroup.GroupName;
                dataGridViewHighlightMask.Rows.Add(row);
            }

            int count = dataGridViewHighlightMask.RowCount;

            if (count > 0 && !dataGridViewHighlightMask.Rows[count - 1].IsNewRow)
            {
                DataGridViewComboBoxCell comboCell =
                    (DataGridViewComboBoxCell)dataGridViewHighlightMask.Rows[count - 1].Cells[1];
                comboCell.Value = comboCell.Items[0];
            }
        }

        private void SaveColumnizerList()
        {
            Preferences.columnizerMaskList.Clear();

            foreach (DataGridViewRow row in dataGridViewColumnizer.Rows)
            {
                if (!row.IsNewRow)
                {
                    ColumnizerMaskEntry entry = new();
                    entry.mask = (string)row.Cells[0].Value;
                    entry.columnizerName = (string)row.Cells[1].Value;
                    Preferences.columnizerMaskList.Add(entry);
                }
            }
        }

        private void SaveHighlightMaskList()
        {
            Preferences.highlightMaskList.Clear();

            foreach (DataGridViewRow row in dataGridViewHighlightMask.Rows)
            {
                if (!row.IsNewRow)
                {
                    HighlightMaskEntry entry = new();
                    entry.mask = (string)row.Cells[0].Value;
                    entry.highlightGroupName = (string)row.Cells[1].Value;
                    Preferences.highlightMaskList.Add(entry);
                }
            }
        }

        private void FillPluginList()
        {
            listBoxPlugin.Items.Clear();

            foreach (IContextMenuEntry entry in PluginRegistry.Instance.RegisteredContextMenuPlugins)
            {
                listBoxPlugin.Items.Add(entry);
                if (entry is ILogExpertPluginConfigurator configurator)
                {
                    configurator.StartConfig();
                }
            }

            foreach (IKeywordAction entry in PluginRegistry.Instance.RegisteredKeywordActions)
            {
                listBoxPlugin.Items.Add(entry);
                if (entry is ILogExpertPluginConfigurator configurator)
                {
                    configurator.StartConfig();
                }
            }

            foreach (IFileSystemPlugin entry in PluginRegistry.Instance.RegisteredFileSystemPlugins)
            {
                listBoxPlugin.Items.Add(entry);
                if (entry is ILogExpertPluginConfigurator configurator)
                {
                    configurator.StartConfig();
                }
            }

            buttonConfigPlugin.Enabled = false;
        }

        private void SavePluginSettings()
        {
            _selectedPlugin?.HideConfigForm();

            foreach (IContextMenuEntry entry in PluginRegistry.Instance.RegisteredContextMenuPlugins)
            {
                if (entry is ILogExpertPluginConfigurator configurator)
                {
                    configurator.SaveConfig(checkBoxPortableMode.Checked ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir);
                }
            }

            foreach (IKeywordAction entry in PluginRegistry.Instance.RegisteredKeywordActions)
            {
                if (entry is ILogExpertPluginConfigurator configurator)
                {
                    configurator.SaveConfig(checkBoxPortableMode.Checked ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir);
                }
            }
        }

        private void FillToolListbox()
        {
            listBoxTools.Items.Clear();

            foreach (ToolEntry tool in Preferences.toolEntries)
            {
                listBoxTools.Items.Add(tool.Clone(), tool.isFavourite);
            }

            if (listBoxTools.Items.Count > 0)
            {
                listBoxTools.SelectedIndex = 0;
            }
        }

        private void FillMultifileSettings()
        {
            switch (Preferences.multiFileOption)
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

            textBoxMultifilePattern.Text = Preferences.multiFileOptions.FormatPattern;
            upDownMultifileDays.Value = Preferences.multiFileOptions.MaxDayTry;
        }

        private void GetToolListBoxData()
        {
            GetCurrentToolValues();
            Preferences.toolEntries.Clear();

            for (int i = 0; i < listBoxTools.Items.Count; ++i)
            {
                Preferences.toolEntries.Add(listBoxTools.Items[i] as ToolEntry);
                (listBoxTools.Items[i] as ToolEntry).isFavourite = listBoxTools.GetItemChecked(i);
            }
        }

        private void GetCurrentToolValues()
        {
            if (_selectedTool != null)
            {
                _selectedTool.name = Util.IsNullOrSpaces(textBoxToolName.Text) ? textBoxTool.Text : textBoxToolName.Text;
                _selectedTool.cmd = textBoxTool.Text;
                _selectedTool.args = textBoxArguments.Text;
                _selectedTool.columnizerName = comboBoxColumnizer.Text;
                _selectedTool.sysout = checkBoxSysout.Checked;
                _selectedTool.workingDir = textBoxWorkingDir.Text;
            }
        }

        private void ShowCurrentToolValues()
        {
            if (_selectedTool != null)
            {
                textBoxToolName.Text = _selectedTool.name;
                textBoxTool.Text = _selectedTool.cmd;
                textBoxArguments.Text = _selectedTool.args;
                comboBoxColumnizer.Text = _selectedTool.columnizerName;
                checkBoxSysout.Checked = _selectedTool.sysout;
                comboBoxColumnizer.Enabled = _selectedTool.sysout;
                textBoxWorkingDir.Text = _selectedTool.workingDir;
            }
        }

        private void DisplayCurrentIcon()
        {
            if (_selectedTool != null)
            {
                Icon icon = Win32.LoadIconFromExe(_selectedTool.iconFile, _selectedTool.iconIndex);
                if (icon != null)
                {
                    Image image = icon.ToBitmap();
                    buttonIcon.Image = image;
                    DestroyIcon(icon.Handle);
                    icon.Dispose();
                }
                else
                {
                    buttonIcon.Image = _emptyImage;
                }
            }
        }

        private void FillEncodingList()
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

        private void OnSettingsDialogLoad(object sender, EventArgs e)
        {
            FillDialog();
        }

        private void OnBtnChangeFontClick(object sender, EventArgs e)
        {
            FontDialog dlg = new()
            {
                ShowEffects = false,
                AllowVerticalFonts = false,
                AllowScriptChange = false,
                Font = new Font(new FontFamily(Preferences.fontName), Preferences.fontSize)
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Preferences.fontSize = dlg.Font.Size;
                Preferences.fontName = dlg.Font.FontFamily.Name;
            }

            DisplayFontName();
        }

        private void OnBtnOkClick(object sender, EventArgs e)
        {
            Preferences.timestampControl = checkBoxTimestamp.Checked;
            Preferences.filterSync = checkBoxSyncFilter.Checked;
            Preferences.filterTail = checkBoxFilterTail.Checked;
            Preferences.followTail = checkBoxFollowTail.Checked;

            if (radioButtonVerticalMouseDrag.Checked)
            {
                Preferences.timestampControlDragOrientation = DateTimeDragControl.DragOrientations.Vertical;
            }
            else if (radioButtonVerticalMouseDragInverted.Checked)
            {
                Preferences.timestampControlDragOrientation = DateTimeDragControl.DragOrientations.InvertedVertical;
            }
            else
            {
                Preferences.timestampControlDragOrientation = DateTimeDragControl.DragOrientations.Horizontal;
            }

            SaveColumnizerList();

            Preferences.maskPrio = checkBoxMaskPrio.Checked;
            Preferences.autoPick = checkBoxAutoPick.Checked;
            Preferences.askForClose = checkBoxAskCloseTabs.Checked;
            Preferences.allowOnlyOneInstance = checkBoxSingleInstance.Checked;
            Preferences.openLastFiles = checkBoxOpenLastFiles.Checked;
            Preferences.showTailState = checkBoxTailState.Checked;
            Preferences.setLastColumnWidth = checkBoxColumnSize.Checked;
            Preferences.lastColumnWidth = (int)cpDownColumnWidth.Value;
            Preferences.showTimeSpread = checkBoxTimeSpread.Checked;
            Preferences.reverseAlpha = checkBoxReverseAlpha.Checked;
            Preferences.timeSpreadTimeMode = radioButtonTimeView.Checked;

            Preferences.saveSessions = checkBoxSaveSessions.Checked;
            Preferences.sessionSaveDirectory = labelSessionSaveOwnDir.Text;

            if (radioButtonsessionSaveDocuments.Checked)
            {
                Preferences.saveLocation = SessionSaveLocation.DocumentsDir;
            }
            else if (radioButtonSessionSaveOwn.Checked)
            {
                Preferences.saveLocation = SessionSaveLocation.OwnDir;
            }
            else if (radioButtonSessionApplicationStartupDir.Checked)
            {
                Preferences.saveLocation = SessionSaveLocation.ApplicationStartupDir;
            }
            else
            {
                Preferences.saveLocation = SessionSaveLocation.SameDir;
            }

            Preferences.saveFilters = checkBoxSaveFilter.Checked;
            Preferences.bufferCount = (int)upDownBlockCount.Value;
            Preferences.linesPerBuffer = (int)upDownLinesPerBlock.Value;
            Preferences.pollingInterval = (int)upDownPollingInterval.Value;
            Preferences.multiThreadFilter = checkBoxMultiThread.Checked;
            Preferences.defaultEncoding = comboBoxEncoding.SelectedItem != null ? (comboBoxEncoding.SelectedItem as Encoding).HeaderName : Encoding.Default.HeaderName;
            Preferences.showColumnFinder = checkBoxColumnFinder.Checked;
            Preferences.useLegacyReader = checkBoxLegacyReader.Checked;

            Preferences.maximumFilterEntries = (int)upDownMaximumFilterEntries.Value;
            Preferences.maximumFilterEntriesDisplayed = (int)upDownMaximumFilterEntriesDisplayed.Value;
            Preferences.ShowErrorMessageAllowOnlyOneInstances = checkBoxShowErrorMessageOnlyOneInstance.Checked;
            Preferences.darkMode = checkBoxDarkMode.Checked;

            SavePluginSettings();
            SaveHighlightMaskList();
            GetToolListBoxData();
            SaveMultifileData();
        }

        private void OnBtnToolClick(object sender, EventArgs e)
        {
            OnBtnToolClickInternal(textBoxTool);
        }

        private void OnBtnArgClick(object sender, EventArgs e)
        {
            OnBtnArgsClickInternal(textBoxArguments);
        }

        //TODO Remove or refactor this function
        private void OnDataGridViewColumnizerRowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            DataGridViewComboBoxCell comboCell = (DataGridViewComboBoxCell)dataGridViewColumnizer.Rows[e.RowIndex].Cells[1];
            if (comboCell.Items.Count > 0)
            {
                //        comboCell.Value = comboCell.Items[0];
            }
        }

        private void OnBtnDeleteClick(object sender, EventArgs e)
        {
            if (dataGridViewColumnizer.CurrentRow != null && !dataGridViewColumnizer.CurrentRow.IsNewRow)
            {
                int index = dataGridViewColumnizer.CurrentRow.Index;
                dataGridViewColumnizer.EndEdit();
                dataGridViewColumnizer.Rows.RemoveAt(index);
            }
        }

        private void OnDataGridViewColumnizerDataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void OnChkBoxSysoutCheckedChanged(object sender, EventArgs e)
        {
            comboBoxColumnizer.Enabled = checkBoxSysout.Checked;
        }

        private void OnBtnTailColorClick(object sender, EventArgs e)
        {
            ColorDialog dlg = new()
            {
                Color = Preferences.showTailColor
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Preferences.showTailColor = dlg.Color;
            }
        }

        private void OnChkBoxColumnSizeCheckedChanged(object sender, EventArgs e)
        {
            cpDownColumnWidth.Enabled = checkBoxColumnSize.Checked;
        }

        private void OnBtnTimespreadColorClick(object sender, EventArgs e)
        {
            ColorDialog dlg = new()
            {
                Color = Preferences.timeSpreadColor
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Preferences.timeSpreadColor = dlg.Color;
            }
        }

        private void OnListBoxPluginSelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedPlugin?.HideConfigForm();

            object o = listBoxPlugin.SelectedItem;

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

        private void OnBtnSessionSaveDirClick(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new();

            if (Preferences.sessionSaveDirectory != null)
            {
                dlg.SelectedPath = Preferences.sessionSaveDirectory;
            }

            dlg.ShowNewFolderButton = true;
            dlg.Description = @"Choose folder for LogExpert's session files";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                labelSessionSaveOwnDir.Text = dlg.SelectedPath;
            }
        }

        private void OnPortableModeCheckedChanged(object sender, EventArgs e)
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

        private void OnBtnConfigPluginClick(object sender, EventArgs e)
        {
            if (!_selectedPlugin.HasEmbeddedForm())
            {
                _selectedPlugin.ShowConfigDialog(this);
            }
        }

        private void OnNumericUpDown1ValueChanged(object sender, EventArgs e)
        {
            //TODO implement
        }

        private void OnListBoxToolSelectedIndexChanged(object sender, EventArgs e)
        {
            GetCurrentToolValues();
            _selectedTool = listBoxTools.SelectedItem as ToolEntry;
            ShowCurrentToolValues();
            listBoxTools.Refresh();
            FillColumnizerForToolsList();
            DisplayCurrentIcon();
        }

        private void OnBtnToolUpClick(object sender, EventArgs e)
        {
            int i = listBoxTools.SelectedIndex;

            if (i > 0)
            {
                bool isChecked = listBoxTools.GetItemChecked(i);
                object item = listBoxTools.Items[i];
                listBoxTools.Items.RemoveAt(i);
                i--;
                listBoxTools.Items.Insert(i, item);
                listBoxTools.SelectedIndex = i;
                listBoxTools.SetItemChecked(i, isChecked);
            }
        }

        private void OnBtnToolDownClick(object sender, EventArgs e)
        {
            int i = listBoxTools.SelectedIndex;

            if (i < listBoxTools.Items.Count - 1)
            {
                bool isChecked = listBoxTools.GetItemChecked(i);
                object item = listBoxTools.Items[i];
                listBoxTools.Items.RemoveAt(i);
                i++;
                listBoxTools.Items.Insert(i, item);
                listBoxTools.SelectedIndex = i;
                listBoxTools.SetItemChecked(i, isChecked);
            }
        }

        private void OnBtnToolAddClick(object sender, EventArgs e)
        {
            listBoxTools.Items.Add(new ToolEntry());
            listBoxTools.SelectedIndex = listBoxTools.Items.Count - 1;
        }

        private void OnToolDeleteButtonClick(object sender, EventArgs e)
        {
            int i = listBoxTools.SelectedIndex;

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

        private void OnBtnIconClick(object sender, EventArgs e)
        {
            if (_selectedTool != null)
            {
                string iconFile = _selectedTool.iconFile;

                if (Util.IsNullOrSpaces(iconFile))
                {
                    iconFile = textBoxTool.Text;
                }

                ChooseIconDlg dlg = new(iconFile);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _selectedTool.iconFile = dlg.FileName;
                    _selectedTool.iconIndex = dlg.IconIndex;
                    DisplayCurrentIcon();
                }
            }
        }

        private void OnBtnCancelClick(object sender, EventArgs e)
        {
            _selectedPlugin?.HideConfigForm();
        }

        private void OnBtnWorkingDirClick(object sender, EventArgs e)
        {
            OnBtnWorkingDirClick(textBoxWorkingDir);
        }

        private void OnMultiFilePatternTextChanged(object sender, EventArgs e)
        {
            string pattern = textBoxMultifilePattern.Text;
            upDownMultifileDays.Enabled = pattern.Contains("$D");
        }

        private void OnBtnExportClick(object sender, EventArgs e)
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
        private void OnBtnImportClick(object sender, EventArgs e)
        {
            ImportSettingsDialog dlg = new();

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
}
