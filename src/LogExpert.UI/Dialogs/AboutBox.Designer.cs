using System.Runtime.Versioning;

namespace LogExpert.UI.Dialogs;

partial class AboutBox
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
        tableLayoutPanel = new TableLayoutPanel();
        logoPictureBox = new PictureBox();
        labelProductName = new Label();
        labelVersion = new Label();
        labelCopyright = new Label();
        linkLabelURL = new LinkLabel();
        okPanel = new Panel();
        okButton = new Button();
        usedComponentsDataGrid = new DataGridView();
        tableLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
        okPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)usedComponentsDataGrid).BeginInit();
        SuspendLayout();
        // 
        // tableLayoutPanel
        // 
        tableLayoutPanel.ColumnCount = 2;
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34.25484F));
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65.74516F));
        tableLayoutPanel.Controls.Add(logoPictureBox, 0, 0);
        tableLayoutPanel.Controls.Add(labelProductName, 1, 0);
        tableLayoutPanel.Controls.Add(labelVersion, 1, 1);
        tableLayoutPanel.Controls.Add(labelCopyright, 1, 2);
        tableLayoutPanel.Controls.Add(linkLabelURL, 1, 3);
        tableLayoutPanel.Controls.Add(okPanel, 1, 5);
        tableLayoutPanel.Controls.Add(usedComponentsDataGrid, 1, 4);
        tableLayoutPanel.Dock = DockStyle.Fill;
        tableLayoutPanel.Location = new Point(14, 14);
        tableLayoutPanel.Margin = new Padding(4, 5, 4, 5);
        tableLayoutPanel.Name = "tableLayoutPanel";
        tableLayoutPanel.RowCount = 6;
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 9.912043F));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 9.912043F));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 9.47226F));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 11.84032F));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 46.68471F));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 12.17862F));
        tableLayoutPanel.Size = new Size(914, 649);
        tableLayoutPanel.TabIndex = 0;
        // 
        // logoPictureBox
        // 
        logoPictureBox.Dock = DockStyle.Fill;
        logoPictureBox.Location = new Point(4, 5);
        logoPictureBox.Margin = new Padding(4, 5, 4, 5);
        logoPictureBox.Name = "logoPictureBox";
        tableLayoutPanel.SetRowSpan(logoPictureBox, 6);
        logoPictureBox.Size = new Size(305, 639);
        logoPictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
        logoPictureBox.TabIndex = 12;
        logoPictureBox.TabStop = false;
        // 
        // labelProductName
        // 
        labelProductName.Dock = DockStyle.Fill;
        labelProductName.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
        labelProductName.Location = new Point(322, 0);
        labelProductName.Margin = new Padding(9, 0, 4, 0);
        labelProductName.MaximumSize = new Size(0, 26);
        labelProductName.Name = "labelProductName";
        labelProductName.Size = new Size(588, 26);
        labelProductName.TabIndex = 19;
        labelProductName.Text = "Product Name";
        labelProductName.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // labelVersion
        // 
        labelVersion.Dock = DockStyle.Fill;
        labelVersion.Location = new Point(322, 64);
        labelVersion.Margin = new Padding(9, 0, 4, 0);
        labelVersion.MaximumSize = new Size(0, 26);
        labelVersion.Name = "labelVersion";
        labelVersion.Size = new Size(588, 26);
        labelVersion.TabIndex = 0;
        labelVersion.Text = "Version";
        labelVersion.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // labelCopyright
        // 
        labelCopyright.Dock = DockStyle.Fill;
        labelCopyright.Location = new Point(322, 128);
        labelCopyright.Margin = new Padding(9, 0, 4, 0);
        labelCopyright.Name = "labelCopyright";
        labelCopyright.Size = new Size(588, 61);
        labelCopyright.TabIndex = 21;
        labelCopyright.Text = "Copyright";
        labelCopyright.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // linkLabelURL
        // 
        linkLabelURL.AutoSize = true;
        linkLabelURL.Dock = DockStyle.Fill;
        linkLabelURL.Location = new Point(317, 189);
        linkLabelURL.Margin = new Padding(4, 0, 4, 0);
        linkLabelURL.Name = "linkLabelURL";
        linkLabelURL.Size = new Size(593, 76);
        linkLabelURL.TabIndex = 25;
        linkLabelURL.TabStop = true;
        linkLabelURL.Text = "https://github.com/LogExperts/LogExpert";
        linkLabelURL.TextAlign = ContentAlignment.MiddleLeft;
        linkLabelURL.LinkClicked += OnLinkLabelURLClicked;
        // 
        // okPanel
        // 
        okPanel.Controls.Add(okButton);
        okPanel.Dock = DockStyle.Fill;
        okPanel.Location = new Point(317, 572);
        okPanel.Margin = new Padding(4, 5, 4, 5);
        okPanel.Name = "okPanel";
        okPanel.Size = new Size(593, 72);
        okPanel.TabIndex = 26;
        // 
        // okButton
        // 
        okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        okButton.DialogResult = DialogResult.OK;
        okButton.Location = new Point(475, 32);
        okButton.Margin = new Padding(4, 5, 4, 5);
        okButton.Name = "okButton";
        okButton.Size = new Size(114, 35);
        okButton.TabIndex = 0;
        okButton.Text = "&OK";
        okButton.UseVisualStyleBackColor = true;
        // 
        // usedComponentsDataGrid
        // 
        usedComponentsDataGrid.AllowUserToAddRows = false;
        usedComponentsDataGrid.AllowUserToDeleteRows = false;
        usedComponentsDataGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        usedComponentsDataGrid.BackgroundColor = SystemColors.Control;
        usedComponentsDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        usedComponentsDataGrid.Dock = DockStyle.Fill;
        usedComponentsDataGrid.Location = new Point(316, 268);
        usedComponentsDataGrid.Name = "usedComponentsDataGrid";
        usedComponentsDataGrid.ReadOnly = true;
        usedComponentsDataGrid.Size = new Size(595, 296);
        usedComponentsDataGrid.TabIndex = 27;
        // 
        // AboutBox
        // 
        ClientSize = new Size(942, 677);
        Controls.Add(tableLayoutPanel);
        Margin = new Padding(4, 5, 4, 5);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "AboutBox";
        Padding = new Padding(14);
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "AboutBox";
        tableLayoutPanel.ResumeLayout(false);
        tableLayoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
        okPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)usedComponentsDataGrid).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.PictureBox logoPictureBox;
		private System.Windows.Forms.Label labelProductName;
		private System.Windows.Forms.Label labelVersion;
		private System.Windows.Forms.Label labelCopyright;
		private System.Windows.Forms.LinkLabel linkLabelURL;
		private System.Windows.Forms.Panel okPanel;
		private System.Windows.Forms.Button okButton;
    private DataGridView usedComponentsDataGrid;
}
