using LogExpert;

namespace GlassfishColumnizer;

internal class GlassFishLogLine : ILogLine
{
    #region Properties

    public string FullLine { get; set; }

    public int LineNumber { get; set; }

    string ITextValue.Text => FullLine;

    #endregion
}