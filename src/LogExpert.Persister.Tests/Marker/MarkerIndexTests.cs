using System.Drawing;
using System.Text.RegularExpressions;

using ColumnizerLib;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Marker;
using LogExpert.Core.Interfaces;

using Moq;

namespace LogExpert.Persister.Tests.Marker;

[TestFixture]
public class MarkerIndexTests
{
    [Test]
    public async Task Search_RegexTimeoutIsReportedAndStopsDiscovery ()
    {
        using var index = new MarkerIndex();
        index.Reset(ReaderOf(new string('a', 50_000) + "!"), MarkerCriteria.ForSearch(
            new Core.Entities.SearchParams { SearchText = "^(a+)+$", IsRegex = true }, Color.Blue.ToArgb()));
        await index.UpdateAsync(1).ConfigureAwait(false);
        Assert.That(index.Snapshot.Error, Is.InstanceOf<RegexMatchTimeoutException>());
        Assert.That(index.IsScanning, Is.False);
    }

    [Test]
    public async Task Update_AppendsMatchesWithoutReadingTheOldPrefixAgain ()
    {
        List<string> lines = ["hit", "miss", "hit"];
        List<int> reads = [];
        var reader = new Mock<ILogfileReader>();
        _ = reader.Setup(source => source.GetLogLineMemory(It.IsAny<int>())).Returns((int line) =>
        {
            reads.Add(line);
            return new LogLine(lines[line], line);
        });
        using var index = new MarkerIndex();
        index.Reset(reader.Object, MarkerCriteria.ForHighlights(
            [new HighlightEntry { SearchText = "hit", BackgroundColor = Color.Red }]));

        await index.UpdateAsync(3).ConfigureAwait(false);
        var first = index.Snapshot;
        lines.Add("hit");
        reads.Clear();
        await index.UpdateAsync(4).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(first.Matches.Select(match => match.LineNumber), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(index.Snapshot.Matches.Select(match => match.LineNumber), Is.EqualTo(new[] { 0, 2, 3 }));
            Assert.That(reads, Is.EqualTo(new[] { 2, 3 }), "Only the unfinished last line and appended lines need reevaluation.");
        });
    }

    [Test]
    public async Task Reset_AnOldBlockedScanCannotReplaceTheNewFilesMarkers ()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var oldReader = new Mock<ILogfileReader>();
        _ = oldReader.Setup(reader => reader.GetLogLineMemory(0)).Returns(() =>
        {
            entered.Set();
            _ = release.Wait(TimeSpan.FromSeconds(10));
            return new LogLine("hit", 0);
        });
        using var index = new MarkerIndex();
        var criteria = MarkerCriteria.ForHighlights([new HighlightEntry { SearchText = "hit", BackgroundColor = Color.Red }]);
        index.Reset(oldReader.Object, criteria);
        var oldScan = index.UpdateAsync(1);
        try
        {
            Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            index.Reset(ReaderOf("miss", "hit"), criteria);
            await index.UpdateAsync(2).ConfigureAwait(false);
        }
        finally
        {
            release.Set();
            await oldScan.ConfigureAwait(false);
        }

        Assert.That(index.Snapshot.Matches.Select(match => match.LineNumber), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public async Task Update_TruncationRebuildsAndLastLineEditsReplaceOldMatches ()
    {
        List<string> lines = ["hit", "hit", "hit"];
        var reader = new Mock<ILogfileReader>();
        _ = reader.Setup(source => source.GetLogLineMemory(It.IsAny<int>()))
            .Returns((int line) => new LogLine(lines[line], line));
        using var index = new MarkerIndex();
        index.Reset(reader.Object, MarkerCriteria.ForHighlights([new HighlightEntry { SearchText = "hit", BackgroundColor = Color.Red }]));
        await index.UpdateAsync(3).ConfigureAwait(false);
        lines[0] = "miss";
        await index.UpdateAsync(1).ConfigureAwait(false);
        Assert.That(index.Snapshot.Matches, Is.Empty);

        lines[0] = "hit";
        await index.UpdateAsync(1, lastLineChanged: true).ConfigureAwait(false);
        Assert.That(index.Snapshot.Matches.Select(match => match.LineNumber), Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public async Task Update_InvalidRegexReportsFailureAndCanRecoverAfterCriteriaChange ()
    {
        using var index = new MarkerIndex();
        var reader = ReaderOf("hit");
        index.Reset(reader, MarkerCriteria.ForHighlights(
            [new HighlightEntry { SearchText = "[", IsRegex = true, BackgroundColor = Color.Red }]));
        await index.UpdateAsync(1).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(index.Snapshot.Error, Is.InstanceOf<ArgumentException>());
            Assert.That(index.IsScanning, Is.False);
            Assert.That(index.Snapshot.Matches, Is.Empty);
        });

        index.Reset(reader, MarkerCriteria.ForHighlights([new HighlightEntry { SearchText = "hit", BackgroundColor = Color.Red }]));
        await index.UpdateAsync(1).ConfigureAwait(false);
        Assert.That(index.Snapshot.Matches.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Reset_DisabledSourceAndDisposedIndexNeverReadTheFile ()
    {
        var reader = new Mock<ILogfileReader>(MockBehavior.Strict);
        using var index = new MarkerIndex();
        index.Reset(reader.Object, null);
        await index.UpdateAsync(100).ConfigureAwait(false);
        index.Dispose();
        index.Reset(reader.Object, MarkerCriteria.ForHighlights([]));
        await index.UpdateAsync(100).ConfigureAwait(false);
        Assert.That(index.Snapshot.Matches, Is.Empty);
    }

    private static ILogfileReader ReaderOf (params string[] lines)
    {
        var reader = new Mock<ILogfileReader>();
        _ = reader.Setup(source => source.GetLogLineMemory(It.IsAny<int>()))
            .Returns((int line) => new LogLine(lines[line], line));
        return reader.Object;
    }
}