namespace LogExpert.Core.Config;

[Serializable]
public class ColumnizerMaskEntry
{
    public string ColumnizerName { get; set; }

    public string Mask { get; set; }

    /// <summary>
    /// How <see cref="Mask"/> is interpreted. Defaults to <see cref="MaskType.Glob"/> for new entries;
    /// the settings migrator rewrites pre-1.21 entries to <see cref="MaskType.Regex"/> to preserve behaviour.
    /// </summary>
    public MaskType Type { get; set; } = MaskType.Glob;
}