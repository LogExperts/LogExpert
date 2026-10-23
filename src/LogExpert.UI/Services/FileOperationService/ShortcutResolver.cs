using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using NLog;

namespace LogExpert.UI.Services.FileOperationService;

/// <summary>
/// Resolves a Windows shortcut (<c>.lnk</c>) to the local file it points at.
/// </summary>
/// <remarks>
/// <para>
/// Resolution goes through the shell's own <c>IShellLink</c> implementation instead of parsing the
/// MS-SHLLINK format by hand. This class lives in the UI assembly, which is
/// <c>net10.0-windows</c>, so the COM dependency is safe here.
/// </para>
/// <para>
/// Anything that is not a <c>.lnk</c>, does not exist, is implausibly large, cannot be resolved, or
/// points off the local machine is returned unchanged: such a shortcut opens as a regular file,
/// exactly as before.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
internal static class ShortcutResolver
{
    #region Constants

    private const string SHORTCUT_EXTENSION = ".lnk";

    /// <summary>Buffer size for <c>IShellLinkW::GetPath</c>, following the shell's own MAX_PATH.</summary>
    private const int TARGET_PATH_CAPACITY = 260;

    /// <summary>
    /// Real shortcuts are a few KB. Anything larger is not one, and handing it to the shell parser
    /// whole would let a dropped file spike memory.
    /// </summary>
    private const int MAX_SHORTCUT_SIZE = 1024 * 1024;

    /// <summary><c>SLGP_RAWPATH</c>: retrieve the stored target path without resolving it against the current shell state.</summary>
    private const uint SLGP_RAWPATH = 0x00000004;

    /// <summary><c>STGM_READ</c>: open the shortcut file read-only.</summary>
    private const uint STGM_READ = 0x00000000;

    #endregion

    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Public methods

    /// <summary>
    /// Resolves <paramref name="fileName"/> to the file a shortcut points at.
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

            IShellLink shellLink = (IShellLink)new ShellLinkClass();
            ((IPersistFile)shellLink).Load(fileName, STGM_READ);

            var buffer = new System.Text.StringBuilder(TARGET_PATH_CAPACITY);
            shellLink.GetPath(buffer, buffer.Capacity, IntPtr.Zero, SLGP_RAWPATH);

            var target = buffer.ToString();
            if (target.Length == 0)
            {
                _logger.Debug("Shortcut {Shortcut} has no target path, keeping it", fileName);
                return fileName;
            }

            // A UNC target would make File.Exists authenticate against the remote host, handing it
            // the user's NetNTLM hash, and device paths (\\.\, \\?\) can block. Such a shortcut
            // opens as a regular file instead.
            if (target.StartsWith(@"\\", StringComparison.Ordinal))
            {
                _logger.Debug("Shortcut target {Target} is not a local path, keeping {Shortcut}", target, fileName);
                return fileName;
            }

            if (!File.Exists(target))
            {
                _logger.Debug("Shortcut target {Target} does not exist, keeping {Shortcut}", target, fileName);
                return fileName;
            }

            return target;
        }
        catch (Exception ex) when (ex is COMException
                                       or ExternalException
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

    #region COM declarations

    /// <summary>The shell link CoClass (<c>CLSID_ShellLink</c>).</summary>
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLinkClass
    {
    }

    /// <summary>
    /// <c>IShellLinkW</c>, declared with only its first method (<c>GetPath</c>). The interface's
    /// remaining methods are never called, so they are left out.
    /// </summary>
    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLink
    {
        void GetPath ([MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cchMaxPath, IntPtr pFindData, uint fFlags);
    }

    /// <summary><c>IPersistFile</c>, used to load the shortcut file into the shell link object.</summary>
    [ComImport]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        // From IPersist, which IPersistFile extends.
        void GetClassID (out Guid pClassID);

        // IsDirty precedes Load in the vtable and has to be declared to keep the slot order.
        [PreserveSig]
        int IsDirty ();

        void Load ([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
    }

    #endregion
}
