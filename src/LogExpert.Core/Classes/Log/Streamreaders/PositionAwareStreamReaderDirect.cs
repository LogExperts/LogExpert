using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Log.Streamreaders;

/// <summary>
/// Finds line boundaries in the raw bytes, decodes each line into pooled char[] blocks and returns
/// ReadOnlyMemory&lt;char&gt; slices without allocating per-line strings.
/// </summary>
/// <remarks>
/// <see cref="PositionAwareStreamReaderBase.Position"/> advances by the raw bytes of each line, so it
/// stays exact for any line ending and for bytes that are invalid in the encoding (ADR 0010).
/// <see cref="ReadChar"/> reads through the base <see cref="StreamReader"/>; do not interleave it with
/// <see cref="TryReadLine"/> without setting the position in between.
/// </remarks>
public class PositionAwareStreamReaderDirect : PositionAwareStreamReaderBase, ILogStreamReaderMemory
{
    #region Constants

    private const int BLOCK_SIZE = 32_768;       // 64 KB (32K chars × 2 bytes), under LOH threshold
    private const int BYTE_BUFFER_SIZE = 65_536;
    private const int CODE_PAGE_UTF16_LE = 1200;

    private static readonly SearchValues<char> _surrogates = SearchValues.Create(Enumerable.Range(0xD800, 0x800).Select(code => (char)code).ToArray());

    #endregion

    #region Fields

    private char[] _readBlock;
    private int _usedChars;          // chars of _readBlock already handed out as lines
    private byte[] _byteBuffer;
    private int _byteStart;          // first byte of the line in progress
    private int _byteEnd;
    private int _scannedBytes;       // bytes after _byteStart already searched without a terminator
    private bool _eof;
    private bool _lastReadShort;     // the last Read returned less than asked for: the rest of the file is buffered
    private bool _initialized;
    private bool _truncating;                  // the line in progress is over the cap: its bytes are dropped up to the terminator
    private ReadOnlyMemory<char> _truncatedLine;
    private int _truncateAfterBytes;

    private int _unitSize;           // bytes per code unit: 1, 2 or 4
    private uint _crUnit;
    private uint _lfUnit;
    private bool _asciiCompatible;
    private bool _isUtf16LittleEndian;

    private readonly List<char[]> _completedBlocks = [];

    public override bool IsDisposed { get; protected set; }

    #endregion

    #region cTor

    public PositionAwareStreamReaderDirect (Stream stream, EncodingOptions encodingOptions, int maximumLineLength)
        : base(stream, encodingOptions, maximumLineLength)
    {
        _readBlock = [];
    }

    #endregion

    #region Public methods

    public override string ReadLine ()
    {
        return TryReadLine(out var memory) ? memory.ToString() : null;
    }

    /// <summary>
    /// Reads the next line, ended by <c>\n</c>, <c>\r\n</c> or a bare <c>\r</c>, and returns a zero-copy
    /// slice of its decoded text, capped at the maximum line length. The byte position advances by the
    /// line's raw bytes plus the bytes of its actual terminator.
    /// </summary>
    public bool TryReadLine (out ReadOnlyMemory<char> lineMemory)
    {
        EnsureInitialized();

        while (true)
        {
            var available = _byteEnd - _byteStart;
            var hitIndex = FindTerminator(_byteBuffer.AsSpan(_byteStart + _scannedBytes, available - _scannedBytes));

            if (hitIndex >= 0)
            {
                hitIndex += _scannedBytes;

                int terminatorBytes;
                if (UnitAt(hitIndex) == _lfUnit)
                {
                    terminatorBytes = _unitSize;
                }
                else if (hitIndex + (2 * _unitSize) <= available)
                {
                    // \r with a known following unit: \r\n if it's \n, otherwise a bare \r.
                    terminatorBytes = UnitAt(hitIndex + _unitSize) == _lfUnit ? 2 * _unitSize : _unitSize;
                }
                else if (_eof)
                {
                    // \r is the very last unit in the file: a bare \r.
                    terminatorBytes = _unitSize;
                }
                else
                {
                    // \r is the last unit read so far: refill, then re-scan from the \r to classify it.
                    if (_truncating)
                    {
                        ConsumeBytes(hitIndex);
                        hitIndex = 0;
                    }

                    _scannedBytes = hitIndex;
                    Refill();
                    continue;
                }

                lineMemory = _truncating ? _truncatedLine : DecodeLine(hitIndex);
                _truncating = false;
                ConsumeBytes(hitIndex + terminatorBytes);
                return true;
            }

            if (_eof)
            {
                // Emit remaining content as final line (no trailing newline)
                if (available > 0 || _truncating)
                {
                    lineMemory = _truncating ? _truncatedLine : DecodeLine(available);
                    _truncating = false;
                    ConsumeBytes(available);
                    return true;
                }

                lineMemory = default;
                return false;
            }

            // Only whole units are searched; a partial one is completed by the refill.
            var searchedBytes = available - (available % _unitSize);

            if (!_truncating && searchedBytes >= _truncateAfterBytes)
            {
                _truncatedLine = DecodeLine(_truncateAfterBytes);
                _truncating = true;
            }

            if (_truncating)
            {
                // Position still counts the dropped bytes, so it stays exact.
                ConsumeBytes(searchedBytes);
            }
            else
            {
                _scannedBytes = searchedBytes;
            }

            Refill();
        }
    }

