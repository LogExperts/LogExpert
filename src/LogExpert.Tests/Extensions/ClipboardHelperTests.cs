using LogExpert.UI.Extensions;

using NUnit.Framework;

using Vanara.PInvoke;

namespace LogExpert.Tests.Extensions;

/// <summary>
/// Tests for the hardened clipboard writes (see issue with "Requested Clipboard operation
/// did not succeed" / ExternalException, previously reported in #195). When another
/// application holds the clipboard open, LogExpert must report failure instead of crashing.
/// </summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
public class ClipboardHelperTests
{
    [Test]
    public void TrySetText_ClipboardAvailable_PlacesTextAndReturnsTrue ()
    {
        var ok = ClipboardHelper.TrySetText("LogExpert clipboard test");

        Assert.That(ok, Is.True);
        Assert.That(GetClipboardTextWithRetry(), Is.EqualTo("LogExpert clipboard test"));
    }

    /// <summary>
    /// Reads the clipboard text, retrying briefly. Clipboard managers and similar tools
    /// open the clipboard to inspect new content right after it changes, which can make an
    /// immediate read-back fail or come up empty even though the write succeeded.
    /// </summary>
    private static string GetClipboardTextWithRetry ()
    {
        for (var i = 0; i < 20; i++)
        {
            var text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            Thread.Sleep(100);
        }

        return string.Empty;
    }

    [Test]
    public void TrySetText_ClipboardHeldByAnotherWindow_ReturnsFalseInsteadOfThrowing ()
    {
        using ClipboardLock clipboardLock = new();

        var ok = ClipboardHelper.TrySetText("some text");

        Assert.That(ok, Is.False);
    }

    [Test]
    public void TrySetDataObject_ClipboardAvailable_PlacesDataAndReturnsTrue ()
    {
        var ok = ClipboardHelper.TrySetDataObject("LogExpert data object test");

        Assert.That(ok, Is.True);
        Assert.That(GetClipboardTextWithRetry(), Is.EqualTo("LogExpert data object test"));
    }

    [Test]
    public void TrySetDataObject_ClipboardHeldByAnotherWindow_ReturnsFalseInsteadOfThrowing ()
    {
        using ClipboardLock clipboardLock = new();

        var ok = ClipboardHelper.TrySetDataObject("some data");

        Assert.That(ok, Is.False);
    }

    /// <summary>
    /// Holds the Win32 clipboard open from a background thread (without closing it),
    /// which makes every clipboard access in other threads/processes fail — the same
    /// situation an external clipboard-monitoring tool causes.
    /// </summary>
    private sealed class ClipboardLock : IDisposable
    {
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _acquired = new(false);
        private readonly ManualResetEventSlim _release = new(false);

        public ClipboardLock ()
        {
            _thread = new Thread(() =>
            {
                if (!User32.OpenClipboard(HWND.NULL))
                {
                    throw new InvalidOperationException("Test setup failed: could not open the clipboard");
                }

                _acquired.Set();
                _release.Wait();
                _ = User32.CloseClipboard();
            })
            {
                IsBackground = true
            };

            _thread.Start();
            _ = _acquired.Wait(TimeSpan.FromSeconds(5));
        }

        public void Dispose ()
        {
            _release.Set();
            _ = _thread.Join(TimeSpan.FromSeconds(5));
        }
    }
}
