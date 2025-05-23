using LogExpert.UI.Controls;

using System.Drawing;

namespace LogExpert.Dialogs
{
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
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HighlightDialog));
            listBoxHighlight = new System.Windows.Forms.ListBox();
            btnAdd = new System.Windows.Forms.Button();
            btnDelete = new System.Windows.Forms.Button();
            btnMoveUp = new System.Windows.Forms.Button();
            btnMoveDown = new System.Windows.Forms.Button();
            labelForgroundColor = new System.Windows.Forms.Label();
            labelBackgroundColor = new System.Windows.Forms.Label();
            btnOk = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            textBoxSearchString = new System.Windows.Forms.TextBox();
            labelSearchString = new System.Windows.Forms.Label();
            btnApply = new System.Windows.Forms.Button();
            btnCustomForeColor = new System.Windows.Forms.Button();
            btnCustomBackColor = new System.Windows.Forms.Button();
            checkBoxRegex = new System.Windows.Forms.CheckBox();
            checkBoxCaseSensitive = new System.Windows.Forms.CheckBox();
            checkBoxDontDirtyLed = new System.Windows.Forms.CheckBox();
            groupBoxLineMatchCriteria = new System.Windows.Forms.GroupBox();
            groupBoxColoring = new System.Windows.Forms.GroupBox();
            checkBoxNoBackground = new System.Windows.Forms.CheckBox();
            checkBoxBold = new System.Windows.Forms.CheckBox();
            checkBoxWordMatch = new System.Windows.Forms.CheckBox();
            colorBoxForeground = new ColorComboBox();
            colorBoxBackground = new ColorComboBox();
            groupBoxActions = new System.Windows.Forms.GroupBox();
            btnBookmarkComment = new System.Windows.Forms.Button();
            btnSelectPlugin = new System.Windows.Forms.Button();
            checkBoxPlugin = new System.Windows.Forms.CheckBox();
            checkBoxStopTail = new System.Windows.Forms.CheckBox();
            checkBoxBookmark = new System.Windows.Forms.CheckBox();
            helpProvider = new System.Windows.Forms.HelpProvider();
            groupBox4 = new System.Windows.Forms.GroupBox();
            btnImportGroup = new System.Windows.Forms.Button();
            btnExportGroup = new System.Windows.Forms.Button();
            btnMoveGroupDown = new System.Windows.Forms.Button();
            btnMoveGroupUp = new System.Windows.Forms.Button();
            labelAssignNamesToGroups = new System.Windows.Forms.Label();
            btnCopyGroup = new System.Windows.Forms.Button();
            btnDeleteGroup = new System.Windows.Forms.Button();
            btnNewGroup = new System.Windows.Forms.Button();
            comboBoxGroups = new System.Windows.Forms.ComboBox();
            toolTip = new System.Windows.Forms.ToolTip(components);
            pnlBackground = new System.Windows.Forms.Panel();
            groupBoxLineMatchCriteria.SuspendLayout();
            groupBoxColoring.SuspendLayout();
            groupBoxActions.SuspendLayout();
            groupBox4.SuspendLayout();
            pnlBackground.SuspendLayout();
            SuspendLayout();
            // 
            // listBoxHighlight
            // 
            listBoxHighlight.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            listBoxHighlight.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            listBoxHighlight.FormattingEnabled = true;
            listBoxHighlight.Location = new Point(32, 160);
            listBoxHighlight.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            listBoxHighlight.Name = "listBoxHighlight";
            listBoxHighlight.Size = new Size(487, 228);
            listBoxHighlight.TabIndex = 0;
            listBoxHighlight.SelectedIndexChanged += OnListBoxHighlightSelectedIndexChanged;
            // 
            // btnAdd
            // 
            btnAdd.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnAdd.Location = new Point(529, 218);
            btnAdd.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            btnDelete.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnDelete.Location = new Point(529, 263);
            btnDelete.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            btnMoveUp.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnMoveUp.Location = new Point(529, 160);
            btnMoveUp.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            btnMoveDown.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnMoveDown.Location = new Point(612, 160);
            btnMoveDown.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            labelForgroundColor.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelForgroundColor.Name = "labelForgroundColor";
            labelForgroundColor.Size = new Size(99, 15);
            labelForgroundColor.TabIndex = 6;
            labelForgroundColor.Text = "Foreground color";
            // 
            // labelBackgroundColor
            // 
            labelBackgroundColor.AutoSize = true;
            labelBackgroundColor.Location = new Point(9, 115);
            labelBackgroundColor.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelBackgroundColor.Name = "labelBackgroundColor";
            labelBackgroundColor.Size = new Size(101, 15);
            labelBackgroundColor.TabIndex = 8;
            labelBackgroundColor.Text = "Background color";
            // 
            // btnOk
            // 
            btnOk.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnOk.Location = new Point(449, 832);
            btnOk.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(112, 35);
            btnOk.TabIndex = 9;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += OnBtnOkClick;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Location = new Point(576, 832);
            btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(112, 35);
            btnCancel.TabIndex = 10;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // textBoxSearchString
            // 
            textBoxSearchString.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textBoxSearchString.Location = new Point(9, 55);
            textBoxSearchString.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            textBoxSearchString.Name = "textBoxSearchString";
            textBoxSearchString.Size = new Size(639, 23);
            textBoxSearchString.TabIndex = 11;
            textBoxSearchString.TextChanged += ChangeToDirty;
            // 
            // labelSearchString
            // 
            labelSearchString.AutoSize = true;
            labelSearchString.Location = new Point(9, 31);
            labelSearchString.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSearchString.Name = "labelSearchString";
            labelSearchString.Size = new Size(78, 15);
            labelSearchString.TabIndex = 12;
            labelSearchString.Text = "Search string:";
            // 
            // btnApply
            // 
            btnApply.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnApply.Enabled = false;
            btnApply.Image = (Image)resources.GetObject("btnApply.Image");
            btnApply.ImageAlign = ContentAlignment.MiddleRight;
            btnApply.Location = new Point(529, 308);
            btnApply.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            btnCustomForeColor.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnCustomForeColor.Location = new Point(247, 60);
            btnCustomForeColor.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            btnCustomBackColor.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnCustomBackColor.Location = new Point(247, 137);
            btnCustomBackColor.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            checkBoxRegex.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            checkBoxRegex.Name = "checkBoxRegex";
            checkBoxRegex.Size = new Size(57, 19);
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
            checkBoxCaseSensitive.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            checkBoxDontDirtyLed.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            groupBoxLineMatchCriteria.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBoxLineMatchCriteria.Controls.Add(textBoxSearchString);
            groupBoxLineMatchCriteria.Controls.Add(labelSearchString);
            groupBoxLineMatchCriteria.Controls.Add(checkBoxRegex);
            groupBoxLineMatchCriteria.Controls.Add(checkBoxCaseSensitive);
            groupBoxLineMatchCriteria.Location = new Point(18, 437);
            groupBoxLineMatchCriteria.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBoxLineMatchCriteria.Name = "groupBoxLineMatchCriteria";
            groupBoxLineMatchCriteria.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBoxLineMatchCriteria.Size = new Size(671, 135);
            groupBoxLineMatchCriteria.TabIndex = 19;
            groupBoxLineMatchCriteria.TabStop = false;
            groupBoxLineMatchCriteria.Text = "Line match criteria";
            // 
            // groupBoxColoring
            // 
            groupBoxColoring.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBoxColoring.Controls.Add(checkBoxNoBackground);
            groupBoxColoring.Controls.Add(checkBoxBold);
            groupBoxColoring.Controls.Add(checkBoxWordMatch);
            groupBoxColoring.Controls.Add(labelForgroundColor);
            groupBoxColoring.Controls.Add(colorBoxForeground);
            groupBoxColoring.Controls.Add(btnCustomForeColor);
            groupBoxColoring.Controls.Add(btnCustomBackColor);
            groupBoxColoring.Controls.Add(labelBackgroundColor);
            groupBoxColoring.Controls.Add(colorBoxBackground);
            groupBoxColoring.Location = new Point(18, 581);
            groupBoxColoring.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBoxColoring.Name = "groupBoxColoring";
            groupBoxColoring.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBoxColoring.Size = new Size(349, 286);
            groupBoxColoring.TabIndex = 20;
            groupBoxColoring.TabStop = false;
            groupBoxColoring.Text = "Coloring";
            // 
            // checkBoxNoBackground
            // 
            checkBoxNoBackground.AutoSize = true;
            checkBoxNoBackground.Enabled = false;
            checkBoxNoBackground.Location = new Point(141, 240);
            checkBoxNoBackground.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            checkBoxBold.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            checkBoxWordMatch.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            colorBoxForeground.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            colorBoxForeground.CustomColor = Color.Black;
            colorBoxForeground.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            colorBoxForeground.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            colorBoxForeground.FormattingEnabled = true;
            colorBoxForeground.Location = new Point(8, 63);
            colorBoxForeground.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            colorBoxForeground.Name = "colorBoxForeground";
            colorBoxForeground.Size = new Size(229, 24);
            colorBoxForeground.TabIndex = 5;
            colorBoxForeground.SelectedIndexChanged += ChangeToDirty;
            // 
            // colorBoxBackground
            // 
            colorBoxBackground.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            colorBoxBackground.CustomColor = Color.Black;
            colorBoxBackground.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            colorBoxBackground.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            colorBoxBackground.FormattingEnabled = true;
            colorBoxBackground.Location = new Point(9, 140);
            colorBoxBackground.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            colorBoxBackground.Name = "colorBoxBackground";
            colorBoxBackground.Size = new Size(229, 24);
            colorBoxBackground.TabIndex = 7;
            colorBoxBackground.SelectedIndexChanged += ChangeToDirty;
            // 
            // groupBoxActions
            // 
            groupBoxActions.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            groupBoxActions.Controls.Add(btnBookmarkComment);
            groupBoxActions.Controls.Add(btnSelectPlugin);
            groupBoxActions.Controls.Add(checkBoxPlugin);
            groupBoxActions.Controls.Add(checkBoxStopTail);
            groupBoxActions.Controls.Add(checkBoxBookmark);
            groupBoxActions.Controls.Add(checkBoxDontDirtyLed);
            groupBoxActions.Location = new Point(377, 581);
            groupBoxActions.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBoxActions.Name = "groupBoxActions";
            groupBoxActions.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBoxActions.Size = new Size(312, 195);
            groupBoxActions.TabIndex = 21;
            groupBoxActions.TabStop = false;
            groupBoxActions.Text = "Actions";
            // 
            // btnBookmarkComment
            // 
            btnBookmarkComment.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnBookmarkComment.Location = new Point(210, 69);
            btnBookmarkComment.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnBookmarkComment.Name = "btnBookmarkComment";
            btnBookmarkComment.Size = new Size(81, 31);
            btnBookmarkComment.TabIndex = 23;
            btnBookmarkComment.Text = "Text...";
            btnBookmarkComment.UseVisualStyleBackColor = true;
            btnBookmarkComment.Click += OnBtnBookmarkCommentClick;
            // 
            // btnSelectPlugin
            // 
            btnSelectPlugin.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnSelectPlugin.Location = new Point(210, 143);
            btnSelectPlugin.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            checkBoxPlugin.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            checkBoxStopTail.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            checkBoxStopTail.Name = "checkBoxStopTail";
            checkBoxStopTail.Size = new Size(109, 19);
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
            checkBoxBookmark.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            groupBox4.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
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
            groupBox4.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox4.Name = "groupBox4";
            groupBox4.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            groupBox4.Size = new Size(671, 129);
            groupBox4.TabIndex = 22;
            groupBox4.TabStop = false;
            groupBox4.Text = "Groups";
            // 
            // btnImportGroup
            // 
            btnImportGroup.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnImportGroup.Location = new Point(333, 26);
            btnImportGroup.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            btnExportGroup.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnExportGroup.Location = new Point(333, 75);
            btnExportGroup.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            btnMoveGroupDown.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnMoveGroupDown.Location = new Point(582, 75);
            btnMoveGroupDown.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            btnMoveGroupUp.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnMoveGroupUp.Location = new Point(499, 75);
            btnMoveGroupUp.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            labelAssignNamesToGroups.AutoSize = true;
            labelAssignNamesToGroups.Location = new Point(9, 88);
            labelAssignNamesToGroups.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelAssignNamesToGroups.Name = "labelAssignNamesToGroups";
            labelAssignNamesToGroups.Size = new Size(276, 15);
            labelAssignNamesToGroups.TabIndex = 4;
            labelAssignNamesToGroups.Text = "You can assign groups to file names in the settings.";
            // 
            // btnCopyGroup
            // 
            btnCopyGroup.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnCopyGroup.Location = new Point(582, 26);
            btnCopyGroup.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            btnDeleteGroup.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnDeleteGroup.Location = new Point(499, 26);
            btnDeleteGroup.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            btnNewGroup.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnNewGroup.Location = new Point(416, 26);
            btnNewGroup.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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
            comboBoxGroups.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            comboBoxGroups.DisplayMember = "GroupName";
            comboBoxGroups.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            comboBoxGroups.Location = new Point(14, 32);
            comboBoxGroups.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            comboBoxGroups.Name = "comboBoxGroups";
            comboBoxGroups.Size = new Size(311, 24);
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
            pnlBackground.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlBackground.Location = new Point(0, 0);
            pnlBackground.Name = "pnlBackground";
            pnlBackground.Size = new Size(708, 895);
            pnlBackground.TabIndex = 23;
            // 
            // HighlightDialog
            // 
            AcceptButton = btnOk;
            CancelButton = btnCancel;
            ClientSize = new Size(708, 895);
            Controls.Add(pnlBackground);
            DoubleBuffered = true;
            helpProvider.SetHelpKeyword(this, "Highlighting.htm");
            helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
            helpProvider.SetHelpString(this, "");
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(660, 934);
            Name = "HighlightDialog";
            helpProvider.SetShowHelp(this, true);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
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
}