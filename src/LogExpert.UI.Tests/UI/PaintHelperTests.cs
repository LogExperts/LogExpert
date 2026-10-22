using System.Runtime.Versioning;

using LogExpert.Core.Classes.Highlight;
using LogExpert.UI.Entities;

using NUnit.Framework;

namespace LogExpert.UI.Tests.UI;

[TestFixture]
[Apartment(ApartmentState.STA)] // Required for WinForms components
[SupportedOSPlatform("windows")]
public class PaintHelperTests
{
    [Test]
    public void ApplyGridViewTheme_DarkMode_DisablesHeaderVisualStyles ()
    {
        // With EnableHeadersVisualStyles = true the column headers are painted by the
        // Windows visual-style renderer, which only exists in a light flavor and
        // ignores Application.SetColorMode(Dark).
        using DataGridView gridView = new();

        PaintHelper.ApplyGridViewTheme(gridView, darkMode: true);

        Assert.That(gridView.EnableHeadersVisualStyles, Is.False);
    }

    [Test]
    public void ApplyGridViewTheme_LightMode_KeepsHeaderVisualStyles ()
    {
        using DataGridView gridView = new();

        PaintHelper.ApplyGridViewTheme(gridView, darkMode: false);

        Assert.That(gridView.EnableHeadersVisualStyles, Is.True);
    }

    [Test]
    public void GetBackColorFromHighlightEntry_NoEntryInDarkMode_FallsBackToDarkColor ()
    {
        // A line matching no highlight rule must not get the light-mode white background.
        var backColor = PaintHelper.GetBackColorFromHighlightEntry(null, darkMode: true);

        Assert.That(IsDark(backColor), Is.True, $"expected a dark fallback color but got {backColor}");
    }

    [Test]
    public void GetBackColorFromHighlightEntry_NoEntryInLightMode_FallsBackToWhite ()
    {
        var backColor = PaintHelper.GetBackColorFromHighlightEntry(null, darkMode: false);

        Assert.That(backColor, Is.EqualTo(Color.White));
    }

    [Test]
    public void GetBackColorFromHighlightEntry_EntryColor_WinsOverTheme ()
    {
        var entry = new HighlightEntry { BackgroundColor = Color.Red };

        Assert.Multiple(() =>
        {
            Assert.That(PaintHelper.GetBackColorFromHighlightEntry(entry, darkMode: true), Is.EqualTo(Color.Red));
            Assert.That(PaintHelper.GetBackColorFromHighlightEntry(entry, darkMode: false), Is.EqualTo(Color.Red));
        });
    }

    [Test]
    public void GetForeColorFromHighlightEntry_NoEntryInDarkMode_FallsBackToLightColor ()
    {
        var foreColor = PaintHelper.GetForeColorFromHighlightEntry(null, darkMode: true);

        Assert.That(IsLight(foreColor), Is.True, $"expected a light fallback color but got {foreColor}");
    }

    [Test]
    public void GetDefaultForeColor_DarkMode_UsesTheSoftOffWhite ()
    {
        // 220,220,220 rather than pure white; the grid, the color combo box and the highlight
        // preview have to agree on it.
        var foreColor = PaintHelper.GetDefaultForeColor(darkMode: true);

        Assert.That(foreColor.ToArgb(), Is.EqualTo(Color.FromArgb(220, 220, 220).ToArgb()));
    }

    [Test]
    public void GetDefaultForeColor_LightMode_UsesBlack ()
    {
        var foreColor = PaintHelper.GetDefaultForeColor(darkMode: false);

        Assert.That(foreColor, Is.EqualTo(Color.Black));
    }

    [Test]
    public void GetDefaultBackColor_MatchesTheWindowBackgroundOfEachColorMode ()
    {
        // 32,32,32 is the surface the grid paints cells with in dark mode; the highlight
        // preview has to use the same one instead of SystemColors.Window.
        Assert.Multiple(() =>
        {
            Assert.That(PaintHelper.GetDefaultBackColor(darkMode: true), Is.EqualTo(Color.FromArgb(32, 32, 32)));
            Assert.That(PaintHelper.GetDefaultBackColor(darkMode: false), Is.EqualTo(Color.White));
        });
    }

