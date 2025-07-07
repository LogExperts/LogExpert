using LogExpert.Core.Entities;

namespace LogExpert.Core.EventArguments;

public class BookmarkEventArgs (Bookmark bookmark) : EventArgs
{
    #region Properties

    public Bookmark Bookmark { get; } = bookmark;

    #endregion
}