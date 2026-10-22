using System.Collections.Generic;
using System.Drawing;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Config;

namespace LogExpert.UI.ControlCharDisplay;

internal static class SubstitutedHighlightSegmenter
{
    /// <summary>
    /// Combines the render-segment list from <see cref="ControlCharRenderer"/> with the
    /// highlight match list to produce a paint-ready sequence of <see cref="PaintSegment"/>s,
    /// where each substituted render segment becomes an atomic paint segment whose
    /// foreground/font derive from <paramref name="settings"/> and whose background is
    /// inherited from the controlling highlight entry.
    /// </summary>
    public static IReadOnlyList<PaintSegment> Combine (
        IReadOnlyList<RenderSegment> renderSegments,
        IEnumerable<HighlightMatchEntry> highlightMatches,
        HighlightEntry groundEntry,
        ControlCharSettings settings)
    {
        // Total source length is the sum of source lengths in the render segments.
        int totalSourceLength = 0;
        for (int i = 0; i < renderSegments.Count; i++)
        {
            totalSourceLength += renderSegments[i].SourceLength;
        }

        // Build the per-source-char controlling-highlight array (mirrors the existing
        // MergeHighlightMatchEntries algorithm — ground entry by default, overwritten by
        // word-mode matches).
        var perChar = new HighlightEntry[totalSourceLength];
        for (int i = 0; i < perChar.Length; i++)
        {
            perChar[i] = groundEntry;
        }

        foreach (var match in highlightMatches)
        {
            if (!match.HighlightEntry.IsWordMatch)
            {
                continue;
            }

            int end = match.StartPos + match.Length;
            for (int i = match.StartPos; i < end && i < perChar.Length; i++)
            {
                perChar[i] = match.HighlightEntry;
            }
        }

        var result = new List<PaintSegment>();
        foreach (var seg in renderSegments)
        {
            if (seg.IsSubstituted)
            {
                result.Add(BuildSubstitutedSegment(seg, perChar, settings));
            }
            else
            {
                EmitRawSegments(seg, perChar, result);
            }
        }

        return result;
    }

    private static PaintSegment BuildSubstitutedSegment (
        RenderSegment seg,
        HighlightEntry[] perChar,
        ControlCharSettings settings)
    {
        var controlling = perChar[seg.SourceStart];
        // When the controlling highlight suppresses background painting, fall back to the
        // substitution settings background. Otherwise inherit the highlight background so
        // a substituted glyph sits inside a coherent highlight band.
        bool useHighlightBack = !controlling.NoBackground && controlling.BackgroundColor != Color.Empty;
        var backColor = useHighlightBack ? controlling.BackgroundColor : settings.BackColor;
        bool noBackground = !useHighlightBack && settings.BackColor == Color.Empty;
        return new PaintSegment(
            RenderedText: seg.RenderedText,
            ForeColor: settings.ForeColor,
            BackColor: backColor,
            IsBold: settings.Bold,
            IsItalic: settings.Italic,
            NoBackground: noBackground,
            IsSubstituted: true);
    }

    private static void EmitRawSegments (
        RenderSegment seg,
        HighlightEntry[] perChar,
        List<PaintSegment> result)
    {
        int start = seg.SourceStart;
        int end = seg.SourceStart + seg.SourceLength;
        int runStart = start;
        var runEntry = perChar[start];

        for (int pos = start + 1; pos < end; pos++)
        {
            if (!ReferenceEquals(perChar[pos], runEntry))
            {
                result.Add(BuildRawSegment(seg, runStart, pos, runEntry));
                runStart = pos;
                runEntry = perChar[pos];
            }
        }

        result.Add(BuildRawSegment(seg, runStart, end, runEntry));
    }

    private static PaintSegment BuildRawSegment (
        RenderSegment seg,
        int absoluteStart,
        int absoluteEnd,
        HighlightEntry controlling)
    {
        // RenderedText for raw segments is the slice of the original raw input that the
        // RenderSegment captured. Slice by the relative offset.
        int relStart = absoluteStart - seg.SourceStart;
        int length = absoluteEnd - absoluteStart;
        string text = length == seg.SourceLength
            ? seg.RenderedText
            : seg.RenderedText.Substring(relStart, length);

        return new PaintSegment(
            RenderedText: text,
            ForeColor: controlling.ForegroundColor,
            BackColor: controlling.BackgroundColor,
            IsBold: controlling.IsBold,
            IsItalic: false,
            NoBackground: controlling.NoBackground,
            IsSubstituted: false);
    }
}
