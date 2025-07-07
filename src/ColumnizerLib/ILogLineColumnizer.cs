using System;

namespace LogExpert;

///<summary>
/// This interface defines a so-called 'Columnizer' for LogExpert.
/// A columnizer splits a single text line into well defined columns. These columns
/// are used in the data grid view of LogExpert.
/// <br></br><br></br> 
/// Optionally a columnizer can parse the log line to determine the date/time of
/// the log line (assuming that all log lines have a timestamp). This is needed for
/// some of the features of LogExpert (see user documentation for more information).
/// <br></br><br></br>
/// You can implement your own columnizers for your logfile format, if needed.
///</summary>
public interface ILogLineColumnizer
{
    #region Public methods

    /// <summary>
    /// Returns the name for the columnizer. This name is used for the columnizer selection dialog.
    /// </summary>
    string GetName();

    /// <summary>
    /// Returns the description of the columnizer. This text is used in the columnizer selection dialog.
    /// </summary>
    string GetDescription();

    /// <summary>
    /// Returns the number of columns the columnizer will split lines into. 
    /// </summary>
    /// <remarks>
    /// This value does not include the column for displaying the line number. The line number column 
    /// is added by LogExpert and is not handled by columnizers.
    /// </remarks>
    int GetColumnCount();

    /// <summary>
    /// Returns the names of the columns. The returned names are used by LogExpert for the column headers in the data grid view.
    /// The names are expected in order from left to right. 
    /// </summary>
    string[] GetColumnNames();

    /// <summary>
    /// Given a single line of the logfile this function splits the line content into columns. The function returns 
    /// a string array containing the splitted content.
    /// </summary>
    /// <remarks>
    /// This function is called by LogExpert for every line that has to be drawn in the grid view. The faster your code
    /// handles the splitting, the faster LogExpert can draw the grid view content.<br></br>
    /// <br></br>
    /// Notes about timeshift handling:<br></br>
    /// If your columnizer implementation supports timeshift (see <see cref="IsTimeshiftImplemented">IsTimeshiftImplemented</see>) 
    /// you have to add the timestamp offset to the columns representing the timestamp (e.g. columns like 'date' and 'time').
    /// In practice this means you have to parse the date/time value of your log line (see <see cref="GetTimestamp">GetTimestamp</see>) 
    /// add the offset and convert the timestamp back to string value(s).
    /// </remarks>
    /// <param name="callback">Callback interface with functions which can be used by the columnizer</param>
    /// <param name="line">The line content to be splitted</param>
    IColumnizedLogLine SplitLine(ILogLineColumnizerCallback callback, ILogLine line);

    /// <summary>
    /// Returns true, if the columnizer supports timeshift handling.
    /// </summary>
    /// <remarks>
    /// If you return true, you also have to implement the function SetTimeOffset(), GetTimeOffset() and GetTimestamp().
    /// You also must handle PushValue() for the column(s) that displays the timestamp.
    /// </remarks>
    bool IsTimeshiftImplemented();

    /// <summary>
    /// Sets an offset to be used for displaying timestamp values. You have to implement this function, if 
    /// your IsTimeshiftImplemented() function return true.
    /// </summary>
    /// <remarks>
    /// You have to store the given value in the Columnizer instance and add this offset to the timestamp column(s) returned by SplitLine() 
    /// (e.g. in the date and time columns).
    /// </remarks>
    /// <param name="msecOffset">The timestamp offset in milliseconds.</param>
    void SetTimeOffset(int msecOffset);

    /// <summary>
    /// Returns the current stored timestamp offset (set by SetTimeOffset()).
    /// </summary>
    int GetTimeOffset();

    /// <summary>
    /// Returns the timestamp value of the given line as a .NET DateTime object. If there's no valid timestamp in the
    /// given line you have to return DateTime.MinValue.
    /// </summary>
    /// <remarks>
    /// When implementing this function you have to parse the given log line for a valid date/time to get a DateTime object.
    /// Before returning the DateTime object you have to add the offset which was set by SetTimeOffset().<br></br>
    /// <br></br>
    /// Note: If not all lines of your log files contain a valid date/time it's recommended to do some fail-fast pre checks before
    /// calling the parse functions of DateTime. This saves a lot of time because DateTime.ParseExact() is very slow when fed with
    /// invalid input.
    /// </remarks>
    /// <param name="callback">Callback interface with functions which can be used by the columnizer</param>
    /// <param name="line">The line content which timestamp has to be returned.</param>
    DateTime GetTimestamp(ILogLineColumnizerCallback callback, ILogLine line);

    /// <summary>
    /// This function is called if the user changes a value in a column (edit mode in the log view).
    /// The purpose of the function is to determine a new timestamp offset. So you have to handle the
    /// call only if the given column displays a timestamp.
    /// </summary>
    /// <remarks>
    /// You should parse both values (oldValue, value) for valid timestamps, determine the time offset and store the offset as
    /// the new timeshift offset (and of course use this offset in the GetTimestamp() and SplitLine() functions).
    /// </remarks>
    /// <param name="callback">Callback interface with functions which can be used by the columnizer</param>
    /// <param name="column">The column number which value has changed.</param>
    /// <param name="value">The new value.</param>
    /// <param name="oldValue">The old value.</param>
    void PushValue(ILogLineColumnizerCallback callback, int column, string value, string oldValue);

    #endregion
}