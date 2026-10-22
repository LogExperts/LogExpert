using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Theming;

/// <summary>
/// DockPanel Suite's VS2015 renderer with one adjustment: dark monochrome icons are inverted
/// while painting. Backgrounds, hover, arrows, separators, check marks and text colours all
/// come from the palette.
///
/// The glyphs in <c>LogExpert.Resources</c> are black-on-transparent PNGs; drawing them
/// unchanged on the dark toolbar makes them invisible. Coloured icons must keep their colours,
/// though - the tail button and the favourite icons loaded from an external .exe are coloured,
/// and inverting them turned blue into orange. Only images that are actually dark and
/// monochrome are inverted.
///
/// Text is deliberately not overridden. <see cref="VisualStudioToolStripRenderer"/> picks the
/// colour from the palette per strip type and item state and never reads
/// <see cref="ToolStripItemTextRenderEventArgs.TextColor"/>, so a per-item colour - the dimmed
/// bookmark entries, for example - cannot be honoured by a renderer subclass at all. Overriding
/// it only means re-deciding every item's colour here, which is what the palette already does.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class DarkToolStripRenderer : VisualStudioToolStripRenderer
{
    // Pixels below this alpha belong to the anti-aliased edge and are ignored when sampling.
    private const int ALPHA_CUTOFF = 32;

    // Largest spread between the R, G and B channels of a pixel that still counts as grey.
    private const int MONOCHROME_TOLERANCE = 24;

    // A monochrome glyph darker than this would disappear on the dark strip and is inverted.
    // Icons that are already light are left alone, otherwise they would turn dark.
    private const int DARK_GLYPH_LUMINANCE = 140;

    // Inverting a 192x192 PNG on every paint would be wasteful, so each distinct image is
    // converted at most once. The table keeps the key (the original image) weak, so icons
    // that get replaced - external tool favourites, for example - are still collected.
    private static readonly ConditionalWeakTable<Image, InvertedGlyph> _invertedGlyphs = new();

    // Invert the RGB channels (black <-> white) and keep alpha, so the transparent
    // background stays transparent instead of getting a white halo.
    private static readonly ColorMatrix _invertMatrix = new(
    [
        [ -1f,  0f,  0f, 0f, 0f ],
        [  0f, -1f,  0f, 0f, 0f ],
        [  0f,  0f, -1f, 0f, 0f ],
        [  0f,  0f,  0f, 1f, 0f ],
        [  1f,  1f,  1f, 0f, 1f ],
    ]);

    public DarkToolStripRenderer (DockPanelColorPalette palette) : base(palette)
    {
    }

    /// <summary>
    /// Tells whether the icon is a dark monochrome glyph that has to be inverted to stay
    /// visible on the dark strip. Coloured icons - including the ones that are already light -
    /// return <c>false</c> and are drawn unchanged.
    /// </summary>
    internal static bool ShouldInvertIcon (Image image)
    {
        return GetInvertedGlyph(image) != null;
    }

    protected override void OnRenderItemImage (ToolStripItemImageRenderEventArgs e)
    {
        var inverted = e.Image == null ? null : GetInvertedGlyph(e.Image);

        if (inverted == null)
        {
            // Coloured icons and items without an image keep the stock behaviour: disabled
            // items are greyed out, ImageTransparentColor and ImageScaling still apply.
            base.OnRenderItemImage(e);
            return;
        }

        // Draw the inverted copy the same way ToolStripRenderer draws a normal image, so a
        // disabled item is still greyed out and the ImageRectangle / scaling are honoured.
        // The copy already carries the alpha channel, so no transparent-color key is needed.
        Image image = inverted;
        Image? disabledImage = null;

        try
        {
            if (!e.Item.Enabled)
            {
                disabledImage = CreateDisabledImage(inverted);
                image = disabledImage;
            }

            // Rectangle.Empty happens for items that are too small to show their image; the
            // stock renderer skips those, and drawing into an empty rectangle is not allowed.
            if (e.ImageRectangle.Width > 0 && e.ImageRectangle.Height > 0)
            {
                e.Graphics.DrawImage(image, e.ImageRectangle);
            }
        }
        finally
        {
            disabledImage?.Dispose();
        }
    }

    private static Bitmap? GetInvertedGlyph (Image image)
    {
        if (image.Width <= 0 || image.Height <= 0)
        {
            return null;
        }

        return _invertedGlyphs.GetValue(image, static source => new InvertedGlyph(CreateInvertedGlyph(source))).Image;
    }

    /// <summary>
    /// Returns an inverted copy of <paramref name="source"/> if it is a dark monochrome glyph,
    /// or <c>null</c> if the image has to be drawn unchanged.
    /// </summary>
    private static Bitmap? CreateInvertedGlyph (Image source)
    {
        var width = source.Width;
        var height = source.Height;
        var bounds = new Rectangle(0, 0, width, height);

        // Normalise to 32bppArgb first: the resources are palette or 24bpp PNGs, which
        // LockBits cannot hand out as 32bpp.
        using var copy = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(copy))
        {
            graphics.DrawImage(source, bounds, 0, 0, width, height, GraphicsUnit.Pixel);
        }

        if (!IsDarkMonochrome(copy))
        {
            return null;
        }

        var inverted = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(inverted))
        using (var attributes = new ImageAttributes())
        {
            attributes.SetColorMatrix(_invertMatrix);
            graphics.DrawImage(copy, bounds, 0, 0, width, height, GraphicsUnit.Pixel, attributes);
        }

        return inverted;
    }

    private static bool IsDarkMonochrome (Bitmap bitmap)
    {
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            var stride = Math.Abs(data.Stride);
            var pixels = new byte[stride * bitmap.Height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

            var luminanceSum = 0;
            var pixelCount = 0;

            for (var offset = 0; offset <= pixels.Length - 4; offset += 4)
            {
                var alpha = pixels[offset + 3];

                if (alpha < ALPHA_CUTOFF)
                {
                    continue;
                }

                var blue = pixels[offset];
                var green = pixels[offset + 1];
                var red = pixels[offset + 2];

                var max = Math.Max(red, Math.Max(green, blue));
                var min = Math.Min(red, Math.Min(green, blue));

                if (max - min > MONOCHROME_TOLERANCE)
                {
                    // A single saturated pixel is enough to call this a coloured icon.
                    return false;
                }

                luminanceSum += ((red * 299) + (green * 587) + (blue * 114)) / 1000;
                pixelCount++;
            }

            return pixelCount > 0 && luminanceSum / pixelCount < DARK_GLYPH_LUMINANCE;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    /// <summary>
    /// Cache entry for one image. Wraps the result so images that must not be inverted can be
    /// remembered too - <see cref="ConditionalWeakTable{TKey,TValue}"/> does not accept a null
    /// value.
    /// </summary>
    private sealed class InvertedGlyph (Bitmap? image)
    {
        public Bitmap? Image { get; } = image;
    }
}
