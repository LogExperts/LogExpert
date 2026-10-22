using ColumnizerLib;

namespace LogExpert.Core.Interfaces;

/// <summary>
/// Read access to the lines of a loaded log file: how many lines there are, the content of a
/// line, and which physical file a line belongs to (relevant in multi-file mode).
/// </summary>
/// <remarks>
/// This is the role of a Log Window that columnizer callbacks
/// (<see cref="Callback.ColumnizerCallback"/>, <see cref="Callback.ColumnizerCallbackMemory"/>)
/// and the paint context consume. A fake needs only these three members.
/// </remarks>
public interface ILogLineSource
{
    /// <summary>
    /// Gets the current number of lines in the loaded log file (all files combined in
    /// multi-file mode).
    /// </summary>
    int LineCount { get; }

    /// <summary>
    /// Gets the file name of the log file that contains the specified line number.
    /// </summary>
    /// <param name="lineNum">The zero-based line number.</param>
    /// <returns>The file name (without path) of the log file containing the specified line.
    /// In multi-file mode, this may return different file names for different line numbers.</returns>
    string GetCurrentFileName (int lineNum);

    /// <summary>
    /// Retrieves the memory representation of a log line at the specified line number.
    /// </summary>
    /// <param name="lineNum">The zero-based index of the log line to retrieve.</param>
    /// <returns>An object that provides access to the memory of the specified log line, or
    /// <c>null</c> when the line number is out of range.</returns>
    ILogLineMemory GetLineMemory (int lineNum);
}
