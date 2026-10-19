using System.Text;

using LogExpert.Core.Classes.Log;
using LogExpert.Core.Classes.Log.ProgressReporters;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;

using NUnit.Framework;

namespace LogExpert.Tests.StreamReaderTests;

[TestFixture]
internal sealed class LogfileReaderMultiFileFlagTests
{
    private static readonly string _testDataDirectory = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "TestData");

    private string _testDirectory = null!;
    private string _logFile = null!;

    [SetUp]
    public void SetUp ()
    {
        _testDirectory = Path.Join(Path.GetTempPath(), "LogExpertTests", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(_testDirectory);
        _logFile = Path.Join(_testDirectory, "app.log");

        File.Copy(Path.Join(_testDataDirectory, "app.log"), _logFile);
        File.Copy(Path.Join(_testDataDirectory, "app.log.1"), _logFile + ".1");
        File.SetLastWriteTimeUtc(_logFile, DateTime.UtcNow);
        File.SetLastWriteTimeUtc(_logFile + ".1", DateTime.UtcNow);

        _ = PluginRegistry.PluginRegistry.Create(_testDirectory, 500);
    }

    [TearDown]
    public void TearDown ()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Test]
    public void SingleFileCtor_MultiFileFalse_DoesNotExpandRollover ()
    {
        using var reader = CreateSingleFileReader(multiFile: false);

        reader.ReadFiles();

        Assert.Multiple(() =>
        {
            Assert.That(reader.IsMultiFile, Is.False);
            Assert.That(reader.GetLogFileInfoList(), Has.Count.EqualTo(1));
            Assert.That(reader.LineCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SingleFileCtor_MultiFileTrue_ExpandsRollover ()
    {
        using var reader = CreateSingleFileReader(multiFile: true);

        reader.ReadFiles();

        Assert.Multiple(() =>
        {
            Assert.That(reader.IsMultiFile, Is.True);
            Assert.That(reader.GetLogFileInfoList(), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void SingleFileCtor_MultiFileTrue_ExcludesIndexAfterExtensionFilesOlderThanMaxDays ()
    {
        var oldRolloverFile = _logFile + ".2";
        File.Copy(Path.Combine(_testDataDirectory, "app.log.2"), oldRolloverFile);
        var now = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(_logFile, now);
        File.SetLastWriteTimeUtc(_logFile + ".1", now.AddDays(-2));
        File.SetLastWriteTimeUtc(oldRolloverFile, now.AddDays(-4));
        MultiFileOptions options = new()
        {
            FormatPattern = "*$J(.)",
            MaxDayTry = 3
        };
        using var reader = CreateSingleFileReader(multiFile: true, options);

        reader.ReadFiles();

        Assert.Multiple(() =>
        {
            Assert.That(reader.IsMultiFile, Is.True);
            Assert.That(reader.GetLogFileInfoList().Select(file => Path.GetFileName(file.FullName)),
                Is.EqualTo(new[] { "app.log.1", "app.log" }));
        });
    }

    [Test]
    public void SingleFileCtor_MultiFileTrue_LoadsIndexBeforeExtension ()
    {
        var options = new MultiFileOptions { FormatPattern = "*$J(.).log" };
        var (firstRolloverFile, secondRolloverFile) = CopyIndexBeforeExtensionFiles();
        File.SetLastWriteTimeUtc(firstRolloverFile, DateTime.UtcNow);
        File.SetLastWriteTimeUtc(secondRolloverFile, DateTime.UtcNow);
        using var reader = CreateSingleFileReader(multiFile: true, options);

        reader.ReadFiles();

        Assert.Multiple(() =>
        {
            Assert.That(reader.IsMultiFile, Is.True);
            Assert.That(reader.GetLogFileInfoList().Select(file => Path.GetFileName(file.FullName)),
                Is.EqualTo(new[] { "app.2.log", "app.1.log", "app.log" }));
        });
    }

    [Test]
    public void SingleFileCtor_MultiFileTrue_ExcludesIndexBeforeExtensionFilesOlderThanMaxDays ()
    {
        var options = new MultiFileOptions
        {
            FormatPattern = "*$J(.).log",
            MaxDayTry = 3
        };
        var (firstRolloverFile, secondRolloverFile) = CopyIndexBeforeExtensionFiles();
        var now = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(_logFile, now);
        File.SetLastWriteTimeUtc(firstRolloverFile, now.AddDays(-2));
        File.SetLastWriteTimeUtc(secondRolloverFile, now.AddDays(-4));
        using var reader = CreateSingleFileReader(multiFile: true, options);

        reader.ReadFiles();

        Assert.Multiple(() =>
        {
            Assert.That(reader.IsMultiFile, Is.True);
            Assert.That(reader.GetLogFileInfoList().Select(file => Path.GetFileName(file.FullName)),
                Is.EqualTo(new[] { "app.1.log", "app.log" }));
        });
    }

    [Test]
    public void MultiFileCtor_AlwaysMultiFile ()
    {
        using var reader = new LogfileReader(
            [_logFile],
            new EncodingOptions { Encoding = Encoding.UTF8 },
            bufferCount: 40,
            linesPerBuffer: 50,
            new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: NullProgressReporter.Instance);

        reader.ReadFiles();

        Assert.Multiple(() =>
        {
            Assert.That(reader.IsMultiFile, Is.True);
            Assert.That(reader.GetLogFileInfoList(), Has.Count.EqualTo(2));
        });
    }

    private LogfileReader CreateSingleFileReader (bool multiFile, MultiFileOptions? options = null)
    {
        return new LogfileReader(
            _logFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile,
            bufferCount: 40,
            linesPerBuffer: 50,
            options ?? new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: NullProgressReporter.Instance);
    }

    private (string FirstRolloverFile, string SecondRolloverFile) CopyIndexBeforeExtensionFiles ()
    {
        var firstRolloverFile = Path.Combine(_testDirectory, "app.1.log");
        var secondRolloverFile = Path.Combine(_testDirectory, "app.2.log");
        File.Copy(Path.Combine(_testDataDirectory, "app.1.log"), firstRolloverFile);
        File.Copy(Path.Combine(_testDataDirectory, "app.2.log"), secondRolloverFile);
        return (firstRolloverFile, secondRolloverFile);
    }

}
