using ColumnizerLib;

namespace LogExpert.UI.Services.ToolLaunchService;

internal readonly record struct ToolLaunchResult
{
    public bool HasError { get; init; }

    public string? ErrorMessage { get; init; }

    public string? PipeFileName { get; init; }

    public ILogLineMemoryColumnizer? Columnizer { get; init; }
}
