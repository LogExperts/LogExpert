using ColumnizerLib;

namespace LogExpert.Core.Interfaces;

//TODO: Add documentation
public interface IPluginRegistry
{
    IList<ILogLineMemoryColumnizer> RegisteredColumnizers { get; }

    IList<IContextMenuEntry> RegisteredContextMenuPlugins { get; }

    IFileSystemPlugin FindFileSystemForUri (string fileNameOrUri);

    IKeywordAction FindKeywordActionPluginByName (string name);
}