using System.ComponentModel;
using System.Runtime.Versioning;
using System.Security;

using ColumnizerLib;
using LogExpert.Core.Config;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Dialogs.Highlight;
using LogExpert.UI.Entities;

namespace LogExpert.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class HighlightDialog : Form
{
    #region Private Fields

    private HighlightGroup _currentGroup;
    private List<HighlightGroup> _highlightGroupList;
    private Color? _selectionColor;

    #endregion

    #region Ctor

    public HighlightDialog (IConfigManager configManager)
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();

        ApplyResources();

        ConfigManager = configManager;
        _selectionColor = configManager.Settings.Preferences.SelectionHighlight.CustomColor;
        checkBoxSelectionOutline.Checked = configManager.Settings.Preferences.SelectionHighlight.Outline;
        UpdateSelectionColorPreview();
        Load += OnHighlightDialogLoad;
        listBoxHighlight.DrawItem += OnHighlightListBoxDrawItem;

        ResumeLayout();
    }

    private void ApplyResources ()
    {
        // Dialog
        Text = Resources.HighlightDialog_UI_Title;

        btnOk.Text = Resources.LogExpert_Common_UI_Button_OK;
        btnCancel.Text = Resources.LogExpert_Common_UI_Button_Cancel;
        btnAdd.Text = Resources.LogExpert_Common_UI_Button_Add;
        btnEdit.Text = Resources.LogExpert_Common_UI_Button_Edit;
        btnDelete.Text = Resources.LogExpert_Common_UI_Button_Delete;
        btnMoveUp.Text = Resources.LogExpert_Common_UI_Button_MoveUp;
        btnMoveDown.Text = Resources.LogExpert_Common_UI_Button_MoveDown;
        btnImportGroup.Text = Resources.LogExpert_Common_UI_Button_Import;
        btnExportGroup.Text = Resources.LogExpert_Common_UI_Button_Export;
        btnMoveGroupDown.Text = Resources.HighlightDialog_UI_Button_GroupDown;
        btnMoveGroupUp.Text = Resources.HighlightDialog_UI_Button_GroupUp;
        btnCopyGroup.Text = Resources.HighlightDialog_UI_Button_Copy;
        btnDeleteGroup.Text = Resources.HighlightDialog_UI_Button_DeleteGroup;
        btnNewGroup.Text = Resources.HighlightDialog_UI_Button_NewGroup;

        labelAssignNamesToGroups.Text = Resources.HighlightDialog_UI_Label_AssignNamesToGroups;
        groupBoxGroups.Text = Resources.HighlightDialog_UI_GroupBox_Groups;
        groupBoxSelection.Text = Resources.HighlightDialog_UI_SelectionHighlight;
        checkBoxSelectionOutline.Text = Resources.HighlightDialog_UI_OutlineSelectedBlocks;
        btnSelectionColor.Text = Resources.HighlightDialog_UI_ChangeSelectionColor;
        btnResetSelectionColor.Text = Resources.HighlightDialog_UI_ResetSelectionColor;
    }

    #endregion

    #region Properties / Indexers

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public List<HighlightGroup> HighlightGroupList
    {
        get => _highlightGroupList;
        set
        {
            _highlightGroupList ??= [];

            foreach (var group in value)
            {
                _highlightGroupList.Add((HighlightGroup)group.Clone());
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public IList<IKeywordAction> KeywordActionList { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string PreSelectedGroupName { get; set; }

    private IConfigManager ConfigManager { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SelectionHighlightSettings SelectionHighlight => new()
    {
        Outline = checkBoxSelectionOutline.Checked,
        CustomColor = _selectionColor
    };

    #endregion

    #region Event handling Methods

    private void OnSelectionColorClick (object sender, EventArgs e)
    {
        using var dialog = new ColorDialog { Color = _selectionColor ?? SystemColors.Highlight, FullOpen = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _selectionColor = dialog.Color;
            UpdateSelectionColorPreview();
        }
    }

    private void OnResetSelectionColorClick (object sender, EventArgs e)
    {
        _selectionColor = null;
        UpdateSelectionColorPreview();
    }

    private void UpdateSelectionColorPreview ()
    {
        btnSelectionColor.BackColor = _selectionColor ?? SystemColors.Highlight;
        btnSelectionColor.ForeColor = PaintHelper.GetForeColorBasedOnBackColor(btnSelectionColor.BackColor);
        btnResetSelectionColor.Enabled = _selectionColor.HasValue;
    }

    private void OnAddButtonClick (object sender, EventArgs e)
    {
        if (_currentGroup == null)
        {
            return;
        }

        var entry = new HighlightEntry
        {
            ForegroundColor = Color.White,
            BackgroundColor = Color.Gray,
        };

        using var dlg = new HighlightEntryDialog(entry, KeywordActionList, isNew: true);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _currentGroup.HighlightEntryList.Add(entry);
            _ = listBoxHighlight.Items.Add(entry);
            listBoxHighlight.SelectedItem = entry;
            ReEvaluateHighlightButtonStates();
        }
    }

    private void OnBtnEditClick (object sender, EventArgs e)
    {
        if (listBoxHighlight.SelectedItem is not HighlightEntry entry)
        {
            return;
        }

        using var dlg = new HighlightEntryDialog(entry, KeywordActionList, isNew: false);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            listBoxHighlight.Refresh();
        }
    }

    private void OnBtnCopyGroupClick (object sender, EventArgs e)
    {
        if (comboBoxGroups.SelectedIndex >= 0 && comboBoxGroups.SelectedIndex < HighlightGroupList.Count)
        {
            var newGroup = (HighlightGroup)HighlightGroupList[comboBoxGroups.SelectedIndex].Clone();
            newGroup.GroupName = $"{Resources.HighlightDialog_UI_Snippet_CopyOf} {newGroup.GroupName}";

            HighlightGroupList.Add(newGroup);
            FillGroupListBoxGroups();
            SelectGroup(HighlightGroupList.Count - 1);
        }
    }

    private void OnBtnDelGroupClick (object sender, EventArgs e)
    {
        // the last group cannot be deleted
        if (HighlightGroupList.Count == 1)
        {
            return;
        }

        if (comboBoxGroups.SelectedIndex >= 0 && comboBoxGroups.SelectedIndex < HighlightGroupList.Count)
        {
            var index = comboBoxGroups.SelectedIndex;
            HighlightGroupList.RemoveAt(comboBoxGroups.SelectedIndex);
            FillGroupListBoxGroups();
            if (index < HighlightGroupList.Count)
            {
                SelectGroup(index);
            }
            else
            {
                SelectGroup(HighlightGroupList.Count - 1);
            }
        }
    }

    //TODO: This class should not knoow ConfigManager?
    private void OnBtnExportGroupClick (object sender, EventArgs e)
    {
        SaveFileDialog dlg = new()
        {
            Title = Resources.HighlightDialog_UI_Title_ExportSettings,
            DefaultExt = "json",
            AddExtension = true,
            Filter = Resources.HighlightDialog_UI_Export_Filter
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            FileInfo fileInfo = new(dlg.FileName);
            ConfigManager.Export(fileInfo, Core.Config.SettingsFlags.HighlightSettings);
        }
    }

    private void OnBtnGroupDownClick (object sender, EventArgs e)
    {
        var index = comboBoxGroups.SelectedIndex;
        if (index > -1 && index < _highlightGroupList.Count - 1)
        {
            _highlightGroupList.Reverse(index, 2);
            comboBoxGroups.Refresh();
            FillGroupListBoxGroups();
            SelectGroup(index + 1);
        }
    }

    private void OnBtnGroupUpClick (object sender, EventArgs e)
    {
        var index = comboBoxGroups.SelectedIndex;
        if (index > 0)
        {
            _highlightGroupList.Reverse(index - 1, 2);
            comboBoxGroups.Refresh();
            FillGroupListBoxGroups();
            SelectGroup(index - 1);
        }
    }

    private void OnBtnImportGroupClick (object sender, EventArgs e)
    {
        ImportSettingsDialog dlg = new(Core.Config.ExportImportFlags.HighlightSettings);

        foreach (Control ctl in dlg.groupBoxImportOptions.Controls)
        {
            if (ctl.Tag != null)
            {
                ((CheckBox)ctl).Checked = false;
            }
        }

        dlg.checkBoxHighlightSettings.Checked = true;
        dlg.checkBoxKeepExistingSettings.Checked = true;

        if (dlg.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dlg.FileName))
        {
            return;
        }

        Cursor.Current = Cursors.WaitCursor;

        FileInfo fileInfo;

        try
        {
            fileInfo = new FileInfo(dlg.FileName);
        }
        catch (Exception ex) when (ex is ArgumentNullException
                                    or SecurityException
                                    or ArgumentException
                                    or UnauthorizedAccessException
                                    or PathTooLongException
                                    or NotSupportedException)
        {
            _ = MessageBox.Show(this, Resources.HighlightDialog_UI_SettingsCouldNotBeImported, Resources.LogExpert_Common_UI_Title_LogExpert);
            return;
        }

        ConfigManager.ImportHighlightSettings(fileInfo, dlg.ImportFlags);
        Cursor.Current = Cursors.Default;

        _highlightGroupList = ConfigManager.Settings.Preferences.HighlightGroupList;

        FillGroupListBoxGroups();

        _ = MessageBox.Show(this, Resources.HighlightDialog_UI_SettingsImported, Resources.LogExpert_Common_UI_Title_LogExpert);
    }

    private void OnBtnMoveDownClick (object sender, EventArgs e)
    {
        var index = listBoxHighlight.SelectedIndex;

        if (index > -1 && index < listBoxHighlight.Items.Count - 1)
        {
            var item = listBoxHighlight.SelectedItem;
            listBoxHighlight.Items.RemoveAt(index);
            listBoxHighlight.Items.Insert(index + 1, item);
            listBoxHighlight.SelectedIndex = index + 1;
            _currentGroup.HighlightEntryList.Reverse(index, 2);
        }
    }

    private void OnBtnMoveUpClick (object sender, EventArgs e)
    {
        var index = listBoxHighlight.SelectedIndex;
        if (index > 0)
        {
            var item = listBoxHighlight.SelectedItem;
            listBoxHighlight.Items.RemoveAt(index); // will also clear the selection
            listBoxHighlight.Items.Insert(index - 1, item);
            listBoxHighlight.SelectedIndex = index - 1; // restore the selection
            _currentGroup.HighlightEntryList.Reverse(index - 1, 2);
        }
    }

    private void OnBtnNewGroupClick (object sender, EventArgs e)
    {
        // Propose a unique name
        var baseName = Resources.HighlightDialog_UI_NewGroup_BaseName;
        var name = baseName;
        var uniqueName = false;
        var i = 1;
        while (!uniqueName)
        {
            uniqueName = HighlightGroupList.FindIndex(delegate (HighlightGroup g)
            { return g.GroupName == name; }) < 0;

            if (!uniqueName)
            {
                name = $"{baseName} #{i++}";
            }
        }

        HighlightGroup newGroup = new() { GroupName = name };
        HighlightGroupList.Add(newGroup);
        FillGroupListBoxGroups();
        SelectGroup(HighlightGroupList.Count - 1);
    }

    private void OnBtnOkClick (object sender, EventArgs e)
    {
        // All edits are committed via the sub-dialog. Nothing to flush here.
    }

    private void OnCmbBoxGroupDrawItem (object sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index >= 0)
        {
            var group = HighlightGroupList[e.Index];
            Rectangle rectangle = new(0, e.Bounds.Top, e.Bounds.Width, e.Bounds.Height);

            Brush brush = new SolidBrush(SystemColors.ControlText);
            e.Graphics.DrawString(group.GroupName, e.Font, brush, new PointF(rectangle.Left, rectangle.Top));
            e.DrawFocusRectangle();
            brush.Dispose();
        }
    }

    private void OnCmbBoxGroupSelectionChangeCommitted (object sender, EventArgs e)
    {
        SelectGroup(comboBoxGroups.SelectedIndex);
    }

    private void OnCmbBoxGroupTextUpdate (object sender, EventArgs e)
    {
        _currentGroup.GroupName = comboBoxGroups.Text;
    }

    private void OnDeleteButtonClick (object sender, EventArgs e)
    {
        if (listBoxHighlight.SelectedIndex >= 0)
        {
            var removeIndex = listBoxHighlight.SelectedIndex;
            _currentGroup.HighlightEntryList.RemoveAt(removeIndex);
            listBoxHighlight.Items.RemoveAt(removeIndex);

            // Select previous (or first if none before)
            var nextSelectIndex = removeIndex;
            if (nextSelectIndex >= listBoxHighlight.Items.Count)
            {
                nextSelectIndex--; // if last item was removed, go one up
            }

            if (nextSelectIndex >= 0)
            {
                listBoxHighlight.SelectedIndex = nextSelectIndex; // if still some item, select it
            }

            ReEvaluateHighlightButtonStates();
        }
    }

    private void OnHighlightDialogLoad (object sender, EventArgs e)
    {
        ReEvaluateHighlightButtonStates();
    }

    private void OnHighlightDialogShown (object sender, EventArgs e)
    {
        InitData();
    }

    private void OnHighlightListBoxDrawItem (object sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index >= 0)
        {
            var entry = (HighlightEntry)listBoxHighlight.Items[e.Index];
            Rectangle rectangle = new(0, e.Bounds.Top, e.Bounds.Width, e.Bounds.Height);

            SolidBrush foregroundBrush;

            if (e.State.HasFlag(DrawItemState.Selected))
            {
                foregroundBrush = new SolidBrush(PaintHelper.GetForeColorBasedOnBackColor(entry.ForegroundColor));
            }
            else
            {
                using var backgroundBrush = new SolidBrush(entry.BackgroundColor);
                e.Graphics.FillRectangle(backgroundBrush, rectangle);
                foregroundBrush = new SolidBrush(entry.ForegroundColor);
            }

            using (foregroundBrush)
            {
                e.Graphics.DrawString(entry.SearchText, e.Font, foregroundBrush, new PointF(rectangle.Left, rectangle.Top));
            }

            e.DrawFocusRectangle();
        }
    }

    private void OnListBoxHighlightSelectedIndexChanged (object sender, EventArgs e)
    {
        ReEvaluateHighlightButtonStates();
    }

    #endregion

    #region Private Methods

    private void FillGroupListBoxGroups ()
    {
        SelectGroup(-1);
        comboBoxGroups.Items.Clear();

        foreach (var group in HighlightGroupList)
        {
            _ = comboBoxGroups.Items.Add(group);
        }

        ReEvaluateGroupButtonStates();
    }

    private void FillHighlightListBox ()
    {
        listBoxHighlight.Items.Clear();
        if (_currentGroup != null)
        {
            foreach (var entry in _currentGroup.HighlightEntryList)
            {
                _ = listBoxHighlight.Items.Add(entry);
            }
        }
    }

    private void InitData ()
    {
        HighlightGroupList ??= [];

        if (HighlightGroupList.Count == 0)
        {
            HighlightGroup highlightGroup = new()
            {
                GroupName = Resources.HighlightDialog_UI_DefaultGroupName,
                HighlightEntryList = []
            };

            HighlightGroupList.Add(highlightGroup);
        }

        FillGroupListBoxGroups();

        _currentGroup = null;
        var groupToSelect = PreSelectedGroupName;
        if (string.IsNullOrEmpty(groupToSelect))
        {
            groupToSelect = Resources.HighlightDialog_UI_DefaultGroupName;
        }

        foreach (var group in HighlightGroupList)
        {
            if (group.GroupName.Equals(groupToSelect, StringComparison.Ordinal))
            {
                _currentGroup = group;
                comboBoxGroups.SelectedValue = group;
                comboBoxGroups.SelectedIndex = HighlightGroupList.IndexOf(group);
                break;
            }
        }

        ReEvaluateGroupButtonStates();

        FillHighlightListBox();
    }

    private void ReEvaluateGroupButtonStates ()
    {
        // Refresh button states based on the selection in the listBoxGroups
        var atLeastOneSelected = comboBoxGroups.SelectedItem != null;
        var moreThanOne = comboBoxGroups.Items.Count > 1;
        var firstSelected = atLeastOneSelected && comboBoxGroups.SelectedIndex == 0;
        var lastSelected = atLeastOneSelected && comboBoxGroups.SelectedIndex == comboBoxGroups.Items.Count - 1;

        btnDeleteGroup.Enabled = atLeastOneSelected;
        btnCopyGroup.Enabled = atLeastOneSelected;
        btnMoveGroupUp.Enabled = atLeastOneSelected && moreThanOne && !firstSelected;
        btnMoveGroupDown.Enabled = atLeastOneSelected && moreThanOne && !lastSelected;
    }

    private void ReEvaluateHighlightButtonStates ()
    {
        // Refresh button states based on the selection in the listbox
        var atLeastOneSelected = listBoxHighlight.SelectedItem != null;
        var moreThanOne = listBoxHighlight.Items.Count > 1;
        var firstSelected = atLeastOneSelected && listBoxHighlight.SelectedIndex == 0;
        var lastSelected = atLeastOneSelected && listBoxHighlight.SelectedIndex == listBoxHighlight.Items.Count - 1;

        btnEdit.Enabled = atLeastOneSelected;
        btnDelete.Enabled = atLeastOneSelected;
        btnMoveUp.Enabled = atLeastOneSelected && moreThanOne && !firstSelected;
        btnMoveDown.Enabled = atLeastOneSelected && moreThanOne && !lastSelected;
    }

    private void SelectGroup (int index)
    {
        if (index >= 0 && index < HighlightGroupList.Count)
        {
            _currentGroup = HighlightGroupList[index];
            //listBoxGroups.Items[index] = _currentGroup;
            comboBoxGroups.SelectedIndex = index;
            comboBoxGroups.SelectedItem = _currentGroup;
            FillHighlightListBox();
        }
        else
        {
            comboBoxGroups.SelectedItem = null;
            _currentGroup = null;
            listBoxHighlight.Items.Clear();
        }

        ReEvaluateHighlightButtonStates();
        ReEvaluateGroupButtonStates();
    }

    #endregion
}
