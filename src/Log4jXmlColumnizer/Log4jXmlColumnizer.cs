using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

using ColumnizerLib;

using LogExpert;

using Newtonsoft.Json;

[assembly: SupportedOSPlatform("windows")]
namespace Log4jXmlColumnizer;

public class Log4jXmlColumnizer : ILogLineMemoryXmlColumnizer, IColumnizerConfiguratorMemory, IColumnizerPriorityMemory, ICloneable
{
    #region Fields

    public const int COLUMN_COUNT = 9;
    protected const string DATETIME_FORMAT = "dd.MM.yyyy HH:mm:ss.fff";

    private static readonly XmlConfig _xmlConfig = new();
    private const char SEPARATOR_CHAR = '\xFFFD';
    private readonly char[] trimChars = ['\xFFFD'];
    private Log4jXmlColumnizerConfig _config;
    private readonly CultureInfo _cultureInfo = new("de-DE");
    private int _timeOffset;

    #endregion

    #region cTor

    public Log4jXmlColumnizer ()
    {
        _config = new Log4jXmlColumnizerConfig(GetAllColumnNames());
    }

    #endregion

    #region Public methods

    public IXmlLogConfiguration GetXmlLogConfiguration ()
    {
        return _xmlConfig;
    }

    public ILogLineMemory GetLineTextForClipboard (ILogLineMemory logLine, ILogLineMemoryColumnizerCallback callback)
    {
        ArgumentNullException.ThrowIfNull(logLine);
        ArgumentNullException.ThrowIfNull(callback);

        return new Log4JLogLine(ReplaceInMemory(logLine.FullLine, SEPARATOR_CHAR, '|'), logLine.Text, logLine.LineNumber);
    }

    public string GetName ()
    {
        return "Log4j XML";
    }

    public string GetCustomName () => GetName();

    public string GetDescription ()
    {
        return "Reads and formats XML log files written with log4j.";
    }

    public int GetColumnCount ()
    {
        return _config.ActiveColumnCount;
    }

