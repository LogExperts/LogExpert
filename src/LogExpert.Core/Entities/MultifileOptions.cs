namespace LogExpert.Core.Entities;

[Serializable]
public class MultiFileOptions
{
    #region Properties

    /// <summary>
    /// Gets or sets the maximum age, in days, of companion files loaded into a MultiFile. For date-based patterns,
    /// this also limits how many missing calendar days are searched. Zero disables the file-age limit.
    /// </summary>
    public int MaxDayTry { get; set; } = 3;

    public string FormatPattern { get; set; } = "*$J(.)";

    #endregion
}
