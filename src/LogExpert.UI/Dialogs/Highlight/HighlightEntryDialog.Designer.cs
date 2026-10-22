using LogExpert.UI.Controls;

namespace LogExpert.UI.Dialogs.Highlight;

partial class HighlightEntryDialog
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
        _toolTip = new ToolTip(components);
        _previewLabel = new Label();
        _tabControl = new TabControl();
        _tabLineMatch = new TabPage();
        _labelSearchString = new Label();
        _textBoxSearchString = new TextBox();
        _checkBoxCaseSensitive = new CheckBox();
        _checkBoxRegex = new CheckBox();
        _tabColoring = new TabPage();
        _labelForeColor = new Label();
        _colorBoxForeground = new ColorComboBox();
        _btnCustomForeColor = new Button();
        _labelBackColor = new Label();
        _colorBoxBackground = new ColorComboBox();
        _btnCustomBackColor = new Button();
        _checkBoxBold = new CheckBox();
        _checkBoxWordMatch = new CheckBox();
        _checkBoxNoBackground = new CheckBox();
        _tabActions = new TabPage();
        _checkBoxBookmark = new CheckBox();
        _btnBookmarkComment = new Button();
        _checkBoxStopTail = new CheckBox();
        _checkBoxDontDirtyLed = new CheckBox();
        _checkBoxHideLine = new CheckBox();
        _checkBoxPlugin = new CheckBox();
        _btnSelectPlugin = new Button();
        _checkBoxAlertOnHit = new CheckBox();
        _labelSoundFile = new Label();
        _textBoxSoundFile = new TextBox();
        _btnBrowseSoundFile = new Button();
        _labelCooldown = new Label();
        _numericCooldownSeconds = new NumericUpDown();
        _labelCooldownSeconds = new Label();
        _btnOk = new Button();
        _btnCancel = new Button();
        _tabControl.SuspendLayout();
        _tabLineMatch.SuspendLayout();
        _tabColoring.SuspendLayout();
        _tabActions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_numericCooldownSeconds).BeginInit();
        SuspendLayout();
        // 
        // _previewLabel
        // 
        _previewLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _previewLabel.AutoEllipsis = true;
        _previewLabel.BorderStyle = BorderStyle.FixedSingle;
        _previewLabel.Location = new Point(12, 12);
        _previewLabel.Name = "_previewLabel";
        _previewLabel.Padding = new Padding(6, 0, 6, 0);
        _previewLabel.Size = new Size(560, 34);
        _previewLabel.TabIndex = 0;
        _previewLabel.Text = "(preview)";
        _previewLabel.TextAlign = ContentAlignment.MiddleLeft;
        _previewLabel.UseMnemonic = false;
        // 
        // _tabControl
        // 
        _tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _tabControl.Controls.Add(_tabLineMatch);
        _tabControl.Controls.Add(_tabColoring);
        _tabControl.Controls.Add(_tabActions);
        _tabControl.Location = new Point(12, 54);
        _tabControl.Name = "_tabControl";
        _tabControl.SelectedIndex = 0;
        _tabControl.Size = new Size(560, 320);
        _tabControl.TabIndex = 1;
        // 
        // _tabLineMatch
        // 
        _tabLineMatch.Controls.Add(_labelSearchString);
        _tabLineMatch.Controls.Add(_textBoxSearchString);
        _tabLineMatch.Controls.Add(_checkBoxCaseSensitive);
        _tabLineMatch.Controls.Add(_checkBoxRegex);
        _tabLineMatch.Location = new Point(4, 24);
        _tabLineMatch.Name = "_tabLineMatch";
        _tabLineMatch.Padding = new Padding(8);
        _tabLineMatch.Size = new Size(552, 292);
        _tabLineMatch.TabIndex = 0;
        _tabLineMatch.Text = "Line Match";
        _tabLineMatch.UseVisualStyleBackColor = true;
        // 
        // _labelSearchString
        // 
        _labelSearchString.AutoSize = true;
        _labelSearchString.Location = new Point(12, 16);
        _labelSearchString.Name = "_labelSearchString";
        _labelSearchString.Size = new Size(0, 15);
        _labelSearchString.TabIndex = 0;
        // 
        // _textBoxSearchString
        // 
        _textBoxSearchString.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _textBoxSearchString.Location = new Point(12, 36);
        _textBoxSearchString.Name = "_textBoxSearchString";
        _textBoxSearchString.Size = new Size(520, 23);
        _textBoxSearchString.TabIndex = 1;
        _textBoxSearchString.TextChanged += OnSearchStringChanged;
        _textBoxSearchString.MouseUp += OnSearchStringMouseUp;
        // 
        // _checkBoxCaseSensitive
        // 
        _checkBoxCaseSensitive.AutoSize = true;
        _checkBoxCaseSensitive.Location = new Point(12, 75);
        _checkBoxCaseSensitive.Name = "_checkBoxCaseSensitive";
        _checkBoxCaseSensitive.Size = new Size(100, 19);
        _checkBoxCaseSensitive.TabIndex = 2;
        _checkBoxCaseSensitive.Text = "Case Sensitive";
        _checkBoxCaseSensitive.UseVisualStyleBackColor = true;
        // 
        // _checkBoxRegex
        // 
        _checkBoxRegex.AutoSize = true;
        _checkBoxRegex.Location = new Point(180, 75);
        _checkBoxRegex.Name = "_checkBoxRegex";
        _checkBoxRegex.Size = new Size(57, 19);
        _checkBoxRegex.TabIndex = 3;
        _checkBoxRegex.Text = "Regex";
        _checkBoxRegex.UseVisualStyleBackColor = true;
        _checkBoxRegex.MouseUp += OnRegexMouseUp;
        // 
        // _tabColoring
        // 
        _tabColoring.Controls.Add(_labelForeColor);
        _tabColoring.Controls.Add(_colorBoxForeground);
        _tabColoring.Controls.Add(_btnCustomForeColor);
        _tabColoring.Controls.Add(_labelBackColor);
        _tabColoring.Controls.Add(_colorBoxBackground);
        _tabColoring.Controls.Add(_btnCustomBackColor);
        _tabColoring.Controls.Add(_checkBoxBold);
        _tabColoring.Controls.Add(_checkBoxWordMatch);
        _tabColoring.Controls.Add(_checkBoxNoBackground);
        _tabColoring.Location = new Point(4, 24);
        _tabColoring.Name = "_tabColoring";
        _tabColoring.Padding = new Padding(8);
        _tabColoring.Size = new Size(552, 292);
        _tabColoring.TabIndex = 1;
        _tabColoring.Text = "Coloring";
        _tabColoring.UseVisualStyleBackColor = true;
        // 
        // _labelForeColor
        // 
        _labelForeColor.AutoSize = true;
        _labelForeColor.Location = new Point(12, 16);
        _labelForeColor.Name = "_labelForeColor";
        _labelForeColor.Size = new Size(0, 15);
        _labelForeColor.TabIndex = 0;
        // 
        // _colorBoxForeground
        // 
        _colorBoxForeground.CustomColor = Color.Black;
        _colorBoxForeground.DrawMode = DrawMode.OwnerDrawFixed;
        _colorBoxForeground.DropDownStyle = ComboBoxStyle.DropDownList;
        _colorBoxForeground.FormattingEnabled = true;
        _colorBoxForeground.Items.AddRange(new object[] { Color.Black, Color.Black, Color.White, Color.Gray, Color.DarkGray, Color.Blue, Color.LightBlue, Color.DarkBlue, Color.Green, Color.LightGreen, Color.DarkGreen, Color.Olive, Color.Red, Color.Pink, Color.Purple, Color.IndianRed, Color.DarkCyan, Color.Yellow });
        _colorBoxForeground.Location = new Point(12, 36);
        _colorBoxForeground.Name = "_colorBoxForeground";
        _colorBoxForeground.Size = new Size(180, 24);
        _colorBoxForeground.TabIndex = 1;
        _colorBoxForeground.SelectedIndexChanged += OnColorOrStyleChanged;
        // 
        // _btnCustomForeColor
        // 
        _btnCustomForeColor.AutoSize = true;
        _btnCustomForeColor.Location = new Point(200, 33);
        _btnCustomForeColor.Name = "_btnCustomForeColor";
        _btnCustomForeColor.Size = new Size(95, 30);
        _btnCustomForeColor.TabIndex = 2;
        _btnCustomForeColor.Text = "Fore Color";
        _btnCustomForeColor.UseVisualStyleBackColor = true;
        _btnCustomForeColor.Click += OnBtnCustomForeColorClicked;
        // 
        // _labelBackColor
        // 
        _labelBackColor.AutoSize = true;
        _labelBackColor.Location = new Point(12, 80);
        _labelBackColor.Name = "_labelBackColor";
        _labelBackColor.Size = new Size(0, 15);
        _labelBackColor.TabIndex = 3;
        // 
        // _colorBoxBackground
        // 
        _colorBoxBackground.CustomColor = Color.Black;
        _colorBoxBackground.DrawMode = DrawMode.OwnerDrawFixed;
        _colorBoxBackground.DropDownStyle = ComboBoxStyle.DropDownList;
        _colorBoxBackground.FormattingEnabled = true;
        _colorBoxBackground.Items.AddRange(new object[] { Color.Black, Color.Black, Color.White, Color.Gray, Color.DarkGray, Color.Blue, Color.LightBlue, Color.DarkBlue, Color.Green, Color.LightGreen, Color.DarkGreen, Color.Olive, Color.Red, Color.Pink, Color.Purple, Color.IndianRed, Color.DarkCyan, Color.Yellow });
        _colorBoxBackground.Location = new Point(12, 100);
        _colorBoxBackground.Name = "_colorBoxBackground";
        _colorBoxBackground.Size = new Size(180, 24);
        _colorBoxBackground.TabIndex = 4;
        _colorBoxBackground.SelectedIndexChanged += OnColorOrStyleChanged;
        // 
        // _btnCustomBackColor
        // 
        _btnCustomBackColor.AutoSize = true;
        _btnCustomBackColor.Location = new Point(200, 97);
        _btnCustomBackColor.Name = "_btnCustomBackColor";
        _btnCustomBackColor.Size = new Size(95, 30);
        _btnCustomBackColor.TabIndex = 5;
        _btnCustomBackColor.Text = "Back Color";
        _btnCustomBackColor.UseVisualStyleBackColor = true;
        _btnCustomBackColor.Click += OnBtnCustomBackColorClicked;
        // 
        // _checkBoxBold
        // 
        _checkBoxBold.AutoSize = true;
        _checkBoxBold.Location = new Point(12, 150);
        _checkBoxBold.Name = "_checkBoxBold";
        _checkBoxBold.Size = new Size(50, 19);
        _checkBoxBold.TabIndex = 6;
        _checkBoxBold.Text = "Bold";
        _checkBoxBold.UseVisualStyleBackColor = true;
        _checkBoxBold.CheckedChanged += OnColorOrStyleChanged;
        // 
        // _checkBoxWordMatch
        // 
        _checkBoxWordMatch.AutoSize = true;
        _checkBoxWordMatch.Location = new Point(12, 180);
        _checkBoxWordMatch.Name = "_checkBoxWordMatch";
        _checkBoxWordMatch.Size = new Size(89, 19);
        _checkBoxWordMatch.TabIndex = 7;
        _checkBoxWordMatch.Text = "Word Mode";
        _checkBoxWordMatch.UseVisualStyleBackColor = true;
        _checkBoxWordMatch.CheckedChanged += OnWordMatchChanged;
        // 
        // _checkBoxNoBackground
        // 
        _checkBoxNoBackground.AutoSize = true;
        _checkBoxNoBackground.Location = new Point(180, 180);
        _checkBoxNoBackground.Name = "_checkBoxNoBackground";
        _checkBoxNoBackground.Size = new Size(109, 19);
        _checkBoxNoBackground.TabIndex = 8;
        _checkBoxNoBackground.Text = "No Background";
        _checkBoxNoBackground.UseVisualStyleBackColor = true;
        _checkBoxNoBackground.CheckedChanged += OnNoBackgroundChanged;
        // 
        // _tabActions
        // 
        _tabActions.Controls.Add(_checkBoxBookmark);
        _tabActions.Controls.Add(_btnBookmarkComment);
        _tabActions.Controls.Add(_checkBoxStopTail);
        _tabActions.Controls.Add(_checkBoxDontDirtyLed);
        _tabActions.Controls.Add(_checkBoxPlugin);
        _tabActions.Controls.Add(_btnSelectPlugin);
        _tabActions.Controls.Add(_checkBoxAlertOnHit);
        _tabActions.Controls.Add(_labelSoundFile);
        _tabActions.Controls.Add(_textBoxSoundFile);
        _tabActions.Controls.Add(_btnBrowseSoundFile);
        _tabActions.Controls.Add(_labelCooldown);
        _tabActions.Controls.Add(_numericCooldownSeconds);
        _tabActions.Controls.Add(_labelCooldownSeconds);
        _tabActions.Controls.Add(_checkBoxHideLine);
        _tabActions.Location = new Point(4, 24);
        _tabActions.Name = "_tabActions";
        _tabActions.Padding = new Padding(8);
        _tabActions.Size = new Size(552, 292);
        _tabActions.TabIndex = 2;
        _tabActions.Text = "Actions";
        _tabActions.UseVisualStyleBackColor = true;
        // 
        // _checkBoxBookmark
        // 
        _checkBoxBookmark.AutoSize = true;
        _checkBoxBookmark.Location = new Point(12, 16);
        _checkBoxBookmark.Name = "_checkBoxBookmark";
        _checkBoxBookmark.Size = new Size(80, 19);
        _checkBoxBookmark.TabIndex = 0;
        _checkBoxBookmark.Text = "Bookmark";
        _checkBoxBookmark.UseVisualStyleBackColor = true;
        _checkBoxBookmark.CheckedChanged += OnBookmarkChanged;
        // 
        // _btnBookmarkComment
        // 
        _btnBookmarkComment.AutoSize = true;
        _btnBookmarkComment.Location = new Point(200, 12);
        _btnBookmarkComment.Name = "_btnBookmarkComment";
        _btnBookmarkComment.Size = new Size(120, 28);
        _btnBookmarkComment.TabIndex = 1;
        _btnBookmarkComment.Text = "Bookmark Comment";
        _btnBookmarkComment.UseVisualStyleBackColor = true;
        _btnBookmarkComment.Click += OnBookmarkCommentClick;
        // 
        // _checkBoxStopTail
        // 
        _checkBoxStopTail.AutoSize = true;
        _checkBoxStopTail.Location = new Point(12, 48);
        _checkBoxStopTail.Name = "_checkBoxStopTail";
        _checkBoxStopTail.Size = new Size(71, 19);
        _checkBoxStopTail.TabIndex = 2;
        _checkBoxStopTail.Text = "Stop Tail";
        _checkBoxStopTail.UseVisualStyleBackColor = true;
        // 
        // _checkBoxDontDirtyLed
        // 
        _checkBoxDontDirtyLed.AutoSize = true;
        _checkBoxDontDirtyLed.Location = new Point(12, 78);
        _checkBoxDontDirtyLed.Name = "_checkBoxDontDirtyLed";
        _checkBoxDontDirtyLed.Size = new Size(102, 19);
        _checkBoxDontDirtyLed.TabIndex = 3;
        _checkBoxDontDirtyLed.Text = "Dont Dirty Led";
        _checkBoxDontDirtyLed.UseVisualStyleBackColor = true;
        // 
        // _checkBoxHideLine
        // 
        _checkBoxHideLine.AutoSize = true;
        _checkBoxHideLine.Location = new Point(12, 250);
        _checkBoxHideLine.Name = "_checkBoxHideLine";
        _checkBoxHideLine.Size = new Size(74, 19);
        _checkBoxHideLine.TabIndex = 13;
        _checkBoxHideLine.Text = "Hide line";
        _checkBoxHideLine.UseVisualStyleBackColor = true;
        // 
        // _checkBoxPlugin
        // 
        _checkBoxPlugin.AutoSize = true;
        _checkBoxPlugin.Location = new Point(12, 108);
        _checkBoxPlugin.Name = "_checkBoxPlugin";
        _checkBoxPlugin.Size = new Size(60, 19);
        _checkBoxPlugin.TabIndex = 4;
        _checkBoxPlugin.Text = "Plugin";
        _checkBoxPlugin.UseVisualStyleBackColor = true;
        _checkBoxPlugin.CheckedChanged += OnPluginChanged;
        // 
        // _btnSelectPlugin
        // 
        _btnSelectPlugin.AutoSize = true;
        _btnSelectPlugin.Location = new Point(200, 104);
        _btnSelectPlugin.Name = "_btnSelectPlugin";
        _btnSelectPlugin.Size = new Size(120, 28);
        _btnSelectPlugin.TabIndex = 5;
        _btnSelectPlugin.Text = "Select Plugin";
        _btnSelectPlugin.UseVisualStyleBackColor = true;
        _btnSelectPlugin.Click += OnSelectPluginClick;
        // 
        // _checkBoxAlertOnHit
        // 
        _checkBoxAlertOnHit.AutoSize = true;
        _checkBoxAlertOnHit.Location = new Point(12, 148);
        _checkBoxAlertOnHit.Name = "_checkBoxAlertOnHit";
        _checkBoxAlertOnHit.Size = new Size(87, 19);
        _checkBoxAlertOnHit.TabIndex = 6;
        _checkBoxAlertOnHit.Text = "Alert on Hit";
        _checkBoxAlertOnHit.UseVisualStyleBackColor = true;
        _checkBoxAlertOnHit.CheckedChanged += OnAlertOnHitChanged;
        // 
        // _labelSoundFile
        // 
        _labelSoundFile.AutoSize = true;
        _labelSoundFile.Location = new Point(30, 180);
        _labelSoundFile.Name = "_labelSoundFile";
        _labelSoundFile.Size = new Size(0, 15);
        _labelSoundFile.TabIndex = 7;
        // 
        // _textBoxSoundFile
        // 
        _textBoxSoundFile.Location = new Point(110, 176);
        _textBoxSoundFile.Name = "_textBoxSoundFile";
        _textBoxSoundFile.Size = new Size(210, 23);
        _textBoxSoundFile.TabIndex = 8;
        // 
        // _btnBrowseSoundFile
        // 
        _btnBrowseSoundFile.AutoSize = true;
        _btnBrowseSoundFile.Location = new Point(326, 174);
        _btnBrowseSoundFile.Name = "_btnBrowseSoundFile";
        _btnBrowseSoundFile.Size = new Size(90, 28);
        _btnBrowseSoundFile.TabIndex = 9;
        _btnBrowseSoundFile.Text = "Select Sound File";
        _btnBrowseSoundFile.UseVisualStyleBackColor = true;
        _btnBrowseSoundFile.Click += OnBrowseSoundFileClick;
        // 
        // _labelCooldown
        // 
        _labelCooldown.AutoSize = true;
        _labelCooldown.Location = new Point(30, 215);
        _labelCooldown.Name = "_labelCooldown";
        _labelCooldown.Size = new Size(0, 15);
        _labelCooldown.TabIndex = 10;
        // 
        // _numericCooldownSeconds
        // 
        _numericCooldownSeconds.Location = new Point(110, 211);
        _numericCooldownSeconds.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
        _numericCooldownSeconds.Name = "_numericCooldownSeconds";
        _numericCooldownSeconds.Size = new Size(70, 23);
        _numericCooldownSeconds.TabIndex = 11;
        _numericCooldownSeconds.Value = new decimal(new int[] { 2, 0, 0, 0 });
        // 
        // _labelCooldownSeconds
        // 
        _labelCooldownSeconds.AutoSize = true;
        _labelCooldownSeconds.Location = new Point(190, 215);
        _labelCooldownSeconds.Name = "_labelCooldownSeconds";
        _labelCooldownSeconds.Size = new Size(0, 15);
        _labelCooldownSeconds.TabIndex = 12;
        // 
        // _btnOk
        // 
        _btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnOk.Location = new Point(378, 385);
        _btnOk.Name = "_btnOk";
        _btnOk.Size = new Size(90, 30);
        _btnOk.TabIndex = 2;
        _btnOk.Text = "Ok";
        _btnOk.UseVisualStyleBackColor = true;
        _btnOk.Click += OnOkClick;
        // 
        // _btnCancel
        // 
        _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnCancel.DialogResult = DialogResult.Cancel;
        _btnCancel.Location = new Point(478, 385);
        _btnCancel.Name = "_btnCancel";
        _btnCancel.Size = new Size(90, 30);
        _btnCancel.TabIndex = 3;
        _btnCancel.Text = "Cancel";
        _btnCancel.UseVisualStyleBackColor = true;
        // 
        // HighlightEntryDialog
        // 
        AcceptButton = _btnOk;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = _btnCancel;
        ClientSize = new Size(584, 427);
        Controls.Add(_previewLabel);
        Controls.Add(_tabControl);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(600, 466);
        Name = "HighlightEntryDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "HighlightEntryDialog";
        _tabControl.ResumeLayout(false);
        _tabLineMatch.ResumeLayout(false);
        _tabLineMatch.PerformLayout();
        _tabColoring.ResumeLayout(false);
        _tabColoring.PerformLayout();
        _tabActions.ResumeLayout(false);
        _tabActions.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_numericCooldownSeconds).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Label _previewLabel;
    private System.Windows.Forms.TabControl _tabControl;
    private System.Windows.Forms.TabPage _tabLineMatch;
    private System.Windows.Forms.TabPage _tabColoring;
    private System.Windows.Forms.TabPage _tabActions;
    private System.Windows.Forms.Label _labelSearchString;
    private System.Windows.Forms.TextBox _textBoxSearchString;
    private System.Windows.Forms.CheckBox _checkBoxCaseSensitive;
    private System.Windows.Forms.CheckBox _checkBoxRegex;
    private System.Windows.Forms.Label _labelForeColor;
    private LogExpert.UI.Controls.ColorComboBox _colorBoxForeground;
    private System.Windows.Forms.Button _btnCustomForeColor;
    private System.Windows.Forms.Label _labelBackColor;
    private LogExpert.UI.Controls.ColorComboBox _colorBoxBackground;
    private System.Windows.Forms.Button _btnCustomBackColor;
    private System.Windows.Forms.CheckBox _checkBoxBold;
    private System.Windows.Forms.CheckBox _checkBoxWordMatch;
    private System.Windows.Forms.CheckBox _checkBoxNoBackground;
    private System.Windows.Forms.CheckBox _checkBoxBookmark;
    private System.Windows.Forms.Button _btnBookmarkComment;
    private System.Windows.Forms.CheckBox _checkBoxStopTail;
    private System.Windows.Forms.CheckBox _checkBoxDontDirtyLed;
    private System.Windows.Forms.CheckBox _checkBoxHideLine;
    private System.Windows.Forms.CheckBox _checkBoxPlugin;
    private System.Windows.Forms.Button _btnSelectPlugin;
    private System.Windows.Forms.CheckBox _checkBoxAlertOnHit;
    private System.Windows.Forms.Label _labelSoundFile;
    private System.Windows.Forms.TextBox _textBoxSoundFile;
    private System.Windows.Forms.Button _btnBrowseSoundFile;
    private System.Windows.Forms.Label _labelCooldown;
    private System.Windows.Forms.NumericUpDown _numericCooldownSeconds;
    private System.Windows.Forms.Label _labelCooldownSeconds;
    private System.Windows.Forms.Button _btnOk;
    private System.Windows.Forms.Button _btnCancel;
    private System.Windows.Forms.ToolTip _toolTip;
}
