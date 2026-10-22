using System.Buffers;

using ColumnizerLib;

using LogExpert.Core.Classes.Log.Buffers;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Buffers;

[TestFixture]
public class LogBufferCharBlockTests
{
    private Mock<ILogFileInfo> _mockFileInfo;

    [SetUp]
    public void Setup ()
    {
        _mockFileInfo = new Mock<ILogFileInfo>();
        _ = _mockFileInfo.Setup(f => f.FullName).Returns("test.log");
    }

    [Test]
    public void AttachCharBlocks_AcceptsBlockList ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);
        var blocks = new List<char[]>
        {
            ArrayPool<char>.Shared.Rent(128),
            ArrayPool<char>.Shared.Rent(128)
        };

        // Should not throw
        buffer.AttachCharBlocks(blocks);

        // Clean up via eviction
        buffer.EvictContent();
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void EvictContent_WhilePinned_TriggersDebugAssert ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);
        var block = ArrayPool<char>.Shared.Rent(128);
        buffer.AttachCharBlocks([block]);
        buffer.AddLine(new LogLine("test".AsMemory(), 0), 0);
        buffer.Pin();

        // In Debug builds, this should trigger the assert.
        // In Release builds, it proceeds (defense in depth via eviction skip).
        // We can't directly test Debug.Assert in NUnit, but we verify the behavior:
        // After eviction while pinned, the buffer should still be disposed
        // (the assert is a developer warning, not a runtime guard).
        buffer.EvictContent();
        buffer.Unpin();
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void EvictContent_ReturnsAttachedCharBlocks ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);
        var block = ArrayPool<char>.Shared.Rent(128);
        buffer.AttachCharBlocks([block]);

        // Add a line so there's something to evict
        var lineMemory = "test line".AsMemory();
        buffer.AddLine(new LogLine(lineMemory, 0), 0);

        buffer.EvictContent();

        // After eviction, the block should have been returned to the pool.
        // We can't directly assert pool return, but we can verify no exception
        // and that re-attaching new blocks works.
        var newBlock = ArrayPool<char>.Shared.Rent(128);
        buffer.AttachCharBlocks([newBlock]);
        buffer.EvictContent(); // clean up
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void DisposeContent_ReturnsAttachedCharBlocks ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);
        var block = ArrayPool<char>.Shared.Rent(128);
        buffer.AttachCharBlocks([block]);

        buffer.AddLine(new LogLine("test".AsMemory(), 0), 0);

        buffer.DisposeContent();

        // Should be able to reinitialise and attach new blocks
        buffer.Reinitialise(_mockFileInfo.Object, 10);
        var newBlock = ArrayPool<char>.Shared.Rent(128);
        buffer.AttachCharBlocks([newBlock]);
        buffer.EvictContent();
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void ClearLines_ReturnsAttachedCharBlocks ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);
        var block = ArrayPool<char>.Shared.Rent(128);
        buffer.AttachCharBlocks([block]);

        buffer.AddLine(new LogLine("test".AsMemory(), 0), 0);

        buffer.ClearLines();

        Assert.That(buffer.LineCount, Is.EqualTo(0));

        // Attach new blocks after clear
        var newBlock = ArrayPool<char>.Shared.Rent(128);
        buffer.AttachCharBlocks([newBlock]);
        buffer.EvictContent();
    }

    [Test]
    public void Reinitialise_ReturnsAttachedCharBlocks ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);
        var block = ArrayPool<char>.Shared.Rent(128);
        buffer.AttachCharBlocks([block]);

        buffer.Reinitialise(_mockFileInfo.Object, 10);

        // After reinitialise, old blocks should be returned.
        // Verify by attaching new blocks (no double-return crash).
        var newBlock = ArrayPool<char>.Shared.Rent(128);
        buffer.AttachCharBlocks([newBlock]);
        buffer.EvictContent();
    }

    [Test]
    public void AttachCharBlocks_ReturnsOldBlocks_WhenCalledTwice ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);

        var block1 = ArrayPool<char>.Shared.Rent(128);
        buffer.AttachCharBlocks([block1]);

        // Attaching new blocks should return the old ones first
        var block2 = ArrayPool<char>.Shared.Rent(128);
        buffer.AttachCharBlocks([block2]);

        buffer.EvictContent();
    }

    [Test]
    public void AttachCharBlocks_Null_DoesNotThrow ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);

        buffer.AttachCharBlocks(null);
        buffer.EvictContent(); // should not throw
    }

    [Test]
    public void AttachCharBlocks_EmptyList_DoesNotThrow ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);

        buffer.AttachCharBlocks([]);
        buffer.EvictContent(); // should not throw
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void EvictContent_WithoutAttachedBlocks_DoesNotThrow ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);
        buffer.AddLine(new LogLine("test".AsMemory(), 0), 0);

        // No char blocks attached — should evict cleanly (backward compat)
        buffer.EvictContent();
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void BlockBackedLines_ContentSurvivesUntilEviction ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);

        // Simulate block-based allocation: rent a block, write lines into it
        var block = ArrayPool<char>.Shared.Rent(1024);
        "Hello World".AsSpan().CopyTo(block.AsSpan(0, 11));
        "Second Line".AsSpan().CopyTo(block.AsSpan(11, 11));

        var line1Memory = new ReadOnlyMemory<char>(block, 0, 11);
        var line2Memory = new ReadOnlyMemory<char>(block, 11, 11);

        buffer.AddLine(new LogLine(line1Memory, 0), 0);
        buffer.AddLine(new LogLine(line2Memory, 1), 11);
        buffer.AttachCharBlocks([block]);

        // Lines should be readable while buffer is alive
        var retrieved1 = buffer.GetLineMemoryOfBlock(0);
        var retrieved2 = buffer.GetLineMemoryOfBlock(1);
        Assert.That(retrieved1.HasValue, Is.True);
        Assert.That(retrieved2.HasValue, Is.True);

        if (retrieved1 is not { } retrievedLine1)
        {
            Assert.Fail("Expected first retrieved line to have a value.");
        }
        else
        {
            Assert.That(retrievedLine1.FullLine.Span.ToString(), Is.EqualTo("Hello World"));
        }

        if (retrieved2 is not { } retrievedLine2)
        {
            Assert.Fail("Expected second retrieved line to have a value.");
        }
        else
        {
            Assert.That(retrievedLine2.FullLine.Span.ToString(), Is.EqualTo("Second Line"));
        }

        // After eviction, blocks are returned and lines are no longer accessible
        buffer.EvictContent();
        Assert.That(buffer.IsDisposed, Is.True);
    }

    // Regression tests for #634: follow-tail intermittently shows blank/stale tail rows in
    // Release builds. Root cause candidate 2 is a use-after-return on ArrayPool char blocks:
    // the background LRU-eviction thread returns a buffer's char blocks to the pool while the
    // UI still holds ReadOnlyMemory<char> slices into them. The defence is the IsPinned guard
    // in LogBuffer.ReturnCharBlocks(). These tests lock that guard down at the behaviour level
    // (content validity), not just IsDisposed.

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void PinnedBuffer_EvictContent_PreservesPreviouslyReadSlice ()
    {
        var buffer = new LogBuffer(_mockFileInfo.Object, 10);

        // Back a line with a rented char block, exactly as the real allocator does.
        const int blockSize = 1024;
        var block = ArrayPool<char>.Shared.Rent(blockSize);
        const string expected = "tail line 42";
        expected.AsSpan().CopyTo(block.AsSpan(0, expected.Length));

        buffer.AddLine(new LogLine(new ReadOnlyMemory<char>(block, 0, expected.Length), 0), 0);
        buffer.AttachCharBlocks([block]);

        // UI reads the slice, then pins (pin-before-eviction protects the backing block).
        var slice = buffer.GetLineMemoryOfBlock(0)!.Value.FullLine;
        buffer.Pin();

        // The LRU-eviction thread fires while the UI still holds the pin.
        buffer.EvictContent();

        // Churn the pool. If the pinned block had been wrongly returned, the very next
        // same-size Rent would hand it back (single-threaded LIFO) and the Fill would
        // clobber our slice into "XXXX...".
        var rented = new List<char[]>();
        for (var i = 0; i < 16; i++)
        {
            var arr = ArrayPool<char>.Shared.Rent(blockSize);
            arr.AsSpan().Fill('X');
            rented.Add(arr);
        }

        Assert.That(slice.Span.ToString(), Is.EqualTo(expected), "A slice into a pinned buffer must stay valid after eviction (#634 use-after-return guard).");

        buffer.Unpin();
        foreach (var arr in rented)
        {
            ArrayPool<char>.Shared.Return(arr);
        }
    }
}