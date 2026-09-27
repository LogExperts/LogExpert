using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

using ColumnizerLib;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Helpers;
using LogExpert.Dialogs;
using LogExpert.UI.Controls;

namespace LogExpert.UI.Dialogs.Highlight;

public partial class HighlightEntryDialog : Form
{
    private readonly HighlightEntry _entry;
    private readonly IList<IKeywordAction> _keywordActionList;
    private readonly bool _isNew;
    private ActionEntry _currentActionEntry;
    private string _bookmarkComment;

    public HighlightEntryDialog (HighlightEntry entry, IList<IKeywordAction> keywordActions, bool isNew)
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();
        ArgumentNullException.ThrowIfNull(entry);

        _entry = entry;
        _keywordActionList = keywordActions;
        _isNew = isNew;
        _currentActionEntry = entry.ActionEntry != null
            ? (ActionEntry)entry.ActionEntry.Clone()
            : new ActionEntry();
        _bookmarkComment = entry.BookmarkComment ?? string.Empty;

        ApplyResources();
        LoadFromEntry();
        UpdateTitle();
        UpdateOkEnabled();
        UpdateAlertEnabled();
        UpdateBookmarkCommentEnabled();
        UpdatePluginEnabled();
        UpdateNoBackgroundEnabled();
        UpdateBackgroundColorEnabled();
        UpdatePreview();
    }

    private void ApplyResources ()
    {
        _tabLineMatch.Text = Resources.HighlightEntryEditDialog_UI_Tab_LineMatchCriteria;
        _tabColoring.Text = Resources.HighlightEntryEditDialog_UI_Tab_Coloring;
        _tabActions.Text = Resources.HighlightEntryEditDialog_UI_Tab_Actions;

        _labelSearchString.Text = Resources.HighlightDialog_UI_Label_SearchString;
        _checkBoxRegex.Text = Resources.HighlightDialog_UI_CheckBox_RegEx;
        _checkBoxCaseSensitive.Text = Resources.HighlightDialog_UI_CheckBox_CaseSensitive;

        _labelForeColor.Text = Resources.HighlightDialog_UI_Label_ForegroundColor;
        _labelBackColor.Text = Resources.HighlightDialog_UI_Label_BackgroundColor;
        _btnCustomForeColor.Text = Resources.HighlightDialog_UI_Button_CustomForeColor;
        _btnCustomBackColor.Text = Resources.HighlightDialog_UI_Button_CustomBackColor;
        _checkBoxBold.Text = Resources.HighlightDialog_UI_CheckBox_Bold;
        _checkBoxWordMatch.Text = Resources.HighlightDialog_UI_CheckBox_WordMatch;
        _checkBoxNoBackground.Text = Resources.HighlightDialog_UI_CheckBox_NoBackground;

        _checkBoxBookmark.Text = Resources.HighlightDialog_UI_CheckBox_Bookmark;
        _btnBookmarkComment.Text = Resources.HighlightDialog_UI_Button_BookmarkComment;
        _checkBoxStopTail.Text = Resources.HighlightDialog_UI_CheckBox_StopTail;
        _checkBoxDontDirtyLed.Text = Resources.HighlightDialog_UI_CheckBox_DontDirtyLed;
        _checkBoxHideLine.Text = Resources.HighlightDialog_UI_CheckBox_HideLine;
        _checkBoxPlugin.Text = Resources.HighlightDialog_UI_CheckBox_Plugin;
        _btnSelectPlugin.Text = Resources.HighlightDialog_UI_Button_SelectPlugin;
        _checkBoxAlertOnHit.Text = Resources.HighlightDialog_UI_CheckBox_AlertOnHit;
        _labelSoundFile.Text = Resources.HighlightDialog_UI_Label_SoundFile;
        _btnBrowseSoundFile.Text = Resources.HighlightDialog_UI_Button_BrowseSoundFile;
        _labelCooldown.Text = Resources.HighlightDialog_UI_Label_Cooldown;
        _labelCooldownSeconds.Text = Resources.HighlightDialog_UI_Label_Seconds;

        _btnOk.Text = Resources.LogExpert_Common_UI_Button_OK;
        _btnCancel.Text = Resources.LogExpert_Common_UI_Button_Cancel;
    }

    private void LoadFromEntry ()
    {
        _textBoxSearchString.Text = _entry.SearchText ?? string.Empty;
        _checkBoxRegex.Checked = _entry.IsRegex;
        _checkBoxCaseSensitive.Checked = _entry.IsCaseSensitive;

        SelectComboColor(_colorBoxForeground, _entry.ForegroundColor.IsEmpty ? Color.White : _entry.ForegroundColor);
        SelectComboColor(_colorBoxBackground, _entry.BackgroundColor.IsEmpty ? Color.Gray : _entry.BackgroundColor);

        _checkBoxBold.Checked = _entry.IsBold;
        _checkBoxWordMatch.Checked = _entry.IsWordMatch;
        _checkBoxNoBackground.Checked = _entry.NoBackground;

        _checkBoxBookmark.Checked = _entry.IsSetBookmark;
        _checkBoxStopTail.Checked = _entry.IsStopTail;
        _checkBoxDontDirtyLed.Checked = _entry.IsLedSwitch;
        _checkBoxHideLine.Checked = _entry.IsHideLine;
        _checkBoxPlugin.Checked = _entry.IsActionEntry;

        _checkBoxAlertOnHit.Checked = _entry.AlertOnHit;
        _textBoxSoundFile.Text = _entry.SoundFilePath ?? string.Empty;
        var cooldown = _entry.CooldownSeconds;

        if (cooldown < _numericCooldownSeconds.Minimum)
        {
            cooldown = (int)_numericCooldownSeconds.Minimum;
        }
        else if (cooldown > _numericCooldownSeconds.Maximum)
        {
            cooldown = (int)_numericCooldownSeconds.Maximum;
        }

        _numericCooldownSeconds.Value = cooldown;
    }

    private static void SelectComboColor (ColorComboBox combo, Color color)
    {
        combo.CustomColor = color;

        if (combo.Items.Contains(color))
        {
            combo.SelectedIndex = combo.Items.Cast<Color>().ToList().LastIndexOf(color);
        }
        else
        {
            combo.SelectedItem = color;
        }
    }

    private void SaveToEntry ()
    {
        _entry.SearchText = _textBoxSearchString.Text;
        _entry.IsRegex = _checkBoxRegex.Checked;
        _entry.IsCaseSensitive = _checkBoxCaseSensitive.Checked;

        _entry.ForegroundColor = _colorBoxForeground.SelectedItem is Color fg ? fg : _colorBoxForeground.SelectedColor;
        _entry.BackgroundColor = _colorBoxBackground.SelectedItem is Color bg ? bg : _colorBoxBackground.SelectedColor;
        _entry.IsBold = _checkBoxBold.Checked;
        _entry.IsWordMatch = _checkBoxWordMatch.Checked;
        _entry.NoBackground = _checkBoxNoBackground.Checked;

        _entry.IsSetBookmark = _checkBoxBookmark.Checked;
        _entry.BookmarkComment = _bookmarkComment;
        _entry.IsStopTail = _checkBoxStopTail.Checked;
        _entry.IsLedSwitch = _checkBoxDontDirtyLed.Checked;
        _entry.IsHideLine = _checkBoxHideLine.Checked;
        _entry.IsActionEntry = _checkBoxPlugin.Checked;
        _entry.ActionEntry = (ActionEntry)_currentActionEntry.Clone();

        _entry.AlertOnHit = _checkBoxAlertOnHit.Checked;
        _entry.SoundFilePath = _textBoxSoundFile.Text?.Trim() ?? string.Empty;
        _entry.CooldownSeconds = (int)_numericCooldownSeconds.Value;
    }

    private void OnOkClick (object sender, EventArgs e)
    {
        // Validate Regex pattern (mirrors legacy CheckRegex)
        if (_checkBoxRegex.Checked)
        {
            if (string.IsNullOrWhiteSpace(_textBoxSearchString.Text))
            {
                ShowError(Resources.HighlightDialog_RegexError);
                _tabControl.SelectedTab = _tabLineMatch;
                _ = _textBoxSearchString.Focus();
                return;
            }

            var (isValid, error) = RegexHelper.IsValidPattern(_textBoxSearchString.Text);
            if (!isValid)
            {
                ShowError(error ?? Resources.HighlightDialog_RegexError);
                _tabControl.SelectedTab = _tabLineMatch;
                _ = _textBoxSearchString.Focus();
                return;
            }
        }

        try
        {
            SaveToEntry();
        }
        catch (Exception ex) when (ex is ArgumentException
                                        or RegexMatchTimeoutException
                                        or ArgumentNullException
                                        or InvalidOperationException
                                        or SystemException)
        {
            ShowError(string.Format(CultureInfo.InvariantCulture,
                _isNew
                    ? Resources.HighlightDialog_UI_ErrorDuringAddOfHighLightEntry
                    : Resources.HighlightDialog_UI_ErrorDuringSavingOfHighlightEntry,
                ex.Message));
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowError (string message)
    {
        _ = MessageBox.Show(this, message,
            Resources.LogExpert_Common_UI_Title_Error,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void OnSearchStringChanged (object sender, EventArgs e)
    {
        UpdateOkEnabled();
        UpdateTitle();
        UpdatePreview();
    }

    private void OnSearchStringMouseUp (object sender, MouseEventArgs e)
    {
        // Legacy parity: right-click on search box (when Regex is on) used to be
        // bound to the regex test helper via the checkbox; we keep that gesture
        // available here when the user right-clicks the search string itself.
        if (e.Button == MouseButtons.Right && _checkBoxRegex.Checked)
        {
            OpenRegexHelper();
        }
    }

    private void OnRegexMouseUp (object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            OpenRegexHelper();
        }
    }

    private void OpenRegexHelper ()
    {
        using RegexHelperDialog dlg = new()
        {
            Owner = this,
            CaseSensitive = _checkBoxCaseSensitive.Checked,
            Pattern = _textBoxSearchString.Text,
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _checkBoxCaseSensitive.Checked = dlg.CaseSensitive;
            _textBoxSearchString.Text = dlg.Pattern;
        }
    }

    private void OnBookmarkChanged (object sender, EventArgs e)
    {
        UpdateBookmarkCommentEnabled();
    }

    private void OnBookmarkCommentClick (object sender, EventArgs e)
    {
        using BookmarkCommentDlg dlg = new()
        {
            Comment = _bookmarkComment,
        };

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _bookmarkComment = dlg.Comment;
        }
    }

    private void OnPluginChanged (object sender, EventArgs e)
    {
        UpdatePluginEnabled();
    }

    private void OnSelectPluginClick (object sender, EventArgs e)
    {
        using KeywordActionDlg dlg = new(_currentActionEntry, _keywordActionList);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _currentActionEntry = dlg.ActionEntry;
        }
    }

    private void OnAlertOnHitChanged (object sender, EventArgs e)
    {
        UpdateAlertEnabled();
    }

    private void OnBrowseSoundFileClick (object sender, EventArgs e)
    {
        using OpenFileDialog dlg = new()
        {
            Title = Resources.HighlightDialog_UI_Label_SoundFile,
            Filter = Resources.HighlightDialog_UI_OpenFileFilter_Audio,
            CheckFileExists = true,
            Multiselect = false,
        };

        if (!string.IsNullOrWhiteSpace(_textBoxSoundFile.Text))
        {
            try
            {
                var existingDir = Path.GetDirectoryName(_textBoxSoundFile.Text);
                if (!string.IsNullOrEmpty(existingDir) && Directory.Exists(existingDir))
                {
                    dlg.InitialDirectory = existingDir;
                }
            }
            catch (Exception ex) when (ex is ArgumentException or PathTooLongException)
            {
                // Ignore invalid existing path; the dialog will use its default.
            }
        }

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _textBoxSoundFile.Text = dlg.FileName;
        }
    }

    private void OnWordMatchChanged (object sender, EventArgs e)
    {
        UpdateNoBackgroundEnabled();
    }

    private void OnNoBackgroundChanged (object sender, EventArgs e)
    {
        UpdateBackgroundColorEnabled();
        UpdatePreview();
    }

    private void OnColorOrStyleChanged (object sender, EventArgs e)
    {
        UpdatePreview();
    }

    private void UpdateTitle ()
    {
        if (_isNew)
        {
            Text = Resources.HighlightEntryEditDialog_UI_Title_Add;
            return;
        }

        var search = _textBoxSearchString.Text ?? string.Empty;
        Text = string.Format(CultureInfo.CurrentCulture, Resources.HighlightEntryEditDialog_UI_Title_EditFormat, search);
    }

    private void UpdateOkEnabled ()
    {
        _btnOk.Enabled = !string.IsNullOrEmpty(_textBoxSearchString.Text);
    }

    private void UpdateAlertEnabled ()
    {
        var on = _checkBoxAlertOnHit.Checked;
        _textBoxSoundFile.Enabled = on;
        _btnBrowseSoundFile.Enabled = on;
        _numericCooldownSeconds.Enabled = on;
    }

    private void UpdateBookmarkCommentEnabled ()
    {
        _btnBookmarkComment.Enabled = _checkBoxBookmark.Checked;
    }

    private void UpdatePluginEnabled ()
    {
        _btnSelectPlugin.Enabled = _checkBoxPlugin.Checked;
    }

    private void UpdateNoBackgroundEnabled ()
    {
        _checkBoxNoBackground.Enabled = _checkBoxWordMatch.Checked;
    }

    private void UpdateBackgroundColorEnabled ()
    {
        var enabled = !_checkBoxNoBackground.Checked;
        _colorBoxBackground.Enabled = enabled;
        _btnCustomBackColor.Enabled = enabled;
    }

    private void UpdatePreview ()
    {
        if (_previewLabel == null)
        {
            return;
        }

        var text = _textBoxSearchString?.Text ?? string.Empty;
        _previewLabel.Text = string.IsNullOrEmpty(text)
            ? Resources.HighlightEntryEditDialog_UI_Preview_Placeholder
            : text;

        _previewLabel.ForeColor = GetSelectedColor(_colorBoxForeground, Color.Black);

        _previewLabel.BackColor = _checkBoxNoBackground != null && _checkBoxNoBackground.Checked
            ? SystemColors.Window
            : GetSelectedColor(_colorBoxBackground, SystemColors.Window);

        var bold = _checkBoxBold != null && _checkBoxBold.Checked;
        var baseFont = Font;
        var desiredStyle = bold ? FontStyle.Bold : FontStyle.Regular;
        if (_previewLabel.Font.Style != desiredStyle || _previewLabel.Font.FontFamily.Name != baseFont.FontFamily.Name)
        {
            var previousFont = _previewLabel.Font;
            _previewLabel.Font = new Font(baseFont, desiredStyle);
            if (!ReferenceEquals(previousFont, baseFont))
            {
                previousFont.Dispose();
            }
        }
    }

    private static Color GetSelectedColor (ColorComboBox combo, Color fallback)
    {
        return combo == null
            ? fallback
            : combo.SelectedItem is Color selected
                ? selected
                : combo.CustomColor.IsEmpty
                    ? fallback
                    : combo.CustomColor;
    }

    private void ChooseColor (ColorComboBox comboBox)
    {
        using ColorDialog colorDialog = new()
        {
            AllowFullOpen = true,
            ShowHelp = false,
            Color = comboBox.CustomColor,
        };

        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            comboBox.CustomColor = colorDialog.Color;
            comboBox.SelectedIndex = 0;
            UpdatePreview();
        }
    }

    private void OnBtnCustomForeColorClicked (object sender, EventArgs e)
    {
        ChooseColor(_colorBoxForeground);
    }

    private void OnBtnCustomBackColorClicked (object sender, EventArgs e)
    {
        ChooseColor(_colorBoxBackground);
    }
}
