using System;
using System.Drawing;
using System.Windows.Forms;

namespace SftpFileSystem;

public partial class FailedKeyDialog : Form
{
    #region Ctor

    public FailedKeyDialog()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        InitializeComponent();
        ResumeLayout();
    }

    #endregion

    #region Event handling Methods

    private void OnBtnCancelClick(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void OnBtnRetryClick(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Retry;
        Close();
    }

    private void OnBtnUsePasswordAuthenticationClick(object sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    #endregion
}
