using System.Runtime.Versioning;

using NUnit.Framework;

namespace LogExpert.Tests;

[SetUpFixture]
[SupportedOSPlatform("windows")]
public class UiTestSetup
{
    [OneTimeSetUp]
    public void EnableApplicationDpiMode ()
    {
        // Match LogExpert.csproj before any fixture creates a window handle.
        _ = Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Assert.That(Application.HighDpiMode, Is.EqualTo(HighDpiMode.PerMonitorV2));
    }
}