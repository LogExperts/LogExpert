using System;

namespace LogExpert;

/// <summary>
/// This helper struct holds a log line and its line number (zero based).
/// This struct is used by <see cref="ILogExpertCallback"/>.
/// </summary>
/// <seealso cref="ILogExpertCallback.AddPipedTab"/>
public struct LineEntry : IEquatable<LineEntry>
{
    /// <summary>
    /// The content of the line.
    /// </summary>
    public ILogLine LogLine { get; set; }

    /// <summary>
    /// The line number. See <see cref="ILogExpertCallback.AddPipedTab"/> for an explanation of the line number.
    /// </summary>
    public int LineNum { get; set; }

    public override bool Equals(object obj)
    {
        return obj is LineEntry other && Equals(other);
    }

    public readonly bool Equals(LineEntry other)
    {
        return LineNum == other.LineNum && Equals(LogLine, other.LogLine);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(LineNum, LogLine);
    }

    public static bool operator == (LineEntry left, LineEntry right) => left.Equals(right);
    public static bool operator != (LineEntry left, LineEntry right) => !left.Equals(right);
}