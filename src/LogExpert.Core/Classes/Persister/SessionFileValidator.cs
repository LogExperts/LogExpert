using System.Security;

using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Persister;

/// <summary>
/// Provides static methods for validating project file references, identifying missing or accessible files, and
/// suggesting alternative file paths using available file system plugins.
/// </summary>
/// <remarks>This class is intended for use with project data that includes file references and a project file
/// path. It supports validation of both local file paths and URI-based files through plugin resolution. All methods are
/// thread-safe and do not modify input data. Use the provided methods to check file existence, resolve canonical file
/// paths, and locate possible alternatives for missing files.</remarks>
public static class SessionFileValidator
{
    /// <summary>
    /// Validates the files referenced by the specified project and identifies missing or accessible files using
    /// available file system plugins.
    /// </summary>
    /// <remarks>Files are considered valid if they exist on disk or if a suitable file system plugin is
    /// available for URI-based files. For missing files, possible alternative paths are suggested based on the project
    /// file location.</remarks>
    /// <param name="sessionData">The project data containing the list of file names to validate and the project file path. Cannot be null.</param>
    /// <param name="pluginRegistry">The plugin registry used to resolve file system plugins for URI-based files. Cannot be null.</param>
    /// <returns>A SessionValidationResult containing lists of valid files, missing files, and possible alternative file paths
    /// for missing files.</returns>
    public static SessionValidationResult ValidateSession (SessionData sessionData, IPluginRegistry pluginRegistry)
    {
        ArgumentNullException.ThrowIfNull(sessionData);
        ArgumentNullException.ThrowIfNull(pluginRegistry);

        var result = new SessionValidationResult();

        // Cache drive letters once to avoid repeated expensive DriveInfo.GetDrives() calls
        var cachedDriveLetters = GetFixedDriveLetters();

        // Enumerate the session directory and its immediate subdirectories once, instead of
        // re-enumerating for every missing file. This is shared across all alternative-path lookups.
        var sessionDirectories = GetSessionSearchDirectories(sessionData.SessionFilePath);

        foreach (var fileName in sessionData.FileNames)
        {
            var normalizedPath = NormalizeFilePath(fileName);

            if (File.Exists(normalizedPath))
            {
                result.ValidFiles.Add(fileName);
            }
            else if (IsUri(fileName))
            {
                // Check if URI-based file system plugin is available
                var fs = pluginRegistry.FindFileSystemForUri(fileName);
                if (fs != null)
                {
                    result.ValidFiles.Add(fileName);
                }
                else
                {
                    result.MissingFiles.Add(fileName);
                }
            }
            else
            {
                result.MissingFiles.Add(fileName);

                var alternativePaths = FindAlternativePaths(fileName, sessionData.SessionFilePath, sessionDirectories, cachedDriveLetters);
                result.PossibleAlternatives[fileName] = alternativePaths;
            }
        }

        return result;
    }

    /// <summary>
    /// Normalizes the specified file path by resolving the actual file name if the file is a persistence file.
    /// </summary>
    /// <remarks>Use this method to obtain the canonical file path for files that may be persisted under a
    /// different name. For files that do not have a ".lxp" extension, the input path is returned unchanged.</remarks>
    /// <param name="fileName">The path of the file to normalize. If the file has a ".lxp" extension, its persisted file name will be resolved;
    /// otherwise, the original path is returned.</param>
    /// <returns>The normalized file path. If the input is a persistence file, returns the resolved file name; otherwise, returns
    /// the original file path.</returns>
    private static string NormalizeFilePath (string fileName)
    {
        if (fileName.EndsWith(".lxp", StringComparison.OrdinalIgnoreCase))
        {
            var persistenceData = Persister.Load(fileName);
            return persistenceData?.FileName ?? fileName;
        }

        return fileName;
    }

    /// <summary>
    /// Determines whether the specified string represents an absolute URI with a scheme other than "file".
    /// </summary>
    /// <remarks>This method returns false for local file paths and URIs with the "file" scheme, treating them
    /// as regular files rather than remote resources. Common URI schemes include "http", "https", "ftp", and
    /// "sftp".</remarks>
    /// <param name="fileName">The string to evaluate as a potential URI. Cannot be null, empty, or consist only of white-space characters.</param>
    /// <returns>true if the string is a valid absolute URI with a non-file scheme; otherwise, false.</returns>
    private static bool IsUri (string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName) &&
               Uri.TryCreate(fileName, UriKind.Absolute, out var uri) &&
               !string.IsNullOrEmpty(uri.Scheme) &&
               !uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the directories to search for alternative file locations: the session file's directory
    /// followed by its immediate subdirectories. Enumerated once per session so that per-missing-file
    /// lookups do not repeatedly hit the file system with the same <see cref="Directory.GetDirectories(string)"/> call.
    /// </summary>
    /// <param name="sessionFilePath">The full path to the session/project file. May be null or empty.</param>
    /// <returns>The session directory and its immediate subdirectories, or an empty list if unavailable.</returns>
    private static List<string> GetSessionSearchDirectories (string sessionFilePath)
    {
        if (string.IsNullOrWhiteSpace(sessionFilePath))
        {
            return [];
        }

        try
        {
            var sessionDir = Path.GetDirectoryName(sessionFilePath);
            if (string.IsNullOrEmpty(sessionDir) || !Directory.Exists(sessionDir))
            {
                return [];
            }

            var directories = new List<string> { sessionDir };
            directories.AddRange(Directory.GetDirectories(sessionDir));
            return directories;
        }
        catch (Exception ex) when (ex is ArgumentException or
                                        ArgumentNullException or
                                        PathTooLongException or
                                        UnauthorizedAccessException or
                                        IOException)
        {
            // Ignore errors when enumerating the session directory
            return [];
        }
    }

