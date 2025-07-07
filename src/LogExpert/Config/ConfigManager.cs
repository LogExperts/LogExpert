using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArguments;
using LogExpert.Core.Interface;

using Newtonsoft.Json;

using NLog;

namespace LogExpert.Config;

public class ConfigManager : IConfigManager
{
    #region Fields

    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    private static readonly object _monitor = new();
    private static ConfigManager _instance;
    private readonly object _loadSaveLock = new();
    private Settings _settings;

    #endregion

    #region cTor

    private ConfigManager ()
    {
        _settings = Load();
    }

    #endregion

    #region Events

    public event EventHandler<ConfigChangedEventArgs> ConfigChanged;

    #endregion

    #region Properties

    //TODO: Change to init
    public static ConfigManager Instance
    {
        get
        {
            lock (_monitor)
            {
                _instance ??= new ConfigManager();
            }
            return _instance;
        }
    }

    public string ConfigDir => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "LogExpert"; //TODO: change to Path.Combine

    /// <summary>
    /// Application.StartupPath + portable
    /// </summary>
    public string PortableModeDir => Application.StartupPath + Path.DirectorySeparatorChar + "portable";

    /// <summary>
    /// portableMode.json
    /// </summary>
    public string PortableModeSettingsFileName => "portableMode.json";

    public Settings Settings => Instance._settings;

    IConfigManager IConfigManager.Instance => Instance;

    //        Action<object, ConfigChangedEventArgs> IConfigManager.ConfigChanged { get => ((IConfigManager)_instance).ConfigChanged; set => ((IConfigManager)_instance).ConfigChanged = value; }

    //public string PortableModeSettingsFileName => ((IConfigManager)_instance).PortableModeSettingsFileName;

    #endregion

    #region Public methods

    public void Save (SettingsFlags flags)
    {
        Instance.Save(Settings, flags);
    }

    public void Export (FileInfo fileInfo)
    {
        Instance.Save(fileInfo, Settings);
    }

    public void Export (FileInfo fileInfo, SettingsFlags flags)
    {
        Instance.Save(fileInfo, Settings, flags);
    }

    public void Import (FileInfo fileInfo, ExportImportFlags flags)
    {
        Instance._settings = Instance.Import(Instance._settings, fileInfo, flags);
        Save(SettingsFlags.All);
    }

    public void ImportHighlightSettings (FileInfo fileInfo, ExportImportFlags flags)
    {
        Instance._settings.Preferences.HighlightGroupList = Instance.Import(Instance._settings.Preferences.HighlightGroupList, fileInfo, flags);
        Save(SettingsFlags.All);
    }

    #endregion

    #region Private Methods

    private Settings Load ()
    {
        _logger.Info(CultureInfo.InvariantCulture, "Loading settings");

        string dir;
        
        if (!File.Exists(Path.Combine(PortableModeDir, PortableModeSettingsFileName)))
        {
            _logger.Info(CultureInfo.InvariantCulture, "Load settings standard mode");
            dir = ConfigDir;
        }
        else
        {
            _logger.Info("Load settings portable mode");
            dir = Application.StartupPath;
        }

        if (!Directory.Exists(dir))
        {
            _ = Directory.CreateDirectory(dir);
        }

        if (!File.Exists(Path.Combine(dir, "settings.json")))
        {
            return LoadOrCreateNew(null);
        }

        try
        {
            FileInfo fileInfo = new(Path.Combine(dir, "settings.json"));
            return LoadOrCreateNew(fileInfo);
        }
        catch (IOException ex)
        {
            _logger.Error($"File system error: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Error($"Access denied: {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error($"Unexpected error: {ex.Message}");
        }

        return LoadOrCreateNew(null);

    }

    /// <summary>
    /// Loads Settings of a given file or creates new settings if the file does not exist
    /// </summary>
    /// <param name="fileInfo">file that has settings saved</param>
    /// <returns>loaded or created settings</returns>
    private Settings LoadOrCreateNew (FileInfo fileInfo)
    {
        lock (_loadSaveLock)
        {
            Settings settings;

            if (fileInfo == null || fileInfo.Exists == false)
            {
                settings = new Settings();
            }
            else
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText($"{fileInfo.FullName}"));
                }
                catch (Exception e)
                {
                    _logger.Error($"Error while deserializing config data: {e}");
                    settings = new Settings();
                }
            }

            settings.Preferences ??= new Preferences();

            settings.Preferences.ToolEntries ??= [];

            settings.Preferences.ColumnizerMaskList ??= [];

            settings.FileHistoryList ??= [];

            settings.LastOpenFilesList ??= [];

            settings.FileColors ??= [];

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

            settings.Preferences.DefaultEncoding ??= Encoding.Default.HeaderName;

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
    }

