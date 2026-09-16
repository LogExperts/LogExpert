using System.Text;

using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;
using LogExpert.Core.Interfaces;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.StreamReaderTests;

[TestFixture]
internal sealed class LogfileReaderSingleFileMonitoringTests
{
    private string _testDirectory = null!;
    private string _logFile = null!;

    [SetUp]
    public void SetUp ()
    {
        _testDirectory = Path.Join(Path.GetTempPath(), "LogExpertTests", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(_testDirectory);
        _logFile = Path.Join(_testDirectory, "app.log");
        File.WriteAllLines(_logFile, Enumerable.Range(1, 100).Select(index => $"Line {index}"), Encoding.UTF8);

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
    public void SingleFileReader_OnTruncation_ReportsNewFileNotRollover ()
    {
        using var loadingFinished = new ManualResetEventSlim(false);
        using var newFileReported = new ManualResetEventSlim(false);
        var progressReporterMock = new Mock<ILoadProgressReporter>();
        _ = progressReporterMock
            .Setup(reporter => reporter.ReportLoadingFinished())
            .Callback(() => loadingFinished.Set());
        _ = progressReporterMock
            .Setup(reporter => reporter.ReportNewFile(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>()))
            .Callback(() => newFileReported.Set());

        using var reader = new LogfileReader(
            _logFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 40,
            linesPerBuffer: 50,
            new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: progressReporterMock.Object);

        var rolloverReported = false;
        reader.FileSizeChanged += (_, args) => rolloverReported |= args.IsRollover;

        reader.StartMonitoring();
        Assert.That(loadingFinished.Wait(TimeSpan.FromSeconds(5)), Is.True, "Initial load did not finish");

        File.WriteAllText(_logFile, "replacement\n", Encoding.UTF8);

        Assert.That(newFileReported.Wait(TimeSpan.FromSeconds(5)), Is.True, "Truncation did not report a new file");
        Assert.That(rolloverReported, Is.False);
    }
}
