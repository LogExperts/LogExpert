using System;
using System.Text;

using LogExpert.Core.Interface;

namespace LogExpert.Core.Classes.Log;

public abstract class LogStreamReaderBase : ILogStreamReader
{
    #region cTor

    protected LogStreamReaderBase()
    {

    }

    ~LogStreamReaderBase()
    {
        Dispose(false);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Current position in the stream.
    /// </summary>
    public abstract long Position { get; set; }

    public abstract bool IsBufferComplete { get; }

    public abstract Encoding Encoding { get; }

    /// <summary>
    /// Indicates whether or not the stream reader has already been disposed.
    /// </summary>
    public abstract bool IsDisposed { get; protected set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Destroy and release the current stream reader.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    /// <summary>
    /// Destroy and release the current stream reader.
    /// </summary>
    /// <param name="disposing">Specifies whether or not the managed objects should be released.</param>
    protected abstract void Dispose(bool disposing);

    public abstract int ReadChar();

    public abstract string ReadLine();

    #endregion
}
