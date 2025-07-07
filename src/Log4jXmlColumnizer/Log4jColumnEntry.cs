using System;

namespace Log4jXmlColumnizer;

/// <summary>
/// Helper class for configuration of the columns.
/// </summary>
[Serializable]
public class Log4jColumnEntry(string name, int index, int maxLen)
{
    public int ColumnIndex { get; set; } = index;

    public string ColumnName { get; set; } = name;

    public int MaxLen { get; set; } = maxLen;

    public bool Visible { get; set; }
}