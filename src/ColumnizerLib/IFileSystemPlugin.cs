namespace LogExpert;

/// <summary>
/// Interface for file system plugins. A file system plugin is responsible for feeding file data to LogExpert.
/// </summary>
/// <remarks>
/// LogExperts file handling is done via file system plugins. The selection if the appropriate plugin for a file is based
/// on URI schemes. If a file system plugin returns <code>true</code> to the <see cref="CanHandleUri"/> method, it will be selected
/// to handle a file.
/// </remarks>
public interface IFileSystemPlugin
{
    #region Properties

    /// <summary>
    /// Name of the plugin. Will be used in the Settings dialog.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Short description. Currently unused, but maybe used later for displaying a short info about the plugin.
    /// </summary>
    string Description { get; }

    #endregion

    #region Public methods

    /// <summary>
    /// Called from LogExpert to determine a file system plugin for a given URI.
    /// </summary>
    /// <param name="uriString">The URI of the file to be loaded.</param>
    /// <returns>Return <code>true</code> if the file system plugin can handle the URI.</returns>
    bool CanHandleUri(string uriString);

    /// <summary>
    /// Return a file system specific implementation of <see cref="ILogFileInfo"/> here.
    /// The method is called from LogExpert when a file is about to be opened. It's called after <see cref="CanHandleUri"/> was called.
    /// </summary>
    /// <param name="uriString"></param>
    /// <returns></returns>
    ILogFileInfo GetLogfileInfo(string uriString);

    #endregion
}