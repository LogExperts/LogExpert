using LogExpert.Core.Entities;

namespace LogExpert.Core.Classes.Tail;

/// <summary>
/// The consumer side of the <see cref="TailFollowEngine"/> seam: the engine sequences tail events
/// on its worker thread and narrates them through this sink. Implemented by the Log Window, which
/// keeps all rendering (grid update, trigger scan, time spread) behind these callbacks.
/// All callbacks are invoked on the engine's single worker thread, in event order; the sink is
/// responsible for any marshalling to the UI thread.
/// </summary>
public interface ITailFollowSink
{
    /// <summary>
    /// True once the sink can no longer accept callbacks (window disposed or closing).
    /// The engine checks this before dispatching an event and exits its worker permanently.
    /// </summary>
    bool IsAbandoned { get; }

    /// <summary>
    /// The file rolled over: shift all line-anchored state (bookmarks, row heights, filter pipes)
    /// by <paramref name="rolloverOffset"/> lines. Raised before <see cref="OnTailLines"/> for the
    /// same event — and regardless of <see cref="IsAbandoned"/>, mirroring the original worker.
    /// </summary>
    void OnRolloverShift (int rolloverOffset);

    /// <summary>
    /// New tail content: render the event and run the tail trigger path. Exceptions thrown here
    /// never kill the engine's worker (see <see cref="TailFollowEngine"/> for the contract).
    /// </summary>
    void OnTailLines (LogEventArgs e);

    /// <summary>
    /// The event's line count, raised after <see cref="OnTailLines"/> for the same event — even
    /// when that dispatch failed non-fatally (the time spread bar always learns the new count).
    /// </summary>
    void OnLineCountChanged (int lineCount);
}
