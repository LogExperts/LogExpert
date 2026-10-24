using System.Drawing;
using System.Globalization;

namespace LogExpert;

public static class ThemedImages
{
    public static Bitmap Get (string name, bool darkMode)
    {
        var culture = CultureInfo.CurrentUICulture;

        if (darkMode)
        {
            if (Resources.ResourceManager.GetObject(name + "_dark", culture) is Bitmap dark)
            {
                return dark;
            }
        }

        return (Bitmap)(Resources.ResourceManager.GetObject(name, culture)
            ?? throw new InvalidOperationException($"Themed image resource '{name}' (or '{name}_dark') was not found."));
    }
}
