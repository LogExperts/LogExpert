namespace LogExpert.UI.Services.FileOperationService;

/// <summary>
/// Result of multi-file load decision logic. Describes the decision, not the UI action.
/// </summary>
internal enum MultiFileDecision
{
    /// <summary>Open each file in its own tab.</summary>
    SingleFiles,

    /// <summary>Open all files in a single multi-file tab.</summary>
    MultiFile,

    /// <summary>User cancelled the operation.</summary>
    Cancel,

    /// <summary>User preference is Ask — caller must show a dialog and call back with the decision.</summary>
    AskUser
}