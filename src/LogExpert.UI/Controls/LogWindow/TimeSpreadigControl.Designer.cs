namespace LogExpert.UI.Controls.LogWindow
{
  partial class TimeSpreadingControl
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.SuspendLayout();
      // 
      // TimeSpreadingControl
      // 
      this.Name = "TimeSpreadingControl";
      this.Size = new System.Drawing.Size(26, 324);
      this.MouseLeave += new System.EventHandler(this.OnTimeSpreadingControlMouseLeave);
      this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnTimeSpreadingControlMouseMove);
      this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnTimeSpreadingControlMouseDown);
      this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnTimeSpreadingControlMouseUp);
      this.SizeChanged += new System.EventHandler(this.OnTimeSpreadingControlSizeChanged);
      this.MouseEnter += new System.EventHandler(this.OnTimeSpreadingControlMouseEnter);
      this.ResumeLayout(false);

    }

    #endregion
  }
}
