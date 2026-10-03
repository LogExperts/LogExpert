using System.Reflection;

using LogExpert.Core.Classes.Highlight;
using LogExpert.UI.Dialogs.Highlight;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Dialogs;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class HighlightEntryDialogColorTests
{
    private static readonly int[] Palette = [0x00102030, 0x00405060];

    [TestCase("OnBtnCustomForeColorClicked")]
    [TestCase("OnBtnCustomBackColorClicked")]
    public void ColorPicker_OpensExpandedWithTheSharedPalette (string handler)
    {
        using var dialog = new HighlightEntryDialog(new HighlightEntry { SearchText = "x" }, [], false) { CustomColors = Palette };
        ColorDialog shown = null;
        int[] shownPalette = null;
        dialog.ShowColorDialog = colorDialog =>
        {
            shown = colorDialog;
            shownPalette = colorDialog.CustomColors;
            return DialogResult.Cancel;
        };

        Invoke(dialog, handler);

        Assert.That(shown.FullOpen, Is.True);
        Assert.That(shownPalette.Take(2), Is.EqualTo(Palette));
    }

    [Test]
    public void ColorPickerOk_KeepsItsPaletteForTheNextPicker ()
    {
        using var dialog = new HighlightEntryDialog(new HighlightEntry { SearchText = "x" }, [], false) { CustomColors = Palette };
        int[] secondPalette = null;
        dialog.ShowColorDialog = colorDialog =>
        {
            colorDialog.CustomColors = [0x00ABCDEF];
            return DialogResult.OK;
        };
        Invoke(dialog, "OnBtnCustomForeColorClicked");

        dialog.ShowColorDialog = colorDialog =>
        {
            secondPalette = colorDialog.CustomColors;
            return DialogResult.Cancel;
        };
        Invoke(dialog, "OnBtnCustomBackColorClicked");

        Assert.That(secondPalette[0], Is.EqualTo(0x00ABCDEF));
        Assert.That(dialog.CustomColors[0], Is.EqualTo(0x00ABCDEF));
    }

    [Test]
    public void ColorPickerCancel_KeepsThePreviousPalette ()
    {
        using var dialog = new HighlightEntryDialog(new HighlightEntry { SearchText = "x" }, [], false) { CustomColors = Palette };
        dialog.ShowColorDialog = colorDialog =>
        {
            colorDialog.CustomColors = [0x00ABCDEF];
            return DialogResult.Cancel;
        };

        Invoke(dialog, "OnBtnCustomForeColorClicked");

        Assert.That(dialog.CustomColors, Is.EqualTo(Palette));
    }

    private static void Invoke (HighlightEntryDialog dialog, string handler)
    {
        _ = typeof(HighlightEntryDialog).GetMethod(handler, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(dialog, [dialog, EventArgs.Empty]);
    }
}
