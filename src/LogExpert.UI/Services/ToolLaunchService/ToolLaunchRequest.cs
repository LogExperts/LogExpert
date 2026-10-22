namespace LogExpert.UI.Services.ToolLaunchService;

internal sealed record ToolLaunchRequest
{
    public required string Cmd { get; init; }

    public string Args { get; init; } = string.Empty;

    public bool SysoutPipe { get; init; }

    public string? ColumnizerName { get; init; }

    public string? WorkingDir { get; init; }
}
