using System;
using System.IO;

namespace LogExpert;

/// <summary>
/// Interface which represents a file in LogExpert. 'File' could be anything that represents text data to be displayed in LogExpert.
/// </summary>
public interface ILogFileInfo
{
    #region Properties

    /// <summary>
    /// The file name (complete path) of the log file. This should be a unique name. E.g. an URI or a path on local disk.
    /// </summary>
    string FullName { get; }

    /// <summary>
    /// The file name without path.
    /// </summary>
    string FileName { get; }

    /// <summary>
    /// The directory of the log file. In most cases this is the FullName minus FileName.
    /// </summary>
    string DirectoryName { get; }

    /// <summary>
    /// Character used to separate directories in a path string.
    /// </summary>
    char DirectorySeparatorChar { get; }

    /// <summary>
    /// The URI of the log file.
    /// </summary>
    Uri Uri { get; }

    /// <summary>
    /// Current length of the file. Return -1 if the file is not found.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Initial file length at the time the ILogFileInfo instance was created. This is used for the buffer shifting when
    /// using the multi file feature. This value should not change after the instance has been created.
    /// </summary>
    long OriginalLength { get; }

    /// <summary>
    /// Whether the file exists.
    /// </summary>
    bool FileExists { get; }

    /// <summary>
    /// The interval (in ms) LogExpert should check for file changes. The property is checked by LogExpert repeatedly in the loop which
    /// checks for file changes. So you can adjust the poll interval as needed. E.g. you can lower the interval when many changes occur and
    /// raise the interval when the file has not been changed for a certain amount of time.
    /// </summary>
    int PollInterval { get; }

    #endregion

    #region Public methods

    /// <summary>
    /// Returns a stream for the log file file. The actual type of stream depends on the implementation.
    /// The caller (LogExpert) is responsible for closing the stream.
    /// </summary>
    /// <remarks>
    /// The returned Stream must support read and seek. Writing is not needed.
    /// </remarks>
    /// <returns>A Stream open for reading</returns>
    Stream OpenStream();

    /// <summary>
    /// Return <code>true</code> if the file has been changed since the last call to this method.
    /// </summary>
    /// <remarks>
    /// LogExpert will poll this method with the interval returned from PollInterval.
    /// </remarks>
    bool FileHasChanged();

    #endregion
}