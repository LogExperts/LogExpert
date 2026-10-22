using ColumnizerLib;

using LogExpert.Core.Config;

namespace LogExpert.Core.Classes.Columnizer;

/// <summary>
/// Pure precedence-chain resolver that picks a columnizer for a given file from up to four sources: per-file
/// persistence, columnizer history, columnizer-mask list, and AutoPick.
/// </summary>
/// <remarks>
/// The exact order of consultation is controlled by <see cref="ColumnizerSelectionPriority"/>. AutoPick fires only when
/// every other source produced <see langword="null"/>; it never outranks an explicit Mask, History, or Persistence hit.
/// <para> Stale Mask entries — those whose <see cref="ColumnizerMaskEntry.ColumnizerName"/> is not registered — are
/// skipped (an optional callback is invoked once per skipped entry) and resolution continues with the next entry in the
/// list. </para>
/// </remarks>
public static class ColumnizerResolver
{
    /// <summary>
    /// Returns the winning columnizer for the inputs, or <see langword="null"/> if no source produced a match.
    /// </summary>
    public static ILogLineMemoryColumnizer? Resolve (ResolveInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        // Look up a columnizer by name with "no signal" semantics — returns null when the name is
        // missing OR not registered, so the precedence chain falls through to the next source.
        ILogLineMemoryColumnizer? byName (string? name) => string.IsNullOrEmpty(name) ? null : ColumnizerPicker.FindMemoryColumnizerByName(name, inputs.Registered);
        ILogLineMemoryColumnizer? mask () => TryGetMaskColumnizer(inputs.MaskList, inputs.ShortFileName, inputs.Registered, inputs.OnStaleMaskEntry);
        ILogLineMemoryColumnizer? history () => byName(inputs.HistoryLookup?.Invoke(inputs.FileName));
        ILogLineMemoryColumnizer? persistence () => byName(inputs.PersistenceColumnizerName);

        var winner = inputs.Priority switch
        {
            ColumnizerSelectionPriority.MaskThenHistory => persistence() ?? mask() ?? history(),
            ColumnizerSelectionPriority.MaskOverridesPersistence => mask() ?? persistence() ?? history(),
            ColumnizerSelectionPriority.HistoryThenMask => persistence() ?? history() ?? mask(),
            _ => persistence() ?? history() ?? mask(),
        };

        return winner ?? inputs.AutoPick?.Invoke();
    }

    /// <summary>
    /// Iterates the mask list and returns the first non-stale match. Entries whose columnizer is not registered invoke
    /// <paramref name="onStale"/> and the iteration continues. Never throws.
    /// </summary>
    public static ILogLineMemoryColumnizer? TryGetMaskColumnizer (
        IReadOnlyList<ColumnizerMaskEntry> maskList,
        string shortFileName,
        IList<ILogLineMemoryColumnizer> registered,
        Action<ColumnizerMaskEntry>? onStale = null)
    {
        if (maskList == null || maskList.Count == 0 || string.IsNullOrEmpty(shortFileName))
        {
            return null;
        }

        foreach (var entry in maskList)
        {
            if (!ColumnizerMaskMatcher.Matches(entry, shortFileName))
            {
                continue;
            }


            // FindMemoryColumnizerByName returns null when the name isn't registered, which is
            // exactly the "stale entry" signal we need.
            var columnizer = ColumnizerPicker.FindMemoryColumnizerByName(entry.ColumnizerName, registered);
            if (columnizer != null)
            {
                return columnizer;
            }

            onStale?.Invoke(entry);
        }

        return null;
    }
}