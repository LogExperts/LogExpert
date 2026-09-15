namespace LogExpert.Core.Classes.IPC;

public class LoadPayload
{
    public List<string> Files { get; set; } = [];

    /// <summary>Optional one-based line number, applied after the log has loaded.</summary>
    public int? TargetLine { get; set; }

    public string? GetValidationError ()
    {
        if (Files == null || Files.Any(string.IsNullOrWhiteSpace))
        {
            return "File paths must not be empty.";
        }

        if (TargetLine == null)
        {
            return null;
        }

        if (TargetLine < 1)
        {
            return "--line must be an integer from 1 through 2147483647.";
        }

        if (Files.Count != 1)
        {
            return "--line requires exactly one log file.";
        }

        if (Files[0].EndsWith(".lxj", StringComparison.OrdinalIgnoreCase)
            || Files[0].EndsWith(".lxp", StringComparison.OrdinalIgnoreCase))
        {
            return "--line does not support .lxj sessions or .lxp session files.";
        }

        return null;
    }

    public override string? ToString ()
    {
        return string.Join(", ", Files);
    }
}
