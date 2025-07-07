using System.Runtime.Versioning;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class MultiFileMaskDialog : Form
{
    #region Fields

    #endregion

    #region cTor

    public MultiFileMaskDialog(Form parent, string fileName)
    {
        InitializeComponent();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        syntaxHelpLabel.Text = "" +
                                    "Pattern syntax:\n\n" +
                                    "* = any characters (wildcard)\n" +
                                    "$D(<date>) = Date pattern\n" +
                                    "$I = File index number\n" +
                                    "$J = File index number, hidden when zero\n" +
                                    "$J(<prefix>) = Like $J, but adding <prefix> when non-zero\n" +
                                    "\n" +
                                    "<date>:\n" +
                                    "DD = day\n" +
                                    "MM = month\n" +
                                    "YY[YY] = year\n" +
                                    "all other chars will be used as given";
        labelFileName.Text = fileName;
    }

    #endregion

    #region Properties

    public string FileNamePattern { get; set; }

    public int MaxDays { get; set; }

    #endregion

    #region Events handler

    private void OnButtonOKClick(object sender, EventArgs e)
    {
        FileNamePattern = fileNamePatternTextBox.Text;
        MaxDays = (int)upDownMaxDays.Value;
    }

    private void OnMultiFileMaskDialogLoad(object sender, EventArgs e)
    {
        fileNamePatternTextBox.Text = FileNamePattern;
        upDownMaxDays.Value = MaxDays;
    }

    #endregion
}