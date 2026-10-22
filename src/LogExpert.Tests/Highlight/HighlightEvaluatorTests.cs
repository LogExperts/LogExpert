using ColumnizerLib;

using LogExpert.Core.Classes.Highlight;

using NUnit.Framework;

namespace LogExpert.Tests.Highlight;

[TestFixture]
public class HighlightEvaluatorTests
{
    #region Helper

    private sealed class TestLine (string text) : ITextValueMemory
    {
        public ReadOnlyMemory<char> Text => text.AsMemory();
    }

    private static ITextValueMemory Line (string text) => new TestLine(text);

    #endregion

    [Test]
    public void IsMatch_PlainSubstring_MatchesCaseInsensitiveByDefault ()
    {
        var entry = new HighlightEntry { SearchText = "error" };

        Assert.That(HighlightEvaluator.IsMatch(entry, Line("Something ERROR happened")), Is.True);
    }

    [Test]
    public void IsMatch_CaseSensitive_RespectsCase ()
    {
        var entry = new HighlightEntry { SearchText = "error", IsCaseSensitive = true };

        Assert.That(HighlightEvaluator.IsMatch(entry, Line("ERROR happened")), Is.False);
        Assert.That(HighlightEvaluator.IsMatch(entry, Line("an error here")), Is.True);
    }

    [Test]
    public void IsMatch_Regex_Matches ()
    {
        var entry = new HighlightEntry { SearchText = "ERR\\w+", IsRegex = true };

        Assert.That(HighlightEvaluator.IsMatch(entry, Line("ERROR happened")), Is.True);
    }

    [Test]
    public void IsMatch_NoSubstring_ReturnsFalse ()
    {
        var entry = new HighlightEntry { SearchText = "FATAL" };

        Assert.That(HighlightEvaluator.IsMatch(entry, Line("just an info line")), Is.False);
    }

    [Test]
    public void FindMatchingEntries_ReturnsOnlyMatches_InOrder ()
    {
        var error = new HighlightEntry { SearchText = "error" };
        var warn = new HighlightEntry { SearchText = "warn" };
        var fatal = new HighlightEntry { SearchText = "fatal" };
        var entries = new[] { error, warn, fatal };

        var result = HighlightEvaluator.FindMatchingEntries(entries, Line("warn: an error occurred"));

        Assert.That(result, Is.EqualTo([error, warn]));
    }

    [Test]
    public void FindMatchingEntries_NullLine_ReturnsEmpty ()
    {
        var entries = new[] { new HighlightEntry { SearchText = "error" } };

        var result = HighlightEvaluator.FindMatchingEntries(entries, null);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetTriggerActions_EmptyList_AllFalse ()
    {
        var actions = HighlightEvaluator.GetTriggerActions([]);

        Assert.That(actions.SuppressLed, Is.False);
        Assert.That(actions.StopTail, Is.False);
        Assert.That(actions.SetBookmark, Is.False);
        Assert.That(actions.BookmarkComment, Is.Empty);
    }

    [Test]
    public void GetTriggerActions_AggregatesFlagsAcrossEntries ()
    {
        var matching = new[]
        {
            new HighlightEntry { IsLedSwitch = true },
            new HighlightEntry { IsStopTail = true },
            new HighlightEntry { IsSetBookmark = true }
        };

        var actions = HighlightEvaluator.GetTriggerActions(matching);

        Assert.That(actions.SuppressLed, Is.True);
        Assert.That(actions.StopTail, Is.True);
        Assert.That(actions.SetBookmark, Is.True);
    }

    [Test]
    public void GetTriggerActions_JoinsBookmarkComments_WithCrlf_TrimmingTrailing ()
    {
        var matching = new[]
        {
            new HighlightEntry { IsSetBookmark = true, BookmarkComment = "first" },
            new HighlightEntry { IsSetBookmark = true, BookmarkComment = "second" }
        };

        var actions = HighlightEvaluator.GetTriggerActions(matching);

        Assert.That(actions.BookmarkComment, Is.EqualTo("first\r\nsecond"));
    }

    [Test]
    public void IsMatch_NullEntry_Throws ()
    {
        _ = Assert.Throws<ArgumentNullException>(() => HighlightEvaluator.IsMatch(null, Line("x")));
    }

    [Test]
    public void FindMatchingEntries_NullEntries_Throws ()
    {
        _ = Assert.Throws<ArgumentNullException>(() => HighlightEvaluator.FindMatchingEntries(null, Line("x")));
    }

    [Test]
    public void GetTriggerActions_NullList_Throws ()
    {
        _ = Assert.Throws<ArgumentNullException>(() => HighlightEvaluator.GetTriggerActions(null));
    }
}
