using System.Runtime.Versioning;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class MultiLoadRequestDialog : Form
{
    #region cTor

    public MultiLoadRequestDialog()
    {
        InitializeComponent();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
    }

    #endregion
}