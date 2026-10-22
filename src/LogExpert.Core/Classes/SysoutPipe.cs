using System.Diagnostics;
using System.Globalization;
using System.Text;

using NLog;

namespace LogExpert.Core.Classes;

public class SysoutPipe : IDisposable
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly Process _process;
    private readonly StreamReader _sysout;
    private StreamWriter _writer;
    private bool _disposed;

    #endregion

    #region cTor

    public SysoutPipe (Process process)
    {
        _disposed = false;

        // Hold a strong reference to the process for the lifetime of the pipe. Without it the
        // process becomes unrooted as soon as the launcher returns, gets finalized, and reading
        // StandardOutput then throws ObjectDisposedException (races on fast-exiting processes).
        // `process` is rooted as the constructor argument here, so reading StandardOutput is safe.
        _process = process;
        _sysout = process.StandardOutput;

        // Subscribe here rather than at the call site so the process cannot exit/dispose between
        // construction and subscription.
        process.Exited += ProcessExitedEventHandler;

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

    public void ClosePipe ()
    {
        _writer.Close();
        _writer = null;
    }


    public void DataReceivedEventHandler (object sender, DataReceivedEventArgs e)
    {
        _writer.WriteLine(e.Data);
    }

    public void ProcessExitedEventHandler (object sender, EventArgs e)
    {
        //ClosePipe();
        if (sender.GetType() == typeof(Process))
        {
            ((Process)sender).Exited -= ProcessExitedEventHandler;
            ((Process)sender).OutputDataReceived -= DataReceivedEventHandler;
        }
    }

    #endregion

    protected void ReaderThread ()
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

        // Output is fully drained — release the process handle deterministically.
        _process.Dispose();
    }

    public void Dispose ()
    {
        Dispose(true);
        GC.SuppressFinalize(this); // Suppress finalization (not needed but best practice)
    }

    protected virtual void Dispose (bool disposing)
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