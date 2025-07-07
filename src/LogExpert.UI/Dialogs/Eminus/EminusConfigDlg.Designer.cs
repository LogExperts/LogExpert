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
      this.label1 = new System.Windows.Forms.Label();
      this.label2 = new System.Windows.Forms.Label();
      this.label3 = new System.Windows.Forms.Label();
      this.label4 = new System.Windows.Forms.Label();
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
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(17, 72);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(29, 13);
      this.label1.TabIndex = 5;
      this.label1.Text = "Host";
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(17, 99);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(26, 13);
      this.label2.TabIndex = 6;
      this.label2.Text = "Port";
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(17, 126);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(53, 13);
      this.label3.TabIndex = 7;
      this.label3.Text = "Password";
      // 
      // label4
      // 
      this.label4.Location = new System.Drawing.Point(13, 13);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(276, 41);
      this.label4.TabIndex = 8;
      this.label4.Text = "Enter the host and the port where the Eclipse plugin is listening to. If a passwo" +
          "rd is configured, enter the password too.";
      // 
      // portTextBox
      // 
      this.portTextBox.Location = new System.Drawing.Point(76, 96);
      this.portTextBox.Mask = "99999";
      this.portTextBox.Name = "portTextBox";
      this.portTextBox.Size = new System.Drawing.Size(100, 20);
      this.portTextBox.TabIndex = 1;
      // 
      // EminusConfigDlg
      // 
      this.ClientSize = new System.Drawing.Size(295, 187);
      this.ControlBox = false;
      this.Controls.Add(this.portTextBox);
      this.Controls.Add(this.label4);
      this.Controls.Add(this.label3);
      this.Controls.Add(this.label2);
      this.Controls.Add(this.label1);
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
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.MaskedTextBox portTextBox;
  }
}