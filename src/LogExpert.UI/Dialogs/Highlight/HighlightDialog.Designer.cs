using LogExpert.UI.Controls;

namespace LogExpert.Dialogs;

partial class HighlightDialog
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose (bool disposing)
    {
        if (disposing && components != null)
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
        components = new System.ComponentModel.Container();
        var resources = new System.ComponentModel.ComponentResourceManager(typeof(HighlightDialog));
        listBoxHighlight = new ListBox();
        btnAdd = new Button();
        btnCopy = new Button();
        btnEdit = new Button();
        btnDelete = new Button();
        btnMoveUp = new Button();
        btnMoveDown = new Button();
        btnOk = new Button();
        btnCancel = new Button();
        helpProvider = new HelpProvider();
        btnExportGroup = new Button();
        btnImportGroup = new Button();
        btnMoveGroupDown = new Button();
        btnMoveGroupUp = new Button();
        labelAssignNamesToGroups = new Label();
        btnCopyGroup = new Button();
        btnDeleteGroup = new Button();
        btnNewGroup = new Button();
        comboBoxGroups = new ComboBox();
        toolTip = new ToolTip(components);
        pnlBackground = new Panel();
        groupBoxGroups = new GroupBox();
        groupBoxSelection = new GroupBox();
        checkBoxSelectionOutline = new CheckBox();
        btnSelectionColor = new Button();
        btnResetSelectionColor = new Button();
        selectionControls = new FlowLayoutPanel();
        pnlBackground.SuspendLayout();
        groupBoxGroups.SuspendLayout();
        SuspendLayout();
        // 
        // listBoxHighlight
        // 
        listBoxHighlight.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        listBoxHighlight.DrawMode = DrawMode.OwnerDrawFixed;
        listBoxHighlight.FormattingEnabled = true;
        listBoxHighlight.Location = new Point(12, 145);
        listBoxHighlight.Margin = new Padding(4, 5, 4, 5);
        listBoxHighlight.Name = "listBoxHighlight";
        listBoxHighlight.Size = new Size(460, 252);
        listBoxHighlight.TabIndex = 0;
        listBoxHighlight.SelectedIndexChanged += OnListBoxHighlightSelectedIndexChanged;
        listBoxHighlight.DoubleClick += OnBtnEditClick;
        // 
        // btnAdd
        // 
        btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnAdd.Location = new Point(478, 142);
        btnAdd.Margin = new Padding(4, 5, 4, 5);
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(85, 35);
        btnAdd.TabIndex = 1;
        btnAdd.Text = "&Add";
        toolTip.SetToolTip(btnAdd, "Create a new highlight item (opens the editor dialog)");
        btnAdd.UseVisualStyleBackColor = true;
        btnAdd.Click += OnAddButtonClick;
        // 
        // btnCopy
        // 
        btnCopy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCopy.Location = new Point(478, 184);
        btnCopy.Margin = new Padding(4, 5, 4, 5);
        btnCopy.Name = "btnCopy";
        btnCopy.Size = new Size(85, 35);
        btnCopy.TabIndex = 2;
        btnCopy.Text = "Co&py";
        toolTip.SetToolTip(btnCopy, "Copy the selected highlight into a new one (opens the editor dialog)");
        btnCopy.UseVisualStyleBackColor = true;
        btnCopy.Click += OnBtnCopyClick;
        // 
        // btnEdit
        // 
        btnEdit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnEdit.Location = new Point(478, 226);
        btnEdit.Margin = new Padding(4, 5, 4, 5);
        btnEdit.Name = "btnEdit";
        btnEdit.Size = new Size(85, 35);
        btnEdit.TabIndex = 3;
        btnEdit.Text = "&Edit";
        toolTip.SetToolTip(btnEdit, "Edit the selected highlight in the editor dialog");
        btnEdit.UseVisualStyleBackColor = true;
        btnEdit.Click += OnBtnEditClick;
        // 
        // btnDelete
        // 
        btnDelete.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnDelete.Location = new Point(478, 268);
        btnDelete.Margin = new Padding(4, 5, 4, 5);
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(85, 35);
        btnDelete.TabIndex = 4;
        btnDelete.Text = "D&elete";
        toolTip.SetToolTip(btnDelete, "Delete the current highlight");
        btnDelete.UseVisualStyleBackColor = true;
        btnDelete.Click += OnDeleteButtonClick;
        // 
        // btnMoveUp
        // 
        btnMoveUp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnMoveUp.Location = new Point(478, 310);
        btnMoveUp.Margin = new Padding(4, 5, 4, 5);
        btnMoveUp.Name = "btnMoveUp";
        btnMoveUp.Size = new Size(85, 35);
        btnMoveUp.TabIndex = 5;
        btnMoveUp.Text = "&Up";
        toolTip.SetToolTip(btnMoveUp, "Move the current highlight one position up");
        btnMoveUp.UseVisualStyleBackColor = true;
        btnMoveUp.Click += OnBtnMoveUpClick;
        // 
        // btnMoveDown
        // 
        btnMoveDown.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnMoveDown.Location = new Point(478, 352);
        btnMoveDown.Margin = new Padding(4, 5, 4, 5);
        btnMoveDown.Name = "btnMoveDown";
        btnMoveDown.Size = new Size(85, 35);
        btnMoveDown.TabIndex = 6;
        btnMoveDown.Text = "&Down";
        toolTip.SetToolTip(btnMoveDown, "Move the current highlight one position down");
        btnMoveDown.UseVisualStyleBackColor = true;
        btnMoveDown.Click += OnBtnMoveDownClick;
        // 
        // btnOk
        // 
        btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new Point(387, 520);
        btnOk.Margin = new Padding(4, 5, 4, 5);
        btnOk.Name = "btnOk";
        btnOk.Size = new Size(85, 35);
        btnOk.TabIndex = 9;
        btnOk.Text = "OK";
        btnOk.UseVisualStyleBackColor = true;
        btnOk.Click += OnBtnOkClick;
        // 
        // btnCancel
        // 
        btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(478, 520);
        btnCancel.Margin = new Padding(4, 5, 4, 5);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(85, 35);
        btnCancel.TabIndex = 10;
        btnCancel.Text = "Cancel";
        btnCancel.UseVisualStyleBackColor = true;
        // 
        // helpProvider
        // 
        helpProvider.HelpNamespace = "LogExpert.chm";
        helpProvider.Tag = "";
        // 
        // btnExportGroup
        // 
        btnExportGroup.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnExportGroup.Location = new Point(108, 520);
        btnExportGroup.Margin = new Padding(4, 5, 4, 5);
        btnExportGroup.Name = "btnExportGroup";
        btnExportGroup.Size = new Size(85, 35);
        btnExportGroup.TabIndex = 2;
        btnExportGroup.Text = "Export";
        toolTip.SetToolTip(btnExportGroup, "Export highlight groups");
        btnExportGroup.UseVisualStyleBackColor = true;
        btnExportGroup.Click += OnBtnExportGroupClick;
        // 
        // btnImportGroup
        // 
        btnImportGroup.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnImportGroup.Location = new Point(12, 520);
        btnImportGroup.Margin = new Padding(4, 5, 4, 5);
        btnImportGroup.Name = "btnImportGroup";
        btnImportGroup.Size = new Size(85, 35);
        btnImportGroup.TabIndex = 1;
        btnImportGroup.Text = "Import";
        toolTip.SetToolTip(btnImportGroup, "Import highlight groups");
        btnImportGroup.UseVisualStyleBackColor = true;
        btnImportGroup.Click += OnBtnImportGroupClick;
        // 
        // btnMoveGroupDown
        // 
        btnMoveGroupDown.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnMoveGroupDown.Location = new Point(383, 53);
        btnMoveGroupDown.Margin = new Padding(4, 5, 4, 5);
        btnMoveGroupDown.Name = "btnMoveGroupDown";
        btnMoveGroupDown.Size = new Size(85, 35);
        btnMoveGroupDown.TabIndex = 6;
        btnMoveGroupDown.Text = "Down";
        toolTip.SetToolTip(btnMoveGroupDown, "Move the current highlight group one position down");
        btnMoveGroupDown.UseVisualStyleBackColor = true;
        btnMoveGroupDown.Click += OnBtnGroupDownClick;
        // 
        // btnMoveGroupUp
        // 
        btnMoveGroupUp.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnMoveGroupUp.Location = new Point(290, 53);
        btnMoveGroupUp.Margin = new Padding(4, 5, 4, 5);
        btnMoveGroupUp.Name = "btnMoveGroupUp";
        btnMoveGroupUp.Size = new Size(85, 35);
        btnMoveGroupUp.TabIndex = 5;
        btnMoveGroupUp.Text = "Up";
        toolTip.SetToolTip(btnMoveGroupUp, "Move the current highlight group one position up");
        btnMoveGroupUp.UseVisualStyleBackColor = true;
        btnMoveGroupUp.Click += OnBtnGroupUpClick;
        // 
        // labelAssignNamesToGroups
        // 
        labelAssignNamesToGroups.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        labelAssignNamesToGroups.AutoSize = true;
        labelAssignNamesToGroups.Location = new Point(8, 93);
        labelAssignNamesToGroups.Margin = new Padding(4, 0, 4, 0);
        labelAssignNamesToGroups.Name = "labelAssignNamesToGroups";
        labelAssignNamesToGroups.Size = new Size(276, 15);
        labelAssignNamesToGroups.TabIndex = 4;
        labelAssignNamesToGroups.Text = "You can assign groups to file names in the settings.";
        // 
        // btnCopyGroup
        // 
        btnCopyGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnCopyGroup.Location = new Point(104, 53);
        btnCopyGroup.Margin = new Padding(4, 5, 4, 5);
        btnCopyGroup.Name = "btnCopyGroup";
        btnCopyGroup.Size = new Size(85, 35);
        btnCopyGroup.TabIndex = 4;
        btnCopyGroup.Text = "Copy Group";
        toolTip.SetToolTip(btnCopyGroup, "Copy the current highlight group into a new one");
        btnCopyGroup.UseVisualStyleBackColor = true;
        btnCopyGroup.Click += OnBtnCopyGroupClick;
        // 
        // btnDeleteGroup
        // 
        btnDeleteGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnDeleteGroup.Location = new Point(197, 53);
        btnDeleteGroup.Margin = new Padding(4, 5, 4, 5);
        btnDeleteGroup.Name = "btnDeleteGroup";
        btnDeleteGroup.Size = new Size(85, 35);
        btnDeleteGroup.TabIndex = 2;
        btnDeleteGroup.Text = "Delete Group";
        toolTip.SetToolTip(btnDeleteGroup, "Delete the current highlight group");
        btnDeleteGroup.UseVisualStyleBackColor = true;
        btnDeleteGroup.Click += OnBtnDelGroupClick;
        // 
        // btnNewGroup
        // 
        btnNewGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnNewGroup.Location = new Point(8, 53);
        btnNewGroup.Margin = new Padding(4, 5, 4, 5);
        btnNewGroup.Name = "btnNewGroup";
        btnNewGroup.Size = new Size(85, 35);
        btnNewGroup.TabIndex = 3;
        btnNewGroup.Text = "New group";
        toolTip.SetToolTip(btnNewGroup, "Create a new empty highlight group");
        btnNewGroup.UseVisualStyleBackColor = true;
        btnNewGroup.Click += OnBtnNewGroupClick;
        // 
        // comboBoxGroups
        // 
        comboBoxGroups.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        comboBoxGroups.DisplayMember = "GroupName";
        comboBoxGroups.DrawMode = DrawMode.OwnerDrawFixed;
        comboBoxGroups.Location = new Point(8, 19);
        comboBoxGroups.Margin = new Padding(4, 5, 4, 5);
        comboBoxGroups.Name = "comboBoxGroups";
        comboBoxGroups.Size = new Size(460, 24);
        comboBoxGroups.TabIndex = 0;
        toolTip.SetToolTip(comboBoxGroups, "Choose a group to create different highlight settings. Type in a name to change in the name of a group.");
        comboBoxGroups.DrawItem += OnCmbBoxGroupDrawItem;
        comboBoxGroups.SelectionChangeCommitted += OnCmbBoxGroupSelectionChangeCommitted;
        comboBoxGroups.TextUpdate += OnCmbBoxGroupTextUpdate;
        // 
        // pnlBackground
        // 
        pnlBackground.Dock = DockStyle.Fill;
        pnlBackground.AutoScroll = true;
        pnlBackground.Controls.Add(btnExportGroup);
        pnlBackground.Controls.Add(listBoxHighlight);
        pnlBackground.Controls.Add(btnImportGroup);
        pnlBackground.Controls.Add(btnAdd);
        pnlBackground.Controls.Add(btnCopy);
        pnlBackground.Controls.Add(btnEdit);
        pnlBackground.Controls.Add(btnDelete);
        pnlBackground.Controls.Add(btnMoveUp);
        pnlBackground.Controls.Add(btnMoveDown);
        pnlBackground.Controls.Add(btnOk);
        pnlBackground.Controls.Add(btnCancel);
        pnlBackground.Controls.Add(groupBoxGroups);
        pnlBackground.Controls.Add(groupBoxSelection);
        pnlBackground.Location = new Point(0, 0);
        pnlBackground.Name = "pnlBackground";
        pnlBackground.Size = new Size(576, 568);
        pnlBackground.TabIndex = 23;
        // 
        // groupBoxGroups
        // 
        groupBoxGroups.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        groupBoxGroups.Controls.Add(btnMoveGroupDown);
        groupBoxGroups.Controls.Add(btnMoveGroupUp);
        groupBoxGroups.Controls.Add(labelAssignNamesToGroups);
        groupBoxGroups.Controls.Add(btnCopyGroup);
        groupBoxGroups.Controls.Add(btnDeleteGroup);
        groupBoxGroups.Controls.Add(btnNewGroup);
        groupBoxGroups.Controls.Add(comboBoxGroups);
        groupBoxGroups.Location = new Point(4, 14);
        groupBoxGroups.Margin = new Padding(4, 5, 4, 5);
        groupBoxGroups.Name = "groupBoxGroups";
        groupBoxGroups.Padding = new Padding(4, 5, 4, 5);
        groupBoxGroups.Size = new Size(568, 117);
        groupBoxGroups.TabIndex = 22;
        groupBoxGroups.TabStop = false;
        groupBoxGroups.Text = "Groups";
        // Selection appearance is application-wide, separate from the group editor.
        groupBoxSelection.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        groupBoxSelection.Location = new Point(12, 404);
        groupBoxSelection.Size = new Size(552, 108);
        groupBoxSelection.TabIndex = 8;
        groupBoxSelection.Controls.Add(selectionControls);
        selectionControls.Dock = DockStyle.Fill;
        selectionControls.Padding = new Padding(6);
        selectionControls.Controls.Add(checkBoxSelectionOutline);
        selectionControls.Controls.Add(btnSelectionColor);
        selectionControls.Controls.Add(btnResetSelectionColor);
        selectionControls.SetFlowBreak(checkBoxSelectionOutline, true);
        checkBoxSelectionOutline.Name = "checkBoxSelectionOutline";
        checkBoxSelectionOutline.AutoSize = true;
        checkBoxSelectionOutline.TabIndex = 0;
        btnSelectionColor.AutoSize = true;
        btnSelectionColor.Name = "btnSelectionColor";
        btnSelectionColor.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnSelectionColor.Padding = new Padding(4);
        btnSelectionColor.TabIndex = 1;
        btnSelectionColor.Click += OnSelectionColorClick;
        btnResetSelectionColor.AutoSize = true;
        btnResetSelectionColor.Name = "btnResetSelectionColor";
        btnResetSelectionColor.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnResetSelectionColor.Padding = new Padding(4);
        btnResetSelectionColor.TabIndex = 2;
        btnResetSelectionColor.Click += OnResetSelectionColorClick;
        // 
        // HighlightDialog
        // 
        AcceptButton = btnOk;
        CancelButton = btnCancel;
        ClientSize = new Size(576, 568);
        Controls.Add(pnlBackground);
        DoubleBuffered = true;
        helpProvider.SetHelpKeyword(this, "Highlighting.htm");
        helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
        helpProvider.SetHelpString(this, "");
        Icon = (Icon)resources.GetObject("$this.Icon");
        Margin = new Padding(4, 5, 4, 5);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(592, 607);
        Name = "HighlightDialog";
        helpProvider.SetShowHelp(this, true);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Highlighting and action triggers";
        Shown += OnHighlightDialogShown;
        pnlBackground.ResumeLayout(false);
        groupBoxGroups.ResumeLayout(false);
        groupBoxGroups.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
    private GroupBox groupBoxSelection;
    private FlowLayoutPanel selectionControls;
    private CheckBox checkBoxSelectionOutline;
    private Button btnSelectionColor;
    private Button btnResetSelectionColor;

    private System.Windows.Forms.ListBox listBoxHighlight;
    private System.Windows.Forms.Button btnAdd;
    private System.Windows.Forms.Button btnCopy;
    private System.Windows.Forms.Button btnEdit;
    private System.Windows.Forms.Button btnDelete;
    private System.Windows.Forms.Button btnMoveUp;
    private System.Windows.Forms.Button btnMoveDown;
    private System.Windows.Forms.Button btnOk;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.HelpProvider helpProvider;
    private System.Windows.Forms.Button btnDeleteGroup;
    private System.Windows.Forms.Button btnNewGroup;
    private System.Windows.Forms.Button btnCopyGroup;
    private System.Windows.Forms.ToolTip toolTip;
    private System.Windows.Forms.Label labelAssignNamesToGroups;
    private System.Windows.Forms.Button btnMoveGroupUp;
    private System.Windows.Forms.Button btnMoveGroupDown;
    private System.Windows.Forms.Button btnImportGroup;
    private System.Windows.Forms.Button btnExportGroup;
    private System.Windows.Forms.Panel pnlBackground;
    private System.Windows.Forms.ComboBox comboBoxGroups;
    private GroupBox groupBoxGroups;
}
