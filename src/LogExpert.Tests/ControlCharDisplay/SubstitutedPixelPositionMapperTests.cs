using LogExpert.UI.ControlCharDisplay;

using NUnit.Framework;

namespace LogExpert.Tests.ControlCharDisplay;

[TestFixture]
public class SubstitutedPixelPositionMapperTests
{
    private static PaintSegment Raw (string text) =>
        new(text, Color.Black, Color.White, IsBold: false, IsItalic: false, NoBackground: false, IsSubstituted: false);

    private static PaintSegment Sub (string text) =>
        new(text, Color.Gray, Color.Empty, IsBold: false, IsItalic: false, NoBackground: true, IsSubstituted: true);

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void PixelToSourceIndex_AllRawSingleSegment_ProportionalMappingMatchesPosition ()
    {
        // Single raw segment "abcde" — 5 source chars, 50 px wide (10 px/char).
        var segments = new List<PaintSegment> { Raw("abcde") };
        var widths = new[] { 50 };
        var sourceStarts = new[] { 0 };
        var sourceLengths = new[] { 5 };

        Assert.That(
            SubstitutedPixelPositionMapper.PixelToSourceIndex(segments, widths, sourceStarts, sourceLengths, 0),
            Is.EqualTo(0));
        Assert.That(
            SubstitutedPixelPositionMapper.PixelToSourceIndex(segments, widths, sourceStarts, sourceLengths, 25),
            Is.EqualTo(3));
        Assert.That(
            SubstitutedPixelPositionMapper.PixelToSourceIndex(segments, widths, sourceStarts, sourceLengths, 50),
            Is.EqualTo(5));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void PixelToSourceIndex_ClickAtPixelZero_ReturnsZero ()
    {
        var segments = new List<PaintSegment> { Raw("abc"), Sub("^A") };
        var widths = new[] { 30, 20 };
        var sourceStarts = new[] { 0, 3 };
        var sourceLengths = new[] { 3, 1 };

        Assert.That(
            SubstitutedPixelPositionMapper.PixelToSourceIndex(segments, widths, sourceStarts, sourceLengths, 0),
            Is.EqualTo(0));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void PixelToSourceIndex_ClickBeyondLastSegment_ReturnsTotalSourceLength ()
    {
        var segments = new List<PaintSegment> { Raw("ab"), Sub("^A") };
        var widths = new[] { 20, 20 };
        var sourceStarts = new[] { 0, 2 };
        var sourceLengths = new[] { 2, 1 };

        // Pixel well past right edge of all segments.
        Assert.That(
            SubstitutedPixelPositionMapper.PixelToSourceIndex(segments, widths, sourceStarts, sourceLengths, 999),
            Is.EqualTo(3));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void PixelToSourceIndex_ClickLeftHalfOfSubstitutedSegment_ReturnsSourceStart ()
    {
        // Single substituted segment at source index 0, 1 source char, 20 px wide.
        var segments = new List<PaintSegment> { Sub("^A") };
        var widths = new[] { 20 };
        var sourceStarts = new[] { 0 };
        var sourceLengths = new[] { 1 };

        Assert.That(
            SubstitutedPixelPositionMapper.PixelToSourceIndex(segments, widths, sourceStarts, sourceLengths, 5),
            Is.EqualTo(0));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void PixelToSourceIndex_ClickRightHalfOfSubstitutedSegment_ReturnsSourceEnd ()
    {
        var segments = new List<PaintSegment> { Sub("^A") };
        var widths = new[] { 20 };
        var sourceStarts = new[] { 0 };
        var sourceLengths = new[] { 1 };

        Assert.That(
            SubstitutedPixelPositionMapper.PixelToSourceIndex(segments, widths, sourceStarts, sourceLengths, 15),
            Is.EqualTo(1));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void PixelToSourceIndex_ClickAtBoundaryBetweenAdjacentSubstituted_ReturnsStartOfSecond ()
    {
        // "\u0001\u0002" → two adjacent substituted segments, each source length 1, 20 px each.
        var segments = new List<PaintSegment> { Sub("^A"), Sub("^B") };
        var widths = new[] { 20, 20 };
        var sourceStarts = new[] { 0, 1 };
        var sourceLengths = new[] { 1, 1 };

        // Pixel 20 is the boundary; click here should resolve to source index 1 (the
        // start of the second substituted segment, equivalent to "between them").
        Assert.That(
            SubstitutedPixelPositionMapper.PixelToSourceIndex(segments, widths, sourceStarts, sourceLengths, 20),
            Is.EqualTo(1));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void StepSourceIndex_InsideRawSegment_MovesByOne ()
    {
        // Single raw "abcde" — step right from 2 → 3, step left from 2 → 1.
        var segments = new List<PaintSegment> { Raw("abcde") };
        var sourceStarts = new[] { 0 };
        var sourceLengths = new[] { 5 };

        Assert.That(
            SubstitutedPixelPositionMapper.StepSourceIndex(segments, sourceStarts, sourceLengths, 2, +1),
            Is.EqualTo(3));
        Assert.That(
            SubstitutedPixelPositionMapper.StepSourceIndex(segments, sourceStarts, sourceLengths, 2, -1),
            Is.EqualTo(1));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void StepSourceIndex_AtLeftEdgeOfSubstituted_StepRight_JumpsToRightEdge ()
    {
        // "a\u0001b" → raw a, sub ^A, raw b. From source index 1 (left edge of ^A),
        // stepping +1 lands at 2 (right edge of ^A).
        var segments = new List<PaintSegment> { Raw("a"), Sub("^A"), Raw("b") };
        var sourceStarts = new[] { 0, 1, 2 };
        var sourceLengths = new[] { 1, 1, 1 };

        Assert.That(
            SubstitutedPixelPositionMapper.StepSourceIndex(segments, sourceStarts, sourceLengths, 1, +1),
            Is.EqualTo(2));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void StepSourceIndex_AtRightEdgeOfSubstituted_StepLeft_JumpsToLeftEdge ()
    {
        var segments = new List<PaintSegment> { Raw("a"), Sub("^A"), Raw("b") };
        var sourceStarts = new[] { 0, 1, 2 };
        var sourceLengths = new[] { 1, 1, 1 };

        // From source index 2 (right edge of ^A = left edge of "b"), stepping -1 should
        // land at 1 (left edge of ^A) — the substituted glyph is a single navigation unit.
        Assert.That(
            SubstitutedPixelPositionMapper.StepSourceIndex(segments, sourceStarts, sourceLengths, 2, -1),
            Is.EqualTo(1));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void StepSourceIndex_ClampsAtZeroAndTotalLength ()
    {
        var segments = new List<PaintSegment> { Raw("abc") };
        var sourceStarts = new[] { 0 };
        var sourceLengths = new[] { 3 };

        Assert.That(
            SubstitutedPixelPositionMapper.StepSourceIndex(segments, sourceStarts, sourceLengths, 0, -1),
            Is.EqualTo(0));
        Assert.That(
            SubstitutedPixelPositionMapper.StepSourceIndex(segments, sourceStarts, sourceLengths, 3, +1),
            Is.EqualTo(3));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void StepSourceIndex_SubstitutionDisabledSingleRawSegment_AlwaysMovesByOne ()
    {
        var segments = new List<PaintSegment> { Raw("hello world") };
        var sourceStarts = new[] { 0 };
        var sourceLengths = new[] { 11 };

        for (int idx = 0; idx < 11; idx++)
        {
            Assert.That(
                SubstitutedPixelPositionMapper.StepSourceIndex(segments, sourceStarts, sourceLengths, idx, +1),
                Is.EqualTo(idx + 1),
                $"+1 step from {idx}");
        }

        for (int idx = 11; idx > 0; idx--)
        {
            Assert.That(
                SubstitutedPixelPositionMapper.StepSourceIndex(segments, sourceStarts, sourceLengths, idx, -1),
                Is.EqualTo(idx - 1),
                $"-1 step from {idx}");
        }
    }
}
