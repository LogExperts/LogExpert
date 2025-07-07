using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace LogExpert.UI.Extensions;

[SupportedOSPlatform("windows")]
internal static partial class NativeMethods
{
    #region Fields

    public const long SM_CYVSCROLL = 20;
    public const long SM_CXHSCROLL = 21;
    public const long SM_CXVSCROLL = 2;
    public const long SM_CYHSCROLL = 3;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int RmRebootReasonNone = 0;
    private const int CCH_RM_MAX_APP_NAME = 255;
    private const int CCH_RM_MAX_SVC_NAME = 63;

    #endregion

    #region Structs

    [StructLayout(LayoutKind.Sequential)]
    public struct RM_UNIQUE_PROCESS
    {
        public int dwProcessId;
        public System.Runtime.InteropServices.
            ComTypes.FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential,
        CharSet = CharSet.Auto)]
    public struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;
        [MarshalAs(UnmanagedType.ByValTStr,
            SizeConst = CCH_RM_MAX_APP_NAME + 1)]
        public string strAppName;
        [MarshalAs(UnmanagedType.ByValTStr,
            SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
        public string strServiceShortName;
        public RM_APP_TYPE ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bRestartable;
    }
    #endregion Structs

    #region Enums
    public enum RM_APP_TYPE
    {
        RmUnknownApp = 0,
        RmMainWindow = 1,
        RmOtherWindow = 2,
        RmService = 3,
        RmExplorer = 4,
        RmConsole = 5,
        RmCritical = 1000
    }

    #endregion Enums

    #region Library Imports

    #region user32.dll Imports
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyIcon (nint hIcon);

    [LibraryImport("User32.dll")]
    public static partial int SetForegroundWindow (nint hWnd);

    [LibraryImport("user32.dll")]
    public static partial long GetSystemMetricsForDpi (long index);

    [LibraryImport("user32.dll")]
    public static partial long GetSystemMetrics (long index);

    [LibraryImport("user32.dll")]
    public static partial short GetKeyState (int vKey);

    #endregion user32.dll Imports

    #region shell32.dll Imports
    /*
    UINT ExtractIconEx(
    LPCTSTR lpszFile,
    int nIconIndex,
    HICON *phiconLarge,
    HICON *phiconSmall,
    UINT nIcons
    );
    * */
    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint ExtractIconEx (
        string fileName,
        int iconIndex,
        out nint iconsLarge,
        out nint iconsSmall,
        uint numIcons
    );

    #endregion shell32.dll Imports

    #region dwmapi.dll Imports

    #region TitleBarDarkMode
    [LibraryImport("dwmapi.dll")]
    public static partial int DwmSetWindowAttribute (nint hwnd, int attr, ref int attrValue, int attrSize);
    #endregion TitleBarDarkMode

    #endregion shell32.dll Imports

    #region rstrtmgr.dll Imports

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int RmGetList (
    uint dwSessionHandle,
    out uint pnProcInfoNeeded,
    ref uint pnProcInfo,
    [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
    ref uint lpdwRebootReasons);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int RmRegisterResources (
    uint pSessionHandle,
    uint nFiles,
    string[] rgsFilenames,
    uint nApplications,
    [In] RM_UNIQUE_PROCESS[] rgApplications,
    uint nServices,
    string[] rgsServiceNames);

    [LibraryImport("rstrtmgr.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RmStartSession (
        out uint pSessionHandle,
        int dwSessionFlags,
        string strSessionKey);

    [LibraryImport("rstrtmgr.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RmEndSession (uint pSessionHandle);

    #endregion rstrtmgr.dll Imports

    #endregion Library Imports

    #region Helper methods

    public static Icon LoadIconFromExe (string fileName, int index)
    {
        nint smallIcons = new();
        nint largeIcons = new();
        int num = (int)ExtractIconEx(fileName, index, out largeIcons, out smallIcons, 1);
        if (num > 0 && smallIcons != nint.Zero)
        {
            var icon = (Icon)Icon.FromHandle(smallIcons).Clone();
            DestroyIcon(smallIcons);
            return icon;
        }
        if (num > 0 && largeIcons != nint.Zero)
        {
            var icon = (Icon)Icon.FromHandle(largeIcons).Clone();
            DestroyIcon(largeIcons);
            return icon;
        }
        return null;
    }

    public static Icon[,] ExtractIcons (string fileName)
    {
        var iconCount = ExtractIconEx(fileName, -1, out var largeIcon, out var smallIcon, 0);
        if (iconCount <= 0)
        {
            return null;
        }

        var result = new Icon[2, iconCount];

        for (var i = 0; i < iconCount; ++i)
        {
            var num = ExtractIconEx(fileName, i, out var largeIcons, out var smallIcons, 1);
            if (smallIcons != nint.Zero)
            {
                result[0, i] = (Icon)Icon.FromHandle(smallIcons).Clone();
                DestroyIcon(smallIcons);
            }
            else
            {
                result[0, i] = null;
            }

            if (num > 0 && largeIcons != nint.Zero)
            {
                result[1, i] = (Icon)Icon.FromHandle(largeIcons).Clone();
                DestroyIcon(largeIcons);
            }
            else
            {
                result[1, i] = null;
            }
        }

        return result;
    }

    public static bool UseImmersiveDarkMode (nint handle, bool enabled)
    {
        var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
        if (IsWindows10OrGreater(18985))
        {
            attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
        }

        var useImmersiveDarkMode = enabled ? 1 : 0;
        return DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;

    }

    private static bool IsWindows10OrGreater (int build = -1)
    {
        return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
    }

    #endregion Helper methods
}