using LogExpert.Core.Classes.Filter;

using System.Runtime.Versioning;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class FilterColumnChooser : Form
{
    #region Fields

    private readonly ILogLineColumnizer _columnizer;
    private readonly FilterParams _filterParams;

    #endregion

    #region cTor

    //TODO: add Suspend and ResumeLayout()
    public FilterColumnChooser(FilterParams filterParams)
    {
        InitializeComponent();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        columnListBox.ItemHeight = columnListBox.Font.Height;

        _columnizer = filterParams.CurrentColumnizer;
        _filterParams = filterParams;

        Init();
    }

    #endregion

    #region Private Methods

    private void Init()
    {
        var count = _columnizer.GetColumnCount();
        var names = _columnizer.GetColumnNames();

        for (var i = 0; i < count; ++i)
        {
            columnListBox.Items.Add(names[i], _filterParams.ColumnList.Contains(i));
        }

        emptyColumnUsePrevRadioButton.Checked = _filterParams.EmptyColumnUsePrev;
        emptyColumnHitRadioButton.Checked = _filterParams.EmptyColumnHit;
        emptyColumnNoHitRadioButton.Checked = _filterParams.EmptyColumnHit == false && _filterParams.EmptyColumnUsePrev == false;
        checkBoxExactMatch.Checked = _filterParams.ExactColumnMatch;
    }

    #endregion

    #region Events handler

    private void OnOkButtonClick(object sender, EventArgs e)
    {
        _filterParams.ColumnList.Clear();

        foreach (int colNum in columnListBox.CheckedIndices)
        {
            _filterParams.ColumnList.Add(colNum);
        }

        _filterParams.EmptyColumnUsePrev = emptyColumnUsePrevRadioButton.Checked;
        _filterParams.EmptyColumnHit = emptyColumnHitRadioButton.Checked;
        _filterParams.ExactColumnMatch = checkBoxExactMatch.Checked;
    }

    #endregion
}