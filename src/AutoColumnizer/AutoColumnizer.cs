using LogExpert;

using System;

namespace AutoColumnizer;

public class AutoColumnizer : ILogLineColumnizer
{
    #region ILogLineColumnizer implementation

    public string Text => GetName();

    public bool IsTimeshiftImplemented()
    {
        return true;
    }

    public string GetName()
    {
        return "Auto Columnizer";
    }

    public string GetDescription()
    {
        return "Automatically find the right columnizer for any file";
    }


    public int GetColumnCount()
    {
        throw new NotImplementedException();
    }

    public string[] GetColumnNames()
    {
        throw new NotImplementedException();
    }

    public IColumnizedLogLine SplitLine(ILogLineColumnizerCallback callback, ILogLine line)
    {
        throw new NotImplementedException();
    }

    public void SetTimeOffset(int msecOffset)
    {
        throw new NotImplementedException();
    }

    public int GetTimeOffset()
    {
        throw new NotImplementedException();
    }

    public DateTime GetTimestamp(ILogLineColumnizerCallback callback, ILogLine line)
    {
        throw new NotImplementedException();
    }

    public void PushValue(ILogLineColumnizerCallback callback, int column, string value, string oldValue)
    {
    }

    #endregion ILogLineColumnizer implementation
}