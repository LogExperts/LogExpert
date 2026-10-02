using System.Diagnostics;
using System.Runtime.Versioning;

using ColumnizerLib;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Marker;
using LogExpert.Core.Config;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogWindow;

using Moq;

using NUnit.Framework;

namespace LogExpert.UI.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
[SupportedOSPlatform("windows")]
public sealed class MarkerBarControllerTests : IDisposable
{
    private const int LINE_COUNT = 10;
    private const int HIGHLIGHT_LANE = 0;
    private const int BOOKMARK_LANE = 1;

    private MarkerBarController? _controller;

    [TearDown]
    public void TearDown ()
    {
        Dispose();
    }

    public void Dispose ()
    {
        _controller?.Dispose();
        _controller?.Bar.Dispose();
        _controller = null;
    }

    [Test]
    public void Update_FrameAggregatedBeforeADataChange_IsDiscarded ()
    {
        using var host = new FakeHost(ReaderOf(_ => "ERROR"));
        var controller = Create(host);
        UpdateUntil(controller, () => HasMarkers(controller.Bar, HIGHLIGHT_LANE));

        host.Bookmarks.Add(5);
        controller.MarkDataChanged();
        host.FilterGate.Reset();
        var update = controller.UpdateAsync();
        controller.MarkDataChanged();
        host.FilterGate.Set();
        Complete(update);

        Assert.That(HasMarkers(controller.Bar, BOOKMARK_LANE), Is.False, "The frame was aggregated before the last data change.");
        UpdateUntil(controller, () => HasMarkers(controller.Bar, BOOKMARK_LANE));
    }

    [Test]
    public void Update_WhileARolloverIsPending_RendersNothing ()
    {
        using var host = new FakeHost(ReaderOf(_ => "ERROR"));
        var controller = Create(host);
        UpdateUntil(controller, () => HasMarkers(controller.Bar, HIGHLIGHT_LANE));

        controller.BeginRollover();
        for (var tick = 0; tick < 5; tick++)
        {
            Update(controller);
        }

        Assert.That(HasMarkers(controller.Bar, HIGHLIGHT_LANE), Is.False);
        controller.EndRollover();
        UpdateUntil(controller, () => HasMarkers(controller.Bar, HIGHLIGHT_LANE));
    }

    [Test]
    public void Update_DiscoveryError_IsReportedOncePerSnapshot ()
    {
        using var host = new FakeHost(ReaderOf(_ => throw new IOException("unreadable")));
        var controller = Create(host);
        UpdateUntil(controller, () => host.Errors.Count > 0);

        for (var tick = 0; tick < 5; tick++)
        {
            Update(controller);
        }

        Assert.That(host.Errors, Has.Count.EqualTo(1));
        Assert.That(host.Errors[0], Does.Contain("unreadable"));
    }

    private MarkerBarController Create (FakeHost host)
    {
        var controller = _controller = new MarkerBarController(host);
        controller.Bar.Size = new Size(80, 120);
        controller.ApplyPreferences(new Preferences { ShowMarkerBar = true }, Color.White, Color.Black);
        return controller;
    }

    private static ILogfileReader ReaderOf (Func<int, string> line)
    {
        var reader = new Mock<ILogfileReader>();
        _ = reader.Setup(source => source.LineCount).Returns(LINE_COUNT);
        _ = reader.Setup(source => source.GetLogLineMemory(It.IsAny<int>())).Returns((int number) => new LogLine(line(number), number));
        return reader.Object;
    }

    // Lanes are a quarter of the width each; the discovery dots sit in the middle, clear of a lane's left edge.
    private static bool HasMarkers (MarkerBar bar, int lane)
    {
        using var bitmap = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(bitmap, bar.ClientRectangle);
        var x = lane * bar.Width / 4 + 3;
        var background = bar.BackColor.ToArgb();
        return Enumerable.Range(bar.TopInset, bar.BucketHeight).Any(y => bitmap.GetPixel(x, y).ToArgb() != background);
    }

    private static void Update (MarkerBarController controller)
    {
        Complete(controller.UpdateAsync());
    }

    private static void Complete (Task update)
    {
        var timer = Stopwatch.StartNew();
        while (!update.IsCompleted && timer.Elapsed < TimeSpan.FromSeconds(10))
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }

        Assert.That(update.IsCompleted, Is.True, "Timed out waiting for the marker update.");
        update.GetAwaiter().GetResult();
    }

    private static void UpdateUntil (MarkerBarController controller, Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (!condition() && timer.Elapsed < TimeSpan.FromSeconds(10))
        {
            Update(controller);
            Thread.Sleep(5);
        }

        Assert.That(condition(), Is.True, "Timed out waiting for the marker bar.");
    }

    private sealed class FakeHost (ILogfileReader reader) : IMarkerBarHost, IDisposable
    {
        public ManualResetEventSlim FilterGate { get; } = new(true);

        public List<int> Bookmarks { get; } = [];

        public List<string> Errors { get; } = [];

        public ILogfileReader? Reader => reader;

        public string FileName => "markers.log";

        public ILogLineMemoryColumnizer Columnizer { get; } = new Mock<ILogLineMemoryColumnizer>().Object;

        public string ConfigDir => string.Empty;

        public bool IsContentUnavailable => false;

        public bool IsClosed => false;

        public int ColumnHeaderHeight => 0;

        public int NavigableLineCount => LINE_COUNT;

        public MarkerCriteria GetHighlightCriteria ()
        {
            return MarkerCriteria.ForHighlights([new HighlightEntry { SearchText = "ERROR", BackgroundColor = Color.Red }]);
        }

        public int[] GetBookmarkLineNumbers ()
        {
            return [.. Bookmarks];
        }

        // Runs inside the off-UI-thread aggregation: holding the gate pins an update between snapshot and render.
        public int[] GetFilterHits ()
        {
            _ = FilterGate.Wait(TimeSpan.FromSeconds(10));
            return [];
        }

        public void StatusLineError (string text)
        {
            Errors.Add(text);
        }

        public void RequestGotoLine (int targetLine)
        {
        }

        public void Dispose ()
        {
            FilterGate.Dispose();
        }
    }
}
