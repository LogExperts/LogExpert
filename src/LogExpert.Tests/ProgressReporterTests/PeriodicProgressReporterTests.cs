using LogExpert.Core.Classes.Log.ProgressReporters;
using LogExpert.Core.EventArguments;

using NUnit.Framework;

namespace LogExpert.Tests.ProgressReporterTests;

/// <summary>
/// Unit tests for <see cref="PeriodicProgressReporter"/>.
/// Uses a short dispatch interval (50ms) to keep tests fast.
/// </summary>
[TestFixture]
internal sealed class PeriodicProgressReporterTests
{
    private const int DISPATCH_MS = 50;
    private const int WAIT_MS = 300; // enough for several dispatch cycles

    [Test]
    public void ReportProgress_FiresLoadFileEvent ()
    {
        using var reporter = new PeriodicProgressReporter(TimeSpan.FromMilliseconds(DISPATCH_MS));

        LoadFileEventArgs? received = null;
        reporter.LoadFile += (_, e) => received = e;

        reporter.ReportProgress("test.log", 500, 1000);

        // Wait for dispatch cycle
        Thread.Sleep(WAIT_MS);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.FileName, Is.EqualTo("test.log"));
        Assert.That(received.ReadPos, Is.EqualTo(500));
        Assert.That(received.FileSize, Is.EqualTo(1000));
        Assert.That(received.Finished, Is.False);
        Assert.That(received.NewFile, Is.False);
    }

    [Test]
    public void ReportComplete_FiresLoadFileEventWithFinished ()
    {
        using var reporter = new PeriodicProgressReporter(TimeSpan.FromMilliseconds(DISPATCH_MS));

        LoadFileEventArgs? received = null;
        reporter.LoadFile += (_, e) =>
        {
            if (e.Finished)
            {
                received = e;
            }
        };

        reporter.ReportComplete("test.log", 1000, 1000);
        Thread.Sleep(WAIT_MS);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Finished, Is.True);
    }

    [Test]
    public void ReportNewFile_FiresLoadFileEventWithNewFile ()
    {
        using var reporter = new PeriodicProgressReporter(TimeSpan.FromMilliseconds(DISPATCH_MS));

        LoadFileEventArgs? received = null;
        reporter.LoadFile += (_, e) =>
        {
            if (e.NewFile)
            {
                received = e;
            }
        };

        reporter.ReportNewFile("new.log", 0, 5000);
        Thread.Sleep(WAIT_MS);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.NewFile, Is.True);
        Assert.That(received.FileName, Is.EqualTo("new.log"));
    }

    [Test]
    public void ReportLoadingStarted_FiresLoadingStartedEvent ()
    {
        using var reporter = new PeriodicProgressReporter(TimeSpan.FromMilliseconds(DISPATCH_MS));

        LoadFileEventArgs? received = null;
        reporter.LoadingStarted += (_, e) => received = e;

        reporter.ReportLoadingStarted("test.log");
        Thread.Sleep(WAIT_MS);

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.FileName, Is.EqualTo("test.log"));
    }

    [Test]
    public void ReportLoadingFinished_FiresLoadingFinishedEvent ()
    {
        using var reporter = new PeriodicProgressReporter(TimeSpan.FromMilliseconds(DISPATCH_MS));

        var fired = false;
        reporter.LoadingFinished += (_, _) => fired = true;

        reporter.ReportLoadingFinished();
        Thread.Sleep(WAIT_MS);

        Assert.That(fired, Is.True);
    }

    [Test]
    public void MultipleProgressReports_CoalescedToLatest ()
    {
        using var reporter = new PeriodicProgressReporter(TimeSpan.FromMilliseconds(DISPATCH_MS));

        LoadFileEventArgs? received = null;
        reporter.LoadFile += (_, e) =>
        {
            if (!e.Finished)
            {
                received = e;
            }
        };

        // Fire many progress reports rapidly — only latest should be dispatched
        for (var i = 0; i < 100; i++)
        {
            reporter.ReportProgress("test.log", i * 100, 10000);
        }

        Thread.Sleep(WAIT_MS);

        Assert.That(received, Is.Not.Null);
        // The dispatched position should be the latest (or near-latest) value
        Assert.That(received!.ReadPos, Is.GreaterThanOrEqualTo(9000));
    }

    [Test]
    public void DispatchOrder_StartedBeforeProgress ()
    {
        using var reporter = new PeriodicProgressReporter(TimeSpan.FromMilliseconds(DISPATCH_MS));

        var order = new List<string>();
        reporter.LoadingStarted += (_, _) => order.Add("started");
        reporter.LoadFile += (_, _) => order.Add("progress");

        reporter.ReportLoadingStarted("test.log");
        reporter.ReportProgress("test.log", 500, 1000);
        Thread.Sleep(WAIT_MS);

        Assert.That(order, Has.Count.GreaterThanOrEqualTo(2));
        var startedIdx = order.IndexOf("started");
        var progressIdx = order.IndexOf("progress");
        Assert.That(startedIdx, Is.GreaterThanOrEqualTo(0), "LoadingStarted not fired");
        Assert.That(progressIdx, Is.GreaterThanOrEqualTo(0), "LoadFile not fired");
        Assert.That(startedIdx, Is.LessThan(progressIdx), "Started should fire before progress");
    }

    [Test]
    public void Dispose_StopsDispatchLoop ()
    {
        var reporter = new PeriodicProgressReporter(TimeSpan.FromMilliseconds(DISPATCH_MS));
        reporter.Dispose();

        // After dispose, reports should not throw but events won't fire
        Assert.DoesNotThrow(() => reporter.ReportProgress("test.log", 100, 1000));
    }

    [Test]
    public void NoSubscribers_DoesNotThrow ()
    {
        using var reporter = new PeriodicProgressReporter(TimeSpan.FromMilliseconds(DISPATCH_MS));

        // Report all event types with no subscribers — should not throw
        Assert.DoesNotThrow(() =>
        {
            reporter.ReportLoadingStarted("test.log");
            reporter.ReportProgress("test.log", 100, 1000);
            reporter.ReportNewFile("new.log", 0, 5000);
            reporter.ReportComplete("test.log", 1000, 1000);
            reporter.ReportLoadingFinished();
        });

        Thread.Sleep(WAIT_MS);
    }
}