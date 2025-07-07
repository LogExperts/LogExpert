namespace LogExpert.Dialogs;

partial class ToolArgsDialog
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
        this.buttonOk = new System.Windows.Forms.Button();
        this.buttonRegexHelp = new System.Windows.Forms.Button();
        this.textBoxArguments = new System.Windows.Forms.TextBox();
        this.buttonTest = new System.Windows.Forms.Button();
        this.labelEnterArguments = new System.Windows.Forms.Label();
        this.labelHelp = new System.Windows.Forms.Label();
        this.buttonCancel = new System.Windows.Forms.Button();
        this.labelTestResult = new System.Windows.Forms.TextBox();
        this.SuspendLayout();
        // 
        // buttonOk
        // 
        this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonOk.Location = new System.Drawing.Point(348, 292);
        this.buttonOk.Name = "buttonOk";
        this.buttonOk.Size = new System.Drawing.Size(75, 23);
        this.buttonOk.TabIndex = 1;
        this.buttonOk.Text = "OK";
        this.buttonOk.UseVisualStyleBackColor = true;
        this.buttonOk.Click += new System.EventHandler(this.OnButtonOkClick);
        // 
        // buttonRegexHelp
        // 
        this.buttonRegexHelp.Location = new System.Drawing.Point(429, 59);
        this.buttonRegexHelp.Name = "buttonRegexHelp";
        this.buttonRegexHelp.Size = new System.Drawing.Size(75, 21);
        this.buttonRegexHelp.TabIndex = 2;
        this.buttonRegexHelp.Text = "RegEx Help";
        this.buttonRegexHelp.UseVisualStyleBackColor = true;
        this.buttonRegexHelp.Click += new System.EventHandler(this.OnButtonRegexHelpClick);
        // 
        // textBoxArguments
        // 
        this.textBoxArguments.Location = new System.Drawing.Point(12, 36);
        this.textBoxArguments.Name = "textBoxArguments";
        this.textBoxArguments.Size = new System.Drawing.Size(395, 26);
        this.textBoxArguments.TabIndex = 8;
        // 
        // buttonTest
        // 
        this.buttonTest.Location = new System.Drawing.Point(429, 30);
        this.buttonTest.Name = "buttonTest";
        this.buttonTest.Size = new System.Drawing.Size(75, 23);
        this.buttonTest.TabIndex = 9;
        this.buttonTest.Text = "Test";
        this.buttonTest.UseVisualStyleBackColor = true;
        this.buttonTest.Click += new System.EventHandler(this.OnButtonTestClick);
        // 
        // labelEnterArguments
        // 
        this.labelEnterArguments.AutoSize = true;
        this.labelEnterArguments.Location = new System.Drawing.Point(12, 13);
        this.labelEnterArguments.Name = "labelEnterArguments";
        this.labelEnterArguments.Size = new System.Drawing.Size(154, 20);
        this.labelEnterArguments.TabIndex = 11;
        this.labelEnterArguments.Text = "Enter command line:";
        // 
        // labelHelp
        // 
        this.labelHelp.Location = new System.Drawing.Point(15, 124);
        this.labelHelp.Name = "labelHelp";
        this.labelHelp.Size = new System.Drawing.Size(392, 157);
        this.labelHelp.TabIndex = 12;
        this.labelHelp.Text = "Help";
        // 
        // buttonCancel
        // 
        this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.buttonCancel.Location = new System.Drawing.Point(429, 292);
        this.buttonCancel.Name = "buttonCancel";
        this.buttonCancel.Size = new System.Drawing.Size(75, 23);
        this.buttonCancel.TabIndex = 13;
        this.buttonCancel.Text = "Cancel";
        this.buttonCancel.UseVisualStyleBackColor = true;
        // 
        // labelTestResult
        // 
        this.labelTestResult.Location = new System.Drawing.Point(12, 68);
        this.labelTestResult.Multiline = true;
        this.labelTestResult.Name = "labelTestResult";
        this.labelTestResult.ReadOnly = true;
        this.labelTestResult.Size = new System.Drawing.Size(395, 48);
        this.labelTestResult.TabIndex = 14;
        // 
        // ToolArgsDialog
        // 
        this.AcceptButton = this.buttonOk;
        this.CancelButton = this.buttonCancel;
        this.ClientSize = new System.Drawing.Size(516, 327);
        this.Controls.Add(this.labelTestResult);
        this.Controls.Add(this.buttonCancel);
        this.Controls.Add(this.labelHelp);
        this.Controls.Add(this.labelEnterArguments);
        this.Controls.Add(this.buttonTest);
        this.Controls.Add(this.textBoxArguments);
        this.Controls.Add(this.buttonRegexHelp);
        this.Controls.Add(this.buttonOk);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ToolArgsDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Tool Arguments Help";
        this.Load += new System.EventHandler(this.OnToolArgsDialogLoad);
        this.ResumeLayout(false);
        this.PerformLayout();

}

#endregion

private System.Windows.Forms.Button buttonOk;
private System.Windows.Forms.Button buttonRegexHelp;
private System.Windows.Forms.TextBox textBoxArguments;
private System.Windows.Forms.Button buttonTest;
private System.Windows.Forms.Label labelEnterArguments;
private System.Windows.Forms.Label labelHelp;
private System.Windows.Forms.Button buttonCancel;
private System.Windows.Forms.TextBox labelTestResult;
}