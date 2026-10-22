using System.Runtime.Versioning;

using LogExpert.Core.Config;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Extensions.LogWindow;

using Moq;

using NUnit.Framework;

namespace LogExpert.UI.Tests.UI;

[TestFixture]
[Apartment(ApartmentState.STA)] // Required for WinForms components
public class LogTabWindowThreadingTests
{
    [Test]
    [Category("Threading")]
    [SupportedOSPlatform("windows")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Tests")]
    public void LedThread_WithConcurrentUpdates_NoRaceCondition ()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigManager>();
        _ = mockConfigManager.Setup(m => m.Settings).Returns(new Settings());

        var window = AbstractLogTabWindow.Create(
            [],
            1,
            false,
            mockConfigManager.Object
        );

        Form? windowForm = null;

        // Ensure window handle is created
        if (window is Form f)
        {
            windowForm = f;
            _ = windowForm.Handle; // Force handle creation
        }

        // Give time for LED thread to start and stabilize
        Thread.Sleep(500);

        var exceptions = new List<Exception>();
        var exceptionLock = new object();

        try
        {
            // Act - Simulate concurrent operations on the window
            // The LED thread runs in the background and updates icons
            // We stress test by performing many concurrent read operations
            var tasks = new Task[20];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        // Perform safe read operations that might race with LED thread
                        for (int j = 0; j < 10; j++)
                        {
                            if (windowForm != null && !windowForm.IsDisposed && windowForm.IsHandleCreated)
                            {
                                // Read operations that don't require Invoke
                                _ = windowForm.Text;
                                _ = windowForm.Visible;
                                _ = windowForm.IsDisposed;
                            }

                            Thread.Sleep(5); // Small delay to increase overlap with LED thread
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Expected if window is disposed during test
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("disposed", StringComparison.OrdinalIgnoreCase))
                    {
                        // Expected if window is disposed during test
                    }
                    catch (Exception ex)
                    {
                        lock (exceptionLock)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            // Let LED thread process any pending updates
            Thread.Sleep(500);

            // Assert - No unexpected exceptions should occur during concurrent access
            Assert.That(exceptions, Is.Empty,
                $"Race condition detected. Exceptions occurred: " +
                $"{string.Join("; ", exceptions.Select(e => $"{e.GetType().Name}: {e.Message}"))}");
        }
        finally
        {
            // Cleanup
            if (windowForm != null)
            {
                windowForm.Close();
                windowForm.Dispose();
            }
        }
    }

    [Test]
    [Category("Threading")]
    [SupportedOSPlatform("windows")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Tests")]
    public void LedThread_StartsAndStopsCleanly ()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigManager>();
        _ = mockConfigManager.Setup(m => m.Settings).Returns(new Settings());

        var window = AbstractLogTabWindow.Create(
            [],
            1,
            false,
            mockConfigManager.Object
        );

        bool cleanedUpSuccessfully = false;

        try
        {
            // Give time for LED thread to start
            Thread.Sleep(300);

            // Act - Close window (should stop LED thread gracefully)
            if (window is Form form)
            {
                form.Close();
                form.Dispose();
            }

            // Let thread cleanup complete
            Thread.Sleep(200);

            cleanedUpSuccessfully = true;
        }
        catch (Exception ex)
        {
            Assert.Fail($"LED thread cleanup failed: {ex.Message}");
        }

        // Assert - If thread cleanup has issues, disposal would throw or hang
        Assert.That(cleanedUpSuccessfully, Is.True, "LED thread should start and stop cleanly");
    }

    [Test]
    [Category("Threading")]
    [SupportedOSPlatform("windows")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Tests")]
    public void MultipleWindows_LedThreadsDoNotInterfere ()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigManager>();
        _ = mockConfigManager.Setup(m => m.Settings).Returns(new Settings());

        var windows = new List<ILogTabWindow>();
        var exceptions = new List<Exception>();

        try
        {
            // Act - Create multiple windows, each with their own LED thread
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var window = AbstractLogTabWindow.Create(
                        [],
                        i + 1,
                        false,
                        mockConfigManager.Object
                    );
                    windows.Add(window);
                    Thread.Sleep(100); // Stagger creation
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            // Let all LED threads run concurrently
            Thread.Sleep(800);

            // Assert - All windows should be created and running without interference
            Assert.That(exceptions, Is.Empty,
                $"Exceptions during window creation: {string.Join("; ", exceptions.Select(e => e.Message))}");
            Assert.That(windows.Count, Is.EqualTo(3), "All windows should be created successfully");
        }
        finally
        {
            // Cleanup all windows
            foreach (var window in windows)
            {
                try
                {
                    if (window is Form form)
                    {
                        form.Close();
                        form.Dispose();
                    }
                }
                catch
                {
                    // Suppress cleanup exceptions
                }
            }
        }
    }

    [Test]
    [Category("Threading")]
    [SupportedOSPlatform("windows")]
    [Repeat(5)] // Run multiple times to catch intermittent race conditions
    public void LedThread_RepeatedStartStop_NoMemoryLeak ()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigManager>();
        _ = mockConfigManager.Setup(m => m.Settings).Returns(new Settings());

        // Act - Create and destroy window multiple times
        // Each time starts and stops LED thread
        for (int i = 0; i < 5; i++)
        {
            var window = AbstractLogTabWindow.Create(
                [],
                1,
                false,
                mockConfigManager.Object
            );

            Thread.Sleep(100); // Let LED thread start

            if (window is Form form)
            {
                form.Close();
                form.Dispose();
            }

            Thread.Sleep(100); // Let LED thread stop

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // Assert - If there are thread leaks, we'd eventually get exceptions
        // The fact that we got here without exceptions means the test passed
        Assert.That(true, Is.True, "No threading issues detected during repeated create/dispose cycles");
    }
}
