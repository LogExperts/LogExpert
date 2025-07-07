namespace LogExpert;

public interface ILogLine : ITextValue
{
    #region Properties

    string FullLine { get; }

    int LineNumber { get; }

    #endregion
}