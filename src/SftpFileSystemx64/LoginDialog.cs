using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SftpFileSystem;

public partial class LoginDialog : Form
{
    #region Private Fields

    private string _username;

    #endregion

    #region Ctor

    public LoginDialog(string host, IList<string> userNames, bool hidePasswordField)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();
        serverNameLabel.Text = host;
        if (userNames != null)
        {
            foreach (var name in userNames)
            {
                if (name != null)
                {
                    cmbUsername.Items.Add(name);
                }
            }
        }

        if (hidePasswordField)
        {
            txtBoxPassword.Enabled = false;
            lblPassword.Enabled = false;
        }

        ResumeLayout();
    }

    #endregion

    #region Properties / Indexers

    public string Password { get; private set; }

    public string Username
    {
        get => _username;
        set
        {
            _username = value ?? string.Empty;
            cmbUsername.Text = value;
        }
    }

    #endregion

    #region Event handling Methods

    private void OnBtnOKClick(object sender, EventArgs e)
    {
        Password = txtBoxPassword.Text;
        _username = cmbUsername.Text;
    }

    private void OnLoginDialogLoad(object sender, EventArgs e)
    {
        if (cmbUsername.Text.Length > 0)
        {
            txtBoxPassword.Focus();
        }
    }

    #endregion
}
