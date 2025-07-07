using System.Drawing;

using LogExpert.Core.Classes.Filter;
using LogExpert.Entities;

namespace LogExpert.Core.Config;

[Serializable]
public class Settings
{
    public Preferences Preferences { get; set; } = new();

    public RegexHistory RegexHistory { get; set; } = new();

    public bool AlwaysOnTop { get; set; }

    public Rectangle AppBounds { get; set; }

    public Rectangle AppBoundsFullscreen { get; set; }

    public IList<ColumnizerHistoryEntry> ColumnizerHistoryList { get; set; } = [];

    public List<ColorEntry> FileColors { get; set; } = [];

    public List<string> FileHistoryList { get; set; } = [];

    public List<string> FilterHistoryList { get; set; } = [];

    public List<FilterParams> FilterList { get; set; } = [];

    public FilterParams FilterParams { get; set; } = new();

    public List<string> FilterRangeHistoryList { get; set; } = [];

    public bool HideLineColumn { get; set; }

    public bool IsMaximized { get; set; }

    public string LastDirectory { get; set; }

    public List<string> LastOpenFilesList { get; set; } = [];

    public List<string> SearchHistoryList { get; set; } = [];

    public SearchParams SearchParams { get; set; } = new();

    public IList<string> UriHistoryList { get; set; } = [];

    public int VersionBuild { get; set; }
}