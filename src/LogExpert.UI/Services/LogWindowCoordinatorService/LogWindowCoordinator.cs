using System.Runtime.Versioning;
using System.Text.RegularExpressions;

using ColumnizerLib;

using LogExpert.Core.Classes;
using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Entities;
using LogExpert.UI.Interface;

using NLog;

namespace LogExpert.UI.Services.LogWindowCoordinatorService;

/// <summary>
/// Coordinates workspace-level operations for LogWindow instances.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class LogWindowCoordinator (
    IConfigManager configManager,
    IPluginRegistry pluginRegistry,
    Controls.LogTabWindow.LogTabWindow logTabWindow,
    ITabController tabController,
    ILedIndicatorService ledIndicatorService,
    IFileOperationService fileOperationService) : ILogWindowCoordinator
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly IConfigManager _configManager = configManager;
    private readonly IPluginRegistry _pluginRegistry = pluginRegistry;
    private readonly Controls.LogTabWindow.LogTabWindow _logTabWindow = logTabWindow;
    private readonly ITabController _tabController = tabController;
    private readonly ILedIndicatorService _ledIndicatorService = ledIndicatorService;
    private readonly IFileOperationService _fileOperationService = fileOperationService;
    private readonly Lock _highlightGroupLock = new();

    private const int DIFF_MAX = 100;

    public event EventHandler HighlightSettingsChanged;

    private List<HighlightGroup> HighlightGroups => _configManager.Settings.Preferences.HighlightGroupList;

    public SearchParams SearchParams { get; } = new SearchParams();

    /// <summary>
    /// Raises the HighlightSettingsChanged event.
    /// </summary>
    public void OnHighlightSettingsChanged ()
    {
        HighlightSettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public HighlightGroup ResolveHighlightGroup (string? groupName, string? fileName)
    {
        lock (_highlightGroupLock)
        {
            // Tier 1: File-mask regex match (if fileName is provided)
            if (fileName != null)
            {
                var maskMatch = FindHighlightGroupByFileMask(fileName);
                if (maskMatch != null)
                {
                    return maskMatch;
                }
            }

            // Tier 2: Name match (if groupName is provided)
            if (groupName != null)
            {
                var nameMatch = FindHighlightGroupByName(groupName);
                if (nameMatch != null)
                {
                    return nameMatch;
                }
            }

            // Tier 3: First group in the list
            if (HighlightGroups.Count > 0)
            {
                return HighlightGroups[0];
            }

            // Tier 4: New empty group (never returns null)
            return new HighlightGroup();
        }
    }

    private HighlightGroup? FindHighlightGroupByName (string groupName)
    {
        return HighlightGroups.FirstOrDefault(g => g.GroupName.Equals(groupName, StringComparison.Ordinal));
    }

    private HighlightGroup? FindHighlightGroupByFileMask (string fileName)
    {
        foreach (var entry in _configManager.Settings.Preferences.HighlightMaskList.Where(entry => entry.Mask != null))
        {
            try
            {
                if (Regex.IsMatch(fileName, entry.Mask))
                {
                    return FindHighlightGroupByName(entry.HighlightGroupName);
                }
            }
            catch (ArgumentException e)
            {
                _logger.Error($"RegEx-error while matching highlight mask: {e}");
            }
        }

        return null;
    }

    public ILogLineMemoryColumnizer? ResolveColumnizer (string fileName)
    {
        var preferences = _configManager.Settings.Preferences;
        var shortName = Util.GetNameFromPath(fileName);

        // History lookup also cleans up stale entries — preserve that side-effect by routing through
        // GetColumnizerHistoryEntry rather than a pure name lookup inside ColumnizerResolver.
        var historyHit = GetColumnizerHistoryEntry(fileName);

        var inputs = new ResolveInputs
        {
            Priority = preferences.ColumnizerSelectionPriority,
            FileName = fileName,
            ShortFileName = shortName,
            MaskList = preferences.ColumnizerMaskList,
            HistoryLookup = _ => historyHit?.GetName(),
            Registered = _pluginRegistry.RegisteredColumnizers,
            OnStaleMaskEntry = entry => _logger.Warn(
                $"Columnizer mask '{entry.Mask}' matched '{shortName}' but its columnizer '{entry.ColumnizerName}' is not registered — skipping."),
        };

        return ColumnizerResolver.Resolve(inputs);
    }

    public ILogLineMemoryColumnizer? TryGetMaskColumnizer (string shortFileName)
    {
        return ColumnizerResolver.TryGetMaskColumnizer(
            _configManager.Settings.Preferences.ColumnizerMaskList,
            shortFileName,
            _pluginRegistry.RegisteredColumnizers,
            entry => _logger.Warn(
                $"Columnizer mask '{entry.Mask}' matched '{shortFileName}' but its columnizer '{entry.ColumnizerName}' is not registered — skipping."));
    }

    private ILogLineMemoryColumnizer? GetColumnizerHistoryEntry (string fileName)
    {
        var historyEntry = FindColumnizerHistoryEntry(fileName);
        if (historyEntry != null)
        {
            var foundColumnizer = _pluginRegistry.RegisteredColumnizers.FirstOrDefault(c => c.GetName().Equals(historyEntry.ColumnizerName, StringComparison.Ordinal));

            if (foundColumnizer != null)
            {
                return foundColumnizer;
            }

            // Stale entry — columnizer name no longer registered. Remove it.
            _ = _configManager.Settings.ColumnizerHistoryList.Remove(historyEntry);
        }

        return null;
    }

    public ColumnizerHistoryEntry? FindColumnizerHistoryEntry (string fileName)
    {
        return _configManager.Settings.ColumnizerHistoryList.FirstOrDefault(entry => entry.FileName.Equals(fileName, StringComparison.Ordinal));
    }

    public LogWindow AddFilterTab (FilterPipe pipe, string title, ILogLineMemoryColumnizer? preProcessColumnizer)
    {
        return _fileOperationService.AddFilterTab(pipe, title, preProcessColumnizer);
    }

    public LogWindow AddTempFileTab (string fileName, string title)
    {
        return _fileOperationService.AddTempFileTab(fileName, title);
    }

    public void ScrollAllTabsToTimestamp (DateTime timestamp, LogWindow sender)
    {
        foreach (var logWindow in _tabController.GetAllWindows().Where(logWindow => logWindow != sender))
        {
            if (logWindow.ScrollToTimestamp(timestamp, false, false))
            {
                IndicateTimeSyncActivity(logWindow);
            }
        }
    }

    public void IndicateTimeSyncActivity (LogWindow logWindow)
    {
        _ledIndicatorService.UpdateWindowActivity(logWindow, DIFF_MAX, false);
    }

    public IList<WindowFileEntry> GetOpenFiles ()
    {
        IList<WindowFileEntry> list = [];

        foreach (var logWindow in _tabController.GetAllWindows())
        {
            list.Add(new WindowFileEntry(logWindow));
        }

        return list;
    }

    public void SelectTab (LogWindow logWindow)
    {
        _tabController.ActivateWindow(logWindow);
    }

    public void NotifyFollowTailChanged (LogWindow logWindow, bool isEnabled, bool offByTrigger)
    {
        _logTabWindow.FollowTailChanged(logWindow, isEnabled, offByTrigger);
    }
}