using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security;

using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.JsonConverters;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArguments;
using LogExpert.Core.Interfaces;

using Newtonsoft.Json;

using NLog;

namespace LogExpert.Configuration;

[SupportedOSPlatform("windows")]
public class ConfigManager : IConfigManager
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private static readonly Lock _monitor = new();
    private readonly Lock _loadSaveLock = new();
    private Settings _settings;

    private string _applicationStartupPath;
    private Rectangle _virtualScreenBounds;
    private bool _isInitialized;

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        Converters =
            {
                new ColumnizerJsonConverter(),
                new EncodingJsonConverter()
            },
        Formatting = Formatting.Indented,
        //This is needed for the BookmarkList and the Bookmark Overlay
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
    };

    private const string SETTINGS_FILE_NAME = "settings.json";
    private const int MAX_FILE_HISTORY = 10;

    #endregion

    #region cTor

    private ConfigManager ()
    {
        // Empty constructor for singleton creation
    }

    #endregion

    #region Events

    public event EventHandler<ConfigChangedEventArgs> ConfigChanged;

    #endregion

    #region Properties

    public static ConfigManager Instance
    {
        get
        {
            lock (_monitor)
            {
                field ??= new ConfigManager();
            }

            return field;
        }
    }

    public Settings Settings
    {
        get
        {
            _settings ??= Load();
            return _settings;
        }
    }

    /// <summary>
    /// {ApplicationStartupPath}/configuration/<br></br>
    /// Used as the unified configuration directory in portable mode.
    /// </summary>
    public string PortableConfigDir => Path.Join(_applicationStartupPath, "configuration");

    /// <summary>
    /// {ApplicationStartupPath}/configuration/sessions/<br></br>
    /// Used for session file storage in portable mode.
    /// </summary>
    public string PortableSessionDir => Path.Join(PortableConfigDir, "sessions");

    /// <summary>%APPDATA%/LogExpert/; tests redirect it so they never touch the user's real configuration.</summary>
    public string ConfigDir { get; internal set; } = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LogExpert");

    /// <summary>
    /// Application.StartUpPath + portable
    /// </summary>
    [Obsolete("Use PortableConfigDir instead. Kept only for old-layout migration detection.")]
    public string PortableModeDir => Path.Join(_applicationStartupPath, "portable");

    /// <summary>
    /// portableMode.json
    /// </summary>
    public string PortableModeSettingsFileName => "portableMode.json";

    /// <summary>
    /// Gets the directory path where the current session's data is stored.
    /// </summary>
    /// <remarks>This property is useful for accessing files or configurations that are specific to the active
    /// session. The returned path may vary between sessions and should not be assumed to be persistent across
    /// application restarts.</remarks>
    public string ActiveSessionDir => Settings.Preferences.PortableMode ? PortableSessionDir : Path.Join(_applicationStartupPath, "sessionFiles");

    /// <summary>
    /// Returns the effective configuration directory.
    /// Portable mode: PortableConfigDir ({AppDir}/configuration/)
    /// Normal mode: ConfigDir (%APPDATA%/LogExpert/)
    /// </summary>
    public string ActiveConfigDir => Settings.Preferences.PortableMode ? PortableConfigDir : ConfigDir;

    #endregion

    #region Public methods

    /// <summary>
    /// Initializes the ConfigManager with application-specific paths and screen information.
    /// This method must be called once before accessing Settings or other configuration.
    /// </summary>
    /// <param name="applicationStartupPath">The application startup path (e.g., Application.StartupPath)</param>
    /// <param name="virtualScreenBounds">The virtual screen bounds (e.g., SystemInformation.VirtualScreen)</param>
    [SupportedOSPlatform("windows")]
    public void Initialize (string applicationStartupPath, Rectangle virtualScreenBounds)
    {
        lock (_monitor)
        {
            if (_isInitialized)
            {
                _logger.Warn("ConfigManager already initialized. Ignoring subsequent initialization attempt.");
                return;
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(applicationStartupPath, nameof(applicationStartupPath));

            _applicationStartupPath = applicationStartupPath;
            _virtualScreenBounds = virtualScreenBounds;
            _isInitialized = true;

            _logger.Info($"ConfigManager initialized with startup path: {applicationStartupPath}");
        }
    }

    /// <summary>
    /// Saves the current settings with the specified flags.
    /// </summary>
    /// <remarks>The method saves the settings based on the provided <paramref name="flags"/>. Ensure that the
    /// flags are correctly set to avoid saving unintended settings.</remarks>
    /// <param name="flags">The flags that determine which settings to save. This parameter cannot be null.</param>
    [SupportedOSPlatform("windows")]
    public void Save (SettingsFlags flags)
    {
        Instance.Save(Settings, flags);
    }

    /// <summary>
    /// Exports the current instance data to the specified file.
    /// </summary>
    /// <remarks>The method saves the current instance data using the provided settings. Ensure that the file
    /// path specified in <paramref name="fileInfo"/> is accessible and writable.</remarks>
    /// <param name="fileInfo">The <see cref="FileInfo"/> object representing the file to which the data will be exported. Cannot be null.</param>
    [SupportedOSPlatform("windows")]
    public void Export (FileInfo fileInfo)
    {
        Save(fileInfo, Settings);
    }

    /// <summary>
    /// Exports only the highlight settings to the specified file.
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <param name="highlightSettings"></param>
    [SupportedOSPlatform("windows")]
    public void Export (FileInfo fileInfo, SettingsFlags highlightSettings)
    {
        Instance.Save(fileInfo, Settings, highlightSettings);
    }

    /// <summary>
    /// Import settings from a file.
    /// Returns ImportResult indicating success, error, or user confirmation requirement.
    /// </summary>
    /// <param name="fileInfo">The file to import from</param>
    /// <param name="importFlags">Flags controlling what to import</param>
    /// <returns>ImportResult with operation outcome</returns>
    [SupportedOSPlatform("windows")]
    public ImportResult Import (FileInfo fileInfo, ExportImportFlags importFlags)
    {
        _logger.Info($"Importing settings from: {fileInfo?.FullName ?? "null"}");

        // Validate import file exists
        if (fileInfo == null || !fileInfo.Exists)
        {
            _logger.Error($"Import file does not exist: {fileInfo?.FullName ?? "null"}");
            return ImportResult.Failed("Import Failed", $"Import file not found:\n{fileInfo?.FullName ?? "unknown"}");
        }

        // Try to load and validate the import file before applying
        Settings importedSettings;
        try
        {
            _logger.Info("Validating import file...");
            LoadResult loadResult = LoadOrCreateNew(fileInfo);

            // Handle any critical errors from loading
            if (loadResult.CriticalFailure)
            {
                return ImportResult.Failed("Import Failed", $"Import file is invalid or corrupted:\n\n{loadResult.CriticalMessage}\n\nImport canceled.");
            }

            importedSettings = loadResult.Settings;
        }
        catch (Exception ex) when (ex is InvalidDataException or
                                         JsonSerializationException)
        {
            _logger.Error($"Import file is invalid or corrupted: {ex}");
            return ImportResult.Failed("Import Failed", $"Import file is invalid or corrupted:\n\n{ex.Message}\n\nImport canceled.");
        }

        if (SettingsAreEmptyOrDefault(importedSettings, importFlags))
        {
            _logger.Warn("Import file appears to contain empty or default settings");

            string confirmationMessage =
                "Warning: Import file appears to be empty or contains default settings.\n\n" +
                "This will overwrite your current configuration with empty settings.\n\n" +
                $"Import file: {fileInfo.Name}\n" +
                $"Filters: {importedSettings.FilterList?.Count ?? 0}\n" +
                $"History: {importedSettings.FileHistoryList?.Count ?? 0}\n" +
                $"Highlights: {importedSettings.Preferences?.HighlightGroupList?.Count ?? 0}\n\n" +
                "Continue with import?";

            return ImportResult.RequiresConfirmation("Confirm Import", confirmationMessage);
        }

        _logger.Info($"Importing: Filters={importedSettings.FilterList?.Count ?? 0}, " +
            $"History={importedSettings.FileHistoryList?.Count ?? 0}, " +
            $"Highlights={importedSettings.Preferences?.HighlightGroupList?.Count ?? 0}");

        // Proceed with import - Use Settings property to ensure _settings is initialized
        _settings = Instance.Import(Instance.Settings, fileInfo, importFlags);
        // Re-apply defaults and materialize runtime-only fields (e.g. Preferences.Font from FontString)
        // since the import path bypasses Load/InitializeSettings.
        _settings = InitializeSettings(_settings);
        Save(SettingsFlags.All);

        _logger.Info("Import completed successfully");
        return ImportResult.Successful();
    }

    /// <summary>
    /// Imports the highlight settings from a file.
    /// Throws ArgumentNullException if fileInfo is null, this should not happen.
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <param name="importFlags"></param>
    /// <exception cref="ArgumentNullException"></exception>
    [SupportedOSPlatform("windows")]
    public void ImportHighlightSettings (FileInfo fileInfo, ExportImportFlags importFlags)
    {
        ArgumentNullException.ThrowIfNull(fileInfo, nameof(fileInfo));

        Instance.Settings.Preferences.HighlightGroupList = Import(Instance.Settings.Preferences.HighlightGroupList, fileInfo, importFlags);
        Save(SettingsFlags.All);
    }

    /// <summary>
    /// Adds the specified file name to the file history list, moving it to the top if it already exists.
    /// </summary>
    /// <remarks>If the file name already exists in the history, it is moved to the top of the list. The file
    /// history list is limited to a maximum number of entries; the oldest entries are removed if the limit is exceeded.
    /// This method is supported only on Windows platforms.</remarks>
    /// <param name="fileName">The name of the file to add to the file history list. Comparison is case-insensitive.</param>
    [SupportedOSPlatform("windows")]
    public void AddToFileHistory (string fileName)
    {
        bool findName (string s) => s.ToUpperInvariant().Equals(fileName.ToUpperInvariant(), StringComparison.Ordinal);

        var index = Instance.Settings.FileHistoryList.FindIndex(findName);

        if (index != -1)
        {
            Instance.Settings.FileHistoryList.RemoveAt(index);
        }

        Instance.Settings.FileHistoryList.Insert(0, fileName);

        while (Instance.Settings.FileHistoryList.Count > MAX_FILE_HISTORY)
        {
            Instance.Settings.FileHistoryList.RemoveAt(Instance.Settings.FileHistoryList.Count - 1);
        }

        Save(SettingsFlags.FileHistory);
    }

    public void RemoveFromFileHistory (string fileName)
    {
        bool findName (string s) => s.ToUpperInvariant().Equals(fileName.ToUpperInvariant(), StringComparison.Ordinal);

        var index = Instance.Settings.FileHistoryList.FindIndex(findName);

        if (index != -1)
        {
            Instance.Settings.FileHistoryList.RemoveAt(index);
        }

        Save(SettingsFlags.FileHistory);
    }

    public void ClearLastOpenFilesList ()
    {
        lock (_loadSaveLock)
        {
            Instance.Settings.LastOpenFilesList.Clear();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Ensures the ConfigManager has been initialized before use.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if not initialized</exception>
    private void EnsureInitialized ()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(Resources.ConfigManager_Error_Messages_InvalidOperation_EnsureInitialized);
        }
    }

    /// <summary>
    /// Loads the Settings from file or creates new settings if the file does not exist.
    /// </summary>
    /// <returns></returns>
    private Settings Load ()
    {
        EnsureInitialized();

        string dir;

        // 1. Check new portable layout first
        if (File.Exists(Path.Join(PortableConfigDir, PortableModeSettingsFileName)))
        {
            _logger.Info("Load: New portable layout detected — loading from {Dir}", PortableConfigDir);
            dir = PortableConfigDir;
        }
        // 2. Check old portable layout (migration candidate)
#pragma warning disable CS0618 // Obsolete PortableModeDir — needed for migration
        else if (File.Exists(Path.Join(PortableModeDir, PortableModeSettingsFileName)))
        {
            _logger.Info("Load: Old portable layout detected — triggering migration");
            MigrateOldPortableLayout();
            dir = PortableConfigDir;
        }
#pragma warning restore CS0618
        // 3. Normal mode
        else
        {
            _logger.Info("Load: Standard mode — loading from {Dir}", ConfigDir);
            dir = ConfigDir;
        }

        if (!Directory.Exists(dir))
        {
            _ = Directory.CreateDirectory(dir);
        }

        LoadResult result;

        if (!File.Exists(Path.Join(dir, SETTINGS_FILE_NAME)))
        {
            result = LoadOrCreateNew(null);
        }
        else
        {
            try
            {
                FileInfo fileInfo = new(Path.Join(dir, SETTINGS_FILE_NAME));
                result = LoadOrCreateNew(fileInfo);
            }
            catch (IOException ex)
            {
                _logger.Error($"File system error: {ex.Message}");
                result = LoadOrCreateNew(null);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Error($"Access denied: {ex}");
                result = LoadOrCreateNew(null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.Error($"Access denied: {ex}");
                result = LoadOrCreateNew(null);
            }
        }

        // Handle recovery notifications (if loaded from backup)
        if (result.LoadedFromBackup)
        {
            _logger.Info($"### {nameof(Load)}: Settings recovered from backup");
        }

        // Handle critical failures
        if (result.CriticalFailure)
        {
            _logger.Error($"### {nameof(Load)}: settings load failure. Set to default settings");
            result = LoadOrCreateNew(null);
        }

        return result.Settings;
    }

    /// <summary>
    /// Loads Settings of a given file or creates new settings if the file does not exist.
    /// Includes automatic backup recovery if main file is corrupted.
    /// Returns LoadResult with the settings and any recovery information.
    /// </summary>
    /// <param name="fileInfo">file that has settings saved</param>
    /// <returns>LoadResult containing loaded/created settings and status</returns>
    /// <exception cref="InvalidDataException"></exception>
    /// <exception cref="JsonSerializationException"></exception>
    private LoadResult LoadOrCreateNew (FileInfo fileInfo)
    {
        //TODO this needs to be refactord, its quite big
        lock (_loadSaveLock)
        {
            Settings settings = null;
            Exception loadException = null;

            if (fileInfo == null || !fileInfo.Exists)
            {
                _logger.Info("No settings file found, creating new default settings");
                settings = new Settings();
            }
            else
            {
                // Try loading main settings file
                try
                {
                    _logger.Info($"Loading settings from: {fileInfo.FullName}");
                    string json = File.ReadAllText(fileInfo.FullName);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        throw new InvalidDataException(Resources.ConfigManager_Error_Messages_InvalidData_SettingsFileIsEmpty);
                    }

                    settings = JsonConvert.DeserializeObject<Settings>(json, _jsonSettings) ?? throw new JsonSerializationException(Resources.ConfigManager_Error_Messages_JSONSerialization_DeserializationReturnedNull);

                    _logger.Info("Settings loaded successfully");
                }
                catch (Exception e) when (e is ArgumentException or
                                               ArgumentNullException or
                                               DirectoryNotFoundException or
                                               FileNotFoundException or
                                               IOException or
                                               InvalidDataException or
                                               NotSupportedException or
                                               PathTooLongException or
                                               UnauthorizedAccessException or
                                               SecurityException or
                                               JsonException or
                                               JsonSerializationException or
                                               JsonReaderException)
                {
                    _logger.Error($"Error deserializing settings.json: {e}");
                    loadException = e;

                    // Try loading from backup file
                    string backupFile = fileInfo.FullName + ".bak";
                    if (File.Exists(backupFile))
                    {
                        try
                        {
                            _logger.Warn($"Attempting to load from backup file: {backupFile}");
                            string backupJson = File.ReadAllText(backupFile);

                            if (!string.IsNullOrWhiteSpace(backupJson))
                            {
                                settings = JsonConvert.DeserializeObject<Settings>(backupJson, _jsonSettings);

                                if (settings != null)
                                {
                                    _logger.Info("Settings recovered from backup successfully");

                                    // Save corrupted file for analysis
                                    string corruptFile = fileInfo.FullName + ".corrupt";
                                    try
                                    {
                                        File.Copy(fileInfo.FullName, corruptFile, overwrite: true);
                                        _logger.Info($"Corrupted file saved to: {corruptFile}");
                                    }
                                    catch (Exception copyException) when (copyException is ArgumentException or
                                                                         ArgumentNullException or
                                                                         DirectoryNotFoundException or
                                                                         FileNotFoundException or
                                                                         IOException or
                                                                         NotSupportedException or
                                                                         PathTooLongException or
                                                                         UnauthorizedAccessException)

                                    {
                                        _logger.Warn($"Could not save corrupted file: {copyException.Message}");
                                    }

                                    // Return recovery result instead of showing MessageBox
                                    settings = InitializeSettings(settings);
                                    return LoadResult.FromBackup(
                                        settings,
                                        "Settings file was corrupted but recovered from backup.\n\n" +
                                        $"Original error: {e.Message}\n\n" +
                                        $"A copy of the corrupted file has been saved as:\n{corruptFile}",
                                        "Settings Recovered from Backup");
                                }
                            }
                        }
                        catch (Exception backupException) when (backupException is ArgumentException or
                                                                                   ArgumentNullException or
                                                                                   DirectoryNotFoundException or
                                                                                   FileNotFoundException or
                                                                                   IOException or
                                                                                   NotSupportedException or
                                                                                   PathTooLongException or
                                                                                   UnauthorizedAccessException or
                                                                                   SecurityException)
                        {
                            _logger.Error($"Backup file also corrupted: {backupException}");
                        }
                    }
                    else
                    {
                        _logger.Error("No backup file available for recovery");
                    }
                }
            }

            // If all loading attempts failed, return critical failure result
            if (settings == null)
            {
                if (loadException != null)
                {
                    _logger.Error("All attempts to load settings failed");

                    // Create new settings for critical failure case
                    settings = new Settings();
                    settings = InitializeSettings(settings);

                    return LoadResult.Critical(
                        settings,
                        "Critical: Settings Load Failed",
                        "Failed to load settings file. All configuration will be lost if you continue.\n\n" +
                        $"Error: {loadException.Message}\n\n" +
                        "Do you want to:\n" +
                        "YES - Create new settings (loses all configuration)\n" +
                        "NO - Exit application (allows manual recovery)\n\n" +
                        "Your corrupted settings file will be preserved for manual recovery.");
                }

                settings = new Settings();
            }

            settings = InitializeSettings(settings);
            return LoadResult.Success(settings);
        }
    }

    /// <summary>
    /// Initialize settings with required default values
    /// </summary>

    private static Settings InitializeSettings (Settings settings)
    {
        // Apply any pending schema migrations before any consumer reads the settings.
        _ = LegacyPreferencesMigrator.Migrate(settings);

        settings.Preferences ??= new Preferences();
        settings.Preferences.ToolEntries ??= [];
        settings.Preferences.ColumnizerMaskList ??= [];

        settings.FileHistoryList ??= [];

        settings.LastOpenFilesList ??= [];

        settings.FileColors ??= [];

        InitializeFont(settings);

        if (settings.Preferences.ShowTailColor == Color.Empty)
        {
            settings.Preferences.ShowTailColor = Color.FromKnownColor(KnownColor.Blue);
        }

        if (settings.Preferences.TimeSpreadColor == Color.Empty)
        {
            settings.Preferences.TimeSpreadColor = Color.Gray;
        }

        if (settings.Preferences.BufferCount < 10)
        {
            settings.Preferences.BufferCount = 100;
        }

        if (settings.Preferences.LinesPerBuffer < 1)
        {
            settings.Preferences.LinesPerBuffer = 500;
        }

        settings.FilterList ??= [];

        settings.SearchHistoryList ??= [];

        settings.FilterHistoryList ??= [];

        settings.FilterRangeHistoryList ??= [];

        foreach (FilterParams filterParams in settings.FilterList)
        {
            filterParams.Init();
        }

        if (settings.Preferences.HighlightGroupList == null)
        {
            settings.Preferences.HighlightGroupList = [];
        }

        settings.Preferences.HighlightMaskList ??= [];

        if (settings.Preferences.PollingInterval < 20)
        {
            settings.Preferences.PollingInterval = 250;
        }

        settings.Preferences.MultiFileOptions ??= new MultiFileOptions();

        // UTF-8 rather than Encoding.Default: identical text on .NET, but Encoding.Default is not one of
        // the rows the Preferences dropdown offers, so seeding from it would name an unselectable entry.
        settings.Preferences.DefaultEncoding ??= System.Text.Encoding.UTF8.HeaderName;

        settings.Preferences.DefaultLanguage ??= CultureInfo.GetCultureInfo("en-US").Name;

        if (settings.Preferences.MaximumFilterEntriesDisplayed == 0)
        {
            settings.Preferences.MaximumFilterEntriesDisplayed = 20;
        }

        if (settings.Preferences.MaximumFilterEntries == 0)
        {
            settings.Preferences.MaximumFilterEntries = 30;
        }

        SetBoundsWithinVirtualScreen(settings);

        return settings;
    }

    /// <summary>
    /// Materializes the persisted <see cref="Preferences.FontString"/> into a live <see cref="Font"/>
    /// instance <see cref="Preferences.FontString"/> is the source of truth in settings.json so
    /// that family, size, style and unit round-trip through the FontDialog.
    /// </summary>
    private static void InitializeFont (Settings settings)
    {
        var converter = TypeDescriptor.GetConverter(typeof(Font));
        var fallbackFamily = FontFamily.GenericMonospace.Name;

        Font font = TryDeserializeFont(converter, settings.Preferences.FontString, "FontString")
            ?? new Font(fallbackFamily, 9f);

        settings.Preferences.Font?.Dispose();
        settings.Preferences.Font = font;
        settings.Preferences.FontString = converter.ConvertToInvariantString(font);
    }

    private static Font? TryDeserializeFont (TypeConverter converter, string? value, string sourceLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return converter.ConvertFromInvariantString(value) as Font;
        }
        catch (Exception ex) when (ex is NotSupportedException or ArgumentException or FormatException)
        {
            _logger.Warn(ex, $"Could not deserialize font from {sourceLabel}='{value}'.");
            return null;
        }
    }

    /// <summary>
    /// Saves the Settings to file, fires OnConfigChanged Event so LogTabWindow is updated
    /// </summary>
    /// <param name="settings">Settings to be saved</param>
    /// <param name="flags">Settings that "changed"</param>
    [SupportedOSPlatform("windows")]
    private void Save (Settings settings, SettingsFlags flags)
    {
        lock (_loadSaveLock)
        {
            string dir = ActiveConfigDir;

            if (!Directory.Exists(dir))
            {
                _ = Directory.CreateDirectory(dir);
            }

            FileInfo fileInfo = new(dir + Path.DirectorySeparatorChar + SETTINGS_FILE_NAME);
            Save(fileInfo, settings);

            OnConfigChanged(flags);
        }
    }

    /// <summary>
    /// Saves the file in any defined format
    /// </summary>
    /// <param name="fileInfo">FileInfo for creating the file (if exists will be overwritten)</param>
    /// <param name="settings">Current Settings</param>
    private void Save (FileInfo fileInfo, Settings settings)
    {
        //Currently only fileFormat, maybe add some other formats later (YAML or XML?)
        SaveAsJSON(fileInfo, settings);
    }

    /// <summary>
    /// Migrates configuration files from the old portable layout ({AppDir}/portable/ + {AppDir}/settings.json)
    /// to the new unified layout ({AppDir}/configuration/).
    /// </summary>
    private void MigrateOldPortableLayout ()
    {
        _logger.Info("Starting migration from old portable layout to new layout");

        try
        {
            // Ensure new directory exists
            _ = Directory.CreateDirectory(PortableConfigDir);

            // Move settings.json from app root to configuration/
            MoveFileIfExists(
                Path.Join(_applicationStartupPath, SETTINGS_FILE_NAME),
                Path.Join(PortableConfigDir, SETTINGS_FILE_NAME));

            MoveFileIfExists(
                Path.Join(_applicationStartupPath, SETTINGS_FILE_NAME + ".bak"),
                Path.Join(PortableConfigDir, SETTINGS_FILE_NAME + ".bak"));

            // Move all files from old portable/ directory to configuration/
#pragma warning disable CS0618
            if (Directory.Exists(PortableModeDir))
            {
                foreach (var file in Directory.GetFiles(PortableModeDir))
                {
                    var fileName = Path.GetFileName(file);
                    if (fileName.Equals(PortableModeSettingsFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Move marker file too
                        MoveFileIfExists(file, Path.Join(PortableConfigDir, fileName));
                        continue;
                    }

                    MoveFileIfExists(file, Path.Join(PortableConfigDir, fileName));
                }

                // Move subdirectories (e.g., Plugins/)
                foreach (var subDir in Directory.GetDirectories(PortableModeDir))
                {
                    var dirName = Path.GetFileName(subDir);
                    var targetDir = Path.Join(PortableConfigDir, dirName);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.Move(subDir, targetDir);
                        _logger.Info("Moved directory: {Source} -> {Target}", subDir, targetDir);
                    }
                }

                // Clean up old directory if empty
                if (!Directory.EnumerateFileSystemEntries(PortableModeDir).Any())
                {
                    Directory.Delete(PortableModeDir);
                    _logger.Info("Deleted empty old portable directory: {Dir}", PortableModeDir);
                }
            }
#pragma warning restore CS0618

            // Move session files if they exist in old location
            var oldSessionDir = Path.Join(_applicationStartupPath, "sessionFiles");
            if (Directory.Exists(oldSessionDir))
            {
                _ = Directory.CreateDirectory(PortableSessionDir);
                foreach (var file in Directory.GetFiles(oldSessionDir, "*.lxp"))
                {
                    MoveFileIfExists(file, Path.Join(PortableSessionDir, Path.GetFileName(file)));
                }

                if (!Directory.EnumerateFileSystemEntries(oldSessionDir).Any())
                {
                    Directory.Delete(oldSessionDir);
                }
            }

            // Copy plugin trust/permissions from %APPDATA% (don't move — user may have normal install too)
            CopyFileIfNotExists(
                Path.Join(ConfigDir, "trusted-plugins.json"),
                Path.Join(PortableConfigDir, "trusted-plugins.json"));

            CopyFileIfNotExists(
                Path.Join(ConfigDir, "plugin-permissions.json"),
                Path.Join(PortableConfigDir, "plugin-permissions.json"));

            _logger.Info("Migration from old portable layout completed successfully");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Error(ex, "Error during old portable layout migration");
        }
    }

    /// <summary>
    /// Copies configuration files from normal mode location (%APPDATA%/LogExpert/)
    /// to the portable configuration directory ({AppDir}/configuration/).
    /// Called when portable mode is activated and user confirms copy.
    /// </summary>
    public void CopyConfigToPortable ()
    {
        _logger.Info("Copying configuration to portable directory: {Dir}", PortableConfigDir);

        try
        {
            _ = Directory.CreateDirectory(PortableConfigDir);

            // Main configuration files
            string[] filesToCopy =
            [
                SETTINGS_FILE_NAME,
            SETTINGS_FILE_NAME + ".bak",
            "trusted-plugins.json",
            "plugin-permissions.json",
        ];

            foreach (var fileName in filesToCopy)
            {
                CopyFileIfExists(
                    Path.Join(ConfigDir, fileName),
                    Path.Join(PortableConfigDir, fileName));
            }

            // Columnizer config files (various extensions)
            foreach (var file in Directory.GetFiles(ConfigDir))
            {
                var ext = Path.GetExtension(file).ToUpperInvariant();
                var name = Path.GetFileName(file);

                // Skip files we already copied and non-config files
                if (filesToCopy.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (ext is ".DAT" or ".CFG" or ".JSON" && !name.Equals(SETTINGS_FILE_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    CopyFileIfExists(file, Path.Join(PortableConfigDir, name));
                }
            }

            // Copy Plugins directory recursively
            var pluginsDir = Path.Join(ConfigDir, "Plugins");
            if (Directory.Exists(pluginsDir))
            {
                CopyDirectoryRecursive(pluginsDir, Path.Join(PortableConfigDir, "Plugins"));
            }

            _logger.Info("Configuration copy to portable directory completed");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Error(ex, "Error copying configuration to portable directory");
            throw; // Re-throw so UI can show error
        }
    }

    /// <summary>
    /// Moves configuration files from the portable directory ({AppDir}/configuration/)
    /// back to normal mode locations (%APPDATA%/LogExpert/).
    /// Called when portable mode is deactivated and user confirms migration.
    /// </summary>
    public void MoveConfigFromPortable ()
    {
        _logger.Info("Moving configuration from portable directory to: {Dir}", ConfigDir);

        try
        {
            _ = Directory.CreateDirectory(ConfigDir);

            // Move all config files
            foreach (var file in Directory.GetFiles(PortableConfigDir))
            {
                var fileName = Path.GetFileName(file);

                // Skip marker file — it will be deleted separately
                if (fileName.Equals(PortableModeSettingsFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var target = Path.Join(ConfigDir, fileName);
                if (File.Exists(target))
                {
                    File.Delete(target);
                }

                File.Move(file, target);
                _logger.Info("Moved: {Source} -> {Target}", file, target);
            }

            // Move Plugins directory
            var portablePluginsDir = Path.Join(PortableConfigDir, "Plugins");
            var normalPluginsDir = Path.Join(ConfigDir, "Plugins");
            if (Directory.Exists(portablePluginsDir))
            {
                CopyDirectoryRecursive(portablePluginsDir, normalPluginsDir);
                Directory.Delete(portablePluginsDir, recursive: true);
            }

            // Move session files to Documents
            var portableSessionsDir = PortableSessionDir;
            if (Directory.Exists(portableSessionsDir))
            {
                var docsSessionDir = Path.Join(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LogExpert");
                _ = Directory.CreateDirectory(docsSessionDir);

                foreach (var file in Directory.GetFiles(portableSessionsDir, "*.lxp"))
                {
                    MoveFileIfExists(file, Path.Join(docsSessionDir, Path.GetFileName(file)));
                }

                if (!Directory.EnumerateFileSystemEntries(portableSessionsDir).Any())
                {
                    Directory.Delete(portableSessionsDir);
                }
            }

            // Clean up portable directory
            if (Directory.Exists(PortableConfigDir) &&
                !Directory.EnumerateFileSystemEntries(PortableConfigDir).Any())
            {
                Directory.Delete(PortableConfigDir);
                _logger.Info("Deleted empty portable configuration directory");
            }

            _logger.Info("Configuration migration from portable directory completed");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Error(ex, "Error moving configuration from portable directory");
            throw;
        }
    }

    private void Save (FileInfo fileInfo, Settings settings, SettingsFlags flags)
    {
        switch (flags)
        {
            case SettingsFlags.HighlightSettings:
                SaveHighlightGroupsAsJSON(fileInfo, settings.Preferences.HighlightGroupList);
                break;
            case SettingsFlags.None:
                // No action required for SettingsFlags.None
                break;
            case SettingsFlags.WindowPosition:
                // No action required for SettingsFlags.WindowPosition
                break;
            case SettingsFlags.FileHistory:
                // No action required for SettingsFlags.FileHistory
                break;
            case SettingsFlags.FilterList:
                // No action required for SettingsFlags.FilterList
                break;
            case SettingsFlags.RegexHistory:
                // No action required for SettingsFlags.RegexHistory
                break;
            case SettingsFlags.ToolSettings:
                // No action required for SettingsFlags.ToolSettings
                break;
            case SettingsFlags.GuiOrColors:
                // No action required for SettingsFlags.GuiOrColors
                break;
            case SettingsFlags.FilterHistory:
                // No action required for SettingsFlags.FilterHistory
                break;
            case SettingsFlags.All:
                // No action required for SettingsFlags.All
                break;
            case SettingsFlags.Settings:
                // No action required for SettingsFlags.Settings
                break;
            default:
                break;
        }

        OnConfigChanged(flags);
    }

    /// <summary>
    /// Saves the settings as JSON file.
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <param name="settings"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private void SaveAsJSON (FileInfo fileInfo, Settings settings)
    {
        if (!ValidateSettings(settings))
        {
            _logger.Error("Settings validation failed - refusing to save");
            throw new InvalidOperationException(Resources.ConfigManager_Error_Messages_InvalidOperation_SettingsValidationFailed);
        }

        settings.VersionBuild = Assembly.GetExecutingAssembly().GetName().Version.Build;
        string json = JsonConvert.SerializeObject(settings, _jsonSettings);

        _logger.Info($"Saving settings: " +
            $"Filters={settings.FilterList?.Count ?? 0}, " +
            $"History={settings.FileHistoryList?.Count ?? 0}, " +
            $"Highlights={settings.Preferences?.HighlightGroupList?.Count ?? 0}, " +
            $"Size={json.Length} bytes");

        WriteSettingsFile(fileInfo, json);
    }

    private static void WriteSettingsFile (FileInfo fileInfo, string json)
    {
        string tempFile = fileInfo.FullName + ".tmp";
        string backupFile = fileInfo.FullName + ".bak";

        try
        {
            _logger.Info($"Writing to {fileInfo.FullName}");
            File.WriteAllText(tempFile, json, System.Text.Encoding.UTF8);

            if (File.Exists(fileInfo.FullName))
            {
                long existingSize = new FileInfo(fileInfo.FullName).Length;
                if (existingSize > 0)
                {
                    File.Copy(fileInfo.FullName, backupFile, overwrite: true);
                    _logger.Info($"Created backup: {backupFile} ({existingSize} bytes)");
                }
                else
                {
                    _logger.Warn($"Existing settings file is empty ({existingSize} bytes), skipping backup");
                }
            }

            File.Move(tempFile, fileInfo.FullName, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save settings: {ex}");

            // Attempt recovery: restore from backup if main file was corrupted
            try
            {
                if (File.Exists(backupFile))
                {
                    var mainFileExists = File.Exists(fileInfo.FullName);
                    var mainFileSize = mainFileExists ? new FileInfo(fileInfo.FullName).Length : 0;

                    if (!mainFileExists || mainFileSize == 0)
                    {
                        File.Copy(backupFile, fileInfo.FullName, overwrite: true);
                        _logger.Warn("Settings save failed, restored from backup");
                    }
                }
            }
            catch (Exception recoverException) when (recoverException is ArgumentException or
                                                                         ArgumentNullException or
                                                                         DirectoryNotFoundException or
                                                                         FileNotFoundException or
                                                                         IOException or
                                                                         NotSupportedException or
                                                                         PathTooLongException or
                                                                         UnauthorizedAccessException)
            {
                _logger.Error($"Failed to recover from backup: {recoverException}");
            }

            throw;
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception cleanUpException) when (cleanUpException is ArgumentException or
                                                                             DirectoryNotFoundException or
                                                                             IOException or
                                                                             NotSupportedException or
                                                                             PathTooLongException or
                                                                             UnauthorizedAccessException)
                {
                    _logger.Warn($"Failed to clean up temp file: {cleanUpException.Message}");
                }
            }
        }
    }

    private static void SaveHighlightGroupsAsJSON (FileInfo fileInfo, List<HighlightGroup> groups)
    {
        string json = JsonConvert.SerializeObject(groups, Formatting.Indented);
        File.WriteAllText(fileInfo.FullName, json, System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// Imports only the highlight groups from the specified file.
    /// </summary>
    /// <param name="currentGroups"></param>
    /// <param name="fileInfo"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    private static List<HighlightGroup> Import (List<HighlightGroup> currentGroups, FileInfo fileInfo, ExportImportFlags flags)
    {
        List<HighlightGroup> newGroups;

        try
        {
            newGroups = JsonConvert.DeserializeObject<List<HighlightGroup>>(File.ReadAllText($"{fileInfo.FullName}"));
        }
        catch (Exception e) when (e is ArgumentException or
                                       ArgumentNullException or
                                       DirectoryNotFoundException or
                                       FileNotFoundException or
                                       IOException or
                                       NotSupportedException or
                                       PathTooLongException or
                                       UnauthorizedAccessException or
                                       SecurityException or
                                       JsonSerializationException)
        {
            _logger.Error($"Error while deserializing config data: {e}");
            newGroups = [];
        }

        if (flags.HasFlag(ExportImportFlags.KeepExisting))
        {
            currentGroups.AddRange(newGroups);
        }
        else
        {
            currentGroups.Clear();
            currentGroups.AddRange(newGroups);
        }

        return currentGroups;
    }

    /// <summary>
    /// Imports all or some of the settings/preferences stored in the input stream.
    /// This will overwrite appropriate parts of the current (own) settings with the imported ones.
    /// </summary>
    /// <param name="currentSettings"></param>
    /// <param name="fileInfo"></param>
    /// <param name="flags">Flags to indicate which parts shall be imported</param>
    [SupportedOSPlatform("windows")]
    private Settings Import (Settings currentSettings, FileInfo fileInfo, ExportImportFlags flags)
    {
        LoadResult loadResult = LoadOrCreateNew(fileInfo);
        Settings importSettings = loadResult.Settings;
        Settings ownSettings = ObjectClone.Clone(currentSettings);
        Settings newSettings;

        // Check for 'All' flag first - import everything
        if (flags.HasFlag(ExportImportFlags.All))
        {
            // For All, start with imported settings and selectively keep some current data if KeepExisting is set
            newSettings = ObjectClone.Clone(importSettings);

            if (flags.HasFlag(ExportImportFlags.KeepExisting))
            {
                // Merge with existing settings
                newSettings.FilterList = ReplaceOrKeepExisting(flags, ownSettings.FilterList, importSettings.FilterList);
                newSettings.FileHistoryList = ReplaceOrKeepExisting(flags, ownSettings.FileHistoryList, importSettings.FileHistoryList);
                newSettings.SearchHistoryList = ReplaceOrKeepExisting(flags, ownSettings.SearchHistoryList, importSettings.SearchHistoryList);
                newSettings.FilterHistoryList = ReplaceOrKeepExisting(flags, ownSettings.FilterHistoryList, importSettings.FilterHistoryList);
                newSettings.FilterRangeHistoryList = ReplaceOrKeepExisting(flags, ownSettings.FilterRangeHistoryList, importSettings.FilterRangeHistoryList);

                newSettings.Preferences.HighlightGroupList = ReplaceOrKeepExisting(flags, ownSettings.Preferences.HighlightGroupList, importSettings.Preferences.HighlightGroupList);
                newSettings.Preferences.ColumnizerMaskList = ReplaceOrKeepExisting(flags, ownSettings.Preferences.ColumnizerMaskList, importSettings.Preferences.ColumnizerMaskList);
                newSettings.Preferences.HighlightMaskList = ReplaceOrKeepExisting(flags, ownSettings.Preferences.HighlightMaskList, importSettings.Preferences.HighlightMaskList);
                newSettings.Preferences.ToolEntries = ReplaceOrKeepExisting(flags, ownSettings.Preferences.ToolEntries, importSettings.Preferences.ToolEntries);
            }

            return newSettings;
        }

        // For partial imports, start with current settings and selectively update
        newSettings = ownSettings;

        // Check for 'Other' as this covers most preference options
        if ((flags & ExportImportFlags.Other) == ExportImportFlags.Other)
        {
            newSettings.Preferences = ObjectClone.Clone(importSettings.Preferences);
            // Preserve specific lists that have their own flags
            newSettings.Preferences.ColumnizerMaskList = ownSettings.Preferences.ColumnizerMaskList;
            newSettings.Preferences.HighlightMaskList = ownSettings.Preferences.HighlightMaskList;
            newSettings.Preferences.HighlightGroupList = ownSettings.Preferences.HighlightGroupList;
            newSettings.Preferences.ToolEntries = ownSettings.Preferences.ToolEntries;
        }

        if ((flags & ExportImportFlags.ColumnizerMasks) == ExportImportFlags.ColumnizerMasks)
        {
            newSettings.Preferences.ColumnizerMaskList = ReplaceOrKeepExisting(flags, ownSettings.Preferences.ColumnizerMaskList, importSettings.Preferences.ColumnizerMaskList);
        }

        if ((flags & ExportImportFlags.HighlightMasks) == ExportImportFlags.HighlightMasks)
        {
            newSettings.Preferences.HighlightMaskList = ReplaceOrKeepExisting(flags, ownSettings.Preferences.HighlightMaskList, importSettings.Preferences.HighlightMaskList);
        }

        if ((flags & ExportImportFlags.HighlightSettings) == ExportImportFlags.HighlightSettings)
        {
            newSettings.Preferences.HighlightGroupList = ReplaceOrKeepExisting(flags, ownSettings.Preferences.HighlightGroupList, importSettings.Preferences.HighlightGroupList);
        }

        if ((flags & ExportImportFlags.ToolEntries) == ExportImportFlags.ToolEntries)
        {
            newSettings.Preferences.ToolEntries = ReplaceOrKeepExisting(flags, ownSettings.Preferences.ToolEntries, importSettings.Preferences.ToolEntries);
        }

        return newSettings;
    }

    /// <summary>
    /// Replaces the existing list with the new list or keeps existing entries based on the flags.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="flags"></param>
    /// <param name="existingList"></param>
    /// <param name="newList"></param>
    /// <returns></returns>
    private static List<T> ReplaceOrKeepExisting<T> (ExportImportFlags flags, List<T> existingList, List<T> newList)
    {
        return (flags & ExportImportFlags.KeepExisting) == ExportImportFlags.KeepExisting
            ? [.. existingList.Union(newList)]
            : newList;
    }

    // Checking if the appBounds values are outside the current virtual screen.
    // If so, the appBounds values are set to 0.
    [SupportedOSPlatform("windows")]
    private static void SetBoundsWithinVirtualScreen (Settings settings)
    {
        Instance.EnsureInitialized();

        if (Instance._virtualScreenBounds.X + Instance._virtualScreenBounds.Width < settings.AppBounds.X + settings.AppBounds.Width ||
            Instance._virtualScreenBounds.Y + Instance._virtualScreenBounds.Height < settings.AppBounds.Y + settings.AppBounds.Height)
        {
            settings.AppBounds = new Rectangle();
        }
    }

    /// <summary>
    /// Checks if settings object appears to be empty or default, considering the import flags.
    /// For full imports, all sections are checked. For partial imports, only relevant sections are validated.
    /// This helps detect corrupted files while allowing legitimate partial imports.
    /// </summary>
    /// <param name="settings">Settings object to validate</param>
    /// <param name="importFlags">Flags indicating which sections are being imported</param>
    /// <returns>True if the relevant settings sections appear empty/default, false if they contain user data</returns>
    private static bool SettingsAreEmptyOrDefault (Settings settings, ExportImportFlags importFlags)
    {
        if (settings == null)
        {
            return true;
        }

        if (settings.Preferences == null)
        {
            return true;
        }

        // For full imports or when no specific flags are set, check all sections
        if (importFlags is ExportImportFlags.All or ExportImportFlags.None)
        {
            var filterCount = settings.FilterList?.Count ?? 0;
            var historyCount = settings.FileHistoryList?.Count ?? 0;
            var searchHistoryCount = settings.SearchHistoryList?.Count ?? 0;
            var highlightCount = settings.Preferences.HighlightGroupList?.Count ?? 0;
            var columnizerMaskCount = settings.Preferences.ColumnizerMaskList?.Count ?? 0;

            return filterCount == 0 &&
                   historyCount == 0 &&
                   searchHistoryCount == 0 &&
                   highlightCount == 0 &&
                   columnizerMaskCount == 0;
        }

        // For partial imports, check only the sections being imported
        // At least one relevant section must have data for the import to be valid
        bool hasAnyRelevantData = false;

        // Check HighlightSettings flag
        if (importFlags.HasFlag(ExportImportFlags.HighlightSettings))
        {
            var highlightCount = settings.Preferences.HighlightGroupList?.Count ?? 0;
            if (highlightCount > 0)
            {
                hasAnyRelevantData = true;
            }
        }

        // Check ColumnizerMasks flag
        if (importFlags.HasFlag(ExportImportFlags.ColumnizerMasks))
        {
            var columnizerMaskCount = settings.Preferences.ColumnizerMaskList?.Count ?? 0;
            if (columnizerMaskCount > 0)
            {
                hasAnyRelevantData = true;
            }
        }

        // Check HighlightMasks flag
        if (importFlags.HasFlag(ExportImportFlags.HighlightMasks))
        {
            var highlightMaskCount = settings.Preferences.HighlightMaskList?.Count ?? 0;
            if (highlightMaskCount > 0)
            {
                hasAnyRelevantData = true;
            }
        }

        // Check ToolEntries flag
        if (importFlags.HasFlag(ExportImportFlags.ToolEntries))
        {
            var toolEntriesCount = settings.Preferences.ToolEntries?.Count ?? 0;
            if (toolEntriesCount > 0)
            {
                hasAnyRelevantData = true;
            }
        }

        // Check Other flag (preferences/settings that don't fall into specific categories)
        if (importFlags.HasFlag(ExportImportFlags.Other))
        {
            // For 'Other', we consider the settings valid if Preferences object exists
            // This covers font settings, colors, and other preference data
            hasAnyRelevantData = true;
        }

        // Return true (isEmpty) if no relevant data was found in any checked section
        return !hasAnyRelevantData;
    }

    /// <summary>
    /// Validates settings object for basic integrity.
    /// Logs warnings for suspicious conditions.
    /// </summary>
    /// <param name="settings">Settings to validate</param>
    /// <returns>True if settings pass validation</returns>
    private bool ValidateSettings (Settings settings)
    {
        if (settings == null)
        {
            _logger.Error("Attempted to save null settings");
            return false;
        }

        if (settings.Preferences == null)
        {
            _logger.Error("Settings.Preferences is null");
            return false;
        }

        // For save operations, always validate all sections (use ExportImportFlags.All)
        if (SettingsAreEmptyOrDefault(settings, ExportImportFlags.All))
        {
            _logger.Warn("Settings appear to be empty - this may indicate data loss");

            if (_settings != null && !SettingsAreEmptyOrDefault(_settings, ExportImportFlags.All))
            {
                _logger.Warn($"Previous settings: " +
                    $"Filters={_settings.FilterList?.Count ?? 0}, " +
                    $"History={_settings.FileHistoryList?.Count ?? 0}, " +
                    $"SearchHistory={_settings.SearchHistoryList?.Count ?? 0}, " +
                    $"Highlights={_settings.Preferences?.HighlightGroupList?.Count ?? 0}");
            }
        }

        return true;
    }

    private static void MoveFileIfExists (string source, string target)
    {
        if (!File.Exists(source))
        {
            return;
        }

        if (File.Exists(target))
        {
            File.Delete(target);
        }

        File.Move(source, target);
        _logger.Info("Moved file: {Source} -> {Target}", source, target);
    }

    private static void CopyFileIfExists (string source, string target)
    {
        if (!File.Exists(source))
        {
            return;
        }

        File.Copy(source, target, overwrite: true);
        _logger.Info("Copied file: {Source} -> {Target}", source, target);
    }

    private static void CopyFileIfNotExists (string source, string target)
    {
        if (!File.Exists(source) || File.Exists(target))
        {
            return;
        }

        File.Copy(source, target);
        _logger.Info("Copied file (new): {Source} -> {Target}", source, target);
    }

    private static void CopyDirectoryRecursive (string sourceDir, string targetDir)
    {
        _ = Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Join(targetDir, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectoryRecursive(dir, Path.Join(targetDir, Path.GetFileName(dir)));
        }
    }

    #endregion

    /// <summary>
    /// Fires the ConfigChanged event
    /// </summary>
    /// <param name="flags"></param>
    protected void OnConfigChanged (SettingsFlags flags)
    {
        ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(flags));
    }
}