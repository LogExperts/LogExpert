using ColumnizerLib;

using LogExpert.Core.Classes.Log.Buffers;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Buffers;

/// <summary>
/// Unit tests for the extracted <see cref="BufferIndex"/> class. Uses in-memory LogBuffers with a fake ILogFileInfo —
/// no file I/O.
/// </summary>
[TestFixture]
internal class BufferIndexTests : IDisposable
{
    private const int MAX_BUFFERS = 50;
    private const int LINES_PER_BUFFER = 500;

    private Mock<ILogFileInfo> _fakeFileInfo = null!;
    private Mock<ILogFileInfo> _fakeFileInfo2 = null!;
    private BufferIndex _index = null!;

    private bool _disposed;

    [SetUp]
    public void SetUp ()
    {
        _fakeFileInfo = new Mock<ILogFileInfo>();
        _ = _fakeFileInfo.Setup(f => f.FullName).Returns("fake1.log");
        _ = _fakeFileInfo.Setup(f => f.FileName).Returns("fake1.log");

        _fakeFileInfo2 = new Mock<ILogFileInfo>();
        _ = _fakeFileInfo2.Setup(f => f.FullName).Returns("fake2.log");
        _ = _fakeFileInfo2.Setup(f => f.FileName).Returns("fake2.log");

        _index = new BufferIndex(MAX_BUFFERS, LINES_PER_BUFFER);
    }

    [TearDown]
    public void TearDown ()
    {
        _index.Dispose();
    }

    #region IDisposable Implementation

