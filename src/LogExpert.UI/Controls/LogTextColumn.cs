using System.Runtime.Versioning;

namespace LogExpert.UI.Controls;

internal class LogTextColumn : DataGridViewColumn
{
    #region cTor

    [SupportedOSPlatform("windows")]
    public LogTextColumn () : base(new LogGridCell())
    {
    }

    #endregion
}
