using System;

namespace LogExpert.Core.Classes.DateTimeParser;

public static class Token
{
    //TOOD: check if the callers are checking for null before calling
    public static bool IsDatePart(string token)
    {
        ArgumentNullException.ThrowIfNull(token, nameof(token));
        return
            token.StartsWith("y", StringComparison.OrdinalIgnoreCase) ||
            token.StartsWith("m", StringComparison.OrdinalIgnoreCase) ||
            token.StartsWith("d", StringComparison.OrdinalIgnoreCase) ||
            token.StartsWith("s", StringComparison.OrdinalIgnoreCase) ||
            token.StartsWith("h", StringComparison.OrdinalIgnoreCase) ||
            token.Equals("tt", StringComparison.OrdinalIgnoreCase);
    }
}