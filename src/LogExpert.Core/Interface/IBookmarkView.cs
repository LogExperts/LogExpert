using LogExpert.Core.Entities;

namespace LogExpert.Core.Interface;

/// <summary>
/// To be implemented by the bookmark window. Will be informed from LogWindow about changes in bookmarks.
/// </summary>
public interface IBookmarkView
{
    #region Properties

    //TODO: After all refactoring is done, take care of this warning.
    bool LineColumnVisible { set; }

    #endregion

    #region Public methods

    void UpdateView ();

    void BookmarkTextChanged (Bookmark bookmark);

    void SelectBookmark (int lineNum);

    void SetBookmarkData (IBookmarkData bookmarkData);

    #endregion
}