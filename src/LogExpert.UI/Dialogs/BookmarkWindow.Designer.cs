namespace LogExpert.Dialogs; 
partial class BookmarkWindow {
/// <summary>
/// Required designer variable.
/// </summary>
private System.ComponentModel.IContainer components = null;

/// <summary>
/// Clean up any resources being used.
/// </summary>
/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
protected override void Dispose(bool disposing) {
  if (disposing && (components != null)) {
    components.Dispose();
  }
  base.Dispose(disposing);
}

#region Windows Form Designer generated code

/// <summary>
/// Required method for Designer support - do not modify
/// the contents of this method with the code editor.
/// </summary>
private void InitializeComponent() {
  this.components = new System.ComponentModel.Container();
  System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BookmarkWindow));
  this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
  this.deleteBookmarkssToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
  this.removeCommentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
  this.bookmarkTextBox = new System.Windows.Forms.TextBox();
  this.splitContainer1 = new System.Windows.Forms.SplitContainer();
  this.bookmarkDataGridView = new LogExpert.Dialogs.BufferedDataGridView();
  this.commentColumnCheckBox = new System.Windows.Forms.CheckBox();
  this.label1 = new System.Windows.Forms.Label();
  this.contextMenuStrip1.SuspendLayout();
  this.splitContainer1.Panel1.SuspendLayout();
  this.splitContainer1.Panel2.SuspendLayout();
  this.splitContainer1.SuspendLayout();
  ((System.ComponentModel.ISupportInitialize)(this.bookmarkDataGridView)).BeginInit();
  this.SuspendLayout();
  // 
  // contextMenuStrip1
  // 
  this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
        this.deleteBookmarkssToolStripMenuItem,
        this.removeCommentsToolStripMenuItem});
  this.contextMenuStrip1.Name = "contextMenuStrip1";
  this.contextMenuStrip1.Size = new System.Drawing.Size(186, 48);
  // 
  // deleteBookmarkssToolStripMenuItem
  // 
  this.deleteBookmarkssToolStripMenuItem.Name = "deleteBookmarkssToolStripMenuItem";
  this.deleteBookmarkssToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
  this.deleteBookmarkssToolStripMenuItem.Text = "Delete bookmarks(s)";
  this.deleteBookmarkssToolStripMenuItem.Click += new System.EventHandler(this.OnDeleteBookmarksToolStripMenuItemClick);
  // 
  // removeCommentsToolStripMenuItem
  // 
  this.removeCommentsToolStripMenuItem.Name = "removeCommentsToolStripMenuItem";
  this.removeCommentsToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
  this.removeCommentsToolStripMenuItem.Text = "Remove comment(s)";
  this.removeCommentsToolStripMenuItem.Click += new System.EventHandler(this.OnRemoveCommentsToolStripMenuItemClick);
  // 
  // bookmarkTextBox
  // 
  this.bookmarkTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
              | System.Windows.Forms.AnchorStyles.Left)
              | System.Windows.Forms.AnchorStyles.Right)));
  this.bookmarkTextBox.Location = new System.Drawing.Point(0, 20);
  this.bookmarkTextBox.Multiline = true;
  this.bookmarkTextBox.Name = "bookmarkTextBox";
  this.bookmarkTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
  this.bookmarkTextBox.Size = new System.Drawing.Size(194, 114);
  this.bookmarkTextBox.TabIndex = 0;
  this.bookmarkTextBox.TextChanged += new System.EventHandler(this.OnBookmarkTextBoxTextChanged);
  // 
  // splitContainer1
  // 
  this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
  this.splitContainer1.Location = new System.Drawing.Point(0, 0);
  this.splitContainer1.Name = "splitContainer1";
  // 
  // splitContainer1.Panel1
  // 
  this.splitContainer1.Panel1.Controls.Add(this.bookmarkDataGridView);
  this.splitContainer1.Panel1MinSize = 40;
  // 
  // splitContainer1.Panel2
  // 
  this.splitContainer1.Panel2.Controls.Add(this.commentColumnCheckBox);
  this.splitContainer1.Panel2.Controls.Add(this.label1);
  this.splitContainer1.Panel2.Controls.Add(this.bookmarkTextBox);
  this.splitContainer1.Size = new System.Drawing.Size(717, 158);
  this.splitContainer1.SplitterDistance = 517;
  this.splitContainer1.TabIndex = 7;
  // 
  // bookmarkDataGridView
  // 
  this.bookmarkDataGridView.AllowUserToAddRows = false;
  this.bookmarkDataGridView.AllowUserToDeleteRows = false;
  this.bookmarkDataGridView.AllowUserToOrderColumns = true;
  this.bookmarkDataGridView.AllowUserToResizeRows = false;
  this.bookmarkDataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
  this.bookmarkDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
  this.bookmarkDataGridView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
  this.bookmarkDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
  this.bookmarkDataGridView.ContextMenuStrip = this.contextMenuStrip1;
  this.bookmarkDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
  this.bookmarkDataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
  this.bookmarkDataGridView.EditModeMenuStrip = null;
  this.bookmarkDataGridView.Location = new System.Drawing.Point(0, 0);
  this.bookmarkDataGridView.Margin = new System.Windows.Forms.Padding(0);
  this.bookmarkDataGridView.Name = "bookmarkDataGridView";
  this.bookmarkDataGridView.PaintWithOverlays = false;
  this.bookmarkDataGridView.ReadOnly = true;
  this.bookmarkDataGridView.RowHeadersVisible = false;
  this.bookmarkDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
  this.bookmarkDataGridView.Size = new System.Drawing.Size(517, 158);
  this.bookmarkDataGridView.TabIndex = 0;
  this.bookmarkDataGridView.VirtualMode = true;
  this.bookmarkDataGridView.Enter += new System.EventHandler(this.OnBookmarkGridViewEnter);
  this.bookmarkDataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnBookmarkDataGridViewCellDoubleClick);
  this.bookmarkDataGridView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.OnBoomarkDataGridViewMouseDoubleClick);
  this.bookmarkDataGridView.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.OnBookmarkDataGridViewPreviewKeyDown);
  this.bookmarkDataGridView.Leave += new System.EventHandler(this.OnBookmarkGridViewLeave);
  this.bookmarkDataGridView.ColumnDividerDoubleClick += new System.Windows.Forms.DataGridViewColumnDividerDoubleClickEventHandler(this.OnBoomarkDataGridViewColumnDividerDoubleClick);
  this.bookmarkDataGridView.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.OnBookmarkDataGridViewRowsAdded);
  this.bookmarkDataGridView.SelectionChanged += new System.EventHandler(OnBookmarkDataGridViewSelectionChanged);
  this.bookmarkDataGridView.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(this.OnBookmarkDataGridViewCellToolTipTextNeeded);
  this.bookmarkDataGridView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnBookmarkGridViewKeyDown);
  this.bookmarkDataGridView.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.OnBookmarkDataGridViewRowsRemoved);
  // 
  // commentColumnCheckBox
  // 
  this.commentColumnCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
  this.commentColumnCheckBox.AutoSize = true;
  this.commentColumnCheckBox.Location = new System.Drawing.Point(7, 138);
  this.commentColumnCheckBox.Name = "commentColumnCheckBox";
  this.commentColumnCheckBox.Size = new System.Drawing.Size(136, 17);
  this.commentColumnCheckBox.TabIndex = 8;
  this.commentColumnCheckBox.Text = "Show comment column";
  this.commentColumnCheckBox.UseVisualStyleBackColor = true;
  this.commentColumnCheckBox.CheckedChanged += new System.EventHandler(this.OnCommentColumnCheckBoxCheckedChanged);
  // 
  // label1
  // 
  this.label1.AutoSize = true;
  this.label1.Location = new System.Drawing.Point(4, 4);
  this.label1.Name = "label1";
  this.label1.Size = new System.Drawing.Size(104, 13);
  this.label1.TabIndex = 7;
  this.label1.Text = "Bookmark comment:";
  // 
  // BookmarkWindow
  // 
  this.ClientSize = new System.Drawing.Size(717, 158);
  this.ControlBox = false;
  this.Controls.Add(this.splitContainer1);
  this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas)(((((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft)
              | WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight)
              | WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop)
              | WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom)));
  this.DoubleBuffered = true;
  this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
  this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
  this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
  this.MaximizeBox = false;
  this.MinimizeBox = false;
  this.Name = "BookmarkWindow";
  this.ShowIcon = false;
  this.ShowInTaskbar = false;
  this.Text = "Bookmarks";
  this.ClientSizeChanged += new System.EventHandler(this.BookmarkWindow_ClientSizeChanged);
  this.SizeChanged += new System.EventHandler(this.OnBookmarkWindowSizeChanged);
  this.contextMenuStrip1.ResumeLayout(false);
  this.splitContainer1.Panel1.ResumeLayout(false);
  this.splitContainer1.Panel2.ResumeLayout(false);
  this.splitContainer1.Panel2.PerformLayout();
  this.splitContainer1.ResumeLayout(false);
  ((System.ComponentModel.ISupportInitialize)(this.bookmarkDataGridView)).EndInit();
  this.ResumeLayout(false);
}

#endregion

private BufferedDataGridView bookmarkDataGridView;
private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
private System.Windows.Forms.ToolStripMenuItem deleteBookmarkssToolStripMenuItem;
private System.Windows.Forms.TextBox bookmarkTextBox;
private System.Windows.Forms.SplitContainer splitContainer1;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.ToolStripMenuItem removeCommentsToolStripMenuItem;
private System.Windows.Forms.CheckBox commentColumnCheckBox;
}