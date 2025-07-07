using LogExpert.Core.Entities;

namespace LogExpert.Core.Interface;

public interface IBookmarkData
{
    #region Properties

    BookmarkCollection Bookmarks { get; }

    #endregion

    #region Public methods

    void ToggleBookmark (int lineNum);

    bool IsBookmarkAtLine (int lineNum);

    int GetBookmarkIndexForLine (int lineNum);

    Bookmark GetBookmarkForLine (int lineNum);

    void SetBookmarks (SortedList<int, Bookmark> bookmarkList);

    #endregion
}