using LogExpert.PluginRegistry;

using NUnit.Framework;

// Outside any namespace so it runs once before every fixture in the assembly.
[SetUpFixture]
public class PluginTrustIsolationSetup
{
    private readonly string _configDir = Path.Join(Path.GetTempPath(), "LogExpert_TestPluginTrust_" + Guid.NewGuid().ToString("N"));

    // Until initialized, PluginValidator reads and writes trusted-plugins.json in the developer's %APPDATA%\LogExpert.
    [OneTimeSetUp]
    public void RedirectPluginTrust ()
    {
        _ = Directory.CreateDirectory(_configDir);
        PluginValidator.Initialize(_configDir);
    }

    [OneTimeTearDown]
    public void RemovePluginTrustDir ()
    {
        Directory.Delete(_configDir, recursive: true);
    }
}
