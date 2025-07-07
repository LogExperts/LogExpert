namespace LogExpert.Core.Classes;

public class PatternBlock
{
    public int BlockId { get; set; }

    public int EndLine { get; set; }

    // key: line num
    public Dictionary<int, QualityInfo> QualityInfoList { get; set; } = [];

    public SortedDictionary<int, int> SrcLines { get; set; } = [];

    public int StartLine { get; set; }

    public int TargetEnd { get; set; }

    public SortedDictionary<int, int> TargetLines { get; set; } = [];

    public int TargetStart { get; set; }

    public int Weigth { get; set; }

    #region Public methods

    public override string ToString ()
    {
        return $"srcStart={StartLine}, srcEnd={EndLine}, targetStart={TargetStart}, targetEnd={TargetEnd}, weight={Weigth}";
    }

    #endregion
}