using System.Globalization;
using System.Text.RegularExpressions;

using ColumnizerLib;

namespace LogExpert.Core.Classes.Columnizer;

public partial class ClfColumnizer : ILogLineMemoryColumnizer
{
    private const string DATE_TIME_FORMAT = "dd/MMM/yyyy:HH:mm:ss zzz";

    #region Fields

    private readonly Regex _lineRegex = LineRegex();

    private readonly CultureInfo _cultureInfo = new("en-US");
    private int _timeOffset;

    #endregion

    #region cTor

    // anon-212-34-174-126.suchen.de - - [08/Mar/2008:00:41:10 +0100] "GET /wiki/index.php?title=Bild:Poster_small.jpg&printable=yes&printable=yes HTTP/1.1" 304 0 "http://www.captain-kloppi.de/wiki/index.php?title=Bild:Poster_small.jpg&printable=yes" "gonzo1[P] +http://www.suchen.de/faq.html"
    public ClfColumnizer ()
    {
    }

    #endregion

    #region Public methods

    public bool IsTimeshiftImplemented ()
    {
        return true;
    }

    public void SetTimeOffset (int msecOffset)
    {
        _timeOffset = msecOffset;
    }

    public int GetTimeOffset ()
    {
        return _timeOffset;
    }

    public string GetName ()
    {
        return "Webserver CLF Columnizer";
    }

    public string GetDescription ()
    {
        return "Common Logfile Format used by webservers.";
    }

    public int GetColumnCount ()
    {
        return 8;
    }

    public string[] GetColumnNames ()
    {
        return ["IP", "User", "Date/Time", "Request", "Status", "Bytes", "Referrer", "User agent"];
    }

