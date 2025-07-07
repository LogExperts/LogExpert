namespace LogExpert.Dialogs;

partial class AllowOnlyOneInstanceErrorDialog
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
        this.checkBoxIgnoreMessage = new System.Windows.Forms.CheckBox();
        this.buttonOk = new System.Windows.Forms.Button();
        this.labelErrorText = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // checkBoxIgnoreMessage
        // 
        this.checkBoxIgnoreMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
        this.checkBoxIgnoreMessage.AutoSize = true;
        this.checkBoxIgnoreMessage.Location = new System.Drawing.Point(9, 56);
        this.checkBoxIgnoreMessage.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
        this.checkBoxIgnoreMessage.Name = "checkBoxIgnoreMessage";
        this.checkBoxIgnoreMessage.Size = new System.Drawing.Size(177, 17);
        this.checkBoxIgnoreMessage.TabIndex = 0;
        this.checkBoxIgnoreMessage.Text = "Show this message only once\\?";
        this.checkBoxIgnoreMessage.UseVisualStyleBackColor = true;
        // 
        // buttonOk
        // 
        this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.buttonOk.Location = new System.Drawing.Point(209, 53);
        this.buttonOk.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
        this.buttonOk.Name = "buttonOk";
        this.buttonOk.Size = new System.Drawing.Size(74, 23);
        this.buttonOk.TabIndex = 1;
        this.buttonOk.Text = "Ok";
        this.buttonOk.UseVisualStyleBackColor = true;
        this.buttonOk.Click += new System.EventHandler(this.OnButtonOkClick);
        // 
        // labelErrorText
        // 
        this.labelErrorText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
        this.labelErrorText.AutoEllipsis = true;
        this.labelErrorText.Location = new System.Drawing.Point(9, 8);
        this.labelErrorText.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
        this.labelErrorText.Name = "labelErrorText";
        this.labelErrorText.Size = new System.Drawing.Size(273, 43);
        this.labelErrorText.TabIndex = 2;
        this.labelErrorText.Text = "Only one instance allowed, uncheck \\\"View Settings => Allow only 1 Instances\\\" to" +
" start multiple instances!";
        // 
        // AllowOnlyOneInstanceErrorDialog
        // 
        this.ClientSize = new System.Drawing.Size(293, 84);
        this.Controls.Add(this.labelErrorText);
        this.Controls.Add(this.buttonOk);
        this.Controls.Add(this.checkBoxIgnoreMessage);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
        this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
        this.MaximizeBox = false;
        this.Name = "AllowOnlyOneInstanceErrorDialog";
        this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
        this.Text = "AllowOnlyOneInstanceErrorDialog";
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.CheckBox checkBoxIgnoreMessage;
    private System.Windows.Forms.Button buttonOk;
    private System.Windows.Forms.Label labelErrorText;
}