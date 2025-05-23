using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.EventArgs;

using Newtonsoft.Json;

using NLog;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace LogExpert.Config
{
    public class ConfigManager
    {
        #region Fields

        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private static readonly object _monitor = new();
        private static ConfigManager _instance;
        private readonly object _loadSaveLock = new();
        private Settings _settings;

        #endregion

        #region cTor

        private ConfigManager()
        {
            _settings = Load();
        }

        #endregion

        #region Events

        public event ConfigChangedEventHandler ConfigChanged;

        #endregion

        #region Properties

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

        public static string ConfigDir => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "LogExpert";

        /// <summary>
        /// Application.StartupPath + portable
        /// </summary>
        public static string PortableModeDir => Application.StartupPath + Path.DirectorySeparatorChar + "portable";

        /// <summary>
        /// portableMode.json
        /// </summary>
        public static string PortableModeSettingsFileName => "portableMode.json";

        public static Settings Settings => Instance._settings;

        #endregion

        #region Public methods

        public static void Save(SettingsFlags flags)
        {
            Instance.Save(Settings, flags);
        }

        public static void Export(FileInfo fileInfo)
        {
            Instance.Save(fileInfo, Settings);
        }

        public static void Export(FileInfo fileInfo, SettingsFlags flags)
        {
            Instance.Save(fileInfo, Settings, flags);
        }

        public static void Import(FileInfo fileInfo, ExportImportFlags flags)
        {
            Instance._settings = Instance.Import(Instance._settings, fileInfo, flags);
            Save(SettingsFlags.All);
        }

        public static void ImportHighlightSettings(FileInfo fileInfo, ExportImportFlags flags)
        {
            Instance._settings.Preferences.HighlightGroupList = Instance.Import(Instance._settings.Preferences.HighlightGroupList, fileInfo, flags);
            Save(SettingsFlags.All);
        }

        #endregion

        #region Private Methods

        private Settings Load()
        {
            _logger.Info("Loading settings");

            string dir;

            if (File.Exists(PortableModeDir + Path.DirectorySeparatorChar + PortableModeSettingsFileName) == false)
            {
                _logger.Info("Load settings standard mode");
                dir = ConfigDir;
            }
            else
            {
                _logger.Info("Load settings portable mode");
                dir = Application.StartupPath;
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(dir + Path.DirectorySeparatorChar + "settings.json"))
            {
                return LoadOrCreateNew(null);
            }

            try
            {
                FileInfo fileInfo = new(dir + Path.DirectorySeparatorChar + "settings.json");
                return LoadOrCreateNew(fileInfo);
            }
            catch (Exception e)
            {
                _logger.Error($"Error loading settings: {e}");
                return LoadOrCreateNew(null);
            }

        }

        /// <summary>
        /// Loads Settings of a given file or creates new settings if the file does not exist
        /// </summary>
        /// <param name="fileInfo">file that has settings saved</param>
        /// <returns>loaded or created settings</returns>
        private Settings LoadOrCreateNew(FileInfo fileInfo)
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

                settings.Preferences.toolEntries ??= [];

                settings.Preferences.columnizerMaskList ??= [];

                settings.fileHistoryList ??= [];

                settings.lastOpenFilesList ??= [];

                settings.fileColors ??= [];

                if (settings.Preferences.showTailColor == Color.Empty)
                {
                    settings.Preferences.showTailColor = Color.FromKnownColor(KnownColor.Blue);
                }

                if (settings.Preferences.timeSpreadColor == Color.Empty)
                {
                    settings.Preferences.timeSpreadColor = Color.Gray;
                }

                if (settings.Preferences.bufferCount < 10)
                {
                    settings.Preferences.bufferCount = 100;
                }

                if (settings.Preferences.linesPerBuffer < 1)
                {
                    settings.Preferences.linesPerBuffer = 500;
                }

                settings.filterList ??= [];

                settings.searchHistoryList ??= [];

                settings.filterHistoryList ??= [];

                settings.filterRangeHistoryList ??= [];

                foreach (FilterParams filterParams in settings.filterList)
                {
                    filterParams.Init();
                }

                if (settings.Preferences.HighlightGroupList == null)
                {
                    settings.Preferences.HighlightGroupList = [];
                }

                settings.Preferences.highlightMaskList ??= [];

                if (settings.Preferences.pollingInterval < 20)
                {
                    settings.Preferences.pollingInterval = 250;
                }

                settings.Preferences.multiFileOptions ??= new MultiFileOptions();

                settings.Preferences.defaultEncoding ??= Encoding.Default.HeaderName;

                if (settings.Preferences.maximumFilterEntriesDisplayed == 0)
                {
                    settings.Preferences.maximumFilterEntriesDisplayed = 20;
                }

                if (settings.Preferences.maximumFilterEntries == 0)
                {
                    settings.Preferences.maximumFilterEntries = 30;
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
        private void Save(Settings settings, SettingsFlags flags)
        {
            lock (_loadSaveLock)
            {
                _logger.Info("Saving settings");
                lock (this)
                {
                    string dir = Settings.Preferences.PortableMode ? Application.StartupPath : ConfigDir;

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    FileInfo fileInfo = new(dir + Path.DirectorySeparatorChar + "settings.json");
                    Save(fileInfo, settings);
                }

                OnConfigChanged(flags);
            }
        }

        /// <summary>
        /// Saves the file in any defined format
        /// </summary>
        /// <param name="fileInfo">FileInfo for creating the file (if exists will be overwritten)</param>
        /// <param name="settings">Current Settings</param>
        private void Save(FileInfo fileInfo, Settings settings)
        {
            //Currently only fileFormat, maybe add some other formats later (YAML or XML?)
            SaveAsJSON(fileInfo, settings);
        }

        private void Save(FileInfo fileInfo, Settings settings, SettingsFlags flags)
        {
            switch (flags)
            {
                case SettingsFlags.HighlightSettings:
                    SaveHighlightgroupsAsJSON(fileInfo, settings.Preferences.HighlightGroupList);
                    break;
            }

            OnConfigChanged(flags);
        }

        private static void SaveAsJSON(FileInfo fileInfo, Settings settings)
        {
            settings.versionBuild = Assembly.GetExecutingAssembly().GetName().Version.Build;

            using StreamWriter sw = new(fileInfo.Create());
            JsonSerializer serializer = new();
            serializer.Serialize(sw, settings);
        }

        private static void SaveHighlightgroupsAsJSON(FileInfo fileInfo, List<HighlightGroup> groups)
        {
            using StreamWriter sw = new(fileInfo.Create());
            JsonSerializer serializer = new();
            serializer.Serialize(sw, groups);
        }

        private List<HighlightGroup> Import(List<HighlightGroup> currentGroups, FileInfo fileInfo, ExportImportFlags flags)
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
        private Settings Import(Settings currentSettings, FileInfo fileInfo, ExportImportFlags flags)
        {
            Settings importSettings = LoadOrCreateNew(fileInfo);
            Settings ownSettings = ObjectClone.Clone(currentSettings);
            Settings newSettings;

            // at first check for 'Other' as this are the most options.
            if ((flags & ExportImportFlags.Other) == ExportImportFlags.Other)
            {
                newSettings = ownSettings;
                newSettings.Preferences = ObjectClone.Clone(importSettings.Preferences);
                newSettings.Preferences.columnizerMaskList = ownSettings.Preferences.columnizerMaskList;
                newSettings.Preferences.highlightMaskList = ownSettings.Preferences.highlightMaskList;
                newSettings.Preferences.HighlightGroupList = ownSettings.Preferences.HighlightGroupList;
                newSettings.Preferences.toolEntries = ownSettings.Preferences.toolEntries;
            }
            else
            {
                newSettings = ownSettings;
            }

            if ((flags & ExportImportFlags.ColumnizerMasks) == ExportImportFlags.ColumnizerMasks)
            {
                newSettings.Preferences.columnizerMaskList = ReplaceOrKeepExisting(flags, ownSettings.Preferences.columnizerMaskList, importSettings.Preferences.columnizerMaskList);
            }
            if ((flags & ExportImportFlags.HighlightMasks) == ExportImportFlags.HighlightMasks)
            {
                newSettings.Preferences.highlightMaskList = ReplaceOrKeepExisting(flags, ownSettings.Preferences.highlightMaskList, importSettings.Preferences.highlightMaskList);
            }
            if ((flags & ExportImportFlags.HighlightSettings) == ExportImportFlags.HighlightSettings)
            {
                newSettings.Preferences.HighlightGroupList = ReplaceOrKeepExisting(flags, ownSettings.Preferences.HighlightGroupList, importSettings.Preferences.HighlightGroupList);
            }
            if ((flags & ExportImportFlags.ToolEntries) == ExportImportFlags.ToolEntries)
            {
                newSettings.Preferences.toolEntries = ReplaceOrKeepExisting(flags, ownSettings.Preferences.toolEntries, importSettings.Preferences.toolEntries);
            }

            return newSettings;
        }

        private static List<T> ReplaceOrKeepExisting<T>(ExportImportFlags flags, List<T> existingList, List<T> newList)
        {
            if ((flags & ExportImportFlags.KeepExisting) == ExportImportFlags.KeepExisting)
            {
                return existingList.Union(newList).ToList();
            }

            return newList;
        }

        // Checking if the appBounds values are outside the current virtual screen.
        // If so, the appBounds values are set to 0.
        private void SetBoundsWithinVirtualScreen(Settings settings)
        {
            var vs = SystemInformation.VirtualScreen;
            if (vs.X + vs.Width < settings.appBounds.X + settings.appBounds.Width ||
                vs.Y + vs.Height < settings.appBounds.Y + settings.appBounds.Height)
            {
                settings.appBounds = new Rectangle();
            }
        }
        #endregion

        protected void OnConfigChanged(SettingsFlags flags)
        {
            ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(flags));
        }

        public delegate void ConfigChangedEventHandler(object sender, ConfigChangedEventArgs e);
    }
}