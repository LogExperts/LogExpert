using System.Drawing;

using ColumnizerLib;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Marker;

using NUnit.Framework;

namespace LogExpert.Tests.Marker;

[TestFixture]
public class MarkerBucketTests
{
    [Test]
    public void Aggregate_BoldOnlyHighlightResolvesForegroundBeforeRendering ()
    {
        var criteria = MarkerCriteria.ForHighlights([new HighlightEntry { SearchText = "hit", IsBold = true }]);
        var match = criteria.Match(0, new LogLine("hit", 0));
        Assert.That(match, Is.Not.Null);

        var buckets = MarkerBucket.Aggregate([match!.Value], 1, 1, Color.Green.ToArgb());

        Assert.That(buckets.Single().ColorArgb, Is.EqualTo(Color.Green.ToArgb()));
    }

    [Test]
    public void Aggregate_ChangedForegroundRecolorsOnlyInheritedMatches ()
    {
        MarkerLine[] matches = [new(0, null), new(1, Color.Red.ToArgb())];

        var light = MarkerBucket.Aggregate(matches, 2, 2, Color.Black.ToArgb());
        var dark = MarkerBucket.Aggregate(matches, 2, 2, Color.White.ToArgb());

        Assert.Multiple(() =>
        {
            Assert.That(light.Select(bucket => bucket.ColorArgb), Is.EqualTo(new[] { Color.Black.ToArgb(), Color.Red.ToArgb() }));
            Assert.That(dark.Select(bucket => bucket.ColorArgb), Is.EqualTo(new[] { Color.White.ToArgb(), Color.Red.ToArgb() }));
        });
    }

    [Test]
    public void Aggregate_FirstAndLastLinesReachBothEndsOfATallBar ()
    {
        var buckets = MarkerBucket.Aggregate([new MarkerLine(0, 10), new MarkerLine(2, 20)], 3, 400, Color.Black.ToArgb());
        Assert.That(buckets.Select(bucket => bucket.Pixel), Is.EqualTo(new[] { 0, 399 }));
    }
    [Test]
    public void Aggregate_DenseMatchesPreserveIsolatedLinesAndChooseNearestMidpoint ()
    {
        MarkerLine[] matches = [new(0, 10), new(6, 20, 2), new(9, 30, 1), new(99, 40)];

        var buckets = MarkerBucket.Aggregate(matches, 100, 10, Color.Black.ToArgb());

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

        Assert.That(MarkerBucket.Aggregate(matches, 10, 1, Color.Black.ToArgb()),
            Is.EqualTo(new MarkerBucket[] { new(0, 0, 9, 2, 4, 20) }));
    }

    [TestCase(0, 10)]
    [TestCase(10, 0)]
    [TestCase(10, -1)]
    public void Aggregate_EmptyFileOrNoPixels_HasNoClickTargets (int lineCount, int height)
    {
        Assert.That(MarkerBucket.Aggregate([new MarkerLine(0, 10)], lineCount, height, Color.Black.ToArgb()), Is.Empty);
    }

    [Test]
    public void Aggregate_SingleLineAndResizeKeepTheSameMatch ()
    {
        MarkerLine[] matches = [new(0, 10)];
        Assert.That(MarkerBucket.Aggregate(matches, 1, 1, Color.Black.ToArgb()),
            Is.EqualTo(new MarkerBucket[] { new(0, 0, 0, 1, 0, 10) }));
        Assert.That(MarkerBucket.Aggregate(matches, 1, 300, Color.Black.ToArgb()),
            Is.EqualTo(new MarkerBucket[] { new(0, 0, 0, 1, 0, 10) }));
    }

    [Test]
    public void Aggregate_HighLineNumbersDoNotOverflow ()
    {
        Assert.That(MarkerBucket.Aggregate([new MarkerLine(int.MaxValue - 1, 10)], int.MaxValue, 2, Color.Black.ToArgb()),
            Is.EqualTo(new MarkerBucket[] { new(1, 2147483646, 2147483646, 1, 2147483646, 10) }));
    }
}