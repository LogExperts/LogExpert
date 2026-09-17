using System.Runtime.Versioning;

using LogExpert.Core.Config;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Extensions.LogWindow;

using Moq;

using NUnit.Framework;

namespace LogExpert.UI.Tests.UI;

[TestFixture]
[Apartment(ApartmentState.STA)] // Required for WinForms components
public class LogTabWindowResourceTests
{
    /// <summary>
    /// Creates a <see cref="Settings"/> instance with <see cref="Preferences.Font"/> materialized
    /// from <see cref="Preferences.FontString"/>, mirroring what <c>ConfigManager.InitializeFont</c>
    /// does at runtime.
    /// </summary>
    private static Settings CreateSettings ()
    {
        var settings = new Settings();
        var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Font));
        settings.Preferences.Font = (Font)converter.ConvertFromInvariantString(settings.Preferences.FontString)!;
        return settings;
    }

    [Test]
    [Category("Resource")]
    [SupportedOSPlatform("windows")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Tests")]
    public void Dispose_DisposesAllGdiResources ()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigManager>();
        _ = mockConfigManager.Setup(m => m.Settings).Returns(CreateSettings());

        // Create the window using the factory method
        ILogTabWindow? window = null;
        bool disposedSuccessfully = false;

        try
        {
            window = AbstractLogTabWindow.Create(
                [],
                1,
                false,
                mockConfigManager.Object
            );

            // Give time for initialization
            Thread.Sleep(300);

            // Act - Dispose via close (Form.Close calls Dispose internally)
            if (window is Form form)
            {
                form.Close();
                form.Dispose();
                disposedSuccessfully = true;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Test failed with exception during disposal: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            // Ensure cleanup even if test fails
            if (window is IDisposable disposable && window is Form form && !form.IsDisposed)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    // Suppress exceptions in cleanup
                }
            }
        }

        // Assert - If disposal has bugs, we'd get exceptions or access violations
        Assert.That(disposedSuccessfully, Is.True, "Window should dispose successfully without exceptions");
    }

    [Test]
    [Category("Resource")]
    [SupportedOSPlatform("windows")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void Constructor_InitializesSuccessfully ()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigManager>();
        _ = mockConfigManager.Setup(m => m.Settings).Returns(CreateSettings());

        ILogTabWindow? window = null;

        try
        {
            // Act
            window = AbstractLogTabWindow.Create(
                [],
                1,
                false,
                mockConfigManager.Object
            );

            // Give time for initialization
            Thread.Sleep(300);

            // Assert - Verify window was created and basic structure exists
            Assert.That(window, Is.Not.Null, "Window should be created");

            if (window is Form form)
            {
                Assert.That(form.IsDisposed, Is.False, "Window should not be disposed after creation");
                Assert.That(form.Handle, Is.Not.EqualTo(IntPtr.Zero), "Window should have a valid handle");
            }
            else
            {
                Assert.Fail("Window should be a Form");
            }
        }
        finally
        {
            if (window is Form form)
            {
                form.Close();
                form.Dispose();
            }
        }
    }

    [Test]
    [Category("Resource")]
    [SupportedOSPlatform("windows")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Tests")]
    public void MultipleCreateDispose_DoesNotLeakResources ()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigManager>();
        _ = mockConfigManager.Setup(m => m.Settings).Returns(CreateSettings());

        var exceptions = new List<Exception>();

        // Act - Create and dispose multiple windows
        // If there's a resource leak, we'll eventually hit system limits or get exceptions
        for (int i = 0; i < 5; i++)
        {
            try
            {
                var window = AbstractLogTabWindow.Create(
                    [],
                    1,
                    false,
                    mockConfigManager.Object
                );

                Thread.Sleep(100); // Allow initialization

                if (window is Form form)
                {
                    form.Close();
                    form.Dispose();
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        Thread.Sleep(200); // Allow final cleanup

        // Assert
        Assert.That(exceptions, Is.Empty,
            $"Should create and dispose multiple windows without exceptions. " +
            $"Exceptions: {string.Join("; ", exceptions.Select(e => e.Message))}");
    }

    [Test]
    [Category("Resource")]
    [SupportedOSPlatform("windows")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void Dispose_CanBeCalledMultipleTimes ()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigManager>();
        _ = mockConfigManager.Setup(m => m.Settings).Returns(CreateSettings());

        var window = AbstractLogTabWindow.Create(
            [],
            1,
            false,
            mockConfigManager.Object
        );

        Thread.Sleep(200);

        // Act & Assert - Multiple dispose calls should not throw
        if (window is Form form)
        {
            Assert.DoesNotThrow(() =>
            {
                form.Close();
                form.Dispose();
                form.Dispose(); // Second dispose should be safe
                form.Dispose(); // Third dispose should be safe
            }, "Multiple Dispose calls should not throw exceptions");
        }
        else
        {
            Assert.Fail("Window should be a Form");
        }
    }
}
