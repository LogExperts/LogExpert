namespace LogExpert.Core.Classes.Persister;

public class SessionValidationResult
{
    public List<string> ValidFiles { get; } = [];

    public List<string> MissingFiles { get; } = [];

    public Dictionary<string, List<string>> PossibleAlternatives { get; } = [];

    public bool HasMissingFiles => MissingFiles.Count > 0;
}
