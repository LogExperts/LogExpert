using ColumnizerLib;

namespace AutoColumnizer;

public class AutoColumnizer : ILogLineMemoryColumnizer
{
    #region ILogLineColumnizer implementation

    public string Text => GetName();

    public bool IsTimeshiftImplemented ()
    {
        return true;
    }

    public string GetName ()
    {
        return "Auto Columnizer";
    }

    public string GetCustomName ()
    {
        return GetName();
    }

    public string GetDescription ()
    {
        return "Automatically find the right columnizer for any file";
    }

    public int GetColumnCount ()
    {
        throw new NotImplementedException();
    }

    public string[] GetColumnNames ()
    {
        throw new NotImplementedException();
    }

    public IColumnizedLogLineMemory SplitLine (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        throw new NotImplementedException();
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
    }

    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, ReadOnlyMemory<char> oldValue)
    {
    }

    #endregion ILogLineColumnizer implementation
}