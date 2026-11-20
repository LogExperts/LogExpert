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
/// The byte <see cref="PositionAwareStreamReaderBase.Position"/> advances by the bytes a line really
/// occupies, so it stays exact for mixed line endings and for byte sequences that are invalid in the
/// encoding (issue #639): re-encoding decoded text would count each U+FFFD as the bytes of a replacement
/// character instead of the bytes it replaced. Line boundaries are searched as code units of the
/// encoding (one byte, or two or four for UTF-16 and UTF-32); in every encoding LogExpert offers, a
/// <c>\r</c> or <c>\n</c> unit never occurs inside a multi-byte character.
/// <see cref="ReadChar"/> still reads through the base <see cref="StreamReader"/>; it and
/// <see cref="TryReadLine"/> must not be interleaved without setting the position in between.
/// </remarks>
public class PositionAwareStreamReaderDirect : PositionAwareStreamReaderBase, ILogStreamReaderMemory
{
    #region Constants

    private const int BLOCK_SIZE = 32_768;       // 64 KB (32K chars × 2 bytes), under LOH threshold
    private const int BYTE_BUFFER_SIZE = 65_536; // raw bytes read from the stream per refill
    private const int CODE_PAGE_UTF16_LE = 1200;
    private const int CODE_PAGE_UTF16_BE = 1201;

    private static readonly SearchValues<char> _surrogates = SearchValues.Create(Enumerable.Range(0xD800, 0x800).Select(code => (char)code).ToArray());

    #endregion

    #region Fields

    private char[] _readBlock;
    private int _charFill;           // chars of _readBlock already handed out as lines
    private byte[] _byteBuffer;      // rented on the first read
    private int _byteStart;          // first byte of the line in progress
    private int _byteEnd;            // end of the valid bytes in _byteBuffer
    private int _scannedBytes;       // bytes after _byteStart already searched without a terminator
    private bool _eof;
    private bool _initialized;       // terminator units resolved for the encoding

    private int _unitSize;           // bytes per code unit: 1, 2 or 4
    private uint _crUnit;
    private uint _lfUnit;
    private bool _asciiCompatible;   // bytes 0x00–0x7F decode to the same chars
    private bool _nativeUtf16;       // UTF-16 in the machine's byte order

    private readonly List<char[]> _completedBlocks = [];

    public override bool IsDisposed { get; protected set; }

    #endregion

    #region cTor

    public PositionAwareStreamReaderDirect (Stream stream, EncodingOptions encodingOptions, int maximumLineLength)
        : base(stream, encodingOptions, maximumLineLength)
    {
        _readBlock = ArrayPool<char>.Shared.Rent(BLOCK_SIZE);
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
                    // \r is the last unit read so far but more data follows. Refill so the next unit
                    // becomes available, then re-scan from the \r to classify it.
                    _scannedBytes = hitIndex;
                    Refill();
                    continue;
                }

