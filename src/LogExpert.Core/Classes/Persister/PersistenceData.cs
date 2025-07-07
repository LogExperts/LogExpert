using System.Text;

using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Entities;

namespace LogExpert.Core.Classes.Persister;

public class PersistenceData
{
    public SortedList<int, Entities.Bookmark> BookmarkList { get; set; } = [];

    public int BookmarkListPosition { get; set; } = 300;

    public bool BookmarkListVisible { get; set; }

    public string ColumnizerName { get; set; }

    public int CurrentLine { get; set; } = -1;

    public Encoding Encoding { get; set; }

    public string FileName { get; set; }

    public bool FilterAdvanced { get; set; }

    public List<FilterParams> FilterParamsList { get; set; } = [];

    public int FilterPosition { get; set; } = 222;

    public bool FilterSaveListVisible { get; set; }

    public List<FilterTabData> FilterTabDataList { get; set; } = [];

    public int FirstDisplayedLine { get; set; } = -1;

    public bool FollowTail { get; set; } = true;

    public string HighlightGroupName { get; set; }

    public bool FilterVisible { get; set; }

    public int LineCount { get; set; }

    public bool MultiFile { get; set; }

    public int MultiFileMaxDays { get; set; }

    public List<string> MultiFileNames { get; set; } = [];

    public string MultiFilePattern { get; set; }

    public SortedList<int, RowHeightEntry> RowHeightList { get; set; } = [];

    public string SessionFileName { get; set; }

    public bool ShowBookmarkCommentColumn { get; set; }

    public string TabName { get; set; }

    public string SettingsSaveLoadLocation { get; set; }
}