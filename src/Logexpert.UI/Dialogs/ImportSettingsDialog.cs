using LogExpert.Core.Config;

using System.Runtime.Versioning;

namespace LogExpert.UI.Dialogs
{
    [SupportedOSPlatform("windows")]
    public partial class ImportSettingsDialog : Form
    {
        #region cTor

        public ImportSettingsDialog(ExportImportFlags importFlags)
        {
            InitializeComponent();
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            ImportFlags = importFlags;
            FileName = string.Empty;

            if (ImportFlags == ExportImportFlags.HighlightSettings)
            {
                checkBoxHighlightSettings.Checked = true;
                checkBoxHighlightSettings.Enabled = false;
                checkBoxHighlightFileMasks.Checked = false;
                checkBoxHighlightFileMasks.Enabled = false;
                checkBoxColumnizerFileMasks.Checked = false;
                checkBoxColumnizerFileMasks.Enabled = false;
                checkBoxExternalTools.Checked = false;
                checkBoxExternalTools.Enabled = false;
                checkBoxOther.Checked = false;
                checkBoxOther.Enabled = false;
            }

            ResumeLayout();
        }

        #endregion

        #region Properties

        public string FileName { get; private set; }

        public ExportImportFlags ImportFlags { get; private set; }

        #endregion

        #region Events handler

        private void OnImportSettingsDialogLoad(object sender, EventArgs e)
        {
        }

        private void OnFileButtonClick(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Title = "Load Settings from file",
                DefaultExt = "json",
                AddExtension = false,
                Filter = "Settings (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                textBoxFileName.Text = dlg.FileName;
            }
        }

        private void OnOkButtonClick(object sender, EventArgs e)
        {
            FileName = textBoxFileName.Text;

            if (ImportFlags != ExportImportFlags.HighlightSettings)
            {
                foreach (Control ctl in groupBoxImportOptions.Controls)
                {
                    if (ctl.Tag != null)
                    {
                        if (((CheckBox)ctl).Checked)
                        {
                            ImportFlags |= (ExportImportFlags)long.Parse(ctl.Tag as string ?? string.Empty);
                        }
                    }
                }
            }
            else
            {
                if (checkBoxKeepExistingSettings.Checked)
                {
                    ImportFlags |= ExportImportFlags.KeepExisting;
                }
            }
        }

        #endregion
    }
}