namespace LogExpert.UI.Dialogs;

partial class RegexHelperDialog
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
        this.buttonCancel = new System.Windows.Forms.Button();
        this.buttonOk = new System.Windows.Forms.Button();
        this.labelRegex = new System.Windows.Forms.Label();
        this.labelTestText = new System.Windows.Forms.Label();
        this.labelMatches = new System.Windows.Forms.Label();
        this.textBoxMatches = new System.Windows.Forms.TextBox();
        this.checkBoxCaseSensitive = new System.Windows.Forms.CheckBox();
        this.comboBoxRegex = new System.Windows.Forms.ComboBox();
        this.comboBoxTestText = new System.Windows.Forms.ComboBox();
        this.helpProvider1 = new System.Windows.Forms.HelpProvider();
        this.buttonHelp = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // buttonCancel
        // 
        this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.buttonCancel.Location = new System.Drawing.Point(365, 371);
        this.buttonCancel.Name = "buttonCancel";
        this.buttonCancel.Size = new System.Drawing.Size(75, 23);
        this.buttonCancel.TabIndex = 0;
        this.buttonCancel.Text = "Cancel";
        this.buttonCancel.UseVisualStyleBackColor = true;
        // 
        // buttonOk
        // 
        this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonOk.Location = new System.Drawing.Point(284, 371);
        this.buttonOk.Name = "buttonOk";
        this.buttonOk.Size = new System.Drawing.Size(75, 23);
        this.buttonOk.TabIndex = 1;
        this.buttonOk.Text = "OK";
        this.buttonOk.UseVisualStyleBackColor = true;
        this.buttonOk.Click += new System.EventHandler(this.OnButtonOkClick);
        // 
        // labelRegex
        // 
        this.labelRegex.AutoSize = true;
        this.labelRegex.Location = new System.Drawing.Point(14, 14);
        this.labelRegex.Name = "labelRegex";
        this.labelRegex.Size = new System.Drawing.Size(151, 20);
        this.labelRegex.TabIndex = 3;
        this.labelRegex.Text = "Regular Expression:";
        // 
        // labelTestText
        // 
        this.labelTestText.AutoSize = true;
        this.labelTestText.Location = new System.Drawing.Point(14, 98);
        this.labelTestText.Name = "labelTestText";
        this.labelTestText.Size = new System.Drawing.Size(74, 20);
        this.labelTestText.TabIndex = 5;
        this.labelTestText.Text = "Test text:";
        // 
        // labelMatches
        // 
        this.labelMatches.AutoSize = true;
        this.labelMatches.Location = new System.Drawing.Point(14, 152);
        this.labelMatches.Name = "labelMatches";
        this.labelMatches.Size = new System.Drawing.Size(74, 20);
        this.labelMatches.TabIndex = 6;
        this.labelMatches.Text = "Matches:";
        // 
        // textBoxMatches
        // 
        this.textBoxMatches.Location = new System.Drawing.Point(12, 175);
        this.textBoxMatches.Multiline = true;
        this.textBoxMatches.Name = "textBoxMatches";
        this.textBoxMatches.ReadOnly = true;
        this.textBoxMatches.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this.textBoxMatches.Size = new System.Drawing.Size(428, 190);
        this.textBoxMatches.TabIndex = 7;
        // 
        // checkBoxCaseSensitive
        // 
        this.checkBoxCaseSensitive.AutoSize = true;
        this.checkBoxCaseSensitive.Location = new System.Drawing.Point(12, 71);
        this.checkBoxCaseSensitive.Name = "checkBoxCaseSensitive";
        this.checkBoxCaseSensitive.Size = new System.Drawing.Size(137, 24);
        this.checkBoxCaseSensitive.TabIndex = 8;
        this.checkBoxCaseSensitive.Text = "Case sensitive";
        this.checkBoxCaseSensitive.UseVisualStyleBackColor = true;
        this.checkBoxCaseSensitive.CheckedChanged += new System.EventHandler(this.OnCaseSensitiveCheckBoxCheckedChanged);
        // 
        // comboBoxRegex
        // 
        this.comboBoxRegex.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
        this.comboBoxRegex.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
        this.comboBoxRegex.FormattingEnabled = true;
        this.comboBoxRegex.Location = new System.Drawing.Point(12, 37);
        this.comboBoxRegex.Name = "comboBoxRegex";
        this.comboBoxRegex.Size = new System.Drawing.Size(428, 28);
        this.comboBoxRegex.TabIndex = 10;
        this.comboBoxRegex.TextChanged += new System.EventHandler(this.OnComboBoxRegexTextChanged);
        // 
        // comboBoxTestText
        // 
        this.comboBoxTestText.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
        this.comboBoxTestText.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
        this.comboBoxTestText.FormattingEnabled = true;
        this.comboBoxTestText.Location = new System.Drawing.Point(12, 121);
        this.comboBoxTestText.Name = "comboBoxTestText";
        this.comboBoxTestText.Size = new System.Drawing.Size(428, 28);
        this.comboBoxTestText.TabIndex = 11;
        this.comboBoxTestText.TextChanged += new System.EventHandler(this.OnComboBoxTestTextTextChanged);
        // 
        // helpProvider1
        // 
        this.helpProvider1.HelpNamespace = "LogExpert.chm";
        // 
        // buttonHelp
        // 
        this.helpProvider1.SetHelpKeyword(this.buttonHelp, "RegEx.htm");
        this.helpProvider1.SetHelpNavigator(this.buttonHelp, System.Windows.Forms.HelpNavigator.Topic);
        this.buttonHelp.Location = new System.Drawing.Point(13, 371);
        this.buttonHelp.Name = "buttonHelp";
        this.helpProvider1.SetShowHelp(this.buttonHelp, true);
        this.buttonHelp.Size = new System.Drawing.Size(75, 23);
        this.buttonHelp.TabIndex = 12;
        this.buttonHelp.Text = "Help";
        this.buttonHelp.UseVisualStyleBackColor = true;
        this.buttonHelp.Click += new System.EventHandler(this.OnButtonHelpClick);
        // 
        // RegexHelperDialog
        // 
        this.AcceptButton = this.buttonOk;
        this.CancelButton = this.buttonCancel;
        this.ClientSize = new System.Drawing.Size(452, 403);
        this.Controls.Add(this.buttonHelp);
        this.Controls.Add(this.comboBoxTestText);
        this.Controls.Add(this.comboBoxRegex);
        this.Controls.Add(this.checkBoxCaseSensitive);
        this.Controls.Add(this.textBoxMatches);
        this.Controls.Add(this.labelMatches);
        this.Controls.Add(this.labelTestText);
        this.Controls.Add(this.labelRegex);
        this.Controls.Add(this.buttonOk);
        this.Controls.Add(this.buttonCancel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.helpProvider1.SetHelpKeyword(this, "RegEx.htm");
        this.helpProvider1.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "RegexHelperDialog";
        this.helpProvider1.SetShowHelp(this, true);
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Regex-Helper";
        this.ResumeLayout(false);
        this.PerformLayout();

}

#endregion

private System.Windows.Forms.Button buttonCancel;
private System.Windows.Forms.Button buttonOk;
private System.Windows.Forms.Label labelRegex;
private System.Windows.Forms.Label labelTestText;
private System.Windows.Forms.Label labelMatches;
private System.Windows.Forms.TextBox textBoxMatches;
private System.Windows.Forms.CheckBox checkBoxCaseSensitive;
private System.Windows.Forms.ComboBox comboBoxRegex;
private System.Windows.Forms.ComboBox comboBoxTestText;
private System.Windows.Forms.HelpProvider helpProvider1;
private System.Windows.Forms.Button buttonHelp;
}