using LogExpert.Configuration;

using NUnit.Framework;

// Outside any namespace so it runs once before every fixture in the assembly.
[SetUpFixture]
public class ConfigDirIsolationSetup
{
    private string _configDir;

    [OneTimeSetUp]
    public void RedirectConfigDir ()
    {
        _configDir = Path.Join(Path.GetTempPath(), "LogExpert_TestConfig_" + Guid.NewGuid().ToString("N"));
        ConfigManager.Instance.ConfigDir = _configDir;
    }

    [OneTimeTearDown]
    public void RemoveConfigDir ()
    {
        if (Directory.Exists(_configDir))
        {
            Directory.Delete(_configDir, recursive: true);
        }
    }
}
