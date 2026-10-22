using System;

using LogExpert.Core.Config;
using LogExpert.UI.ControlCharDisplay;

using NUnit.Framework;

namespace LogExpert.Tests.ControlCharDisplay;

[TestFixture]
public class ControlCharStyleFormatterTests
{
    [TestCase(0x00, "^@")]
    [TestCase(0x01, "^A")]
    [TestCase(0x07, "^G")]
    [TestCase(0x1F, "^_")]
    [TestCase(0x7F, "^?")]
    public void Format_CaretStyle_ReturnsCaretNotation (int codepoint, string expected)
    {
        string actual = ControlCharStyleFormatter.Format(codepoint, ControlCharStyle.Caret);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(0x00, "\\0")]
    [TestCase(0x07, "\\a")]
    [TestCase(0x08, "\\b")]
    [TestCase(0x09, "\\t")]
    [TestCase(0x0A, "\\n")]
    [TestCase(0x0B, "\\v")]
    [TestCase(0x0C, "\\f")]
    [TestCase(0x0D, "\\r")]
    public void Format_CEscape_ExplicitTable_ReturnsBackslashLetter (int codepoint, string expected)
    {
        string actual = ControlCharStyleFormatter.Format(codepoint, ControlCharStyle.CEscape);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(0x01, "\\x01")]
    [TestCase(0x1F, "\\x1F")]
    [TestCase(0x7F, "\\x7F")]
    public void Format_CEscape_Fallback_ReturnsHexEscape (int codepoint, string expected)
    {
        string actual = ControlCharStyleFormatter.Format(codepoint, ControlCharStyle.CEscape);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(0x00, "NUL")]
    [TestCase(0x01, "SOH")]
    [TestCase(0x02, "STX")]
    [TestCase(0x03, "ETX")]
    [TestCase(0x04, "EOT")]
    [TestCase(0x05, "ENQ")]
    [TestCase(0x06, "ACK")]
    [TestCase(0x07, "BEL")]
    [TestCase(0x08, "BS")]
    [TestCase(0x09, "HT")]
    [TestCase(0x0A, "LF")]
    [TestCase(0x0B, "VT")]
    [TestCase(0x0C, "FF")]
    [TestCase(0x0D, "CR")]
    [TestCase(0x0E, "SO")]
    [TestCase(0x0F, "SI")]
    [TestCase(0x10, "DLE")]
    [TestCase(0x11, "DC1")]
    [TestCase(0x12, "DC2")]
    [TestCase(0x13, "DC3")]
    [TestCase(0x14, "DC4")]
    [TestCase(0x15, "NAK")]
    [TestCase(0x16, "SYN")]
    [TestCase(0x17, "ETB")]
    [TestCase(0x18, "CAN")]
    [TestCase(0x19, "EM")]
    [TestCase(0x1A, "SUB")]
    [TestCase(0x1B, "ESC")]
    [TestCase(0x1C, "FS")]
    [TestCase(0x1D, "GS")]
    [TestCase(0x1E, "RS")]
    [TestCase(0x1F, "US")]
    [TestCase(0x7F, "DEL")]
    public void Format_Abbreviation_ReturnsMnemonic (int codepoint, string expected)
    {
        string actual = ControlCharStyleFormatter.Format(codepoint, ControlCharStyle.Abbreviation);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(0x00, "\u2400")]
    [TestCase(0x07, "\u2407")]
    [TestCase(0x1F, "\u241F")]
    [TestCase(0x7F, "\u2421")]
    public void Format_ControlPictures_ReturnsU24xxGlyph (int codepoint, string expected)
    {
        string actual = ControlCharStyleFormatter.Format(codepoint, ControlCharStyle.ControlPictures);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Format_Iso2047_CoveredCharacter_ReturnsIso2047Glyph ()
    {
        // DEL has a distinct ISO 2047 representation (U+2425, "SYMBOL FOR DELETE FORM TWO"),
        // separate from the Control Pictures glyph for DEL (U+2421).
        string actual = ControlCharStyleFormatter.Format(0x7F, ControlCharStyle.Iso2047);
        Assert.That(actual, Is.EqualTo("\u2425"));
    }

    [Test]
    public void Format_Iso2047_UncoveredCharacter_FallsBackToControlPictures ()
    {
        string actual = ControlCharStyleFormatter.Format(0x07, ControlCharStyle.Iso2047);
        Assert.That(actual, Is.EqualTo("\u2407"));
    }

    [TestCase(0x20)]
    [TestCase(0x41)]
    [TestCase(0x80)]
    [TestCase(-1)]
    public void Format_OutOfScopeCodepoint_Throws (int codepoint)
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(
            () => ControlCharStyleFormatter.Format(codepoint, ControlCharStyle.ControlPictures));
    }
}
