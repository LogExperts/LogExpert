using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LogExpert.Core.Config;

/// <summary>
/// Determines how a <see cref="ColumnizerMaskEntry.Mask"/> string is interpreted when matching file names.
/// </summary>
/// <remarks>Serialized by name (e.g. "Glob"/"Regex"); the converter still reads legacy integer values.</remarks>
[JsonConverter(typeof(StringEnumConverter))]
public enum MaskType
{
    /// <summary>Glob pattern using <c>*</c> and <c>?</c> wildcards (default for new entries).</summary>
    Glob = 0,

    /// <summary>.NET regular expression syntax (legacy default; preserved by migration for pre-existing entries).</summary>
    Regex = 1,
}
