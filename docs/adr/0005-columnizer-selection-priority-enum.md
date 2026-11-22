---
status: accepted
---

# Columnizer Selection Priority replaces the MaskPrio bool

> Reconstructed on 2026-11-22 from the code (`ColumnizerSelectionPriority`, `ColumnizerResolver`, `LegacyPreferencesMigrator`). The original ADR was lost.

Four sources can choose the columnizer a file opens with: its Session File, the Columnizer History, a Columnizer Mask Entry, and AutoPick. The old `Preferences.MaskPrio` bool could only swap History and Mask. Users who wanted a matching mask to win over a file's saved columnizer had no way to say so. We replaced the bool with a three-value enum, resolved in one place (`ColumnizerResolver`):

- `HistoryThenMask` (default, the old behavior): Session File → History → Mask → AutoPick.
- `MaskThenHistory` (the old `MaskPrio = true`): Session File → Mask → History → AutoPick.
- `MaskOverridesPersistence`: Mask → Session File → History → AutoPick. Only the Session File's columnizer is overridden; its bookmarks, filters and other state still restore.

AutoPick only runs when none of these produces a columnizer.

## Consequences

- `MaskPrio` is kept, marked obsolete, so old settings still load. `LegacyPreferencesMigrator` maps `MaskPrio = true` to `MaskThenHistory`, but only while the enum is still at its default, so a choice the user already made is never overwritten.
- The member name `MaskOverridesPersistence` keeps the old "persistence" wording because the enum is serialized by name. The user-facing term is **Session File**.
