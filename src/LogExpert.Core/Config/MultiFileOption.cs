using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LogExpert.Core.Config;

[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum MultiFileOption
{
    SingleFiles = 0,
    MultiFile = 1,
    Ask = 2
}