using System.Text;

using LogExpert.Core.Interfaces;

using Newtonsoft.Json;

using NLog;

namespace LogExpert.Core.Classes.Persister;

public static class SessionPersister
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    #region Public methods

    /// <summary>
    /// Loads a Session (.lxj) from disk, including validation of referenced files.
    /// Resolves Session File (.lxp) entries to actual .log files before validation.
    /// </summary>
    /// <param name="sessionFileName">The path to the Session file (.lxj)</param>
    /// <param name="pluginRegistry">The plugin registry for file system validation</param>
    /// <returns>A <see cref="SessionLoadResult"/> containing the session data and validation results</returns>
    public static SessionLoadResult LoadSessionData (string sessionFileName, IPluginRegistry pluginRegistry)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            };

            var json = File.ReadAllText(sessionFileName, Encoding.UTF8);
            var sessionData = JsonConvert.DeserializeObject<SessionData>(json, settings);

            // Set Session file path for alternative file search
            sessionData.SessionFilePath = sessionFileName;
            sessionData.FileNames ??= [];

            // v1.42.0 wrote Sessions with an empty FileNames list (issue #694): the save path
            // enumerated DockPanelSuite's DisplayingContents with foreach, which yields nothing.
            // The tab layout XML in those files still names every log window, so recover from there.
            if (sessionData.FileNames.Count == 0)
            {
                var recovered = SessionFileResolver.RecoverFileNamesFromLayout(sessionData.TabLayoutXml);

                if (recovered.Count > 0)
                {
                    _logger.Warn($"Session {sessionFileName} has an empty file list; recovered {recovered.Count} file(s) from the tab layout");
                    sessionData.FileNames = [.. recovered];
                }
            }

            // Resolve Session File (.lxp) entries to actual .log files
            var resolvedFiles = SessionFileResolver.ResolveSessionFiles(sessionData, pluginRegistry);

            // Create mapping: logFile → originalFile
            var logToOriginalMapping = new Dictionary<string, string>();
            foreach (var (logFile, originalFile) in resolvedFiles)
            {
                logToOriginalMapping[logFile] = originalFile;
            }

            // Create new SessionData with resolved log file paths
            var resolvedSessionData = new SessionData
            {
                FileNames = [.. resolvedFiles.Select(r => r.LogFile)],
                TabLayoutXml = sessionData.TabLayoutXml,
                SessionFilePath = sessionData.SessionFilePath
            };

            // Validate the actual log files (not Session File .lxp entries)
            var validationResult = SessionFileValidator.ValidateSession(resolvedSessionData, pluginRegistry);

            return new SessionLoadResult
            {
                SessionData = resolvedSessionData,
                ValidationResult = validationResult,
                LogToOriginalFileMapping = logToOriginalMapping
            };
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or
                                         IOException or
                                         JsonSerializationException)
        {
            _logger.Warn($"Error loading session data from {sessionFileName}, trying old xml version");

            var sessionData = SessionPersisterXML.LoadSessionData(sessionFileName);

            // Set Session file path for alternative file search
            sessionData.SessionFilePath = sessionFileName;

            // Resolve Session File (.lxp) entries for XML fallback as well
            var resolvedFiles = SessionFileResolver.ResolveSessionFiles(sessionData, pluginRegistry);

            var logToOriginalMapping = new Dictionary<string, string>();
            foreach (var (logFile, originalFile) in resolvedFiles)
            {
                logToOriginalMapping[logFile] = originalFile;
            }

            var resolvedSessionData = new SessionData
            {
                FileNames = [.. resolvedFiles.Select(r => r.LogFile)],
                TabLayoutXml = sessionData.TabLayoutXml,
                SessionFilePath = sessionData.SessionFilePath
            };

            var validationResult = SessionFileValidator.ValidateSession(resolvedSessionData, pluginRegistry);

            return new SessionLoadResult
            {
                SessionData = resolvedSessionData,
                ValidationResult = validationResult,
                LogToOriginalFileMapping = logToOriginalMapping
            };
        }
    }

    /// <summary>
    /// Saves the specified Session data to a file in JSON format.
    /// </summary>
    /// <remarks>The method serializes the <paramref name="sessionData"/> into a JSON string with indented
    /// formatting and writes it to the specified <paramref name="sessionFileName"/> using UTF-8 encoding.</remarks>
    /// <param name="sessionFileName">The path to the file where the Session data will be saved. Cannot be null or empty.</param>
    /// <param name="sessionData">The Session data to be serialized and saved. Cannot be null.</param>
    public static void SaveSessionData (string sessionFileName, SessionData sessionData)
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
        };

        try
        {
            var json = JsonConvert.SerializeObject(sessionData, settings);
            File.WriteAllText(sessionFileName, json, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is JsonSerializationException or
                                         UnauthorizedAccessException or
                                         IOException)
        {
            _logger.Error(ex, $"Error saving session data to {sessionFileName}");
        }
    }

    #endregion
}
