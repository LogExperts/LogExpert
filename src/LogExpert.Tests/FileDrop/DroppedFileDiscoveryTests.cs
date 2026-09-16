using LogExpert.Core.Classes.FileDrop;

using NUnit.Framework;

namespace LogExpert.Tests.FileDrop;

[TestFixture]
public class DroppedFileDiscoveryTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp ()
    {
        _directory = Path.Join(Path.GetTempPath(), "LogExpertDropTests", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown ()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public async Task DiscoverAsync_FolderTree_ReturnsFilesInsteadOfDirectories ()
    {
        var first = CreateFile("a.log");
        var nested = CreateFile(Path.Join("nested", "b.txt"));

        var result = await new DroppedFileDiscovery().DiscoverAsync([_directory]).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.IncludesFolders, Is.True);
            Assert.That(result.Files, Is.EqualTo([first, nested]));
            Assert.That(result.Skipped, Is.Empty);
        });
    }

    [Test]
    public async Task DiscoverAsync_MixedOverlappingDrops_PreparesOneNormalizedEligibleSelection ()
    {
        var log = CreateFile("a.log");
        var rotated = CreateFile("a.log.1");
        var extensionless = CreateFile(Path.Join("nested", "output"));
        var sessionFile = CreateFile("a.lxp");
        _ = CreateFile("skip.LXP");
        _ = CreateFile("skip.LXJ");
        var session = CreateFile("explicit.lxj");

        var result = await new DroppedFileDiscovery().DiscoverAsync([_directory, Path.Join(_directory, "nested"), Path.Join(_directory, ".", "a.log"), log.ToUpperInvariant(), sessionFile, session]).ConfigureAwait(false);

        Assert.That(result.Files, Is.EqualTo([log, rotated, sessionFile, session, extensionless]));
    }

    [Test]
    public async Task DiscoverAsync_UnavailableEntriesAndDirectoryLinks_ReportsSkipsAndKeepsAccessibleFiles ()
    {
        var good = CreateFile("good.log");
        var inaccessible = CreateFile(Path.Join("denied", "hidden.log"));
        var link = Path.Join(_directory, "link");
        _ = Directory.CreateDirectory(link);
        var missing = Path.Join(_directory, "disappeared.log");
        var deniedDirectory = Path.GetDirectoryName(inaccessible)!;
        var discovery = new DroppedFileDiscovery(
            path => path == link ? FileAttributes.Directory | FileAttributes.ReparsePoint : File.GetAttributes(path),
            path => path == deniedDirectory ? throw new UnauthorizedAccessException() : Directory.EnumerateFileSystemEntries(path));

        var result = await discovery.DiscoverAsync([_directory, missing]).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Files, Is.EqualTo([good]));
            Assert.That(result.Skipped.Select(skip => skip.Path), Is.EquivalentTo(new[] { deniedDirectory, link, missing }));
        });
    }
    [Test]
    public void DiscoverAsync_CancelledBeforeStart_DoesNotAccessTheFileSystem ()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var discovery = new DroppedFileDiscovery(_ => throw new AssertionException("Filesystem was accessed"));

        Assert.That(async () => await discovery.DiscoverAsync([_directory], cancellation.Token).ConfigureAwait(false), Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void DiscoverAsync_CancelledDuringEnumeration_DiscardsPartialResults ()
    {
        using var cancellation = new CancellationTokenSource();
        var first = CreateFile("first.log");
        IEnumerable<string> enumerate (string path)
        {
            yield return first;
            cancellation.Cancel();
            yield return Path.Join(path, "next.log");
        }

        var discovery = new DroppedFileDiscovery(enumerateEntries: enumerate);

        Assert.That(async () => await discovery.DiscoverAsync([_directory], cancellation.Token).ConfigureAwait(false), Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task DiscoverAsync_IteratorFailsAfterAFile_KeepsDiscoveredContentAndReportsDirectory ()
    {
        var first = CreateFile("first.log");
        IEnumerable<string> enumerate (string path)
        {
            yield return first;
            throw new DirectoryNotFoundException(path);
        }

        var result = await new DroppedFileDiscovery(enumerateEntries: enumerate).DiscoverAsync([_directory]).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Files, Is.EqualTo([first]));
            Assert.That(result.Skipped.Select(skip => skip.Path), Is.EqualTo([_directory]));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task DiscoverAsync_EmptyOrOnlySessionFiles_ReturnsEmptyFolderResult (bool addSessionFiles)
    {
        if (addSessionFiles)
        {
            _ = CreateFile("state.lxp");
            _ = CreateFile("session.lxj");
        }

        var result = await new DroppedFileDiscovery().DiscoverAsync([_directory]).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.IncludesFolders, Is.True);
            Assert.That(result.Files, Is.Empty);
            Assert.That(result.Skipped, Is.Empty);
        });
    }

    [Test]
    public async Task DiscoverAsync_ExplicitFilesOnly_PreservesSessionPathsAndDoesNotRequestPreview ()
    {
        var log = CreateFile("a.txt");
        var sessionFile = CreateFile("b.lxp");
        var session = CreateFile("c.lxj");

        var result = await new DroppedFileDiscovery().DiscoverAsync([session, log, sessionFile, log]).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(result.IncludesFolders, Is.False);
            Assert.That(result.Files, Is.EqualTo([log, sessionFile, session]));
        });
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
    public async Task DiscoverAsync_BlockedFileSystem_DoesNotBlockCaller ()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        var discovery = new DroppedFileDiscovery(path =>
        {
            entered.Set();
            _ = release.Wait(TimeSpan.FromSeconds(10));
            return File.GetAttributes(path);
        });

        var task = discovery.DiscoverAsync([_directory], cancellation.Token);
        try
        {
            Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(task.IsCompleted, Is.False);
            await cancellation.CancelAsync().ConfigureAwait(false);
        }
        finally
        {
            release.Set();
        }

        try
        {
            _ = await task.ConfigureAwait(false);
            Assert.Fail("Cancelled discovery published a result");
        }
        catch (OperationCanceledException)
        {
        }
    }
    private string CreateFile (string relativePath)
    {
        var path = Path.Join(_directory, relativePath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "log line");
        return path;
    }
}
