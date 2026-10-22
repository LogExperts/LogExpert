using System.Text;

using ColumnizerLib;

using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Entities;

namespace LogExpert.Core.Classes.Persister;

/// <summary>
/// A neutral, UI-free capture of one Log Window's persistable state. Gathered by the Log Window when a Session File is saved and applied back in two
/// Log-Window-side phases when one is loaded. Mapped to and from <see cref="PersistenceData"/> by
/// <see cref="SessionFileComposer"/>.
/// </summary>
public class SessionSnapshot
{
    public bool FollowTail { get; set; }

    public Encoding Encoding { get; set; }

    /// <summary>
    /// The log file's line count at save time. Never applied to the window — it is the input to
    /// the Rollover staleness rule (<see cref="SessionFileComposer.IsStale"/>).
    /// </summary>
    public int LineCount { get; set; }

    public int CurrentLine { get; set; }

    public int FirstDisplayedLine { get; set; }

    /// <summary>Splitter distance of the filter panel, as a plain value.</summary>
    public int FilterPosition { get; set; }

    public bool FilterVisible { get; set; }

    public bool FilterAdvanced { get; set; }

    public bool CellSelectMode { get; set; }

    public bool FilterSaveListVisible { get; set; }

    public bool MultiFile { get; set; }

    public int MultiFileMaxDays { get; set; }

    public string MultiFilePattern { get; set; }

    public string TabName { get; set; }

    public string HighlightGroupName { get; set; }

    /// <summary>
    /// The log file the session belongs to. Never applied to the window — consumed by
    /// <c>PersisterHelpers.FindFilenameForSettings</c> on direct <c>.lxp</c> open. Note:
    /// <see cref="Persister"/> rewrites it on SameDir saves *after* compose (transformed by
    /// design, named in the round-trip exclusion list).
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Manual bookmarks only — filtering out auto-generated bookmarks is a gather-side rule of
    /// the Log Window; the snapshot carries what it is given.
    /// </summary>
    public SortedList<int, Entities.Bookmark> BookmarkList { get; set; } = [];

    public SortedList<int, RowHeightEntry> RowHeightList { get; set; } = [];

    public List<string> MultiFileNames { get; set; } = [];

    /// <summary>
    /// The window's own filter, or <c>null</c> when there is none to save (Save Filters off, or a
    /// Session File that carries no filter). Its serialized form is a one-entry list — see
    /// <see cref="SessionFileComposer"/>.
    /// </summary>
    public FilterParams FilterParams { get; set; }

    public ILogLineMemoryColumnizer Columnizer { get; set; }

    /// <summary>
    /// The window's Filter Pipe tabs. Each child carries its own full nested snapshot, so the
    /// whole tab tree rides along on save and load.
    /// </summary>
    public List<FilterTabSnapshot> FilterTabs { get; set; } = [];
}
