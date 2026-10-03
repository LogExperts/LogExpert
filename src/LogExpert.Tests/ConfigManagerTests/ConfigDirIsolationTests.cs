using LogExpert.Configuration;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

[TestFixture]
public class ConfigDirIsolationTests
{
    [Test]
    public void Tests_NeverUseTheUsersRealConfigDirectory ()
    {
        var realConfigDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LogExpert");

        Assert.That(ConfigManager.Instance.ConfigDir, Is.Not.EqualTo(realConfigDir));
    }
}
