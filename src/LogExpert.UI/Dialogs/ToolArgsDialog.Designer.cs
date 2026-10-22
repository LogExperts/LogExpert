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
    private void InitializeComponent ()
    {
        buttonOk = new Button();
        buttonRegexHelp = new Button();
        textBoxArguments = new TextBox();
        buttonTest = new Button();
        labelEnterArguments = new Label();
        labelHelp = new Label();
        buttonCancel = new Button();
        labelTestResult = new TextBox();
        SuspendLayout();
        // 
        // buttonOk
        // 
        buttonOk.DialogResult = DialogResult.OK;
        buttonOk.Location = new Point(348, 424);
        buttonOk.Name = "buttonOk";
        buttonOk.Size = new Size(75, 25);
        buttonOk.TabIndex = 1;
        buttonOk.Text = "OK";
        buttonOk.UseVisualStyleBackColor = true;
        buttonOk.Click += OnButtonOkClick;
        // 
        // buttonRegexHelp
        // 
        buttonRegexHelp.Location = new Point(429, 68);
        buttonRegexHelp.Name = "buttonRegexHelp";
        buttonRegexHelp.Size = new Size(75, 25);
        buttonRegexHelp.TabIndex = 2;
        buttonRegexHelp.Text = "RegEx Help";
        buttonRegexHelp.UseVisualStyleBackColor = true;
        buttonRegexHelp.Click += OnButtonRegexHelpClick;
        // 
        // textBoxArguments
        // 
        textBoxArguments.Location = new Point(12, 36);
        textBoxArguments.Name = "textBoxArguments";
        textBoxArguments.Size = new Size(395, 23);
        textBoxArguments.TabIndex = 8;
        // 
        // buttonTest
        // 
        buttonTest.Location = new Point(429, 36);
        buttonTest.Name = "buttonTest";
        buttonTest.Size = new Size(75, 25);
        buttonTest.TabIndex = 9;
        buttonTest.Text = "Test";
        buttonTest.UseVisualStyleBackColor = true;
        buttonTest.Click += OnButtonTestClick;
        // 
        // labelEnterArguments
        // 
        labelEnterArguments.AutoSize = true;
        labelEnterArguments.Location = new Point(12, 13);
        labelEnterArguments.Name = "labelEnterArguments";
        labelEnterArguments.Size = new Size(117, 15);
        labelEnterArguments.TabIndex = 11;
        labelEnterArguments.Text = "Enter command line:";
        // 
        // labelHelp
        // 
        labelHelp.Location = new Point(15, 124);
        labelHelp.Name = "labelHelp";
        labelHelp.Size = new Size(392, 297);
        labelHelp.TabIndex = 12;
        labelHelp.Text = "Help";
        // 
        // buttonCancel
        // 
        buttonCancel.DialogResult = DialogResult.Cancel;
        buttonCancel.Location = new Point(429, 424);
        buttonCancel.Name = "buttonCancel";
        buttonCancel.Size = new Size(75, 25);
        buttonCancel.TabIndex = 13;
        buttonCancel.Text = "Cancel";
        buttonCancel.UseVisualStyleBackColor = true;
        // 
        // labelTestResult
        // 
        labelTestResult.Location = new Point(12, 68);
        labelTestResult.Multiline = true;
        labelTestResult.Name = "labelTestResult";
        labelTestResult.ReadOnly = true;
        labelTestResult.Size = new Size(395, 48);
        labelTestResult.TabIndex = 14;
        // 
        // ToolArgsDialog
        // 
        AcceptButton = buttonOk;
        CancelButton = buttonCancel;
        ClientSize = new Size(516, 461);
        Controls.Add(labelTestResult);
        Controls.Add(buttonCancel);
        Controls.Add(labelHelp);
        Controls.Add(labelEnterArguments);
        Controls.Add(buttonTest);
        Controls.Add(textBoxArguments);
        Controls.Add(buttonRegexHelp);
        Controls.Add(buttonOk);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ToolArgsDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Tool Arguments Help";
        Load += OnToolArgsDialogLoad;
        ResumeLayout(false);
        PerformLayout();

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