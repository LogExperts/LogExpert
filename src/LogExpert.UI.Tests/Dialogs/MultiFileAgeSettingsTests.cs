using System.Reflection;

using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.Dialogs;
using LogExpert.UI.Dialogs;

using Moq;

using NUnit.Framework;

using UIStrings = LogExpert.Resources;

namespace LogExpert.UI.Tests.Dialogs;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class MultiFileAgeSettingsTests
{
    [Test]
    public void MultiFileMaskDialog_LoadsAndSavesBothLimits ()
    {
        using var dialog = new MultiFileMaskDialog("app.log")
        {
            FileNamePattern = "*$J(.)",
            MaxDayGap = 3,
            MaxFileAgeDays = 14
        };

        InvokePrivate(dialog, "OnMultiFileMaskDialogLoad");

        var maxDayGap = Find<NumericUpDown>(dialog, "upDownMaxDayGap");
        var maxFileAgeDays = Find<NumericUpDown>(dialog, "upDownMaxFileAgeDays");
        Assert.Multiple(() =>
        {
            Assert.That(maxDayGap.Value, Is.EqualTo(3));
            Assert.That(maxFileAgeDays.Value, Is.EqualTo(14));
            Assert.That(maxFileAgeDays.Maximum, Is.EqualTo(MultiFileOptions.MAX_FILE_AGE_DAYS));
            Assert.That(Find<Label>(dialog, "labelMaxDayGap").Text,
                Is.EqualTo(UIStrings.MultiFileMaskDialog_UI_Label_MaxDayGap));
            Assert.That(Find<Label>(dialog, "labelMaxFileAgeDays").Text,
                Is.EqualTo(UIStrings.MultiFileMaskDialog_UI_Label_MaxFileAgeDays));
        });

        maxDayGap.Value = 5;
        maxFileAgeDays.Value = 30;
        InvokePrivate(dialog, "OnButtonOKClick");

        Assert.Multiple(() =>
        {
            Assert.That(dialog.MaxDayGap, Is.EqualTo(5));
            Assert.That(dialog.MaxFileAgeDays, Is.EqualTo(30));
        });
    }

    [Test]
    public void MultiFileMaskDialog_ClampsAnOversizedPersistedFileAge ()
    {
        using var dialog = new MultiFileMaskDialog("app.log") { MaxFileAgeDays = int.MaxValue };

        InvokePrivate(dialog, "OnMultiFileMaskDialogLoad");

        Assert.That(Find<NumericUpDown>(dialog, "upDownMaxFileAgeDays").Value,
            Is.EqualTo(MultiFileOptions.MAX_FILE_AGE_DAYS));
    }

    [Test]
    public void SettingsDialog_LoadsAndSavesGlobalMaxFileAge ()
    {
        var preferences = new Preferences
        {
            MultiFileOptions = new MultiFileOptions
            {
                FormatPattern = "*$J(.)",
                MaxDayTry = 3,
                MaxFileAgeDays = 14
            }
        };
        var configManager = new Mock<IConfigManager>();
        _ = configManager.SetupGet(manager => manager.Settings).Returns(new Settings());

        using var dialog = new SettingsDialog(preferences, null!, 0, configManager.Object);
        InvokePrivate(dialog, "FillMultifileSettings");
        var maxDayGap = Find<NumericUpDown>(dialog, "upDownMultifileDays");
        var maxFileAgeDays = Find<NumericUpDown>(dialog, "upDownMultifileMaxFileAgeDays");

        Assert.Multiple(() =>
        {
            Assert.That(maxDayGap.Value, Is.EqualTo(3));
            Assert.That(maxFileAgeDays.Value, Is.EqualTo(14));
            Assert.That(maxFileAgeDays.Maximum, Is.EqualTo(MultiFileOptions.MAX_FILE_AGE_DAYS));
        });

        maxDayGap.Value = 7;
        maxFileAgeDays.Value = 30;
        InvokePrivate(dialog, "SaveMultifileData");

        Assert.Multiple(() =>
        {
            Assert.That(preferences.MultiFileOptions.MaxDayTry, Is.EqualTo(7));
            Assert.That(preferences.MultiFileOptions.MaxFileAgeDays, Is.EqualTo(30));
        });
    }

    [Test]
    public void SettingsDialog_ClampsAnOversizedPersistedFileAge ()
    {
        var preferences = new Preferences
        {
            MultiFileOptions = new MultiFileOptions { MaxFileAgeDays = int.MaxValue }
        };
        var configManager = new Mock<IConfigManager>();
        _ = configManager.SetupGet(manager => manager.Settings).Returns(new Settings());

        using var dialog = new SettingsDialog(preferences, null!, 0, configManager.Object);
        InvokePrivate(dialog, "FillMultifileSettings");

        Assert.That(Find<NumericUpDown>(dialog, "upDownMultifileMaxFileAgeDays").Value,
            Is.EqualTo(MultiFileOptions.MAX_FILE_AGE_DAYS));
    }

    private static TControl Find<TControl> (Control parent, string name) where TControl : Control
    {
        return (TControl)parent.Controls.Find(name, searchAllChildren: true).Single();
    }

    private static void InvokePrivate (object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        var arguments = method!.GetParameters().Length == 0 ? null : new object[] { null!, EventArgs.Empty };
        _ = method.Invoke(target, arguments);
    }
}
