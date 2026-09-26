using System.Drawing;
using System.Text.RegularExpressions;

using LogExpert.Core.Helpers;

using Newtonsoft.Json;

namespace LogExpert.Core.Classes.Highlight;

[Serializable]
[method: JsonConstructor]
public class HighlightEntry () : ICloneable
{
    #region Fields

    [NonSerialized] private Regex _regex = null;

    private string _searchText = string.Empty;

    #endregion Fields

    #region Properties

    public bool IsStopTail { get; set; }

    public bool IsSetBookmark { get; set; }

    public bool IsRegex { get; set; }

    public bool IsCaseSensitive { get; set; }

    public Color ForegroundColor { get; set; }

    public Color BackgroundColor { get; set; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            _regex = null;
        }
    }

    public bool IsLedSwitch { get; set; }

    public ActionEntry ActionEntry { get; set; }

    public bool IsActionEntry { get; set; }

    public string BookmarkComment { get; set; }

    public Regex Regex
    {
        get
        {
            _regex ??= IsRegex
                    ? RegexHelper.GetOrCreateCached(SearchText, IsCaseSensitive
                        ? RegexOptions.None
                        : RegexOptions.IgnoreCase)
                    : RegexHelper.GetOrCreateCached(Regex.Escape(SearchText),
                                IsCaseSensitive
                                ? RegexOptions.None
                                : RegexOptions.IgnoreCase);

            return _regex;
        }
    }

    public bool IsWordMatch { get; set; }

    // Highlight search result
    [field: NonSerialized]
    public bool IsSearchHit { get; set; }

    public bool IsBold { get; set; }

    /// <summary>
    /// When true, a sound is played whenever a newly tailed line matches this
    /// entry. The sound is throttled globally across all log windows; see
    /// <see cref="CooldownSeconds"/>.
    /// </summary>
    public bool AlertOnHit { get; set; }

    /// <summary>
    /// Absolute path to an audio file to play when <see cref="AlertOnHit"/> fires.
    /// When empty or null, the Windows default beep is played instead.
    /// </summary>
    public string SoundFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Minimum seconds between two alert plays (process-wide, across all
    /// highlight entries and log windows). <c>0</c> disables throttling.
    /// </summary>
    public int CooldownSeconds { get; set; } = 2;

    public bool NoBackground { get; set; }

    /// <summary>
    /// When true, lines matching this entry are removed from the Log Window's main grid. Display state only:
    /// the lines stay in the reader and remain eligible for search, filtering and triggers.
    /// </summary>
    public bool IsHideLine { get; set; }

    public object Clone ()
    {
        var highLightEntry = new HighlightEntry
        {
            SearchText = SearchText,
            ForegroundColor = ForegroundColor,
            BackgroundColor = BackgroundColor,
            IsRegex = IsRegex,
            IsCaseSensitive = IsCaseSensitive,
            IsLedSwitch = IsLedSwitch,
            IsStopTail = IsStopTail,
            IsSetBookmark = IsSetBookmark,
            IsActionEntry = IsActionEntry,
            ActionEntry = ActionEntry != null ? (ActionEntry)ActionEntry.Clone() : null,
            IsWordMatch = IsWordMatch,
            IsBold = IsBold,
            AlertOnHit = AlertOnHit,
            SoundFilePath = SoundFilePath,
            CooldownSeconds = CooldownSeconds,
            BookmarkComment = BookmarkComment,
            NoBackground = NoBackground,
            IsHideLine = IsHideLine,
            IsSearchHit = IsSearchHit
        };

        return highLightEntry;
    }

    #endregion Properties
}