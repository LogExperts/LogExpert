using System.Runtime.Versioning;

using LogExpert.UI.Services.FileOperationService;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Services;

[TestFixture]
[SupportedOSPlatform("windows")]
internal class ShortcutResolverTests
{
    [Test]
    public void Resolve_ShortcutToLocalTarget_ReturnsTarget ()
    {
        // Arrange
        var directory = TestShortcuts.CreateTempDirectory("ShortcutLocal");

        try
        {
            var log = Path.Join(directory, "actual.log");
            File.WriteAllText(log, "line1");

            var shortcut = Path.Join(directory, "actual.lnk");
            TestShortcuts.Create(shortcut, log);

            // Act
            var resolved = ShortcutResolver.Resolve(shortcut);

            // Assert
            Assert.That(resolved, Is.EqualTo(log));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Resolve_ShortcutToUncTarget_ReturnsShortcutUnchanged ()
    {
        // Arrange
        var directory = TestShortcuts.CreateTempDirectory("ShortcutUnc");

        try
        {
            var shortcut = Path.Join(directory, "remote.lnk");
            TestShortcuts.Create(shortcut, @"\\nonexistent-host\share\actual.log");

            // Act
            var resolved = ShortcutResolver.Resolve(shortcut);

            // Assert
            Assert.That(resolved, Is.EqualTo(shortcut),
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
        // Arrange
        var directory = TestShortcuts.CreateTempDirectory("ShortcutOversized");

        try
        {
            var shortcut = Path.Join(directory, "oversized.lnk");
            File.WriteAllBytes(shortcut, new byte[ShortcutResolver.MAX_SHORTCUT_SIZE + 1]);

            // Act
            var resolved = ShortcutResolver.Resolve(shortcut);

            // Assert
            Assert.That(resolved, Is.EqualTo(shortcut));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    // The shell resolves .lnk targets when saving, so a real chain cannot be produced with
    // WScript.Shell; the follower is exercised with injected hops instead.
    [Test]
    public void FollowChain_ShortcutToShortcut_ReturnsFinalTarget ()
    {
        // Arrange
        var targets = new Dictionary<string, string>
        {
            [@"C:\logs\first.lnk"] = @"C:\logs\second.lnk",
            [@"C:\logs\second.lnk"] = @"C:\logs\actual.log"
        };

        // Act
        var resolved = ShortcutResolver.FollowChain(@"C:\logs\first.lnk", path => targets.GetValueOrDefault(path));

        // Assert
        Assert.That(resolved, Is.EqualTo(@"C:\logs\actual.log"));
    }

    [Test]
    public void FollowChain_ChainAtLimit_ReturnsFinalTarget ()
    {
        // Arrange
        var targets = CreateChain(ShortcutResolver.MAX_SHORTCUT_HOPS);

        // Act
        var resolved = ShortcutResolver.FollowChain(@"C:\logs\link1.lnk", path => targets.GetValueOrDefault(path));

        // Assert
        Assert.That(resolved, Is.EqualTo(@"C:\logs\actual.log"));
    }

    [Test]
    public void FollowChain_ChainBeyondLimit_StopsAfterMaxHops ()
    {
        // Arrange
        var targets = CreateChain(ShortcutResolver.MAX_SHORTCUT_HOPS + 1);

        // Act
        var resolved = ShortcutResolver.FollowChain(@"C:\logs\link1.lnk", path => targets.GetValueOrDefault(path));

        // Assert
        Assert.That(resolved, Is.EqualTo(@"C:\logs\link1.lnk"));
    }

    [Test]
    public void FollowChain_CyclicChain_StopsAfterMaxHops ()
    {
        // Arrange
        var targets = new Dictionary<string, string>
        {
            [@"C:\logs\first.lnk"] = @"C:\logs\second.lnk",
            [@"C:\logs\second.lnk"] = @"C:\logs\first.lnk"
        };

        // Act
        var resolved = ShortcutResolver.FollowChain(@"C:\logs\first.lnk", path => targets.GetValueOrDefault(path));

        // Assert
        Assert.That(resolved, Is.EqualTo(@"C:\logs\first.lnk"));
    }

    [TestCase("//nonexistent-host/share/actual.log")]
    [TestCase(@"\\?\UNC\nonexistent-host\share\actual.log")]
    [TestCase(@"\\?\C:\Windows\system32\drivers\etc\hosts")]
    [TestCase(@"\\.\C:\nonexistent\actual.log")]
    [TestCase(@"\??\UNC\nonexistent-host\share\actual.log")]
    public void SanitizeLocalTarget_DisguisedNonLocalTarget_IsRejected (string target)
    {
        // Arrange
        const string shortcutDirectory = @"C:\logs";

        // Act
        var sanitized = ShortcutResolver.SanitizeLocalTarget(target, shortcutDirectory);

        // Assert
        Assert.That(sanitized, Is.Null,
            $"'{target}' must not be followed: probing it could reach the network");
    }

    [Test]
    public void SanitizeLocalTarget_PlainLocalTarget_ReturnsFullPath ()
    {
        // Arrange
        const string target = @"C:\logs\actual.log";

        // Act
        var sanitized = ShortcutResolver.SanitizeLocalTarget(target, @"D:\other");

        // Assert
        Assert.That(sanitized, Is.EqualTo(target));
    }

    [Test]
    public void SanitizeLocalTarget_RelativeTarget_ResolvesAgainstShortcutDirectory ()
    {
        // Arrange
        const string target = @"sub\actual.log";

        // Act
        var sanitized = ShortcutResolver.SanitizeLocalTarget(target, @"C:\logs");

        // Assert
        Assert.That(sanitized, Is.EqualTo(@"C:\logs\sub\actual.log"));
    }

    [Test]
    public void SanitizeLocalTarget_EnvironmentVariableTarget_IsExpanded ()
    {
        // Arrange
        Environment.SetEnvironmentVariable("LOGEXPERT_TEST_VAR", @"C:\logs", EnvironmentVariableTarget.Process);

        try
        {
            // Act
            var sanitized = ShortcutResolver.SanitizeLocalTarget(@"%LOGEXPERT_TEST_VAR%\actual.log", @"D:\other");

            // Assert
            Assert.That(sanitized, Is.EqualTo(@"C:\logs\actual.log"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOGEXPERT_TEST_VAR", null, EnvironmentVariableTarget.Process);
        }
    }

    private static Dictionary<string, string> CreateChain (int linkCount)
    {
        var targets = new Dictionary<string, string>();

        for (var i = 1; i <= linkCount; i++)
        {
            targets[$@"C:\logs\link{i}.lnk"] = i == linkCount
                ? @"C:\logs\actual.log"
                : $@"C:\logs\link{i + 1}.lnk";
        }

        return targets;
    }
}
