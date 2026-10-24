using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Services;

[SupportedOSPlatform("windows")]
internal static class TestShortcuts
{
    public static void Create (string shortcutPath, string targetPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        Assert.That(shellType, Is.Not.Null, "WScript.Shell should be available on Windows test machines");

        dynamic shell = Activator.CreateInstance(shellType)!;
        object? shortcut = null;
        try
        {
            dynamic link = shell.CreateShortcut(shortcutPath);
            shortcut = link;
            link.TargetPath = targetPath;
            link.Save();
        }
        finally
        {
            if (shortcut != null)
            {
                Marshal.FinalReleaseComObject(shortcut);
            }

            Marshal.FinalReleaseComObject(shell);
        }
    }
}
