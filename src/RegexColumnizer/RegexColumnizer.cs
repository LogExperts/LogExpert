using System.Globalization;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

using ColumnizerLib;

using LogExpert.Core.Helpers;

using Newtonsoft.Json;

[assembly: SupportedOSPlatform("windows")]
namespace RegexColumnizer;

/// <summary>
/// Provides a base class for columnizing log lines using regular expressions, supporting configurable column
/// definitions and integration with log line memory interfaces.
/// </summary>
/// <remarks>This abstract class implements core logic for splitting log lines into columns based on regular
/// expression group matches. It supports configuration loading and saving, and is intended to be extended by concrete
/// columnizer implementations. The class ensures that columnized output always matches the expected column count, and
/// provides mechanisms for both memory-efficient and string-based log line processing. Thread safety is not guaranteed;
/// instances should not be shared across threads without external synchronization.</remarks>
public abstract class BaseRegexColumnizer : ILogLineMemoryColumnizer, IColumnizerConfiguratorMemory, ICloneable
{
    #region Fields

    private readonly XmlSerializer _xml = new(typeof(RegexColumnizerConfig));
    private string[] _columns;
    private RegexColumnizerConfig _config;

    // Seam for error notification.
    // Defaults to a modal dialog, in tests replaced so they can assert without blocking on a headless runner.
    internal Action<string, string, MessageBoxIcon> ShowError { get; set; } = static (message, title, icon) => MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
    #endregion

    #region Properties

