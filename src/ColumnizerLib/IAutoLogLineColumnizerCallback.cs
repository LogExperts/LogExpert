namespace LogExpert;

public interface IAutoLogLineColumnizerCallback
{
    /// <summary>
    /// Returns the log line with the given index (zero-based).
    /// </summary>
    /// <param name="lineNum">Number of the line to be retrieved</param>
    /// <returns>A string with line content or null if line number is out of range</returns>
    ILogLine GetLogLine (int lineNum);
}