using LogExpert.Core.Classes.Log.Buffers;

using NUnit.Framework;

namespace LogExpert.Tests.Buffers;

[TestFixture]
internal class BufferIndexSnapshotTests
{
    [Test]
    public void ToString_ReturnsFormattedSummary ()
    {
        var snapshot = new BufferIndexSnapshot
        {
            BufferCount = 5,
            TotalLineCount = 2500,
            LruCacheCount = 3,
            Buffers = []
        };

        Assert.That(snapshot.ToString(), Is.EqualTo("Buffers=5, Lines=2500, LRU=3"));
    }

    [Test]
    public void BufferInfo_RecordEquality ()
    {
        var a = new BufferIndexSnapshot.BufferInfo(0, 100, 0, 1000, false, "file.log");
        var b = new BufferIndexSnapshot.BufferInfo(0, 100, 0, 1000, false, "file.log");
        var c = new BufferIndexSnapshot.BufferInfo(100, 100, 1000, 1000, false, "file.log");

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Is.Not.EqualTo(c));
    }

    [Test]
    public void DefaultBuffers_IsEmptyList ()
    {
        var snapshot = new BufferIndexSnapshot
        {
            BufferCount = 0,
            TotalLineCount = 0,
            LruCacheCount = 0
        };

        Assert.That(snapshot.Buffers, Is.Empty);
    }
}