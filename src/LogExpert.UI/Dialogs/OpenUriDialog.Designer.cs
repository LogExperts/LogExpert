namespace LogExpert.UI.Dialogs;

partial class OpenUriDialog
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
  System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OpenUriDialog));
  this.cmbUri = new System.Windows.Forms.ComboBox();
  this.label1 = new System.Windows.Forms.Label();
  this.okButton = new System.Windows.Forms.Button();
  this.cancelButton = new System.Windows.Forms.Button();
  this.label2 = new System.Windows.Forms.Label();
  this.SuspendLayout();
  // 
  // uriComboBox
  // 
  this.cmbUri.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
              | System.Windows.Forms.AnchorStyles.Right)));
  this.cmbUri.FormattingEnabled = true;
  this.cmbUri.Location = new System.Drawing.Point(12, 37);
  this.cmbUri.Name = "cmbUri";
  this.cmbUri.Size = new System.Drawing.Size(449, 21);
  this.cmbUri.TabIndex = 0;
  // 
  // label1
  // 
  this.label1.AutoSize = true;
  this.label1.Location = new System.Drawing.Point(12, 21);
  this.label1.Name = "label1";
  this.label1.Size = new System.Drawing.Size(32, 13);
  this.label1.TabIndex = 1;
  this.label1.Text = "URL:";
  // 
  // okButton
  // 
  this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
  this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
  this.okButton.Location = new System.Drawing.Point(305, 127);
  this.okButton.Name = "okButton";
  this.okButton.Size = new System.Drawing.Size(75, 23);
  this.okButton.TabIndex = 2;
  this.okButton.Text = "OK";
  this.okButton.UseVisualStyleBackColor = true;
  this.okButton.Click += new System.EventHandler(this.OnBtnOkClick);
  // 
  // cancelButton
  // 
  this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
  this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
  this.cancelButton.Location = new System.Drawing.Point(386, 127);
  this.cancelButton.Name = "cancelButton";
  this.cancelButton.Size = new System.Drawing.Size(75, 23);
  this.cancelButton.TabIndex = 3;
  this.cancelButton.Text = "Cancel";
  this.cancelButton.UseVisualStyleBackColor = true;
  // 
  // label2
  // 
  this.label2.AutoSize = true;
  this.label2.Location = new System.Drawing.Point(12, 84);
  this.label2.Name = "label2";
  this.label2.Size = new System.Drawing.Size(410, 13);
  this.label2.TabIndex = 4;
  this.label2.Text = "Enter a URL which is supported by an installed file system plugin (e.g. file:// o" +
      "r sftp://)";
  // 
  // OpenUriDialog
  // 
  this.AcceptButton = this.okButton;
  this.CancelButton = this.cancelButton;
  this.ClientSize = new System.Drawing.Size(475, 162);
  this.Controls.Add(this.label2);
  this.Controls.Add(this.cancelButton);
  this.Controls.Add(this.okButton);
  this.Controls.Add(this.label1);
  this.Controls.Add(this.cmbUri);
  this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
  this.MaximizeBox = false;
  this.MaximumSize = new System.Drawing.Size(800, 260);
  this.MinimizeBox = false;
  this.MinimumSize = new System.Drawing.Size(491, 200);
  this.Name = "OpenUriDialog";
  this.Text = "Open URL";
  this.Load += new System.EventHandler(this.OnOpenUriDialogLoad);
  this.ResumeLayout(false);
  this.PerformLayout();

}

#endregion

private System.Windows.Forms.ComboBox cmbUri;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.Button okButton;
private System.Windows.Forms.Button cancelButton;
private System.Windows.Forms.Label label2;
}