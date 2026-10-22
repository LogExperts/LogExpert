using System.Collections.Concurrent;

using ColumnizerLib;

namespace LogExpert.Core.Classes.Log.Buffers;

public sealed class LogBufferPool (int maxSize)
{
    private readonly ConcurrentBag<LogBuffer> _pool = [];
    private readonly int _maxSize = maxSize;

    public LogBuffer Rent (ILogFileInfo fileInfo, int maxLines)
    {
        if (_pool.TryTake(out var buffer))
        {
            buffer.Reinitialise(fileInfo, maxLines);
            return buffer;
        }

        return new LogBuffer(fileInfo, maxLines);
    }

    /// <summary>
    /// Returns a <see cref="LogBuffer"/> to the pool for reuse.
    /// </summary>
    /// <remarks>
    /// Disposing the buffer's content is handled by this method, so callers should not dispose the buffer themselves.
    /// </remarks>
    /// <param name="buffer">The buffer to return.</param>
    public void Return (LogBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        buffer.DisposeContent();
        if (_pool.Count < _maxSize)
        {
            _pool.Add(buffer);
        }
    }
}
