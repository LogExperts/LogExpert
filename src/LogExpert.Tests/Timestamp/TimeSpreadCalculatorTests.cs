using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ColumnizerLib;

using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Timestamp;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogWindow;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Timestamp;

/// <summary>
/// Exercises both spread algorithms through the worker's public configuration and completion
/// event, with a real TimestampLocator over an in-memory source. No WinForms objects are created.
/// Keeping the worker covers mode selection and wakeup as well as the calculation; completion
/// is awaited with a bounded timeout rather than guessed with sleeps or private-method calls.
/// </summary>
[TestFixture]
public class TimeSpreadCalculatorTests
{
    private static readonly DateTime StartTime = new(2026, 1, 1, 10, 0, 0);

    [TestCase(false)]
    [TestCase(true)]
    public async Task Calculate_EmptyFile_CompletesWithNoEntries (bool timeMode)
    {
        var entries = await Calculate(timeMode, 100).ConfigureAwait(false);

        Assert.That(entries, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Calculate_SingleTimestamp_CompletesWithNoEntries (bool timeMode)
    {
        var entries = await Calculate(timeMode, 100, 0).ConfigureAwait(false);

        Assert.That(entries, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Calculate_NoTimestamps_CompletesWithNoEntries (bool timeMode)
    {
        var entries = await Calculate(timeMode, 100, null, null, null).ConfigureAwait(false);

        Assert.That(entries, Is.Empty);
    }

    [Test]
    public async Task Calculate_LineModeUniformSpacing_SamplesInteriorLines ()
    {
        var entries = await Calculate(false, 100, 0, 30000, 60000, 90000, 120000, 150000).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(entries.Select(e => e.LineNum), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(entries.Select(e => e.Timestamp), Is.EqualTo(new[]
            {
                StartTime.AddSeconds(30), StartTime.AddSeconds(60),
                StartTime.AddSeconds(90), StartTime.AddSeconds(120)
            }));
            Assert.That(entries.Select(e => e.Diff), Is.All.Zero);
            Assert.That(entries.Skip(1).Select(e => e.Value).Distinct().Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Calculate_LineModeIrregularGaps_LargerGapIsDarker ()
    {
        var entries = await Calculate(false, 100, 0, 30000, 60000, 150000, 180000, 210000).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(entries.Select(e => e.LineNum), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(entries.Select(e => e.Timestamp), Is.EqualTo(new[]
            {
                StartTime.AddSeconds(30), StartTime.AddSeconds(60),
                StartTime.AddSeconds(150), StartTime.AddSeconds(180)
            }));
            Assert.That(entries[1].Value, Is.EqualTo(255));
            Assert.That(entries[2].Value, Is.LessThan(entries[1].Value));
            Assert.That(entries[3].Value, Is.EqualTo(255));
        });
    }

    [Test]
    public async Task Calculate_LineModeSmallDisplay_SamplesEverySecondLine ()
    {
        var entries = await Calculate(false, 3, 0, 30000, 60000, 90000, 120000, 150000).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(entries.Select(e => e.LineNum), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(entries.Select(e => e.Timestamp), Is.EqualTo(new[]
            {
                StartTime.AddSeconds(30), StartTime.AddSeconds(90)
            }));
        });
    }

    [TestCase(1000, 6)]
    [TestCase(1, 100)]
    public async Task Calculate_TimeModeUniformSpacing_ProducesEqualDensity (int spacing, int displayHeight)
    {
        var entries = await Calculate(true, displayHeight, 0, spacing, 2 * spacing,
            3 * spacing, 4 * spacing, 5 * spacing, 6 * spacing).ConfigureAwait(false);

        // The initial two samples are omitted by the spread display. One line remains per bin.
        Assert.Multiple(() =>
        {
            Assert.That(entries.Select(e => e.LineNum), Is.EqualTo(new[] { 2, 3, 4, 5, 6 }));
            Assert.That(entries.Select(e => (e.Timestamp - StartTime).TotalMilliseconds),
                Is.EqualTo(new[] { 2 * spacing, 3 * spacing, 4 * spacing, 5 * spacing, 6 * spacing }));
            Assert.That(entries.Select(e => e.Diff), Is.All.EqualTo(1));
            Assert.That(entries.Select(e => e.Value), Is.All.EqualTo(198));
        });
    }

    [Test]
    public async Task Calculate_TimeModeIrregularGaps_EmptyTimeBinsAreLighter ()
    {
        var entries = await Calculate(true, 8, 0, 1000, 2000, 5000, 6000, 7000, 8000).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(entries.Select(e => e.LineNum), Is.EqualTo(new[] { 2, 2, 2, 3, 4, 5, 6 }));
            Assert.That(entries.Select(e => (e.Timestamp - StartTime).TotalSeconds),
                Is.EqualTo(new[] { 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(entries.Select(e => e.Diff), Is.EqualTo(new[] { 1, 0, 0, 1, 1, 1, 1 }));
            Assert.That(entries.Select(e => e.Value), Is.EqualTo(new[] { 122, 255, 255, 122, 122, 122, 122 }));
        });
    }

    private static async Task<List<SpreadEntry>> Calculate (bool timeMode, int displayHeight, params int?[] milliseconds)
    {
        var source = SourceOver(milliseconds);
        var calculator = new TimeSpreadCalculator(new TimestampLocator(source), source)
        {
            TimeMode = timeMode
        };
        var completion = new TaskCompletionSource<List<SpreadEntry>>(TaskCreationOptions.RunContinuationsAsynchronously);
        calculator.CalcDone += (_, _) => completion.TrySetResult(calculator.DiffList);

        try
        {
            calculator.SetLineCount(milliseconds.Length);
            calculator.SetDisplayHeight(displayHeight);
            calculator.Enabled = true;
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
        finally
        {
            calculator.Stop();
        }
    }

    private static ITimestampSource SourceOver (int?[] milliseconds)
    {
        var reader = new Mock<ILogfileReader>();
        _ = reader.Setup(r => r.LineCount).Returns(milliseconds.Length);
        _ = reader.Setup(r => r.GetLogLineMemory(It.IsAny<int>()))
            .Returns((int lineNumber) =>
            {
                if (lineNumber < 0 || lineNumber >= milliseconds.Length)
                {
                    return null!;
                }

                var line = new Mock<ILogLineMemory>();
                var text = milliseconds[lineNumber]?.ToString(CultureInfo.InvariantCulture) ?? "";
                _ = line.Setup(l => l.FullLine).Returns(text.AsMemory());
                return line.Object;
            });

        var columnizer = new Mock<ILogLineMemoryColumnizer>();
        _ = columnizer.Setup(c => c.IsTimeshiftImplemented()).Returns(true);
        _ = columnizer.Setup(c => c.GetTimestamp(It.IsAny<ILogLineMemoryColumnizerCallback>(), It.IsAny<ILogLineMemory>()))
            .Returns((ILogLineMemoryColumnizerCallback _, ILogLineMemory line) =>
                line.FullLine.IsEmpty ? DateTime.MinValue : StartTime.AddMilliseconds(int.Parse(line.FullLine.Span, CultureInfo.InvariantCulture)));

        var source = new Mock<ITimestampSource>();
        _ = source.Setup(s => s.Reader).Returns(reader.Object);
        _ = source.Setup(s => s.Columnizer).Returns(columnizer.Object);
        _ = source.Setup(s => s.Callback).Returns(Mock.Of<IPositionedColumnizerCallback>());
        _ = source.Setup(s => s.ColumnizerLock).Returns(new Lock());
        return source.Object;
    }
}