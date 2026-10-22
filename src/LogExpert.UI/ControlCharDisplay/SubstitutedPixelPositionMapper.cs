namespace LogExpert.UI.ControlCharDisplay;

/// <summary>
/// Pure pixel-to-source-index mapping for cell text containing substituted glyphs.
/// Substituted segments snap to one of their two source edges; raw segments use
/// proportional mapping within the segment width. Callers are responsible for
/// pre-measuring segment pixel widths.
/// </summary>
internal static class SubstitutedPixelPositionMapper
{
    public static int PixelToSourceIndex (
        IReadOnlyList<PaintSegment> paintSegments,
        IReadOnlyList<int> segmentPixelWidths,
        IReadOnlyList<int> segmentSourceStarts,
        IReadOnlyList<int> segmentSourceLengths,
        int pixelX)
    {
        if (paintSegments.Count == 0)
        {
            return 0;
        }

        if (pixelX <= 0)
        {
            return segmentSourceStarts[0];
        }

        int totalSourceEnd = segmentSourceStarts[^1] + segmentSourceLengths[^1];

        int cursor = 0;
        for (int i = 0; i < paintSegments.Count; i++)
        {
            int width = segmentPixelWidths[i];
            int segEnd = cursor + width;
            if (pixelX >= segEnd && i < paintSegments.Count - 1)
            {
                cursor = segEnd;
                continue;
            }

            int relX = pixelX - cursor;
            int srcStart = segmentSourceStarts[i];
            int srcLen = segmentSourceLengths[i];

            if (paintSegments[i].IsSubstituted)
            {
                // Snap to left edge when click is on left half, right edge otherwise.
                return relX * 2 < width ? srcStart : srcStart + srcLen;
            }

            // Raw segment: proportional, round to nearest character edge.
            if (width <= 0)
            {
                return srcStart;
            }

            int offset = (relX * srcLen + width / 2) / width;
            if (offset < 0)
            {
                offset = 0;
            }
            else if (offset > srcLen)
            {
                offset = srcLen;
            }

            return srcStart + offset;
        }

        return totalSourceEnd;
    }

    /// <summary>
    /// Steps the cursor by one source position in the given direction (+1 = right,
    /// -1 = left), treating substituted segments as atomic units. Raw segments behave
    /// like normal text. Result is clamped to [0, totalSourceLength].
    /// </summary>
    public static int StepSourceIndex (
        IReadOnlyList<PaintSegment> paintSegments,
        IReadOnlyList<int> segmentSourceStarts,
        IReadOnlyList<int> segmentSourceLengths,
        int currentSourceIndex,
        int direction)
    {
        if (paintSegments.Count == 0)
        {
            return 0;
        }

        int totalSourceEnd = segmentSourceStarts[^1] + segmentSourceLengths[^1];

        if (direction > 0)
        {
            if (currentSourceIndex >= totalSourceEnd)
            {
                return totalSourceEnd;
            }

            // Find the segment that begins exactly at or contains currentSourceIndex
            // such that stepping forward exits it.
            for (int i = 0; i < paintSegments.Count; i++)
            {
                int segStart = segmentSourceStarts[i];
                int segEnd = segStart + segmentSourceLengths[i];

                if (currentSourceIndex < segEnd)
                {
                    return paintSegments[i].IsSubstituted
                        ? segEnd
                        : currentSourceIndex + 1;
                }
            }

            return totalSourceEnd;
        }

        if (direction < 0)
        {
            if (currentSourceIndex <= 0)
            {
                return 0;
            }

            // Stepping left: find the segment ending at or containing
            // (currentSourceIndex - 1) and jump to its start when substituted.
            for (int i = paintSegments.Count - 1; i >= 0; i--)
            {
                int segStart = segmentSourceStarts[i];
                int segEnd = segStart + segmentSourceLengths[i];
                if (currentSourceIndex > segStart && currentSourceIndex <= segEnd)
                {
                    return paintSegments[i].IsSubstituted
                        ? segStart
                        : currentSourceIndex - 1;
                }
            }

            return 0;
        }

        return currentSourceIndex;
    }
}