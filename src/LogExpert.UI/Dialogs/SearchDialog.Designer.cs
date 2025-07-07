namespace LogExpert.Dialogs;

partial class SearchDialog
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
        this.buttonOk = new System.Windows.Forms.Button();
        this.buttonCancel = new System.Windows.Forms.Button();
        this.labelSearchFor = new System.Windows.Forms.Label();
        this.checkBoxCaseSensitive = new System.Windows.Forms.CheckBox();
        this.checkBoxRegex = new System.Windows.Forms.CheckBox();
        this.buttonRegexHelper = new System.Windows.Forms.Button();
        this.radioButtonFromTop = new System.Windows.Forms.RadioButton();
        this.radioButtonFromSelected = new System.Windows.Forms.RadioButton();
        this.groupBoxSearchStart = new System.Windows.Forms.GroupBox();
        this.groupBoxOptions = new System.Windows.Forms.GroupBox();
        this.groupBoxDirection = new System.Windows.Forms.GroupBox();
        this.radioButtonBackward = new System.Windows.Forms.RadioButton();
        this.radioButtonForward = new System.Windows.Forms.RadioButton();
        this.comboBoxSearchFor = new System.Windows.Forms.ComboBox();
        this.helpProvider1 = new System.Windows.Forms.HelpProvider();
        this.groupBoxSearchStart.SuspendLayout();
        this.groupBoxOptions.SuspendLayout();
        this.groupBoxDirection.SuspendLayout();
        this.SuspendLayout();
        // 
        // buttonOk
        // 
        this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonOk.Location = new System.Drawing.Point(317, 215);
        this.buttonOk.Name = "buttonOk";
        this.buttonOk.Size = new System.Drawing.Size(75, 23);
        this.buttonOk.TabIndex = 5;
        this.buttonOk.Text = "OK";
        this.buttonOk.UseVisualStyleBackColor = true;
        this.buttonOk.Click += new System.EventHandler(this.OnButtonOkClick);
        // 
        // buttonCancel
        // 
        this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.buttonCancel.Location = new System.Drawing.Point(398, 215);
        this.buttonCancel.Name = "buttonCancel";
        this.buttonCancel.Size = new System.Drawing.Size(75, 23);
        this.buttonCancel.TabIndex = 6;
        this.buttonCancel.Text = "Cancel";
        this.buttonCancel.UseVisualStyleBackColor = true;
        this.buttonCancel.Click += new System.EventHandler(this.OnButtonCancelClick);
        // 
        // labelSearchFor
        // 
        this.labelSearchFor.AutoSize = true;
        this.labelSearchFor.Location = new System.Drawing.Point(10, 9);
        this.labelSearchFor.Name = "labelSearchFor";
        this.labelSearchFor.Size = new System.Drawing.Size(87, 20);
        this.labelSearchFor.TabIndex = 0;
        this.labelSearchFor.Text = "&Search for:";
        // 
        // checkBoxCaseSensitive
        // 
        this.checkBoxCaseSensitive.AutoSize = true;
        this.checkBoxCaseSensitive.Location = new System.Drawing.Point(6, 19);
        this.checkBoxCaseSensitive.Name = "checkBoxCaseSensitive";
        this.checkBoxCaseSensitive.Size = new System.Drawing.Size(137, 24);
        this.checkBoxCaseSensitive.TabIndex = 4;
        this.checkBoxCaseSensitive.Text = "&Case sensitive";
        this.checkBoxCaseSensitive.UseVisualStyleBackColor = true;
        // 
        // checkBoxRegex
        // 
        this.checkBoxRegex.AutoSize = true;
        this.checkBoxRegex.Location = new System.Drawing.Point(6, 43);
        this.checkBoxRegex.Name = "checkBoxRegex";
        this.checkBoxRegex.Size = new System.Drawing.Size(171, 24);
        this.checkBoxRegex.TabIndex = 5;
        this.checkBoxRegex.Text = "&Regular expression";
        this.checkBoxRegex.UseVisualStyleBackColor = true;
        // 
        // buttonRegexHelper
        // 
        this.buttonRegexHelper.Location = new System.Drawing.Point(6, 72);
        this.buttonRegexHelper.Name = "buttonRegexHelper";
        this.buttonRegexHelper.Size = new System.Drawing.Size(84, 26);
        this.buttonRegexHelper.TabIndex = 3;
        this.buttonRegexHelper.Text = "Regex-&Helper";
        this.buttonRegexHelper.UseVisualStyleBackColor = true;
        this.buttonRegexHelper.Click += new System.EventHandler(this.OnButtonRegexClick);
        // 
        // radioButtonFromTop
        // 
        this.radioButtonFromTop.AutoSize = true;
        this.radioButtonFromTop.Location = new System.Drawing.Point(6, 19);
        this.radioButtonFromTop.Name = "radioButtonFromTop";
        this.radioButtonFromTop.Size = new System.Drawing.Size(98, 24);
        this.radioButtonFromTop.TabIndex = 7;
        this.radioButtonFromTop.TabStop = true;
        this.radioButtonFromTop.Text = "From top";
        this.radioButtonFromTop.UseVisualStyleBackColor = true;
        // 
        // radioButtonFromSelected
        // 
        this.radioButtonFromSelected.AutoSize = true;
        this.radioButtonFromSelected.Location = new System.Drawing.Point(6, 42);
        this.radioButtonFromSelected.Name = "radioButtonFromSelected";
        this.radioButtonFromSelected.Size = new System.Drawing.Size(163, 24);
        this.radioButtonFromSelected.TabIndex = 8;
        this.radioButtonFromSelected.TabStop = true;
        this.radioButtonFromSelected.Text = "From selected line";
        this.radioButtonFromSelected.UseVisualStyleBackColor = true;
        // 
        // groupBoxSearchStart
        // 
        this.groupBoxSearchStart.Controls.Add(this.radioButtonFromTop);
        this.groupBoxSearchStart.Controls.Add(this.radioButtonFromSelected);
        this.groupBoxSearchStart.Location = new System.Drawing.Point(12, 74);
        this.groupBoxSearchStart.Name = "groupBoxSearchStart";
        this.groupBoxSearchStart.Size = new System.Drawing.Size(179, 79);
        this.groupBoxSearchStart.TabIndex = 1;
        this.groupBoxSearchStart.TabStop = false;
        this.groupBoxSearchStart.Text = "Search start";
        // 
        // groupBoxOptions
        // 
        this.groupBoxOptions.Controls.Add(this.checkBoxCaseSensitive);
        this.groupBoxOptions.Controls.Add(this.checkBoxRegex);
        this.groupBoxOptions.Controls.Add(this.buttonRegexHelper);
        this.groupBoxOptions.Location = new System.Drawing.Point(198, 74);
        this.groupBoxOptions.Name = "groupBoxOptions";
        this.groupBoxOptions.Size = new System.Drawing.Size(275, 104);
        this.groupBoxOptions.TabIndex = 2;
        this.groupBoxOptions.TabStop = false;
        this.groupBoxOptions.Text = "Options";
        // 
        // groupBoxDirection
        // 
        this.groupBoxDirection.Controls.Add(this.radioButtonBackward);
        this.groupBoxDirection.Controls.Add(this.radioButtonForward);
        this.groupBoxDirection.Location = new System.Drawing.Point(13, 159);
        this.groupBoxDirection.Name = "groupBoxDirection";
        this.groupBoxDirection.Size = new System.Drawing.Size(122, 79);
        this.groupBoxDirection.TabIndex = 4;
        this.groupBoxDirection.TabStop = false;
        this.groupBoxDirection.Text = "Direction";
        // 
        // radioButtonBackward
        // 
        this.radioButtonBackward.AutoSize = true;
        this.radioButtonBackward.Location = new System.Drawing.Point(7, 44);
        this.radioButtonBackward.Name = "radioButtonBackward";
        this.radioButtonBackward.Size = new System.Drawing.Size(104, 24);
        this.radioButtonBackward.TabIndex = 1;
        this.radioButtonBackward.TabStop = true;
        this.radioButtonBackward.Text = "Backward";
        this.radioButtonBackward.UseVisualStyleBackColor = true;
        // 
        // radioButtonForward
        // 
        this.radioButtonForward.AutoSize = true;
        this.radioButtonForward.Location = new System.Drawing.Point(7, 20);
        this.radioButtonForward.Name = "radioButtonForward";
        this.radioButtonForward.Size = new System.Drawing.Size(92, 24);
        this.radioButtonForward.TabIndex = 0;
        this.radioButtonForward.TabStop = true;
        this.radioButtonForward.Text = "Forward";
        this.radioButtonForward.UseVisualStyleBackColor = true;
        // 
        // comboBoxSearchFor
        // 
        this.comboBoxSearchFor.FormattingEnabled = true;
        this.comboBoxSearchFor.Location = new System.Drawing.Point(13, 34);
        this.comboBoxSearchFor.Name = "comboBoxSearchFor";
        this.comboBoxSearchFor.Size = new System.Drawing.Size(460, 28);
        this.comboBoxSearchFor.TabIndex = 0;
        // 
        // helpProvider1
        // 
        this.helpProvider1.HelpNamespace = "LogExpert.chm";
        // 
        // SearchDialog
        // 
        this.AcceptButton = this.buttonOk;
        this.CancelButton = this.buttonCancel;
        this.ClientSize = new System.Drawing.Size(488, 250);
        this.Controls.Add(this.comboBoxSearchFor);
        this.Controls.Add(this.groupBoxDirection);
        this.Controls.Add(this.groupBoxOptions);
        this.Controls.Add(this.groupBoxSearchStart);
        this.Controls.Add(this.labelSearchFor);
        this.Controls.Add(this.buttonCancel);
        this.Controls.Add(this.buttonOk);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.helpProvider1.SetHelpKeyword(this, "Search and Navigation.htm");
        this.helpProvider1.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "SearchDialog";
        this.helpProvider1.SetShowHelp(this, true);
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Search";
        this.groupBoxSearchStart.ResumeLayout(false);
        this.groupBoxSearchStart.PerformLayout();
        this.groupBoxOptions.ResumeLayout(false);
        this.groupBoxOptions.PerformLayout();
        this.groupBoxDirection.ResumeLayout(false);
        this.groupBoxDirection.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();

}

#endregion

private System.Windows.Forms.Button buttonOk;
private System.Windows.Forms.Button buttonCancel;
private System.Windows.Forms.Label labelSearchFor;
private System.Windows.Forms.CheckBox checkBoxCaseSensitive;
private System.Windows.Forms.CheckBox checkBoxRegex;
private System.Windows.Forms.Button buttonRegexHelper;
private System.Windows.Forms.RadioButton radioButtonFromTop;
private System.Windows.Forms.RadioButton radioButtonFromSelected;
private System.Windows.Forms.GroupBox groupBoxSearchStart;
private System.Windows.Forms.GroupBox groupBoxOptions;
private System.Windows.Forms.GroupBox groupBoxDirection;
private System.Windows.Forms.RadioButton radioButtonBackward;
private System.Windows.Forms.RadioButton radioButtonForward;
private System.Windows.Forms.ComboBox comboBoxSearchFor;
private System.Windows.Forms.HelpProvider helpProvider1;
}