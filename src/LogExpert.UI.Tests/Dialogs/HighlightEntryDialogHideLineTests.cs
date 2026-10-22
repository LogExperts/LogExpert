using System.Reflection;

using LogExpert.Core.Classes.Highlight;
using LogExpert.UI.Dialogs.Highlight;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Dialogs;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class HighlightEntryDialogHideLineTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void Load_ShowsTheEntrysHideFlag (bool hidden)
    {
        using var dialog = new HighlightEntryDialog(new HighlightEntry { SearchText = "DEBUG", IsHideLine = hidden }, [], false);

        Assert.That(HideCheckBox(dialog).Checked, Is.EqualTo(hidden));
    }

    [Test]
    public void Ok_SavesTheHideFlag ()
    {
        var entry = new HighlightEntry { SearchText = "DEBUG" };
        using var dialog = new HighlightEntryDialog(entry, [], false);
        HideCheckBox(dialog).Checked = true;

        _ = typeof(HighlightEntryDialog).GetMethod("OnOkClick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(dialog, [dialog, EventArgs.Empty]);

        Assert.That(entry.IsHideLine, Is.True);
    }

    private static CheckBox HideCheckBox (Form dialog)
    {
        return (CheckBox)dialog.Controls.Find("_checkBoxHideLine", true).Single();
    }
}
