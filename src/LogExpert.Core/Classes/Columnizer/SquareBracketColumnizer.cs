using ColumnizerLib;

namespace LogExpert.Core.Classes.Columnizer;

/// <summary>
/// Provides functionality to split log lines into columns based on square bracket delimiters, typically extracting
/// date, time, and message fields for log analysis.
/// </summary>
/// <remarks>
/// This columnizer is designed for log formats where fields are enclosed in square brackets or separated by whitespace,
/// with optional date and time columns at the beginning of each line. It supports dynamic detection of column structure
/// based on sample log lines and can apply a time offset to parsed timestamps. The class implements interfaces for
/// memory-efficient log line processing and columnizer prioritization, making it suitable for integration with log
/// viewers or analysis tools that require flexible column extraction.
/// </remarks>
public class SquareBracketColumnizer : ILogLineMemoryColumnizer, IColumnizerPriorityMemory, ICloneable
{
    #region ILogLineMemoryColumnizer implementation

    private int _timeOffset;
    private readonly TimeFormatDeterminer _timeFormatDeterminer = new();

    // TODO: need preparing this columnizer with sample log lines before use it.
    private int _columnCount = 5;
    private bool _isTimeExists;

    public SquareBracketColumnizer ()
    {
    }

    public SquareBracketColumnizer (int columnCount, bool isTimeExists) : this()
    {
        // Add message column
        _columnCount = columnCount + 1;
        _isTimeExists = isTimeExists;

        if (_isTimeExists)
        {
            // Time and date
            _columnCount += 2;
        }
    }

    /// <summary>
    /// Creates an independent copy of the detected column layout and time offset.
    /// </summary>
    public object Clone ()
    {
        return new SquareBracketColumnizer
        {
            _columnCount = _columnCount,
            _isTimeExists = _isTimeExists,
            _timeOffset = _timeOffset
        };
    }

    /// <summary>
    /// Determines whether timeshift functionality is implemented.
    /// </summary>
    /// <returns><see langword="true"/> if timeshift is implemented; otherwise, <see langword="false"/>.</returns>
    public bool IsTimeshiftImplemented ()
    {
        return true;
    }

    /// <summary>
    /// Sets the time offset, in milliseconds, to be applied to time calculations.
    /// </summary>
    /// <param name="msecOffset">
    /// The time offset, in milliseconds, to apply. Positive values advance the time; negative values delay it.
    /// </param>
    public void SetTimeOffset (int msecOffset)
    {
        _timeOffset = msecOffset;
    }

    /// <summary>
    /// Gets the current time offset, in seconds, applied to time calculations.
    /// </summary>
    /// <returns>
    /// The time offset, in seconds. A positive value indicates a forward offset; a negative value indicates a backward
    /// offset.
    /// </returns>
    public int GetTimeOffset ()
    {
        return _timeOffset;
    }