    public Regex Regex { get; private set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Gets the configured name, or a default name if no configuration is set.
    /// </summary>
    /// <returns>A string containing the configured name if available; otherwise, a default name.</returns>
    public string GetName ()
    {
        return string.IsNullOrWhiteSpace(_config?.Name)
            ? GetNameInternal()
            : _config.Name;
    }

    /// <summary>
    /// Gets the custom name if specified; otherwise, returns the default name.
    /// </summary>
    /// <returns>A string containing the custom name if it is set and not empty; otherwise, the default name.</returns>
    public string GetCustomName ()
    {
        return string.IsNullOrWhiteSpace(_config?.CustomName)
            ? GetNameInternal()
            : _config.CustomName;
    }

    /// <summary>
    /// Gets a localized description of the regular expression columnizer.
    /// </summary>
    /// <returns>A string containing the localized description text for the regular expression columnizer.</returns>
    public string GetDescription () => Resources.RegexColumnizer_Description;

    /// <summary>
    /// Gets the number of columns in the collection.
    /// </summary>
    /// <returns>The total number of columns contained in the collection.</returns>
    public int GetColumnCount () => _columns.Length;

    /// <summary>
    /// Returns the names of all columns in the current schema.
    /// </summary>
    /// <returns>An array of strings containing the names of the columns. The array will be empty if no columns are defined.</returns>
    public string[] GetColumnNames () => _columns;

    /// <summary>
    /// Returns a slice of the specified memory corresponding to the matched group, or an empty memory if the group was
    /// not successful or has zero length.
    /// </summary>
    /// <remarks>This method avoids allocating a new string by returning a memory slice over the original
    /// input. The returned memory is valid as long as the underlying memory of <paramref name="lineMemory"/> remains
    /// valid.</remarks>
    /// <param name="lineMemory">The memory region containing the original input text from which the group was matched.</param>
    /// <param name="group">The regular expression group whose matched value is to be extracted from the memory. The group must have been
    /// matched against the input represented by <paramref name="lineMemory"/>.</param>
    /// <returns>A <see cref="ReadOnlyMemory{Char}"/> representing the portion of <paramref name="lineMemory"/> matched by
    /// <paramref name="group"/>. Returns <see cref="ReadOnlyMemory{Char}.Empty"/> if the group was not successful or
    /// has zero length.</returns>
    private static ReadOnlyMemory<char> GetGroupMemory (ReadOnlyMemory<char> lineMemory, Group group)
    {
        if (!group.Success || group.Length == 0)
        {
            return ReadOnlyMemory<char>.Empty;
        }

        // Use group's Index and Length to slice original memory
        // This avoids allocating a new string for the group value
        return lineMemory.Slice(group.Index, group.Length);
    }

    /// <summary>
    /// Splits the specified log line into columns according to the configured columnization logic.
    /// </summary>
    /// <remarks>If the log line does not match the configured regular expression, the entire line is placed
    /// in the last column and other columns are set to empty. The method ensures that the returned object always
    /// contains the expected number of columns, avoiding null values in the column array.</remarks>
    /// <param name="callback">A callback interface used to provide additional context or services during columnization. Cannot be null.</param>
    /// <param name="logLine">The log line to be split into columns. Cannot be null.</param>
    /// <returns>An object representing the columnized version of the input log line, with each column containing its
    /// corresponding value.</returns>
    public IColumnizedLogLineMemory SplitLine (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        ArgumentNullException.ThrowIfNull(logLine, nameof(logLine));
        ArgumentNullException.ThrowIfNull(callback, nameof(callback));

        var columnizedLogLine = new ColumnizedLogLine
        {
            LogLine = logLine
        };

        if (Regex != null)
        {
            if (Regex.IsMatch(logLine.FullLine.Span))
            {
                // To extract regex group captures, we must convert to string.
                // This is an unavoidable allocation - .NET Regex doesn't provide
                // a way to get group capture positions from ReadOnlySpan<char>.
                // However, GetGroupMemory() will slice the original ReadOnlyMemory,
                // so we avoid allocating strings for each captured group.
                var lineString = logLine.FullLine.ToString();
                var match = Regex.Match(lineString);

                var groupCount = Math.Min(match.Groups.Count - 1, _columns.Length);

                var cols = Column.CreateColumns(_columns.Length, columnizedLogLine);

                for (var i = 0; i < groupCount; i++)
                {
                    cols[i].FullValue = GetGroupMemory(logLine.FullLine, match.Groups[i + 1]);
                }

                columnizedLogLine.ColumnValues = [.. cols.Select(c => c as IColumnMemory)];
            }
            else
            {
                //Fill other columns with empty string to avoid null pointer exceptions in unexpected places
                var columns = Column.CreateColumns(_columns.Length, columnizedLogLine);

                // Set last column to full line
                columns[^1].FullValue = logLine.FullLine;

                //Move non matching lines in the last column
                columnizedLogLine.ColumnValues = [.. columns.Select(c => c as IColumnMemory)];
            }
        }
        else //Regex is null, just put the full line in the first column
        {
            var cols = Column.CreateColumns(_columns.Length, columnizedLogLine);
            cols[0].FullValue = logLine.FullLine;

            columnizedLogLine.ColumnValues = [.. cols.Select(c => c as IColumnMemory)];
        }

        return columnizedLogLine;
    }

    /// <summary>
    /// Determines whether timeshift functionality is implemented.
    /// </summary>
    /// <returns><see langword="true"/> if timeshift is implemented; otherwise, <see langword="false"/>.</returns>
    public bool IsTimeshiftImplemented () => false;

    /// <summary>
    /// Sets the time offset, in milliseconds, to be applied to time calculations or operations.
    /// </summary>
    /// <param name="msecOffset">The time offset, in milliseconds. Positive values indicate a forward offset; negative values indicate a backward
    /// offset.</param>
    /// <exception cref="NotImplementedException">The method is not implemented.</exception>
    public void SetTimeOffset (int msecOffset)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the time offset, in seconds, between the local system time and Coordinated Universal Time (UTC).
    /// </summary>
    /// <returns>The number of seconds that the local time is offset from UTC. A positive value indicates the local time is ahead
    /// of UTC; a negative value indicates it is behind.</returns>
    /// <exception cref="NotImplementedException">Thrown in all cases. This method is not implemented.</exception>
    public int GetTimeOffset ()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Loads the configuration for the columnizer from the specified directory, using either a JSON or XML
    /// configuration file if available.
    /// </summary>
    /// <remarks>If both JSON and XML configuration files are missing or invalid, a default configuration is
    /// created. The method displays an error message if deserialization fails.</remarks>
    /// <param name="configDir">The path to the directory containing the configuration files. Must not be null or empty.</param>
    public void LoadConfig (string configDir)
    {
        var configFile = GetConfigFileJSON(configDir);

        if (!File.Exists(configFile))
        {
            configFile = GetConfigFileXML(configDir);

            if (!File.Exists(configFile))
            {
                _config = new RegexColumnizerConfig
                {
                    Name = GetName()
                };
            }
            else
            {
                try
                {
                    using var reader = new StreamReader(configFile);
                    _config = _xml.Deserialize(reader) as RegexColumnizerConfig;
                }
                catch (Exception ex) when (ex is InvalidOperationException or
                                                 IOException or
                                                 ArgumentException or
                                                 ArgumentNullException or
                                                 FileNotFoundException or
                                                 DirectoryNotFoundException)
                {
                    ShowError(ex.Message, Resources.RegexColumnizer_UI_Title_Deserialize, MessageBoxIcon.Error);
                    _config = new RegexColumnizerConfig
                    {
                        Name = GetName()
                    };
                }
            }
        }
        else
        {
            try
            {
                string jsonContent = File.ReadAllText(configFile);

                _config = JsonConvert.DeserializeObject<RegexColumnizerConfig>(jsonContent) ?? new RegexColumnizerConfig { Name = GetName() };

            }
            catch (JsonException ex)
            {
                ShowError(ex.Message, Resources.RegexColumnizer_UI_Title_Deserialize, MessageBoxIcon.Error);
                _config = new RegexColumnizerConfig
                {
                    Name = GetName()
                };
            }
        }

        Init();
    }

    /// <summary>
    /// Validates that the columnizer name contains no path separators or invalid characters
    /// to prevent path traversal attacks (SEC-02)
    /// </summary>
    private static void ValidateColumnizerName (string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(Resources.RegexColumnizer_Error_Message_ColumnizerNameCannotBeNullOrEmpty);
        }

        // Check for path separators (both Windows and Unix)
        if (name.Contains(Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            name.Contains(Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            name.Contains('/', StringComparison.OrdinalIgnoreCase) ||
            name.Contains('\\', StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(Resources.RegexColumnizer_Error_Message_ColumnizerNameNameContainsPathSeparatorsWhichAreNotAllowed);
        }

        // Check for invalid filename characters
        char[] invalidChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalidChars) >= 0)
        {
            throw new InvalidOperationException(Resources.RegexColumnizer_Error_Message_ColumnizerNameNameContainsInvalidFilenameCharacters);
        }

        // Check for path traversal patterns
        if (name.Contains("..", StringComparison.OrdinalIgnoreCase) || name.Contains('~', StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(Resources.RegexColumnizer_Error_Message_ColumnizerNameNameContainsPathTraversalPatternsWhichAreNotAllowed);
        }
    }

    [Obsolete("XML Configuration is deprecated, use JSON instead")]
    private string GetConfigFileXML (string configDir)
    {
        var name = GetType().Name;
        ValidateColumnizerName(name);

        var configPath = Path.Join(configDir, name);
        configPath = Path.ChangeExtension(configPath, "xml");
        return configPath;
    }

    private string GetConfigFileJSON (string configDir)
    {
        var name = GetType().Name;
        ValidateColumnizerName(name);

        var configPath = Path.Join(configDir, name);
        configPath = Path.ChangeExtension(configPath, "json");
        return configPath;
    }

    #endregion

    /// <summary>
    /// ToString, this is displayed in the columnizer picker combobox only in the FilterSelectionDialog
    /// </summary>
    /// <returns></returns>
    public override string ToString ()
    {
        return GetName();
    }

    public object Clone ()
    {
        BaseRegexColumnizer clone = Activator.CreateInstance(GetType()) as BaseRegexColumnizer
            ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                Resources.RegexColumnizer_Error_Message_SnapshotUnavailable, GetName()));

        clone._config = new RegexColumnizerConfig
        {
            Expression = _config.Expression,
            Name = _config.Name,
            CustomName = _config.CustomName
        };
        clone.ShowError = ShowError;
        clone.Init();
        return clone;
    }

