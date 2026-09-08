using LogExpert.Configuration;
using LogExpert.Core.Config;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

[TestFixture]
public class LegacyPreferencesMigratorTests
{
    [Test]
    public void V0Settings_WithMaskPrioTrue_BecomesMaskThenHistory ()
    {
        var settings = new Settings
        {
            SettingsVersion = 0,
        };
#pragma warning disable CS0618
        settings.Preferences.MaskPrio = true;
#pragma warning restore CS0618

        var changed = LegacyPreferencesMigrator.Migrate(settings);

        Assert.That(changed, Is.True);
        Assert.That(settings.Preferences.ColumnizerSelectionPriority, Is.EqualTo(ColumnizerSelectionPriority.MaskThenHistory));
        Assert.That(settings.SettingsVersion, Is.EqualTo(1));
    }

    [Test]
    public void V0Settings_WithMaskPrioFalse_KeepsDefaultPriority ()
    {
        var settings = new Settings { SettingsVersion = 0 };

        _ = LegacyPreferencesMigrator.Migrate(settings);

        Assert.That(settings.Preferences.ColumnizerSelectionPriority, Is.EqualTo(ColumnizerSelectionPriority.HistoryThenMask));
        Assert.That(settings.SettingsVersion, Is.EqualTo(1));
    }

    [Test]
    public void V0Settings_PreExistingMaskEntries_AreRewrittenToRegex ()
    {
        var settings = new Settings { SettingsVersion = 0 };
        settings.Preferences.ColumnizerMaskList.Add(new ColumnizerMaskEntry
        {
            Mask = @".+\.log$",
            ColumnizerName = "JsonColumnizer",
            // Default-loaded value is Glob (the new default).
        });

        _ = LegacyPreferencesMigrator.Migrate(settings);

        Assert.That(settings.Preferences.ColumnizerMaskList[0].Type, Is.EqualTo(MaskType.Regex));
    }

    [Test]
    public void CurrentSettings_IsNoOp ()
    {
        var settings = new Settings { SettingsVersion = LegacyPreferencesMigrator.CURRENT_SETTINGS_VERSION };
        settings.Preferences.ColumnizerMaskList.Add(new ColumnizerMaskEntry
        {
            Mask = "*.log",
            ColumnizerName = "C",
            Type = MaskType.Glob,
        });

        var changed = LegacyPreferencesMigrator.Migrate(settings);

        Assert.That(changed, Is.False);
        Assert.That(settings.Preferences.ColumnizerMaskList[0].Type, Is.EqualTo(MaskType.Glob));
    }

    [Test]
    public void NullSettings_Throws ()
    {
        _ = Assert.Throws<ArgumentNullException>(() => LegacyPreferencesMigrator.Migrate(null!));
    }
}
