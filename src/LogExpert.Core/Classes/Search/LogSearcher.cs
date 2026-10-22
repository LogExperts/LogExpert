using System.Text.RegularExpressions;

using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Search;

/// <summary>
/// Owner of Log Search execution: direction resolution, the wrap-around-once rule, and
/// regex/ordinal matching over an <see cref="ILogfileReader"/>.
/// Pure — no dependency on controls or status lines; progress and the wrap event are
/// reported through a progress sink, cancellation through a token.
/// </summary>
public static class LogSearcher
{
    /// <summary>Lines scanned between two progress reports.</summary>
    private const int PROGRESS_REPORT_MODULO = 1000;

    /// <summary>
    /// Resolves the effective direction of a search from the forward / find-next / Shift+F3 flags.
    /// </summary>
    public static SearchDirection ResolveDirection (SearchParams searchParams)
    {
        ArgumentNullException.ThrowIfNull(searchParams);

        return (searchParams.IsForward || searchParams.IsFindNext) && !searchParams.IsShiftF3Pressed
            ? SearchDirection.Forward
            : SearchDirection.Backward;
    }

    /// <summary>
    /// Runs the search and returns how it ended. Never throws for user input errors —
    /// an uncompilable regex yields <see cref="SearchOutcome.InvalidPattern"/>.
    /// </summary>
    public static SearchResult Find (SearchParams searchParams, ILogfileReader reader, CancellationToken cancellationToken, IProgress<SearchProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(searchParams);
        ArgumentNullException.ThrowIfNull(reader);

        if (string.IsNullOrEmpty(searchParams.SearchText))
        {
            return new SearchResult(SearchOutcome.NotFound, -1, false);
        }

        // Snapshot: the caller may share this instance (F3 mutates it while a search runs).
        var search = new SearchParams();
        search.CopyFrom(searchParams);

        var isForward = ResolveDirection(search) == SearchDirection.Forward;

        var lineNum = search.IsFromTop && !search.IsFindNext
            ? 0
            : search.CurrentLine;

        var comparison = search.IsCaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        // Compiled once per search, not once per line; a bad pattern fails before any line is read.
        Regex? regex = null;
        if (search.IsRegex)
        {
            try
            {
                regex = new Regex(search.SearchText, search.IsCaseSensitive
                    ? RegexOptions.None
                    : RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                return new SearchResult(SearchOutcome.InvalidPattern, -1, false);
            }
        }

        var hasWrapped = false;
        var linesScanned = 0;

        while (true)
        {
            if (isForward ? lineNum >= reader.LineCount : lineNum < 0)
            {
                if (hasWrapped)
                {
                    return new SearchResult(SearchOutcome.NotFound, -1, true);
                }

                hasWrapped = true;
                lineNum = isForward ? 0 : reader.LineCount - 1;
                linesScanned = 0;
                progress?.Report(new SearchProgress(0, isForward ? SearchWrap.ToStart : SearchWrap.ToEnd));
            }

            var line = reader.GetLogLineMemory(lineNum);
            if (line == null)
            {
                return new SearchResult(SearchOutcome.NotFound, -1, hasWrapped);
            }

            var isMatch = regex != null
                ? regex.IsMatch(line.FullLine.ToString())
                : line.FullLine.Span.Contains(search.SearchText, comparison);

            if (isMatch)
            {
                return new SearchResult(SearchOutcome.Found, lineNum, hasWrapped);
            }

            lineNum = isForward ? lineNum + 1 : lineNum - 1;

            // Checked after the match test, matching the original loop: a search whose
            // current line matches returns the hit even if cancellation was requested.
            if (cancellationToken.IsCancellationRequested)
            {
                return new SearchResult(SearchOutcome.Cancelled, -1, hasWrapped);
            }

            if (++linesScanned % PROGRESS_REPORT_MODULO == 0)
            {
                progress?.Report(new SearchProgress(linesScanned, SearchWrap.None));
            }
        }
    }
}
