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

    /// <summary>
    /// Issue #639: a file with a binary block in the middle must grow like any other file. An overcounted
    /// byte position made the file look smaller than what had been read, so every append was reported as
    /// a new file and the window reloaded and jumped to the top.
    /// </summary>
    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Unit Tests")]
    public void SingleFileReader_GrowingFileWithBinaryBlock_TakesGrowthPathNotNewFile ()
    {
        // No BOM: FileSize counts from after the preamble, so without one it equals the length on disk.
        var binary = new byte[33_000];
        new Random(639).NextBytes(binary);
        using (var stream = new FileStream(_logFile, FileMode.Create, FileAccess.Write))
        {
            stream.Write(Encoding.UTF8.GetBytes(string.Concat(Enumerable.Range(1, 100).Select(index => $"Line {index}\n"))));
            stream.Write(binary);
            stream.Write(Encoding.UTF8.GetBytes("Line after binary\n"));
        }

        using var loadingFinished = new ManualResetEventSlim(false);
        using var grown = new ManualResetEventSlim(false);
        var newFileReported = false;
        var progressReporterMock = new Mock<ILoadProgressReporter>();
        _ = progressReporterMock
            .Setup(reporter => reporter.ReportLoadingFinished())
            .Callback(() => loadingFinished.Set());
        _ = progressReporterMock
            .Setup(reporter => reporter.ReportNewFile(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>()))
            .Callback(() => newFileReported = true);

        using var reader = new LogfileReader(
            _logFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 40,
            linesPerBuffer: 50,
            new MultiFileOptions(),
            ReaderType.SystemDirect,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: progressReporterMock.Object);

        reader.StartMonitoring();
        Assert.That(loadingFinished.Wait(TimeSpan.FromSeconds(5)), Is.True, "Initial load did not finish");
        Assert.That(reader.FileSize, Is.EqualTo(new FileInfo(_logFile).Length), "Size read after the initial load");

        var linesBefore = reader.LineCount;
        reader.FileSizeChanged += (_, args) =>
        {
            if (args.LineCount > linesBefore)
            {
                grown.Set();
            }
        };

        File.AppendAllText(_logFile, "Appended 1\nAppended 2\n", Encoding.UTF8);

        Assert.That(grown.Wait(TimeSpan.FromSeconds(5)), Is.True, "Growth was not reported");
        Assert.Multiple(() =>
        {
            Assert.That(newFileReported, Is.False, "Growth was mistaken for a new file");
            Assert.That(reader.LineCount, Is.EqualTo(linesBefore + 2));
            Assert.That(reader.FileSize, Is.EqualTo(new FileInfo(_logFile).Length), "Size read after the growth");
        });
    }
}
