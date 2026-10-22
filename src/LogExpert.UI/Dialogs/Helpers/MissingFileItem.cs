namespace LogExpert.UI.Dialogs.Helpers;

/// <summary>
/// Represents a file item in the Missing Files Dialog ListView.
/// </summary>
/// <param name="originalPath">Original path from session file</param>
/// <param name="status">Current file status</param>
public class MissingFileItem (string originalPath, FileStatus status)
{
    /// <summary>
    /// Original file path from the session/project file.
    /// </summary>
    public string OriginalPath { get; set; } = originalPath;

    /// <summary>
    /// Current status of the file.
    /// </summary>
    public FileStatus Status { get; set; } = status;

    /// <summary>
    /// List of alternative paths that might be the same file.
    /// </summary>
    public List<string> Alternatives { get; set; } = [];

    /// <summary>
    /// Currently selected path (original or alternative).
    /// </summary>
    public string SelectedPath { get; set; } = originalPath;

    /// <summary>
    /// Indicates whether the file is accessible.
    /// </summary>
    public bool IsAccessible => Status is FileStatus.Valid or FileStatus.AlternativeSelected;

    /// <summary>
    /// Gets the display name for the ListView (just the filename).
    /// </summary>
    public string DisplayName => Path.GetFileName(OriginalPath) ?? OriginalPath;

    /// <summary>
    /// Gets the status text for display.
    /// </summary>
    public string StatusText => Status switch
    {
        FileStatus.Valid => "Found",
        FileStatus.MissingWithAlternatives => $"Missing ({Alternatives.Count} alternatives)",
        FileStatus.Missing => "Missing",
        FileStatus.AlternativeSelected => "Alternative Selected",
        _ => "Unknown"
    };
}
