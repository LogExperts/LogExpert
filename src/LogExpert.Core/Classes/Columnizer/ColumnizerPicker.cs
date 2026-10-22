using ColumnizerLib;

using LogExpert.Core.Entities;

namespace LogExpert.Core.Classes.Columnizer;

public static class ColumnizerPicker
{
    private const string AUTO_COLUMNIZER_NAME = "Auto Columnizer";

    /// <summary>
    /// Searches the specified list for a columnizer whose name matches the provided value using an ordinal string
    /// comparison.
    /// </summary>
    /// <remarks>If multiple columnizers in the list have the same name, only the first occurrence is
    /// returned. The comparison is case-sensitive and culture-insensitive.</remarks>
    /// <param name="name">The name of the columnizer to locate. The comparison is case-sensitive and uses ordinal string comparison.
    /// Cannot be null.</param>
    /// <param name="list">The list of available columnizers to search. Cannot be null.</param>
    /// <returns>The first columnizer from the list whose name matches the specified value; otherwise, null if no match is found.</returns>
    public static ILogLineMemoryColumnizer FindMemoryColumnizerByName (string name, IList<ILogLineMemoryColumnizer> list)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(list, nameof(list));

        foreach (var columnizer in list)
        {
            if (columnizer.GetName().Equals(name, StringComparison.Ordinal))
            {
                return columnizer;
            }
        }

