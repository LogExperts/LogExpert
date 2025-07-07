using LogExpert.Config;
using LogExpert.Core.Config;

using Newtonsoft.Json;

using NUnit.Framework;

using System.IO;

namespace LogExpert.Tests;

[TestFixture]
public class JSONSaveTest
{
    [Test(Author = "Hirogen", Description = "Save Options as JSON and Check if the written file can be cast again into the settings object")]
    public void SaveOptionsAsJSON()
    {
        ConfigManager.Instance.Settings.AlwaysOnTop = true;
        ConfigManager.Instance.Save(SettingsFlags.All);
        var configDir = ConfigManager.Instance.ConfigDir;
        var settingsFile = configDir + "\\settings.json";

        Settings settings = null;
        
        Assert.DoesNotThrow(CastSettings);
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.AlwaysOnTop, Is.True);

        ConfigManager.Instance.Settings.AlwaysOnTop = false;
        ConfigManager.Instance.Save(SettingsFlags.All);
        
        settings = null;
        Assert.DoesNotThrow(CastSettings);

        Assert.That(settings, !Is.Null);
        Assert.That(settings.AlwaysOnTop, Is.False);


        void CastSettings()
        {
            settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFile));
        }
    }
}
