using System.Reflection;

using LogExpert.Configuration;
using LogExpert.PluginRegistry;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

[TestFixture]
public class ConfigDirIsolationTests
{
    [Test]
    public void Tests_NeverUseTheUsersRealConfigOrPluginTrustDirectory ()
    {
        var realConfigDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LogExpert");

        Assert.That(ConfigManager.Instance.ConfigDir, Is.Not.EqualTo(realConfigDir));

        var trustFile = (string)typeof(PluginValidator).GetField("_configPath", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        Assert.That(trustFile, Does.Not.StartWith(realConfigDir));
    }
}
