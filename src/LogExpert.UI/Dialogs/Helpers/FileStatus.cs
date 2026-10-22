namespace LogExpert.UI.Dialogs.Helpers;

/// <summary>
/// Represents the status of a file in the missing files dialog.
/// </summary>
public enum FileStatus
{
    /// <summary>
    /// File exists and is accessible.
    /// </summary>
    Valid,

    /// <summary>
    /// File is missing but alternatives are available.
    /// </summary>
    MissingWithAlternatives,

    /// <summary>
    /// File is missing and no alternatives found.
    /// </summary>
    Missing,

    /// <summary>
    /// User has manually selected an alternative path.
    /// </summary>
    AlternativeSelected
}
