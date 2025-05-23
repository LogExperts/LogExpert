using NLog;

namespace LogExpert.PluginRegistry.FileSystem
{
    public class LogFileInfo : ILogFileInfo
    {
        #region Fields

        private const int RETRY_COUNT = 5;
        private const int RETRY_SLEEP = 250;
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        //FileStream fStream;
        private readonly FileInfo fInfo;

        private long lastLength;

        #endregion

        #region cTor

        public LogFileInfo(Uri fileUri)
        {
            fInfo = new FileInfo(fileUri.LocalPath);
            Uri = fileUri;
            OriginalLength = lastLength = LengthWithoutRetry;
            //this.oldLength = 0;
        }

        #endregion

        #region Properties

        public string FullName
        {
            get { return fInfo.FullName; }
        }

        public string FileName
        {
            get { return fInfo.Name; }
        }


        public string DirectoryName
        {
            get { return fInfo.DirectoryName; }
        }

        public char DirectorySeparatorChar
        {
            get { return Path.DirectorySeparatorChar; }
        }

        public Uri Uri { get; }

        public long Length
        {
            get
            {
                if (fInfo == null)
                {
                    return -1;
                }

                int retry = RETRY_COUNT;

                while (retry > 0)
                {
                    try
                    {
                        fInfo.Refresh();
                        return fInfo.Length;
                    }
                    catch (IOException e)
                    {
                        if (--retry <= 0)
                        {
                            _logger.Warn(e, "LogFileInfo.Length");
                            return -1;
                        }
                        Thread.Sleep(RETRY_SLEEP);
                    }
                }

                return -1;
            }
        }

        public long OriginalLength { get; }

        public bool FileExists
        {
            get
            {
                fInfo.Refresh();
                return fInfo.Exists;
            }
        }

        //TODO this should be set from outside once
        public int PollInterval => PluginRegistry.PollingInterval;

        public long LengthWithoutRetry
        {
            get
            {
                if (fInfo == null)
                {
                    return -1;
                }
                try
                {
                    fInfo.Refresh();
                    return fInfo.Length;
                }
                catch (IOException)
                {
                    return -1;
                }
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Creates a new FileStream for the file. The caller is responsible for closing.
        /// If file opening fails it will be tried RETRY_COUNT times. This may be needed sometimes
        /// if the file is locked for a short amount of time or temporarly unaccessible because of
        /// rollover situations.
        /// </summary>
        /// <returns></returns>
        public Stream OpenStream()
        {
            int retry = RETRY_COUNT;

            while (true)
            {
                try
                {
                    return new FileStream(fInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                }
                catch (IOException fe)
                {
                    _logger.Debug(fe, "LogFileInfo.OpenFile(): \r\nRetry counter {0}", retry);
                    if (--retry <= 0)
                    {
                        throw;
                    }
                    Thread.Sleep(RETRY_SLEEP);
                }
                catch (UnauthorizedAccessException uae)
                {
                    _logger.Debug(uae, "LogFileInfo.OpenFile(): \r\nRetry counter: {0}", retry);
                    if (--retry <= 0)
                    {
                        throw new IOException("Error opening file", uae);
                    }
                    Thread.Sleep(RETRY_SLEEP);
                }
            }
        }

        //TODO Replace with Event from FileSystemWatcher
        public bool FileHasChanged()
        {
            if (LengthWithoutRetry != lastLength)
            {
                lastLength = LengthWithoutRetry;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return fInfo.FullName + ", OldLen: " + OriginalLength + ", Len: " + Length;
        }

        #endregion
    }
}