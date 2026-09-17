using System.Diagnostics;
using System.Text;

using ColumnizerLib;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Marker;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArguments;
using LogExpert.Core.Interfaces;

using NUnit.Framework;

namespace LogExpert.Tests.Marker;

[TestFixture]
public class MarkerPerformanceTests
{
    private const int INITIAL_LINE_COUNT = 500_000;
    private const int APPENDED_LINE_COUNT = 2_000;

    [Test, Explicit("Opt-in marker indexing performance experiment")]
    public async Task MarkerIndex_ScanAndAppend_DenseMatchesReportPerformanceAndCorrectness ()
    {
        // This uses an in-memory reader seam to keep the experiment independent of file watcher timing and buffering.
        var reader = new InMemoryBenchmarkReader(INITIAL_LINE_COUNT);
        using var index = new MarkerIndex();
        index.Reset(reader, MarkerCriteria.ForHighlights(
        [
            new HighlightEntry { SearchText = "^never-match-a$", IsRegex = true, BackgroundColor = System.Drawing.Color.Red },
            new HighlightEntry { SearchText = "^never-match-b$", IsRegex = true, BackgroundColor = System.Drawing.Color.Blue },
            new HighlightEntry { SearchText = "\\babsent\\b", IsRegex = true, BackgroundColor = System.Drawing.Color.Green },
            new HighlightEntry { SearchText = "COMMON-\\d+", IsRegex = true, BackgroundColor = System.Drawing.Color.Yellow }
        ]));

        var scanMemoryBefore = GC.GetTotalMemory(true);
        var scanAllocatedBefore = GC.GetTotalAllocatedBytes(true);
        var scanTimer = Stopwatch.StartNew();
        await index.UpdateAsync(INITIAL_LINE_COUNT).ConfigureAwait(false);
        scanTimer.Stop();
        var scanMemoryAfter = GC.GetTotalMemory(true);
        var scanAllocatedAfter = GC.GetTotalAllocatedBytes(true);

        var initialSnapshot = index.Snapshot;
        var initialMatches = initialSnapshot.Matches.ToArray();
        var initialBuckets = MarkerBucket.Aggregate(initialMatches, INITIAL_LINE_COUNT, 2_000, System.Drawing.Color.Black.ToArgb());

        reader.Append(APPENDED_LINE_COUNT);
        var appendMemoryBefore = GC.GetTotalMemory(true);
        var appendAllocatedBefore = GC.GetTotalAllocatedBytes(true);
        var appendTimer = Stopwatch.StartNew();
        await index.UpdateAsync(reader.LineCount).ConfigureAwait(false);
        appendTimer.Stop();
        var appendMemoryAfter = GC.GetTotalMemory(true);
        var appendAllocatedAfter = GC.GetTotalAllocatedBytes(true);

        var appendedSnapshot = index.Snapshot;
        var appendedMatches = appendedSnapshot.Matches.ToArray();
        var appendedBuckets = MarkerBucket.Aggregate(appendedMatches, reader.LineCount, 2_000, System.Drawing.Color.Black.ToArgb());

        Assert.Multiple(() =>
        {
            Assert.That(initialSnapshot.Error, Is.Null);
            Assert.That(initialSnapshot.ScannedLineCount, Is.EqualTo(INITIAL_LINE_COUNT));
            Assert.That(initialMatches, Has.Length.EqualTo(INITIAL_LINE_COUNT), "Every dense line must match.");
            Assert.That(appendedSnapshot.Error, Is.Null);
            Assert.That(appendedSnapshot.ScannedLineCount, Is.EqualTo(INITIAL_LINE_COUNT + APPENDED_LINE_COUNT));
            Assert.That(appendedMatches, Has.Length.EqualTo(INITIAL_LINE_COUNT + APPENDED_LINE_COUNT));
            Assert.That(appendedMatches[^1].LineNumber, Is.EqualTo(INITIAL_LINE_COUNT + APPENDED_LINE_COUNT - 1));
            Assert.That(reader.LineCount, Is.EqualTo(INITIAL_LINE_COUNT + APPENDED_LINE_COUNT));
            Assert.That(reader.FileSize, Is.GreaterThan(0));
            Assert.That(initialBuckets, Is.Not.Empty);
            Assert.That(appendedBuckets, Is.Not.Empty);
            Assert.That(initialBuckets.Sum(bucket => bucket.Count), Is.EqualTo(initialMatches.Length));
            Assert.That(appendedBuckets.Sum(bucket => bucket.Count), Is.EqualTo(appendedMatches.Length));
        });

        await TestContext.Progress.WriteLineAsync(
            $"Marker experiment: initial lines={INITIAL_LINE_COUNT}, appended={APPENDED_LINE_COUNT}, " +
            $"initial bytes={reader.InitialFileSize}, final bytes={reader.FileSize}, " +
            $"initial matches={initialMatches.Length}, final matches={appendedMatches.Length}, " +
            $"initial buckets={initialBuckets.Count}, final buckets={appendedBuckets.Count}, " +
            $"scan elapsed={scanTimer.Elapsed}, append elapsed={appendTimer.Elapsed}, " +
            $"scan managed delta={scanMemoryAfter - scanMemoryBefore}, append managed delta={appendMemoryAfter - appendMemoryBefore}, " +
            $"scan allocations={scanAllocatedAfter - scanAllocatedBefore}, append allocations={appendAllocatedAfter - appendAllocatedBefore}").ConfigureAwait(false);
    }

