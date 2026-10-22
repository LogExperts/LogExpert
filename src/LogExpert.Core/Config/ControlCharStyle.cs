using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LogExpert.Core.Config;

[JsonConverter(typeof(StringEnumConverter))]
public enum ControlCharStyle
{
    ControlPictures = 0,
    Caret = 1,
    CEscape = 2,
    Abbreviation = 3,
    Iso2047 = 4,
}
