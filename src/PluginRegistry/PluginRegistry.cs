using System.Globalization;
using System.Reflection;

using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Entities;
using LogExpert.Core.Interface;
using LogExpert.PluginRegistry.FileSystem;

using NLog;

namespace LogExpert.PluginRegistry;

/// <summary>
/// Holds all registered plugins.
/// </summary>
/// <remarks>
/// It all has started with Columnizers only. So the different types of plugins have no common super interface. I didn't change it
/// to keep existing plugin API stable. In a future version this may change.
/// </remarks>
public class PluginRegistry : IPluginRegistry
{
    #region Fields

    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private static PluginRegistry? _instance;
    private static readonly object _lock = new();

    private readonly IFileSystemCallback _fileSystemCallback = new FileSystemCallback();
    private readonly IList<ILogExpertPlugin> _pluginList = [];
    private readonly IDictionary<string, IKeywordAction> _registeredKeywordsDict = new Dictionary<string, IKeywordAction>();

    #endregion

    private static string _applicationConfigurationFolder = string.Empty;
    private static int _pollingInterval = 250;

    #region cTor
    // Private constructor to prevent instantiation
    private PluginRegistry (string applicationConfigurationFolder, int pollingInterval)
    {
        _applicationConfigurationFolder = applicationConfigurationFolder;
        _pollingInterval = pollingInterval;
    }

    public PluginRegistry Create (string applicationConfigurationFolder, int pollingInterval)
    {
        if (_instance != null)
        {
            return _instance;
        }

        lock (_lock)
        {
            _instance = new PluginRegistry(applicationConfigurationFolder, pollingInterval);
        }

        _applicationConfigurationFolder = applicationConfigurationFolder;
        _pollingInterval = pollingInterval;

        _instance.LoadPlugins();
        return Instance;
    }

    #endregion

    #region Properties

    public static PluginRegistry Instance => _instance ?? new PluginRegistry(_applicationConfigurationFolder, _pollingInterval);

    public IList<ILogLineColumnizer> RegisteredColumnizers { get; private set; }

    public IList<IContextMenuEntry> RegisteredContextMenuPlugins { get; } = [];

    public IList<IKeywordAction> RegisteredKeywordActions { get; } = [];

    public IList<IFileSystemPlugin> RegisteredFileSystemPlugins { get; } = [];

    #endregion

    #region Public methods

    public static int PollingInterval => _pollingInterval;

    #endregion

    #region Internals

