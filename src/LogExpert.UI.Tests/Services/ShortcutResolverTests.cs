using System.Runtime.Versioning;

using LogExpert.UI.Services.FileOperationService;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Services;

/// <summary>
/// Tests the guard rails of <see cref="ShortcutResolver"/>: end to end via
/// <see cref="ShortcutResolver.Resolve"/> for whole-file behaviour (real shortcuts, oversized
/// files, plain UNC targets), and directly on <see cref="ShortcutResolver.SanitizeLocalTarget"/>
/// for disguised target forms — the shell parser cannot be fed strings like
/// <c>//host/share</c> through any API a test could use, but the guard must reject them, so it is
/// written as a pure function and tested as one.
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
            TestShortcuts.Create(shortcut, log);

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
            TestShortcuts.Create(shortcut, @"\\nonexistent-host\share\actual.log");

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
            File.WriteAllBytes(shortcut, new byte[ShortcutResolver.MAX_SHORTCUT_SIZE + 1]);

            Assert.That(ShortcutResolver.Resolve(shortcut), Is.EqualTo(shortcut));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase("//nonexistent-host/share/actual.log")]
    [TestCase(@"\\?\UNC\nonexistent-host\share\actual.log")]
    [TestCase(@"\\?\C:\Windows\system32\drivers\etc\hosts")]
    [TestCase(@"\\.\C:\nonexistent\actual.log")]
    [TestCase(@"\??\UNC\nonexistent-host\share\actual.log")]
    public void Sanitize_DisguisedNonLocalTarget_IsRejected (string target)
    {
        Assert.That(ShortcutResolver.SanitizeLocalTarget(target, @"C:\logs"), Is.Null,
            $"'{target}' must not be followed: probing it could reach the network");
    }

    [Test]
    public void Sanitize_PlainLocalTarget_ReturnsFullPath ()
    {
        Assert.That(ShortcutResolver.SanitizeLocalTarget(@"C:\logs\actual.log", @"D:\other"),
            Is.EqualTo(@"C:\logs\actual.log"));
    }

    [Test]
    public void Sanitize_RelativeTarget_ResolvesAgainstShortcutDirectory ()
    {
        Assert.That(ShortcutResolver.SanitizeLocalTarget(@"sub\actual.log", @"C:\logs"),
            Is.EqualTo(@"C:\logs\sub\actual.log"));
    }

    [Test]
    public void Sanitize_EnvironmentVariableTarget_IsExpanded ()
    {
        Environment.SetEnvironmentVariable("LOGEXPERT_TEST_VAR", @"C:\logs", EnvironmentVariableTarget.Process);

        try
        {
            Assert.That(ShortcutResolver.SanitizeLocalTarget(@"%LOGEXPERT_TEST_VAR%\actual.log", @"D:\other"),
                Is.EqualTo(@"C:\logs\actual.log"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOGEXPERT_TEST_VAR", null, EnvironmentVariableTarget.Process);
        }
    }

    private static string CreateTempDirectory (string name)
    {
        var directory = Path.Join(Path.GetTempPath(), "LogExpertTests", name, Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(directory);
        return directory;
    }
}
