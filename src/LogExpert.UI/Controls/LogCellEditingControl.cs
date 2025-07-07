using System.Runtime.Versioning;

namespace LogExpert.UI.Controls;

[SupportedOSPlatform("windows")]
internal class LogCellEditingControl : DataGridViewTextBoxEditingControl
{
    #region Public methods

    public override bool EditingControlWantsInputKey(Keys key, bool dataGridViewWantsInputKey)
    {
        switch (key & Keys.KeyCode)
        {
            case Keys.Left:
            case Keys.Up:
            case Keys.Down:
            case Keys.Right:
            case Keys.Home:
            case Keys.End:
            case Keys.PageDown:
            case Keys.PageUp:
                {
                    return true;
                }
        }

        return !dataGridViewWantsInputKey;
    }

    #endregion
}
