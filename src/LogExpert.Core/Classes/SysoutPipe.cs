using NLog;

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace LogExpert.Core.Classes;

public class SysoutPipe : IDisposable
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly StreamReader _sysout;
    private StreamWriter _writer;
    private bool _disposed;

    #endregion

    #region cTor

    public SysoutPipe(StreamReader sysout)
    {
        _disposed = false;
        this._sysout = sysout;
        FileName = Path.GetTempFileName();
        _logger.Info(CultureInfo.InvariantCulture, "sysoutPipe created temp file: {0}", FileName);

        FileStream fStream = new(FileName, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(fStream, Encoding.Unicode);

        Thread thread = new(new ThreadStart(ReaderThread))
        {
            IsBackground = true
        };
        thread.Start();
    }

    #endregion

    #region Properties

    public string FileName { get; }

    #endregion

    #region Public methods

    public void ClosePipe()
    {
        _writer.Close();
        _writer = null;
    }


    public void DataReceivedEventHandler(object sender, DataReceivedEventArgs e)
    {
        _writer.WriteLine(e.Data);
    }

    public void ProcessExitedEventHandler(object sender, System.EventArgs e)
    {
        //ClosePipe();
        if (sender.GetType() == typeof(Process))
        {
            ((Process)sender).Exited -= ProcessExitedEventHandler;
            ((Process)sender).OutputDataReceived -= DataReceivedEventHandler;
        }
    }

    #endregion

    protected void ReaderThread()
    {
        var buff = new char[256];

        while (true)
        {
            try
            {
                var read = _sysout.Read(buff, 0, 256);
                if (read == 0)
                {
                    break;
                }
                _writer.Write(buff, 0, read);
            }
            catch (IOException e)
            {
                _logger.Error(e);
                break;
            }
        }

        ClosePipe();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this); // Suppress finalization (not needed but best practice)
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _writer.Dispose(); // Dispose managed resources
            }

            _disposed = true;
        }
    }
}