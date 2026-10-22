namespace LogExpert.Core.Classes.Log.Buffers;

/// <summary>
/// Immutable point-in-time capture of <see cref="BufferIndex"/> state.
/// Taken under a single read lock, safe to inspect afterward without locks.
/// </summary>
public sealed class BufferIndexSnapshot
{
    public int BufferCount { get; init; }
    public int TotalLineCount { get; init; }
    public int LruCacheCount { get; init; }
    public IReadOnlyList<BufferInfo> Buffers { get; init; } = [];

    public sealed record BufferInfo (
        int StartLine,
        int LineCount,
        long StartPos,
        long Size,
        bool IsDisposed,
        string FileName);

    public override string ToString () =>
        $"Buffers={BufferCount}, Lines={TotalLineCount}, LRU={LruCacheCount}";
}