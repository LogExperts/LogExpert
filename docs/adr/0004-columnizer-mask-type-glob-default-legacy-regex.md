---
status: accepted
---

# Columnizer masks: Glob for new entries, Regex for existing ones

> Reconstructed on 2026-11-22 from the code (`MaskType`, `ColumnizerMaskEntry`, `LegacyPreferencesMigrator`). The original ADR was lost.

Columnizer Mask Entries used to be .NET regular expressions only, which is surprising for users who type `*.log`. We added a per-entry **Mask Type** (`Glob` or `Regex`). New entries default to `Glob`, which matches the short file name with `*` and `?`.

Settings saved before the field existed have no type. If they loaded with the new default, they would silently become globs and stop matching. `LegacyPreferencesMigrator` (settings version 0 → 1) therefore rewrites every existing entry to `Regex`, so old settings behave exactly as before.

## Consequences

- The migration depends on `SettingsVersion`. The migrator must run before the settings are used, and the version must be bumped for any later migration.
- `MaskType` is serialized by name. The converter still reads legacy integer values.
