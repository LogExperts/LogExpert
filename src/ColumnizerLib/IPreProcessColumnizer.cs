using System;
using System.Collections.Generic;
using System.Text;

namespace LogExpert;

/// <summary>
/// <para>
/// Implement this interface in your columnizer if you want to pre-process every line
/// directly when it's loaded from file system.</para>
/// <para>
/// You can also use this to drop lines.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// By implementing this interface with your Columnizer you get the ability to modify the
/// content of a log file right before it will be seen by LogExpert.
/// </para>
/// <para>
/// Note that the <see cref="PreProcessLine"/>
/// method is only used when loading a line from disk. Because of internal buffering a log line may
/// be read only once or multiple times. You have to ensure that the behaviour is consistent 
/// for every call to <see cref="PreProcessLine"/> for a specific line. That's especially true
/// when dropping lines. Dropping a line changes the line count seen by LogExpert. That has implications
/// for things like bookmarks etc.
/// </para>
/// </remarks>
public interface IPreProcessColumnizer
{
    #region Public methods

    /// <summary>
    /// This function is called for every line read from the log file. You can change the content
    /// by returning a different string. You can also drop the complete line by returning null.
    /// </summary>
    /// <param name="logLine">Line content</param>
    /// <param name="lineNum">Line number as seen by LogExpert</param>
    /// <param name="realLineNum">Actual line number in the file</param>
    /// <returns>The changed content or null to drop the line</returns>
    /// <remarks>
    /// <para>
    /// The values of lineNum and realLineNum differ only if you drop lines (by returning null).
    /// When you drop a line, this line is hidden completely from LogExpert's log buffers. No chance
    /// to bring it back later. When you drop a line, the lineNum will stay the same for the next
    /// method call (for the next line). But realLineNum will increase for every call.
    /// </para>
    /// <para>
    /// A usage example is the CsvColumnizer: If configuration says that there are field names
    /// in the first line if the CSV file, the CsvColumnizer will store the first line for later
    /// field name retrieval. But then the line is dropped. So the line isn't seen by LogExpert.
    /// Detecting the first line in the file is only possible by checking the realLineNum parameter.
    /// </para>
    /// <para>
    /// Remember that the <see cref="PreProcessLine"/> method is called in an early state 
    /// when loading the file. So the file isn't loaded completely and the internal state 
    /// of LogExpert isn't complete. You cannot make any assumptions about file size or other
    /// things. The given parameters are the only 'stateful' informations you can rely on.
    /// </para>
    /// </remarks>
    string PreProcessLine(string logLine, int lineNum, int realLineNum);

    #endregion
}