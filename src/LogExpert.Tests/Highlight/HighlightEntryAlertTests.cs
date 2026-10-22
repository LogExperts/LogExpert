using LogExpert.Core.Classes.Highlight;

using NUnit.Framework;

namespace LogExpert.Tests.Highlight;

[TestFixture]
public class HighlightEntryAlertTests
{
    [Test]
    public void Defaults_AlertOnHitIsFalse_CooldownIsTwo_SoundFileIsEmpty ()
    {
        var entry = new HighlightEntry();

        Assert.Multiple(() =>
        {
            Assert.That(entry.AlertOnHit, Is.False);
            Assert.That(entry.CooldownSeconds, Is.EqualTo(2));
            Assert.That(entry.SoundFilePath, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void Clone_CopiesAlertFields ()
    {
        var entry = new HighlightEntry
        {
            SearchText = "ERROR",
            AlertOnHit = true,
            SoundFilePath = @"C:\sounds\alert.wav",
            CooldownSeconds = 15,
        };

        var clone = (HighlightEntry)entry.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(clone.AlertOnHit, Is.True);
            Assert.That(clone.SoundFilePath, Is.EqualTo(@"C:\sounds\alert.wav"));
            Assert.That(clone.CooldownSeconds, Is.EqualTo(15));
            Assert.That(clone, Is.Not.SameAs(entry));
        });
    }

    [Test]
    public void Clone_DefaultsRoundTrip ()
    {
        var entry = new HighlightEntry { SearchText = "x" };

        var clone = (HighlightEntry)entry.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(clone.AlertOnHit, Is.False);
            Assert.That(clone.SoundFilePath, Is.EqualTo(string.Empty));
            Assert.That(clone.CooldownSeconds, Is.EqualTo(2));
        });
    }
}
