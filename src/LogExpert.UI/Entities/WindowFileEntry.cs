using System.Runtime.Versioning;

using LogExpert.UI.Controls.LogWindow;

namespace LogExpert.UI.Entities;

/// <summary>
/// Represents a log file and its window. Used as a kind of handle for menus or list of open files.
/// </summary>
internal class WindowFileEntry (LogWindow logWindow)
{
    #region Fields

    private const int MAX_LEN = 40;

    #endregion

    #region Properties

    [SupportedOSPlatform("windows")]
    public string Title
    {
        get
        {
            var title = LogWindow.Text;

            if (title.Length > MAX_LEN)
            {
                title = string.Concat("...", title.AsSpan(title.Length - MAX_LEN));
            }

            return title;
        }
    }

    [SupportedOSPlatform("windows")]
    public string FileName => LogWindow.FileName;

    public LogWindow LogWindow { get; } = logWindow;

    #endregion
}