    internal void LoadPlugins ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "Loading plugins...");

        RegisteredColumnizers =
        [
            //TODO: Remove these plugins and load them as any other plugin
            new DefaultLogfileColumnizer(),
            new TimestampColumnizer(),
            new SquareBracketColumnizer(),
            new ClfColumnizer(),
        ];
        RegisteredFileSystemPlugins.Add(new LocalFileSystem());

        var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        //TODO: FIXME: This is a hack for the tests to pass. Need to find a better approach
        if (!Directory.Exists(pluginDir))
        {
            pluginDir = ".";
        }

        AppDomain.CurrentDomain.AssemblyResolve += ColumnizerResolveEventHandler;

        var interfaceName = typeof(ILogLineColumnizer).FullName
            ?? throw new NotImplementedException("The interface name is null. How did this happen? Let's fix this.");

        foreach (var dllName in Directory.EnumerateFiles(pluginDir, "*.dll"))
        {
            try
            {
                LoadPluginAssembly(dllName, interfaceName);
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException)
            {
                // Can happen when a 32bit-only DLL is loaded on a 64bit system (or vice versa)
                // or could be a not columnizer DLL (e.g. A DLL that is needed by a plugin).
                _logger.Error(ex, dllName);
            }
            catch (ReflectionTypeLoadException ex)
            {
                // can happen when a dll dependency is missing
                if (ex.LoaderExceptions != null && ex.LoaderExceptions.Length != 0)
                {
                    foreach (Exception loaderException in ex.LoaderExceptions)
                    {
                        _logger.Error(loaderException, "Plugin load failed with '{0}'", dllName);
                    }
                }
                _logger.Error(ex, "Loader exception during load of dll '{0}'", dllName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"General Exception for the file {dllName}, of type: {ex.GetType()}, with the message: {ex.Message}");
                throw;
            }
        }

        _logger.Info(CultureInfo.InvariantCulture, "Plugin loading complete.");
    }

    private void LoadPluginAssembly (string dllName, string interfaceName)
    {
        var assembly = Assembly.LoadFrom(dllName);
        Type[] types = assembly.GetTypes();

        foreach (Type type in types)
        {
            _logger.Info($"Type {type.FullName} in assembly {assembly.FullName} implements {interfaceName}");

            if (type.GetInterfaces().Any(i => i.FullName == interfaceName))
            {
                ConstructorInfo cti = type.GetConstructor(Type.EmptyTypes);
                if (cti != null)
                {
                    var instance = cti.Invoke([]);
                    RegisteredColumnizers.Add((ILogLineColumnizer)instance);

                    if (instance is IColumnizerConfigurator configurator)
                    {
                        configurator.LoadConfig(_applicationConfigurationFolder);
                    }

                    if (instance is ILogExpertPlugin plugin)
                    {
                        _pluginList.Add(plugin);
                        plugin.PluginLoaded();
                    }

                    _logger.Info($"Added columnizer {type.Name}");
                }
            }
            else
            {
                if (TryAsContextMenu(type))
                {
                    continue;
                }

                if (TryAsKeywordAction(type))
                {
                    continue;
                }

                if (TryAsFileSystem(type))
                {
                    continue;
                }
            }
        }
    }

    public IKeywordAction FindKeywordActionPluginByName (string name)
    {
        _registeredKeywordsDict.TryGetValue(name, out IKeywordAction action);
        return action;
    }

    public void CleanupPlugins ()
    {
        foreach (ILogExpertPlugin plugin in _pluginList)
        {
            plugin.AppExiting();
        }
    }

    public IFileSystemPlugin FindFileSystemForUri (string uriString)
    {
        if (_logger.IsDebugEnabled)
        {
            _logger.Debug(CultureInfo.InvariantCulture, "Trying to find file system plugin for uri {0}", uriString);
        }

        foreach (IFileSystemPlugin fs in RegisteredFileSystemPlugins)
        {
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug(CultureInfo.InvariantCulture, "Checking {0}", fs.Text);
            }

            if (fs.CanHandleUri(uriString))
            {
                if (_logger.IsDebugEnabled)
                {
                    _logger.Debug(CultureInfo.InvariantCulture, "Found match {0}", fs.Text);
                }

                return fs;
            }
        }

        _logger.Error("No file system plugin found for uri {0}", uriString);
        return null;
    }

    #endregion

    #region Private Methods
    //TODO: Can this be deleted?
    private bool TryAsContextMenu (Type type)
    {
        IContextMenuEntry me = TryInstantiate<IContextMenuEntry>(type);

        if (me != null)
        {
            RegisteredContextMenuPlugins.Add(me);
            if (me is ILogExpertPluginConfigurator configurator)
            {
                configurator.LoadConfig(_applicationConfigurationFolder);
            }

            if (me is ILogExpertPlugin plugin)
            {
                _pluginList.Add(plugin);
                plugin.PluginLoaded();
            }

            _logger.Info(CultureInfo.InvariantCulture, "Added context menu plugin {0}", type);
            return true;
        }

        return false;
    }

    //TODO: Can this be delted?
    private bool TryAsKeywordAction (Type type)
    {
        IKeywordAction ka = TryInstantiate<IKeywordAction>(type);
        if (ka != null)
        {
            RegisteredKeywordActions.Add(ka);
            _registeredKeywordsDict.Add(ka.GetName(), ka);
            if (ka is ILogExpertPluginConfigurator configurator)
            {
                configurator.LoadConfig(_applicationConfigurationFolder);
            }

            if (ka is ILogExpertPlugin plugin)
            {
                _pluginList.Add(plugin);
                plugin.PluginLoaded();
            }

            _logger.Info(CultureInfo.InvariantCulture, "Added keyword plugin {0}", type);
            return true;
        }

        return false;
    }

    //TODO: Can this be delted?
    private bool TryAsFileSystem (Type type)
    {
        // file system plugins can have optional constructor with IFileSystemCallback argument
        IFileSystemPlugin fs = TryInstantiate<IFileSystemPlugin>(type, _fileSystemCallback);
        fs ??= TryInstantiate<IFileSystemPlugin>(type);

        if (fs != null)
        {
            RegisteredFileSystemPlugins.Add(fs);
            if (fs is ILogExpertPluginConfigurator configurator)
            {
                //TODO Refactor, this should be set from outside once and not loaded all the time
                configurator.LoadConfig(_applicationConfigurationFolder);
            }

            if (fs is ILogExpertPlugin plugin)
            {
                _pluginList.Add(plugin);
                plugin.PluginLoaded();
            }

            _logger.Info(CultureInfo.InvariantCulture, "Added file system plugin {0}", type);
            return true;
        }

        return false;
    }

    private static T TryInstantiate<T> (Type loadedType) where T : class
    {
        Type t = typeof(T);
        Type inter = loadedType.GetInterface(t.Name);
        if (inter != null)
        {
            ConstructorInfo cti = loadedType.GetConstructor(Type.EmptyTypes);
            if (cti != null)
            {
                var o = cti.Invoke([]);
                return o as T;
            }
        }

        return default;
    }

    private static T TryInstantiate<T> (Type loadedType, IFileSystemCallback fsCallback) where T : class
    {
        Type t = typeof(T);
        Type inter = loadedType.GetInterface(t.Name);
        if (inter != null)
        {
            ConstructorInfo cti = loadedType.GetConstructor([typeof(IFileSystemCallback)]);
            if (cti != null)
            {
                var o = cti.Invoke([fsCallback]);
                return o as T;
            }
        }

        return default;
    }

    #endregion

    #region Events handler

    private static Assembly ColumnizerResolveEventHandler (object? sender, ResolveEventArgs args)
    {
        var fileName = new AssemblyName(args.Name).Name + ".dll";

        var mainDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", fileName);

        if (File.Exists(mainDir))
        {
            return Assembly.LoadFrom(mainDir);
        }

        if (File.Exists(pluginDir))
        {
            return Assembly.LoadFrom(pluginDir);
        }

        return null;
    }

    #endregion
}