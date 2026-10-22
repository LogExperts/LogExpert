using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LogExpert.Core.Config;

/// <summary>
/// Color scheme the application applies at startup (issue #698).
/// </summary>
[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum ColorMode
{
    /// <summary>
    /// Forced light mode, ignoring the Windows theme (maps to SystemColorMode.Classic).
    /// </summary>
    Light = 0,

    /// <summary>
    /// Forced dark mode, ignoring the Windows theme.
    /// </summary>
    Dark = 1,

    /// <summary>
    /// Follow the Windows theme.
    /// </summary>
    System = 2,
}
