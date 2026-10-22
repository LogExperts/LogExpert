using Log4jXmlColumnizer;

using System;
using System.Drawing;
using System.Windows.Forms;

namespace LogExpert;

public partial class Log4jXmlColumnizerConfigDlg : Form
{
    #region Fields

    private readonly Log4jXmlColumnizerConfig _config;

    #endregion

    #region cTor

    public Log4jXmlColumnizerConfigDlg(Log4jXmlColumnizerConfig config)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        _config = config;
        InitializeComponent();

        ApplyResources();

        FillListBox();
        localTimeCheckBox.Checked = _config.LocalTimestamps;
        ResumeLayout();
    }

    #endregion

    private void ApplyResources ()
    {
        Text = Resources.Log4jXmlColumnizerConfigDlg_UI_Title;
        label1.Text = Resources.Log4jXmlColumnizerConfigDlg_UI_Label_ChooseColumns;
        localTimeCheckBox.Text = Resources.Log4jXmlColumnizerConfigDlg_UI_CheckBox_LocalTime;
        Column.HeaderText = Resources.Log4jXmlColumnizerConfigDlg_UI_ColumnHeader_Column;
        maxWidth.HeaderText = Resources.Log4jXmlColumnizerConfigDlg_UI_ColumnHeader_MaxLen;
        maxWidth.ToolTipText = Resources.Log4jXmlColumnizerConfigDlg_UI_ColumnHeader_ToolTipMaxLen;
        okButton.Text = Resources.Log4jXmlColumnizerConfigDlg_UI_Button_OK;
        cancelButton.Text = Resources.Log4jXmlColumnizerConfigDlg_UI_Button_Cancel;
    }

    #region Private Methods

    private void FillListBox()
    {
        var checkColumn = (DataGridViewCheckBoxColumn)columnGridView.Columns[0];
        var nameColumn = (DataGridViewTextBoxColumn)columnGridView.Columns[1];
        var lenColumn = (DataGridViewTextBoxColumn)columnGridView.Columns[2];

        foreach (var entry in _config.ColumnList)
        {
            DataGridViewRow row = new();
            row.Cells.Add(new DataGridViewCheckBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells.Add(new DataGridViewTextBoxCell());
            row.Cells[0].Value = entry.Visible;
            row.Cells[1].Value = entry.ColumnName;
            row.Cells[2].Value = entry.MaxLen > 0 ? "" + entry.MaxLen : "";
            columnGridView.Rows.Add(row);
        }
    }

    #endregion

    #region Events handler

    private void OkButton_Click(object sender, EventArgs e)
    {
        //  for (int i = 0; i < this.config.columnList.Count; ++i)
        //  {
        //    this.config.columnList[i]. visible = this.columnListBox.GetItemChecked(i);
        //  }
        for (var i = 0; i < columnGridView.Rows.Count; ++i)
        {
            _config.ColumnList[i].Visible = (bool)columnGridView.Rows[i].Cells[0].Value;
            var sLen = (string)columnGridView.Rows[i].Cells[2].Value;

            if (int.TryParse(sLen, out var len))
            {
                _config.ColumnList[i].MaxLen = len;
            }
            else
            {
                _config.ColumnList[i].MaxLen = 0;
            }
        }

        _config.LocalTimestamps = localTimeCheckBox.Checked;
    }

    #endregion
}