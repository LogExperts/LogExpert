using System.Runtime.Versioning;

using LogExpert.PluginRegistry;

using NUnit.Framework;

namespace LogExpert.UI.Tests;

[SetUpFixture]
[SupportedOSPlatform("windows")]
public class UiTestSetup
{
    private readonly string _pluginTrustDir = Path.Join(Path.GetTempPath(), "LogExpert_UiTestPluginTrust_" + Guid.NewGuid().ToString("N"));

    [OneTimeSetUp]
    public void EnableApplicationDpiMode ()
    {
        // Match LogExpert.csproj before any fixture creates a window handle.
        _ = Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Assert.That(Application.HighDpiMode, Is.EqualTo(HighDpiMode.PerMonitorV2));

        // On by default only under a debugger; off, a control touched from a worker thread can create its
        // window there and later deadlock the UI thread instead of failing the test.
        Control.CheckForIllegalCrossThreadCalls = true;

        // Until initialized, PluginValidator reads and writes trusted-plugins.json in the developer's %APPDATA%\LogExpert.
        _ = Directory.CreateDirectory(_pluginTrustDir);
        PluginValidator.Initialize(_pluginTrustDir);
    }

    [OneTimeTearDown]
    public void RemovePluginTrustDir ()
    {
        Directory.Delete(_pluginTrustDir, recursive: true);
    }
}