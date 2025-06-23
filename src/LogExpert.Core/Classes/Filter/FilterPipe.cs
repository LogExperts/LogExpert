using System.Globalization;
using System.Text;
using LogExpert.Core.Interface;
using NLog;

namespace LogExpert.Core.Classes.Filter;

public class FilterPipe : IDisposable
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private List<int> _lineMappingList = [];
    private StreamWriter _writer;
    private readonly object _fileNameLock = new();
    private bool _disposed;

    #endregion

    #region cTor

    public FilterPipe(FilterParams filterParams, ILogWindow logWindow)
    {
        FilterParams = filterParams;
        LogWindow = logWindow;
        IsStopped = false;
        FileName = Path.GetTempFileName();
        _disposed = false;

        _logger.Info($"Created temp file: {FileName}");
    }

    #endregion

    #region Events

    public event EventHandler<EventArgs> Closed;

    #endregion

    #region Properties

    public bool IsStopped { get; set; }

    public string FileName { get; }

    public FilterParams FilterParams { get; }

    public IList<int> LastLinesHistoryList { get; } = [];

    public ILogWindow LogWindow { get; }

    public ILogWindow OwnLogWindow { get; set; }

    #endregion

    #region Public methods

    public void OpenFile ()
    {
        FileStream fStream = new(FileName, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(fStream, new UnicodeEncoding(false, false));
    }

    public void CloseFile ()
    {
        if (_writer != null)
        {
            _writer.Close();
            _writer = null;
        }
    }

    //TOOD: check if the callers are checking for null before calling
    public bool WriteToPipe (ILogLine textLine, int orgLineNum)
    {
        ArgumentNullException.ThrowIfNull(textLine, nameof(textLine));

        try
        {
            lock (_fileNameLock)
            {
                lock (_lineMappingList)
                {
                    try
                    {
                        _writer.WriteLine(textLine.FullLine);
                        _lineMappingList.Add(orgLineNum);
                        return true;
                    }
                    catch (IOException e)
                    {
                        _logger.Error(e, "writeToPipe()");
                        return false;
                    }
                }
            }
        }
        catch (IOException ex)
        {
            _logger.Error(ex, "writeToPipe(): file was closed");
            return false;
        }
    }

    public int GetOriginalLineNum (int lineNum)
    {
        lock (_lineMappingList)
        {
            return _lineMappingList.Count > lineNum
                ? _lineMappingList[lineNum]
                : -1;
        }
    }

    public void ShiftLineNums (int offset)
    {
        _logger.Debug($"FilterPipe.ShiftLineNums() offset={offset}");
        List<int> newList = [];
        lock (_lineMappingList)
        {
            foreach (var lineNum in _lineMappingList)
            {
                var line = lineNum - offset;
                if (line >= 0)
                {
                    newList.Add(line);
                }
                else
                {
                    newList.Add(-1);
                }
            }

            _lineMappingList = newList;
        }
    }

    public void ClearLineNums ()
    {
        _logger.Debug(CultureInfo.InvariantCulture, "FilterPipe.ClearLineNums()");
        lock (_lineMappingList)
        {
            for (var i = 0; i < _lineMappingList.Count; ++i)
            {
                _lineMappingList[i] = -1;
            }
        }
    }

    public void ClearLineList ()
    {
        lock (_lineMappingList)
        {
            _lineMappingList.Clear();
        }
    }

    public void RecreateTempFile ()
    {
        lock (_lineMappingList)
        {
            _lineMappingList = [];
        }

        lock (_fileNameLock)
        {
            CloseFile();
            // trunc file
            FileStream fStream = new(FileName, FileMode.Truncate, FileAccess.Write, FileShare.Read);
            fStream.SetLength(0);
            fStream.Close();
        }
    }

    public void CloseAndDisconnect ()
    {
        ClearLineList();
        OnClosed();
    }

    #endregion

    #region Private Methods

    private void OnClosed ()
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose ()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _writer?.Dispose();
            }

            _disposed = true;
        }
    }

    #endregion
}