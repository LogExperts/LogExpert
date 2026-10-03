using LogExpert.Core.Config;
using LogExpert.Core.Interfaces;
using LogExpert.Dialogs;

using Moq;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Dialogs;

[TestFixture]
[Apartment(ApartmentState.STA)]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class HighlightDialogLayoutTests
{
    [Test]
    public void EnlargingTheDialog_GrowsTheEntryListBothWays ()
    {
        using var dialog = CreateDialog();
        dialog.Show();
        var list = dialog.Controls.Find("listBoxHighlight", true).Single();
        var before = list.Size;

        dialog.Size = new Size(dialog.Width + 200, dialog.Height + 150);

        Assert.That(list.Width, Is.EqualTo(before.Width + 200));
        Assert.That(list.Height, Is.GreaterThan(before.Height + 100));
    }

    [TestCase(1f)]
    [TestCase(1.5f)]
    [TestCase(2f)]
    public void AtTheMinimumSize_ControlsNeitherOverlapNorLeaveTheirContainer (float scale)
    {
        using var dialog = CreateDialog();
        dialog.Scale(new SizeF(scale, scale));
        dialog.Show();
        dialog.Size = dialog.MinimumSize;

        // Windows caps a form at the screen size; small CI screens can't host the scaled minimum.
        Assume.That(dialog.Size, Is.EqualTo(dialog.MinimumSize), "screen too small for the scaled minimum size");

        foreach (var name in new[] { "pnlBackground", "groupBoxGroups" })
        {
            var container = dialog.Controls.Find(name, true).Single();
            var children = container.Controls.Cast<Control>().Where(c => c.Visible).ToList();

            foreach (var child in children)
            {
                Assert.That(container.ClientRectangle.Contains(child.Bounds), Is.True, $"{child.Name} inside {name}");

                foreach (var other in children.Where(o => o != child))
                {
                    Assert.That(child.Bounds.IntersectsWith(other.Bounds), Is.False, $"{child.Name} overlaps {other.Name}");
                }
            }
        }
    }

    private static HighlightDialog CreateDialog ()
    {
        var config = new Mock<IConfigManager>();
        _ = config.SetupGet(c => c.Settings).Returns(new Settings());
        return new HighlightDialog(config.Object) { HighlightGroupList = [] };
    }
}
