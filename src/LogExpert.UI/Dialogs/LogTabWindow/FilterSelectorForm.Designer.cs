namespace LogExpert.Dialogs;

partial class FilterSelectorForm
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
  System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FilterSelectorForm));
  this.filterComboBox = new System.Windows.Forms.ComboBox();
  this.label1 = new System.Windows.Forms.Label();
  this.okButton = new System.Windows.Forms.Button();
  this.cancelButton = new System.Windows.Forms.Button();
  this.commentTextBox = new System.Windows.Forms.TextBox();
  this.applyToAllCheckBox = new System.Windows.Forms.CheckBox();
  this.helpProvider1 = new System.Windows.Forms.HelpProvider();
  this.configButton = new System.Windows.Forms.Button();
  this.SuspendLayout();
  // 
  // filterComboBox
  // 
  this.filterComboBox.DisplayMember = "Text";
  this.filterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
  this.filterComboBox.FormattingEnabled = true;
  this.filterComboBox.Location = new System.Drawing.Point(12, 25);
  this.filterComboBox.Name = "filterComboBox";
  this.filterComboBox.Size = new System.Drawing.Size(298, 21);
  this.filterComboBox.TabIndex = 0;
  // 
  // label1
  // 
  this.label1.AutoSize = true;
  this.label1.Location = new System.Drawing.Point(13, 6);
  this.label1.Name = "label1";
  this.label1.Size = new System.Drawing.Size(108, 13);
  this.label1.TabIndex = 1;
  this.label1.Text = "Choose a columnizer:";
  // 
  // okButton
  // 
  this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
  this.okButton.Location = new System.Drawing.Point(154, 194);
  this.okButton.Name = "okButton";
  this.okButton.Size = new System.Drawing.Size(75, 23);
  this.okButton.TabIndex = 2;
  this.okButton.Text = "OK";
  this.okButton.UseVisualStyleBackColor = true;
  // 
  // cancelButton
  // 
  this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
  this.cancelButton.Location = new System.Drawing.Point(235, 194);
  this.cancelButton.Name = "cancelButton";
  this.cancelButton.Size = new System.Drawing.Size(75, 23);
  this.cancelButton.TabIndex = 3;
  this.cancelButton.Text = "Cancel";
  this.cancelButton.UseVisualStyleBackColor = true;
  // 
  // commentTextBox
  // 
  this.commentTextBox.Location = new System.Drawing.Point(12, 81);
  this.commentTextBox.Multiline = true;
  this.commentTextBox.Name = "commentTextBox";
  this.commentTextBox.ReadOnly = true;
  this.commentTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
  this.commentTextBox.Size = new System.Drawing.Size(298, 89);
  this.commentTextBox.TabIndex = 4;
  // 
  // applyToAllCheckBox
  // 
  this.applyToAllCheckBox.AutoSize = true;
  this.applyToAllCheckBox.Location = new System.Drawing.Point(12, 176);
  this.applyToAllCheckBox.Name = "applyToAllCheckBox";
  this.applyToAllCheckBox.Size = new System.Drawing.Size(125, 17);
  this.applyToAllCheckBox.TabIndex = 6;
  this.applyToAllCheckBox.Text = "Apply to all open files";
  this.applyToAllCheckBox.UseVisualStyleBackColor = true;
  // 
  // helpProvider1
  // 
  this.helpProvider1.HelpNamespace = "LogExpert.chm";
  // 
  // configButton
  // 
  this.configButton.Location = new System.Drawing.Point(235, 52);
  this.configButton.Name = "configButton";
  this.configButton.Size = new System.Drawing.Size(75, 23);
  this.configButton.TabIndex = 7;
  this.configButton.Text = "Config...";
  this.configButton.UseVisualStyleBackColor = true;
  this.configButton.Click += new System.EventHandler(this.OnConfigButtonClick);
  // 
  // FilterSelectorForm
  // 
  this.AcceptButton = this.okButton;
  this.CancelButton = this.cancelButton;
  this.ClientSize = new System.Drawing.Size(322, 232);
  this.Controls.Add(this.configButton);
  this.Controls.Add(this.applyToAllCheckBox);
  this.Controls.Add(this.commentTextBox);
  this.Controls.Add(this.cancelButton);
  this.Controls.Add(this.okButton);
  this.Controls.Add(this.label1);
  this.Controls.Add(this.filterComboBox);
  this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
  this.helpProvider1.SetHelpKeyword(this, "Columnizers.htm");
  this.helpProvider1.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
  this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
  this.MaximizeBox = false;
  this.MinimizeBox = false;
  this.Name = "FilterSelectorForm";
  this.helpProvider1.SetShowHelp(this, true);
  this.ShowIcon = false;
  this.ShowInTaskbar = false;
  this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
  this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
  this.Text = "Columnizer";
  this.ResumeLayout(false);
  this.PerformLayout();

}

#endregion

private System.Windows.Forms.ComboBox filterComboBox;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.Button okButton;
private System.Windows.Forms.Button cancelButton;
private System.Windows.Forms.TextBox commentTextBox;
private System.Windows.Forms.CheckBox applyToAllCheckBox;
private System.Windows.Forms.HelpProvider helpProvider1;
private System.Windows.Forms.Button configButton;
}