using System.Drawing;
using System.Runtime.Versioning;

namespace LogExpert.Core.Helpers;

public static class FontHelper
{
    [SupportedOSPlatform("windows")]
    public static Font ParseFontStringOrDefault (string fontString)
    {
        if (!string.IsNullOrWhiteSpace(fontString))
        {
            try
            {
                var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));
                if (converter.ConvertFromInvariantString(fontString) is Font parsed)
                {
                    return parsed;
                }
            }
            catch (Exception ex) when (ex is NotSupportedException or ArgumentException or FormatException)
            {
                // Fall back to the default below.
            }
        }

        return new Font(FontFamily.GenericMonospace.Name, 9f);
    }
}
