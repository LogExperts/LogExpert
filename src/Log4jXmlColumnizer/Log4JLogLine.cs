using LogExpert;

namespace Log4jXmlColumnizer;

internal class Log4JLogLine : ILogLine
{
    #region Properties

    public string FullLine { get; set; }

    public int LineNumber { get; set; }

    string ITextValue.Text => FullLine;

    #endregion
}