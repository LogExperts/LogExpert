using System.Runtime.Versioning;

using NUnit.Framework;

namespace LogExpert.UI.Tests;

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

        // On by default only under a debugger; off, a control touched from a worker thread can create its
        // window there and later deadlock the UI thread instead of failing the test.
        Control.CheckForIllegalCrossThreadCalls = true;
    }
}