using System.Drawing;
using System.Linq;

using LogExpert.Core.Config;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

[TestFixture]
public class PreferencesControlCharTests
{
    [Test]
    public void Deserialize_LegacyJsonMissingControlCharSettings_GetsDefaultInstance ()
    {
        const string legacyJson = "{}";

        var prefs = JsonConvert.DeserializeObject<Preferences>(legacyJson);

        Assert.That(prefs, Is.Not.Null);
        Assert.That(prefs!.ControlCharSettings, Is.Not.Null);
        Assert.That(prefs.ControlCharSettings.Substitute, Is.False);
        Assert.That(prefs.ControlCharSettings.Style, Is.EqualTo(ControlCharStyle.ControlPictures));
        Assert.That(prefs.ControlCharSettings.ForeColor.ToArgb(), Is.EqualTo(Color.Gray.ToArgb()));
        Assert.That(prefs.ControlCharSettings.EnabledCodepoints.Count, Is.EqualTo(30));
    }
}
