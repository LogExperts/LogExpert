using static LogExpert.Core.Classes.Columnizer.TimeFormatDeterminer;

namespace LogExpert.Core.Classes.Columnizer;

public class TimestampColumnizer : ILogLineColumnizer, IColumnizerPriority
{
    #region ILogLineColumnizer implementation

    private int timeOffset;
    private readonly TimeFormatDeterminer _timeFormatDeterminer = new();

    public bool IsTimeshiftImplemented ()
    {
        return true;
    }

    public void SetTimeOffset (int msecOffset)
    {
        timeOffset = msecOffset;
    }

    public int GetTimeOffset ()
    {
        return timeOffset;
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
                timeOffset = (int)(mSecsNew - mSecsOld);
            }
            catch (FormatException)
            {
            }
        }
    }

    public string GetName ()
    {
        return "Timestamp Columnizer";
    }

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

    public IColumnizedLogLine SplitLine (ILogLineColumnizerCallback callback, ILogLine line)
    {
        // 0         1         2         3         4         5         6         7         8         9         10        11        12        13        14        15        16
        // 012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
        // 03.01.2008 14:48:00.066 <rest of line>

        ColumnizedLogLine clogLine = new();
        clogLine.LogLine = line;

        var columns = new Column[3]
        {
            new() {FullValue = "", Parent = clogLine},
            new() {FullValue = "", Parent = clogLine},
            new() {FullValue = "", Parent = clogLine},
        };

        clogLine.ColumnValues = columns.Select(a => a as IColumn).ToArray();

        var temp = line.FullLine;

        FormatInfo formatInfo = _timeFormatDeterminer.DetermineDateTimeFormatInfo(temp);
        if (formatInfo == null)
        {
            columns[2].FullValue = temp;
            return clogLine;
        }
        var endPos = formatInfo.DateTimeFormat.Length;
        var timeLen = formatInfo.TimeFormat.Length;
        var dateLen = formatInfo.DateFormat.Length;
        try
        {
            if (timeOffset != 0)
            {
                if (formatInfo.IgnoreFirstChar)
                {
                    // First character is a bracket and should be ignored
                    var dateTime = DateTime.ParseExact(temp.Substring(1, endPos), formatInfo.DateTimeFormat,
                        formatInfo.CultureInfo);
                    dateTime = dateTime.Add(new TimeSpan(0, 0, 0, 0, timeOffset));
                    var newDate = dateTime.ToString(formatInfo.DateTimeFormat, formatInfo.CultureInfo);
                    columns[0].FullValue = newDate.Substring(0, dateLen); // date
                    columns[1].FullValue = newDate.Substring(dateLen + 1, timeLen); // time
                    columns[2].FullValue = temp.Substring(endPos + 2); // rest of line
                }
                else
                {
                    var dateTime = DateTime.ParseExact(temp.Substring(0, endPos), formatInfo.DateTimeFormat,
                        formatInfo.CultureInfo);
                    dateTime = dateTime.Add(new TimeSpan(0, 0, 0, 0, timeOffset));
                    var newDate = dateTime.ToString(formatInfo.DateTimeFormat, formatInfo.CultureInfo);
                    columns[0].FullValue = newDate.Substring(0, dateLen); // date
                    columns[1].FullValue = newDate.Substring(dateLen + 1, timeLen); // time
                    columns[2].FullValue = temp.Substring(endPos); // rest of line
                }
            }
            else
            {
                if (formatInfo.IgnoreFirstChar)
                {
                    // First character is a bracket and should be ignored
                    columns[0].FullValue = temp.Substring(1, dateLen); // date
                    columns[1].FullValue = temp.Substring(dateLen + 2, timeLen); // time
                    columns[2].FullValue = temp.Substring(endPos + 2); // rest of line
                }
                else
                {
                    columns[0].FullValue = temp.Substring(0, dateLen); // date
                    columns[1].FullValue = temp.Substring(dateLen + 1, timeLen); // time
                    columns[2].FullValue = temp.Substring(endPos); // rest of line
                }
            }
        }
        catch (Exception)
        {
            columns[0].FullValue = "n/a";
            columns[1].FullValue = "n/a";
            columns[2].FullValue = temp;
        }
        return clogLine;
    }

    public Priority GetPriority (string fileName, IEnumerable<ILogLine> samples)
    {
        Priority result = Priority.NotSupport;

        var timeStampCount = 0;
        foreach (ILogLine line in samples)
        {
            if (line == null || string.IsNullOrEmpty(line.FullLine))
            {
                continue;
            }
            var timeDeterminer = new TimeFormatDeterminer();
            if (null != timeDeterminer.DetermineDateTimeFormatInfo(line.FullLine))
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