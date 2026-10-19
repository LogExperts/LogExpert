namespace LogExpert.Core.Entities;

[Serializable]
public class MultiFileOptions
{
    #region Properties

    /// <summary>
    /// Gets or sets how many consecutive days without a file may be skipped when walking a date pattern.
    /// </summary>
    public int MaxDayTry { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum age, in days, of rollover files loaded into a MultiFile. Zero disables the limit.
    /// </summary>
    public int MaxFileAgeDays { get; set; }

    public string FormatPattern { get; set; } = "*$J(.)";

    #endregion
}