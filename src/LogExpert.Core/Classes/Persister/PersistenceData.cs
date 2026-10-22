using ColumnizerLib;

using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.JsonConverters;
using LogExpert.Core.Entities;

using Newtonsoft.Json;

namespace LogExpert.Core.Classes.Persister;

[Serializable]
public class PersistenceData
{
    public SortedList<int, Entities.Bookmark> BookmarkList { get; set; } = [];

    public int BookmarkListPosition { get; set; } = 300;

    public bool BookmarkListVisible { get; set; }

    /// <summary>
    /// The columnizer to use for this session. This property stores the entire columnizer configuration including any custom names.
    /// </summary>
    [JsonConverter(typeof(ColumnizerJsonConverter))]
    public ILogLineMemoryColumnizer Columnizer { get; set; }

    /// <summary>
    /// Deprecated: Use Columnizer property instead. This is kept for backward compatibility with old session files.
    /// </summary>
    [Obsolete("Use Columnizer property instead. This property is kept for backward compatibility.")]
    public string ColumnizerName { get; set; }

    public int CurrentLine { get; set; } = -1;

    [JsonConverter(typeof(EncodingJsonConverter))]
    public System.Text.Encoding Encoding { get; set; }

    public string FileName { get; set; }

    public bool FilterAdvanced { get; set; }

    /// <summary>
    /// The Log Window's own filter, as the single entry of a list. A list for backward
    /// compatibility only — <see cref="SessionFileComposer"/> owns the mapping and documents what
    /// older Session Files carry here.
    /// </summary>
    public List<FilterParams> FilterParamsList { get; set; } = [];

    public int FilterPosition { get; set; } = 222;

    public bool FilterSaveListVisible { get; set; }

    public List<FilterTabData> FilterTabDataList { get; set; } = [];

    /// <summary>
    /// Selection mode for Cells
    /// Default: false (FullRowSelect mode)
    /// </summary>
    public bool CellSelectMode { get; set; }

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