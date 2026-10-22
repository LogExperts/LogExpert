namespace LogExpert.Core.Classes.Search;

/// <summary>
/// How a Log Search run ended.
/// </summary>
public enum SearchOutcome
{
    /// <summary>A matching line was found.</summary>
    Found,

    /// <summary>No matching line exists (including after a full wrap-around), or the search text was empty.</summary>
    NotFound,

    /// <summary>The search was cancelled before a match was found.</summary>
    Cancelled,

    /// <summary>The search text was a regular expression that does not compile. No line was read.</summary>
    InvalidPattern,
}
