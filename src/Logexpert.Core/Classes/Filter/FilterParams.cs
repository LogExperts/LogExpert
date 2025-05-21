﻿using System.Collections;
using System.Drawing;
using System.Text.RegularExpressions;

namespace LogExpert.Core.Classes.Filter
{
    [Serializable]
    public class FilterParams
    {
        #region Fields

        private string _rangeSearchText = string.Empty;
        private string _searchText = string.Empty;

        //public List<string> historyList = new List<string>();
        //public List<string> rangeHistoryList = new List<string>();

        #endregion

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                LowerSearchText = _searchText.ToLowerInvariant();
            }
        }

        public string RangeSearchText
        {
            get => _rangeSearchText;
            set
            {
                _rangeSearchText = value;
                LowerRangeSearchText = _rangeSearchText.ToLowerInvariant();
            }
        }

        public bool SpreadEnabled => SpreadBefore > 0 || SpreadBehind > 0;

        public bool IsCaseSensitive { get; set; }

        public bool IsFilterTail { get; set; }

        public int FuzzyValue { get; set; }

        public bool EmptyColumnUsePrev { get; set; }

        public bool EmptyColumnHit { get; set; }

        public bool ExactColumnMatch { get; set; } = false;

        public bool ColumnRestrict { get; set; }

        public Color Color { get; set; } = Color.Black;

        public int SpreadBefore { get; set; }

        public int SpreadBehind { get; set; }

        public bool IsInvert { get; set; }

        public bool IsRangeSearch { get; set; } = false;

        public bool IsRegex { get; set; }

        // list of columns in which to search
        public List<int> ColumnList { get; set; } = [];

        [field: NonSerialized]
        public ILogLineColumnizer CurrentColumnizer { get; set; }

        /// <summary>
        /// false=looking for start
        /// true=looking for end
        /// </summary>
        [field: NonSerialized]
        public bool IsInRange { get; set; } = false;

        [field: NonSerialized]
        public string LastLine { get; set; } = string.Empty;

        [field: NonSerialized]
        public Hashtable LastNonEmptyCols { get; set; } = [];

        [field: NonSerialized]
        public bool LastResult { get; set; }

        [field: NonSerialized]
        public string LowerRangeSearchText { get; set; } = string.Empty;

        [field: NonSerialized]
        public string LowerSearchText { get; set; } = string.Empty;

        [field: NonSerialized]
        public Regex RangeRex { get; set; }

        [field: NonSerialized]
        public Regex Rex { get; set; }

        #endregion

        #region Public methods

        public FilterParams CopyCurrentColumnizer()
        {
            FilterParams newParams = MemberwiseCopy();
            newParams.Init();
            // removed cloning of columnizer for filtering, because this causes issues with columnizers that hold internal states (like CsvColumnizer)
            // newParams.currentColumnizer = Util.CloneColumnizer(this.currentColumnizer);
            newParams.CurrentColumnizer = CurrentColumnizer;
            return newParams;
        }

        // call after deserialization!
        public void Init()
        {
            LastNonEmptyCols = [];
            LowerRangeSearchText = RangeSearchText.ToLower();
            LowerSearchText = SearchText.ToLower();
            LastLine = string.Empty;
        }

        // Reset before a new search
        public void Reset()
        {
            LastNonEmptyCols.Clear();
            IsInRange = false;
        }

        public void CreateRegex()
        {
            if (SearchText != null)
            {
                Rex = new Regex(SearchText, IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            }
            if (RangeSearchText != null && IsRangeSearch)
            {
                RangeRex = new Regex(RangeSearchText, IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            }
        }

        /// <summary>
        /// Shallow Copy
        /// </summary>
        /// <returns></returns>
        public FilterParams MemberwiseCopy()
        {
            return (FilterParams)MemberwiseClone();
        }

        #endregion
    }
}