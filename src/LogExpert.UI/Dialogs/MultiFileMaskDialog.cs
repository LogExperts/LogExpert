using System.ComponentModel;
using System.Runtime.Versioning;

using LogExpert.Core.Entities;

namespace LogExpert.UI.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class MultiFileMaskDialog : Form
{
    #region Fields

    #endregion

    #region cTor

    public MultiFileMaskDialog (string fileName)
    {
        SuspendLayout();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeComponent();

        ApplyResources();

        labelFileName.Text = fileName;

        ResumeLayout();
    }

    private void ApplyResources ()
    {
        Text = Resources.MultiFileMaskDialog_UI_Title;
        labelMultiSettingsFor.Text = Resources.MultiFileMaskDialog_UI_Label_SettingsFor;
        labelFileNamePattern.Text = Resources.MultiFileMaskDialog_UI_Label_FileNamePattern;
        labelMaxDayGap.Text = Resources.MultiFileMaskDialog_UI_Label_MaxDayGap;
        labelMaxFileAgeDays.Text = Resources.MultiFileMaskDialog_UI_Label_MaxFileAgeDays;
        labelMaxFileAgeDaysHint.Text = Resources.MultiFileMaskDialog_UI_Label_MaxFileAgeDaysHint;
        syntaxHelpLabel.Text = Resources.MultiFileMaskDialog_UI_Label_SyntaxHelp;
        buttonOk.Text = Resources.LogExpert_Common_UI_Button_OK;
        buttonCancel.Text = Resources.LogExpert_Common_UI_Button_Cancel;
    }

    #endregion

    #region Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string FileNamePattern { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int MaxDayGap { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int MaxFileAgeDays { get; set; }

    #endregion

    #region Events handler

    private void OnButtonOKClick (object sender, EventArgs e)
    {
        FileNamePattern = fileNamePatternTextBox.Text;
        MaxDayGap = (int)upDownMaxDayGap.Value;
        MaxFileAgeDays = (int)upDownMaxFileAgeDays.Value;
    }

    private void OnMultiFileMaskDialogLoad (object sender, EventArgs e)
    {
        fileNamePatternTextBox.Text = FileNamePattern;
        upDownMaxDayGap.Value = MaxDayGap;
        upDownMaxFileAgeDays.Value = MultiFileOptions.ClampMaxFileAgeDays(MaxFileAgeDays);
    }

    #endregion
}