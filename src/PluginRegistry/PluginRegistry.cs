using System.Globalization;
using System.Reflection;
using System.Security;

using ColumnizerLib;

using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.PluginRegistry.Events;
using LogExpert.PluginRegistry.FileSystem;
using LogExpert.PluginRegistry.Interfaces;

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

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private static volatile PluginRegistry? _instance;
    private static readonly Lock _lock = new();

    private readonly IFileSystemCallback _fileSystemCallback = new FileSystemCallback();
    private readonly IList<ILogExpertPlugin> _pluginList = [];
    private readonly Dictionary<string, IKeywordAction> _registeredKeywordsDict = [];

    private readonly IPluginLoader _pluginLoader;
    private readonly PluginCache? _pluginCache;
    private readonly PluginEventBus _eventBus;

    // Lazy loaders for each plugin type - Type-Aware Lazy Loading
    private readonly List<LazyPluginLoader<ILogLineMemoryColumnizer>> _lazyColumnizers = [];
    private readonly List<LazyPluginLoader<IFileSystemPlugin>> _lazyFileSystemPlugins = [];
    private readonly List<LazyPluginLoader<IContextMenuEntry>> _lazyContextMenuPlugins = [];
    private readonly List<LazyPluginLoader<IKeywordAction>> _lazyKeywordActions = [];

    // Type-aware lazy loading
    private bool _useLazyLoading;
    private bool _usePluginCache;
    private bool _useLifecycleHooks = true;
    private bool _useEventBus = true;

    #endregion

    private static string _applicationConfigurationFolder = string.Empty;

    #region Events

    /// <summary>
    /// Occurs when plugin loading progress changes.
    /// </summary>
    public event EventHandler<PluginLoadProgressEventArgs>? PluginLoadProgress;

    #endregion

    #region cTor
    // Private constructor to prevent instantiation
    private PluginRegistry (string applicationConfigurationFolder, int pollingInterval)
    {
        _applicationConfigurationFolder = applicationConfigurationFolder;
        PollingInterval = pollingInterval;

        _pluginLoader = new DefaultPluginLoader();
        _eventBus = new PluginEventBus();

        // Load feature flags from configuration
        LoadFeatureFlags();

        // Initialize cache if enabled
        if (_usePluginCache)
        {
            _pluginCache = new PluginCache(
                cacheExpiration: TimeSpan.FromHours(24),
                loader: _pluginLoader);
            _logger.Info("Plugin cache enabled (24-hour expiration)");
        }

        if (_useLazyLoading)
        {
            _logger.Info("Lazy plugin loading enabled");
        }
    }

    public static PluginRegistry Create (string applicationConfigurationFolder, int pollingInterval)
    {
        if (_instance != null)
        {
            return _instance;
        }

        lock (_lock)
        {
            // Re-check inside the lock: another thread may have created the
            // instance while this one was waiting on the lock.
            // Happens mostly in UnitTests where Create() is called multiple times.
            // CA1508 is not applicable here because the null check is intentional for initialization.
#pragma warning disable CA1508 // Avoid dead conditional code
            if (_instance == null)
            {
                var registry = new PluginRegistry(applicationConfigurationFolder, pollingInterval);

                // Fully initialize before publishing so the lock-free fast path
                // above never returns a half-loaded registry.
                registry.LoadPlugins();

                _instance = registry;
            }
#pragma warning restore CA1508 // Avoid dead conditional code
        }

        return _instance;
    }

    /// <summary>
    /// Registers the assembly resolve handler so that plugin assemblies in the "plugins" subdirectory
    /// can be found during type resolution (e.g., when deserializing settings that reference plugin types).
    /// Must be called before any code that may trigger Type.GetType() for plugin types.
    /// </summary>
    public static void RegisterAssemblyResolver ()
    {
        AppDomain.CurrentDomain.AssemblyResolve -= ColumnizerResolveEventHandler;
        AppDomain.CurrentDomain.AssemblyResolve += ColumnizerResolveEventHandler;
    }

    #endregion

    #region Properties

    public static PluginRegistry Instance => _instance ?? new PluginRegistry(_applicationConfigurationFolder, PollingInterval);

    /// <summary>
    /// Gets the list of registered columnizer plugins.
    /// Triggers lazy loading of columnizers if lazy loading is enabled.
    /// </summary>
    public IList<ILogLineMemoryColumnizer> RegisteredColumnizers
    {
        get
        {
            // Trigger lazy loading on first access
            if (_useLazyLoading && _lazyColumnizers.Count > 0)
            {
                _logger.Debug("Lazy loading {Count} columnizer(s) on first access", _lazyColumnizers.Count);

                foreach (var loader in _lazyColumnizers.ToList())
                {
                    var instance = loader.GetInstance();
                    // Data duplication by type, not by reference: GetInstance() returns a freshly constructed
                    // object, so Contains() (reference equality) would never catch a same-type duplicate.
                    if (instance != null && !field.Any(c => c.GetType() == instance.GetType()))
                    {
                        field.Add(instance);
                        InitializePluginIfNeeded(instance, loader.Manifest, loader.DllPath);

                        // Add to keyword actions dictionary if applicable
                        if (instance is IKeywordAction keywordAction &&
                            !_registeredKeywordsDict.ContainsKey(keywordAction.GetName()))
                        {
                            _registeredKeywordsDict.Add(keywordAction.GetName(), keywordAction);
                        }
                    }
                }

                _lazyColumnizers.Clear();
                _logger.Info("Lazy loaded columnizers, total count: {Count}", field.Count);
            }

            return field;
        }
        private set;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentionally Catch All")]
    internal void LoadPlugins ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "Loading plugins with security validation and manifest support...");

        // Load plugin permissions from configuration
        PluginPermissionManager.LoadPermissions(_applicationConfigurationFolder);

        RegisteredColumnizers =
        [
            //Default Columnizer if other Plugins can not be loaded
            new DefaultLogfileColumnizer(),
            new TimestampColumnizer(),
            new SquareBracketColumnizer(),
            new ClfColumnizer(),
        ];

        //Default FileSystem if other FileSystem Plugins cannot be loaded
        RegisteredFileSystemPlugins.Add(new LocalFileSystem());

        var pluginDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "plugins");

        if (!Directory.Exists(pluginDir))
        {
            _logger.Warn("Plugin directory not found: {PluginDir}. Skipping plugin loading.", pluginDir);
            pluginDir = ".";
        }

        AppDomain.CurrentDomain.AssemblyResolve += ColumnizerResolveEventHandler;

        var loadedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        // Get list of DLL files for progress tracking
        var dllFiles = Directory.EnumerateFiles(pluginDir, "*.dll").ToList();
        var totalPlugins = dllFiles.Count;

        // Fire Started event
        OnPluginLoadProgress(new PluginLoadProgressEventArgs(
            pluginDir,
            "Plugin Loading",
            0,
            totalPlugins,
            PluginLoadStatus.Started,
            $"Starting to load {totalPlugins} potential plugin(s)"));

        var currentIndex = 0;
        foreach (var dllName in dllFiles)
        {
            var fileName = Path.GetFileName(dllName);

            try
            {
                // Fire Validating event
                OnPluginLoadProgress(new PluginLoadProgressEventArgs(
                    dllName,
                    fileName,
                    currentIndex,
                    totalPlugins,
                    PluginLoadStatus.Validating,
                    Resources.PluginRegistry_PluginLoadingProgress_ValidatingPluginSecurityAndManifest));

                // Validate plugin before loading (with manifest support)
                if (!PluginValidator.ValidatePlugin(dllName, out var manifest, out var errorMessage))
                {
                    skippedCount++;
                    _logger.Info("Skipped plugin (failed validation): {FileName}", fileName);

                    // Fire Skipped event with user-friendly error message
                    OnPluginLoadProgress(new PluginLoadProgressEventArgs(
                        dllName,
                        fileName,
                        currentIndex,
                        totalPlugins,
                        PluginLoadStatus.Skipped,
                        errorMessage ?? Resources.PluginRegistry_PluginLoadingProgress_FailedValidationNotTrustedOrInvalidManifest));

                    currentIndex++;
                    continue;
                }

                // Fire Validated event
                OnPluginLoadProgress(new PluginLoadProgressEventArgs(
                    dllName,
                    fileName,
                    currentIndex,
                    totalPlugins,
                    PluginLoadStatus.Validated,
                    manifest != null ? $"Validated: {manifest.Name} v{manifest.Version}" : "Validated successfully"));

                // Log manifest information if available
                if (manifest != null)
                {
                    _logger.Info("Plugin {PluginName} v{Version} by {Author}",
                        manifest.Name, manifest.Version, manifest.Author ?? "Unknown");
                    if (manifest.Permissions != null && manifest.Permissions.Count > 0)
                    {
                        _logger.Debug("  Permissions: {Permissions}", string.Join(", ", manifest.Permissions));
                    }
                }

                // Fire Loading event
                OnPluginLoadProgress(new PluginLoadProgressEventArgs(
                    dllName,
                    fileName,
                    currentIndex,
                    totalPlugins,
                    PluginLoadStatus.Loading,
                    Resources.PluginRegistry_PluginLoadingProgress_LoadingPluginAssembly));

                // Load plugin with timeout and exception handling (with manifest support)
                // LoadPluginAssemblySafe will detect and register all plugin types (ILogLineColumnizer, IFileSystemPlugin, etc.)
                if (LoadPluginAssemblySafe(dllName, manifest))
                {
                    loadedCount++;

                    // Fire Loaded event
                    OnPluginLoadProgress(new PluginLoadProgressEventArgs(
                        dllName,
                        fileName,
                        currentIndex,
                        totalPlugins,
                        PluginLoadStatus.Loaded,
                        manifest != null ? $"Loaded {manifest.Name}" : "Loaded successfully"));
                }
                else
                {
                    failedCount++;

                    // Fire Failed event
                    OnPluginLoadProgress(new PluginLoadProgressEventArgs(
                        dllName,
                        fileName,
                        currentIndex,
                        totalPlugins,
                        PluginLoadStatus.Failed,
                        Resources.PluginRegistry_PluginLoadingProgress_FailedToLoadPluginAssemblyTimeoutOrError));
                }
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException)
            {
                // Can happen when a 32bit-only DLL is loaded on a 64bit system (or vice versa)
                // or could be a not columnizer DLL (e.g. A DLL that is needed by a plugin).
                var errorMsg = PluginErrorMessages.BadImageFormat(fileName, Environment.Is64BitProcess);
                _logger.Warn(ex, "Plugin load failed (bad format): {FileName}", fileName);
                failedCount++;

                // Fire Failed event
                OnPluginLoadProgress(new PluginLoadProgressEventArgs(
                    dllName,
                    fileName,
                    currentIndex,
                    totalPlugins,
                    PluginLoadStatus.Failed,
                    errorMsg));
            }
            catch (ReflectionTypeLoadException ex)
            {
                // can happen when a dll dependency is missing
                string errorMsg = Resources.PluginRegistry_PluginLoadingProgress_FailedToLoadPluginAssemblyTimeoutOrError;

                if (ex.LoaderExceptions != null && ex.LoaderExceptions.Length != 0)
                {
                    foreach (var loaderException in ex.LoaderExceptions)
                    {
                        _logger.Error(loaderException, "Plugin load failed with '{0}'", dllName);
                    }

                    // Extract dependency name from first exception if possible
                    var firstException = ex.LoaderExceptions[0];
                    if (firstException is FileNotFoundException fileNotFound &&
                        !string.IsNullOrEmpty(fileNotFound.FileName))
                    {
                        errorMsg = PluginErrorMessages.MissingDependency(fileName, fileNotFound.FileName);
                    }
                }

                _logger.Error(ex, "Loader exception during load of dll '{0}'", dllName);
                failedCount++;

                // Fire Failed event
                OnPluginLoadProgress(new PluginLoadProgressEventArgs(
                    dllName,
                    fileName,
                    currentIndex,
                    totalPlugins,
                    PluginLoadStatus.Failed,
                    errorMsg));
            }
            catch (Exception ex)
            {
                var errorMsg = PluginErrorMessages.GenericError("loading", fileName, ex);
                _logger.Error(ex, "General exception loading plugin: {FileName}", fileName);
                failedCount++;

                // Fire Failed event
                OnPluginLoadProgress(new PluginLoadProgressEventArgs(
                    dllName,
                    fileName,
                    currentIndex,
                    totalPlugins,
                    PluginLoadStatus.Failed,
                    errorMsg));
            }

            currentIndex++;
        }

        _logger.Info("Plugin loading complete. Loaded: {LoadedCount}, Skipped: {SkippedCount}, Failed: {FailedCount}", loadedCount, skippedCount, failedCount);

        // Fire Completed event
        OnPluginLoadProgress(new PluginLoadProgressEventArgs(
            pluginDir,
            "Plugin Loading",
            totalPlugins,
            totalPlugins,
            PluginLoadStatus.Completed,
            $"Completed: {loadedCount} loaded, {skippedCount} skipped, {failedCount} failed"));

        // Save any permission changes
        PluginPermissionManager.SavePermissions(_applicationConfigurationFolder);
    }

    /// <summary>
    /// Raises the PluginLoadProgress event.
    /// </summary>
    /// <param name="e">Event arguments containing progress information.</param>
    protected virtual void OnPluginLoadProgress (PluginLoadProgressEventArgs e)
    {
        PluginLoadProgress?.Invoke(this, e);
    }

    /// <summary>
    /// Gets the list of registered file system plugins.
    /// Triggers lazy loading of file system plugins if lazy loading is enabled.
    /// </summary>
    public IList<IFileSystemPlugin> RegisteredFileSystemPlugins
    {
        get
        {
            if (_useLazyLoading && _lazyFileSystemPlugins.Count > 0)
            {
                _logger.Debug("Lazy loading {Count} file system plugin(s) on first access", _lazyFileSystemPlugins.Count);

                foreach (var loader in _lazyFileSystemPlugins.ToList())
                {
                    var instance = loader.GetInstance();
                    if (instance != null && !field.Contains(instance))
                    {
                        field.Add(instance);
                        InitializePluginIfNeeded(instance, loader.Manifest, loader.DllPath);
                    }
                }

                _lazyFileSystemPlugins.Clear();
                _logger.Info("Lazy loaded file system plugins, total count: {Count}", field.Count);
            }

            return field;
        }
    } = [];

    /// <summary>
    /// Gets the list of registered context menu plugins.
    /// Triggers lazy loading of context menu plugins if lazy loading is enabled.
    /// </summary>
    public IList<IContextMenuEntry> RegisteredContextMenuPlugins
    {
        get
        {
            if (_useLazyLoading && _lazyContextMenuPlugins.Count > 0)
            {
                _logger.Debug("Lazy loading {Count} context menu plugin(s) on first access", _lazyContextMenuPlugins.Count);

                foreach (var loader in _lazyContextMenuPlugins.ToList())
                {
                    var instance = loader.GetInstance();
                    if (instance != null && !field.Contains(instance))
                    {
                        field.Add(instance);
                        InitializePluginIfNeeded(instance, loader.Manifest, loader.DllPath);
                    }
                }

                _lazyContextMenuPlugins.Clear();
                _logger.Info("Lazy loaded context menu plugins, total count: {Count}", field.Count);
            }

            return field;
        }
    } = [];

    /// <summary>
    /// Gets the list of registered keyword action plugins.
    /// Triggers lazy loading of keyword action plugins if lazy loading is enabled.
    /// </summary>
    public IList<IKeywordAction> RegisteredKeywordActions
    {
        get
        {
            if (_useLazyLoading && _lazyKeywordActions.Count > 0)
            {
                _logger.Debug("Lazy loading {Count} keyword action plugin(s) on first access", _lazyKeywordActions.Count);

                foreach (var loader in _lazyKeywordActions.ToList())
                {
                    var instance = loader.GetInstance();
                    if (instance != null && !field.Contains(instance))
                    {
                        field.Add(instance);

                        // Add to dictionary for lookup
                        if (!_registeredKeywordsDict.ContainsKey(instance.GetName()))
                        {
                            _registeredKeywordsDict.Add(instance.GetName(), instance);
                        }

                        InitializePluginIfNeeded(instance, loader.Manifest, loader.DllPath);
                    }
                }

                _lazyKeywordActions.Clear();
                _logger.Info("Lazy loaded keyword action plugins, total count: {Count}", field.Count);
            }

            return field;
        }
    } = [];

    #endregion

    #region Public methods

    public static int PollingInterval { get; private set; } = 250;

    #endregion

    #region Internals

    /// <summary>
    /// Loads feature flags from configuration.
    /// </summary>
    private void LoadFeatureFlags ()
    {
        // TODO: Load from app.config or appsettings.json in future
        //Type - aware lazy loading supports all plugin types
        _useLazyLoading = true;
        _usePluginCache = true;
        _useLifecycleHooks = true;
        _useEventBus = true;

        _logger.Info("Feature flags - Lazy: {Lazy}, Cache: {Cache}, Lifecycle: {Lifecycle}, EventBus: {EventBus}", _useLazyLoading, _usePluginCache, _useLifecycleHooks, _useEventBus);
    }

    /// <summary>
    /// Creates a plugin context for lifecycle initialization.
    /// </summary>
    private static PluginContext CreatePluginContext (string pluginName, string pluginPath)
    {
        var pluginDir = Path.GetDirectoryName(pluginPath) ?? AppDomain.CurrentDomain.BaseDirectory;
        var configDir = Path.Join(_applicationConfigurationFolder, "Plugins", pluginName);

        // Ensure config directory exists
        _ = Directory.CreateDirectory(Path.GetFullPath(configDir));

        return new PluginContext
        {
            Logger = new PluginLogger(pluginName),
            PluginDirectory = pluginDir,
            HostVersion = typeof(PluginRegistry).Assembly.GetName().Version ?? new Version(1, 0),
            ConfigurationDirectory = configDir
        };
    }

    /// <summary>
    /// Registers lazy-loaded plugins based on their types.
    /// Creates appropriate LazyPluginLoader for each plugin type found in the assembly.
    /// </summary>
    /// <param name="dllName">Path to the plugin DLL</param>
    /// <param name="manifest">Plugin manifest if available</param>
    /// <param name="typeInfo">Information about plugin types in the assembly</param>
    /// <returns>True if at least one lazy loader was registered</returns>
    private bool RegisterLazyPlugins (string dllName, PluginManifest? manifest, PluginTypeInfo typeInfo)
    {
        var registered = false;

        if (typeInfo.HasColumnizer)
        {
            var loader = new LazyPluginLoader<ILogLineMemoryColumnizer>(dllName, manifest);
            _lazyColumnizers.Add(loader);
            _logger.Info("Registered lazy columnizer: {Plugin}", manifest?.Name ?? Path.GetFileName(dllName));
            registered = true;
        }

        if (typeInfo.HasFileSystem)
        {
            var loader = new LazyPluginLoader<IFileSystemPlugin>(dllName, manifest, _fileSystemCallback);
            _lazyFileSystemPlugins.Add(loader);
            _logger.Info("Registered lazy file system plugin: {Plugin}", manifest?.Name ?? Path.GetFileName(dllName));
            registered = true;
        }

        if (typeInfo.HasContextMenu)
        {
            var loader = new LazyPluginLoader<IContextMenuEntry>(dllName, manifest);
            _lazyContextMenuPlugins.Add(loader);
            _logger.Info("Registered lazy context menu plugin: {Plugin}", manifest?.Name ?? Path.GetFileName(dllName));
            registered = true;
        }

        if (typeInfo.HasKeywordAction)
        {
            var loader = new LazyPluginLoader<IKeywordAction>(dllName, manifest);
            _lazyKeywordActions.Add(loader);
            _logger.Info("Registered lazy keyword action plugin: {Plugin}", manifest?.Name ?? Path.GetFileName(dllName));
            registered = true;
        }

        // Publish event for each registered lazy plugin
        if (registered && _useEventBus)
        {
            _eventBus.Publish(new PluginLoadedEvent
            {
                Source = "PluginRegistry",
                PluginName = manifest?.Name ?? Path.GetFileName(dllName),
                PluginVersion = manifest?.Version ?? "Unknown"
            });
        }

        return registered;
    }

    /// <summary>
    /// Initializes a plugin if it supports lifecycle hooks and configuration.
    /// Called after lazy-loading a plugin instance.
    /// </summary>
    /// <param name="plugin">The plugin instance to initialize</param>
    /// <param name="manifest">Plugin manifest if available</param>
    /// <param name="dllPath">Path to the plugin DLL</param>
    private void InitializePluginIfNeeded (object plugin, PluginManifest? manifest, string dllPath)
    {
        // Call lifecycle Initialize if supported
        if (_useLifecycleHooks && plugin is IPluginLifecycle lifecycle)
        {
            try
            {
                var context = CreatePluginContext(manifest?.Name ?? Path.GetFileNameWithoutExtension(dllPath), dllPath);
                lifecycle.Initialize(context);
                _logger.Debug("Initialized lazy-loaded plugin: {Plugin}", manifest?.Name);
            }
            catch (Exception ex) when (ex is ArgumentException or
                                             ArgumentNullException or
                                             PathTooLongException or
                                             NotSupportedException or
                                             SecurityException or
                                             IOException or
                                             UnauthorizedAccessException or
                                             DirectoryNotFoundException)
            {
                _logger.Error(ex, "Failed to initialize lazy-loaded plugin");
            }
        }

        // Call IColumnizerConfigurator.LoadConfig if supported
        if (plugin is IColumnizerConfigurator configurator)
        {
            try
            {
                configurator.LoadConfig(_applicationConfigurationFolder);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load config for lazy-loaded plugin");
            }
        }

        // Call ILogExpertPluginConfigurator.LoadConfig if supported
        if (plugin is ILogExpertPluginConfigurator pluginConfigurator)
        {
            try
            {
                pluginConfigurator.LoadConfig(_applicationConfigurationFolder);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load plugin configurator config");
            }
        }

        // Call ILogExpertPlugin.PluginLoaded if supported
        if (plugin is ILogExpertPlugin legacyPlugin)
        {
            if (!_pluginList.Contains(legacyPlugin))
            {
                _pluginList.Add(legacyPlugin);
            }

            try
            {
                legacyPlugin.PluginLoaded();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to call PluginLoaded on lazy-loaded plugin");
            }
        }
    }

    /// <summary>
    /// Loads a plugin assembly with security measures: timeout protection, exception handling, and type-aware lazy loading.
    /// </summary>
    /// <param name="dllName">Path to the plugin DLL</param>
    /// <param name="manifest">Plugin manifest (if available)</param>
    /// <returns>True if plugin loaded or registered for lazy loading successfully, false otherwise</returns>
    private bool LoadPluginAssemblySafe (string dllName, PluginManifest? manifest)
    {
        try
        {
            // Option 1: Cached Loading (if enabled) - Check cache first
            if (_usePluginCache && _pluginCache != null)
            {
                var result = _pluginCache.LoadPluginWithCache(dllName);

                if (result.Success)
                {
                    // Process ALL plugins from the result, not just the first one
                    if (result.AllPlugins != null && result.AllPlugins.Count > 0)
                    {
                        _logger.Info("Processing {Count} plugin(s) from cache for {FileName}", result.AllPlugins.Count, Path.GetFileName(dllName));

                        foreach (var plugin in result.AllPlugins)
                        {
                            ProcessLoadedPlugin(plugin, manifest, dllName);
                        }

                        return true;
                    }
                    else if (result.Plugin != null)
                    {
                        // Fallback for backward compatibility
                        ProcessLoadedPlugin(result.Plugin, manifest, dllName);
                        return true;
                    }
                }

                _logger.Warn("Cache load failed for {Plugin}, falling back to direct load", Path.GetFileName(dllName));
            }

            // Option 2: Type-Aware Lazy Loading (if enabled)
            if (_useLazyLoading)
            {
                // Inspect assembly to determine which plugin types it contains
                var typeInfo = AssemblyInspector.InspectAssembly(dllName);

                if (typeInfo.IsEmpty)
                {
                    _logger.Debug("No plugins found in {FileName} during inspection", Path.GetFileName(dllName));
                    return false;
                }

                // Strategy: Lazy load if assembly contains only ONE plugin type
                // This avoids complexity of mixed assemblies where one type might
                // be accessed before another, causing initialization issues
                if (typeInfo.IsSingleType)
                {
                    _logger.Debug("Assembly {FileName} contains single plugin type, registering for lazy loading", Path.GetFileName(dllName));
                    return RegisterLazyPlugins(dllName, manifest, typeInfo);
                }

                // If assembly has multiple plugin types, load immediately to ensure
                // all types are available and properly initialized together
                _logger.Debug("Assembly {FileName} contains {Count} plugin types, loading immediately", Path.GetFileName(dllName), typeInfo.TypeCount);
            }

            // Option 3: Direct Loading - For all plugin types when lazy loading disabled
            // or when assembly contains multiple plugin types
            var loadTask = Task.Run(() => LoadPluginAssembly(dllName, manifest));

            if (!loadTask.Wait(TimeSpan.FromSeconds(10)))
            {
                var errorMsg = PluginErrorMessages.PluginLoadTimeout(Path.GetFileName(dllName), 10);
                _logger.Error(errorMsg);
                return false;
            }

            return loadTask.Result;
        }
        catch (AggregateException ex)
        {
            var innerEx = ex.InnerException ?? ex;
            _logger.Error(innerEx, "Exception during plugin load: {FileName}", Path.GetFileName(dllName));
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected exception during plugin load: {FileName}", Path.GetFileName(dllName));
            return false;
        }
    }

    private bool LoadPluginAssembly (string dllName, PluginManifest? manifest)
    {
        // Log plugin loading for audit trail
        _logger.Info("Loading plugin assembly: {FileName}", Path.GetFileName(dllName));

        var assembly = Assembly.LoadFrom(dllName);
        var types = assembly.GetTypes();
        var pluginLoadedCount = 0;

        foreach (var type in types)
        {
            _logger.Debug("Checking type {TypeName} in assembly {AssemblyName}", type.FullName, assembly.FullName);

            // Check for ILogLineColumnizer
            if (type.GetInterfaces().Any(i => i.FullName == typeof(ILogLineMemoryColumnizer).FullName) &&
                TryInstantiatePluginSafe(type, out var instance) &&
                instance is ILogLineMemoryColumnizer columnizer)
            {
                ProcessLoadedPlugin(columnizer, manifest, dllName);
                pluginLoadedCount++;
            }

            // Check for other plugin types (regardless of whether ILogLineColumnizer was found)
            // A single assembly can contain multiple plugin types
            if (TryAsFileSystem(type))
            {
                pluginLoadedCount++;
            }

            if (TryAsContextMenu(type))
            {
                pluginLoadedCount++;
            }

            if (TryAsKeywordAction(type))
            {
                pluginLoadedCount++;
            }
        }

        if (pluginLoadedCount == 0)
        {
            _logger.Warn("No plugins found in assembly: {FileName}", Path.GetFileName(dllName));
        }

        return pluginLoadedCount > 0;
    }

    private static bool TryInstantiatePluginSafe (Type type, out object instance)
    {
        instance = null;

        try
        {
            var cti = type.GetConstructor(Type.EmptyTypes);
            if (cti == null)
            {
                _logger.Warn("Plugin type has no parameterless constructor: {TypeName}", type.Name);
                return false;
            }

            // **SECURITY**: Use timeout for plugin instantiation
            var instantiateTask = Task.Run(() => cti.Invoke([]));

            if (!instantiateTask.Wait(TimeSpan.FromSeconds(5)))
            {
                var errorMsg = PluginErrorMessages.PluginLoadTimeout(type.Name, 5);
                _logger.Error(errorMsg);
                return false;
            }

            instance = instantiateTask.Result;
            return instance != null;
        }
        catch (Exception ex) when (ex is TargetInvocationException or
                                         MethodAccessException or
                                         MemberAccessException or
                                         ArgumentException or
                                         ArgumentNullException or
                                         TargetParameterCountException or
                                         NotSupportedException or
                                         SecurityException)
        {
            var errorMsg = PluginErrorMessages.InstantiationFailed(type.Assembly.GetName().Name, type.FullName);
            _logger.Error(ex, errorMsg);
            return false;
        }
    }

    /// <summary>
    /// Processes a loaded plugin (either from cache or fresh load).
    /// </summary>
    private void ProcessLoadedPlugin (object plugin, PluginManifest? manifest, string dllPath)
    {
        if (plugin is not ILogLineMemoryColumnizer columnizer)
        {
            _logger.Warn("Loaded plugin is not ILogLineColumnizer: {Type}", plugin.GetType().Name);
            return;
        }

        // Skip if a columnizer of this type is already registered. RegisteredColumnizers holds one
        // template instance per type, so a same-type entry is always a duplicate (e.g. a built-in
        // columnizer rediscovered because its defining assembly was scanned).
        if (RegisteredColumnizers.Any(c => c.GetType() == columnizer.GetType()))
        {
            _logger.Debug("Columnizer type {Type} already registered, skipping duplicate", columnizer.GetType().FullName);
            return;
        }

        // Add to registered columnizers
        RegisteredColumnizers.Add(columnizer);

        // Call lifecycle Initialize if supported
        if (_useLifecycleHooks && columnizer is IPluginLifecycle lifecycle)
        {
            try
            {
                var context = CreatePluginContext(manifest?.Name ?? Path.GetFileNameWithoutExtension(dllPath), dllPath);
                lifecycle.Initialize(context);
                _logger.Debug("Called Initialize on {Plugin}", manifest?.Name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Plugin Initialize failed: {Plugin}", manifest?.Name);
            }
        }

        if (columnizer is IColumnizerConfiguratorMemory configurator)
        {
            try
            {
                configurator.LoadConfig(_applicationConfigurationFolder);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Plugin config loading failed: {Plugin}", manifest?.Name);
            }
        }

        // Existing ILogExpertPlugin support
        if (columnizer is ILogExpertPlugin legacyPlugin)
        {
            _pluginList.Add(legacyPlugin);
            try
            {
                legacyPlugin.PluginLoaded();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Plugin PluginLoaded callback failed: {Plugin}", manifest?.Name);
            }
        }

        // Publish loaded event
        if (_useEventBus)
        {
            _eventBus.Publish(new PluginLoadedEvent
            {
                Source = "PluginRegistry",
                PluginName = manifest?.Name ?? Path.GetFileNameWithoutExtension(dllPath),
                PluginVersion = manifest?.Version ?? "Unknown"
            });
        }

        _logger.Info("Plugin processed: {Plugin}", manifest?.Name ?? Path.GetFileNameWithoutExtension(dllPath));
    }

    public void CleanupPlugins ()
    {
        _logger.Info("Cleaning up plugins...");

        // Call legacy AppExiting
        foreach (var plugin in _pluginList)
        {
            try
            {
                plugin.AppExiting();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Plugin AppExiting failed");
            }
        }

        // Call lifecycle Shutdown
        if (_useLifecycleHooks)
        {
            foreach (var lifecycle in RegisteredColumnizers.OfType<IPluginLifecycle>())
            {
                try
                {
                    lifecycle.Shutdown();
                    _logger.Debug("Called Shutdown on plugin");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Plugin Shutdown failed");
                }
            }
        }

        // Cleanup all lazy loaders
        if (_useLazyLoading)
        {
            _lazyColumnizers.Clear();
            _lazyFileSystemPlugins.Clear();
            _lazyContextMenuPlugins.Clear();
            _lazyKeywordActions.Clear();
            _logger.Debug("Cleared all lazy plugin loaders");
        }

        // Cleanup cache
        if (_usePluginCache && _pluginCache != null)
        {
            var stats = _pluginCache.GetStatistics();
            _logger.Info("Cache stats at shutdown - Total: {Total}, Active: {Active}",
                stats.TotalEntries, stats.ActiveEntries);
            _pluginCache.ClearCache();
        }

        // Cleanup event bus
        if (_useEventBus)
        {
            // Event bus cleanup (subscribers will be garbage collected)
            _logger.Debug("Event bus cleanup complete");
        }

        _logger.Info("Plugin cleanup complete");
    }

    public IFileSystemPlugin FindFileSystemForUri (string uriString)
    {
        if (_logger.IsDebugEnabled)
        {
            _logger.Debug(CultureInfo.InvariantCulture, "Trying to find file system plugin for uri {0}", uriString);
        }

        foreach (var fs in RegisteredFileSystemPlugins)
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

    public IKeywordAction FindKeywordActionPluginByName (string name)
    {
        _ = _registeredKeywordsDict.TryGetValue(name, out var action);
        return action;
    }

    #endregion

    #region Private Methods

    //TODO: Can this be deleted?
    private bool TryAsContextMenu (Type type)
    {
        var me = TryInstantiate<IContextMenuEntry>(type);

        if (me != null)
        {
            RegisteredContextMenuPlugins.Add(me);

            // Call lifecycle Initialize if supported
            if (_useLifecycleHooks && me is IPluginLifecycle lifecycle)
            {
                try
                {
                    var context = CreatePluginContext(
                        type.Name,
                        type.Assembly.Location);
                    lifecycle.Initialize(context);
                    _logger.Debug("Initialized context menu plugin: {TypeName}", type.Name);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to initialize context menu plugin: {TypeName}", type.Name);
                }
            }

            // Load configuration if supported
            if (me is ILogExpertPluginConfigurator configurator)
            {
                try
                {
                    configurator.LoadConfig(_applicationConfigurationFolder);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to load config for context menu plugin: {TypeName}", type.Name);
                }
            }

            // Register legacy plugin and call PluginLoaded
            if (me is ILogExpertPlugin plugin)
            {
                if (!_pluginList.Contains(plugin))
                {
                    _pluginList.Add(plugin);
                }

                try
                {
                    plugin.PluginLoaded();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to call PluginLoaded on context menu plugin: {TypeName}", type.Name);
                }
            }

            // Publish event if event bus is enabled
            if (_useEventBus)
            {
                _eventBus.Publish(new PluginLoadedEvent
                {
                    Source = "PluginRegistry",
                    PluginName = type.Name,
                    PluginVersion = type.Assembly.GetName().Version?.ToString() ?? "Unknown"
                });
            }

            _logger.Info(CultureInfo.InvariantCulture, "Added context menu plugin {0}", type);
            return true;
        }

        return false;
    }

    //TODO: Can this be deleted?
    private bool TryAsKeywordAction (Type type)
    {
        var ka = TryInstantiate<IKeywordAction>(type);
        if (ka != null)
        {
            RegisteredKeywordActions.Add(ka);

            // Add to dictionary for quick lookup - with duplicate check
            var keywordName = ka.GetName();
            if (!_registeredKeywordsDict.ContainsKey(keywordName))
            {
                _registeredKeywordsDict.Add(keywordName, ka);
            }
            else
            {
                _logger.Warn("Keyword action with name '{KeywordName}' already registered, skipping dictionary entry for {TypeName}",
                    keywordName, type.Name);
            }

            // Call lifecycle Initialize if supported
            if (_useLifecycleHooks && ka is IPluginLifecycle lifecycle)
            {
                try
                {
                    var context = CreatePluginContext(
                        type.Name,
                        type.Assembly.Location);
                    lifecycle.Initialize(context);
                    _logger.Debug("Initialized keyword action plugin: {TypeName}", type.Name);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to initialize keyword action plugin: {TypeName}", type.Name);
                }
            }

            // Load configuration if supported
            if (ka is ILogExpertPluginConfigurator configurator)
            {
                try
                {
                    configurator.LoadConfig(_applicationConfigurationFolder);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to load config for keyword action plugin: {TypeName}", type.Name);
                }
            }

            // Register legacy plugin and call PluginLoaded
            if (ka is ILogExpertPlugin plugin)
            {
                if (!_pluginList.Contains(plugin))
                {
                    _pluginList.Add(plugin);
                }

                try
                {
                    plugin.PluginLoaded();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to call PluginLoaded on keyword action plugin: {TypeName}", type.Name);
                }
            }

            // Publish event if event bus is enabled
            if (_useEventBus)
            {
                _eventBus.Publish(new PluginLoadedEvent
                {
                    Source = "PluginRegistry",
                    PluginName = type.Name,
                    PluginVersion = type.Assembly.GetName().Version?.ToString() ?? "Unknown"
                });
            }

            _logger.Info(CultureInfo.InvariantCulture, "Added keyword plugin {0}", type);
            return true;
        }

        return false;
    }

    private bool TryAsFileSystem (Type type)
    {
        var fs = TryInstantiate<IFileSystemPlugin>(type, _fileSystemCallback);
        fs ??= TryInstantiate<IFileSystemPlugin>(type);

        if (fs != null)
        {
            RegisteredFileSystemPlugins.Add(fs);
            if (fs is ILogExpertPluginConfigurator configurator)
            {
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
        var t = typeof(T);
        var inter = loadedType.GetInterface(t.Name);
        if (inter != null)
        {
            var cti = loadedType.GetConstructor(Type.EmptyTypes);
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
        var t = typeof(T);
        var inter = loadedType.GetInterface(t.Name);
        if (inter != null)
        {
            var cti = loadedType.GetConstructor([typeof(IFileSystemCallback)]);
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
        var assemblyName = new AssemblyName(args.Name).Name;

        // Type names come from settings and .lxp files; a name like "../x" must not reach outside our directories.
        if (string.IsNullOrEmpty(assemblyName) ||
            assemblyName != Path.GetFileName(assemblyName) ||
            assemblyName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        var fileName = assemblyName + ".dll";

        var mainDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, fileName);
        var pluginDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "plugins", fileName);

        return File.Exists(mainDir)
            ? Assembly.LoadFrom(mainDir)
            : File.Exists(pluginDir)
                ? Assembly.LoadFrom(pluginDir)
                : null;
    }

    #endregion
}