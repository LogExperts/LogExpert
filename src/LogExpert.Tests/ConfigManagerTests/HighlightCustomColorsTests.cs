using LogExpert.Core.Config;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

[TestFixture]
public class HighlightCustomColorsTests
{
    [Test]
    public void Palette_SurvivesRoundTrip ()
    {
        var preferences = new Preferences { HighlightCustomColors = [0x00123456, 0x00FFFFFF] };

        var restored = JsonConvert.DeserializeObject<Preferences>(JsonConvert.SerializeObject(preferences));

        Assert.That(restored.HighlightCustomColors, Is.EqualTo(new[] { 0x00123456, 0x00FFFFFF }));
    }

    [Test]
    public void OlderSettings_LoadWithAnEmptyPalette ()
    {
        var preferences = JsonConvert.DeserializeObject<Preferences>("{}");

        Assert.That(preferences.HighlightCustomColors, Is.Empty);
    }

    [Test]
    public void NullPalette_BecomesEmpty ()
    {
        var preferences = JsonConvert.DeserializeObject<Preferences>("{\"HighlightCustomColors\":null}");

        Assert.That(preferences.HighlightCustomColors, Is.Empty);
    }
}
