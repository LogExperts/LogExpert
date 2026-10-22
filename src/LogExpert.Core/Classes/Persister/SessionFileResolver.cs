using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Linq;

using LogExpert.Core.Enums;
using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Persister;

/// <summary>
/// Helper class to resolve Session file references to actual log files.
/// Handles Session File (.lxp) entries by extracting the actual log file path.
/// </summary>
public static class SessionFileResolver
{
    /// <summary>
    /// Resolves Session file names to actual log files.
    /// If a file is a Session File (.lxp) entry, extracts the log file path from it.
    /// </summary>
    /// <param name="sessionData">The Session data containing file references</param>
    /// <param name="pluginRegistry">Plugin registry for file system resolution (optional)</param>
    /// <returns>List of tuples containing (logFilePath, originalFilePath)</returns>
    public static ReadOnlyCollection<(string LogFile, string OriginalFile)> ResolveSessionFiles (SessionData sessionData, IPluginRegistry pluginRegistry = null)
    {
        ArgumentNullException.ThrowIfNull(sessionData);

        var resolved = new List<(string LogFile, string OriginalFile)>();

        foreach (var fileName in sessionData.FileNames)
        {
            var logFile = PersisterHelpers.FindFilenameForSettings(fileName, pluginRegistry);
            resolved.Add((logFile, fileName));
        }

        return resolved.AsReadOnly();
    }

    /// <summary>
    /// Recovers the log file paths from a Session's tab layout XML. The DockPanel layout names
    /// every log window in a <c>PersistString="LogWindow#&lt;path&gt;"</c> attribute, so a Session
    /// whose FileNames list is missing or empty can still be restored from its layout.
    /// </summary>
    /// <param name="tabLayoutXml">The DockPanel layout XML stored in the Session, may be null or malformed</param>
    /// <returns>The log file paths in layout order; empty if the XML is null, malformed, or names no log windows</returns>
    public static ReadOnlyCollection<string> RecoverFileNamesFromLayout (string tabLayoutXml)
    {
        if (string.IsNullOrWhiteSpace(tabLayoutXml))
        {
            return ReadOnlyCollection<string>.Empty;
        }

        var prefix = WindowTypes.LogWindow + "#";

        try
        {
            var fileNames = XDocument.Parse(tabLayoutXml)
                .Descendants("Content")
                .Select(content => (string)content.Attribute("PersistString"))
                .Where(persistString => persistString != null &&
                                        persistString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                                        persistString.Length > prefix.Length)
                .Select(persistString => persistString[prefix.Length..])
                .ToList();

            return fileNames.AsReadOnly();
        }
        catch (XmlException)
        {
            return ReadOnlyCollection<string>.Empty;
        }
    }
}
