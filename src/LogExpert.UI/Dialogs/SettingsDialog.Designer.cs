namespace LogExpert.Dialogs;

partial class SettingsDialog
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent ()
    {
        components = new System.ComponentModel.Container();
        var resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsDialog));
        tabControlSettings = new TabControl();
        tabPageViewSettings = new TabPage();
        labelWarningMaximumLineLength = new Label();
        upDownMaximumLineLength = new NumericUpDown();
        labelMaximumLineLength = new Label();
        upDownMaxDisplayLength = new NumericUpDown();
        labelMaxDisplayLength = new Label();
        upDownMaximumFilterEntriesDisplayed = new NumericUpDown();
        labelMaximumFilterEntriesDisplayed = new Label();
        upDownMaximumFilterEntries = new NumericUpDown();
        labelMaximumFilterEntries = new Label();
        labelDefaultEncoding = new Label();
        comboBoxEncoding = new ComboBox();
        groupBoxMisc = new GroupBox();
        checkBoxShowErrorMessageOnlyOneInstance = new CheckBox();
        cpDownColumnWidth = new NumericUpDown();
        checkBoxColumnSize = new CheckBox();
        buttonTailColor = new Button();
        checkBoxTailState = new CheckBox();
        checkBoxOpenLastFiles = new CheckBox();
        checkBoxSingleInstance = new CheckBox();
        checkBoxAskCloseTabs = new CheckBox();
        groupBoxDefaults = new GroupBox();
        labelLanguage = new Label();
        comboBoxLanguage = new ComboBox();
        labelColorMode = new Label();
        comboBoxColorMode = new ComboBox();
        checkBoxFollowTail = new CheckBox();
        checkBoxColumnFinder = new CheckBox();
        checkBoxSyncFilter = new CheckBox();
        checkBoxFilterTail = new CheckBox();
        groupBoxFont = new GroupBox();
        buttonChangeFont = new Button();
        labelFont = new Label();
        tabPageTimeStampFeatures = new TabPage();
        groupBoxTimeSpreadDisplay = new GroupBox();
        groupBoxDisplayMode = new GroupBox();
        radioButtonLineView = new RadioButton();
        radioButtonTimeView = new RadioButton();
        checkBoxReverseAlpha = new CheckBox();
        buttonTimespreadColor = new Button();
        checkBoxTimeSpread = new CheckBox();
        groupBoxTimeStampNavigationControl = new GroupBox();
        checkBoxTimestamp = new CheckBox();
        groupBoxMouseDragDefaults = new GroupBox();
        radioButtonVerticalMouseDragInverted = new RadioButton();
        radioButtonHorizMouseDrag = new RadioButton();
        radioButtonVerticalMouseDrag = new RadioButton();
        tabPageExternalTools = new TabPage();
        labelToolsDescription = new Label();
        buttonToolDelete = new Button();
        buttonToolAdd = new Button();
        buttonToolDown = new Button();
        buttonToolUp = new Button();
        listBoxTools = new CheckedListBox();
        groupBoxToolSettings = new GroupBox();
        labelWorkingDir = new Label();
        buttonWorkingDir = new Button();
        textBoxWorkingDir = new TextBox();
        buttonIcon = new Button();
        labelToolName = new Label();
        labelToolColumnizerForOutput = new Label();
        comboBoxColumnizer = new ComboBox();
        textBoxToolName = new TextBox();
        checkBoxSysout = new CheckBox();
        buttonArguments = new Button();
        labelTool = new Label();
        buttonTool = new Button();
        textBoxTool = new TextBox();
        labelArguments = new Label();
        textBoxArguments = new TextBox();
        tabPageColumnizers = new TabPage();
        groupBoxColumnizerPriority = new GroupBox();
        checkBoxAutoPick = new CheckBox();
        radioColumnizerPriorityHistoryThenMask = new RadioButton();
        radioColumnizerPriorityMaskThenHistory = new RadioButton();
        radioColumnizerPriorityMaskOverridesPersistence = new RadioButton();
        buttonDelete = new Button();
        dataGridViewColumnizer = new DataGridView();
        tabPageHighlightMask = new TabPage();
        dataGridViewHighlightMask = new DataGridView();
        dataGridViewTextBoxColumnFileName = new DataGridViewTextBoxColumn();
        dataGridViewComboBoxColumnHighlightGroup = new DataGridViewComboBoxColumn();
        tabPageControlCharacters = new TabPage();
        checkBoxControlCharsEnable = new CheckBox();
        labelControlCharsWarning = new Label();
        labelControlCharsHint = new Label();
        groupBoxControlCharsStyle = new GroupBox();
        radioButtonControlCharStyleControlPictures = new RadioButton();
        radioButtonControlCharStyleCaret = new RadioButton();
        radioButtonControlCharStyleCEscape = new RadioButton();
        radioButtonControlCharStyleAbbreviation = new RadioButton();
        radioButtonControlCharStyleIso2047 = new RadioButton();
        buttonControlCharsPresetAll = new Button();
        buttonControlCharsPresetNone = new Button();
        buttonControlCharsPresetNonWhitespace = new Button();
        dataGridViewControlChars = new DataGridView();
        columnControlCharEnabled = new DataGridViewCheckBoxColumn();
        columnControlCharHex = new DataGridViewTextBoxColumn();
        columnControlCharAbbr = new DataGridViewTextBoxColumn();
        columnControlCharCaret = new DataGridViewTextBoxColumn();
        columnControlCharPreview = new DataGridViewTextBoxColumn();
        groupBoxControlCharsAppearance = new GroupBox();
        labelControlCharsForeColor = new Label();
        buttonControlCharsForeColor = new Button();
        labelControlCharsBackColor = new Label();
        buttonControlCharsBackColor = new Button();
        buttonControlCharsBackColorClear = new Button();
        checkBoxControlCharsBold = new CheckBox();
        checkBoxControlCharsItalic = new CheckBox();
        labelControlCharsSample = new Label();
        groupBoxControlCharsClipboard = new GroupBox();
        checkBoxControlCharsCopyDisplayedForm = new CheckBox();
        tabPageMultiFile = new TabPage();
        groupBoxDefaultFileNamePattern = new GroupBox();
        labelMaxDays = new Label();
        labelPattern = new Label();
        upDownMultifileDays = new NumericUpDown();
        textBoxMultifilePattern = new TextBox();
        labelHintMultiFile = new Label();
        labelNoteMultiFile = new Label();
        groupBoxWhenOpeningMultiFile = new GroupBox();
        radioButtonAskWhatToDo = new RadioButton();
        radioButtonTreatAllFilesAsOneMultifile = new RadioButton();
        radioButtonLoadEveryFileIntoSeperatedTab = new RadioButton();
        tabPagePlugins = new TabPage();
        groupBoxPlugins = new GroupBox();
        listBoxPlugin = new ListBox();
        groupBoxSettings = new GroupBox();
        panelPlugin = new Panel();
        buttonConfigPlugin = new Button();
        tabPageSessions = new TabPage();
        checkBoxPortableMode = new CheckBox();
        checkBoxSaveFilter = new CheckBox();
        groupBoxPersistantFileLocation = new GroupBox();
        labelSessionSaveOwnDir = new Label();
        buttonSessionSaveDir = new Button();
        radioButtonSessionSaveOwn = new RadioButton();
        radioButtonsessionSaveDocuments = new RadioButton();
        radioButtonSessionSameDir = new RadioButton();
        radioButtonSessionApplicationStartupDir = new RadioButton();
        checkBoxSaveSessions = new CheckBox();
        tabPageMemory = new TabPage();
        groupBoxCPUAndStuff = new GroupBox();
        comboBoxReaderType = new ComboBox();
        checkBoxMultiThread = new CheckBox();
        labelFilePollingInterval = new Label();
        upDownPollingInterval = new NumericUpDown();
        groupBoxLineBufferUsage = new GroupBox();
        labelInfo = new Label();
        labelNumberOfBlocks = new Label();
        upDownLinesPerBlock = new NumericUpDown();
        upDownBlockCount = new NumericUpDown();
        labelLinesPerBlock = new Label();
        buttonCancel = new Button();
        buttonOk = new Button();
        helpProvider = new HelpProvider();
        toolTip = new ToolTip(components);
        buttonExport = new Button();
        buttonImport = new Button();
        dataGridViewImageColumnColumnizerStale = new DataGridViewImageColumn();
        dataGridViewTextBoxColumnFileMask = new DataGridViewTextBoxColumn();
        dataGridViewComboBoxColumnColumnizerMaskType = new DataGridViewComboBoxColumn();
        dataGridViewComboBoxColumnColumnizer = new DataGridViewComboBoxColumn();
        tabControlSettings.SuspendLayout();
        tabPageViewSettings.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)upDownMaximumLineLength).BeginInit();
        ((System.ComponentModel.ISupportInitialize)upDownMaxDisplayLength).BeginInit();
        ((System.ComponentModel.ISupportInitialize)upDownMaximumFilterEntriesDisplayed).BeginInit();
        ((System.ComponentModel.ISupportInitialize)upDownMaximumFilterEntries).BeginInit();
        groupBoxMisc.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)cpDownColumnWidth).BeginInit();
        groupBoxDefaults.SuspendLayout();
        groupBoxFont.SuspendLayout();
        tabPageTimeStampFeatures.SuspendLayout();
        groupBoxTimeSpreadDisplay.SuspendLayout();
        groupBoxDisplayMode.SuspendLayout();
        groupBoxTimeStampNavigationControl.SuspendLayout();
        groupBoxMouseDragDefaults.SuspendLayout();
        tabPageExternalTools.SuspendLayout();
        groupBoxToolSettings.SuspendLayout();
        tabPageColumnizers.SuspendLayout();
        groupBoxColumnizerPriority.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridViewColumnizer).BeginInit();
        tabPageHighlightMask.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridViewHighlightMask).BeginInit();
        tabPageControlCharacters.SuspendLayout();
        groupBoxControlCharsStyle.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridViewControlChars).BeginInit();
        groupBoxControlCharsAppearance.SuspendLayout();
        groupBoxControlCharsClipboard.SuspendLayout();
        tabPageMultiFile.SuspendLayout();
        groupBoxDefaultFileNamePattern.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)upDownMultifileDays).BeginInit();
        groupBoxWhenOpeningMultiFile.SuspendLayout();
        tabPagePlugins.SuspendLayout();
        groupBoxPlugins.SuspendLayout();
        groupBoxSettings.SuspendLayout();
        panelPlugin.SuspendLayout();
        tabPageSessions.SuspendLayout();
        groupBoxPersistantFileLocation.SuspendLayout();
        tabPageMemory.SuspendLayout();
        groupBoxCPUAndStuff.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)upDownPollingInterval).BeginInit();
        groupBoxLineBufferUsage.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)upDownLinesPerBlock).BeginInit();
        ((System.ComponentModel.ISupportInitialize)upDownBlockCount).BeginInit();
        SuspendLayout();
        // 
        // tabControlSettings
        // 
        tabControlSettings.Controls.Add(tabPageViewSettings);
        tabControlSettings.Controls.Add(tabPageTimeStampFeatures);
        tabControlSettings.Controls.Add(tabPageExternalTools);
        tabControlSettings.Controls.Add(tabPageColumnizers);
        tabControlSettings.Controls.Add(tabPageHighlightMask);
        tabControlSettings.Controls.Add(tabPageControlCharacters);
        tabControlSettings.Controls.Add(tabPageMultiFile);
        tabControlSettings.Controls.Add(tabPagePlugins);
        tabControlSettings.Controls.Add(tabPageSessions);
        tabControlSettings.Controls.Add(tabPageMemory);
        tabControlSettings.Location = new Point(2, 3);
        tabControlSettings.Margin = new Padding(4, 5, 4, 5);
        tabControlSettings.Name = "tabControlSettings";
        tabControlSettings.SelectedIndex = 0;
        tabControlSettings.Size = new Size(950, 468);
        tabControlSettings.TabIndex = 0;
        // 
        // tabPageViewSettings
        // 
        tabPageViewSettings.Controls.Add(labelWarningMaximumLineLength);
        tabPageViewSettings.Controls.Add(upDownMaximumLineLength);
        tabPageViewSettings.Controls.Add(labelMaximumLineLength);
        tabPageViewSettings.Controls.Add(upDownMaxDisplayLength);
        tabPageViewSettings.Controls.Add(labelMaxDisplayLength);
        tabPageViewSettings.Controls.Add(upDownMaximumFilterEntriesDisplayed);
        tabPageViewSettings.Controls.Add(labelMaximumFilterEntriesDisplayed);
        tabPageViewSettings.Controls.Add(upDownMaximumFilterEntries);
        tabPageViewSettings.Controls.Add(labelMaximumFilterEntries);
        tabPageViewSettings.Controls.Add(labelDefaultEncoding);
        tabPageViewSettings.Controls.Add(comboBoxEncoding);
        tabPageViewSettings.Controls.Add(groupBoxMisc);
        tabPageViewSettings.Controls.Add(groupBoxDefaults);
        tabPageViewSettings.Controls.Add(groupBoxFont);
        tabPageViewSettings.Location = new Point(4, 24);
        tabPageViewSettings.Margin = new Padding(4, 5, 4, 5);
        tabPageViewSettings.Name = "tabPageViewSettings";
        tabPageViewSettings.Padding = new Padding(4, 5, 4, 5);
        tabPageViewSettings.Size = new Size(942, 440);
        tabPageViewSettings.TabIndex = 0;
        tabPageViewSettings.Text = "View settings";
        tabPageViewSettings.UseVisualStyleBackColor = true;
        // 
        // labelWarningMaximumLineLength
        // 
        labelWarningMaximumLineLength.AutoSize = true;
        labelWarningMaximumLineLength.Location = new Point(446, 101);
        labelWarningMaximumLineLength.Name = "labelWarningMaximumLineLength";
        labelWarningMaximumLineLength.Size = new Size(482, 15);
        labelWarningMaximumLineLength.TabIndex = 16;
        labelWarningMaximumLineLength.Text = "! Changing the Maximum Line Length can impact performance and is not recommended !";
        // 
        // upDownMaximumLineLength
        // 
        upDownMaximumLineLength.Location = new Point(762, 121);
        upDownMaximumLineLength.Margin = new Padding(4, 5, 4, 5);
        upDownMaximumLineLength.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
        upDownMaximumLineLength.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
        upDownMaximumLineLength.Name = "upDownMaximumLineLength";
        upDownMaximumLineLength.Size = new Size(106, 23);
        upDownMaximumLineLength.TabIndex = 15;
        upDownMaximumLineLength.Value = new decimal(new int[] { 20000, 0, 0, 0 });
        upDownMaximumLineLength.ValueChanged += OnUpDownMaximumLineLengthValueChanged;
        // 
        // labelMaximumLineLength
        // 
        labelMaximumLineLength.AutoSize = true;
        labelMaximumLineLength.Location = new Point(467, 123);
        labelMaximumLineLength.Margin = new Padding(4, 0, 4, 0);
        labelMaximumLineLength.Name = "labelMaximumLineLength";
        labelMaximumLineLength.Size = new Size(217, 15);
        labelMaximumLineLength.TabIndex = 14;
        labelMaximumLineLength.Text = "Maximum Line Length (restart required)";
        // 
        // upDownMaxDisplayLength
        // 
        upDownMaxDisplayLength.Location = new Point(762, 149);
        upDownMaxDisplayLength.Margin = new Padding(4, 5, 4, 5);
        upDownMaxDisplayLength.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
        upDownMaxDisplayLength.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
        upDownMaxDisplayLength.Name = "upDownMaxDisplayLength";
        upDownMaxDisplayLength.Size = new Size(106, 23);
        upDownMaxDisplayLength.TabIndex = 18;
        upDownMaxDisplayLength.Value = new decimal(new int[] { 20000, 0, 0, 0 });
        upDownMaxDisplayLength.ValueChanged += OnUpDownMaxDisplayLengthValueChanged;
        // 
        // labelMaxDisplayLength
        // 
        labelMaxDisplayLength.AutoSize = true;
        labelMaxDisplayLength.Location = new Point(467, 151);
        labelMaxDisplayLength.Margin = new Padding(4, 0, 4, 0);
        labelMaxDisplayLength.Name = "labelMaxDisplayLength";
        labelMaxDisplayLength.Size = new Size(233, 15);
        labelMaxDisplayLength.TabIndex = 17;
        labelMaxDisplayLength.Text = "Maximum Display Length (restart required)";
        // 
        // upDownMaximumFilterEntriesDisplayed
        // 
        upDownMaximumFilterEntriesDisplayed.Location = new Point(762, 69);
        upDownMaximumFilterEntriesDisplayed.Margin = new Padding(4, 5, 4, 5);
        upDownMaximumFilterEntriesDisplayed.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
        upDownMaximumFilterEntriesDisplayed.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
        upDownMaximumFilterEntriesDisplayed.Name = "upDownMaximumFilterEntriesDisplayed";
        upDownMaximumFilterEntriesDisplayed.Size = new Size(106, 23);
        upDownMaximumFilterEntriesDisplayed.TabIndex = 13;
        upDownMaximumFilterEntriesDisplayed.Value = new decimal(new int[] { 20, 0, 0, 0 });
        // 
        // labelMaximumFilterEntriesDisplayed
        // 
        labelMaximumFilterEntriesDisplayed.AutoSize = true;
        labelMaximumFilterEntriesDisplayed.Location = new Point(467, 71);
        labelMaximumFilterEntriesDisplayed.Margin = new Padding(4, 0, 4, 0);
        labelMaximumFilterEntriesDisplayed.Name = "labelMaximumFilterEntriesDisplayed";
        labelMaximumFilterEntriesDisplayed.Size = new Size(179, 15);
        labelMaximumFilterEntriesDisplayed.TabIndex = 12;
        labelMaximumFilterEntriesDisplayed.Text = "Maximum filter entries displayed";
        // 
        // upDownMaximumFilterEntries
        // 
        upDownMaximumFilterEntries.Location = new Point(762, 42);
        upDownMaximumFilterEntries.Margin = new Padding(4, 5, 4, 5);
        upDownMaximumFilterEntries.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
        upDownMaximumFilterEntries.Name = "upDownMaximumFilterEntries";
        upDownMaximumFilterEntries.Size = new Size(106, 23);
        upDownMaximumFilterEntries.TabIndex = 11;
        upDownMaximumFilterEntries.Value = new decimal(new int[] { 30, 0, 0, 0 });
        // 
        // labelMaximumFilterEntries
        // 
        labelMaximumFilterEntries.AutoSize = true;
        labelMaximumFilterEntries.Location = new Point(467, 44);
        labelMaximumFilterEntries.Margin = new Padding(4, 0, 4, 0);
        labelMaximumFilterEntries.Name = "labelMaximumFilterEntries";
        labelMaximumFilterEntries.Size = new Size(126, 15);
        labelMaximumFilterEntries.TabIndex = 10;
        labelMaximumFilterEntries.Text = "Maximum filter entries";
        // 
        // labelDefaultEncoding
        // 
        labelDefaultEncoding.AutoSize = true;
        labelDefaultEncoding.Location = new Point(467, 17);
        labelDefaultEncoding.Margin = new Padding(4, 0, 4, 0);
        labelDefaultEncoding.Name = "labelDefaultEncoding";
        labelDefaultEncoding.Size = new Size(98, 15);
        labelDefaultEncoding.TabIndex = 9;
        labelDefaultEncoding.Text = "Default encoding";
        // 
        // comboBoxEncoding
        // 
        comboBoxEncoding.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxEncoding.FormattingEnabled = true;
        comboBoxEncoding.Location = new Point(691, 9);
        comboBoxEncoding.Margin = new Padding(4, 5, 4, 5);
        comboBoxEncoding.Name = "comboBoxEncoding";
        comboBoxEncoding.Size = new Size(177, 23);
        comboBoxEncoding.TabIndex = 8;
        toolTip.SetToolTip(comboBoxEncoding, "Encoding to be used when no BOM header and no persistence data is available.");
        // 
        // groupBoxMisc
        // 
        groupBoxMisc.Controls.Add(checkBoxShowErrorMessageOnlyOneInstance);
        groupBoxMisc.Controls.Add(cpDownColumnWidth);
        groupBoxMisc.Controls.Add(checkBoxColumnSize);
        groupBoxMisc.Controls.Add(buttonTailColor);
        groupBoxMisc.Controls.Add(checkBoxTailState);
        groupBoxMisc.Controls.Add(checkBoxOpenLastFiles);
        groupBoxMisc.Controls.Add(checkBoxSingleInstance);
        groupBoxMisc.Controls.Add(checkBoxAskCloseTabs);
        groupBoxMisc.Location = new Point(458, 171);
        groupBoxMisc.Margin = new Padding(4, 5, 4, 5);
        groupBoxMisc.Name = "groupBoxMisc";
        groupBoxMisc.Padding = new Padding(4, 5, 4, 5);
        groupBoxMisc.Size = new Size(410, 226);
        groupBoxMisc.TabIndex = 7;
        groupBoxMisc.TabStop = false;
        groupBoxMisc.Text = "Misc";
        // 
        // checkBoxShowErrorMessageOnlyOneInstance
        // 
        checkBoxShowErrorMessageOnlyOneInstance.AutoSize = true;
        checkBoxShowErrorMessageOnlyOneInstance.Location = new Point(210, 66);
        checkBoxShowErrorMessageOnlyOneInstance.Margin = new Padding(4, 5, 4, 5);
        checkBoxShowErrorMessageOnlyOneInstance.Name = "checkBoxShowErrorMessageOnlyOneInstance";
        checkBoxShowErrorMessageOnlyOneInstance.Size = new Size(137, 19);
        checkBoxShowErrorMessageOnlyOneInstance.TabIndex = 7;
        checkBoxShowErrorMessageOnlyOneInstance.Text = "Show Error Message?";
        checkBoxShowErrorMessageOnlyOneInstance.UseVisualStyleBackColor = true;
        // 
        // cpDownColumnWidth
        // 
        cpDownColumnWidth.Location = new Point(304, 175);
        cpDownColumnWidth.Margin = new Padding(4, 5, 4, 5);
        cpDownColumnWidth.Maximum = new decimal(new int[] { 9000, 0, 0, 0 });
        cpDownColumnWidth.Minimum = new decimal(new int[] { 300, 0, 0, 0 });
        cpDownColumnWidth.Name = "cpDownColumnWidth";
        cpDownColumnWidth.Size = new Size(84, 23);
        cpDownColumnWidth.TabIndex = 6;
        cpDownColumnWidth.Value = new decimal(new int[] { 2000, 0, 0, 0 });
        // 
        // checkBoxColumnSize
        // 
        checkBoxColumnSize.AutoSize = true;
        checkBoxColumnSize.Location = new Point(9, 177);
        checkBoxColumnSize.Margin = new Padding(4, 5, 4, 5);
        checkBoxColumnSize.Name = "checkBoxColumnSize";
        checkBoxColumnSize.Size = new Size(140, 19);
        checkBoxColumnSize.TabIndex = 5;
        checkBoxColumnSize.Text = "Set last column width";
        checkBoxColumnSize.UseVisualStyleBackColor = true;
        checkBoxColumnSize.CheckedChanged += OnChkBoxColumnSizeCheckedChanged;
        // 
        // buttonTailColor
        // 
        buttonTailColor.Location = new Point(304, 135);
        buttonTailColor.Margin = new Padding(4, 5, 4, 5);
        buttonTailColor.Name = "buttonTailColor";
        buttonTailColor.Size = new Size(84, 32);
        buttonTailColor.TabIndex = 4;
        buttonTailColor.Text = "Color...";
        buttonTailColor.UseVisualStyleBackColor = true;
        buttonTailColor.Click += OnBtnTailColorClick;
        // 
        // checkBoxTailState
        // 
        checkBoxTailState.AutoSize = true;
        checkBoxTailState.Location = new Point(9, 140);
        checkBoxTailState.Margin = new Padding(4, 5, 4, 5);
        checkBoxTailState.Name = "checkBoxTailState";
        checkBoxTailState.Size = new Size(144, 19);
        checkBoxTailState.TabIndex = 3;
        checkBoxTailState.Text = "Show tail state on tabs";
        checkBoxTailState.UseVisualStyleBackColor = true;
        // 
        // checkBoxOpenLastFiles
        // 
        checkBoxOpenLastFiles.AutoSize = true;
        checkBoxOpenLastFiles.Location = new Point(9, 103);
        checkBoxOpenLastFiles.Margin = new Padding(4, 5, 4, 5);
        checkBoxOpenLastFiles.Name = "checkBoxOpenLastFiles";
        checkBoxOpenLastFiles.Size = new Size(144, 19);
        checkBoxOpenLastFiles.TabIndex = 2;
        checkBoxOpenLastFiles.Text = "Re-open last used files";
        checkBoxOpenLastFiles.UseVisualStyleBackColor = true;
        // 
        // checkBoxSingleInstance
        // 
        checkBoxSingleInstance.AutoSize = true;
        checkBoxSingleInstance.Location = new Point(9, 66);
        checkBoxSingleInstance.Margin = new Padding(4, 5, 4, 5);
        checkBoxSingleInstance.Name = "checkBoxSingleInstance";
        checkBoxSingleInstance.Size = new Size(138, 19);
        checkBoxSingleInstance.TabIndex = 1;
        checkBoxSingleInstance.Text = "Allow only 1 Instance";
        checkBoxSingleInstance.UseVisualStyleBackColor = true;
        // 
        // checkBoxAskCloseTabs
        // 
        checkBoxAskCloseTabs.AutoSize = true;
        checkBoxAskCloseTabs.Location = new Point(9, 29);
        checkBoxAskCloseTabs.Margin = new Padding(4, 5, 4, 5);
        checkBoxAskCloseTabs.Name = "checkBoxAskCloseTabs";
        checkBoxAskCloseTabs.Size = new Size(148, 19);
        checkBoxAskCloseTabs.TabIndex = 0;
        checkBoxAskCloseTabs.Text = "Ask before closing tabs";
        checkBoxAskCloseTabs.UseVisualStyleBackColor = true;
        // 
        // groupBoxDefaults
        // 
        groupBoxDefaults.Controls.Add(labelLanguage);
        groupBoxDefaults.Controls.Add(comboBoxLanguage);
        groupBoxDefaults.Controls.Add(labelColorMode);
        groupBoxDefaults.Controls.Add(comboBoxColorMode);
        groupBoxDefaults.Controls.Add(checkBoxFollowTail);
        groupBoxDefaults.Controls.Add(checkBoxColumnFinder);
        groupBoxDefaults.Controls.Add(checkBoxSyncFilter);
        groupBoxDefaults.Controls.Add(checkBoxFilterTail);
        groupBoxDefaults.Location = new Point(10, 171);
        groupBoxDefaults.Margin = new Padding(4, 5, 4, 5);
        groupBoxDefaults.Name = "groupBoxDefaults";
        groupBoxDefaults.Padding = new Padding(4, 5, 4, 5);
        groupBoxDefaults.Size = new Size(411, 226);
        groupBoxDefaults.TabIndex = 6;
        groupBoxDefaults.TabStop = false;
        groupBoxDefaults.Text = "Defaults";
        // 
        // labelLanguage
        // 
        labelLanguage.AutoSize = true;
        labelLanguage.Location = new Point(9, 175);
        labelLanguage.Margin = new Padding(4, 0, 4, 0);
        labelLanguage.Name = "labelLanguage";
        labelLanguage.Size = new Size(187, 15);
        labelLanguage.TabIndex = 17;
        labelLanguage.Text = "Default encoding (requires restart)";
        // 
        // comboBoxLanguage
        // 
        comboBoxLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxLanguage.FormattingEnabled = true;
        comboBoxLanguage.Location = new Point(204, 172);
        comboBoxLanguage.Margin = new Padding(4, 5, 4, 5);
        comboBoxLanguage.Name = "comboBoxLanguage";
        comboBoxLanguage.Size = new Size(177, 23);
        comboBoxLanguage.TabIndex = 9;
        toolTip.SetToolTip(comboBoxLanguage, "Userinterface language");
        //
        // labelColorMode
        //
        labelColorMode.AutoSize = true;
        labelColorMode.Location = new Point(9, 147);
        labelColorMode.Margin = new Padding(4, 0, 4, 0);
        labelColorMode.Name = "labelColorMode";
        labelColorMode.Size = new Size(160, 15);
        labelColorMode.TabIndex = 18;
        labelColorMode.Text = "Color mode (restart required)";
        //
        // comboBoxColorMode
        //
        comboBoxColorMode.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxColorMode.FormattingEnabled = true;
        comboBoxColorMode.Location = new Point(204, 144);
        comboBoxColorMode.Margin = new Padding(4, 5, 4, 5);
        comboBoxColorMode.Name = "comboBoxColorMode";
        comboBoxColorMode.Size = new Size(177, 23);
        comboBoxColorMode.TabIndex = 6;
        // 
        // checkBoxFollowTail
        // 
        checkBoxFollowTail.AutoSize = true;
        checkBoxFollowTail.Location = new Point(9, 29);
        checkBoxFollowTail.Margin = new Padding(4, 5, 4, 5);
        checkBoxFollowTail.Name = "checkBoxFollowTail";
        checkBoxFollowTail.Size = new Size(125, 19);
        checkBoxFollowTail.TabIndex = 3;
        checkBoxFollowTail.Text = "Follow tail enabled";
        checkBoxFollowTail.UseVisualStyleBackColor = true;
        // 
        // checkBoxColumnFinder
        // 
        checkBoxColumnFinder.AutoSize = true;
        checkBoxColumnFinder.Location = new Point(9, 116);
        checkBoxColumnFinder.Margin = new Padding(4, 5, 4, 5);
        checkBoxColumnFinder.Name = "checkBoxColumnFinder";
        checkBoxColumnFinder.Size = new Size(133, 19);
        checkBoxColumnFinder.TabIndex = 5;
        checkBoxColumnFinder.Text = "Show column finder";
        checkBoxColumnFinder.UseVisualStyleBackColor = true;
        // 
        // checkBoxSyncFilter
        // 
        checkBoxSyncFilter.AutoSize = true;
        checkBoxSyncFilter.Location = new Point(9, 87);
        checkBoxSyncFilter.Margin = new Padding(4, 5, 4, 5);
        checkBoxSyncFilter.Name = "checkBoxSyncFilter";
        checkBoxSyncFilter.Size = new Size(141, 19);
        checkBoxSyncFilter.TabIndex = 5;
        checkBoxSyncFilter.Text = "Sync filter list enabled";
        checkBoxSyncFilter.UseVisualStyleBackColor = true;
        // 
        // checkBoxFilterTail
        // 
        checkBoxFilterTail.AutoSize = true;
        checkBoxFilterTail.Location = new Point(9, 58);
        checkBoxFilterTail.Margin = new Padding(4, 5, 4, 5);
        checkBoxFilterTail.Name = "checkBoxFilterTail";
        checkBoxFilterTail.Size = new Size(116, 19);
        checkBoxFilterTail.TabIndex = 4;
        checkBoxFilterTail.Text = "Filter tail enabled";
        checkBoxFilterTail.UseVisualStyleBackColor = true;
        // 
        // groupBoxFont
        // 
        groupBoxFont.Controls.Add(buttonChangeFont);
        groupBoxFont.Controls.Add(labelFont);
        groupBoxFont.Location = new Point(10, 9);
        groupBoxFont.Margin = new Padding(4, 5, 4, 5);
        groupBoxFont.Name = "groupBoxFont";
        groupBoxFont.Padding = new Padding(4, 5, 4, 5);
        groupBoxFont.Size = new Size(408, 128);
        groupBoxFont.TabIndex = 1;
        groupBoxFont.TabStop = false;
        groupBoxFont.Text = "Font";
        // 
        // buttonChangeFont
        // 
        buttonChangeFont.Location = new Point(9, 77);
        buttonChangeFont.Margin = new Padding(4, 5, 4, 5);
        buttonChangeFont.Name = "buttonChangeFont";
        buttonChangeFont.Size = new Size(112, 35);
        buttonChangeFont.TabIndex = 1;
        buttonChangeFont.Text = "Change...";
        buttonChangeFont.UseVisualStyleBackColor = true;
        buttonChangeFont.Click += OnBtnChangeFontClick;
        // 
        // labelFont
        // 
        labelFont.Location = new Point(9, 25);
        labelFont.Margin = new Padding(4, 0, 4, 0);
        labelFont.Name = "labelFont";
        labelFont.Size = new Size(312, 48);
        labelFont.TabIndex = 0;
        labelFont.Text = "Font";
        labelFont.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // tabPageTimeStampFeatures
        // 
        tabPageTimeStampFeatures.Controls.Add(groupBoxTimeSpreadDisplay);
        tabPageTimeStampFeatures.Controls.Add(groupBoxTimeStampNavigationControl);
        tabPageTimeStampFeatures.Location = new Point(4, 24);
        tabPageTimeStampFeatures.Margin = new Padding(4, 5, 4, 5);
        tabPageTimeStampFeatures.Name = "tabPageTimeStampFeatures";
        tabPageTimeStampFeatures.Padding = new Padding(4, 5, 4, 5);
        tabPageTimeStampFeatures.Size = new Size(942, 440);
        tabPageTimeStampFeatures.TabIndex = 1;
        tabPageTimeStampFeatures.Text = "Timestamp features";
        tabPageTimeStampFeatures.UseVisualStyleBackColor = true;
        // 
        // groupBoxTimeSpreadDisplay
        // 
        groupBoxTimeSpreadDisplay.Controls.Add(groupBoxDisplayMode);
        groupBoxTimeSpreadDisplay.Controls.Add(checkBoxReverseAlpha);
        groupBoxTimeSpreadDisplay.Controls.Add(buttonTimespreadColor);
        groupBoxTimeSpreadDisplay.Controls.Add(checkBoxTimeSpread);
        groupBoxTimeSpreadDisplay.Location = new Point(490, 25);
        groupBoxTimeSpreadDisplay.Margin = new Padding(4, 5, 4, 5);
        groupBoxTimeSpreadDisplay.Name = "groupBoxTimeSpreadDisplay";
        groupBoxTimeSpreadDisplay.Padding = new Padding(4, 5, 4, 5);
        groupBoxTimeSpreadDisplay.Size = new Size(300, 246);
        groupBoxTimeSpreadDisplay.TabIndex = 8;
        groupBoxTimeSpreadDisplay.TabStop = false;
        groupBoxTimeSpreadDisplay.Text = "Time spread display";
        // 
        // groupBoxDisplayMode
        // 
        groupBoxDisplayMode.Controls.Add(radioButtonLineView);
        groupBoxDisplayMode.Controls.Add(radioButtonTimeView);
        groupBoxDisplayMode.Location = new Point(22, 109);
        groupBoxDisplayMode.Margin = new Padding(4, 5, 4, 5);
        groupBoxDisplayMode.Name = "groupBoxDisplayMode";
        groupBoxDisplayMode.Padding = new Padding(4, 5, 4, 5);
        groupBoxDisplayMode.Size = new Size(188, 118);
        groupBoxDisplayMode.TabIndex = 11;
        groupBoxDisplayMode.TabStop = false;
        groupBoxDisplayMode.Text = "Display mode";
        // 
        // radioButtonLineView
        // 
        radioButtonLineView.AutoSize = true;
        radioButtonLineView.Location = new Point(9, 65);
        radioButtonLineView.Margin = new Padding(4, 5, 4, 5);
        radioButtonLineView.Name = "radioButtonLineView";
        radioButtonLineView.Size = new Size(74, 19);
        radioButtonLineView.TabIndex = 9;
        radioButtonLineView.TabStop = true;
        radioButtonLineView.Text = "Line view";
        radioButtonLineView.UseVisualStyleBackColor = true;
        // 
        // radioButtonTimeView
        // 
        radioButtonTimeView.AutoSize = true;
        radioButtonTimeView.Location = new Point(9, 29);
        radioButtonTimeView.Margin = new Padding(4, 5, 4, 5);
        radioButtonTimeView.Name = "radioButtonTimeView";
        radioButtonTimeView.Size = new Size(79, 19);
        radioButtonTimeView.TabIndex = 10;
        radioButtonTimeView.TabStop = true;
        radioButtonTimeView.Text = "Time view";
        radioButtonTimeView.UseVisualStyleBackColor = true;
        // 
        // checkBoxReverseAlpha
        // 
        checkBoxReverseAlpha.AutoSize = true;
        checkBoxReverseAlpha.Location = new Point(22, 74);
        checkBoxReverseAlpha.Margin = new Padding(4, 5, 4, 5);
        checkBoxReverseAlpha.Name = "checkBoxReverseAlpha";
        checkBoxReverseAlpha.Size = new Size(98, 19);
        checkBoxReverseAlpha.TabIndex = 8;
        checkBoxReverseAlpha.Text = "Reverse alpha";
        checkBoxReverseAlpha.UseVisualStyleBackColor = true;
        // 
        // buttonTimespreadColor
        // 
        buttonTimespreadColor.Location = new Point(207, 32);
        buttonTimespreadColor.Margin = new Padding(4, 5, 4, 5);
        buttonTimespreadColor.Name = "buttonTimespreadColor";
        buttonTimespreadColor.Size = new Size(84, 32);
        buttonTimespreadColor.TabIndex = 7;
        buttonTimespreadColor.Text = "Color...";
        buttonTimespreadColor.UseVisualStyleBackColor = true;
        buttonTimespreadColor.Click += OnBtnTimespreadColorClick;
        // 
        // checkBoxTimeSpread
        // 
        checkBoxTimeSpread.AutoSize = true;
        checkBoxTimeSpread.Location = new Point(22, 37);
        checkBoxTimeSpread.Margin = new Padding(4, 5, 4, 5);
        checkBoxTimeSpread.Name = "checkBoxTimeSpread";
        checkBoxTimeSpread.Size = new Size(120, 19);
        checkBoxTimeSpread.TabIndex = 6;
        checkBoxTimeSpread.Text = "Show time spread";
        checkBoxTimeSpread.UseVisualStyleBackColor = true;
        // 
        // groupBoxTimeStampNavigationControl
        // 
        groupBoxTimeStampNavigationControl.Controls.Add(checkBoxTimestamp);
        groupBoxTimeStampNavigationControl.Controls.Add(groupBoxMouseDragDefaults);
        groupBoxTimeStampNavigationControl.Location = new Point(10, 25);
        groupBoxTimeStampNavigationControl.Margin = new Padding(4, 5, 4, 5);
        groupBoxTimeStampNavigationControl.Name = "groupBoxTimeStampNavigationControl";
        groupBoxTimeStampNavigationControl.Padding = new Padding(4, 5, 4, 5);
        groupBoxTimeStampNavigationControl.Size = new Size(450, 246);
        groupBoxTimeStampNavigationControl.TabIndex = 7;
        groupBoxTimeStampNavigationControl.TabStop = false;
        groupBoxTimeStampNavigationControl.Text = "Timestamp navigation control";
        // 
        // checkBoxTimestamp
        // 
        checkBoxTimestamp.AutoSize = true;
        checkBoxTimestamp.Location = new Point(27, 37);
        checkBoxTimestamp.Margin = new Padding(4, 5, 4, 5);
        checkBoxTimestamp.Name = "checkBoxTimestamp";
        checkBoxTimestamp.Size = new Size(304, 19);
        checkBoxTimestamp.TabIndex = 3;
        checkBoxTimestamp.Text = "Show timestamp control, if supported by columnizer";
        checkBoxTimestamp.UseVisualStyleBackColor = true;
        // 
        // groupBoxMouseDragDefaults
        // 
        groupBoxMouseDragDefaults.Controls.Add(radioButtonVerticalMouseDragInverted);
        groupBoxMouseDragDefaults.Controls.Add(radioButtonHorizMouseDrag);
        groupBoxMouseDragDefaults.Controls.Add(radioButtonVerticalMouseDrag);
        groupBoxMouseDragDefaults.Location = new Point(27, 80);
        groupBoxMouseDragDefaults.Margin = new Padding(4, 5, 4, 5);
        groupBoxMouseDragDefaults.Name = "groupBoxMouseDragDefaults";
        groupBoxMouseDragDefaults.Padding = new Padding(4, 5, 4, 5);
        groupBoxMouseDragDefaults.Size = new Size(186, 148);
        groupBoxMouseDragDefaults.TabIndex = 5;
        groupBoxMouseDragDefaults.TabStop = false;
        groupBoxMouseDragDefaults.Text = "Mouse Drag Default";
        // 
        // radioButtonVerticalMouseDragInverted
        // 
        radioButtonVerticalMouseDragInverted.AutoSize = true;
        radioButtonVerticalMouseDragInverted.Location = new Point(9, 102);
        radioButtonVerticalMouseDragInverted.Margin = new Padding(4, 5, 4, 5);
        radioButtonVerticalMouseDragInverted.Name = "radioButtonVerticalMouseDragInverted";
        radioButtonVerticalMouseDragInverted.Size = new Size(109, 19);
        radioButtonVerticalMouseDragInverted.TabIndex = 6;
        radioButtonVerticalMouseDragInverted.TabStop = true;
        radioButtonVerticalMouseDragInverted.Text = "Vertical Inverted";
        radioButtonVerticalMouseDragInverted.UseVisualStyleBackColor = true;
        // 
        // radioButtonHorizMouseDrag
        // 
        radioButtonHorizMouseDrag.AutoSize = true;
        radioButtonHorizMouseDrag.Location = new Point(9, 29);
        radioButtonHorizMouseDrag.Margin = new Padding(4, 5, 4, 5);
        radioButtonHorizMouseDrag.Name = "radioButtonHorizMouseDrag";
        radioButtonHorizMouseDrag.Size = new Size(80, 19);
        radioButtonHorizMouseDrag.TabIndex = 5;
        radioButtonHorizMouseDrag.TabStop = true;
        radioButtonHorizMouseDrag.Text = "Horizontal";
        radioButtonHorizMouseDrag.UseVisualStyleBackColor = true;
        // 
        // radioButtonVerticalMouseDrag
        // 
        radioButtonVerticalMouseDrag.AutoSize = true;
        radioButtonVerticalMouseDrag.Location = new Point(9, 65);
        radioButtonVerticalMouseDrag.Margin = new Padding(4, 5, 4, 5);
        radioButtonVerticalMouseDrag.Name = "radioButtonVerticalMouseDrag";
        radioButtonVerticalMouseDrag.Size = new Size(63, 19);
        radioButtonVerticalMouseDrag.TabIndex = 4;
        radioButtonVerticalMouseDrag.TabStop = true;
        radioButtonVerticalMouseDrag.Text = "Vertical";
        radioButtonVerticalMouseDrag.UseVisualStyleBackColor = true;
        // 
        // tabPageExternalTools
        // 
        tabPageExternalTools.Controls.Add(labelToolsDescription);
        tabPageExternalTools.Controls.Add(buttonToolDelete);
        tabPageExternalTools.Controls.Add(buttonToolAdd);
        tabPageExternalTools.Controls.Add(buttonToolDown);
        tabPageExternalTools.Controls.Add(buttonToolUp);
        tabPageExternalTools.Controls.Add(listBoxTools);
        tabPageExternalTools.Controls.Add(groupBoxToolSettings);
        tabPageExternalTools.Location = new Point(4, 24);
        tabPageExternalTools.Margin = new Padding(4, 5, 4, 5);
        tabPageExternalTools.Name = "tabPageExternalTools";
        tabPageExternalTools.Padding = new Padding(4, 5, 4, 5);
        tabPageExternalTools.Size = new Size(942, 440);
        tabPageExternalTools.TabIndex = 2;
        tabPageExternalTools.Text = "External Tools";
        tabPageExternalTools.UseVisualStyleBackColor = true;
        // 
        // labelToolsDescription
        // 
        labelToolsDescription.Location = new Point(546, 102);
        labelToolsDescription.Margin = new Padding(4, 0, 4, 0);
        labelToolsDescription.Name = "labelToolsDescription";
        labelToolsDescription.Size = new Size(376, 80);
        labelToolsDescription.TabIndex = 6;
        labelToolsDescription.Text = "You can configure as many tools as you want. \r\nChecked tools will appear in the icon bar. All other tools are available in the tools menu.";
        // 
        // buttonToolDelete
        // 
        buttonToolDelete.Location = new Point(550, 14);
        buttonToolDelete.Margin = new Padding(4, 5, 4, 5);
        buttonToolDelete.Name = "buttonToolDelete";
        buttonToolDelete.Size = new Size(112, 35);
        buttonToolDelete.TabIndex = 2;
        buttonToolDelete.Text = "Remove";
        buttonToolDelete.UseVisualStyleBackColor = true;
        buttonToolDelete.Click += OnToolDeleteButtonClick;
        // 
        // buttonToolAdd
        // 
        buttonToolAdd.Location = new Point(429, 14);
        buttonToolAdd.Margin = new Padding(4, 5, 4, 5);
        buttonToolAdd.Name = "buttonToolAdd";
        buttonToolAdd.Size = new Size(112, 35);
        buttonToolAdd.TabIndex = 1;
        buttonToolAdd.Text = "Add new";
        buttonToolAdd.UseVisualStyleBackColor = true;
        buttonToolAdd.Click += OnBtnToolAddClick;
        // 
        // buttonToolDown
        // 
        buttonToolDown.Location = new Point(429, 146);
        buttonToolDown.Margin = new Padding(4, 5, 4, 5);
        buttonToolDown.Name = "buttonToolDown";
        buttonToolDown.Size = new Size(64, 35);
        buttonToolDown.TabIndex = 4;
        buttonToolDown.Text = "Down";
        buttonToolDown.UseVisualStyleBackColor = true;
        buttonToolDown.Click += OnBtnToolDownClick;
        // 
        // buttonToolUp
        // 
        buttonToolUp.Location = new Point(429, 102);
        buttonToolUp.Margin = new Padding(4, 5, 4, 5);
        buttonToolUp.Name = "buttonToolUp";
        buttonToolUp.Size = new Size(64, 35);
        buttonToolUp.TabIndex = 3;
        buttonToolUp.Text = "Up";
        buttonToolUp.UseVisualStyleBackColor = true;
        buttonToolUp.Click += OnBtnToolUpClick;
        // 
        // listBoxTools
        // 
        listBoxTools.DisplayMember = "Name";
        listBoxTools.FormattingEnabled = true;
        listBoxTools.Location = new Point(10, 14);
        listBoxTools.Margin = new Padding(4, 5, 4, 5);
        listBoxTools.Name = "listBoxTools";
        listBoxTools.Size = new Size(406, 148);
        listBoxTools.TabIndex = 0;
        listBoxTools.SelectedIndexChanged += OnListBoxToolSelectedIndexChanged;
        // 
        // groupBoxToolSettings
        // 
        groupBoxToolSettings.Controls.Add(labelWorkingDir);
        groupBoxToolSettings.Controls.Add(buttonWorkingDir);
        groupBoxToolSettings.Controls.Add(textBoxWorkingDir);
        groupBoxToolSettings.Controls.Add(buttonIcon);
        groupBoxToolSettings.Controls.Add(labelToolName);
        groupBoxToolSettings.Controls.Add(labelToolColumnizerForOutput);
        groupBoxToolSettings.Controls.Add(comboBoxColumnizer);
        groupBoxToolSettings.Controls.Add(textBoxToolName);
        groupBoxToolSettings.Controls.Add(checkBoxSysout);
        groupBoxToolSettings.Controls.Add(buttonArguments);
        groupBoxToolSettings.Controls.Add(labelTool);
        groupBoxToolSettings.Controls.Add(buttonTool);
        groupBoxToolSettings.Controls.Add(textBoxTool);
        groupBoxToolSettings.Controls.Add(labelArguments);
        groupBoxToolSettings.Controls.Add(textBoxArguments);
        groupBoxToolSettings.Location = new Point(10, 191);
        groupBoxToolSettings.Margin = new Padding(4, 5, 4, 5);
        groupBoxToolSettings.Name = "groupBoxToolSettings";
        groupBoxToolSettings.Padding = new Padding(4, 5, 4, 5);
        groupBoxToolSettings.Size = new Size(912, 228);
        groupBoxToolSettings.TabIndex = 0;
        groupBoxToolSettings.TabStop = false;
        groupBoxToolSettings.Text = "Tool settings";
        // 
        // labelWorkingDir
        // 
        labelWorkingDir.AutoSize = true;
        labelWorkingDir.Location = new Point(474, 86);
        labelWorkingDir.Margin = new Padding(4, 0, 4, 0);
        labelWorkingDir.Name = "labelWorkingDir";
        labelWorkingDir.Size = new Size(72, 15);
        labelWorkingDir.TabIndex = 11;
        labelWorkingDir.Text = "Working dir:";
        // 
        // buttonWorkingDir
        // 
        buttonWorkingDir.Location = new Point(856, 80);
        buttonWorkingDir.Margin = new Padding(4, 5, 4, 5);
        buttonWorkingDir.Name = "buttonWorkingDir";
        buttonWorkingDir.Size = new Size(45, 31);
        buttonWorkingDir.TabIndex = 10;
        buttonWorkingDir.Text = "...";
        buttonWorkingDir.UseVisualStyleBackColor = true;
        buttonWorkingDir.Click += OnBtnWorkingDirClick;
        // 
        // textBoxWorkingDir
        // 
        textBoxWorkingDir.Location = new Point(576, 82);
        textBoxWorkingDir.Margin = new Padding(4, 5, 4, 5);
        textBoxWorkingDir.Name = "textBoxWorkingDir";
        textBoxWorkingDir.Size = new Size(270, 23);
        textBoxWorkingDir.TabIndex = 9;
        // 
        // buttonIcon
        // 
        buttonIcon.ImageAlign = ContentAlignment.MiddleLeft;
        buttonIcon.Location = new Point(418, 26);
        buttonIcon.Margin = new Padding(4, 5, 4, 5);
        buttonIcon.Name = "buttonIcon";
        buttonIcon.Size = new Size(112, 35);
        buttonIcon.TabIndex = 1;
        buttonIcon.Text = "   Icon...";
        buttonIcon.TextImageRelation = TextImageRelation.ImageBeforeText;
        buttonIcon.UseVisualStyleBackColor = true;
        buttonIcon.Click += OnBtnToolIconClick;
        // 
        // labelToolName
        // 
        labelToolName.AutoSize = true;
        labelToolName.Location = new Point(9, 34);
        labelToolName.Margin = new Padding(4, 0, 4, 0);
        labelToolName.Name = "labelToolName";
        labelToolName.Size = new Size(42, 15);
        labelToolName.TabIndex = 8;
        labelToolName.Text = "Name:";
        // 
        // labelToolColumnizerForOutput
        // 
        labelToolColumnizerForOutput.AutoSize = true;
        labelToolColumnizerForOutput.Location = new Point(404, 185);
        labelToolColumnizerForOutput.Margin = new Padding(4, 0, 4, 0);
        labelToolColumnizerForOutput.Name = "labelToolColumnizerForOutput";
        labelToolColumnizerForOutput.Size = new Size(128, 15);
        labelToolColumnizerForOutput.TabIndex = 6;
        labelToolColumnizerForOutput.Text = "Columnizer for output:";
        // 
        // comboBoxColumnizer
        // 
        comboBoxColumnizer.FormattingEnabled = true;
        comboBoxColumnizer.Location = new Point(576, 180);
        comboBoxColumnizer.Margin = new Padding(4, 5, 4, 5);
        comboBoxColumnizer.Name = "comboBoxColumnizer";
        comboBoxColumnizer.Size = new Size(270, 23);
        comboBoxColumnizer.TabIndex = 7;
        // 
        // textBoxToolName
        // 
        textBoxToolName.Location = new Point(108, 29);
        textBoxToolName.Margin = new Padding(4, 5, 4, 5);
        textBoxToolName.Name = "textBoxToolName";
        textBoxToolName.Size = new Size(298, 23);
        textBoxToolName.TabIndex = 0;
        // 
        // checkBoxSysout
        // 
        checkBoxSysout.AutoSize = true;
        checkBoxSysout.Location = new Point(108, 183);
        checkBoxSysout.Margin = new Padding(4, 5, 4, 5);
        checkBoxSysout.Name = "checkBoxSysout";
        checkBoxSysout.Size = new Size(120, 19);
        checkBoxSysout.TabIndex = 6;
        checkBoxSysout.Text = "Pipe sysout to tab";
        checkBoxSysout.UseVisualStyleBackColor = true;
        checkBoxSysout.CheckedChanged += OnChkBoxSysoutCheckedChanged;
        // 
        // buttonArguments
        // 
        buttonArguments.Location = new Point(856, 128);
        buttonArguments.Margin = new Padding(4, 5, 4, 5);
        buttonArguments.Name = "buttonArguments";
        buttonArguments.Size = new Size(46, 32);
        buttonArguments.TabIndex = 5;
        buttonArguments.Text = "...";
        buttonArguments.UseVisualStyleBackColor = true;
        buttonArguments.Click += OnBtnArgClick;
        // 
        // labelTool
        // 
        labelTool.AutoSize = true;
        labelTool.Location = new Point(9, 86);
        labelTool.Margin = new Padding(4, 0, 4, 0);
        labelTool.Name = "labelTool";
        labelTool.Size = new Size(56, 15);
        labelTool.TabIndex = 4;
        labelTool.Text = "Program:";
        // 
        // buttonTool
        // 
        buttonTool.Location = new Point(418, 78);
        buttonTool.Margin = new Padding(4, 5, 4, 5);
        buttonTool.Name = "buttonTool";
        buttonTool.Size = new Size(45, 31);
        buttonTool.TabIndex = 3;
        buttonTool.Text = "...";
        buttonTool.UseVisualStyleBackColor = true;
        buttonTool.Click += OnBtnToolClick;
        // 
        // textBoxTool
        // 
        textBoxTool.Location = new Point(108, 80);
        textBoxTool.Margin = new Padding(4, 5, 4, 5);
        textBoxTool.Name = "textBoxTool";
        textBoxTool.Size = new Size(298, 23);
        textBoxTool.TabIndex = 2;
        // 
        // labelArguments
        // 
        labelArguments.AutoSize = true;
        labelArguments.Location = new Point(9, 134);
        labelArguments.Margin = new Padding(4, 0, 4, 0);
        labelArguments.Name = "labelArguments";
        labelArguments.Size = new Size(69, 15);
        labelArguments.TabIndex = 1;
        labelArguments.Text = "Arguments:";
        // 
        // textBoxArguments
        // 
        textBoxArguments.Location = new Point(108, 129);
        textBoxArguments.Margin = new Padding(4, 5, 4, 5);
        textBoxArguments.Name = "textBoxArguments";
        textBoxArguments.Size = new Size(738, 23);
        textBoxArguments.TabIndex = 4;
        // 
        // tabPageColumnizers
        // 
        tabPageColumnizers.Controls.Add(groupBoxColumnizerPriority);
        tabPageColumnizers.Controls.Add(buttonDelete);
        tabPageColumnizers.Controls.Add(dataGridViewColumnizer);
        tabPageColumnizers.Location = new Point(4, 24);
        tabPageColumnizers.Margin = new Padding(4, 5, 4, 5);
        tabPageColumnizers.Name = "tabPageColumnizers";
        tabPageColumnizers.Padding = new Padding(4, 5, 4, 5);
        tabPageColumnizers.Size = new Size(942, 440);
        tabPageColumnizers.TabIndex = 3;
        tabPageColumnizers.Text = "Columnizers";
        tabPageColumnizers.UseVisualStyleBackColor = true;
        // 
        // groupBoxColumnizerPriority
        // 
        groupBoxColumnizerPriority.Controls.Add(checkBoxAutoPick);
        groupBoxColumnizerPriority.Controls.Add(radioColumnizerPriorityHistoryThenMask);
        groupBoxColumnizerPriority.Controls.Add(radioColumnizerPriorityMaskThenHistory);
        groupBoxColumnizerPriority.Controls.Add(radioColumnizerPriorityMaskOverridesPersistence);
        groupBoxColumnizerPriority.Location = new Point(4, 339);
        groupBoxColumnizerPriority.Name = "groupBoxColumnizerPriority";
        groupBoxColumnizerPriority.Size = new Size(500, 98);
        groupBoxColumnizerPriority.TabIndex = 4;
        groupBoxColumnizerPriority.TabStop = false;
        groupBoxColumnizerPriority.Text = "Columnizer selection priority";
        // 
        // checkBoxAutoPick
        // 
        checkBoxAutoPick.AutoSize = true;
        checkBoxAutoPick.Checked = true;
        checkBoxAutoPick.CheckState = CheckState.Checked;
        checkBoxAutoPick.Location = new Point(301, 72);
        checkBoxAutoPick.Margin = new Padding(4, 5, 4, 5);
        checkBoxAutoPick.Name = "checkBoxAutoPick";
        checkBoxAutoPick.Size = new Size(192, 19);
        checkBoxAutoPick.TabIndex = 5;
        checkBoxAutoPick.Text = "Automatically pick for new files";
        checkBoxAutoPick.UseVisualStyleBackColor = true;
        // 
        // radioColumnizerPriorityHistoryThenMask
        // 
        radioColumnizerPriorityHistoryThenMask.AutoSize = true;
        radioColumnizerPriorityHistoryThenMask.Checked = true;
        radioColumnizerPriorityHistoryThenMask.Location = new Point(6, 22);
        radioColumnizerPriorityHistoryThenMask.Name = "radioColumnizerPriorityHistoryThenMask";
        radioColumnizerPriorityHistoryThenMask.Size = new Size(189, 19);
        radioColumnizerPriorityHistoryThenMask.TabIndex = 0;
        radioColumnizerPriorityHistoryThenMask.TabStop = true;
        radioColumnizerPriorityHistoryThenMask.Text = "Use history then mask (default)";
        radioColumnizerPriorityHistoryThenMask.UseVisualStyleBackColor = true;
        // 
        // radioColumnizerPriorityMaskThenHistory
        // 
        radioColumnizerPriorityMaskThenHistory.AutoSize = true;
        radioColumnizerPriorityMaskThenHistory.Location = new Point(6, 48);
        radioColumnizerPriorityMaskThenHistory.Name = "radioColumnizerPriorityMaskThenHistory";
        radioColumnizerPriorityMaskThenHistory.Size = new Size(144, 19);
        radioColumnizerPriorityMaskThenHistory.TabIndex = 1;
        radioColumnizerPriorityMaskThenHistory.Text = "Use mask, then history";
        radioColumnizerPriorityMaskThenHistory.UseVisualStyleBackColor = true;
        // 
        // radioColumnizerPriorityMaskOverridesPersistence
        // 
        radioColumnizerPriorityMaskOverridesPersistence.AutoSize = true;
        radioColumnizerPriorityMaskOverridesPersistence.Location = new Point(6, 73);
        radioColumnizerPriorityMaskOverridesPersistence.Name = "radioColumnizerPriorityMaskOverridesPersistence";
        radioColumnizerPriorityMaskOverridesPersistence.Size = new Size(227, 19);
        radioColumnizerPriorityMaskOverridesPersistence.TabIndex = 2;
        radioColumnizerPriorityMaskOverridesPersistence.Text = "Use mask, override per-file persistence";
        radioColumnizerPriorityMaskOverridesPersistence.UseVisualStyleBackColor = true;
        // 
        // buttonDelete
        // 
        buttonDelete.Location = new Point(812, 395);
        buttonDelete.Margin = new Padding(4, 5, 4, 5);
        buttonDelete.Name = "buttonDelete";
        buttonDelete.Size = new Size(112, 35);
        buttonDelete.TabIndex = 3;
        buttonDelete.Text = "Delete";
        buttonDelete.UseVisualStyleBackColor = true;
        buttonDelete.Click += OnBtnDeleteClick;
        // 
        // dataGridViewColumnizer
        // 
        dataGridViewColumnizer.AllowUserToResizeRows = false;
        dataGridViewColumnizer.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dataGridViewColumnizer.BackgroundColor = SystemColors.ControlLight;
        dataGridViewColumnizer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridViewColumnizer.Columns.AddRange(new DataGridViewColumn[] { dataGridViewImageColumnColumnizerStale, dataGridViewTextBoxColumnFileMask, dataGridViewComboBoxColumnColumnizerMaskType, dataGridViewComboBoxColumnColumnizer });
        dataGridViewColumnizer.Dock = DockStyle.Top;
        dataGridViewColumnizer.EditMode = DataGridViewEditMode.EditOnEnter;
        dataGridViewColumnizer.Location = new Point(4, 5);
        dataGridViewColumnizer.Margin = new Padding(4, 5, 4, 5);
        dataGridViewColumnizer.Name = "dataGridViewColumnizer";
        dataGridViewColumnizer.RowHeadersWidth = 62;
        dataGridViewColumnizer.Size = new Size(934, 326);
        dataGridViewColumnizer.TabIndex = 2;
        dataGridViewColumnizer.DefaultValuesNeeded += OnDataGridViewColumnizerDefaultValuesNeeded;
        dataGridViewColumnizer.CurrentCellDirtyStateChanged += OnDataGridViewColumnizerCurrentCellDirtyStateChanged;
        // 
        // tabPageHighlightMask
        // 
        tabPageHighlightMask.Controls.Add(dataGridViewHighlightMask);
        tabPageHighlightMask.Location = new Point(4, 24);
        tabPageHighlightMask.Margin = new Padding(4, 5, 4, 5);
        tabPageHighlightMask.Name = "tabPageHighlightMask";
        tabPageHighlightMask.Padding = new Padding(4, 5, 4, 5);
        tabPageHighlightMask.Size = new Size(942, 440);
        tabPageHighlightMask.TabIndex = 8;
        tabPageHighlightMask.Text = "Highlight";
        tabPageHighlightMask.UseVisualStyleBackColor = true;
        // 
        // dataGridViewHighlightMask
        // 
        dataGridViewHighlightMask.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dataGridViewHighlightMask.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridViewHighlightMask.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumnFileName, dataGridViewComboBoxColumnHighlightGroup });
        dataGridViewHighlightMask.Dock = DockStyle.Fill;
        dataGridViewHighlightMask.Location = new Point(4, 5);
        dataGridViewHighlightMask.Margin = new Padding(4, 5, 4, 5);
        dataGridViewHighlightMask.Name = "dataGridViewHighlightMask";
        dataGridViewHighlightMask.RowHeadersWidth = 62;
        dataGridViewHighlightMask.Size = new Size(934, 430);
        dataGridViewHighlightMask.TabIndex = 0;
        // 
        // dataGridViewTextBoxColumnFileName
        // 
        dataGridViewTextBoxColumnFileName.HeaderText = "File name mask (RegEx)";
        dataGridViewTextBoxColumnFileName.MinimumWidth = 40;
        dataGridViewTextBoxColumnFileName.Name = "dataGridViewTextBoxColumnFileName";
        // 
        // dataGridViewComboBoxColumnHighlightGroup
        // 
        dataGridViewComboBoxColumnHighlightGroup.HeaderText = "Highlight group";
        dataGridViewComboBoxColumnHighlightGroup.MinimumWidth = 50;
        dataGridViewComboBoxColumnHighlightGroup.Name = "dataGridViewComboBoxColumnHighlightGroup";
        // 
        // tabPageControlCharacters
        // 
        tabPageControlCharacters.Controls.Add(checkBoxControlCharsEnable);
        tabPageControlCharacters.Controls.Add(labelControlCharsWarning);
        tabPageControlCharacters.Controls.Add(labelControlCharsHint);
        tabPageControlCharacters.Controls.Add(groupBoxControlCharsStyle);
        tabPageControlCharacters.Controls.Add(buttonControlCharsPresetAll);
        tabPageControlCharacters.Controls.Add(buttonControlCharsPresetNone);
        tabPageControlCharacters.Controls.Add(buttonControlCharsPresetNonWhitespace);
        tabPageControlCharacters.Controls.Add(dataGridViewControlChars);
        tabPageControlCharacters.Controls.Add(groupBoxControlCharsAppearance);
        tabPageControlCharacters.Controls.Add(groupBoxControlCharsClipboard);
        tabPageControlCharacters.Location = new Point(4, 24);
        tabPageControlCharacters.Margin = new Padding(4, 5, 4, 5);
        tabPageControlCharacters.Name = "tabPageControlCharacters";
        tabPageControlCharacters.Padding = new Padding(8);
        tabPageControlCharacters.Size = new Size(942, 440);
        tabPageControlCharacters.TabIndex = 9;
        tabPageControlCharacters.Text = "Control characters";
        tabPageControlCharacters.UseVisualStyleBackColor = true;
        // 
        // checkBoxControlCharsEnable
        // 
        checkBoxControlCharsEnable.AutoSize = true;
        checkBoxControlCharsEnable.Location = new Point(12, 12);
        checkBoxControlCharsEnable.Name = "checkBoxControlCharsEnable";
        checkBoxControlCharsEnable.Size = new Size(230, 19);
        checkBoxControlCharsEnable.TabIndex = 0;
        checkBoxControlCharsEnable.Tag = "";
        checkBoxControlCharsEnable.Text = "Substitute control characters in display";
        checkBoxControlCharsEnable.UseVisualStyleBackColor = true;
        checkBoxControlCharsEnable.CheckedChanged += OnControlCharsEnableChanged;
        // 
        // labelControlCharsWarning
        // 
        labelControlCharsWarning.Location = new Point(12, 38);
        labelControlCharsWarning.Name = "labelControlCharsWarning";
        labelControlCharsWarning.Size = new Size(900, 36);
        labelControlCharsWarning.TabIndex = 1;
        labelControlCharsWarning.Text = "Display only — does not modify file contents or affect search, filter, or copy-to-clipboard (unless the option below is enabled). Enabling this can make some log files look unusual.";
        // 
        // labelControlCharsHint
        // 
        labelControlCharsHint.AutoSize = true;
        labelControlCharsHint.ForeColor = SystemColors.GrayText;
        labelControlCharsHint.Location = new Point(12, 78);
        labelControlCharsHint.Name = "labelControlCharsHint";
        labelControlCharsHint.Size = new Size(192, 15);
        labelControlCharsHint.TabIndex = 2;
        labelControlCharsHint.Text = "Enable substitution above to apply.";
        // 
        // groupBoxControlCharsStyle
        // 
        groupBoxControlCharsStyle.Controls.Add(radioButtonControlCharStyleControlPictures);
        groupBoxControlCharsStyle.Controls.Add(radioButtonControlCharStyleCaret);
        groupBoxControlCharsStyle.Controls.Add(radioButtonControlCharStyleCEscape);
        groupBoxControlCharsStyle.Controls.Add(radioButtonControlCharStyleAbbreviation);
        groupBoxControlCharsStyle.Controls.Add(radioButtonControlCharStyleIso2047);
        groupBoxControlCharsStyle.Location = new Point(12, 102);
        groupBoxControlCharsStyle.Name = "groupBoxControlCharsStyle";
        groupBoxControlCharsStyle.Size = new Size(360, 160);
        groupBoxControlCharsStyle.TabIndex = 3;
        groupBoxControlCharsStyle.TabStop = false;
        groupBoxControlCharsStyle.Text = "Display style";
        // 
        // radioButtonControlCharStyleControlPictures
        // 
        radioButtonControlCharStyleControlPictures.AutoSize = true;
        radioButtonControlCharStyleControlPictures.Location = new Point(12, 22);
        radioButtonControlCharStyleControlPictures.Name = "radioButtonControlCharStyleControlPictures";
        radioButtonControlCharStyleControlPictures.Size = new Size(205, 19);
        radioButtonControlCharStyleControlPictures.TabIndex = 0;
        radioButtonControlCharStyleControlPictures.Text = "Unicode Control Pictures (default)";
        radioButtonControlCharStyleControlPictures.UseVisualStyleBackColor = true;
        radioButtonControlCharStyleControlPictures.CheckedChanged += OnControlCharStyleChanged;
        // 
        // radioButtonControlCharStyleCaret
        // 
        radioButtonControlCharStyleCaret.AutoSize = true;
        radioButtonControlCharStyleCaret.Location = new Point(12, 48);
        radioButtonControlCharStyleCaret.Name = "radioButtonControlCharStyleCaret";
        radioButtonControlCharStyleCaret.Size = new Size(127, 19);
        radioButtonControlCharStyleCaret.TabIndex = 1;
        radioButtonControlCharStyleCaret.Text = "Caret notation (^X)";
        radioButtonControlCharStyleCaret.UseVisualStyleBackColor = true;
        radioButtonControlCharStyleCaret.CheckedChanged += OnControlCharStyleChanged;
        // 
        // radioButtonControlCharStyleCEscape
        // 
        radioButtonControlCharStyleCEscape.AutoSize = true;
        radioButtonControlCharStyleCEscape.Location = new Point(12, 74);
        radioButtonControlCharStyleCEscape.Name = "radioButtonControlCharStyleCEscape";
        radioButtonControlCharStyleCEscape.Size = new Size(158, 19);
        radioButtonControlCharStyleCEscape.TabIndex = 2;
        radioButtonControlCharStyleCEscape.Text = "C escape sequence (\\x01)";
        radioButtonControlCharStyleCEscape.UseVisualStyleBackColor = true;
        radioButtonControlCharStyleCEscape.CheckedChanged += OnControlCharStyleChanged;
        // 
        // radioButtonControlCharStyleAbbreviation
        // 
        radioButtonControlCharStyleAbbreviation.AutoSize = true;
        radioButtonControlCharStyleAbbreviation.Location = new Point(12, 100);
        radioButtonControlCharStyleAbbreviation.Name = "radioButtonControlCharStyleAbbreviation";
        radioButtonControlCharStyleAbbreviation.Size = new Size(144, 19);
        radioButtonControlCharStyleAbbreviation.TabIndex = 3;
        radioButtonControlCharStyleAbbreviation.Text = "Abbreviation (<SOH>)";
        radioButtonControlCharStyleAbbreviation.UseVisualStyleBackColor = true;
        radioButtonControlCharStyleAbbreviation.CheckedChanged += OnControlCharStyleChanged;
        // 
        // radioButtonControlCharStyleIso2047
        // 
        radioButtonControlCharStyleIso2047.AutoSize = true;
        radioButtonControlCharStyleIso2047.Location = new Point(12, 126);
        radioButtonControlCharStyleIso2047.Name = "radioButtonControlCharStyleIso2047";
        radioButtonControlCharStyleIso2047.Size = new Size(118, 19);
        radioButtonControlCharStyleIso2047.TabIndex = 4;
        radioButtonControlCharStyleIso2047.Text = "ISO 2047 graphics";
        radioButtonControlCharStyleIso2047.UseVisualStyleBackColor = true;
        radioButtonControlCharStyleIso2047.CheckedChanged += OnControlCharStyleChanged;
        // 
        // buttonControlCharsPresetAll
        // 
        buttonControlCharsPresetAll.Location = new Point(384, 109);
        buttonControlCharsPresetAll.Name = "buttonControlCharsPresetAll";
        buttonControlCharsPresetAll.Size = new Size(188, 28);
        buttonControlCharsPresetAll.TabIndex = 4;
        buttonControlCharsPresetAll.Text = "Enable all";
        buttonControlCharsPresetAll.UseVisualStyleBackColor = true;
        buttonControlCharsPresetAll.Click += OnControlCharsPresetAllClick;
        // 
        // buttonControlCharsPresetNone
        // 
        buttonControlCharsPresetNone.Location = new Point(384, 143);
        buttonControlCharsPresetNone.Name = "buttonControlCharsPresetNone";
        buttonControlCharsPresetNone.Size = new Size(188, 28);
        buttonControlCharsPresetNone.TabIndex = 5;
        buttonControlCharsPresetNone.Text = "Disable all";
        buttonControlCharsPresetNone.UseVisualStyleBackColor = true;
        buttonControlCharsPresetNone.Click += OnControlCharsPresetNoneClick;
        // 
        // buttonControlCharsPresetNonWhitespace
        // 
        buttonControlCharsPresetNonWhitespace.Location = new Point(384, 177);
        buttonControlCharsPresetNonWhitespace.Name = "buttonControlCharsPresetNonWhitespace";
        buttonControlCharsPresetNonWhitespace.Size = new Size(188, 28);
        buttonControlCharsPresetNonWhitespace.TabIndex = 6;
        buttonControlCharsPresetNonWhitespace.Text = "Non-whitespace defaults";
        buttonControlCharsPresetNonWhitespace.UseVisualStyleBackColor = true;
        buttonControlCharsPresetNonWhitespace.Click += OnControlCharsPresetNonWhitespaceClick;
        // 
        // dataGridViewControlChars
        // 
        dataGridViewControlChars.AllowUserToAddRows = false;
        dataGridViewControlChars.AllowUserToDeleteRows = false;
        dataGridViewControlChars.AllowUserToResizeRows = false;
        dataGridViewControlChars.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridViewControlChars.Columns.AddRange(new DataGridViewColumn[] { columnControlCharEnabled, columnControlCharHex, columnControlCharAbbr, columnControlCharCaret, columnControlCharPreview });
        dataGridViewControlChars.EditMode = DataGridViewEditMode.EditOnEnter;
        dataGridViewControlChars.Location = new Point(12, 268);
        dataGridViewControlChars.MultiSelect = false;
        dataGridViewControlChars.Name = "dataGridViewControlChars";
        dataGridViewControlChars.RowHeadersVisible = false;
        dataGridViewControlChars.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToDisplayedHeaders;
        dataGridViewControlChars.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridViewControlChars.Size = new Size(560, 161);
        dataGridViewControlChars.TabIndex = 7;
        dataGridViewControlChars.CellValueChanged += OnControlCharsGridCellValueChanged;
        dataGridViewControlChars.CurrentCellDirtyStateChanged += OnControlCharsGridCurrentCellDirtyStateChanged;
        // 
        // columnControlCharEnabled
        // 
        columnControlCharEnabled.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
        columnControlCharEnabled.HeaderText = "On";
        columnControlCharEnabled.MinimumWidth = 30;
        columnControlCharEnabled.Name = "columnControlCharEnabled";
        columnControlCharEnabled.Resizable = DataGridViewTriState.False;
        columnControlCharEnabled.Width = 30;
        // 
        // columnControlCharHex
        // 
        columnControlCharHex.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        columnControlCharHex.HeaderText = "Hex";
        columnControlCharHex.Name = "columnControlCharHex";
        columnControlCharHex.ReadOnly = true;
        // 
        // columnControlCharAbbr
        // 
        columnControlCharAbbr.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        columnControlCharAbbr.HeaderText = "Abbr";
        columnControlCharAbbr.Name = "columnControlCharAbbr";
        columnControlCharAbbr.ReadOnly = true;
        // 
        // columnControlCharCaret
        // 
        columnControlCharCaret.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        columnControlCharCaret.HeaderText = "Caret";
        columnControlCharCaret.Name = "columnControlCharCaret";
        columnControlCharCaret.ReadOnly = true;
        // 
        // columnControlCharPreview
        // 
        columnControlCharPreview.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        columnControlCharPreview.HeaderText = "Preview";
        columnControlCharPreview.Name = "columnControlCharPreview";
        columnControlCharPreview.ReadOnly = true;
        // 
        // groupBoxControlCharsAppearance
        // 
        groupBoxControlCharsAppearance.Controls.Add(labelControlCharsForeColor);
        groupBoxControlCharsAppearance.Controls.Add(buttonControlCharsForeColor);
        groupBoxControlCharsAppearance.Controls.Add(labelControlCharsBackColor);
        groupBoxControlCharsAppearance.Controls.Add(buttonControlCharsBackColor);
        groupBoxControlCharsAppearance.Controls.Add(buttonControlCharsBackColorClear);
        groupBoxControlCharsAppearance.Controls.Add(checkBoxControlCharsBold);
        groupBoxControlCharsAppearance.Controls.Add(checkBoxControlCharsItalic);
        groupBoxControlCharsAppearance.Controls.Add(labelControlCharsSample);
        groupBoxControlCharsAppearance.Location = new Point(588, 102);
        groupBoxControlCharsAppearance.Name = "groupBoxControlCharsAppearance";
        groupBoxControlCharsAppearance.Size = new Size(340, 220);
        groupBoxControlCharsAppearance.TabIndex = 8;
        groupBoxControlCharsAppearance.TabStop = false;
        groupBoxControlCharsAppearance.Text = "Appearance";
        // 
        // labelControlCharsForeColor
        // 
        labelControlCharsForeColor.AutoSize = true;
        labelControlCharsForeColor.Location = new Point(12, 28);
        labelControlCharsForeColor.Name = "labelControlCharsForeColor";
        labelControlCharsForeColor.Size = new Size(109, 15);
        labelControlCharsForeColor.TabIndex = 0;
        labelControlCharsForeColor.Text = "Foreground colour:";
        // 
        // buttonControlCharsForeColor
        // 
        buttonControlCharsForeColor.Location = new Point(150, 24);
        buttonControlCharsForeColor.Name = "buttonControlCharsForeColor";
        buttonControlCharsForeColor.Size = new Size(40, 24);
        buttonControlCharsForeColor.TabIndex = 1;
        buttonControlCharsForeColor.UseVisualStyleBackColor = false;
        buttonControlCharsForeColor.Click += OnControlCharsForeColorClick;
        // 
        // labelControlCharsBackColor
        // 
        labelControlCharsBackColor.AutoSize = true;
        labelControlCharsBackColor.Location = new Point(12, 60);
        labelControlCharsBackColor.Name = "labelControlCharsBackColor";
        labelControlCharsBackColor.Size = new Size(111, 15);
        labelControlCharsBackColor.TabIndex = 2;
        labelControlCharsBackColor.Text = "Background colour:";
        // 
        // buttonControlCharsBackColor
        // 
        buttonControlCharsBackColor.Location = new Point(150, 56);
        buttonControlCharsBackColor.Name = "buttonControlCharsBackColor";
        buttonControlCharsBackColor.Size = new Size(40, 24);
        buttonControlCharsBackColor.TabIndex = 3;
        buttonControlCharsBackColor.UseVisualStyleBackColor = false;
        buttonControlCharsBackColor.Click += OnControlCharsBackColorClick;
        // 
        // buttonControlCharsBackColorClear
        // 
        buttonControlCharsBackColorClear.Location = new Point(196, 56);
        buttonControlCharsBackColorClear.Name = "buttonControlCharsBackColorClear";
        buttonControlCharsBackColorClear.Size = new Size(90, 24);
        buttonControlCharsBackColorClear.TabIndex = 4;
        buttonControlCharsBackColorClear.Text = "(no color)";
        buttonControlCharsBackColorClear.UseVisualStyleBackColor = true;
        buttonControlCharsBackColorClear.Click += OnControlCharsBackColorClearClick;
        // 
        // checkBoxControlCharsBold
        // 
        checkBoxControlCharsBold.AutoSize = true;
        checkBoxControlCharsBold.Location = new Point(12, 96);
        checkBoxControlCharsBold.Name = "checkBoxControlCharsBold";
        checkBoxControlCharsBold.Size = new Size(50, 19);
        checkBoxControlCharsBold.TabIndex = 5;
        checkBoxControlCharsBold.Text = "Bold";
        checkBoxControlCharsBold.UseVisualStyleBackColor = true;
        checkBoxControlCharsBold.CheckedChanged += OnControlCharsBoldChanged;
        // 
        // checkBoxControlCharsItalic
        // 
        checkBoxControlCharsItalic.AutoSize = true;
        checkBoxControlCharsItalic.Location = new Point(100, 96);
        checkBoxControlCharsItalic.Name = "checkBoxControlCharsItalic";
        checkBoxControlCharsItalic.Size = new Size(51, 19);
        checkBoxControlCharsItalic.TabIndex = 6;
        checkBoxControlCharsItalic.Text = "Italic";
        checkBoxControlCharsItalic.UseVisualStyleBackColor = true;
        checkBoxControlCharsItalic.CheckedChanged += OnControlCharsItalicChanged;
        // 
        // labelControlCharsSample
        // 
        labelControlCharsSample.BorderStyle = BorderStyle.FixedSingle;
        labelControlCharsSample.Font = new Font("Courier New", 12F);
        labelControlCharsSample.Location = new Point(12, 132);
        labelControlCharsSample.Name = "labelControlCharsSample";
        labelControlCharsSample.Size = new Size(316, 64);
        labelControlCharsSample.TabIndex = 7;
        labelControlCharsSample.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // groupBoxControlCharsClipboard
        // 
        groupBoxControlCharsClipboard.Controls.Add(checkBoxControlCharsCopyDisplayedForm);
        groupBoxControlCharsClipboard.Location = new Point(588, 332);
        groupBoxControlCharsClipboard.Name = "groupBoxControlCharsClipboard";
        groupBoxControlCharsClipboard.Size = new Size(340, 60);
        groupBoxControlCharsClipboard.TabIndex = 9;
        groupBoxControlCharsClipboard.TabStop = false;
        groupBoxControlCharsClipboard.Text = "Clipboard";
        // 
        // checkBoxControlCharsCopyDisplayedForm
        // 
        checkBoxControlCharsCopyDisplayedForm.AutoSize = true;
        checkBoxControlCharsCopyDisplayedForm.Location = new Point(12, 24);
        checkBoxControlCharsCopyDisplayedForm.Name = "checkBoxControlCharsCopyDisplayedForm";
        checkBoxControlCharsCopyDisplayedForm.Size = new Size(15, 14);
        checkBoxControlCharsCopyDisplayedForm.TabIndex = 0;
        checkBoxControlCharsCopyDisplayedForm.UseVisualStyleBackColor = true;
        // 
        // tabPageMultiFile
        // 
        tabPageMultiFile.Controls.Add(groupBoxDefaultFileNamePattern);
        tabPageMultiFile.Controls.Add(labelHintMultiFile);
        tabPageMultiFile.Controls.Add(labelNoteMultiFile);
        tabPageMultiFile.Controls.Add(groupBoxWhenOpeningMultiFile);
        tabPageMultiFile.Location = new Point(4, 24);
        tabPageMultiFile.Margin = new Padding(4, 5, 4, 5);
        tabPageMultiFile.Name = "tabPageMultiFile";
        tabPageMultiFile.Padding = new Padding(4, 5, 4, 5);
        tabPageMultiFile.Size = new Size(942, 440);
        tabPageMultiFile.TabIndex = 4;
        tabPageMultiFile.Text = "MultiFile";
        tabPageMultiFile.UseVisualStyleBackColor = true;
        // 
        // groupBoxDefaultFileNamePattern
        // 
        groupBoxDefaultFileNamePattern.Controls.Add(labelMaxDays);
        groupBoxDefaultFileNamePattern.Controls.Add(labelPattern);
        groupBoxDefaultFileNamePattern.Controls.Add(upDownMultifileDays);
        groupBoxDefaultFileNamePattern.Controls.Add(textBoxMultifilePattern);
        groupBoxDefaultFileNamePattern.Location = new Point(364, 28);
        groupBoxDefaultFileNamePattern.Margin = new Padding(4, 5, 4, 5);
        groupBoxDefaultFileNamePattern.Name = "groupBoxDefaultFileNamePattern";
        groupBoxDefaultFileNamePattern.Padding = new Padding(4, 5, 4, 5);
        groupBoxDefaultFileNamePattern.Size = new Size(436, 154);
        groupBoxDefaultFileNamePattern.TabIndex = 3;
        groupBoxDefaultFileNamePattern.TabStop = false;
        groupBoxDefaultFileNamePattern.Text = "Default filename pattern";
        // 
        // labelMaxDays
        // 
        labelMaxDays.AutoSize = true;
        labelMaxDays.Location = new Point(10, 75);
        labelMaxDays.Margin = new Padding(4, 0, 4, 0);
        labelMaxDays.Name = "labelMaxDays";
        labelMaxDays.Size = new Size(59, 15);
        labelMaxDays.TabIndex = 3;
        labelMaxDays.Text = "Max days:";
        // 
        // labelPattern
        // 
        labelPattern.AutoSize = true;
        labelPattern.Location = new Point(10, 37);
        labelPattern.Margin = new Padding(4, 0, 4, 0);
        labelPattern.Name = "labelPattern";
        labelPattern.Size = new Size(48, 15);
        labelPattern.TabIndex = 2;
        labelPattern.Text = "Pattern:";
        // 
        // upDownMultifileDays
        // 
        upDownMultifileDays.Location = new Point(102, 72);
        upDownMultifileDays.Margin = new Padding(4, 5, 4, 5);
        upDownMultifileDays.Maximum = new decimal(new int[] { 40, 0, 0, 0 });
        upDownMultifileDays.Name = "upDownMultifileDays";
        helpProvider.SetShowHelp(upDownMultifileDays, false);
        upDownMultifileDays.Size = new Size(92, 23);
        upDownMultifileDays.TabIndex = 1;
        upDownMultifileDays.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // textBoxMultifilePattern
        // 
        textBoxMultifilePattern.Location = new Point(102, 32);
        textBoxMultifilePattern.Margin = new Padding(4, 5, 4, 5);
        textBoxMultifilePattern.Name = "textBoxMultifilePattern";
        textBoxMultifilePattern.Size = new Size(278, 23);
        textBoxMultifilePattern.TabIndex = 0;
        textBoxMultifilePattern.TextChanged += OnMultiFilePatternTextChanged;
        // 
        // labelHintMultiFile
        // 
        labelHintMultiFile.Location = new Point(6, 203);
        labelHintMultiFile.Margin = new Padding(4, 0, 4, 0);
        labelHintMultiFile.Name = "labelHintMultiFile";
        labelHintMultiFile.Size = new Size(304, 111);
        labelHintMultiFile.TabIndex = 2;
        labelHintMultiFile.Text = "Hint: Pressing the Shift key while dropping files onto LogExpert will switch the behaviour from single to multi and vice versa.";
        // 
        // labelNoteMultiFile
        // 
        labelNoteMultiFile.Location = new Point(6, 314);
        labelNoteMultiFile.Margin = new Padding(4, 0, 4, 0);
        labelNoteMultiFile.Name = "labelNoteMultiFile";
        labelNoteMultiFile.Size = new Size(705, 82);
        labelNoteMultiFile.TabIndex = 1;
        labelNoteMultiFile.Text = resources.GetString("labelNoteMultiFile.Text");
        // 
        // groupBoxWhenOpeningMultiFile
        // 
        groupBoxWhenOpeningMultiFile.Controls.Add(radioButtonAskWhatToDo);
        groupBoxWhenOpeningMultiFile.Controls.Add(radioButtonTreatAllFilesAsOneMultifile);
        groupBoxWhenOpeningMultiFile.Controls.Add(radioButtonLoadEveryFileIntoSeperatedTab);
        groupBoxWhenOpeningMultiFile.Location = new Point(10, 28);
        groupBoxWhenOpeningMultiFile.Margin = new Padding(4, 5, 4, 5);
        groupBoxWhenOpeningMultiFile.Name = "groupBoxWhenOpeningMultiFile";
        groupBoxWhenOpeningMultiFile.Padding = new Padding(4, 5, 4, 5);
        groupBoxWhenOpeningMultiFile.Size = new Size(300, 154);
        groupBoxWhenOpeningMultiFile.TabIndex = 0;
        groupBoxWhenOpeningMultiFile.TabStop = false;
        groupBoxWhenOpeningMultiFile.Text = "When opening multiple files...";
        // 
        // radioButtonAskWhatToDo
        // 
        radioButtonAskWhatToDo.AutoSize = true;
        radioButtonAskWhatToDo.Location = new Point(10, 105);
        radioButtonAskWhatToDo.Margin = new Padding(4, 5, 4, 5);
        radioButtonAskWhatToDo.Name = "radioButtonAskWhatToDo";
        radioButtonAskWhatToDo.Size = new Size(104, 19);
        radioButtonAskWhatToDo.TabIndex = 2;
        radioButtonAskWhatToDo.TabStop = true;
        radioButtonAskWhatToDo.Text = "Ask what to do";
        radioButtonAskWhatToDo.UseVisualStyleBackColor = true;
        // 
        // radioButtonTreatAllFilesAsOneMultifile
        // 
        radioButtonTreatAllFilesAsOneMultifile.AutoSize = true;
        radioButtonTreatAllFilesAsOneMultifile.Location = new Point(10, 68);
        radioButtonTreatAllFilesAsOneMultifile.Margin = new Padding(4, 5, 4, 5);
        radioButtonTreatAllFilesAsOneMultifile.Name = "radioButtonTreatAllFilesAsOneMultifile";
        radioButtonTreatAllFilesAsOneMultifile.Size = new Size(182, 19);
        radioButtonTreatAllFilesAsOneMultifile.TabIndex = 1;
        radioButtonTreatAllFilesAsOneMultifile.TabStop = true;
        radioButtonTreatAllFilesAsOneMultifile.Text = "Treat all files as one 'MultiFile'";
        radioButtonTreatAllFilesAsOneMultifile.UseVisualStyleBackColor = true;
        // 
        // radioButtonLoadEveryFileIntoSeperatedTab
        // 
        radioButtonLoadEveryFileIntoSeperatedTab.AutoSize = true;
        radioButtonLoadEveryFileIntoSeperatedTab.Location = new Point(10, 31);
        radioButtonLoadEveryFileIntoSeperatedTab.Margin = new Padding(4, 5, 4, 5);
        radioButtonLoadEveryFileIntoSeperatedTab.Name = "radioButtonLoadEveryFileIntoSeperatedTab";
        radioButtonLoadEveryFileIntoSeperatedTab.Size = new Size(201, 19);
        radioButtonLoadEveryFileIntoSeperatedTab.TabIndex = 0;
        radioButtonLoadEveryFileIntoSeperatedTab.TabStop = true;
        radioButtonLoadEveryFileIntoSeperatedTab.Text = "Load every file into a separate tab";
        radioButtonLoadEveryFileIntoSeperatedTab.UseVisualStyleBackColor = true;
        // 
        // tabPagePlugins
        // 
        tabPagePlugins.Controls.Add(groupBoxPlugins);
        tabPagePlugins.Controls.Add(groupBoxSettings);
        tabPagePlugins.Location = new Point(4, 24);
        tabPagePlugins.Margin = new Padding(4, 5, 4, 5);
        tabPagePlugins.Name = "tabPagePlugins";
        tabPagePlugins.Padding = new Padding(4, 5, 4, 5);
        tabPagePlugins.Size = new Size(942, 440);
        tabPagePlugins.TabIndex = 5;
        tabPagePlugins.Text = "Plugins";
        tabPagePlugins.UseVisualStyleBackColor = true;
        // 
        // groupBoxPlugins
        // 
        groupBoxPlugins.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        groupBoxPlugins.Controls.Add(listBoxPlugin);
        groupBoxPlugins.Location = new Point(10, 23);
        groupBoxPlugins.Margin = new Padding(4, 5, 4, 5);
        groupBoxPlugins.Name = "groupBoxPlugins";
        groupBoxPlugins.Padding = new Padding(4, 5, 4, 5);
        groupBoxPlugins.Size = new Size(342, 400);
        groupBoxPlugins.TabIndex = 3;
        groupBoxPlugins.TabStop = false;
        groupBoxPlugins.Text = "Plugins";
        // 
        // listBoxPlugin
        // 
        listBoxPlugin.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        listBoxPlugin.DisplayMember = "Text";
        listBoxPlugin.FormattingEnabled = true;
        listBoxPlugin.Location = new Point(9, 29);
        listBoxPlugin.Margin = new Padding(4, 5, 4, 5);
        listBoxPlugin.Name = "listBoxPlugin";
        listBoxPlugin.Size = new Size(322, 349);
        listBoxPlugin.TabIndex = 0;
        listBoxPlugin.ValueMember = "Text";
        listBoxPlugin.SelectedIndexChanged += OnListBoxPluginSelectedIndexChanged;
        // 
        // groupBoxSettings
        // 
        groupBoxSettings.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        groupBoxSettings.Controls.Add(panelPlugin);
        groupBoxSettings.Location = new Point(362, 23);
        groupBoxSettings.Margin = new Padding(4, 5, 4, 5);
        groupBoxSettings.Name = "groupBoxSettings";
        groupBoxSettings.Padding = new Padding(4, 5, 4, 5);
        groupBoxSettings.Size = new Size(567, 400);
        groupBoxSettings.TabIndex = 2;
        groupBoxSettings.TabStop = false;
        groupBoxSettings.Text = "Settings";
        // 
        // panelPlugin
        // 
        panelPlugin.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        panelPlugin.AutoScroll = true;
        panelPlugin.Controls.Add(buttonConfigPlugin);
        panelPlugin.Location = new Point(9, 29);
        panelPlugin.Margin = new Padding(4, 5, 4, 5);
        panelPlugin.Name = "panelPlugin";
        panelPlugin.Size = new Size(549, 362);
        panelPlugin.TabIndex = 1;
        // 
        // buttonConfigPlugin
        // 
        buttonConfigPlugin.Location = new Point(164, 163);
        buttonConfigPlugin.Margin = new Padding(4, 5, 4, 5);
        buttonConfigPlugin.Name = "buttonConfigPlugin";
        buttonConfigPlugin.Size = new Size(170, 35);
        buttonConfigPlugin.TabIndex = 0;
        buttonConfigPlugin.Text = "Configure...";
        buttonConfigPlugin.UseVisualStyleBackColor = true;
        buttonConfigPlugin.Click += OnBtnConfigPluginClick;
        // 
        // tabPageSessions
        // 
        tabPageSessions.Controls.Add(checkBoxPortableMode);
        tabPageSessions.Controls.Add(checkBoxSaveFilter);
        tabPageSessions.Controls.Add(groupBoxPersistantFileLocation);
        tabPageSessions.Controls.Add(checkBoxSaveSessions);
        tabPageSessions.Location = new Point(4, 24);
        tabPageSessions.Margin = new Padding(4, 5, 4, 5);
        tabPageSessions.Name = "tabPageSessions";
        tabPageSessions.Padding = new Padding(4, 5, 4, 5);
        tabPageSessions.Size = new Size(942, 440);
        tabPageSessions.TabIndex = 6;
        tabPageSessions.Text = "Persistence";
        tabPageSessions.UseVisualStyleBackColor = true;
        // 
        // checkBoxPortableMode
        // 
        checkBoxPortableMode.AutoSize = true;
        checkBoxPortableMode.Location = new Point(35, 110);
        checkBoxPortableMode.Margin = new Padding(4, 5, 4, 5);
        checkBoxPortableMode.Name = "checkBoxPortableMode";
        checkBoxPortableMode.Size = new Size(150, 19);
        checkBoxPortableMode.TabIndex = 3;
        checkBoxPortableMode.Text = "Activate Portable Mode";
        toolTip.SetToolTip(checkBoxPortableMode, "If this mode is activated, the save file will be loaded from the Executable Location");
        checkBoxPortableMode.UseVisualStyleBackColor = true;
        checkBoxPortableMode.CheckedChanged += OnPortableModeCheckedChanged;
        // 
        // checkBoxSaveFilter
        // 
        checkBoxSaveFilter.AutoSize = true;
        checkBoxSaveFilter.Location = new Point(35, 75);
        checkBoxSaveFilter.Margin = new Padding(4, 5, 4, 5);
        checkBoxSaveFilter.Name = "checkBoxSaveFilter";
        checkBoxSaveFilter.Size = new Size(217, 19);
        checkBoxSaveFilter.TabIndex = 2;
        checkBoxSaveFilter.Text = " Save and restore filter and filter tabs";
        checkBoxSaveFilter.UseVisualStyleBackColor = true;
        // 
        // groupBoxPersistantFileLocation
        // 
        groupBoxPersistantFileLocation.Controls.Add(labelSessionSaveOwnDir);
        groupBoxPersistantFileLocation.Controls.Add(buttonSessionSaveDir);
        groupBoxPersistantFileLocation.Controls.Add(radioButtonSessionSaveOwn);
        groupBoxPersistantFileLocation.Controls.Add(radioButtonsessionSaveDocuments);
        groupBoxPersistantFileLocation.Controls.Add(radioButtonSessionSameDir);
        groupBoxPersistantFileLocation.Controls.Add(radioButtonSessionApplicationStartupDir);
        groupBoxPersistantFileLocation.Location = new Point(34, 145);
        groupBoxPersistantFileLocation.Margin = new Padding(4, 5, 4, 5);
        groupBoxPersistantFileLocation.Name = "groupBoxPersistantFileLocation";
        groupBoxPersistantFileLocation.Padding = new Padding(4, 5, 4, 5);
        groupBoxPersistantFileLocation.Size = new Size(411, 190);
        groupBoxPersistantFileLocation.TabIndex = 1;
        groupBoxPersistantFileLocation.TabStop = false;
        groupBoxPersistantFileLocation.Text = "Persistence file location";
        // 
        // labelSessionSaveOwnDir
        // 
        labelSessionSaveOwnDir.Location = new Point(27, 160);
        labelSessionSaveOwnDir.Margin = new Padding(4, 0, 4, 0);
        labelSessionSaveOwnDir.Name = "labelSessionSaveOwnDir";
        labelSessionSaveOwnDir.Size = new Size(252, 31);
        labelSessionSaveOwnDir.TabIndex = 4;
        labelSessionSaveOwnDir.Text = "sessionSaveOwnDirLabel";
        // 
        // buttonSessionSaveDir
        // 
        buttonSessionSaveDir.Location = new Point(358, 135);
        buttonSessionSaveDir.Margin = new Padding(4, 5, 4, 5);
        buttonSessionSaveDir.Name = "buttonSessionSaveDir";
        buttonSessionSaveDir.Size = new Size(45, 19);
        buttonSessionSaveDir.TabIndex = 3;
        buttonSessionSaveDir.Text = "...";
        buttonSessionSaveDir.UseVisualStyleBackColor = true;
        buttonSessionSaveDir.Click += OnBtnSessionSaveDirClick;
        // 
        // radioButtonSessionSaveOwn
        // 
        radioButtonSessionSaveOwn.AutoSize = true;
        radioButtonSessionSaveOwn.Location = new Point(10, 135);
        radioButtonSessionSaveOwn.Margin = new Padding(4, 5, 4, 5);
        radioButtonSessionSaveOwn.Name = "radioButtonSessionSaveOwn";
        radioButtonSessionSaveOwn.Size = new Size(100, 19);
        radioButtonSessionSaveOwn.TabIndex = 2;
        radioButtonSessionSaveOwn.TabStop = true;
        radioButtonSessionSaveOwn.Text = "Own directory";
        radioButtonSessionSaveOwn.UseVisualStyleBackColor = true;
        // 
        // radioButtonsessionSaveDocuments
        // 
        radioButtonsessionSaveDocuments.AutoSize = true;
        radioButtonsessionSaveDocuments.Location = new Point(10, 65);
        radioButtonsessionSaveDocuments.Margin = new Padding(4, 5, 4, 5);
        radioButtonsessionSaveDocuments.Name = "radioButtonsessionSaveDocuments";
        radioButtonsessionSaveDocuments.Size = new Size(160, 19);
        radioButtonsessionSaveDocuments.TabIndex = 1;
        radioButtonsessionSaveDocuments.TabStop = true;
        radioButtonsessionSaveDocuments.Text = "MyDocuments/LogExpert";
        radioButtonsessionSaveDocuments.UseVisualStyleBackColor = true;
        // 
        // radioButtonSessionSameDir
        // 
        radioButtonSessionSameDir.AutoSize = true;
        radioButtonSessionSameDir.Location = new Point(10, 30);
        radioButtonSessionSameDir.Margin = new Padding(4, 5, 4, 5);
        radioButtonSessionSameDir.Name = "radioButtonSessionSameDir";
        radioButtonSessionSameDir.Size = new Size(157, 19);
        radioButtonSessionSameDir.TabIndex = 0;
        radioButtonSessionSameDir.TabStop = true;
        radioButtonSessionSameDir.Text = "Same directory as log file";
        radioButtonSessionSameDir.UseVisualStyleBackColor = true;
        // 
        // radioButtonSessionApplicationStartupDir
        // 
        radioButtonSessionApplicationStartupDir.AutoSize = true;
        radioButtonSessionApplicationStartupDir.Location = new Point(10, 100);
        radioButtonSessionApplicationStartupDir.Margin = new Padding(4, 5, 4, 5);
        radioButtonSessionApplicationStartupDir.Name = "radioButtonSessionApplicationStartupDir";
        radioButtonSessionApplicationStartupDir.Size = new Size(176, 19);
        radioButtonSessionApplicationStartupDir.TabIndex = 5;
        radioButtonSessionApplicationStartupDir.TabStop = true;
        radioButtonSessionApplicationStartupDir.Text = "Application startup directory";
        toolTip.SetToolTip(radioButtonSessionApplicationStartupDir, "This path is based on the executable and where it has been started from.");
        radioButtonSessionApplicationStartupDir.UseVisualStyleBackColor = true;
        // 
        // checkBoxSaveSessions
        // 
        checkBoxSaveSessions.AutoSize = true;
        checkBoxSaveSessions.Location = new Point(35, 40);
        checkBoxSaveSessions.Margin = new Padding(4, 5, 4, 5);
        checkBoxSaveSessions.Name = "checkBoxSaveSessions";
        checkBoxSaveSessions.Size = new Size(241, 19);
        checkBoxSaveSessions.TabIndex = 0;
        checkBoxSaveSessions.Text = "Automatically save persistence files (.lxp)";
        checkBoxSaveSessions.UseVisualStyleBackColor = true;
        // 
        // tabPageMemory
        // 
        tabPageMemory.Controls.Add(groupBoxCPUAndStuff);
        tabPageMemory.Controls.Add(groupBoxLineBufferUsage);
        tabPageMemory.Location = new Point(4, 24);
        tabPageMemory.Margin = new Padding(4, 5, 4, 5);
        tabPageMemory.Name = "tabPageMemory";
        tabPageMemory.Padding = new Padding(4, 5, 4, 5);
        tabPageMemory.Size = new Size(942, 440);
        tabPageMemory.TabIndex = 7;
        tabPageMemory.Text = "Memory/CPU";
        tabPageMemory.UseVisualStyleBackColor = true;
        // 
        // groupBoxCPUAndStuff
        // 
        groupBoxCPUAndStuff.Controls.Add(comboBoxReaderType);
        groupBoxCPUAndStuff.Controls.Add(checkBoxMultiThread);
        groupBoxCPUAndStuff.Controls.Add(labelFilePollingInterval);
        groupBoxCPUAndStuff.Controls.Add(upDownPollingInterval);
        groupBoxCPUAndStuff.Location = new Point(408, 29);
        groupBoxCPUAndStuff.Margin = new Padding(4, 5, 4, 5);
        groupBoxCPUAndStuff.Name = "groupBoxCPUAndStuff";
        groupBoxCPUAndStuff.Padding = new Padding(4, 5, 4, 5);
        groupBoxCPUAndStuff.Size = new Size(300, 197);
        groupBoxCPUAndStuff.TabIndex = 8;
        groupBoxCPUAndStuff.TabStop = false;
        groupBoxCPUAndStuff.Text = "CPU and stuff";
        // 
        // comboBoxReaderType
        // 
        comboBoxReaderType.FormattingEnabled = true;
        comboBoxReaderType.Location = new Point(14, 76);
        comboBoxReaderType.Name = "comboBoxReaderType";
        comboBoxReaderType.Size = new Size(262, 23);
        comboBoxReaderType.TabIndex = 10;
        // 
        // checkBoxMultiThread
        // 
        checkBoxMultiThread.AutoSize = true;
        checkBoxMultiThread.Location = new Point(14, 49);
        checkBoxMultiThread.Margin = new Padding(4, 5, 4, 5);
        checkBoxMultiThread.Name = "checkBoxMultiThread";
        checkBoxMultiThread.Size = new Size(131, 19);
        checkBoxMultiThread.TabIndex = 5;
        checkBoxMultiThread.Text = "Multi threaded filter";
        checkBoxMultiThread.UseVisualStyleBackColor = true;
        // 
        // labelFilePollingInterval
        // 
        labelFilePollingInterval.AutoSize = true;
        labelFilePollingInterval.Location = new Point(14, 21);
        labelFilePollingInterval.Margin = new Padding(4, 0, 4, 0);
        labelFilePollingInterval.Name = "labelFilePollingInterval";
        labelFilePollingInterval.Size = new Size(137, 15);
        labelFilePollingInterval.TabIndex = 7;
        labelFilePollingInterval.Text = "File polling interval (ms):";
        // 
        // upDownPollingInterval
        // 
        upDownPollingInterval.Location = new Point(190, 19);
        upDownPollingInterval.Margin = new Padding(4, 5, 4, 5);
        upDownPollingInterval.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
        upDownPollingInterval.Minimum = new decimal(new int[] { 20, 0, 0, 0 });
        upDownPollingInterval.Name = "upDownPollingInterval";
        upDownPollingInterval.Size = new Size(86, 23);
        upDownPollingInterval.TabIndex = 6;
        upDownPollingInterval.Value = new decimal(new int[] { 20, 0, 0, 0 });
        // 
        // groupBoxLineBufferUsage
        // 
        groupBoxLineBufferUsage.Controls.Add(labelInfo);
        groupBoxLineBufferUsage.Controls.Add(labelNumberOfBlocks);
        groupBoxLineBufferUsage.Controls.Add(upDownLinesPerBlock);
        groupBoxLineBufferUsage.Controls.Add(upDownBlockCount);
        groupBoxLineBufferUsage.Controls.Add(labelLinesPerBlock);
        groupBoxLineBufferUsage.Location = new Point(10, 29);
        groupBoxLineBufferUsage.Margin = new Padding(4, 5, 4, 5);
        groupBoxLineBufferUsage.Name = "groupBoxLineBufferUsage";
        groupBoxLineBufferUsage.Padding = new Padding(4, 5, 4, 5);
        groupBoxLineBufferUsage.Size = new Size(326, 197);
        groupBoxLineBufferUsage.TabIndex = 4;
        groupBoxLineBufferUsage.TabStop = false;
        groupBoxLineBufferUsage.Text = "Line buffer usage";
        // 
        // labelInfo
        // 
        labelInfo.AutoSize = true;
        labelInfo.Location = new Point(9, 145);
        labelInfo.Margin = new Padding(4, 0, 4, 0);
        labelInfo.Name = "labelInfo";
        labelInfo.Size = new Size(219, 15);
        labelInfo.TabIndex = 4;
        labelInfo.Text = "Changes will take effect on next file load";
        // 
        // labelNumberOfBlocks
        // 
        labelNumberOfBlocks.AutoSize = true;
        labelNumberOfBlocks.Location = new Point(9, 52);
        labelNumberOfBlocks.Margin = new Padding(4, 0, 4, 0);
        labelNumberOfBlocks.Name = "labelNumberOfBlocks";
        labelNumberOfBlocks.Size = new Size(102, 15);
        labelNumberOfBlocks.TabIndex = 1;
        labelNumberOfBlocks.Text = "Number of blocks";
        // 
        // upDownLinesPerBlock
        // 
        upDownLinesPerBlock.Location = new Point(210, 102);
        upDownLinesPerBlock.Margin = new Padding(4, 5, 4, 5);
        upDownLinesPerBlock.Maximum = new decimal(new int[] { 5000000, 0, 0, 0 });
        upDownLinesPerBlock.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        upDownLinesPerBlock.Name = "upDownLinesPerBlock";
        upDownLinesPerBlock.Size = new Size(94, 23);
        upDownLinesPerBlock.TabIndex = 3;
        upDownLinesPerBlock.Value = new decimal(new int[] { 50000, 0, 0, 0 });
        upDownLinesPerBlock.ValueChanged += OnNumericUpDown1ValueChanged;
        // 
        // upDownBlockCount
        // 
        upDownBlockCount.Location = new Point(210, 49);
        upDownBlockCount.Margin = new Padding(4, 5, 4, 5);
        upDownBlockCount.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
        upDownBlockCount.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
        upDownBlockCount.Name = "upDownBlockCount";
        upDownBlockCount.Size = new Size(94, 23);
        upDownBlockCount.TabIndex = 0;
        upDownBlockCount.Value = new decimal(new int[] { 100, 0, 0, 0 });
        // 
        // labelLinesPerBlock
        // 
        labelLinesPerBlock.AutoSize = true;
        labelLinesPerBlock.Location = new Point(9, 105);
        labelLinesPerBlock.Margin = new Padding(4, 0, 4, 0);
        labelLinesPerBlock.Name = "labelLinesPerBlock";
        labelLinesPerBlock.Size = new Size(68, 15);
        labelLinesPerBlock.TabIndex = 2;
        labelLinesPerBlock.Text = "Lines/block";
        // 
        // buttonCancel
        // 
        buttonCancel.DialogResult = DialogResult.Cancel;
        buttonCancel.Location = new Point(818, 509);
        buttonCancel.Margin = new Padding(4, 5, 4, 5);
        buttonCancel.Name = "buttonCancel";
        buttonCancel.Size = new Size(112, 35);
        buttonCancel.TabIndex = 1;
        buttonCancel.Text = "Cancel";
        buttonCancel.UseVisualStyleBackColor = true;
        buttonCancel.Click += OnBtnCancelClick;
        // 
        // buttonOk
        // 
        buttonOk.DialogResult = DialogResult.OK;
        buttonOk.Location = new Point(696, 509);
        buttonOk.Margin = new Padding(4, 5, 4, 5);
        buttonOk.Name = "buttonOk";
        buttonOk.Size = new Size(112, 35);
        buttonOk.TabIndex = 0;
        buttonOk.Text = "OK";
        buttonOk.UseVisualStyleBackColor = true;
        buttonOk.Click += OnBtnOkClick;
        // 
        // helpProvider
        // 
        helpProvider.HelpNamespace = "LogExpert.chm";
        // 
        // buttonExport
        // 
        buttonExport.Location = new Point(20, 509);
        buttonExport.Margin = new Padding(4, 5, 4, 5);
        buttonExport.Name = "buttonExport";
        buttonExport.Size = new Size(112, 35);
        buttonExport.TabIndex = 2;
        buttonExport.Text = "Export...";
        buttonExport.UseVisualStyleBackColor = true;
        buttonExport.Click += OnBtnExportClick;
        // 
        // buttonImport
        // 
        buttonImport.Location = new Point(142, 509);
        buttonImport.Margin = new Padding(4, 5, 4, 5);
        buttonImport.Name = "buttonImport";
        buttonImport.Size = new Size(112, 35);
        buttonImport.TabIndex = 3;
        buttonImport.Text = "Import...";
        buttonImport.UseVisualStyleBackColor = true;
        buttonImport.Click += OnBtnImportClick;
        // 
        // dataGridViewImageColumnColumnizerStale
        // 
        dataGridViewImageColumnColumnizerStale.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        dataGridViewImageColumnColumnizerStale.HeaderText = "";
        dataGridViewImageColumnColumnizerStale.ImageLayout = DataGridViewImageCellLayout.Zoom;
        dataGridViewImageColumnColumnizerStale.MinimumWidth = 24;
        dataGridViewImageColumnColumnizerStale.Name = "dataGridViewImageColumnColumnizerStale";
        dataGridViewImageColumnColumnizerStale.ReadOnly = true;
        dataGridViewImageColumnColumnizerStale.Resizable = DataGridViewTriState.False;
        dataGridViewImageColumnColumnizerStale.Width = 24;
        // 
        // dataGridViewTextBoxColumnFileMask
        // 
        dataGridViewTextBoxColumnFileMask.HeaderText = "File name mask";
        dataGridViewTextBoxColumnFileMask.MinimumWidth = 40;
        dataGridViewTextBoxColumnFileMask.Name = "dataGridViewTextBoxColumnFileMask";
        // 
        // dataGridViewComboBoxColumnColumnizerMaskType
        // 
        dataGridViewComboBoxColumnColumnizerMaskType.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        dataGridViewComboBoxColumnColumnizerMaskType.HeaderText = "Type";
        dataGridViewComboBoxColumnColumnizerMaskType.MinimumWidth = 80;
        dataGridViewComboBoxColumnColumnizerMaskType.Name = "dataGridViewComboBoxColumnColumnizerMaskType";
        dataGridViewComboBoxColumnColumnizerMaskType.Width = 80;
        // 
        // dataGridViewComboBoxColumnColumnizer
        // 
        dataGridViewComboBoxColumnColumnizer.HeaderText = "Columnizer";
        dataGridViewComboBoxColumnColumnizer.MinimumWidth = 230;
        dataGridViewComboBoxColumnColumnizer.Name = "dataGridViewComboBoxColumnColumnizer";
        // 
        // SettingsDialog
        // 
        AcceptButton = buttonOk;
        CancelButton = buttonCancel;
        ClientSize = new Size(956, 563);
        Controls.Add(buttonImport);
        Controls.Add(buttonExport);
        Controls.Add(buttonOk);
        Controls.Add(buttonCancel);
        Controls.Add(tabControlSettings);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        helpProvider.SetHelpKeyword(this, "Settings.htm");
        helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
        Margin = new Padding(4, 5, 4, 5);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsDialog";
        helpProvider.SetShowHelp(this, true);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Settings";
        Load += OnSettingsDialogLoad;
        tabControlSettings.ResumeLayout(false);
        tabPageViewSettings.ResumeLayout(false);
        tabPageViewSettings.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)upDownMaximumLineLength).EndInit();
        ((System.ComponentModel.ISupportInitialize)upDownMaxDisplayLength).EndInit();
        ((System.ComponentModel.ISupportInitialize)upDownMaximumFilterEntriesDisplayed).EndInit();
        ((System.ComponentModel.ISupportInitialize)upDownMaximumFilterEntries).EndInit();
        groupBoxMisc.ResumeLayout(false);
        groupBoxMisc.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)cpDownColumnWidth).EndInit();
        groupBoxDefaults.ResumeLayout(false);
        groupBoxDefaults.PerformLayout();
        groupBoxFont.ResumeLayout(false);
        tabPageTimeStampFeatures.ResumeLayout(false);
        groupBoxTimeSpreadDisplay.ResumeLayout(false);
        groupBoxTimeSpreadDisplay.PerformLayout();
        groupBoxDisplayMode.ResumeLayout(false);
        groupBoxDisplayMode.PerformLayout();
        groupBoxTimeStampNavigationControl.ResumeLayout(false);
        groupBoxTimeStampNavigationControl.PerformLayout();
        groupBoxMouseDragDefaults.ResumeLayout(false);
        groupBoxMouseDragDefaults.PerformLayout();
        tabPageExternalTools.ResumeLayout(false);
        groupBoxToolSettings.ResumeLayout(false);
        groupBoxToolSettings.PerformLayout();
        tabPageColumnizers.ResumeLayout(false);
        groupBoxColumnizerPriority.ResumeLayout(false);
        groupBoxColumnizerPriority.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridViewColumnizer).EndInit();
        tabPageHighlightMask.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dataGridViewHighlightMask).EndInit();
        tabPageControlCharacters.ResumeLayout(false);
        tabPageControlCharacters.PerformLayout();
        groupBoxControlCharsStyle.ResumeLayout(false);
        groupBoxControlCharsStyle.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridViewControlChars).EndInit();
        groupBoxControlCharsAppearance.ResumeLayout(false);
        groupBoxControlCharsAppearance.PerformLayout();
        groupBoxControlCharsClipboard.ResumeLayout(false);
        groupBoxControlCharsClipboard.PerformLayout();
        tabPageMultiFile.ResumeLayout(false);
        groupBoxDefaultFileNamePattern.ResumeLayout(false);
        groupBoxDefaultFileNamePattern.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)upDownMultifileDays).EndInit();
        groupBoxWhenOpeningMultiFile.ResumeLayout(false);
        groupBoxWhenOpeningMultiFile.PerformLayout();
        tabPagePlugins.ResumeLayout(false);
        groupBoxPlugins.ResumeLayout(false);
        groupBoxSettings.ResumeLayout(false);
        panelPlugin.ResumeLayout(false);
        tabPageSessions.ResumeLayout(false);
        tabPageSessions.PerformLayout();
        groupBoxPersistantFileLocation.ResumeLayout(false);
        groupBoxPersistantFileLocation.PerformLayout();
        tabPageMemory.ResumeLayout(false);
        groupBoxCPUAndStuff.ResumeLayout(false);
        groupBoxCPUAndStuff.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)upDownPollingInterval).EndInit();
        groupBoxLineBufferUsage.ResumeLayout(false);
        groupBoxLineBufferUsage.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)upDownLinesPerBlock).EndInit();
        ((System.ComponentModel.ISupportInitialize)upDownBlockCount).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TabControl tabControlSettings;
    private System.Windows.Forms.TabPage tabPageViewSettings;
    private System.Windows.Forms.Label labelFont;
    private System.Windows.Forms.TabPage tabPageTimeStampFeatures;
    private System.Windows.Forms.GroupBox groupBoxFont;
    private System.Windows.Forms.Button buttonChangeFont;
    private System.Windows.Forms.Button buttonCancel;
    private System.Windows.Forms.Button buttonOk;
    private System.Windows.Forms.CheckBox checkBoxFilterTail;
    private System.Windows.Forms.CheckBox checkBoxFollowTail;
    private System.Windows.Forms.CheckBox checkBoxSyncFilter;
    private System.Windows.Forms.GroupBox groupBoxDefaults;
    private System.Windows.Forms.CheckBox checkBoxTimestamp;
    private System.Windows.Forms.GroupBox groupBoxMouseDragDefaults;
    private System.Windows.Forms.RadioButton radioButtonHorizMouseDrag;
    private System.Windows.Forms.RadioButton radioButtonVerticalMouseDrag;
    private System.Windows.Forms.RadioButton radioButtonVerticalMouseDragInverted;
    private System.Windows.Forms.TabPage tabPageExternalTools;
    private System.Windows.Forms.GroupBox groupBoxToolSettings;
    private System.Windows.Forms.TextBox textBoxArguments;
    private System.Windows.Forms.Button buttonTool;
    private System.Windows.Forms.TextBox textBoxTool;
    private System.Windows.Forms.Label labelArguments;
    private System.Windows.Forms.Label labelTool;
    private System.Windows.Forms.Button buttonArguments;
    private System.Windows.Forms.TabPage tabPageColumnizers;
    private System.Windows.Forms.DataGridView dataGridViewColumnizer;
    private System.Windows.Forms.Button buttonDelete;
    private System.Windows.Forms.CheckBox checkBoxSysout;
    private System.Windows.Forms.GroupBox groupBoxColumnizerPriority;
    private System.Windows.Forms.RadioButton radioColumnizerPriorityHistoryThenMask;
    private System.Windows.Forms.RadioButton radioColumnizerPriorityMaskThenHistory;
    private System.Windows.Forms.RadioButton radioColumnizerPriorityMaskOverridesPersistence;
    private System.Windows.Forms.GroupBox groupBoxMisc;
    private System.Windows.Forms.CheckBox checkBoxAskCloseTabs;
    private System.Windows.Forms.TabPage tabPageMultiFile;
    private System.Windows.Forms.GroupBox groupBoxWhenOpeningMultiFile;
    private System.Windows.Forms.RadioButton radioButtonAskWhatToDo;
    private System.Windows.Forms.RadioButton radioButtonTreatAllFilesAsOneMultifile;
    private System.Windows.Forms.RadioButton radioButtonLoadEveryFileIntoSeperatedTab;
    private System.Windows.Forms.Label labelNoteMultiFile;
    private System.Windows.Forms.Label labelHintMultiFile;
    private System.Windows.Forms.HelpProvider helpProvider;
    private System.Windows.Forms.CheckBox checkBoxSingleInstance;
    private System.Windows.Forms.ComboBox comboBoxColumnizer;
    private System.Windows.Forms.CheckBox checkBoxOpenLastFiles;
    private System.Windows.Forms.CheckBox checkBoxTailState;
    private System.Windows.Forms.Button buttonTailColor;
    private System.Windows.Forms.NumericUpDown cpDownColumnWidth;
    private System.Windows.Forms.CheckBox checkBoxColumnSize;
    private System.Windows.Forms.CheckBox checkBoxTimeSpread;
    private System.Windows.Forms.GroupBox groupBoxTimeSpreadDisplay;
    private System.Windows.Forms.GroupBox groupBoxTimeStampNavigationControl;
    private System.Windows.Forms.Button buttonTimespreadColor;
    private System.Windows.Forms.CheckBox checkBoxReverseAlpha;
    private System.Windows.Forms.GroupBox groupBoxDisplayMode;
    private System.Windows.Forms.RadioButton radioButtonLineView;
    private System.Windows.Forms.RadioButton radioButtonTimeView;
    private System.Windows.Forms.TabPage tabPagePlugins;
    private System.Windows.Forms.ListBox listBoxPlugin;
    private System.Windows.Forms.Panel panelPlugin;
    private System.Windows.Forms.GroupBox groupBoxPlugins;
    private System.Windows.Forms.GroupBox groupBoxSettings;
    private System.Windows.Forms.Button buttonConfigPlugin;
    private System.Windows.Forms.TabPage tabPageSessions;
    private System.Windows.Forms.GroupBox groupBoxPersistantFileLocation;
    private System.Windows.Forms.RadioButton radioButtonsessionSaveDocuments;
    private System.Windows.Forms.RadioButton radioButtonSessionSameDir;
    private System.Windows.Forms.CheckBox checkBoxSaveSessions;
    private System.Windows.Forms.RadioButton radioButtonSessionSaveOwn;
    private System.Windows.Forms.Label labelSessionSaveOwnDir;
    private System.Windows.Forms.Button buttonSessionSaveDir;
    private System.Windows.Forms.CheckBox checkBoxSaveFilter;
    private System.Windows.Forms.TabPage tabPageMemory;
    private System.Windows.Forms.Label labelNumberOfBlocks;
    private System.Windows.Forms.NumericUpDown upDownBlockCount;
    private System.Windows.Forms.NumericUpDown upDownLinesPerBlock;
    private System.Windows.Forms.Label labelLinesPerBlock;
    private System.Windows.Forms.GroupBox groupBoxLineBufferUsage;
    private System.Windows.Forms.Label labelInfo;
    private System.Windows.Forms.TabPage tabPageHighlightMask;
    private System.Windows.Forms.DataGridView dataGridViewHighlightMask;
    private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumnFileName;
    private System.Windows.Forms.DataGridViewComboBoxColumn dataGridViewComboBoxColumnHighlightGroup;
    private System.Windows.Forms.CheckBox checkBoxMultiThread;
    private System.Windows.Forms.Label labelFilePollingInterval;
    private System.Windows.Forms.NumericUpDown upDownPollingInterval;
    private System.Windows.Forms.GroupBox groupBoxCPUAndStuff;
    private System.Windows.Forms.Label labelToolColumnizerForOutput;
    private System.Windows.Forms.Label labelToolName;
    private System.Windows.Forms.CheckedListBox listBoxTools;
    private System.Windows.Forms.TextBox textBoxToolName;
    private System.Windows.Forms.Button buttonToolDelete;
    private System.Windows.Forms.Button buttonToolAdd;
    private System.Windows.Forms.Button buttonToolDown;
    private System.Windows.Forms.Button buttonToolUp;
    private System.Windows.Forms.Label labelToolsDescription;
    private System.Windows.Forms.Button buttonIcon;
    private System.Windows.Forms.Label labelWorkingDir;
    private System.Windows.Forms.Button buttonWorkingDir;
    private System.Windows.Forms.TextBox textBoxWorkingDir;
    private System.Windows.Forms.GroupBox groupBoxDefaultFileNamePattern;
    private System.Windows.Forms.Label labelMaxDays;
    private System.Windows.Forms.Label labelPattern;
    private System.Windows.Forms.NumericUpDown upDownMultifileDays;
    private System.Windows.Forms.TextBox textBoxMultifilePattern;
    private System.Windows.Forms.Label labelDefaultEncoding;
    private System.Windows.Forms.ComboBox comboBoxEncoding;
    private System.Windows.Forms.ToolTip toolTip;
    private System.Windows.Forms.CheckBox checkBoxColumnFinder;
    private System.Windows.Forms.Button buttonExport;
    private System.Windows.Forms.Button buttonImport;
    private System.Windows.Forms.Label labelMaximumFilterEntries;
    private System.Windows.Forms.NumericUpDown upDownMaximumFilterEntries;
    private System.Windows.Forms.NumericUpDown upDownMaximumFilterEntriesDisplayed;
    private System.Windows.Forms.Label labelMaximumFilterEntriesDisplayed;
    private System.Windows.Forms.CheckBox checkBoxAutoPick;
    private System.Windows.Forms.CheckBox checkBoxPortableMode;
    private System.Windows.Forms.RadioButton radioButtonSessionApplicationStartupDir;
    private System.Windows.Forms.CheckBox checkBoxShowErrorMessageOnlyOneInstance;
    private System.Windows.Forms.Label labelColorMode;
    private System.Windows.Forms.ComboBox comboBoxColorMode;
    private System.Windows.Forms.NumericUpDown upDownMaximumLineLength;
    private System.Windows.Forms.Label labelMaximumLineLength;
    private System.Windows.Forms.Label labelWarningMaximumLineLength;
    private System.Windows.Forms.TabPage tabPageControlCharacters;
    private System.Windows.Forms.CheckBox checkBoxControlCharsEnable;
    private System.Windows.Forms.Label labelControlCharsWarning;
    private System.Windows.Forms.Label labelControlCharsHint;
    private System.Windows.Forms.GroupBox groupBoxControlCharsStyle;
    private System.Windows.Forms.RadioButton radioButtonControlCharStyleControlPictures;
    private System.Windows.Forms.RadioButton radioButtonControlCharStyleCaret;
    private System.Windows.Forms.RadioButton radioButtonControlCharStyleCEscape;
    private System.Windows.Forms.RadioButton radioButtonControlCharStyleAbbreviation;
    private System.Windows.Forms.RadioButton radioButtonControlCharStyleIso2047;
    private System.Windows.Forms.Button buttonControlCharsPresetAll;
    private System.Windows.Forms.Button buttonControlCharsPresetNone;
    private System.Windows.Forms.Button buttonControlCharsPresetNonWhitespace;
    private System.Windows.Forms.DataGridView dataGridViewControlChars;
    private System.Windows.Forms.GroupBox groupBoxControlCharsAppearance;
    private System.Windows.Forms.Label labelControlCharsForeColor;
    private System.Windows.Forms.Button buttonControlCharsForeColor;
    private System.Windows.Forms.Label labelControlCharsBackColor;
    private System.Windows.Forms.Button buttonControlCharsBackColor;
    private System.Windows.Forms.Button buttonControlCharsBackColorClear;
    private System.Windows.Forms.CheckBox checkBoxControlCharsBold;
    private System.Windows.Forms.CheckBox checkBoxControlCharsItalic;
    private System.Windows.Forms.Label labelControlCharsSample;
    private System.Windows.Forms.GroupBox groupBoxControlCharsClipboard;
    private System.Windows.Forms.CheckBox checkBoxControlCharsCopyDisplayedForm;
    private System.Windows.Forms.NumericUpDown upDownMaxDisplayLength;
    private System.Windows.Forms.Label labelMaxDisplayLength;
    private Label labelLanguage;
    private ComboBox comboBoxLanguage;
    private ComboBox comboBoxReaderType;
    private DataGridViewCheckBoxColumn columnControlCharEnabled;
    private DataGridViewTextBoxColumn columnControlCharHex;
    private DataGridViewTextBoxColumn columnControlCharAbbr;
    private DataGridViewTextBoxColumn columnControlCharCaret;
    private DataGridViewTextBoxColumn columnControlCharPreview;
    private DataGridViewImageColumn dataGridViewImageColumnColumnizerStale;
    private DataGridViewTextBoxColumn dataGridViewTextBoxColumnFileMask;
    private DataGridViewComboBoxColumn dataGridViewComboBoxColumnColumnizerMaskType;
    private DataGridViewComboBoxColumn dataGridViewComboBoxColumnColumnizer;
}
