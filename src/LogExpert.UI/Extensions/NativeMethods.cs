using System.Runtime.Versioning;

namespace LogExpert.UI.Extensions;

[SupportedOSPlatform("windows")]
internal static partial class NativeMethods
{
    #region Fields

    /// <summary>
    /// Specifies the system metric index for the height, in pixels, of a vertical scroll bar in Windows.
    /// </summary>
    /// <remarks>This constant is used with Windows API functions, such as GetSystemMetrics, to retrieve the
    /// height of a standard vertical scroll bar. The value corresponds to the SM_CYVSCROLL metric defined by the
    /// Windows operating system.</remarks>
    public const uint SM_CYVSCROLL = 20;

    /// <summary>
    /// Specifies the system metric index for the width, in pixels, of a horizontal scroll bar.
    /// </summary>
    /// <remarks>Use this constant with system metric retrieval functions, such as GetSystemMetrics, to obtain
    /// the width of a standard horizontal scroll bar. This value is commonly used when designing user interfaces that
    /// need to account for scroll bar dimensions.</remarks>
    public const uint SM_CXHSCROLL = 21;

    /// <summary>
    /// Specifies the system metric index for the width of a vertical scroll bar for a specific DPI.
    /// </summary>
    /// <remarks>This constant is used with DPI-aware functions, such as GetSystemMetricsForDpi, to retrieve the
    /// width of a standard vertical scroll bar for a specific DPI setting.</remarks>
    public const uint SM_CXVSCROLL = 2;

    /// <summary>
    /// Specifies the height, in pixels, of a horizontal scroll bar in a standard window.
    /// </summary>
    /// <remarks>This constant is used with system metrics APIs, such as GetSystemMetrics, to retrieve the
    /// height of horizontal scroll bars for consistent UI layout across Windows applications.</remarks>
    public const uint SM_CYHSCROLL = 3;

    #endregion

    #region Win32 constants shared by keyboard handling

    public const int WM_KEYDOWN = 0x0100;

    public const long KEYDOWN_AUTOREPEAT_FLAG = 1L << 30;

    #endregion

    #region P/Invoke

    // DwmApi is the one place that does not go through Vanara: the generated P/Invoke there carries
    // no DefaultDllImportSearchPaths, and dwmapi.dll is not a KnownDLL, so dropping the System32
    // restriction would reopen a search-order hijack.
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    [System.Runtime.InteropServices.DefaultDllImportSearchPaths(System.Runtime.InteropServices.DllImportSearchPath.System32)]
    public static extern int DwmSetWindowAttribute (IntPtr hwnd, int attribute, ref int value, int size);

    #endregion

    #region Helper methods

    // Tells the repeated WM_KEYDOWN messages Windows sends while a key is held down from the single
    // message of an actual press.
    public static bool IsAutoRepeat (Message msg)
    {
        return msg.Msg == WM_KEYDOWN && (msg.LParam.ToInt64() & KEYDOWN_AUTOREPEAT_FLAG) != 0;
    }

    public static Icon LoadIconFromExe (string fileName, int index)
    {

        Vanara.PInvoke.HICON[] largeIcons = [1];
        Vanara.PInvoke.HICON[] smallIcons = [1];
        var num = Vanara.PInvoke.Shell32.ExtractIconEx(fileName, index, largeIcons, smallIcons, 1);

        if (num > 0 && !smallIcons[0].IsNull)
        {
            var icon = (Icon)Icon.FromHandle((nint)smallIcons[0]).Clone();
            _ = Vanara.PInvoke.User32.DestroyIcon(smallIcons[0]);
            return icon;
        }

        if (num > 0 && !largeIcons[0].IsNull)
        {
            var icon = (Icon)Icon.FromHandle((nint)largeIcons[0]).Clone();
            _ = Vanara.PInvoke.User32.DestroyIcon(largeIcons[0]);
            return icon;
        }

        return null;
    }

    public static Icon[][] ExtractIcons (string fileName)
    {
        var iconCount = Vanara.PInvoke.Shell32.ExtractIconEx(fileName, -1, null, null, 0);

        if (iconCount <= 0)
        {
            return null;
        }

        var icons = new Icon[2][];
        var result = icons;

        result[0] = new Icon[iconCount]; // small icons
        result[1] = new Icon[iconCount]; // large icons

        for (var i = 0; i < iconCount; ++i)
        {
            Vanara.PInvoke.HICON[] largeIcons = [1];
            Vanara.PInvoke.HICON[] smallIcons = [1];

            var num = Vanara.PInvoke.Shell32.ExtractIconEx(fileName, i, largeIcons, smallIcons, 1);
            if (num > 0 && !smallIcons[0].IsNull)
            {
                result[0][i] = (Icon)Icon.FromHandle((nint)smallIcons[0]).Clone();
                _ = Vanara.PInvoke.User32.DestroyIcon(smallIcons[0]);
            }
            else
            {
                result[0][i] = null;
            }

            if (num > 0 && !largeIcons[0].IsNull)
            {
                result[1][i] = (Icon)Icon.FromHandle((nint)largeIcons[0]).Clone();
                _ = Vanara.PInvoke.User32.DestroyIcon(largeIcons[0]);
            }
            else
            {
                result[1][i] = null;
            }
        }

        return result;
    }

    #endregion Helper methods
}