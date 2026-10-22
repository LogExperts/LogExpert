namespace LogExpert.Core.Classes.Search;

/// <summary>
/// Result of a Log Search run. The caller narrates the outcome (status line, scrolling);
/// the searcher never touches UI.
/// </summary>
/// <param name="Outcome">How the search ended.</param>
/// <param name="LineNumber">Zero-based hit line when <see cref="SearchOutcome.Found"/>; -1 otherwise.</param>
/// <param name="Wrapped">Whether the search wrapped around the file boundary before ending.</param>
public readonly record struct SearchResult (SearchOutcome Outcome, int LineNumber, bool Wrapped);
