namespace LogExpert;

///<summary>
///This is a callback interface. Some of the ILogLineColumnizer functions
///are called with this interface as an argument. You don't have to implement this interface. It's implemented
///by LogExpert. You can use it in your own columnizers, if you need it.
///</summary>
///<remarks>
///Implementors of ILogLineColumnizer can use the provided functions to get some more informations
///about the log file. In the most cases you don't need this interface. It's provided here for special cases.<br></br>
///<br></br>
///An example would be when the log lines contains only the time of day but the date is coded in the file name. In this situation
///you can use the GetFileName() function to retrieve the name of the current file to build a complete timestamp.
///</remarks>
public interface ILogLineColumnizerCallback
{
    #region Public methods

    /// <summary>
    /// This property returns the current line number. That is the line number of the log line
    /// a ILogLineColumnizer function is called for (e.g. the line that has to be painted).
    /// </summary>
    /// <returns>The current line number starting at 0</returns>
    int LineNum { get; }

    /// <summary>
    /// Returns the full file name (path + name) of the current log file.
    /// </summary>
    /// <returns>File name of current log file</returns>
    string GetFileName ();

    /// <summary>
    /// Returns the log line with the given index (zero-based).
    /// </summary>
    /// <param name="lineNum">Number of the line to be retrieved</param>
    /// <returns>A string with line content or null if line number is out of range</returns>
    ILogLine GetLogLine (int lineNum);

    /// <summary>
    /// Returns the number of lines of the logfile.
    /// </summary>
    /// <returns>Number of lines.</returns>
    int GetLineCount ();

    #endregion
}