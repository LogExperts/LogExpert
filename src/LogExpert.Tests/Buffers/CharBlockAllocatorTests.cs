using System.Buffers;

using LogExpert.Core.Classes.Log.Buffers;

using NUnit.Framework;

namespace LogExpert.Tests.Buffers;

[TestFixture]
public class CharBlockAllocatorTests
{
    [Test]
    public void Rent_SmallAllocation_ReturnsMemoryFromSameBlock ()
    {
        using var allocator = new CharBlockAllocator(1024);

        var mem1 = allocator.Rent(100);
        var mem2 = allocator.Rent(100);

        Assert.That(allocator.BlockCount, Is.EqualTo(1));
        Assert.That(mem1.Length, Is.EqualTo(100));
        Assert.That(mem2.Length, Is.EqualTo(100));
    }

    [Test]
    public void Rent_ExceedsBlock_AllocatesNewBlock ()
    {
        using var allocator = new CharBlockAllocator(128);

        var mem1 = allocator.Rent(100);
        var mem2 = allocator.Rent(100); // won't fit in first block

        Assert.That(allocator.BlockCount, Is.EqualTo(2));
        Assert.That(mem1.Length, Is.EqualTo(100));
        Assert.That(mem2.Length, Is.EqualTo(100));
    }

    [Test]
    public void Rent_OversizedLine_GetsStandaloneArray ()
    {
        using var allocator = new CharBlockAllocator(128);

        var mem = allocator.Rent(256);

        Assert.That(allocator.BlockCount, Is.EqualTo(1)); // normal blocks unchanged
        Assert.That(allocator.OversizedBlockCount, Is.EqualTo(1)); // tracked separately
        Assert.That(mem.Length, Is.EqualTo(256));
    }

    [Test]
    public void Rent_ZeroLength_ReturnsEmpty ()
    {
        using var allocator = new CharBlockAllocator(128);

        var mem = allocator.Rent(0);

        Assert.That(mem.IsEmpty, Is.True);
    }

    [Test]
    public void DetachBlocks_ReturnsNormalBlocks_ResetsAllocator ()
    {
        using var allocator = new CharBlockAllocator(128);

        _ = allocator.Rent(100);
        _ = allocator.Rent(100); // triggers second block

        var blocks = allocator.DetachBlocks();

        Assert.That(blocks, Has.Count.EqualTo(2));
        Assert.That(allocator.BlockCount, Is.EqualTo(1)); // fresh block created
    }

    [Test]
    public void DetachBlocks_ReturnsOversizedImmediately ()
    {
        using var allocator = new CharBlockAllocator(128);

        _ = allocator.Rent(100);  // normal
        _ = allocator.Rent(256);  // oversized
        _ = allocator.Rent(50);   // normal (fits in second block)

        Assert.That(allocator.OversizedBlockCount, Is.EqualTo(1));

        var blocks = allocator.DetachBlocks();

        // All blocks (normal + oversized) are transferred to caller
        Assert.That(blocks, Has.Count.EqualTo(3)); // initial + second normal + oversized
        Assert.That(allocator.OversizedBlockCount, Is.EqualTo(0)); // transferred to caller
    }

    [Test]
    public void ReturnAll_ReturnsBlocksToPool ()
    {
        var allocator = new CharBlockAllocator(128);

        _ = allocator.Rent(100);
        _ = allocator.Rent(100);

        allocator.ReturnAll();

        Assert.That(allocator.BlockCount, Is.EqualTo(0));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void SlicesAreIndependent ()
    {
        using var allocator = new CharBlockAllocator(1024);

        var mem1 = allocator.Rent(5);
        "Hello".AsSpan().CopyTo(mem1.Span);

        var mem2 = allocator.Rent(5);
        "World".AsSpan().CopyTo(mem2.Span);

        Assert.That(mem1.Span.ToString(), Is.EqualTo("Hello"));
        Assert.That(mem2.Span.ToString(), Is.EqualTo("World"));
    }

    [Test]
    public void Dispose_IsIdempotent ()
    {
        var allocator = new CharBlockAllocator(128);
        _ = allocator.Rent(64);

        allocator.Dispose();
        allocator.Dispose(); // should not throw
    }
}