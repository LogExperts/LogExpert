using ColumnizerLib;

namespace LogExpert.Core.Interfaces;

/// <summary>
/// Capability interface for navigation across physical file boundaries in Multi-File Mode.
/// Query for this interface only when <see cref="ILogfileReader.IsMultiFile"/> is true.
///
/// <para><b>Invariants:</b></para>
/// <list type="bullet">
///   <item>All line numbers are zero-based virtual line numbers spanning the combined file set.</item>
///   <item>"Next/Prev" methods return -1 when no boundary exists in that direction.</item>
///   <item>For single-file readers that also implement this interface, methods return
///         sensible defaults (the only file name, -1 for next/prev).</item>
/// </list>
/// </summary>
public interface IMultiFileNavigation
{
    /// <summary>
    /// Returns the file name (full path) of the physical file containing the given line.
    /// For single-file readers, always returns the primary file name.
    /// </summary>
    string GetLogFileNameForLine(int lineNum);

    /// <summary>
    /// Returns the <see cref="ILogFileInfo"/> for the physical file containing the given line.
    /// Returns null if the line number is out of range.
    /// </summary>
    ILogFileInfo GetLogFileInfoForLine(int lineNum);

    /// <summary>
    /// Returns the first line number of the next physical file after <paramref name="lineNum"/>,
    /// or -1 if <paramref name="lineNum"/> is in the last file.
    /// </summary>
    int GetNextMultiFileLine(int lineNum);

    /// <summary>
    /// Returns the first line number of the previous physical file before <paramref name="lineNum"/>,
    /// or -1 if <paramref name="lineNum"/> is in the first file.
    /// </summary>
    int GetPrevMultiFileLine(int lineNum);

    /// <summary>
    /// Maps a virtual (combined multi-file) line number to the real line number within
    /// its physical file. Used when launching external tools that need file-relative positions.
    /// Returns -1 if the mapping cannot be determined.
    /// </summary>
    int GetRealLineNumForVirtualLineNum(int lineNum);
}
