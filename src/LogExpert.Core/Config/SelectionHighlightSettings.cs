using System.Drawing;

namespace LogExpert.Core.Config;

/// <summary>Application-wide selection appearance, edited in the Highlights dialog.</summary>
[Serializable]
public sealed class SelectionHighlightSettings
{
    public bool Outline { get; set; }

    /// <summary>Null follows the system selection color.</summary>
    public Color? CustomColor { get; set; }
}