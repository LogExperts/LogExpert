using System.ComponentModel;
using System.Globalization;
using System.Runtime.Versioning;

namespace LogExpert.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class ParamRequesterDialog : Form
{
    #region Fields

    private readonly string[] _values;
    private readonly string _paramName;

    #endregion

    #region cTor

    public ParamRequesterDialog (string parameterName, string[] values)
    {
        SuspendLayout();

        _values = values;
        _paramName = parameterName;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();
        ApplyResources();

        ResumeLayout();
    }

    private void ApplyResources ()
    {
        Text = Resources.ParamRequesterDialog_UI_Title;
        buttonOk.Text = Resources.LogExpert_Common_UI_Button_OK;
        buttonCancel.Text = Resources.LogExpert_Common_UI_Button_Cancel;

        // The tool argument syntax (?"Name?"(a,b)) may carry a parameter name; show it when present.
        labelValueForParameter.Text = string.IsNullOrEmpty(_paramName)
            ? Resources.ParamRequesterDialog_UI_Label_ValueForParameter
            : string.Format(CultureInfo.CurrentCulture, Resources.ParamRequesterDialog_UI_Label_ValueForParameterNamed, _paramName);
    }

    #endregion

    #region Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string ParamValue { get; set; }

    #endregion

    #region Events handler

    private void OnParamRequesterDialogShown (object sender, EventArgs e)
    {
        if (_values != null)
        {
            foreach (var value in _values)
            {
                _ = comboBoxValue.Items.Add(value);
            }

            comboBoxValue.SelectedIndex = 0;
        }
    }

    private void OnButtonOkClick (object sender, EventArgs e)
    {
        ParamValue = comboBoxValue.Text;
    }

    #endregion
}