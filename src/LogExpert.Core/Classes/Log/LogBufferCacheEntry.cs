namespace LogExpert.Core.Classes.Log;

public class LogBufferCacheEntry
{
    #region Fields

    #endregion

    #region cTor

    public LogBufferCacheEntry()
    {
        Touch();
    }

    #endregion

    #region Properties

    public LogBuffer LogBuffer { get; set; }

    public long LastUseTimeStamp { get; private set; }

    #endregion

    #region Public methods

    public void Touch()
    {
        LastUseTimeStamp = Environment.TickCount & int.MaxValue;
    }

    #endregion
}