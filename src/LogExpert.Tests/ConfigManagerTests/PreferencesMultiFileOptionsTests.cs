using LogExpert.Core.Config;
using LogExpert.Core.Entities;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

[TestFixture]
public class PreferencesMultiFileOptionsTests
{
    [Test]
    public void Deserialize_LegacyJsonWithoutMaxFileAgeDays_DefaultsToOff ()
    {
        const string legacyJson = """
            {
              "MultiFileOptions": {
                "FormatPattern": "*$J(.)",
                "MaxDayTry": 7
              }
            }
            """;

        var preferences = JsonConvert.DeserializeObject<Preferences>(legacyJson);

        Assert.That(preferences, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(preferences!.MultiFileOptions.MaxDayTry, Is.EqualTo(7));
            Assert.That(preferences.MultiFileOptions.MaxFileAgeDays, Is.Zero);
        });
    }

    [Test]
    public void RoundTrip_PreservesMaxFileAgeDays ()
    {
        var preferences = new Preferences
        {
            MultiFileOptions = new MultiFileOptions { MaxFileAgeDays = 14 }
        };

        var restored = JsonConvert.DeserializeObject<Preferences>(JsonConvert.SerializeObject(preferences));

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored!.MultiFileOptions.MaxFileAgeDays, Is.EqualTo(14));
    }
}
