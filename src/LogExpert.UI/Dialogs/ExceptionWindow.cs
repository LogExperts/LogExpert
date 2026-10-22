using System.Runtime.Versioning;

using LogExpert.UI.Extensions;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
public partial class ExceptionWindow : Form
{
    #region Fields

    private readonly string _errorText;

    private readonly string _stackTrace;

    #endregion

    #region cTor

    public ExceptionWindow (string errorText, string stackTrace)
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();
        ApplyResources();

        _errorText = errorText;
        _stackTrace = stackTrace;

        stackTraceTextBox.Text = _errorText + Environment.NewLine + Environment.NewLine + _stackTrace;
        stackTraceTextBox.Select(0, 0);

        ResumeLayout();
    }

    private void ApplyResources ()
    {
        // Dialog title
        Text = Resources.ExceptionWindow_UI_Title;

        labelErrorMessage.Text = Resources.ExceptionWindow_UI_Label_ErrorMessage;

        okButton.Text = Resources.LogExpert_Common_UI_Button_OK;
        copyButton.Text = Resources.ExceptionWindow_UI_Button_CopyToClipboard;
    }

    #endregion

    #region Private Methods

    private void CopyToClipboard ()
    {
        // Never let a failed clipboard access escalate out of the error dialog itself
        _ = ClipboardHelper.TrySetText(_errorText + Environment.NewLine + Environment.NewLine + _stackTrace);
    }

    #endregion

    #region Events handler

    private void OnCopyButtonClick (object sender, EventArgs e)
    {
        CopyToClipboard();
    }

    #endregion
}