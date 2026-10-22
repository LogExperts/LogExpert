using LogExpert.Core.Config;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

[TestFixture]
public class PreferencesMarkerBarTests
{
    [Test]
    public void Defaults_EnableMarkerBarOnDemand_AndShowAllMarkerKinds ()
    {
        var preferences = new Preferences();

        Assert.That(preferences.ShowMarkerBar, Is.False);
        Assert.That(preferences.ShowHighlightMarkers, Is.True);
        Assert.That(preferences.ShowBookmarkMarkers, Is.True);
        Assert.That(preferences.ShowSearchMarkers, Is.True);
        Assert.That(preferences.ShowFilterMarkers, Is.True);
    }

    [Test]
    public void RoundTrip_PreservesMarkerBarPreferences ()
    {
        var original = new Preferences
        {
            ShowMarkerBar = true,
            ShowHighlightMarkers = false,
            ShowBookmarkMarkers = false,
            ShowSearchMarkers = false,
            ShowFilterMarkers = true
        };

        var restored = JsonConvert.DeserializeObject<Preferences>(JsonConvert.SerializeObject(original));

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored!.ShowMarkerBar, Is.True);
        Assert.That(restored.ShowHighlightMarkers, Is.False);
        Assert.That(restored.ShowBookmarkMarkers, Is.False);
        Assert.That(restored.ShowSearchMarkers, Is.False);
        Assert.That(restored.ShowFilterMarkers, Is.True);
    }

    [Test]
    public void Deserialize_LegacyJson_UsesMarkerBarDefaults ()
    {
        var restored = JsonConvert.DeserializeObject<Preferences>("{\"MaxLineLength\":1000}");

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored!.ShowMarkerBar, Is.False);
        Assert.That(restored.ShowHighlightMarkers, Is.True);
        Assert.That(restored.ShowBookmarkMarkers, Is.True);
        Assert.That(restored.ShowSearchMarkers, Is.True);
        Assert.That(restored.ShowFilterMarkers, Is.True);
    }
}