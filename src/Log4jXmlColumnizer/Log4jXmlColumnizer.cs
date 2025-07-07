using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Windows.Forms;

using LogExpert;

using Newtonsoft.Json;

[assembly: SupportedOSPlatform("windows")]
namespace Log4jXmlColumnizer;

public class Log4jXmlColumnizer : ILogLineXmlColumnizer, IColumnizerConfigurator, IColumnizerPriority
{
    #region Fields

    public const int COLUMN_COUNT = 9;
    protected const string DATETIME_FORMAT = "dd.MM.yyyy HH:mm:ss.fff";

    private static readonly XmlConfig xmlConfig = new();
    private const char separatorChar = '\xFFFD';
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
        return xmlConfig;
    }

    public ILogLine GetLineTextForClipboard (ILogLine logLine, ILogLineColumnizerCallback callback)
    {
        Log4JLogLine line = new()
        {
            FullLine = logLine.FullLine.Replace(separatorChar, '|'),
            LineNumber = logLine.LineNumber
        };

        return line;
    }

    public string GetName ()
    {
        return "Log4j XML";
    }

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

    public IColumnizedLogLine SplitLine (ILogLineColumnizerCallback callback, ILogLine line)
    {
        ColumnizedLogLine clogLine = new();
        clogLine.LogLine = line;

        Column[] columns = Column.CreateColumns(COLUMN_COUNT, clogLine);

        // If the line is too short (i.e. does not follow the format for this columnizer) return the whole line content
        // in colum 8 (the log message column). Date and time column will be left blank.
        if (line.FullLine.Length < 15)
        {
            columns[8].FullValue = line.FullLine;
        }
        else
        {
            try
            {
                DateTime dateTime = GetTimestamp(callback, line);

                if (dateTime == DateTime.MinValue)
                {
                    columns[8].FullValue = line.FullLine;
                }

                var newDate = dateTime.ToString(DATETIME_FORMAT);
                columns[0].FullValue = newDate;
            }
            catch (Exception)
            {
                columns[0].FullValue = "n/a";
            }

            Column timestmp = columns[0];

            string[] cols;
            cols = line.FullLine.Split(trimChars, COLUMN_COUNT, StringSplitOptions.None);

            if (cols.Length != COLUMN_COUNT)
            {
                columns[0].FullValue = "";
                columns[1].FullValue = "";
                columns[2].FullValue = "";
                columns[3].FullValue = "";
                columns[4].FullValue = "";
                columns[5].FullValue = "";
                columns[6].FullValue = "";
                columns[7].FullValue = "";
                columns[8].FullValue = line.FullLine;
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

        Column[] filteredColumns = MapColumns(columns);

        clogLine.ColumnValues = filteredColumns.Select(a => a as IColumn).ToArray();


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

    public DateTime GetTimestamp (ILogLineColumnizerCallback callback, ILogLine line)
    {
        if (line.FullLine.Length < 15)
        {
            return DateTime.MinValue;
        }

        var endIndex = line.FullLine.IndexOf(separatorChar, 1);

        if (endIndex > 20 || endIndex < 0)
        {
            return DateTime.MinValue;
        }
        var value = line.FullLine.Substring(0, endIndex);

        try
        {
            // convert log4j timestamp into a readable format:
            if (long.TryParse(value, out var timestamp))
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
        catch (Exception)
        {
            return DateTime.MinValue;
        }
    }

    public void PushValue (ILogLineColumnizerCallback callback, int column, string value, string oldValue)
    {
        if (column == 0)
        {
            try
            {
                var newDateTime = DateTime.ParseExact(value, DATETIME_FORMAT, _cultureInfo);
                var oldDateTime = DateTime.ParseExact(oldValue, DATETIME_FORMAT, _cultureInfo);
                var mSecsOld = oldDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                var mSecsNew = newDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                _timeOffset = (int)(mSecsNew - mSecsOld);
            }
            catch (FormatException)
            {
            }
        }
    }

    public void Configure (ILogLineColumnizerCallback callback, string configDir)
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
        var configPath = configDir + Path.DirectorySeparatorChar + "log4jxmlcolumnizer.json";

        FileInfo fileInfo = new(configDir + Path.DirectorySeparatorChar + "log4jxmlcolumnizer.json");

        if (!File.Exists(configPath))
        {
            _config = new Log4jXmlColumnizerConfig(GetAllColumnNames());
        }
        else
        {
            try
            {
                _config = JsonConvert.DeserializeObject<Log4jXmlColumnizerConfig>(File.ReadAllText($"{fileInfo.FullName}"));
                if (_config.ColumnList.Count < COLUMN_COUNT)
                {
                    _config = new Log4jXmlColumnizerConfig(GetAllColumnNames());
                }
            }
            catch (SerializationException e)
            {
                MessageBox.Show(e.Message, "Deserialize");
                _config = new Log4jXmlColumnizerConfig(GetAllColumnNames());
            }
        }
    }

    public Priority GetPriority (string fileName, IEnumerable<ILogLine> samples)
    {
        Priority result = Priority.NotSupport;
        if (fileName.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
        {
            result = Priority.CanSupport;
        }
        return result;
    }

    #endregion

    #region Private Methods

    private string[] GetAllColumnNames () => ["Timestamp", "Level", "Logger", "Thread", "Class", "Method", "File", "Line", "Message"];

    /// <summary>
    /// Returns only the columns which are "active". The order of the columns depends on the column order in the config
    /// </summary>
    /// <param name="cols"></param>
    /// <returns></returns>
    private Column[] MapColumns (Column[] cols)
    {
        List<Column> output = [];
        var index = 0;
        foreach (Log4jColumnEntry entry in _config.ColumnList)
        {
            if (entry.Visible)
            {
                Column column = cols[index];
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

    #endregion
}