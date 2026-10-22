using System.Buffers;

namespace LogExpert.Core.Classes.Log.Buffers;

/// <remarks>
/// Blocks are rented from <see cref="ArrayPool{T}.Shared"/> and returned when the owning <see cref="LogBuffer"/> is
/// evicted. The pinning mechanism (Phase 1/2) ensures that buffers whose content is still displayed by the UI are
/// exempt from eviction, preventing use-after-return corruption.
/// This class is NOT thread-safe. Each reader/fill operation should use its own instance.
/// </remarks>
public sealed class CharBlockAllocator : IDisposable
{
    private const int DEFAULT_BLOCK_SIZE = 32_768; // 64 KB (32K chars × 2 bytes), stays under 85 KB LOH threshold

    private readonly int _blockSize;
    private List<char[]> _blocks = [];
    private readonly List<char[]> _oversizedBlocks = [];
    private char[] _currentBlock;
    private int _currentOffset;
    private bool _disposed;

    public CharBlockAllocator (int blockSize = DEFAULT_BLOCK_SIZE)
    {
        _blockSize = blockSize;
        _currentBlock = ArrayPool<char>.Shared.Rent(_blockSize);
        _blocks.Add(_currentBlock);
        _currentOffset = 0;
    }

    /// <summary>
    /// Gets the number of normal (fixed-size) blocks currently rented from the pool.
    /// </summary>
    public int BlockCount => _blocks.Count;

    /// <summary>
    /// Gets the number of oversized (standalone) blocks currently rented from the pool.
    /// Useful for diagnostics — a high count indicates pathological line lengths.
    /// </summary>
    public int OversizedBlockCount => _oversizedBlocks.Count;

    /// <summary>
    /// Allocates a <see cref="Memory{Char}"/> region of the specified length from the current block.
    /// If the current block has insufficient space, a new block is rented.
    /// Lines longer than the block size receive a standalone rental tracked separately.
    /// </summary>
    public Memory<char> Rent (int length)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (length <= 0)
        {
            return Memory<char>.Empty;
        }

        // Oversized line: give it its own array, tracked separately
        if (length > _blockSize)
        {
            var oversized = new char[length];
            _oversizedBlocks.Add(oversized);
            return oversized.AsMemory(0, length);
        }

        // Current block has space
        if (_currentOffset + length <= _currentBlock.Length)
        {
            var memory = _currentBlock.AsMemory(_currentOffset, length);
            _currentOffset += length;
            return memory;
        }

        // Need a new block
        _currentBlock = ArrayPool<char>.Shared.Rent(_blockSize);
        _blocks.Add(_currentBlock);
        _currentOffset = length;
        return _currentBlock.AsMemory(0, length);
    }

    /// <summary>
    /// Detaches and returns the list of all blocks (normal + oversized). After this call,
    /// the allocator no longer owns those blocks — the caller (LogBuffer) holds them
    /// until GC collects them after all <see cref="ReadOnlyMemory{Char}"/> slices are released.
    /// </summary>
    public List<char[]> DetachBlocks ()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Merge oversized blocks into the main list so the caller owns everything
        if (_oversizedBlocks.Count > 0)
        {
            _blocks.AddRange(_oversizedBlocks);
            _oversizedBlocks.Clear();
        }

        // Swap the list — O(1), no copy. Caller owns the old list.
        var blocks = _blocks;
        _currentBlock = ArrayPool<char>.Shared.Rent(_blockSize);
        _blocks = [_currentBlock];
        _currentOffset = 0;
        return blocks;
    }

    /// <summary>
    /// Returns all blocks to <see cref="ArrayPool{T}.Shared"/>.
    /// Safe to call only when no <see cref="ReadOnlyMemory{Char}"/> slices reference these blocks
    /// (i.e., after DetachBlocks transferred ownership to LogBuffer, and the reader is being disposed).
    /// </summary>
    public void ReturnAll ()
    {
        foreach (var block in _blocks)
        {
            ArrayPool<char>.Shared.Return(block);
        }

        _blocks.Clear();

        foreach (var block in _oversizedBlocks)
        {
            ArrayPool<char>.Shared.Return(block);
        }

        _oversizedBlocks.Clear();
        _currentBlock = null!;
        _currentOffset = 0;
    }

    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        ReturnAll();
        _disposed = true;
    }
}