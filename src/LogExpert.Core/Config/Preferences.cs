using System.Drawing;
using System.Runtime.Versioning;

using LogExpert.Core.Entities;
using LogExpert.Core.Enums;
using LogExpert.Core.Helpers;

namespace LogExpert.Core.Config;

[Serializable]
public class Preferences
{
    private SelectionHighlightSettings _selectionHighlight = new();

    public SelectionHighlightSettings SelectionHighlight
    {
        get => _selectionHighlight;
        set => _selectionHighlight = value ?? new();
    }

    /// <summary>
    /// List of highlight groups for syntax highlighting and text coloring.
    /// </summary>
    /// <remarks>
    /// Supports legacy property name "hilightGroupList" (with typo) for backward compatibility. Old settings files
    /// using the incorrect spelling will be automatically imported.
    /// </remarks>
    [Newtonsoft.Json.JsonProperty("HighlightGroupList")]
    [System.Text.Json.Serialization.JsonPropertyName("HighlightGroupList")]
    public List<HighlightGroup> HighlightGroupList { get; set; } = [];

    /// <summary>
    /// Legacy property for backward compatibility with old settings files that used the typo "hilightGroupList". This
    /// setter redirects data to the correct <see cref="HighlightGroupList"/> property. Will be removed in a future
    /// version once migration period is complete.
    /// </summary>
    [Obsolete("This property exists only for backward compatibility with old settings files. Use HighlightGroupList instead. This will be removed with version 1.50")]
    [Newtonsoft.Json.JsonProperty("hilightGroupList", DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore]
    public List<HighlightGroup> HilightGroupList
    {
        get => null; // Always return null so Newtonsoft.Json won't serialize this property
        set => HighlightGroupList = value ?? [];
    }

    public bool PortableMode { get; set; }

    /// <summary>
    /// Settings controlling display substitution of ASCII control characters (C0 + DEL) in the log grid.
    /// </summary>
    public ControlCharSettings ControlCharSettings { get; set; } = new();

    /// <summary>
    /// OBSOLETE: This setting is no longer used. It was originally intended to show an error dialog when "Allow Only
    /// One Instance" was enabled, but this behavior was incorrect (showed dialog on success instead of failure). The
    /// feature now works silently on success and only shows a warning on IPC failure. This property is kept for
    /// backward compatibility with old settings files but is no longer used or saved. Will be removed in a future
    /// version.
    /// </summary>
    [Obsolete("This setting is no longer used and will be removed in version 1.50. The 'Allow Only One Instance' feature now works silently.")]
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool ShowErrorMessageAllowOnlyOneInstances { get; set; }

    /// <summary>
    /// Maximum length of lines that can be read from log files at the reader level. Lines exceeding this length will be
    /// truncated during file reading operations. This setting protects against memory issues and performance
    /// degradation from extremely long lines.
    /// </summary>
    /// <remarks>
    /// <para> This property controls line truncation at the I/O reader level before lines are processed by columnizers.
    /// It is implemented in <see cref="Classes.Log.Streamreaders.PositionAwareStreamReaderSystem"/> and
    /// <see cref="Classes.Log.Streamreaders.PositionAwareStreamReaderLegacy"/>. </para> <para> Related property:
    /// <see cref="MaxDisplayLength"/> controls display-level truncation in UI columns, which must not exceed this
    /// value. Default is 20000 characters. </para>
    /// </remarks>
    public int MaxLineLength { get; set; } = 20000;

    /// <summary>
    /// Maximum length of text displayed in columns before truncation with "...". This is separate from
    /// <see cref="MaxLineLength"/> which controls reader-level line reading. Must not exceed
    /// <see cref="MaxLineLength"/>. Default is 20000 characters.
    /// </summary>
    public int MaxDisplayLength { get; set; } = 20000;

    public bool AllowOnlyOneInstance { get; set; }

    public bool AskForClose { get; set; }

    /// <summary>
    /// Color scheme applied at startup: forced light, forced dark, or follow the Windows theme.
    /// Takes effect after a restart. Replaces the old bool <c>DarkMode</c> setting (issue #698).
    /// </summary>
    public ColorMode ColorMode
    {
        get => _colorMode;
        set
        {
            _colorMode = value;
            _colorModeAssigned = true;
        }
    }

    private ColorMode _colorMode = ColorMode.Light;

    private bool _colorModeAssigned;

    /// <summary>
    /// Legacy property for backward compatibility with old settings files that stored the color scheme as the bool
    /// "DarkMode". true maps to <see cref="ColorMode.Dark"/>, false to <see cref="ColorMode.Light"/> — an unchecked
    /// box meant forced light, not follow-OS (issue #698). An explicit <see cref="ColorMode"/> value always wins.
    /// This setter redirects data on load; the property is never written back.
    /// </summary>
    [Obsolete("This property exists only for backward compatibility with old settings files. Use ColorMode instead. This will be removed with version 1.50")]
    [Newtonsoft.Json.JsonProperty("DarkMode", DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool? DarkMode
    {
        get => null; // Always return null so Newtonsoft.Json won't serialize this property
        set
        {
            if (!_colorModeAssigned && value.HasValue)
            {
                _colorMode = value.Value ? ColorMode.Dark : ColorMode.Light;
            }
        }
    }

    [Obsolete("This setting is no longer used and will be removed in version 1.50. The 'UseLegacyReader' now works with ReaderType.Legacy")]
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool UseLegacyReader { get; set; }

    public ReaderType ReaderType { get; set; } = ReaderType.SystemDirect;

    public List<ToolEntry> ToolEntries { get; set; } = [];

    public DragOrientations TimestampControlDragOrientation { get; set; } = DragOrientations.Horizontal;

    public bool TimestampControl { get; set; }

    public bool TimeSpreadTimeMode { get; set; }

    /// <summary>
    /// Save Directory of the last logfile
    /// </summary>
    public string SessionSaveDirectory { get; set; }

    public bool SaveFilters { get; set; } = true;

    public SessionSaveLocation SaveLocation { get; set; } = SessionSaveLocation.DocumentsDir;

    public bool SaveSessions { get; set; } = true;

    public bool SetLastColumnWidth { get; set; }

    public bool ShowBubbles { get; set; } = true;

    public bool ShowColumnFinder { get; set; }

    public Color ShowTailColor { get; set; } = Color.FromKnownColor(KnownColor.Blue);

    public bool ShowTailState { get; set; } = true;

    public bool ShowTimeSpread { get; set; }

    public Color TimeSpreadColor { get; set; } = Color.FromKnownColor(KnownColor.Gray);

    public bool IsAutoHideFilterList { get; set; }

    public bool IsFilterOnLoad { get; set; }

    public int LastColumnWidth { get; set; } = 2000;

    public int LinesPerBuffer { get; set; } = 500;

    public int MaximumFilterEntries { get; set; } = 30;

    public int MaximumFilterEntriesDisplayed { get; set; } = 20;

    /// <summary>
    /// Obsolete: replaced by <see cref="ColumnizerSelectionPriority"/>. Will be removed in 1.50. During settings load,
    /// <see cref="LegacyPreferencesMigrator"/> migrates a <c> true</c> value here to
    /// <see cref="ColumnizerSelectionPriority.MaskThenHistory"/> on the new property.
    /// </summary>
    [Obsolete("Replaced by ColumnizerSelectionPriority; will be removed in 1.50.")]
    public bool MaskPrio { get; set; }

    /// <summary>
    /// Controls the precedence order used to resolve a columnizer for a newly opened file.
    /// </summary>
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public ColumnizerSelectionPriority ColumnizerSelectionPriority { get; set; } = ColumnizerSelectionPriority.HistoryThenMask;

    public bool AutoPick { get; set; }

    //TODO Refactor Enum
    public MultiFileOption MultiFileOption { get; set; }

    //TODO Refactor Class
    public MultiFileOptions MultiFileOptions { get; set; }

    public bool MultiThreadFilter { get; set; } = true;

    public bool OpenLastFiles { get; set; } = true;

    public int PollingInterval { get; set; } = 250;

    public bool ReverseAlpha { get; set; }

    public int BufferCount { get; set; } = 100;

    public List<ColumnizerMaskEntry> ColumnizerMaskList { get; set; } = [];

    public string DefaultEncoding { get; set; } = "utf-8";

    public string DefaultLanguage { get; set; } = "en-US";

    public bool FilterSync { get; set; } = true;

    public bool FilterTail { get; set; } = true;

    public bool FollowTail { get; set; } = true;

    [Obsolete("This setting is no longer used and will be removed in version 1.50. The 'FontString' will be used for Importing / Exporting the Font")]
    public string FontName { get; set; } = "Courier New";

    public string FontString { get; set; } = "Courier New, 9pt, style=Regular";

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [SupportedOSPlatform("windows")]
    public Font Font
    {
        // Lazily materialize from FontString so callers that build Preferences directly (e.g. tests
        // that bypass ConfigManager.InitializeFont) get a usable Font without manual setup.
        // ConfigManager.InitializeFont still goes through the setter to install the canonical
        // instance during real config load.
        get => field ??= FontHelper.ParseFontStringOrDefault(FontString);
        set;
    }

    [Obsolete("This setting is no longer used and will be removed in version 1.50. The 'FontString' will be used for Importing / Exporting the Font")]
    public float FontSize { get => field; set => field = MathF.Round(value, 1); } = 9.0f;

    public List<HighlightMaskEntry> HighlightMaskList { get; set; } = [];
}
