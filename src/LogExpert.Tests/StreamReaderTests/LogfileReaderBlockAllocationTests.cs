using System.Text;

using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;

using NUnit.Framework;

namespace LogExpert.Tests.StreamReaderTests;

[TestFixture]
public class LogfileReaderBlockAllocationTests
{
    private string _tempFile;

    [SetUp]
    public void Setup ()
    {
        _tempFile = Path.GetTempFileName();
        _ = PluginRegistry.PluginRegistry.Create(Path.GetDirectoryName(_tempFile)!, 500);
    }

    [TearDown]
    public void Cleanup ()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Test]
    [TestCase(10)]
    [TestCase(100)]
    [TestCase(1_000)]
    [TestCase(10_000)]
    public void ReadFiles_AllLinesCorrect_WithBlockAllocation (int lineCount)
    {
        GenerateLogFile(_tempFile, lineCount);

        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 100,
            linesPerBuffer: 500,
            new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: Core.Classes.Log.ProgressReporters.NullProgressReporter.Instance);

        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.EqualTo(lineCount));

        // Verify first, middle, and last lines
        VerifyLine(reader, 0, 0);
        VerifyLine(reader, lineCount / 2, lineCount / 2);
        VerifyLine(reader, lineCount - 1, lineCount - 1);
    }

    [Test]
    [TestCase(10)]
    [TestCase(100)]
    [TestCase(1_000)]
    [TestCase(10_000)]
    public void ReadFiles_AllLinesCorrect_WithDirectRead (int lineCount)
    {
        GenerateLogFile(_tempFile, lineCount);

        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 100,
            linesPerBuffer: 500,
            new MultiFileOptions(),
            ReaderType.SystemDirect,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: Core.Classes.Log.ProgressReporters.NullProgressReporter.Instance);

        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.EqualTo(lineCount));

        // Verify first, middle, and last lines
        VerifyLine(reader, 0, 0);
        VerifyLine(reader, lineCount / 2, lineCount / 2);
        VerifyLine(reader, lineCount - 1, lineCount - 1);
    }

    [Test]
    public void ReadFiles_EmptyLines_PreservedCorrectly ()
    {
        File.WriteAllText(_tempFile, "Line 1\n\nLine 3\n\nLine 5\n", Encoding.UTF8);

        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 100,
            linesPerBuffer: 500,
            new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: Core.Classes.Log.ProgressReporters.NullProgressReporter.Instance);

        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.EqualTo(5));

        var line1 = reader.GetLogLineMemory(0);
        var line2 = reader.GetLogLineMemory(1);
        var line3 = reader.GetLogLineMemory(2);

        Assert.That(line1?.FullLine.Span.ToString(), Is.EqualTo("Line 1"));
        Assert.That(line2?.FullLine.IsEmpty, Is.True); // empty line
        Assert.That(line3?.FullLine.Span.ToString(), Is.EqualTo("Line 3"));
    }

    [Test]
    public void ReadFiles_UTF8MultiByte_ContentPreserved ()
    {
        File.WriteAllText(_tempFile, "日本語テスト\nÄÖÜ äöü\nLine 3\n", Encoding.UTF8);

        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 100,
            linesPerBuffer: 500,
            new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: Core.Classes.Log.ProgressReporters.NullProgressReporter.Instance);

        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.EqualTo(3));

        var line1 = reader.GetLogLineMemory(0);
        var line2 = reader.GetLogLineMemory(1);

        Assert.That(line1?.FullLine.Span.ToString(), Is.EqualTo("日本語テスト"));
        Assert.That(line2?.FullLine.Span.ToString(), Is.EqualTo("ÄÖÜ äöü"));
    }

    [Test]
    public void ReadFiles_LongLine_TruncatedToMaxLength ()
    {
        var longLine = new string('X', 1000);
        File.WriteAllText(_tempFile, $"{longLine}\nShort\n", Encoding.UTF8);

        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 100,
            linesPerBuffer: 500,
            new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: Core.Classes.Log.ProgressReporters.NullProgressReporter.Instance);

        reader.ReadFiles();

        var line1 = reader.GetLogLineMemory(0);
        Assert.That(line1?.FullLine.Length, Is.EqualTo(500));
    }

    [Test]
    public void ReadFiles_LineContentBackedByPooledMemory ()
    {
        File.WriteAllText(_tempFile, "Test line content\nSecond line\n", Encoding.UTF8);

        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 100,
            linesPerBuffer: 500,
            new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: Core.Classes.Log.ProgressReporters.NullProgressReporter.Instance);

        reader.ReadFiles();

        var line = reader.GetLogLineMemory(0);
        Assert.That(line, Is.Not.Null);

        // Verify the FullLine memory is array-backed (block-allocated), not string-backed
        var success = System.Runtime.InteropServices.MemoryMarshal.TryGetArray(line.FullLine, out var segment);
        Assert.That(success, Is.True, "LogLine.FullLine should be backed by a char[] block, not a string");
    }

    [Test]
    public void ReadFiles_MultipleBufferRotations_AllLinesCorrect ()
    {
        // With linesPerBuffer=50 and 500 lines, this forces 10 buffer rotations
        GenerateLogFile(_tempFile, 500);

        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 100,
            linesPerBuffer: 50,
            new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: Core.Classes.Log.ProgressReporters.NullProgressReporter.Instance);

        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.EqualTo(500));

        // Spot-check lines across buffer boundaries
        for (var i = 0; i < 500; i += 49) // stride of 49 crosses buffer boundaries
        {
            VerifyLine(reader, i, i);
        }
    }

    private static void VerifyLine (LogfileReader reader, int lineNum, int expectedIndex)
    {
        var line = reader.GetLogLineMemory(lineNum);
        Assert.That(line, Is.Not.Null, $"Line {lineNum} should not be null");
        var text = line.FullLine.Span.ToString();
        Assert.That(text, Does.Contain($"message {expectedIndex}"),
            $"Line {lineNum} content mismatch: '{text}'");
    }

    private static void GenerateLogFile (string path, int lineCount)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8, bufferSize: 65536);
        for (var i = 0; i < lineCount; i++)
        {
            writer.Write("2026-04-23 12:00:00.");
            writer.Write(i % 1000);
            writer.Write(" [INFO] Thread-1 SomeClass - log message ");
            writer.WriteLine(i);
        }
    }
}