    public void ReturnMemory (ReadOnlyMemory<char> memory)
    {
        // Bulk return via DetachBlocks()/Dispose(). Individual return not needed.
    }

    /// <summary>
    /// Detaches the blocks that back the lines read so far, for transfer to the LogBuffer.
    /// The reader continues in a fresh block.
    /// </summary>
    public List<char[]> DetachCharBlocks ()
    {
        if (_usedChars > 0)
        {
            _completedBlocks.Add(_readBlock);
            _readBlock = [];
            _usedChars = 0;
        }

        if (_completedBlocks.Count == 0)
        {
            return [];
        }

        var blocks = _completedBlocks.ToList();
        _completedBlocks.Clear();
        return blocks;
    }

    #endregion

    #region Private Methods

    private void EnsureInitialized ()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _byteBuffer ??= ArrayPool<byte>.Shared.Rent(BYTE_BUFFER_SIZE);

        if (_unitSize == 0)
        {
            ResolveTerminatorUnits();
        }
    }

    // In every encoding LogExpert offers, \r and \n are single code units (1 byte, or 2/4 for
    // UTF-16/UTF-32) that never occur inside a multi-byte character, so lines can be split on bytes.
    private void ResolveTerminatorUnits ()
    {
        var cr = Encoding.GetBytes("\r");
        var lf = Encoding.GetBytes("\n");

        if (cr.Length != lf.Length || lf.Length is not (1 or 2 or 4))
        {
            throw new NotSupportedException($"Encoding '{Encoding.WebName}' does not encode line terminators as single code units.");
        }

        _unitSize = lf.Length;
        _crUnit = ReadUnit(cr, _unitSize);
        _lfUnit = ReadUnit(lf, _unitSize);
        _asciiCompatible = _unitSize == 1 && DecodesAsciiUnchanged(Encoding);
        _isUtf16LittleEndian = Encoding.CodePage == CODE_PAGE_UTF16_LE;

        // No supported encoding needs more than 4 bytes per char, so this prefix holds the first MaximumLineLength
        // chars complete; the extra 4 bytes keep a character cut at the end beyond the cap. A multiple of every unit size.
        _truncateAfterBytes = (int)Math.Min((4L * Math.Max(MaximumLineLength, 0)) + 4, 1 << 29);
    }

    // False for encodings like UTF-7 or EBCDIC, whose 0x00–0x7F bytes are not plain ASCII.
    private static bool DecodesAsciiUnchanged (Encoding encoding)
    {
        Span<byte> ascii = stackalloc byte[128];
        for (var i = 0; i < ascii.Length; i++)
        {
            ascii[i] = (byte)i;
        }

        Span<char> decoded = stackalloc char[encoding.GetMaxCharCount(ascii.Length)];
        var charCount = encoding.GetChars(ascii, decoded);
        if (charCount != ascii.Length)
        {
            return false;
        }

        for (var i = 0; i < ascii.Length; i++)
        {
            if (decoded[i] != i)
            {
                return false;
            }
        }

        return true;
    }

    private static uint ReadUnit (ReadOnlySpan<byte> bytes, int unitSize)
    {
        return unitSize switch
        {
            1 => bytes[0],
            2 => MemoryMarshal.Read<ushort>(bytes),
            _ => MemoryMarshal.Read<uint>(bytes)
        };
    }

    private uint UnitAt (int offset)
    {
        return _unitSize == 1
            ? _byteBuffer[_byteStart + offset]
            : ReadUnit(_byteBuffer.AsSpan(_byteStart + offset, _unitSize), _unitSize);
    }

    private int FindTerminator (ReadOnlySpan<byte> bytes)
    {
        switch (_unitSize)
        {
            case 1:
                {
                    return bytes.IndexOfAny((byte)_crUnit, (byte)_lfUnit);
                }
            case 2:
                {
                    var index = MemoryMarshal.Cast<byte, ushort>(bytes).IndexOfAny((ushort)_crUnit, (ushort)_lfUnit);
                    return index < 0 ? -1 : index * 2;
                }
            default:
                {
                    var index = MemoryMarshal.Cast<byte, uint>(bytes).IndexOfAny(_crUnit, _lfUnit);
                    return index < 0 ? -1 : index * 4;
                }
        }
    }

    // A line is decoded on its own: a sequence cut short by the terminator becomes U+FFFD in this line.
    // Only the text up to the maximum line length reaches the char block.
    private ReadOnlyMemory<char> DecodeLine (int byteCount)
    {
        var bytes = _byteBuffer.AsSpan(_byteStart, byteCount);

        if (_asciiCompatible)
        {
            // Pure-ASCII text skips the per-call cost of the encoding's decoder. An ASCII byte is one char
            // whatever follows, so only the bytes up to the cap are converted.
            var prefix = bytes[..Math.Min(byteCount, MaximumLineLength)];
            EnsureCharCapacity(prefix.Length);
            if (Ascii.ToUtf16(prefix, _readBlock.AsSpan(_usedChars), out var asciiCount) == OperationStatus.Done)
            {
                return Commit(asciiCount);
            }
        }
        else if (_isUtf16LittleEndian)
        {
            // UTF-16 LE without surrogates decodes to its own code units. An odd trailing byte beyond the cap does not matter.
            var units = MemoryMarshal.Cast<byte, char>(bytes);
            var prefix = units[..Math.Min(units.Length, MaximumLineLength)];
            if (((byteCount & 1) == 0 || units.Length >= MaximumLineLength) && !prefix.ContainsAny(_surrogates))
            {
                EnsureCharCapacity(prefix.Length);
                prefix.CopyTo(_readBlock.AsSpan(_usedChars));
                return Commit(prefix.Length);
            }
        }

        return DecodeWithEncoding(bytes);
    }

    private ReadOnlyMemory<char> DecodeWithEncoding (ReadOnlySpan<byte> bytes)
    {
        var maxChars = Encoding.GetMaxCharCount(bytes.Length);
        if (maxChars <= MaximumLineLength)
        {
            EnsureCharCapacity(maxChars);
            return Commit(Encoding.GetChars(bytes, _readBlock.AsSpan(_usedChars)));
        }

        // Text that may exceed the cap is decoded into scratch, so the char block holds only the capped text.
        var scratch = ArrayPool<char>.Shared.Rent(maxChars);
        try
        {
            var cappedLength = Math.Min(Encoding.GetChars(bytes, scratch), MaximumLineLength);
            EnsureCharCapacity(cappedLength);
            scratch.AsSpan(0, cappedLength).CopyTo(_readBlock.AsSpan(_usedChars));
            return Commit(cappedLength);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(scratch);
        }
    }

    private ReadOnlyMemory<char> Commit (int charCount)
    {
        var lineMemory = _readBlock.AsMemory(_usedChars, charCount);
        _usedChars += charCount;
        return lineMemory;
    }

    private void ConsumeBytes (int byteCount)
    {
        MovePosition(byteCount);
        _byteStart += byteCount;
        _scannedBytes = 0;
    }

    // A block that already holds lines is kept for DetachCharBlocks; lines longer than BLOCK_SIZE get a block of their own.
    // A block is never larger than the bytes left to read, so a tail poll that reads one line does not pin a full block.
    private void EnsureCharCapacity (int charCount)
    {
        if (_readBlock.Length - _usedChars >= charCount)
        {
            return;
        }

        if (_usedChars > 0)
        {
            _completedBlocks.Add(_readBlock);
        }
        else if (_readBlock.Length > 0)
        {
            ArrayPool<char>.Shared.Return(_readBlock);
        }

        int blockSize;
        if (charCount > BLOCK_SIZE)
        {
            blockSize = BLOCK_SIZE;
            while (blockSize < charCount)
            {
                blockSize *= 2;
            }
        }
        else
        {
            // After a short read the rest of the file is buffered, and no encoding decodes n bytes to more than n + 1 chars.
            blockSize = _lastReadShort
                ? Math.Clamp(_byteEnd - _byteStart + 1, charCount, BLOCK_SIZE)
                : BLOCK_SIZE;
        }

        _readBlock = ArrayPool<char>.Shared.Rent(blockSize);
        _usedChars = 0;
    }

    // When the buffer is full, the line in progress moves to the front, or the buffer doubles if that line fills it.
    private void Refill ()
    {
        if (_byteEnd == _byteBuffer.Length)
        {
            var lineLength = _byteEnd - _byteStart;

            if (_byteStart > 0)
            {
                _byteBuffer.AsSpan(_byteStart, lineLength).CopyTo(_byteBuffer);
            }
            else
            {
                var grown = ArrayPool<byte>.Shared.Rent(_byteBuffer.Length * 2);
                _byteBuffer.AsSpan(0, lineLength).CopyTo(grown);
                ArrayPool<byte>.Shared.Return(_byteBuffer);
                _byteBuffer = grown;
            }

            _byteStart = 0;
            _byteEnd = lineLength;
        }

        var requested = _byteBuffer.Length - _byteEnd;
        var bytesRead = GetByteStream().Read(_byteBuffer, _byteEnd, requested);
        _byteEnd += bytesRead;
        _lastReadShort = bytesRead < requested;

        if (bytesRead == 0)
        {
            _eof = true;
        }
    }

    // Only value-type fields: the base constructor calls this before the fields are initialized.
    // Lines already handed out stay valid until DetachCharBlocks.
    protected override void ResetReader ()
    {
        _byteStart = 0;
        _byteEnd = 0;
        _scannedBytes = 0;
        _eof = false;
        _lastReadShort = false;
        _truncating = false;
        _initialized = false;

        base.ResetReader();
    }

    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            // Return the current (partially filled) block
            if (_readBlock is { Length: > 0 })
            {
                ArrayPool<char>.Shared.Return(_readBlock);
            }

            _readBlock = null!;

            if (_byteBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_byteBuffer);
                _byteBuffer = null!;
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