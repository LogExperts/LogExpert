namespace ColumnizerLib;

/// <summary>
/// Defines methods for splitting log lines into columns and extracting column values from in-memory log data using a
/// callback-based approach.
/// </summary>
/// <remarks>Implementations of this interface enable advanced log line parsing and columnization scenarios,
/// allowing consumers to process log data efficiently in memory. The interface is designed for use with log sources
/// that provide direct memory access to log lines, supporting custom column extraction and value notification
/// workflows. Thread safety and performance characteristics depend on the specific implementation.</remarks>
public interface ILogLineMemoryColumnizer
{
    #region Public methods

    /// <summary>
    /// Returns the name for the columnizer. This name is used for the columnizer selection dialog.
    /// </summary>
    string GetName ();

    /// <summary>
    /// Returns the name that is given by the user for this columnizer.
    /// </summary>
    string GetCustomName ();

    /// <summary>
    /// Returns the description of the columnizer. This text is used in the columnizer selection dialog.
    /// </summary>
    string GetDescription ();

    /// <summary>
    /// Returns the number of columns the columnizer will split lines into.
    /// </summary>
    /// <remarks>
    /// This value does not include the column for displaying the line number. The line number column
    /// is added by LogExpert and is not handled by columnizers.
    /// </remarks>
    int GetColumnCount ();

    /// <summary>
    /// Returns the names of the columns. The returned names are used by LogExpert for the column headers in the data grid view.
    /// The names are expected in order from left to right.
    /// </summary>
    string[] GetColumnNames ();

    /// <summary>
    /// Returns true, if the columnizer supports timeshift handling.
    /// </summary>
    /// <remarks>
    /// If you return true, you also have to implement the function SetTimeOffset(), GetTimeOffset() and GetTimestamp().
    /// You also must handle PushValue() for the column(s) that displays the timestamp.
    /// </remarks>
    bool IsTimeshiftImplemented ();

    /// <summary>
    /// Sets an offset to be used for displaying timestamp values. You have to implement this function, if
    /// your IsTimeshiftImplemented() function return true.
    /// </summary>
    /// <remarks>
    /// You have to store the given value in the Columnizer instance and add this offset to the timestamp column(s) returned by SplitLine()
    /// (e.g. in the date and time columns).
    /// </remarks>
    /// <param name="msecOffset">The timestamp offset in milliseconds.</param>
    void SetTimeOffset (int msecOffset);

    /// <summary>
    /// Returns the current stored timestamp offset (set by SetTimeOffset()).
    /// </summary>
    int GetTimeOffset ();

    /// <summary>
    /// Splits a log line into columns using the specified callback for columnization.
    /// </summary>
    /// <param name="callback">The callback used to determine how the log line is split into columns. Cannot be null.</param>
    /// <param name="logLine">The log line to be split into columns. Cannot be null.</param>
    /// <returns>An object representing the columnized log line. The returned object contains the extracted columns from the
    /// input line.</returns>
    IColumnizedLogLineMemory SplitLine (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine);

    /// <summary>
    /// Retrieves the timestamp associated with the specified log line.
    /// </summary>
    /// <param name="callback">An object that provides access to columnizer services for the log line. Used to obtain additional context or
    /// data required for timestamp extraction.</param>
    /// <param name="logLine">The log line from which to extract the timestamp.</param>
    /// <returns>A DateTime value representing the timestamp of the specified log line.</returns>
    DateTime GetTimestamp (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine);

    /// <summary>
    /// Notifies the callback of a new value for the specified column, providing both the current and previous values.
    /// </summary>
    /// <param name="callback">The callback interface that receives the value update notification. Cannot be null.</param>
    /// <param name="column">The zero-based index of the column for which the value is being updated.</param>
    /// <param name="value">The new value to be associated with the specified column.</param>
    /// <param name="oldValue">The previous value that was associated with the specified column before the update.</param>
    void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, string oldValue);

    // <summary>
    /// <param name="callback">
    /// The callback interface that receives the value update notification. Cannot be null.
    /// </param>
    /// <param name="column">The zero-based index of the column for which the value is being updated.</param>
    /// <param name="value">The new value to be associated with the specified column.</param>
    /// <param name="oldValue">
    /// The previous value <see cref="ReadOnlyMemory{T}"/> associated with the specified
    /// column before the update.
    /// </param>
    void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, ReadOnlyMemory<char> oldValue);

    #endregion
}