namespace LogExpert.Core.Interfaces;

//TODO ILogStreamReader function should be moved to ILogStreamReaderMemory, and ILogStreamReader should be removed, as it is not used anywhere else in the codebase. This will simplify the interface hierarchy and reduce confusion about which methods are available on which interfaces.
public interface ILogStreamReaderMemory : ILogStreamReader
{

    /// <summary>
    /// Attempts to read the next line from the stream.
    /// </summary>
    /// <param name="lineMemory">
    /// When this method returns <c> true</c>, contains a <see cref="ReadOnlyMemory{Char}"/> representing the next line
    /// read from the stream. The memory is only valid until the next call to <see cref="TryReadLine"/> or until
    /// <see cref="ReturnMemory"/> is called.
    /// </param>
    /// <returns>
    /// <c> true</c> if a line was successfully read; <c> false</c> if the end of the stream has been reached or no more
    /// lines are available.
    /// </returns>
    /// <remarks>
    /// The returned memory is only valid until the next call to <see cref="TryReadLine"/> or until
    /// <see cref="ReturnMemory"/> is called. This method is not guaranteed to be thread-safe; concurrent access should
    /// be synchronized externally.
    /// </remarks>
    bool TryReadLine (out ReadOnlyMemory<char> lineMemory);

    /// <summary>
    /// Returns the memory buffer previously obtained from <see cref="TryReadLine"/> to the underlying pool or resource
    /// manager.
    /// </summary>
    /// <param name="memory">
    /// The <see cref="ReadOnlyMemory{Char}"/> instance previously obtained from <see cref="TryReadLine"/>.
    /// </param>
    /// <remarks>
    /// Call this method when you are done processing the memory returned by <see cref="TryReadLine"/> to avoid memory
    /// leaks or resource retention. Failing to call this method may result in increased memory usage. It is safe to
    /// call this method multiple times for the same memory, but only the first call will have an effect.
    /// </remarks>
    void ReturnMemory (ReadOnlyMemory<char> memory);

    /// <summary>
    /// Detaches the completed character blocks that back the <see cref="ReadOnlyMemory{Char}"/> slices handed out by
    /// <see cref="TryReadLine"/>, transferring their ownership to the caller (the <c>LogBuffer</c> that owns those
    /// lines).
    /// </summary>
    /// <returns>
    /// The list of blocks the reader has filled. The caller owns them until every slice into them is released, at
    /// which point they are returned to the shared pool. Readers that have nothing to hand over return an empty list.
    /// </returns>
    /// <remarks>
    /// Call this after a buffer's worth of lines has been read and before the reader continues filling the next
    /// buffer. The reader retains any partially-scanned block so reading can continue seamlessly.
    /// </remarks>
    List<char[]> DetachCharBlocks ();
}