    public void Dispose ()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _index?.Dispose();
        }

        _disposed = true;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a LogBuffer with the given startLine and lineCount, populated with dummy LogLines.
    /// </summary>
    private LogBuffer CreateBuffer (int startLine, int lineCount, ILogFileInfo? fileInfo = null)
    {
        var info = fileInfo ?? _fakeFileInfo.Object;
        var buffer = new LogBuffer(info, LINES_PER_BUFFER)
        {
            StartLine = startLine,
            StartPos = startLine * 100
        };

        for (var j = 0; j < lineCount; j++)
        {
            buffer.AddLine(new LogLine($"line {startLine + j}".AsMemory(), startLine + j), (startLine + j) * 100);
        }

        buffer.Size = lineCount * 100;
        return buffer;
    }

    /// <summary>
    /// Populates the index with <paramref name="count"/> uniform buffers of <paramref name="linesPerBuffer"/> lines.
    /// </summary>
    private void PopulateUniform (int count, int linesPerBuffer = LINES_PER_BUFFER, ILogFileInfo? fileInfo = null)
    {
        using var w = _index.AcquireWriteLock();
        for (var i = 0; i < count; i++)
        {
            _index.Add(CreateBuffer(i * linesPerBuffer, linesPerBuffer, fileInfo));
        }
    }

    #endregion

    #region Lookup — 4-layer strategy

    [Test]
    public void TryFindBuffer_EmptyIndex_ReturnsFalse ()
    {
        using var r = _index.AcquireReadLock();
        Assert.That(_index.TryFindBuffer(0).Found, Is.False);
    }

    [Test]
    public void TryFindBuffer_SingleBuffer_FindsLine ()
    {
        PopulateUniform(1, 10);

        using var r = _index.AcquireReadLock();
        Assert.That(_index.TryFindBuffer(0).Found, Is.True);
        Assert.That(_index.TryFindBuffer(0).Buffer, Is.Not.Null);
        Assert.That(_index.TryFindBuffer(0).Buffer!.StartLine, Is.EqualTo(0));
        Assert.That(_index.TryFindBuffer(0).Buffer!.LineCount, Is.EqualTo(10));
    }

    [Test]
    public void TryFindBuffer_OutOfRange_ReturnsFalse ()
    {
        PopulateUniform(2, 10); // lines 0–19

        using var r = _index.AcquireReadLock();
        Assert.That(_index.TryFindBuffer(20).Found, Is.False);
        Assert.That(_index.TryFindBuffer(-1).Found, Is.False);
    }

    [TestCase(0, Description = "First line — Layer 0 or direct map")]
    [TestCase(499, Description = "Last line of first buffer")]
    [TestCase(500, Description = "First line of second buffer — boundary")]
    [TestCase(4999, Description = "Last line overall")]
    public void TryFindBuffer_VariousLines_FindsCorrectBuffer (int lineNum)
    {
        PopulateUniform(10); // 10 buffers × 500 lines = 5000 lines

        using var r = _index.AcquireReadLock();
        Assert.That(_index.TryFindBuffer(lineNum).Found, Is.True);
        Assert.That(_index.TryFindBuffer(lineNum).Buffer, Is.Not.Null);
        Assert.That(lineNum, Is.GreaterThanOrEqualTo(_index.TryFindBuffer(lineNum).Buffer!.StartLine));
        Assert.That(lineNum, Is.LessThan(_index.TryFindBuffer(lineNum).Buffer!.StartLine + _index.TryFindBuffer(lineNum).Buffer!.LineCount));
    }

    [Test]
    public void TryFindBuffer_SequentialAccess_HitsThreadLocalCache ()
    {
        PopulateUniform(5, 100); // 500 lines

        using var r = _index.AcquireReadLock();
        // First call sets thread-local cache
        Assert.That(_index.TryFindBuffer(50).Found, Is.True);
        // Second call within same buffer should hit Layer 0
        Assert.That(_index.TryFindBuffer(60).Found, Is.True);
        Assert.That(_index.TryFindBuffer(60).Buffer!.StartLine, Is.EqualTo(0));
    }

    [Test]
    public void TryFindBuffer_AdjacentForward_FindsNextBuffer ()
    {
        PopulateUniform(3, 100); // lines 0–299

        using var r = _index.AcquireReadLock();
        // Prime thread-local to buffer 0
        Assert.That(_index.TryFindBuffer(50).Found, Is.True);
        // Cross boundary into buffer 1 — Layer 1 adjacent prediction
        Assert.That(_index.TryFindBuffer(100).Found, Is.True);
        Assert.That(_index.TryFindBuffer(100).Buffer!.StartLine, Is.EqualTo(100));
    }

    [Test]
    public void TryFindBuffer_AdjacentBackward_FindsPrevBuffer ()
    {
        PopulateUniform(3, 100); // lines 0–299

        using var r = _index.AcquireReadLock();
        // Prime thread-local to buffer 1
        Assert.That(_index.TryFindBuffer(150).Found, Is.True);
        // Cross backward into buffer 0
        Assert.That(_index.TryFindBuffer(50).Found, Is.True);
        Assert.That(_index.TryFindBuffer(50).Buffer!.StartLine, Is.EqualTo(0));
    }

    [Test]
    public void TryFindBuffer_RandomStride_FindsAllLines ()
    {
        PopulateUniform(20, 100); // 2000 lines

        using var r = _index.AcquireReadLock();
        // Co-prime stride exercises Layers 2 and 3
        var stride = 701;
        var lineNum = 0;
        for (var i = 0; i < 2000; i++)
        {
            lineNum = (lineNum + stride) % 2000;
            var logBufferEntry = _index.TryFindBuffer(lineNum);
            Assert.That(logBufferEntry.Found, Is.True,
                $"Failed to find buffer for line {lineNum}");
            Assert.That(lineNum, Is.GreaterThanOrEqualTo(logBufferEntry.Buffer!.StartLine));
            Assert.That(lineNum, Is.LessThan(logBufferEntry.Buffer.StartLine + logBufferEntry.Buffer.LineCount));
        }
    }

    #endregion

    #region GetBufferForLineWithIndex

    [Test]
    public void GetBufferForLineWithIndex_ReturnsBufferAndPositionalIndex ()
    {
        PopulateUniform(3, 100);

        using var r = _index.AcquireReadLock();
        var logBufferEntry = _index.GetBufferForLineWithIndex(150);
        Assert.That(logBufferEntry.Buffer, Is.Not.Null);
        Assert.That(logBufferEntry.Buffer!.StartLine, Is.EqualTo(100));
        Assert.That(logBufferEntry.Index, Is.EqualTo(1));
    }

    [Test]
    public void GetBufferForLineWithIndex_OutOfRange_ReturnsNull ()
    {
        PopulateUniform(1, 10);

        using var r = _index.AcquireReadLock();
        var logBufferEntry = _index.GetBufferForLineWithIndex(999);
        Assert.That(logBufferEntry.Buffer, Is.Null);
        Assert.That(logBufferEntry.Index, Is.EqualTo(-1));
    }

    #endregion

    #region Mutation — Add / Remove / UpdateStartLine / Clear

    [Test]
    public void Add_IncreasesBufferCount ()
    {
        using var w = _index.AcquireWriteLock();
        Assert.That(_index.BufferCount, Is.EqualTo(0));

        _index.Add(CreateBuffer(0, 10));
        Assert.That(_index.BufferCount, Is.EqualTo(1));

        _index.Add(CreateBuffer(10, 10));
        Assert.That(_index.BufferCount, Is.EqualTo(2));
    }

    [Test]
    public void Remove_DecreasesBufferCount ()
    {
        var buf = CreateBuffer(0, 10);
        using var w = _index.AcquireWriteLock();
        _index.Add(buf);
        Assert.That(_index.BufferCount, Is.EqualTo(1));

        var removed = _index.Remove(buf);
        Assert.That(removed, Is.True);
        Assert.That(_index.BufferCount, Is.EqualTo(0));
    }

    [Test]
    public void Remove_NonExistentBuffer_ReturnsFalse ()
    {
        PopulateUniform(1, 10);
        var orphan = CreateBuffer(9999, 10);

        using var w = _index.AcquireWriteLock();
        Assert.That(_index.Remove(orphan), Is.False);
    }

    [Test]
    public void UpdateStartLine_MovesBuffer ()
    {
        PopulateUniform(3, 100); // 0, 100, 200

        using var w = _index.AcquireWriteLock();
        var buf = _index.GetBufferAt(1); // startLine=100
        Assert.That(buf.StartLine, Is.EqualTo(100));

        _index.UpdateStartLine(buf, 50);
        Assert.That(buf.StartLine, Is.EqualTo(50));

        // Old key gone — no buffer starts at 100 anymore
        Assert.That(_index.TryFindBuffer(100).Buffer?.StartLine, Is.Not.EqualTo(100));
        // New key present — buffer is reachable via its new start line
        Assert.That(_index.TryFindBuffer(50).Found, Is.True);
        Assert.That(_index.TryFindBuffer(50).Buffer, Is.SameAs(buf));
    }

    [Test]
    public void Clear_RemovesAll ()
    {
        PopulateUniform(5, 100);

        using var w = _index.AcquireWriteLock();
        Assert.That(_index.BufferCount, Is.EqualTo(5));

        _index.Clear();
        Assert.That(_index.BufferCount, Is.EqualTo(0));
        Assert.That(_index.TotalLineCount, Is.EqualTo(0));
        Assert.That(_index.LruCacheCount, Is.EqualTo(0));
    }

    #endregion

    #region TotalLineCount - dirty/clean caching

    [Test]
    public void TotalLineCount_ReflectsBufferContents ()
    {
        PopulateUniform(3, 100);

        using var r = _index.AcquireReadLock();
        Assert.That(_index.TotalLineCount, Is.EqualTo(300));
    }

    [Test]
    public void TotalLineCount_AfterMarkDirty_Recalculates ()
    {
        PopulateUniform(2, 100);

        using var r = _index.AcquireReadLock();
        var first = _index.TotalLineCount;
        Assert.That(first, Is.EqualTo(200));

        // Mark dirty and re-read — should recalculate to same value
        _index.MarkLineCountDirty();
        var second = _index.TotalLineCount;
        Assert.That(second, Is.EqualTo(200));
    }

    #endregion

    #region Multi-file navigation

    [Test]
    public void TryGetNextFileStartLine_TwoFiles_FindsBoundary ()
    {
        // File1: buffers at 0, 100. File2: buffers at 200, 300.
        using var w = _index.AcquireWriteLock();
        _index.Add(CreateBuffer(0, 100, _fakeFileInfo.Object));
        _index.Add(CreateBuffer(100, 100, _fakeFileInfo.Object));
        _index.Add(CreateBuffer(200, 100, _fakeFileInfo2.Object));
        _index.Add(CreateBuffer(300, 100, _fakeFileInfo2.Object));

        Assert.That(_index.TryGetNextFileStartLine(50).Found, Is.True);
        Assert.That(_index.TryGetNextFileStartLine(50).StartLine, Is.EqualTo(200));
    }

    [Test]
    public void TryGetNextFileStartLine_LastFile_ReturnsFalse ()
    {
        using var w = _index.AcquireWriteLock();
        _index.Add(CreateBuffer(0, 100, _fakeFileInfo.Object));
        _index.Add(CreateBuffer(100, 100, _fakeFileInfo2.Object));

        // Line in second (last) file — no next file
        Assert.That(_index.TryGetNextFileStartLine(150).Found, Is.False);
    }

    [Test]
    public void TryGetPrevFileStartLine_TwoFiles_FindsBoundary ()
    {
        using var w = _index.AcquireWriteLock();
        _index.Add(CreateBuffer(0, 100, _fakeFileInfo.Object));
        _index.Add(CreateBuffer(100, 100, _fakeFileInfo.Object));
        _index.Add(CreateBuffer(200, 100, _fakeFileInfo2.Object));
        _index.Add(CreateBuffer(300, 100, _fakeFileInfo2.Object));

        // Line 250 is in file2 — prev file ends at line 200 (startLine + lineCount of last file1 buffer)
        Assert.That(_index.TryGetPrevFileStartLine(250).Found, Is.True);
        Assert.That(_index.TryGetPrevFileStartLine(250).StartLine, Is.EqualTo(200));
    }

    [Test]
    public void TryGetPrevFileStartLine_FirstFile_ReturnsFalse ()
    {
        using var w = _index.AcquireWriteLock();
        _index.Add(CreateBuffer(0, 100, _fakeFileInfo.Object));
        _index.Add(CreateBuffer(100, 100, _fakeFileInfo2.Object));

        Assert.That(_index.TryGetPrevFileStartLine(50).Found, Is.False);
    }

    [Test]
    public void GetFirstBufferForFile_ReturnsEarliestBuffer ()
    {
        using var w = _index.AcquireWriteLock();
        _index.Add(CreateBuffer(0, 100, _fakeFileInfo.Object));
        _index.Add(CreateBuffer(100, 100, _fakeFileInfo.Object));
        _index.Add(CreateBuffer(200, 100, _fakeFileInfo2.Object));

        var logBufferEntry = _index.GetBufferForLineWithIndex(150);
        var first = _index.GetFirstBufferForFile(logBufferEntry.Buffer!, logBufferEntry.Index);
        Assert.That(first!.StartLine, Is.EqualTo(0));
    }

    #endregion

    #region LRU Eviction

    [Test]
    public void EvictLeastRecentlyUsed_BelowThreshold_DoesNothing ()
    {
        PopulateUniform(5, 10); // well below MaxBuffers=50

        using var r = _index.AcquireReadLock();
        // Touch all buffers
        for (var i = 0; i < 50; i += 10)
        {
            _ = _index.TryFindBuffer(i);
        }

        _index.EvictLeastRecentlyUsed();

        // All buffers still have content
        for (var i = 0; i < 5; i++)
        {
            Assert.That(_index.GetBufferAt(i).IsDisposed, Is.False);
        }
    }

    [Test]
    public void EvictLeastRecentlyUsed_AboveThreshold_EvictsOldest ()
    {
        // Use a small maxBuffers so we can exceed it easily
        _index.Dispose();
        _index = new BufferIndex(maxBuffers: 3, maxLinesPerBuffer: 10);

        using var w = _index.AcquireWriteLock();
        // Add 20 buffers (way above maxBuffers=3)
        for (var i = 0; i < 20; i++)
        {
            _index.Add(CreateBuffer(i * 10, 10));
        }

        // Touch only the last 3 buffers (make them "recent")
        _ = _index.TryFindBuffer(170);
        _ = _index.TryFindBuffer(180);
        _ = _index.TryFindBuffer(190);

        _index.EvictLeastRecentlyUsed();

        // LRU cache should be reduced toward maxBuffers
        Assert.That(_index.LruCacheCount, Is.LessThanOrEqualTo(3 + 10));
    }

    #endregion

    #region ClearLru

    [Test]
    public void ClearLru_ClearsIndexBeforeReturningToPool ()
    {
        var pool = new LogBufferPool(100);

        using var w = _index.AcquireWriteLock();
        _index.Add(CreateBuffer(0, 10));
        _index.Add(CreateBuffer(10, 10));

        // Touch buffers to populate LRU
        _ = _index.TryFindBuffer(0);
        _ = _index.TryFindBuffer(10);

        Assert.That(_index.BufferCount, Is.EqualTo(2));
        Assert.That(_index.LruCacheCount, Is.GreaterThan(0));

        _index.ClearLru(pool);

        // Index is empty — this is the bug fix: index clears FIRST
        Assert.That(_index.BufferCount, Is.EqualTo(0));
        Assert.That(_index.LruCacheCount, Is.EqualTo(0));
        Assert.That(_index.TotalLineCount, Is.EqualTo(0));

        // Verify no lookup succeeds after ClearLru (prevents stale reference bug)
        Assert.That(_index.TryFindBuffer(0).Found, Is.False);
        Assert.That(_index.TryFindBuffer(10).Found, Is.False);
    }

    [Test]
    public void ClearLru_BuffersReturnedToPool ()
    {
        var pool = new LogBufferPool(100);

        using var w = _index.AcquireWriteLock();
        var buf1 = CreateBuffer(0, 10);
        var buf2 = CreateBuffer(10, 10);
        _index.Add(buf1);
        _index.Add(buf2);

        // Touch to populate LRU
        _ = _index.TryFindBuffer(0);
        _ = _index.TryFindBuffer(10);

        _index.ClearLru(pool);

        // Buffers should be disposed (returned to pool disposes content)
        Assert.That(buf1.IsDisposed, Is.True);
        Assert.That(buf2.IsDisposed, Is.True);
    }

    #endregion

    #region SnapShot

    [Test]
    public void CreateSnapshot_CapturesCurrentState ()
    {
        PopulateUniform(3, 100);

        var snapshot = _index.CreateSnapshot();
        Assert.That(snapshot.BufferCount, Is.EqualTo(3));
        Assert.That(snapshot.TotalLineCount, Is.EqualTo(300));
        Assert.That(snapshot.Buffers, Has.Count.EqualTo(3));

        Assert.That(snapshot.Buffers[0].StartLine, Is.EqualTo(0));
        Assert.That(snapshot.Buffers[0].LineCount, Is.EqualTo(100));
        Assert.That(snapshot.Buffers[0].FileName, Is.EqualTo("fake1.log"));

        Assert.That(snapshot.Buffers[1].StartLine, Is.EqualTo(100));
        Assert.That(snapshot.Buffers[2].StartLine, Is.EqualTo(200));
    }

    [Test]
    public void CreateSnapshot_ImmutableAfterModification ()
    {
        PopulateUniform(2, 100);

        var snapshot = _index.CreateSnapshot();
        Assert.That(snapshot.BufferCount, Is.EqualTo(2));

        // Add more buffers after snapshot
        using var w = _index.AcquireWriteLock();
        _index.Add(CreateBuffer(200, 100));

        // Snapshot is unchanged
        Assert.That(snapshot.BufferCount, Is.EqualTo(2));
        Assert.That(snapshot.Buffers, Has.Count.EqualTo(2));
    }

    #endregion

    #region LockScopeTests

    [Test]
    public void UpgradeableReadLock_CanUpgradeToWrite ()
    {
        using var upgradeable = _index.AcquireUpgradeableReadLock();
        _index.Add(CreateBuffer(0, 10)); // Add requires at least upgradeable

        using (upgradeable.UpgradeToWrite())
        {
            _index.Add(CreateBuffer(10, 10));
        }

        // Back to upgradeable-read
        Assert.That(_index.BufferCount, Is.EqualTo(2));
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes ()
    {
        _index.Dispose();
        Assert.DoesNotThrow(_index.Dispose);
    }

    #endregion
}