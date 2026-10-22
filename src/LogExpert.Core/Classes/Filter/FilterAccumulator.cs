namespace LogExpert.Core.Classes.Filter;

/// <summary>
/// The single home of the per-hit filter accumulation recipe: hit → <see cref="FilterSpread.Expand"/> →
/// append to results/history → <see cref="FilterSpread.TrimHistory"/>. Used internally by the serial
/// Filter Engine and per line by the Log Window's tail filter path, so the recipe exists exactly once.
/// Not thread-safe — callers that share an instance with concurrent readers (the tail path's GUI)
/// synchronize around it.
/// </summary>
public class FilterAccumulator
{
    public FilterAccumulator () : this([], [], [])
    {
    }

    /// <summary>
    /// Adopts existing lists as the accumulator's state — the tail filter path wraps the Log
    /// Window's canonical result/hit/history lists (and Filter Pipes their per-pipe history) so a
    /// full run's output, including its spread history, is continued in place rather than copied.
    /// </summary>
    public FilterAccumulator (IList<int> resultLines, IList<int> hitLines, IList<int> history)
    {
        ArgumentNullException.ThrowIfNull(resultLines);
        ArgumentNullException.ThrowIfNull(hitLines);
        ArgumentNullException.ThrowIfNull(history);

        ResultLines = resultLines;
        HitLines = hitLines;
        History = history;
    }

    /// <summary>Filter result lines: every hit plus its spread context, in accumulation order.</summary>
    public IList<int> ResultLines { get; }

    /// <summary>The hit lines only, without spread.</summary>
    public IList<int> HitLines { get; }

    /// <summary>The trailing dedup window (spread history) recent expansions were checked against.</summary>
    public IList<int> History { get; }

    /// <summary>
    /// Records a filter hit: adds it to <see cref="HitLines"/> and appends its spread expansion
    /// (deduplicated against <see cref="History"/>, clamped to the file's line range) to
    /// <see cref="ResultLines"/>. Returns the expansion so callers that emit it (Filter Pipes)
    /// need not diff the lists.
    /// </summary>
    public IList<int> AddHit (int lineNum, int spreadBefore, int spreadBehind, int lineCount)
    {
        HitLines.Add(lineNum);
        var expanded = FilterSpread.Expand(lineNum, spreadBefore, spreadBehind, lineCount, History);
        foreach (var line in expanded)
        {
            ResultLines.Add(line);
            History.Add(line);
        }

        FilterSpread.TrimHistory(History);
        return expanded;
    }
}
