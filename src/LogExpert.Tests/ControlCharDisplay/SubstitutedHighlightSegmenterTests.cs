using System.Collections.Generic;
using System.Drawing;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Config;
using LogExpert.UI.ControlCharDisplay;

using NUnit.Framework;

namespace LogExpert.Tests.ControlCharDisplay;

[TestFixture]
public class SubstitutedHighlightSegmenterTests
{
    private static HighlightEntry Ground (Color fore, Color back) => new()
    {
        ForegroundColor = fore,
        BackgroundColor = back,
    };

    [Test]
    public void Combine_NoHighlights_AllRaw_ReturnsSingleGroundStyleSegment ()
    {
        const string raw = "hello";
        var ground = Ground(Color.Black, Color.White);
        var settings = new ControlCharSettings();
        var renderSegments = new List<RenderSegment>
        {
            new(0, raw.Length, raw, false),
        };
        var highlightMatches = new List<HighlightMatchEntry>();

        IReadOnlyList<PaintSegment> segments =
            SubstitutedHighlightSegmenter.Combine(renderSegments, highlightMatches, ground, settings);

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0].RenderedText, Is.EqualTo("hello"));
        Assert.That(segments[0].ForeColor, Is.EqualTo(Color.Black));
        Assert.That(segments[0].BackColor, Is.EqualTo(Color.White));
        Assert.That(segments[0].IsSubstituted, Is.False);
    }

    [Test]
    public void Combine_OneSubstitutedSegment_UsesSubstitutionStyleAndAtomic ()
    {
        var ground = Ground(Color.Black, Color.White);
        var settings = new ControlCharSettings
        {
            ForeColor = Color.Gray,
        };
        // Source: "\u0001" — one char, substituted to "^A" (Caret style)
        var renderSegments = new List<RenderSegment>
        {
            new(0, 1, "^A", true),
        };
        settings.Style = ControlCharStyle.Caret;

        IReadOnlyList<PaintSegment> segments =
            SubstitutedHighlightSegmenter.Combine(renderSegments, [], ground, settings);

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0].RenderedText, Is.EqualTo("^A"));
        Assert.That(segments[0].ForeColor, Is.EqualTo(Color.Gray));
        Assert.That(segments[0].IsSubstituted, Is.True);
    }

    [Test]
    public void Combine_WordHighlightInsideRaw_SplitsRawIntoMultiplePaintSegments ()
    {
        // Raw: "hello world", word match "world" at 6..10 (length 5, IsWordMatch=true)
        const string raw = "hello world";
        var ground = Ground(Color.Black, Color.White);
        var settings = new ControlCharSettings();
        var renderSegments = new List<RenderSegment>
        {
            new(0, raw.Length, raw, false),
        };
        var wordHighlight = new HighlightEntry
        {
            ForegroundColor = Color.Yellow,
            BackgroundColor = Color.Red,
            IsWordMatch = true,
        };
        var matches = new List<HighlightMatchEntry>
        {
            new() { StartPos = 6, Length = 5, HighlightEntry = wordHighlight },
        };

        IReadOnlyList<PaintSegment> segments =
            SubstitutedHighlightSegmenter.Combine(renderSegments, matches, ground, settings);

        Assert.That(segments, Has.Count.EqualTo(2));
        Assert.That(segments[0].RenderedText, Is.EqualTo("hello "));
        Assert.That(segments[0].ForeColor, Is.EqualTo(Color.Black));
        Assert.That(segments[1].RenderedText, Is.EqualTo("world"));
        Assert.That(segments[1].ForeColor, Is.EqualTo(Color.Yellow));
        Assert.That(segments[1].BackColor, Is.EqualTo(Color.Red));
    }

    [Test]
    public void Combine_WordHighlightCoversSubstitutedCharacter_BackgroundFromHighlight_ForegroundFromSettings ()
    {
        // Raw: "\u0001" — single substituted char, fully covered by a word highlight.
        var ground = Ground(Color.Black, Color.White);
        var settings = new ControlCharSettings { ForeColor = Color.Gray };
        var renderSegments = new List<RenderSegment>
        {
            new(0, 1, "\u2401", true),
        };
        var wordHighlight = new HighlightEntry
        {
            ForegroundColor = Color.Yellow,
            BackgroundColor = Color.Red,
            IsWordMatch = true,
        };
        var matches = new List<HighlightMatchEntry>
        {
            new() { StartPos = 0, Length = 1, HighlightEntry = wordHighlight },
        };

        IReadOnlyList<PaintSegment> segments =
            SubstitutedHighlightSegmenter.Combine(renderSegments, matches, ground, settings);

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0].IsSubstituted, Is.True);
        Assert.That(segments[0].ForeColor, Is.EqualTo(Color.Gray));
        Assert.That(segments[0].BackColor, Is.EqualTo(Color.Red));
    }

    [Test]
    public void Combine_WordHighlightStraddlesSubstitutionBoundary_SplitsCorrectly ()
    {
        // Raw: "a\u0001b" → render segments: raw "a"(0), sub "^A"(1), raw "b"(2)
        // Word highlight covers positions 1..2 (substituted + trailing raw).
        var ground = Ground(Color.Black, Color.White);
        var settings = new ControlCharSettings { ForeColor = Color.Gray };
        var renderSegments = new List<RenderSegment>
        {
            new(0, 1, "a", false),
            new(1, 1, "^A", true),
            new(2, 1, "b", false),
        };
        var wordHighlight = new HighlightEntry
        {
            ForegroundColor = Color.Yellow,
            BackgroundColor = Color.Red,
            IsWordMatch = true,
        };
        var matches = new List<HighlightMatchEntry>
        {
            new() { StartPos = 1, Length = 2, HighlightEntry = wordHighlight },
        };

        IReadOnlyList<PaintSegment> segments =
            SubstitutedHighlightSegmenter.Combine(renderSegments, matches, ground, settings);

        Assert.That(segments, Has.Count.EqualTo(3));
        // Raw "a" — ground style
        Assert.That(segments[0].RenderedText, Is.EqualTo("a"));
        Assert.That(segments[0].ForeColor, Is.EqualTo(Color.Black));
        // Substituted — sub fore, highlight back
        Assert.That(segments[1].RenderedText, Is.EqualTo("^A"));
        Assert.That(segments[1].IsSubstituted, Is.True);
        Assert.That(segments[1].ForeColor, Is.EqualTo(Color.Gray));
        Assert.That(segments[1].BackColor, Is.EqualTo(Color.Red));
        // Raw "b" — highlight style
        Assert.That(segments[2].RenderedText, Is.EqualTo("b"));
        Assert.That(segments[2].ForeColor, Is.EqualTo(Color.Yellow));
        Assert.That(segments[2].BackColor, Is.EqualTo(Color.Red));
    }

    [Test]
    public void Combine_AdjacentSubstitutedSegments_StayAtomicEvenWithIdenticalStyle ()
    {
        var ground = Ground(Color.Black, Color.White);
        var settings = new ControlCharSettings { ForeColor = Color.Gray };
        var renderSegments = new List<RenderSegment>
        {
            new(0, 1, "^A", true),
            new(1, 1, "^B", true),
        };

        IReadOnlyList<PaintSegment> segments =
            SubstitutedHighlightSegmenter.Combine(renderSegments, [], ground, settings);

        Assert.That(segments, Has.Count.EqualTo(2));
        Assert.That(segments[0].RenderedText, Is.EqualTo("^A"));
        Assert.That(segments[1].RenderedText, Is.EqualTo("^B"));
        Assert.That(segments[0].IsSubstituted, Is.True);
        Assert.That(segments[1].IsSubstituted, Is.True);
    }

    [Test]
    public void Combine_BoldItalicFromSettings_PropagateToSubstitutedSegment ()
    {
        var ground = Ground(Color.Black, Color.White);
        var settings = new ControlCharSettings
        {
            ForeColor = Color.Gray,
            Bold = true,
            Italic = true,
        };
        var renderSegments = new List<RenderSegment>
        {
            new(0, 1, "^A", true),
        };

        IReadOnlyList<PaintSegment> segments =
            SubstitutedHighlightSegmenter.Combine(renderSegments, [], ground, settings);

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0].IsBold, Is.True);
        Assert.That(segments[0].IsItalic, Is.True);
    }

    [Test]
    public void Combine_HighlightWithNoBackgroundCoversSubstituted_FallsBackToSettingsBackColor ()
    {
        var ground = Ground(Color.Black, Color.White);
        var settings = new ControlCharSettings
        {
            ForeColor = Color.Gray,
            BackColor = Color.LightBlue,
        };
        var renderSegments = new List<RenderSegment>
        {
            new(0, 1, "^A", true),
        };
        var noBgHighlight = new HighlightEntry
        {
            ForegroundColor = Color.Yellow,
            BackgroundColor = Color.Red,
            NoBackground = true,
            IsWordMatch = true,
        };
        var matches = new List<HighlightMatchEntry>
        {
            new() { StartPos = 0, Length = 1, HighlightEntry = noBgHighlight },
        };

        IReadOnlyList<PaintSegment> segments =
            SubstitutedHighlightSegmenter.Combine(renderSegments, matches, ground, settings);

        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0].IsSubstituted, Is.True);
        Assert.That(segments[0].BackColor, Is.EqualTo(Color.LightBlue));
        Assert.That(segments[0].NoBackground, Is.False);
    }

    [Test]
    public void Combine_SourceIndexIntegrity_ConcatenatedRenderedTextEqualsRendererConcatenation ()
    {
        // Mixed input with raw + substituted + raw, plus a straddling highlight.
        var ground = Ground(Color.Black, Color.White);
        var settings = new ControlCharSettings { ForeColor = Color.Gray };
        var renderSegments = new List<RenderSegment>
        {
            new(0, 3, "abc", false),
            new(3, 1, "^A", true),
            new(4, 2, "de", false),
            new(6, 1, "^B", true),
            new(7, 1, "f", false),
        };
        var wordHighlight = new HighlightEntry
        {
            ForegroundColor = Color.Yellow,
            BackgroundColor = Color.Red,
            IsWordMatch = true,
        };
        var matches = new List<HighlightMatchEntry>
        {
            new() { StartPos = 2, Length = 4, HighlightEntry = wordHighlight },
        };

        IReadOnlyList<PaintSegment> segments =
            SubstitutedHighlightSegmenter.Combine(renderSegments, matches, ground, settings);

        var rendererConcat = string.Concat(System.Linq.Enumerable.Select(renderSegments, s => s.RenderedText));
        var segmenterConcat = string.Concat(System.Linq.Enumerable.Select(segments, s => s.RenderedText));
        Assert.That(segmenterConcat, Is.EqualTo(rendererConcat));
    }
}
