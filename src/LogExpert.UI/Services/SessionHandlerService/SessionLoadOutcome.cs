using LogExpert.Core.Classes.Persister;

namespace LogExpert.UI.Services.SessionHandlerService;

internal sealed class SessionLoadOutcome
{
    public enum LoadStatus
    {
        Success,
        NeedsIntervention,
        Error,
        EmptySession
    }

    public required LoadStatus Status { get; init; }

    public SessionData? SessionData { get; init; }

    public SessionValidationResult? ValidationResult { get; init; }

    public string? LayoutXml { get; init; }

    public bool HasLayoutData => !string.IsNullOrEmpty(LayoutXml);

    public string? ErrorMessage { get; init; }
}