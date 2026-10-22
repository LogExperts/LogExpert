using ColumnizerLib;

using LogExpert.Core.Interfaces;

namespace LogExpert.Benchmarks.Support;

/// <summary>
/// No-op IPluginRegistry for benchmarks. Returns empty columnizer list and
/// a stub file system plugin that handles all URIs via local file system.
/// </summary>
internal sealed class NullPluginRegistry : IPluginRegistry
{
    public static readonly NullPluginRegistry Instance = new();

    public IList<ILogLineMemoryColumnizer> RegisteredColumnizers { get; } = [];

    public IList<IContextMenuEntry> RegisteredContextMenuPlugins { get; } = [];

    public IFileSystemPlugin FindFileSystemForUri (string fileNameOrUri) => NullFileSystemPlugin.Instance;

    public IKeywordAction FindKeywordActionPluginByName (string name) => null;

    private sealed class NullFileSystemPlugin : IFileSystemPlugin
    {
        public static readonly NullFileSystemPlugin Instance = new();

        public string Text => "Null";
        public string Description => "No-op file system for benchmarks";
        public bool CanHandleUri (string uriString) => true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "For UnitTests")]
        public ILogFileInfo GetLogfileInfo (string uriString) => throw new NotSupportedException("NullFileSystemPlugin does not support GetLogfileInfo");
    }
}