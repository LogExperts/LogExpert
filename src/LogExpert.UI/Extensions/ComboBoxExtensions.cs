using System.Runtime.Versioning;

namespace LogExpert.UI.Extensions;

[SupportedOSPlatform("windows")]
internal static class ComboBoxExtensions
{
    /// <seealso href="https://stackoverflow.com/a/4842576/1987788"/>
    public static int GetMaxTextWidth(this ComboBox comboBox)
    {
        var maxTextWidth = comboBox.Width;

        foreach (var item in comboBox.Items)
        {
            var textWidthInPixels = TextRenderer.MeasureText(item.ToString(), comboBox.Font).Width;

            if (textWidthInPixels > maxTextWidth)
            {
                maxTextWidth = textWidthInPixels;
            }
        }

        return maxTextWidth;
    }
}
