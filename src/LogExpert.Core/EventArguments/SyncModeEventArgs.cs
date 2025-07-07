namespace LogExpert.Core.EventArguments;

public class SyncModeEventArgs(bool isSynced) : System.EventArgs
{
    #region Properties

    public bool IsTimeSynced { get; } = isSynced;

    #endregion
}