    /// <summary>
    /// Extracts the timestamp from the specified log line using the provided callback.
    /// </summary>
    /// <remarks>
    /// If the log line does not contain a valid timestamp in the expected column or format, the method returns
    /// DateTime.MinValue. The expected timestamp format and column position are determined by the implementation and
    /// may vary depending on the log source.
    /// </remarks>
    /// <param name="callback">
    /// A callback interface used to assist in parsing the log line and retrieving column information.
    /// </param>
    /// <param name="logLine">The log line from which to extract the timestamp.</param>
    /// <returns>
    /// A DateTime value representing the timestamp extracted from the log line. Returns DateTime.MinValue if the
    /// timestamp cannot be parsed or is not present.
    /// </returns>
    public DateTime GetTimestamp (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        // Use SplitLine to parse, then extract timestamp column
        var cols = SplitLine(callback, logLine);

        if (cols == null || cols.ColumnValues.Length < 8)
        {
            return DateTime.MinValue;
        }

        if (cols.ColumnValues[2] is not IColumnMemory dateColumn || dateColumn.FullValue.IsEmpty)
        {
            return DateTime.MinValue;
        }

        try
        {
            return DateTime.ParseExact(dateColumn.FullValue.Span, DATE_TIME_FORMAT, _cultureInfo);
        }
        catch (Exception ex) when (ex is ArgumentException or
                                         FormatException or
                                         ArgumentOutOfRangeException)
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Splits a log line into its constituent columns using the configured columnizer logic.
    /// </summary>
    /// <remarks>
    /// If the input line does not match the expected format, the entire line is placed in the request column. For lines
    /// longer than 1024 characters, only the first 1024 characters are used for columnization. The method does not
    /// localize column values.
    /// </remarks>
    /// <param name="callback">
    /// A callback interface used to provide additional context or services required during columnization. Cannot be
    /// null.
    /// </param>
    /// <param name="logLine">The log line to be split into columns. Cannot be null.</param>
    /// <returns>
    /// An object representing the columnized log line, with each column populated according to the parsed content of
    /// the input line.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Intentionally Passed")]
    public IColumnizedLogLineMemory SplitLine (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        ArgumentNullException.ThrowIfNull(logLine, nameof(logLine));
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        ColumnizedLogLine cLogLine = new()
        {
            LogLine = logLine
        };

        var columns = Column.CreateColumns(8, cLogLine);

        var lineMemory = logLine.FullLine;

        if (lineMemory.Length > 1024)
        {
            columns[3].FullValue = lineMemory[..1024];
            cLogLine.ColumnValues = [.. columns.Select(a => a as IColumnMemory)];
            return cLogLine;
        }

        var span = logLine.FullLine.Span;

        // 0         1         2         3         4         5         6         7         8         9         10        11        12        13        14        15        16
        // anon-212-34-174-126.suchen.de - - [08/Mar/2008:00:41:10 +0100] "GET /wiki/index.php?title=Bild:Poster_small.jpg&printable=yes&printable=yes HTTP/1.1" 304 0 "http://www.captain-kloppi.de/wiki/index.php?title=Bild:Poster_small.jpg&printable=yes" "gonzo1[P] +http://www.suchen.de/faq.html"
        // 192.168.1.10 - - [21/Oct/2024:12:34:56 +0000] "GET /api/v1/users HTTP/1.1" 200 1234 "https://example.com/" "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36" "-"0.025 0.025
        if (!_lineRegex.IsMatch(span))
        {
            // Pattern didn't match - put entire line in request column
            columns[3].FullValue = lineMemory;
            cLogLine.ColumnValues = [.. columns.Select(a => a as IColumnMemory)];
            return cLogLine;
        }

        // To extract regex group captures, we must convert to string.
        // This is an unavoidable allocation - .NET Regex doesn't provide
        // a way to get group capture positions from ReadOnlySpan<char>.
        // However, GetGroupMemory() will slice the original ReadOnlyMemory,
        // so we avoid allocating strings for each captured group.
        var lineString = logLine.FullLine.ToString();
        var match = _lineRegex.Match(lineString);

        if (match.Groups.Count == 10)
        {
            columns[0].FullValue = GetGroupMemory(lineMemory, match.Groups[1]);
            columns[1].FullValue = GetGroupMemory(lineMemory, match.Groups[3]);
            columns[3].FullValue = GetGroupMemory(lineMemory, match.Groups[5]);
            columns[4].FullValue = GetGroupMemory(lineMemory, match.Groups[6]);
            columns[5].FullValue = GetGroupMemory(lineMemory, match.Groups[7]);
            columns[6].FullValue = GetGroupMemory(lineMemory, match.Groups[8]);
            columns[7].FullValue = GetGroupMemory(lineMemory, match.Groups[9]);

            var dateTimeMemory = GetGroupMemory(lineMemory, match.Groups[4]);

            if (dateTimeMemory.Length > 2)
            {
                // Skip '[' at start and ']' at end
                dateTimeMemory = dateTimeMemory[1..^1];
            }

            var dateSpan = dateTimeMemory.Span;

            // dirty probing of date/time format (much faster than DateTime.ParseExact()
            if (dateSpan.Length >= 12 && dateSpan[2] == '/' && dateSpan[6] == '/' && dateSpan[11] == ':')
            {
                if (_timeOffset != 0)
                {
                    try
                    {
                        var dateTime = DateTime.ParseExact(dateSpan, DATE_TIME_FORMAT, _cultureInfo);
                        dateTime = dateTime.Add(new TimeSpan(0, 0, 0, 0, _timeOffset));
                        var newDate = dateTime.ToString(DATE_TIME_FORMAT, _cultureInfo);
                        columns[2].FullValue = newDate.AsMemory();
                    }
                    catch (Exception ex) when (ex is ArgumentException or
                                                     FormatException or
                                                     ArgumentOutOfRangeException)
                    {
                        columns[2].FullValue = "n/a".AsMemory();
                    }
                }
                else
                {
                    columns[2].FullValue = dateTimeMemory;
                }
            }
            else
            {
                columns[2].FullValue = dateTimeMemory;
            }
        }
        else
        {
            // Regex matched but unexpected group count - put full line in request column
            columns[3].FullValue = lineMemory;
        }

        cLogLine.ColumnValues = [.. columns.Select(a => a as IColumnMemory)];
        return cLogLine;
    }

    /// <summary>
    /// Converts a Regex Group capture to ReadOnlyMemory slice from original line
    /// </summary>
    //TODO Extract to utility class
    private static ReadOnlyMemory<char> GetGroupMemory (ReadOnlyMemory<char> lineMemory, Group group)
    {
        if (!group.Success || group.Length == 0)
        {
            return ReadOnlyMemory<char>.Empty;
        }

        // Use group's Index and Length to slice original memory
        // This avoids allocating a new string for the group value
        return lineMemory.Slice(group.Index, group.Length);
    }

    public string GetCustomName ()
    {
        return GetName();
    }

    /// <summary>
    /// Processes a value change for a specified column and notifies the callback of the update.
    /// </summary>
    /// <remarks>
    /// If the column index is 2, the method attempts to interpret the values as date and time strings and calculates
    /// the time offset in milliseconds. No action is taken for other column indices.
    /// </remarks>
    /// <param name="callback">The callback interface used to handle column value updates.</param>
    /// <param name="column">The zero-based index of the column for which the value is being updated.</param>
    /// <param name="value">The new value to be set for the specified column.</param>
    /// <param name="oldValue">The previous value of the specified column before the update.</param>
    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, string oldValue)
    {
        PushValue(callback, column, value, oldValue.AsMemory());
    }

    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, ReadOnlyMemory<char> oldValue)
    {
        if (column == 2)
        {
            try
            {
                var newDateTime = DateTime.ParseExact(value, DATE_TIME_FORMAT, _cultureInfo);
                var oldDateTime = DateTime.ParseExact(oldValue.ToString(), DATE_TIME_FORMAT, _cultureInfo);
                var mSecsOld = oldDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                var mSecsNew = newDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                _timeOffset = (int)(mSecsNew - mSecsOld);
            }
            catch (FormatException)
            {
            }
        }
    }

    /// <summary>
    /// Provides a compiled regular expression used to parse lines matching a specific log entry format.
    /// </summary>
    /// <remarks>
    /// The regular expression is precompiled for performance and is intended to extract fields from log lines with a
    /// fixed format. The pattern captures multiple groups, including text fields and quoted values. Use the returned
    /// <see cref="Regex"/> to match and extract data from log entries conforming to this structure.
    /// </remarks>
    /// <returns>A <see cref="Regex"/> instance that matches lines with the expected log entry structure.</returns>
    [GeneratedRegex(@"^(\S+) (\S+) (\S+) (\[[^\]]+\]) ("".*?"") (\S+) (\S+) ("".*?"") ("".*?"")")]
    private static partial Regex LineRegex ();

    #endregion
}