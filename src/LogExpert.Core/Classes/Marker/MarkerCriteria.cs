using System.Drawing;

using ColumnizerLib;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Entities;

namespace LogExpert.Core.Classes.Marker;

/// <summary>Snapshot of the matching and visual portions of marker criteria. Never invokes triggers.</summary>
public sealed class MarkerCriteria
{
    private readonly HighlightEntry[] _entries;
    private HighlightEntry? _search;

    public bool IsEmpty => _search == null && !_entries.Any(IsVisual);

    private MarkerCriteria (HighlightEntry[] entries)
    {
        _entries = entries;
    }

    public static MarkerCriteria ForHighlights (IEnumerable<HighlightEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return new MarkerCriteria(entries.Select(entry => (HighlightEntry)entry.Clone()).ToArray());
    }

    public static MarkerCriteria ForSearch (SearchParams search, int colorArgb)
    {
        ArgumentNullException.ThrowIfNull(search);
        return new MarkerCriteria([])
        {
            _search = string.IsNullOrEmpty(search.SearchText) ? null : new HighlightEntry
            {
                SearchText = search.SearchText,
                IsRegex = search.IsRegex,
                IsCaseSensitive = search.IsCaseSensitive,
                BackgroundColor = Color.FromArgb(colorArgb)
            }
        };
    }

    public MarkerLine? Match (int lineNumber, ILogLineMemory line,
        Func<int, ILogLineMemory, IReadOnlyList<ITextValueMemory>>? getColumns = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(line);
        cancellationToken.ThrowIfCancellationRequested();
        if (_search != null)
        {
            var matched = _search.IsRegex
                ? _search.Regex.IsMatch(line.FullLine.Span)
                : line.FullLine.Span.Contains(_search.SearchText,
                    _search.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return matched ? new MarkerLine(lineNumber, _search.BackgroundColor.ToArgb()) : null;
        }

        IReadOnlyList<ITextValueMemory>? columns = null;
        for (var priority = 0; priority < _entries.Length; priority++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = _entries[priority];
            if (!IsVisual(entry))
            {
                continue;
            }

            bool matched;
            if (entry.IsWordMatch)
            {
                columns ??= getColumns?.Invoke(lineNumber, line)
                    ?? [new Column { FullValue = line.FullLine }];
                matched = columns.Any(column => HasVisibleWordMatch(entry, column));
            }
            else
            {
                matched = HighlightEvaluator.IsMatch(entry, line);
            }

            if (matched)
            {
                int? color = HasBackground(entry) ? entry.BackgroundColor.ToArgb()
                    : HasForeground(entry) ? entry.ForegroundColor.ToArgb() : null;
                return new MarkerLine(lineNumber, color, priority);
            }
        }

        return null;
    }

    private static bool IsVisual (HighlightEntry entry)
    {
        return !entry.IsSearchHit && (HasBackground(entry)
            || HasForeground(entry) || entry.IsBold);
    }

    private static bool HasBackground (HighlightEntry entry)
    {
        return (!entry.IsWordMatch || !entry.NoBackground) && entry.BackgroundColor.A > 0;
    }

    private static bool HasForeground (HighlightEntry entry)
    {
        return entry.ForegroundColor.A > 0;
    }

    private static bool HasVisibleWordMatch (HighlightEntry entry, ITextValueMemory column)
    {
        // Word highlighting uses Regex even for literal text, against each displayed column.
        foreach (var match in entry.Regex.EnumerateMatches(column.Text.Span))
        {
            if (match.Length > 0)
            {
                return true;
            }
        }

        return false;
    }
}