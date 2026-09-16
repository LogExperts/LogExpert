using System.Security;

namespace LogExpert.Core.Classes.FileDrop;

/// <summary>Prepares a snapshot of dropped files without opening logs or touching the UI.</summary>
public sealed class DroppedFileDiscovery (
    Func<string, FileAttributes>? getAttributes = null,
    Func<string, IEnumerable<string>>? enumerateEntries = null)
{
    private readonly Func<string, FileAttributes> _getAttributes = getAttributes ?? File.GetAttributes;
    private readonly Func<string, IEnumerable<string>> _enumerateEntries = enumerateEntries ?? Directory.EnumerateFileSystemEntries;

    public Task<DroppedFileDiscoveryResult> DiscoverAsync (IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var snapshot = paths.ToArray();
        return Task.Run(() => Discover(snapshot, cancellationToken), cancellationToken);
    }

    private DroppedFileDiscoveryResult Discover (string[] paths, CancellationToken cancellationToken)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var directories = new Stack<string>();
        var skipped = new Dictionary<string, SkippedDropPath>(StringComparer.OrdinalIgnoreCase);
        var includesFolders = false;

        void addPath (string path, bool explicitlyDropped)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                path = Path.GetFullPath(path);
                var attributes = _getAttributes(path);
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    includesFolders = true;
                    directories.Push(Path.TrimEndingDirectorySeparator(path));
                }
                else if (explicitlyDropped || !IsSessionPath(path))
                {
                    files.Add(path);
                }
            }
            catch (Exception ex) when (IsPathError(ex))
            {
                skipped[path] = new SkippedDropPath(path, ex.Message);
            }
        }

        foreach (var path in paths)
        {
            addPath(path, true);
        }

        while (directories.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!visited.Add(directory))
            {
                continue;
            }

            try
            {
                // Recheck immediately before enumeration: a directory may have changed since it was queued.
                if ((_getAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                {
                    skipped[directory] = new SkippedDropPath(directory, null);
                    continue;
                }

                using var entries = _enumerateEntries(directory).GetEnumerator();
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!entries.MoveNext())
                    {
                        break;
                    }
                    addPath(entries.Current, false);
                }
            }
            catch (Exception ex) when (IsPathError(ex))
            {
                skipped[directory] = new SkippedDropPath(directory, ex.Message);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new DroppedFileDiscoveryResult(includesFolders,
            files.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            skipped.Values.OrderBy(skip => skip.Path, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static bool IsSessionPath (string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".lxp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".lxj", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPathError (Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or SecurityException or ArgumentException or NotSupportedException;
    }
}