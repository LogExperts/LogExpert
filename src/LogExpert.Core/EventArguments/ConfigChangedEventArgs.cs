using LogExpert.Core.Config;

namespace LogExpert.Core.EventArguments;

public class ConfigChangedEventArgs(SettingsFlags changeFlags) : System.EventArgs
{
    #region Properties

    public SettingsFlags Flags { get; } = changeFlags;

    #endregion
}