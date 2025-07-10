using LogExpert.UI.Controls;

using System.Drawing;

namespace LogExpert.Dialogs;

partial class HighlightDialog
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent ()
    {
        components = new System.ComponentModel.Container();
        var resources = new System.ComponentModel.ComponentResourceManager(typeof(HighlightDialog));
        listBoxHighlight = new ListBox();
        btnAdd = new Button();
        btnDelete = new Button();
        btnMoveUp = new Button();
        btnMoveDown = new Button();
        labelForgroundColor = new Label();
        labelBackgroundColor = new Label();
        btnOk = new Button();
        btnCancel = new Button();
        textBoxSearchString = new TextBox();
        labelSearchString = new Label();
        btnApply = new Button();
        btnCustomForeColor = new Button();
        btnCustomBackColor = new Button();
        checkBoxRegex = new CheckBox();
        checkBoxCaseSensitive = new CheckBox();
        checkBoxDontDirtyLed = new CheckBox();
        groupBoxLineMatchCriteria = new GroupBox();
        groupBoxColoring = new GroupBox();
        checkBoxNoBackground = new CheckBox();
        checkBoxBold = new CheckBox();
        checkBoxWordMatch = new CheckBox();
        colorBoxForeground = new ColorComboBox();
        colorBoxBackground = new ColorComboBox();
        groupBoxActions = new GroupBox();
        btnBookmarkComment = new Button();
        btnSelectPlugin = new Button();
        checkBoxPlugin = new CheckBox();
        checkBoxStopTail = new CheckBox();
        checkBoxBookmark = new CheckBox();
        helpProvider = new HelpProvider();
        groupBox4 = new GroupBox();
        btnImportGroup = new Button();
        btnExportGroup = new Button();
        btnMoveGroupDown = new Button();
        btnMoveGroupUp = new Button();
        labelAssignNamesToGroups = new Label();
        btnCopyGroup = new Button();
        btnDeleteGroup = new Button();
        btnNewGroup = new Button();
        comboBoxGroups = new ComboBox();
        toolTip = new ToolTip(components);
        pnlBackground = new Panel();
        groupBoxLineMatchCriteria.SuspendLayout();
        groupBoxColoring.SuspendLayout();
        groupBoxActions.SuspendLayout();
        groupBox4.SuspendLayout();
        pnlBackground.SuspendLayout();
        SuspendLayout();
        // 
        // listBoxHighlight
        // 
        listBoxHighlight.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        listBoxHighlight.DrawMode = DrawMode.OwnerDrawFixed;
        listBoxHighlight.FormattingEnabled = true;
        listBoxHighlight.Location = new Point(32, 129);
        listBoxHighlight.Margin = new Padding(4, 5, 4, 5);
        listBoxHighlight.Name = "listBoxHighlight";
        listBoxHighlight.Size = new Size(413, 180);
        listBoxHighlight.TabIndex = 0;
        listBoxHighlight.SelectedIndexChanged += OnListBoxHighlightSelectedIndexChanged;
        // 
        // btnAdd
        // 
        btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnAdd.Location = new Point(453, 187);
        btnAdd.Margin = new Padding(4, 5, 4, 5);
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(158, 35);
        btnAdd.TabIndex = 1;
        btnAdd.Text = "&Add";
        toolTip.SetToolTip(btnAdd, "Create a new hilight item from information below");
        btnAdd.UseVisualStyleBackColor = true;
        btnAdd.Click += OnAddButtonClick;
        // 
        // btnDelete
        // 
        btnDelete.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnDelete.Location = new Point(453, 232);
        btnDelete.Margin = new Padding(4, 5, 4, 5);
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(158, 35);
        btnDelete.TabIndex = 2;
        btnDelete.Text = "D&elete";
        toolTip.SetToolTip(btnDelete, "Delete the current hilight");
        btnDelete.UseVisualStyleBackColor = true;
        btnDelete.Click += OnDeleteButtonClick;
        // 
        // btnMoveUp
        // 
        btnMoveUp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnMoveUp.Location = new Point(453, 129);
        btnMoveUp.Margin = new Padding(4, 5, 4, 5);
        btnMoveUp.Name = "btnMoveUp";
        btnMoveUp.Size = new Size(75, 35);
        btnMoveUp.TabIndex = 3;
        btnMoveUp.Text = "&Up";
        toolTip.SetToolTip(btnMoveUp, "Move the current hilight one position up");
        btnMoveUp.UseVisualStyleBackColor = true;
        btnMoveUp.Click += OnBtnMoveUpClick;
        // 
        // btnMoveDown
        // 
        btnMoveDown.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnMoveDown.Location = new Point(536, 129);
        btnMoveDown.Margin = new Padding(4, 5, 4, 5);
        btnMoveDown.Name = "btnMoveDown";
        btnMoveDown.Size = new Size(75, 35);
        btnMoveDown.TabIndex = 4;
        btnMoveDown.Text = "&Down";
        toolTip.SetToolTip(btnMoveDown, "Move the current hilight one position down");
        btnMoveDown.UseVisualStyleBackColor = true;
        btnMoveDown.Click += OnBtnMoveDownClick;
        // 
        // labelForgroundColor
        // 
        labelForgroundColor.AutoSize = true;
        labelForgroundColor.Location = new Point(9, 38);
        labelForgroundColor.Margin = new Padding(4, 0, 4, 0);
        labelForgroundColor.Name = "labelForgroundColor";
        labelForgroundColor.Size = new Size(99, 15);
        labelForgroundColor.TabIndex = 6;
        labelForgroundColor.Text = "Foreground color";
        // 
        // labelBackgroundColor
        // 
        labelBackgroundColor.AutoSize = true;
        labelBackgroundColor.Location = new Point(9, 115);
        labelBackgroundColor.Margin = new Padding(4, 0, 4, 0);
        labelBackgroundColor.Name = "labelBackgroundColor";
        labelBackgroundColor.Size = new Size(101, 15);
        labelBackgroundColor.TabIndex = 8;
        labelBackgroundColor.Text = "Background color";
        // 
        // btnOk
        // 
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new Point(372, 718);
        btnOk.Margin = new Padding(4, 5, 4, 5);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(112, 35);
        btnOk.TabIndex = 9;
        btnOk.Text = "OK";
        btnOk.UseVisualStyleBackColor = true;
        btnOk.Click += OnBtnOkClick;
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(499, 718);
        btnCancel.Margin = new Padding(4, 5, 4, 5);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(112, 35);
        btnCancel.TabIndex = 10;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // textBoxSearchString
        // 
        textBoxSearchString.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        textBoxSearchString.Location = new Point(9, 55);
        textBoxSearchString.Margin = new Padding(4, 5, 4, 5);
        textBoxSearchString.Name = "textBoxSearchString";
        textBoxSearchString.Size = new Size(575, 23);
        textBoxSearchString.TabIndex = 11;
        textBoxSearchString.TextChanged += ChangeToDirty;
        // 
        // labelSearchString
        // 
        labelSearchString.AutoSize = true;
        labelSearchString.Location = new Point(9, 31);
        labelSearchString.Margin = new Padding(4, 0, 4, 0);
        labelSearchString.Name = "labelSearchString";
        labelSearchString.Size = new Size(78, 15);
        labelSearchString.TabIndex = 12;
        labelSearchString.Text = "Search string:";
        // 
        // btnApply
        // 
        btnApply.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnApply.Enabled = false;
        btnApply.Image = (Image)resources.GetObject("btnApply.Image");
        btnApply.ImageAlign = ContentAlignment.MiddleRight;
        btnApply.Location = new Point(453, 277);
        btnApply.Margin = new Padding(4, 5, 4, 5);
        btnApply.Name = "btnApply";
        btnApply.Size = new Size(158, 35);
        btnApply.TabIndex = 13;
        btnApply.Text = "A&pply";
        toolTip.SetToolTip(btnApply, "Apply changes below to current hiligth");
        btnApply.UseVisualStyleBackColor = true;
        btnApply.Click += OnBtnApplyClick;
        // 
        // btnCustomForeColor
        // 
        btnCustomForeColor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCustomForeColor.Location = new Point(183, 60);
        btnCustomForeColor.Margin = new Padding(4, 5, 4, 5);
        btnCustomForeColor.Name = "btnCustomForeColor";
        btnCustomForeColor.Size = new Size(78, 35);
        btnCustomForeColor.TabIndex = 14;
        btnCustomForeColor.Text = "Custom";
        toolTip.SetToolTip(btnCustomForeColor, "Pick a custom foreground color");
        btnCustomForeColor.UseVisualStyleBackColor = true;
        btnCustomForeColor.Click += OnBtnCustomForeColorClick;
        // 
        // btnCustomBackColor
        // 
        btnCustomBackColor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCustomBackColor.Location = new Point(183, 137);
        btnCustomBackColor.Margin = new Padding(4, 5, 4, 5);
        btnCustomBackColor.Name = "btnCustomBackColor";
        btnCustomBackColor.Size = new Size(78, 35);
        btnCustomBackColor.TabIndex = 15;
        btnCustomBackColor.Text = "Custom";
        toolTip.SetToolTip(btnCustomBackColor, "Pick a custom background color");
        btnCustomBackColor.UseVisualStyleBackColor = true;
        btnCustomBackColor.Click += OnBtnCustomBackColorClick;
        // 
        // checkBoxRegex
        // 
        checkBoxRegex.AutoSize = true;
        checkBoxRegex.Location = new Point(180, 95);
        checkBoxRegex.Margin = new Padding(4, 5, 4, 5);
        checkBoxRegex.Name = "checkBoxRegex";
        checkBoxRegex.Size = new Size(58, 19);
        checkBoxRegex.TabIndex = 16;
        checkBoxRegex.Text = "RegEx";
        toolTip.SetToolTip(checkBoxRegex, "Whether the string is a regular expresion");
        checkBoxRegex.UseVisualStyleBackColor = true;
        checkBoxRegex.CheckedChanged += ChangeToDirty;
        checkBoxRegex.MouseUp += OnChkBoxRegexMouseUp;
        // 
        // checkBoxCaseSensitive
        // 
        checkBoxCaseSensitive.AutoSize = true;
        checkBoxCaseSensitive.Location = new Point(14, 95);
        checkBoxCaseSensitive.Margin = new Padding(4, 5, 4, 5);
        checkBoxCaseSensitive.Name = "checkBoxCaseSensitive";
        checkBoxCaseSensitive.Size = new Size(99, 19);
        checkBoxCaseSensitive.TabIndex = 17;
        checkBoxCaseSensitive.Text = "Case sensitive";
        toolTip.SetToolTip(checkBoxCaseSensitive, "Whether the string will match uppercases and lowercases");
        checkBoxCaseSensitive.UseVisualStyleBackColor = true;
        checkBoxCaseSensitive.CheckedChanged += ChangeToDirty;
        // 
        // checkBoxDontDirtyLed
        // 
        checkBoxDontDirtyLed.AutoSize = true;
        checkBoxDontDirtyLed.Location = new Point(15, 38);
        checkBoxDontDirtyLed.Margin = new Padding(4, 5, 4, 5);
        checkBoxDontDirtyLed.Name = "checkBoxDontDirtyLed";
        checkBoxDontDirtyLed.Size = new Size(118, 19);
        checkBoxDontDirtyLed.TabIndex = 18;
        checkBoxDontDirtyLed.Text = "Don't lit dirty LED";
        toolTip.SetToolTip(checkBoxDontDirtyLed, "When matching a line, don't mark the page as \"dirty\"");
        checkBoxDontDirtyLed.UseVisualStyleBackColor = true;
        checkBoxDontDirtyLed.CheckedChanged += ChangeToDirty;
        // 
        // groupBoxLineMatchCriteria
        // 
        groupBoxLineMatchCriteria.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        groupBoxLineMatchCriteria.Controls.Add(textBoxSearchString);
        groupBoxLineMatchCriteria.Controls.Add(labelSearchString);
        groupBoxLineMatchCriteria.Controls.Add(checkBoxRegex);
        groupBoxLineMatchCriteria.Controls.Add(checkBoxCaseSensitive);
        groupBoxLineMatchCriteria.Location = new Point(18, 322);
        groupBoxLineMatchCriteria.Margin = new Padding(4, 5, 4, 5);
        groupBoxLineMatchCriteria.Name = "groupBoxLineMatchCriteria";
        groupBoxLineMatchCriteria.Padding = new Padding(4, 5, 4, 5);
        groupBoxLineMatchCriteria.Size = new Size(607, 135);
        groupBoxLineMatchCriteria.TabIndex = 19;
        groupBoxLineMatchCriteria.TabStop = false;
        groupBoxLineMatchCriteria.Text = "Line match criteria";
        // 
        // groupBoxColoring
        // 
        groupBoxColoring.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        groupBoxColoring.Controls.Add(checkBoxNoBackground);
        groupBoxColoring.Controls.Add(checkBoxBold);
        groupBoxColoring.Controls.Add(checkBoxWordMatch);
        groupBoxColoring.Controls.Add(labelForgroundColor);
        groupBoxColoring.Controls.Add(colorBoxForeground);
        groupBoxColoring.Controls.Add(btnCustomForeColor);
        groupBoxColoring.Controls.Add(btnCustomBackColor);
        groupBoxColoring.Controls.Add(labelBackgroundColor);
        groupBoxColoring.Controls.Add(colorBoxBackground);
        groupBoxColoring.Location = new Point(18, 467);
        groupBoxColoring.Margin = new Padding(4, 5, 4, 5);
        groupBoxColoring.Name = "groupBoxColoring";
        groupBoxColoring.Padding = new Padding(4, 5, 4, 5);
        groupBoxColoring.Size = new Size(285, 286);
        groupBoxColoring.TabIndex = 20;
        groupBoxColoring.TabStop = false;
        groupBoxColoring.Text = "Coloring";
        // 
        // checkBoxNoBackground
        // 
        checkBoxNoBackground.AutoSize = true;
        checkBoxNoBackground.Enabled = false;
        checkBoxNoBackground.Location = new Point(141, 240);
        checkBoxNoBackground.Margin = new Padding(4, 5, 4, 5);
        checkBoxNoBackground.Name = "checkBoxNoBackground";
        checkBoxNoBackground.Size = new Size(109, 19);
        checkBoxNoBackground.TabIndex = 18;
        checkBoxNoBackground.Text = "No Background";
        toolTip.SetToolTip(checkBoxNoBackground, "Don't set the background color");
        checkBoxNoBackground.UseVisualStyleBackColor = true;
        checkBoxNoBackground.CheckedChanged += OnChkBoxNoBackgroundCheckedChanged;
        // 
        // checkBoxBold
        // 
        checkBoxBold.AutoSize = true;
        checkBoxBold.Location = new Point(9, 205);
        checkBoxBold.Margin = new Padding(4, 5, 4, 5);
        checkBoxBold.Name = "checkBoxBold";
        checkBoxBold.Size = new Size(50, 19);
        checkBoxBold.TabIndex = 17;
        checkBoxBold.Text = "Bold";
        toolTip.SetToolTip(checkBoxBold, "Display the line in bold characters");
        checkBoxBold.UseVisualStyleBackColor = true;
        checkBoxBold.CheckedChanged += OnChkBoxBoldCheckedChanged;
        // 
        // checkBoxWordMatch
        // 
        checkBoxWordMatch.AutoSize = true;
        checkBoxWordMatch.Location = new Point(9, 240);
        checkBoxWordMatch.Margin = new Padding(4, 5, 4, 5);
        checkBoxWordMatch.Name = "checkBoxWordMatch";
        checkBoxWordMatch.Size = new Size(89, 19);
        checkBoxWordMatch.TabIndex = 16;
        checkBoxWordMatch.Text = "Word mode";
        toolTip.SetToolTip(checkBoxWordMatch, "Don't highlight the whole line but only the matching keywords");
        checkBoxWordMatch.UseVisualStyleBackColor = true;
        checkBoxWordMatch.CheckedChanged += OnChkBoxWordMatchCheckedChanged;
        // 
        // colorBoxForeground
        // 
        colorBoxForeground.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        colorBoxForeground.CustomColor = Color.Black;
        colorBoxForeground.DrawMode = DrawMode.OwnerDrawFixed;
        colorBoxForeground.DropDownStyle = ComboBoxStyle.DropDownList;
        colorBoxForeground.FormattingEnabled = true;
        colorBoxForeground.Items.AddRange(new object[] { Color.Black, Color.Black, Color.White, Color.Gray, Color.DarkGray, Color.Blue, Color.LightBlue, Color.DarkBlue, Color.Green, Color.LightGreen, Color.DarkGreen, Color.Olive, Color.Red, Color.Pink, Color.Purple, Color.IndianRed, Color.DarkCyan, Color.Yellow, Color.Black, Color.Black, Color.White, Color.Gray, Color.DarkGray, Color.Blue, Color.LightBlue, Color.DarkBlue, Color.Green, Color.LightGreen, Color.DarkGreen, Color.Olive, Color.Red, Color.Pink, Color.Purple, Color.IndianRed, Color.DarkCyan, Color.Yellow, Color.Black, Color.Black, Color.White, Color.Gray, Color.DarkGray, Color.Blue, Color.LightBlue, Color.DarkBlue, Color.Green, Color.LightGreen, Color.DarkGreen, Color.Olive, Color.Red, Color.Pink, Color.Purple, Color.IndianRed, Color.DarkCyan, Color.Yellow });
        colorBoxForeground.Location = new Point(8, 63);
        colorBoxForeground.Margin = new Padding(4, 5, 4, 5);
        colorBoxForeground.Name = "colorBoxForeground";
        colorBoxForeground.Size = new Size(165, 24);
        colorBoxForeground.TabIndex = 5;
        colorBoxForeground.SelectedIndexChanged += ChangeToDirty;
        // 
        // colorBoxBackground
        // 
        colorBoxBackground.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        colorBoxBackground.CustomColor = Color.Black;
        colorBoxBackground.DrawMode = DrawMode.OwnerDrawFixed;
        colorBoxBackground.DropDownStyle = ComboBoxStyle.DropDownList;
        colorBoxBackground.FormattingEnabled = true;
        colorBoxBackground.Items.AddRange(new object[] { Color.Black, Color.Black, Color.White, Color.Gray, Color.DarkGray, Color.Blue, Color.LightBlue, Color.DarkBlue, Color.Green, Color.LightGreen, Color.DarkGreen, Color.Olive, Color.Red, Color.Pink, Color.Purple, Color.IndianRed, Color.DarkCyan, Color.Yellow, Color.Black, Color.Black, Color.White, Color.Gray, Color.DarkGray, Color.Blue, Color.LightBlue, Color.DarkBlue, Color.Green, Color.LightGreen, Color.DarkGreen, Color.Olive, Color.Red, Color.Pink, Color.Purple, Color.IndianRed, Color.DarkCyan, Color.Yellow, Color.Black, Color.Black, Color.White, Color.Gray, Color.DarkGray, Color.Blue, Color.LightBlue, Color.DarkBlue, Color.Green, Color.LightGreen, Color.DarkGreen, Color.Olive, Color.Red, Color.Pink, Color.Purple, Color.IndianRed, Color.DarkCyan, Color.Yellow });
        colorBoxBackground.Location = new Point(9, 140);
        colorBoxBackground.Margin = new Padding(4, 5, 4, 5);
        colorBoxBackground.Name = "colorBoxBackground";
        colorBoxBackground.Size = new Size(165, 24);
        colorBoxBackground.TabIndex = 7;
        colorBoxBackground.SelectedIndexChanged += ChangeToDirty;
        // 
        // groupBoxActions
        // 
        groupBoxActions.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        groupBoxActions.Controls.Add(btnBookmarkComment);
        groupBoxActions.Controls.Add(btnSelectPlugin);
        groupBoxActions.Controls.Add(checkBoxPlugin);
        groupBoxActions.Controls.Add(checkBoxStopTail);
        groupBoxActions.Controls.Add(checkBoxBookmark);
        groupBoxActions.Controls.Add(checkBoxDontDirtyLed);
        groupBoxActions.Location = new Point(313, 467);
        groupBoxActions.Margin = new Padding(4, 5, 4, 5);
        groupBoxActions.Name = "groupBoxActions";
        groupBoxActions.Padding = new Padding(4, 5, 4, 5);
        groupBoxActions.Size = new Size(312, 195);
        groupBoxActions.TabIndex = 21;
        groupBoxActions.TabStop = false;
        groupBoxActions.Text = "Actions";
        // 
        // btnBookmarkComment
        // 
        btnBookmarkComment.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBookmarkComment.Location = new Point(210, 69);
        btnBookmarkComment.Margin = new Padding(4, 5, 4, 5);
        btnBookmarkComment.Name = "btnBookmarkComment";
        btnBookmarkComment.Size = new Size(81, 31);
        btnBookmarkComment.TabIndex = 23;
        btnBookmarkComment.Text = "Text...";
        btnBookmarkComment.UseVisualStyleBackColor = true;
        btnBookmarkComment.Click += OnBtnBookmarkCommentClick;
        // 
        // btnSelectPlugin
        // 
        btnSelectPlugin.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSelectPlugin.Location = new Point(210, 143);
        btnSelectPlugin.Margin = new Padding(4, 5, 4, 5);
        btnSelectPlugin.Name = "btnSelectPlugin";
        btnSelectPlugin.Size = new Size(81, 31);
        btnSelectPlugin.TabIndex = 22;
        btnSelectPlugin.Text = "Select...";
        btnSelectPlugin.UseVisualStyleBackColor = true;
        btnSelectPlugin.Click += OnPluginButtonClick;
        // 
        // checkBoxPlugin
        // 
        checkBoxPlugin.AutoSize = true;
        checkBoxPlugin.Location = new Point(15, 148);
        checkBoxPlugin.Margin = new Padding(4, 5, 4, 5);
        checkBoxPlugin.Name = "checkBoxPlugin";
        checkBoxPlugin.Size = new Size(60, 19);
        checkBoxPlugin.TabIndex = 21;
        checkBoxPlugin.Text = "Plugin";
        toolTip.SetToolTip(checkBoxPlugin, "When matching a line, call a keyword action plugin");
        checkBoxPlugin.UseVisualStyleBackColor = true;
        checkBoxPlugin.CheckedChanged += OnChkBoxPluginCheckedChanged;
        // 
        // checkBoxStopTail
        // 
        checkBoxStopTail.AutoSize = true;
        checkBoxStopTail.Location = new Point(15, 111);
        checkBoxStopTail.Margin = new Padding(4, 5, 4, 5);
        checkBoxStopTail.Name = "checkBoxStopTail";
        checkBoxStopTail.Size = new Size(108, 19);
        checkBoxStopTail.TabIndex = 20;
        checkBoxStopTail.Text = "Stop Follow Tail";
        toolTip.SetToolTip(checkBoxStopTail, "When matching a line, stop automatic scrolling");
        checkBoxStopTail.UseVisualStyleBackColor = true;
        checkBoxStopTail.CheckedChanged += ChangeToDirty;
        // 
        // checkBoxBookmark
        // 
        checkBoxBookmark.AutoSize = true;
        checkBoxBookmark.Location = new Point(15, 74);
        checkBoxBookmark.Margin = new Padding(4, 5, 4, 5);
        checkBoxBookmark.Name = "checkBoxBookmark";
        checkBoxBookmark.Size = new Size(99, 19);
        checkBoxBookmark.TabIndex = 19;
        checkBoxBookmark.Text = "Set bookmark";
        toolTip.SetToolTip(checkBoxBookmark, "When matching a line, create a new bookmark for it");
        checkBoxBookmark.UseVisualStyleBackColor = true;
        checkBoxBookmark.CheckedChanged += ChangeToDirty;
        // 
        // helpProvider
        // 
        helpProvider.HelpNamespace = "LogExpert.chm";
        helpProvider.Tag = "";
        // 
        // groupBox4
        // 
        groupBox4.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        groupBox4.Controls.Add(btnImportGroup);
        groupBox4.Controls.Add(btnExportGroup);
        groupBox4.Controls.Add(btnMoveGroupDown);
        groupBox4.Controls.Add(btnMoveGroupUp);
        groupBox4.Controls.Add(labelAssignNamesToGroups);
        groupBox4.Controls.Add(btnCopyGroup);
        groupBox4.Controls.Add(btnDeleteGroup);
        groupBox4.Controls.Add(btnNewGroup);
        groupBox4.Controls.Add(comboBoxGroups);
        groupBox4.Location = new Point(18, 5);
        groupBox4.Margin = new Padding(4, 5, 4, 5);
        groupBox4.Name = "groupBox4";
        groupBox4.Padding = new Padding(4, 5, 4, 5);
        groupBox4.Size = new Size(607, 114);
        groupBox4.TabIndex = 22;
        groupBox4.TabStop = false;
        groupBox4.Text = "Groups";
        // 
        // btnImportGroup
        // 
        btnImportGroup.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnImportGroup.Location = new Point(269, 11);
        btnImportGroup.Margin = new Padding(4, 5, 4, 5);
        btnImportGroup.Name = "btnImportGroup";
        btnImportGroup.Size = new Size(75, 35);
        btnImportGroup.TabIndex = 7;
        btnImportGroup.Text = "Import";
        toolTip.SetToolTip(btnImportGroup, "Import highlight groups");
        btnImportGroup.UseVisualStyleBackColor = true;
        btnImportGroup.Click += OnBtnImportGroupClick;
        // 
        // btnExportGroup
        // 
        btnExportGroup.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnExportGroup.Location = new Point(269, 53);
        btnExportGroup.Margin = new Padding(4, 5, 4, 5);
        btnExportGroup.Name = "btnExportGroup";
        btnExportGroup.Size = new Size(75, 35);
        btnExportGroup.TabIndex = 8;
        btnExportGroup.Text = "Export";
        toolTip.SetToolTip(btnExportGroup, "Export highlight groups");
        btnExportGroup.UseVisualStyleBackColor = true;
        btnExportGroup.Click += OnBtnExportGroupClick;
        // 
        // btnMoveGroupDown
        // 
        btnMoveGroupDown.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnMoveGroupDown.Location = new Point(518, 56);
        btnMoveGroupDown.Margin = new Padding(4, 5, 4, 5);
        btnMoveGroupDown.Name = "btnMoveGroupDown";
        btnMoveGroupDown.Size = new Size(75, 35);
        btnMoveGroupDown.TabIndex = 6;
        btnMoveGroupDown.Text = "Down";
        toolTip.SetToolTip(btnMoveGroupDown, "Move the current hilight group one position down");
        btnMoveGroupDown.UseVisualStyleBackColor = true;
        btnMoveGroupDown.Click += OnBtnGroupDownClick;
        // 
        // btnMoveGroupUp
        // 
        btnMoveGroupUp.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnMoveGroupUp.Location = new Point(435, 53);
        btnMoveGroupUp.Margin = new Padding(4, 5, 4, 5);
        btnMoveGroupUp.Name = "btnMoveGroupUp";
        btnMoveGroupUp.Size = new Size(75, 35);
        btnMoveGroupUp.TabIndex = 5;
        btnMoveGroupUp.Text = "Up";
        toolTip.SetToolTip(btnMoveGroupUp, "Move the current hilight group one position up");
        btnMoveGroupUp.UseVisualStyleBackColor = true;
        btnMoveGroupUp.Click += OnBtnGroupUpClick;
        // 
        // labelAssignNamesToGroups
        // 
        labelAssignNamesToGroups.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        labelAssignNamesToGroups.AutoSize = true;
        labelAssignNamesToGroups.Location = new Point(8, 93);
        labelAssignNamesToGroups.Margin = new Padding(4, 0, 4, 0);
        labelAssignNamesToGroups.Name = "labelAssignNamesToGroups";
        labelAssignNamesToGroups.Size = new Size(276, 15);
        labelAssignNamesToGroups.TabIndex = 4;
        labelAssignNamesToGroups.Text = "You can assign groups to file names in the settings.";
        // 
        // btnCopyGroup
        // 
        btnCopyGroup.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCopyGroup.Location = new Point(518, 11);
        btnCopyGroup.Margin = new Padding(4, 5, 4, 5);
        btnCopyGroup.Name = "btnCopyGroup";
        btnCopyGroup.Size = new Size(75, 35);
        btnCopyGroup.TabIndex = 3;
        btnCopyGroup.Text = "Copy";
        toolTip.SetToolTip(btnCopyGroup, "Copy the current hilight group into a new one");
        btnCopyGroup.UseVisualStyleBackColor = true;
        btnCopyGroup.Click += OnBtnCopyGroupClick;
        // 
        // btnDeleteGroup
        // 
        btnDeleteGroup.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnDeleteGroup.Location = new Point(435, 11);
        btnDeleteGroup.Margin = new Padding(4, 5, 4, 5);
        btnDeleteGroup.Name = "btnDeleteGroup";
        btnDeleteGroup.Size = new Size(75, 35);
        btnDeleteGroup.TabIndex = 2;
        btnDeleteGroup.Text = "Del";
        toolTip.SetToolTip(btnDeleteGroup, "Delete the current hilight group");
        btnDeleteGroup.UseVisualStyleBackColor = true;
        btnDeleteGroup.Click += OnBtnDelGroupClick;
        // 
        // btnNewGroup
        // 
        btnNewGroup.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnNewGroup.Location = new Point(352, 11);
        btnNewGroup.Margin = new Padding(4, 5, 4, 5);
        btnNewGroup.Name = "btnNewGroup";
        btnNewGroup.Size = new Size(75, 35);
        btnNewGroup.TabIndex = 1;
        btnNewGroup.Text = "New group";
        btnNewGroup.TextAlign = ContentAlignment.MiddleLeft;
        toolTip.SetToolTip(btnNewGroup, "Create a new empty hilight group");
        btnNewGroup.UseVisualStyleBackColor = true;
        btnNewGroup.Click += OnBtnNewGroupClick;
        // 
        // comboBoxGroups
        // 
        comboBoxGroups.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        comboBoxGroups.DisplayMember = "GroupName";
        comboBoxGroups.DrawMode = DrawMode.OwnerDrawFixed;
        comboBoxGroups.Location = new Point(14, 17);
        comboBoxGroups.Margin = new Padding(4, 5, 4, 5);
        comboBoxGroups.Name = "comboBoxGroups";
        comboBoxGroups.Size = new Size(247, 24);
        comboBoxGroups.TabIndex = 0;
        toolTip.SetToolTip(comboBoxGroups, "Choose a group to create different highlight settings. Type in a name to change in the name of a group.");
        comboBoxGroups.DrawItem += OnCmbBoxGroupDrawItem;
        comboBoxGroups.SelectionChangeCommitted += OnCmbBoxGroupSelectionChangeCommitted;
        comboBoxGroups.TextUpdate += OnCmbBoxGroupTextUpdate;
        // 
        // pnlBackground
        // 
        pnlBackground.AutoScroll = true;
        pnlBackground.Controls.Add(listBoxHighlight);
        pnlBackground.Controls.Add(btnAdd);
        pnlBackground.Controls.Add(btnDelete);
        pnlBackground.Controls.Add(btnMoveUp);
        pnlBackground.Controls.Add(btnMoveDown);
        pnlBackground.Controls.Add(btnOk);
        pnlBackground.Controls.Add(btnCancel);
        pnlBackground.Controls.Add(btnApply);
        pnlBackground.Controls.Add(groupBoxLineMatchCriteria);
        pnlBackground.Controls.Add(groupBoxColoring);
        pnlBackground.Controls.Add(groupBoxActions);
        pnlBackground.Controls.Add(groupBox4);
        pnlBackground.Dock = DockStyle.Fill;
        pnlBackground.Location = new Point(0, 0);
        pnlBackground.Name = "pnlBackground";
        pnlBackground.Size = new Size(644, 761);
        pnlBackground.TabIndex = 23;
        // 
        // HighlightDialog
        // 
        AcceptButton = btnOk;
        CancelButton = btnCancel;
        ClientSize = new Size(644, 761);
        Controls.Add(pnlBackground);
        DoubleBuffered = true;
        helpProvider.SetHelpKeyword(this, "Highlighting.htm");
        helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
        helpProvider.SetHelpString(this, "");
        Icon = (Icon)resources.GetObject("$this.Icon");
        Margin = new Padding(4, 5, 4, 5);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(660, 800);
        Name = "HighlightDialog";
        helpProvider.SetShowHelp(this, true);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Highlighting and action triggers";
        Shown += OnHighlightDialogShown;
        groupBoxLineMatchCriteria.ResumeLayout(false);
        groupBoxLineMatchCriteria.PerformLayout();
        groupBoxColoring.ResumeLayout(false);
        groupBoxColoring.PerformLayout();
        groupBoxActions.ResumeLayout(false);
        groupBoxActions.PerformLayout();
        groupBox4.ResumeLayout(false);
        groupBox4.PerformLayout();
        pnlBackground.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.ListBox listBoxHighlight;
    private System.Windows.Forms.Button btnAdd;
    private System.Windows.Forms.Button btnDelete;
    private System.Windows.Forms.Button btnMoveUp;
    private System.Windows.Forms.Button btnMoveDown;
    private ColorComboBox colorBoxForeground;
    private System.Windows.Forms.Label labelForgroundColor;
    private ColorComboBox colorBoxBackground;
    private System.Windows.Forms.Label labelBackgroundColor;
    private System.Windows.Forms.Button btnOk;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.TextBox textBoxSearchString;
    private System.Windows.Forms.Label labelSearchString;
    private System.Windows.Forms.Button btnApply;
    private System.Windows.Forms.Button btnCustomForeColor;
    private System.Windows.Forms.Button btnCustomBackColor;
    private System.Windows.Forms.CheckBox checkBoxRegex;
    private System.Windows.Forms.CheckBox checkBoxCaseSensitive;
    private System.Windows.Forms.CheckBox checkBoxDontDirtyLed;
    private System.Windows.Forms.GroupBox groupBoxLineMatchCriteria;
    private System.Windows.Forms.GroupBox groupBoxColoring;
    private System.Windows.Forms.GroupBox groupBoxActions;
    private System.Windows.Forms.CheckBox checkBoxBookmark;
    private System.Windows.Forms.CheckBox checkBoxStopTail;
    private System.Windows.Forms.HelpProvider helpProvider;
    private System.Windows.Forms.CheckBox checkBoxPlugin;
    private System.Windows.Forms.Button btnSelectPlugin;
    private System.Windows.Forms.Button btnBookmarkComment;
    private System.Windows.Forms.GroupBox groupBox4;
    private System.Windows.Forms.ComboBox comboBoxGroups;
    private System.Windows.Forms.Button btnDeleteGroup;
    private System.Windows.Forms.Button btnNewGroup;
    private System.Windows.Forms.Button btnCopyGroup;
    private System.Windows.Forms.ToolTip toolTip;
    private System.Windows.Forms.Label labelAssignNamesToGroups;
    private System.Windows.Forms.Button btnMoveGroupUp;
    private System.Windows.Forms.Button btnMoveGroupDown;
    private System.Windows.Forms.CheckBox checkBoxWordMatch;
    private System.Windows.Forms.CheckBox checkBoxBold;
    private System.Windows.Forms.CheckBox checkBoxNoBackground;
    private System.Windows.Forms.Button btnImportGroup;
    private System.Windows.Forms.Button btnExportGroup;
    private System.Windows.Forms.Panel pnlBackground;
}