using ColumnizerLib;

namespace LogExpert.UI.Services.FileOperationService;

/// <summary>
/// Parameter object for file tab creation. Replaces the 6 positional parameters on AddFileTab.
/// </summary>
internal sealed record FileTabRequest
{
    /// <summary>
    /// The file name as provided by the caller (may be relative, may be a .lxp settings file).
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>Optional one-based line to select after loading and Session File restoration.</summary>
    public int? TargetLine { get; init; }

    /// <summary>
    /// Whether this is a temporary file (filter results, clipboard paste).
    /// </summary>
    public bool IsTempFile { get; init; }

    /// <summary>
    /// Display title for temp file tabs. Ignored for non-temp files.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Forces loading of persistence data (session restore).
    /// </summary>
    public bool ForcePersistenceLoading { get; init; }

    /// <summary>
    /// Columnizer to apply before loading. Used for filter tabs.
    /// </summary>
    public ILogLineMemoryColumnizer? PreProcessColumnizer { get; init; }

    /// <summary>
    /// If true, the window is tracked but not added to the DockPanel (deferred loading for layout restore).
    /// </summary>
    public bool DoNotAddToDockPanel { get; init; }
}