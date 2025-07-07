#region

using System;

#endregion

namespace LogExpert.Core.Config;

[Serializable]
public class ColumnizerHistoryEntry
{
    #region cTor

    public ColumnizerHistoryEntry(string fileName, string columnizerName)
    {
        FileName = fileName;
        ColumnizerName = columnizerName;
    }

    #endregion

    #region Fields

    public string FileName { get; }

    #endregion

    public string ColumnizerName { get; }
}