namespace LogExpert.Extensions;

//TODO: Move this to LogExpert.UI, change to internal and fix tests
public static class LogLineExtensions
{
    //TOOD: check if the callers are checking for null before calling
    public static string ToClipBoardText (this ILogLine logLine)
    {
        return logLine == null ? string.Empty : $"\t{logLine.LineNumber + 1}\t{logLine.FullLine}";
    }
}