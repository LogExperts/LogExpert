using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Services;

[SupportedOSPlatform("windows")]
internal static class TestShortcuts
{
    public static string CreateTempDirectory (string name)
    {
        var directory = Path.Join(Path.GetTempPath(), "LogExpertTests", name, Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(directory);
        return directory;
    }

    public static void Create (string shortcutPath, string targetPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        Assert.That(shellType, Is.Not.Null, "WScript.Shell should be available on Windows test machines");

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic? link = null;
        try
        {
            link = shell.CreateShortcut(shortcutPath);
            link.TargetPath = targetPath;
            link.Save();
        }
        finally
        {
            if (link != null)
            {
                Marshal.FinalReleaseComObject(link);
            }

            Marshal.FinalReleaseComObject(shell);
        }
    }
}
