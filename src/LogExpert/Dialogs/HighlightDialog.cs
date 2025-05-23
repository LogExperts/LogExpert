using LogExpert.Config;
using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Entities;
using LogExpert.UI.Controls;
using LogExpert.UI.Dialogs;

using NLog;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LogExpert.Dialogs
{
    public partial class HighlightDialog : Form
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        #region Private Fields

        private readonly Image _applyButtonImage;
        private string _bookmarkComment;
        private ActionEntry _currentActionEntry = new();
        private HighlightGroup _currentGroup;
        private List<HighlightGroup> _highlightGroupList;

        #endregion

        #region Ctor

        public HighlightDialog()
        {
            InitializeComponent();

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Load += OnHighlightDialogLoad;
            listBoxHighlight.DrawItem += OnHighlightListBoxDrawItem;
            _applyButtonImage = btnApply.Image;
            btnApply.Image = null;
        }

        #endregion

        #region Properties / Indexers

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

        public IList<IKeywordAction> KeywordActionList { get; set; }

        public string PreSelectedGroupName { get; set; }

        private bool IsDirty => btnApply.Image == _applyButtonImage;

        #endregion

        #region Event handling Methods

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            AddNewEntry();
            Dirty();
        }

        private void OnBtnApplyClick(object sender, EventArgs e)
        {
            SaveEntry();
        }

        private void OnBtnBookmarkCommentClick(object sender, EventArgs e)
        {
            BookmarkCommentDlg dlg = new();
            dlg.Comment = _bookmarkComment;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _bookmarkComment = dlg.Comment;
                Dirty();
            }
        }

        private void OnBtnCopyGroupClick(object sender, EventArgs e)
        {
            if (comboBoxGroups.SelectedIndex >= 0 && comboBoxGroups.SelectedIndex < HighlightGroupList.Count)
            {
                HighlightGroup newGroup = (HighlightGroup)HighlightGroupList[comboBoxGroups.SelectedIndex].Clone();
                newGroup.GroupName = "Copy of " + newGroup.GroupName;

                HighlightGroupList.Add(newGroup);
                FillGroupComboBox();
                SelectGroup(HighlightGroupList.Count - 1);
            }
        }

        private void OnBtnCustomBackColorClick(object sender, EventArgs e)
        {
            ChooseColor(colorBoxBackground);
            Dirty();
        }

        private void OnBtnCustomForeColorClick(object sender, EventArgs e)
        {
            ChooseColor(colorBoxForeground);
            Dirty();
        }

        private void OnBtnDelGroupClick(object sender, EventArgs e)
        {
            // the last group cannot be deleted
            if (HighlightGroupList.Count == 1)
            {
                return;
            }

            if (comboBoxGroups.SelectedIndex >= 0 && comboBoxGroups.SelectedIndex < HighlightGroupList.Count)
            {
                int index = comboBoxGroups.SelectedIndex;
                HighlightGroupList.RemoveAt(comboBoxGroups.SelectedIndex);
                FillGroupComboBox();
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

        private void OnBtnExportGroupClick(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new()
            {
                Title = @"Export Settings to file",
                DefaultExt = "json",
                AddExtension = true,
                Filter = @"Settings (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                FileInfo fileInfo = new(dlg.FileName);
                ConfigManager.Export(fileInfo, Core.Config.SettingsFlags.HighlightSettings);
            }
        }

        private void OnBtnGroupDownClick(object sender, EventArgs e)
        {
            int index = comboBoxGroups.SelectedIndex;
            if (index > -1 && index < _highlightGroupList.Count - 1)
            {
                _highlightGroupList.Reverse(index, 2);
                comboBoxGroups.Refresh();
                FillGroupComboBox();
                SelectGroup(index + 1);
            }
        }

        private void OnBtnGroupUpClick(object sender, EventArgs e)
        {
            int index = comboBoxGroups.SelectedIndex;
            if (index > 0)
            {
                _highlightGroupList.Reverse(index - 1, 2);
                comboBoxGroups.Refresh();
                FillGroupComboBox();
                SelectGroup(index - 1);
            }
        }

        private void OnBtnImportGroupClick(object sender, EventArgs e)
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
            catch (Exception ex)
            {
                MessageBox.Show(this, $@"Settings could not be imported: {ex}", @"LogExpert");
                _logger.Error($"Error while trying to access file: {dlg.FileName}: {ex}");
                return;
            }

            ConfigManager.ImportHighlightSettings(fileInfo, dlg.ImportFlags);
            Cursor.Current = Cursors.Default;

            _highlightGroupList = ConfigManager.Settings.Preferences.HighlightGroupList;

            FillGroupComboBox();

            MessageBox.Show(this, @"Settings imported", @"LogExpert");
        }

        private void OnBtnMoveDownClick(object sender, EventArgs e)
        {
            int index = listBoxHighlight.SelectedIndex;

            if (index > -1 && index < listBoxHighlight.Items.Count - 1)
            {
                object item = listBoxHighlight.SelectedItem;
                listBoxHighlight.Items.RemoveAt(index);
                listBoxHighlight.Items.Insert(index + 1, item);
                listBoxHighlight.SelectedIndex = index + 1;
                _currentGroup.HighlightEntryList.Reverse(index, 2);
            }
        }

        private void OnBtnMoveUpClick(object sender, EventArgs e)
        {
            int index = listBoxHighlight.SelectedIndex;
            if (index > 0)
            {
                object item = listBoxHighlight.SelectedItem;
                listBoxHighlight.Items.RemoveAt(index); // will also clear the selection
                listBoxHighlight.Items.Insert(index - 1, item);
                listBoxHighlight.SelectedIndex = index - 1; // restore the selection
                _currentGroup.HighlightEntryList.Reverse(index - 1, 2);
            }
        }

        private void OnBtnNewGroupClick(object sender, EventArgs e)
        {
            // Propose a unique name
            const string baseName = "New group";
            string name = baseName;
            bool uniqueName = false;
            int i = 1;
            while (!uniqueName)
            {
                uniqueName = HighlightGroupList.FindIndex(delegate (HighlightGroup g) { return g.GroupName == name; }) < 0;

                if (!uniqueName)
                {
                    name = $"{baseName} #{i++}";
                }
            }

            HighlightGroup newGroup = new() { GroupName = name };
            HighlightGroupList.Add(newGroup);
            FillGroupComboBox();
            SelectGroup(HighlightGroupList.Count - 1);
        }

        private void OnBtnOkClick(object sender, EventArgs e)
        {
            // Apply pending changes if closing the form.
            if (IsDirty)
            {
                // cannot call 'this.applyButton.PerformClick();' because it prohibits the OK button to terminate the dialog
                OnBtnApplyClick(btnApply, EventArgs.Empty);
            }
        }

        private void OnChkBoxBoldCheckedChanged(object sender, EventArgs e)
        {
            Dirty();
        }

        private void OnChkBoxNoBackgroundCheckedChanged(object sender, EventArgs e)
        {
            colorBoxBackground.Enabled = !checkBoxNoBackground.Checked;
            btnCustomBackColor.Enabled = !checkBoxNoBackground.Checked;
            Dirty();
        }

        private void OnChkBoxPluginCheckedChanged(object sender, EventArgs e)
        {
            Dirty();
            btnSelectPlugin.Enabled = checkBoxPlugin.Checked;
        }

        private void OnChkBoxRegexMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                RegexHelperDialog dlg = new()
                {
                    Owner = this,
                    CaseSensitive = checkBoxCaseSensitive.Checked,
                    Pattern = textBoxSearchString.Text
                };

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    checkBoxCaseSensitive.Checked = dlg.CaseSensitive;
                    textBoxSearchString.Text = dlg.Pattern;
                }
            }
        }

        private void OnChkBoxWordMatchCheckedChanged(object sender, EventArgs e)
        {
            Dirty();
            checkBoxNoBackground.Enabled = checkBoxWordMatch.Checked;
        }

        private void OnCmbBoxGroupDrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0)
            {
                HighlightGroup group = HighlightGroupList[e.Index];
                Rectangle rectangle = new(0, e.Bounds.Top, e.Bounds.Width, e.Bounds.Height);

                Brush brush = new SolidBrush(SystemColors.ControlText);
                e.Graphics.DrawString(group.GroupName, e.Font, brush, new PointF(rectangle.Left, rectangle.Top));
                e.DrawFocusRectangle();
                brush.Dispose();
            }
        }

        private void OnCmbBoxGroupSelectionChangeCommitted(object sender, EventArgs e)
        {
            SelectGroup(comboBoxGroups.SelectedIndex);
        }

        private void OnCmbBoxGroupTextUpdate(object sender, EventArgs e)
        {
            _currentGroup.GroupName = comboBoxGroups.Text;
        }

        private void OnDeleteButtonClick(object sender, EventArgs e)
        {
            if (listBoxHighlight.SelectedIndex >= 0)
            {
                int removeIndex = listBoxHighlight.SelectedIndex;
                _currentGroup.HighlightEntryList.RemoveAt(removeIndex);
                listBoxHighlight.Items.RemoveAt(removeIndex);

                // Select previous (or first if none before)
                int nextSelectIndex = removeIndex;
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

        private void OnHighlightDialogLoad(object sender, EventArgs e)
        {
            colorBoxForeground.SelectedIndex = 1;
            colorBoxBackground.SelectedIndex = 2;
            btnApply.Enabled = false;
            btnApply.Image = null;
            btnBookmarkComment.Enabled = false;
            btnSelectPlugin.Enabled = false;

            ReEvaluateHighlightButtonStates();
        }

        private void OnHighlightDialogShown(object sender, EventArgs e)
        {
            InitData();
        }

        private void OnHighlightListBoxDrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0)
            {
                HighlightEntry entry = (HighlightEntry)listBoxHighlight.Items[e.Index];
                Rectangle rectangle = new(0, e.Bounds.Top, e.Bounds.Width, e.Bounds.Height);

                if ((e.State & DrawItemState.Selected) != DrawItemState.Selected)
                {
                    e.Graphics.FillRectangle(new SolidBrush(entry.BackgroundColor), rectangle);
                }

                e.Graphics.DrawString(entry.SearchText, e.Font, new SolidBrush(entry.ForegroundColor),
                    new PointF(rectangle.Left, rectangle.Top));

                e.DrawFocusRectangle();
            }
        }

        private void OnListBoxHighlightSelectedIndexChanged(object sender, EventArgs e)
        {
            StartEditEntry();
        }

        private void OnPluginButtonClick(object sender, EventArgs e)
        {
            KeywordActionDlg dlg = new(_currentActionEntry, KeywordActionList);

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _currentActionEntry = dlg.ActionEntry;
                Dirty();
            }
        }

        #endregion

        #region Private Methods

        private void AddNewEntry()
        {
            {
                try
                {
                    CheckRegex();

                    HighlightEntry entry = new()
                    {
                        SearchText = textBoxSearchString.Text,
                        ForegroundColor = colorBoxForeground.SelectedColor,
                        BackgroundColor = colorBoxBackground.SelectedColor,
                        IsRegEx = checkBoxRegex.Checked,
                        IsCaseSensitive = checkBoxCaseSensitive.Checked,
                        IsLedSwitch = checkBoxDontDirtyLed.Checked,
                        IsStopTail = checkBoxStopTail.Checked,
                        IsSetBookmark = checkBoxBookmark.Checked,
                        IsActionEntry = checkBoxPlugin.Checked,
                        ActionEntry = _currentActionEntry,
                        IsWordMatch = checkBoxWordMatch.Checked,
                        IsBold = checkBoxBold.Checked,
                        NoBackground = checkBoxNoBackground.Checked
                    };

                    listBoxHighlight.Items.Add(entry);

                    // Select the newly created item
                    _currentGroup.HighlightEntryList.Add(entry);
                    listBoxHighlight.SelectedItem = entry;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error during add of highlight entry");
                    MessageBox.Show($"Error during add of entry.\r\n{ex.Message}");
                }
            }
        }

        private void ChangeToDirty(object sender, EventArgs e)
        {
            Dirty();
        }

        private void CheckRegex()
        {
            if (checkBoxRegex.Checked)
            {
                if (string.IsNullOrWhiteSpace(textBoxSearchString.Text))
                {
                    throw new ArgumentException("Regex value is null or whitespace");
                }

                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                Regex.IsMatch("", textBoxSearchString.Text);
            }
        }

        private void ChooseColor(ColorComboBox comboBox)
        {
            ColorDialog colorDialog = new();
            colorDialog.AllowFullOpen = true;
            colorDialog.ShowHelp = false;
            colorDialog.Color = comboBox.CustomColor;
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                comboBox.CustomColor = colorDialog.Color;
                comboBox.SelectedIndex = 0;
            }
        }

        private void Dirty()
        {
            int index = listBoxHighlight.SelectedIndex;
            if (index > -1)
            {
                btnApply.Enabled = true;
                btnApply.Image = _applyButtonImage;
            }

            btnAdd.Enabled = textBoxSearchString.Text.Length > 0;
        }

        private void FillGroupComboBox()
        {
            SelectGroup(-1);

            comboBoxGroups.Items.Clear();

            foreach (HighlightGroup group in HighlightGroupList)
            {
                comboBoxGroups.Items.Add(group);
            }

            ReEvaluateGroupButtonStates();
        }

        private void FillHighlightListBox()
        {
            listBoxHighlight.Items.Clear();
            if (_currentGroup != null)
            {
                foreach (HighlightEntry entry in _currentGroup.HighlightEntryList)
                {
                    listBoxHighlight.Items.Add(entry);
                }
            }
        }

        private void InitData()
        {
            const string def = "[Default]";
            HighlightGroupList ??= [];

            if (HighlightGroupList.Count == 0)
            {
                HighlightGroup highlightGroup = new()
                {
                    GroupName = def,
                    HighlightEntryList = []
                };

                HighlightGroupList.Add(highlightGroup);
            }

            FillGroupComboBox();

            _currentGroup = null;
            string groupToSelect = PreSelectedGroupName;
            if (string.IsNullOrEmpty(groupToSelect))
            {
                groupToSelect = def;
            }

            foreach (HighlightGroup group in HighlightGroupList)
            {
                if (group.GroupName.Equals(groupToSelect))
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

        private void ReEvaluateGroupButtonStates()
        {
            // Refresh button states based on the selection in the combobox
            bool atLeastOneSelected = comboBoxGroups.SelectedItem != null;
            bool moreThanOne = comboBoxGroups.Items.Count > 1;
            bool firstSelected = atLeastOneSelected && comboBoxGroups.SelectedIndex == 0;
            bool lastSelected = atLeastOneSelected && comboBoxGroups.SelectedIndex == comboBoxGroups.Items.Count - 1;

            btnDeleteGroup.Enabled = atLeastOneSelected;
            btnCopyGroup.Enabled = atLeastOneSelected;
            btnMoveGroupUp.Enabled = atLeastOneSelected && moreThanOne && !firstSelected;
            btnMoveGroupDown.Enabled = atLeastOneSelected && moreThanOne && !lastSelected;
        }

        private void ReEvaluateHighlightButtonStates()
        {
            // Refresh button states based on the selection in the combobox
            bool atLeastOneSelected = listBoxHighlight.SelectedItem != null;
            bool moreThanOne = listBoxHighlight.Items.Count > 1;
            bool firstSelected = atLeastOneSelected && listBoxHighlight.SelectedIndex == 0;
            bool lastSelected = atLeastOneSelected && listBoxHighlight.SelectedIndex == listBoxHighlight.Items.Count - 1;

            btnDelete.Enabled = atLeastOneSelected;
            btnMoveUp.Enabled = atLeastOneSelected && moreThanOne && !firstSelected;
            btnMoveDown.Enabled = atLeastOneSelected && moreThanOne && !lastSelected;
        }

        private void SaveEntry()
        {
            try
            {
                CheckRegex();

                HighlightEntry entry = (HighlightEntry)listBoxHighlight.SelectedItem;

                entry.ForegroundColor = (Color)colorBoxForeground.SelectedItem;
                entry.BackgroundColor = (Color)colorBoxBackground.SelectedItem;
                entry.SearchText = textBoxSearchString.Text;
                entry.IsRegEx = checkBoxRegex.Checked;
                entry.IsCaseSensitive = checkBoxCaseSensitive.Checked;
                btnApply.Enabled = false;
                btnApply.Image = null;
                entry.IsLedSwitch = checkBoxDontDirtyLed.Checked;
                entry.IsSetBookmark = checkBoxBookmark.Checked;
                entry.IsStopTail = checkBoxStopTail.Checked;
                entry.IsActionEntry = checkBoxPlugin.Checked;
                entry.ActionEntry = (ActionEntry)_currentActionEntry.Clone();
                entry.BookmarkComment = _bookmarkComment;
                entry.IsWordMatch = checkBoxWordMatch.Checked;
                entry.IsBold = checkBoxBold.Checked;
                entry.NoBackground = checkBoxNoBackground.Checked;
                listBoxHighlight.Refresh();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during save of save highlight entry");
                MessageBox.Show($"Error during save of entry.\r\n{ex.Message}");
            }
        }

        private void SelectGroup(int index)
        {
            if (index >= 0 && index < HighlightGroupList.Count)
            {
                _currentGroup = HighlightGroupList[index];
                comboBoxGroups.Items[index] = _currentGroup;
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

        private void StartEditEntry()
        {
            HighlightEntry entry = (HighlightEntry)listBoxHighlight.SelectedItem;

            if (entry != null)
            {
                textBoxSearchString.Text = entry.SearchText;

                colorBoxForeground.CustomColor = entry.ForegroundColor;
                colorBoxBackground.CustomColor = entry.BackgroundColor;

                if (colorBoxForeground.Items.Contains(entry.ForegroundColor))
                {
                    colorBoxForeground.SelectedIndex = colorBoxForeground.Items.Cast<Color>().ToList().LastIndexOf(entry.ForegroundColor);
                }
                else
                {
                    colorBoxForeground.SelectedItem = entry.ForegroundColor;
                }

                if (colorBoxForeground.Items.Contains(entry.ForegroundColor))
                {
                    colorBoxBackground.SelectedIndex = colorBoxBackground.Items.Cast<Color>().ToList().LastIndexOf(entry.BackgroundColor);
                }
                else
                {
                    colorBoxBackground.SelectedItem = entry.BackgroundColor;
                }

                checkBoxRegex.Checked = entry.IsRegEx;
                checkBoxCaseSensitive.Checked = entry.IsCaseSensitive;
                checkBoxDontDirtyLed.Checked = entry.IsLedSwitch;
                checkBoxBookmark.Checked = entry.IsSetBookmark;
                checkBoxStopTail.Checked = entry.IsStopTail;
                checkBoxPlugin.Checked = entry.IsActionEntry;
                btnSelectPlugin.Enabled = checkBoxPlugin.Checked;
                btnBookmarkComment.Enabled = checkBoxBookmark.Checked;
                _currentActionEntry = entry.ActionEntry != null ? (ActionEntry)entry.ActionEntry.Clone() : new ActionEntry();
                _bookmarkComment = entry.BookmarkComment;
                checkBoxWordMatch.Checked = entry.IsWordMatch;
                checkBoxBold.Checked = entry.IsBold;
                checkBoxNoBackground.Checked = entry.NoBackground;
            }

            btnApply.Enabled = false;
            btnApply.Image = null;

            ReEvaluateHighlightButtonStates();
        }

        #endregion
    }
}
