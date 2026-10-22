using System.Runtime.Versioning;

using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Interface;
using LogExpert.UI.Services.FileOperationService;

using NLog;

namespace LogExpert.UI.Services.SessionHandlerService;

[SupportedOSPlatform("windows")]
internal sealed class SessionHandler (
    IPluginRegistry pluginRegistry,
    Func<FileTabRequest, LogWindow> addFileTab,
    Action closeAllTabs)
    : ISessionHandler
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public SessionLoadOutcome LoadSession (string sessionFileName)
    {
        try
        {
            if (!File.Exists(sessionFileName))
            {
                _logger.Warn("LoadSession: File does not exist: {FileName}", sessionFileName);

                return new SessionLoadOutcome
                {
                    Status = SessionLoadOutcome.LoadStatus.Error,
                    ErrorMessage = $"Project file not found: {sessionFileName}"
                };
            }

            var loadResult = SessionPersister.LoadSessionData(sessionFileName, pluginRegistry);

            if (loadResult?.SessionData == null)
            {
                _logger.Warn("LoadSession: SessionData is null for {FileName}", sessionFileName);

                return new SessionLoadOutcome
                {
                    Status = SessionLoadOutcome.LoadStatus.Error,
                    ErrorMessage = Resources.LoadSession_UI_Message_Error_FileMaybeCorruptedOrInaccessible
                };
            }

            var sessionData = loadResult.SessionData;

            if (sessionData.FileNames.Count == 0)
            {
                _logger.Warn("LoadSession: No files in project {FileName}", sessionFileName);

                return new SessionLoadOutcome
                {
                    Status = SessionLoadOutcome.LoadStatus.EmptySession,
                    ErrorMessage = Resources.LoadSession_UI_Message_Message_FilesForSessionCouldNotBeFound
                };
            }

            var layoutXml = sessionData.TabLayoutXml;

            return loadResult.RequiresUserIntervention
                ? new SessionLoadOutcome
                {
                    Status = SessionLoadOutcome.LoadStatus.NeedsIntervention,
                    SessionData = sessionData,
                    ValidationResult = loadResult.ValidationResult,
                    LayoutXml = layoutXml
                }
                : new SessionLoadOutcome
                {
                    Status = SessionLoadOutcome.LoadStatus.Success,
                    SessionData = sessionData,
                    LayoutXml = layoutXml
                };
        }
        catch (Exception ex) when (ex is IOException or
                                         UnauthorizedAccessException or
                                         InvalidOperationException or
                                         Newtonsoft.Json.JsonException)
        {
            _logger.Error(ex, "LoadSession: Exception loading {FileName}", sessionFileName);

            return new SessionLoadOutcome
            {
                Status = SessionLoadOutcome.LoadStatus.Error,
                ErrorMessage = $"Error loading project: {ex.Message}"
            };
        }
    }

    public ContinueLoadResult ContinueLoad (SessionLoadOutcome loadOutcome, MissingFilesResolution? resolution, bool restoreLayout)
    {
        ArgumentNullException.ThrowIfNull(loadOutcome);
        ArgumentNullException.ThrowIfNull(loadOutcome.SessionData);

        var sessionData = loadOutcome.SessionData;

        // Apply selected alternatives to file paths (in-place, exact string match)
        if (resolution?.SelectedAlternatives is { } alternatives)
        {
            for (int i = 0; i < sessionData.FileNames.Count; i++)
            {
                var originalPath = sessionData.FileNames[i];
                if (alternatives.TryGetValue(originalPath, out var replacement))
                {
                    sessionData.FileNames[i] = replacement;
                }
            }
        }

        // Save updated session file if requested
        if (resolution is { UpdateSessionFile: true } && !string.IsNullOrEmpty(sessionData.SessionFilePath))
        {
            try
            {
                SessionPersister.SaveSessionData(sessionData.SessionFilePath, sessionData);
                _logger.Info("ContinueLoad: Updated session file {FileName}", sessionData.SessionFilePath);
            }
            catch (Exception ex) when (ex is IOException or
                                             UnauthorizedAccessException or
                                             InvalidOperationException or
                                             ArgumentException)
            {
                _logger.Error(ex, "ContinueLoad: Failed to update session file {FileName}", sessionData.SessionFilePath);
            }
        }

        // Handle "Open in new window" — resolve file names but do NOT open tabs
        if (resolution is { OpenInNewWindow: true })
        {
            var resolvedFiles = PersisterHelpers.FindFilenameForSettings(
                sessionData.FileNames.AsReadOnly(), pluginRegistry);

            return new ContinueLoadResult
            {
                OpenedTabs = false,
                OpenInNewWindowFiles = [.. resolvedFiles]
            };
        }

        // Close existing tabs BEFORE opening the new session windows. The new windows are
        // tracked by the tab controller as soon as they are created (even deferred ones that
        // are not yet on the dock panel), so closing afterwards would also close them and the
        // subsequent layout restore would find nothing to dock.
        if (resolution is { CloseAllTabs: true })
        {
            closeAllTabs();
        }

        // Open file tabs
        bool deferForLayout = loadOutcome.HasLayoutData && restoreLayout;
        int openedCount = 0;

        foreach (var fileName in sessionData.FileNames)
        {
            var request = new FileTabRequest
            {
                FileName = fileName,
                ForcePersistenceLoading = true,
                DoNotAddToDockPanel = deferForLayout
            };

            try
            {
                _ = addFileTab(request);
                openedCount++;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                                         and not StackOverflowException
                                         and not AccessViolationException
                                         and not AppDomainUnloadedException
                                         and not BadImageFormatException
                                         and not CannotUnloadAppDomainException
                                         and not InvalidProgramException
                                         and not ThreadAbortException)
            {
                _logger.Error(ex, "ContinueLoad: Failed to open tab for {FileName}", fileName);
            }
        }

        return new ContinueLoadResult
        {
            OpenedTabs = openedCount > 0,
            OpenInNewWindowFiles = null
        };
    }

    public bool SaveSession (string sessionFileName, SessionData sessionData, out string? errorMessage)
    {
        try
        {
            SessionPersister.SaveSessionData(sessionFileName, sessionData);
            errorMessage = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException or
                                         UnauthorizedAccessException or
                                         InvalidOperationException or
                                         ArgumentException)
        {
            _logger.Error(ex, "SaveSession: Failed to save {FileName}", sessionFileName);
            errorMessage = $"Error saving project: {ex.Message}";
            return false;
        }
    }
}