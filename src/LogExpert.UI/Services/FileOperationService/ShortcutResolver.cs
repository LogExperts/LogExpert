using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Text;

using NLog;

using IShellLinkW = Vanara.PInvoke.Shell32.IShellLinkW;
using CShellLinkW = Vanara.PInvoke.Shell32.CShellLinkW;
using SLGP = Vanara.PInvoke.Shell32.SLGP;
using STGM = Vanara.PInvoke.STGM;

namespace LogExpert.UI.Services.FileOperationService;

[SupportedOSPlatform("windows")]
internal static class ShortcutResolver
{
    #region Constants

    private const string SHORTCUT_EXTENSION = ".lnk";

    private const int TARGET_PATH_CAPACITY = 1024;

    internal const int MAX_SHORTCUT_SIZE = 1024 * 1024;

    // A chain is followed to its end, but a broken or cyclic chain must not load COM objects forever.
    internal const int MAX_SHORTCUT_HOPS = 3;

    #endregion

    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Public methods

    /// <summary>Follows <paramref name="fileName"/> through at most <see cref="MAX_SHORTCUT_HOPS"/> shortcuts.</summary>
    /// <returns>
    /// The final existing target; otherwise the last path that could not be followed (the input
    /// itself when the hop limit is reached).
    /// </returns>
    public static string Resolve (string fileName)
    {
        return FollowChain(fileName, ResolveOneHop);
    }

    #endregion

    #region Internal methods

    internal static string FollowChain (string fileName, Func<string, string?> resolveOneHop)
    {
        var current = fileName;

        for (var hop = 0; ; hop++)
        {
            if (!IsShortcut(current))
            {
                return current;
            }

            if (hop >= MAX_SHORTCUT_HOPS)
            {
                _logger.Warn("Shortcut chain starting at {Shortcut} exceeds {Limit} hops; keeping it", fileName, MAX_SHORTCUT_HOPS);
                return fileName;
            }

            var target = resolveOneHop(current);
            if (target is null)
            {
                return current;
            }

            current = target;
        }
    }

    internal static string? SanitizeLocalTarget (string target, string shortcutDirectory)
    {
        try
        {
            var expanded = Environment.ExpandEnvironmentVariables(target);
            var fullTarget = Path.GetFullPath(expanded, shortcutDirectory);

            // Mapped drives are allowed on purpose: the user created the mapping, so it is trusted.
            var root = Path.GetPathRoot(fullTarget);
            return root is not null && root.Length == 3 && root[1] == ':' ? fullTarget : null;
        }
        catch (Exception ex) when (ex is ArgumentException
                                       or IOException
                                       or NotSupportedException)
        {
            return null;
        }
    }

    #endregion

    #region Private methods

    internal static bool IsShortcut (string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName)
               && Path.GetExtension(fileName).Equals(SHORTCUT_EXTENSION, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveOneHop (string fileName)
    {
        IShellLinkW? shellLink = null;

        try
        {
            if (!File.Exists(fileName))
            {
                _logger.Debug("Shortcut {Shortcut} does not exist, keeping it", fileName);
                return null;
            }

            if (new FileInfo(fileName).Length > MAX_SHORTCUT_SIZE)
            {
                _logger.Warn("Ignoring {Shortcut}: it is above the {Limit} byte shortcut limit", fileName, MAX_SHORTCUT_SIZE);
                return null;
            }

            shellLink = (IShellLinkW)new CShellLinkW();
            ((IPersistFile)shellLink).Load(fileName, (int)STGM.STGM_READ);

            var buffer = new StringBuilder(TARGET_PATH_CAPACITY);
            shellLink.GetPath(buffer, buffer.Capacity, out _, SLGP.SLGP_RAWPATH);

            var target = buffer.ToString();
            if (target.Length == 0)
            {
                _logger.Debug("Shortcut {Shortcut} has no target path, keeping it", fileName);
                return null;
            }

            var shortcutDirectory = Path.GetDirectoryName(Path.GetFullPath(fileName))!;

            // The target comes straight from the .lnk bytes, so only follow a plain local drive path:
            // probing a UNC/device path would authenticate against a remote host (NetNTLM leak).
            var fullTarget = SanitizeLocalTarget(target, shortcutDirectory);
            if (fullTarget is null)
            {
                _logger.Debug("Shortcut target {Target} is not a local drive path, keeping {Shortcut}", target, fileName);
                return null;
            }

            if (!File.Exists(fullTarget))
            {
                _logger.Debug("Shortcut target {Target} does not exist, keeping {Shortcut}", target, fileName);
                return null;
            }

            return fullTarget;
        }
        catch (Exception ex) when (ex is ExternalException
                                       or IOException
                                       or UnauthorizedAccessException
                                       or ArgumentException
                                       or NotSupportedException)
        {
            _logger.Debug(ex, "Could not resolve shortcut {Shortcut}, keeping it", fileName);
            return null;
        }
        finally
        {
            if (shellLink is not null)
            {
                Marshal.FinalReleaseComObject(shellLink);
            }
        }
    }

    #endregion
}
