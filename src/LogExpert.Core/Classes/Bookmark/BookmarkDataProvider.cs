using System.Globalization;

using LogExpert.Core.Entities;
using LogExpert.Core.Interface;

using NLog;

namespace LogExpert.Core.Classes.Bookmark;

public class BookmarkDataProvider : IBookmarkData
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region cTor

    public BookmarkDataProvider ()
    {
        BookmarkList = [];
    }

    public BookmarkDataProvider (SortedList<int, Entities.Bookmark> bookmarkList)
    {
        BookmarkList = bookmarkList;
    }

    #endregion

    #region Events

    public event EventHandler<EventArgs> BookmarkAdded;
    public event EventHandler<EventArgs> BookmarkRemoved;
    public event EventHandler<EventArgs> AllBookmarksRemoved;

    #endregion

    #region Properties

    public BookmarkCollection Bookmarks => new(BookmarkList);

    public SortedList<int, Entities.Bookmark> BookmarkList { get; private set; }

    #endregion

    #region Public methods

    public void SetBookmarks (SortedList<int, Entities.Bookmark> bookmarkList)
    {
        BookmarkList = bookmarkList;
    }

    public void ToggleBookmark (int lineNum)
    {
        if (IsBookmarkAtLine(lineNum))
        {
            RemoveBookmarkForLine(lineNum);
        }
        else
        {
            AddBookmark(new Entities.Bookmark(lineNum));
        }
    }

    public bool IsBookmarkAtLine (int lineNum)
    {
        return BookmarkList.ContainsKey(lineNum);
    }

    public int GetBookmarkIndexForLine (int lineNum)
    {
        return BookmarkList.IndexOfKey(lineNum);
    }

    public Entities.Bookmark GetBookmarkForLine (int lineNum)
    {
        return BookmarkList[lineNum];
    }

    #endregion

    #region Internals

    public void ShiftBookmarks (int offset)
    {
        SortedList<int, Entities.Bookmark> newBookmarkList = [];

        foreach (Entities.Bookmark bookmark in BookmarkList.Values)
        {
            var line = bookmark.LineNum - offset;
            if (line >= 0)
            {
                bookmark.LineNum = line;
                newBookmarkList.Add(line, bookmark);
            }
        }

        BookmarkList = newBookmarkList;
    }

    public int FindPrevBookmarkIndex (int lineNum)
    {
        IList<Entities.Bookmark> values = BookmarkList.Values;
        for (var i = BookmarkList.Count - 1; i >= 0; --i)
        {
            if (values[i].LineNum <= lineNum)
            {
                return i;
            }
        }

        return BookmarkList.Count - 1;
    }

    public int FindNextBookmarkIndex (int lineNum)
    {
        IList<Entities.Bookmark> values = BookmarkList.Values;
        for (var i = 0; i < BookmarkList.Count; ++i)
        {
            if (values[i].LineNum >= lineNum)
            {
                return i;
            }
        }
        return 0;
    }

    public void RemoveBookmarkForLine (int lineNum)
    {
        _ = BookmarkList.Remove(lineNum);
        OnBookmarkRemoved();
    }

    //TOOD: check if the callers are checking for null before calling
    public void RemoveBookmarksForLines (IEnumerable<int> lineNumList)
    {
        ArgumentNullException.ThrowIfNull(lineNumList, nameof(lineNumList));

        foreach (var lineNum in lineNumList)
        {
            _ = BookmarkList.Remove(lineNum);

        }

        OnBookmarkRemoved();
    }

    //TOOD: check if the callers are checking for null before calling
    public void AddBookmark (Entities.Bookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(bookmark, nameof(bookmark));

        BookmarkList.Add(bookmark.LineNum, bookmark);
        OnBookmarkAdded();
    }

    public void ClearAllBookmarks ()
    {
        _logger.Debug(CultureInfo.InvariantCulture, "Removing all bookmarks");
        BookmarkList.Clear();
        OnAllBookmarksRemoved();
    }

    #endregion

    protected void OnBookmarkAdded ()
    {
        BookmarkAdded?.Invoke(this, EventArgs.Empty);
    }

    protected void OnBookmarkRemoved ()
    {
        BookmarkRemoved?.Invoke(this, EventArgs.Empty);
    }

    protected void OnAllBookmarksRemoved ()
    {
        AllBookmarksRemoved?.Invoke(this, EventArgs.Empty);
    }
}