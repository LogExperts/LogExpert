using System.Globalization;
using System.Runtime.Versioning;

using LogExpert.UI.Extensions;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class PluginHashDialog : Form
{
    #region Fields

    private readonly string _hash;

    #endregion

    #region cTor

    public PluginHashDialog (Form parent, string pluginName, string hash)
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();
        ApplyResources(pluginName);

        Owner = parent;
        _hash = hash;

        textBoxHash.Text = hash;
        textBoxHash.Select(0, 0); // Deselect

        ResumeLayout();
    }

    #endregion

    #region Private Methods

    private void ApplyResources (string pluginName)
    {
        Text = Resources.PluginHashDialog_UI_Title;

        labelPluginName.Text = string.Format(CultureInfo.InvariantCulture, Resources.PluginHashDialog_UI_Label_PluginName, pluginName);
        labelHash.Text = Resources.PluginHashDialog_UI_Label_Hash;

        buttonCopy.Text = Resources.PluginHashDialog_UI_Button_Copy;
        buttonClose.Text = Resources.PluginHashDialog_UI_Button_Close;
    }

    #endregion

    #region Event Handlers

    private void OnButtonCopyClick (object sender, EventArgs e)
    {
        _ = ClipboardHelper.TrySetText(_hash)
            ? MessageBox.Show(
                Resources.PluginHashDialog_UI_Message_CopySuccess,
                Resources.PluginHashDialog_UI_Message_SuccessTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)
            : MessageBox.Show(
                string.Format(CultureInfo.InvariantCulture, Resources.PluginHashDialog_UI_Message_CopyError, Resources.LogExpert_Common_UI_Message_ClipboardInUse),
                Resources.PluginHashDialog_UI_Message_ErrorTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
    }

    private void OnButtonCloseClick (object sender, EventArgs e)
    {
        Close();
    }

    #endregion
}
