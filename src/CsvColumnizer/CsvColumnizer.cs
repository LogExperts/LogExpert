using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security;

using ColumnizerLib;

using CsvHelper;

using Newtonsoft.Json;

[assembly: SupportedOSPlatform("windows")]
namespace CsvColumnizer;

/// <summary>
/// This Columnizer can parse CSV files. It uses the IInitColumnizer interface for support of dynamic field count.
/// The IPreProcessColumnizer is implemented to read field names from the very first line of the file. Then
/// the line is dropped. So it's not seen by LogExpert. The field names will be used as column names.
/// </summary>
public class CsvColumnizer : ILogLineMemoryColumnizer, IInitColumnizerMemory, IColumnizerConfiguratorMemory, IPreProcessColumnizerMemory, IColumnizerPriorityMemory, IColumnizerSnapshotMemory
{
    #region Fields

    private const string CONFIGFILENAME = "csvcolumnizer.json";

    private readonly IList<CsvColumn> _columnList = [];
    private CsvColumnizerConfig _config = CreateDefaultConfig();

    private ILogLineMemory _firstLine;

    // if CSV is detected to be 'invalid' the columnizer will behave like a default columnizer
    private bool _isValidCsv;

    #endregion

    #region Public methods

    public string PreProcessLine (string logLine, int lineNum, int realLineNum)
    {
        ArgumentNullException.ThrowIfNull(logLine, nameof(logLine));

        return PreProcessLine(logLine.AsMemory(), lineNum, realLineNum).ToString();
    }
    private static CsvColumnizerConfig CreateDefaultConfig ()
    {
        var config = new CsvColumnizerConfig();
        config.InitDefaults();
        return config;
    }

    public ReadOnlyMemory<char> PreProcessLine (ReadOnlyMemory<char> logLine, int lineNum, int realLineNum)
    {
        if (realLineNum == 0)
        {
            // Auto-detect delimiter from the first line
            AutoDetectDelimiter(logLine);

            // store for later field names and field count retrieval
            _firstLine = new CsvLogLine(logLine, 0);

            if (_config != null && _config.MinColumns > 0)
            {
                using CsvReader csv = new(new StringReader(logLine.ToString()), _config.ReaderConfiguration);
                if (csv.Parser.Count < _config.MinColumns)
                {
                    // on invalid CSV don't hide the first line from LogExpert, since the file will be displayed in plain mode
                    _isValidCsv = false;
                    return logLine;
                }
            }

            _isValidCsv = true;
        }

        if (_config.HasFieldNames && realLineNum == 0)
        {
            return null; // hide from LogExpert
        }

        return _config.CommentChar != ' ' &&
               logLine.Span.StartsWith("" + _config.CommentChar, StringComparison.OrdinalIgnoreCase)
                    ? null
                    : logLine;
    }

    public string GetName ()
    {
        return "CSV Columnizer";
    }

    public string GetCustomName ()
    {
        return GetName();
    }

    public string GetDescription ()
    {
        return Resources.CsvColumnizer_Description;
    }

    public int GetColumnCount ()
    {
        return _isValidCsv ? _columnList.Count : 1;
    }

    public string[] GetColumnNames ()
    {
        var names = new string[GetColumnCount()];
        if (_isValidCsv)
        {
            var i = 0;
            foreach (var column in _columnList)
            {
                names[i++] = column.Name;
            }
        }
        else
        {
            names[0] = "Text";
        }

        return names;
    }

    public IColumnizedLogLineMemory SplitLine (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        ArgumentNullException.ThrowIfNull(logLine, nameof(logLine));

        return _isValidCsv
            ? SplitCsvLine(logLine)
            : CreateColumnizedLogLine(logLine);
    }

    private static ColumnizedLogLine CreateColumnizedLogLine (ILogLineMemory line)
    {
        ColumnizedLogLine cLogLine = new()
        {
            LogLine = line
        };

        cLogLine.ColumnValues = [new Column { FullValue = line.FullLine, Parent = cLogLine }];
        return cLogLine;
    }

