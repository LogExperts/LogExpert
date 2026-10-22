using System.Drawing;

using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Entities;

namespace LogExpert.Core.Config;

[Serializable]
public class Settings
{
    public Preferences Preferences { get; set; } = new();

    /// <summary>
    /// Settings schema version. Incremented when <see cref="LegacyPreferencesMigrator"/> introduces a
    /// migration step. Pre-existing settings files have version <c>0</c>.
    /// </summary>
    public int SettingsVersion { get; set; }

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

    /// <summary>
    /// Legacy property for backward compatibility with old settings files that had hilightEntryList at root level.
    /// This property redirects data to Preferences.HighlightGroupList during import.
    /// Will be removed in a future version once migration period is complete.
    /// </summary>
    [Obsolete("This property exists only for backward compatibility with old settings files. Data is stored in Preferences.HighlightGroupList.")]
    [Newtonsoft.Json.JsonProperty("hilightEntryList", DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore]
    public List<HighlightEntry> HilightEntryList
    {
        get => null; // Never serialize this
        set
        {
            // This was likely empty in old files as entries were in groups
            // Keep for compatibility but likely unused
        }
    }

    /// <summary>
    /// Legacy property for backward compatibility with old settings files that had hilightGroupList at root level.
    /// This property redirects data to Preferences.HighlightGroupList during import.
    /// Will be removed in a future version once migration period is complete.
    /// </summary>
    [Obsolete("This property exists only for backward compatibility with old settings files. Data is stored in Preferences.HighlightGroupList.")]
    [Newtonsoft.Json.JsonProperty("hilightGroupList", DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore]
    public List<HighlightGroup> HilightGroupList
    {
        get => null; // Never serialize this
        set
        {
            if (value != null && value.Count > 0)
            {
                Preferences ??= new Preferences();
                Preferences.HighlightGroupList = value;
            }
        }
    }

    public bool IsMaximized { get; set; }

    public string LastDirectory { get; set; }

    public List<string> LastOpenFilesList { get; set; } = [];

    public List<string> SearchHistoryList { get; set; } = [];

    public SearchParams SearchParams { get; set; } = new();

    public IList<string> UriHistoryList { get; set; } = [];

    public int VersionBuild { get; set; }
}