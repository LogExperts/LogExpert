namespace LogExpert.Core.Entities;

[Serializable]
public class MultiFileOptions
{
    #region Properties

    /// <summary>
    /// The largest file age exposed by the UI and accepted by the rollover reader, in days.
    /// </summary>
    public const int MAX_FILE_AGE_DAYS = 3650;

    public int MaxDayTry { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum age, in days, of rollover files loaded into a MultiFile. Zero disables the limit.
    /// Values greater than <see cref="MAX_FILE_AGE_DAYS"/> are treated as that maximum.
    /// </summary>
    public int MaxFileAgeDays { get; set; }

    public static int ClampMaxFileAgeDays (int value)
    {
        return Math.Clamp(value, 0, MAX_FILE_AGE_DAYS);
    }

    public string FormatPattern { get; set; } = "*$J(.)";

    #endregion
}