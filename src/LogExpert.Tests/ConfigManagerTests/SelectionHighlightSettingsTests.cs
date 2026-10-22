using LogExpert.Core.Config;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

[TestFixture]
public class SelectionHighlightSettingsTests
{
    [Test]
    public void CustomAppearance_SurvivesRoundTrip ()
    {
        var preferences = new Preferences
        {
            SelectionHighlight = new() { Outline = true, CustomColor = Color.FromArgb(25, 100, 180) }
        };

        var restored = JsonConvert.DeserializeObject<Preferences>(JsonConvert.SerializeObject(preferences));

        Assert.That(restored.SelectionHighlight.Outline, Is.True);
        Assert.That(restored.SelectionHighlight.CustomColor, Is.EqualTo(Color.FromArgb(25, 100, 180)));
    }

    [Test]
    public void OlderSettings_KeepSystemFilledSelection ()
    {
        var preferences = JsonConvert.DeserializeObject<Preferences>("{}");

        Assert.That(preferences.SelectionHighlight.Outline, Is.False);
        Assert.That(preferences.SelectionHighlight.CustomColor, Is.Null);
    }
}