using System.Text;

using LogExpert.Core.Entities;

namespace LogExpert.Core.Classes.Log.Streamreaders;

public abstract class PositionAwareStreamReaderBase : LogStreamReaderBase
{

    #region Fields

    private readonly BufferedStream _stream;
    private readonly StreamReader _reader;

    private readonly int _preambleLength;
    private readonly int _posIncPrecomputed;

    private long _position;

    private static readonly Encoding[] _preambleEncodings =
    [
        Encoding.UTF8,
        Encoding.Unicode,
        Encoding.BigEndianUnicode,
        Encoding.UTF32
    ];

    #endregion

    #region cTor

    protected PositionAwareStreamReaderBase (Stream stream, EncodingOptions encodingOptions, int maximumLineLength)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must support reading.", nameof(stream));
        }

        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must support seeking.", nameof(stream));
        }

        _stream = new BufferedStream(stream);

        MaximumLineLength = maximumLineLength;

        (_preambleLength, Encoding? detectedEncoding) = DetectPreambleLength(_stream);

        var usedEncoding = DetermineEncoding(encodingOptions, detectedEncoding);
        _posIncPrecomputed = GetPosIncPrecomputed(usedEncoding);

        _reader = new StreamReader(_stream, usedEncoding, true);

        Position = 0;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Current position in the stream.
    /// </summary>
    public sealed override long Position
    {
        get => _position;
        set
        {
            /*
             * 1: Sometime commented (+Encoding.GetPreamble().Length)
             * 2: Date 1.1 3207
             * 3: Error Message from Piet because of Unicode-Bugs.
             *    No Idea, if this is OK.
             * 4: 27.07.09: Preamble-Length is now calculated in CT, because Encoding.GetPreamble().Length
             *    always delivers a fixed length (does not mater what kind of data)
             */
            _position = value; //  +Encoding.GetPreamble().Length;      // 1
                               //stream.Seek(pos, SeekOrigin.Begin);     // 2
                               //stream.Seek(pos + Encoding.GetPreamble().Length, SeekOrigin.Begin);  // 3
            _ = _stream.Seek(_position + _preambleLength, SeekOrigin.Begin); // 4

            ResetReader();
        }
    }

    public sealed override Encoding Encoding => _reader.CurrentEncoding;

    public sealed override bool IsBufferComplete => true;

    protected static int MaximumLineLength
    {
        get => field;
        private set => field = value;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Destroy and release the current stream reader.
    /// </summary>
    /// <param name="disposing">Specifies whether or not the managed objects should be released.</param>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            _stream.Dispose();
            _reader.Dispose();
            IsDisposed = true;
        }
    }

    //TODO This is unsafe and should be refactored
    public override unsafe int ReadChar ()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, GetType());

        try
        {
            var readInt = _reader.Read();
            if (readInt != -1)
            {
                var readChar = (char)readInt;
                if (_posIncPrecomputed != 0)
                {
                    _position += _posIncPrecomputed;
                }
                else
                {
                    _position += _reader.CurrentEncoding.GetByteCount(&readChar, 1);
                }
            }

            return readInt;
        }
        catch (IOException)
        {
            return -1;
        }
    }

    protected virtual void ResetReader ()
    {
        _reader.DiscardBufferedData();
    }

    protected StreamReader GetStreamReader ()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, GetType());
        return _reader;
    }

    protected void MovePosition (int offset)
    {
        _position += offset;
    }

    #endregion

    #region Private Methods

    public static Encoding DetermineEncoding (EncodingOptions options, Encoding detectedEncoding)
    {
        return options?.Encoding != null
            ? options.Encoding
            : detectedEncoding ?? options?.DefaultEncoding ?? Encoding.Default;
    }

    /// <summary>
    /// Determines the actual number of preamble bytes in the file and the Encoding.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns>Number of preamble bytes in the file and the Encoding if there is one</returns>
    public static (int length, Encoding? detectedEncoding) DetectPreambleLength (Stream stream)
    {
        /*
        UTF-8:                          EF BB BF
        UTF-16-Big-Endian-Byteorder:    FE FF
        UTF-16-Little-Endian-Byteorder: FF FE
        UTF-32-Big-Endian-Byteorder:    00 00 FE FF
        UTF-32-Little-Endian-Byteorder: FF FE 00 00
        */

        if (!stream.CanSeek)
        {
            return (0, null);
        }

        var originalPos = stream.Position;
        Span<byte> buffer = stackalloc byte[4];
        _ = stream.Seek(0, SeekOrigin.Begin);
        var readBytes = stream.Read(buffer);
        _ = stream.Seek(originalPos, SeekOrigin.Begin);

        if (readBytes >= 2)
        {
            foreach (var encoding in _preambleEncodings)
            {
                var preamble = encoding.GetPreamble();
                var fail = false;
                for (var i = 0; i < readBytes && i < preamble.Length; ++i)
                {
                    if (buffer[i] != preamble[i])
                    {
                        fail = true;
                        break;
                    }
                }

                if (!fail)
                {
                    return (preamble.Length, encoding);
                }
            }
        }

        return (0, null);
    }

    /// <summary>
    /// Bytes to advance the position by per character read, or 0 for "measure the character".
    /// </summary>
    /// <remarks>
    /// The fallback is keyed on <see cref="Encoding.IsSingleByte"/>, not on "everything else is one
    /// byte": a variable-width code page such as GB2312 (one byte per ASCII character, two per Chinese
    /// one) is neither a <see cref="UTF8Encoding"/> nor a <see cref="UnicodeEncoding"/>, and counting it
    /// as one byte per character drifts the position on the first non-ASCII character.
    /// </remarks>
    public static int GetPosIncPrecomputed (Encoding usedEncoding)
    {
        ArgumentNullException.ThrowIfNull(usedEncoding);

        switch (usedEncoding)
        {
            case UTF8Encoding:
                {
                    return 0;
                }
            case UnicodeEncoding:
                {
                    return 2;
                }
            default:
                {
                    return usedEncoding.IsSingleByte ? 1 : 0;
                }
        }
    }

    #endregion
}