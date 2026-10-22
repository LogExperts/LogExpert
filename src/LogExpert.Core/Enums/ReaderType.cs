using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LogExpert.Core.Enums;

/// <summary>
/// Defines the available stream reader implementations.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ReaderType
{
    /// <summary>
    /// Direct-read implementation: reads decoded chars directly into pooled blocks via
    /// StreamReader.Read(char[], offset, count), eliminating per-line string allocation.
    /// </summary>
    SystemDirect = 0,

    /// <summary>
    /// Legacy reader implementation (original).
    /// </summary>
    Legacy = 1,

    /// <summary>
    /// System.IO.StreamReader based implementation.
    /// </summary>
    System = 2
}
