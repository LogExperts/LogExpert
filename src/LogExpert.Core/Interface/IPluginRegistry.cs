namespace LogExpert.Core.Interface;

//TODO: Add documentation
public interface IPluginRegistry
{
    IList<ILogLineColumnizer> RegisteredColumnizers { get; }

    IFileSystemPlugin FindFileSystemForUri (string fileNameOrUri);
}