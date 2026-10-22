using LogExpert.Core.EventArguments;
using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Log.ProgressReporters;

/// <summary>
/// Periodically dispatches coalesced progress events on a background thread.
/// The I/O thread never blocks on subscribers — it writes volatile state and returns.
/// </summary>
internal sealed class PeriodicProgressReporter : ILoadProgressReporter
{
    private readonly Task _dispatchTask;
    private readonly CancellationTokenSource _cts = new();
    private readonly TimeSpan _dispatchInterval;

    public event EventHandler<LoadFileEventArgs>? LoadFile;
    public event EventHandler<LoadFileEventArgs>? LoadingStarted;
    public event EventHandler<EventArgs>? LoadingFinished;

    // Volatile state: written by I/O thread, read by dispatch loop
    private volatile ProgressState _latestProgress = ProgressState.Empty;
    private volatile ProgressState _latestComplete = ProgressState.Empty;

    private volatile bool _hasProgress;
    private volatile bool _hasComplete;
    private volatile bool _hasNewFile;
    private volatile bool _hasStarted;
    private volatile bool _hasFinished;

    public PeriodicProgressReporter (TimeSpan? dispatchInterval = null)
    {
        _dispatchInterval = dispatchInterval ?? TimeSpan.FromMilliseconds(200);
        _dispatchTask = Task.Run(DispatchLoop);
    }

    // I/O thread calls (non-blocking)
    public void ReportProgress (string fileName, long position, long fileLength)
    {
        _latestProgress = new ProgressState(fileName, position, fileLength);
        _hasProgress = true;
    }

    public void ReportComplete (string fileName, long position, long fileLength)
    {
        _latestComplete = new ProgressState(fileName, position, fileLength);
        _hasComplete = true;
    }

    public void ReportNewFile (string fileName, long position, long fileLength)
    {
        _latestComplete = new ProgressState(fileName, position, fileLength);
        _hasNewFile = true;
    }

    public void ReportLoadingStarted (string fileName)
    {
        _latestProgress = new ProgressState(fileName, 0, 0);
        _hasStarted = true;
    }

    public void ReportLoadingFinished ()
    {
        _hasFinished = true;
    }

    // Dispatch loop (fires in lifecycle order: Started → Progress → NewFile → Complete → Finished)
    private async Task DispatchLoop ()
    {
        var token = _cts.Token;

        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_dispatchInterval, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (_hasStarted)
            {
                _hasStarted = false;
                var s = _latestProgress;
                LoadingStarted?.Invoke(this, new LoadFileEventArgs(s.FileName, 0, false, 0, false));
            }

            if (_hasProgress)
            {
                _hasProgress = false;
                var s = _latestProgress;
                LoadFile?.Invoke(this, new LoadFileEventArgs(s.FileName, s.Position, false, s.FileLength, false));
            }

            if (_hasNewFile)
            {
                _hasNewFile = false;
                var s = _latestComplete;
                LoadFile?.Invoke(this, new LoadFileEventArgs(s.FileName, s.Position, false, s.FileLength, true));
            }

            if (_hasComplete)
            {
                _hasComplete = false;
                var s = _latestComplete;
                LoadFile?.Invoke(this, new LoadFileEventArgs(s.FileName, s.Position, true, s.FileLength, false));
            }

            if (_hasFinished)
            {
                _hasFinished = false;
                LoadingFinished?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private sealed record ProgressState (string FileName, long Position, long FileLength)
    {
        public static readonly ProgressState Empty = new(string.Empty, 0, 0);
    }

    public void Dispose ()
    {
        _cts.Cancel();
        _ = _dispatchTask.Wait(TimeSpan.FromSeconds(2));
        _cts.Dispose();
    }
}