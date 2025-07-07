namespace LogExpert.UI.Dialogs;

partial class MultiLoadRequestDialog
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
        this.buttonSingleMode = new System.Windows.Forms.Button();
        this.buttonMultiMode = new System.Windows.Forms.Button();
        this.labelChooseLoadingMode = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // buttonSingleMode
        // 
        this.buttonSingleMode.DialogResult = System.Windows.Forms.DialogResult.Yes;
        this.buttonSingleMode.Location = new System.Drawing.Point(12, 59);
        this.buttonSingleMode.Name = "buttonSingleMode";
        this.buttonSingleMode.Size = new System.Drawing.Size(75, 23);
        this.buttonSingleMode.TabIndex = 1;
        this.buttonSingleMode.Text = "Single files";
        this.buttonSingleMode.UseVisualStyleBackColor = true;
        // 
        // buttonMultiMode
        // 
        this.buttonMultiMode.DialogResult = System.Windows.Forms.DialogResult.No;
        this.buttonMultiMode.Location = new System.Drawing.Point(114, 59);
        this.buttonMultiMode.Name = "buttonMultiMode";
        this.buttonMultiMode.Size = new System.Drawing.Size(75, 23);
        this.buttonMultiMode.TabIndex = 2;
        this.buttonMultiMode.Text = "Multi file";
        this.buttonMultiMode.UseVisualStyleBackColor = true;
        // 
        // labelChooseLoadingMode
        // 
        this.labelChooseLoadingMode.AutoSize = true;
        this.labelChooseLoadingMode.Location = new System.Drawing.Point(48, 18);
        this.labelChooseLoadingMode.Name = "labelChooseLoadingMode";
        this.labelChooseLoadingMode.Size = new System.Drawing.Size(167, 20);
        this.labelChooseLoadingMode.TabIndex = 4;
        this.labelChooseLoadingMode.Text = "Choose loading mode:";
        // 
        // MultiLoadRequestDialog
        // 
        this.ClientSize = new System.Drawing.Size(237, 103);
        this.Controls.Add(this.labelChooseLoadingMode);
        this.Controls.Add(this.buttonMultiMode);
        this.Controls.Add(this.buttonSingleMode);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "MultiLoadRequestDialog";
        this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Loading multiple files";
        this.ResumeLayout(false);
        this.PerformLayout();

}

#endregion

private System.Windows.Forms.Button buttonSingleMode;
private System.Windows.Forms.Button buttonMultiMode;
private System.Windows.Forms.Label labelChooseLoadingMode;
}