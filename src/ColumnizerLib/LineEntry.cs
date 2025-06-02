namespace LogExpert;

/// <summary>
/// This helper struct holds a log line and its line number (zero based).
/// This struct is used by <see cref="ILogExpertCallback"/>.
/// </summary>
/// <seealso cref="ILogExpertCallback.AddPipedTab"/>
public struct LineEntry
{
    /// <summary>
    /// The content of the line.
    /// </summary>
    public ILogLine LogLine { get; set; }

    /// <summary>
    /// The line number. See <see cref="ILogExpertCallback.AddPipedTab"/> for an explanation of the line number.
    /// </summary>
    public int LineNum { get; set; }

}