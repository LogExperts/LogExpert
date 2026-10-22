using System.Globalization;

using ColumnizerLib;

namespace GlassfishColumnizer;

internal class GlassfishColumnizer : ILogLineMemoryXmlColumnizer
{
    #region Fields

    public const int COLUMN_COUNT = 2;
    private const string DATETIME_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffzzzz";
    private const string DATETIME_FORMAT_OUT = "yyyy-MM-dd HH:mm:ss.fff";
    private const int MIN_TIMESTAMP_LENGTH = 28;
    private const char SEPARATOR_CHAR = '|';

    private static readonly XmlConfig _xmlConfig = new();

    //We keep it, just don't know where it comes from
    //private readonly char[] trimChars = ['|'];

    private readonly CultureInfo _cultureInfo = new("en-US");
    private int _timeOffset;

    #endregion

    #region cTor

    public GlassfishColumnizer ()
    {
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Gets the current XML log configuration.
    /// </summary>
    /// <returns>An object that provides access to the XML log configuration settings.</returns>
    public IXmlLogConfiguration GetXmlLogConfiguration ()
    {
        return _xmlConfig;
    }

    public string GetName ()
    {
        return "Glassfish";
    }

    public string GetCustomName ()
    {
        return GetName();
    }

    public string GetDescription ()
    {
        return "Parse the timestamps in Glassfish logfiles.";
    }

    public int GetColumnCount ()
    {
        return COLUMN_COUNT;
    }

    public string[] GetColumnNames ()
    {
        return ["Date/Time", "Message"];
    }

    /// <summary>
    /// Creates a new log line instance with text formatted for clipboard copying.
    /// </summary>
    /// <remarks>
    /// The returned log line replaces separator characters in the original line with the '|' character to ensure
    /// compatibility with clipboard operations.
    /// </remarks>
    /// <param name="logLine">The log line to be formatted for clipboard use. Cannot be null.</param>
    /// <param name="callback">
    /// A callback interface for columnizer operations. This parameter is reserved for future use and is not utilized in
    /// this method.
    /// </param>
    /// <returns>
    /// A new <see cref="ILogLineMemory"/> instance containing the clipboard-formatted text of the specified log line.
    /// </returns>
    public ILogLineMemory GetLineTextForClipboard (ILogLineMemory logLine, ILogLineMemoryColumnizerCallback callback)
    {
        return new GlassFishLogLine(ReplaceInMemory(logLine.FullLine, SEPARATOR_CHAR, '|'), logLine.Text, logLine.LineNumber);
    }

    /// <summary>
    /// Parses a log line into its constituent columns according to the columnizer's format.
    /// </summary>
    /// <remarks>
    /// If the input line does not conform to the expected format or is too short, only the log message column is
    /// populated and date/time columns are left blank. The method is tolerant of malformed input and will not throw for
    /// common formatting issues.
    /// </remarks>
    /// <param name="callback">
    /// A callback interface used to provide context or services required during columnization.
    /// </param>
    /// <param name="logLine">The log line to be split into columns.</param>
    /// <returns>
    /// An object representing the columnized log line, with each column populated based on the input line. If the line
    /// does not match the expected format, the entire line is placed in the log message column.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Intentionally passed")]
    public IColumnizedLogLineMemory SplitLine (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        //[#|2025-03-14T10:36:37.159846Z|INFO|glassfish|javax.enterprise.system.core.server|_ThreadID=14;_ThreadName=main;| GlassFish Server Open Source Edition 5.1.0 (5.1.0) startup time : milliseconds 987 |#]
        //[#|2008-08-24T08:58:38.325+0200|INFO|sun-appserver9.1|STC.eWay.batch.com.stc.connector.batchadapter.system.BatchInboundWork|_ThreadID=43;_ThreadName=p: thread-pool-1; w: 7;|BATCH-MSG-M0992: Another Work item already checking for files... |#]
        //[#|2025-03-14T10:40:00.000Z|WARNING|glassfish|javax.enterprise.system.container.web|_ThreadID=25;_ThreadName=http-thread-pool-8080-4;|Potential security issue detected: multiple applications are sharing the same session cookie name in the same domain. |#]
        //[#|2025-03-14T10:45:15.220Z|SEVERE|glassfish|javax.enterprise.system.core|_ThreadID=10;_ThreadName=main;|CORE5004: Exception during GlassFish Server startup. Aborting startup.|#]

        ColumnizedLogLine cLogLine = new()
        {
            LogLine = logLine
        };

        var columns = Column.CreateColumns(COLUMN_COUNT, cLogLine);

        var temp = logLine.FullLine;

        // delete '[#|' and '|#]'
        if (temp.Span.StartsWith("[#|", StringComparison.OrdinalIgnoreCase))
        {
            temp = temp[3..];
        }

        if (temp.Span.EndsWith("|#]", StringComparison.OrdinalIgnoreCase))
        {
            temp = temp[..^3];
        }

        // If the line is too short (i.e. does not follow the format for this columnizer) return the whole line content
        // in column 2 (the log message column). Date and time column will be left blank.
        if (temp.Length < MIN_TIMESTAMP_LENGTH)
        {
            columns[1].FullValue = temp;
            cLogLine.ColumnValues = [.. columns.Select(a => a as IColumnMemory)];
            return cLogLine;
        }

        try
        {
            var dateTime = GetTimestamp(callback, logLine);
            if (dateTime == DateTime.MinValue)
            {
                columns[1].FullValue = temp;
                cLogLine.ColumnValues = [.. columns.Select(a => a as IColumnMemory)];
                return cLogLine;
            }

            var newDate = dateTime.ToString(DATETIME_FORMAT_OUT, CultureInfo.InvariantCulture);
            columns[0].FullValue = newDate.AsMemory();

            var cols = SplitIntoTwo(temp, SEPARATOR_CHAR);

            // Check if separator was found (cols[1] would be empty if not found)
            if (cols[1].IsEmpty)
            {
                columns[0].FullValue = ReadOnlyMemory<char>.Empty;
                columns[1].FullValue = temp;
            }
            else
            {
                // Keep the formatted timestamp in column 0
                columns[1].FullValue = cols[1];
            }
        }
        catch (Exception ex) when (ex is ArgumentException or
                                         FormatException or
                                         ArgumentOutOfRangeException)
        {
            columns[0].FullValue = "n/a".AsMemory();
            columns[1].FullValue = temp;
        }

        cLogLine.ColumnValues = [.. columns.Select(a => a as IColumnMemory)];
        return cLogLine;
    }

    /// <summary>
    /// Splits ReadOnlyMemory into two parts at the first occurrence of separator
    /// </summary>
    /// <param name="input">The memory to split</param>
    /// <param name="separator">The separator character</param>
    /// <returns>
    /// Array with 2 elements: [before separator, after separator]. If separator not found, returns [input, Empty]
    /// </returns>
    private static ReadOnlyMemory<char>[] SplitIntoTwo (ReadOnlyMemory<char> input, char separator)
    {
        var span = input.Span;
        var index = span.IndexOf(separator);

        if (index == -1)
        {
            // No separator found - return whole input in first element
            return [input, ReadOnlyMemory<char>.Empty];
        }

        // Split at the separator
        return
        [
            input[..index],           // Before separator
            input[(index + 1)..]      // After separator (skip the separator itself)
        ];
    }

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

    /// <summary>
    /// Extracts the timestamp from the specified log line using the expected GlassFish log format.
    /// </summary>
    /// <remarks>
    /// The method expects the log line to contain a timestamp in a specific format, typically used by GlassFish logs.
    /// If the log line does not match the expected format or the timestamp cannot be parsed, the method returns
    /// DateTime.MinValue.
    /// </remarks>
    /// <param name="callback">
    /// A callback interface for columnizer operations. This parameter is not used by this method but is required by the
    /// interface.
    /// </param>
    /// <param name="logLine">
    /// The log line from which to extract the timestamp. Must not be null and should contain a timestamp in the
    /// expected format.
    /// </param>
    /// <returns>
    /// A DateTime value representing the parsed timestamp from the log line. Returns DateTime.MinValue if the timestamp
    /// cannot be extracted or parsed.
    /// </returns>
    public DateTime GetTimestamp (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        var temp = logLine.FullLine;

        // delete '[#|' and '|#]'
        if (temp.Span.StartsWith("[#|", StringComparison.OrdinalIgnoreCase))
        {
            temp = temp[3..];
        }

        if (temp.Span.EndsWith("|#]", StringComparison.OrdinalIgnoreCase))
        {
            temp = temp[..^3];
        }

        if (temp.Span.Length < MIN_TIMESTAMP_LENGTH)
        {
            return DateTime.MinValue;
        }

        var endIndex = temp.Span.IndexOf(SEPARATOR_CHAR);
        if (endIndex is > MIN_TIMESTAMP_LENGTH or < 0)
        {
            return DateTime.MinValue;
        }

        var value = temp[..endIndex];

        if (!DateTime.TryParseExact(value.Span, DATETIME_FORMAT, _cultureInfo, DateTimeStyles.None, out var timestamp))
        {
            return DateTime.MinValue;
        }

        try
        {
            return timestamp.AddMilliseconds(_timeOffset);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Updates the internal time offset based on the difference between the specified new and old values when the
    /// column index is zero.
    /// </summary>
    /// <remarks>
    /// If the column index is not zero, this method performs no action. For column 0, both value and oldValue must be
    /// valid date and time strings in the required format; otherwise, the time offset is not updated.
    /// </remarks>
    /// <param name="callback">
    /// The callback interface for columnizer operations. This parameter is not used in this method but may be required
    /// for interface compatibility.
    /// </param>
    /// <param name="column">
    /// The zero-based index of the column to update. Only a value of 0 triggers a time offset update.
    /// </param>
    /// <param name="value">
    /// The new value to apply. For column 0, this should be a date and time string in the expected format.
    /// </param>
    /// <param name="oldValue">
    /// The previous value to compare against. For column 0, this should be a date and time string in the expected
    /// format.
    /// </param>
    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, string oldValue)
    {
        PushValue(callback, column, value, oldValue.AsMemory());
    }

    /// <summary>
    /// Updates the internal time offset based on the difference between the specified new and old values when the
    /// column index is zero.
    /// </summary>
    /// <remarks>
    /// If the column index is not zero, this method performs no action. For column 0, both value and oldValue must be
    /// valid date and time strings in the required format; otherwise, the time offset is not updated.
    /// </remarks>
    /// <param name="callback">
    /// The callback interface for columnizer operations. This parameter is not used in this method but may be required
    /// for interface compatibility.
    /// </param>
    /// <param name="column">
    /// The zero-based index of the column to update. Only a value of 0 triggers a time offset update.
    /// </param>
    /// <param name="value">
    /// The new value to apply. For column 0, this should be a date and time string in the expected format.
    /// </param>
    /// <param name="oldValue">
    /// The previous value to compare against. For column 0, this should be a date and time string in the expected
    /// format.
    /// </param>
    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, ReadOnlyMemory<char> oldValue)
    {
        if (column == 0)
        {
            try
            {
                var newDateTime = DateTime.ParseExact(value, DATETIME_FORMAT_OUT, _cultureInfo);
                var oldDateTime = DateTime.ParseExact(oldValue.ToString(), DATETIME_FORMAT_OUT, _cultureInfo);
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
    /// Replaces all occurrences of a character in ReadOnlyMemory<char> (optimized)
    /// </summary>
    //TODO: Extract to a common utility class
    private static ReadOnlyMemory<char> ReplaceInMemory (ReadOnlyMemory<char> input, char oldChar, char newChar)
    {
        var span = input.Span;

        // check is there anything to replace?
        if (!span.Contains(oldChar))
        {
            return input;
        }

        // Allocate new buffer only when needed
        var buffer = new char[input.Length];

        for (var i = 0; i < span.Length; i++)
        {
            buffer[i] = span[i] == oldChar ? newChar : span[i];
        }

        return buffer.AsMemory();
    }

    #endregion
}