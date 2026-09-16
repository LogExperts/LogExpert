using ColumnizerLib;

using LogExpert.Core.Classes.Log.Buffers;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Buffers;

[TestFixture]
internal class LogBufferPinningTests
{
    private Mock<ILogFileInfo> _fakeFileInfo = null!;

    [SetUp]
    public void SetUp ()
    {
        _fakeFileInfo = new Mock<ILogFileInfo>();
        _ = _fakeFileInfo.Setup(f => f.FullName).Returns("fake.log");
        _ = _fakeFileInfo.Setup(f => f.FileName).Returns("fake.log");
    }

    #region LogBuffer Pin/Unpin

    [Test]
    public void NewBuffer_IsNotPinned ()
    {
        var buffer = new LogBuffer(_fakeFileInfo.Object, 10);
        Assert.That(buffer.IsPinned, Is.False);
    }

    [Test]
    public void Pin_MakesBufferPinned ()
    {
        var buffer = new LogBuffer(_fakeFileInfo.Object, 10);
        buffer.Pin();
        Assert.That(buffer.IsPinned, Is.True);
        buffer.Unpin();
    }

    [Test]
    public void PinUnpin_ReturnsToUnpinned ()
    {
        var buffer = new LogBuffer(_fakeFileInfo.Object, 10);
        buffer.Pin();
        buffer.Unpin();
        Assert.That(buffer.IsPinned, Is.False);
    }

    [Test]
    public void MultiplePin_RequiresMatchingUnpins ()
    {
        var buffer = new LogBuffer(_fakeFileInfo.Object, 10);
        buffer.Pin();
        buffer.Pin();
        buffer.Unpin();
        Assert.That(buffer.IsPinned, Is.True, "Still pinned after one unpin of two pins");
        buffer.Unpin();
        Assert.That(buffer.IsPinned, Is.False, "Unpinned after matching unpins");
    }

    [Test]
    public void Reinitialise_ResetsPinCount ()
    {
        var buffer = new LogBuffer(_fakeFileInfo.Object, 10);
        buffer.Pin();
        buffer.Pin();
        buffer.Reinitialise(_fakeFileInfo.Object, 10);
        Assert.That(buffer.IsPinned, Is.False);
    }

    #endregion

    #region BufferIndex eviction with pins

    [Test]
    public void EvictLeastRecentlyUsed_SkipsPinnedBuffer ()
    {
        using var index = new BufferIndex(maxBuffers: 3, maxLinesPerBuffer: 10);

        using var w = index.AcquireWriteLock();

        // Add 20 buffers (way above maxBuffers=3)
        for (var i = 0; i < 20; i++)
        {
            var buf = new LogBuffer(_fakeFileInfo.Object, 10) { StartLine = i * 10, StartPos = i * 100 };
            for (var j = 0; j < 10; j++)
            {
                buf.AddLine(new LogLine($"line {i * 10 + j}".AsMemory(), i * 10 + j), (i * 10 + j) * 10);
            }

            buf.Size = 100;
            index.Add(buf);
        }

        // Pin the first buffer (oldest = first to be evicted)
        var firstEntry = index.TryFindBuffer(0);
        Assert.That(firstEntry.Found, Is.True);
        firstEntry.Buffer!.Pin();

        index.EvictLeastRecentlyUsed();

        // The pinned buffer should NOT be evicted
        Assert.That(firstEntry.Buffer.IsDisposed, Is.False, "Pinned buffer should survive eviction");
        Assert.That(firstEntry.Buffer.IsPinned, Is.True);

        // Clean up
        firstEntry.Buffer.Unpin();
    }

    [Test]
    public void EvictLeastRecentlyUsed_EvictsAfterUnpin ()
    {
        using var index = new BufferIndex(maxBuffers: 3, maxLinesPerBuffer: 10);

        using var w = index.AcquireWriteLock();

        // Add 20 buffers (way above maxBuffers=3)
        for (var i = 0; i < 20; i++)
        {
            var buf = new LogBuffer(_fakeFileInfo.Object, 10) { StartLine = i * 10, StartPos = i * 100 };
            for (var j = 0; j < 10; j++)
            {
                buf.AddLine(new LogLine($"line {i * 10 + j}".AsMemory(), i * 10 + j), (i * 10 + j) * 10);
            }

            buf.Size = 100;
            index.Add(buf);
        }

        // Pin buffer 0, evict (it survives), then unpin
        var firstEntry = index.TryFindBuffer(0);
        firstEntry.Buffer!.Pin();
        index.EvictLeastRecentlyUsed();
        Assert.That(firstEntry.Buffer.IsDisposed, Is.False);
        firstEntry.Buffer.Unpin();

        // After first eviction, LRU count is near maxBuffers (~3-4 entries).
        // EvictLeastRecentlyUsed has a threshold guard: only fires when count > maxBuffers + 10.
        // Add fresh buffers to push LRU count above that threshold.
        for (var i = 20; i < 40; i++)
        {
            var buf = new LogBuffer(_fakeFileInfo.Object, 10) { StartLine = i * 10, StartPos = i * 100 };
            for (var j = 0; j < 10; j++)
            {
                buf.AddLine(new LogLine($"line {i * 10 + j}".AsMemory(), i * 10 + j), (i * 10 + j) * 10);
            }

            buf.Size = 100;
            index.Add(buf);
        }

        // Touch new buffers to populate LRU
        for (var i = 20; i < 40; i++)
        {
            _ = index.TryFindBuffer(i * 10);
        }

        index.EvictLeastRecentlyUsed();

        Assert.That(firstEntry.Buffer.IsDisposed, Is.True, "Unpinned buffer should be evicted");
    }

    [Test]
    public void EvictLeastRecentlyUsed_UnpinnedBuffersStillEvicted ()
    {
        using var index = new BufferIndex(maxBuffers: 3, maxLinesPerBuffer: 10);

        using var w = index.AcquireWriteLock();

        for (var i = 0; i < 20; i++)
        {
            var buf = new LogBuffer(_fakeFileInfo.Object, 10) { StartLine = i * 10, StartPos = i * 100 };
            for (var j = 0; j < 10; j++)
            {
                buf.AddLine(new LogLine($"line {i * 10 + j}".AsMemory(), i * 10 + j), (i * 10 + j) * 10);
            }

            buf.Size = 100;
            index.Add(buf);
        }

        // Touch only the last 3 to make them recent
        _ = index.TryFindBuffer(170);
        _ = index.TryFindBuffer(180);
        _ = index.TryFindBuffer(190);

        index.EvictLeastRecentlyUsed();

        // LRU should have reduced — unpinned old buffers evicted
        Assert.That(index.LruCacheCount, Is.LessThanOrEqualTo(3 + 10));
    }

    #endregion
}