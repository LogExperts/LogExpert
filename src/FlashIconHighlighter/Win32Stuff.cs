using System;
using System.Runtime.InteropServices;

namespace FlashIconHighlighter;

/*
 * Flash stuff stolen from http://blogs.x2line.com/al/archive/2008/04/19/3392.aspx
 */

[StructLayout(LayoutKind.Sequential)]
public struct FLASHWINFO
{
    public uint cbSize;
    public IntPtr hwnd;
    public int dwFlags;
    public uint uCount;
    public int dwTimeout;
}

public partial class Win32Stuff
{
    #region Public methods

    [LibraryImport("user32.dll")]
    public static partial int FlashWindowEx(ref FLASHWINFO pwfi);

    #endregion
}