using System.ComponentModel;
using System.Runtime.Versioning;

using LogExpert.UI.Controls.LogTabWindow;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Entities;

namespace LogExpert.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class ToolArgsDialog : Form
{
    #region Fields

    private readonly LogTabWindow _logTabWin;

    #endregion

    #region cTor

    public ToolArgsDialog (LogTabWindow logTabWin, Form parent)
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        _logTabWin = logTabWin;
        parent.AddOwnedForm(this);
        TopMost = parent.TopMost;

        InitializeComponent();

        ApplyResources();

        ResumeLayout();
    }

    private void ApplyResources ()
    {
        Text = Resources.ToolArgsDialog_UI_Title;
        labelEnterArguments.Text = Resources.ToolArgsDialog_UI_Label_EnterCommandLine;
        buttonTest.Text = Resources.ToolArgsDialog_UI_Button_Test;
        buttonRegexHelp.Text = Resources.ToolArgsDialog_UI_Button_RegexHelp;
        buttonOk.Text = Resources.LogExpert_Common_UI_Button_OK;
        buttonCancel.Text = Resources.LogExpert_Common_UI_Button_Cancel;
    }

    #endregion

    #region Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Arg { get; set; }

    #endregion

    #region Events handler

    private void OnToolArgsDialogLoad (object sender, EventArgs e)
    {
        labelHelp.Text = Resources.ToolArgsDialog_UI_HelpText;
        textBoxArguments.Text = Arg;
    }

    private void OnButtonRegexHelpClick (object sender, EventArgs e)
    {
        RegexHelperDialog regexDlg = new();
        if (regexDlg.ShowDialog() == DialogResult.OK)
        {
            textBoxArguments.SelectedText = regexDlg.Pattern;
        }
    }

    //TODO: what is the purpose of this in the settings? Can we just send the line and info instead of the object?
    private void OnButtonTestClick (object sender, EventArgs e)
    {
        if (_logTabWin.CurrentLogWindow != null)
        {
            var line = _logTabWin.CurrentLogWindow.GetCurrentLine();
            var info = _logTabWin.CurrentLogWindow.GetCurrentFileInfo();
            if (line != null && info != null)
            {
                ArgParser parser = new(textBoxArguments.Text);
                var args = parser.BuildArgs(line, _logTabWin.CurrentLogWindow.GetRealLineNum() + 1, info, this);
                labelTestResult.Text = args;
            }
        }
    }

    private void OnButtonOkClick (object sender, EventArgs e)
    {
        Arg = textBoxArguments.Text;
    }

    #endregion
}