using LogExpert.UI.Extensions;
using System.Runtime.Versioning;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class ChooseIconDlg : Form
{
    #region Fields

    #endregion

    #region cTor

    public ChooseIconDlg(string fileName)
    {
        InitializeComponent();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        FileName = fileName;
    }

    #endregion

    #region Properties

    public string FileName { get; set; }

    public int IconIndex { get; set; }

    #endregion

    #region Private Methods

    private void FillIconList()
    {
        iconListView.Items.Clear();

        Icon[,] icons = NativeMethods.ExtractIcons(FileName);

        if (icons == null)
        {
            return;
        }

        ImageList imageList = new();

        if (icons.GetLength(0) > 0)
        {
            imageList.ImageSize = icons[1, 0].Size;
            iconListView.LargeImageList = imageList;

            for (var i = 0; i < icons.GetLength(1); ++i)
            {
                imageList.Images.Add(icons[1, i]);
                ListViewItem item = new()
                {
                    ImageIndex = i
                };
                iconListView.Items.Add(item);
            }
        }
    }

    private void DisposeIcons()
    {
        ImageList imageList = iconListView.LargeImageList;
        iconListView.LargeImageList = null;
        foreach (Image image in imageList.Images)
        {
            image.Dispose();
        }
    }

    #endregion

    #region Events handler

    private void ChooseIconDlg_Load(object sender, EventArgs e)
    {
        FillIconList();
        iconFileLabel.Text = FileName;
    }

    private void OnButtonChooseIconFileClick(object sender, EventArgs e)
    {
        OpenFileDialog dlg = new()
        {
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (string.IsNullOrEmpty(iconFileLabel.Text) == false)
        {
            FileInfo info = new(iconFileLabel.Text);
            if (info.Directory != null && info.Directory.Exists)
            {
                dlg.InitialDirectory = info.DirectoryName;
            }
        }

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            iconFileLabel.Text = dlg.FileName;
            FileName = dlg.FileName;
            FillIconList();
        }
    }

    private void OnOkButtonClick(object sender, EventArgs e)
    {
        IconIndex = iconListView.SelectedIndices.Count > 0 ? iconListView.SelectedIndices[0] : -1;

        DisposeIcons();
    }

    #endregion
}