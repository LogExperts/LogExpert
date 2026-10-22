using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LogExpert.Core.Config;

/// <summary>
/// Controls the precedence order used by the columnizer resolver when multiple sources
/// (per-file persistence, history, mask list) could supply a columnizer for a file.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ColumnizerSelectionPriority
{
    /// <summary>Persistence → History → Mask → AutoPick (default — preserves legacy behaviour).</summary>
    HistoryThenMask = 0,

    /// <summary>Persistence → Mask → History → AutoPick.</summary>
    MaskThenHistory = 1,

    /// <summary>Mask → Persistence → History → AutoPick. A matching mask outranks the saved <c>.lxp</c> columnizer.</summary>
    MaskOverridesPersistence = 2,
}