    [Test]
    public void GetForeColorFromHighlightEntry_EntryWithoutForeColor_UsesTheDefaultForeColor ()
    {
        // HighlightEntry.ForegroundColor defaults to Color.Empty ("not set"). Both callers of
        // a highlight without a color must end up with exactly the same result.
        Assert.Multiple(() =>
        {
            Assert.That(PaintHelper.GetForeColorFromHighlightEntry(null, darkMode: true),
                Is.EqualTo(PaintHelper.GetDefaultForeColor(darkMode: true)));
            Assert.That(PaintHelper.GetForeColorFromHighlightEntry(new HighlightEntry(), darkMode: false),
                Is.EqualTo(PaintHelper.GetDefaultForeColor(darkMode: false)));
        });
    }

    [Test]
    public void GetBookmarkMarkerTextColor_LightMode_KeepsDarkOrange ()
    {
        var markerColor = PaintHelper.GetBookmarkMarkerTextColor(darkMode: false);

        Assert.That(markerColor.ToArgb(), Is.EqualTo(Color.FromArgb(255, 190, 100, 0).ToArgb()));
    }

    [Test]
    public void GetBookmarkMarkerTextColor_DarkMode_IsBrighterAndStaysOrange ()
    {
        var dark = PaintHelper.GetBookmarkMarkerTextColor(darkMode: true);
        var light = PaintHelper.GetBookmarkMarkerTextColor(darkMode: false);

        Assert.Multiple(() =>
        {
            Assert.That(dark.GetBrightness(), Is.GreaterThan(light.GetBrightness()),
                $"expected a brighter dark-mode marker but got {dark} vs {light}");
            Assert.That(dark.R, Is.GreaterThan(dark.G), "marker is expected to stay orange");
            Assert.That(dark.G, Is.GreaterThan(dark.B), "marker is expected to stay orange");
        });
    }

    [Test]
    public void GetForeColorFromHighlightEntry_NoEntryInLightMode_FallsBackToBlack ()
    {
        var foreColor = PaintHelper.GetForeColorFromHighlightEntry(null, darkMode: false);

        Assert.That(foreColor, Is.EqualTo(Color.Black));
    }

    [Test]
    public void GetForeColorFromHighlightEntry_EntryColor_WinsOverTheme ()
    {
        var entry = new HighlightEntry { ForegroundColor = Color.Red };

        Assert.Multiple(() =>
        {
            Assert.That(PaintHelper.GetForeColorFromHighlightEntry(entry, darkMode: true), Is.EqualTo(Color.Red));
            Assert.That(PaintHelper.GetForeColorFromHighlightEntry(entry, darkMode: false), Is.EqualTo(Color.Red));
        });
    }

    [Test]
    public void GetForeColorFromHighlightEntry_EntryWithoutForeColor_FallsBackToTheme ()
    {
        // HighlightEntry.ForegroundColor defaults to Color.Empty ("not set").
        var entry = new HighlightEntry();

        Assert.Multiple(() =>
        {
            Assert.That(IsLight(PaintHelper.GetForeColorFromHighlightEntry(entry, darkMode: true)), Is.True);
            Assert.That(PaintHelper.GetForeColorFromHighlightEntry(entry, darkMode: false), Is.EqualTo(Color.Black));
        });
    }

    [Test]
    public void GetDataGridViewCellStyle_DarkMode_UsesLightForeColor ()
    {
        var style = PaintHelper.GetDataGridViewCellStyle(darkMode: true);

        Assert.That(IsLight(style.ForeColor), Is.True, $"expected a light fore color but got {style.ForeColor}");
    }

    [Test]
    public void GetDataGridViewCellStyle_LightMode_UsesDarkForeColor ()
    {
        var style = PaintHelper.GetDataGridViewCellStyle(darkMode: false);

        Assert.That(IsDark(style.ForeColor), Is.True, $"expected a dark fore color but got {style.ForeColor}");
    }

