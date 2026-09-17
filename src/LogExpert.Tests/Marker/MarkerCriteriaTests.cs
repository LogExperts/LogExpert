using System.Drawing;

using ColumnizerLib;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Marker;
using LogExpert.Core.Entities;

using NUnit.Framework;

namespace LogExpert.Tests.Marker;

[TestFixture]
public class MarkerCriteriaTests
{
    [TestCase(false, false)]
    [TestCase(true, true)]
    public void Highlights_OnlyWordRulesMatchDisplayedColumns (bool wordMatch, bool expected)
    {
        var criteria = MarkerCriteria.ForHighlights([
            new HighlightEntry { SearchText = "^error$", IsRegex = true, IsWordMatch = wordMatch, BackgroundColor = Color.Yellow }
        ]);
        var match = criteria.Match(0, new LogLine("{\"message\":\"error\"}", 0), (_, _) => [new LogLine("error", 0)]);
        Assert.That(match.HasValue, Is.EqualTo(expected));
    }

    [TestCase(false, false, "ERROR", true)]
    [TestCase(false, true, "ERROR", false)]
    [TestCase(true, true, "^error$", true)]
    [TestCase(false, false, "^error$", false)]
    public void Search_UsesExecutedCriteriaSnapshot (bool regex, bool caseSensitive, string text, bool expected)
    {
        var search = new SearchParams { SearchText = text, IsRegex = regex, IsCaseSensitive = caseSensitive };
        var criteria = MarkerCriteria.ForSearch(search, Color.Blue.ToArgb());
        search.SearchText = "changed";
        Assert.That(criteria.Match(3, new LogLine("error", 3)).HasValue, Is.EqualTo(expected));
    }

    [Test]
    public void Highlights_UseDisplayedWordColumnsAndExcludeSearchEntries ()
    {
        var criteria = MarkerCriteria.ForHighlights([
            new HighlightEntry { SearchText = "raw", IsSearchHit = true, BackgroundColor = Color.Red },
            new HighlightEntry { SearchText = "display", IsWordMatch = true, ForegroundColor = Color.Green }
        ]);
        var result = criteria.Match(0, new LogLine("raw", 0), (_, _) => [new LogLine("display", 0)]);
        Assert.That(result, Is.EqualTo(new MarkerLine(0, Color.Green.ToArgb(), 1)));
        Assert.That(criteria.Match(0, new LogLine("raw", 0)), Is.Null);
    }

    [Test]
    public void Highlights_FirstVisualRuleWinsAndCriteriaAreSnapshotted ()
    {
        var trigger = new HighlightEntry { SearchText = "error", IsSetBookmark = true, AlertOnHit = true };
        var word = new HighlightEntry { SearchText = "error", IsWordMatch = true, NoBackground = true,
            ForegroundColor = Color.Red, BackgroundColor = Color.Yellow };
        var wholeLine = new HighlightEntry { SearchText = "error", BackgroundColor = Color.Blue };
        var criteria = MarkerCriteria.ForHighlights([trigger, word, wholeLine]);
        word.SearchText = "changed";
        word.ForegroundColor = Color.Green;

        var match = criteria.Match(7, new LogLine("ERROR occurred", 7));

        Assert.That(match, Is.EqualTo(new MarkerLine(7, Color.Red.ToArgb(), 1)));
    }
}