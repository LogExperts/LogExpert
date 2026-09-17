using LogExpert.Core.Config;
using LogExpert.Core.Interfaces;
using LogExpert.Dialogs;

using Moq;

using NUnit.Framework;

using UIStrings = LogExpert.Resources;

namespace LogExpert.UI.Tests.Dialogs;

/// <summary>
/// Regression tests for issue #658: populating the settings dialog from preferences set the
/// portable-mode checkbox programmatically, which fired the CheckedChanged handler and ran the
/// full activation flow — showing the "copy settings?" question dialog and (re)creating the
/// marker file — every time the dialog was opened while portable mode was active.
/// </summary>
[TestFixture]
public class SettingsDialogPortableModeTests
{
    private string _testDataPath = null!;
    private string _portableConfigDir = null!;

    private sealed record FillPortableModeResult(bool Finished, Exception? Failure, bool PortableModeAfterFill, string? CheckBoxText);

    [SetUp]
    public void SetUp ()
    {
        _testDataPath = Path.Join(Path.GetTempPath(), "LogExpertSettingsDialogTests", Guid.NewGuid().ToString());
        _portableConfigDir = Path.Join(_testDataPath, "configuration");
    }

    [TearDown]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Test")]
    public void TearDown ()
    {
        if (Directory.Exists(_testDataPath))
        {
            try
            {
                Directory.Delete(_testDataPath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Constructs the dialog and calls FillPortableMode on a disposable STA thread so that a
    /// regression (the CheckedChanged flow showing a modal question dialog again) surfaces as
    /// a Join timeout instead of hanging the run.
    /// </summary>
    private FillPortableModeResult RunFillPortableMode (bool portableMode)
    {
        Mock<IConfigManager> configManager = new();
        _ = configManager.SetupGet(m => m.PortableConfigDir).Returns(_portableConfigDir);
        _ = configManager.SetupGet(m => m.PortableModeSettingsFileName).Returns("portableMode.json");

        Preferences preferences = new()
        {
            PortableMode = portableMode,
        };

        Exception? failure = null;
        var portableModeAfterFill = !portableMode;
        string? checkBoxText = null;

        Thread worker = new(() =>
        {
            try
            {
                using SettingsDialog dialog = new(preferences, null!, 0, configManager.Object);
                dialog.FillPortableMode();

                portableModeAfterFill = dialog.Preferences.PortableMode;
                checkBoxText = dialog.Controls.Find("checkBoxPortableMode", searchAllChildren: true).Single().Text;
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        })
        {
            IsBackground = true,
        };
        worker.SetApartmentState(ApartmentState.STA);
        worker.Start();
        var finished = worker.Join(TimeSpan.FromSeconds(20));

        return new FillPortableModeResult(finished, failure, portableModeAfterFill, checkBoxText);
    }

    [Test]
    [Description("Issue #658: opening the settings dialog with portable mode active must not prompt or touch files")]
    public void FillPortableMode_PortableModeActive_PerformsNoSideEffects ()
    {
        var result = RunFillPortableMode(portableMode: true);

        Assert.That(result.Finished, Is.True,
            "FillPortableMode must return without user interaction — a modal question dialog means the portable-mode activation flow ran again");
        Assert.Multiple(() =>
        {
            Assert.That(result.Failure, Is.Null, $"FillPortableMode must not throw, but threw: {result.Failure}");
            Assert.That(Directory.Exists(_portableConfigDir), Is.False,
                "Populating the dialog must not create the portable configuration directory or marker file");
            Assert.That(result.PortableModeAfterFill, Is.True, "Preferences must be left untouched");
            Assert.That(result.CheckBoxText, Is.EqualTo(UIStrings.SettingsDialog_UI_DeActivatePortableMode),
                "The checkbox label must reflect the active portable mode state");
        });
    }

    [Test]
    [Description("Issue #658: populating the dialog with portable mode off must also be side-effect free")]
    public void FillPortableMode_PortableModeInactive_PerformsNoSideEffects ()
    {
        var result = RunFillPortableMode(portableMode: false);

        Assert.That(result.Finished, Is.True,
            "FillPortableMode must return without user interaction");
        Assert.Multiple(() =>
        {
            Assert.That(result.Failure, Is.Null, $"FillPortableMode must not throw, but threw: {result.Failure}");
            Assert.That(Directory.Exists(_portableConfigDir), Is.False,
                "Populating the dialog must not touch the portable configuration directory");
            Assert.That(result.PortableModeAfterFill, Is.False, "Preferences must be left untouched");
            Assert.That(result.CheckBoxText, Is.EqualTo(UIStrings.SettingsDialog_UI_ActivatePortableMode),
                "The checkbox label must reflect the inactive portable mode state");
        });
    }
}
