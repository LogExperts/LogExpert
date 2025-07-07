using System.Globalization;
using System.Text.RegularExpressions;

using static LogExpert.Core.Classes.Columnizer.TimeFormatDeterminer;

namespace LogExpert.Core.Classes.Columnizer;

public class SquareBracketColumnizer : ILogLineColumnizer, IColumnizerPriority
{
    #region ILogLineColumnizer implementation

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

    public DateTime GetTimestamp (ILogLineColumnizerCallback callback, ILogLine line)
    {
        IColumnizedLogLine cols = SplitLine(callback, line);
        if (cols == null || cols.ColumnValues == null || cols.ColumnValues.Length < 2)
        {
            return DateTime.MinValue;
        }

        if (cols.ColumnValues[0].FullValue.Length == 0 || cols.ColumnValues[1].FullValue.Length == 0)
        {
            return DateTime.MinValue;
        }

        FormatInfo formatInfo = _timeFormatDeterminer.DetermineDateTimeFormatInfo(line.FullLine);
        if (formatInfo == null)
        {
            return DateTime.MinValue;
        }

        try
        {
            var dateTime = DateTime.ParseExact(
                cols.ColumnValues[0].FullValue + " " + cols.ColumnValues[1].FullValue, formatInfo.DateTimeFormat,
                formatInfo.CultureInfo);
            return dateTime;
        }
        catch (Exception)
        {
            return DateTime.MinValue;
        }
    }

    public void PushValue (ILogLineColumnizerCallback callback, int column, string value, string oldValue)
    {
        if (column == 1)
        {
            try
            {
                FormatInfo formatInfo = _timeFormatDeterminer.DetermineTimeFormatInfo(oldValue);
                if (formatInfo == null)
                {
                    return;
                }

                var newDateTime = DateTime.ParseExact(value, formatInfo.TimeFormat, formatInfo.CultureInfo);
                var oldDateTime = DateTime.ParseExact(oldValue, formatInfo.TimeFormat, formatInfo.CultureInfo);
                var mSecsOld = oldDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                var mSecsNew = newDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                _timeOffset = (int)(mSecsNew - mSecsOld);
            }
            catch (FormatException)
            {
            }
        }
    }

    public string GetName ()
    {
        return "Square Bracket Columnizer";
    }

    public string GetDescription ()
    {
        return "Splits every line into n fields: Date, Time and the rest of the log message";
    }

    public int GetColumnCount ()
    {
        return _columnCount;
    }

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

        return columnNames.ToArray();
    }

    public IColumnizedLogLine SplitLine (ILogLineColumnizerCallback callback, ILogLine line)
    {
        // 0         1         2         3         4         5         6         7         8         9         10        11        12        13        14        15        16
        // 012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
        // 03.01.2008 14:48:00.066 <rest of line>

        ColumnizedLogLine clogLine = new()
        {
            LogLine = line
        };

        var columns = new Column[]
        {
            new() {FullValue = "", Parent = clogLine},
            new() {FullValue = "", Parent = clogLine},
            new() {FullValue = "", Parent = clogLine},
        };

        var temp = line.FullLine;

        if (temp.Length < 3)
        {
            columns[2].FullValue = temp;
            return clogLine;
        }

        FormatInfo formatInfo = _timeFormatDeterminer.DetermineDateTimeFormatInfo(line.FullLine);
        if (formatInfo == null)
        {
            columns[2].FullValue = temp;
            SquareSplit(ref columns, temp, 0, 0, 0, clogLine);
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
                    var dateTime = DateTime.ParseExact(temp[..endPos], formatInfo.DateTimeFormat,
                        formatInfo.CultureInfo);
                    dateTime = dateTime.Add(new TimeSpan(0, 0, 0, 0, _timeOffset));
                    var newDate = dateTime.ToString(formatInfo.DateTimeFormat, formatInfo.CultureInfo);

                    SquareSplit(ref columns, newDate, dateLen, timeLen, endPos, clogLine);
                }
                else
                {
                    SquareSplit(ref columns, temp, dateLen, timeLen, endPos, clogLine);
                }
            }
            catch (Exception)
            {
                columns[0].FullValue = "n/a";
                columns[1].FullValue = "n/a";
                columns[2].FullValue = temp;
            }
        }

        clogLine.ColumnValues = columns.Select(a => a as IColumn).ToArray();

        return clogLine;
    }

    void SquareSplit (ref Column[] columns, string line, int dateLen, int timeLen, int dateTimeEndPos, ColumnizedLogLine clogLine)
    {
        List<Column> columnList = [];
        var restColumn = _columnCount;
        if (_isTimeExists)
        {
            columnList.Add(new Column { FullValue = line[..dateLen], Parent = clogLine });
            columnList.Add(new Column { FullValue = line.Substring(dateLen + 1, timeLen), Parent = clogLine });
            restColumn -= 2;
        }

        var nextPos = dateTimeEndPos;

        var rest = line;

        for (var i = 0; i < restColumn; i++)
        {
            rest = rest[nextPos..];
            //var fullValue = rest.Substring(0, rest.IndexOf(']')).TrimStart(new char[] {' '}).TrimEnd(new char[] { ' ' });
            var trimmed = rest.TrimStart([' ']);
            if (string.IsNullOrEmpty(trimmed) || trimmed[0] != '[' || rest.IndexOf(']', StringComparison.Ordinal) < 0 || i == restColumn - 1)
            {
                columnList.Add(new Column { FullValue = rest, Parent = clogLine });
                break;
            }

            nextPos = rest.IndexOf(']', StringComparison.Ordinal) + 1;
            var fullValue = rest[..nextPos];
            columnList.Add(new Column { FullValue = fullValue, Parent = clogLine });
        }

        while (columnList.Count < _columnCount)
        {
            columnList.Insert(columnList.Count - 1, new Column { FullValue = "", Parent = clogLine });
        }

        columns = columnList.ToArray();
    }

    public Priority GetPriority (string fileName, IEnumerable<ILogLine> samples)
    {
        Priority result = Priority.NotSupport;
        TimeFormatDeterminer timeDeterminer = new();
        var timeStampExistsCount = 0;
        var bracketsExistsCount = 0;
        var maxBracketNumbers = 1;

        foreach (ILogLine logline in samples)
        {
            var line = logline?.FullLine;
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var bracketNumbers = 1;
            if (null != timeDeterminer.DetermineDateTimeFormatInfo(line))
            {
                timeStampExistsCount++;
            }
            else
            {
                timeStampExistsCount--;
            }

            var noSpaceLine = line.Replace(" ", string.Empty, StringComparison.Ordinal);
            if (noSpaceLine.Contains('[', StringComparison.Ordinal) && noSpaceLine.Contains(']', StringComparison.Ordinal)
                                              && noSpaceLine.IndexOf('[', StringComparison.Ordinal) < noSpaceLine.IndexOf(']', StringComparison.Ordinal))
            {
                bracketNumbers += Regex.Matches(noSpaceLine, @"\]\[").Count;
                bracketsExistsCount++;
            }
            else
            {
                bracketsExistsCount--;
            }

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

    #endregion
}