using System;
using System.Collections.Generic;
using System.Linq;

using LogExpert;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonColumnizer;

/// <summary>
///     This Columnizer can parse JSON files.
/// </summary>
public class JsonColumnizer : ILogLineColumnizer, IInitColumnizer, IColumnizerPriority
{
    #region Properties

    public HashSet<string> ColumnSet { get; set; } = [];

    protected IList<JsonColumn> ColumnList { get; } = new List<JsonColumn>([InitialColumn]);

    protected static JsonColumn InitialColumn { get; } = new JsonColumn("Text");

    #endregion

    #region Public methods

    public virtual void Selected (ILogLineColumnizerCallback callback)
    {
        ColumnList.Clear();
        ColumnSet.Clear();

        ILogLine line = callback.GetLogLine(0);

        if (line != null)
        {
            JObject json = ParseJson(line);
            if (json != null)
            {
                var fieldCount = json.Properties().Count();

                for (var i = 0; i < fieldCount; ++i)
                {
                    var columeName = json.Properties().ToArray()[i].Name;
                    if (ColumnSet.Add(columeName))
                    {
                        ColumnList.Add(new JsonColumn(columeName));
                    }
                }
            }
            else
            {
                _ = ColumnSet.Add("Text");
                ColumnList.Add(InitialColumn);
            }
        }

        if (ColumnList.Count == 0)
        {
            _ = ColumnSet.Add("Text");
            ColumnList.Add(InitialColumn);
        }
    }

    public virtual void DeSelected (ILogLineColumnizerCallback callback)
    {
        // nothing to do
    }

    public virtual string GetName ()
    {
        return "JSON Columnizer";
    }

    public virtual string GetDescription ()
    {
        return "Splits JSON files into columns.\r\n\r\nCredits:\r\nThis Columnizer uses the Newtonsoft json package.\r\n\r\nFirst line must be valid or else only one column will be displayed and the other values dropped!";
    }

    public virtual int GetColumnCount ()
    {
        return ColumnList.Count;
    }

    public virtual string[] GetColumnNames ()
    {
        var names = new string[GetColumnCount()];
        var i = 0;
        foreach (JsonColumn column in ColumnList)
        {
            names[i++] = column.Name;
        }

        return names;
    }

    public virtual IColumnizedLogLine SplitLine (ILogLineColumnizerCallback callback, ILogLine line)
    {
        JObject json = ParseJson(line);

        if (json != null)
        {
            return SplitJsonLine(line, json);
        }

        var cLogLine = new ColumnizedLogLine { LogLine = line };

        Column[] columns = Column.CreateColumns(ColumnList.Count, cLogLine);

        columns.Last().FullValue = line.FullLine;

        cLogLine.ColumnValues = columns.Select(a => (IColumn)a).ToArray();

        return cLogLine;
    }

    public virtual bool IsTimeshiftImplemented ()
    {
        return false;
    }

    public virtual void SetTimeOffset (int msecOffset)
    {
        throw new NotImplementedException();
    }

    public virtual int GetTimeOffset ()
    {
        throw new NotImplementedException();
    }

    public virtual DateTime GetTimestamp (ILogLineColumnizerCallback callback, ILogLine line)
    {
        throw new NotImplementedException();
    }

    public virtual void PushValue (ILogLineColumnizerCallback callback, int column, string value, string oldValue)
    {
        throw new NotImplementedException();
    }

    public virtual Priority GetPriority (string fileName, IEnumerable<ILogLine> samples)
    {
        Priority result = Priority.NotSupport;
        if (fileName.EndsWith("json", StringComparison.OrdinalIgnoreCase))
        {
            result = Priority.WellSupport;
        }

        return result;
    }

    #endregion

    #region Private Methods

    protected static JObject ParseJson (ILogLine line)
    {
        return JsonConvert.DeserializeObject<JObject>(line.FullLine, new JsonSerializerSettings()
        {
            Error = (sender, args) => { args.ErrorContext.Handled = true; } //We ignore the error and handle the null value
        });
    }

    public class ColumnWithName : Column
    {
        public string ColumnName { get; set; }
    }

    //
    // Following two log lines should be loaded and displayed in correct grid.
    // {"time":"2019-02-13T02:55:35.5186240Z","message":"Hosting starting"}
    // {"time":"2019-02-13T02:55:35.5186240Z","level":"warning", "message":"invalid host."}
    //
    protected virtual IColumnizedLogLine SplitJsonLine (ILogLine line, JObject json)
    {
        var cLogLine = new ColumnizedLogLine { LogLine = line };

        var columns = json.Properties().Select(property => new ColumnWithName { FullValue = property.Value.ToString(), ColumnName = property.Name.ToString(), Parent = cLogLine }).ToList();

        foreach (ColumnWithName jsonColumn in columns)
        {
            // When find new column in a log line, add a new column in the end of the list.
            if (!ColumnSet.Contains(jsonColumn.ColumnName))
            {
                if (ColumnList.Count == 1 && !ColumnSet.Contains(ColumnList[0].Name))
                {
                    ColumnList.Clear();
                }

                _ = ColumnSet.Add(jsonColumn.ColumnName);
                ColumnList.Add(new JsonColumn(jsonColumn.ColumnName));
            }
        }

        //
        // Always rearrage the order of all json fields within a line to follow the sequence of columnNameList.
        // This will make sure the log line displayed correct even the order of json fields changed.
        //
        List<IColumn> returnColumns = [];
        foreach (JsonColumn column in ColumnList)
        {
            ColumnWithName existingColumn = columns.Find(x => x.ColumnName == column.Name);
            if (existingColumn != null)
            {
                returnColumns.Add(new Column() { FullValue = existingColumn.FullValue, Parent = cLogLine });
                continue;
            }

            // Fields that is missing in current line should be shown as empty.
            returnColumns.Add(new Column() { FullValue = "", Parent = cLogLine });
        }

        cLogLine.ColumnValues = [.. returnColumns];

        return cLogLine;
    }

    #endregion
}