namespace LogExpert.Core.Classes.Search;

/// <summary>
/// Wrap event reported through the progress sink, so the caller can show the
/// "started from beginning/end of file" notice while the search is still running.
/// </summary>
public enum SearchWrap
{
    /// <summary>Ordinary progress report; no wrap occurred.</summary>
    None,

    /// <summary>A forward search passed the last line and continued from line 0.</summary>
    ToStart,

    /// <summary>A backward search passed line 0 and continued from the last line.</summary>
    ToEnd,
}
