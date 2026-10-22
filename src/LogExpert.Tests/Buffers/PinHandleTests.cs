using ColumnizerLib;

using LogExpert.Core.Classes.Log.Buffers;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Buffers;

[TestFixture]
internal class PinHandleTests
{
    private Mock<ILogFileInfo> _fakeFileInfo = null!;

    [SetUp]
    public void SetUp ()
    {
        _fakeFileInfo = new Mock<ILogFileInfo>();
        _ = _fakeFileInfo.Setup(f => f.FullName).Returns("fake.log");
        _ = _fakeFileInfo.Setup(f => f.FileName).Returns("fake.log");
    }

    private LogBuffer CreateBuffer (int startLine, int lineCount)
    {
        var buffer = new LogBuffer(_fakeFileInfo.Object, 500)
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

    #region PinHandle

    [Test]
    public void Dispose_UnpinsAllBuffers ()
    {
        var buf1 = CreateBuffer(0, 10);
        var buf2 = CreateBuffer(10, 10);
        buf1.Pin();
        buf2.Pin();

        // Note: PinHandle constructor is internal, test accesses via InternalsVisibleTo or PinRange
        var handle = new PinHandle([buf1, buf2]);

        handle.Dispose();

        Assert.That(buf1.IsPinned, Is.False);
        Assert.That(buf2.IsPinned, Is.False);
    }

    [Test]
    public void DoubleDispose_IsNoop ()
    {
        var buf = CreateBuffer(0, 10);
        buf.Pin();

        var handle = new PinHandle([buf]);
        handle.Dispose();
        handle.Dispose(); // should not throw or double-unpin

        Assert.That(buf.IsPinned, Is.False);
    }

    [Test]
    public void EmptyHandle_DisposeIsNoop ()
    {
        var handle = new PinHandle([]);
        handle.Dispose(); // should not throw
    }

    #endregion

    #region BufferIndex.PinRange

    [Test]
    public void PinRange_SingleBuffer_PinsIt ()
    {
        using var index = new BufferIndex(50, 500);
        using var w = index.AcquireWriteLock();

        index.Add(CreateBuffer(0, 10));

        using var handle = index.PinRange(0, 9);

        Assert.That(handle.Count, Is.EqualTo(1));
        var entry = index.TryFindBuffer(0);
        Assert.That(entry.Buffer!.IsPinned, Is.True);
    }

    [Test]
    public void PinRange_MultipleBuffers_PinsAll ()
    {
        using var index = new BufferIndex(50, 500);
        using var w = index.AcquireWriteLock();

        index.Add(CreateBuffer(0, 10));
        index.Add(CreateBuffer(10, 10));
        index.Add(CreateBuffer(20, 10));

        using var handle = index.PinRange(0, 29);

        Assert.That(handle.Count, Is.EqualTo(3));
    }

    [Test]
    public void PinRange_EmptyIndex_ReturnsEmptyHandle ()
    {
        using var index = new BufferIndex(50, 500);
        using var w = index.AcquireWriteLock();

        using var handle = index.PinRange(0, 10);

        Assert.That(handle.Count, Is.EqualTo(0));
    }

    [Test]
    public void PinRange_PartialOverlap_PinsOnlyExisting ()
    {
        using var index = new BufferIndex(50, 500);
        using var w = index.AcquireWriteLock();

        index.Add(CreateBuffer(0, 10));

        // Range extends past available buffers
        using var handle = index.PinRange(0, 100);

        Assert.That(handle.Count, Is.EqualTo(1));
    }

    [Test]
    public void DisposeHandle_UnpinsBuffers_AllowsEviction ()
    {
        using var index = new BufferIndex(maxBuffers: 3, maxLinesPerBuffer: 10);
        using var w = index.AcquireWriteLock();

        for (var i = 0; i < 20; i++)
        {
            index.Add(CreateBuffer(i * 10, 10));
        }

        // Pin first buffer
        var handle = index.PinRange(0, 9);
        index.EvictLeastRecentlyUsed();

        var entry = index.TryFindBuffer(0);
        Assert.That(entry.Buffer!.IsDisposed, Is.False, "Pinned buffer should survive");

        // Dispose handle (unpin), then re-populate LRU above eviction threshold
        handle.Dispose();

        // After first eviction, LRU count is ~3-4. EvictLeastRecentlyUsed requires
        // count > maxBuffers + 10 = 13 to trigger. Add fresh buffers to exceed that.
        for (var i = 20; i < 40; i++)
        {
            index.Add(CreateBuffer(i * 10, 10));
        }

        // Touch new buffers to populate LRU
        for (var i = 20; i < 40; i++)
        {
            _ = index.TryFindBuffer(i * 10);
        }

        index.EvictLeastRecentlyUsed();

        Assert.That(entry.Buffer.IsDisposed, Is.True, "Unpinned buffer should be evicted");
    }

    [Test]
    public void SequentialPrefetchPattern_OldPinsReleased ()
    {
        using var index = new BufferIndex(50, 500);
        using var w = index.AcquireWriteLock();

        index.Add(CreateBuffer(0, 10));
        index.Add(CreateBuffer(10, 10));
        index.Add(CreateBuffer(20, 10));

        // Simulate first prefetch: pin buffer 0
        var handle1 = index.PinRange(0, 9);
        var buf0 = index.TryFindBuffer(0).Buffer!;
        Assert.That(buf0.IsPinned, Is.True);

        // Simulate scroll: dispose old, pin new range
        handle1.Dispose();
        Assert.That(buf0.IsPinned, Is.False);

        var handle2 = index.PinRange(10, 19);
        var buf1 = index.TryFindBuffer(10).Buffer!;
        Assert.That(buf1.IsPinned, Is.True);
        Assert.That(buf0.IsPinned, Is.False);

        handle2.Dispose();
    }

    #endregion
}