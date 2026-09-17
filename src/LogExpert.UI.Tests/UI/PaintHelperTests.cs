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
