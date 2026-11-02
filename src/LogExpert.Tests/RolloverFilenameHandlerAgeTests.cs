using ColumnizerLib;

using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests;

[TestFixture]
internal sealed class RolloverFilenameHandlerAgeTests
{
    private const string LOG_DIRECTORY = "/logs";
    private static readonly DateTimeOffset _now = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    [TestCase("*$J(.)", "app.log.1", "app.log.2")]
    [TestCase("*$J(.).log", "app.1.log", "app.2.log")]
    public void GetNameList_MaxFileAge_StopsAtFirstOldIndexedFile (
        string pattern,
        string recentFileName,
        string oldFileName)
    {
        var context = CreateContext(
            "app.log",
            new MultiFileOptions { FormatPattern = pattern, MaxFileAgeDays = 5 },
            (recentFileName, _now.AddDays(-2).UtcDateTime),
            (oldFileName, _now.AddDays(-10).UtcDateTime));

        var result = context.Handler.GetNameList(context.Registry);

        Assert.That(result, Is.EqualTo(new[] { GetPath(recentFileName), GetPath("app.log") }));
    }

    [Test]
    public void GetNameList_ZeroMaxFileAge_LoadsAllIndexedFiles ()
    {
        var context = CreateContext(
            "app.log",
            new MultiFileOptions { FormatPattern = "*$J(.)", MaxFileAgeDays = 0 },
            ("app.log.1", _now.AddDays(-10).UtcDateTime),
            ("app.log.2", _now.AddDays(-20).UtcDateTime));

        var result = context.Handler.GetNameList(context.Registry);

        Assert.That(result, Is.EqualTo(new[] { GetPath("app.log.2"), GetPath("app.log.1"), GetPath("app.log") }));
    }

    [Test]
    public void GetNameList_FileAgeAboveMaximum_IsClampedWithoutOverflow ()
    {
        var context = CreateContext(
            "app.log",
            new MultiFileOptions { FormatPattern = "*$J(.)", MaxFileAgeDays = int.MaxValue },
            ("app.log.1", _now.AddDays(-10).UtcDateTime));

        var result = context.Handler.GetNameList(context.Registry);

        Assert.That(result, Is.EqualTo(new[] { GetPath("app.log.1"), GetPath("app.log") }));
    }

