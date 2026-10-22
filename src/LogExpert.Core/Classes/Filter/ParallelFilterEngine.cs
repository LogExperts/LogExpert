using LogExpert.Core.Callback;

using NLog;

namespace LogExpert.Core.Classes.Filter;

/// <summary>
/// The multi-threaded Filter Engine: wraps <see cref="FilterStarter"/>'s chunk-and-merge machinery
/// (the file is split into <c>ProcessorCount + 2</c> intervals, each filtered on its own worker with a
/// cloned <see cref="FilterParams"/>, results merged sorted and deduplicated). Worker faults surface
/// as <see cref="FilterRunOutcome.Failed"/> — never as a rethrow — and cancellation is forwarded to
/// the workers through the token. See <see cref="IFilterEngine"/> for the shared contract.
/// </summary>
public class ParallelFilterEngine : IFilterEngine
{
    /// <summary>Chunk count beyond the processor count — today's effective FilterStarter value.</summary>
    private const int CHUNK_FACTOR = 2;

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly int _chunkCount;

    public ParallelFilterEngine () : this(Environment.ProcessorCount + CHUNK_FACTOR)
    {
    }

    /// <summary>Fixed chunk count — used by tests to make chunk boundaries deterministic.</summary>
    public ParallelFilterEngine (int chunkCount)
    {
        _chunkCount = chunkCount;
    }

    public FilterRun Run (FilterParams filterParams, ColumnizerCallback callback, CancellationToken cancellationToken, IProgress<int> progress = null)
    {
        ArgumentNullException.ThrowIfNull(filterParams);
        ArgumentNullException.ThrowIfNull(callback);

        // Snapshot: the caller may mutate its instance while the run executes (workers clone
        // again per thread, but their source must already be private to this run).
        var snapshot = filterParams.CloneWithCurrentColumnizer();
        snapshot.Reset();

        // Snapshot: lines appended during the run belong to the tail path.
        var lineCount = callback.GetLineCount();

        FilterStarter filterStarter = new(callback, _chunkCount);

        try
        {
            filterStarter
                .DoFilter(snapshot, 0, lineCount, count => progress?.Report(count), cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while filtering (parallel engine)");
            return Snapshot(filterStarter, FilterRunOutcome.Failed, ex);
        }

        var outcome = cancellationToken.IsCancellationRequested
            ? FilterRunOutcome.Cancelled
            : FilterRunOutcome.Completed;

        return Snapshot(filterStarter, outcome);
    }

    /// <summary>
    /// Materializes the run result. FilterStarter's merge already yields sorted-ascending,
    /// deduplicated lists, satisfying the engine contract.
    /// </summary>
    private static FilterRun Snapshot (FilterStarter filterStarter, FilterRunOutcome outcome, Exception error = null)
    {
        return new FilterRun(
            [.. filterStarter.FilterResultLines],
            [.. filterStarter.FilterHitList],
            [.. filterStarter.LastFilterLinesList],
            outcome,
            error);
    }
}
