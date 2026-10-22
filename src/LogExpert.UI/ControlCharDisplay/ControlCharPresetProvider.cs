using System.Collections.Frozen;

namespace LogExpert.UI.ControlCharDisplay;

/// <summary>
/// Immutable preset sets of control-character code points used by the settings dialog's
/// quick-preset buttons. Sets are returned as <see cref="FrozenSet{T}"/> so callers
/// cannot mutate them; the dialog copies into a new <c>HashSet</c> when assigning.
/// </summary>
public static class ControlCharPresetProvider
{
    /// <summary>C0 control characters (0x00..0x1F) plus DEL (0x7F) — 33 entries.</summary>
    public static IReadOnlySet<int> All { get; } = Enumerable.Range(0, 32).Append(0x7F).ToFrozenSet();

    /// <summary>Empty set.</summary>
    public static IReadOnlySet<int> None { get; } = FrozenSet<int>.Empty;

    /// <summary><see cref="All"/> minus TAB (0x09), LF (0x0A), CR (0x0D) — 30 entries.</summary>
    public static IReadOnlySet<int> NonWhitespaceDefaults { get; } = All.Where(cp => cp is not 0x09 and not 0x0A and not 0x0D).ToFrozenSet();
}
