using Newtonsoft.Json;

using System.Drawing;
using System.Text.RegularExpressions;

namespace LogExpert.Core.Classes.Highlight
{
    [Serializable]
    [method: JsonConstructor]
    public class HighlightEntry() : ICloneable
    {
        #region Fields

        [NonSerialized] private Regex regex = null;

        private string _searchText = string.Empty;

        #endregion Fields

        #region Properties

        public bool IsStopTail { get; set; }

        public bool IsSetBookmark { get; set; }

        public bool IsRegEx { get; set; }

        public bool IsCaseSensitive { get; set; }

        public Color ForegroundColor { get; set; }

        public Color BackgroundColor { get; set; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                regex = null;
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
                if (regex == null)
                {
                    if (IsRegEx)
                    {
                        regex = new Regex(SearchText, IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        regex = new Regex(Regex.Escape(SearchText), IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                    }
                }
                return regex;
            }
        }

        public bool IsWordMatch { get; set; }

        // highlightes search result
        [field: NonSerialized]
        public bool IsSearchHit { get; set; }

        public bool IsBold { get; set; }

        public bool NoBackground { get; set; }

        public object Clone()
        {
            var highLightEntry = new HighlightEntry
            {
                SearchText = SearchText,
                ForegroundColor = ForegroundColor,
                BackgroundColor = BackgroundColor,
                IsRegEx = IsRegEx,
                IsCaseSensitive = IsCaseSensitive,
                IsLedSwitch = IsLedSwitch,
                IsStopTail = IsStopTail,
                IsSetBookmark = IsSetBookmark,
                IsActionEntry = IsActionEntry,
                ActionEntry = ActionEntry != null ? (ActionEntry)ActionEntry.Clone() : null,
                IsWordMatch = IsWordMatch,
                IsBold = IsBold,
                BookmarkComment = BookmarkComment,
                NoBackground = NoBackground,
                IsSearchHit = IsSearchHit
            };

            return highLightEntry;
        }

        #endregion Properties
    }
}