    public bool IsTimeshiftImplemented ()
    {
        return false;
    }

    public void SetTimeOffset (int msecOffset)
    {
        throw new NotImplementedException();
    }

    public int GetTimeOffset ()
    {
        throw new NotImplementedException();
    }

    public DateTime GetTimestamp (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        throw new NotImplementedException();
    }

    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, string oldValue)
    {
        throw new NotImplementedException();
    }

    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, ReadOnlyMemory<char> oldValue)
    {
        throw new NotImplementedException();
    }

    public void Selected (ILogLineMemoryColumnizerCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        DetectDelimiterFromFile(callback);
        BuildColumns(callback);
    }

    /// <summary>
    /// This instance may not have pre-processed the current file (e.g. a clone that loaded the saved config),
    /// so re-detect the delimiter from the file's first line, like PreProcessLine() does on load.
    /// </summary>
    private void DetectDelimiterFromFile (ILogLineMemoryColumnizerCallback callback)
    {
        var line = _firstLine ?? callback.GetLogLineMemory(0);
        if (line != null)
        {
            AutoDetectDelimiter(line.FullLine);
        }
    }

    private void BuildColumns (ILogLineMemoryColumnizerCallback callback)
    {
        if (_isValidCsv) // see PreProcessLine()
        {
            _columnList.Clear();
            var line = _config.HasFieldNames
                ? _firstLine ?? callback.GetLogLineMemory(0)
                : callback.GetLogLineMemory(0);

            string[]? fields = null;
            if (line != null)
            {
                try
                {
                    fields = ReadFields(line.FullLine.ToString());
                }
                catch (CsvHelperException)
                {
                    // first line doesn't parse with the current settings (e.g. wrong delimiter); same fallback as SplitCsvLine
                }
            }

            if (fields != null)
            {
                if (_config.HasFieldNames)
                {
                    foreach (var headerColumn in fields)
                    {
                        _columnList.Add(new CsvColumn(headerColumn));
                    }
                }
                else
                {
                    for (var i = 0; i < fields.Length; ++i)
                    {
                        _columnList.Add(new CsvColumn(string.Format(CultureInfo.InvariantCulture, "Column {0}", i + 1)));
                    }
                }
            }
            else
            {
                _columnList.Add(new CsvColumn("Text"));
            }
        }
    }

    public void DeSelected (ILogLineMemoryColumnizerCallback callback)
    {
        // nothing to do
    }

    public ILogLineMemoryColumnizer CreateSnapshot ()
    {
        CsvColumnizerConfig config = new()
        {
            CommentChar = _config.CommentChar,
            DelimiterChar = _config.DelimiterChar,
            EscapeChar = _config.EscapeChar,
            HasFieldNames = _config.HasFieldNames,
            MinColumns = _config.MinColumns,
            QuoteChar = _config.QuoteChar,
            VersionBuild = _config.VersionBuild
        };
        config.ConfigureReaderConfiguration();

        CsvColumnizer clone = new()
        {
            _config = config,
            _isValidCsv = _isValidCsv,
            _firstLine = _firstLine == null ? null : new CsvLogLine(_firstLine.FullLine.ToString(), _firstLine.LineNumber)
        };

        foreach (var column in _columnList)
        {
            clone._columnList.Add(new CsvColumn(column.Name));
        }

        return clone;
    }

    public void Configure (ILogLineMemoryColumnizerCallback callback, string configDir)
    {
        var configPath = configDir + "\\" + CONFIGFILENAME;
        FileInfo fileInfo = new(configPath);

        // show the file's delimiter rather than the one from the saved config
        if (callback != null)
        {
            DetectDelimiterFromFile(callback);
        }

        CsvColumnizerConfigDlg dlg = new(_config);

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _config.VersionBuild = Assembly.GetExecutingAssembly().GetName().Version.Build;

            using (StreamWriter sw = new(fileInfo.Create()))
            {
                JsonSerializer serializer = new();
                serializer.Serialize(sw, _config);
            }

            _config.ConfigureReaderConfiguration();

            // no re-detection here: keep the delimiter the user chose in the dialog
            if (callback != null)
            {
                BuildColumns(callback);
            }
        }
    }

    public void LoadConfig (string configDir)
    {
        var configPath = Path.Join(configDir, CONFIGFILENAME);

        if (!File.Exists(configPath))
        {
            _config = new CsvColumnizerConfig();
            _config.InitDefaults();
        }
        else
        {
            try
            {
                _config = JsonConvert.DeserializeObject<CsvColumnizerConfig>(File.ReadAllText(configPath));
                _config.ConfigureReaderConfiguration();
            }
            catch (Exception ex) when (ex is JsonException or
                                             ArgumentException or
                                             ArgumentNullException or
                                             PathTooLongException or
                                             DirectoryNotFoundException or
                                             IOException or
                                             UnauthorizedAccessException or
                                             FileNotFoundException or
                                             NotSupportedException or
                                             SecurityException)
            {
                _ = MessageBox.Show(string.Format(CultureInfo.InvariantCulture, Resources.CsvColumnizer_UI_Message_ErrorWhileDeserializing, ex.Message), Resources.CsvColumnizer_UI_Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                _config = new CsvColumnizerConfig();
                _config.InitDefaults();
            }
        }
    }

    public Priority GetPriority (string fileName, IEnumerable<ILogLineMemory> samples)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        var result = Priority.NotSupport;

        if (fileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
        {
            result = Priority.CanSupport;
        }

        return result;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Auto-detects the delimiter using CsvHelper's built-in detection.
    /// After parsing, the detected delimiter is extracted from csv.Parser.Delimiter.
    /// </summary>
    private void AutoDetectDelimiter (ReadOnlyMemory<char> lineContent)
    {
        if (lineContent.IsEmpty)
        {
            return;
        }

        try
        {
            var autoDetectedConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                DetectDelimiter = true,
                DetectDelimiterValues = [",", ";", "\t", "|"]
            };

            using CsvReader csv = new(new StringReader(lineContent.ToString()), autoDetectedConfig);
            _ = csv.Read();

            var detectedDelimiter = csv.Parser.Delimiter;

            if (detectedDelimiter != _config.DelimiterChar)
            {
                _config.DelimiterChar = detectedDelimiter;
                _config.ConfigureReaderConfiguration();
            }
        }
        catch (CsvHelperException)
        {
            // If detection fails, keep the current config delimiter
        }
    }

    /// <summary>
    /// Parses one line into its fields; avoids ReadHeader(), which throws when HasHeaderRecord is false.
    /// </summary>
    private string[]? ReadFields (string line)
    {
        using CsvReader csv = new(new StringReader(line), _config.ReaderConfiguration);
        return csv.Read() ? csv.Parser.Record : null;
    }

    private ColumnizedLogLine SplitCsvLine (ILogLineMemory line)
    {
        if (line.FullLine.IsEmpty)
        {
            return CreateColumnizedLogLine(line);
        }

        ColumnizedLogLine cLogLine = new()
        {
            LogLine = line
        };

        try
        {
            var records = ReadFields(line.FullLine.ToString());

            if (records != null)
            {
                List<Column> columns = [];

                foreach (var record in records)
                {
                    columns.Add(new Column { FullValue = record.AsMemory(), Parent = cLogLine });
                }

                cLogLine.ColumnValues = [.. columns.Select(a => a as IColumnMemory)];
            }
        }
        catch (CsvHelperException)
        {
            return CreateColumnizedLogLine(line);
        }

        return cLogLine;
    }

    #endregion
}