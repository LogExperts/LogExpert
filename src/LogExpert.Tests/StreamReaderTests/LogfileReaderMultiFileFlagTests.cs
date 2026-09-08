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
    private static readonly string _testDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

    private string _testDirectory = null!;
    private string _logFile = null!;

    [SetUp]
    public void SetUp ()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LogExpertTests", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(_testDirectory);
        _logFile = Path.Combine(_testDirectory, "app.log");

        File.Copy(Path.Combine(_testDataDirectory, "app.log"), _logFile);
        File.Copy(Path.Combine(_testDataDirectory, "app.log.1"), _logFile + ".1");

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
    public void SingleFileCtor_MultiFileTrue_LoadsIndexBeforeExtension ()
    {
        var options = new MultiFileOptions { FormatPattern = "*$J(.).log" };
        File.Copy(Path.Combine(_testDataDirectory, "app.1.log"), Path.Combine(_testDirectory, "app.1.log"));
        File.Copy(Path.Combine(_testDataDirectory, "app.2.log"), Path.Combine(_testDirectory, "app.2.log"));
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

}