    /// <summary>
    /// Saves the Settings to file, fires OnConfigChanged Event so LogTabWindow is updated
    /// </summary>
    /// <param name="settings">Settings to be saved</param>
    /// <param name="flags">Settings that "changed"</param>
    private void Save (Settings settings, SettingsFlags flags)
    {
        lock (_loadSaveLock)
        {
            _logger.Info(CultureInfo.InvariantCulture, "Saving settings");
            var dir = Settings.Preferences.PortableMode ? Application.StartupPath : ConfigDir;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            FileInfo fileInfo = new(dir + Path.DirectorySeparatorChar + "settings.json");
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

    private void Save (FileInfo fileInfo, Settings settings, SettingsFlags flags)
    {
        switch (flags)
        {
            case SettingsFlags.HighlightSettings:
                SaveHighlightgroupsAsJSON(fileInfo, settings.Preferences.HighlightGroupList);
                break;
        }

        OnConfigChanged(flags);
    }

    private static void SaveAsJSON (FileInfo fileInfo, Settings settings)
    {
        settings.VersionBuild = Assembly.GetExecutingAssembly().GetName().Version.Build;

        using StreamWriter sw = new(fileInfo.Create());
        JsonSerializer serializer = new();
        serializer.Serialize(sw, settings);
    }

    private static void SaveHighlightgroupsAsJSON (FileInfo fileInfo, List<HighlightGroup> groups)
    {
        using StreamWriter sw = new(fileInfo.Create());
        JsonSerializer serializer = new();
        serializer.Serialize(sw, groups);
    }

    private List<HighlightGroup> Import (List<HighlightGroup> currentGroups, FileInfo fileInfo, ExportImportFlags flags)
    {
        List<HighlightGroup> newGroups;

        try
        {
            newGroups = JsonConvert.DeserializeObject<List<HighlightGroup>>(File.ReadAllText($"{fileInfo.FullName}"));
        }
        catch (Exception e)
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
    /// Imports all or some of the settings/prefs stored in the input stream.
    /// This will overwrite appropriate parts of the current (own) settings with the imported ones.
    /// </summary>
    /// <param name="currentSettings"></param>
    /// <param name="fileInfo"></param>
    /// <param name="flags">Flags to indicate which parts shall be imported</param>
    private Settings Import (Settings currentSettings, FileInfo fileInfo, ExportImportFlags flags)
    {
        var importSettings = LoadOrCreateNew(fileInfo);
        var ownSettings = ObjectClone.Clone(currentSettings);
        Settings newSettings;

        // at first check for 'Other' as this are the most options.
        if ((flags & ExportImportFlags.Other) == ExportImportFlags.Other)
        {
            newSettings = ownSettings;
            newSettings.Preferences = ObjectClone.Clone(importSettings.Preferences);
            newSettings.Preferences.ColumnizerMaskList = ownSettings.Preferences.ColumnizerMaskList;
            newSettings.Preferences.HighlightMaskList = ownSettings.Preferences.HighlightMaskList;
            newSettings.Preferences.HighlightGroupList = ownSettings.Preferences.HighlightGroupList;
            newSettings.Preferences.ToolEntries = ownSettings.Preferences.ToolEntries;
        }
        else
        {
            newSettings = ownSettings;
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

    private static List<T> ReplaceOrKeepExisting<T> (ExportImportFlags flags, List<T> existingList, List<T> newList)
    {
        if ((flags & ExportImportFlags.KeepExisting) == ExportImportFlags.KeepExisting)
        {
            return existingList.Union(newList).ToList();
        }

        return newList;
    }

    // Checking if the appBounds values are outside the current virtual screen.
    // If so, the appBounds values are set to 0.
    private void SetBoundsWithinVirtualScreen (Settings settings)
    {
        var vs = SystemInformation.VirtualScreen;
        if (vs.X + vs.Width < settings.AppBounds.X + settings.AppBounds.Width ||
            vs.Y + vs.Height < settings.AppBounds.Y + settings.AppBounds.Height)
        {
            settings.AppBounds = new Rectangle();
        }
    }
    #endregion

    protected void OnConfigChanged (SettingsFlags flags)
    {
        ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(flags));
    }
}