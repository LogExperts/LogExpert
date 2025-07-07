using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Forms;

using CsvHelper;

using LogExpert;

using Newtonsoft.Json;

[assembly: SupportedOSPlatform("windows")]
namespace CsvColumnizer;

/// <summary>
/// This Columnizer can parse CSV files. It uses the IInitColumnizer interface for support of dynamic field count.
/// The IPreProcessColumnizer is implemented to read field names from the very first line of the file. Then
/// the line is dropped. So it's not seen by LogExpert. The field names will be used as column names.
/// </summary>
public class CsvColumnizer : ILogLineColumnizer, IInitColumnizer, IColumnizerConfigurator, IPreProcessColumnizer, IColumnizerPriority
{
    #region Fields

    private const string CONFIGFILENAME = "csvcolumnizer.json";

    private readonly IList<CsvColumn> _columnList = [];
    private CsvColumnizerConfig _config;

    private ILogLine _firstLine;

    // if CSV is detected to be 'invalid' the columnizer will behave like a default columnizer
    private bool _isValidCsv;

    #endregion

    #region Public methods

    public string PreProcessLine (string logLine, int lineNum, int realLineNum)
    {
        if (realLineNum == 0)
        {
            // store for later field names and field count retrieval
            _firstLine = new CsvLogLine(logLine, 0);

            if (_config.MinColumns > 0)
            {
                using CsvReader csv = new(new StringReader(logLine), _config.ReaderConfiguration);
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

        if (_config.CommentChar != ' ' && logLine.StartsWith("" + _config.CommentChar))
        {
            return null;
        }

        return logLine;
    }

    public string GetName ()
    {
        return "CSV Columnizer";
    }

    public string GetDescription ()
    {
        return "Splits CSV files into columns.\r\n\r\nCredits:\r\nThis Columnizer uses the CsvHelper. https://github.com/JoshClose/CsvHelper. \r\n";
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
            foreach (CsvColumn column in _columnList)
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

    public IColumnizedLogLine SplitLine (ILogLineColumnizerCallback callback, ILogLine line)
    {
        if (_isValidCsv)
        {
            return SplitCsvLine(line);
        }

        return CreateColumnizedLogLine(line);
    }

    private static ColumnizedLogLine CreateColumnizedLogLine (ILogLine line)
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

    public DateTime GetTimestamp (ILogLineColumnizerCallback callback, ILogLine line)
    {
        throw new NotImplementedException();
    }

    public void PushValue (ILogLineColumnizerCallback callback, int column, string value, string oldValue)
    {
        throw new NotImplementedException();
    }

    public void Selected (ILogLineColumnizerCallback callback)
    {
        if (_isValidCsv) // see PreProcessLine()
        {
            _columnList.Clear();
            ILogLine line = _config.HasFieldNames ? _firstLine : callback.GetLogLine(0);

            if (line != null)
            {
                using CsvReader csv = new(new StringReader(line.FullLine), _config.ReaderConfiguration);
                csv.Read();
                csv.ReadHeader();

                var fieldCount = csv.Parser.Count;

                var headerRecord = csv.HeaderRecord;

                if (_config.HasFieldNames && headerRecord != null)
                {
                    foreach (var headerColumn in headerRecord)
                    {
                        _columnList.Add(new CsvColumn(headerColumn));
                    }
                }
                else
                {
                    for (var i = 0; i < fieldCount; ++i)
                    {
                        _columnList.Add(new CsvColumn("Column " + i + 1));
                    }
                }
            }
        }
    }

    public void DeSelected (ILogLineColumnizerCallback callback)
    {
        // nothing to do
    }

    public void Configure (ILogLineColumnizerCallback callback, string configDir)
    {
        var configPath = configDir + "\\" + CONFIGFILENAME;
        FileInfo fileInfo = new(configPath);

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

            Selected(callback);
        }
    }

    public void LoadConfig (string configDir)
    {
        var configPath = Path.Combine(configDir, CONFIGFILENAME);

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
            catch (Exception e)
            {
                MessageBox.Show($"Error while deserializing config data: {e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _config = new CsvColumnizerConfig();
                _config.InitDefaults();
            }
        }
    }

    public Priority GetPriority (string fileName, IEnumerable<ILogLine> samples)
    {
        Priority result = Priority.NotSupport;

        if (fileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
        {
            result = Priority.CanSupport;
        }

        return result;
    }

    #endregion

    #region Private Methods

    private IColumnizedLogLine SplitCsvLine (ILogLine line)
    {
        ColumnizedLogLine cLogLine = new()
        {
            LogLine = line
        };

        using CsvReader csv = new(new StringReader(line.FullLine), _config.ReaderConfiguration);
        csv.Read();
        csv.ReadHeader();

        //we only read line by line and not the whole file so it is always the header
        var records = csv.HeaderRecord;

        if (records != null)
        {
            List<Column> columns = [];

            foreach (var record in records)
            {
                columns.Add(new Column { FullValue = record, Parent = cLogLine });
            }

            cLogLine.ColumnValues = columns.Select(a => a as IColumn).ToArray();
        }

        return cLogLine;
    }

    #endregion
}