namespace LogExpert.Dialogs
{
  partial class DateTimeDragControl
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
      // DateTimeDragControl
      // 
      this.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.Name = "DateTimeDragControl";
      this.Size = new System.Drawing.Size(142, 57);
      this.Load += new System.EventHandler(this.DateTimeDragControl_Load);
      this.MouseLeave += new System.EventHandler(this.DateTimeDragControl_MouseLeave);
      this.Resize += new System.EventHandler(this.DateTimeDragControl_Resize);
      this.ResumeLayout(false);
    }

    #endregion
  }
}
