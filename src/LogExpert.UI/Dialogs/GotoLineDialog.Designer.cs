namespace LogExpert.UI.Dialogs;

partial class GotoLineDialog
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
        this.labelLineNumber = new System.Windows.Forms.Label();
        this.buttonOk = new System.Windows.Forms.Button();
        this.buttonCancel = new System.Windows.Forms.Button();
        this.lineNumberTextBox = new System.Windows.Forms.TextBox();
        this.SuspendLayout();
        // 
        // labelLineNumber
        // 
        this.labelLineNumber.AutoSize = true;
        this.labelLineNumber.Location = new System.Drawing.Point(12, 30);
        this.labelLineNumber.Name = "labelLineNumber";
        this.labelLineNumber.Size = new System.Drawing.Size(101, 20);
        this.labelLineNumber.TabIndex = 0;
        this.labelLineNumber.Text = "Line number:";
        // 
        // buttonOk
        // 
        this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        this.buttonOk.AutoSize = true;
        this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonOk.Location = new System.Drawing.Point(15, 90);
        this.buttonOk.Name = "buttonOk";
        this.buttonOk.Size = new System.Drawing.Size(75, 30);
        this.buttonOk.TabIndex = 2;
        this.buttonOk.Text = "OK";
        this.buttonOk.UseVisualStyleBackColor = true;
        this.buttonOk.Click += new System.EventHandler(this.okButton_Click);
        // 
        // buttonCancel
        // 
        this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonCancel.AutoSize = true;
        this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.buttonCancel.Location = new System.Drawing.Point(149, 90);
        this.buttonCancel.Name = "buttonCancel";
        this.buttonCancel.Size = new System.Drawing.Size(75, 30);
        this.buttonCancel.TabIndex = 3;
        this.buttonCancel.Text = "Cancel";
        this.buttonCancel.UseVisualStyleBackColor = true;
        // 
        // lineNumberTextBox
        // 
        this.lineNumberTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.lineNumberTextBox.Location = new System.Drawing.Point(124, 27);
        this.lineNumberTextBox.Name = "lineNumberTextBox";
        this.lineNumberTextBox.Size = new System.Drawing.Size(100, 26);
        this.lineNumberTextBox.TabIndex = 0;
        // 
        // GotoLineDialog
        // 
        this.AcceptButton = this.buttonOk;
        this.CancelButton = this.buttonCancel;
        this.ClientSize = new System.Drawing.Size(243, 130);
        this.Controls.Add(this.lineNumberTextBox);
        this.Controls.Add(this.buttonCancel);
        this.Controls.Add(this.buttonOk);
        this.Controls.Add(this.labelLineNumber);
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "GotoLineDialog";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Go to line";
        this.Load += new System.EventHandler(this.GotoLineDialog_Load);
        this.ResumeLayout(false);
        this.PerformLayout();

}

#endregion

private System.Windows.Forms.Label labelLineNumber;
private System.Windows.Forms.Button buttonOk;
private System.Windows.Forms.Button buttonCancel;
private System.Windows.Forms.TextBox lineNumberTextBox;
}