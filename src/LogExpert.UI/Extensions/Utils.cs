using System.Runtime.Versioning;

namespace LogExpert.UI.Extensions;


internal static class Utils
{
    [SupportedOSPlatform("windows")]
    public static string GetWordFromPos (int xPos, string text, Graphics g, Font font)
    {
        var words = text.Split([' ', '.', ':', ';']);

        var index = 0;

        List<CharacterRange> crList = [];

        for (var i = 0; i < words.Length; ++i)
        {
            crList.Add(new CharacterRange(index, words[i].Length));
            index += words[i].Length;
        }

        CharacterRange[] crArray = [.. crList];

        StringFormat stringFormat = new(StringFormat.GenericTypographic)
        {
            Trimming = StringTrimming.None,
            FormatFlags = StringFormatFlags.NoClip
        };

        stringFormat.SetMeasurableCharacterRanges(crArray);

        RectangleF rect = new(0, 0, 3000, 20);
        Region[] stringRegions = g.MeasureCharacterRanges(text, font, rect, stringFormat);

        var found = false;

        var y = 0;

        foreach (Region regio in stringRegions)
        {
            if (regio.IsVisible(xPos, 3, g))
            {
                found = true;
                break;
            }

            y++;
        }

        return found
            ? words[y]
            : null;
    }
}
