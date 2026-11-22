---
status: accepted
---

# Audio alert cooldown is global, and the active cooldown wins

> Reconstructed on 2026-11-22 from the code (`LogExpert.Audio.AudioPlayer`), its tests and PR #581. The original ADR was lost. The decision numbers are kept because `AudioPlayerTests` cites "decision 2".

Audio Alerts fire from tail-only highlight matches, so a burst of matching lines can trigger many alerts in a short time. We throttle them in one place, the static `AudioPlayer`:

1. **One process-wide player and cooldown.** `AudioPlayer` is static and owns a single last-played timestamp. The cooldown applies to every Audio Alert in the application, not per Highlight Entry or per Log Window, so bursts from several entries or windows cannot overlap or rapid-fire.
2. **The active cooldown wins.** The cooldown of the most recently *played* alert gates every later attempt until it expires, whatever cooldown the new attempt passes in. A short-cooldown entry cannot cut a long-cooldown alert's quiet period short. A cooldown of `0` disables throttling for the next attempt only.
3. **Playback is fire-and-forget with a beep fallback.** Callers never block. An empty path, a missing file or a file NAudio cannot play falls back to the system beep and is logged at warn level, so the user still gets feedback.

## Consequences

- A suppressed alert is dropped, not queued.
- Tests must reset the shared state (`ResetCooldownStateForTesting`), because it is static.
