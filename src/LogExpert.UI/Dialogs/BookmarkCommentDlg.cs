using System.Runtime.Versioning;

namespace LogExpert.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class BookmarkCommentDlg : Form
{
    #region cTor

    public BookmarkCommentDlg()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();
    }

    #endregion

    #region Properties

    public string Comment
    {
        set => commentTextBox.Text = value;
        get => commentTextBox.Text;
    }

    #endregion
}