    [Test]
    public void GetDataGridDefaultRowStyle_DarkMode_UsesLightForeColor ()
    {
        var style = PaintHelper.GetDataGridDefaultRowStyle(darkMode: true);

        Assert.That(IsLight(style.ForeColor), Is.True, $"expected a light fore color but got {style.ForeColor}");
    }

    [Test]
    public void GetDataGridDefaultRowStyle_LightMode_UsesDarkForeColor ()
    {
        var style = PaintHelper.GetDataGridDefaultRowStyle(darkMode: false);

        Assert.That(IsDark(style.ForeColor), Is.True, $"expected a dark fore color but got {style.ForeColor}");
    }

    [Test]
    public void GetDataGridDefaultRowStyle_AsRowsDefaultCellStyle_CellInheritsGridFont ()
    {
        // The cell editing control (double-click / Ctrl+E) takes its font from the inherited cell style (#720).
        using DataGridView gridView = new();
        using Font configuredFont = new("Consolas", 10f);
        _ = gridView.Columns.Add("text", "text");
        _ = gridView.Rows.Add("line");
        gridView.DefaultCellStyle.Font = configuredFont;

        gridView.RowsDefaultCellStyle = PaintHelper.GetDataGridDefaultRowStyle(darkMode: false);

        Assert.That(gridView.Rows[0].Cells[0].InheritedStyle.Font, Is.EqualTo(configuredFont));
    }

    [Test]
    public void GetBrushForFocusedControl_UnfocusedDarkMode_UsesDarkGray ()
    {
        using var brush = (SolidBrush)PaintHelper.GetBrushForFocusedControl(focused: false, SystemColors.Highlight, darkMode: true);

        Assert.That(IsDark(brush.Color), Is.True, $"expected a dark unfocused-selection color but got {brush.Color}");
    }

    [Test]
    public void GetBrushForFocusedControl_UnfocusedLightMode_KeepsLightGray ()
    {
        using var brush = (SolidBrush)PaintHelper.GetBrushForFocusedControl(focused: false, SystemColors.Highlight, darkMode: false);

        Assert.That(brush.Color.ToArgb(), Is.EqualTo(Color.FromArgb(255, 170, 170, 170).ToArgb()));
    }

    [Test]
    public void GetBrushForFocusedControl_Focused_UsesSelectionColor ()
    {
        using var brushDark = (SolidBrush)PaintHelper.GetBrushForFocusedControl(focused: true, Color.Teal, darkMode: true);
        using var brushLight = (SolidBrush)PaintHelper.GetBrushForFocusedControl(focused: true, Color.Teal, darkMode: false);

        Assert.Multiple(() =>
        {
            Assert.That(brushDark.Color, Is.EqualTo(Color.Teal));
            Assert.That(brushLight.Color, Is.EqualTo(Color.Teal));
        });
    }

    [Test]
    public void ApplyTabControlTheme_DarkMode_DisablesVisualStyleBackColorOnAllPages ()
    {
        // With UseVisualStyleBackColor = true the tab page body is painted by the
        // light-only visual-style renderer while child controls inherit the dark
        // ambient color, giving light pages with dark patches in dark mode.
        using TabControl tabControl = new();
        tabControl.TabPages.Add(new TabPage { UseVisualStyleBackColor = true });
        tabControl.TabPages.Add(new TabPage { UseVisualStyleBackColor = true });

        PaintHelper.ApplyTabControlTheme(tabControl, darkMode: true);

        Assert.That(tabControl.TabPages.Cast<TabPage>().Select(p => p.UseVisualStyleBackColor), Is.All.False);
    }

    [Test]
    public void ApplyTabControlTheme_LightMode_KeepsVisualStyleBackColor ()
    {
        using TabControl tabControl = new();
        tabControl.TabPages.Add(new TabPage { UseVisualStyleBackColor = true });

        PaintHelper.ApplyTabControlTheme(tabControl, darkMode: false);

        Assert.That(tabControl.TabPages[0].UseVisualStyleBackColor, Is.True);
    }

    private static bool IsDark (Color color) => color.R < 128 && color.G < 128 && color.B < 128;

    private static bool IsLight (Color color) => color.R >= 128 && color.G >= 128 && color.B >= 128;
}
