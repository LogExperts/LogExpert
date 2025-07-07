namespace LogExpert.Core.Structs;

public struct ActEntry
{
    public string Name { get; set; }

    public IKeywordAction Plugin { get; set; }
}