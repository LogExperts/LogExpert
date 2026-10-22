namespace LogExpert.UI.Services.SessionHandlerService;

internal readonly record struct MissingFilesResolution
{
    public bool CloseAllTabs { get; init; }

    public bool OpenInNewWindow { get; init; }

    public bool UpdateSessionFile { get; init; }

    public IReadOnlyDictionary<string, string>? SelectedAlternatives { get; init; }
}