namespace LogExpert
{
  partial class EminusConfigDlg
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EminusConfigDlg));
      this.hostTextBox = new System.Windows.Forms.TextBox();
      this.passwordTextBox = new System.Windows.Forms.TextBox();
      this.labelHost = new System.Windows.Forms.Label();
      this.labelPort = new System.Windows.Forms.Label();
      this.labelPassword = new System.Windows.Forms.Label();
      this.labelDescription = new System.Windows.Forms.Label();
      this.portTextBox = new System.Windows.Forms.MaskedTextBox();
      this.SuspendLayout();
      // 
      // hostTextBox
      // 
      this.hostTextBox.Location = new System.Drawing.Point(76, 69);
      this.hostTextBox.Name = "hostTextBox";
      this.hostTextBox.Size = new System.Drawing.Size(175, 20);
      this.hostTextBox.TabIndex = 0;
      // 
      // passwordTextBox
      // 
      this.passwordTextBox.Location = new System.Drawing.Point(76, 122);
      this.passwordTextBox.Name = "passwordTextBox";
      this.passwordTextBox.Size = new System.Drawing.Size(175, 20);
      this.passwordTextBox.TabIndex = 2;
      this.passwordTextBox.UseSystemPasswordChar = true;
      // 
      // label1
      // 
      this.labelHost.AutoSize = true;
      this.labelHost.Location = new System.Drawing.Point(17, 72);
      this.labelHost.Name = "host";
      this.labelHost.Size = new System.Drawing.Size(29, 13);
      this.labelHost.TabIndex = 5;
      this.labelHost.Text = "Host";
      // 
      // label2
      // 
      this.labelPort.AutoSize = true;
      this.labelPort.Location = new System.Drawing.Point(17, 99);
      this.labelPort.Name = "port";
      this.labelPort.Size = new System.Drawing.Size(26, 13);
      this.labelPort.TabIndex = 6;
      this.labelPort.Text = "Port";
      // 
      // label3
      // 
      this.labelPassword.AutoSize = true;
      this.labelPassword.Location = new System.Drawing.Point(17, 126);
      this.labelPassword.Name = "password";
      this.labelPassword.Size = new System.Drawing.Size(53, 13);
      this.labelPassword.TabIndex = 7;
      this.labelPassword.Text = "Password";
      // 
      // label4
      // 
      this.labelDescription.Location = new System.Drawing.Point(13, 13);
      this.labelDescription.Name = "description";
      this.labelDescription.Size = new System.Drawing.Size(276, 41);
      this.labelDescription.TabIndex = 8;
      this.labelDescription.Text = "Enter the host and the port where the Eclipse plugin is listening to. If a passwo" +
          "rd is configured, enter the password too.";
      // 
      // portTextBox
      // 
      this.portTextBox.Location = new System.Drawing.Point(76, 96);
      this.portTextBox.Mask = "99999";
      this.portTextBox.Name = "port";
      this.portTextBox.Size = new System.Drawing.Size(100, 20);
      this.portTextBox.TabIndex = 1;
      // 
      // EminusConfigDlg
      // 
      this.ClientSize = new System.Drawing.Size(295, 187);
      this.ControlBox = false;
      this.Controls.Add(this.portTextBox);
      this.Controls.Add(this.labelDescription);
      this.Controls.Add(this.labelPassword);
      this.Controls.Add(this.labelPort);
      this.Controls.Add(this.labelHost);
      this.Controls.Add(this.passwordTextBox);
      this.Controls.Add(this.hostTextBox);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "EminusConfigDlg";
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Eclipse Remote Navigation";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox hostTextBox;
    private System.Windows.Forms.TextBox passwordTextBox;
    private System.Windows.Forms.Label labelHost;
    private System.Windows.Forms.Label labelPort;
    private System.Windows.Forms.Label labelPassword;
    private System.Windows.Forms.Label labelDescription;
    private System.Windows.Forms.MaskedTextBox portTextBox;
  }
}