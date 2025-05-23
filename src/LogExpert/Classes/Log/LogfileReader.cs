using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Log;
using LogExpert.Core.Classes.xml;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArgs;
using LogExpert.Core.Interface;

using NLog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogExpert.Classes.Log
{
    public class LogfileReader : IAutoLogLineColumnizerCallback
    {
        #region Fields

        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly GetLogLineFx _logLineFx;

        private readonly string _fileName;
        private readonly int _MAX_BUFFERS = 10;
        private readonly int _MAX_LINES_PER_BUFFER = 100;

        private readonly object _monitor = new();
        private readonly MultiFileOptions _multiFileOptions;

        private IList<LogBuffer> _bufferList;
        private ReaderWriterLock _bufferListLock;
        private IList<LogBuffer> _bufferLru;
        private bool _contentDeleted;
        private int _currLineCount;
        private ReaderWriterLock _disposeLock;
        private EncodingOptions _encodingOptions;
        private long _fileLength;

        private Task _garbageCollectorTask;
        private Task _monitorTask;
        private readonly CancellationTokenSource cts = new();

        private bool _isDeleted;
        private bool _isFailModeCheckCallPending;
        private bool _isFastFailOnGetLogLine;
        private bool _isLineCountDirty = true;
        private IList<ILogFileInfo> _logFileInfoList = [];
        private Dictionary<int, LogBufferCacheEntry> _lruCacheDict;

        private ReaderWriterLock _lruCacheDictLock;

        private bool _shouldStop;
        private ILogFileInfo _watchedILogFileInfo;

        #endregion

        #region cTor

        public LogfileReader(string fileName, EncodingOptions encodingOptions, bool multiFile, int bufferCount, int linesPerBuffer, MultiFileOptions multiFileOptions)
        {
            if (fileName == null)
            {
                return;
            }

            _fileName = fileName;
            EncodingOptions = encodingOptions;
            IsMultiFile = multiFile;
            _MAX_BUFFERS = bufferCount;
            _MAX_LINES_PER_BUFFER = linesPerBuffer;
            _multiFileOptions = multiFileOptions;
            _logLineFx = GetLogLineInternal;
            InitLruBuffers();

            if (multiFile)
            {
                ILogFileInfo info = GetLogFileInfo(fileName);
                RolloverFilenameHandler rolloverHandler = new(info, _multiFileOptions);
                LinkedList<string> nameList = rolloverHandler.GetNameList();

                ILogFileInfo fileInfo = null;
                foreach (string name in nameList)
                {
                    fileInfo = AddFile(name);
                }

                _watchedILogFileInfo = fileInfo; // last added file in the list is the watched file
            }
            else
            {
                _watchedILogFileInfo = AddFile(fileName);
            }

            StartGCThread();
        }

        public LogfileReader(string[] fileNames, EncodingOptions encodingOptions, int bufferCount, int linesPerBuffer, MultiFileOptions multiFileOptions)
        {
            if (fileNames == null || fileNames.Length < 1)
            {
                return;
            }

            EncodingOptions = encodingOptions;
            IsMultiFile = true;
            _MAX_BUFFERS = bufferCount;
            _MAX_LINES_PER_BUFFER = linesPerBuffer;
            _multiFileOptions = multiFileOptions;
            _logLineFx = GetLogLineInternal;

            InitLruBuffers();

            ILogFileInfo fileInfo = null;
            foreach (string name in fileNames)
            {
                fileInfo = AddFile(name);
            }

            _watchedILogFileInfo = fileInfo;
            _fileName = fileInfo.FullName;

            StartGCThread();
        }

        #endregion

        #region Delegates

        public delegate void BlockLoadedEventHandler(object sender, LoadFileEventArgs e);

        public delegate void FileNotFoundEventHandler(object sender, EventArgs e);

        public delegate void FileRespawnedEventHandler(object sender, EventArgs e);

        public delegate void FileSizeChangedEventHandler(object sender, LogEventArgs e);

        public delegate void FinishedLoadingEventHandler(object sender, EventArgs e);

        private delegate Task<ILogLine> GetLogLineFx(int lineNum);

        public delegate void LoadingStartedEventHandler(object sender, LoadFileEventArgs e);

        #endregion

        #region Events

        public event FileSizeChangedEventHandler FileSizeChanged;
        public event BlockLoadedEventHandler LoadFile;
        public event LoadingStartedEventHandler LoadingStarted;
        public event FinishedLoadingEventHandler LoadingFinished;
        public event FileNotFoundEventHandler FileNotFound;
        public event FileRespawnedEventHandler Respawned;

        #endregion

        #region Properties

        public int LineCount
        {
            get
            {
                if (_isLineCountDirty)
                {
                    _currLineCount = 0;
                    AcquireBufferListReaderLock();
                    foreach (LogBuffer buffer in _bufferList)
                    {
                        _currLineCount += buffer.LineCount;
                    }

                    ReleaseBufferListReaderLock();
                    _isLineCountDirty = false;
                }

                return _currLineCount;
            }
            set => _currLineCount = value;
        }

        public bool IsMultiFile { get; }

        public Encoding CurrentEncoding { get; private set; }

        public long FileSize { get; private set; }

        public bool IsXmlMode { get; set; } = false;

        public IXmlLogConfiguration XmlLogConfig { get; set; }

        public IPreProcessColumnizer PreProcessColumnizer { get; set; } = null;

        public EncodingOptions EncodingOptions
        {
            get => _encodingOptions;
            set
            {
                {
                    _encodingOptions = new EncodingOptions
                    {
                        DefaultEncoding = value.DefaultEncoding,
                        Encoding = value.Encoding
                    };
                }
            }
        }

        public bool UseNewReader { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Public for unit test reasons
        /// </summary>
        public void ReadFiles()
        {
            FileSize = 0;
            LineCount = 0;
            //this.lastReturnedLine = "";
            //this.lastReturnedLineNum = -1;
            //this.lastReturnedLineNumForBuffer = -1;
            _isDeleted = false;
            ClearLru();
            AcquireBufferListWriterLock();
            _bufferList.Clear();
            ReleaseBufferListWriterLock();
            try
            {
                foreach (ILogFileInfo info in _logFileInfoList)
                {
                    //info.OpenFile();
                    ReadToBufferList(info, 0, LineCount);
                }

                if (_logFileInfoList.Count > 0)
                {
                    ILogFileInfo info = _logFileInfoList[_logFileInfoList.Count - 1];
                    _fileLength = info.Length;
                    _watchedILogFileInfo = info;
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
        /// Public for unit tests.
        /// </summary>
        /// <returns></returns>
        public int ShiftBuffers()
        {
            _logger.Info("ShiftBuffers() begin for {0}{1}", _fileName, IsMultiFile ? " (MultiFile)" : "");
            AcquireBufferListWriterLock();
            int offset = 0;
            _isLineCountDirty = true;
            lock (_monitor)
            {
                RolloverFilenameHandler rolloverHandler = new(_watchedILogFileInfo, _multiFileOptions);
                LinkedList<string> fileNameList = rolloverHandler.GetNameList();

                ResetBufferCache();
                IList<ILogFileInfo> lostILogFileInfoList = [];
                IList<ILogFileInfo> readNewILogFileInfoList = [];
                IList<ILogFileInfo> newFileInfoList = [];
                IEnumerator<ILogFileInfo> enumerator = _logFileInfoList.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    ILogFileInfo logFileInfo = enumerator.Current;
                    string fileName = logFileInfo.FullName;
                    _logger.Debug("Testing file {0}", fileName);
                    LinkedListNode<string> node = fileNameList.Find(fileName);
                    if (node == null)
                    {
                        _logger.Warn("File {0} not found", fileName);
                        continue;
                    }

                    if (node.Previous != null)
                    {
                        fileName = node.Previous.Value;
                        ILogFileInfo newILogFileInfo = GetLogFileInfo(fileName);
                        _logger.Debug("{0} exists\r\nOld size={1}, new size={2}", fileName, logFileInfo.OriginalLength, newILogFileInfo.Length);
                        // is the new file the same as the old buffer info?
                        if (newILogFileInfo.Length == logFileInfo.OriginalLength)
                        {
                            ReplaceBufferInfos(logFileInfo, newILogFileInfo);
                            newFileInfoList.Add(newILogFileInfo);
                        }
                        else
                        {
                            _logger.Debug("Buffer for {0} must be re-read.", fileName);
                            // not the same. so must read the rest of the list anew from the files
                            readNewILogFileInfoList.Add(newILogFileInfo);
                            while (enumerator.MoveNext())
                            {
                                fileName = enumerator.Current.FullName;
                                node = fileNameList.Find(fileName);
                                if (node == null)
                                {
                                    _logger.Warn("File {0} not found", fileName);
                                    continue;
                                }

                                if (node.Previous != null)
                                {
                                    fileName = node.Previous.Value;
                                    _logger.Debug("New name is {0}", fileName);
                                    readNewILogFileInfoList.Add(GetLogFileInfo(fileName));
                                }
                                else
                                {
                                    _logger.Warn("No previous file for {0} found", fileName);
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.Info("{0} does not exist", fileName);
                        lostILogFileInfoList.Add(logFileInfo);
#if DEBUG // for better overview in logfile:
                        //ILogFileInfo newILogFileInfo = new ILogFileInfo(fileName);
                        //ReplaceBufferInfos(ILogFileInfo, newILogFileInfo);
#endif
                    }
                }

                if (lostILogFileInfoList.Count > 0)
                {
                    _logger.Info("Deleting buffers for lost files");
                    foreach (ILogFileInfo ILogFileInfo in lostILogFileInfoList)
                    {
                        //this.ILogFileInfoList.Remove(ILogFileInfo);
                        LogBuffer lastBuffer = DeleteBuffersForInfo(ILogFileInfo, false);
                        if (lastBuffer != null)
                        {
                            offset += lastBuffer.StartLine + lastBuffer.LineCount;
                        }
                    }

                    _lruCacheDictLock.AcquireWriterLock(Timeout.Infinite);
                    _logger.Info("Adjusting StartLine values in {0} buffers by offset {1}", _bufferList.Count, offset);
                    foreach (LogBuffer buffer in _bufferList)
                    {
                        SetNewStartLineForBuffer(buffer, buffer.StartLine - offset);
                    }

                    _lruCacheDictLock.ReleaseWriterLock();
#if DEBUG
                    if (_bufferList.Count > 0)
                    {
                        _logger.Debug("First buffer now has StartLine {0}", _bufferList[0].StartLine);
                    }
#endif
                }

                // Read anew all buffers following a buffer info that couldn't be matched with the corresponding existing file
                _logger.Info("Deleting buffers for files that must be re-read");
                foreach (ILogFileInfo ILogFileInfo in readNewILogFileInfoList)
                {
                    DeleteBuffersForInfo(ILogFileInfo, true);
                    //this.ILogFileInfoList.Remove(ILogFileInfo);
                }

                _logger.Info("Deleting buffers for the watched file");
                DeleteBuffersForInfo(_watchedILogFileInfo, true);
                int startLine = LineCount - 1;
                _logger.Info("Re-Reading files");
                foreach (ILogFileInfo ILogFileInfo in readNewILogFileInfoList)
                {
                    //ILogFileInfo.OpenFile();
                    ReadToBufferList(ILogFileInfo, 0, LineCount);
                    //this.ILogFileInfoList.Add(ILogFileInfo);
                    newFileInfoList.Add(ILogFileInfo);
                }

                //this.watchedILogFileInfo = this.ILogFileInfoList[this.ILogFileInfoList.Count - 1];
                _logFileInfoList = newFileInfoList;
                _watchedILogFileInfo = GetLogFileInfo(_watchedILogFileInfo.FullName);
                _logFileInfoList.Add(_watchedILogFileInfo);
                _logger.Info("Reading watched file");
                ReadToBufferList(_watchedILogFileInfo, 0, LineCount);
            }

            _logger.Info("ShiftBuffers() end. offset={0}", offset);
            ReleaseBufferListWriterLock();
            return offset;
        }

        public ILogLine GetLogLine(int lineNum)
        {
            return GetLogLineInternal(lineNum).Result;
        }

        /// <summary>
        /// Get the text content of the given line number.
        /// The actual work is done in an async thread. This method waits for thread completion for only 1 second. If the async
        /// thread has not returned, the method will return <code>null</code>. This is because this method is also called from GUI thread
        /// (e.g. LogWindow draw events). Under some circumstances, repeated calls to this method would lead the GUI to freeze. E.g. when
        /// trying to re-load content from disk but the file was deleted. Especially on network shares.
        /// </summary>
        /// <remarks>
        /// Once the method detects a timeout it will enter a kind of 'fast fail mode'. That means all following calls will be returned with
        /// <code>null</code> immediately (without 1 second wait). A background call to GetLogLineInternal() will check if a result is available.
        /// If so, the 'fast fail mode' is switched off. In most cases a fail is caused by a deleted file. But it may also be caused by slow
        /// network connections. So all this effort is needed to prevent entering an endless 'fast fail mode' just because of temporary problems.
        /// </remarks>
        /// <param name="lineNum">line to retrieve</param>
        /// <returns></returns>
        public async Task<ILogLine> GetLogLineWithWait(int lineNum)
        {
            const int WAIT_TIME = 1000;

            ILogLine result = null;

            if (!_isFastFailOnGetLogLine)
            {
                var task = Task.Run(() => _logLineFx(lineNum));
                if (task.Wait(WAIT_TIME))
                {
                    result = task.Result;
                    _isFastFailOnGetLogLine = false;
                }
                else
                {
                    _isFastFailOnGetLogLine = true;
                    _logger.Debug("No result after {0}ms. Returning <null>.", WAIT_TIME);
                }
            }
            else
            {
                _logger.Debug("Fast failing GetLogLine()");
                if (!_isFailModeCheckCallPending)
                {
                    _isFailModeCheckCallPending = true;
                    var logLine = await _logLineFx(lineNum);
                    GetLineFinishedCallback(logLine);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the file name of the actual file for the given line. Needed for MultiFile.
        /// </summary>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        public string GetLogFileNameForLine(int lineNum)
        {
            AcquireBufferListReaderLock();
            LogBuffer logBuffer = GetBufferForLine(lineNum);
            string fileName = logBuffer?.FileInfo.FullName;
            ReleaseBufferListReaderLock();
            return fileName;
        }

        /// <summary>
        /// Returns the ILogFileInfo for the actual file for the given line. Needed for MultiFile.
        /// </summary>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        public ILogFileInfo GetLogFileInfoForLine(int lineNum)
        {
            AcquireBufferListReaderLock();
            LogBuffer logBuffer = GetBufferForLine(lineNum);
            ILogFileInfo info = logBuffer?.FileInfo;
            ReleaseBufferListReaderLock();
            return info;
        }

        /// <summary>
        /// Returns the line number (starting from the given number) where the next multi file
        /// starts.
        /// </summary>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        public int GetNextMultiFileLine(int lineNum)
        {
            int result = -1;
            AcquireBufferListReaderLock();
            LogBuffer logBuffer = GetBufferForLine(lineNum);
            if (logBuffer != null)
            {
                int index = _bufferList.IndexOf(logBuffer);
                if (index != -1)
                {
                    for (int i = index; i < _bufferList.Count; ++i)
                    {
                        if (_bufferList[i].FileInfo != logBuffer.FileInfo)
                        {
                            result = _bufferList[i].StartLine;
                            break;
                        }
                    }
                }
            }

            ReleaseBufferListReaderLock();
            return result;
        }

        public int GetPrevMultiFileLine(int lineNum)
        {
            int result = -1;
            AcquireBufferListReaderLock();
            LogBuffer logBuffer = GetBufferForLine(lineNum);
            if (logBuffer != null)
            {
                int index = _bufferList.IndexOf(logBuffer);
                if (index != -1)
                {
                    for (int i = index; i >= 0; --i)
                    {
                        if (_bufferList[i].FileInfo != logBuffer.FileInfo)
                        {
                            result = _bufferList[i].StartLine + _bufferList[i].LineCount;
                            break;
                        }
                    }
                }
            }

            ReleaseBufferListReaderLock();
            return result;
        }

        /// <summary>
        /// Returns the actual line number in the file for the given 'virtual line num'.
        /// This is needed for multi file mode. 'Virtual' means that the given line num is a line
        /// number in the collections of the files currently viewed together in multi file mode as one large virtual file.
        /// This method finds the real file for the line number and maps the line number to the correct position
        /// in that file. This is needed when launching external tools to provide correct line number arguments.
        /// </summary>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        public int GetRealLineNumForVirtualLineNum(int lineNum)
        {
            AcquireBufferListReaderLock();
            LogBuffer logBuffer = GetBufferForLine(lineNum);
            int result = -1;
            if (logBuffer != null)
            {
                logBuffer = GetFirstBufferForFileByLogBuffer(logBuffer);
                if (logBuffer != null)
                {
                    result = lineNum - logBuffer.StartLine;
                }
            }

            ReleaseBufferListReaderLock();
            return result;
        }

        public void StartMonitoring()
        {
            _logger.Info("startMonitoring()");
            _monitorTask = Task.Run(MonitorThreadProc, cts.Token);
            _shouldStop = false;
        }

        public void StopMonitoring()
        {
            _logger.Info("stopMonitoring()");
            _shouldStop = true;

            Thread.Sleep(_watchedILogFileInfo.PollInterval); // leave time for the threads to stop by themselves

            if (_monitorTask != null)
            {
                if (_monitorTask.Status == TaskStatus.Running) // if thread has not finished, abort it
                {
                    cts.Cancel();
                }
            }

            if (_garbageCollectorTask.IsCanceled == false)
            {
                if (_garbageCollectorTask.Status == TaskStatus.Running) // if thread has not finished, abort it
                {
                    cts.Cancel();
                }
            }

            //this.loadThread = null;
            //_monitorThread = null;
            //_garbageCollectorThread = null; // preventive call
            CloseFiles();
        }

        /// <summary>
        /// calls stopMonitoring() in a background thread and returns to the caller immediately.
        /// This is useful for a fast responding GUI (e.g. when closing a file tab)
        /// </summary>
        public void StopMonitoringAsync()
        {
            Task task = Task.Run(StopMonitoring);

            //Thread stopperThread = new(new ThreadStart(StopMonitoring))
            //{
            //    IsBackground = true
            //};
            //stopperThread.Start();
        }

        /// <summary>
        /// Deletes all buffer lines and disposes their content. Use only when the LogfileReader
        /// is about to be closed!
        /// </summary>
        public void DeleteAllContent()
        {
            if (_contentDeleted)
            {
                _logger.Debug("Buffers for {0} already deleted.", Util.GetNameFromPath(_fileName));
                return;
            }

            _logger.Info("Deleting all log buffers for {0}. Used mem: {1:N0}", Util.GetNameFromPath(_fileName), GC.GetTotalMemory(true)); //TODO [Z] uh GC collect calls creepy
            AcquireBufferListWriterLock();
            _lruCacheDictLock.AcquireWriterLock(Timeout.Infinite);
            _disposeLock.AcquireWriterLock(Timeout.Infinite);

            foreach (LogBuffer logBuffer in _bufferList)
            {
                if (!logBuffer.IsDisposed)
                {
                    logBuffer.DisposeContent();
                }
            }

            _lruCacheDict.Clear();
            _bufferList.Clear();

            _disposeLock.ReleaseWriterLock();
            _lruCacheDictLock.ReleaseWriterLock();
            ReleaseBufferListWriterLock();
            GC.Collect();
            _contentDeleted = true;
            _logger.Info("Deleting complete. Used mem: {0:N0}", GC.GetTotalMemory(true)); //TODO [Z] uh GC collect calls creepy
        }

        /// <summary>
        /// Explicit change the encoding.
        /// </summary>
        /// <param name="encoding"></param>
        public void ChangeEncoding(Encoding encoding)
        {
            CurrentEncoding = encoding;
            EncodingOptions.Encoding = encoding;
            ResetBufferCache();
            ClearLru();
        }

        /// <summary>
        /// For unit tests only.
        /// </summary>
        /// <returns></returns>
        public IList<ILogFileInfo> GetLogFileInfoList()
        {
            return _logFileInfoList;
        }

        /// <summary>
        /// For unit tests only
        /// </summary>
        /// <returns></returns>
        public IList<LogBuffer> GetBufferList()
        {
            return _bufferList;
        }

        #endregion

        #region Internals

#if DEBUG

        public void LogBufferInfoForLine(int lineNum)
        {
            AcquireBufferListReaderLock();
            LogBuffer buffer = GetBufferForLine(lineNum);
            if (buffer == null)
            {
                ReleaseBufferListReaderLock();
                _logger.Error("Cannot find buffer for line {0}, file: {1}{2}", lineNum, _fileName, IsMultiFile ? " (MultiFile)" : "");
                return;
            }

            _logger.Info("-----------------------------------");
            _disposeLock.AcquireReaderLock(Timeout.Infinite);
            _logger.Info("Buffer info for line {0}", lineNum);
            DumpBufferInfos(buffer);
            _logger.Info("File pos for current line: {0}", buffer.GetFilePosForLineOfBlock(lineNum - buffer.StartLine));
            _disposeLock.ReleaseReaderLock();
            _logger.Info("-----------------------------------");
            ReleaseBufferListReaderLock();
        }
#endif

#if DEBUG
        public void LogBufferDiagnostic()
        {
            _logger.Info("-------- Buffer diagnostics -------");
            _lruCacheDictLock.AcquireReaderLock(Timeout.Infinite);
            int cacheCount = _lruCacheDict.Count;
            _logger.Info("LRU entries: {0}", cacheCount);
            _lruCacheDictLock.ReleaseReaderLock();

            AcquireBufferListReaderLock();
            _logger.Info("File: {0}\r\nBuffer count: {1}\r\nDisposed buffers: {2}", _fileName, _bufferList.Count, _bufferList.Count - cacheCount);
            int lineNum = 0;
            long disposeSum = 0;
            long maxDispose = 0;
            long minDispose = int.MaxValue;
            for (int i = 0; i < _bufferList.Count; ++i)
            {
                LogBuffer buffer = _bufferList[i];
                _disposeLock.AcquireReaderLock(Timeout.Infinite);
                if (buffer.StartLine != lineNum)
                {
                    _logger.Error("Start line of buffer is: {0}, expected: {1}", buffer.StartLine, lineNum);
                    _logger.Info("Info of buffer follows:");
                    DumpBufferInfos(buffer);
                }

                lineNum += buffer.LineCount;
                disposeSum += buffer.DisposeCount;
                maxDispose = Math.Max(maxDispose, buffer.DisposeCount);
                minDispose = Math.Min(minDispose, buffer.DisposeCount);
                _disposeLock.ReleaseReaderLock();
            }

            ReleaseBufferListReaderLock();
            _logger.Info("Dispose count sum is: {0}\r\nMin dispose count is: {1}\r\nMax dispose count is: {2}\r\n-----------------------------------", disposeSum, minDispose, maxDispose);
        }

#endif

        #endregion

        #region Private Methods

        private ILogFileInfo AddFile(string fileName)
        {
            _logger.Info("Adding file to ILogFileInfoList: " + fileName);
            ILogFileInfo info = GetLogFileInfo(fileName);
            _logFileInfoList.Add(info);
            return info;
        }

        private Task<ILogLine> GetLogLineInternal(int lineNum)
        {
            if (_isDeleted)
            {
                _logger.Debug("Returning null for line {0} because file is deleted.", lineNum);

                // fast fail if dead file was detected. Prevents repeated lags in GUI thread caused by callbacks from control (e.g. repaint)
                return null;
            }

            AcquireBufferListReaderLock();
            LogBuffer logBuffer = GetBufferForLine(lineNum);
            if (logBuffer == null)
            {
                ReleaseBufferListReaderLock();
                _logger.Error("Cannot find buffer for line {0}, file: {1}{2}", lineNum, _fileName, IsMultiFile ? " (MultiFile)" : "");
                return null;
            }

            // disposeLock prevents that the garbage collector is disposing just in the moment we use the buffer
            _disposeLock.AcquireReaderLock(Timeout.Infinite);
            if (logBuffer.IsDisposed)
            {
                LockCookie cookie = _disposeLock.UpgradeToWriterLock(Timeout.Infinite);
                lock (logBuffer.FileInfo)
                {
                    ReReadBuffer(logBuffer);
                }

                _disposeLock.DowngradeFromWriterLock(ref cookie);
            }

            ILogLine line = logBuffer.GetLineOfBlock(lineNum - logBuffer.StartLine);
            _disposeLock.ReleaseReaderLock();
            ReleaseBufferListReaderLock();

            return Task.FromResult(line);
        }

        private void InitLruBuffers()
        {
            _bufferList = [];
            _bufferLru = new List<LogBuffer>(_MAX_BUFFERS + 1);
            //this.lruDict = new Dictionary<int, int>(this.MAX_BUFFERS + 1);  // key=startline, value = index in bufferLru
            _lruCacheDict = new Dictionary<int, LogBufferCacheEntry>(_MAX_BUFFERS + 1);
            _lruCacheDictLock = new ReaderWriterLock();
            _bufferListLock = new ReaderWriterLock();
            _disposeLock = new ReaderWriterLock();
        }

        private void StartGCThread()
        {
            _garbageCollectorTask = Task.Run(GarbageCollectorThreadProc, cts.Token);
            //_garbageCollectorThread = new Thread(new ThreadStart(GarbageCollectorThreadProc));
            //_garbageCollectorThread.IsBackground = true;
            //_garbageCollectorThread.Start();
        }

        private void ResetBufferCache()
        {
            FileSize = 0;
            LineCount = 0;
            //this.lastReturnedLine = "";
            //this.lastReturnedLineNum = -1;
            //this.lastReturnedLineNumForBuffer = -1;
        }

        private void CloseFiles()
        {
            //foreach (ILogFileInfo info in this.ILogFileInfoList)
            //{
            //  info.CloseFile();
            //}
            FileSize = 0;
            LineCount = 0;
            //this.lastReturnedLine = "";
            //this.lastReturnedLineNum = -1;
            //this.lastReturnedLineNumForBuffer = -1;
        }

        private ILogFileInfo GetLogFileInfo(string fileNameOrUri)
        {
            //TODO this must be fixed and should be given to the logfilereader not just called
            IFileSystemPlugin fs = PluginRegistry.PluginRegistry.Instance.FindFileSystemForUri(fileNameOrUri) ?? throw new LogFileException("No file system plugin found for " + fileNameOrUri);
            ILogFileInfo logFileInfo = fs.GetLogfileInfo(fileNameOrUri);
            return logFileInfo ?? throw new LogFileException("Cannot find " + fileNameOrUri);
        }

        private void ReplaceBufferInfos(ILogFileInfo oldLogFileInfo, ILogFileInfo newLogFileInfo)
        {
            _logger.Debug("ReplaceBufferInfos() " + oldLogFileInfo.FullName + " -> " + newLogFileInfo.FullName);
            AcquireBufferListReaderLock();
            foreach (LogBuffer buffer in _bufferList)
            {
                if (buffer.FileInfo == oldLogFileInfo)
                {
                    _logger.Debug("Buffer with startLine={0}, lineCount={1}, filePos={2}, size={3} gets new filename {4}", buffer.StartLine, buffer.LineCount, buffer.StartPos, buffer.Size, newLogFileInfo.FullName);
                    buffer.FileInfo = newLogFileInfo;
                }
            }

            ReleaseBufferListReaderLock();
        }

        private LogBuffer DeleteBuffersForInfo(ILogFileInfo ILogFileInfo, bool matchNamesOnly)
        {
            _logger.Info("Deleting buffers for file {0}", ILogFileInfo.FullName);
            LogBuffer lastRemovedBuffer = null;
            IList<LogBuffer> deleteList = [];
            AcquireBufferListWriterLock();
            _lruCacheDictLock.AcquireWriterLock(Timeout.Infinite);
            if (matchNamesOnly)
            {
                foreach (LogBuffer buffer in _bufferList)
                {
                    if (buffer.FileInfo.FullName.ToLower().Equals(ILogFileInfo.FullName.ToLower()))
                    {
                        lastRemovedBuffer = buffer;
                        deleteList.Add(buffer);
                    }
                }
            }
            else
            {
                foreach (LogBuffer buffer in _bufferList)
                {
                    if (buffer.FileInfo == ILogFileInfo)
                    {
                        lastRemovedBuffer = buffer;
                        deleteList.Add(buffer);
                    }
                }
            }

            foreach (LogBuffer buffer in deleteList)
            {
                RemoveFromBufferList(buffer);
            }

            _lruCacheDictLock.ReleaseWriterLock();
            ReleaseBufferListWriterLock();
            if (lastRemovedBuffer == null)
            {
                _logger.Info("lastRemovedBuffer is null");
            }
            else
            {
                _logger.Info("lastRemovedBuffer: startLine={0}", lastRemovedBuffer.StartLine);
            }

            return lastRemovedBuffer;
        }

        /// <summary>
        /// The caller must have writer locks for lruCache and buffer list!
        /// </summary>
        /// <param name="buffer"></param>
        private void RemoveFromBufferList(LogBuffer buffer)
        {
            Util.AssertTrue(_lruCacheDictLock.IsWriterLockHeld, "No writer lock for lru cache");
            Util.AssertTrue(_bufferListLock.IsWriterLockHeld, "No writer lock for buffer list");
            _lruCacheDict.Remove(buffer.StartLine);
            _bufferList.Remove(buffer);
        }

        private void ReadToBufferList(ILogFileInfo logFileInfo, long filePos, int startLine)
        {
            try
            {
                using Stream fileStream = logFileInfo.OpenStream();
                try
                {
                    using ILogStreamReader reader = GetLogStreamReader(fileStream, EncodingOptions, UseNewReader);
                    reader.Position = filePos;
                    _fileLength = logFileInfo.Length;

                    int lineNum = startLine;
                    LogBuffer logBuffer;
                    AcquireBufferListReaderLock();
                    if (_bufferList.Count == 0)
                    {
                        logBuffer = new LogBuffer(logFileInfo, _MAX_LINES_PER_BUFFER);
                        logBuffer.StartLine = startLine;
                        logBuffer.StartPos = filePos;
                        LockCookie cookie = UpgradeBufferListLockToWriter();
                        AddBufferToList(logBuffer);
                        DowngradeBufferListLockFromWriter(ref cookie);
                    }
                    else
                    {
                        logBuffer = _bufferList[_bufferList.Count - 1];

                        if (!logBuffer.FileInfo.FullName.Equals(logFileInfo.FullName))
                        {
                            logBuffer = new LogBuffer(logFileInfo, _MAX_LINES_PER_BUFFER);
                            logBuffer.StartLine = startLine;
                            logBuffer.StartPos = filePos;
                            LockCookie cookie = UpgradeBufferListLockToWriter();
                            AddBufferToList(logBuffer);
                            DowngradeBufferListLockFromWriter(ref cookie);
                        }

                        _disposeLock.AcquireReaderLock(Timeout.Infinite);
                        if (logBuffer.IsDisposed)
                        {
                            LockCookie cookie = _disposeLock.UpgradeToWriterLock(Timeout.Infinite);
                            ReReadBuffer(logBuffer);
                            _disposeLock.DowngradeFromWriterLock(ref cookie);
                        }

                        _disposeLock.ReleaseReaderLock();
                    }

                    Monitor.Enter(logBuffer); // Lock the buffer
                    ReleaseBufferListReaderLock();
                    int lineCount = logBuffer.LineCount;
                    int droppedLines = logBuffer.PrevBuffersDroppedLinesSum;
                    filePos = reader.Position;

                    while (ReadLine(reader, logBuffer.StartLine + logBuffer.LineCount, logBuffer.StartLine + logBuffer.LineCount + droppedLines, out var line))
                    {
                        LogLine logLine = new();
                        if (_shouldStop)
                        {
                            Monitor.Exit(logBuffer);
                            return;
                        }

                        if (line == null)
                        {
                            logBuffer.DroppedLinesCount += 1;
                            droppedLines++;
                            continue;
                        }

                        lineCount++;
                        if (lineCount > _MAX_LINES_PER_BUFFER && reader.IsBufferComplete)
                        {
                            OnLoadFile(new LoadFileEventArgs(logFileInfo.FullName, filePos, false, logFileInfo.Length, false));

                            Monitor.Exit(logBuffer);
                            logBuffer = new LogBuffer(logFileInfo, _MAX_LINES_PER_BUFFER);
                            Monitor.Enter(logBuffer);
                            logBuffer.StartLine = lineNum;
                            logBuffer.StartPos = filePos;
                            logBuffer.PrevBuffersDroppedLinesSum = droppedLines;
                            AcquireBufferListWriterLock();
                            AddBufferToList(logBuffer);
                            ReleaseBufferListWriterLock();
                            lineCount = 1;
                        }

                        logLine.FullLine = line;
                        logLine.LineNumber = logBuffer.StartLine + logBuffer.LineCount;

                        logBuffer.AddLine(logLine, filePos);
                        filePos = reader.Position;
                        lineNum++;
                    }

                    logBuffer.Size = filePos - logBuffer.StartPos;
                    Monitor.Exit(logBuffer);
                    _isLineCountDirty = true;
                    FileSize = reader.Position;
                    CurrentEncoding = reader.Encoding; // Reader may have detected another encoding
                    if (!_shouldStop)
                    {
                        OnLoadFile(new LoadFileEventArgs(logFileInfo.FullName, filePos, true, _fileLength, false));
                        // Fire "Ready" Event
                    }
                }
                catch (IOException ioex)
                {
                    _logger.Warn(ioex);
                }
            }
            catch (IOException fe)
            {
                _logger.Warn(fe, "IOException: ");
                _isDeleted = true;
                LineCount = 0;
                FileSize = 0;
                OnFileNotFound(); // notify LogWindow
            }
        }

        private void AddBufferToList(LogBuffer logBuffer)
        {
#if DEBUG
            _logger.Debug("AddBufferToList(): {0}/{1}/{2}", logBuffer.StartLine, logBuffer.LineCount, logBuffer.FileInfo.FullName);
#endif
            _bufferList.Add(logBuffer);
            //UpdateLru(logBuffer);
            UpdateLruCache(logBuffer);
        }

        private void UpdateLruCache(LogBuffer logBuffer)
        {
            _lruCacheDictLock.AcquireReaderLock(Timeout.Infinite);
            if (_lruCacheDict.TryGetValue(logBuffer.StartLine, out LogBufferCacheEntry cacheEntry))
            {
                cacheEntry.Touch();
            }
            else
            {
                LockCookie cookie = _lruCacheDictLock.UpgradeToWriterLock(Timeout.Infinite);
                if (!_lruCacheDict.TryGetValue(logBuffer.StartLine, out cacheEntry)
                ) // #536: re-test, because multiple threads may have been waiting for writer lock
                {
                    cacheEntry = new LogBufferCacheEntry();
                    cacheEntry.LogBuffer = logBuffer;
                    try
                    {
                        _lruCacheDict.Add(logBuffer.StartLine, cacheEntry);
                    }
                    catch (ArgumentException e)
                    {
                        _logger.Error(e, "Error in LRU cache: " + e.Message);
#if DEBUG // there seems to be a bug with double added key

                        _logger.Info("Added buffer:");
                        DumpBufferInfos(logBuffer);
                        if (_lruCacheDict.TryGetValue(logBuffer.StartLine, out LogBufferCacheEntry existingEntry))
                        {
                            _logger.Info("Existing buffer: ");
                            DumpBufferInfos(existingEntry.LogBuffer);
                        }
                        else
                        {
                            _logger.Warn("Ooops? Cannot find the already existing entry in LRU.");
                        }
#endif
                        _lruCacheDictLock.ReleaseLock();
                        throw;
                    }
                }

                _lruCacheDictLock.DowngradeFromWriterLock(ref cookie);
            }

            _lruCacheDictLock.ReleaseReaderLock();
        }

        /// <summary>
        /// Sets a new start line in the given buffer and updates the LRU cache, if the buffer
        /// is present in the cache. The caller must have write lock for 'lruCacheDictLock';
        /// </summary>
        /// <param name="logBuffer"></param>
        /// <param name="newLineNum"></param>
        private void SetNewStartLineForBuffer(LogBuffer logBuffer, int newLineNum)
        {
            Util.AssertTrue(_lruCacheDictLock.IsWriterLockHeld, "No writer lock for lru cache");
            if (_lruCacheDict.ContainsKey(logBuffer.StartLine))
            {
                _lruCacheDict.Remove(logBuffer.StartLine);
                logBuffer.StartLine = newLineNum;
                LogBufferCacheEntry cacheEntry = new();
                cacheEntry.LogBuffer = logBuffer;
                _lruCacheDict.Add(logBuffer.StartLine, cacheEntry);
            }
            else
            {
                logBuffer.StartLine = newLineNum;
            }
        }

        private void GarbageCollectLruCache()
        {
#if DEBUG
            long startTime = Environment.TickCount;
#endif
            _logger.Debug("Starting garbage collection");
            int threshold = 10;
            _lruCacheDictLock.AcquireWriterLock(Timeout.Infinite);
            int diff = 0;
            if (_lruCacheDict.Count - (_MAX_BUFFERS + threshold) > 0)
            {
                diff = _lruCacheDict.Count - _MAX_BUFFERS;
#if DEBUG
                if (diff > 0)
                {
                    _logger.Info("Removing {0} entries from LRU cache for {1}", diff, Util.GetNameFromPath(_fileName));
                }
#endif
                SortedList<long, int> useSorterList = [];
                // sort by usage counter
                foreach (LogBufferCacheEntry entry in _lruCacheDict.Values)
                {
                    if (!useSorterList.ContainsKey(entry.LastUseTimeStamp))
                    {
                        useSorterList.Add(entry.LastUseTimeStamp, entry.LogBuffer.StartLine);
                    }
                }

                // remove first <diff> entries (least usage)
                _disposeLock.AcquireWriterLock(Timeout.Infinite);
                for (int i = 0; i < diff; ++i)
                {
                    if (i >= useSorterList.Count)
                    {
                        break;
                    }

                    int startLine = useSorterList.Values[i];
                    LogBufferCacheEntry entry = _lruCacheDict[startLine];
                    _lruCacheDict.Remove(startLine);
                    entry.LogBuffer.DisposeContent();
                }

                _disposeLock.ReleaseWriterLock();
            }

            _lruCacheDictLock.ReleaseWriterLock();
#if DEBUG
            if (diff > 0)
            {
                long endTime = Environment.TickCount;
                _logger.Info("Garbage collector time: " + (endTime - startTime) + " ms.");
            }
#endif
        }

        private void GarbageCollectorThreadProc()
        {
            while (!_shouldStop)
            {
                try
                {
                    Thread.Sleep(10000);
                }
                catch (Exception)
                {
                }

                GarbageCollectLruCache();
            }
        }

        //    private void UpdateLru(LogBuffer logBuffer)
        //    {
        //      lock (this.monitor)
        //      {
        //        int index;
        //        if (this.lruDict.TryGetValue(logBuffer.StartLine, out index))
        //        {
        //          RemoveBufferFromLru(logBuffer, index);
        //          AddBufferToLru(logBuffer);
        //        }
        //        else
        //        {
        //          if (this.bufferLru.Count > MAX_BUFFERS - 1)
        //          {
        //            LogBuffer looser = this.bufferLru[0];
        //            if (looser != null)
        //            {
        //#if DEBUG
        //              _logger.logDebug("Disposing buffer: " + looser.StartLine + "/" + looser.LineCount + "/" + looser.FileInfo.FileName);
        //#endif
        //              looser.DisposeContent();
        //              RemoveBufferFromLru(looser);
        //            }
        //          }
        //          AddBufferToLru(logBuffer);
        //        }
        //      }
        //    }

        ///// <summary>
        ///// Removes a LogBuffer from the LRU. Note that the LogBuffer is searched in the lruDict
        ///// via StartLine. So this property must have a consistent value.
        ///// </summary>
        ///// <param name="buffer"></param>
        //private void RemoveBufferFromLru(LogBuffer buffer)
        //{
        //  int index;
        //  lock (this.monitor)
        //  {
        //    if (this.lruDict.TryGetValue(buffer.StartLine, out index))
        //    {
        //      RemoveBufferFromLru(buffer, index);
        //    }
        //  }
        //}

        ///// <summary>
        ///// Removes a LogBuffer from the LRU with known index. Note that the LogBuffer is searched in the lruDict
        ///// via StartLine. So this property must have a consistent value.
        ///// </summary>
        ///// <param name="buffer"></param>
        ///// <param name="index"></param>
        //private void RemoveBufferFromLru(LogBuffer buffer, int index)
        //{
        //  lock (this.monitor)
        //  {
        //    this.bufferLru.RemoveAt(index);
        //    this.lruDict.Remove(buffer.StartLine);
        //    // adjust indizes, they have changed because of the remove
        //    for (int i = index; i < this.bufferLru.Count; ++i)
        //    {
        //      this.lruDict[this.bufferLru[i].StartLine] = this.lruDict[this.bufferLru[i].StartLine] - 1;
        //    }
        //  }
        //}

        //private void AddBufferToLru(LogBuffer logBuffer)
        //{
        //  lock (this.monitor)
        //  {
        //    this.bufferLru.Add(logBuffer);
        //    int newIndex = this.bufferLru.Count - 1;
        //    this.lruDict[logBuffer.StartLine] = newIndex;
        //  }
        //}

        private void ClearLru()
        {
            //lock (this.monitor)
            //{
            //  foreach (LogBuffer buffer in this.bufferLru)
            //  {
            //    buffer.DisposeContent();
            //  }
            //  this.bufferLru.Clear();
            //  this.lruDict.Clear();
            //}
            _logger.Info("Clearing LRU cache.");
            _lruCacheDictLock.AcquireWriterLock(Timeout.Infinite);
            _disposeLock.AcquireWriterLock(Timeout.Infinite);
            foreach (LogBufferCacheEntry entry in _lruCacheDict.Values)
            {
                entry.LogBuffer.DisposeContent();
            }

            _lruCacheDict.Clear();
            _disposeLock.ReleaseWriterLock();
            _lruCacheDictLock.ReleaseWriterLock();
            _logger.Info("Clearing done.");
        }

        private void ReReadBuffer(LogBuffer logBuffer)
        {
#if DEBUG
            _logger.Info("re-reading buffer: {0}/{1}/{2}", logBuffer.StartLine, logBuffer.LineCount, logBuffer.FileInfo.FullName);
#endif
            try
            {
                Monitor.Enter(logBuffer);
                Stream fileStream = null;
                try
                {
                    fileStream = logBuffer.FileInfo.OpenStream();
                }
                catch (IOException e)
                {
                    _logger.Warn(e);
                    return;
                }

                try
                {
                    ILogStreamReader reader = GetLogStreamReader(fileStream, EncodingOptions, UseNewReader);

                    long filePos = logBuffer.StartPos;
                    reader.Position = logBuffer.StartPos;
                    int maxLinesCount = logBuffer.LineCount;
                    int lineCount = 0;
                    int dropCount = logBuffer.PrevBuffersDroppedLinesSum;
                    logBuffer.ClearLines();

                    while (ReadLine(reader, logBuffer.StartLine + logBuffer.LineCount, logBuffer.StartLine + logBuffer.LineCount + dropCount, out string line))
                    {
                        if (lineCount >= maxLinesCount)
                        {
                            break;
                        }

                        if (line == null)
                        {
                            dropCount++;
                            continue;
                        }

                        LogLine logLine = new()
                        {
                            FullLine = line,
                            LineNumber = logBuffer.StartLine + logBuffer.LineCount
                        };

                        logBuffer.AddLine(logLine, filePos);
                        filePos = reader.Position;
                        lineCount++;
                    }

                    if (maxLinesCount != logBuffer.LineCount)
                    {
                        _logger.Warn("LineCount in buffer differs after re-reading. old={0}, new={1}", maxLinesCount, logBuffer.LineCount);
                    }

                    if (dropCount - logBuffer.PrevBuffersDroppedLinesSum != logBuffer.DroppedLinesCount)
                    {
                        _logger.Warn("DroppedLinesCount in buffer differs after re-reading. old={0}, new={1}", logBuffer.DroppedLinesCount, dropCount);
                        logBuffer.DroppedLinesCount = dropCount - logBuffer.PrevBuffersDroppedLinesSum;
                    }

                    GC.KeepAlive(fileStream);
                }
                catch (IOException e)
                {
                    _logger.Warn(e);
                }
                finally
                {
                    fileStream.Close();
                }
            }
            finally
            {
                Monitor.Exit(logBuffer);
            }
        }

        private LogBuffer GetBufferForLine(int lineNum)
        {
#if DEBUG
            long startTime = Environment.TickCount;
#endif
            LogBuffer logBuffer = null;
            AcquireBufferListReaderLock();
            //if (lineNum == this.lastReturnedLineNumForBuffer)
            //{
            //  return this.lastReturnedBuffer;
            //}

            //int startIndex = lineNum / LogBuffer.MAX_LINES;  // doesn't work anymore since XML buffer may contain more lines than MAX_LINES
            int startIndex = 0;
            int count = _bufferList.Count;
            for (int i = startIndex; i < count; ++i)
            {
                logBuffer = _bufferList[i];
                if (lineNum >= logBuffer.StartLine && lineNum < logBuffer.StartLine + logBuffer.LineCount)
                {
                    //UpdateLru(logBuffer);
                    UpdateLruCache(logBuffer);
                    //this.lastReturnedLineNumForBuffer = lineNum;
                    //this.lastReturnedBuffer = logBuffer;
                    break;
                }
            }
#if DEBUG
            long endTime = Environment.TickCount;
            //_logger.logDebug("getBufferForLine(" + lineNum + ") duration: " + ((endTime - startTime)) + " ms. Buffer start line: " + logBuffer.StartLine);
#endif
            ReleaseBufferListReaderLock();
            return logBuffer;
        }

        /// <summary>
        /// Async callback used to check if the GetLogLine() call is succeeding again after a detected timeout.
        /// </summary>
        private void GetLineFinishedCallback(ILogLine line)
        {
            _isFailModeCheckCallPending = false;
            if (line != null)
            {
                _logger.Debug("'isFastFailOnGetLogLine' flag was reset");
                _isFastFailOnGetLogLine = false;
            }

            _logger.Debug("'isLogLineCallPending' flag was reset.");
        }

        private LogBuffer GetFirstBufferForFileByLogBuffer(LogBuffer logBuffer)
        {
            ILogFileInfo info = logBuffer.FileInfo;
            AcquireBufferListReaderLock();
            int index = _bufferList.IndexOf(logBuffer);
            if (index == -1)
            {
                ReleaseBufferListReaderLock();
                return null;
            }

            LogBuffer resultBuffer = logBuffer;
            while (true)
            {
                index--;
                if (index < 0 || _bufferList[index].FileInfo != info)
                {
                    break;
                }

                resultBuffer = _bufferList[index];
            }

            ReleaseBufferListReaderLock();
            return resultBuffer;
        }

        private void MonitorThreadProc()
        {
            Thread.CurrentThread.Name = "MonitorThread";
            //IFileSystemPlugin fs = PluginRegistry.GetInstance().FindFileSystemForUri(this.watchedILogFileInfo.FullName);
            _logger.Info("MonitorThreadProc() for file {0}", _watchedILogFileInfo.FullName);

            long oldSize = 0;
            try
            {
                OnLoadingStarted(new LoadFileEventArgs(_fileName, 0, false, 0, false));
                ReadFiles();
                if (!_isDeleted)
                {
                    oldSize = _fileLength;
                    OnLoadingFinished();
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
                    int pollInterval = _watchedILogFileInfo.PollInterval;
                    //#if DEBUG
                    //          if (_logger.IsDebug)
                    //          {
                    //            _logger.logDebug("Poll interval for " + this.fileName + ": " + pollInterval);
                    //          }
                    //#endif
                    Thread.Sleep(pollInterval);
                }
                catch (Exception e)
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
                            oldSize = _fileLength;
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

        private void MonitoredFileNotFound()
        {
            long oldSize;
            if (!_isDeleted)
            {
                _logger.Debug("File not FileNotFoundException catched. Switching to 'deleted' mode.");
                _isDeleted = true;
                oldSize = _fileLength = -1;
                FileSize = 0;
                OnFileNotFound(); // notify LogWindow
            }
#if DEBUG
            else
            {
                _logger.Debug("File not FileNotFoundException catched. Already in deleted mode.");
            }
#endif
        }

        private void FileChanged()
        {
            if (_isDeleted)
            {
                OnRespawned();
                // prevent size update events. The window should reload the complete file.
                FileSize = _fileLength;
            }

            long newSize = _fileLength;
            //if (this.currFileSize != newSize)
            {
                _logger.Info("file size changed. new size={0}, file: {1}", newSize, _fileName);
                FireChangeEvent();
            }
        }

        private void FireChangeEvent()
        {
            LogEventArgs args = new();
            args.PrevFileSize = FileSize;
            args.PrevLineCount = LineCount;
            long newSize = _fileLength;
            if (newSize < FileSize || _isDeleted)
            {
                _logger.Info("File was created anew: new size={0}, oldSize={1}", newSize, FileSize);
                // Fire "New File" event
                FileSize = 0;
                LineCount = 0;
                try
                {
                    if (!IsMultiFile)
                    {
                        // ReloadBufferList();  // removed because reloading is triggered by owning LogWindow
                        // Trigger "new file" handling (reload)
                        OnLoadFile(new LoadFileEventArgs(_fileName, 0, true, _fileLength, true));

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
                    else
                    {
                        int offset = ShiftBuffers();
                        //this.currFileSize = newSize;    // removed because ShiftBuffers() calls ReadToBuffer() which will set the actual read size
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

        private ILogStreamReader GetLogStreamReader(Stream stream, EncodingOptions encodingOptions, bool useNewReader)
        {
            ILogStreamReader reader = CreateLogStreamReader(stream, encodingOptions, useNewReader);

            return IsXmlMode ? new XmlBlockSplitter(new XmlLogReader(reader), XmlLogConfig) : reader;
        }

        private ILogStreamReader CreateLogStreamReader(Stream stream, EncodingOptions encodingOptions, bool useSystemReader)
        {
            if (useSystemReader)
            {
                return new PositionAwareStreamReaderSystem(stream, encodingOptions);
            }

            return new PositionAwareStreamReaderLegacy(stream, encodingOptions);
        }

        private bool ReadLine(ILogStreamReader reader, int lineNum, int realLineNum, out string outLine)
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

        private void AcquireBufferListReaderLock()
        {
            try
            {
                _bufferListLock.AcquireReaderLock(10000);
#if DEBUG && TRACE_LOCKS
        StackTrace st = new StackTrace(true);
        StackFrame callerFrame = st.GetFrame(2);
        this.bufferListLockInfo =
"Read lock from " + callerFrame.GetMethod().DeclaringType.Name + "." + callerFrame.GetMethod().Name + "() " + callerFrame.GetFileLineNumber();
#endif
            }
            catch (ApplicationException e)
            {
                _logger.Warn(e, "Reader lock wait for bufferList timed out. Now trying infinite.");
#if DEBUG && TRACE_LOCKS
        _logger.logInfo(this.bufferListLockInfo);
#endif
                _bufferListLock.AcquireReaderLock(Timeout.Infinite);
            }
        }

        private void ReleaseBufferListReaderLock()
        {
            _bufferListLock.ReleaseReaderLock();
        }

        private void AcquireBufferListWriterLock()
        {
            try
            {
                _bufferListLock.AcquireWriterLock(10000);
#if DEBUG && TRACE_LOCKS
        StackTrace st = new StackTrace(true);
        StackFrame callerFrame = st.GetFrame(1);
        this.bufferListLockInfo =
"Write lock from " + callerFrame.GetMethod().DeclaringType.Name + "." + callerFrame.GetMethod().Name + "() " + callerFrame.GetFileLineNumber();
        callerFrame.GetFileName();
#endif
            }
            catch (ApplicationException e)
            {
                _logger.Warn(e, "Writer lock wait for bufferList timed out. Now trying infinite.");
#if DEBUG && TRACE_LOCKS
        _logger.logInfo(this.bufferListLockInfo);
#endif
                _bufferListLock.AcquireWriterLock(Timeout.Infinite);
            }
        }

        private void ReleaseBufferListWriterLock()
        {
            _bufferListLock.ReleaseWriterLock();
        }

        private LockCookie UpgradeBufferListLockToWriter()
        {
            try
            {
                LockCookie cookie = _bufferListLock.UpgradeToWriterLock(10000);
#if DEBUG && TRACE_LOCKS
        StackTrace st = new StackTrace(true);
        StackFrame callerFrame = st.GetFrame(2);
        this.bufferListLockInfo +=
", upgraded to writer from " + callerFrame.GetMethod().DeclaringType.Name + "." + callerFrame.GetMethod().Name + "() " + callerFrame.GetFileLineNumber();
#endif
                return cookie;
            }
            catch (ApplicationException e)
            {
                _logger.Warn(e, "Writer lock update wait for bufferList timed out. Now trying infinite.");
#if DEBUG && TRACE_LOCKS
        _logger.logInfo(this.bufferListLockInfo);
#endif
                return _bufferListLock.UpgradeToWriterLock(Timeout.Infinite);
            }
        }

        private void DowngradeBufferListLockFromWriter(ref LockCookie cookie)
        {
            _bufferListLock.DowngradeFromWriterLock(ref cookie);
#if DEBUG && TRACE_LOCKS
      StackTrace st = new StackTrace(true);
      StackFrame callerFrame = st.GetFrame(2);
      this.bufferListLockInfo +=
", downgraded to reader from " + callerFrame.GetMethod().DeclaringType.Name + "." + callerFrame.GetMethod().Name + "() " + callerFrame.GetFileLineNumber();
#endif
        }

#if DEBUG
        private void DumpBufferInfos(LogBuffer buffer)
        {
            if (_logger.IsTraceEnabled)
            {
                _logger.Trace("StartLine: {0}\r\nLineCount: {1}\r\nStartPos: {2}\r\nSize: {3}\r\nDisposed: {4}\r\nDisposeCount: {5}\r\nFile: {6}",
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

        ~LogfileReader()
        {
            DeleteAllContent();
        }

        protected virtual void OnFileSizeChanged(LogEventArgs e)
        {
            FileSizeChanged?.Invoke(this, e);
        }

        protected virtual void OnLoadFile(LoadFileEventArgs e)
        {
            LoadFile?.Invoke(this, e);
        }

        protected virtual void OnLoadingStarted(LoadFileEventArgs e)
        {
            LoadingStarted?.Invoke(this, e);
        }

        protected virtual void OnLoadingFinished()
        {
            LoadingFinished?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnFileNotFound()
        {
            FileNotFound?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnRespawned()
        {
            _logger.Info("OnRespawned()");
            Respawned?.Invoke(this, EventArgs.Empty);
        }

        private class LogLine : ILogLine
        {
            #region Properties

            public string FullLine { get; set; }

            public int LineNumber { get; set; }

            string ITextValue.Text => FullLine;

            #endregion
        }
    }
}