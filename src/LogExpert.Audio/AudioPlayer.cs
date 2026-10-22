using System.Media;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using NAudio;
using NAudio.Wave;

using NLog;

namespace LogExpert.Audio;

/// <summary>
/// Plays short audio cues (alerts) from arbitrary audio files supported by NAudio
/// (WAV, MP3, AIFF, ...). Falls back to the Windows default beep when the path is
/// empty or the file cannot be opened.
/// <para>
/// All playback is fire-and-forget; callers never block. A single, process-wide
/// cooldown is maintained by <see cref="PlayThrottled"/> so that bursts of trigger
/// hits cannot produce overlapping or rapid-fire sounds. The cooldown is global
/// across all callers.
/// </para>
/// </summary>
[SupportedOSPlatform("windows")]
public static class AudioPlayer
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private static readonly object _gate = new();
    private static DateTime _lastPlayUtc = DateTime.MinValue;
    private static int _activeCooldownSeconds;

    /// <summary>
    /// Plays the given audio file fire-and-forget. When <paramref name="filePath"/>
    /// is null, empty or whitespace, the Windows default beep is played instead.
    /// When the file cannot be opened the failure is logged and the default beep
    /// is played as a fallback so the user still gets feedback.
    /// </summary>
    /// <param name="filePath">Absolute path to an audio file readable by NAudio,
    /// or <see langword="null"/>/empty for the default system beep.</param>
    public static void Play (string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            PlaySystemBeep();
            return;
        }

        if (!File.Exists(filePath))
        {
            _logger.Warn("Audio alert file not found: {0}. Falling back to system beep.", filePath);
            PlaySystemBeep();
            return;
        }

        try
        {
            var reader = new AudioFileReader(filePath);
            var output = new WaveOutEvent();
            output.PlaybackStopped += (_, _) =>
            {
                try
                {
                    output.Dispose();
                    reader.Dispose();
                }
                catch (Exception disposeEx) when (disposeEx is MmException or
                                                               IOException or
                                                               ObjectDisposedException or
                                                               InvalidOperationException or
                                                               NullReferenceException)
                {
                    _logger.Debug(disposeEx, "Error disposing audio playback resources.");
                }
            };

            output.Init(reader);
            output.Play();
        }
        catch (Exception ex) when (ex is MmException or
                                         IOException or
                                         UnauthorizedAccessException or
                                         InvalidOperationException or
                                         COMException or
                                         FormatException)
        {
            _logger.Warn(ex, "Failed to play audio alert file '{0}'. Falling back to system beep.", filePath);
            PlaySystemBeep();
        }
    }

    /// <summary>
    /// Plays the given audio file, applying a single process-wide cooldown.
    /// Returns <see langword="true"/> when the sound was actually played, or
    /// <see langword="false"/> when the call was suppressed because the cooldown
    /// from a previous play is still active.
    /// <para>
    /// Cooldown semantics (Option-1 "active cooldown wins"): the cooldown of the
    /// most recently played alert gates every subsequent call until it expires,
    /// regardless of the cooldown value passed in for the new attempt.
    /// </para>
    /// </summary>
    /// <param name="filePath">Audio file path, or null/empty for the system beep.</param>
    /// <param name="cooldownSeconds">Minimum seconds before the next alert may
    /// play after this one. Values &lt;= 0 disable throttling for the next call.</param>
    /// <returns><see langword="true"/> if played, <see langword="false"/> if suppressed.</returns>
    public static bool PlayThrottled (string? filePath, int cooldownSeconds)
    {
        if (cooldownSeconds < 0)
        {
            cooldownSeconds = 0;
        }

        lock (_gate)
        {
            var now = DateTime.UtcNow;
            if (_activeCooldownSeconds > 0 &&
                (now - _lastPlayUtc).TotalSeconds < _activeCooldownSeconds)
            {
                return false;
            }

            _lastPlayUtc = now;
            _activeCooldownSeconds = cooldownSeconds;
        }

        Play(filePath);
        return true;
    }

    private static void PlaySystemBeep ()
    {
        try
        {
            SystemSounds.Beep.Play();
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Failed to play system beep.");
        }
    }

    /// <summary>
    /// Test-only: resets the process-wide cooldown state so unit tests are
    /// isolated from each other. Not intended for production use.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static void ResetCooldownStateForTesting ()
    {
        lock (_gate)
        {
            _lastPlayUtc = DateTime.MinValue;
            _activeCooldownSeconds = 0;
        }
    }
}
