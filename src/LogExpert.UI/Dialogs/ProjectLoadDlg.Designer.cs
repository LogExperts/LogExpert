namespace LogExpert.Dialogs;

partial class ProjectLoadDlg
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectLoadDlg));
        this.labelInformational = new System.Windows.Forms.Label();
        this.buttonCloseTabs = new System.Windows.Forms.Button();
        this.buttonNewWindow = new System.Windows.Forms.Button();
        this.buttonIgnore = new System.Windows.Forms.Button();
        this.labelChooseHowToProceed = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // labelInformational
        // 
        this.labelInformational.Location = new System.Drawing.Point(12, 9);
        this.labelInformational.Name = "labelInformational";
        this.labelInformational.Size = new System.Drawing.Size(178, 41);
        this.labelInformational.TabIndex = 0;
        this.labelInformational.Text = "Restoring layout requires an empty workbench. \r\n\r\n";
        // 
        // buttonCloseTabs
        // 
        this.buttonCloseTabs.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonCloseTabs.Location = new System.Drawing.Point(43, 94);
        this.buttonCloseTabs.Name = "buttonCloseTabs";
        this.buttonCloseTabs.Size = new System.Drawing.Size(113, 23);
        this.buttonCloseTabs.TabIndex = 1;
        this.buttonCloseTabs.Text = "Close existing tabs";
        this.buttonCloseTabs.UseVisualStyleBackColor = true;
        this.buttonCloseTabs.Click += new System.EventHandler(this.OnButtonCloseTabsClick);
        // 
        // buttonNewWindow
        // 
        this.buttonNewWindow.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonNewWindow.Location = new System.Drawing.Point(43, 124);
        this.buttonNewWindow.Name = "buttonNewWindow";
        this.buttonNewWindow.Size = new System.Drawing.Size(113, 23);
        this.buttonNewWindow.TabIndex = 2;
        this.buttonNewWindow.Text = "Open new window";
        this.buttonNewWindow.UseVisualStyleBackColor = true;
        this.buttonNewWindow.Click += new System.EventHandler(this.OnButtonNewWindowClick);
        // 
        // buttonIgnore
        // 
        this.buttonIgnore.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonIgnore.Location = new System.Drawing.Point(43, 154);
        this.buttonIgnore.Name = "buttonIgnore";
        this.buttonIgnore.Size = new System.Drawing.Size(113, 23);
        this.buttonIgnore.TabIndex = 3;
        this.buttonIgnore.Text = "Ignore layout data";
        this.buttonIgnore.UseVisualStyleBackColor = true;
        this.buttonIgnore.Click += new System.EventHandler(this.OnButtonIgnoreClick);
        // 
        // labelChooseHowToProceed
        // 
        this.labelChooseHowToProceed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
        this.labelChooseHowToProceed.AutoSize = true;
        this.labelChooseHowToProceed.Location = new System.Drawing.Point(21, 66);
        this.labelChooseHowToProceed.Name = "labelChooseHowToProceed";
        this.labelChooseHowToProceed.Size = new System.Drawing.Size(230, 20);
        this.labelChooseHowToProceed.TabIndex = 4;
        this.labelChooseHowToProceed.Text = "Please choose how to proceed:";
        // 
        // ProjectLoadDlg
        // 
        this.ClientSize = new System.Drawing.Size(258, 196);
        this.Controls.Add(this.labelChooseHowToProceed);
        this.Controls.Add(this.buttonIgnore);
        this.Controls.Add(this.buttonNewWindow);
        this.Controls.Add(this.buttonCloseTabs);
        this.Controls.Add(this.labelInformational);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "ProjectLoadDlg";
        this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Loading Session";
        this.ResumeLayout(false);
        this.PerformLayout();

}

#endregion

private System.Windows.Forms.Label labelInformational;
private System.Windows.Forms.Button buttonCloseTabs;
private System.Windows.Forms.Button buttonNewWindow;
private System.Windows.Forms.Button buttonIgnore;
private System.Windows.Forms.Label labelChooseHowToProceed;
}