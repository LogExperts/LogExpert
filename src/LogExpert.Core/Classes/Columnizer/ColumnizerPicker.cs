using System.Reflection;

using LogExpert.Core.Entities;

namespace LogExpert.Core.Classes.Columnizer;

public static class ColumnizerPicker
{
    public static ILogLineColumnizer FindColumnizerByName (string name, IList<ILogLineColumnizer> list)
    {
        foreach (ILogLineColumnizer columnizer in list)
        {
            if (columnizer.GetName().Equals(name, StringComparison.Ordinal))
            {
                return columnizer;
            }
        }
        return null;
    }

    public static ILogLineColumnizer DecideColumnizerByName (string name, IList<ILogLineColumnizer> list)
    {
        foreach (ILogLineColumnizer columnizer in list)
        {
            if (columnizer.GetName().Equals(name, StringComparison.Ordinal))
            {
                return columnizer;
            }
        }

        return FindColumnizer(null, null, list);
    }

    public static ILogLineColumnizer CloneColumnizer (ILogLineColumnizer columnizer, string directory)
    {
        if (columnizer == null)
        {
            return null;
        }
        ConstructorInfo cti = columnizer.GetType().GetConstructor(Type.EmptyTypes);

        if (cti != null)
        {
            var o = cti.Invoke([]);

            if (o is IColumnizerConfigurator configurator)
            {
                configurator.LoadConfig(directory);
            }
            return (ILogLineColumnizer)o;
        }
        return null;
    }

    /// <summary>
    /// This method implemented the "auto columnizer" feature.
    /// This method should be called after each columnizer is changed to update the columizer.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="logFileReader"></param>
    /// <param name="logLineColumnizer"></param>
    /// <returns></returns>
    public static ILogLineColumnizer FindReplacementForAutoColumnizer (string fileName,
        IAutoLogLineColumnizerCallback logFileReader,
        ILogLineColumnizer logLineColumnizer,
        IList<ILogLineColumnizer> list)
    {
        if (logLineColumnizer == null || logLineColumnizer.GetName() == "Auto Columnizer")
        {
            return FindColumnizer(fileName, logFileReader, list);
        }
        return logLineColumnizer;
    }

    public static ILogLineColumnizer FindBetterColumnizer (string fileName,
        IAutoLogLineColumnizerCallback logFileReader,
        ILogLineColumnizer logLineColumnizer,
        IList<ILogLineColumnizer> list)
    {
        var newColumnizer = FindColumnizer(fileName, logFileReader, list);

        if (newColumnizer.GetType().Equals(logLineColumnizer.GetType()))
        {
            return null;
        }
        return newColumnizer;
    }

    //TOOD: check if the callers are checking for null before calling
    /// <summary>
    /// This method will search all registered columnizer and return one according to the priority that returned
    /// by the each columnizer.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="logFileReader"></param>
    /// <returns></returns>
    public static ILogLineColumnizer FindColumnizer (string fileName, IAutoLogLineColumnizerCallback logFileReader, IList<ILogLineColumnizer> registeredColumnizer)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return new DefaultLogfileColumnizer();
        }

        ArgumentNullException.ThrowIfNull(registeredColumnizer, nameof(registeredColumnizer));

        List<ILogLine> loglines = [];

        if (logFileReader != null)
        {
            loglines =
            [
                // Sampling a few lines to select the correct columnizer
                logFileReader.GetLogLine(0),
                logFileReader.GetLogLine(1),
                logFileReader.GetLogLine(2),
                logFileReader.GetLogLine(3),
                logFileReader.GetLogLine(4),
                logFileReader.GetLogLine(5),
                logFileReader.GetLogLine(25),
                logFileReader.GetLogLine(100),
                logFileReader.GetLogLine(200),
                logFileReader.GetLogLine(400)
            ];
        }

        List<(Priority priority, ILogLineColumnizer columnizer)> priorityListOfColumnizers = [];

        foreach (ILogLineColumnizer logLineColumnizer in registeredColumnizer)
        {
            Priority priority = default;
            if (logLineColumnizer is IColumnizerPriority columnizerPriority)
            {
                priority = columnizerPriority.GetPriority(fileName, loglines);
            }

            priorityListOfColumnizers.Add((priority, logLineColumnizer));
        }

        ILogLineColumnizer lineColumnizer = priorityListOfColumnizers.OrderByDescending(item => item.priority).Select(item => item.columnizer).First();

        return lineColumnizer;
    }
}
