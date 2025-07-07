namespace LogExpert.UI.Dialogs;

partial class TabRenameDialog
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
        this.textBoxTabName = new System.Windows.Forms.TextBox();
        this.labelName = new System.Windows.Forms.Label();
        this.buttonOk = new System.Windows.Forms.Button();
        this.buttonCancel = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // textBoxTabName
        // 
        this.textBoxTabName.Location = new System.Drawing.Point(12, 34);
        this.textBoxTabName.Name = "textBoxTabName";
        this.textBoxTabName.Size = new System.Drawing.Size(193, 26);
        this.textBoxTabName.TabIndex = 0;
        // 
        // labelName
        // 
        this.labelName.AutoSize = true;
        this.labelName.Location = new System.Drawing.Point(13, 11);
        this.labelName.Name = "labelName";
        this.labelName.Size = new System.Drawing.Size(55, 20);
        this.labelName.TabIndex = 1;
        this.labelName.Text = "Name:";
        // 
        // buttonOk
        // 
        this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonOk.Location = new System.Drawing.Point(49, 73);
        this.buttonOk.Name = "buttonOk";
        this.buttonOk.Size = new System.Drawing.Size(75, 23);
        this.buttonOk.TabIndex = 2;
        this.buttonOk.Text = "OK";
        this.buttonOk.UseVisualStyleBackColor = true;
        // 
        // buttonCancel
        // 
        this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.buttonCancel.Location = new System.Drawing.Point(130, 73);
        this.buttonCancel.Name = "buttonCancel";
        this.buttonCancel.Size = new System.Drawing.Size(75, 23);
        this.buttonCancel.TabIndex = 3;
        this.buttonCancel.Text = "Cancel";
        this.buttonCancel.UseVisualStyleBackColor = true;
        // 
        // TabRenameDlg
        // 
        this.AcceptButton = this.buttonOk;
        this.ClientSize = new System.Drawing.Size(219, 108);
        this.Controls.Add(this.buttonCancel);
        this.Controls.Add(this.buttonOk);
        this.Controls.Add(this.labelName);
        this.Controls.Add(this.textBoxTabName);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
        this.KeyPreview = true;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "TabRenameDlg";
        this.ShowIcon = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Rename Tab";
        this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnTabRenameDlgKeyDown);
        this.ResumeLayout(false);
        this.PerformLayout();

}

#endregion

        private System.Windows.Forms.TextBox textBoxTabName;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonCancel;
}