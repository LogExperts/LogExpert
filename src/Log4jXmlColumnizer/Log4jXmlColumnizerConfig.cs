﻿using System;
using System.Collections.Generic;

namespace Log4jXmlColumnizer;

[Serializable]
public class Log4jXmlColumnizerConfig
{
    #region Fields

    public List<Log4jColumnEntry> columnList = [];
    public bool localTimestamps = true;

    #endregion

    #region cTor

    public Log4jXmlColumnizerConfig(string[] columnNames)
    {
        FillDefaults(columnNames);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Returns the column count. Because the user can deactivate columns in the config
    /// the actual column count may be smaller than the number of available columns.
    /// </summary>
    public int ActiveColumnCount
    {
        get
        {
            var count = 0;
            foreach (Log4jColumnEntry entry in columnList)
            {
                if (entry.Visible)
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Returns the names of all active columns.
    /// </summary>
    public string[] ActiveColumnNames
    {
        get
        {
            var names = new string[ActiveColumnCount];
            var index = 0;
            foreach (Log4jColumnEntry entry in columnList)
            {
                if (entry.Visible)
                {
                    names[index++] = entry.ColumnName;
                }
            }
            return names;
        }
    }

    #endregion

    #region Public methods

    public void FillDefaults(string[] columnNames)
    {
        columnList.Clear();
        for (var i = 0; i < columnNames.Length; ++i)
        {
            columnList.Add(new Log4jColumnEntry(columnNames[i], i, 0));
        }
    }

    #endregion
}