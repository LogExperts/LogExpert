namespace LogExpert.UI.Dialogs;

partial class ExceptionWindow
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExceptionWindow));
        this.stackTraceTextBox = new System.Windows.Forms.TextBox();
        this.label1 = new System.Windows.Forms.Label();
        this.okButton = new System.Windows.Forms.Button();
        this.copyButton = new System.Windows.Forms.Button();
        this.SuspendLayout();
        // 
        // stackTraceTextBox
        // 
        this.stackTraceTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
        | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
        this.stackTraceTextBox.Location = new System.Drawing.Point(12, 42);
        this.stackTraceTextBox.Multiline = true;
        this.stackTraceTextBox.Name = "stackTraceTextBox";
        this.stackTraceTextBox.ReadOnly = true;
        this.stackTraceTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this.stackTraceTextBox.Size = new System.Drawing.Size(439, 205);
        this.stackTraceTextBox.TabIndex = 0;
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(13, 13);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(310, 13);
        this.label1.TabIndex = 1;
        this.label1.Text = "An unhandled error has occured. Please report to the developer.";
        // 
        // okButton
        // 
        this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.okButton.Location = new System.Drawing.Point(376, 265);
        this.okButton.Name = "okButton";
        this.okButton.Size = new System.Drawing.Size(75, 23);
        this.okButton.TabIndex = 2;
        this.okButton.Text = "Close";
        this.okButton.UseVisualStyleBackColor = true;
        // 
        // copyButton
        // 
        this.copyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        this.copyButton.Location = new System.Drawing.Point(12, 265);
        this.copyButton.Name = "copyButton";
        this.copyButton.Size = new System.Drawing.Size(117, 23);
        this.copyButton.TabIndex = 4;
        this.copyButton.Text = "Copy to clipboard";
        this.copyButton.UseVisualStyleBackColor = true;
        this.copyButton.Click += new System.EventHandler(this.copyButton_Click);
        // 
        // ExceptionWindow
        // 
        this.CancelButton = this.okButton;
        this.ClientSize = new System.Drawing.Size(464, 300);
        this.Controls.Add(this.copyButton);
        this.Controls.Add(this.okButton);
        this.Controls.Add(this.label1);
        this.Controls.Add(this.stackTraceTextBox);
        this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ExceptionWindow";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "LogExpert Error";
        this.ResumeLayout(false);
        this.PerformLayout();

}

#endregion

private System.Windows.Forms.TextBox stackTraceTextBox;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.Button okButton;
    private System.Windows.Forms.Button copyButton;
}