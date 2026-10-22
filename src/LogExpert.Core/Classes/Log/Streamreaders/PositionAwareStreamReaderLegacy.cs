using LogExpert.Core.Classes.Log.Buffers;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Log.Streamreaders;

public class PositionAwareStreamReaderLegacy (Stream stream, EncodingOptions encodingOptions, int maximumLineLength) : PositionAwareStreamReaderBase(stream, encodingOptions, maximumLineLength), ILogStreamReaderMemory
{
    #region Fields

    private readonly char[] _charBuffer = new char[maximumLineLength];

    private int _charBufferPos;
    private bool _crDetect;

    public override bool IsDisposed { get; protected set; }

    #endregion

    #region Properties

    private CharBlockAllocator BlockAllocator
    {
        get => field ??= new CharBlockAllocator();
        set;
    }

    #endregion

    #region Public methods

    public bool TryReadLine (out ReadOnlyMemory<char> lineMemory)
    {
        var line = ReadLine();

        if (line is null)
        {
            lineMemory = default;
            return false;
        }

        var target = BlockAllocator.Rent(line.Length);
        line.AsSpan().CopyTo(target.Span);
        lineMemory = target;
        return true;
    }

    public void ReturnMemory (ReadOnlyMemory<char> memory)
    {
        // Bulk return via DetachCharBlocks() when the LogBuffer is evicted, or Dispose().
    }

    public List<char[]> DetachCharBlocks () => BlockAllocator.DetachBlocks();

    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            BlockAllocator?.Dispose();
            BlockAllocator = null;
        }

        base.Dispose(disposing);
    }

    public override string ReadLine ()
    {
        int readInt;

        while (-1 != (readInt = ReadChar()))
        {
            var readChar = (char)readInt;

            switch (readChar)
            {
                case '\n':
                    {
                        _crDetect = false;
                        return GetLineAndResetCharBufferPos();
                    }
                case '\r':
                    {
                        if (_crDetect)
                        {
                            return GetLineAndResetCharBufferPos();
                        }

                        _crDetect = true;
                        break;
                    }
                default:
                    {
                        if (_crDetect)
                        {
                            _crDetect = false;
                            var line = GetLineAndResetCharBufferPos();
                            AppendToCharBuffer(readChar);
                            return line;
                        }

                        AppendToCharBuffer(readChar);
                        break;
                    }
            }
        }

        var result = GetLineAndResetCharBufferPos();
        if (readInt == -1 && result.Length == 0 && !_crDetect)
        {
            return null; // EOF
        }

        _crDetect = false;
        return result;
    }

    protected override void ResetReader ()
    {
        ResetCharBufferPos();

        base.ResetReader();
    }

    #endregion

    #region Private Methods

    private string GetLineAndResetCharBufferPos ()
    {
        string result = new(_charBuffer, 0, _charBufferPos);
        ResetCharBufferPos();
        return result;
    }

    private void AppendToCharBuffer (char readChar)
    {
        if (_charBufferPos < MaximumLineLength)
        {
            _charBuffer[_charBufferPos++] = readChar;
        }
    }

    private void ResetCharBufferPos ()
    {
        _charBufferPos = 0;
    }

    #endregion
}