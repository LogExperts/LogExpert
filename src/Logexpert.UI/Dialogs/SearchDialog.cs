using LogExpert.Core.Entities;
using LogExpert.UI.Dialogs;

using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace LogExpert.Dialogs;

[SupportedOSPlatform("windows")]
public partial class SearchDialog : Form
{
    #region Fields

    private static readonly int MAX_HISTORY = 30;

    #endregion

    #region cTor

    public SearchDialog()
    {
        InitializeComponent();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Load += OnSearchDialogLoad;
    }

    #endregion

    #region Properties

    public SearchParams SearchParams { get; set; } = new();

    #endregion

    #region Events handler

    private void OnSearchDialogLoad(object? sender, EventArgs e)
    {
        if (SearchParams != null)
        {
            if (SearchParams.IsFromTop)
            {
                radioButtonFromTop.Checked = true;
            }
            else
            {
                radioButtonFromSelected.Checked = true;
            }

            if (SearchParams.IsForward)
            {
                radioButtonForward.Checked = true;
            }
            else
            {
                radioButtonBackward.Checked = true;
            }

            checkBoxRegex.Checked = SearchParams.IsRegex;
            checkBoxCaseSensitive.Checked = SearchParams.IsCaseSensitive;
            foreach (string item in SearchParams.HistoryList)
            {
                comboBoxSearchFor.Items.Add(item);
            }

            if (comboBoxSearchFor.Items.Count > 0)
            {
                comboBoxSearchFor.SelectedIndex = 0;
            }
        }
        else
        {
            radioButtonFromSelected.Checked = true;
            radioButtonForward.Checked = true;
            SearchParams = new SearchParams();
        }
    }

    private void OnButtonRegexClick(object sender, EventArgs e)
    {
        RegexHelperDialog dlg = new()
        {
            Owner = this,
            CaseSensitive = checkBoxCaseSensitive.Checked,
            Pattern = comboBoxSearchFor.Text
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            checkBoxCaseSensitive.Checked = dlg.CaseSensitive;
            comboBoxSearchFor.Text = dlg.Pattern;
        }
    }

    private void OnButtonOkClick(object sender, EventArgs e)
    {
        try
        {
            if (checkBoxRegex.Checked)
            {
                if (string.IsNullOrWhiteSpace(comboBoxSearchFor.Text))
                {
                    throw new ArgumentException("Search text is empty");
                }

                Regex.IsMatch("", comboBoxSearchFor.Text);
            }

            SearchParams.SearchText = comboBoxSearchFor.Text;
            SearchParams.IsCaseSensitive = checkBoxCaseSensitive.Checked;
            SearchParams.IsForward = radioButtonForward.Checked;
            SearchParams.IsFromTop = radioButtonFromTop.Checked;
            SearchParams.IsRegex = checkBoxRegex.Checked;
            SearchParams.HistoryList.Remove(comboBoxSearchFor.Text);
            SearchParams.HistoryList.Insert(0, comboBoxSearchFor.Text);

            if (SearchParams.HistoryList.Count > MAX_HISTORY)
            {
                SearchParams.HistoryList.RemoveAt(SearchParams.HistoryList.Count - 1);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during creation of search parameter\r\n{ex.Message}");
        }
    }

    #endregion

    private void OnButtonCancelClick(object sender, EventArgs e)
    {
        Close();
    }
}