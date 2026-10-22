using LogExpert.Core.Callback;

namespace LogExpert.Core.Classes.Filter;

/// <summary>
/// A Filter Engine executes one full Filter Run over a log file and returns a <see cref="FilterRun"/>.
/// Two engines implement the seam — <see cref="SerialFilterEngine"/> and <see cref="ParallelFilterEngine"/>,
/// selected by the MultiThreadFilter preference — and are held to an identical contract:
/// <list type="bullet">
/// <item><see cref="FilterRun.ResultLines"/> and <see cref="FilterRun.HitLines"/> are sorted ascending with no duplicates.</item>
/// <item><see cref="FilterParams"/> is snapshotted at entry — mutating the caller's instance mid-run has no effect.</item>
/// <item>The run covers <c>[0, LineCount-at-entry)</c>; lines appended during the run belong to the tail path.</item>
/// <item>The engine never throws for filter errors and never touches UI: failures, like cancellation,
/// are reported as the run's <see cref="FilterRun.Outcome"/>.</item>
/// </list>
/// The dual-engine equivalence test table pins serial and parallel to byte-identical output.
/// </summary>
public interface IFilterEngine
{
    /// <summary>
    /// Runs the filter over all lines available at entry. Progress is a cumulative scanned-line
    /// count through the sink; cancellation yields <see cref="FilterRunOutcome.Cancelled"/> with the
    /// partial lists accumulated so far.
    /// </summary>
    FilterRun Run (FilterParams filterParams, ColumnizerCallback callback, CancellationToken cancellationToken, IProgress<int> progress = null);
}
