using System.Drawing;

using LogExpert.Core.Config;
using LogExpert.Core.EventArguments;

namespace LogExpert.Core.Interfaces;

/// <summary>
/// Manages application configuration settings including loading, saving, importing, and exporting.
/// Provides centralized access to application settings with automatic backup/recovery and validation.
/// </summary>
/// <remarks>
/// This interface defines the contract for configuration management in LogExpert.
/// Implementations use a singleton pattern and require explicit initialization via <see cref="Initialize"/>
/// before accessing any settings. The manager handles JSON serialization, backup file creation,
/// corruption recovery, and thread-safe operations.
/// </remarks>
public interface IConfigManager
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    /// <remarks>
    /// Settings are lazy-loaded on first access. The manager must be initialized via
    /// <see cref="Initialize"/> before accessing this property.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if accessed before initialization.</exception>
    Settings Settings { get; }

    /// <summary>
    /// Gets the directory path for portable mode settings.
    /// </summary>
    /// <remarks>
    /// Returns the application startup path combined with "portable" subdirectory.
    /// When a portableMode.json file exists in this directory, the application runs in portable mode.
    /// </remarks>
    [Obsolete("Use PortableConfigDir instead. This property is misnamed and may cause confusion. It will be removed in a future version.")]
    string PortableModeDir { get; }

    /// <summary>
    /// Gets the directory path for portable mode settings.
    /// </summary>
    /// <remarks>
    /// Returns the application startup path combined with "portable" subdirectory.
    /// When a portableMode.json file exists in this directory, the application runs in portable mode.
    /// </remarks>
    string PortableConfigDir { get; }

    /// <summary>
    /// Gets the standard configuration directory path.
    /// </summary>
    /// <remarks>
    /// Returns the path to the AppData\Roaming\LogExpert directory where settings are stored
    /// when not running in portable mode.
    /// </remarks>
    string ConfigDir { get; }

    /// <summary>
    /// Gets the filename for the portable mode indicator file.
    /// </summary>
    /// <value>Returns "portableMode.json"</value>
    string PortableModeSettingsFileName { get; }

    /// <summary>
    /// {ApplicationStartupPath}/configuration/sessions/<br></br>
    /// Used for session file storage in portable mode.
    /// </summary>
    string PortableSessionDir { get; }

    /// <summary>
    /// Gets the directory path where the current session's data is stored.
    /// </summary>
    /// <remarks>This property is useful for accessing files or configurations that are specific to the active
    /// session. The returned path may vary between sessions and should not be assumed to be persistent across
    /// application restarts.</remarks>
    string ActiveSessionDir { get; }

    /// <summary>
    /// Initializes the ConfigManager with application-specific paths and screen information.
    /// This method must be called once before accessing Settings or other configuration.
    /// </summary>
    /// <param name="applicationStartupPath">The application startup path (e.g., Application.StartupPath)</param>
    /// <param name="virtualScreenBounds">The virtual screen bounds (e.g., SystemInformation.VirtualScreen)</param>
    /// <remarks>
    /// This method should be called early in the application startup sequence, before any
    /// settings access. Subsequent calls are ignored with a warning logged.
    /// The virtual screen bounds are used to validate and correct window positions.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if applicationStartupPath is null or whitespace.</exception>
    void Initialize (string applicationStartupPath, Rectangle virtualScreenBounds);

    /// <summary>
    /// Exports specific settings to a file based on the provided flags.
    /// </summary>
    /// <param name="fileInfo">The file to export settings to. Will be created or overwritten.</param>
    /// <param name="highlightSettings">Flags indicating which settings to export (e.g., SettingsFlags.HighlightSettings)</param>
    /// <remarks>
    /// Currently only supports exporting highlight settings. Other flags may be ignored.
    /// The file is written in JSON format.
    /// </remarks>
    /// <exception cref="IOException">Thrown if the file cannot be written.</exception>
    void Export (FileInfo fileInfo, SettingsFlags highlightSettings);

    /// <summary>
    /// Exports all current settings to a file.
    /// </summary>
    /// <param name="fileInfo">The file to export settings to. Will be created or overwritten.</param>
    /// <remarks>
    /// Exports the complete settings object including preferences, filters, history, and highlights.
    /// A backup (.bak) file is created if the target file already exists.
    /// </remarks>
    /// <exception cref="IOException">Thrown if the file cannot be written.</exception>
    /// <exception cref="InvalidOperationException">Thrown if settings validation fails.</exception>
    void Export (FileInfo fileInfo);

    /// <summary>
    /// Imports settings from a file with validation and user confirmation support.
    /// </summary>
    /// <param name="fileInfo">The file to import settings from. Must exist.</param>
    /// <param name="importFlags">Flags controlling which parts of settings to import and how to merge them</param>
    /// <returns>
    /// An <see cref="ImportResult"/> indicating success, failure, or need for user confirmation.
    /// Check <see cref="ImportResult.Success"/> and <see cref="ImportResult.RequiresUserConfirmation"/> to determine the outcome.
    /// </returns>
    /// <remarks>
    /// This method validates the import file before applying changes. It detects corrupted files,
    /// empty/default settings, and handles backup recovery. If the import file appears empty,
    /// it returns a result requiring user confirmation to prevent accidental data loss.
    /// The current settings are only modified if the import is successful.
    /// </remarks>
    ImportResult Import (FileInfo fileInfo, ExportImportFlags importFlags);

    /// <summary>
    /// Imports only highlight settings from a file.
    /// </summary>
    /// <param name="fileInfo">The file containing highlight settings to import. Must exist and contain valid highlight groups.</param>
    /// <param name="importFlags">Flags controlling whether to keep existing highlights or replace them</param>
    /// <remarks>
    /// This is a specialized import method for highlight configurations. If <see cref="ExportImportFlags.KeepExisting"/>
    /// is set, imported highlights are added to existing ones; otherwise, existing highlights are replaced.
    /// Changes are saved immediately after import.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if fileInfo is null.</exception>
    void ImportHighlightSettings (FileInfo fileInfo, ExportImportFlags importFlags);

    /// <summary>
    /// Occurs when configuration settings are changed and saved.
    /// </summary>
    /// <remarks>
    /// This event is raised after settings are successfully saved, allowing UI components and other
    /// parts of the application to respond to configuration changes. The event args include
    /// <see cref="SettingsFlags"/> indicating which settings were modified.
    /// </remarks>
    event EventHandler<ConfigChangedEventArgs> ConfigChanged; //TODO: All handlers that are public shoulld be in Core

    /// <summary>
    /// Saves the current settings with the specified flags.
    /// </summary>
    /// <param name="flags">Flags indicating which parts of settings have changed and should be saved</param>
    /// <remarks>
    /// This method saves settings to disk with automatic backup creation. A temporary file is used
    /// during the write operation to prevent corruption. The previous settings file is backed up
    /// as .bak before being replaced. After successful save, the <see cref="ConfigChanged"/> event is raised.
    /// Settings validation is performed before saving to prevent data loss.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if settings validation fails.</exception>
    /// <exception cref="IOException">Thrown if the file cannot be written.</exception>
    void Save (SettingsFlags flags);

    /// <summary>
    /// Adds the specified file name to the file history list, moving it to the top if it already exists.
    /// </summary>
    /// <remarks>If the file name already exists in the history, it is moved to the top of the list. The file
    /// history list is limited to a maximum number of entries; the oldest entries are removed if the limit is exceeded.
    /// This method is supported only on Windows platforms.</remarks>
    /// <param name="fileName">The name of the file to add to the file history list. Comparison is case-insensitive.</param>
    void AddToFileHistory (string fileName);
    
    /// <summary>
    /// Removes the specified file name from the file history list.
    /// </summary>
    /// <param name="fileName">The name of the file to remove from the file history list. Comparison is case-insensitive.</param>
    void RemoveFromFileHistory (string fileName);

    /// <summary>
    /// Removes all entries from the file history list.
    /// </summary>
    /// <remarks>Only the file history list is cleared — the other history lists (search, filter, filter
    /// range, URI, regex, columnizer) keep their entries. The settings are saved immediately; because
    /// the save writes the whole settings file, the flag passed along names the change rather than
    /// narrowing what is written.</remarks>
    void ClearFileHistory ();

    /// <summary>
    /// Clears the list of recently opened files.
    /// </summary>
    /// <remarks>Call this method to remove all entries from the recent files list, typically to reset user
    /// history or in response to a privacy-related action. After calling this method, the list of last open files will
    /// be empty until new files are opened.</remarks>
    void ClearLastOpenFilesList ();

    /// <summary>
    /// Returns the active configuration directory based on the current mode.
    /// In portable mode: {AppDir}/configuration/
    /// In normal mode: %APPDATA%/LogExpert/
    /// </summary>
    string ActiveConfigDir { get; }

    /// <summary>
    /// Copies configuration files from normal mode location (%APPDATA%/LogExpert/)
    /// to the portable configuration directory ({AppDir}/configuration/).
    /// Called when portable mode is activated and user confirms copy.
    /// </summary>
    void CopyConfigToPortable ();

    /// <summary>
    /// Moves configuration files from the portable directory ({AppDir}/configuration/)
    /// back to normal mode locations (%APPDATA%/LogExpert/).
    /// Called when portable mode is deactivated and user confirms migration.
    /// </summary>
    void MoveConfigFromPortable ();
}