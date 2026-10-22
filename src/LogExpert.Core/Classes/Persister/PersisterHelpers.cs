using System.Collections.ObjectModel;

using LogExpert.Core.Helpers;
using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Persister;

public static class PersisterHelpers
{

    private const string LOCAL_FILE_SYSTEM_NAME = "LocalFileSystem";

    /// <summary>
    /// Resolves a shortcut to the file it points at, then checks if that file is a settings file
    /// (.lxp). If so, the contained logfile name is returned. If not, the given file name is
    /// returned unchanged.
    /// </summary>
    /// <param name="fileName">The file name to resolve</param>
    /// <param name="pluginRegistry">Plugin registry for file system resolution (optional)</param>
    /// <returns>The resolved log file path</returns>
    public static string FindFilenameForSettings (string fileName, IPluginRegistry pluginRegistry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        // A shortcut is not a log file: resolve it first, so the rest of this method — the .lxp
        // lookup included — sees the file the user actually meant. Anything unresolvable comes back
        // unchanged and keeps the previous behaviour.
        fileName = ShellLinkResolver.Resolve(fileName);

        if (fileName.EndsWith(".lxp", StringComparison.OrdinalIgnoreCase))
        {
            var persistenceData = Persister.Load(fileName);
            if (persistenceData == null)
            {
                return fileName;
            }

            if (!string.IsNullOrEmpty(persistenceData.FileName))
            {
                if (pluginRegistry != null)
                {
                    var fs = pluginRegistry.FindFileSystemForUri(persistenceData.FileName);
                    // Use file system plugin for non-local files (network, SFTP, etc.)
                    if (fs != null && fs.GetType().Name != LOCAL_FILE_SYSTEM_NAME)
                    {
                        return persistenceData.FileName;
                    }
                }

                // Handle rooted paths (absolute paths)
                if (Path.IsPathRooted(persistenceData.FileName))
                {
                    return persistenceData.FileName;
                }

                // Handle relative paths in .lxp files
                var dir = Path.GetDirectoryName(fileName);
                return Path.Join(dir, persistenceData.FileName);
            }
        }

        return fileName;
    }

    public static ReadOnlyCollection<string> FindFilenameForSettings (ReadOnlyCollection<string> fileNames, IPluginRegistry pluginRegistry)
    {
        ArgumentNullException.ThrowIfNull(fileNames);

        var foundFiles = new List<string>(fileNames.Count);

        foreach (var fileName in fileNames)
        {
            foundFiles.Add(FindFilenameForSettings(fileName, pluginRegistry));
        }

        return foundFiles.AsReadOnly();
    }
}
