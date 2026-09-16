using LogExpert.Core.Classes.Marker;

using NUnit.Framework;

namespace LogExpert.Tests.Marker;

[TestFixture]
public class MarkerBucketTests
{
    [Test]
    public void Aggregate_FirstAndLastLinesReachBothEndsOfATallBar ()
    {
        var buckets = MarkerBucket.Aggregate([new MarkerLine(0, 10), new MarkerLine(2, 20)], 3, 400);
        Assert.That(buckets.Select(bucket => bucket.Pixel), Is.EqualTo(new[] { 0, 399 }));
    }
    [Test]
    public void Aggregate_DenseMatchesPreserveIsolatedLinesAndChooseNearestMidpoint ()
    {
        MarkerLine[] matches = [new(0, 10), new(6, 20, 2), new(9, 30, 1), new(99, 40)];

        var buckets = MarkerBucket.Aggregate(matches, 100, 10);

        Assert.That(buckets, Is.EqualTo(new MarkerBucket[]
        {
            new(0, 0, 10, 3, 6, 10),
            new(9, 99, 99, 1, 99, 40)
        }));
    }

    [Test]
    public void Aggregate_TieChoosesLowerLineAndHighestPriorityColor ()
    {
        MarkerLine[] matches = [new(4, 10, 3), new(4, 10, 3), new(5, 20, 1)];

        Assert.That(MarkerBucket.Aggregate(matches, 10, 1),
            Is.EqualTo(new MarkerBucket[] { new(0, 0, 9, 2, 4, 20) }));
    }

    [TestCase(0, 10)]
    [TestCase(10, 0)]
    [TestCase(10, -1)]
    public void Aggregate_EmptyFileOrNoPixels_HasNoClickTargets (int lineCount, int height)
    {
        Assert.That(MarkerBucket.Aggregate([new MarkerLine(0, 10)], lineCount, height), Is.Empty);
    }

    [Test]
    public void Aggregate_SingleLineAndResizeKeepTheSameMatch ()
    {
        MarkerLine[] matches = [new(0, 10)];
        Assert.That(MarkerBucket.Aggregate(matches, 1, 1),
            Is.EqualTo(new MarkerBucket[] { new(0, 0, 0, 1, 0, 10) }));
        Assert.That(MarkerBucket.Aggregate(matches, 1, 300),
            Is.EqualTo(new MarkerBucket[] { new(0, 0, 0, 1, 0, 10) }));
    }

    [Test]
    public void Aggregate_HighLineNumbersDoNotOverflow ()
    {
        Assert.That(MarkerBucket.Aggregate([new MarkerLine(int.MaxValue - 1, 10)], int.MaxValue, 2),
            Is.EqualTo(new MarkerBucket[] { new(1, 2147483646, 2147483646, 1, 2147483646, 10) }));
    }
}