namespace LogExpert.UI.Dialogs;

partial class ImportSettingsDialog
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportSettingsDialog));
        this.buttonFile = new System.Windows.Forms.Button();
        this.textBoxFileName = new System.Windows.Forms.TextBox();
        this.labelSettingsFileToImport = new System.Windows.Forms.Label();
        this.groupBoxImportOptions = new System.Windows.Forms.GroupBox();
        this.checkBoxKeepExistingSettings = new System.Windows.Forms.CheckBox();
        this.checkBoxOther = new System.Windows.Forms.CheckBox();
        this.checkBoxExternalTools = new System.Windows.Forms.CheckBox();
        this.checkBoxColumnizerFileMasks = new System.Windows.Forms.CheckBox();
        this.checkBoxHighlightFileMasks = new System.Windows.Forms.CheckBox();
        this.checkBoxHighlightSettings = new System.Windows.Forms.CheckBox();
        this.buttonOk = new System.Windows.Forms.Button();
        this.buttonCancel = new System.Windows.Forms.Button();
        this.groupBoxImportOptions.SuspendLayout();
        this.SuspendLayout();
        // 
        // buttonFile
        // 
        this.buttonFile.Location = new System.Drawing.Point(306, 25);
        this.buttonFile.Name = "buttonFile";
        this.buttonFile.Size = new System.Drawing.Size(94, 23);
        this.buttonFile.TabIndex = 0;
        this.buttonFile.Text = "Choose file...";
        this.buttonFile.UseVisualStyleBackColor = true;
        this.buttonFile.Click += new System.EventHandler(this.OnFileButtonClick);
        // 
        // textBoxFileName
        // 
        this.textBoxFileName.Location = new System.Drawing.Point(13, 27);
        this.textBoxFileName.Name = "textBoxFileName";
        this.textBoxFileName.Size = new System.Drawing.Size(287, 20);
        this.textBoxFileName.TabIndex = 1;
        // 
        // labelSettingsFileToImport
        // 
        this.labelSettingsFileToImport.AutoSize = true;
        this.labelSettingsFileToImport.Location = new System.Drawing.Point(13, 8);
        this.labelSettingsFileToImport.Name = "labelSettingsFileToImport";
        this.labelSettingsFileToImport.Size = new System.Drawing.Size(107, 13);
        this.labelSettingsFileToImport.TabIndex = 2;
        this.labelSettingsFileToImport.Text = "Settings file to import:";
        // 
        // groupBoxImportOptions
        // 
        this.groupBoxImportOptions.Controls.Add(this.checkBoxKeepExistingSettings);
        this.groupBoxImportOptions.Controls.Add(this.checkBoxOther);
        this.groupBoxImportOptions.Controls.Add(this.checkBoxExternalTools);
        this.groupBoxImportOptions.Controls.Add(this.checkBoxColumnizerFileMasks);
        this.groupBoxImportOptions.Controls.Add(this.checkBoxHighlightFileMasks);
        this.groupBoxImportOptions.Controls.Add(this.checkBoxHighlightSettings);
        this.groupBoxImportOptions.Location = new System.Drawing.Point(16, 67);
        this.groupBoxImportOptions.Name = "groupBoxImportOptions";
        this.groupBoxImportOptions.Size = new System.Drawing.Size(284, 143);
        this.groupBoxImportOptions.TabIndex = 3;
        this.groupBoxImportOptions.TabStop = false;
        this.groupBoxImportOptions.Text = "Import options";
        // 
        // checkBoxKeepExistingSettings
        // 
        this.checkBoxKeepExistingSettings.AutoSize = true;
        this.checkBoxKeepExistingSettings.Location = new System.Drawing.Point(141, 20);
        this.checkBoxKeepExistingSettings.Name = "checkBoxKeepExistingSettings";
        this.checkBoxKeepExistingSettings.Size = new System.Drawing.Size(128, 17);
        this.checkBoxKeepExistingSettings.TabIndex = 1;
        this.checkBoxKeepExistingSettings.Tag = "32";
        this.checkBoxKeepExistingSettings.Text = "Keep existing settings";
        this.checkBoxKeepExistingSettings.UseVisualStyleBackColor = true;
        // 
        // checkBoxOther
        // 
        this.checkBoxOther.AutoSize = true;
        this.checkBoxOther.Checked = true;
        this.checkBoxOther.CheckState = System.Windows.Forms.CheckState.Checked;
        this.checkBoxOther.Location = new System.Drawing.Point(7, 112);
        this.checkBoxOther.Name = "checkBoxOther";
        this.checkBoxOther.Size = new System.Drawing.Size(52, 17);
        this.checkBoxOther.TabIndex = 0;
        this.checkBoxOther.Tag = "16";
        this.checkBoxOther.Text = "Other";
        this.checkBoxOther.UseVisualStyleBackColor = true;
        // 
        // checkBoxExternalTools
        // 
        this.checkBoxExternalTools.AutoSize = true;
        this.checkBoxExternalTools.Checked = true;
        this.checkBoxExternalTools.CheckState = System.Windows.Forms.CheckState.Checked;
        this.checkBoxExternalTools.Location = new System.Drawing.Point(7, 89);
        this.checkBoxExternalTools.Name = "checkBoxExternalTools";
        this.checkBoxExternalTools.Size = new System.Drawing.Size(89, 17);
        this.checkBoxExternalTools.TabIndex = 0;
        this.checkBoxExternalTools.Tag = "8";
        this.checkBoxExternalTools.Text = "External tools";
        this.checkBoxExternalTools.UseVisualStyleBackColor = true;
        // 
        // checkBoxColumnizerFileMasks
        // 
        this.checkBoxColumnizerFileMasks.AutoSize = true;
        this.checkBoxColumnizerFileMasks.Checked = true;
        this.checkBoxColumnizerFileMasks.CheckState = System.Windows.Forms.CheckState.Checked;
        this.checkBoxColumnizerFileMasks.Location = new System.Drawing.Point(7, 66);
        this.checkBoxColumnizerFileMasks.Name = "checkBoxColumnizerFileMasks";
        this.checkBoxColumnizerFileMasks.Size = new System.Drawing.Size(126, 17);
        this.checkBoxColumnizerFileMasks.TabIndex = 0;
        this.checkBoxColumnizerFileMasks.Tag = "2";
        this.checkBoxColumnizerFileMasks.Text = "Columnizer file masks";
        this.checkBoxColumnizerFileMasks.UseVisualStyleBackColor = true;
        // 
        // checkBoxHighlightFileMasks
        // 
        this.checkBoxHighlightFileMasks.AutoSize = true;
        this.checkBoxHighlightFileMasks.Checked = true;
        this.checkBoxHighlightFileMasks.CheckState = System.Windows.Forms.CheckState.Checked;
        this.checkBoxHighlightFileMasks.Location = new System.Drawing.Point(7, 43);
        this.checkBoxHighlightFileMasks.Name = "checkBoxHighlightFileMasks";
        this.checkBoxHighlightFileMasks.Size = new System.Drawing.Size(116, 17);
        this.checkBoxHighlightFileMasks.TabIndex = 0;
        this.checkBoxHighlightFileMasks.Tag = "4";
        this.checkBoxHighlightFileMasks.Text = "Highlight file masks";
        this.checkBoxHighlightFileMasks.UseVisualStyleBackColor = true;
        // 
        // checkBoxHighlightSettings
        // 
        this.checkBoxHighlightSettings.AutoSize = true;
        this.checkBoxHighlightSettings.Checked = true;
        this.checkBoxHighlightSettings.CheckState = System.Windows.Forms.CheckState.Checked;
        this.checkBoxHighlightSettings.Location = new System.Drawing.Point(7, 20);
        this.checkBoxHighlightSettings.Name = "checkBoxHighlightSettings";
        this.checkBoxHighlightSettings.Size = new System.Drawing.Size(106, 17);
        this.checkBoxHighlightSettings.TabIndex = 0;
        this.checkBoxHighlightSettings.Tag = "1";
        this.checkBoxHighlightSettings.Text = "Highlight settings";
        this.checkBoxHighlightSettings.UseVisualStyleBackColor = true;
        // 
        // buttonOk
        // 
        this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonOk.Location = new System.Drawing.Point(325, 187);
        this.buttonOk.Name = "buttonOk";
        this.buttonOk.Size = new System.Drawing.Size(75, 23);
        this.buttonOk.TabIndex = 4;
        this.buttonOk.Text = "OK";
        this.buttonOk.UseVisualStyleBackColor = true;
        this.buttonOk.Click += new System.EventHandler(this.OnOkButtonClick);
        // 
        // buttonCancel
        // 
        this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.buttonCancel.Location = new System.Drawing.Point(325, 154);
        this.buttonCancel.Name = "buttonCancel";
        this.buttonCancel.Size = new System.Drawing.Size(75, 23);
        this.buttonCancel.TabIndex = 5;
        this.buttonCancel.Text = "Cancel";
        this.buttonCancel.UseVisualStyleBackColor = true;
        // 
        // ImportSettingsDialog
        // 
        this.AcceptButton = this.buttonOk;
        this.CancelButton = this.buttonCancel;
        this.ClientSize = new System.Drawing.Size(412, 224);
        this.Controls.Add(this.buttonCancel);
        this.Controls.Add(this.buttonOk);
        this.Controls.Add(this.groupBoxImportOptions);
        this.Controls.Add(this.labelSettingsFileToImport);
        this.Controls.Add(this.textBoxFileName);
        this.Controls.Add(this.buttonFile);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ImportSettingsDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Import Settings";
        this.Load += new System.EventHandler(this.OnImportSettingsDialogLoad);
        this.groupBoxImportOptions.ResumeLayout(false);
        this.groupBoxImportOptions.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();

}

#endregion

private System.Windows.Forms.Button buttonFile;
private System.Windows.Forms.TextBox textBoxFileName;
private System.Windows.Forms.Label labelSettingsFileToImport;
private System.Windows.Forms.Button buttonOk;
private System.Windows.Forms.Button buttonCancel;
    public System.Windows.Forms.CheckBox checkBoxExternalTools;
    public System.Windows.Forms.CheckBox checkBoxColumnizerFileMasks;
    public System.Windows.Forms.CheckBox checkBoxHighlightFileMasks;
    public System.Windows.Forms.CheckBox checkBoxHighlightSettings;
    public System.Windows.Forms.CheckBox checkBoxOther;
    public System.Windows.Forms.CheckBox checkBoxKeepExistingSettings;
    public System.Windows.Forms.GroupBox groupBoxImportOptions;
}