    private sealed class InMemoryBenchmarkReader : ILogfileReader
    {
        private readonly List<string> _lines;

        public InMemoryBenchmarkReader (int lineCount)
        {
            _lines = Enumerable.Range(0, lineCount).Select(CreateLine).ToList();
            InitialFileSize = FileSize;
        }

        public event EventHandler<LogEventArgs> FileSizeChanged
        {
            add { }
            remove { }
        }

        public event EventHandler<LoadFileEventArgs> LoadFile
        {
            add { }
            remove { }
        }

        public event EventHandler<LoadFileEventArgs> LoadingStarted
        {
            add { }
            remove { }
        }

        public event EventHandler<EventArgs> LoadingFinished
        {
            add { }
            remove { }
        }

        public event EventHandler<EventArgs> FileNotFound
        {
            add { }
            remove { }
        }

        public event EventHandler<EventArgs> Respawned
        {
            add { }
            remove { }
        }

        public int LineCount => _lines.Count;

        public bool IsMultiFile => false;

        public Encoding CurrentEncoding => Encoding.UTF8;

        // The model reports UTF-8 content with a two-byte CRLF terminator per line.
        public long FileSize => _lines.Sum(line => (long)CurrentEncoding.GetByteCount(line) + 2);

        public long InitialFileSize { get; }

        public ILogLineMemory GetLogLineMemory (int lineNum)
        {
            return lineNum >= 0 && lineNum < _lines.Count ? new LogLine(_lines[lineNum], lineNum) : null!;
        }

        public Task<ILogLineMemory> GetLogLineMemoryWithWait (int lineNum)
        {
            return Task.FromResult(GetLogLineMemory(lineNum));
        }

        public ILogLineMemory[] GetLogLineMemories (int startLine, int count)
        {
            return Enumerable.Range(startLine, count)
                .Select(GetLogLineMemory)
                .Where(line => line != null)
                .ToArray();
        }

        public void StartMonitoring () { }

        public void StopMonitoring () { }

        public void StopMonitoringAsync () { }

        public void DeleteAllContent () => _lines.Clear();

        public void Dispose () { }

        public void Append (int count)
        {
            var start = _lines.Count;
            _lines.AddRange(Enumerable.Range(start, count).Select(CreateLine));
        }

        private static string CreateLine (int lineNumber) => $"COMMON-{lineNumber:D6} dense marker";
    }
}