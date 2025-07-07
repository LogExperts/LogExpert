using CsvHelper.Configuration;

using Newtonsoft.Json;

using System;
using System.Globalization;

namespace CsvColumnizer;

[Serializable]
public class CsvColumnizerConfig
{
    #region Fields

    public char CommentChar { get; set; }

    public string DelimiterChar { get; set; }

    public char EscapeChar { get; set; }

    public bool HasFieldNames { get; set; }

    public int MinColumns { get; set; }

    public char QuoteChar { get; set; }

    public int VersionBuild { get; set; }

    [JsonIgnore]
    public IReaderConfiguration ReaderConfiguration { get; set; }

    #endregion

    #region Public methods

    public void InitDefaults()
    {
        ReaderConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            Escape = '"',
            Quote = '"',
            Comment = '#',
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.None
        };

        DelimiterChar = ReaderConfiguration.Delimiter;
        EscapeChar = ReaderConfiguration.Escape;
        QuoteChar = ReaderConfiguration.Quote;
        CommentChar = ReaderConfiguration.Comment;
        HasFieldNames = ReaderConfiguration.HasHeaderRecord;
        MinColumns = 0;
    }

    public void ConfigureReaderConfiguration()
    {
        ReaderConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = DelimiterChar,
            Escape = EscapeChar,
            Quote = QuoteChar,
            Comment = CommentChar,
            HasHeaderRecord = HasFieldNames
        };
    }

    #endregion
}