        return null;
    }

    /// <summary>
    /// Selects an appropriate columnizer from the provided list based on the specified name.
    /// </summary>
    /// <remarks>If no columnizer in the list matches the specified name, a default columnizer is returned by
    /// calling FindColumnizer with null parameters. The search uses ordinal, case-sensitive comparison.</remarks>
    /// <param name="name">The name of the columnizer to select. Comparison is case-sensitive and uses ordinal comparison.</param>
    /// <param name="list">A list of available columnizers to search. Cannot be null.</param>
    /// <returns>The columnizer from the list whose name matches the specified name, or a default columnizer if no match is
    /// found.</returns>
    public static ILogLineMemoryColumnizer DecideMemoryColumnizerByName (string name, IList<ILogLineMemoryColumnizer> list)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(list, nameof(list));

        foreach (var columnizer in list)
        {
            if (columnizer.GetName().Equals(name, StringComparison.Ordinal))
            {
                return columnizer;
            }
        }

        return FindMemoryColumnizer(null, null, list);
    }

    /// <summary>
    /// Creates a new instance of the specified columnizer type and loads its configuration from the given directory.
    /// </summary>
    /// <remarks>The method requires that the columnizer type has a public parameterless constructor. If the
    /// type implements IColumnizerConfiguratorMemory, its configuration is loaded from the specified directory. If these
    /// conditions are not met, the method returns null.</remarks>
    /// <param name="columnizer">The columnizer instance whose type will be cloned. If null, the method returns null.</param>
    /// <param name="directory">The directory path from which to load the configuration for the new columnizer instance.</param>
    /// <returns>A new instance of the same type as the specified columnizer with its configuration loaded from the given
    /// directory, or null if the columnizer is null or cannot be cloned.</returns>
    public static ILogLineMemoryColumnizer CloneMemoryColumnizer (ILogLineMemoryColumnizer columnizer, string directory)
    {
        if (columnizer == null)
        {
            return null;
        }

        var cti = columnizer.GetType().GetConstructor(Type.EmptyTypes);

        if (cti != null)
        {
            var o = cti.Invoke([]);

            if (o is IColumnizerConfiguratorMemory configurator)
            {
                configurator.LoadConfig(directory);
            }

            return (ILogLineMemoryColumnizer)o;
        }

        return null;
    }

    /// <summary>
    /// Selects an appropriate log line columnizer for the specified file, replacing the auto columnizer if necessary.
    /// </summary>
    /// <remarks>If the provided columnizer is null or set to auto, this method attempts to find a suitable
    /// replacement based on the file and available columnizers. Otherwise, it returns the provided columnizer
    /// unchanged.</remarks>
    /// <param name="fileName">The path of the file for which to determine the appropriate columnizer. Cannot be null.</param>
    /// <param name="logFileReader">A callback interface used to read log file lines for columnizer selection. Cannot be null.</param>
    /// <param name="logLineColumnizer">The current columnizer to use, or null to indicate that a suitable columnizer should be selected automatically.</param>
    /// <param name="list">A list of available columnizers to consider when selecting a replacement. Cannot be null.</param>
    /// <returns>An instance of a log line memory columnizer appropriate for the specified file. Returns the provided columnizer
    /// unless it is null or set to auto; otherwise, returns a suitable replacement from the list.</returns>
    public static ILogLineMemoryColumnizer FindReplacementForAutoMemoryColumnizer (
        string fileName,
        IAutoLogLineMemoryColumnizerCallback logFileReader,
        ILogLineMemoryColumnizer logLineColumnizer,
        IList<ILogLineMemoryColumnizer> list)
    {
        return logLineColumnizer == null || logLineColumnizer.GetName() == AUTO_COLUMNIZER_NAME
            ? FindMemoryColumnizer(fileName, logFileReader, list)
            : logLineColumnizer;
    }

    /// <summary>
    /// Selects a more suitable columnizer for the specified file, if one is available.
    /// </summary>
    /// <param name="fileName">The path of the file for which to determine a better columnizer.</param>
    /// <param name="logFileReader">A callback interface used to read log file lines for columnizer evaluation.</param>
    /// <param name="logLineColumnizer">The current columnizer in use for the file. Cannot be null.</param>
    /// <param name="list">A list of available columnizers to consider when searching for a better match.</param>
    /// <returns>A columnizer that is better suited for the specified file than the current one, or null if no better columnizer
    /// is found.</returns>
    public static ILogLineMemoryColumnizer FindBetterMemoryColumnizer (
        string fileName,
        IAutoLogLineMemoryColumnizerCallback logFileReader,
        ILogLineMemoryColumnizer logLineColumnizer,
        IList<ILogLineMemoryColumnizer> list)
    {
        ArgumentNullException.ThrowIfNull(logLineColumnizer, nameof(logLineColumnizer));

        var newColumnizer = FindMemoryColumnizer(fileName, logFileReader, list);

        return newColumnizer.GetType().Equals(logLineColumnizer.GetType())
            ? null
            : newColumnizer;
    }

    /// <summary>
    /// Selects the most appropriate log line columnizer for the specified file and sample log lines from the provided
    /// list of registered columnizers.
    /// </summary>
    /// <remarks>The method evaluates each registered columnizer, optionally using sample log lines from the
    /// file, to determine which is most suitable. The selection is based on priority as determined by each columnizer
    /// implementation.</remarks>
    /// <param name="fileName">The path or name of the log file to analyze. If null or empty, a default columnizer is returned.</param>
    /// <param name="logFileReader">An optional callback used to retrieve sample log lines for analysis. If null, only the file name is used to
    /// determine the columnizer.</param>
    /// <param name="registeredColumnizer">A list of available columnizer instances to consider for selection. Cannot be null.</param>
    /// <returns>An instance of a log line memory columnizer determined to be the best match for the specified file and sample
    /// log lines. Returns a default columnizer if the file name is null or empty.</returns>
    public static ILogLineMemoryColumnizer FindMemoryColumnizer (string fileName, IAutoLogLineMemoryColumnizerCallback logFileReader, IList<ILogLineMemoryColumnizer> registeredColumnizer)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return new DefaultLogfileColumnizer();
        }

        ArgumentNullException.ThrowIfNull(registeredColumnizer, nameof(registeredColumnizer));

        List<ILogLineMemory> loglines = [];

        if (logFileReader != null)
        {
            loglines =
            [
                // Sampling a few lines to select the correct columnizer
                logFileReader.GetLogLineMemory(0),
                logFileReader.GetLogLineMemory(1),
                logFileReader.GetLogLineMemory(2),
                logFileReader.GetLogLineMemory(3),
                logFileReader.GetLogLineMemory(4),
                logFileReader.GetLogLineMemory(5),
                logFileReader.GetLogLineMemory(25),
                logFileReader.GetLogLineMemory(100),
                logFileReader.GetLogLineMemory(200),
                logFileReader.GetLogLineMemory(400)
            ];
        }

        List<(Priority priority, ILogLineMemoryColumnizer columnizer)> priorityListOfColumnizers = [];

        foreach (var logLineColumnizer in registeredColumnizer)
        {
            Priority priority = default;
            if (logLineColumnizer is IColumnizerPriorityMemory columnizerPriority)
            {
                priority = columnizerPriority.GetPriority(fileName, loglines);
            }

            priorityListOfColumnizers.Add((priority, logLineColumnizer));
        }

        var lineColumnizer = priorityListOfColumnizers.OrderByDescending(item => item.priority).Select(item => item.columnizer).First();

        return lineColumnizer;
    }
}