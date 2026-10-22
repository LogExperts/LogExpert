using System.ComponentModel;
using System.Runtime.Versioning;

using LogExpert.Core.Enums;

namespace LogExpert.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class SessionLoadDlg : Form
{
    #region Fields

    #endregion

    #region cTor

    public SessionLoadDlg ()
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();

        ApplyResources();

        ResumeLayout();
    }

    private void ApplyResources ()
    {
        Text = Resources.MissingFilesDialog_UI_Title;
        labelInformational.Text = Resources.MissingFilesDialog_UI_Label_Informational;
        labelChooseHowToProceed.Text = Resources.MissingFilesDialog_UI_Label_ChooseHowToProceed;
        buttonCloseTabs.Text = Resources.MissingFilesDialog_UI_Button_CloseTabs;
        buttonNewWindow.Text = Resources.MissingFilesDialog_UI_Button_NewWindow;
        buttonIgnore.Text = Resources.MissingFilesDialog_UI_Button_Ignore;
    }

    #endregion

    #region Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public SessionLoadDlgResult SessionLoadResult { get; set; } = SessionLoadDlgResult.Cancel;

    #endregion

    #region Events handler

    private void OnButtonCloseTabsClick (object sender, EventArgs e)
    {
        SessionLoadResult = SessionLoadDlgResult.CloseTabs;
        Close();
    }

    private void OnButtonNewWindowClick (object sender, EventArgs e)
    {
        SessionLoadResult = SessionLoadDlgResult.NewWindow;
        Close();
    }

    private void OnButtonIgnoreClick (object sender, EventArgs e)
    {
        SessionLoadResult = SessionLoadDlgResult.IgnoreLayout;
        Close();
    }

    #endregion
}