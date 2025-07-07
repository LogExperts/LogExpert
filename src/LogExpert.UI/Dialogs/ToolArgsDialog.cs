using System.Runtime.Versioning;

using LogExpert.UI.Controls.LogTabWindow;
using LogExpert.UI.Dialogs;
using LogExpert.UI.Entities;

namespace LogExpert.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class ToolArgsDialog : Form
{
    #region Fields

    private readonly LogTabWindow logTabWin;

    #endregion

    #region cTor

    public ToolArgsDialog (LogTabWindow logTabWin, Form parent)
    {
        this.logTabWin = logTabWin;
        parent.AddOwnedForm(this);
        TopMost = parent.TopMost;
        InitializeComponent();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
    }

    #endregion

    #region Properties

    public string Arg { get; set; }

    #endregion

    #region Events handler

    private void OnToolArgsDialogLoad (object sender, EventArgs e)
    {
        labelHelp.Text = "" +
                              "%L = Current line number\n" +
                              "%N = Current log file name without path\n" +
                              "%P = Path (directory) of current log file\n" +
                              "%F = Full name (incl. path) of log file\n" +
                              "%E = Extension of log file name (e.g. 'txt')\n" +
                              "%M = Name of log file without extension\n" +
                              "%S = User (from URI)\n" +
                              "%R = Path (from URI)\n" +
                              "%H = Host (from URI)\n" +
                              "%T = Port (from URI)\n" +
                              "?\"<name>\" = variable parameter 'name'\n" +
                              "?\"<name>\"(def1,def2,...) = variable parameter with predefined values\n" +
                              "\n" +
                              "{<regex>}{<replace>}:\n" +
                              "Regex search/replace on current selected line.";

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
        if (logTabWin.CurrentLogWindow != null)
        {
            ILogLine line = logTabWin.CurrentLogWindow.GetCurrentLine();
            ILogFileInfo info = logTabWin.CurrentLogWindow.GetCurrentFileInfo();
            if (line != null && info != null)
            {
                ArgParser parser = new(textBoxArguments.Text);
                var args = parser.BuildArgs(line, logTabWin.CurrentLogWindow.GetRealLineNum() + 1, info, this);
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