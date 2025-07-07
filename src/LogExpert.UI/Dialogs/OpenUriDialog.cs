using System.Runtime.Versioning;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class OpenUriDialog : Form
{
    #region Fields

    #endregion

    #region cTor

    public OpenUriDialog()
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        InitializeComponent();

        ResumeLayout();
    }

    #endregion

    #region Properties

    public string Uri => cmbUri.Text;

    public IList<string> UriHistory { get; set; }

    #endregion

    #region Events handler

    private void OnOpenUriDialogLoad(object sender, EventArgs e)
    {
        if (UriHistory != null)
        {
            cmbUri.Items.Clear();
            foreach (var uri in UriHistory)
            {
                cmbUri.Items.Add(uri);
            }
        }
    }

    private void OnBtnOkClick(object sender, EventArgs e)
    {
        UriHistory = [];

        foreach (var item in cmbUri.Items)
        {
            UriHistory.Add(item.ToString());
        }

        if (UriHistory.Contains(cmbUri.Text))
        {
            UriHistory.Remove(cmbUri.Text);
        }
        UriHistory.Insert(0, cmbUri.Text);

        while (UriHistory.Count > 20)
        {
            UriHistory.RemoveAt(UriHistory.Count - 1);
        }
    }

    #endregion
}