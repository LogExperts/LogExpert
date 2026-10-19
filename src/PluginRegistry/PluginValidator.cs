using System.Reflection;
using System.Security;

using Newtonsoft.Json;

using NLog;

namespace LogExpert.PluginRegistry;

/// <summary>
/// Validates plugin assemblies before loading to prevent security vulnerabilities.
/// </summary>
public static partial class PluginValidator
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private static TrustedPluginConfig _trustedPluginConfig;
    private static readonly Lock _configLock = new();
    private static string _configDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LogExpert");
    private static string _configPath = Path.Join(_configDirectory, "trusted-plugins.json");

    // Whitelist of trusted plugin file names (shipped with LogExpert) - used as defaults
    private static readonly HashSet<string> _trustedPluginNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AutoColumnizer.dll",
        "CsvColumnizer.dll",
        "JsonColumnizer.dll",
        "JsonCompactColumnizer.dll",
        "RegexColumnizer.dll",
        "Log4jXmlColumnizer.dll",
        "GlassfishColumnizer.dll",
        "DefaultPlugins.dll",
        "FlashIconHighlighter.dll",
        "SftpFileSystem.dll",
        "SftpFileSystemx86.dll",
    };

    // Known safe dependencies (not plugins themselves)
    private static readonly HashSet<string> _knownDependencies = new(StringComparer.OrdinalIgnoreCase)
    {
        // LogExpert's own assemblies are not plugins. LogExpert.Core defines the built-in
        // columnizers; if it (or a copy) lands in the plugins folder it must not be scanned,
        // or the built-in columnizers would be registered twice.
        "LogExpert.Core.dll",
        "LogExpert.Resources.dll",
        "ColumnizerLib.dll",
        "Newtonsoft.Json.dll",
        "CsvHelper.dll",
        "Renci.SshNet.dll",
        "Microsoft.Bcl.AsyncInterfaces.dll",
        "Microsoft.Bcl.HashCode.dll",
        "System.Buffers.dll",
        "System.Memory.dll",
        "System.Numerics.Vectors.dll",
        "System.Runtime.CompilerServices.Unsafe.dll",
        "System.Threading.Tasks.Extensions.dll"
    };

    #endregion

    #region Public methods

    /// <summary>
    /// Initializes the PluginValidator with the appropriate configuration directory.
    /// Must be called during application startup after ConfigManager is initialized.
    /// If not called, falls back to %APPDATA%/LogExpert/ for backward compatibility.
    /// </summary>
    /// <param name="configDirectory">The active configuration directory</param>
    public static void Initialize (string configDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);

        lock (_configLock)
        {
            _configDirectory = configDirectory;
            _configPath = Path.Join(_configDirectory, "trusted-plugins.json");
            _logger.Info("PluginValidator initialized with config directory: {Dir}", configDirectory);
            LoadTrustedPluginConfiguration();
        }
    }

    /// <summary>
    /// Ensures the trusted plugin configuration is loaded. The configuration is loaded
    /// lazily on first use (issue #658): an eager load at class-load time would read from
    /// and create the file in the default %APPDATA% directory before Initialize() could
    /// point the validator at the active (e.g. portable) configuration directory.
    /// </summary>
    private static void EnsureConfigLoaded ()
    {
        lock (_configLock)
        {
            if (_trustedPluginConfig == null)
            {
                LoadTrustedPluginConfiguration();
            }
        }
    }

    /// <summary>
    /// Loads trusted plugin configuration from disk.
    /// </summary>
    private static void LoadTrustedPluginConfiguration ()
    {
        lock (_configLock)
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    var json = File.ReadAllText(_configPath);
                    _trustedPluginConfig = JsonConvert.DeserializeObject<TrustedPluginConfig>(json);
                    _logger.Info("Loaded trusted plugin configuration from {ConfigPath}", _configPath);

                    // Validate configuration
                    if (_trustedPluginConfig == null)
                    {
                        _logger.Warn("Deserialized config is null, creating default");
                        _trustedPluginConfig = CreateDefaultConfiguration();
                        SaveTrustedPluginConfiguration();
                    }
                    else if (RefreshBuiltInPlugins(_trustedPluginConfig))
                    {
                        _logger.Info("Refreshed built-in plugin hashes from the running build");
                        SaveTrustedPluginConfiguration();
                    }
                }
                catch (Exception ex) when (ex is IOException or
                                                 UnauthorizedAccessException or
                                                 ArgumentException or
                                                 ArgumentNullException or
                                                 PathTooLongException or
                                                 DirectoryNotFoundException or
                                                 NotSupportedException or
                                                 SecurityException or
                                                 JsonSerializationException)
                {
                    _logger.Error(ex, "Failed to load trusted plugin configuration, using defaults");
                    _trustedPluginConfig = CreateDefaultConfiguration();
                    SaveTrustedPluginConfiguration();
                }
            }
            else
            {
                _logger.Info("No trusted plugin configuration found, creating default");
                _trustedPluginConfig = CreateDefaultConfiguration();
                SaveTrustedPluginConfiguration();
            }
        }
    }

    /// <summary>
    /// Creates default configuration with built-in trusted plugins.
    /// </summary>
    private static TrustedPluginConfig CreateDefaultConfiguration ()
    {
        return new TrustedPluginConfig
        {
            PluginNames = [.. _trustedPluginNames],
            PluginHashes = GetBuiltInPluginHashes(),
            AllowUserTrustedPlugins = true,
            HashAlgorithm = "SHA256",
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Makes the running build's built-in names and hashes authoritative in a loaded configuration,
    /// so an upgrade does not reject the new built-in plugins against hashes persisted by an older build.
    /// </summary>
    /// <returns>True if the configuration was changed</returns>
    private static bool RefreshBuiltInPlugins (TrustedPluginConfig config)
    {
        var changed = false;

        foreach (var name in _trustedPluginNames)
        {
            if (!config.PluginNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                config.PluginNames.Add(name);
                changed = true;
            }
        }

        foreach (var (name, hash) in GetBuiltInPluginHashes())
        {
            if (!config.PluginHashes.TryGetValue(name, out var persistedHash) || !string.Equals(persistedHash, hash, StringComparison.OrdinalIgnoreCase))
            {
                config.PluginHashes[name] = hash;
                changed = true;
            }
        }

        if (changed)
        {
            config.LastUpdated = DateTime.UtcNow;
        }

        return changed;
    }

    /// <summary>
    /// Saves trusted plugin configuration to disk.
    /// </summary>
    private static void SaveTrustedPluginConfiguration ()
    {
        lock (_configLock)
        {
            try
            {
                _ = Directory.CreateDirectory(_configDirectory);
                var json = JsonConvert.SerializeObject(_trustedPluginConfig, Formatting.Indented);
                File.WriteAllText(_configPath, json);
                _logger.Info("Saved trusted plugin configuration to {ConfigPath}", _configPath);
            }
            catch (Exception ex) when (ex is IOException or
                                             UnauthorizedAccessException or
                                             ArgumentException or
                                             ArgumentNullException or
                                             PathTooLongException or
                                             DirectoryNotFoundException or
                                             NotSupportedException or
                                             SecurityException or
                                             JsonSerializationException)
            {
                _logger.Error(ex, "Failed to save trusted plugin configuration");
            }
        }
    }

    /// <summary>
    /// Adds a plugin to the trusted list and saves the configuration.
    /// </summary>
    /// <param name="dllPath">Path to the plugin DLL</param>
    /// <param name="errorMessage">Error message if operation fails</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool AddTrustedPlugin (string dllPath, out string errorMessage)
    {
        errorMessage = null;

        try
        {
            if (!File.Exists(dllPath))
            {
                errorMessage = PluginErrorMessages.PluginFileNotFound(dllPath);
                return false;
            }

            var fileName = Path.GetFileName(dllPath);
            var hash = PluginHashCalculator.CalculateHash(dllPath);

            EnsureConfigLoaded();

            lock (_configLock)
            {
                if (!_trustedPluginConfig.AllowUserTrustedPlugins)
                {
                    errorMessage = PluginErrorMessages.UserPluginsNotAllowed();
                    return false;
                }

                if (!_trustedPluginConfig.PluginNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                {
                    _trustedPluginConfig.PluginNames.Add(fileName);
                }

                _trustedPluginConfig.PluginHashes[fileName] = hash;
                _trustedPluginConfig.LastUpdated = DateTime.UtcNow;

                SaveTrustedPluginConfiguration();
            }

            _logger.Info("Added trusted plugin: {FileName}, Hash: {Hash}", fileName, hash);
            return true;
        }
        catch (Exception ex) when (ex is IOException or
                                         UnauthorizedAccessException or
                                         ArgumentException or
                                         ArgumentNullException or
                                         PathTooLongException or
                                         DirectoryNotFoundException or
                                         NotSupportedException or
                                         SecurityException or
                                         JsonSerializationException)
        {
            errorMessage = PluginErrorMessages.GenericError("adding trusted plugin", Path.GetFileName(dllPath), ex);
            _logger.Error(ex, "Error adding trusted plugin: {DllPath}", dllPath);
            return false;
        }
    }

    /// <summary>
    /// Removes a plugin from the trusted list.
    /// </summary>
    /// <param name="fileName">Plugin file name</param>
    /// <returns>True if removed, false if not found</returns>
    public static bool RemoveTrustedPlugin (string fileName)
    {
        EnsureConfigLoaded();

        lock (_configLock)
        {
            var removed = _trustedPluginConfig.PluginNames.Remove(fileName);
            if (removed)
            {
                _ = _trustedPluginConfig.PluginHashes.Remove(fileName);
                _trustedPluginConfig.LastUpdated = DateTime.UtcNow;
                SaveTrustedPluginConfiguration();
                _logger.Info("Removed trusted plugin: {FileName}", fileName);
            }

            return removed;
        }
    }

    /// <summary>
    /// Validates a plugin assembly before loading.
    /// </summary>
    /// <param name="dllPath">Path to the plugin DLL</param>
    /// <returns>True if the plugin is valid and safe to load</returns>
    public static bool ValidatePlugin (string dllPath)
    {
        return ValidatePlugin(dllPath, out _, out _);
    }

    /// <summary>
    /// Validates a plugin assembly before loading with manifest information.
    /// </summary>
    /// <param name="dllPath">Path to the plugin DLL</param>
    /// <param name="manifest">Output manifest if found and valid, null otherwise</param>
    /// <returns>True if the plugin is valid and safe to load</returns>
    public static bool ValidatePlugin (string dllPath, out PluginManifest manifest)
    {
        return ValidatePlugin(dllPath, out manifest, out _);
    }

    /// <summary>
    /// Validates a plugin assembly before loading with manifest information.
    /// </summary>
    /// <param name="dllPath">Path to the plugin DLL</param>
    /// <param name="manifest">Output manifest if found and valid, null otherwise</param>
    /// <param name="errorMessage">User-friendly error message if validation fails</param>
    /// <returns>True if the plugin is valid and safe to load</returns>
    public static bool ValidatePlugin (string dllPath, out PluginManifest manifest, out string errorMessage)
    {
        manifest = null;
        errorMessage = null;

        try
        {
            EnsureConfigLoaded();

            // 1. Check if file exists
            if (!File.Exists(dllPath))
            {
                errorMessage = PluginErrorMessages.PluginFileNotFound(dllPath);
                _logger.Warn("Plugin file does not exist: {DllPath}", dllPath);
                return false;
            }

            var fileName = Path.GetFileName(dllPath);

            // 2. Check if it's a known dependency (not a plugin)
            if (_knownDependencies.Contains(fileName))
            {
                _logger.Debug("Skipping dependency DLL: {FileName}", fileName);
                return false; // Not a plugin, skip it
            }

            // 3. Calculate file hash using PluginHashCalculator
            string fileHash;
            try
            {
                fileHash = PluginHashCalculator.CalculateHash(dllPath);
                _logger.Debug("Plugin {FileName} hash: {Hash}", fileName, fileHash);
            }
            catch (Exception ex) when (ex is IOException or
                                             FileNotFoundException or
                                             ArgumentNullException)
            {
                errorMessage = PluginErrorMessages.GenericError("hash calculation", fileName, ex);
                _logger.Error(ex, "Failed to calculate hash for plugin: {FileName}", fileName);
                return false;
            }

            // 4. Check trust status
            // In Debug mode we trust the plugins (so testing and developing is possible)
#if DEBUG
            var isTrustedByName = true;
            var isTrustedByHash = true;
#else
            var isTrustedByName = _trustedPluginConfig.PluginNames.Contains(fileName, StringComparer.OrdinalIgnoreCase);
            var isTrustedByHash = _trustedPluginConfig.PluginHashes.ContainsValue(fileHash);
#endif

            if (!isTrustedByName && !isTrustedByHash)
            {
                errorMessage = PluginErrorMessages.PluginNotTrusted(fileName, fileHash);
                _logger.Warn("Plugin not trusted: {FileName}, Hash: {Hash}", fileName, fileHash);
                return false;
            }

            // 5. Verify hash for known plugins using PluginHashCalculator
            if (isTrustedByName && _trustedPluginConfig.PluginHashes.TryGetValue(fileName, out var expectedHash))
            {
                if (!PluginHashCalculator.VerifyHash(dllPath, expectedHash))
                {
                    errorMessage = PluginErrorMessages.PluginHashMismatch(fileName, expectedHash, fileHash);
                    _logger.Error("SECURITY: Plugin hash mismatch for {FileName}", fileName);
                    _logger.Error("  Expected: {Expected}", expectedHash);
                    _logger.Error("  Actual:   {Actual}", fileHash);
                    _logger.Error("  This could indicate file tampering or corruption!");
                    return false;
                }

                _logger.Debug("Plugin hash verified: {FileName}", fileName);
            }
            else if (isTrustedByHash)
            {
                _logger.Info("Plugin {FileName} trusted by hash: {Hash}", fileName, fileHash);
            }

            // 6. Try to load and validate manifest
            manifest = LoadAndValidateManifest(dllPath, out var manifestErrors);
            if (manifest != null)
            {
                _logger.Info("Loaded manifest for plugin: {PluginName} v{Version}", manifest.Name, manifest.Version);

                // 6a. Check version compatibility
                if (!CheckVersionCompatibility(manifest, out var versionError))
                {
                    errorMessage = versionError;
                    _logger.Error("Plugin {PluginName} is not compatible with current LogExpert version", manifest.Name);
                    return false;
                }

                // 6b. Validate manifest paths for security
                if (!ValidateManifestPaths(manifest, Path.GetDirectoryName(dllPath), out var pathError))
                {
                    errorMessage = pathError;
                    _logger.Error("Manifest path validation failed for {Plugin}", manifest.Name);
                    return false;
                }

                // 6c. Extract and set permissions from manifest
                if (manifest.Permissions != null && manifest.Permissions.Count > 0)
                {
                    var permissions = PluginPermissionManager.ParsePermissions(manifest.Permissions);
                    var pluginName = Path.GetFileNameWithoutExtension(fileName);
                    PluginPermissionManager.SetPermissions(pluginName, permissions);
                    _logger.Info("Set permissions for {PluginName}: {Permissions}", pluginName, PluginPermissionManager.PermissionToString(permissions));
                }
            }
            else if (manifestErrors != null && manifestErrors.Count > 0)
            {
                errorMessage = PluginErrorMessages.InvalidManifest(fileName, manifestErrors);
                _logger.Error("Invalid manifest for {FileName}", fileName);
                return false;
            }
            else
            {
                _logger.Debug("No manifest found for {FileName}, using default permissions", fileName);
            }

            // 7. Verify assembly can be loaded (basic validation)
            if (!CanLoadAssembly(dllPath, out var loadError))
            {
                errorMessage = loadError;
                _logger.Error("Plugin assembly cannot be loaded: {FileName}", fileName);
                return false;
            }

            // 8. Verify assembly is a valid .NET assembly
            if (!IsValidDotNetAssembly(dllPath))
            {
                errorMessage = PluginErrorMessages.AssemblyLoadFailed(fileName, "Not a valid .NET assembly");
                _logger.Error("Plugin is not a valid .NET assembly: {FileName}", fileName);
                return false;
            }

            _logger.Info("Plugin validated successfully: {FileName}", fileName);
            return true;
        }
        catch (Exception ex) when (ex is IOException or
                                         UnauthorizedAccessException or
                                         ArgumentException or
                                         BadImageFormatException)
        {
            errorMessage = PluginErrorMessages.GenericError("validation", Path.GetFileName(dllPath), ex);
            _logger.Error(ex, "Error validating plugin: {DllPath}", dllPath);
            return false;
        }
    }

    /// <summary>
    /// Checks if a plugin is in the trusted whitelist.
    /// </summary>
    public static bool IsTrustedPlugin (string fileName)
    {
        var pluginName = Path.GetFileName(fileName);
        return _trustedPluginNames.Contains(pluginName);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates that manifest paths don't escape the plugin directory (path traversal protection).
    /// </summary>
    /// <param name="manifest">Plugin manifest</param>
    /// <param name="pluginDirectory">Plugin directory path</param>
    /// <param name="errorMessage">User-friendly error message if validation fails</param>
    /// <returns>True if paths are safe, false if path traversal detected</returns>
    private static bool ValidateManifestPaths (PluginManifest manifest, string pluginDirectory, out string errorMessage)
    {
        errorMessage = null;

        try
        {
            var pluginDir = Path.GetFullPath(pluginDirectory);

            // Validate main file path
            var mainPath = Path.GetFullPath(Path.Join(pluginDirectory, manifest.Main));

            if (!mainPath.StartsWith(pluginDir, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = PluginErrorMessages.PathTraversalDetected(manifest.Name, manifest.Main);
                _logger.Error("SECURITY: Plugin main file outside plugin directory");
                _logger.Error("  Plugin: {Plugin}", manifest.Name);
                _logger.Error("  Main path: {MainPath}", mainPath);
                _logger.Error("  Expected directory: {PluginDir}", pluginDir);
                return false;
            }

            // Validate dependency paths if they contain file references
            if (manifest.Dependencies != null)
            {
                foreach (var (key, value) in manifest.Dependencies)
                {
                    // Check for suspicious path patterns
                    if (key.Contains("..", StringComparison.OrdinalIgnoreCase) ||
                        key.Contains('~', StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("..", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains('~', StringComparison.OrdinalIgnoreCase))
                    {
                        errorMessage = PluginErrorMessages.PathTraversalDetected(manifest.Name, $"{key} = {value}");
                        _logger.Warn("Suspicious path in manifest dependencies: {Key} = {Value}", key, value);
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or
                                         SecurityException or
                                         ArgumentNullException or
                                         PathTooLongException or
                                         IOException or
                                         UnauthorizedAccessException or
                                         NotSupportedException)
        {
            errorMessage = PluginErrorMessages.GenericError("manifest path validation", manifest.Name, ex);
            _logger.Error(ex, "Error validating manifest paths for {Plugin}", manifest.Name);
            return false;
        }
    }

    /// <summary>
    /// Loads and validates a plugin manifest file.
    /// </summary>
    /// <param name="dllPath">Path to the plugin DLL</param>
    /// <param name="validationErrors">List of validation errors if manifest is invalid</param>
    /// <returns>Validated manifest or null if not found/invalid</returns>
    private static PluginManifest LoadAndValidateManifest (string dllPath, out List<string> validationErrors)
    {
        validationErrors = null;

        try
        {
            // Look for manifest file: PluginName.manifest.json
            var manifestPath = Path.ChangeExtension(dllPath, ".manifest.json");

            if (!File.Exists(manifestPath))
            {
                _logger.Debug("No manifest file found at: {ManifestPath}", manifestPath);
                return null;
            }

            // Load manifest
            var manifest = PluginManifest.Load(manifestPath);
            if (manifest == null)
            {
                validationErrors = ["Failed to deserialize manifest file"];
                _logger.Error("Failed to load manifest from: {ManifestPath}", manifestPath);
                return null;
            }

            // Validate manifest
            if (!manifest.Validate(out var errors))
            {
                validationErrors = errors;
                _logger.Error("Manifest validation failed for {ManifestPath}:", manifestPath);
                foreach (var error in errors)
                {
                    _logger.Error("  - {Error}", error);
                }

                return null;
            }

            return manifest;
        }
        catch (Exception ex) when (ex is IOException or
                                         UnauthorizedAccessException or
                                         ArgumentException)
        {
            validationErrors = [$"Error loading manifest: {ex.Message}"];
            _logger.Error(ex, "Error loading manifest for: {DllPath}", dllPath);
            return null;
        }
    }

    /// <summary>
    /// Checks if the plugin is compatible with the current LogExpert version.
    /// </summary>
    /// <param name="manifest">Plugin manifest</param>
    /// <param name="errorMessage">User-friendly error message if incompatible</param>
    /// <returns>True if compatible, false otherwise</returns>
    private static bool CheckVersionCompatibility (PluginManifest manifest, out string errorMessage)
    {
        errorMessage = null;

        try
        {
            // Get current LogExpert version
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;

            if (version == null)
            {
                _logger.Warn("Could not determine LogExpert version, assuming compatible");
                return true;
            }

            // Check compatibility
            if (!manifest.IsCompatibleWith(version))
            {
                errorMessage = PluginErrorMessages.VersionIncompatible(
                    manifest.Name,
                    manifest.Version ?? "Unknown",
                    manifest.Requires?.LogExpert ?? "Unknown",
                    version.ToString());

                _logger.Error("Plugin {PluginName} requires LogExpert {Requirement}, but current version is {CurrentVersion}", manifest.Name, manifest.Requires?.LogExpert ?? "unknown", version);
                return false;
            }

            _logger.Debug("Plugin {PluginName} is compatible with LogExpert {Version}", manifest.Name, version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking version compatibility for plugin: {PluginName}", manifest.Name);
            // On error, assume compatible (don't block plugin loading)
            return true;
        }
    }

    /// <summary>
    /// Checks if an assembly can be loaded without throwing exceptions.
    /// </summary>
    private static bool CanLoadAssembly (string dllPath, out string errorMessage)
    {
        errorMessage = null;
        var fileName = Path.GetFileName(dllPath);

        try
        {
            // Try to get assembly name without loading it fully
            _ = AssemblyName.GetAssemblyName(dllPath);
            return true;
        }
        catch (BadImageFormatException ex)
        {
            errorMessage = PluginErrorMessages.BadImageFormat(fileName, Environment.Is64BitProcess);
            _logger.Debug(ex, "Plugin has invalid format (possibly wrong architecture): {DllPath}", dllPath);
            return false;
        }
        catch (Exception ex) when (ex is FileNotFoundException or
                                         FileLoadException or
                                         UnauthorizedAccessException or
                                         ArgumentException or
                                         IOException or
                                         SecurityException)
        {
            errorMessage = PluginErrorMessages.AssemblyLoadFailed(fileName, ex.Message);
            _logger.Debug(ex, "Cannot load plugin assembly: {DllPath}", dllPath);
            return false;
        }
    }

    /// <summary>
    /// Validates that the file is a valid .NET assembly.
    /// </summary>
    private static bool IsValidDotNetAssembly (string dllPath)
    {
        try
        {
            using var stream = File.OpenRead(dllPath);
            using var reader = new BinaryReader(stream);

            // Check PE header
            if (stream.Length < 64)
            {
                return false;
            }

            // Read DOS header
            var dosHeader = reader.ReadUInt16();
            if (dosHeader != 0x5A4D) // "MZ"
            {
                return false;
            }

            // Jump to PE header offset
            _ = stream.Seek(60, SeekOrigin.Begin);
            var peHeaderOffset = reader.ReadInt32();

            if (peHeaderOffset >= stream.Length - 4)
            {
                return false;
            }

            // Read PE signature
            _ = stream.Seek(peHeaderOffset, SeekOrigin.Begin);
            var peSignature = reader.ReadUInt32();
            if (peSignature != 0x00004550) // "PE\0\0"
            {
                return false;
            }

            // Basic validation passed
            return true;
        }
        catch (Exception ex) when (ex is IOException or
                                         UnauthorizedAccessException or
                                         ArgumentException or
                                         ObjectDisposedException)
        {
            _logger.Debug(ex, "Error checking PE format: {DllPath}", dllPath);
            return false;
        }
    }

    #endregion
}
