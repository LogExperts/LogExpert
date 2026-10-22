using LogExpert.Core.Config;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

[TestFixture]
public class PreferencesColorModeTests
{
    [Test]
    public void Deserialize_LegacyDarkModeTrue_MapsToDark ()
    {
        const string legacyJson = "{\"DarkMode\": true}";

        var prefs = JsonConvert.DeserializeObject<Preferences>(legacyJson);

        Assert.That(prefs, Is.Not.Null);
        Assert.That(prefs!.ColorMode, Is.EqualTo(ColorMode.Dark));
    }

    [Test]
    public void Deserialize_LegacyDarkModeFalse_MapsToLight ()
    {
        // Unchecked "Dark Mode" meant forced light, not follow-OS (issue #698).
        const string legacyJson = "{\"DarkMode\": false}";

        var prefs = JsonConvert.DeserializeObject<Preferences>(legacyJson);

        Assert.That(prefs, Is.Not.Null);
        Assert.That(prefs!.ColorMode, Is.EqualTo(ColorMode.Light));
    }

    [Test]
    public void Deserialize_NoColorSetting_DefaultsToLight ()
    {
        const string legacyJson = "{}";

        var prefs = JsonConvert.DeserializeObject<Preferences>(legacyJson);

        Assert.That(prefs, Is.Not.Null);
        Assert.That(prefs!.ColorMode, Is.EqualTo(ColorMode.Light));
    }

    [Test]
    public void Deserialize_ColorModeString_ReadsEnum ()
    {
        const string json = "{\"ColorMode\": \"System\"}";

        var prefs = JsonConvert.DeserializeObject<Preferences>(json);

        Assert.That(prefs, Is.Not.Null);
        Assert.That(prefs!.ColorMode, Is.EqualTo(ColorMode.System));
    }

    [Test]
    public void Deserialize_ColorModeWinsOverLegacyDarkMode ()
    {
        // A file that carries both keys must obey the new one, regardless of key order.
        const string json = "{\"ColorMode\": \"Light\", \"DarkMode\": true}";

        var prefs = JsonConvert.DeserializeObject<Preferences>(json);

        Assert.That(prefs, Is.Not.Null);
        Assert.That(prefs!.ColorMode, Is.EqualTo(ColorMode.Light));
    }

    [Test]
    public void Serialize_WritesColorModeAsString_AndNoLegacyDarkMode ()
    {
        var prefs = new Preferences { ColorMode = ColorMode.System };

        var json = JsonConvert.SerializeObject(prefs);

        Assert.That(json, Does.Contain("\"ColorMode\":\"System\""));
        Assert.That(json, Does.Not.Contain("\"DarkMode\""));
    }
}
