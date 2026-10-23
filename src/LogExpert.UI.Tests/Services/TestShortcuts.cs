using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Services;

/// <summary>
/// Creates real shortcuts through the shell's WScript.Shell automation object, so the tests
/// exercise the same files Explorer writes. The COM objects are released right after saving.
/// </summary>
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
