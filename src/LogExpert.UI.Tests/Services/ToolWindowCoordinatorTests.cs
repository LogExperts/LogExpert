using System.Runtime.Versioning;

using LogExpert.Core.Config;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Services.ToolWindowCoordinatorService;

using Moq;

using NUnit.Framework;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Tests.Services;

[TestFixture]
[Apartment(ApartmentState.STA)]
[SupportedOSPlatform("windows")]
public class ToolWindowCoordinatorTests : IDisposable
{
    private Mock<IConfigManager> _configManagerMock;
    private Settings _settings;
    private ToolWindowCoordinator _coordinator;
    private WindowsFormsSynchronizationContext? _syncContext;
    private bool _disposed;

    [SetUp]
    public void Setup ()
    {
        if (SynchronizationContext.Current == null)
        {
            _syncContext = new WindowsFormsSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(_syncContext);
        }

        _configManagerMock = new Mock<IConfigManager>();
        _settings = new Settings();

        // Materialize Font from FontString (mirrors ConfigManager.InitializeFont)
        var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));
        _settings.Preferences.Font = (Font)converter.ConvertFromInvariantString(_settings.Preferences.FontString)!;

        _ = _configManagerMock.Setup(cm => cm.Settings).Returns(_settings);

        _coordinator = new ToolWindowCoordinator(_configManagerMock.Object);
    }

    [TearDown]
    public void TearDown ()
    {
        _coordinator?.Dispose();
        _syncContext?.Dispose();
    }

    public void Dispose ()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _coordinator?.Dispose();
            _syncContext?.Dispose();
        }

        _disposed = true;
    }

    [Test]
    public void Initialize_CreatesBookmarkWindow ()
    {
        // Act
        _coordinator.Initialize();

        // Assert — GetDockContent should return non-null for bookmark persist string
        var content = _coordinator.GetDockContent("BookmarkWindow");
        Assert.That(content, Is.Not.Null);
    }

    [Test]
    public void Destroy_ClosesBookmarkWindow ()
    {
        // Arrange
        _coordinator.Initialize();

        // Act
        _coordinator.Destroy();

        // Assert — GetDockContent should return null after destroy
        var content = _coordinator.GetDockContent("BookmarkWindow");
        Assert.That(content, Is.Null);
    }

    [Test]
    public void GetDockContent_ReturnsBoo‌kmarkWindow_ForMatchingPersistString ()
    {
        // Arrange
        _coordinator.Initialize();

        // Act
        var result = _coordinator.GetDockContent("BookmarkWindow");

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void GetDockContent_ReturnsNull_ForNonMatchingPersistString ()
    {
        // Arrange
        _coordinator.Initialize();

        // Act
        var result = _coordinator.GetDockContent("SomeOtherWindow");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Disconnect_WithoutConnect_DoesNotThrow ()
    {
        // Arrange
        _coordinator.Initialize();

        // Act & Assert
        Assert.DoesNotThrow(_coordinator.Disconnect);
    }

    [Test]
    public void ApplyPreferences_DoesNotThrow_WhenInitialized ()
    {
        // Arrange
        _coordinator.Initialize();

        // Act & Assert
        Assert.DoesNotThrow(() => _coordinator.ApplyPreferences(new Font("Courier New", 10f), true, 500, SettingsFlags.All));
    }

    [Test]
    public void SetLineColumnVisible_DoesNotThrow_WhenInitialized ()
    {
        // Arrange
        _coordinator.Initialize();

        // Act & Assert
        Assert.DoesNotThrow(() => _coordinator.SetLineColumnVisible(true));
        Assert.DoesNotThrow(() => _coordinator.SetLineColumnVisible(false));
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes ()
    {
        // Arrange
        _coordinator.Initialize();

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            _coordinator.Dispose();
            _coordinator.Dispose();
        });

        _coordinator = null; // prevent double dispose in TearDown
    }

    [Test]
    public void ToggleBookmarkVisibility_WhenNotInitialized_DoesNotThrow ()
    {
        // Arrange — coordinator not initialized (no bookmark window)
        using var form = new Form();
        using var dockPanel = new DockPanel();
        form.Controls.Add(dockPanel);

        // Act & Assert
        Assert.DoesNotThrow(() => _coordinator.ToggleBookmarkVisibility(dockPanel));
    }

    [Test]
    public void GetDockContent_BeforeInitialize_ReturnsNull ()
    {
        // Act
        var result = _coordinator.GetDockContent("BookmarkWindow");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ApplyPreferences_BeforeInitialize_DoesNotThrow ()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _coordinator.ApplyPreferences(new Font("Courier New", 10f), true, 500, SettingsFlags.All));
    }

    [Test]
    public void SetLineColumnVisible_BeforeInitialize_DoesNotThrow ()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _coordinator.SetLineColumnVisible(true));
    }
}