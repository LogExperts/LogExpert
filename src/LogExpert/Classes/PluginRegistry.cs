using LogExpert.Classes.Columnizer;
using LogExpert.Config;
using LogExpert.Entities;
using LogExpert.Extensions;

using NLog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace LogExpert.Classes
{
    /// <summary>
    /// Holds all registered plugins.
    /// </summary>
    /// <remarks>
    /// It all has started with Columnizers only. So the different types of plugins have no common super interface. I didn't change it
    /// to keep existing plugin API stable. In a future version this may change.
    /// </remarks>
    public class PluginRegistry
    {
        #region Fields

        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private static readonly Lazy<PluginRegistry> _instance = new(() => new PluginRegistry());

        private readonly IFileSystemCallback _fileSystemCallback = new FileSystemCallback();
        private readonly IList<ILogExpertPlugin> _pluginList = new List<ILogExpertPlugin>();
        private readonly IDictionary<string, IKeywordAction> _registeredKeywordsDict = new Dictionary<string, IKeywordAction>();

        #endregion

        #region cTor
        // Private constructor to prevent instantiation
        private PluginRegistry()
        {
            LoadPlugins();
        }

        #endregion

        #region Properties

        public static PluginRegistry Instance => _instance.Value;

        public IList<ILogLineColumnizer> RegisteredColumnizers { get; private set; }

        public IList<IContextMenuEntry> RegisteredContextMenuPlugins { get; } = new List<IContextMenuEntry>();

        public IList<IKeywordAction> RegisteredKeywordActions { get; } = new List<IKeywordAction>();

        public IList<IFileSystemPlugin> RegisteredFileSystemPlugins { get; } = new List<IFileSystemPlugin>();

        #endregion

        #region Public methods
        #endregion

        #region Internals

        internal void LoadPlugins()
        {
            _logger.Info("Loading plugins...");

            RegisteredColumnizers =
            [
                //TODO: Remove these plugins and load them as any other plugin
                new DefaultLogfileColumnizer(),
                new TimestampColumnizer(),
                new SquareBracketColumnizer(),
                new ClfColumnizer(),
            ];
            RegisteredFileSystemPlugins.Add(new LocalFileSystem());

            string pluginDir = Path.Combine(Application.StartupPath, "plugins");
            //TODO: FIXME: This is a hack for the tests to pass. Need to find a better approach
            if (!Directory.Exists(pluginDir))
            {
                pluginDir = ".";
            }

            AppDomain.CurrentDomain.AssemblyResolve += ColumnizerResolveEventHandler;

            string interfaceName = typeof(ILogLineColumnizer).FullName;
            foreach (string dllName in Directory.EnumerateFiles(pluginDir, "*.dll"))
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
                    if (!ex.LoaderExceptions.IsEmpty())
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

            _logger.Info("Plugin loading complete.");
        }

        private void LoadPluginAssembly(string dllName, string interfaceName)
        {
            Assembly assembly = Assembly.LoadFrom(dllName);
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                _logger.Info($"Type {type.FullName} in assembly {assembly.FullName} implements {interfaceName}");

                if (type.GetInterfaces().Any(i => i.FullName == interfaceName))
                {
                    ConstructorInfo cti = type.GetConstructor(Type.EmptyTypes);
                    if (cti != null)
                    {
                        object instance = cti.Invoke([]);
                        RegisteredColumnizers.Add((ILogLineColumnizer)instance);

                        if (instance is IColumnizerConfigurator configurator)
                        {
                            configurator.LoadConfig(ConfigManager.Settings.Preferences.PortableMode ? ConfigManager.PortableModeDir : ConfigManager.ConfigDir);
                        }

                        if (instance is ILogExpertPlugin plugin)
                        {
                            _pluginList.Add(plugin);
                            plugin.PluginLoaded();
                        }

                        _logger.Info("Added columnizer {0}", type.Name);
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

        internal IKeywordAction FindKeywordActionPluginByName(string name)
        {
            _registeredKeywordsDict.TryGetValue(name, out IKeywordAction action);
            return action;
        }

        internal void CleanupPlugins()
        {
            foreach (ILogExpertPlugin plugin in _pluginList)
            {
                plugin.AppExiting();
            }
        }

        internal IFileSystemPlugin FindFileSystemForUri(string uriString)
        {
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug("Trying to find file system plugin for uri {0}", uriString);
            }

            foreach (IFileSystemPlugin fs in RegisteredFileSystemPlugins)
            {
                if (_logger.IsDebugEnabled)
                {
                    _logger.Debug("Checking {0}", fs.Text);
                }

                if (fs.CanHandleUri(uriString))
                {
                    if (_logger.IsDebugEnabled)
                    {
                        _logger.Debug("Found match {0}", fs.Text);
                    }

                    return fs;
                }
            }

            _logger.Error("No file system plugin found for uri {0}", uriString);
            return null;
        }

        #endregion

        #region Private Methods
        //TODO: Can this be delted?
        private bool TryAsContextMenu(Type type)
        {
            IContextMenuEntry me = TryInstantiate<IContextMenuEntry>(type);
            if (me != null)
            {
                RegisteredContextMenuPlugins.Add(me);
                if (me is ILogExpertPluginConfigurator configurator)
                {
                    configurator.LoadConfig(ConfigManager.ConfigDir);
                }

                if (me is ILogExpertPlugin)
                {
                    _pluginList.Add(me as ILogExpertPlugin);
                    (me as ILogExpertPlugin).PluginLoaded();
                }

                _logger.Info("Added context menu plugin {0}", type);
                return true;
            }

            return false;
        }

        //TODO: Can this be delted?
        private bool TryAsKeywordAction(Type type)
        {
            IKeywordAction ka = TryInstantiate<IKeywordAction>(type);
            if (ka != null)
            {
                RegisteredKeywordActions.Add(ka);
                _registeredKeywordsDict.Add(ka.GetName(), ka);
                if (ka is ILogExpertPluginConfigurator configurator)
                {
                    configurator.LoadConfig(ConfigManager.ConfigDir);
                }

                if (ka is ILogExpertPlugin)
                {
                    _pluginList.Add(ka as ILogExpertPlugin);
                    (ka as ILogExpertPlugin).PluginLoaded();
                }

                _logger.Info("Added keyword plugin {0}", type);
                return true;
            }

            return false;
        }

        //TODO: Can this be delted?
        private bool TryAsFileSystem(Type type)
        {
            // file system plugins can have optional constructor with IFileSystemCallback argument
            IFileSystemPlugin fs = TryInstantiate<IFileSystemPlugin>(type, _fileSystemCallback);
            fs ??= TryInstantiate<IFileSystemPlugin>(type);

            if (fs != null)
            {
                RegisteredFileSystemPlugins.Add(fs);
                if (fs is ILogExpertPluginConfigurator configurator)
                {
                    configurator.LoadConfig(ConfigManager.ConfigDir);
                }

                if (fs is ILogExpertPlugin)
                {
                    _pluginList.Add(fs as ILogExpertPlugin);
                    (fs as ILogExpertPlugin).PluginLoaded();
                }

                _logger.Info("Added file system plugin {0}", type);
                return true;
            }

            return false;
        }

        private static T TryInstantiate<T>(Type loadedType) where T : class
        {
            Type t = typeof(T);
            Type inter = loadedType.GetInterface(t.Name);
            if (inter != null)
            {
                ConstructorInfo cti = loadedType.GetConstructor(Type.EmptyTypes);
                if (cti != null)
                {
                    object o = cti.Invoke([]);
                    return o as T;
                }
            }

            return default(T);
        }

        private static T TryInstantiate<T>(Type loadedType, IFileSystemCallback fsCallback) where T : class
        {
            Type t = typeof(T);
            Type inter = loadedType.GetInterface(t.Name);
            if (inter != null)
            {
                ConstructorInfo cti = loadedType.GetConstructor([typeof(IFileSystemCallback)]);
                if (cti != null)
                {
                    object o = cti.Invoke([fsCallback]);
                    return o as T;
                }
            }

            return default;
        }

        #endregion

        #region Events handler

        private static Assembly ColumnizerResolveEventHandler(object sender, ResolveEventArgs args)
        {
            string fileName = new AssemblyName(args.Name).Name + ".dll";
            string mainDir = Path.Combine(Application.StartupPath, fileName);
            string pluginDir = Path.Combine(Application.StartupPath, "plugins", fileName);

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
}