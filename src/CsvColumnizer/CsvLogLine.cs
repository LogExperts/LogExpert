using LogExpert;

namespace CsvColumnizer;

public class CsvLogLine(string fullLine, int lineNumber) : ILogLine
{
    #region Properties

    public string FullLine { get; set; } = fullLine;

    public int LineNumber { get; set; } = lineNumber;

    string ITextValue.Text => FullLine;

    #endregion
}