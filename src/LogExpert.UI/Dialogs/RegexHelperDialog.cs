using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class RegexHelperDialog : Form
{
    #region Fields

    private const int MAX_HISTORY = 30;
    private bool _caseSensitive;
    private List<string> _expressionHistoryList = [];
    private List<string> _testtextHistoryList = [];

    #endregion

    #region cTor

    public RegexHelperDialog ()
    {
        InitializeComponent();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Load += OnRegexHelperDialogLoad;
    }

    #endregion

    #region Properties

    public bool CaseSensitive
    {
        get => _caseSensitive;
        set
        {
            _caseSensitive = value;
            checkBoxCaseSensitive.Checked = value;
        }
    }

    public string Pattern
    {
        get => comboBoxRegex.Text;
        set => comboBoxRegex.Text = value;
    }

    public List<string> ExpressionHistoryList
    {
        get => _expressionHistoryList;
        set => _expressionHistoryList = value;
    }

    public List<string> TesttextHistoryList
    {
        get => _testtextHistoryList;
        set => _testtextHistoryList = value;
    }

    #endregion

    #region Private Methods

    private void UpdateMatches ()
    {
        textBoxMatches.Text = "";
        try
        {
            Regex rex = new(comboBoxRegex.Text, _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            MatchCollection matches = rex.Matches(comboBoxTestText.Text);

            foreach (Match match in matches)
            {
                textBoxMatches.Text += $"{match.Value}\r\n";
            }
        }
        catch (ArgumentException)
        {
            textBoxMatches.Text = "No valid regex pattern";
        }
    }

    private void LoadHistory ()
    {
        comboBoxRegex.Items.Clear();
        comboBoxRegex.DataSource = _expressionHistoryList;

        comboBoxTestText.Items.Clear();
        comboBoxTestText.DataSource = _testtextHistoryList;
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

    private void OnButtonOkClick (object sender, EventArgs e)
    {
        var text = comboBoxRegex.Text;
        comboBoxRegex.Items.Remove(text);
        comboBoxRegex.Items.Insert(0, text);

        text = comboBoxTestText.Text;
        comboBoxTestText.Items.Remove(text);
        comboBoxTestText.Items.Insert(0, text);

        if (comboBoxRegex.Items.Count > MAX_HISTORY)
        {
            comboBoxRegex.Items.Remove(comboBoxRegex.Items.Count - 1);
        }

        if (comboBoxTestText.Items.Count > MAX_HISTORY)
        {
            comboBoxTestText.Items.Remove(comboBoxTestText.Items.Count - 1);
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
        Help.ShowHelp(this, "LogExpert.chm", HelpNavigator.Topic, "RegEx.htm");
    }

    #endregion
}