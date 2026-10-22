using System.Buffers;
using System.Text;

using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Log.Streamreaders;

/// <summary>
/// Reads decoded characters directly into pooled char[] blocks via StreamReader.Read(),
/// scans for newline boundaries, and returns ReadOnlyMemory&lt;char&gt; slices without
/// allocating per-line strings. Eliminates the StreamReader.ReadLine() string allocation.
/// </summary>
public class PositionAwareStreamReaderDirect : PositionAwareStreamReaderBase, ILogStreamReaderMemory
{
    #region Constants

    private const int BLOCK_SIZE = 32_768; // 64 KB (32K chars × 2 bytes), under LOH threshold
    private const char CHAR_LF = '\n';
    private const char CHAR_CR = '\r';

    private static readonly SearchValues<char> _lineTerminators = SearchValues.Create("\r\n");

    #endregion

    #region Fields

    private char[] _readBlock;
    private int _readBlockLength;  // valid chars in _readBlock
    private int _scanOffset;       // current scan position in _readBlock
    private bool _eof;
    private bool _initialized;     // first block filled from the current stream position
    private int _terminatorCharByteSize; // bytes for a single '\r' or '\n' in the active encoding
    private readonly List<char[]> _completedBlocks = [];

    public override bool IsDisposed { get; protected set; }

    #endregion

    #region cTor

    public PositionAwareStreamReaderDirect (Stream stream, EncodingOptions encodingOptions, int maximumLineLength)
        : base(stream, encodingOptions, maximumLineLength)
    {
        _readBlock = ArrayPool<char>.Shared.Rent(BLOCK_SIZE);
        _readBlockLength = 0;
        _scanOffset = 0;
        _eof = false;
    }

    #endregion

    #region Public methods

    public override string ReadLine ()
    {
        return TryReadLine(out var memory) ? memory.ToString() : null;
    }

    /// <summary>
    /// Reads the next line by scanning the current block for the next line terminator
    /// (<c>\n</c>, <c>\r\n</c>, or a bare <c>\r</c>). If the block is exhausted, tail-copies
    /// the partial line to a new block and refills. Returns a zero-copy
    /// ReadOnlyMemory&lt;char&gt; slice into the pooled block. The byte position advances by the
    /// content bytes plus the bytes of the <em>actual</em> terminator, so it stays exact on
    /// files with mixed line endings.
    /// </summary>
    public bool TryReadLine (out ReadOnlyMemory<char> lineMemory)
    {
        var reader = GetStreamReader();

        EnsureInitialized(reader);

        while (true)
        {
            // If we have data to scan, look for the next \r or \n
            if (_scanOffset < _readBlockLength)
            {
                var searchSpan = _readBlock.AsSpan(_scanOffset, _readBlockLength - _scanOffset);
                var hitIndex = searchSpan.IndexOfAny(_lineTerminators);

                if (hitIndex >= 0)
                {
                    // Number of char cells the terminator occupies (1 for \n or bare \r, 2 for \r\n).
                    int terminatorChars;

                    if (searchSpan[hitIndex] == CHAR_LF)
                    {
                        terminatorChars = 1;
                    }
                    else if (hitIndex + 1 < searchSpan.Length)
                    {
                        // \r with a known following char: \r\n if it's \n, otherwise a bare \r.
                        terminatorChars = searchSpan[hitIndex + 1] == CHAR_LF ? 2 : 1;
                    }
                    else if (_eof)
                    {
                        // \r is the very last char in the file: a bare \r.
                        terminatorChars = 1;
                    }
                    else
                    {
                        // \r is the last char of the block but more data follows. Refill so the
                        // next char becomes available (the tail-copy carries the \r forward),
                        // then re-scan to classify it as \r\n or a bare \r.
                        RefillBlock(reader);
                        continue;
                    }

                    var lineLength = hitIndex;

                    // Enforce MaximumLineLength on the returned slice, but count the full
                    // content for the byte position.
                    var cappedLength = Math.Min(lineLength, MaximumLineLength);
                    lineMemory = _readBlock.AsMemory(_scanOffset, cappedLength);

                    var contentSpan = _readBlock.AsSpan(_scanOffset, lineLength);
                    MovePosition(Encoding.GetByteCount(contentSpan) + (terminatorChars * _terminatorCharByteSize));

                    // Advance scan past the content and its terminator.
                    _scanOffset += hitIndex + terminatorChars;

                    return true;
                }
            }

            // No terminator found (or no data at all). Need to refill.
            if (_eof)
            {
                // Emit remaining content as final line (no trailing newline)
                if (_scanOffset < _readBlockLength)
                {
                    var remaining = _readBlockLength - _scanOffset;
                    var cappedLength = Math.Min(remaining, MaximumLineLength);
                    lineMemory = _readBlock.AsMemory(_scanOffset, cappedLength);

                    var fullSpan = _readBlock.AsSpan(_scanOffset, remaining);
                    MovePosition(Encoding.GetByteCount(fullSpan));

                    _scanOffset = _readBlockLength;
                    return true;
                }

                lineMemory = default;
                return false;
            }

            // Tail-copy: move unconsumed chars to a new block and refill
            RefillBlock(reader);
        }
    }

