namespace LogExpert.UI.Controls.LogWindow
{
  partial class PatternWindow
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PatternWindow));
      this.splitContainer1 = new System.Windows.Forms.SplitContainer();
      this.panel3 = new System.Windows.Forms.Panel();
      this.splitContainer2 = new System.Windows.Forms.SplitContainer();
      this.panel2 = new System.Windows.Forms.Panel();
      this.blockCountLabel = new System.Windows.Forms.Label();
      this.label1 = new System.Windows.Forms.Label();
      this.panel1 = new System.Windows.Forms.Panel();
      this.label2 = new System.Windows.Forms.Label();
      this.blockLinesLabel = new System.Windows.Forms.Label();
      this.panel4 = new System.Windows.Forms.Panel();
      this.setRangeButton = new System.Windows.Forms.Button();
      this.label7 = new System.Windows.Forms.Label();
      this.recalcButton = new System.Windows.Forms.Button();
      this.label6 = new System.Windows.Forms.Label();
      this.label5 = new System.Windows.Forms.Label();
      this.label4 = new System.Windows.Forms.Label();
      this.label3 = new System.Windows.Forms.Label();
      this.rangeLabel = new System.Windows.Forms.Label();
      this.weigthKnobControl = new KnobControl();
      this.maxMissesKnobControl = new KnobControl();
      this.maxDiffKnobControl = new KnobControl();
      this.fuzzyKnobControl = new KnobControl();
      this.patternHitsDataGridView = new LogExpert.Dialogs.BufferedDataGridView();
      this.contentDataGridView = new LogExpert.Dialogs.BufferedDataGridView();
      this.splitContainer1.Panel1.SuspendLayout();
      this.splitContainer1.Panel2.SuspendLayout();
      this.splitContainer1.SuspendLayout();
      this.splitContainer2.Panel1.SuspendLayout();
      this.splitContainer2.Panel2.SuspendLayout();
      this.splitContainer2.SuspendLayout();
      this.panel2.SuspendLayout();
      this.panel1.SuspendLayout();
      this.panel4.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.patternHitsDataGridView)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(this.contentDataGridView)).BeginInit();
      this.SuspendLayout();
      // 
      // splitContainer1
      // 
      this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.splitContainer1.BackColor = System.Drawing.SystemColors.Control;
      this.splitContainer1.IsSplitterFixed = true;
      this.splitContainer1.Location = new System.Drawing.Point(2, 1);
      this.splitContainer1.Name = "splitContainer1";
      this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
      // 
      // splitContainer1.Panel1
      // 
      this.splitContainer1.Panel1.Controls.Add(this.panel3);
      this.splitContainer1.Panel1Collapsed = true;
      this.splitContainer1.Panel1MinSize = 0;
      // 
      // splitContainer1.Panel2
      // 
      this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
      this.splitContainer1.Panel2MinSize = 0;
      this.splitContainer1.Size = new System.Drawing.Size(795, 106);
      this.splitContainer1.SplitterDistance = 106;
      this.splitContainer1.TabIndex = 3;
      // 
      // panel3
      // 
      this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panel3.Location = new System.Drawing.Point(0, 0);
      this.panel3.Name = "panel3";
      this.panel3.Size = new System.Drawing.Size(150, 106);
      this.panel3.TabIndex = 1;
      // 
      // splitContainer2
      // 
      this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
      this.splitContainer2.Location = new System.Drawing.Point(0, 0);
      this.splitContainer2.Name = "splitContainer2";
      // 
      // splitContainer2.Panel1
      // 
      this.splitContainer2.Panel1.Controls.Add(this.panel2);
      // 
      // splitContainer2.Panel2
      // 
      this.splitContainer2.Panel2.Controls.Add(this.panel1);
      this.splitContainer2.Size = new System.Drawing.Size(795, 106);
      this.splitContainer2.SplitterDistance = 294;
      this.splitContainer2.TabIndex = 2;
      // 
      // panel2
      // 
      this.panel2.Controls.Add(this.blockCountLabel);
      this.panel2.Controls.Add(this.label1);
      this.panel2.Controls.Add(this.patternHitsDataGridView);
      this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panel2.Location = new System.Drawing.Point(0, 0);
      this.panel2.Name = "panel2";
      this.panel2.Size = new System.Drawing.Size(294, 106);
      this.panel2.TabIndex = 2;
      // 
      // blockCountLabel
      // 
      this.blockCountLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.blockCountLabel.AutoSize = true;
      this.blockCountLabel.Location = new System.Drawing.Point(184, 89);
      this.blockCountLabel.Name = "blockCountLabel";
      this.blockCountLabel.Size = new System.Drawing.Size(13, 13);
      this.blockCountLabel.TabIndex = 3;
      this.blockCountLabel.Text = "0";
      // 
      // label1
      // 
      this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(0, 89);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(175, 13);
      this.label1.TabIndex = 2;
      this.label1.Text = "Number of blocks (pattern variants):";
      // 
      // panel1
      // 
      this.panel1.Controls.Add(this.label2);
      this.panel1.Controls.Add(this.blockLinesLabel);
      this.panel1.Controls.Add(this.contentDataGridView);
      this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panel1.Location = new System.Drawing.Point(0, 0);
      this.panel1.Name = "panel1";
      this.panel1.Size = new System.Drawing.Size(497, 106);
      this.panel1.TabIndex = 1;
      // 
      // label2
      // 
      this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(3, 89);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(61, 13);
      this.label2.TabIndex = 2;
      this.label2.Text = "Block lines:";
      // 
      // blockLinesLabel
      // 
      this.blockLinesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.blockLinesLabel.Location = new System.Drawing.Point(70, 89);
      this.blockLinesLabel.Name = "blockLinesLabel";
      this.blockLinesLabel.Size = new System.Drawing.Size(69, 13);
      this.blockLinesLabel.TabIndex = 1;
      this.blockLinesLabel.Text = "0";
      // 
      // panel4
      // 
      this.panel4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.panel4.Controls.Add(this.setRangeButton);
      this.panel4.Controls.Add(this.label7);
      this.panel4.Controls.Add(this.recalcButton);
      this.panel4.Controls.Add(this.weigthKnobControl);
      this.panel4.Controls.Add(this.label6);
      this.panel4.Controls.Add(this.maxMissesKnobControl);
      this.panel4.Controls.Add(this.label5);
      this.panel4.Controls.Add(this.maxDiffKnobControl);
      this.panel4.Controls.Add(this.label4);
      this.panel4.Controls.Add(this.fuzzyKnobControl);
      this.panel4.Location = new System.Drawing.Point(3, 106);
      this.panel4.Name = "panel4";
      this.panel4.Size = new System.Drawing.Size(345, 57);
      this.panel4.TabIndex = 5;
      // 
      // setRangeButton
      // 
      this.setRangeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.setRangeButton.Location = new System.Drawing.Point(267, 5);
      this.setRangeButton.Name = "setRangeButton";
      this.setRangeButton.Size = new System.Drawing.Size(75, 23);
      this.setRangeButton.TabIndex = 12;
      this.setRangeButton.Text = "Set range";
      this.setRangeButton.UseVisualStyleBackColor = true;
      this.setRangeButton.Click += new System.EventHandler(this.OnSetRangeButtonClick);
      // 
      // label7
      // 
      this.label7.AutoSize = true;
      this.label7.Location = new System.Drawing.Point(192, 41);
      this.label7.Name = "label7";
      this.label7.Size = new System.Drawing.Size(41, 13);
      this.label7.TabIndex = 11;
      this.label7.Text = "Weigth";
      // 
      // recalcButton
      // 
      this.recalcButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.recalcButton.Location = new System.Drawing.Point(267, 30);
      this.recalcButton.Name = "recalcButton";
      this.recalcButton.Size = new System.Drawing.Size(75, 23);
      this.recalcButton.TabIndex = 6;
      this.recalcButton.Text = "Recalc";
      this.recalcButton.UseVisualStyleBackColor = true;
      this.recalcButton.Click += new System.EventHandler(this.OnRecalcButtonClick);
      // 
      // label6
      // 
      this.label6.AutoSize = true;
      this.label6.Location = new System.Drawing.Point(113, 41);
      this.label6.Name = "label6";
      this.label6.Size = new System.Drawing.Size(61, 13);
      this.label6.TabIndex = 9;
      this.label6.Text = "Max misses";
      // 
      // label5
      // 
      this.label5.AutoSize = true;
      this.label5.Location = new System.Drawing.Point(57, 41);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(44, 13);
      this.label5.TabIndex = 7;
      this.label5.Text = "Max diff";
      // 
      // label4
      // 
      this.label4.AutoSize = true;
      this.label4.Location = new System.Drawing.Point(6, 41);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(34, 13);
      this.label4.TabIndex = 5;
      this.label4.Text = "Fuzzy";
      // 
      // label3
      // 
      this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.label3.Location = new System.Drawing.Point(483, 110);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(303, 49);
      this.label3.TabIndex = 12;
      this.label3.Text = "This feature is pre-beta and does not work :)\r\nUsage: Select a range in the log w" +
          "indow and press \"Recalc\". \r\nThis will search for text ranges similar to the sele" +
          "cted one.";
      // 
      // rangeLabel
      // 
      this.rangeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.rangeLabel.Location = new System.Drawing.Point(352, 114);
      this.rangeLabel.Name = "rangeLabel";
      this.rangeLabel.Size = new System.Drawing.Size(125, 42);
      this.rangeLabel.TabIndex = 13;
      this.rangeLabel.Text = "(no range set)";
      // 
      // weigthKnobControl
      // 
      this.weigthKnobControl.Location = new System.Drawing.Point(202, 5);
      this.weigthKnobControl.MaxValue = 30;
      this.weigthKnobControl.MinValue = 1;
      this.weigthKnobControl.Name = "weigthKnobControl";
      this.weigthKnobControl.Size = new System.Drawing.Size(21, 35);
      this.weigthKnobControl.TabIndex = 10;
      this.weigthKnobControl.Value = 0;
      // 
      // maxMissesKnobControl
      // 
      this.maxMissesKnobControl.Location = new System.Drawing.Point(134, 5);
      this.maxMissesKnobControl.MaxValue = 30;
      this.maxMissesKnobControl.MinValue = 0;
      this.maxMissesKnobControl.Name = "maxMissesKnobControl";
      this.maxMissesKnobControl.Size = new System.Drawing.Size(22, 35);
      this.maxMissesKnobControl.TabIndex = 8;
      this.maxMissesKnobControl.Value = 0;
      // 
      // maxDiffKnobControl
      // 
      this.maxDiffKnobControl.Location = new System.Drawing.Point(69, 5);
      this.maxDiffKnobControl.MaxValue = 30;
      this.maxDiffKnobControl.MinValue = 0;
      this.maxDiffKnobControl.Name = "maxDiffKnobControl";
      this.maxDiffKnobControl.Size = new System.Drawing.Size(21, 35);
      this.maxDiffKnobControl.TabIndex = 6;
      this.maxDiffKnobControl.Value = 0;
      // 
      // fuzzyKnobControl
      // 
      this.fuzzyKnobControl.Location = new System.Drawing.Point(9, 5);
      this.fuzzyKnobControl.MaxValue = 20;
      this.fuzzyKnobControl.MinValue = 0;
      this.fuzzyKnobControl.Name = "fuzzyKnobControl";
      this.fuzzyKnobControl.Size = new System.Drawing.Size(22, 35);
      this.fuzzyKnobControl.TabIndex = 4;
      this.fuzzyKnobControl.Value = 0;
      // 
      // patternHitsDataGridView
      // 
      this.patternHitsDataGridView.AllowUserToAddRows = false;
      this.patternHitsDataGridView.AllowUserToDeleteRows = false;
      this.patternHitsDataGridView.AllowUserToResizeRows = false;
      this.patternHitsDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.patternHitsDataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
      this.patternHitsDataGridView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
      this.patternHitsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.patternHitsDataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
      this.patternHitsDataGridView.EditModeMenuStrip = null;
      this.patternHitsDataGridView.Location = new System.Drawing.Point(3, 3);
      this.patternHitsDataGridView.MultiSelect = false;
      this.patternHitsDataGridView.Name = "patternHitsDataGridView";
      this.patternHitsDataGridView.ReadOnly = true;
      this.patternHitsDataGridView.RowHeadersVisible = false;
      this.patternHitsDataGridView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.patternHitsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
      this.patternHitsDataGridView.ShowCellToolTips = false;
      this.patternHitsDataGridView.Size = new System.Drawing.Size(289, 83);
      this.patternHitsDataGridView.TabIndex = 1;
      this.patternHitsDataGridView.VirtualMode = true;
      this.patternHitsDataGridView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.OnPatternHitsDataGridViewMouseDoubleClick);
      this.patternHitsDataGridView.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.OnPatternHitsDataGridViewCellValueNeeded);
      this.patternHitsDataGridView.ColumnDividerDoubleClick += new System.Windows.Forms.DataGridViewColumnDividerDoubleClickEventHandler(this.OnPatternHitsDataGridViewColumnDividerDoubleClick);
      this.patternHitsDataGridView.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.OnPatternHitsDataGridViewCellPainting);
      this.patternHitsDataGridView.CurrentCellChanged += new System.EventHandler(this.OnPatternHitsDataGridViewCurrentCellChanged);
      // 
      // contentDataGridView
      // 
      this.contentDataGridView.AllowUserToAddRows = false;
      this.contentDataGridView.AllowUserToDeleteRows = false;
      this.contentDataGridView.AllowUserToResizeRows = false;
      this.contentDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.contentDataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
      this.contentDataGridView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
      this.contentDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this.contentDataGridView.EditModeMenuStrip = null;
      this.contentDataGridView.Location = new System.Drawing.Point(3, 3);
      this.contentDataGridView.Name = "contentDataGridView";
      this.contentDataGridView.ReadOnly = true;
      this.contentDataGridView.RowHeadersVisible = false;
      this.contentDataGridView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.contentDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
      this.contentDataGridView.ShowCellToolTips = false;
      this.contentDataGridView.Size = new System.Drawing.Size(491, 83);
      this.contentDataGridView.TabIndex = 0;
      this.contentDataGridView.VirtualMode = true;
      this.contentDataGridView.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.OnContentDataGridViewCellValueNeeded);
      this.contentDataGridView.ColumnDividerDoubleClick += new System.Windows.Forms.DataGridViewColumnDividerDoubleClickEventHandler(this.OnContentDataGridViewColumnDividerDoubleClick);
      this.contentDataGridView.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.OnContentDataGridViewCellPainting);
      this.contentDataGridView.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.OnContentDataGridViewCellMouseDoubleClick);
      // 
      // PatternWindow
      // 
      this.ClientSize = new System.Drawing.Size(798, 165);
      this.Controls.Add(this.rangeLabel);
      this.Controls.Add(this.label3);
      this.Controls.Add(this.panel4);
      this.Controls.Add(this.splitContainer1);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.MaximizeBox = false;
      this.Name = "PatternWindow";
      this.Text = "Patterns";
      this.splitContainer1.Panel1.ResumeLayout(false);
      this.splitContainer1.Panel2.ResumeLayout(false);
      this.splitContainer1.ResumeLayout(false);
      this.splitContainer2.Panel1.ResumeLayout(false);
      this.splitContainer2.Panel2.ResumeLayout(false);
      this.splitContainer2.ResumeLayout(false);
      this.panel2.ResumeLayout(false);
      this.panel2.PerformLayout();
      this.panel1.ResumeLayout(false);
      this.panel1.PerformLayout();
      this.panel4.ResumeLayout(false);
      this.panel4.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.patternHitsDataGridView)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(this.contentDataGridView)).EndInit();
      this.ResumeLayout(false);

    }

    #endregion

    private LogExpert.Dialogs.BufferedDataGridView patternHitsDataGridView;
    private System.Windows.Forms.SplitContainer splitContainer1;
    private System.Windows.Forms.SplitContainer splitContainer2;
    private LogExpert.Dialogs.BufferedDataGridView contentDataGridView;
    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label blockLinesLabel;
    private System.Windows.Forms.Panel panel2;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label blockCountLabel;
    private System.Windows.Forms.Panel panel3;
    private KnobControl fuzzyKnobControl;
    private System.Windows.Forms.Panel panel4;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label label5;
    private KnobControl maxDiffKnobControl;
    private System.Windows.Forms.Label label6;
    private KnobControl maxMissesKnobControl;
    private System.Windows.Forms.Label label7;
    private KnobControl weigthKnobControl;
    private System.Windows.Forms.Button recalcButton;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Button setRangeButton;
    private System.Windows.Forms.Label rangeLabel;
  }
}