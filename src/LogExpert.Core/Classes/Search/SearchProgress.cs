namespace LogExpert.Core.Classes.Search;

/// <summary>
/// Progress report from a running Log Search.
/// </summary>
/// <param name="LinesScanned">Lines scanned in the current segment (resets when the search wraps),
/// suitable for driving a progress bar whose maximum is the line count.</param>
/// <param name="Wrap">The wrap event, when this report announces one.</param>
public readonly record struct SearchProgress (int LinesScanned, SearchWrap Wrap);
