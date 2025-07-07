namespace LogExpert.Dialogs;

partial class BookmarkCommentDlg
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
  System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BookmarkCommentDlg));
  this.okButton = new System.Windows.Forms.Button();
  this.cancelButton = new System.Windows.Forms.Button();
  this.commentTextBox = new System.Windows.Forms.TextBox();
  this.SuspendLayout();
  // 
  // okButton
  // 
  this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
  this.okButton.Location = new System.Drawing.Point(150, 86);
  this.okButton.Name = "okButton";
  this.okButton.Size = new System.Drawing.Size(75, 23);
  this.okButton.TabIndex = 1;
  this.okButton.Text = "OK";
  this.okButton.UseVisualStyleBackColor = true;
  // 
  // cancelButton
  // 
  this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
  this.cancelButton.Location = new System.Drawing.Point(241, 86);
  this.cancelButton.Name = "cancelButton";
  this.cancelButton.Size = new System.Drawing.Size(75, 23);
  this.cancelButton.TabIndex = 2;
  this.cancelButton.Text = "&Cancel";
  this.cancelButton.UseVisualStyleBackColor = true;
  // 
  // commentTextBox
  // 
  this.commentTextBox.Dock = System.Windows.Forms.DockStyle.Top;
  this.commentTextBox.Location = new System.Drawing.Point(0, 0);
  this.commentTextBox.Multiline = true;
  this.commentTextBox.Name = "commentTextBox";
  this.commentTextBox.Size = new System.Drawing.Size(324, 80);
  this.commentTextBox.TabIndex = 0;
  // 
  // BookmarkCommentDlg
  // 
  this.AcceptButton = this.okButton;
  this.CancelButton = this.cancelButton;
  this.ClientSize = new System.Drawing.Size(324, 115);
  this.Controls.Add(this.commentTextBox);
  this.Controls.Add(this.cancelButton);
  this.Controls.Add(this.okButton);
  this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
  this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
  this.Name = "BookmarkCommentDlg";
  this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
  this.Text = "Bookmark comment";
  this.ResumeLayout(false);
  this.PerformLayout();

}

#endregion

private System.Windows.Forms.Button okButton;
private System.Windows.Forms.Button cancelButton;
private System.Windows.Forms.TextBox commentTextBox;
}