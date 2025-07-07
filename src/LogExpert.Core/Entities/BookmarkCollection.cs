using System.Collections.ObjectModel;

namespace LogExpert.Core.Entities;

public class BookmarkCollection : ReadOnlyCollection<Bookmark>
{
    #region Fields

    private SortedList<int, Bookmark> bookmarkList;

    #endregion

    #region cTor

    internal BookmarkCollection(SortedList<int, Bookmark> bookmarkList)
        : base(bookmarkList.Values)
    {
        this.bookmarkList = bookmarkList;
    }

    #endregion
}