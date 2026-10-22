using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Entities;
using LogExpert.UI.Controls.LogWindow;

namespace LogExpert.UI.Services.FileOperationService;

/// <summary>
/// Event args raised after a file tab is created. Carries the original request and
/// resolved metadata so LogTabWindow can perform UI-only post-creation work
/// (tooltip, tab coloring, multi-file BeginInvoke).
/// </summary>
/// <remarks>
/// Designed to be stable as new creation variants are added. All creation context
/// is derived from the <see cref="Request"/> and optional extras, rather than
/// growing ad-hoc boolean properties.
/// </remarks>
internal sealed class FileOpenedEventArgs : EventArgs
{
    /// <summary>The created LogWindow.</summary>
    public required LogWindow LogWindow { get; init; }

    /// <summary>The original request that triggered the creation.</summary>
    public required FileTabRequest Request { get; init; }

    /// <summary>The resolved log file name (after .lxp resolution).</summary>
    public required string ResolvedFileName { get; init; }

    /// <summary>The encoding options resolved by the service.</summary>
    public EncodingOptions? EncodingOptions { get; init; }

    /// <summary>Set when AddFilterTab created this tab. LogTabWindow uses this for filter tooltip setup.</summary>
    public FilterPipe? FilterPipe { get; init; }

    /// <summary>Set when AddMultiFileTab created this tab. LogTabWindow uses this for BeginInvoke(LoadFilesAsMulti).</summary>
    public string[]? MultiFileNames { get; init; }
}