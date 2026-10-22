using LogExpert.Core.Callback;

using NLog;

namespace LogExpert.Core.Classes.Filter;

/// <summary>
/// The single-threaded Filter Engine: one sequential pass over the file, accumulating through
/// <see cref="FilterAccumulator"/>. Ported from the Log Window's private filter loop; behaviour
/// changes versus that loop are the engine contract (params + line-count snapshot at entry,
/// Failed outcome instead of a MessageBox) — see <see cref="IFilterEngine"/>.
/// </summary>
public class SerialFilterEngine : IFilterEngine
{
    private const int PROGRESS_REPORT_MODULO = 1000;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public FilterRun Run (FilterParams filterParams, ColumnizerCallback callback, CancellationToken cancellationToken, IProgress<int> progress = null)
    {
        ArgumentNullException.ThrowIfNull(filterParams);
        ArgumentNullException.ThrowIfNull(callback);

        // Snapshot: the caller may mutate its instance (filter panel edits) while the run executes.
        var snapshot = filterParams.CloneWithCurrentColumnizer();
        snapshot.Reset();

        // Snapshot: lines appended during the run belong to the tail path.
        var lineCount = callback.GetLineCount();

        var accumulator = new FilterAccumulator();
        var outcome = FilterRunOutcome.Completed;

        try
        {
            for (var lineNum = 0; lineNum < lineCount; lineNum++)
            {
                var line = callback.GetLogLineMemory(lineNum);
                if (line == null)
                {
                    break;
                }

                callback.SetLineNum(lineNum);
                if (Util.TestFilterCondition(snapshot, line, callback))
                {
                    accumulator.AddHit(lineNum, snapshot.SpreadBefore, snapshot.SpreadBehind, lineCount);
                }

                // Checked after the line is processed, matching the original loop: a cancel
                // requested mid-line still keeps that line's hit in the partial result.
                if (cancellationToken.IsCancellationRequested)
                {
                    outcome = FilterRunOutcome.Cancelled;
                    break;
                }

                if ((lineNum + 1) % PROGRESS_REPORT_MODULO == 0)
                {
                    progress?.Report(lineNum + 1);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while filtering (serial engine)");
            return Snapshot(accumulator, FilterRunOutcome.Failed, ex);
        }

        return Snapshot(accumulator, outcome);
    }

    /// <summary>
    /// Materializes the run result under the engine contract: results and hits sorted ascending,
    /// duplicates removed; history kept in accumulation order (it is dedup-window state, not output).
    /// </summary>
    private static FilterRun Snapshot (FilterAccumulator accumulator, FilterRunOutcome outcome, Exception error = null)
    {
        return new FilterRun(
            Normalize(accumulator.ResultLines),
            Normalize(accumulator.HitLines),
            [.. accumulator.History],
            outcome,
            error);
    }

    private static List<int> Normalize (IList<int> lines)
    {
        var normalized = new List<int>(new SortedSet<int>(lines));
        return normalized;
    }
}
