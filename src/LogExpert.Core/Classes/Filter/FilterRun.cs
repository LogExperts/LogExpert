namespace LogExpert.Core.Classes.Filter;

/// <summary>How a Filter Run ended.</summary>
public enum FilterRunOutcome
{
    /// <summary>All lines in the run's range were filtered.</summary>
    Completed,

    /// <summary>The run observed cancellation; the lists hold the partial results accumulated so far.</summary>
    Cancelled,

    /// <summary>The filter condition threw; <see cref="FilterRun.Error"/> carries the exception.
    /// The caller narrates — the engine never rethrows.</summary>
    Failed,
}

/// <summary>
/// The result of one Filter Run. <see cref="ResultLines"/> and <see cref="HitLines"/> are sorted
/// ascending without duplicates (the engine contract); <see cref="History"/> is the spread-dedup
/// window state in accumulation order, handed to the tail filter path to continue from.
/// </summary>
public sealed record FilterRun (
    IReadOnlyList<int> ResultLines,
    IReadOnlyList<int> HitLines,
    IReadOnlyList<int> History,
    FilterRunOutcome Outcome,
    Exception Error = null);