    public void ReturnMemory (ReadOnlyMemory<char> memory)
    {
        // Bulk return via DetachBlocks()/Dispose(). Individual return not needed.
    }

    /// <summary>
    /// Detaches completed blocks (fully scanned) for transfer to the LogBuffer.
    /// The current _readBlock (partially scanned) stays with the reader.
    /// </summary>
    public List<char[]> DetachCharBlocks ()
    {
        // Nothing to detach: no completed blocks and no lines were scanned from the current block.
        if (_completedBlocks.Count == 0 && _scanOffset == 0)
        {
            return [];
        }

        // The current _readBlock contains memory backing lines already added to the LogBuffer.
        // It must be transferred to the buffer along with any completed blocks.
        _completedBlocks.Add(_readBlock);

        // Rent a fresh block and carry over any unscanned data (partial line in progress)
        var tailLength = _readBlockLength - _scanOffset;

        // The tail may exceed BLOCK_SIZE after reading a long line (buffer was grown).
        var newBlockSize = BLOCK_SIZE;
        while (tailLength > newBlockSize)
        {
            newBlockSize *= 2;
        }

        var newBlock = ArrayPool<char>.Shared.Rent(newBlockSize);

        if (tailLength > 0)
        {
            _readBlock.AsSpan(_scanOffset, tailLength).CopyTo(newBlock.AsSpan(0, tailLength));
        }

        _readBlock = newBlock;
        _readBlockLength = tailLength;
        _scanOffset = 0;

        var blocks = _completedBlocks.ToList();
        _completedBlocks.Clear();
        return blocks;
    }

    #endregion

    #region Private Methods

    private void RefillBlock (StreamReader reader)
    {
        var tailLength = _readBlockLength - _scanOffset;

        // Determine new block size: if the tail already fills a standard block,
        // grow the buffer so there's room to read more data. This handles lines
        // longer than BLOCK_SIZE (e.g. huge XML payloads).
        var newBlockSize = BLOCK_SIZE;
        while (tailLength >= newBlockSize)
        {
            newBlockSize *= 2;
        }

        // Rent a new block (may be larger than BLOCK_SIZE for very long lines)
        var newBlock = ArrayPool<char>.Shared.Rent(newBlockSize);

        // Copy the tail (partial line) to the start of the new block
        if (tailLength > 0)
        {
            _readBlock.AsSpan(_scanOffset, tailLength).CopyTo(newBlock.AsSpan(0, tailLength));
        }

        // The old block is fully scanned — add to completed list
        _completedBlocks.Add(_readBlock);

        _readBlock = newBlock;
        _scanOffset = 0;

        // Fill the rest of the block from the stream
        var available = newBlock.Length - tailLength;
        var charsRead = reader.Read(newBlock, tailLength, available);

        _readBlockLength = tailLength + charsRead;

        if (charsRead == 0)
        {
            _eof = true;
        }
    }

    /// <summary>
    /// Lazily fills the first block from the current stream position. Called on the first read
    /// after construction and after any <see cref="PositionAwareStreamReaderBase.Position"/> seek
    /// (which resets <c>_initialized</c> via <see cref="ResetReader"/>).
    /// </summary>
    private void EnsureInitialized (StreamReader reader)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        // A single '\r' and a single '\n' encode to the same number of bytes in every encoding
        // LogExpert uses (ASCII control chars), so one value covers \n, \r and (×2) \r\n.
        Span<char> singleTerminator = [CHAR_LF];
        _terminatorCharByteSize = Encoding.GetByteCount(singleTerminator);

        var charsRead = reader.Read(_readBlock, 0, BLOCK_SIZE);
        _readBlockLength = charsRead;
        _scanOffset = 0;

        if (charsRead == 0)
        {
            _eof = true;
        }
    }

    /// <summary>
    /// Resets scan state so the next read re-fills from the (just seeked) stream position.
    /// Only touches value-type fields, which is safe under the base constructor's virtual call
    /// to this method (the pooled <c>_readBlock</c> is left untouched).
    /// </summary>
    protected override void ResetReader ()
    {
        _scanOffset = 0;
        _readBlockLength = 0;
        _eof = false;
        _initialized = false;

        base.ResetReader();
    }

    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            // Return the current (partially scanned) block
            if (_readBlock != null)
            {
                ArrayPool<char>.Shared.Return(_readBlock);
                _readBlock = null!;
            }

            // Return any completed blocks not yet detached
            foreach (var block in _completedBlocks)
            {
                ArrayPool<char>.Shared.Return(block);
            }

            _completedBlocks.Clear();
        }

        base.Dispose(disposing);
    }

    #endregion
}