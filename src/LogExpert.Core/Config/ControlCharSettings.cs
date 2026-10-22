using System.Drawing;

using Newtonsoft.Json;

namespace LogExpert.Core.Config;

[Serializable]
public sealed class ControlCharSettings
{
    public bool Substitute { get; set; }

    public ControlCharStyle Style
    {
        get;
        set => field = Enum.IsDefined(value) ? value : ControlCharStyle.ControlPictures;
    } = ControlCharStyle.ControlPictures;

    public Color ForeColor { get; set; } = Color.Gray;

    public Color BackColor { get; set; } = Color.Empty;

    public bool Bold { get; set; }

    public bool Italic { get; set; }

    public bool CopyDisplayedForm { get; set; }

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, NullValueHandling = NullValueHandling.Ignore)]
    public HashSet<int> EnabledCodepoints { get; set; } = BuildNonWhitespacePreset();

    internal static HashSet<int> BuildNonWhitespacePreset ()
    {
        return
        [
            .. Enumerable.Range(0x00, 0x20).Where(c => c is not 0x09 and not 0x0A and not 0x0D),
            0x7F,
        ];
    }
}
