using System.Drawing;

namespace LogExpert;

public static class ThemedImages
{
    public static Bitmap Get (string name, bool darkMode)
    {
        if (darkMode)
        {
            if (Resources.ResourceManager.GetObject(name + "_dark") is Bitmap dark)
            {
                return dark;
            }
        }

        return (Bitmap)(Resources.ResourceManager.GetObject(name)
            ?? throw new InvalidOperationException($"Themed image resource '{name}' (or '{name}_dark') was not found."));
    }
}
