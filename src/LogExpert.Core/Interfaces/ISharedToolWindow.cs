using System.Drawing;

using ColumnizerLib;

using LogExpert.Core.Config;

namespace LogExpert.Core.Interfaces;

/// <summary>
/// Interface to be implemented by tools windows that are shared across multiple log files.
/// The implementor will be called whenever the current log file changes. So it can draw new content
/// according to the current active log file.
/// </summary>
public interface ISharedToolWindow
{
    #region Public methods

    /// <summary>
    /// Called when a file becomes the active file (e.g. when user selects a tab).
    /// </summary>
    /// <param name="ctx"></param>
    void SetCurrentFile (IFileViewContext ctx);

    /// <summary>
    /// Called whenever the current file has been changed.
    /// </summary>
    void FileChanged ();

    void SetColumnizer (ILogLineMemoryColumnizer columnizer);

    void PreferencesChanged (Font font, bool setLastColumnWidth, int lastColumnWidth, SettingsFlags flags);

    #endregion
}