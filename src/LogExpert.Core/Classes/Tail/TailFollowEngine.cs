using System.ComponentModel;
using System.Runtime.InteropServices;

using LogExpert.Core.Entities;

using NLog;

namespace LogExpert.Core.Classes.Tail;

/// <summary>
/// The tail-follow engine: owns the queue and single worker thread that sequence file-size-changed
/// events from the Logfile Reader into ordered <see cref="ITailFollowSink"/> callbacks.
/// The engine does not own the reader subscription — the Log Window swaps its reader on
/// reload/rollover and forwards events via <see cref="Post"/>.
/// </summary>
public sealed class TailFollowEngine : IDisposable
{
    private const int WORKER_SHUTDOWN_TIMEOUT = 2000; // ms to wait for the worker to drain during teardown before giving up

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly ITailFollowSink _sink;
    private readonly List<LogEventArgs> _queue = [];
    private readonly EventWaitHandle _wakeEvent = new AutoResetEvent(false);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _workerTask;

    /// <summary>
    /// Creates the engine and immediately starts its dedicated worker thread; events posted
    /// before the first tail activity simply park until <see cref="Post"/> wakes the worker.
    /// </summary>
    public TailFollowEngine (ITailFollowSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        _sink = sink;
        _workerTask = Task.Factory.StartNew(Worker, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    /// <summary>
    /// Enqueues a tail event for sequenced delivery. Thread-safe; callable from any thread.
    /// </summary>
    public void Post (LogEventArgs e)
    {
        lock (_queue)
        {
            _queue.Add(e);
            _ = _wakeEvent.Set();
        }
    }

    /// <summary>
    /// Stops the worker: wakes it if parked, cancels, and waits a bounded time so closing a
    /// window can never hang on a worker blocked in dispatch. Idempotent.
    /// </summary>
    public void Stop ()
    {
        _ = _wakeEvent.Set(); // wake the worker if it is parked on the event so it observes the cancel and exits
        _cts.Cancel();

        try
        {
            _ = _workerTask.Wait(WORKER_SHUTDOWN_TIMEOUT);
        }
        catch (AggregateException)
        {
            // The worker threw on its way out; nothing actionable during teardown.
        }
    }

    /// <summary>
    /// <see cref="Stop"/> plus disposal of the wake event and CTS. Test consumers use this;
    /// the Log Window deliberately calls only <see cref="Stop"/> at close (leak-parity with
    /// the original worker, whose wait handle was never disposed — disposing while a stuck
    /// worker is still parked would fault it on the wake handle).
    /// </summary>
    public void Dispose ()
    {
        Stop();
        _wakeEvent.Dispose();
        _cts.Dispose();
    }

    private void Worker ()
    {
        Thread.CurrentThread.Name = "TailFollowEngine";

        while (!_cts.Token.IsCancellationRequested)
        {
            _ = _wakeEvent.WaitOne();

            while (!_cts.Token.IsCancellationRequested)
            {
                LogEventArgs e;
                lock (_queue)
                {
                    if (_queue.Count == 0)
                    {
                        break;
                    }

                    e = _queue[0];
                    _queue.RemoveAt(0);
                }

                if (e.IsRollover)
                {
                    _sink.OnRolloverShift(e.RolloverOffset);
                }

                if (_sink.IsAbandoned)
                {
                    return;
                }

                try
                {
                    _sink.OnTailLines(e);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex) when (ex is InvalidOperationException or
                                                 ArgumentOutOfRangeException or
                                                 ExternalException or
                                                 Win32Exception)
                {
                    // Never let a single bad event kill the worker thread. Before this guard, an
                    // exception here (e.g. a missing optional assembly loaded lazily from the tail
                    // path) terminated the loop, so follow-tail silently stopped updating for the
                    // whole window until reload (#634). Log and continue with the next event.
                    _logger.Error(ex, "### TailFollowEngine: error while processing a file-size-changed event; follow-tail continues.");
                }

                _sink.OnLineCountChanged(e.LineCount);
            }
        }
    }
}
