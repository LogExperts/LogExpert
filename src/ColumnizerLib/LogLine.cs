namespace ColumnizerLib;

/// <summary>
/// Represents a single log line, including its full text and line number.
/// </summary>
/// <remarks>
/// <para> <b> Purpose:</b> <br/> The <c> LogLine</c> struct encapsulates the content and line number of a log entry. It
/// is used throughout the columnizer and log processing infrastructure to provide a strongly-typed, immutable
/// representation of a log line. </para>
/// <para> <b> Usage:</b> <br/> This struct implements the
/// <see cref="ILogLineMemory"/> interface, allowing it to be used wherever an <c> ILogLineMemory</c> is expected. It
/// provides value semantics and is intended to be lightweight and efficiently passed by value. </para> <para> <b>
/// Relationship to ILogLineMemory:</b> <br/> <c> LogLine</c> is a concrete, immutable implementation of the
/// <see cref="ILogLineMemory"/> interface, providing properties for the full line text and its line number. </para>
/// This is a readonly record struct implementing
/// <see cref="ILogLineMemory"/>. Stored inline in <c> List&lt;LogLine&gt;</c> to avoid boxing and heap allocation.
/// Boxing occurs only when returned through the <c> ILogLineMemory</c> interface boundary.
/// </remarks>
public readonly record struct LogLine : ILogLineMemory
{
    public int LineNumber { get; }

    public ReadOnlyMemory<char> FullLine { get; }

    public ReadOnlyMemory<char> Text { get; }

    public LogLine (string fullLine, int lineNumber)
    {
        LineNumber = lineNumber;
        FullLine = fullLine.AsMemory();
        Text = fullLine.AsMemory();
    }

    public LogLine (ReadOnlyMemory<char> fullLine, int lineNumber)
    {
        LineNumber = lineNumber;
        FullLine = fullLine;
        Text = fullLine;
    }
}
