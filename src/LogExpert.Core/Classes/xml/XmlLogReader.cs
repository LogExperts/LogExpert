using LogExpert.Core.Classes.Log;
using LogExpert.Core.Interface;

using System.Text;

namespace LogExpert.Core.Classes.xml;

public class XmlLogReader : LogStreamReaderBase
{
    #region Fields

    private readonly ILogStreamReader reader;
    public override bool IsDisposed { get; protected set; }

    #endregion

    #region cTor

    public XmlLogReader(ILogStreamReader reader)
    {
        this.reader = reader;
    }

    #endregion

    #region Properties

    public override long Position
    {
        get => reader.Position;
        set => reader.Position = value;
    }

    public override Encoding Encoding => reader.Encoding;

    public override bool IsBufferComplete => reader.IsBufferComplete;

    public string StartTag { get; set; } = "<log4j:event";

    public string EndTag { get; set; } = "</log4j:event>";

    #endregion

    #region Public methods

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            reader.Dispose();
            IsDisposed = true;
        }
    }

    public override int ReadChar()
    {
        return reader.ReadChar();
    }

    public override string ReadLine()
    {
        short state = 0;
        var tagIndex = 0;
        var blockComplete = false;
        var eof = false;
        var tryCounter = 5;

        StringBuilder builder = new();

        while (!eof && !blockComplete)
        {
            var readInt = ReadChar();
            if (readInt == -1)
            {
                // if eof before the block is complete, wait some msecs for the logger to flush the complete xml struct
                if (state != 0)
                {
                    if (--tryCounter > 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            var readChar = (char)readInt;
            // state:
            // 0 = looking for tag start
            // 1 = reading into buffer as long as the read data matches the start tag
            // 2 = reading into buffer while waiting for the begin of the end tag
            // 3 = reading into buffer as long as data matches the end tag. stopping when tag complete
            switch (state)
            {
                case 0:
                    if (readChar == StartTag[0])
                    {
                        //_logger.logInfo("state = 1");
                        state = 1;
                        tagIndex = 1;
                        builder.Append(readChar);
                    }
                    //else
                    //{
                    //  _logger.logInfo("char: " + readChar);
                    //}
                    break;
                case 1:
                    if (readChar == StartTag[tagIndex])
                    {
                        builder.Append(readChar);

                        if (++tagIndex >= StartTag.Length)
                        {
                            //_logger.logInfo("state = 2");
                            state = 2; // start Tag complete
                            tagIndex = 0;
                        }
                    }
                    else
                    {
                        // tag doesn't match anymore
                        //_logger.logInfo("state = 0 [" + buffer.ToString() + readChar + "]");
                        state = 0;
                        builder.Clear();
                    }
                    break;
                case 2:
                    builder.Append(readChar);

                    if (readChar == EndTag[0])
                    {
                        //_logger.logInfo("state = 3");
                        state = 3;
                        tagIndex = 1;
                    }
                    break;
                case 3:
                    builder.Append(readChar);

                    if (readChar == EndTag[tagIndex])
                    {
                        tagIndex++;
                        if (tagIndex >= EndTag.Length)
                        {
                            blockComplete = true;
                            break;
                        }
                    }
                    else
                    {
                        //_logger.logInfo("state = 2");
                        state = 2;
                    }
                    break;
            }
        }

        return blockComplete ? builder.ToString() : null;
    }

    #endregion
}