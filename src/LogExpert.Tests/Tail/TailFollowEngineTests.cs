using LogExpert.Core.Classes.Tail;
using LogExpert.Core.Entities;

using NUnit.Framework;

namespace LogExpert.Tests.Tail;

[TestFixture]
public class TailFollowEngineTests
{
    [Test]
    public void Post_DeliversTailLinesThenLineCountToSink ()
    {
        var sink = new RecordingSink();
        using var engine = new TailFollowEngine(sink);

        engine.Post(new LogEventArgs { LineCount = 42 });

        Assert.That(sink.WaitUntil(calls => calls.Count >= 2), Is.True, "sink was never called");
        Assert.That(sink.Calls, Is.EqualTo(["lines:42", "count:42"]));
    }

    [Test]
    public void RolloverEvent_RaisesRolloverShiftBeforeTailLines ()
    {
        var sink = new RecordingSink();
        using var engine = new TailFollowEngine(sink);

        engine.Post(new LogEventArgs { IsRollover = true, RolloverOffset = 17, LineCount = 5 });

        Assert.That(sink.WaitUntil(calls => calls.Count >= 3), Is.True, "sink was never called");
        Assert.That(sink.Calls, Is.EqualTo(["shift:17", "lines:5", "count:5"]));
    }

    [Test]
    public void NonRolloverEvent_RaisesNoRolloverShift ()
    {
        var sink = new RecordingSink();
        using var engine = new TailFollowEngine(sink);

        engine.Post(new LogEventArgs { IsRollover = false, LineCount = 5 });

        Assert.That(sink.WaitUntil(calls => calls.Count >= 2), Is.True, "sink was never called");
        Assert.That(sink.Calls, Does.Not.Contain("shift:0"));
    }

    [Test]
    public void SinkFailure_NeverKillsTheWorker ()
    {
        // Regression pin for #634: a failing dispatch (e.g. a missing optional assembly loaded
        // lazily from the tail path) must not silently stop follow-tail for the whole window.
        var sink = new RecordingSink
        {
            TailLinesFailure = e => e.LineCount == 1 ? new InvalidOperationException("boom") : null,
        };
        using var engine = new TailFollowEngine(sink);

        engine.Post(new LogEventArgs { LineCount = 1 });
        engine.Post(new LogEventArgs { LineCount = 2 });

        Assert.That(sink.WaitUntil(calls => calls.Count >= 4), Is.True, "worker died after the sink failure");
        // The faulted event still reports its line count (the time spread bar always learns it),
        // and the next event is processed normally.
        Assert.That(sink.Calls, Is.EqualTo(["lines:1", "count:1", "lines:2", "count:2"]));
    }

    [Test]
    public void AbandonedSink_ExitsWithoutDispatching_ButStillShiftsRollover ()
    {
        // Mirrors the original worker: the rollover shift runs before the abandoned check,
        // so line-anchored state is shifted even while the window is on its way out.
        var sink = new RecordingSink { Abandoned = true };
        using var engine = new TailFollowEngine(sink);

        engine.Post(new LogEventArgs { IsRollover = true, RolloverOffset = 3, LineCount = 9 });

        Assert.That(sink.WaitUntil(calls => calls.Contains("shift:3")), Is.True, "rollover shift was skipped");
        Assert.That(sink.WaitUntil(calls => calls.Count > 1, timeoutMs: 300), Is.False, "abandoned sink still got dispatch callbacks");
    }

    [Test]
    public void SinkDisposedDuringDispatch_ExitsWorkerPermanently ()
    {
        var sink = new RecordingSink
        {
            TailLinesFailure = _ => new ObjectDisposedException("window"),
        };
        using var engine = new TailFollowEngine(sink);

        engine.Post(new LogEventArgs { LineCount = 1 });
        engine.Post(new LogEventArgs { LineCount = 2 });

        Assert.That(sink.WaitUntil(calls => calls.Contains("lines:1")), Is.True, "first event never dispatched");
        Assert.That(sink.WaitUntil(calls => calls.Count > 1, timeoutMs: 300), Is.False, "worker survived ObjectDisposedException");
    }

    [Test]
    public void BurstOfEvents_DeliveredExactlyOnceInPostOrder ()
    {
        var sink = new RecordingSink();
        using var engine = new TailFollowEngine(sink);

        for (var i = 1; i <= 50; i++)
        {
            engine.Post(new LogEventArgs { LineCount = i });
        }

        Assert.That(sink.WaitUntil(calls => calls.Count >= 100), Is.True, "not all events were delivered");
        var expected = Enumerable.Range(1, 50).SelectMany(i => new[] { $"lines:{i}", $"count:{i}" });
        Assert.That(sink.Calls, Is.EqualTo(expected));
    }

    [Test]
    public void Stop_HaltsDeliveryAndIsIdempotent ()
    {
        var sink = new RecordingSink();
        using var engine = new TailFollowEngine(sink);

        engine.Post(new LogEventArgs { LineCount = 1 });
        Assert.That(sink.WaitUntil(calls => calls.Count >= 2), Is.True, "event before Stop was not delivered");

        engine.Stop();
        engine.Stop();

        engine.Post(new LogEventArgs { LineCount = 2 });
        Assert.That(sink.WaitUntil(calls => calls.Count > 2, timeoutMs: 300), Is.False, "event was delivered after Stop");
    }

    /// <summary>
    /// Records every sink callback as a string so tests can assert on exact call order.
    /// </summary>
    private sealed class RecordingSink : ITailFollowSink
    {
        private readonly List<string> _calls = [];

        public bool Abandoned { get; set; }

        public Func<LogEventArgs, Exception> TailLinesFailure { get; set; }

        public IReadOnlyList<string> Calls
        {
            get
            {
                lock (_calls)
                {
                    return [.. _calls];
                }
            }
        }

        public bool IsAbandoned => Abandoned;

        public void OnRolloverShift (int rolloverOffset)
        {
            Record($"shift:{rolloverOffset}");
        }

        public void OnTailLines (LogEventArgs e)
        {
            Record($"lines:{e.LineCount}");
            var failure = TailLinesFailure?.Invoke(e);
            if (failure != null)
            {
                throw failure;
            }
        }

        public void OnLineCountChanged (int lineCount)
        {
            Record($"count:{lineCount}");
        }

        public bool WaitUntil (Func<IReadOnlyList<string>, bool> condition, int timeoutMs = 5000)
        {
            var deadline = Environment.TickCount64 + timeoutMs;
            while (Environment.TickCount64 < deadline)
            {
                if (condition(Calls))
                {
                    return true;
                }

                Thread.Sleep(10);
            }

            return condition(Calls);
        }

        private void Record (string call)
        {
            lock (_calls)
            {
                _calls.Add(call);
            }
        }
    }
}
