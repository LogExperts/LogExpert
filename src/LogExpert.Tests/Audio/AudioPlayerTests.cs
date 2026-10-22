using System;
using System.IO;
using System.Threading;

using LogExpert.Audio;

using NUnit.Framework;

namespace LogExpert.Tests.Audio;

[TestFixture]
[Platform("Win")]
[NonParallelizable] // AudioPlayer uses process-wide static state
public class AudioPlayerTests
{
    [SetUp]
    public void SetUp ()
    {
        AudioPlayer.ResetCooldownStateForTesting();
    }

    [TearDown]
    public void TearDown ()
    {
        AudioPlayer.ResetCooldownStateForTesting();
    }

    [Test]
    public void Play_NullPath_DoesNotThrow ()
    {
        Assert.DoesNotThrow(() => AudioPlayer.Play(null));
    }

    [Test]
    public void Play_EmptyPath_DoesNotThrow ()
    {
        Assert.DoesNotThrow(() => AudioPlayer.Play(string.Empty));
    }

    [Test]
    public void Play_WhitespacePath_DoesNotThrow ()
    {
        Assert.DoesNotThrow(() => AudioPlayer.Play("   "));
    }

    [Test]
    public void Play_NonExistentPath_FallsBackSilently ()
    {
        var missing = Path.Join(Path.GetTempPath(), $"definitely-missing-{Guid.NewGuid():N}.wav");
        Assert.DoesNotThrow(() => AudioPlayer.Play(missing));
    }

    [Test]
    public void PlayThrottled_FirstCall_ReturnsTrue ()
    {
        var played = AudioPlayer.PlayThrottled(null, cooldownSeconds: 10);
        Assert.That(played, Is.True);
    }

    [Test]
    public void PlayThrottled_SecondCallWithinCooldown_ReturnsFalse ()
    {
        Assume.That(AudioPlayer.PlayThrottled(null, cooldownSeconds: 30), Is.True);

        var second = AudioPlayer.PlayThrottled(null, cooldownSeconds: 30);

        Assert.That(second, Is.False, "Second call inside the active cooldown window must be suppressed.");
    }

    [Test]
    public void PlayThrottled_ZeroCooldown_AllowsImmediateNextCall ()
    {
        Assume.That(AudioPlayer.PlayThrottled(null, cooldownSeconds: 0), Is.True);

        var second = AudioPlayer.PlayThrottled(null, cooldownSeconds: 0);

        Assert.That(second, Is.True, "A zero cooldown must not suppress the next call.");
    }

    [Test]
    public void PlayThrottled_NegativeCooldown_NormalizedToZero ()
    {
        Assume.That(AudioPlayer.PlayThrottled(null, cooldownSeconds: -5), Is.True);

        var second = AudioPlayer.PlayThrottled(null, cooldownSeconds: -5);

        Assert.That(second, Is.True, "Negative cooldown must be treated as 0 and not suppress.");
    }

    [Test]
    public void PlayThrottled_ActiveCooldownWins_LongFirstGatesShortSecond ()
    {
        // ADR 0001 decision 2: the cooldown of the most recently played alert
        // gates every subsequent attempt, regardless of the new attempt's value.
        Assume.That(AudioPlayer.PlayThrottled(null, cooldownSeconds: 60), Is.True);

        var shortAttempt = AudioPlayer.PlayThrottled(null, cooldownSeconds: 0);

        Assert.That(shortAttempt, Is.False,
            "A short/zero cooldown must not bypass an already-active long cooldown.");
    }

    [Test]
    public void PlayThrottled_AfterCooldownExpires_PlaysAgain ()
    {
        Assume.That(AudioPlayer.PlayThrottled(null, cooldownSeconds: 1), Is.True);

        // Wait slightly more than the cooldown window.
        Thread.Sleep(1100);

        var second = AudioPlayer.PlayThrottled(null, cooldownSeconds: 1);

        Assert.That(second, Is.True, "After the cooldown window elapses the next call must play.");
    }
}
