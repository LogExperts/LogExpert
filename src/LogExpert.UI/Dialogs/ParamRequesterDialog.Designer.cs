namespace LogExpert.Dialogs;

partial class ParamRequesterDialog
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
        this.labelValueForParameter = new System.Windows.Forms.Label();
        this.buttonCancel = new System.Windows.Forms.Button();
        this.buttonOk = new System.Windows.Forms.Button();
        this.comboBoxValue = new System.Windows.Forms.ComboBox();
        this.SuspendLayout();
        // 
        // labelValueForParameter
        // 
        this.labelValueForParameter.AutoSize = true;
        this.labelValueForParameter.Location = new System.Drawing.Point(13, 25);
        this.labelValueForParameter.Name = "labelValueForParameter";
        this.labelValueForParameter.Size = new System.Drawing.Size(154, 20);
        this.labelValueForParameter.TabIndex = 0;
        this.labelValueForParameter.Text = "&Value for parameter:";
        // 
        // buttonCancel
        // 
        this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.buttonCancel.Location = new System.Drawing.Point(238, 88);
        this.buttonCancel.Name = "buttonCancel";
        this.buttonCancel.Size = new System.Drawing.Size(75, 23);
        this.buttonCancel.TabIndex = 3;
        this.buttonCancel.Text = "Cancel";
        this.buttonCancel.UseVisualStyleBackColor = true;
        // 
        // buttonOk
        // 
        this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonOk.Location = new System.Drawing.Point(148, 88);
        this.buttonOk.Name = "buttonOk";
        this.buttonOk.Size = new System.Drawing.Size(75, 23);
        this.buttonOk.TabIndex = 2;
        this.buttonOk.Text = "OK";
        this.buttonOk.UseVisualStyleBackColor = true;
        this.buttonOk.Click += new System.EventHandler(this.OnButtonOkClick);
        // 
        // comboBoxValue
        // 
        this.comboBoxValue.FormattingEnabled = true;
        this.comboBoxValue.Location = new System.Drawing.Point(16, 48);
        this.comboBoxValue.Name = "comboBoxValue";
        this.comboBoxValue.Size = new System.Drawing.Size(297, 28);
        this.comboBoxValue.TabIndex = 1;
        // 
        // ParamRequesterDialog
        // 
        this.AcceptButton = this.buttonOk;
        this.CancelButton = this.buttonCancel;
        this.ClientSize = new System.Drawing.Size(332, 130);
        this.Controls.Add(this.comboBoxValue);
        this.Controls.Add(this.buttonOk);
        this.Controls.Add(this.buttonCancel);
        this.Controls.Add(this.labelValueForParameter);
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ParamRequesterDialog";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Tool parameter";
        this.Shown += new System.EventHandler(this.ParamRequesterDialog_Shown);
        this.ResumeLayout(false);
        this.PerformLayout();

}

#endregion

private System.Windows.Forms.Label labelValueForParameter;
private System.Windows.Forms.Button buttonCancel;
private System.Windows.Forms.Button buttonOk;
private System.Windows.Forms.ComboBox comboBoxValue;
}