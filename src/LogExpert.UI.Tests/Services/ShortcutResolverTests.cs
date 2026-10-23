using System.Runtime.Versioning;

using LogExpert.UI.Services.FileOperationService;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Services;

/// <summary>
/// Tests the guard rails of <see cref="ShortcutResolver"/>: oversized files and targets off the
/// local machine are not followed, the shortcut keeps opening as a regular file instead.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows")]
internal class ShortcutResolverTests
{
    [Test]
    public void Resolve_ShortcutToLocalTarget_ReturnsTarget ()
    {
        var directory = CreateTempDirectory("ShortcutLocal");

        try
        {
            var log = Path.Join(directory, "actual.log");
            File.WriteAllText(log, "line1");

            var shortcut = Path.Join(directory, "actual.lnk");
            CreateShortcut(shortcut, log);

            Assert.That(ShortcutResolver.Resolve(shortcut), Is.EqualTo(log));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Resolve_ShortcutToUncTarget_ReturnsShortcutUnchanged ()
    {
        var directory = CreateTempDirectory("ShortcutUnc");

        try
        {
            var shortcut = Path.Join(directory, "remote.lnk");
            CreateShortcut(shortcut, @"\\nonexistent-host\share\actual.log");

            Assert.That(ShortcutResolver.Resolve(shortcut), Is.EqualTo(shortcut),
                "a UNC target must not be probed: File.Exists would authenticate against the remote host");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Resolve_OversizedShortcutFile_ReturnsPathUnchanged ()
    {
        var directory = CreateTempDirectory("ShortcutOversized");

        try
        {
            var shortcut = Path.Join(directory, "oversized.lnk");
            File.WriteAllBytes(shortcut, new byte[1024 * 1024 + 1]);

            Assert.That(ShortcutResolver.Resolve(shortcut), Is.EqualTo(shortcut));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateTempDirectory (string name)
    {
        var directory = Path.Join(Path.GetTempPath(), "LogExpertTests", name, Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>
    /// Creates a real shortcut through the shell's WScript.Shell automation object, so the tests
    /// exercise the same files Explorer writes.
    /// </summary>
    private static void CreateShortcut (string shortcutPath, string targetPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        Assert.That(shellType, Is.Not.Null, "WScript.Shell should be available on Windows test machines");

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.Save();
    }
}
