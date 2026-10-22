using LogExpert.Core.Config;
using LogExpert.UI.ControlCharDisplay;

using NUnit.Framework;

namespace LogExpert.Tests.ControlCharDisplay;

[TestFixture]
public class SubstitutedClipboardBuilderTests
{
    [Test]
    public void Build_SubstituteFalse_ReturnsRawSubstring ()
    {
        var settings = new ControlCharSettings
        {
            Substitute = false,
            CopyDisplayedForm = true, // Even with this true, Substitute=false short-circuits.
        };

        var result = SubstitutedClipboardBuilder.Build("hello\u0001world", 0, 11, settings);

        Assert.That(result, Is.EqualTo("hello\u0001world"));
    }

    [Test]
    public void Build_SubstituteTrue_CopyDisplayedFormFalse_ReturnsRawSubstring ()
    {
        var settings = new ControlCharSettings
        {
            Substitute = true,
            CopyDisplayedForm = false,
            Style = ControlCharStyle.Caret,
        };

        var result = SubstitutedClipboardBuilder.Build("a\u0001b", 0, 3, settings);

        Assert.That(result, Is.EqualTo("a\u0001b"));
    }

    [Test]
    public void Build_CopyDisplayedFormTrue_SelectionWithNoEnabledCodepoints_ReturnsRawSubstring ()
    {
        var settings = new ControlCharSettings
        {
            Substitute = true,
            CopyDisplayedForm = true,
            Style = ControlCharStyle.Caret,
        };

        // EnabledCodepoints default preset (non-whitespace C0+DEL). "plain text" has none.
        var result = SubstitutedClipboardBuilder.Build("plain text", 0, 10, settings);

        Assert.That(result, Is.EqualTo("plain text"));
    }

    [Test]
    public void Build_CaretStyle_SelectionContainingOneSubstitutedChar_Interleaves ()
    {
        var settings = new ControlCharSettings
        {
            Substitute = true,
            CopyDisplayedForm = true,
            Style = ControlCharStyle.Caret,
        };

        var result = SubstitutedClipboardBuilder.Build("a\u0001b", 0, 3, settings);

        Assert.That(result, Is.EqualTo("a^Ab"));
    }

    [TestCase(ControlCharStyle.Caret, "a^Ab")]
    [TestCase(ControlCharStyle.CEscape, "a\\x01b")]
    [TestCase(ControlCharStyle.Abbreviation, "aSOHb")]
    [TestCase(ControlCharStyle.ControlPictures, "a\u2401b")]
    [TestCase(ControlCharStyle.Iso2047, "a\u2401b")]
    public void Build_AllStyles_ProduceExpectedRenderedText (ControlCharStyle style, string expected)
    {
        var settings = new ControlCharSettings
        {
            Substitute = true,
            CopyDisplayedForm = true,
            Style = style,
        };

        var result = SubstitutedClipboardBuilder.Build("a\u0001b", 0, 3, settings);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Build_SelectionStartsMidRaw_EndsMidRaw_SpansOneSubstituted ()
    {
        var settings = new ControlCharSettings
        {
            Substitute = true,
            CopyDisplayedForm = true,
            Style = ControlCharStyle.Caret,
        };

        // Raw "abc\u0001def" — select "c\u0001d" (start=2, len=3).
        var result = SubstitutedClipboardBuilder.Build("abc\u0001def", 2, 3, settings);

        Assert.That(result, Is.EqualTo("c^Ad"));
    }

    [Test]
    public void Build_SelectionStartsOnSubstitutedChar_OutputBeginsWithGlyph ()
    {
        var settings = new ControlCharSettings
        {
            Substitute = true,
            CopyDisplayedForm = true,
            Style = ControlCharStyle.Caret,
        };

        // Raw "a\u0001b" — select starting at the SOH (start=1, len=2).
        var result = SubstitutedClipboardBuilder.Build("a\u0001b", 1, 2, settings);

        Assert.That(result, Is.EqualTo("^Ab"));
    }

    [Test]
    public void Build_SelectionEndsOnSubstitutedChar_OutputEndsWithGlyph ()
    {
        var settings = new ControlCharSettings
        {
            Substitute = true,
            CopyDisplayedForm = true,
            Style = ControlCharStyle.Caret,
        };

        // Raw "a\u0001b" — select "a\u0001" (start=0, len=2).
        var result = SubstitutedClipboardBuilder.Build("a\u0001b", 0, 2, settings);

        Assert.That(result, Is.EqualTo("a^A"));
    }

    [Test]
    public void Build_ZeroLengthSelection_ReturnsEmptyString ()
    {
        var settings = new ControlCharSettings
        {
            Substitute = true,
            CopyDisplayedForm = true,
            Style = ControlCharStyle.Caret,
        };

        Assert.That(SubstitutedClipboardBuilder.Build("anything\u0001", 3, 0, settings), Is.Empty);
    }

    [Test]
    public void Build_SelectionEqualsFullInput_MatchesRendererConcatenation ()
    {
        const string raw = "abc\u0001def\u0007ghi";
        var settings = new ControlCharSettings
        {
            Substitute = true,
            CopyDisplayedForm = true,
            Style = ControlCharStyle.Caret,
        };

        var rendered = ControlCharRenderer.Render(raw, settings);
        var expected = new System.Text.StringBuilder();
        foreach (var seg in rendered)
        {
            _ = expected.Append(seg.RenderedText);
        }

        var result = SubstitutedClipboardBuilder.Build(raw, 0, raw.Length, settings);

        Assert.That(result, Is.EqualTo(expected.ToString()));
    }
}
