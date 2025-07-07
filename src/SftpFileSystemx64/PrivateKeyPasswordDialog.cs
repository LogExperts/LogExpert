using System;
using System.Drawing;
using System.Windows.Forms;

namespace SftpFileSystem;

public partial class PrivateKeyPasswordDialog : Form
{
    #region Ctor

    public PrivateKeyPasswordDialog()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        InitializeComponent();
        ResumeLayout();
    }

    #endregion

    #region Properties / Indexers

    public string Password { get; private set; }

    #endregion

    #region Event handling Methods

    private void OnLoginDialogLoad(object sender, EventArgs e)
    {
        passwordTextBox.Focus();
    }

    private void OnBtnOkClick(object sender, EventArgs e)
    {
        Password = passwordTextBox.Text;
    }

    #endregion
}
