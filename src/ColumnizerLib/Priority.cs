namespace LogExpert;

/// <summary>
/// Priority for columnizer.
/// </summary>
public enum Priority
{
    /// <summary>
    /// Not support target file.
    /// </summary>
    NotSupport = 0,

    /// <summary>
    /// Can support target file. E.g. default one CanSupport most of the files.
    /// </summary>
    CanSupport,

    /// <summary>
    /// Target file is soundly supported. E.g. JsonColumnizer WellSupport all json files.
    /// </summary>
    WellSupport,

    /// <summary>
    /// The columnizer is designed to support target file. E.g. JsonCompactColumnizer PerfectlySupport all compact Json files.
    /// </summary>
    PerfectlySupport,

    /// <summary>
    /// Target file is only supported by this columnizer.
    /// </summary>
    Exclusive
}