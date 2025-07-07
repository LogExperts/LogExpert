namespace LogExpert.UI.Dialogs;

	partial class MultiFileMaskDialog
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MultiFileMaskDialog));
        this.labelMultiSettingsFor = new System.Windows.Forms.Label();
        this.labelFileName = new System.Windows.Forms.Label();
        this.labelFileNamePattern = new System.Windows.Forms.Label();
        this.upDownMaxDays = new System.Windows.Forms.NumericUpDown();
        this.fileNamePatternTextBox = new System.Windows.Forms.TextBox();
        this.labelMaxDays = new System.Windows.Forms.Label();
        this.Settings = new System.Windows.Forms.GroupBox();
        this.syntaxHelpLabel = new System.Windows.Forms.Label();
        this.buttonOk = new System.Windows.Forms.Button();
        this.buttonCancel = new System.Windows.Forms.Button();
        ((System.ComponentModel.ISupportInitialize)(this.upDownMaxDays)).BeginInit();
        this.Settings.SuspendLayout();
        this.SuspendLayout();
        // 
        // labelMultiSettingsFor
        // 
        this.labelMultiSettingsFor.AutoSize = true;
        this.labelMultiSettingsFor.Location = new System.Drawing.Point(12, 13);
        this.labelMultiSettingsFor.Name = "labelMultiSettingsFor";
        this.labelMultiSettingsFor.Size = new System.Drawing.Size(154, 20);
        this.labelMultiSettingsFor.TabIndex = 0;
        this.labelMultiSettingsFor.Text = "MultiFile settings for:";
        // 
        // labelFileName
        // 
        this.labelFileName.AutoSize = true;
        this.labelFileName.Location = new System.Drawing.Point(172, 13);
        this.labelFileName.Name = "labelFileName";
        this.labelFileName.Size = new System.Drawing.Size(91, 20);
        this.labelFileName.TabIndex = 1;
        this.labelFileName.Text = "<file name>";
        // 
        // labelFileNamePattern
        // 
        this.labelFileNamePattern.AutoSize = true;
        this.labelFileNamePattern.Location = new System.Drawing.Point(6, 28);
        this.labelFileNamePattern.Name = "labelFileNamePattern";
        this.labelFileNamePattern.Size = new System.Drawing.Size(137, 20);
        this.labelFileNamePattern.TabIndex = 2;
        this.labelFileNamePattern.Text = "File name pattern:";
        // 
        // upDownMaxDays
        // 
        this.upDownMaxDays.Location = new System.Drawing.Point(91, 55);
        this.upDownMaxDays.Maximum = new decimal(new int[] {
        40,
        0,
        0,
        0});
        this.upDownMaxDays.Name = "upDownMaxDays";
        this.upDownMaxDays.Size = new System.Drawing.Size(49, 26);
        this.upDownMaxDays.TabIndex = 3;
        this.upDownMaxDays.Value = new decimal(new int[] {
        1,
        0,
        0,
        0});
        // 
        // fileNamePatternTextBox
        // 
        this.fileNamePatternTextBox.Location = new System.Drawing.Point(149, 25);
        this.fileNamePatternTextBox.Name = "fileNamePatternTextBox";
        this.fileNamePatternTextBox.Size = new System.Drawing.Size(247, 26);
        this.fileNamePatternTextBox.TabIndex = 4;
        // 
        // labelMaxDays
        // 
        this.labelMaxDays.AutoSize = true;
        this.labelMaxDays.Location = new System.Drawing.Point(6, 57);
        this.labelMaxDays.Name = "labelMaxDays";
        this.labelMaxDays.Size = new System.Drawing.Size(79, 20);
        this.labelMaxDays.TabIndex = 5;
        this.labelMaxDays.Text = "Max days:";
        // 
        // Settings
        // 
        this.Settings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
        this.Settings.Controls.Add(this.labelFileNamePattern);
        this.Settings.Controls.Add(this.labelMaxDays);
        this.Settings.Controls.Add(this.upDownMaxDays);
        this.Settings.Controls.Add(this.fileNamePatternTextBox);
        this.Settings.Location = new System.Drawing.Point(15, 39);
        this.Settings.Name = "Settings";
        this.Settings.Size = new System.Drawing.Size(402, 98);
        this.Settings.TabIndex = 6;
        this.Settings.TabStop = false;
        // 
        // syntaxHelpLabel
        // 
        this.syntaxHelpLabel.Location = new System.Drawing.Point(15, 140);
        this.syntaxHelpLabel.Name = "syntaxHelpLabel";
        this.syntaxHelpLabel.Size = new System.Drawing.Size(402, 194);
        this.syntaxHelpLabel.TabIndex = 7;
        this.syntaxHelpLabel.Text = "Syntax Help Label";
        // 
        // buttonOk
        // 
        this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonOk.Location = new System.Drawing.Point(261, 347);
        this.buttonOk.Name = "buttonOk";
        this.buttonOk.Size = new System.Drawing.Size(75, 23);
        this.buttonOk.TabIndex = 8;
        this.buttonOk.Text = "OK";
        this.buttonOk.UseVisualStyleBackColor = true;
        this.buttonOk.Click += new System.EventHandler(this.OnButtonOKClick);
        // 
        // buttonCancel
        // 
        this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.buttonCancel.Location = new System.Drawing.Point(342, 347);
        this.buttonCancel.Name = "buttonCancel";
        this.buttonCancel.Size = new System.Drawing.Size(75, 23);
        this.buttonCancel.TabIndex = 9;
        this.buttonCancel.Text = "Cancel";
        this.buttonCancel.UseVisualStyleBackColor = true;
        // 
        // MultiFileMaskDialog
        // 
        this.AcceptButton = this.buttonOk;
        this.CancelButton = this.buttonCancel;
        this.ClientSize = new System.Drawing.Size(434, 386);
        this.Controls.Add(this.buttonCancel);
        this.Controls.Add(this.buttonOk);
        this.Controls.Add(this.syntaxHelpLabel);
        this.Controls.Add(this.Settings);
        this.Controls.Add(this.labelFileName);
        this.Controls.Add(this.labelMultiSettingsFor);
        this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.MinimumSize = new System.Drawing.Size(329, 420);
        this.Name = "MultiFileMaskDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "MultiFile settings";
        this.TopMost = true;
        this.Load += new System.EventHandler(this.OnMultiFileMaskDialogLoad);
        ((System.ComponentModel.ISupportInitialize)(this.upDownMaxDays)).EndInit();
        this.Settings.ResumeLayout(false);
        this.Settings.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelMultiSettingsFor;
		private System.Windows.Forms.Label labelFileName;
		private System.Windows.Forms.Label labelFileNamePattern;
		private System.Windows.Forms.NumericUpDown upDownMaxDays;
		private System.Windows.Forms.TextBox fileNamePatternTextBox;
		private System.Windows.Forms.Label labelMaxDays;
		private System.Windows.Forms.GroupBox Settings;
		private System.Windows.Forms.Label syntaxHelpLabel;
		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.Button buttonCancel;
	}