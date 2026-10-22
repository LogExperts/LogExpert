namespace LogExpert.UI.Dialogs.Helpers;

/// <summary>
/// Represents the result of the Missing Files Dialog interaction.
/// </summary>
public enum MissingFilesDialogResult
{
    /// <summary>
    /// User cancelled the operation.
    /// </summary>
    Cancel,

    /// <summary>
    /// Load only the valid files that were found.
    /// </summary>
    LoadValidFiles,

    /// <summary>
    /// Load valid files and update the session file with new paths.
    /// </summary>
    LoadAndUpdateSession,

    /// <summary>
    /// Close existing tabs before loading the project with layout restoration.
    /// Used when there are existing tabs open and the project has layout data.
    /// </summary>
    CloseTabsAndRestoreLayout,

    /// <summary>
    /// Open the project in a new window.
    /// Used when there are existing tabs open and the project has layout data.
    /// </summary>
    OpenInNewWindow,

    /// <summary>
    /// Ignore the layout data and just load the files.
    /// Used when there are existing tabs open and the project has layout data.
    /// </summary>
    IgnoreLayout,

    /// <summary>
    /// Show a message box with information about the missing files.
    /// </summary>
    ShowMissingFilesMessage,

    /// <summary>
    /// Retry loading the files after resolving the issues.
    /// </summary>
    RetryLoadFiles,

    /// <summary>
    /// Skip the missing files and continue with the operation.
    /// </summary>
    SkipMissingFiles
}
