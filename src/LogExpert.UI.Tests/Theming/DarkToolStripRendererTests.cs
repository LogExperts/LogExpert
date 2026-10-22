using System.Drawing.Imaging;
using System.Runtime.Versioning;

using LogExpert.UI.Theming;

using NUnit.Framework;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Tests.Theming;

[TestFixture]
[Apartment(ApartmentState.STA)] // Required for WinForms components
[SupportedOSPlatform("windows")]
public class DarkToolStripRendererTests
{
    [Test]
    public void Constructor_WithTheVS2015DarkPalette_Succeeds ()
    {
        // Guards the wiring in ToolStripTheme: the theme has to expose its palette before any
        // window exists, otherwise the renderer cannot be built at start-up.
        var theme = new VS2015DarkTheme();

        var renderer = new DarkToolStripRenderer(theme.ColorPalette);

        Assert.That(renderer, Is.Not.Null);
    }

    [Test]
    public void ShouldInvertIcon_DarkMonochromeGlyph_ReturnsTrue ()
    {
        // The resource glyphs are black-on-transparent PNGs.
        using var glyph = CreateIcon(Color.Black);

        Assert.That(DarkToolStripRenderer.ShouldInvertIcon(glyph), Is.True);
    }

    [Test]
    public void ShouldInvertIcon_ColouredIcon_ReturnsFalse ()
    {
        // Inverting a blue icon turned it orange; coloured icons must keep their colours.
        using var icon = CreateIcon(Color.FromArgb(0, 120, 215));

        Assert.That(DarkToolStripRenderer.ShouldInvertIcon(icon), Is.False);
    }

    [Test]
    public void ShouldInvertIcon_LightMonochromeGlyph_ReturnsFalse ()
    {
        // A light glyph is already visible on the dark strip; inverting would make it invisible.
        using var glyph = CreateIcon(Color.White);

        Assert.That(DarkToolStripRenderer.ShouldInvertIcon(glyph), Is.False);
    }

    [Test]
    public void ShouldInvertIcon_EmptyImage_ReturnsFalse ()
    {
        using var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);

        Assert.That(DarkToolStripRenderer.ShouldInvertIcon(bitmap), Is.False);
    }

    [Test]
    public void ShouldInvertIcon_GreyscaleGlyphWithAntiAliasing_ReturnsTrue ()
    {
        // Real glyphs are anti-aliased, so their edge pixels are grey rather than black. The
        // tolerance has to accept them; otherwise half the toolbar would stay dark.
        using var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(Color.FromArgb(90, 90, 90)))
        {
            graphics.FillRectangle(brush, 2, 2, 12, 12);
        }

        Assert.That(DarkToolStripRenderer.ShouldInvertIcon(bitmap), Is.True);
    }

    private static Bitmap CreateIcon (Color color)
    {
        var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(bitmap);
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, 2, 2, 12, 12);

        return bitmap;
    }
}
