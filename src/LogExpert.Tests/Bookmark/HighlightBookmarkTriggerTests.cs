using LogExpert.Core.Classes.Bookmark;
using LogExpert.Core.Classes.Highlight;

using NUnit.Framework;

namespace LogExpert.Tests.Bookmark;

[TestFixture]
public class HighlightBookmarkTriggerTests
{
    #region GetHighlightActions Tests

    [Test]
    public void GetHighlightActions_WhenIsSetBookmarkTrue_ReturnsSetBookmarkTrue ()
    {
        // Arrange
        var entry = new HighlightEntry
        {
            SearchText = "ERROR",
            IsSetBookmark = true,
            BookmarkComment = "Error found"
        };
        IList<HighlightEntry> matchingList = [entry];

        // Act — GetHighlightActions is a private static method in LogWindow.
        // We replicate its logic here to test the contract.
        var (_, _, setBookmark, bookmarkComment) = ExtractHighlightActions(matchingList);

        // Assert
        Assert.That(setBookmark, Is.True);
        Assert.That(bookmarkComment, Is.EqualTo("Error found"));
    }

    [Test]
    public void GetHighlightActions_WhenIsSetBookmarkFalse_ReturnsSetBookmarkFalse ()
    {
        // Arrange
        var entry = new HighlightEntry
        {
            SearchText = "INFO",
            IsSetBookmark = false
        };
        IList<HighlightEntry> matchingList = [entry];

        // Act
        var (_, _, setBookmark, _) = ExtractHighlightActions(matchingList);

        // Assert
        Assert.That(setBookmark, Is.False);
    }

    [Test]
    public void GetHighlightActions_WhenMultipleEntriesWithBookmarks_ConcatenatesComments ()
    {
        // Arrange
        var entry1 = new HighlightEntry
        {
            SearchText = "ERROR",
            IsSetBookmark = true,
            BookmarkComment = "First"
        };

        var entry2 = new HighlightEntry
        {
            SearchText = "WARN",
            IsSetBookmark = true,
            BookmarkComment = "Second"
        };

        IList<HighlightEntry> matchingList = [entry1, entry2];

        // Act
        var (_, _, setBookmark, bookmarkComment) = ExtractHighlightActions(matchingList);

        // Assert
        Assert.That(setBookmark, Is.True);
        Assert.That(bookmarkComment, Is.EqualTo("First\r\nSecond"));
    }

    [Test]
    public void GetHighlightActions_WhenEmptyList_ReturnsAllFalse ()
    {
        // Arrange
        IList<HighlightEntry> matchingList = [];

        // Act
        var (noLed, stopTail, setBookmark, bookmarkComment) = ExtractHighlightActions(matchingList);

        // Assert
        Assert.That(noLed, Is.False);
        Assert.That(stopTail, Is.False);
        Assert.That(setBookmark, Is.False);
        Assert.That(bookmarkComment, Is.Empty);
    }

    [Test]
    public void GetHighlightActions_WhenBookmarkCommentIsEmpty_ReturnsSetBookmarkTrueWithEmptyComment ()
    {
        // Arrange
        var entry = new HighlightEntry
        {
            SearchText = "ERROR",
            IsSetBookmark = true,
            BookmarkComment = string.Empty
        };
        IList<HighlightEntry> matchingList = [entry];

        // Act
        var (_, _, setBookmark, bookmarkComment) = ExtractHighlightActions(matchingList);

        // Assert
        Assert.That(setBookmark, Is.True);
        Assert.That(bookmarkComment, Is.Empty);
    }

    [Test]
    public void GetHighlightActions_WhenBookmarkCommentIsNull_ReturnsSetBookmarkTrueWithEmptyComment ()
    {
        // Arrange
        var entry = new HighlightEntry
        {
            SearchText = "ERROR",
            IsSetBookmark = true,
            BookmarkComment = null
        };
        IList<HighlightEntry> matchingList = [entry];

        // Act
        var (_, _, setBookmark, bookmarkComment) = ExtractHighlightActions(matchingList);

        // Assert
        Assert.That(setBookmark, Is.True);
        Assert.That(bookmarkComment, Is.Empty);
    }

