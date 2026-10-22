using System;
using System.Collections.Generic;
using System.Globalization;

using LogExpert.Core.Config;

namespace LogExpert.UI.ControlCharDisplay;

internal static class ControlCharStyleFormatter
{
    private static readonly IReadOnlyDictionary<int, string> CEscapeExplicit = new Dictionary<int, string>
    {
        [0x00] = "\\0",
        [0x07] = "\\a",
        [0x08] = "\\b",
        [0x09] = "\\t",
        [0x0A] = "\\n",
        [0x0B] = "\\v",
        [0x0C] = "\\f",
        [0x0D] = "\\r",
    };

    private static readonly string[] AbbreviationsC0 =
    [
        "NUL", "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL",
        "BS",  "HT",  "LF",  "VT",  "FF",  "CR",  "SO",  "SI",
        "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB",
        "CAN", "EM",  "SUB", "ESC", "FS",  "GS",  "RS",  "US",
    ];

    // ISO 2047 defines pictographic glyphs for C0 control codes. Most are not present in
    // standard Unicode as distinct codepoints, so this table is sparse: only entries with a
    // distinct Unicode representation that differs from the U+2400 Control Pictures block
    // are listed. Everything not in this table falls back to the Control Pictures glyph.
    private static readonly IReadOnlyDictionary<int, string> Iso2047Glyphs = new Dictionary<int, string>
    {
        [0x7F] = "\u2425", // SYMBOL FOR DELETE FORM TWO
    };

    public static string Format (int codepoint, ControlCharStyle style)
    {
        if (!IsInScope(codepoint))
        {
            throw new ArgumentOutOfRangeException(
                nameof(codepoint),
                codepoint,
                "Codepoint must be in C0 range (0x00..0x1F) or DEL (0x7F).");
        }

        return style switch
        {
            ControlCharStyle.Caret => FormatCaret(codepoint),
            ControlCharStyle.CEscape => FormatCEscape(codepoint),
            ControlCharStyle.Abbreviation => FormatAbbreviation(codepoint),
            ControlCharStyle.ControlPictures => FormatControlPictures(codepoint),
            ControlCharStyle.Iso2047 => FormatIso2047(codepoint),
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, "Style not implemented."),
        };
    }

    private static bool IsInScope (int codepoint)
    {
        return (codepoint >= 0x00 && codepoint <= 0x1F) || codepoint == 0x7F;
    }

    private static string FormatCaret (int codepoint)
    {
        if (codepoint == 0x7F)
        {
            return "^?";
        }

        return "^" + (char)(codepoint + 0x40);
    }

    private static string FormatCEscape (int codepoint)
    {
        if (CEscapeExplicit.TryGetValue(codepoint, out string? explicitEscape))
        {
            return explicitEscape;
        }

        return "\\x" + codepoint.ToString("X2", CultureInfo.InvariantCulture);
    }

    private static string FormatAbbreviation (int codepoint)
    {
        if (codepoint == 0x7F)
        {
            return "DEL";
        }

        return AbbreviationsC0[codepoint];
    }

    private static string FormatControlPictures (int codepoint)
    {
        if (codepoint == 0x7F)
        {
            return "\u2421";
        }

        return ((char)(0x2400 + codepoint)).ToString();
    }

    private static string FormatIso2047 (int codepoint)
    {
        if (Iso2047Glyphs.TryGetValue(codepoint, out string? glyph))
        {
            return glyph;
        }

        return FormatControlPictures(codepoint);
    }
}