    #region Private Methods

    protected abstract string GetNameInternal ();

    public void Init ()
    {
        try
        {
            Regex = RegexHelper.GetOrCreateCached(_config.Expression, RegexOptions.Compiled);
            var skip = Regex.GetGroupNames().Length == 1
                ? 0
                : 1;

            _columns = [.. Regex.GetGroupNames().Skip(skip)];
        }
        catch (Exception ex) when (ex is ArgumentException or
                                         ArgumentNullException or
                                         OverflowException or
                                         RegexParseException)
        {
            Regex = null;
            _columns = ["text"];
        }
    }

    /// <summary>
    /// Retrieves the timestamp associated with the specified log line.
    /// </summary>
    /// <param name="callback">An object that provides callback methods for columnizer operations. Used to access additional context or
    /// services required during timestamp extraction.</param>
    /// <param name="logLine">The log line from which to extract the timestamp. Cannot be null.</param>
    /// <returns>A DateTime value representing the timestamp of the specified log line.</returns>
    /// <exception cref="NotImplementedException">The method is not implemented.</exception>
    public DateTime GetTimestamp (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Notifies the callback of a value change for a specific column.
    /// </summary>
    /// <param name="callback">The callback interface to receive the value change notification. Cannot be null.</param>
    /// <param name="column">The zero-based index of the column whose value has changed.</param>
    /// <param name="value">The new value to be associated with the specified column.</param>
    /// <param name="oldValue">The previous value of the specified column before the change.</param>
    /// <exception cref="NotImplementedException">The method is not implemented.</exception>
    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, string oldValue)
    {
        throw new NotImplementedException();
    }

    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, ReadOnlyMemory<char> oldValue)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Displays a configuration dialog for the columnizer and saves the updated settings to the specified configuration
    /// directory.
    /// </summary>
    /// <remarks>If the specified configuration directory does not exist, the method attempts to create it. If
    /// the user cancels the configuration dialog, no changes are made. Any errors encountered during directory creation
    /// or file saving are displayed to the user via a message box. The callback parameter is not used in the current
    /// implementation.</remarks>
    /// <param name="callback">A callback interface for columnizer memory operations. This parameter is reserved for future use and can be
    /// null.</param>
    /// <param name="configDir">The path to the directory where the configuration file will be stored. Cannot be null, empty, or consist only of
    /// white-space characters.</param>
    /// <exception cref="ArgumentException">Thrown if configDir is null, empty, or consists only of white-space characters.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the columnizer name is null or empty.</exception>
    public void Configure (ILogLineMemoryColumnizerCallback callback, string configDir)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(configDir))
        {
            throw new ArgumentException(Resources.RegexColumnizer_Configuration_DirectoryCannotBeNullOrEmpty, nameof(configDir));
        }

        string name = GetName();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(Resources.RegexColumnizer_Error_Message_ColumnizerNameCannotBeNullOrEmpty);
        }

        // Ensure directory exists
        if (!Directory.Exists(configDir))
        {
            try
            {
                _ = Directory.CreateDirectory(configDir);
            }
            catch (Exception ex) when (ex is IOException or
                                             UnauthorizedAccessException)
            {
                ShowError(string.Format(CultureInfo.InvariantCulture, Resources.RegexColumnizer_UI_Message_FailedToCreateConfigurationDirectory, ex.Message),
                          Resources.RegexColumnizer_UI_Title_Error,
                          MessageBoxIcon.Error);
                return;
            }
        }

        string filePath = Path.Join(configDir, $"{name}Columnizer.json");

        _config ??= new RegexColumnizerConfig
        {
            Name = GetName()
        };

        RegexColumnizerConfigDialog dlg = new(_config);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            try
            {
                // Only validate regex if expression is provided (empty is allowed and uses default)
                if (!string.IsNullOrWhiteSpace(dlg.Config.Expression))
                {
                    // Test regex compilation to catch errors early
                    _ = RegexHelper.CreateSafeRegex(dlg.Config.Expression);
                }

                // Save configuration
                string json = JsonConvert.SerializeObject(dlg.Config, Formatting.Indented);
                File.WriteAllText(filePath, json);

                _config = dlg.Config;
                Init();
            }
            catch (RegexMatchTimeoutException ex)
            {
                ShowError(string.Format(CultureInfo.InvariantCulture, Resources.RegexColumnizer_UI_Message_RegexTimeout, ex.Message), Resources.RegexColumnizer_UI_Title_Warning, MessageBoxIcon.Error);
            }
            catch (ArgumentException ex)
            {
                ShowError(string.Format(CultureInfo.InvariantCulture, Resources.RegexColumnizer_UI_Message_InvalidRegexPattern, ex.Message), Resources.RegexColumnizer_UI_Title_Error, MessageBoxIcon.Error);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ShowError(string.Format(CultureInfo.InvariantCulture, Resources.RegexColumnizer_UI_Message_FailedToSaveConfiguration, ex.Message), Resources.RegexColumnizer_UI_Title_Error, MessageBoxIcon.Error);
            }
        }
    }

    #endregion
}

#region RegexColumnizer Implementations

public class Regex1Columnizer : BaseRegexColumnizer
{
    protected override string GetNameInternal () => "Regex1";
}

public class Regex2Columnizer : BaseRegexColumnizer
{
    protected override string GetNameInternal () => "Regex2";
}

public class Regex3Columnizer : BaseRegexColumnizer
{
    protected override string GetNameInternal () => "Regex3";
}

public class Regex4Columnizer : BaseRegexColumnizer
{
    protected override string GetNameInternal () => "Regex4";
}

public class Regex5Columnizer : BaseRegexColumnizer
{
    protected override string GetNameInternal () => "Regex5";
}

public class Regex6Columnizer : BaseRegexColumnizer
{
    protected override string GetNameInternal () => "Regex6";
}

public class Regex7Columnizer : BaseRegexColumnizer
{
    protected override string GetNameInternal () => "Regex7";
}

public class Regex8Columnizer : BaseRegexColumnizer
{
    protected override string GetNameInternal () => "Regex8";
}

public class Regex9Columnizer : BaseRegexColumnizer
{
    protected override string GetNameInternal () => "Regex9";
}

#endregion