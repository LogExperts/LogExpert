using ColumnizerLib;
using LogExpert.Core.Config;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;

namespace LogExpert.UI.Interface;

/// <summary>
/// Declares methods that are needed for drawing log lines. Used by PaintHelper.
/// Extends the Core-visible line-source role with the paint-specific members.
/// </summary>
internal interface ILogPaintContextUI : ILogLineSource
{
    #region Properties

    Font MonospacedFont { get; }

    Font NormalFont { get; }

    Font BoldFont { get; }

    Color BookmarkColor { get; }

    SelectionHighlightSettings SelectionHighlight { get; }

    #endregion

    #region Public methods

    ILogLineMemory GetLogLineMemory (int lineNum);

    IColumnMemory GetCellValue (int rowIndex, int columnIndex);

    Bookmark GetBookmarkForLine (int lineNum);

    HighlightEntry FindHighlightEntry (ITextValueMemory line, bool noWordMatches);

    IList<HighlightMatchEntry> FindHighlightMatches (ITextValueMemory line);

    #endregion
}
