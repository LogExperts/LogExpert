using LogExpert.Config;
using LogExpert.Entities;

using System;
using System.IO;
using System.Text;

namespace LogExpert.Classes.Log
{
    public abstract class PositionAwareStreamReaderBase : LogStreamReaderBase
    {
        #region Fields

        private const int MAX_LINE_LEN = 20000;

        private static readonly Encoding[] _preambleEncodings = { Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode, Encoding.UTF32 };

        private readonly BufferedStream _stream;
        private readonly StreamReader _reader;

        private readonly int _preambleLength;
        private readonly int _posIncPrecomputed;

        private long _position;

        #endregion

        #region cTor

        protected PositionAwareStreamReaderBase(Stream stream, EncodingOptions encodingOptions)
        {
            _stream = new BufferedStream(stream);

            _preambleLength = DetectPreambleLengthAndEncoding(out Encoding detectedEncoding);

            Encoding usedEncoding = GetUsedEncoding(encodingOptions, detectedEncoding);
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
                _stream.Seek(_position + _preambleLength, SeekOrigin.Begin); // 4

                ResetReader();
            }
        }

        public sealed override Encoding Encoding => _reader.CurrentEncoding;

        public sealed override bool IsBufferComplete => true;

        protected static int MaxLineLen => ConfigManager.Settings.Preferences.MaxLineLength;

        #endregion

        #region Public methods

        /// <summary>
        /// Destroy and release the current stream reader.
        /// </summary>
        /// <param name="disposing">Specifies whether or not the managed objects should be released.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
                _reader.Dispose();
            }
        }

        public override unsafe int ReadChar()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(ToString());
            }

            try
            {
                int readInt = _reader.Read();
                if (readInt != -1)
                {
                    char readChar = (char)readInt;
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

        protected virtual void ResetReader()
        {
            _reader.DiscardBufferedData();
        }

        protected StreamReader GetStreamReader()
        {
            return IsDisposed ? throw new ObjectDisposedException(ToString()) : _reader;
        }

        protected void MovePosition(int offset)
        {
            _position += offset;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines the actual number of preamble bytes in the file.
        /// </summary>
        /// <returns>Number of preamble bytes in the file</returns>
        private int DetectPreambleLengthAndEncoding(out Encoding detectedEncoding)
        {
            /*
            UTF-8:                          EF BB BF
            UTF-16-Big-Endian-Byteorder:    FE FF
            UTF-16-Little-Endian-Byteorder: FF FE
            UTF-32-Big-Endian-Byteorder:    00 00 FE FF
            UTF-32-Little-Endian-Byteorder: FF FE 00 00
            */

            byte[] readPreamble = new byte[4];
            int readLen = _stream.Read(readPreamble, 0, 4);
            if (readLen >= 2)
            {
                foreach (Encoding encoding in _preambleEncodings)
                {
                    byte[] preamble = encoding.GetPreamble();
                    bool fail = false;
                    for (int i = 0; i < readLen && i < preamble.Length; ++i)
                    {
                        if (readPreamble[i] != preamble[i])
                        {
                            fail = true;
                            break;
                        }
                    }

                    if (!fail)
                    {
                        detectedEncoding = encoding;
                        return preamble.Length;
                    }
                }
            }

            // not found or less than 2 byte read
            detectedEncoding = null;
            return 0;
        }

        private Encoding GetUsedEncoding(EncodingOptions encodingOptions, Encoding detectedEncoding)
        {
            if (encodingOptions.Encoding != null)
            {
                return encodingOptions.Encoding;
            }

            if (detectedEncoding != null)
            {
                return detectedEncoding;
            }

            return encodingOptions.DefaultEncoding ?? Encoding.Default;
        }
        private int GetPosIncPrecomputed(Encoding usedEncoding)
        {
            switch (usedEncoding)
            {
                case UTF8Encoding _:
                    {
                        return 0;
                    }
                case UnicodeEncoding _:
                    {
                        return 2;
                    }
                default:
                    {
                        return 1;
                    }
            }
        }

        #endregion
    }
}