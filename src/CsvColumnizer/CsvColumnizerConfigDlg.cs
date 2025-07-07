using System;
using System.Drawing;
using System.Windows.Forms;

namespace CsvColumnizer;

public partial class CsvColumnizerConfigDlg : Form
{
    #region Fields

    private readonly CsvColumnizerConfig _config;

    #endregion

    #region cTor

    public CsvColumnizerConfigDlg(CsvColumnizerConfig config)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        _config = config;
        InitializeComponent();
        FillValues();
        ResumeLayout();
    }

    #endregion

    #region Private Methods

    private void FillValues()
    {
        delimiterTextBox.Text = _config.DelimiterChar;
        textBoxQuoteChar.Text = _config.QuoteChar.ToString();
        textboxEscapeChar.Text = _config.EscapeChar.ToString();
        checkBoxEscape.Checked = _config.EscapeChar != '\0';
        textBoxCommentChar.Text = _config.CommentChar.ToString();
        checkBoxFieldNames.Checked = _config.HasFieldNames;
        textboxEscapeChar.Enabled = checkBoxEscape.Checked;
        numericUpDownMinColumns.Value = _config.MinColumns;
    }

    private void RetrieveValues()
    {
        _config.DelimiterChar = delimiterTextBox.Text;
        _config.QuoteChar = textBoxQuoteChar.Text[0];
        _config.EscapeChar = checkBoxEscape.Checked ? textboxEscapeChar.Text[0] : '\0';
        _config.CommentChar = textBoxCommentChar.Text[0];
        _config.HasFieldNames = checkBoxFieldNames.Checked;
        _config.MinColumns = (int)numericUpDownMinColumns.Value;
    }

    #endregion

    #region Events handler

    private void OnOkButtonClick(object sender, EventArgs e)
    {
        RetrieveValues();
    }

    private void OnEscapeCheckBoxCheckedChanged(object sender, EventArgs e)
    {
        textboxEscapeChar.Enabled = checkBoxEscape.Checked;
    }

    #endregion

}