    public string[] GetColumnNames ()
    {
        return _config.ActiveColumnNames;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Intentionally passed")]
    public IColumnizedLogLineMemory SplitLine (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        ArgumentNullException.ThrowIfNull(logLine);
        ArgumentNullException.ThrowIfNull(callback);

        ColumnizedLogLine clogLine = new()
        {
            LogLine = logLine
        };

        var columns = Column.CreateColumns(COLUMN_COUNT, clogLine);

        // If the line is too short (i.e. does not follow the format for this columnizer) return the whole line content
        // in colum 8 (the log message column). Date and time column will be left blank.
        if (logLine.FullLine.Length < 15)
        {
            columns[8].FullValue = logLine.FullLine;
        }
        else
        {
            try
            {
                var dateTime = GetTimestamp(callback, logLine);

                if (dateTime == DateTime.MinValue)
                {
                    columns[8].FullValue = logLine.FullLine;
                }

                var newDate = dateTime.ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture);
                columns[0].FullValue = newDate.AsMemory();
            }
            catch (Exception ex) when (ex is ArgumentException or
                                             FormatException or
                                             ArgumentOutOfRangeException)
            {
                columns[0].FullValue = "n/a".AsMemory();
            }

            var timestmp = columns[0];

            ReadOnlyMemory<char>[] cols;
            cols = SplitMemory(logLine.FullLine, trimChars[0], COLUMN_COUNT);

            if (cols.Length != COLUMN_COUNT)
            {
                columns[0].FullValue = ReadOnlyMemory<char>.Empty;
                columns[1].FullValue = ReadOnlyMemory<char>.Empty;
                columns[2].FullValue = ReadOnlyMemory<char>.Empty;
                columns[3].FullValue = ReadOnlyMemory<char>.Empty;
                columns[4].FullValue = ReadOnlyMemory<char>.Empty;
                columns[5].FullValue = ReadOnlyMemory<char>.Empty;
                columns[6].FullValue = ReadOnlyMemory<char>.Empty;
                columns[7].FullValue = ReadOnlyMemory<char>.Empty;
                columns[8].FullValue = logLine.FullLine;
            }
            else
            {
                columns[0] = timestmp;

                for (var i = 1; i < cols.Length; i++)
                {
                    columns[i].FullValue = cols[i];
                }
            }
        }

        var filteredColumns = MapColumns(columns);

        clogLine.ColumnValues = [.. filteredColumns.Select(a => a as IColumnMemory)];

        return clogLine;
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

    public DateTime GetTimestamp (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        ArgumentNullException.ThrowIfNull(logLine);
        ArgumentNullException.ThrowIfNull(callback);

        if (logLine.FullLine.Length < 15)
        {
            return DateTime.MinValue;
        }

        var span = logLine.FullLine.Span;

        var endIndex = span.IndexOf(SEPARATOR_CHAR);

        if (endIndex is > 20 or < 0)
        {
            return DateTime.MinValue;
        }

        var value = logLine.FullLine[..endIndex];

        try
        {
            // convert log4j timestamp into a readable format:
            if (long.TryParse(value.ToString(), out var timestamp))
            {
                // Add the time offset before returning
                DateTime dateTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                dateTime = dateTime.AddMilliseconds(timestamp);

                if (_config.LocalTimestamps)
                {
                    dateTime = dateTime.ToLocalTime();
                }

                return dateTime.AddMilliseconds(_timeOffset);
            }
            else
            {
                return DateTime.MinValue;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or
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
        if (column == 0)
        {
            try
            {
                var newDateTime = DateTime.ParseExact(value, DATETIME_FORMAT, _cultureInfo);
                var oldDateTime = DateTime.ParseExact(oldValue.ToString(), DATETIME_FORMAT, _cultureInfo);
                var mSecsOld = oldDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                var mSecsNew = newDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                _timeOffset = (int)(mSecsNew - mSecsOld);
            }
            catch (FormatException)
            {
            }
        }
    }

    public void Configure (ILogLineMemoryColumnizerCallback callback, string configDir)
    {
        FileInfo fileInfo = new(configDir + Path.DirectorySeparatorChar + "log4jxmlcolumnizer.json");

        Log4jXmlColumnizerConfigDlg dlg = new(_config);

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            using StreamWriter sw = new(fileInfo.Create());
            JsonSerializer serializer = new();
            serializer.Serialize(sw, _config);
        }
    }

    public void LoadConfig (string configDir)
    {
        var configPath = Path.Join(configDir, "log4jxmlcolumnizer.json");

        FileInfo fileInfo = new(configPath);

        if (!File.Exists(configPath))
        {
            _config = new Log4jXmlColumnizerConfig(GetAllColumnNames());
        }
        else
        {
            try
            {
                _config = JsonConvert.DeserializeObject<Log4jXmlColumnizerConfig>(File.ReadAllText(fileInfo.FullName));

                if (_config.ColumnList.Count < COLUMN_COUNT)
                {
                    _config = new Log4jXmlColumnizerConfig(GetAllColumnNames());
                }
            }
            catch (SerializationException e)
            {
                _ = MessageBox.Show(e.Message, Resources.Log4jXmlColumnizer_UI_Title_Deserialize);
                _config = new Log4jXmlColumnizerConfig(GetAllColumnNames());
            }
        }
    }

    public object Clone ()
    {
        Log4jXmlColumnizer clone = new()
        {
            _config = new Log4jXmlColumnizerConfig(GetAllColumnNames())
            {
                LocalTimestamps = _config.LocalTimestamps,
                ColumnList = [.. _config.ColumnList.Select(entry => new Log4jColumnEntry(entry.ColumnName, entry.ColumnIndex, entry.MaxLen)
                {
                    Visible = entry.Visible
                })]
            },
            _timeOffset = _timeOffset
        };

        return clone;
    }

    public Priority GetPriority (string fileName, IEnumerable<ILogLineMemory> samples)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(samples);

        var result = Priority.NotSupport;
        if (fileName.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
        {
            result = Priority.CanSupport;
        }

        return result;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Splits ReadOnlyMemory by separator character with max count limit
    /// </summary>
    /// <param name="input">The memory to split</param>
    /// <param name="separator">The separator character (SEPARATOR_CHAR = '\xFFFD')</param>
    /// <param name="maxCount">Maximum number of parts to return (9 in this case)</param>
    /// <returns>Array of ReadOnlyMemory segments</returns>
    private static ReadOnlyMemory<char>[] SplitMemory (ReadOnlyMemory<char> input, char separator, int maxCount)
    {
        var span = input.Span;
        var result = new List<ReadOnlyMemory<char>>(maxCount);
        var start = 0;

        // Split until we have maxCount - 1 segments
        // (last segment gets all remaining content)
        for (var i = 0; i < span.Length && result.Count < maxCount - 1; i++)
        {
            if (span[i] == separator)
            {
                // Found separator - add segment before it
                result.Add(input[start..i]);
                start = i + 1;  // Skip the separator
            }
        }

        // Add remaining content as last segment
        // (or entire string if no separators found)
        if (start <= input.Length)
        {
            result.Add(input[start..]);
        }

        return [.. result];
    }

    private static string[] GetAllColumnNames () => ["Timestamp", "Level", "Logger", "Thread", "Class", "Method", "File", "Line", "Message"];

    /// <summary>
    /// Returns only the columns which are "active". The order of the columns depends on the column order in the config
    /// </summary>
    /// <param name="cols"></param>
    /// <returns></returns>
    private Column[] MapColumns (Column[] cols)
    {
        List<Column> output = [];
        var index = 0;
        foreach (var entry in _config.ColumnList)
        {
            if (entry.Visible)
            {
                var column = cols[index];
                output.Add(column);

                if (entry.MaxLen > 0 && column.FullValue.Length > entry.MaxLen)
                {
                    column.FullValue = column.FullValue[^entry.MaxLen..];
                }
            }

            index++;
        }

        return [.. output];
    }

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