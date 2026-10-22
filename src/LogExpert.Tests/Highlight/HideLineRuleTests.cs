using ColumnizerLib;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Entities;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.Highlight;

[TestFixture]
public class HideLineRuleTests
{
    private sealed class TestLine (string text) : ITextValueMemory
    {
        public ReadOnlyMemory<char> Text => text.AsMemory();
    }

    private static ITextValueMemory Line (string text) => new TestLine(text);

    [Test]
    public void IsHideLine_DefaultsToFalse ()
    {
        Assert.That(new HighlightEntry().IsHideLine, Is.False);
    }

    [Test]
    public void Clone_PreservesIsHideLine ()
    {
        var clone = (HighlightEntry)new HighlightEntry { SearchText = "DEBUG", IsHideLine = true }.Clone();

        Assert.That(clone.IsHideLine, Is.True);
    }

    [Test]
    public void Json_RoundTripsIsHideLine ()
    {
        var groups = new List<HighlightGroup>
        {
            new() { GroupName = "g", HighlightEntryList = [new HighlightEntry { SearchText = "DEBUG", IsHideLine = true }] }
        };

        var restored = JsonConvert.DeserializeObject<List<HighlightGroup>>(JsonConvert.SerializeObject(groups));

        Assert.That(restored[0].HighlightEntryList[0].IsHideLine, Is.True);
    }

    [Test]
    public void Json_LegacyEntryWithoutTheFlag_ReadsAsNotHidden ()
    {
        const string legacy = """[{"GroupName":"g","HighlightEntryList":[{"SearchText":"DEBUG","IsStopTail":false}]}]""";

        var restored = JsonConvert.DeserializeObject<List<HighlightGroup>>(legacy);

        Assert.That(restored[0].HighlightEntryList[0].IsHideLine, Is.False);
    }

    [Test]
    public void IsHidden_MatchingHideRule_HidesLine ()
    {
        HighlightEntry[] entries = [new HighlightEntry { SearchText = "debug", IsHideLine = true }];

        Assert.That(HighlightEvaluator.IsHidden(entries, Line("2026 DEBUG noise")), Is.True);
        Assert.That(HighlightEvaluator.IsHidden(entries, Line("2026 INFO start")), Is.False);
    }

    [Test]
    public void IsHidden_OnlyColoringRulesMatch_LineStaysVisible ()
    {
        HighlightEntry[] entries = [new HighlightEntry { SearchText = "ERROR", BackgroundColor = Color.Red }];

        Assert.That(HighlightEvaluator.IsHidden(entries, Line("ERROR boom")), Is.False);
    }

    [Test]
    public void IsHidden_EarlierColoringRuleMatches_CannotCancelLaterHideRule ()
    {
        HighlightEntry[] entries =
        [
            new HighlightEntry { SearchText = "heartbeat", BackgroundColor = Color.Yellow },
            new HighlightEntry { SearchText = "heart", IsHideLine = true }
        ];

        Assert.That(HighlightEvaluator.IsHidden(entries, Line("heartbeat ok")), Is.True);
    }

    [Test]
    public void IsHidden_WordMatchHideRule_HidesTheWholeLine ()
    {
        HighlightEntry[] entries = [new HighlightEntry { SearchText = "ping", IsWordMatch = true, IsHideLine = true }];

        Assert.That(HighlightEvaluator.IsHidden(entries, Line("host ping 3ms")), Is.True);
    }

    [Test]
    public void IsHidden_RegexAndCaseSensitivity_UseHighlightSemantics ()
    {
        HighlightEntry[] entries = [new HighlightEntry { SearchText = "^TRACE\\b", IsRegex = true, IsCaseSensitive = true, IsHideLine = true }];

        Assert.That(HighlightEvaluator.IsHidden(entries, Line("TRACE enter")), Is.True);
        Assert.That(HighlightEvaluator.IsHidden(entries, Line("trace enter")), Is.False);
        Assert.That(HighlightEvaluator.IsHidden(entries, Line("x TRACE")), Is.False);
    }

    [Test]
    public void IsHidden_SearchHitEntries_NeverHide ()
    {
        HighlightEntry[] entries = [new HighlightEntry { SearchText = "x", IsHideLine = true, IsSearchHit = true }];

        Assert.That(HighlightEvaluator.IsHidden(entries, Line("x")), Is.False);
    }
}
