namespace LogExpert.UI.Services.SessionHandlerService;

internal readonly record struct ContinueLoadResult
{
    public bool OpenedTabs { get; init; }

    public string[]? OpenInNewWindowFiles { get; init; }
}