                lineMemory = DecodeLine(hitIndex);
                ConsumeBytes(hitIndex + terminatorBytes);
                return true;
            }

            if (_eof)
            {
                // Emit remaining content as final line (no trailing newline)
                if (available > 0)
                {
                    lineMemory = DecodeLine(available);
                    ConsumeBytes(available);
                    return true;
                }

                lineMemory = default;
                return false;
            }

            // Everything read so far is searched; only whole units count, a partial one is completed by the refill.
            _scannedBytes = available - (available % _unitSize);
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
        if (_charFill > 0)
        {
            _completedBlocks.Add(_readBlock);
            _readBlock = ArrayPool<char>.Shared.Rent(BLOCK_SIZE);
            _charFill = 0;
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

    /// <summary>
    /// Reads how <c>\r</c> and <c>\n</c> are encoded: one byte in single-byte and multi-byte code pages
    /// and UTF-8, a 2-byte or 4-byte unit in UTF-16 and UTF-32 of either byte order.
    /// </summary>
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
        _nativeUtf16 = Encoding.CodePage == (BitConverter.IsLittleEndian ? CODE_PAGE_UTF16_LE : CODE_PAGE_UTF16_BE);
    }

    /// <summary>
    /// Whether every byte 0x00–0x7F decodes to the same char: true for UTF-8, ASCII, Latin-1, the
    /// Windows code pages and GB2312; false for UTF-7 or EBCDIC, which take the encoding's decoder.
    /// </summary>
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

    /// <summary>
    /// Byte offset of the first <c>\r</c> or <c>\n</c> unit in <paramref name="bytes"/>, which starts on a
    /// unit boundary, or -1. A trailing partial unit is not searched.
    /// </summary>
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

    /// <summary>
    /// Decodes the first <paramref name="byteCount"/> bytes of the line in progress into the current
    /// block and returns the text, capped at the maximum line length. A line is decoded on its own:
    /// a sequence cut short by the terminator becomes U+FFFD in this line, as a continuous decode does.
    /// </summary>
    private ReadOnlyMemory<char> DecodeLine (int byteCount)
    {
        var bytes = _byteBuffer.AsSpan(_byteStart, byteCount);

        int charCount;
        if (_asciiCompatible)
        {
            // Most log lines are pure ASCII: widen them directly, which skips the per-call cost of the
            // encoding's decoder. A line with any other byte is decoded by the encoding instead.
            EnsureCharCapacity(byteCount);
            if (Ascii.ToUtf16(bytes, _readBlock.AsSpan(_charFill), out charCount) != OperationStatus.Done)
            {
                charCount = DecodeWithEncoding(bytes);
            }
        }
        else if (_nativeUtf16 && (byteCount & 1) == 0 && !MemoryMarshal.Cast<byte, char>(bytes).ContainsAny(_surrogates))
        {
            // UTF-16 in the machine's byte order without surrogates decodes to its own code units.
            var source = MemoryMarshal.Cast<byte, char>(bytes);
            EnsureCharCapacity(source.Length);
            source.CopyTo(_readBlock.AsSpan(_charFill));
            charCount = source.Length;
        }
        else
        {
            charCount = DecodeWithEncoding(bytes);
        }

        var cappedLength = Math.Min(charCount, MaximumLineLength);

        var lineMemory = _readBlock.AsMemory(_charFill, cappedLength);
        _charFill += cappedLength;
        return lineMemory;
    }

    private int DecodeWithEncoding (ReadOnlySpan<byte> bytes)
    {
        EnsureCharCapacity(Encoding.GetMaxCharCount(bytes.Length));
        return Encoding.GetChars(bytes, _readBlock.AsSpan(_charFill));
    }

    private void ConsumeBytes (int byteCount)
    {
        MovePosition(byteCount);
        _byteStart += byteCount;
        _scannedBytes = 0;
    }

    /// <summary>
    /// Makes room for <paramref name="charCount"/> chars after the lines already in the current block.
    /// A block that holds lines is kept for <see cref="DetachCharBlocks"/>; an unused one goes back to the pool.
    /// Lines longer than BLOCK_SIZE (e.g. huge XML payloads) get a block of their own.
    /// </summary>
    private void EnsureCharCapacity (int charCount)
    {
        if (_readBlock.Length - _charFill >= charCount)
        {
            return;
        }

        if (_charFill > 0)
        {
            _completedBlocks.Add(_readBlock);
        }
        else
        {
            ArrayPool<char>.Shared.Return(_readBlock);
        }

        var blockSize = BLOCK_SIZE;
        while (blockSize < charCount)
        {
            blockSize *= 2;
        }

        _readBlock = ArrayPool<char>.Shared.Rent(blockSize);
        _charFill = 0;
    }

    /// <summary>
    /// Reads more bytes after the line in progress. When the buffer is full, the line in progress is
    /// moved to the front, or the buffer doubles if that line already fills it.
    /// </summary>
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

        var bytesRead = GetByteStream().Read(_byteBuffer, _byteEnd, _byteBuffer.Length - _byteEnd);
        _byteEnd += bytesRead;

        if (bytesRead == 0)
        {
            _eof = true;
        }
    }

    /// <summary>
    /// Discards the bytes read ahead so the next read starts at the (just seeked) stream position.
    /// Only touches value-type fields, which is safe under the base constructor's virtual call to this
    /// method. Lines already handed out stay valid: their chars are kept until <see cref="DetachCharBlocks"/>.
    /// </summary>
    protected override void ResetReader ()
    {
        _byteStart = 0;
        _byteEnd = 0;
        _scannedBytes = 0;
        _eof = false;
        _initialized = false;

        base.ResetReader();
    }

    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            // Return the current (partially filled) block
            if (_readBlock != null)
            {
                ArrayPool<char>.Shared.Return(_readBlock);
                _readBlock = null!;
            }

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