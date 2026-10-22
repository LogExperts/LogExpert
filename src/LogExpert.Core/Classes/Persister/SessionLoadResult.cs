namespace LogExpert.Core.Classes.Persister;

/// <summary>
/// Represents the result of loading a project file, including validation information.
/// </summary>
public class SessionLoadResult
{
    /// <summary>
    /// The loaded project data (contains resolved log file paths).
    /// </summary>
    public SessionData SessionData { get; set; }

    /// <summary>
    /// Validation result containing valid, missing, and alternative file paths.
    /// </summary>
    public SessionValidationResult ValidationResult { get; set; }

    /// <summary>
    /// Mapping of original file references to resolved log files.
    /// Key: resolved log file path (.log)
    /// Value: original file reference (.lxp or .log)
    /// Used to update persistence files when user selects alternatives.
    /// </summary>
    public Dictionary<string, string> LogToOriginalFileMapping { get; set; } = [];

    /// <summary>
    /// Indicates whether the project has at least one valid file to load.
    /// </summary>
    public bool HasValidFiles => ValidationResult?.ValidFiles.Count > 0;

    /// <summary>
    /// Indicates whether user intervention is needed due to missing files.
    /// </summary>
    public bool RequiresUserIntervention => ValidationResult?.HasMissingFiles ?? false;
}