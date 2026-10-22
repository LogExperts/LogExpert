using ColumnizerLib;

using LogExpert.Core.Config;

namespace LogExpert.Core.Classes.Columnizer;

/// <summary>
/// Inputs to <see cref="Resolve"/>. Designed so the module is exercisable from unit tests with no
/// dependency on settings storage, plugin registry singletons, or UI.
/// </summary>
public sealed class ResolveInputs
{
    public ColumnizerSelectionPriority Priority { get; init; }

    /// <summary>The full path or identifier of the file being opened.</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>The short (filename-only) form of <see cref="FileName"/>, used for mask matching.</summary>
    public string ShortFileName { get; init; } = string.Empty;

    public IReadOnlyList<ColumnizerMaskEntry> MaskList { get; init; } = [];

    /// <summary>Lookup of a saved history columnizer name for <see cref="FileName"/>. May be <see langword="null"/>.</summary>
    public Func<string, string?>? HistoryLookup { get; init; }

    /// <summary>Columnizer name supplied by the per-file persistence (<c>.lxp</c>). May be <see langword="null"/>.</summary>
    public string? PersistenceColumnizerName { get; init; }

    /// <summary>AutoPick callback (e.g. content-based detection). May be <see langword="null"/>.</summary>
    public Func<ILogLineMemoryColumnizer?>? AutoPick { get; init; }

    public IList<ILogLineMemoryColumnizer> Registered { get; init; } = [];

    /// <summary>Invoked once for each Mask entry that matched but referenced a missing columnizer.</summary>
    public Action<ColumnizerMaskEntry>? OnStaleMaskEntry { get; init; }
}
