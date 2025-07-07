using LogExpert.Core.Config;
using LogExpert.Core.EventArguments;

namespace LogExpert.Core.Interface;

//TODO: Add documentation
public interface IConfigManager
{
    Settings Settings { get; }

    string PortableModeDir { get; }

    string ConfigDir { get; }

    IConfigManager Instance { get; }

    string PortableModeSettingsFileName { get; }

    void Export (FileInfo fileInfo, SettingsFlags highlightSettings);

    void Export (FileInfo fileInfo);

    void Import (FileInfo fileInfo, ExportImportFlags importFlags);

    void ImportHighlightSettings (FileInfo fileInfo, ExportImportFlags importFlags);

    event EventHandler<ConfigChangedEventArgs> ConfigChanged; //TODO: All handlers that are public shoulld be in Core

    void Save (SettingsFlags flags);
}