    /// <summary>
    /// Replicates the logic from LogWindow.GetHighlightActions (private static). This must stay in sync with the actual
    /// implementation. If the implementation is refactored to be testable directly, remove this helper.
    /// </summary>
    private static (bool NoLed, bool StopTail, bool SetBookmark, string BookmarkComment) ExtractHighlightActions (IList<HighlightEntry> matchingList)
    {
        var noLed = false;
        var stopTail = false;
        var setBookmark = false;
        var bookmarkComment = string.Empty;

        foreach (var entry in matchingList)
        {
            if (entry.IsLedSwitch)
            {
                noLed = true;
            }

            if (entry.IsSetBookmark)
            {
                setBookmark = true;
                if (!string.IsNullOrEmpty(entry.BookmarkComment))
                {
                    bookmarkComment += entry.BookmarkComment + "\r\n";
                }
            }

            if (entry.IsStopTail)
            {
                stopTail = true;
            }
        }

        bookmarkComment = bookmarkComment.TrimEnd(['\r', '\n']);

        return (noLed, stopTail, setBookmark, bookmarkComment);
    }

    #endregion

    #region BookmarkDataProvider Tests

    [Test]
    public void ConvertToManualBookmark_ConvertsAutoToManual ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(Core.Entities.Bookmark.CreateAutoGenerated(42, "auto", "ERROR"));

        // Act
        var result = provider.ConvertToManualBookmark(42);

