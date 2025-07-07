using System;
using System.Collections.Generic;

namespace LogExpert.Core.Classes.DateTimeParser;

//TODO: This should be moved into LogExpert.UI and changed to internal
// Ensures we have constant width (number of characters) date formats
public static class DateFormatPartAdjuster
{
    private static readonly IDictionary<string, string> _dateTimePartReplacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["y"] = "yyy",
        ["yyy"] = "yyyy",
        ["m"] = "mm",
        ["d"] = "dd",
        ["h"] = "hh",
        ["s"] = "ss"
    };

    public static string AdjustDateTimeFormatPart(string part)
    {
        ArgumentNullException.ThrowIfNull(part, nameof(part));

        if (!_dateTimePartReplacements.TryGetValue(part, out var adjustedPart))
        {
            return part;
        }

        if (char.IsUpper(part[0]))
        {
            return adjustedPart.ToUpper();
        }
        else
        {
            return adjustedPart.ToLower();
        }
    }
}