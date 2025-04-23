using LogExpert.Entities;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading;

namespace LogExpert.Classes.Log
{
    public class LruCacheManager
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly int _maxBuffers;
        private readonly ReaderWriterLockSlim _lruCacheDictLock;
        private readonly ReaderWriterLockSlim _disposeLock;
        private Dictionary<int, LogBufferCacheEntry> _lruCacheDict;

        public ReaderWriterLockSlim Lock { get { return _lruCacheDictLock; }
}

public int Count
        {
            get
            {
                _lruCacheDictLock.EnterReadLock();
                try
                {
                    return _lruCacheDict.Count;
                }
                finally
                {
                    _lruCacheDictLock.ExitReadLock();
                }
            }
        }

        public LruCacheManager(int maxBuffers)
        {
            _maxBuffers = maxBuffers;
            _lruCacheDictLock = new ReaderWriterLockSlim();
            _disposeLock = new ReaderWriterLockSlim();
            _lruCacheDict = new Dictionary<int, LogBufferCacheEntry>(_maxBuffers + 1);
        }

        public void UpdateLruCache(LogBuffer logBuffer)
        {
            _lruCacheDictLock.EnterUpgradeableReadLock();
            try
            {
                if (_lruCacheDict.TryGetValue(logBuffer.StartLine, out LogBufferCacheEntry cacheEntry))
                {
                    cacheEntry.Touch();
                }
                else
                {
                    _lruCacheDictLock.EnterWriteLock();
                    try
                    {
                        if (!_lruCacheDict.TryGetValue(logBuffer.StartLine, out cacheEntry))
                        {
                            cacheEntry = new LogBufferCacheEntry { LogBuffer = logBuffer };
                            _lruCacheDict.Add(logBuffer.StartLine, cacheEntry);
                        }
                    }
                    finally
                    {
                        _lruCacheDictLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lruCacheDictLock.ExitUpgradeableReadLock();
            }
        }

        public void ClearLru()
        {
            _logger.Info("Clearing LRU cache.");
            _lruCacheDictLock.EnterWriteLock();
            _disposeLock.EnterWriteLock();
            try
            {
                foreach (LogBufferCacheEntry entry in _lruCacheDict.Values)
                {
                    entry.LogBuffer.DisposeContent();
                }

                _lruCacheDict.Clear();
            }
            finally
            {
                _disposeLock.ExitWriteLock();
                _lruCacheDictLock.ExitWriteLock();
            }
            _logger.Info("Clearing done.");
        }

        public void GarbageCollectLruCache()
        {
#if DEBUG
            long startTime = Environment.TickCount;
#endif
            _logger.Debug("Starting garbage collection");
            int threshold = 10;
            _lruCacheDictLock.EnterWriteLock();
            try
            {
                int diff = _lruCacheDict.Count - _maxBuffers;
                if (diff > threshold)
                {
#if DEBUG
                    _logger.Info("Removing {0} entries from LRU cache", diff);
#endif
                    SortedList<long, int> useSorterList = new();
                    foreach (LogBufferCacheEntry entry in _lruCacheDict.Values)
                    {
                        if (!useSorterList.ContainsKey(entry.LastUseTimeStamp))
                        {
                            useSorterList.Add(entry.LastUseTimeStamp, entry.LogBuffer.StartLine);
                        }
                    }

                    _disposeLock.EnterWriteLock();
                    try
                    {
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
                    }
                    finally
                    {
                        _disposeLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lruCacheDictLock.ExitWriteLock();
            }
#if DEBUG
            long endTime = Environment.TickCount;
            _logger.Info("Garbage collector time: " + (endTime - startTime) + " ms.");
#endif
        }

        public void SetNewStartLineForBuffer(LogBuffer logBuffer, int newLineNum)
        {
            Util.AssertTrue(_lruCacheDictLock.IsWriteLockHeld, "No writer lock for lru cache");
            if (_lruCacheDict.ContainsKey(logBuffer.StartLine))
            {
                _lruCacheDict.Remove(logBuffer.StartLine);
                logBuffer.StartLine = newLineNum;
                LogBufferCacheEntry cacheEntry = new() { LogBuffer = logBuffer };
                _lruCacheDict.Add(logBuffer.StartLine, cacheEntry);
            }
            else
            {
                logBuffer.StartLine = newLineNum;
            }
        }

        internal void RemoveBuffers(IList<LogBuffer> deleteList)
        {
            _lruCacheDictLock.EnterWriteLock();
            try
            {
                foreach (LogBuffer buffer in deleteList)
                {
                    _lruCacheDict.Remove(buffer.StartLine);
                }
            }
            finally
            {
                _lruCacheDictLock.ExitWriteLock();
            }
        }
    }
}
