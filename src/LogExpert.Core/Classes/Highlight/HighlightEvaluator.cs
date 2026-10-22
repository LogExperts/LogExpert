using System.Text;

using ColumnizerLib;

namespace LogExpert.Core.Classes.Highlight;

/// <summary>
/// Pure evaluation of <see cref="HighlightEntry"/> rules against a log line: which entries match and
/// which trigger actions their matches imply. This is the single home of highlight-match semantics,
/// shared by the tail trigger path (<c>LogWindow.CheckFilterAndHighlight</c>) and the bulk
/// <see cref="Bookmark.HighlightBookmarkScanner"/>.
/// <para>
/// It reports the <em>decision</em> only. Side-effecting triggers (Audio Alert, Set Bookmark,
/// Stop Tail) are fired by the caller, so the "Audio Alert fires only on the tail path" invariant
/// stays at the call site and cannot leak onto bulk/paint paths.
/// </para>
/// </summary>
public static class HighlightEvaluator
{
    /// <summary>
    /// Returns whether a single <see cref="HighlightEntry"/> matches the given line.
    /// </summary>
    public static bool IsMatch (HighlightEntry entry, ITextValueMemory line)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(line);

        if (entry.IsRegex)
        {
            return entry.Regex.IsMatch(line.Text.ToString());
        }

        var comparison = entry.IsCaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

        return line.Text.Span.Contains(entry.SearchText.AsSpan(), comparison);
    }

    /// <summary>
    /// Returns every entry in <paramref name="entries"/> that matches the line, preserving order.
    /// A null line matches nothing.
    /// </summary>
    public static IList<HighlightEntry> FindMatchingEntries (IEnumerable<HighlightEntry> entries, ITextValueMemory line)
    {
        ArgumentNullException.ThrowIfNull(entries);

        List<HighlightEntry> result = [];
        if (line == null)
        {
            return result;
        }

        foreach (var entry in entries.Where(e => IsMatch(e, line)))
        {
            result.Add(entry);
        }

        return result;
    }

    /// <summary>
    /// Returns whether any hide-line entry matches the line. Other matching entries cannot cancel the decision,
    /// whatever their order.
    /// </summary>
    public static bool IsHidden (IEnumerable<HighlightEntry> entries, ITextValueMemory line)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(line);

        return entries.Any(e => IsHideRule(e) && IsMatch(e, line));
    }

    /// <summary>Returns whether the entry hides the lines it matches; temporary search-hit entries never do.</summary>
    public static bool IsHideRule (HighlightEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.IsHideLine && !entry.IsSearchHit;
    }

    /// <summary>
    /// Classifies the non-plugin trigger actions implied by a set of matching entries: whether to
    /// suppress the dirty LED, stop tailing, set a bookmark, and the concatenated bookmark comment.
    /// Plugin and Audio Alert triggers are intentionally not handled here — they are fired by the
    /// caller.
    /// </summary>
    public static HighlightActions GetTriggerActions (IEnumerable<HighlightEntry> matchingEntries)
    {
        ArgumentNullException.ThrowIfNull(matchingEntries);

        var suppressLed = false;
        var stopTail = false;
        var setBookmark = false;
        var bookmarkCommentBuilder = new StringBuilder();

        foreach (var entry in matchingEntries)
        {
            if (entry.IsLedSwitch)
            {
                suppressLed = true;
            }

            if (entry.IsSetBookmark)
            {
                setBookmark = true;
                if (!string.IsNullOrEmpty(entry.BookmarkComment))
                {

                    _ = bookmarkCommentBuilder.Append(entry.BookmarkComment);
                    _ = bookmarkCommentBuilder.Append("\r\n");
                }
            }

            if (entry.IsStopTail)
            {
                stopTail = true;
            }
        }

        var bookmarkComment = bookmarkCommentBuilder.ToString().TrimEnd(['\r', '\n']);

        return new HighlightActions(suppressLed, stopTail, setBookmark, bookmarkComment);
    }
}

/// <summary>
/// The non-plugin trigger decision for a line: which LED/tail/bookmark side effects its matching
/// entries imply. Carries no side effects of its own — the caller acts on it.
/// </summary>
public readonly record struct HighlightActions (bool SuppressLed, bool StopTail, bool SetBookmark, string BookmarkComment);
