#region

using System.Drawing;

#endregion

namespace LogExpert.Core.Config;

[Serializable]
public class ColorEntry (string FileName, Color Color)
{
    public Color Color { get; } = Color;

    public string FileName { get; } = FileName;
}