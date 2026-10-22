using LogExpert.Core.Config;
using LogExpert.UI.ControlCharDisplay;

using NUnit.Framework;

namespace LogExpert.Tests.ControlCharDisplay;

[TestFixture]
public class ControlCharPresetProviderTests
{
    [Test]
    public void All_ContainsAllC0AndDel_33Entries ()
    {
        var expected = Enumerable.Range(0, 32).Append(0x7F).ToHashSet();

        Assert.That(ControlCharPresetProvider.All, Is.EquivalentTo(expected));
        Assert.That(ControlCharPresetProvider.All.Count, Is.EqualTo(33));
    }

    [Test]
    public void None_IsEmpty ()
    {
        Assert.That(ControlCharPresetProvider.None, Is.Empty);
    }

    [Test]
    public void NonWhitespaceDefaults_IsAllMinusTabLfCr_30Entries ()
    {
        var expected = Enumerable.Range(0, 32)
            .Append(0x7F)
            .Where(cp => cp is not 0x09 and not 0x0A and not 0x0D)
            .ToHashSet();

        Assert.That(ControlCharPresetProvider.NonWhitespaceDefaults, Is.EquivalentTo(expected));
        Assert.That(ControlCharPresetProvider.NonWhitespaceDefaults.Count, Is.EqualTo(30));
    }

    [Test]
    public void ReturnedSets_AreImmutable ()
    {
        // FrozenSet implements ISet but throws on mutation; verify that contract.
        Assert.Multiple(() =>
        {
            _ = Assert.Throws<NotSupportedException>(() => ((ISet<int>)ControlCharPresetProvider.All).Add(99));
            _ = Assert.Throws<NotSupportedException>(() => ((ISet<int>)ControlCharPresetProvider.None).Add(99));
            _ = Assert.Throws<NotSupportedException>(() => ((ISet<int>)ControlCharPresetProvider.NonWhitespaceDefaults).Add(99));
        });
    }

    [Test]
    public void NonWhitespaceDefaults_EqualsControlCharSettingsDefault ()
    {
        // Sanity check that the preset matches what a fresh ControlCharSettings yields.
        var freshSettings = new ControlCharSettings();
        Assert.That(freshSettings.EnabledCodepoints, Is.EquivalentTo(ControlCharPresetProvider.NonWhitespaceDefaults));
    }
}
