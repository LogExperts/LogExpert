using System.Runtime.Versioning;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class GotoLineDialog : Form
{
    #region Fields

    #endregion

    #region cTor

    public GotoLineDialog(Form parent)
    {
        InitializeComponent();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Owner = parent;
    }

    #endregion

    #region Properties

    public int Line { get; private set; }

    #endregion

    #region Events handler

    private void GotoLineDialog_Load(object sender, EventArgs e)
    {
    }

    private void okButton_Click(object sender, EventArgs e)
    {
        try
        {
            Line = int.Parse(lineNumberTextBox.Text);
        }
        catch (Exception)
        {
            Line = -1;
        }
    }

    #endregion
}