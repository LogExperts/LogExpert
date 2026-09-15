using ColumnizerLib;

using LogExpert.Core.Classes.Filter;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Services.FileOperationService;

namespace LogExpert.UI.Interface;

/// <summary>
/// Application-level Facade that orchestrates all file-loading coordination:
/// determining how to load files, checking for duplicates, managing file history,
/// resolving default encoding, creating LogWindows via an injected factory,
/// and persisting last-open file lists.
/// </summary>
/// <remarks>
/// <para>
/// This interface intentionally covers multiple related responsibilities (tab creation,
/// history, multi-file decisions, clipboard, drag-drop, startup persistence) as a single
/// Facade. Splitting into smaller interfaces is deferred until usage patterns stabilize.
/// </para>
/// <para>
/// <b>Thread affinity:</b> All public methods must be invoked on the UI thread.
/// The service does not marshal calls internally.
/// </para>
/// </remarks>
internal interface IFileOperationService
{
    /// <summary>
    /// Central file-tab creation method. Checks for duplicates, resolves encoding,
    /// creates LogWindow via factory, adds to history, raises FileOpened.
    /// </summary>
    LogWindow AddFileTab (FileTabRequest request);

    /// <summary>
    /// Convenience for AddFileTab with DoNotAddToDockPanel = true.
    /// Used by LoadSession for layout-restored windows.
    /// </summary>
    LogWindow AddFileTabDeferred (string fileName, bool isTempFile, string? title, bool forcePersistenceLoading, ILogLineMemoryColumnizer? preProcessColumnizer);

    /// <summary>
    /// Creates a filter-result tab. Sets up filter tooltip on the LogWindowData.
    /// </summary>
    LogWindow AddFilterTab (FilterPipe pipe, string title, ILogLineMemoryColumnizer? preProcessColumnizer);

    /// <summary>
    /// Creates a temp file tab. Convenience wrapper for AddFileTab with IsTempFile = true.
    /// </summary>
    LogWindow AddTempFileTab (string fileName, string title);

    /// <summary>
    /// Creates a multi-file tab.
    /// </summary>
    LogWindow? AddMultiFileTab (string[] fileNames);

    /// <summary>
    /// Public entry point that invokes AddFileTabs on the UI thread.
    /// </summary>
    void LoadFiles (string[] fileNames, int? targetLine = null);

    /// <summary>
    /// Resolves multi-file preference and returns a MultiFileDecision.
    /// Does NOT show dialogs. Returns AskUser when the preference is Ask.
    /// Sorts the file array for deterministic tab order.
    /// </summary>
    MultiFileDecision LoadFilesWithOption (string[] fileNames, bool invertLogic);

    /// <summary>
    /// Iterates file names; routes .lxj files to a callback, adds all others via AddFileTab.
    /// </summary>
    void AddFileTabs (string[] fileNames, int? targetLine = null);

    /// <summary>
    /// Creates temp file from clipboard text, calls AddTempFileTab.
    /// </summary>
    LogWindow? PasteFromClipboard ();

    /// <summary>
    /// Adds to ConfigManager history, raises FileHistoryChanged.
    /// </summary>
    void AddToFileHistory (string fileName);

    /// <summary>
    /// Persists currently open non-temp files to ConfigManager.
    /// </summary>
    void SaveLastOpenFilesList ();

    /// <summary>
    /// Loads startup files or last-open files. Startup files take precedence.
    /// </summary>
    void LoadStartupFiles (IList<string> lastOpenFiles, string[]? startupFileNames, int? targetLine = null);

    /// <summary>
    /// Duplicate-tab detection, delegates to TabController.
    /// </summary>
    LogWindow? FindWindowForFile (string fileName);

    /// <summary>
    /// Returns true if the data object contains DataFormats.FileDrop.
    /// </summary>
    bool CanHandleDrop (IDataObject data);

    /// <summary>
    /// Raised after AddToFileHistory. LogTabWindow subscribes to refresh the history menu.
    /// </summary>
    event EventHandler? FileHistoryChanged;

    /// <summary>
    /// Raised after a file tab is created. LogTabWindow subscribes for UI post-creation work.
    /// The event args carry the original FileTabRequest plus resolved metadata.
    /// </summary>
    event EventHandler<FileOpenedEventArgs>? FileOpened;
}