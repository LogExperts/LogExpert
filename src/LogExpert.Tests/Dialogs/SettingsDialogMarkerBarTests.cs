using LogExpert.Core.Config;
using LogExpert.Core.Interfaces;
using LogExpert.Dialogs;

using Moq;

using NUnit.Framework;

using UIStrings = LogExpert.Resources;

namespace LogExpert.Tests.Dialogs;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class SettingsDialogMarkerBarTests
{
    [Test]
    public void MarkerBarTab_LocalizesVisibleOptions_AndSavesCheckboxValues ()
    {
        var preferences = new Preferences();
        var configManager = new Mock<IConfigManager>();
        _ = configManager.SetupGet(manager => manager.Settings).Returns(new Settings());

        using var dialog = new SettingsDialog(preferences, null!, 0, configManager.Object);
        var tab = dialog.Controls.Find("tabControlSettings", true).Single().Controls.Find("tabPageMarkerBar", true).Single();

        Assert.Multiple(() =>
        {
            Assert.That(tab.Text, Is.EqualTo(UIStrings.SettingsDialog_UI_TabPage_tabPageMarkerBar));
            Assert.That(tab.Controls.Find("checkBoxShowMarkerBar", true).Single().Text, Is.EqualTo(UIStrings.SettingsDialog_UI_CheckBox_checkBoxShowMarkerBar));
            Assert.That(tab.Controls.Find("checkBoxShowHighlightMarkers", true).Single().Text, Is.EqualTo(UIStrings.SettingsDialog_UI_CheckBox_checkBoxShowHighlightMarkers));
            Assert.That(tab.Controls.Find("checkBoxShowBookmarkMarkers", true).Single().Text, Is.EqualTo(UIStrings.SettingsDialog_UI_CheckBox_checkBoxShowBookmarkMarkers));
            Assert.That(tab.Controls.Find("checkBoxShowSearchMarkers", true).Single().Text, Is.EqualTo(UIStrings.SettingsDialog_UI_CheckBox_checkBoxShowSearchMarkers));
            Assert.That(tab.Controls.Find("checkBoxShowFilterMarkers", true).Single().Text, Is.EqualTo(UIStrings.SettingsDialog_UI_CheckBox_checkBoxShowFilterMarkers));
        });

        foreach (var checkBox in tab.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<CheckBox>())
        {
            TestContext.Progress.WriteLine($"{checkBox.Name}: {checkBox.Text}");
        }

        ((CheckBox)tab.Controls.Find("checkBoxShowMarkerBar", true).Single()).Checked = true;
        ((CheckBox)tab.Controls.Find("checkBoxShowHighlightMarkers", true).Single()).Checked = false;
        ((CheckBox)tab.Controls.Find("checkBoxShowBookmarkMarkers", true).Single()).Checked = false;
        ((CheckBox)tab.Controls.Find("checkBoxShowSearchMarkers", true).Single()).Checked = false;
        ((CheckBox)tab.Controls.Find("checkBoxShowFilterMarkers", true).Single()).Checked = true;
        dialog.SaveMarkerBarTab();

        Assert.Multiple(() =>
        {
            Assert.That(preferences.ShowMarkerBar, Is.True);
            Assert.That(preferences.ShowHighlightMarkers, Is.False);
            Assert.That(preferences.ShowBookmarkMarkers, Is.False);
            Assert.That(preferences.ShowSearchMarkers, Is.False);
            Assert.That(preferences.ShowFilterMarkers, Is.True);
        });
    }
}