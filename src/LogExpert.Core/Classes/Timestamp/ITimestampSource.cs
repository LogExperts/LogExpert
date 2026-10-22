using System.Threading;

using ColumnizerLib;

using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Timestamp;

/// <summary>
/// The narrow view of a Log Window that <see cref="TimestampLocator"/> needs: a Logfile Reader,
/// the active Columnizer, a callback to position, and the lock that guards Columnizer swaps.
/// </summary>
/// <remarks>
/// Every member is a property rather than a value captured at construction, deliberately.
/// A Log Window replaces its Logfile Reader on load, reload and rollover, and replaces its
/// Columnizer whenever the user picks a different one; a locator that had captured either
/// would go stale. Consumers such as the Time Spread calculator outlive both events.
/// </remarks>
public interface ITimestampSource
{
    /// <summary>
    /// The Logfile Reader currently backing the window. Read on every access — never cached.
    /// </summary>
    ILogfileReader Reader { get; }

    /// <summary>
    /// The Columnizer currently selected for the window. Read under <see cref="ColumnizerLock"/>.
    /// </summary>
    ILogLineMemoryColumnizer Columnizer { get; }

    /// <summary>
    /// The callback handed to the Columnizer. The locator positions it on each line it asks about.
    /// </summary>
    IPositionedColumnizerCallback Callback { get; }

    /// <summary>
    /// Guards <see cref="Columnizer"/> against being swapped mid-lookup. Owned by the window —
    /// the same lock its Columnizer setter takes — and merely borrowed by the locator.
    /// </summary>
    Lock ColumnizerLock { get; }
}
