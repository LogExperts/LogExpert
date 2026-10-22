using LogExpert.Core.Classes.Persister;
using LogExpert.UI.Services.SessionHandlerService;

namespace LogExpert.UI.Interface;

internal interface ISessionHandler
{
    SessionLoadOutcome LoadSession (string sessionFileName);

    ContinueLoadResult ContinueLoad (SessionLoadOutcome loadOutcome, MissingFilesResolution? resolution, bool restoreLayout);

    bool SaveSession (string sessionFileName, SessionData sessionData, out string? errorMessage);
}