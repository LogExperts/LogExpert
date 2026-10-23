using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Text;

using NLog;

using IShellLinkW = Vanara.PInvoke.Shell32.IShellLinkW;
using CShellLinkW = Vanara.PInvoke.Shell32.CShellLinkW;
using SLGP = Vanara.PInvoke.Shell32.SLGP;

namespace LogExpert.UI.Services.FileOperationService;

/// <summary>
/// Resolves a Windows shortcut (<c>.lnk</c>) to the local file it points at.
/// </summary>
/// <remarks>
/// <para>
/// Resolution goes through the shell's own <c>IShellLink</c> implementation (Vanara's
/// <see cref="IShellLinkW"/>) instead of parsing the MS-SHLLINK format by hand. This class lives in
/// the UI assembly, which is <c>net10.0-windows</c>, so the COM dependency is safe here.
/// </para>
/// <para>
/// Anything that is not a <c>.lnk</c>, does not exist, is implausibly large, cannot be resolved, or
/// points off the local machine is returned unchanged: such a shortcut opens as a regular file,
/// exactly as before. Resolution is one hop: a target that is itself a shortcut is returned as-is
/// and resolved by the next caller.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
internal static class ShortcutResolver
{
    #region Constants

    private const string SHORTCUT_EXTENSION = ".lnk";

    /// <summary>
    /// Buffer size for <c>IShellLinkW::GetPath</c>, above MAX_PATH so long-path targets are not truncated.
    /// </summary>
    private const int TARGET_PATH_CAPACITY = 1024;

    /// <summary>
    /// Real shortcuts are a few KB. Anything larger is not one, and handing it to the shell parser
    /// whole would let a dropped file spike memory.
    /// </summary>
    internal const int MAX_SHORTCUT_SIZE = 1024 * 1024;

    /// <summary><c>STGM_READ</c>: open the shortcut file read-only.</summary>
    private const int STGM_READ = 0;

    #endregion

    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Public methods

    /// <summary>
    /// Resolves <paramref name="fileName"/> to the file a shortcut points at (one hop).
    /// </summary>
    /// <param name="fileName">A path that may or may not be a shortcut.</param>
    /// <returns>
    /// The target path when it exists; otherwise <paramref name="fileName"/> unchanged.
    /// </returns>
    public static string Resolve (string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || !Path.GetExtension(fileName).Equals(SHORTCUT_EXTENSION, StringComparison.OrdinalIgnoreCase)
            || !File.Exists(fileName))
        {
            return fileName;
        }

        try
        {
            if (new FileInfo(fileName).Length > MAX_SHORTCUT_SIZE)
            {
                _logger.Warn("Ignoring {Shortcut}: it is above the {Limit} byte shortcut limit", fileName, MAX_SHORTCUT_SIZE);
                return fileName;
            }

            IShellLinkW shellLink = (IShellLinkW)new CShellLinkW();
            ((IPersistFile)shellLink).Load(fileName, STGM_READ);

            var buffer = new StringBuilder(TARGET_PATH_CAPACITY);
            shellLink.GetPath(buffer, buffer.Capacity, out _, SLGP.SLGP_RAWPATH);

            var target = buffer.ToString();
            if (target.Length == 0)
            {
                _logger.Debug("Shortcut {Shortcut} has no target path, keeping it", fileName);
                return fileName;
            }

            // A relative target sits next to the shortcut, not in the current working directory.
            var shortcutDirectory = Path.GetDirectoryName(Path.GetFullPath(fileName))!;

            // The target string comes straight from the .lnk bytes, so it can be a UNC or device
            // path in disguise (//host/share, \\?\UNC\...). Only a plain local drive path is
            // followed, because probing anything else would authenticate against a remote host,
            // handing it the user's NetNTLM hash.
            var fullTarget = SanitizeLocalTarget(target, shortcutDirectory);
            if (fullTarget is null)
            {
                _logger.Debug("Shortcut target {Target} is not a local drive path, keeping {Shortcut}", target, fileName);
                return fileName;
            }

            if (!File.Exists(fullTarget))
            {
                _logger.Debug("Shortcut target {Target} does not exist, keeping {Shortcut}", target, fileName);
                return fileName;
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
            return fileName;
        }
    }

    #endregion

    #region Internal methods

    /// <summary>
    /// Turns a raw shortcut target into the full path to follow — but only when it lands on a
    /// plain local drive (<c>X:\</c>). Environment variables are expanded and relative targets are
    /// resolved against the shortcut's directory; anything else (UNC, device paths like
    /// <c>\\?\</c> or <c>\\.\</c>, malformed strings) yields <see langword="null"/>, because
    /// probing such a path would authenticate against a remote host and leak the user's NetNTLM
    /// hash. Pure string handling: no file system access.
    /// </summary>
    /// <param name="target">The raw target string from the .lnk.</param>
    /// <param name="shortcutDirectory">Directory the shortcut lives in; the base for relative targets.</param>
    internal static string? SanitizeLocalTarget (string target, string shortcutDirectory)
    {
        try
        {
            var expanded = Environment.ExpandEnvironmentVariables(target);
            var fullTarget = Path.GetFullPath(expanded, shortcutDirectory);

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
}
