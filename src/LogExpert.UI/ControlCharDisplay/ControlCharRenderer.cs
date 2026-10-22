using System;
using System.Collections.Generic;

using LogExpert.Core.Config;

namespace LogExpert.UI.ControlCharDisplay;

internal static class ControlCharRenderer
{
    public static IReadOnlyList<RenderSegment> Render (string raw, ControlCharSettings settings)
    {
        if (raw.Length == 0)
        {
            return [];
        }

        if (!settings.Substitute)
        {
            return [new RenderSegment(0, raw.Length, raw, false)];
        }

        var segments = new List<RenderSegment>();
        int rawRunStart = 0;

        for (int i = 0; i < raw.Length; i++)
        {
            int codepoint = raw[i];
            if (!settings.EnabledCodepoints.Contains(codepoint))
            {
                continue;
            }

            if (i > rawRunStart)
            {
                segments.Add(new RenderSegment(
                    rawRunStart,
                    i - rawRunStart,
                    raw.Substring(rawRunStart, i - rawRunStart),
                    false));
            }

            string rendered = ControlCharStyleFormatter.Format(codepoint, settings.Style);
            segments.Add(new RenderSegment(i, 1, rendered, true));
            rawRunStart = i + 1;
        }

        if (rawRunStart < raw.Length)
        {
            segments.Add(new RenderSegment(
                rawRunStart,
                raw.Length - rawRunStart,
                raw[rawRunStart..],
                false));
        }

        return segments;
    }

    /// <summary>
    /// Cheap pre-scan used by paint code to decide whether to take the substitution path.
    /// Returns true when at least one character in <paramref name="text"/> is contained in
    /// <paramref name="enabledCodepoints"/>.
    /// </summary>
    public static bool HasAnyEnabledCodepoint (ReadOnlySpan<char> text, HashSet<int> enabledCodepoints)
    {
        if (enabledCodepoints is null || enabledCodepoints.Count == 0)
        {
            return false;
        }

        foreach (char c in text)
        {
            if (enabledCodepoints.Contains(c))
            {
                return true;
            }
        }

        return false;
    }
}