    /// <summary>
    /// Extracts and parses the timestamp from the specified log line.
    /// </summary>
    /// <remarks>
    /// If the log line does not contain a valid timestamp or if parsing fails, the method returns DateTime.MinValue.
    /// The expected timestamp is typically composed of the first two columns in the log line.
    /// </remarks>
    /// <param name="callback">A callback interface used to assist with columnizing the log line.</param>
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
            var dateTime = DateTime.ParseExact(cols.ColumnValues[0].FullValue + " " + cols.ColumnValues[1].FullValue, formatInfo.DateTimeFormat, formatInfo.CultureInfo);
            return dateTime;
        }
        catch (Exception ex) when (ex is ArgumentException or
                                         FormatException or
                                         ArgumentOutOfRangeException)
        {
            return DateTime.MinValue;
        }
    }

    public string GetName ()
    {
        return "Square Bracket Columnizer";
    }

    public string GetCustomName () => GetName();

    /// <summary>
    /// Gets a description of the log line splitting format, including the expected fields.
    /// </summary>
    /// <returns>
    /// A string describing how each log line is split into fields: Date, Time, and the remainder of the log message.
    /// </returns>
    public string GetDescription ()
    {
        return "Splits every line into n fields: Date, Time and the rest of the log message";
    }

    /// <summary>
    /// Gets the number of columns in the current data structure.
    /// </summary>
    /// <returns>The total number of columns. Returns 0 if no columns are present.</returns>
    public int GetColumnCount ()
    {
        return _columnCount;
    }

    /// <summary>
    /// Returns an array of column names based on the current log format configuration.
    /// </summary>
    /// <remarks>
    /// The set and order of column names depend on the log format and configuration. If time information is present,
    /// the array includes "Date" and "Time" columns. Additional columns such as "Level" and "Source" are included if
    /// the log contains more than three or four columns, respectively. Any extra columns are named sequentially as
    /// "Source1", "Source2", etc., before the final "Message" column.
    /// </remarks>
    /// <returns>
    /// An array of strings containing the names of all columns in the log. The array includes standard columns such as
    /// "Date", "Time", "Level", "Source", and "Message", as well as additional source columns if present. The array
    /// will contain one element for each column in the log, in the order they appear.
    /// </returns>
    public string[] GetColumnNames ()
    {
        var columnNames = new List<string>(GetColumnCount());
        if (_isTimeExists)
        {
            columnNames.Add("Date");
            columnNames.Add("Time");
        }

        // TODO: Make this configurable.
        if (GetColumnCount() > 3)
        {
            columnNames.Add("Level");
        }

        if (GetColumnCount() > 4)
        {
            columnNames.Add("Source");
        }

        // Last column is the message
        columnNames.Add("Message");
        var i = 1;
        while (columnNames.Count < GetColumnCount())
        {
            columnNames.Insert(columnNames.Count - 1, $"Source{i++}");
        }

        return [.. columnNames];
    }

    /// <summary>
    /// Splits the specified log line into its constituent columns based on detected date and time formats.
    /// </summary>
    /// <remarks>
    /// If the log line does not match a recognized date and time format, the entire line is treated as a single column.
    /// If the log line is too short to contain date or time information, it is returned as a single column as well.
    /// </remarks>
    /// <param name="callback">
    /// A callback interface that can be used during the columnization process. This parameter may be used to provide
    /// additional context or services required for columnization.
    /// </param>
    /// <param name="logLine">The log line to be split into columns. Cannot be null.</param>
    /// <returns>
    /// An object representing the columnized version of the input log line. The returned object contains the extracted
    /// columns, which may include date, time, and the remainder of the line, depending on the detected format.
    /// </returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Intentionally passed")]
    public IColumnizedLogLineMemory SplitLine (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        ArgumentNullException.ThrowIfNull(logLine, nameof(logLine));

        // 0         1         2         3         4         5         6         7         8         9         10        11        12        13        14        15        16
        // 012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
        // 03.01.2008 14:48:00.066 <rest of line>

        ColumnizedLogLine clogLine = new()
        {
            LogLine = logLine
        };

        var columns = new Column[]
        {
            new() {FullValue = ReadOnlyMemory<char>.Empty, Parent = clogLine},
            new() {FullValue = ReadOnlyMemory<char>.Empty, Parent = clogLine},
            new() {FullValue = ReadOnlyMemory<char>.Empty, Parent = clogLine},
        };

        var temp = logLine.FullLine;

        if (temp.Length < 3)
        {
            columns[2].FullValue = temp;
            return clogLine;
        }

        var formatInfo = _timeFormatDeterminer.DetermineDateTimeFormatInfo(logLine.FullLine.Span);
        if (formatInfo == null)
        {
            columns = SquareSplit(temp, 0, 0, 0, clogLine);
        }
        else
        {
            var endPos = formatInfo.DateTimeFormat.Length;
            var timeLen = formatInfo.TimeFormat.Length;
            var dateLen = formatInfo.DateFormat.Length;
            try
            {
                if (_timeOffset != 0)
                {
                    var dateTime = DateTime.ParseExact(temp[..endPos].ToString(), formatInfo.DateTimeFormat, formatInfo.CultureInfo);
                    dateTime = dateTime.Add(new TimeSpan(0, 0, 0, 0, _timeOffset));
                    var newDate = dateTime.ToString(formatInfo.DateTimeFormat, formatInfo.CultureInfo);

                    columns = SquareSplit(newDate.AsMemory(), dateLen, timeLen, endPos, clogLine);
                }
                else
                {
                    columns = SquareSplit(temp, dateLen, timeLen, endPos, clogLine);
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
        }

        clogLine.ColumnValues = [.. columns.Select(a => a as IColumnMemory)];

        return clogLine;
    }

    /// <summary>
    /// Splits a log line into an array of columns based on date, time, and bracketed field positions.
    /// </summary>
    /// <remarks>
    /// If the input line does not contain enough fields to match the expected column count, empty columns are inserted
    /// to ensure the returned array has the correct length. The method associates each column with the provided parent
    /// log line object.
    /// </remarks>
    /// <param name="line">The log line to split, provided as a read-only memory buffer of characters.</param>
    /// <param name="dateLen">The length, in characters, of the date field at the start of the line.</param>
    /// <param name="timeLen">The length, in characters, of the time field following the date field.</param>
    /// <param name="dateTimeEndPos">
    /// The zero-based position in the line immediately after the date and time fields.
    /// </param>
    /// <param name="clogLine">The parent log line object to associate with each resulting column.</param>
    /// <returns>
    /// An array of columns parsed from the input line. The array contains one element for each expected column, with
    /// empty columns inserted if the input does not provide enough fields.
    /// </returns>
    private Column[] SquareSplit (ReadOnlyMemory<char> line, int dateLen, int timeLen, int dateTimeEndPos, ColumnizedLogLine clogLine)
    {
        List<Column> columnList = [];
        var restColumn = _columnCount;

        if (_isTimeExists)
        {
            columnList.Add(new Column { FullValue = line[..dateLen], Parent = clogLine });
            columnList.Add(new Column { FullValue = line.Slice(dateLen + 1, timeLen), Parent = clogLine });
            restColumn -= 2;
        }

        var nextPos = dateTimeEndPos;

        var rest = line;

        for (var i = 0; i < restColumn; i++)
        {
            rest = rest[nextPos..];
            //var fullValue = rest.Substring(0, rest.IndexOf(']')).TrimStart(new char[] {' '}).TrimEnd(new char[] { ' ' });
            var trimmed = rest.TrimStart([' ']);
            var span = trimmed.Span;
            var trimStart = 0;
            while (trimStart < span.Length && span[trimStart] == ' ')
            {
                trimStart++;
            }

            if (trimStart > 0)
            {
                trimmed = rest[trimStart..];
            }

            if (trimmed.Length == 0 || trimmed.Span[0] != '[' || i == restColumn - 1)
            {
                columnList.Add(new Column { FullValue = rest, Parent = clogLine });
                break;
            }

            // Find the ']' that matches the opening '[', accounting for nested brackets
            // (e.g. "[ProgramName/MethodName[41]]" is a single field).
            var closeSpan = trimmed.Span;
            var depth = 0;
            var closingBracketIndex = -1;
            for (var j = 0; j < closeSpan.Length; j++)
            {
                if (closeSpan[j] == '[')
                {
                    depth++;
                }
                else if (closeSpan[j] == ']' && --depth == 0)
                {
                    closingBracketIndex = j;
                    break;
                }
            }

            if (closingBracketIndex < 0)
            {
                columnList.Add(new Column { FullValue = rest, Parent = clogLine });
                break;
            }

            nextPos = closingBracketIndex + 1;
            var fullValue = rest[..nextPos];
            columnList.Add(new Column { FullValue = fullValue, Parent = clogLine });
        }

        while (columnList.Count < _columnCount)
        {
            columnList.Insert(columnList.Count - 1, new Column { FullValue = ReadOnlyMemory<char>.Empty, Parent = clogLine });
        }

        return [.. columnList];
    }

    /// <summary>
    /// Determines the priority level for parsing log lines based on the specified file name and a collection of log
    /// line samples.
    /// </summary>
    /// <remarks>
    /// The returned priority reflects how well the log format is supported based on the structure and content of the
    /// provided samples. This method does not modify the input collection.
    /// </remarks>
    /// <param name="fileName">The name of the log file to analyze. Cannot be null.</param>
    /// <param name="samples">A collection of log line samples to evaluate for format support. Cannot be null.</param>
    /// <returns>
    /// A value indicating the priority level for parsing the provided log lines. Returns a higher priority if the
    /// format is well supported or perfectly supported; otherwise, returns a lower priority.
    /// </returns>
    public Priority GetPriority (string fileName, IEnumerable<ILogLineMemory> samples)
    {
        ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
        ArgumentNullException.ThrowIfNull(samples, nameof(samples));

        var result = Priority.NotSupport;
        TimeFormatDeterminer timeDeterminer = new();
        var timeStampExistsCount = 0;
        var bracketsExistsCount = 0;
        var maxBracketNumbers = 1;

        foreach (var logline in samples)
        {
            var line = logline?.FullLine;
            if (!line.HasValue || line.Value.Length == 0)
            {
                continue;
            }

            var consecutiveBracketPairs = 0;
            var lastCharWasCloseBracket = false;
            var bracketNumbers = 1;

            if (timeDeterminer.DetermineDateTimeFormatInfo(line.Value.Span) != null)
            {
                timeStampExistsCount++;
            }
            else
            {
                timeStampExistsCount--;
            }

            var span = line.Value.Span;

            // Check if line has brackets in correct order
            if (!span.Contains('[') || !span.Contains(']') || span.IndexOf('[') >= span.IndexOf(']'))
            {
                bracketsExistsCount--;
                continue;
            }

            // Count "][" patterns ignoring spaces
            for (var i = 0; i < span.Length; i++)
            {
                if (span[i] == ' ')
                {
                    continue;
                }

                if (span[i] == ']')
                {
                    lastCharWasCloseBracket = true;
                }
                else if (span[i] == '[' && lastCharWasCloseBracket)
                {
                    consecutiveBracketPairs++;
                    lastCharWasCloseBracket = false;
                }
                else
                {
                    lastCharWasCloseBracket = false;
                }
            }

            bracketNumbers += consecutiveBracketPairs;
            bracketsExistsCount++;

            maxBracketNumbers = Math.Max(bracketNumbers, maxBracketNumbers);
        }

        // Add message
        _columnCount = maxBracketNumbers + 1;
        _isTimeExists = timeStampExistsCount > 0;
        if (_isTimeExists)
        {
            _columnCount += 2;
        }

        if (maxBracketNumbers > 1)
        {
            result = Priority.WellSupport;
            if (bracketsExistsCount > 0)
            {
                result = Priority.PerfectlySupport;
            }
        }

        return result;
    }

    /// <summary>
    /// Determines the priority for processing the specified log file based on the provided log line samples.
    /// </summary>
    /// <param name="fileName">
    /// The name of the log file for which to determine the processing priority. Cannot be null or empty.
    /// </param>
    /// <param name="samples">
    /// A collection of log line samples used to assess the file's priority. Cannot be null.
    /// </param>
    /// <returns>A value indicating the determined priority for the specified log file.</returns>
    public Priority GetPriority (string fileName, IEnumerable<ILogLine> samples)
    {
        return GetPriority(fileName, samples.Cast<ILogLineMemory>());
    }

    /// <summary>
    /// Processes a value change for a specified column and updates the time offset if the column represents a
    /// timestamp.
    /// </summary>
    /// <remarks>
    /// This method only updates the time offset when the specified column index is 1 and both the new and old values
    /// can be parsed as valid timestamps according to the determined time format. No action is taken for other columns
    /// or if the values cannot be parsed as dates.
    /// </remarks>
    /// <param name="callback">
    /// The callback interface used to interact with the columnizer during value processing.
    /// </param>
    /// <param name="column">
    /// The zero-based index of the column for which the value is being processed. If the value is 1, the method
    /// attempts to update the time offset.
    /// </param>
    /// <param name="value">The new value to be processed for the specified column.</param>
    /// <param name="oldValue">The previous value of the specified column before the change.</param>
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

    #endregion
}