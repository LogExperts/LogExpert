using System.Text;

using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;
using LogExpert.PluginRegistry.FileSystem;

using NUnit.Framework;

namespace LogExpert.Tests.Buffers;

[TestFixture]
internal class BufferShiftTest : RolloverHandlerTestBase
{
    [TearDown]
    public void TearDown ()
    {
        Cleanup();
    }

    [OneTimeSetUp]
    public void Boot ()
    {
        Cleanup();
    }

    [Test]
    [TestCase(ReaderType.System)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Test")]
    //[TestCase(ReaderType.Legacy)] Legacy Reader does not Support this
    //TO Test real life scenario, use the LogRotator tool, in the src/Tools/LogRotator directory,
    //to create files and perform rollovers while watching the files in LogExpert with MultiFile enabled
    //(pattern: *$J(.))
    public void TestShiftBuffers1 (ReaderType readerType)
    {
        var linesPerFile = 10;
        MultiFileOptions options = new()
        {
            MaxDayTry = 0,
            FormatPattern = "*$J(.)"
        };

        var files = CreateTestFilesWithoutDate();

        EncodingOptions encodingOptions = new()
        {
            Encoding = Encoding.Default
        };

        _ = PluginRegistry.PluginRegistry.Create(TestDirectory.FullName, 500);
        LogfileReader reader = new(files.Last.Value, encodingOptions, true, 40, 50, options, readerType, PluginRegistry.PluginRegistry.Instance, 500);
        reader.ReadFiles();

        var lil = reader.GetLogFileInfoList();
        Assert.That(lil.Count, Is.EqualTo(files.Count));

        var enumerator = files.GetEnumerator();
        _ = enumerator.MoveNext();

        foreach (var li in lil.Cast<LogFileInfo>())
        {
            var fileName = enumerator.Current;
            Assert.That(li.FullName, Is.EqualTo(fileName));
            _ = enumerator.MoveNext();
        }

        var oldCount = lil.Count;

        // Simulate rollover
        files = RolloverSimulation(files, "*$J(.)", false);

        // Simulate rollover detection
        _ = reader.ShiftBuffers();

        lil = reader.GetLogFileInfoList();

        Assert.That(lil.Count, Is.EqualTo(oldCount + 1));

        Assert.That(reader.LineCount, Is.EqualTo(linesPerFile * lil.Count));

        // Check if rollover'd file names have been handled by LogfileReader
        Assert.That(lil.Count, Is.EqualTo(files.Count));
        enumerator = files.GetEnumerator();
        _ = enumerator.MoveNext();

        foreach (LogFileInfo li in lil.Cast<LogFileInfo>())
        {
            var fileName = enumerator.Current;
            Assert.That(li.FullName, Is.EqualTo(fileName));
            _ = enumerator.MoveNext();
        }

        // Check if file buffers have correct files. Assuming here that one buffer fits for a complete file
        enumerator = files.GetEnumerator();
        _ = enumerator.MoveNext();

        var snapshot = reader.BufferIndex.CreateSnapshot();
        var startLine = 0;

        foreach (var logBuffer in snapshot.Buffers)
        {
            Assert.That(enumerator.Current, Is.EqualTo(logBuffer.FileName));
            Assert.That(logBuffer.StartLine, Is.EqualTo(startLine));
            startLine += 10;
            _ = enumerator.MoveNext();
        }

        // Checking file content
        enumerator = files.GetEnumerator();
        _ = enumerator.MoveNext();
        _ = enumerator.MoveNext(); // move to 2nd entry. The first file now contains 2nd file's content (because rollover)

        snapshot = reader.BufferIndex.CreateSnapshot();
        int i;

        for (i = 0; i < snapshot.Buffers.Count - 2; ++i)
        {
            var logBuffer = snapshot.Buffers[i];
            var line = reader.GetLogLineMemory(logBuffer.StartLine);
            if (line == null)
            {
                Assert.Fail("Expected first block line to be present.");
                continue;
            }

            Assert.That(line.FullLine.Span.Contains(enumerator.Current.AsSpan(), StringComparison.Ordinal));
            _ = enumerator.MoveNext();
        }

        // the last 2 files now contain the content of the previously watched file
        for (; i < snapshot.Buffers.Count; ++i)
        {
            var logBuffer = snapshot.Buffers[i];
            var line = reader.GetLogLineMemory(logBuffer.StartLine);

            if (line == null)
            {
                Assert.Fail("Expected first block line to be present.");
                continue;
            }

            Assert.That(line.FullLine.Span.Contains(enumerator.Current.AsSpan(), StringComparison.Ordinal));
        }

        oldCount = lil.Count;

        // Simulate rollover again - now latest file will be deleted (simulates logger's rollover history limit)
        files = RolloverSimulation(files, "*$J(.)", true);

        // Simulate rollover detection
        _ = reader.ShiftBuffers();
        lil = reader.GetLogFileInfoList();

        Assert.That(lil.Count, Is.EqualTo(oldCount)); // same count because oldest file is deleted
        Assert.That(lil.Count, Is.EqualTo(files.Count));
        Assert.That(reader.LineCount, Is.EqualTo(linesPerFile * lil.Count));

        // Check first line to see if buffers are correct
        var firstLine = reader.GetLogLineMemory(0);
        var names = new string[files.Count];
        files.CopyTo(names, 0);
        Assert.That(firstLine.FullLine.Span.Contains(names[2].AsSpan(), StringComparison.Ordinal));
    }
}