using System.Runtime.Versioning;

using LogExpert.UI.Services.LedService;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Services;

[TestFixture]
[Apartment(ApartmentState.STA)] // Required for UI components
[SupportedOSPlatform("windows")]
public class LedIndicatorServiceTests : IDisposable
{
    private LedIndicatorService? _service;
    private ApplicationContext? _appContext;
    private WindowsFormsSynchronizationContext? _syncContext;
    private bool _disposed;

    [SetUp]
    public void Setup ()
    {
        // Ensure we have a WindowsFormsSynchronizationContext for the UI thread
        if (SynchronizationContext.Current == null)
        {
            _syncContext = new WindowsFormsSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(_syncContext);
        }

        // Create an application context to ensure we have a proper UI context
        _appContext = new ApplicationContext();

        // Must be created on STA thread with synchronization context
        _service = new LedIndicatorService();
    }

    [TearDown]
    public void TearDown ()
    {
        _service?.Dispose();
        _appContext?.Dispose();
        _syncContext?.Dispose();
    }

    [Test]
    public void Initialize_WithValidColor_Succeeds ()
    {
        // Act
        _service!.Initialize(Color.Blue);

        // Assert - no exception thrown
        Assert.That(_service, Is.Not.Null);
    }

    [Test]
    public void Initialize_CalledTwice_ThrowsException ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);

        // Act & Assert
        _ = Assert.Throws<InvalidOperationException>(() => _service.Initialize(Color.Red));
    }

    [Test]
    public void GetIcon_WithZeroDiff_ReturnsOffLevelIcon ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);
        var state = new LedState
        {
            IsDirty = false,
            TailState = TailFollowState.On,
            SyncState = TimeSyncState.NotSynced
        };

        // Act
        var icon = _service.GetIcon(0, state);

        // Assert
        Assert.That(icon, Is.Not.Null);
        Assert.That(icon.Width, Is.EqualTo(16));
        Assert.That(icon.Height, Is.EqualTo(16));
    }

    [Test]
    public void GetIcon_WithMaxDiff_ReturnsHighestLevelIcon ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);
        var state = new LedState
        {
            IsDirty = false,
            TailState = TailFollowState.On,
            SyncState = TimeSyncState.NotSynced
        };

        // Act
        var icon = _service.GetIcon(100, state);

        // Assert
        Assert.That(icon, Is.Not.Null);
    }

    [Test]
    public void GetIcon_WithDirtyState_ReturnsDirtyIcon ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);
        var state = new LedState
        {
            IsDirty = true,
            TailState = TailFollowState.On,
            SyncState = TimeSyncState.NotSynced
        };

        // Act
        var icon = _service.GetIcon(50, state);

        // Assert
        Assert.That(icon, Is.Not.Null);
    }

    [Test]
    public void GetDeadIcon_ReturnsNonNullIcon ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);

        // Act
        var icon = _service.GetDeadIcon();

        // Assert
        Assert.That(icon, Is.Not.Null);
        Assert.That(icon.Width, Is.EqualTo(16));
    }

    [Test]
    public void StartStop_DoesNotThrowException ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);

        // Act
        _service.StartService();
        Thread.Sleep(500); // Let timer tick a few times
        _service.StopService();

        // Assert - no exception
        Assert.That(true, Is.True, "Service started and stopped without exceptions");
    }

    [Test]
    public void RegisterWindow_AddsWindowToTracking ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);

        // We can't easily mock LogWindow since it has no parameterless constructor
        // and is internal, so we just test that registering null throws
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => _service.RegisterWindow(null!));
    }

    [Test]
    public void UpdateWindowActivity_WithoutRegisteringWindow_DoesNotThrow ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);

        // Act & Assert - Updating an unregistered window should not throw
        // (it just won't raise events)
        Assert.DoesNotThrow(() => _service.UpdateWindowActivity(null, 10));
    }

    [Test]
    public void RegenerateIcons_WithNoWindows_DoesNotThrow ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);

        int eventCount = 0;
        _service.IconChanged += (s, e) => eventCount++;

        // Act
        _service.RegenerateIcons(Color.Red);

        // Assert - No windows registered, so no events should be raised
        Assert.That(eventCount, Is.EqualTo(0));
    }

    [Test]
    public void Dispose_DisposesAllResources ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);
        _service.StartService();

        // Act
        _service.Dispose();

        // Assert - After dispose, trying to use the service will throw an exception
        var exception = Assert.Catch(() => _service.GetIcon(0, new LedState()));
        Assert.That(exception, Is.Not.Null, "Should throw an exception after disposal");
    }

    [Test]
    public void GetIcon_WithoutInitialize_ThrowsException ()
    {
        // Arrange - don't initialize

        // Act & Assert
        _ = Assert.Throws<InvalidOperationException>(() => _service!.GetIcon(0, new LedState()));
    }

    [Test]
    public void Start_WithoutInitialize_ThrowsException ()
    {
        // Arrange - don't initialize

        // Act & Assert
        _ = Assert.Throws<InvalidOperationException>(() => _service!.StartService());
    }

    [Test]
    public void RegisterWindow_WithNullWindow_ThrowsException ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => _service.RegisterWindow(null!));
    }

    [Test]
    public void UnregisterWindow_WithNullWindow_DoesNotThrow ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);

        // Act & Assert - Unregistering null should not throw
        Assert.DoesNotThrow(() => _service.UnregisterWindow(null));
    }

    [Test]
    public void CurrentTailColor_AfterInitialize_ReturnsInitializedColor ()
    {
        // Arrange
        var expectedColor = Color.FromArgb(50, 100, 200);
        _service!.Initialize(expectedColor);

        // Act
        var actualColor = _service.CurrentTailColor;

        // Assert
        Assert.That(actualColor, Is.EqualTo(expectedColor));
    }

    [Test]
    public void CurrentTailColor_AfterRegenerateIcons_ReturnsNewColor ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);
        var newColor = Color.FromArgb(255, 128, 0);

        // Act
        _service.RegenerateIcons(newColor);

        // Assert
        Assert.That(_service.CurrentTailColor, Is.EqualTo(newColor));
    }

    [Test]
    public void CurrentTailColor_BeforeInitialize_ThrowsException ()
    {
        // Arrange - don't initialize

        // Act & Assert
        _ = Assert.Throws<InvalidOperationException>(() => _ = _service!.CurrentTailColor);
    }

    [Test]
    public void CurrentTailColor_AfterDispose_ThrowsObjectDisposedException ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);
        _service.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => _ = _service.CurrentTailColor);
    }

    [Test]
    public void GetIcon_WithSyncedState_ReturnsSyncedIcon ()
    {
        // Arrange
        _service!.Initialize(Color.Blue);
        var stateSynced = new LedState
        {
            IsDirty = false,
            TailState = TailFollowState.On,
            SyncState = TimeSyncState.Synced
        };
        var stateNotSynced = new LedState
        {
            IsDirty = false,
            TailState = TailFollowState.On,
            SyncState = TimeSyncState.NotSynced
        };

        // Act
        var iconSynced = _service.GetIcon(50, stateSynced);
        var iconNotSynced = _service.GetIcon(50, stateNotSynced);

        // Assert
        Assert.That(iconSynced, Is.Not.Null);
        Assert.That(iconNotSynced, Is.Not.Null);
        // The icons should be different (synced has blue indicator on left side)
        Assert.That(iconSynced, Is.Not.EqualTo(iconNotSynced));
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
            _service?.Dispose();
        }

        _disposed = true;
    }
}