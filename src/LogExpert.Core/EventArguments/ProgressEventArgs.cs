namespace LogExpert.Core.EventArguments;

public class ProgressEventArgs : System.EventArgs
{
    #region Properties

    public int Value { get; set; }

    public int MinValue { get; set; }

    public int MaxValue { get; set; }

    public bool Visible { get; set; }

    #endregion
}