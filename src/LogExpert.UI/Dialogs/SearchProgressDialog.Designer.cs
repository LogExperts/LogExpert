namespace LogExpert.UI.Dialogs;

partial class SearchProgressDialog
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
        this.labelSearchProgress = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // buttonCancel
        // 
        this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.buttonCancel.Location = new System.Drawing.Point(159, 40);
        this.buttonCancel.Name = "buttonCancel";
        this.buttonCancel.Size = new System.Drawing.Size(90, 23);
        this.buttonCancel.TabIndex = 0;
        this.buttonCancel.Text = "Cancel search";
        this.buttonCancel.UseVisualStyleBackColor = true;
        this.buttonCancel.Click += new System.EventHandler(this.OnButtonCancelClick);
        // 
        // labelSearchProgress
        // 
        this.labelSearchProgress.AutoSize = true;
        this.labelSearchProgress.Location = new System.Drawing.Point(13, 13);
        this.labelSearchProgress.Name = "labelSearchProgress";
        this.labelSearchProgress.Size = new System.Drawing.Size(175, 20);
        this.labelSearchProgress.TabIndex = 1;
        this.labelSearchProgress.Text = "Searching in progress...";
        // 
        // SearchProgressDialog
        // 
        this.ClientSize = new System.Drawing.Size(261, 80);
        this.ControlBox = false;
        this.Controls.Add(this.labelSearchProgress);
        this.Controls.Add(this.buttonCancel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.Name = "SearchProgressDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Searching...";
        this.ResumeLayout(false);
        this.PerformLayout();

}

#endregion

private System.Windows.Forms.Button buttonCancel;
private System.Windows.Forms.Label labelSearchProgress;
}