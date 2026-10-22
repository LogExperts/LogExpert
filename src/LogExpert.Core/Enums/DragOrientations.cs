using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LogExpert.Core.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum DragOrientations
{
    Horizontal = 0,
    Vertical = 1,
    InvertedVertical = 2
}
