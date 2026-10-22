using System.Text;

using LogExpert.Core.Classes.Log.Buffers;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Log.Streamreaders;

/// <summary>
/// This class is responsible for reading line from the log file. It also decodes characters with the appropriate charset encoding.
/// PositionAwareStreamReaderSystem tries a BOM detection to determine correct file offsets when directly seeking into the file (on re-loading flushed buffers).
/// UTF-8 handling is a bit slower, because after reading a character the byte length of the character must be determined.
/// Lines are read char-by-char. StreamReader.ReadLine() is not used because StreamReader cannot tell a file position.
/// </summary>
public class PositionAwareStreamReaderSystem : PositionAwareStreamReaderBase, ILogStreamReaderMemory
{
    #region Fields

    private const int CHAR_CR = 0x0D;
    private const int CHAR_LF = 0x0A;

    private int _newLineSequenceLength;

    public override bool IsDisposed { get; protected set; }

    #endregion

    #region cTor

    public PositionAwareStreamReaderSystem (Stream stream, EncodingOptions encodingOptions, int maximumLineLength) : base(stream, encodingOptions, maximumLineLength)
    {
    }

    #endregion

    #region Properties

    /// <summary>
    /// The block allocator used by this reader instance. Blocks are handed to the owning buffer via
    /// <see cref="DetachCharBlocks"/>; ownership of the allocator does not cross the reader seam.
    /// </summary>
    private CharBlockAllocator BlockAllocator
    {
        get => field ??= new CharBlockAllocator();
        set;
    }

    #endregion

    #region Public methods

    public override string ReadLine ()
    {
        var reader = GetStreamReader();

        if (_newLineSequenceLength == 0)
        {
            _newLineSequenceLength = GuessNewLineSequenceLength(reader);
        }

        var line = reader.ReadLine();

        if (line != null)
        {
            MovePosition(Encoding.GetByteCount(line) + _newLineSequenceLength);

            if (line.Length > MaximumLineLength)
            {
                line = line[..MaximumLineLength];
            }
        }

        return line;
    }

    /// <summary>
    /// Attempts to read the next line from the stream without allocating a new string.
    /// The returned Memory&lt;char&gt; is valid until the next call to TryReadLine or ReturnMemory.
    /// </summary>
    public bool TryReadLine (out ReadOnlyMemory<char> lineMemory)
    {
        var reader = GetStreamReader();

        if (_newLineSequenceLength == 0)
        {
            _newLineSequenceLength = GuessNewLineSequenceLength(reader);
        }

        var line = reader.ReadLine();

        if (line is null)
        {
            lineMemory = default;
            return false;
        }

        MovePosition(Encoding.GetByteCount(line) + _newLineSequenceLength);

        var length = Math.Min(line.Length, MaximumLineLength);

        // Allocate from block and copy
        var allocator = BlockAllocator;
        var target = allocator.Rent(length);
        line.AsSpan(0, length).CopyTo(target.Span);
        lineMemory = target;
        return true;
    }

    /// <summary>
    /// Returns the memory buffer. For the block-based reader, individual returns are not tracked — blocks are returned
    /// in bulk via the BlockAllocator when the LogBuffer is evicted or the reader is disposed.
    /// </summary>
    public void ReturnMemory (ReadOnlyMemory<char> memory)
    {
        // Bulk return via DetachCharBlocks() or Dispose().
        // Individual per-line return is not needed with block-based allocation.
    }

    public List<char[]> DetachCharBlocks () => BlockAllocator.DetachBlocks();

    #endregion

    #region Private Methods

    private int GuessNewLineSequenceLength (StreamReader reader)
    {
        var currentPos = Position;

        try
        {
            var line = reader.ReadLine();

            if (line != null)
            {
                Position += Encoding.GetByteCount(line);

                var firstChar = reader.Read();
                if (firstChar == CHAR_CR) // check \r
                {
                    var secondChar = reader.Read();
                    if (secondChar == CHAR_LF) // check \n
                    {
                        // Use stackalloc or SpanOwner instead of string
                        Span<char> newline = ['\r', '\n'];
                        return Encoding.GetByteCount(newline);
                        //return Encoding.GetByteCount("\r\n");
                    }
                }

                Span<char> singleChar = [(char)firstChar];
                return Encoding.GetByteCount(singleChar);
            }

            return 0;
        }
        finally
        {
            Position = currentPos;
        }
    }

    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            BlockAllocator?.Dispose();
            BlockAllocator = null;
        }

        base.Dispose(disposing);
    }

    #endregion
}