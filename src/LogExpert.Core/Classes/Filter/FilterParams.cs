using System.Collections;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LogExpert.Core.Classes.Filter;

[Serializable]
public class FilterParams : ICloneable
{
    #region Fields
    //public List<string> historyList = new List<string>();
    //public List<string> rangeHistoryList = new List<string>();

    #endregion

    #region Properties

    public string SearchText { get; set; }

    public string RangeSearchText { get; set; }

    //public bool SpreadEnabled => SpreadBefore > 0 || SpreadBehind > 0;

    public bool IsCaseSensitive { get; set; }

    public bool IsFilterTail { get; set; }

    public int FuzzyValue { get; set; }

    public bool EmptyColumnUsePrev { get; set; }

    public bool EmptyColumnHit { get; set; }

    public bool ExactColumnMatch { get; set; }

    public bool ColumnRestrict { get; set; }

    public Color Color { get; set; } = Color.Black;

    public int SpreadBefore { get; set; }

    public int SpreadBehind { get; set; }

    public bool IsInvert { get; set; }

    public bool IsRangeSearch { get; set; }

    public bool IsRegex { get; set; }

    // list of columns in which to search
    public Collection<int> ColumnList { get; } = [];

    [JsonIgnore]
    [field: NonSerialized]
    public ILogLineColumnizer CurrentColumnizer { get; set; }

    /// <summary>
    /// false=looking for start
    /// true=looking for end
    /// </summary>
    [field: NonSerialized]
    public bool IsInRange { get; set; }

    [field: NonSerialized]
    public string LastLine { get; set; } = string.Empty;

    [field: NonSerialized]
    public Hashtable LastNonEmptyCols { get; set; } = [];

    [field: NonSerialized]
    public bool LastResult { get; set; }

    ///Returns RangeSearchText.ToUpperInvariant
    [JsonIgnore]
    internal string NormalizedRangeSearchText => RangeSearchText.ToUpperInvariant();

    ///Returns SearchText.ToUpperInvariant
    [JsonIgnore]
    internal string NormalizedSearchText => SearchText.ToUpperInvariant();

    [field: NonSerialized]
    public Regex RangeRex { get; set; }

    [field: NonSerialized]
    public Regex Rex { get; set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Returns a new FilterParams object with the current columnizer set to the one used in this object.
    /// </summary>
    /// <returns></returns>
    public FilterParams CloneWithCurrentColumnizer ()
    {
        FilterParams newParams = Clone();
        newParams.Init();
        // removed cloning of columnizer for filtering, because this causes issues with columnizers that hold internal states (like CsvColumnizer)
        // newParams.currentColumnizer = Util.CloneColumnizer(this.currentColumnizer);
        newParams.CurrentColumnizer = CurrentColumnizer;
        return newParams;
    }

    // call after deserialization!
    public void Init ()
    {
        LastNonEmptyCols = [];
        LastLine = string.Empty;
    }

    // Reset before a new search
    public void Reset ()
    {
        LastNonEmptyCols.Clear();
        IsInRange = false;
    }

    public void CreateRegex ()
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
    public FilterParams Clone ()
    {
        return (FilterParams)MemberwiseClone();
    }

    /// <summary>
    /// Shallow Copy
    /// </summary>
    /// <returns></returns>
    object ICloneable.Clone ()
    {
        return Clone();
    }

    #endregion
}