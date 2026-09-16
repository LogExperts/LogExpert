namespace LogExpert.UI.Controls.LogWindow;

[Flags]
internal enum MarkerSource
{
    None = 0,
    Highlights = 1,
    Bookmarks = 2,
    Search = 4,
    Filter = 8,
    All = Highlights | Bookmarks | Search | Filter
}