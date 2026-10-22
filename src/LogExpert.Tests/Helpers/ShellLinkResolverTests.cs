using LogExpert.Core.Helpers;

using NUnit.Framework;

namespace LogExpert.Tests.Helpers;

/// <summary>
/// Tests for <see cref="ShellLinkResolver"/>. The parsing tests feed crafted byte arrays in, so they
/// need no real shortcut; the resolution tests use a temporary directory.
/// </summary>
[TestFixture]
public class ShellLinkResolverTests
{
    private string _directory;

    [SetUp]
    public void Setup ()
    {
        _directory = Path.Join(Path.GetTempPath(), "LogExpertTests", "ShellLink", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown ()
    {
        if (!Directory.Exists(_directory))
        {
            return;
        }

        try
        {
            Directory.Delete(_directory, true);
        }
        catch (IOException)
        {
            // A file still held open by another process; the temp directory gets cleaned up anyway.
        }
    }

    #region Helper methods

    private string CreateLog (string relativePath)
    {
        var path = Path.Join(_directory, relativePath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "log line");
        return path;
    }

    private string CreateShortcut (string fileName, byte[] bytes)
    {
        var path = Path.Join(_directory, fileName);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    #endregion

    #region Parsing

    [Test]
    public void TryParseTargets_UnicodeLinkInfo_ReadsLocalBasePath ()
    {
        var data = ShellLinkFixture.Build(localBasePath: @"C:\logs\app.log");

        Assert.That(ShellLinkResolver.TryParseTargets(data, out var targets), Is.True);
        Assert.That(targets.LocalBasePath, Is.EqualTo(@"C:\logs\app.log"));
    }

    [Test]
    public void TryParseTargets_AnsiLinkInfo_ReadsLocalBasePath ()
    {
        var data = ShellLinkFixture.Build(localBasePath: @"C:\logs\app.log", unicode: false);

        Assert.That(ShellLinkResolver.TryParseTargets(data, out var targets), Is.True);
        Assert.That(targets.LocalBasePath, Is.EqualTo(@"C:\logs\app.log"));
    }

    [Test]
    public void TryParseTargets_NameBeforeRelativePath_ReadsRelativePath ()
    {
        // StringData carries no NULL terminator (MS-SHLLINK 2.4). Skipping one used to land four
        // bytes into RELATIVE_PATH, which the old resolution order then hid.
        var data = ShellLinkFixture.Build(name: "a comment", relativePath: @"sub\target.log");

        Assert.That(ShellLinkResolver.TryParseTargets(data, out var targets), Is.True);
        Assert.That(targets.RelativePath, Is.EqualTo(@"sub\target.log"));
    }

    [Test]
    public void TryParseTargets_TruncatedHeader_ReturnsFalse ()
    {
        var data = ShellLinkFixture.Build(relativePath: "app.log");

        Assert.That(ShellLinkResolver.TryParseTargets(data.AsSpan(0, 0x40), out _), Is.False);
    }

    [Test]
    public void TryParseTargets_TruncatedLinkInfo_ReturnsFalse ()
    {
        var data = ShellLinkFixture.Build(localBasePath: @"C:\logs\app.log");

        Assert.That(ShellLinkResolver.TryParseTargets(data.AsSpan(0, 0x4C + 10), out _), Is.False);
    }

    [Test]
    public void TryParseTargets_TruncatedStringData_ReturnsFalse ()
    {
        var data = ShellLinkFixture.Build(relativePath: "a-long-relative-path.log");

        Assert.That(ShellLinkResolver.TryParseTargets(data.AsSpan(0, data.Length - 4), out _), Is.False);
    }

    [Test]
    public void TryParseTargets_CorruptSignature_ReturnsFalse ()
    {
        var data = ShellLinkFixture.Build(relativePath: "app.log", corruptSignature: true);

        Assert.That(ShellLinkResolver.TryParseTargets(data, out _), Is.False);
    }

    #endregion

    #region Candidate paths

    [Test]
    public void BuildCandidatePaths_LocalTarget_ReturnsAbsolutePath ()
    {
        var data = ShellLinkFixture.Build(relativePath: Path.Join("sub", "app.log"));

        Assert.That(ShellLinkResolver.TryParseTargets(data, out var targets), Is.True);
        Assert.That(ShellLinkResolver.BuildCandidatePaths(Path.Join(_directory, "link.lnk"), targets),
            Is.EqualTo(new[] { Path.Join(_directory, "sub", "app.log") }));
    }

    [TestCase(@"\\server\share\app.log")]
    [TestCase(@"\\.\pipe\app.log")]
    [TestCase(@"\\?\C:\logs\app.log")]
    public void BuildCandidatePaths_NonLocalTarget_IsSkipped (string target)
    {
        // Following these would contact another host (leaking the NetNTLM hash) or block on a
        // device. They must not even become candidates.
        var data = ShellLinkFixture.Build(localBasePath: target);

        Assert.That(ShellLinkResolver.TryParseTargets(data, out var targets), Is.True);
        Assert.That(ShellLinkResolver.BuildCandidatePaths(Path.Join(_directory, "link.lnk"), targets), Is.Empty);
    }

    #endregion

    #region Resolution

    [TestCase(true)]
    [TestCase(false)]
    public void Resolve_RelativePathTarget_ReturnsTarget (bool unicode)
    {
        var target = CreateLog(Path.Join("sub", "app.log"));
        var shortcut = CreateShortcut("link.lnk", ShellLinkFixture.Build(relativePath: Path.Join("sub", "app.log"), unicode: unicode));

        Assert.That(ShellLinkResolver.Resolve(shortcut), Is.EqualTo(target));
    }

    [Test]
    public void Resolve_LocalBasePath_ReturnsTarget ()
    {
        var target = CreateLog("app.log");
        var shortcut = CreateShortcut("link.lnk", ShellLinkFixture.Build(localBasePath: target));

        Assert.That(ShellLinkResolver.Resolve(shortcut), Is.EqualTo(target));
    }

    [Test]
    public void Resolve_BasePathWithCommonSuffix_ReturnsTarget ()
    {
        var target = CreateLog("app.log");
        var shortcut = CreateShortcut("link.lnk", ShellLinkFixture.Build(
            localBasePath: _directory,
            commonPathSuffix: "app.log"));

        Assert.That(ShellLinkResolver.Resolve(shortcut), Is.EqualTo(target));
    }

    [Test]
    public void Resolve_NonShortcutPath_ReturnsUnchanged ()
    {
        var missing = Path.Join(_directory, "missing.log");

        Assert.That(ShellLinkResolver.Resolve(missing), Is.EqualTo(missing));
    }

    [Test]
    public void Resolve_MissingTarget_ReturnsShortcutPath ()
    {
        var shortcut = CreateShortcut("link.lnk", ShellLinkFixture.Build(relativePath: "gone.log"));

        Assert.That(ShellLinkResolver.Resolve(shortcut), Is.EqualTo(shortcut));
    }

    [Test]
    public void Resolve_ChainOfShortcuts_ReturnsFinalTarget ()
    {
        var target = CreateLog("app.log");
        _ = CreateShortcut("second.lnk", ShellLinkFixture.Build(relativePath: "app.log"));
        var first = CreateShortcut("first.lnk", ShellLinkFixture.Build(relativePath: "second.lnk"));

        Assert.That(ShellLinkResolver.Resolve(first), Is.EqualTo(target));
    }

    [Test]
    public void Resolve_BrokenSecondLink_ReturnsOriginalInput ()
    {
        // Not the intermediate shortcut: a half-followed chain is no more usable than the input.
        _ = CreateShortcut("second.lnk", ShellLinkFixture.Build(relativePath: "gone.log"));
        var first = CreateShortcut("first.lnk", ShellLinkFixture.Build(relativePath: "second.lnk"));

        Assert.That(ShellLinkResolver.Resolve(first), Is.EqualTo(first));
    }

    [Test]
    public void Resolve_Loop_ReturnsOriginalInput ()
    {
        var first = CreateShortcut("first.lnk", ShellLinkFixture.Build(relativePath: "second.lnk"));
        _ = CreateShortcut("second.lnk", ShellLinkFixture.Build(relativePath: "first.lnk"));

        Assert.That(ShellLinkResolver.Resolve(first), Is.EqualTo(first));
    }

    [Test]
    public void Resolve_CorruptShortcut_ReturnsUnchanged ()
    {
        var shortcut = CreateShortcut("corrupt.lnk", ShellLinkFixture.Build(relativePath: "app.log", corruptSignature: true));

        Assert.That(ShellLinkResolver.Resolve(shortcut), Is.EqualTo(shortcut));
    }

    [Test]
    public void Resolve_OversizedFile_ReturnsUnchanged ()
    {
        var bytes = new byte[(1024 * 1024) + 1];
        ShellLinkFixture.Build(relativePath: "app.log").CopyTo(bytes, 0);
        var shortcut = CreateShortcut("huge.lnk", bytes);

        Assert.That(ShellLinkResolver.Resolve(shortcut), Is.EqualTo(shortcut));
    }

    #endregion
}
