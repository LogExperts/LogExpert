using System.Runtime.Versioning;
using System.Text;

using ColumnizerLib;

using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Entities;
using LogExpert.Core.Helpers;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Interface;

using NLog;

namespace LogExpert.UI.Services.FileOperationService;

[SupportedOSPlatform("windows")]
internal sealed class FileOperationService (
    IConfigManager configManager,
    ITabController tabController,
    ILedIndicatorService ledIndicatorService,
    IPluginRegistry pluginRegistry,
    Func<FileTabRequest, EncodingOptions, LogWindow> logWindowFactory,
    Func<string?> clipboardTextProvider,
    Action<string, bool> loadSessionCallback) : IFileOperationService
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Whether a closing window has already replaced the persisted last-open-files list in this
    /// process. All LogTabWindows share one list, so the first window to close replaces it — dropping
    /// entries left over from earlier sessions — and every window closing after it appends, which
    /// restores a multi-window session as a whole.
    /// </summary>
    internal static bool LastOpenFilesListReplaced { get; set; }

    private readonly IConfigManager _configManager = configManager;
    private readonly ITabController _tabController = tabController;
    private readonly ILedIndicatorService _ledIndicatorService = ledIndicatorService;
    private readonly IPluginRegistry _pluginRegistry = pluginRegistry;
    private readonly Func<FileTabRequest, EncodingOptions, LogWindow> _logWindowFactory = logWindowFactory;
    private readonly Func<string?> _clipboardTextProvider = clipboardTextProvider;
    private readonly Action<string, bool> _loadSessionCallback = loadSessionCallback;

    public event EventHandler? FileHistoryChanged;
    public event EventHandler<FileOpenedEventArgs>? FileOpened;


    // TODO TabColor was part of older Versions, this is a feature that should be readded, sooner or later, but it is not a priority right now.
    //    var data = logWindow.Tag as LogWindowData;
    //    data.Color = _defaultTabColor;
    //    //TODO SetTabColor and the Coloring must be reimplemented with a different UI Framework
    //    //SetTabColor(logWindow, _defaultTabColor);
    //    //data.tabPage.BorderColor = this.defaultTabBorderColor;
    //    //if (!isTempFile)
    //    //{
    //    //    foreach (var colorEntry in ConfigManager.Settings.FileColors)
    //    //    {
    //    //        if (colorEntry.FileName.ToUpperInvariant().Equals(logFileName.ToUpperInvariant(), StringComparison.Ordinal))
    //    //        {
    //    //            data.Color = colorEntry.Color;
    //    //            //SetTabColor(logWindow, colorEntry.Color);
    //    //            break;
    //    //        }
    //    //    }
    //    //}

    public LogWindow AddFileTab (FileTabRequest request)
    {
        var logFileName = PersisterHelpers.FindFilenameForSettings(request.FileName, _pluginRegistry);
        var normalizedPath = Path.GetFullPath(logFileName);

        var existingWindow = FindWindowForFile(normalizedPath);
        if (existingWindow != null)
        {
            if (!request.IsTempFile)
            {
                AddToFileHistory(request.FileName);
            }

            _tabController.ActivateWindow(existingWindow);
            if (request.TargetLine is int existingTarget)
            {
                existingWindow.RequestGotoLine(existingTarget);
            }
            return existingWindow;
        }

        EncodingOptions encodingOptions = new();
        FillDefaultEncodingFromSettings(encodingOptions);

        if (request.IsTempFile)
        {
            encodingOptions.Encoding = new UnicodeEncoding(false, false);
        }

        var logWindow = _logWindowFactory(request, encodingOptions);

        if (!request.IsTempFile)
        {
            AddToFileHistory(request.FileName);
        }

        if (request.FileName.EndsWith(".lxp", StringComparison.Ordinal))
        {
            logWindow.ForcedPersistenceFileName = request.FileName;
        }

        // Register before starting the asynchronous load, including very small files.
        if (request.TargetLine is int targetLine)
        {
            logWindow.RequestGotoLine(targetLine);
        }

        _ = Task.Run(() => logWindow.LoadFile(logFileName, encodingOptions));

        FileOpened?.Invoke(this, new FileOpenedEventArgs
        {
            LogWindow = logWindow,
            Request = request,
            ResolvedFileName = logFileName,
            EncodingOptions = encodingOptions
        });

        return logWindow;
    }

    public LogWindow? FindWindowForFile (string fileName)
    {
        return _tabController.FindWindowByFileName(fileName);
    }

    public void AddToFileHistory (string fileName)
    {
        _configManager.AddToFileHistory(fileName);
        FileHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void FillDefaultEncodingFromSettings (EncodingOptions encodingOptions)
    {
        var configuredEncoding = _configManager.Settings.Preferences.DefaultEncoding;
        if (configuredEncoding == null)
        {
            return;
        }

        if (EncodingRegistry.TryGetEncoding(configuredEncoding, out var encoding))
        {
            encodingOptions.DefaultEncoding = encoding;
        }
        else
        {
            _logger.Warn($"### FillDefaultEncodingFromSettings: Encoding {configuredEncoding} is not a valid encoding");
            encodingOptions.DefaultEncoding = null;
        }
    }

    public LogWindow AddFilterTab (FilterPipe pipe, string title, ILogLineMemoryColumnizer? preProcessColumnizer)
    {
        var request = new FileTabRequest
        {
            FileName = pipe.FileName,
            IsTempFile = true,
            Title = title,
            PreProcessColumnizer = preProcessColumnizer
        };

        var logWindow = AddFileTab(request);

        // The filter-specific tooltip needs the pipe's FilterParams, which is not part of
        // the generic FileTabRequest. We raise FileOpened again with the FilterPipe attached
        // so LogTabWindow can set up the filter tooltip in OnFileOperationServiceFileOpened.
        // Note: AddFileTab already raised FileOpened once (for tab color/tooltip setup).
        // This second raise carries the FilterPipe for the filter-specific tooltip.
        if (pipe.FilterParams.SearchText?.Length > 0)
        {
            FileOpened?.Invoke(this, new FileOpenedEventArgs
            {
                LogWindow = logWindow,
                Request = request,
                ResolvedFileName = pipe.FileName,
                FilterPipe = pipe
            });
        }

        return logWindow;
    }

    public LogWindow AddTempFileTab (string fileName, string title)
    {
        return AddFileTab(new FileTabRequest
        {
            FileName = fileName,
            IsTempFile = true,
            Title = title
        });
    }

    public LogWindow? AddMultiFileTab (string[] fileNames)
    {
        if (fileNames.Length < 1)
        {
            return null;
        }

        // Session files (.lxj) aren't logs — dispatch them to the project loader and
        // continue with only the log files. Without this, the .lxj would be treated as
        // a raw log line in the multi-file tab.
        if (fileNames.Any(f => f.EndsWith(".lxj", StringComparison.OrdinalIgnoreCase)))
        {
            AddFileTabs(fileNames.Where(f => f.EndsWith(".lxj", StringComparison.OrdinalIgnoreCase)).ToArray());

            var logs = fileNames.Where(f => !f.EndsWith(".lxj", StringComparison.OrdinalIgnoreCase)).ToArray();
            return logs.Length > 0 ? AddMultiFileTab(logs) : null;
        }

        EncodingOptions encodingOptions = new();
        FillDefaultEncodingFromSettings(encodingOptions);

        var request = new FileTabRequest
        {
            FileName = fileNames[^1],
            IsTempFile = false
        };

        var logWindow = _logWindowFactory(request, encodingOptions);

        // BeginInvoke for LoadFilesAsMulti must be done by the caller (LogTabWindow)
        // since it requires a Control. The factory already added the window to the DockPanel.
        // We raise FileOpened with MultiFileNames so LogTabWindow can call BeginInvoke(logWindow.LoadFilesAsMulti, ...).
        AddToFileHistory(fileNames[0]);

        FileOpened?.Invoke(this, new FileOpenedEventArgs
        {
            LogWindow = logWindow,
            Request = request,
            ResolvedFileName = fileNames[^1],
            EncodingOptions = encodingOptions,
            MultiFileNames = fileNames
        });

        return logWindow;
    }

    public void LoadDroppedFiles (string[] selectedFiles, bool invertLogic, Func<MultiFileDecision> askUser)
    {
        if (selectedFiles.Length == 0)
        {
            return;
        }

        var decision = LoadFilesWithOption(selectedFiles, invertLogic);
        if (decision == MultiFileDecision.AskUser)
        {
            switch (askUser())
            {
                case MultiFileDecision.SingleFiles:
                    AddFileTabs(selectedFiles);
                    break;
                case MultiFileDecision.MultiFile:
                    _ = AddMultiFileTab(selectedFiles);
                    break;
            }
        }
    }
    public MultiFileDecision LoadFilesWithOption (string[] fileNames, bool invertLogic)
    {
        Array.Sort(fileNames, StringComparer.OrdinalIgnoreCase);

        if (fileNames.Length == 1)
        {
            if (fileNames[0].EndsWith(".lxj", StringComparison.OrdinalIgnoreCase))
            {
                _loadSessionCallback(fileNames[0], true);
                return MultiFileDecision.Cancel; // Already handled
            }

            _ = AddFileTab(new FileTabRequest { FileName = fileNames[0] });
            return MultiFileDecision.SingleFiles;
        }

        var option = _configManager.Settings.Preferences.MultiFileOption;

        if (option == Core.Config.MultiFileOption.Ask)
        {
            return MultiFileDecision.AskUser;
        }

        if (invertLogic)
        {
            option = option == Core.Config.MultiFileOption.SingleFiles
                ? Core.Config.MultiFileOption.MultiFile
                : Core.Config.MultiFileOption.SingleFiles;
        }

        if (option == Core.Config.MultiFileOption.SingleFiles)
        {
            AddFileTabs(fileNames);
            return MultiFileDecision.SingleFiles;
        }

        _ = AddMultiFileTab(fileNames);
        return MultiFileDecision.MultiFile;
    }

    public void AddFileTabs (string[] fileNames, int? targetLine = null)
    {
        foreach (var fileName in fileNames.Where(filename => !string.IsNullOrEmpty(filename)))
        {
            try
            {
                if (fileName.EndsWith(".lxj", StringComparison.OrdinalIgnoreCase))
                {
                    _loadSessionCallback(fileName, false);
                }
                else
                {
                    _ = AddFileTab(new FileTabRequest { FileName = fileName, TargetLine = targetLine });
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or LogFileException)
            {
                _logger.Warn(ex, "Could not open selected file: {0}", fileName);
            }
        }
    }

    public LogWindow? PasteFromClipboard ()
    {
        var text = _clipboardTextProvider();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var fileName = Path.GetTempFileName();

        using (FileStream fStream = new(fileName, FileMode.Append, FileAccess.Write, FileShare.Read))
        using (StreamWriter writer = new(fStream, Encoding.Unicode))
        {
            writer.Write(text);
            writer.Close();
        }

        var title = "Clipboard"; // LogTabWindow will use the resource string via FileOpened event
        return AddTempFileTab(fileName, title);
    }

    public bool CanHandleDrop (IDataObject data)
    {
        return data?.GetDataPresent(DataFormats.FileDrop) == true;
    }

    public LogWindow AddFileTabDeferred (string fileName, bool isTempFile, string? title, bool forcePersistenceLoading, ILogLineMemoryColumnizer? preProcessColumnizer)
    {
        return AddFileTab(new FileTabRequest
        {
            FileName = fileName,
            IsTempFile = isTempFile,
            Title = title,
            ForcePersistenceLoading = forcePersistenceLoading,
            PreProcessColumnizer = preProcessColumnizer,
            DoNotAddToDockPanel = true
        });
    }

    public void LoadFiles (string[] fileNames, int? targetLine = null)
    {
        AddFileTabs(fileNames, targetLine);
    }

    public void SaveLastOpenFilesList ()
    {
        // Nothing else clears the list, so the first window to close has to replace it: appending
        // unconditionally would keep entries from earlier sessions and grow the list without
        // bound. Windows closing after it append, so all of their files are restored.
        if (!LastOpenFilesListReplaced)
        {
            _configManager.ClearLastOpenFilesList();
            LastOpenFilesListReplaced = true;
        }

        foreach (var logWin in _tabController.GetAllWindowsFromDockPanel().Where(logwin => !logwin.IsTempFile))
        {
            _configManager.Settings.LastOpenFilesList.Add(logWin.GivenFileName);
        }
    }

    public void LoadStartupFiles (IList<string> lastOpenFiles, string[]? startupFileNames, int? targetLine = null)
    {
        if (startupFileNames != null && startupFileNames.Length > 0)
        {
            if (targetLine.HasValue)
            {
                AddFileTabs(startupFileNames, targetLine);
            }
            else
            {
                _ = LoadFilesWithOption(startupFileNames, false);
            }
            return;
        }

        if (_configManager.Settings.Preferences.OpenLastFiles)
        {
            foreach (var name in lastOpenFiles.Where(filename => !string.IsNullOrEmpty(filename)))
            {
                _ = AddFileTab(new FileTabRequest { FileName = name });
            }
        }
    }
}