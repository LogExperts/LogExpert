using System.Runtime.Versioning;

namespace LogExpert.Dialogs;

[SupportedOSPlatform("windows")]
public partial class AllowOnlyOneInstanceErrorDialog : Form
{
    public bool DoNotShowThisMessageAgain { get; private set; }

    public AllowOnlyOneInstanceErrorDialog ()
    {
        InitializeComponent();
        SetText();
    }

    private void SetText ()
    {
        labelErrorText.Text = @"Only one instance allowed, uncheck ""View Settings => Allow only 1 Instances"" to start multiple instances!";
    }

    private void OnButtonOkClick (object sender, System.EventArgs e)
    {
        DoNotShowThisMessageAgain = checkBoxIgnoreMessage.Checked;
    }
}
