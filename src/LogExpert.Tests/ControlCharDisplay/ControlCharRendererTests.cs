using System.Collections.Generic;

using LogExpert.Core.Config;
using LogExpert.UI.ControlCharDisplay;

using NUnit.Framework;

namespace LogExpert.Tests.ControlCharDisplay;

[TestFixture]
public class ControlCharRendererTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void Render_EmptyInput_ReturnsEmptySegmentList (bool substitute)
    {
        var settings = new ControlCharSettings { Substitute = substitute };

        IReadOnlyList<RenderSegment> segments = ControlCharRenderer.Render(string.Empty, settings);

        Assert.That(segments, Is.Empty);
    }

    [Test]
    public void Render_SubstituteOff_ReturnsSingleRawSegmentForWholeInput ()
    {
        var settings = new ControlCharSettings { Substitute = false };
        const string raw = "hello\u0001world";

        IReadOnlyList<RenderSegment> segments = ControlCharRenderer.Render(raw, settings);

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0].SourceStart, Is.EqualTo(0));
        Assert.That(segments[0].SourceLength, Is.EqualTo(raw.Length));
        Assert.That(segments[0].RenderedText, Is.EqualTo(raw));
        Assert.That(segments[0].IsSubstituted, Is.False);
    }

    [Test]
    public void Render_SubstituteOn_NoControlChars_ReturnsSingleRawSegment ()
    {
        var settings = new ControlCharSettings { Substitute = true };
        const string raw = "plain text only";

        IReadOnlyList<RenderSegment> segments = ControlCharRenderer.Render(raw, settings);

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0].SourceStart, Is.EqualTo(0));
        Assert.That(segments[0].SourceLength, Is.EqualTo(raw.Length));
        Assert.That(segments[0].RenderedText, Is.EqualTo(raw));
        Assert.That(segments[0].IsSubstituted, Is.False);
    }

    [Test]
    public void Render_SubstituteOn_SingleEnabledControlChar_ReturnsSingleSubstitutedSegment ()
    {
        var settings = new ControlCharSettings { Substitute = true, Style = ControlCharStyle.Caret };
        const string raw = "\u0001";

        var segments = ControlCharRenderer.Render(raw, settings);

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0].SourceStart, Is.EqualTo(0));
        Assert.That(segments[0].SourceLength, Is.EqualTo(1));
        Assert.That(segments[0].RenderedText, Is.EqualTo("^A"));
        Assert.That(segments[0].IsSubstituted, Is.True);
    }

    [Test]
    public void Render_SubstituteOn_SingleDisabledControlChar_ReturnsRawSegment ()
    {
        // 0x01 is in the default preset; explicitly remove it.
        var settings = new ControlCharSettings
        {
            Substitute = true,
            EnabledCodepoints = new HashSet<int>(),
        };
        const string raw = "\u0001";

        var segments = ControlCharRenderer.Render(raw, settings);

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0].IsSubstituted, Is.False);
        Assert.That(segments[0].RenderedText, Is.EqualTo("\u0001"));
        Assert.That(segments[0].SourceLength, Is.EqualTo(1));
    }

    [Test]
    public void Render_SubstituteOn_MixedInput_SplitsIntoThreeSegments ()
    {
        var settings = new ControlCharSettings { Substitute = true, Style = ControlCharStyle.Caret };
        const string raw = "a\u0001b";

        var segments = ControlCharRenderer.Render(raw, settings);

        Assert.That(segments, Has.Count.EqualTo(3));

        Assert.That(segments[0].SourceStart, Is.EqualTo(0));
        Assert.That(segments[0].SourceLength, Is.EqualTo(1));
        Assert.That(segments[0].RenderedText, Is.EqualTo("a"));
        Assert.That(segments[0].IsSubstituted, Is.False);

        Assert.That(segments[1].SourceStart, Is.EqualTo(1));
        Assert.That(segments[1].SourceLength, Is.EqualTo(1));
        Assert.That(segments[1].RenderedText, Is.EqualTo("^A"));
        Assert.That(segments[1].IsSubstituted, Is.True);

        Assert.That(segments[2].SourceStart, Is.EqualTo(2));
        Assert.That(segments[2].SourceLength, Is.EqualTo(1));
        Assert.That(segments[2].RenderedText, Is.EqualTo("b"));
        Assert.That(segments[2].IsSubstituted, Is.False);
    }

    [Test]
    public void Render_AdjacentControlChars_ProducesTwoSubstitutedSegments ()
    {
        var settings = new ControlCharSettings { Substitute = true, Style = ControlCharStyle.Caret };
        const string raw = "\u0001\u0002";

        var segments = ControlCharRenderer.Render(raw, settings);

        Assert.That(segments, Has.Count.EqualTo(2));
        Assert.That(segments[0].IsSubstituted, Is.True);
        Assert.That(segments[0].RenderedText, Is.EqualTo("^A"));
        Assert.That(segments[0].SourceStart, Is.EqualTo(0));
        Assert.That(segments[1].IsSubstituted, Is.True);
        Assert.That(segments[1].RenderedText, Is.EqualTo("^B"));
        Assert.That(segments[1].SourceStart, Is.EqualTo(1));
    }

    [Test]
    public void Render_LeadingControlChar_NoEmptyRawSegmentBefore ()
    {
        var settings = new ControlCharSettings { Substitute = true, Style = ControlCharStyle.Caret };
        const string raw = "\u0001a";

        var segments = ControlCharRenderer.Render(raw, settings);

        Assert.That(segments, Has.Count.EqualTo(2));
        Assert.That(segments[0].IsSubstituted, Is.True);
        Assert.That(segments[1].IsSubstituted, Is.False);
        Assert.That(segments[1].RenderedText, Is.EqualTo("a"));
    }

    [Test]
    public void Render_TrailingControlChar_NoEmptyRawSegmentAfter ()
    {
        var settings = new ControlCharSettings { Substitute = true, Style = ControlCharStyle.Caret };
        const string raw = "a\u0001";

        var segments = ControlCharRenderer.Render(raw, settings);

        Assert.That(segments, Has.Count.EqualTo(2));
        Assert.That(segments[0].IsSubstituted, Is.False);
        Assert.That(segments[0].RenderedText, Is.EqualTo("a"));
        Assert.That(segments[1].IsSubstituted, Is.True);
    }

    private static readonly object[] s_sourceIndexCases =
    [
        "",
        "plain text",
        "\u0001",
        "a\u0001b",
        "\u0001\u0002",
        "\u0001a",
        "a\u0001",
        "abc\u0001\u0002def\u0007ghi",
    ];

    [TestCaseSource(nameof(s_sourceIndexCases))]
    public void Render_SourceIndexIntegrity_SegmentsCoverInputExactly (string raw)
    {
        var settings = new ControlCharSettings { Substitute = true, Style = ControlCharStyle.Caret };

        var segments = ControlCharRenderer.Render(raw, settings);

        var reconstructed = new System.Text.StringBuilder();
        int expectedStart = 0;
        foreach (var seg in segments)
        {
            Assert.That(seg.SourceStart, Is.EqualTo(expectedStart),
                $"Segment starts at {seg.SourceStart}, expected {expectedStart}.");
            _ = reconstructed.Append(raw.AsSpan(seg.SourceStart, seg.SourceLength));
            expectedStart = seg.SourceStart + seg.SourceLength;
        }

        Assert.That(reconstructed.ToString(), Is.EqualTo(raw));
        Assert.That(expectedStart, Is.EqualTo(raw.Length));
    }

    [TestCase(ControlCharStyle.Caret, "^G")]
    [TestCase(ControlCharStyle.Abbreviation, "BEL")]
    [TestCase(ControlCharStyle.ControlPictures, "\u2407")]
    public void Render_StylePropagation_DelegatesToFormatter (ControlCharStyle style, string expected)
    {
        var settings = new ControlCharSettings { Substitute = true, Style = style };
        const string raw = "\u0007";

        var segments = ControlCharRenderer.Render(raw, settings);

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0].RenderedText, Is.EqualTo(expected));
        Assert.That(segments[0].IsSubstituted, Is.True);
    }

    [Test]
    public void Render_SelectiveOptIn_LeavesDisabledCodepointAsRaw ()
    {
        // Only \x02 is enabled; \x01 must remain in a raw segment.
        var settings = new ControlCharSettings
        {
            Substitute = true,
            Style = ControlCharStyle.ControlPictures,
            EnabledCodepoints = [0x02],
        };
        const string raw = "a\u0001b\u0002c";

        var segments = ControlCharRenderer.Render(raw, settings);

        Assert.That(segments, Has.Count.EqualTo(3));
        Assert.That(segments[0].IsSubstituted, Is.False);
        Assert.That(segments[0].RenderedText, Is.EqualTo("a\u0001b"));
        Assert.That(segments[0].SourceStart, Is.EqualTo(0));
        Assert.That(segments[0].SourceLength, Is.EqualTo(3));

        Assert.That(segments[1].IsSubstituted, Is.True);
        Assert.That(segments[1].RenderedText, Is.EqualTo("\u2402"));
        Assert.That(segments[1].SourceStart, Is.EqualTo(3));
        Assert.That(segments[1].SourceLength, Is.EqualTo(1));

        Assert.That(segments[2].IsSubstituted, Is.False);
        Assert.That(segments[2].RenderedText, Is.EqualTo("c"));
        Assert.That(segments[2].SourceStart, Is.EqualTo(4));
    }

    [Test]
    public void HasAnyEnabledCodepoint_NoMatch_ReturnsFalse ()
    {
        var enabled = new HashSet<int> { 0x01, 0x07 };
        Assert.That(ControlCharRenderer.HasAnyEnabledCodepoint("plain text", enabled), Is.False);
    }

    [Test]
    public void HasAnyEnabledCodepoint_OneMatch_ReturnsTrue ()
    {
        var enabled = new HashSet<int> { 0x01 };
        Assert.That(ControlCharRenderer.HasAnyEnabledCodepoint("a\u0001b", enabled), Is.True);
    }

    [Test]
    public void HasAnyEnabledCodepoint_EmptyOrNullSet_ReturnsFalse ()
    {
        Assert.That(ControlCharRenderer.HasAnyEnabledCodepoint("a\u0001b", []), Is.False);
        Assert.That(ControlCharRenderer.HasAnyEnabledCodepoint("a\u0001b", null!), Is.False);
    }
}
