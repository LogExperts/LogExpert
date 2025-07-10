using System.Globalization;
using System.Runtime.Versioning;

using LogExpert.UI.Dialogs.Eminus;

namespace LogExpert;

[SupportedOSPlatform("windows")]
internal partial class EminusConfigDlg : Form
{
    #region Fields

    #endregion

    #region cTor

    public EminusConfigDlg (EminusConfig config)
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();
        LoadResources();

        TopLevel = false;
        Config = config;

        hostTextBox.Text = config.Host;
        portTextBox.Text = string.Empty + config.Port;
        passwordTextBox.Text = config.Password;

        ResumeLayout();
    }

    private void LoadResources ()
    {
        Text = Resources.EminusConfigDlg_UI_Text;
        labelHost.Text = Resources.EminusConfigDlg_UI_Label_Host;
        labelPort.Text = Resources.EminusConfigDlg_UI_Label_Port;
        labelPassword.Text = Resources.EminusConfigDlg_UI_Label_Password;
        labelDescription.Text = Resources.EminusConfigDlg_UI_Label_Description;
    }

    #endregion

    #region Properties

    public EminusConfig Config { get; set; }

    #endregion

    #region Public methods

    public void ApplyChanges ()
    {
        Config.Host = hostTextBox.Text;
        try
        {
            Config.Port = short.Parse(portTextBox.Text, NumberStyles.None, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            Config.Port = 0;
        }

        Config.Password = passwordTextBox.Text;
    }

    #endregion
}