using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Entities;
using LogExpert.Core.Interface;

namespace LogExpert.UI.Interface;

/// <summary>
/// Declares methods that are needed for drawing log lines. Used by PaintHelper.
/// </summary>
internal interface ILogPaintContextUI : ILogPaintContext
{
    #region Properties

    Font MonospacedFont { get; } // Font font = new Font("Courier New", this.Preferences.fontSize, FontStyle.Bold);
    Font NormalFont { get; }
    Font BoldFont { get; }
    Color BookmarkColor { get; }

    #endregion

    #region Public methods

    ILogLine GetLogLine(int lineNum);

    IColumn GetCellValue(int rowIndex, int columnIndex);

    Bookmark GetBookmarkForLine(int lineNum);

    HighlightEntry FindHighlightEntry(ITextValue line, bool noWordMatches);

    IList<HighlightMatchEntry> FindHighlightMatches(ITextValue line);

    #endregion
}