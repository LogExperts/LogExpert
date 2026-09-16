using System.Globalization;
using System.Text;

using ColumnizerLib;

using LogExpert.Core.Classes.Log.Buffers;
using LogExpert.Core.Classes.Log.ProgressReporters;
using LogExpert.Core.Classes.Log.Streamreaders;
using LogExpert.Core.Classes.xml;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;
using LogExpert.Core.EventArguments;
using LogExpert.Core.Interfaces;

using NLog;

namespace LogExpert.Core.Classes.Log;

public partial class LogfileReader : ILogfileReader, IMultiFileNavigation, ILogfileReaderConfiguration, IBufferPinning, ILogfileReaderDiagnostics
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly string _fileName;
    private readonly int _max_buffers;
    private readonly int _maxLinesPerBuffer;
    private readonly Lock _monitor = new();
    private readonly MultiFileOptions _multiFileOptions;
    private readonly IPluginRegistry _pluginRegistry;
    private readonly CancellationTokenSource _cts = new();
    private readonly ReaderType _readerType;
    private readonly int _maximumLineLength;

    private readonly LogBufferPool _bufferPool;
    private readonly Lock _logBufferLock = new();

    private readonly ILoadProgressReporter _progressReporter;

    private const int WAIT_TIME = 1000;

    private bool _contentDeleted;

    private long _fileLength;
    private Task _garbageCollectorTask;
    private Task _monitorTask;
    private bool _isDeleted;

    private IList<ILogFileInfo> _logFileInfoList = [];
    private readonly bool _hasExplicitFileList;
    private bool _shouldStop;
    private bool _disposed;
    private ILogFileInfo _watchedILogFileInfo;

    private volatile bool _isFailModeCheckCallPending;
    private volatile bool _isFastFailOnGetLogLine;

    #endregion

    #region cTor

    /// Public constructor for single file.
    public LogfileReader (
        string fileName,
        EncodingOptions encodingOptions,
        bool multiFile,
        int bufferCount,
        int linesPerBuffer,
        MultiFileOptions multiFileOptions,
        ReaderType readerType,
        IPluginRegistry pluginRegistry,
        int maximumLineLength,
        ILoadProgressReporter? progressReporter = null)
    : this(
          [fileName],
          encodingOptions,
          multiFile,
          bufferCount,
          linesPerBuffer,
          multiFileOptions,
          readerType,
          pluginRegistry,
          maximumLineLength,
          progressReporter)
    {
    }

    /// Public constructor for multiple files.
    public LogfileReader (
        string[] fileNames,
        EncodingOptions encodingOptions,
        int bufferCount,
        int linesPerBuffer,
        MultiFileOptions multiFileOptions,
        ReaderType readerType,
        IPluginRegistry pluginRegistry,
        int maximumLineLength,
        ILoadProgressReporter? progressReporter = null)
        : this(
              fileNames,
              encodingOptions,
              true,
              bufferCount,
              linesPerBuffer,
              multiFileOptions,
              readerType,
              pluginRegistry,
              maximumLineLength,
              progressReporter)
    {
        // In this overload, we assume multiFile is always true.
    }

    // Single private constructor that contains the common initialization logic.
    private LogfileReader (
        string[] fileNames,
        EncodingOptions encodingOptions,
        bool multiFile,
        int bufferCount,
        int linesPerBuffer,
        MultiFileOptions multiFileOptions,
        ReaderType readerType,
        IPluginRegistry pluginRegistry,
        int maximumLineLength,
        ILoadProgressReporter? progressReporter = null)
    {
        // Validate input: at least one file must be provided.
        if (fileNames == null || fileNames.Length < 1)
        {
            throw new ArgumentException(Resources.LogfileReader_Error_Message_MustProvideAtLeastOneFile, nameof(fileNames));
        }

        //Set default maximum line length if invalid value provided.
        if (maximumLineLength <= 0)
        {
            maximumLineLength = 500;
        }

        _maximumLineLength = maximumLineLength;
        _readerType = readerType;
        EncodingOptions = encodingOptions;
        _max_buffers = bufferCount;
        _maxLinesPerBuffer = linesPerBuffer;
        _multiFileOptions = multiFileOptions;
        _pluginRegistry = pluginRegistry;
        _disposed = false;

        _bufferPool = new LogBufferPool(_max_buffers * 2);

        BufferIndex = new BufferIndex(_max_buffers, _maxLinesPerBuffer);

        ILogFileInfo fileInfo = null;

        IsMultiFile = multiFile;
        _hasExplicitFileList = fileNames.Length > 1;
        _fileName = fileNames[0];

        IEnumerable<string> names = _hasExplicitFileList ? fileNames : IsMultiFile
            // For multi-file rollover mode: get rollover names.
            ? new RolloverFilenameHandler(GetLogFileInfo(_fileName), _multiFileOptions).GetNameList(_pluginRegistry)
            : [_fileName];

        if (progressReporter != null)
        {
            _progressReporter = progressReporter;
        }
        else
        {
            var reporter = new PeriodicProgressReporter();
            reporter.LoadFile += (_, e) => OnLoadFile(e);
            reporter.LoadingStarted += (_, e) => OnLoadingStarted(e);
            reporter.LoadingFinished += (_, _) => OnLoadingFinished();
            _progressReporter = reporter;
        }

        foreach (var name in names)
        {
            try
            {
                fileInfo = AddFile(name);
            }
            catch (Exception ex) when (_hasExplicitFileList && ex is IOException or UnauthorizedAccessException or LogFileException)
            {
                _logger.Warn(ex, "Skipping unavailable selected file: {0}", name);
            }
        }

        if (IsMultiFile)
        {
            // Use the full name of the last file as _fileName.
            _fileName = fileInfo?.FullName ?? fileNames[^1];
        }

        _watchedILogFileInfo = fileInfo ?? GetLogFileInfo(fileNames[^1]);

        StartGCThread();
    }

    #endregion

    #region Events

    public event EventHandler<LogEventArgs> FileSizeChanged;
    public event EventHandler<LoadFileEventArgs> LoadFile;
    public event EventHandler<LoadFileEventArgs> LoadingStarted;
    public event EventHandler<EventArgs> LoadingFinished;
    public event EventHandler<EventArgs> FileNotFound;
    public event EventHandler<EventArgs> Respawned;

    #endregion

    #region Properties

    /// <summary>For tests and diagnostics.</summary>
    public BufferIndex BufferIndex { get; }

    /// <summary>
    /// Gets the total number of lines contained in all buffers.
    /// </summary>
    /// <remarks>
    /// The value is recalculated on demand if the underlying buffers have changed since the last access. Accessing this
    /// property is thread-safe.
    /// </remarks>
    public int LineCount
    {
        get
        {
            using var _ = BufferIndex.AcquireReadLock();
            return BufferIndex.TotalLineCount;
        }

        private set
        {
            // Only used for resetting to 0. The actual count is computed by BufferIndex.
            if (value == 0)
            {
                BufferIndex.MarkLineCountDirty();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current operation involves multiple files.
    /// </summary>
    public bool IsMultiFile { get; }

    /// <summary>
    /// Gets the character encoding currently used for reading or writing operations.
    /// </summary>
    public Encoding CurrentEncoding { get; private set; }

    /// <summary>
    /// Gets the size of the file, in bytes.
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether XML mode is enabled.
    /// </summary>
    public bool IsXmlMode { get; set; }

    /// <summary>
    /// Gets or sets the XML log configuration used to control logging behavior and settings.
    /// </summary>
    public IXmlLogConfiguration XmlLogConfig { get; set; }

    /// <summary>
    /// Gets or sets the columnizer used to preprocess data before further processing.
    /// </summary>
    public IPreProcessColumnizerMemory PreProcessColumnizer { get; set; }

    /// <summary>
    /// Gets or sets the encoding options used for text processing operations.
    /// </summary>
    private EncodingOptions EncodingOptions
    {
        get;
        set
        {
            {
                field = new EncodingOptions
                {
                    DefaultEncoding = value.DefaultEncoding,
                    Encoding = value.Encoding
                };
            }
        }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Reads all log files and refreshes the internal buffer and related state to reflect the current contents of the
    /// files. Public for unit test reasons
    /// </summary>
    /// <remarks>
    /// This method resets file size and line count tracking, clears any cached data, and repopulates the buffer with
    /// the latest data from the log files. If an I/O error occurs while reading the files, the internal state is
    /// updated to indicate that the files are unavailable. After reading, a file size changed event is raised to notify
    /// listeners of the update.
    /// </remarks>
    //TODO: Make this private
    public void ReadFiles ()
    {
        FileSize = 0;

        _isDeleted = false;

        try
        {
            using var _ = BufferIndex.AcquireWriteLock();
            BufferIndex.ClearLru(_bufferPool);

            ILogFileInfo lastReadableFile = null;
            foreach (var info in _logFileInfoList)
            {
                if (ReadToBufferList(info, 0, LineCount))
                {
                    lastReadableFile = info;
                }
            }

            if (_logFileInfoList.Count > 0)
            {
                var info = _hasExplicitFileList ? lastReadableFile : _logFileInfoList[^1];
                if (info != null)
                {
                    _fileLength = info.Length;
                    _watchedILogFileInfo = info;
                }
            }
        }
        catch (IOException e)
        {
            _logger.Warn(e, "IOException");
            _fileLength = 0;
            _isDeleted = true;
            LineCount = 0;
        }

        LogEventArgs args = new()
        {
            PrevFileSize = 0,
            PrevLineCount = 0,
            LineCount = LineCount,
            FileSize = FileSize
        };

        OnFileSizeChanged(args);
    }

    /// <summary>
    /// Synchronizes the internal buffer state with the current set of log files, updating or removing buffers as
    /// necessary to reflect file changes. Public for unit tests.
    /// </summary>
    /// <remarks>
    /// Call this method after external changes to the underlying log files, such as file rotation or deletion, to
    /// ensure the buffer accurately represents the current log file set. This method may remove, update, or re-read
    /// buffers to match the current files. Thread safety is ensured during the operation.
    /// </remarks>
    /// <returns>
    /// The total number of lines removed from the buffer as a result of deleted or replaced log files. Returns 0 if no
    /// lines were removed.
    /// </returns>
    //TODO: Make this private
    public int ShiftBuffers ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "ShiftBuffers() begin for {0}{1}", _fileName, IsMultiFile ? " (MultiFile)" : "");

        using var writeLock = BufferIndex.AcquireWriteLock();
        {
            BufferIndex.ResetThreadLocalCache();

            var offset = 0;
            BufferIndex.MarkLineCountDirty();

            lock (_monitor)
            {
                RolloverFilenameHandler rolloverHandler = new(_watchedILogFileInfo, _multiFileOptions);
                var fileNameList = rolloverHandler.GetNameList(_pluginRegistry);

                FileSize = 0;
                LineCount = 0;

                IList<ILogFileInfo> lostILogFileInfoList = [];
                IList<ILogFileInfo> readNewILogFileInfoList = [];
                IList<ILogFileInfo> newFileInfoList = [];

                var enumerator = _logFileInfoList.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    var logFileInfo = enumerator.Current;
                    var fileName = logFileInfo.FullName;
                    _logger.Debug(CultureInfo.InvariantCulture, "Testing file {0}", fileName);

                    var node = fileNameList.Find(fileName);

                    if (node == null)
                    {
                        _logger.Warn(CultureInfo.InvariantCulture, "File {0} not found", fileName);
                        continue;
                    }

                    if (node.Previous != null)
                    {
                        fileName = node.Previous.Value;
                        var newILogFileInfo = GetLogFileInfo(fileName);
                        _logger.Debug(CultureInfo.InvariantCulture, "{0} exists\r\nOld size={1}, new size={2}", fileName, logFileInfo.OriginalLength, newILogFileInfo.Length);
                        // is the new file the same as the old buffer info?
                        if (newILogFileInfo.Length == logFileInfo.OriginalLength)
                        {
                            ReplaceBufferInfos(logFileInfo, newILogFileInfo);
                            newFileInfoList.Add(newILogFileInfo);
                        }
                        else
                        {
                            _logger.Debug(CultureInfo.InvariantCulture, "Buffer for {0} must be re-read.", fileName);
                            // not the same. so must read the rest of the list anew from the files
                            readNewILogFileInfoList.Add(newILogFileInfo);
                            while (enumerator.MoveNext())
                            {
                                fileName = enumerator.Current.FullName;
                                node = fileNameList.Find(fileName);
                                if (node == null)
                                {
                                    _logger.Warn(CultureInfo.InvariantCulture, "File {0} not found", fileName);
                                    continue;
                                }

                                if (node.Previous != null)
                                {
                                    fileName = node.Previous.Value;
                                    _logger.Debug(CultureInfo.InvariantCulture, "New name is {0}", fileName);
                                    readNewILogFileInfoList.Add(GetLogFileInfo(fileName));
                                }
                                else
                                {
                                    _logger.Warn(CultureInfo.InvariantCulture, "No previous file for {0} found", fileName);
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.Info(CultureInfo.InvariantCulture, "{0} does not exist", fileName);
                        lostILogFileInfoList.Add(logFileInfo);
                    }
                }

                if (lostILogFileInfoList.Count > 0)
                {
                    _logger.Info(CultureInfo.InvariantCulture, "Deleting buffers for lost files");

                    foreach (var logFileInfo in lostILogFileInfoList)
                    {
                        var lastDeletedBufferInfo = DeleteBuffersForInfo(logFileInfo, false);
                        if (lastDeletedBufferInfo != null)
                        {
                            offset += lastDeletedBufferInfo.Value.StartLine + lastDeletedBufferInfo.Value.LineCount;
                        }
                    }

                    _logger.Info(CultureInfo.InvariantCulture, "Adjusting StartLine values in {0} buffers by offset {1}", BufferIndex.BufferCount, offset);
                    foreach (var buffer in BufferIndex.EnumerateBuffers())
                    {
                        BufferIndex.UpdateStartLine(buffer, buffer.StartLine - offset);
                    }

#if DEBUG
                    if (BufferIndex.BufferCount > 0)
                    {
                        _logger.Debug(CultureInfo.InvariantCulture, "First buffer now has StartLine {0}", BufferIndex.GetBufferAt(0).StartLine);
                    }
#endif
                }

                // Read anew all buffers following a buffer info that couldn't be matched with the corresponding existing file
                _logger.Info(CultureInfo.InvariantCulture, "Deleting buffers for files that must be re-read");

                foreach (var iLogFileInfo in readNewILogFileInfoList)
                {
                    DeleteBuffersForInfo(iLogFileInfo, true);
                }

                _logger.Info(CultureInfo.InvariantCulture, "Deleting buffers for the watched file");

                DeleteBuffersForInfo(_watchedILogFileInfo, true);

                _logger.Info(CultureInfo.InvariantCulture, "Re-Reading files");

                foreach (var iLogFileInfo in readNewILogFileInfoList)
                {
                    ReadToBufferList(iLogFileInfo, 0, LineCount);
                    newFileInfoList.Add(iLogFileInfo);
                }

                _logFileInfoList = newFileInfoList;
                _watchedILogFileInfo = GetLogFileInfo(_watchedILogFileInfo.FullName);
                _logFileInfoList.Add(_watchedILogFileInfo);
                _logger.Info(CultureInfo.InvariantCulture, "Reading watched file");

                ReadToBufferList(_watchedILogFileInfo, 0, LineCount);
            }

            _logger.Info(CultureInfo.InvariantCulture, "ShiftBuffers() end. offset={0}", offset);

            return offset;
        }
    }

    //TODO Make Task Based
    public ILogLineMemory GetLogLineMemory (int lineNum)
    {
        return GetLogLineMemoryInternal(lineNum).Result;
    }

    /// <summary>
    /// Get the text content of the given line number. The actual work is done in an async thread. This method waits for
    /// thread completion for only 1 second. If the async thread has not returned, the method will return <code>
    /// null</code>. This is because this method is also called from GUI thread (e.g. LogWindow draw events). Under some
    /// circumstances, repeated calls to this method would lead the GUI to freeze. E.g. when trying to re-load content
    /// from disk but the file was deleted. Especially on network shares.
    /// </summary>
    /// <remarks>
    /// Once the method detects a timeout it will enter a kind of 'fast fail mode'. That means all following calls will
    /// be returned with <code> null</code> immediately (without 1 second wait). A background call to
    /// GetLogLineInternal() will check if a result is available. If so, the 'fast fail mode' is switched off. In most
    /// cases a fail is caused by a deleted file. But it may also be caused by slow network connections. So all this
    /// effort is needed to prevent entering an endless 'fast fail mode' just because of temporary problems.
    /// </remarks>
    /// <param name="lineNum">line to retrieve</param>
    /// <returns></returns>
    public async Task<ILogLineMemory> GetLogLineMemoryWithWait (int lineNum)
    {
        ILogLineMemory result = null;

        if (!_isFastFailOnGetLogLine)
        {
            // Fast path: if the buffer is in memory, skip the thread-pool hop entirely
            bool canFastPath = false;

            using (BufferIndex.AcquireReadLock())
            {
                var logBufferEntry = BufferIndex.GetBufferForLineWithIndex(lineNum);
                canFastPath = logBufferEntry.Buffer is { IsDisposed: false };
            }

            if (canFastPath)
            {
                result = GetLogLineMemoryInternal(lineNum).Result;
                _isFastFailOnGetLogLine = false;
            }
            else
            {
                // Slow path: buffer disposed or not found — use Task.Run with timeout
                var task = Task.Run(() => GetLogLineMemoryInternal(lineNum).AsTask());
                if (task.Wait(WAIT_TIME))
                {
                    result = await task.ConfigureAwait(false);
                    _isFastFailOnGetLogLine = false;
                }
                else
                {
                    _isFastFailOnGetLogLine = true;
                    _logger.Info(CultureInfo.InvariantCulture, "Entering fast-fail mode for line {0}. No result after {1}ms.", lineNum, WAIT_TIME);
                }
            }
        }
        else
        {
            _logger.Info(CultureInfo.InvariantCulture, "Fast-fail returning null for line {0}", lineNum);
            if (!_isFailModeCheckCallPending)
            {
                _isFailModeCheckCallPending = true;
                var logLine = await GetLogLineMemoryInternal(lineNum).ConfigureAwait(true);
                GetLineMemoryFinishedCallback(logLine);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the file name of the actual file for the given line. Needed for MultiFile.
    /// </summary>
    /// <param name="lineNum"></param>
    /// <returns></returns>
    public string GetLogFileNameForLine (int lineNum)
    {
        using var _ = BufferIndex.AcquireReadLock();
        var logBufferEntry = BufferIndex.TryFindBuffer(lineNum);
        return logBufferEntry.Buffer?.FileInfo.FullName;
    }

    /// <summary>
    /// Returns the ILogFileInfo for the actual file for the given line. Needed for MultiFile.
    /// </summary>
    /// <param name="lineNum"></param>
    /// <returns></returns>
    public ILogFileInfo GetLogFileInfoForLine (int lineNum)
    {
        using var _ = BufferIndex.AcquireReadLock();
        var logBufferEntry = BufferIndex.TryFindBuffer(lineNum);
        return logBufferEntry.Buffer?.FileInfo;
    }

    /// <summary>
    /// Returns the line number (starting from the given number) where the next multi file starts.
    /// </summary>
    /// <param name="lineNum"></param>
    /// <returns></returns>
    public int GetNextMultiFileLine (int lineNum)
    {
        using var _ = BufferIndex.AcquireReadLock();
        var (found, startLine) = BufferIndex.TryGetNextFileStartLine(lineNum);
        return found ? startLine : -1;
    }

    /// <summary>
    /// Finds the starting line number of the previous file segment before the specified line number across multiple
    /// files.
    /// </summary>
    /// <remarks>
    /// This method is useful when navigating through a collection of files represented as contiguous line segments. If
    /// the specified line number is within the first file segment, the method returns -1 to indicate that there is no
    /// previous file segment.
    /// </remarks>
    /// <param name="lineNum">
    /// The line number for which to locate the previous file segment. Must be a valid line number within the buffer.
    /// </param>
    /// <returns>The starting line number of the previous file segment if one exists; otherwise, -1.</returns>
    public int GetPrevMultiFileLine (int lineNum)
    {
        using var _ = BufferIndex.AcquireReadLock();
        var (found, startLine) = BufferIndex.TryGetPrevFileStartLine(lineNum);
        return found ? startLine : -1;
    }

    /// <summary>
    /// Returns the actual line number in the file for the given 'virtual line num'. This is needed for multi file mode.
    /// 'Virtual' means that the given line num is a line number in the collections of the files currently viewed
    /// together in multi file mode as one large virtual file. This method finds the real file for the line number and
    /// maps the line number to the correct position in that file. This is needed when launching external tools to
    /// provide correct line number arguments.
    /// </summary>
    /// <param name="lineNum"></param>
    /// <returns></returns>
    public int GetRealLineNumForVirtualLineNum (int lineNum)
    {
        using var _ = BufferIndex.AcquireReadLock();
        var logBufferEntry = BufferIndex.GetBufferForLineWithIndex(lineNum);
        if (!logBufferEntry.Found)
        {
            return logBufferEntry.Index;
        }

        var buffer = BufferIndex.GetFirstBufferForFile(logBufferEntry.Buffer, logBufferEntry.Index);
        return buffer != null ? lineNum - buffer.StartLine : -1;
    }

    /// <summary>
    /// Begins monitoring by starting the background monitoring process.
    /// </summary>
    /// <remarks>
    /// This method initiates monitoring if it is not already running. To stop monitoring, call the corresponding stop
    /// method if available. This method is not thread-safe; ensure that it is not called concurrently with other
    /// monitoring control methods.
    /// </remarks>
    public void StartMonitoring ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "startMonitoring()");
        _monitorTask = Task.Run(MonitorThreadProc, _cts.Token);
        _shouldStop = false;
    }

    /// <summary>
    /// Stops monitoring the log file and terminates any background monitoring or cleanup tasks.
    /// </summary>
    /// <remarks>
    /// Call this method to halt all ongoing monitoring activity and release associated resources. After calling this
    /// method, monitoring cannot be resumed without restarting the monitoring process.
    /// </remarks>
    public void StopMonitoring ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "stopMonitoring()");
        _shouldStop = true;
        _cts.Cancel();

        try
        {
            var timeout = TimeSpan.FromSeconds(5);
            _ = _monitorTask?.Wait(timeout);
            _ = _garbageCollectorTask?.Wait(timeout);
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
        {
            //Expected exceptions due to task cancellation, can be safely ignored.
        }
        catch (AggregateException ex)
        {
            _logger.Warn(ex, "Exception while waiting for monitor or GC task to complete");
        }

        CloseFiles();
    }

    /// <summary>
    /// calls stopMonitoring() in a background thread and returns to the caller immediately. This is useful for a fast
    /// responding GUI (e.g. when closing a file tab)
    /// </summary>
    public void StopMonitoringAsync ()
    {
        var task = Task.Run(StopMonitoring);
    }

    /// <summary>
    /// Deletes all buffer lines and disposes their content. Use only when the LogfileReader is about to be closed!
    /// </summary>
    public void DeleteAllContent ()
    {
        if (_contentDeleted)
        {
            _logger.Debug(CultureInfo.InvariantCulture, "Buffers for {0} already deleted.", Util.GetNameFromPath(_fileName));
            return;
        }

        _logger.Info(CultureInfo.InvariantCulture, "Deleting all log buffers for {0}. Used mem: {1:N0}", Util.GetNameFromPath(_fileName), GC.GetTotalMemory(false));

        using var _ = BufferIndex.AcquireWriteLock();
        BufferIndex.ResetThreadLocalCache();

        foreach (var logBuffer in BufferIndex.EnumerateBuffers())
        {
            if (!logBuffer.IsDisposed)
            {
                logBuffer.DisposeContent();
            }
        }

        BufferIndex.Clear();
        _contentDeleted = true;
        _logger.Info(CultureInfo.InvariantCulture, "Deleting complete. Used mem: {0:N0}", GC.GetTotalMemory(false));
    }

    /// <summary>
    /// Explicit change the encoding.
    /// </summary>
    /// <param name="encoding"></param>
    public void ChangeEncoding (Encoding encoding)
    {
        CurrentEncoding = encoding;
        EncodingOptions.Encoding = encoding;
        FileSize = 0;
        using var _ = BufferIndex.AcquireWriteLock();
        BufferIndex.ClearLru(_bufferPool);
    }

    /// <summary>
    /// For unit tests only.
    /// </summary>
    /// <returns></returns>
    public IList<ILogFileInfo> GetLogFileInfoList ()
    {
        return _logFileInfoList;
    }

    #endregion

    #region Internals

#if DEBUG

    /// <summary>
    /// Logs detailed buffer information for the specified line number to the debug output.
    /// </summary>
    /// <remarks>
    /// This method is intended for debugging purposes and is only available in debug builds. It logs buffer details and
    /// file position information to assist with diagnostics.
    /// </remarks>
    /// <param name="lineNum">The zero-based line number for which buffer information is logged.</param>
    public void LogBufferInfoForLine (int lineNum)
    {
        using var readLock = BufferIndex.AcquireReadLock();
        {
            var logBufferEntry = BufferIndex.GetBufferForLineWithIndex(lineNum);
            if (!logBufferEntry.Found)
            {
                _logger.Error("Cannot find buffer for line {0}, file: {1}{2}", lineNum, _fileName, IsMultiFile ? " (MultiFile)" : "");
                return;
            }

            _logger.Info(CultureInfo.InvariantCulture, "-----------------------------------");
            _logger.Info(CultureInfo.InvariantCulture, "Buffer info for line {0}", lineNum);
            DumpBufferInfos(logBufferEntry.Buffer);
            _logger.Info(CultureInfo.InvariantCulture, "File pos for current line: {0}", logBufferEntry.Buffer.GetFilePosForLineOfBlock(lineNum - logBufferEntry.Buffer.StartLine));
            _logger.Info(CultureInfo.InvariantCulture, "-----------------------------------");
        }
    }

    /// <summary>
    /// Logs diagnostic information about the current state of the buffer and LRU cache for debugging purposes.
    /// </summary>
    /// <remarks>
    /// This method is intended for use in debug builds to assist with troubleshooting and analyzing buffer management.
    /// It outputs details such as the number of LRU cache entries, buffer counts, and dispose statistics to the logger.
    /// This method does not modify the state of the buffers or cache.
    /// </remarks>
    public void LogBufferDiagnostic ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "-------- Buffer diagnostics -------");
        var cacheCount = BufferIndex.LruCacheCount;
        _logger.Info(CultureInfo.InvariantCulture, "LRU entries: {0}", cacheCount);

        using var readLock = BufferIndex.AcquireReadLock();
        {
            _logger.Info(CultureInfo.InvariantCulture, "File: {0}\r\nBuffer count: {1}\r\nDisposed buffers: {2}", _fileName, BufferIndex.BufferCount, BufferIndex.BufferCount - cacheCount);
            var lineNum = 0;
            long disposeSum = 0;
            long maxDispose = 0;
            long minDispose = int.MaxValue;

            for (var i = 0; i < BufferIndex.BufferCount; ++i)
            {
                var buffer = BufferIndex.GetBufferAt(i);
                if (buffer.StartLine != lineNum)
                {
                    _logger.Error("Start line of buffer is: {0}, expected: {1}", buffer.StartLine, lineNum);
                    _logger.Info(CultureInfo.InvariantCulture, "Info of buffer follows:");
                    DumpBufferInfos(buffer);
                }

                lineNum += buffer.LineCount;
                disposeSum += buffer.DisposeCount;
                maxDispose = Math.Max(maxDispose, buffer.DisposeCount);
                minDispose = Math.Min(minDispose, buffer.DisposeCount);
            }

            _logger.Info(CultureInfo.InvariantCulture, "Dispose count sum is: {0}\r\nMin dispose count is: {1}\r\nMax dispose count is: {2}\r\n-----------------------------------", disposeSum, minDispose, maxDispose);
        }
    }

#endif

    #endregion

    #region Private Methods

    /// <summary>
    /// Adds a log file to the collection and returns information about the added file.
    /// </summary>
    /// <param name="fileName">The path of the log file to add. Cannot be null or empty.</param>
    /// <returns>An object that provides information about the added log file.</returns>
    private ILogFileInfo AddFile (string fileName)
    {
        _logger.Info(CultureInfo.InvariantCulture, "Adding file to ILogFileInfoList: " + fileName);
        var info = GetLogFileInfo(fileName);
        _logFileInfoList.Add(info);
        return info;
    }

    /// <summary>
    /// Retrieves a contiguous range of log lines starting at the specified line number. Acquires locks once for the
    /// entire batch, amortising synchronisation overhead.
    /// </summary>
    /// <param name="startLine">The zero-based line number of the first line to retrieve.</param>
    /// <param name="count">The number of lines to retrieve. May be clamped to available lines.</param>
    /// <returns>
    /// An array of <see cref="ILogLineMemory"/> instances. The array length may be less than <paramref name="count"/>
    /// if the end of file is reached. Entries may be null if a buffer is unavailable.
    /// </returns>
    public ILogLineMemory[] GetLogLineMemories (int startLine, int count)
    {
        if (_isDeleted || count <= 0)
        {
            return [];
        }

        var result = new ILogLineMemory[count];
        var filled = 0;

        using var readLock = BufferIndex.AcquireReadLock();
        {
            var lineNum = startLine;
            while (filled < count)
            {
                var logBufferEntry = BufferIndex.GetBufferForLineWithIndex(lineNum);
                if (!logBufferEntry.Found)
                {
                    break;
                }

                // Protect against concurrent disposal
                var lockTaken = false;
                try
                {
                    logBufferEntry.Buffer.AcquireContentLock(ref lockTaken);

                    if (logBufferEntry.Buffer.IsDisposed)
                    {
                        lock (logBufferEntry.Buffer.FileInfo)
                        {
                            ReReadBuffer(logBufferEntry.Buffer);
                        }
                    }

                    // Copy lines from this buffer
                    var bufferOffset = lineNum - logBufferEntry.Buffer.StartLine;
                    var availableInBuffer = logBufferEntry.Buffer.LineCount - bufferOffset;
                    var toCopy = Math.Min(count - filled, availableInBuffer);

                    if (bufferOffset < 0 || toCopy <= 0)
                    {
                        break;
                    }

                    for (var i = 0; i < toCopy; i++)
                    {
                        result[filled + i] = logBufferEntry.Buffer.GetLineMemoryOfBlock(bufferOffset + i);
                    }

                    filled += toCopy;
                    lineNum += toCopy;
                }
                finally
                {
                    if (lockTaken)
                    {
                        logBufferEntry.Buffer.ReleaseContentLock();
                    }
                }
            }
        }

        // Trim if we got fewer lines than requested
        if (filled < count)
        {
            Array.Resize(ref result, filled);
        }

        return result;
    }

    /// <summary>
    /// Retrieves the log line at the specified line number, or returns null if the file has been deleted or the line
    /// cannot be found.
    /// </summary>
    /// <param name="lineNum">The zero-based line number of the log entry to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the log line at the specified line
    /// number, or null if the file is deleted or the line does not exist.
    /// </returns>
    private ValueTask<ILogLineMemory> GetLogLineMemoryInternal (int lineNum)
    {
        if (_isDeleted)
        {
            _logger.Debug(CultureInfo.InvariantCulture, "Returning null for line {0} because file is deleted.", lineNum);
            // fast fail if dead file was detected. Prevents repeated lags in GUI thread caused by callbacks from control (e.g. repaint)
            return default;
        }

        using var readLock = BufferIndex.AcquireReadLock();
        {
            var logBufferEntry = BufferIndex.GetBufferForLineWithIndex(lineNum);
            if (!logBufferEntry.Found)
            {
                _logger.Error("Cannot find buffer for line {0}, file: {1}{2}", lineNum, _fileName, IsMultiFile ? " (MultiFile)" : "");
                return default;
            }

            var lockTaken = false;
            try
            {
                logBufferEntry.Buffer.AcquireContentLock(ref lockTaken);

                if (logBufferEntry.Buffer.IsDisposed)
                {
                    lock (logBufferEntry.Buffer.FileInfo)
                    {
                        ReReadBuffer(logBufferEntry.Buffer);
                    }
                }

                var line = logBufferEntry.Buffer.GetLineMemoryOfBlock(lineNum - logBufferEntry.Buffer.StartLine);
                return line.HasValue
                ? new ValueTask<ILogLineMemory>(line.Value)
                : default;
            }
            finally
            {
                if (lockTaken)
                {
                    logBufferEntry.Buffer.ReleaseContentLock();
                }
            }
        }
    }

    /// <summary>
    /// Starts the background task responsible for performing garbage collection operations.
    /// </summary>
    /// <remarks>
    /// This method initiates the garbage collection process on a separate thread or task. It is intended for internal
    /// use to manage resource cleanup asynchronously. Calling this method multiple times without proper synchronization
    /// may result in multiple concurrent garbage collection tasks.
    /// </remarks>
    private void StartGCThread ()
    {
        _garbageCollectorTask = Task.Run(GarbageCollectorThreadProc, _cts.Token);
    }

    /// <summary>
    /// Releases resources associated with open log files and resets related state information.
    /// </summary>
    private void CloseFiles ()
    {
        FileSize = 0;
        LineCount = 0;
    }

    /// <summary>
    /// Retrieves information about a log file specified by its file name or URI.
    /// </summary>
    /// <param name="fileNameOrUri">
    /// The file name or URI identifying the log file for which to retrieve information. Cannot be null or empty.
    /// </param>
    /// <returns>An object containing information about the specified log file.</returns>
    /// <exception cref="LogFileException">
    /// Thrown if no file system plugin is found for the specified file name or URI, or if the log file cannot be found.
    /// </exception>
    private ILogFileInfo GetLogFileInfo (string fileNameOrUri) //TODO: I changed to static
    {
        //TODO this must be fixed and should be given to the logfilereader not just called (https://github.com/LogExperts/LogExpert/issues/402)
        var fs = _pluginRegistry.FindFileSystemForUri(fileNameOrUri) ?? throw new LogFileException("No file system plugin found for " + fileNameOrUri);
        var logFileInfo = fs.GetLogfileInfo(fileNameOrUri);
        return logFileInfo ?? throw new LogFileException("Cannot find " + fileNameOrUri);
    }

    /// <summary>
    /// Replaces references to an existing log file information object with a new one in all managed buffers.
    /// </summary>
    /// <remarks>
    /// This method updates all buffer entries that reference the specified old log file information object, assigning
    /// them the new log file information object instead. Use this method when a log file has been renamed or its
    /// metadata has changed, and all associated buffers need to reference the updated information.
    /// </remarks>
    /// <param name="oldLogFileInfo">The log file information object to be replaced. Cannot be null.</param>
    /// <param name="newLogFileInfo">
    /// The new log file information object to use as a replacement. Cannot be null.
    /// </param>
    private void ReplaceBufferInfos (ILogFileInfo oldLogFileInfo, ILogFileInfo newLogFileInfo)
    {
        _logger.Debug(CultureInfo.InvariantCulture, "ReplaceBufferInfos() " + oldLogFileInfo.FullName + " -> " + newLogFileInfo.FullName);
        foreach (var buffer in BufferIndex.EnumerateBuffers())
        {
            if (buffer.FileInfo == oldLogFileInfo)
            {
                _logger.Debug($"Buffer with startLine={buffer.StartLine}, lineCount={buffer.LineCount}, filePos={buffer.StartPos}, size={buffer.Size} gets new filename {newLogFileInfo.FullName}");
                buffer.FileInfo = newLogFileInfo;
            }
        }
    }

    /// <summary>
    /// Deletes all log buffers associated with the specified log file information and returns the last buffer that was
    /// removed.
    /// </summary>
    /// <remarks>
    /// If multiple buffers match the specified criteria, all are removed and the last one found is returned. If no
    /// buffers match, the method returns null.
    /// </remarks>
    /// <param name="iLogFileInfo">
    /// The log file information used to identify which buffers to delete. Cannot be null.
    /// </param>
    /// <param name="matchNamesOnly">
    /// true to match buffers by file name only; false to require an exact object match for the log file information.
    /// </param>
    /// <returns>The StartLine and LineCount of the Logbuffer that was removed or null</returns>
    private (int StartLine, int LineCount)? DeleteBuffersForInfo (ILogFileInfo iLogFileInfo, bool matchNamesOnly)
    {
        _logger.Info($"Deleting buffers for file {iLogFileInfo.FullName}");
        BufferIndex.ResetThreadLocalCache();
        (int StartLine, int LineCount)? lastRemovedInfo = null;
        IList<LogBuffer> deleteList = [];

        if (matchNamesOnly)
        {
            foreach (var buffer in BufferIndex.EnumerateBuffers())
            {
                if (buffer.FileInfo.FullName.Equals(iLogFileInfo.FullName, StringComparison.Ordinal))
                {
                    lastRemovedInfo = (buffer.StartLine, buffer.LineCount);
                    deleteList.Add(buffer);
                }
            }
        }
        else
        {
            foreach (var buffer in BufferIndex.EnumerateBuffers())
            {
                if (buffer.FileInfo == iLogFileInfo)
                {
                    lastRemovedInfo = (buffer.StartLine, buffer.LineCount);
                    deleteList.Add(buffer);
                }
            }
        }

        foreach (var buffer in deleteList)
        {
            _ = BufferIndex.Remove(buffer);

            var lockTaken = false;
            try
            {
                buffer.AcquireContentLock(ref lockTaken);
                _bufferPool.Return(buffer);
            }
            finally
            {
                if (lockTaken)
                {
                    buffer.ReleaseContentLock();
                }
            }
        }

        if (lastRemovedInfo == null)
        {
            _logger.Info(CultureInfo.InvariantCulture, "lastRemovedBuffer is null");
        }
        else
        {
            _logger.Info(CultureInfo.InvariantCulture, $"lastRemovedBuffer: startLine={lastRemovedInfo.Value.StartLine}, lineCount={lastRemovedInfo.Value.LineCount}");
        }

        return lastRemovedInfo;
    }

    /// <summary>
    /// Reads log lines from the specified log file starting at the given file position and line number, and populates
    /// the internal buffer list with the read data.
    /// </summary>
    /// <remarks>
    /// If the buffer list is empty or the log file changes, a new buffer is created. The method updates internal state
    /// such as file size, encoding, and line count, and may trigger events to notify about file loading progress or
    /// file not found conditions. This method is not thread-safe and should be called with appropriate synchronization
    /// if accessed concurrently.
    /// </remarks>
    /// <param name="logFileInfo">The log file information used to open and read the file. Must not be null.</param>
    /// <param name="filePos">The byte position in the file at which to begin reading.</param>
    /// <param name="startLine">
    /// The line number corresponding to the starting position in the file. Used to assign line numbers to buffered log
    /// lines.
    /// </param>
    private bool ReadToBufferList (ILogFileInfo logFileInfo, long filePos, int startLine)
    {
        try
        {
            using var fileStream = logFileInfo.OpenStream();
            using var reader = GetLogStreamReader(fileStream, EncodingOptions) as ILogStreamReaderMemory;

            reader.Position = filePos;
            _fileLength = logFileInfo.Length;

            var lineNum = startLine;
            LogBuffer logBuffer;

            using var upgradeabelLock = BufferIndex.AcquireUpgradeableReadLock();
            {
                if (BufferIndex.BufferCount == 0)
                {
                    logBuffer = _bufferPool.Rent(logFileInfo, _maxLinesPerBuffer);
                    logBuffer.StartLine = startLine;
                    logBuffer.StartPos = filePos;

                    using (upgradeabelLock.UpgradeToWrite())
                    {
                        BufferIndex.Add(logBuffer);
                    }
                }
                else
                {
                    logBuffer = BufferIndex.GetLastBuffer();

                    if (!logBuffer.FileInfo.FullName.Equals(logFileInfo.FullName, StringComparison.Ordinal))
                    {
                        logBuffer = _bufferPool.Rent(logFileInfo, _maxLinesPerBuffer);
                        logBuffer.StartLine = startLine;
                        logBuffer.StartPos = filePos;

                        using (upgradeabelLock.UpgradeToWrite())
                        {
                            BufferIndex.Add(logBuffer);
                        }
                    }

                    var lockTaken = false;

                    try
                    {
                        logBuffer.AcquireContentLock(ref lockTaken);
                        if (logBuffer.IsDisposed)
                        {
                            ReReadBuffer(logBuffer);
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            logBuffer.ReleaseContentLock();
                        }
                    }
                }
            }

            Monitor.Enter(logBuffer);
            try
            {
                var lineCount = logBuffer.LineCount;
                var droppedLines = logBuffer.PrevBuffersDroppedLinesSum;
                filePos = reader.Position;

                var (success, lineMemory, wasDropped) = ReadLineMemory(reader, logBuffer.StartLine + logBuffer.LineCount, logBuffer.StartLine + logBuffer.LineCount + droppedLines);

                while (success)
                {
                    if (_shouldStop)
                    {
                        return false;
                    }

                    if (wasDropped)
                    {
                        logBuffer.DroppedLinesCount += 1;
                        droppedLines++;
                        (success, lineMemory, wasDropped) = ReadLineMemory(reader, logBuffer.StartLine + logBuffer.LineCount, logBuffer.StartLine + logBuffer.LineCount + droppedLines);
                        continue;
                    }

                    lineCount++;

                    // Add the line to the CURRENT buffer BEFORE any buffer rotation.
                    // The lineMemory slice is backed by the allocator's current char[] block,
                    // so it must be added to the buffer that will own those blocks after DetachBlocks().
                    var logLine = new LogLine(lineMemory, logBuffer.StartLine + logBuffer.LineCount);
                    logBuffer.AddLine(logLine, filePos);
                    filePos = reader.Position;
                    lineNum++;

                    if (lineCount > _maxLinesPerBuffer && reader.IsBufferComplete)
                    {
                        _progressReporter.ReportProgress(logFileInfo.FullName, filePos, logFileInfo.Length);

                        logBuffer.Size = filePos - logBuffer.StartPos;

                        // Detach char blocks from the reader and attach to the completed buffer.
                        // Must happen before Monitor.Exit so the buffer is still exclusively owned.
                        logBuffer.AttachCharBlocks(reader.DetachCharBlocks());

                        Monitor.Exit(logBuffer);
                        try
                        {
                            var newBuffer = _bufferPool.Rent(logFileInfo, _maxLinesPerBuffer);
                            newBuffer.StartLine = lineNum;
                            newBuffer.StartPos = filePos;
                            newBuffer.PrevBuffersDroppedLinesSum = droppedLines;

                            using (upgradeabelLock.UpgradeToWrite())
                            {
                                BufferIndex.Add(newBuffer);
                            }

                            logBuffer = newBuffer;
                            Monitor.Enter(logBuffer);
                            lineCount = 1;
                        }
                        catch (Exception)
                        {
                            Monitor.Enter(logBuffer);
                            throw;
                        }
                    }

                    (success, lineMemory, wasDropped) = ReadLineMemory(reader, logBuffer.StartLine + logBuffer.LineCount, logBuffer.StartLine + logBuffer.LineCount + droppedLines);
                }

                logBuffer.Size = filePos - logBuffer.StartPos;

                // Attach remaining blocks to the final buffer
                logBuffer.AttachCharBlocks(reader.DetachCharBlocks());
            }
            finally
            {
                Monitor.Exit(logBuffer);
            }

            BufferIndex.MarkLineCountDirty();
            FileSize = reader.Position;

            // Reader may have detected another encoding
            CurrentEncoding = reader.Encoding;

            if (!_shouldStop)
            {
                _progressReporter.ReportComplete(logFileInfo.FullName, filePos, _fileLength);
            }
            return true;
        }
        catch (Exception ex) when (_hasExplicitFileList && ex is IOException or UnauthorizedAccessException)
        {
            // A selected batch may change while the preview is open. Keep the other files readable.
            _logger.Warn(ex, "Skipping unavailable selected file: {0}", logFileInfo.FullName);
            return false;
        }
        catch (IOException ioex)
        {
            _logger.Warn(ioex, "IOException: ");
            _isDeleted = true;
            LineCount = 0;
            FileSize = 0;
            OnFileNotFound(); // notify LogWindow
            return false;
        }
    }

    /// <summary>
    /// Executes the background thread procedure responsible for periodically triggering garbage collection of the least
    /// recently used (LRU) cache while the thread is active.
    /// </summary>
    /// <remarks>
    /// This method is intended to run on a dedicated background thread. It repeatedly waits for a fixed interval and
    /// then invokes cache cleanup, continuing until a stop signal is received. Exceptions during the sleep interval are
    /// caught and ignored to ensure the thread remains active.
    /// </remarks>
    private async Task GarbageCollectorThreadProc ()
    {
        while (!_shouldStop)
        {
            try
            {
                await Task.Delay(10000, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            BufferIndex.EvictLeastRecentlyUsed();
        }
    }

    /// <summary>
    /// Re-reads the contents of the specified log buffer from its associated file, updating its lines and dropped line
    /// count as necessary.
    /// </summary>
    /// <remarks>
    /// This method acquires a lock on the provided log buffer during the operation to ensure thread safety. If an I/O
    /// error occurs while accessing the file, the method logs a warning and returns without updating the buffer.
    /// </remarks>
    /// <param name="logBuffer">
    /// The log buffer to refresh with the latest data from its underlying file. Cannot be null.
    /// </param>
    private void ReReadBuffer (LogBuffer logBuffer)
    {
#if DEBUG
        _logger.Info(CultureInfo.InvariantCulture, "re-reading buffer: {0}/{1}/{2}", logBuffer.StartLine, logBuffer.LineCount, logBuffer.FileInfo.FullName);
#endif
        lock (_logBufferLock)
        {
            Stream openendFileStream;
            try
            {
                openendFileStream = logBuffer.FileInfo.OpenStream();
            }
            catch (IOException e)
            {
                _logger.Warn(e);
                return;
            }

            using Stream fileStream = openendFileStream;
            {
                //TODO LogStream Reader has to be changed to ILogStreamReaderMemory
                var reader = GetLogStreamReader(fileStream, EncodingOptions) as ILogStreamReaderMemory;

                using var readerDisposabel = reader as IDisposable;
                {
                    var filePos = logBuffer.StartPos;
                    reader.Position = logBuffer.StartPos;
                    var maxLinesCount = logBuffer.LineCount;
                    var lineCount = 0;
                    var dropCount = logBuffer.PrevBuffersDroppedLinesSum;
                    logBuffer.ClearLines();

                    var (success, lineMemory, wasDropped) = ReadLineMemory(reader, logBuffer.StartLine + logBuffer.LineCount, logBuffer.StartLine + logBuffer.LineCount + dropCount);

                    while (success)
                    {
                        if (lineCount >= maxLinesCount)
                        {
                            break;
                        }

                        if (wasDropped)
                        {
                            dropCount++;
                            (success, lineMemory, wasDropped) = ReadLineMemory(reader, logBuffer.StartLine + logBuffer.LineCount, logBuffer.StartLine + logBuffer.LineCount + dropCount);
                            continue;
                        }

                        LogLine logLine = new(lineMemory, logBuffer.StartLine + logBuffer.LineCount);

                        logBuffer.AddLine(logLine, filePos);
                        filePos = reader.Position;
                        lineCount++;

                        (success, lineMemory, wasDropped) = ReadLineMemory(reader, logBuffer.StartLine + logBuffer.LineCount, logBuffer.StartLine + logBuffer.LineCount + dropCount);
                    }

                    // Attach char blocks from the reader to the re-read buffer
                    logBuffer.AttachCharBlocks(reader.DetachCharBlocks());

                    if (maxLinesCount != logBuffer.LineCount)
                    {
                        _logger.Warn(CultureInfo.InvariantCulture, "LineCount in buffer differs after re-reading. old={0}, new={1}", maxLinesCount, logBuffer.LineCount);
                    }

                    if (dropCount - logBuffer.PrevBuffersDroppedLinesSum != logBuffer.DroppedLinesCount)
                    {
                        _logger.Warn(CultureInfo.InvariantCulture, "DroppedLinesCount in buffer differs after re-reading. old={0}, new={1}", logBuffer.DroppedLinesCount, dropCount);
                        logBuffer.DroppedLinesCount = dropCount - logBuffer.PrevBuffersDroppedLinesSum;
                    }
                }

                GC.KeepAlive(fileStream);
            }
        }
    }

    private void GetLineMemoryFinishedCallback (ILogLineMemory line)
    {
        _isFailModeCheckCallPending = false;
        if (line != null)
        {
            _logger.Debug(CultureInfo.InvariantCulture, "'isFastFailOnGetLogLine' flag was reset");
            _isFastFailOnGetLogLine = false;
        }

        _logger.Debug(CultureInfo.InvariantCulture, "'isLogLineCallPending' flag was reset.");
    }

    /// <summary>
    /// Monitors the specified log file for changes and processes updates in a background thread.
    /// </summary>
    /// <remarks>
    /// This method is intended to be used as the entry point for a monitoring thread. It periodically checks the
    /// watched log file for changes, handles file not found scenarios, and triggers appropriate events when the file is
    /// updated or deleted. The method runs until a stop signal is received. Exceptions encountered during monitoring
    /// are logged but do not terminate the monitoring loop.
    /// </remarks>
    private async Task MonitorThreadProc ()
    {
        Thread.CurrentThread.Name = "MonitorThread";
        //IFileSystemPlugin fs = PluginRegistry.GetInstance().FindFileSystemForUri(this.watchedILogFileInfo.FullName);
        _logger.Info(CultureInfo.InvariantCulture, "MonitorThreadProc() for file {0}", _watchedILogFileInfo.FullName);

        try
        {
            _progressReporter.ReportLoadingStarted(_fileName);
            ReadFiles();
            if (!_isDeleted)
            {
                _progressReporter.ReportLoadingFinished();
            }
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }

        while (!_shouldStop)
        {
            try
            {
                var pollInterval = _watchedILogFileInfo.PollInterval;
                await Task.Delay(pollInterval, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                _logger.Error(e);
            }

            if (_shouldStop)
            {
                return;
            }

            try
            {
                if (_watchedILogFileInfo.FileHasChanged())
                {
                    _fileLength = _watchedILogFileInfo.Length;
                    if (_fileLength == -1)
                    {
                        MonitoredFileNotFound();
                    }
                    else
                    {
                        FileChanged();
                    }
                }
            }
            catch (FileNotFoundException)
            {
                MonitoredFileNotFound();
            }
        }
    }

    /// <summary>
    /// Handles the scenario when the monitored file is not found and updates the internal state to reflect that the
    /// file has been deleted.
    /// </summary>
    /// <remarks>
    /// This method should be called when a monitored file is determined to be missing, such as after a
    /// FileNotFoundException. It transitions the monitoring logic into a 'deleted' state and notifies any listeners of
    /// the file's absence. Subsequent calls have no effect if the file is already marked as deleted.
    /// </remarks>
    private void MonitoredFileNotFound ()
    {
        long oldSize;
        if (!_isDeleted)
        {
            _logger.Debug(CultureInfo.InvariantCulture, "File not FileNotFoundException catched. Switching to 'deleted' mode.");
            _isDeleted = true;
            oldSize = _fileLength = -1;
            FileSize = 0;
            OnFileNotFound(); // notify LogWindow
        }
#if DEBUG
        else
        {
            _logger.Debug(CultureInfo.InvariantCulture, "File not FileNotFoundException catched. Already in deleted mode.");
        }
#endif
    }

    /// <summary>
    /// Handles updates when the underlying file has changed, such as when it is modified or restored after deletion.
    /// </summary>
    /// <remarks>
    /// This method should be called when the file being monitored is detected to have changed. If the file was
    /// previously deleted and has been restored, the method triggers a respawn event and resets the file size. It also
    /// logs the change and notifies listeners of the update.
    /// </remarks>
    private void FileChanged ()
    {
        if (_isDeleted)
        {
            OnRespawned();
            // prevent size update events. The window should reload the complete file.
            FileSize = _fileLength;
        }

        var newSize = _fileLength;
        //if (this.currFileSize != newSize)
        {
            _logger.Info(CultureInfo.InvariantCulture, "file size changed. new size={0}, file: {1}", newSize, _fileName);
            FireChangeEvent();
        }
    }

    /// <summary>
    /// Raises a change event to notify listeners of updates to the monitored file, such as changes in file size, line
    /// count, or file rollover events.
    /// </summary>
    /// <remarks>
    /// This method should be called whenever the state of the monitored file may have changed, including when the file
    /// is recreated, deleted, or rolled over. It updates relevant event arguments and invokes event handlers as
    /// appropriate. Listeners can use the event data to respond to file changes, such as updating UI elements or
    /// processing new log entries.
    /// </remarks>
    private void FireChangeEvent ()
    {
        LogEventArgs args = new()
        {
            PrevFileSize = FileSize,
            PrevLineCount = LineCount
        };

        var newSize = _fileLength;
        if (newSize < FileSize || _isDeleted)
        {
            _logger.Info(CultureInfo.InvariantCulture, "File was created anew: new size={0}, oldSize={1}", newSize, FileSize);
            // Fire "New File" event
            FileSize = 0;
            LineCount = 0;
            try
            {
                if (IsMultiFile)
                {
                    var offset = ShiftBuffers();
                    args.FileSize = newSize;
                    args.LineCount = LineCount;
                    args.IsRollover = true;
                    args.RolloverOffset = offset;
                    _isDeleted = false;
                    if (!_shouldStop)
                    {
                        OnFileSizeChanged(args);
                    }
                }
                else
                {
                    // Trigger "new file" handling (reload)
                    _progressReporter.ReportNewFile(_fileName, 0, _fileLength);

                    if (_isDeleted)
                    {
                        args.FileSize = newSize;
                        args.LineCount = LineCount;
                        if (args.PrevLineCount != args.LineCount && !_shouldStop)
                        {
                            OnFileSizeChanged(args);
                        }
                    }

                    _isDeleted = false;
                }
            }
            catch (FileNotFoundException e)
            {
                // trying anew in next poll intervall. So let currFileSize untouched.
                _logger.Warn(e);
            }
        }
        else
        {
            ReadToBufferList(_watchedILogFileInfo, FileSize, LineCount);
            args.FileSize = newSize;
            args.LineCount = LineCount;
            //if (args.PrevLineCount != args.LineCount && !this.shouldStop)
            OnFileSizeChanged(args);
        }
    }

    /// <summary>
    /// Creates an <see cref="ILogStreamReader"/> for reading log entries from the specified stream using the provided
    /// encoding options.
    /// </summary>
    /// <remarks>
    /// If XML mode is enabled, the returned reader splits and parses XML log blocks according to the current XML log
    /// configuration. The caller is responsible for disposing the returned reader when finished.
    /// </remarks>
    /// <param name="stream">
    /// The input stream containing the log data to be read. The stream must be readable and positioned at the start of
    /// the log content.
    /// </param>
    /// <param name="encodingOptions">
    /// The encoding options to use when interpreting the log data from the stream.
    /// </param>
    /// <returns>
    /// An <see cref="ILogStreamReader"/> instance for reading log entries from the specified stream. If XML mode is
    /// enabled, the reader parses XML log blocks; otherwise, it reads logs in the default format.
    /// </returns>
    private ILogStreamReader GetLogStreamReader (Stream stream, EncodingOptions encodingOptions)
    {
        var reader = CreateLogStreamReader(stream, encodingOptions);

        return IsXmlMode ? new XmlBlockSplitter(new XmlLogReader(reader), XmlLogConfig) : reader;
    }

    /// <summary>
    /// Creates an instance of an ILogStreamReader for reading log data from the specified stream using the provided
    /// encoding options.
    /// </summary>
    /// <param name="stream">
    /// The input stream containing the log data to be read. The stream must be readable and positioned at the start of
    /// the log data.
    /// </param>
    /// <param name="encodingOptions">
    /// The encoding options to use when interpreting the log data from the stream.
    /// </param>
    /// <returns>
    /// An ILogStreamReader instance configured to read from the specified stream with the given encoding options.
    /// </returns>
    private ILogStreamReader CreateLogStreamReader (Stream stream, EncodingOptions encodingOptions)
    {
        return _readerType switch
        {
            ReaderType.Legacy => new PositionAwareStreamReaderLegacy(stream, encodingOptions, _maximumLineLength),
            ReaderType.System => new PositionAwareStreamReaderSystem(stream, encodingOptions, _maximumLineLength),
            ReaderType.SystemDirect => new PositionAwareStreamReaderDirect(stream, encodingOptions, _maximumLineLength),
            //Default will be SystemDirect, because it is the best performing reader and should be used if not explicitly overridden by user.
            _ => new PositionAwareStreamReaderDirect(stream, encodingOptions, _maximumLineLength),
        };
    }

    /// <summary>
    /// Attempts to read a single line from the specified log stream reader and applies optional preprocessing.
    /// </summary>
    /// <remarks>
    /// If an IOException or NotSupportedException occurs during reading, the method logs a warning and treats the
    /// situation as end of stream. If a PreProcessColumnizer is set, the line is processed before being returned.
    /// </remarks>
    /// <param name="reader">The log stream reader from which to read the next line. Cannot be null.</param>
    /// <param name="lineNum">
    /// The logical line number to associate with the line being read. Used for preprocessing.
    /// </param>
    /// <param name="realLineNum">The actual line number in the underlying data source. Used for preprocessing.</param>
    /// <param name="outLine">
    /// When this method returns, contains the line that was read and optionally preprocessed, or null if the end of the
    /// stream is reached or an error occurs.
    /// </param>
    /// <returns>true if a line was successfully read and assigned to outLine; otherwise, false.</returns>
    private bool ReadLine (ILogStreamReader reader, int lineNum, int realLineNum, out string outLine)
    {
        string line = null;
        try
        {
            line = reader.ReadLine();
        }
        catch (IOException e)
        {
            _logger.Warn(e);
        }
        catch (NotSupportedException e)
        {
            // Bug#11: "Reading operations are not supported by the stream"
            // Currently not reproducible. Probably happens at an unlucky time interval (after opening the file)
            // when the file is being deleted (rolling)
            // This will be handled as EOF.
            _logger.Warn(e);
        }

        if (line == null) // EOF or catched Exception
        {
            outLine = null;
            return false;
        }

        if (PreProcessColumnizer != null)
        {
            line = PreProcessColumnizer.PreProcessLine(line, lineNum, realLineNum);
        }

        outLine = line;
        return true;
    }

    /// <summary>
    /// Attempts to read a single line from the specified log stream reader, returning both the line as a string and, if
    /// available, as a memory buffer without additional allocations.
    /// </summary>
    /// <remarks>
    /// If the reader implements memory-based access, this method avoids unnecessary string allocations by returning the
    /// line as a ReadOnlyMemory<char>. Otherwise, it falls back to reading the line as a string only. The returned
    /// memory buffer is only valid until the next read operation on the reader.
    /// </remarks>
    /// <param name="reader">The log stream reader from which to read the line. Must not be null.</param>
    /// <param name="lineNum">
    /// The zero-based logical line number to associate with the read operation. Used for preprocessing or context.
    /// </param>
    /// <param name="realLineNum">
    /// The zero-based physical line number in the underlying data source. Used for preprocessing or context.
    /// </param>
    /// <returns>
    /// A tuple containing a boolean indicating success, a read-only memory buffer containing the line if available, and
    /// the line as a string. If the reader supports memory-based access, the memory buffer is populated; otherwise, it
    /// is null.
    /// </returns>
    private (bool Success, ReadOnlyMemory<char> LineMemory, bool wasDropped) ReadLineMemory (ILogStreamReaderMemory reader, int lineNum, int realLineNum)
    {
        if (reader is null)
        {
            // Fallback to string-based reading if memory reader not available
            if (ReadLine(reader, lineNum, realLineNum, out var outLine))
            {
                return (true, outLine.AsMemory(), false);
            }

            return (false, ReadOnlyMemory<char>.Empty, false);
        }

        if (!reader.TryReadLine(out var lineMemory))
        {
            return (false, ReadOnlyMemory<char>.Empty, false);
        }

        var originalMemory = lineMemory;

        if (PreProcessColumnizer != null)
        {
            lineMemory = PreProcessColumnizer.PreProcessLine(lineMemory, lineNum, realLineNum);

            if (lineMemory.IsEmpty && !originalMemory.IsEmpty)
            {
                // Line was dropped by preprocessor
                return (true, ReadOnlyMemory<char>.Empty, true);
            }
        }

        return (true, lineMemory, false);
    }

#if DEBUG
    /// <summary>
    /// Outputs detailed information about the specified log buffer to the trace logger for debugging purposes.
    /// </summary>
    /// <remarks>
    /// This method is only available in debug builds. It writes buffer details such as start line, line count,
    /// position, size, disposal state, and associated file to the trace log if trace logging is enabled.
    /// </remarks>
    /// <param name="buffer">
    /// The log buffer whose information will be written to the trace output. Cannot be null.
    /// </param>
    private static void DumpBufferInfos (LogBuffer buffer)
    {
        if (_logger.IsTraceEnabled)
        {
            _logger.Trace(CultureInfo.InvariantCulture, "StartLine: {0}\r\nLineCount: {1}\r\nStartPos: {2}\r\nSize: {3}\r\nDisposed: {4}\r\nDisposeCount: {5}\r\nFile: {6}",
                buffer.StartLine,
                buffer.LineCount,
                buffer.StartPos,
                buffer.Size,
                buffer.IsDisposed ? "yes" : "no",
                buffer.DisposeCount,
                buffer.FileInfo.FullName);
        }
    }

#endif

    #endregion

    #region IDisposable Support

    /// <summary>
    /// Releases all resources used by the current instance of the class.
    /// </summary>
    /// <remarks>
    /// Call this method when you are finished using the object to release unmanaged resources and perform other cleanup
    /// operations. After calling Dispose, the object should not be used.
    /// </remarks>
    public void Dispose ()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the object and optionally releases the managed resources.
    /// </summary>
    /// <remarks>
    /// This method is called by public Dispose methods and can be overridden to release additional resources in derived
    /// classes. When disposing is true, both managed and unmanaged resources should be released. When disposing is
    /// false, only unmanaged resources should be released.
    /// </remarks>
    /// <param name="disposing">
    /// true to release both managed and unmanaged resources; false to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose (bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                //Keep Dispose Order unless otherwise noted.
                //For example, the progress reporter waits 2 seconds for the dispatch task
                //and DeleteAllContent may trigger final events.
                DeleteAllContent();
                _cts.Dispose();
                BufferIndex.Dispose();
                _progressReporter.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizes an instance of the LogfileReader class, releasing unmanaged resources before the object is reclaimed
    /// by garbage collection.
    /// </summary>
    /// <remarks>
    /// This destructor is called automatically by the garbage collector when the object is no longer accessible. It
    /// ensures that any unmanaged resources are properly released if Dispose was not called explicitly.
    /// </remarks>
    //TODO: Seems that this can be deleted. Need to verify.
    ~LogfileReader ()
    {
        Dispose(false);
    }

    #endregion IDisposable Support

    #region Event Handlers

    /// <summary>
    /// Raises the FileSizeChanged event to notify subscribers when the size of the log file changes.
    /// </summary>
    /// <remarks>
    /// Derived classes can override this method to provide custom handling when the file size changes. This method is
    /// typically called after the file size has been updated.
    /// </remarks>
    /// <param name="e">An object that contains the event data associated with the file size change.</param>
    protected virtual void OnFileSizeChanged (LogEventArgs e)
    {
        FileSizeChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the LoadFile event to notify subscribers that a file load operation has occurred.
    /// </summary>
    /// <remarks>
    /// Override this method in a derived class to provide custom handling when a file is loaded. Calling the base
    /// implementation ensures that registered event handlers are invoked.
    /// </remarks>
    /// <param name="e">An object that contains the event data for the file load operation.</param>
    protected virtual void OnLoadFile (LoadFileEventArgs e)
    {
        LoadFile?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the LoadingStarted event to signal that a file loading operation has begun.
    /// </summary>
    /// <remarks>
    /// Derived classes can override this method to provide custom handling when a loading operation starts. This method
    /// is typically called to notify subscribers that loading has commenced.
    /// </remarks>
    /// <param name="e">An object that contains the event data associated with the loading operation.</param>
    protected virtual void OnLoadingStarted (LoadFileEventArgs e)
    {
        LoadingStarted?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the LoadingFinished event to signal that the loading process has completed.
    /// </summary>
    /// <remarks>
    /// Override this method in a derived class to provide custom logic when loading is finished. This method is
    /// typically called after all loading operations are complete to notify subscribers.
    /// </remarks>
    protected virtual void OnLoadingFinished ()
    {
        LoadingFinished?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the event that signals a file was not found.
    /// </summary>
    /// <remarks>
    /// Override this method in a derived class to provide custom handling when a file is not found. This method invokes
    /// the associated event handlers, if any are subscribed.
    /// </remarks>
    protected virtual void OnFileNotFound ()
    {
        FileNotFound?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the Respawned event to notify subscribers that the object has respawned.
    /// </summary>
    /// <remarks>
    /// Override this method in a derived class to provide custom logic when the object respawns. Always call the base
    /// implementation to ensure that the Respawned event is raised.
    /// </remarks>
    protected virtual void OnRespawned ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "OnRespawned()");
        Respawned?.Invoke(this, EventArgs.Empty);
    }

    #endregion Event Handlers

    #region IBufferPinning

    /// <inheritdoc />
    PinHandle IBufferPinning.PinRange (int startLine, int endLine)
    {
        using var readLock = BufferIndex.AcquireReadLock();
        return BufferIndex.PinRange(startLine, endLine);
    }

    #endregion
}
