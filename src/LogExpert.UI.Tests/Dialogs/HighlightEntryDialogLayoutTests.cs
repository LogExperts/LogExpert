using System.Globalization;

using LogExpert.Core.Classes.Highlight;
using LogExpert.UI.Dialogs.Highlight;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Dialogs;

[TestFixture]
[Apartment(ApartmentState.STA)]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class HighlightEntryDialogLayoutTests
{
    private CultureInfo _previousCulture;

    [SetUp]
    public void SetUp ()
    {
        _previousCulture = Thread.CurrentThread.CurrentUICulture;
    }

    [TearDown]
    public void TearDown ()
    {
        Thread.CurrentThread.CurrentUICulture = _previousCulture;
    }

    [TestCase("en-US", 1f)]
    [TestCase("de-DE", 1f)]
    [TestCase("en-US", 1.5f)]
    [TestCase("de-DE", 2f)]
    public void ButtonLabels_FitOnOneLine (string culture, float scale)
    {
        using var dialog = CreateDialog(culture, scale);

        foreach (var name in new[] { "_btnCustomForeColor", "_btnCustomBackColor", "_btnBookmarkComment", "_btnSelectPlugin", "_btnBrowseSoundFile" })
        {
            var button = dialog.Controls.Find(name, true).Single();
            var preferred = button.GetPreferredSize(Size.Empty);

            Assert.That(button.Width, Is.GreaterThanOrEqualTo(preferred.Width), name);
            Assert.That(button.Height, Is.GreaterThanOrEqualTo(preferred.Height), name);
        }
    }

    [TestCase("en-US", 1f)]
    [TestCase("de-DE", 1.5f)]
    public void ShrinkingTheDialog_KeepsEveryControlVisible (string culture, float scale)
    {
        using var dialog = CreateDialog(culture, scale);

        dialog.Size = new Size(100, 100);

        var tabs = (TabControl)dialog.Controls.Find("_tabControl", true).Single();
        foreach (TabPage page in tabs.TabPages)
        {
            tabs.SelectedTab = page;
            foreach (Control control in page.Controls)
            {
                Assert.That(page.ClientRectangle.Contains(control.Bounds), Is.True, $"{control.Name} inside {page.Name}");
            }
        }

        foreach (var name in new[] { "_btnOk", "_btnCancel" })
        {
            var button = dialog.Controls.Find(name, true).Single();
            Assert.That(dialog.ClientRectangle.Contains(button.Bounds), Is.True, name);
            Assert.That(button.Bounds.IntersectsWith(tabs.Bounds), Is.False, name);
        }
    }

    private static HighlightEntryDialog CreateDialog (string culture, float scale)
    {
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        var dialog = new HighlightEntryDialog(new HighlightEntry { SearchText = "ERROR" }, [], false);
        dialog.Scale(new SizeF(scale, scale));
        dialog.Show();
        return dialog;
    }
}