        // Assert
        Assert.That(result, Is.True);
        var bookmark = provider.GetBookmarkForLine(42);
        Assert.That(bookmark.IsAutoGenerated, Is.False);
        Assert.That(bookmark.SourceHighlightText, Is.Null);
    }

    [Test]
    public void ConvertToManualBookmark_ManualBookmark_ReturnsFalse ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(new Core.Entities.Bookmark(42, "manual"));

        // Act
        var result = provider.ConvertToManualBookmark(42);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ConvertToManualBookmark_NoBookmark_ReturnsFalse ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();

        // Act
        var result = provider.ConvertToManualBookmark(42);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ConvertToManualBookmark_SurvivesRemoveAutoGenerated ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(Core.Entities.Bookmark.CreateAutoGenerated(10, "auto1", "ERROR"));
        provider.AddBookmark(Core.Entities.Bookmark.CreateAutoGenerated(20, "auto2", "WARN"));
        _ = provider.ConvertToManualBookmark(10); // convert line 10 to manual

        // Act
        provider.RemoveAutoGeneratedBookmarks();

        // Assert
        Assert.That(provider.IsBookmarkAtLine(10), Is.True, "Converted bookmark should survive");
        Assert.That(provider.IsBookmarkAtLine(20), Is.False, "Non-converted auto bookmark should be removed");
    }

    [Test]
    public void BookmarkDataProvider_AddBookmark_IsBookmarkAtLineReturnsTrue ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        var bookmark = new Core.Entities.Bookmark(42, "Test comment");

        // Act
        provider.AddBookmark(bookmark);

        // Assert
        Assert.That(provider.IsBookmarkAtLine(42), Is.True);
    }

    [Test]
    public void BookmarkDataProvider_AddBookmark_GetBookmarkReturnsCorrectBookmark ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        var bookmark = new Core.Entities.Bookmark(42, "Test comment");

        // Act
        provider.AddBookmark(bookmark);
        var result = provider.GetBookmarkForLine(42);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.LineNum, Is.EqualTo(42));
        Assert.That(result.Text, Is.EqualTo("Test comment"));
    }

    [Test]
    public void BookmarkDataProvider_RemoveBookmark_IsBookmarkAtLineReturnsFalse ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(new Core.Entities.Bookmark(42, "Test"));

        // Act
        provider.RemoveBookmarkForLine(42);

        // Assert
        Assert.That(provider.IsBookmarkAtLine(42), Is.False);
    }

    [Test]
    public void BookmarkDataProvider_AddBookmark_FiresBookmarkAddedEvent ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        var eventFired = false;
        provider.BookmarkAdded += (_, _) => eventFired = true;

        // Act
        provider.AddBookmark(new Core.Entities.Bookmark(42));

        // Assert
        Assert.That(eventFired, Is.True);
    }

    #endregion

    #region HighlightEntry Serialization Tests

    [Test]
    public void HighlightEntry_Clone_PreservesIsSetBookmark ()
    {
        // Arrange
        var entry = new HighlightEntry
        {
            SearchText = "ERROR",
            IsSetBookmark = true,
            BookmarkComment = "Critical error"
        };

        // Act
        var clone = entry.Clone();

        // Assert
        Assert.That(((HighlightEntry)clone).IsSetBookmark, Is.True);
        Assert.That(((HighlightEntry)clone).BookmarkComment, Is.EqualTo("Critical error"));
    }

    [Test]
    public void HighlightEntry_Clone_PreservesIsSetBookmarkWhenFalse ()
    {
        // Arrange
        var entry = new HighlightEntry
        {
            SearchText = "INFO",
            IsSetBookmark = false,
            BookmarkComment = string.Empty
        };

        // Act
        var clone = entry.Clone();

        // Assert
        Assert.That(((HighlightEntry)clone).IsSetBookmark, Is.False);
    }

    #endregion

    #region Closure Regression Tests

    /// <summary>
    /// Demonstrates the closure-over-loop-variable bug pattern. This test proves the bug exists when loop variables are
    /// captured directly. If this test fails in the future, the C# language has changed loop variable capture semantics
    /// for `for` loops.
    /// </summary>
    [Test]
    public void ClosureBug_ForLoopVariable_CapturedByReference_DemonstratesBug ()
    {
        // Arrange
        var capturedValues = new List<int>();
        var tasks = new List<Task>();

        // Act — simulate the buggy pattern
        for (var i = 0; i < 5; i++)
        {
            // -- BUG PATTERN: capturing `i` directly
            tasks.Add(Task.Run(() =>
            {
                lock (capturedValues)
                {
                    capturedValues.Add(i);
                }
            }));
        }

        Task.WaitAll([.. tasks]);

        // Assert — at least one value should be wrong (likely all are 5)
        // The key observation: NOT all values 0..4 are present
        var hasAllExpected = capturedValues.Order().SequenceEqual([0, 1, 2, 3, 4]);
        Assert.That(hasAllExpected, Is.False,
            "If this fails, the closure-over-loop-variable issue no longer applies to `for` loops in this C# version.");
    }

    /// <summary>
    /// Demonstrates the correct pattern — capturing the loop variable in a local. This is the pattern that must be
    /// applied in CheckFilterAndHighlight().
    /// </summary>
    [Test]
    public void ClosureFix_LocalCapture_AllValuesCorrect ()
    {
        // Arrange
        var capturedValues = new List<int>();
        var tasks = new List<Task>();

        // Act — correct pattern: capture in local variable
        for (var i = 0; i < 5; i++)
        {
            var captured = i; // FIX: local capture
            tasks.Add(Task.Run(() =>
            {
                lock (capturedValues)
                {
                    capturedValues.Add(captured);
                }
            }));
        }

        Task.WaitAll([.. tasks]);

        // Assert — all values 0..4 must be present
        capturedValues.Sort();
        Assert.That(capturedValues, Is.EquivalentTo([0, 1, 2, 3, 4]));
    }

    [Test]
    public void Bookmark_DefaultConstructor_IsAutoGeneratedIsFalse ()
    {
        // Arrange & Act
        var bookmark = new Core.Entities.Bookmark();

        // Assert
        Assert.That(bookmark.IsAutoGenerated, Is.False);
        Assert.That(bookmark.SourceHighlightText, Is.Null);
    }

    [Test]
    public void Bookmark_LineNumConstructor_IsAutoGeneratedIsFalse ()
    {
        // Arrange & Act
        var bookmark = new Core.Entities.Bookmark(42);

        // Assert
        Assert.That(bookmark.IsAutoGenerated, Is.False);
        Assert.That(bookmark.SourceHighlightText, Is.Null);
    }

    [Test]
    public void Bookmark_CommentConstructor_IsAutoGeneratedIsFalse ()
    {
        // Arrange & Act
        var bookmark = new Core.Entities.Bookmark(42, "test comment");

        // Assert
        Assert.That(bookmark.IsAutoGenerated, Is.False);
        Assert.That(bookmark.SourceHighlightText, Is.Null);
    }

    [Test]
    public void BookmarkDataProvider_AddBookmarks_AddsAllAndFiresEventOnce ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        var eventCount = 0;
        provider.BookmarkAdded += (_, _) => eventCount++;

        var bookmarks = new[]
        {
            new Core.Entities.Bookmark(10),
            new Core.Entities.Bookmark(20),
            new Core.Entities.Bookmark(30)
        };

        // Act
        var added = provider.AddBookmarks(bookmarks);

        // Assert
        Assert.That(added, Is.EqualTo(3));
        Assert.That(provider.Bookmarks.Count, Is.EqualTo(3));
        Assert.That(eventCount, Is.EqualTo(1), "BookmarkAdded should fire exactly once for a batch add");
    }

    [Test]
    public void BookmarkDataProvider_AddBookmarks_SkipsDuplicates ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(new Core.Entities.Bookmark(20));

        var eventCount = 0;
        provider.BookmarkAdded += (_, _) => eventCount++;

        var bookmarks = new[]
        {
            new Core.Entities.Bookmark(10),
            new Core.Entities.Bookmark(20), // duplicate — should be skipped
            new Core.Entities.Bookmark(30)
        };

        // Act
        var added = provider.AddBookmarks(bookmarks);

        // Assert
        Assert.That(added, Is.EqualTo(2));
        Assert.That(provider.Bookmarks.Count, Is.EqualTo(3));
        Assert.That(provider.IsBookmarkAtLine(10), Is.True);
        Assert.That(provider.IsBookmarkAtLine(20), Is.True);
        Assert.That(provider.IsBookmarkAtLine(30), Is.True);
    }

    [Test]
    public void BookmarkDataProvider_AddBookmarks_EmptyList_DoesNotFireEvent ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        var eventFired = false;
        provider.BookmarkAdded += (_, _) => eventFired = true;

        // Act
        var added = provider.AddBookmarks([]);

        // Assert
        Assert.That(added, Is.EqualTo(0));
        Assert.That(eventFired, Is.False, "BookmarkAdded should not fire when no bookmarks were added");
    }

    [Test]
    public void BookmarkDataProvider_AddBookmarks_AllDuplicates_DoesNotFireEvent ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(new Core.Entities.Bookmark(10));

        var eventCount = 0;
        provider.BookmarkAdded += (_, _) => eventCount++;

        // Act
        var added = provider.AddBookmarks([new Core.Entities.Bookmark(10)]);

        // Assert
        Assert.That(added, Is.EqualTo(0));
        Assert.That(eventCount, Is.EqualTo(0), "BookmarkAdded should not fire when all bookmarks are duplicates");
    }

    [Test]
    public void Bookmark_CreateAutoGenerated_SetsPropertiesCorrectly ()
    {
        // Arrange & Act
        var bookmark = Core.Entities.Bookmark.CreateAutoGenerated(100, "Error found", "ERROR");

        // Assert
        Assert.That(bookmark.IsAutoGenerated, Is.True);
        Assert.That(bookmark.SourceHighlightText, Is.EqualTo("ERROR"));
        Assert.That(bookmark.LineNum, Is.EqualTo(100));
        Assert.That(bookmark.Text, Is.EqualTo("Error found"));
    }

    [Test]
    public void Bookmark_CreateAutoGenerated_WithEmptyComment_Works ()
    {
        // Arrange & Act
        var bookmark = Core.Entities.Bookmark.CreateAutoGenerated(50, string.Empty, "WARN");

        // Assert
        Assert.That(bookmark.IsAutoGenerated, Is.True);
        Assert.That(bookmark.SourceHighlightText, Is.EqualTo("WARN"));
        Assert.That(bookmark.Text, Is.Empty);
    }

    [Test]
    public void RemoveAutoGeneratedBookmarks_RemovesOnlyAutoGenerated ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(new Core.Entities.Bookmark(10, "manual"));
        provider.AddBookmark(Core.Entities.Bookmark.CreateAutoGenerated(20, "auto1", "ERROR"));
        provider.AddBookmark(new Core.Entities.Bookmark(30, "manual2"));
        provider.AddBookmark(Core.Entities.Bookmark.CreateAutoGenerated(40, "auto2", "WARN"));

        // Act
        provider.RemoveAutoGeneratedBookmarks();

        // Assert
        Assert.That(provider.Bookmarks.Count, Is.EqualTo(2));
        Assert.That(provider.IsBookmarkAtLine(10), Is.True);
        Assert.That(provider.IsBookmarkAtLine(20), Is.False);
        Assert.That(provider.IsBookmarkAtLine(30), Is.True);
        Assert.That(provider.IsBookmarkAtLine(40), Is.False);
    }

    [Test]
    public void RemoveAutoGeneratedBookmarks_NoAutoGenerated_IsNoOp ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(new Core.Entities.Bookmark(10, "manual"));
        provider.AddBookmark(new Core.Entities.Bookmark(20, "manual2"));
        var eventFired = false;
        provider.BookmarkRemoved += (_, _) => eventFired = true;

        // Act
        provider.RemoveAutoGeneratedBookmarks();

        // Assert
        Assert.That(provider.Bookmarks.Count, Is.EqualTo(2));
        Assert.That(eventFired, Is.False);
    }

    [Test]
    public void RemoveAutoGeneratedBookmarks_EmptyProvider_IsNoOp ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        var eventFired = false;
        provider.BookmarkRemoved += (_, _) => eventFired = true;

        // Act
        provider.RemoveAutoGeneratedBookmarks();

        // Assert
        Assert.That(provider.Bookmarks.Count, Is.EqualTo(0));
        Assert.That(eventFired, Is.False);
    }

    [Test]
    public void RemoveAutoGeneratedBookmarks_AllAutoGenerated_RemovesAll ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(Core.Entities.Bookmark.CreateAutoGenerated(10, "auto1", "ERROR"));
        provider.AddBookmark(Core.Entities.Bookmark.CreateAutoGenerated(20, "auto2", "WARN"));

        // Act
        provider.RemoveAutoGeneratedBookmarks();

        // Assert
        Assert.That(provider.Bookmarks.Count, Is.EqualTo(0));
    }

    [Test]
    public void RemoveAutoGeneratedBookmarks_FiresBookmarkRemovedEvent ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(Core.Entities.Bookmark.CreateAutoGenerated(10, "auto", "ERROR"));
        var eventFired = false;
        provider.BookmarkRemoved += (_, _) => eventFired = true;

        // Act
        provider.RemoveAutoGeneratedBookmarks();

        // Assert
        Assert.That(eventFired, Is.True);
    }

    #endregion

    #region Bookmark Serialization Tests

    [Test]
    public void Bookmark_JsonSerialize_ExcludesAutoGeneratedProperties ()
    {
        // Arrange
        var bookmark = Core.Entities.Bookmark.CreateAutoGenerated(42, "Error", "ERROR");

        // Act
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(bookmark);

        // Assert
        Assert.That(json, Does.Not.Contain("IsAutoGenerated"));
        Assert.That(json, Does.Not.Contain("SourceHighlightText"));
    }

    [Test]
    public void Bookmark_JsonDeserialize_DefaultsToManual ()
    {
        // Arrange
        var json = "{\"LineNum\":42,\"Text\":\"Error\"}";

        // Act
        var bookmark = Newtonsoft.Json.JsonConvert.DeserializeObject<Core.Entities.Bookmark>(json);

        // Assert
        Assert.That(bookmark.IsAutoGenerated, Is.False);
        Assert.That(bookmark.SourceHighlightText, Is.Null);
    }

    #endregion

    #region Persistence Exclusion Tests

    [Test]
    public void PersistenceExclusion_AutoBookmarks_FilteredFromSerialization ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(new Core.Entities.Bookmark(10, "manual"));
        provider.AddBookmark(Core.Entities.Bookmark.CreateAutoGenerated(20, "auto", "ERROR"));
        provider.AddBookmark(new Core.Entities.Bookmark(30, "manual2"));

        // Act — simulate the manual-bookmarks-only rule at the GatherSessionSnapshot gather site
        SortedList<int, Core.Entities.Bookmark> manualBookmarks = [];
        foreach (var kvp in provider.BookmarkList)
        {
            if (!kvp.Value.IsAutoGenerated)
            {
                manualBookmarks.Add(kvp.Key, kvp.Value);
            }
        }

        // Assert
        Assert.That(manualBookmarks.Count, Is.EqualTo(2));
        Assert.That(manualBookmarks.ContainsKey(10), Is.True);
        Assert.That(manualBookmarks.ContainsKey(20), Is.False);
        Assert.That(manualBookmarks.ContainsKey(30), Is.True);
    }

    [Test]
    public void PersistenceExclusion_ConvertedBookmarks_IncludedInSerialization ()
    {
        // Arrange
        var provider = new BookmarkDataProvider();
        provider.AddBookmark(Core.Entities.Bookmark.CreateAutoGenerated(10, "auto", "ERROR"));
        _ = provider.ConvertToManualBookmark(10);

        // Act — simulate the manual-bookmarks-only rule at the GatherSessionSnapshot gather site
        SortedList<int, Core.Entities.Bookmark> manualBookmarks = [];
        foreach (var kvp in provider.BookmarkList)
        {
            if (!kvp.Value.IsAutoGenerated)
            {
                manualBookmarks.Add(kvp.Key, kvp.Value);
            }
        }

        // Assert — converted bookmark should be included
        Assert.That(manualBookmarks.Count, Is.EqualTo(1));
        Assert.That(manualBookmarks.ContainsKey(10), Is.True);
    }

    [Test]
    public void PersistenceExclusion_RoundTrip_ExcludesAutoGenerated ()
    {
        // Arrange
        var auto = Core.Entities.Bookmark.CreateAutoGenerated(42, "auto bookmark", "ERROR");
        var manual = new Core.Entities.Bookmark(100, "manual bookmark");

        // Act — serialize both
        var autoJson = Newtonsoft.Json.JsonConvert.SerializeObject(auto);
        var manualJson = Newtonsoft.Json.JsonConvert.SerializeObject(manual);

        // Deserialize
        var autoDeserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<Core.Entities.Bookmark>(autoJson);
        var manualDeserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<Core.Entities.Bookmark>(manualJson);

        // Assert — both deserialize as manual (IsAutoGenerated is not persisted)
        Assert.That(autoDeserialized.IsAutoGenerated, Is.False);
        Assert.That(manualDeserialized.IsAutoGenerated, Is.False);
        Assert.That(autoDeserialized.SourceHighlightText, Is.Null);
    }

    #endregion
}