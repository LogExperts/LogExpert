using System.Runtime.Versioning;

using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Interface;

using Moq;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
[SupportedOSPlatform("windows")]
public class LogWindowPluginRegistryTests
{
    private WindowsFormsSynchronizationContext _syncContext;

    [SetUp]
    public void Setup ()
    {
        if (SynchronizationContext.Current == null)
        {
            _syncContext = new WindowsFormsSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(_syncContext);
        }
    }

    [TearDown]
    public void TearDown ()
    {
        _syncContext?.Dispose();
    }

    [Test]
    public void Constructor_UsesInjectedPluginRegistryForInitialColumnizer ()
    {
        var coordinatorMock = new Mock<ILogWindowCoordinator>();
        _ = coordinatorMock.Setup(c => c.ResolveHighlightGroup(It.IsAny<string?>(), It.IsAny<string?>())).Returns(new HighlightGroup());
        _ = coordinatorMock.Setup(c => c.SearchParams).Returns(new SearchParams());

        var configManagerMock = new Mock<IConfigManager>();
        _ = configManagerMock.Setup(cm => cm.Settings).Returns(new Settings());

        // A fresh instance the global registry cannot contain: reference equality below
        // proves the columnizer came from the injected registry, not PluginRegistry.Instance.
        var markerColumnizer = new DefaultLogfileColumnizer();
        var registryMock = new Mock<IPluginRegistry>();
        _ = registryMock.Setup(r => r.RegisteredColumnizers).Returns([markerColumnizer]);

        using var logWindow = new LogWindow(
            coordinatorMock.Object,
            "test.log",
            true,
            false,
            configManagerMock.Object,
            registryMock.Object);

        Assert.That(logWindow.CurrentColumnizer, Is.SameAs(markerColumnizer));
    }
}
