using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

using LogExpert.Core.Helpers;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class RegexHelperDialog : Form
{
    #region Fields

    private const int MAX_HISTORY = 30;
    private bool _caseSensitive;

    #endregion

    #region cTor

    public RegexHelperDialog ()
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();

        ApplyResources();

        Load += OnRegexHelperDialogLoad;

        ResumeLayout();
    }

    private void ApplyResources ()
    {
        buttonCancel.Text = Resources.LogExpert_Common_UI_Button_Cancel;
        buttonOk.Text = Resources.LogExpert_Common_UI_Button_OK;
        labelRegex.Text = Resources.RegexHelperDialog_UI_Label_Regex;
        labelTestText.Text = Resources.RegexHelperDialog_UI_Label_TestText;
        labelMatches.Text = Resources.RegexHelperDialog_UI_Label_Matches;
        checkBoxCaseSensitive.Text = Resources.RegexHelperDialog_UI_CheckBox_CaseSensitive;
        buttonHelp.Text = Resources.LogExpert_Common_UI_Button_Help;
        Text = Resources.RegexHelperDialog_UI_Title;
    }

    #endregion

    #region Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool CaseSensitive
    {
        get => _caseSensitive;
        set
        {
            _caseSensitive = value;
            checkBoxCaseSensitive.Checked = value;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Pattern
    {
        get => comboBoxRegex.Text;
        set => comboBoxRegex.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public List<string> ExpressionHistoryList { get; set; } = [];

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public List<string> TesttextHistoryList { get; set; } = [];

    #endregion

    #region Private Methods

    private void UpdateMatches ()
    {
        textBoxMatches.Text = string.Empty;

        try
        {
            Regex rex = RegexHelper.CreateSafeRegex(comboBoxRegex.Text, _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            var (isValid, _) = RegexHelper.IsValidPattern(comboBoxRegex.Text);
            if (isValid)
            {
                var matches = rex.Matches(comboBoxTestText.Text);

                foreach (Match match in matches)
                {
                    textBoxMatches.Text += $"Match Value: \"{match.Value}\"\r\n";
                }
            }
            else
            {
                textBoxMatches.Text = Resources.RegexHelperDialog_UI_TextBox_Matches_NoValidRegexPattern;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or
                                         ArgumentNullException)
        {
            textBoxMatches.Text = Resources.RegexHelperDialog_UI_TextBox_Matches_NoValidRegexPattern;
        }
    }

    internal void LoadHistory ()
    {
        comboBoxRegex.Items.Clear();
        comboBoxRegex.DataSource = ExpressionHistoryList;

        comboBoxTestText.Items.Clear();
        comboBoxTestText.DataSource = TesttextHistoryList;
    }

    #endregion

    #region Events handler

    private void OnRegexHelperDialogLoad (object? sender, EventArgs e)
    {
        LoadHistory();
    }

    private void OnCaseSensitiveCheckBoxCheckedChanged (object sender, EventArgs e)
    {
        _caseSensitive = checkBoxCaseSensitive.Checked;
        UpdateMatches();
    }

    internal void OnButtonOkClick (object sender, EventArgs e)
    {
        // Both combos are DataSource-bound to the history lists (LoadHistory), so their
        // Items collections must not be touched — mutate the bound lists instead. The
        // dialog closes with DialogResult.OK right after, so no rebind is needed.
        var text = comboBoxRegex.Text;
        _ = ExpressionHistoryList.Remove(text);
        ExpressionHistoryList.Insert(0, text);

        text = comboBoxTestText.Text;
        _ = TesttextHistoryList.Remove(text);
        TesttextHistoryList.Insert(0, text);

        if (ExpressionHistoryList.Count > MAX_HISTORY)
        {
            ExpressionHistoryList.RemoveAt(ExpressionHistoryList.Count - 1);
        }

        if (TesttextHistoryList.Count > MAX_HISTORY)
        {
            TesttextHistoryList.RemoveAt(TesttextHistoryList.Count - 1);
        }
    }

    private void OnComboBoxRegexTextChanged (object sender, EventArgs e)
    {
        UpdateMatches();
    }

    private void OnComboBoxTestTextTextChanged (object sender, EventArgs e)
    {
        UpdateMatches();
    }

    private void OnButtonHelpClick (object sender, EventArgs e)
    {
        Help.ShowHelp(this, Resources.LogTabWindow_HelpFile, HelpNavigator.Topic, Resources.RegexHelperDialog_Help_Chapter);
    }

    #endregion
}