    /// <summary>
    /// Gets the list of fixed drive letters that are ready.
    /// Extracted to avoid repeated expensive DriveInfo.GetDrives() calls.
    /// </summary>
    private static List<char> GetFixedDriveLetters ()
    {
        try
        {
            return [.. DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .Select(d => d.Name[0])];
        }
        catch (Exception ex) when (ex is IOException or
                                         UnauthorizedAccessException or
                                         SecurityException or
                                         DriveNotFoundException or
                                         ArgumentNullException)
        {
            return [];
        }
    }

    /// <summary>
    /// Searches for alternative file paths that may correspond to the specified file name, considering common locations
    /// such as the project directory, its subdirectories, the user's Documents/LogExpert folder, alternate drive
    /// letters, and relative paths from the project directory.
    /// </summary>
    /// <remarks>This method attempts to locate files that may have been moved, renamed, or exist in typical
    /// user or project directories. It ignores errors encountered during directory or file access and does not
    /// guarantee that all possible alternative locations are checked. Duplicate paths are excluded from the
    /// result.</remarks>
    /// <param name="fileName">The name or path of the file to search for. Can be an absolute or relative path. Cannot be null, empty, or
    /// whitespace.</param>
    /// <param name="sessionFilePath">The full path to the project file used as a reference for searching related directories. Can be null or empty if
    /// project context is not available.</param>
    /// <param name="sessionDirectories">Pre-enumerated session directory and its immediate subdirectories, shared across all missing files.</param>
    /// <param name="cachedDriveLetters">Pre-computed list of fixed drive letters to avoid repeated DriveInfo.GetDrives() calls.</param>
    /// <returns>A list of strings containing the full paths of files found that match the specified file name in alternative
    /// locations. The list will be empty if no matching files are found.</returns>
    private static List<string> FindAlternativePaths (string fileName, string sessionFilePath, List<string> sessionDirectories, List<char> cachedDriveLetters)
    {
        var alternatives = new List<string>();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return alternatives;
        }

        var baseName = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(baseName))
        {
            return alternatives;
        }

        // Search in directory of .lxj project file and its immediate subdirectories (pre-enumerated once per session)
        alternatives.AddRange(
            sessionDirectories
                .Select(dir => Path.Join(dir, baseName))
                .Where(File.Exists));

        // Search in Documents/LogExpert folder
        try
        {
            var documentsPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LogExpert");

            if (Directory.Exists(documentsPath))
            {
                var docCandidate = Path.Join(documentsPath, baseName);
                if (File.Exists(docCandidate) && !alternatives.Contains(docCandidate))
                {
                    alternatives.Add(docCandidate);
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException or
                                         ArgumentNullException or
                                         PathTooLongException or
                                         UnauthorizedAccessException or
                                         IOException)
        {
            // Ignore errors when searching in Documents folder
        }

        // If the original path is absolute, try to find the file in the same directory structure
        // but on a different drive (useful when drive letters change)
        if (Path.IsPathRooted(fileName))
        {
            try
            {
                var originalDrive = Path.GetPathRoot(fileName)?[0];
                var pathWithoutDrive = fileName.Length > 3 ? fileName[3..] : string.Empty;

                foreach (var drive in cachedDriveLetters.Where(drive => drive != originalDrive && !string.IsNullOrEmpty(pathWithoutDrive)))
                {
                    var alternatePath = $"{drive}:\\{pathWithoutDrive}";
                    if (File.Exists(alternatePath) && !alternatives.Contains(alternatePath))
                    {
                        alternatives.Add(alternatePath);
                    }
                }
            }
            catch (Exception ex) when (ex is ArgumentException or
                                             ArgumentNullException or
                                             PathTooLongException or
                                             UnauthorizedAccessException or
                                             IOException)
            {
                // Ignore errors when searching on different drives
            }
        }

        // Try relative path resolution from project directory
        if (!Path.IsPathRooted(fileName) && !string.IsNullOrWhiteSpace(sessionFilePath))
        {
            try
            {
                var sessionDir = Path.GetDirectoryName(sessionFilePath);
                if (!string.IsNullOrEmpty(sessionDir))
                {
                    var relativePath = Path.Join(sessionDir, fileName);
                    var normalizedPath = Path.GetFullPath(relativePath);

                    if (File.Exists(normalizedPath) && !alternatives.Contains(normalizedPath))
                    {
                        alternatives.Add(normalizedPath);
                    }
                }
            }
            catch (Exception ex) when (ex is ArgumentException or
                                            ArgumentNullException or
                                            PathTooLongException or
                                            UnauthorizedAccessException or
                                            IOException or
                                            NotSupportedException)
            {
                // Ignore errors with relative path resolution
            }
        }

        return alternatives;
    }
}