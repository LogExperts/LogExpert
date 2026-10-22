using ColumnizerLib;

namespace LogExpert.Core.Classes.Columnizer;

public class TimestampColumnizer : ILogLineMemoryColumnizer, IColumnizerPriorityMemory
{
    #region ILogLineColumnizer implementation

    private int _timeOffset;
    private readonly TimeFormatDeterminer _timeFormatDeterminer = new();

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
        return "Timestamp Columnizer";
    }

    public string GetCustomName () => GetName();

    public string GetDescription ()
    {
        return "Splits every line into 3 fields: Date, Time and the rest of the log message";
    }

    public int GetColumnCount ()
    {
        return 3;
    }

    public string[] GetColumnNames ()
    {
        return ["Date", "Time", "Message"];
    }

    /// <summary>
    /// Splits a log line into its constituent columns, typically separating date, time, and the remainder of the line.
    /// </summary>
    /// <remarks>
    /// If the log line does not match a recognized date/time format, the entire line is returned as a single column.
    /// Columns typically represent the date, time, and the rest of the log entry. If parsing fails due to format
    /// issues, column values are set to "n/a" except for the remainder, which contains the original line.
    /// </remarks>
    /// <param name="callback">
    /// A callback interface used to provide additional context or services required during columnization.
    /// </param>
    /// <param name="logLine">The log line to be split into columns. Cannot be null.</param>
    /// <returns>
    /// An object representing the columnized log line, with each column containing a segment of the original log line.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Intentionally passed")]
    public IColumnizedLogLineMemory SplitLine (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        ArgumentNullException.ThrowIfNull(logLine, nameof(logLine));
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        // 0         1         2         3         4         5         6         7         8         9         10        11        12        13        14        15        16
        // 012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
        // 03.01.2008 14:48:00.066 <rest of line>

        ColumnizedLogLine clogLine = new()
        {
            LogLine = logLine
        };

        var columns = Column.CreateColumns(3, clogLine);

        var temp = logLine.FullLine;

        var formatInfo = _timeFormatDeterminer.DetermineDateTimeFormatInfo(temp.Span);
        if (formatInfo == null)
        {
            columns[2].FullValue = temp;
            clogLine.ColumnValues = [.. columns.Select(a => a as IColumnMemory)];
            return clogLine;
        }

        var endPos = formatInfo.DateTimeFormat.Length;
        var timeLen = formatInfo.TimeFormat.Length;
        var dateLen = formatInfo.DateFormat.Length;
        try
        {
            if (_timeOffset != 0)
            {
                if (formatInfo.IgnoreFirstChar)
                {
                    // Format: [DD.MM.YYYY HH:mm:ss.fff] rest
                    // Skip opening bracket [, then parse datetime, then skip closing bracket ] and space
                    var dateTime = DateTime.ParseExact(temp[1..endPos].Span, formatInfo.DateTimeFormat, formatInfo.CultureInfo);
                    dateTime = dateTime.Add(new TimeSpan(0, 0, 0, 0, _timeOffset));
                    var newDate = dateTime.ToString(formatInfo.DateTimeFormat, formatInfo.CultureInfo).AsMemory();
                    columns[0].FullValue = newDate[..dateLen]; // date
                    columns[1].FullValue = newDate.Slice(dateLen + 1, timeLen); // time
                    columns[2].FullValue = temp[(endPos + 2)..]; // Skip format length + ] + space, rest of line
                }
                else
                {
                    var dateTime = DateTime.ParseExact(temp[..endPos].Span, formatInfo.DateTimeFormat, formatInfo.CultureInfo);
                    dateTime = dateTime.Add(new TimeSpan(0, 0, 0, 0, _timeOffset));
                    var newDate = dateTime.ToString(formatInfo.DateTimeFormat, formatInfo.CultureInfo).AsMemory();
                    columns[0].FullValue = newDate[..dateLen]; // date
                    columns[1].FullValue = newDate.Slice(dateLen + 1, timeLen); // time
                    columns[2].FullValue = temp[endPos..]; // rest of line
                }
            }
            else
            {
                if (formatInfo.IgnoreFirstChar)
                {
                    // First character is a bracket and should be ignored
                    columns[0].FullValue = temp.Slice(1, dateLen); // date
                    columns[1].FullValue = temp.Slice(dateLen + 2, timeLen); // time
                    columns[2].FullValue = temp[(endPos + 2)..]; // rest of line
                }
                else
                {
                    columns[0].FullValue = temp[..dateLen]; // date
                    columns[1].FullValue = temp.Slice(dateLen + 1, timeLen); // time
                    columns[2].FullValue = temp[endPos..]; // rest of line
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException or
                                         FormatException or
                                         ArgumentOutOfRangeException)
        {
            columns[0].FullValue = "n/a".AsMemory();
            columns[1].FullValue = "n/a".AsMemory();
            columns[2].FullValue = temp;
        }

        clogLine.ColumnValues = [.. columns.Select(a => a as IColumnMemory)];
        return clogLine;
    }

    /// <summary>
    /// Extracts and parses the timestamp from the specified log line using the provided callback.
    /// </summary>
    /// <remarks>
    /// If the log line does not contain a valid timestamp in the expected columns or if parsing fails, the method
    /// returns DateTime.MinValue. The timestamp is expected to be composed from the first two columns of the log line.
    /// </remarks>
    /// <param name="callback">The callback used to access column information for the log line.</param>
    /// <param name="logLine">The log line from which to extract the timestamp.</param>
    /// <returns>
    /// A DateTime value representing the parsed timestamp if extraction and parsing succeed; otherwise,
    /// DateTime.MinValue.
    /// </returns>
    public DateTime GetTimestamp (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        var cols = SplitLine(callback, logLine);

        if (cols == null || cols.ColumnValues == null || cols.ColumnValues.Length < 2)
        {
            return DateTime.MinValue;
        }

        if (cols.ColumnValues[0].FullValue.Length == 0 || cols.ColumnValues[1].FullValue.Length == 0)
        {
            return DateTime.MinValue;
        }

        var formatInfo = _timeFormatDeterminer.DetermineDateTimeFormatInfo(logLine.FullLine.Span);
        if (formatInfo == null)
        {
            return DateTime.MinValue;
        }

        try
        {
            var column0 = cols.ColumnValues[0].FullValue.Span;
            var column1 = cols.ColumnValues[1].FullValue.Span;

            Span<char> dateTimeBuffer = stackalloc char[column0.Length + 1 + column1.Length];
            column0.CopyTo(dateTimeBuffer);
            dateTimeBuffer[column0.Length] = ' ';
            column1.CopyTo(dateTimeBuffer[(column0.Length + 1)..]);

            return DateTime.ParseExact(dateTimeBuffer, formatInfo.DateTimeFormat, formatInfo.CultureInfo);
        }
        catch (Exception ex) when (ex is ArgumentException or
                                         FormatException or
                                         ArgumentOutOfRangeException)
        {
            return DateTime.MinValue;
        }
    }

    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, string oldValue)
    {
        PushValue(callback, column, value, oldValue.AsMemory());
    }

    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, ReadOnlyMemory<char> oldValue)
    {
        if (column == 1)
        {
            try
            {
                var formatInfo = _timeFormatDeterminer.DetermineTimeFormatInfo(oldValue.Span);
                if (formatInfo == null)
                {
                    return;
                }

                var newDateTime = DateTime.ParseExact(value, formatInfo.TimeFormat, formatInfo.CultureInfo);
                var oldDateTime = DateTime.ParseExact(oldValue.Span, formatInfo.TimeFormat, formatInfo.CultureInfo);
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
    /// Determines the priority level for processing a log file based on the presence of recognizable timestamp formats
    /// in the provided log lines.
    /// </summary>
    /// <param name="fileName">The name of the log file to evaluate. Cannot be null.</param>
    /// <param name="samples">A collection of log lines to analyze for timestamp patterns. Cannot be null.</param>
    /// <returns>
    /// A value indicating the priority for processing the specified log file. Returns Priority.WellSupport if the
    /// majority of log lines contain recognizable timestamps; otherwise, returns Priority.NotSupport.
    /// </returns>
    public Priority GetPriority (string fileName, IEnumerable<ILogLineMemory> samples)
    {
        ArgumentNullException.ThrowIfNull(samples, nameof(samples));
        ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));

        var result = Priority.NotSupport;

        var timeStampCount = 0;
        foreach (var line in samples)
        {
            if (line?.FullLine.IsEmpty ?? true)
            {
                continue;
            }

            if (_timeFormatDeterminer.DetermineDateTimeFormatInfo(line.FullLine.Span) != null)
            {
                timeStampCount++;
            }
            else
            {
                timeStampCount--;
            }
        }

        if (timeStampCount > 0)
        {
            result = Priority.WellSupport;
        }

        return result;
    }

    #endregion
}