    [Test]
    public void GetNameList_FirstRolloverIsOld_StopsBeforeNewerHigherIndex ()
    {
        var context = CreateContext(
            "app.log",
            new MultiFileOptions { FormatPattern = "*$J(.)", MaxFileAgeDays = 5 },
            ("app.log.1", _now.AddDays(-10).UtcDateTime),
            ("app.log.2", _now.AddDays(-1).UtcDateTime));

        var result = context.Handler.GetNameList(context.Registry);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new[] { GetPath("app.log") }));
            Assert.That(context.RequestedPaths, Does.Not.Contain(GetPath("app.log.2")));
        });
    }

    [Test]
    public void GetNameList_OpenedFileIsOld_StillIncludesOpenedFile ()
    {
        var context = CreateContext(
            "app.log",
            new MultiFileOptions { FormatPattern = "*$J(.)", MaxFileAgeDays = 5 },
            activeLastWriteTimeUtc: _now.AddDays(-10).UtcDateTime);

        var result = context.Handler.GetNameList(context.Registry);

        Assert.That(result, Is.EqualTo(new[] { GetPath("app.log") }));
    }

    [Test]
    public void GetNameList_DatePattern_SkipsConfiguredGapAndStopsAtFirstOldFile ()
    {
        var context = CreateContext(
            "app_2026-10-19.log",
            new MultiFileOptions
            {
                FormatPattern = "*$D(yyyy-MM-dd).log",
                MaxDayTry = 3,
                MaxFileAgeDays = 5
            },
            ("app_2026-10-17.log", _now.AddDays(-2).UtcDateTime),
            ("app_2026-10-16.log", _now.AddDays(-10).UtcDateTime));

        var result = context.Handler.GetNameList(context.Registry);

        Assert.That(result, Is.EqualTo(new[] { GetPath("app_2026-10-17.log"), GetPath("app_2026-10-19.log") }));
    }

    [Test]
    public void GetNameList_UnknownLastWriteTime_IncludesFile ()
    {
        var context = CreateContext(
            "app.log",
            new MultiFileOptions { FormatPattern = "*$J(.)", MaxFileAgeDays = 5 },
            ("app.log.1", null));

        var result = context.Handler.GetNameList(context.Registry);

        Assert.That(result, Is.EqualTo(new[] { GetPath("app.log.1"), GetPath("app.log") }));
    }

    [Test]
    public void GetNameList_MixedPattern_OldIndexedFileStopsBeforeDateFallback ()
    {
        var previousDateFile = "app_2026-10-18.log";
        var context = CreateContext(
            "app_2026-10-19.log",
            new MultiFileOptions
            {
                FormatPattern = "*$D(yyyy-MM-dd)$J(.).log",
                MaxDayTry = 3,
                MaxFileAgeDays = 5
            },
            ("app_2026-10-19.1.log", _now.AddDays(-10).UtcDateTime),
            (previousDateFile, _now.AddDays(-1).UtcDateTime));

        var result = context.Handler.GetNameList(context.Registry);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new[] { GetPath("app_2026-10-19.log") }));
            Assert.That(context.RequestedPaths, Does.Not.Contain(GetPath(previousDateFile)));
        });
    }

    private static TestContext CreateContext (
        string activeFileName,
        MultiFileOptions options,
        params (string FileName, DateTime? LastWriteTimeUtc)[] rolloverFiles)
    {
        return CreateContext(activeFileName, options, _now.UtcDateTime, rolloverFiles);
    }

    private static TestContext CreateContext (
        string activeFileName,
        MultiFileOptions options,
        DateTime? activeLastWriteTimeUtc,
        params (string FileName, DateTime? LastWriteTimeUtc)[] rolloverFiles)
    {
        var activeFile = CreateFileInfo(activeFileName, activeLastWriteTimeUtc);
        var files = rolloverFiles.ToDictionary(file => GetPath(file.FileName),
            file => CreateFileInfo(file.FileName, file.LastWriteTimeUtc));
        List<string> requestedPaths = [];

        var fileSystem = new Mock<IFileSystemPlugin>();
        _ = fileSystem.Setup(fs => fs.GetLogfileInfo(It.IsAny<string>()))
            .Returns((string path) =>
            {
                requestedPaths.Add(path);
                return files.GetValueOrDefault(path)?.Object;
            });

        var registry = new Mock<IPluginRegistry>();
        _ = registry.Setup(r => r.FindFileSystemForUri(It.IsAny<string>())).Returns(fileSystem.Object);

        RolloverFilenameHandler handler = new(activeFile.Object, options, new FixedTimeProvider(_now));
        return new TestContext(handler, registry.Object, requestedPaths);
    }

    private static Mock<ILogFileInfo> CreateFileInfo (string fileName, DateTime? lastWriteTimeUtc)
    {
        var fileInfo = new Mock<ILogFileInfo>();
        _ = fileInfo.SetupGet(file => file.FileName).Returns(fileName);
        _ = fileInfo.SetupGet(file => file.DirectoryName).Returns(LOG_DIRECTORY);
        _ = fileInfo.SetupGet(file => file.DirectorySeparatorChar).Returns('/');
        _ = fileInfo.SetupGet(file => file.FileExists).Returns(true);
        _ = fileInfo.SetupGet(file => file.LastWriteTimeUtc).Returns(lastWriteTimeUtc);
        return fileInfo;
    }

    private static string GetPath (string fileName) => $"{LOG_DIRECTORY}/{fileName}";

    private sealed record TestContext (
        RolloverFilenameHandler Handler,
        IPluginRegistry Registry,
        List<string> RequestedPaths);

    private sealed class FixedTimeProvider (DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow () => utcNow;
    }
}
