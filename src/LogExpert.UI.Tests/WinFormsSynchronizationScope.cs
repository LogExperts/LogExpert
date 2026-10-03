namespace LogExpert.UI.Tests;

/// <summary>
/// Installs the WinForms synchronization context the app runs with. NUnit gives STA tests its own context, which
/// WinForms does not replace, so services that capture it would run UI callbacks on other threads.
/// </summary>
internal sealed class WinFormsSynchronizationScope : IDisposable
{
    private readonly SynchronizationContext? _previous = SynchronizationContext.Current;
    private readonly WindowsFormsSynchronizationContext _context = new();

    public WinFormsSynchronizationScope ()
    {
        SynchronizationContext.SetSynchronizationContext(_context);
    }

    // Not disposed: disposing a WinForms context destroys the thread's shared marshaling control, and await
    // continuations still queued by the test would then crash the test host.
    public void Dispose ()
    {
        SynchronizationContext.SetSynchronizationContext(_previous);
    }
}
