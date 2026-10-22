using System.Runtime.Versioning;
using System.Text;

using LogExpert.Core.Classes.Persister;

namespace LogExpert.UI.Dialogs.Helpers;

/// <summary>
/// Temporary helper for showing missing file alerts until full dialog is implemented.
/// This provides a simple MessageBox-based notification system for Phase 1 of the implementation.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class MissingFilesMessageBox
{
    /// <summary>
    /// Shows a message box alerting the user about missing files from a project/session.
    /// </summary>
    /// <param name="validationResult">The validation result containing missing file information</param>
    /// <returns>True if user wants to continue loading valid files, false to cancel</returns>
    public static bool Show (SessionValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        var sb = new StringBuilder();
        _ = sb.AppendLine("Some files from the session could not be found:");
        _ = sb.AppendLine();

        // Show first 10 missing files
        var displayCount = Math.Min(10, validationResult.MissingFiles.Count);
        for (var i = 0; i < displayCount; i++)
        {
            var missing = validationResult.MissingFiles[i];
            _ = sb.AppendLine($"  • {Path.GetFileName(missing)}");
        }

        // If there are more than 10, show count of remaining
        if (validationResult.MissingFiles.Count > 10)
        {
            _ = sb.AppendLine($"  ... and {validationResult.MissingFiles.Count - 10} more");
        }

        _ = sb.AppendLine();
        var totalFiles = validationResult.ValidFiles.Count + validationResult.MissingFiles.Count;
        _ = sb.AppendLine($"Found: {validationResult.ValidFiles.Count} of {totalFiles} files");
        _ = sb.AppendLine();
        _ = sb.AppendLine("Do you want to load the files that were found?");

        var result = MessageBox.Show(
            sb.ToString(),
            "Missing Files",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        return result == DialogResult.Yes;
    }
}
