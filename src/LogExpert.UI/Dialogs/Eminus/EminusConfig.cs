namespace LogExpert.UI.Dialogs.Eminus;

[Serializable]
internal class EminusConfig
{
    public string Host { get; set; } = "127.0.0.1";

    public string Password { get; set; } = string.Empty;

    public int Port { get; set; } = 12345;

    #region Public methods

    public EminusConfig Clone ()
    {
        EminusConfig config = new()
        {
            Host = Host,
            Port = Port,
            Password = Password
        };
        return config;
    }

    #endregion
}