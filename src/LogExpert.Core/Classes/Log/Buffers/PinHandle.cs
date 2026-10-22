namespace LogExpert.Core.Classes.Log.Buffers;

/// <summary>
/// Tracks a set of pinned <see cref="LogBuffer"/> instances. Disposing the handle
/// unpins all held buffers, making them eligible for LRU eviction.
/// </summary>
public sealed class PinHandle : IDisposable
{
    private List<LogBuffer>? _pinnedBuffers;

    internal PinHandle (List<LogBuffer> pinnedBuffers)
    {
        _pinnedBuffers = pinnedBuffers;
    }

    /// <summary>
    /// Gets the number of buffers currently pinned by this handle.
    /// </summary>
    public int Count => _pinnedBuffers?.Count ?? 0;

    public void Dispose ()
    {
        var buffers = Interlocked.Exchange(ref _pinnedBuffers, null);
        if (buffers is null)
        {
            return;
        }

        foreach (var buffer in buffers)
        {
            buffer.Unpin();
        }
    }
}