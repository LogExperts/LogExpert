using LogExpert.Core.Config;
using LogExpert.Core.Interfaces;
using LogExpert.Dialogs;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.UI;

[TestFixture]
[Apartment(ApartmentState.STA)]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class SelectionHighlightDialogTests
{
    [TestCase("en-US", 1f)]
    [TestCase("de-DE", 1.5f)]
    public void SelectionControls_FitDialogWithoutAnOpenFile (string culture, float scale)
    {
        var previousCulture = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(culture);

            var settings = new Settings();
            settings.Preferences.SelectionHighlight.CustomColor = Color.Yellow;

            var config = new Mock<IConfigManager>();
            _ = config.SetupGet(c => c.Settings).Returns(settings);

            using var dialog = new HighlightDialog(config.Object) { HighlightGroupList = [] };
            dialog.Scale(new SizeF(scale, scale));
            dialog.Show();

            foreach (var name in new[] { "checkBoxSelectionOutline", "btnSelectionColor", "btnResetSelectionColor", "btnOk", "btnCancel" })
            {
                var control = dialog.Controls.Find(name, true).Single();

                Assert.That(control.Visible, Is.True, name);
                Assert.That(control.Parent.ClientRectangle.Contains(control.Bounds), Is.True, name);
                Assert.That(dialog.RectangleToScreen(dialog.ClientRectangle).Contains(control.RectangleToScreen(control.ClientRectangle)), Is.True, name);
            }

            using var bitmap = new Bitmap(dialog.Width, dialog.Height);
            dialog.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));

            var path = Path.Join(TestContext.CurrentContext.WorkDirectory, $"selection-highlight-{culture}.png");
            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);

            TestContext.AddTestAttachment(path);
            ((Button)dialog.Controls.Find("btnResetSelectionColor", true).Single()).PerformClick();

            Assert.That(dialog.SelectionHighlight.CustomColor, Is.Null);
            Assert.That(settings.Preferences.SelectionHighlight.CustomColor, Is.EqualTo(Color.Yellow));
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = previousCulture;
        }
    }

    [TestCase(DialogResult.OK)]
    [TestCase(DialogResult.Cancel)]
    public void EditingSelection_LeavesLiveSettingsUntouchedUntilCallerAccepts (DialogResult result)
    {
        var settings = new Settings();

        var config = new Mock<IConfigManager>();
        _ = config.SetupGet(c => c.Settings).Returns(settings);

        using var dialog = new HighlightDialog(config.Object) { HighlightGroupList = [] };
        dialog.Show();

        var outline = (CheckBox)dialog.Controls.Find("checkBoxSelectionOutline", true).Single();
        outline.Checked = true;
        ((Button)dialog.Controls.Find(result == DialogResult.OK ? "btnOk" : "btnCancel", true).Single()).PerformClick();

        Assert.That(dialog.DialogResult, Is.EqualTo(result));
        Assert.That(settings.Preferences.SelectionHighlight.Outline, Is.False);

        if (result == DialogResult.OK)
        {
            Assert.That(dialog.SelectionHighlight.Outline, Is.True);
        }

        config.Verify(c => c.Save(It.IsAny<SettingsFlags>()), Times.Never);
    }
}