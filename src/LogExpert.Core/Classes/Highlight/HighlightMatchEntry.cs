namespace LogExpert.Core.Classes.Highlight;

/// <summary>
/// Class for storing word-wise highlight matches. Used for colouring different matches on one line.
/// </summary>
public class HighlightMatchEntry
{
    #region Properties

    public HighlightEntry HighlightEntry { get; set; }

    public int StartPos { get; set; }

    public int Length { get; set; }

    #endregion

    #region Public methods

    public override string ToString ()
    {
        return $"{HighlightEntry.SearchText}/{StartPos}/{Length}";
    }

    #endregion
}