namespace LogExpert.UI.Dialogs;

partial class FilterColumnChooser
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
        this.components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FilterColumnChooser));
        this.columnListBox = new System.Windows.Forms.CheckedListBox();
        this.buttonOk = new System.Windows.Forms.Button();
        this.toolTipEmptyColumnNoHit = new System.Windows.Forms.ToolTip(this.components);
        this.emptyColumnNoHitRadioButton = new System.Windows.Forms.RadioButton();
        this.toolTipListBox = new System.Windows.Forms.ToolTip(this.components);
        this.checkBoxExactMatch = new System.Windows.Forms.CheckBox();
        this.groupBox1 = new System.Windows.Forms.GroupBox();
        this.emptyColumnUsePrevRadioButton = new System.Windows.Forms.RadioButton();
        this.emptyColumnHitRadioButton = new System.Windows.Forms.RadioButton();
        this.groupBoxExectMatch = new System.Windows.Forms.GroupBox();
        this.toolTipSearchHit = new System.Windows.Forms.ToolTip(this.components);
        this.toolTipPrevContent = new System.Windows.Forms.ToolTip(this.components);
        this.toolTipExactMatch = new System.Windows.Forms.ToolTip(this.components);
        this.buttonCancel = new System.Windows.Forms.Button();
        this.groupBox1.SuspendLayout();
        this.groupBoxExectMatch.SuspendLayout();
        this.SuspendLayout();
        // 
        // columnListBox
        // 
        this.columnListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
        this.columnListBox.CheckOnClick = true;
        this.columnListBox.Font = new System.Drawing.Font("Courier New", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        this.columnListBox.FormattingEnabled = true;
        this.columnListBox.Location = new System.Drawing.Point(3, 3);
        this.columnListBox.Name = "columnListBox";
        this.columnListBox.Size = new System.Drawing.Size(177, 119);
        this.columnListBox.TabIndex = 0;
        this.toolTipListBox.SetToolTip(this.columnListBox, "Choose one ore more columns to restrict the search operations to the selected col" +
    "umns.");
        // 
        // buttonOk
        // 
        this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonOk.Location = new System.Drawing.Point(3, 309);
        this.buttonOk.Name = "buttonOk";
        this.buttonOk.Size = new System.Drawing.Size(75, 23);
        this.buttonOk.TabIndex = 2;
        this.buttonOk.Text = "OK";
        this.buttonOk.UseVisualStyleBackColor = true;
        this.buttonOk.Click += new System.EventHandler(this.OnOkButtonClick);
        // 
        // toolTipEmptyColumnNoHit
        // 
        this.toolTipEmptyColumnNoHit.ToolTipTitle = "Empty column";
        // 
        // emptyColumnNoHitRadioButton
        // 
        this.emptyColumnNoHitRadioButton.AutoSize = true;
        this.emptyColumnNoHitRadioButton.Location = new System.Drawing.Point(6, 21);
        this.emptyColumnNoHitRadioButton.Name = "emptyColumnNoHitRadioButton";
        this.emptyColumnNoHitRadioButton.Size = new System.Drawing.Size(75, 24);
        this.emptyColumnNoHitRadioButton.TabIndex = 0;
        this.emptyColumnNoHitRadioButton.TabStop = true;
        this.emptyColumnNoHitRadioButton.Text = "No hit";
        this.toolTipEmptyColumnNoHit.SetToolTip(this.emptyColumnNoHitRadioButton, "No search hit on empty columns");
        this.emptyColumnNoHitRadioButton.UseVisualStyleBackColor = true;
        // 
        // toolTipListBox
        // 
        this.toolTipListBox.ToolTipTitle = "Columns";
        // 
        // checkBoxExactMatch
        // 
        this.checkBoxExactMatch.AutoSize = true;
        this.checkBoxExactMatch.Location = new System.Drawing.Point(9, 11);
        this.checkBoxExactMatch.Name = "checkBoxExactMatch";
        this.checkBoxExactMatch.Size = new System.Drawing.Size(123, 24);
        this.checkBoxExactMatch.TabIndex = 4;
        this.checkBoxExactMatch.Text = "Exact match";
        this.toolTipExactMatch.SetToolTip(this.checkBoxExactMatch, "If selected, the search string must match exactly (no substring search)");
        this.checkBoxExactMatch.UseVisualStyleBackColor = true;
        // 
        // groupBox1
        // 
        this.groupBox1.Controls.Add(this.emptyColumnUsePrevRadioButton);
        this.groupBox1.Controls.Add(this.emptyColumnHitRadioButton);
        this.groupBox1.Controls.Add(this.emptyColumnNoHitRadioButton);
        this.groupBox1.Location = new System.Drawing.Point(3, 177);
        this.groupBox1.Name = "groupBox1";
        this.groupBox1.Size = new System.Drawing.Size(177, 108);
        this.groupBox1.TabIndex = 5;
        this.groupBox1.TabStop = false;
        this.groupBox1.Text = "On empty columns";
        // 
        // emptyColumnUsePrevRadioButton
        // 
        this.emptyColumnUsePrevRadioButton.AutoSize = true;
        this.emptyColumnUsePrevRadioButton.Location = new System.Drawing.Point(6, 76);
        this.emptyColumnUsePrevRadioButton.Name = "emptyColumnUsePrevRadioButton";
        this.emptyColumnUsePrevRadioButton.Size = new System.Drawing.Size(155, 24);
        this.emptyColumnUsePrevRadioButton.TabIndex = 2;
        this.emptyColumnUsePrevRadioButton.TabStop = true;
        this.emptyColumnUsePrevRadioButton.Text = "Use prev content";
        this.toolTipPrevContent.SetToolTip(this.emptyColumnUsePrevRadioButton, "An empty column will be a search hit if the previous non-empty column was a searc" +
    "h hit");
        this.emptyColumnUsePrevRadioButton.UseVisualStyleBackColor = true;
        // 
        // emptyColumnHitRadioButton
        // 
        this.emptyColumnHitRadioButton.AutoSize = true;
        this.emptyColumnHitRadioButton.Location = new System.Drawing.Point(6, 49);
        this.emptyColumnHitRadioButton.Name = "emptyColumnHitRadioButton";
        this.emptyColumnHitRadioButton.Size = new System.Drawing.Size(106, 24);
        this.emptyColumnHitRadioButton.TabIndex = 1;
        this.emptyColumnHitRadioButton.TabStop = true;
        this.emptyColumnHitRadioButton.Text = "Search hit";
        this.toolTipSearchHit.SetToolTip(this.emptyColumnHitRadioButton, "An empty column will always be a search hit");
        this.emptyColumnHitRadioButton.UseVisualStyleBackColor = true;
        // 
        // groupBoxExectMatch
        // 
        this.groupBoxExectMatch.Controls.Add(this.checkBoxExactMatch);
        this.groupBoxExectMatch.Location = new System.Drawing.Point(3, 130);
        this.groupBoxExectMatch.Name = "groupBoxExectMatch";
        this.groupBoxExectMatch.Size = new System.Drawing.Size(177, 41);
        this.groupBoxExectMatch.TabIndex = 6;
        this.groupBoxExectMatch.TabStop = false;
        // 
        // toolTipSearchHit
        // 
        this.toolTipSearchHit.ToolTipTitle = "Empty column";
        // 
        // toolTipPrevContent
        // 
        this.toolTipPrevContent.ToolTipTitle = "Empty column";
        // 
        // toolTipExactMatch
        // 
        this.toolTipExactMatch.ToolTipTitle = "Exact match";
        // 
        // buttonCancel
        // 
        this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.buttonCancel.Location = new System.Drawing.Point(104, 309);
        this.buttonCancel.Name = "buttonCancel";
        this.buttonCancel.Size = new System.Drawing.Size(75, 23);
        this.buttonCancel.TabIndex = 7;
        this.buttonCancel.Text = "Cancel";
        this.buttonCancel.UseVisualStyleBackColor = true;
        // 
        // FilterColumnChooser
        // 
        this.AcceptButton = this.buttonOk;
        this.CancelButton = this.buttonCancel;
        this.ClientSize = new System.Drawing.Size(184, 335);
        this.Controls.Add(this.buttonCancel);
        this.Controls.Add(this.groupBoxExectMatch);
        this.Controls.Add(this.groupBox1);
        this.Controls.Add(this.buttonOk);
        this.Controls.Add(this.columnListBox);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
        this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
        this.Name = "FilterColumnChooser";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Columns";
        this.groupBox1.ResumeLayout(false);
        this.groupBox1.PerformLayout();
        this.groupBoxExectMatch.ResumeLayout(false);
        this.groupBoxExectMatch.PerformLayout();
        this.ResumeLayout(false);

}

#endregion

private System.Windows.Forms.CheckedListBox columnListBox;
private System.Windows.Forms.Button buttonOk;
private System.Windows.Forms.ToolTip toolTipEmptyColumnNoHit;
private System.Windows.Forms.ToolTip toolTipListBox;
private System.Windows.Forms.CheckBox checkBoxExactMatch;
private System.Windows.Forms.GroupBox groupBox1;
private System.Windows.Forms.RadioButton emptyColumnUsePrevRadioButton;
private System.Windows.Forms.RadioButton emptyColumnHitRadioButton;
private System.Windows.Forms.RadioButton emptyColumnNoHitRadioButton;
private System.Windows.Forms.GroupBox groupBoxExectMatch;
private System.Windows.Forms.ToolTip toolTipExactMatch;
private System.Windows.Forms.ToolTip toolTipSearchHit;
private System.Windows.Forms.ToolTip toolTipPrevContent;
private System.Windows.Forms.Button buttonCancel;
}