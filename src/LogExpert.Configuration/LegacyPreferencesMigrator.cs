using LogExpert.Core.Config;

namespace LogExpert.Configuration;

/// <summary>
/// Applies one-shot, in-memory migrations to a freshly deserialised <see cref="Settings"/> object so that
/// older settings files behave equivalently under newer schema versions. Idempotent: calling it on already
/// up-to-date settings is a no-op.
/// </summary>
public static class LegacyPreferencesMigrator
{
    /// <summary>Current schema version. Bumped whenever a new migration step is added.</summary>
    public const int CURRENT_SETTINGS_VERSION = 1;

    /// <summary>
    /// Migrates the given <see cref="Settings"/> in place. Returns <see langword="true"/> if any
    /// migration step was applied (the caller may use this signal to persist the upgraded settings).
    /// </summary>
    public static bool Migrate (Settings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var changed = false;

        if (settings.SettingsVersion < 1)
        {
            MigrateToV1(settings);
            settings.SettingsVersion = 1;
            changed = true;
        }

        return changed;
    }

    private static void MigrateToV1 (Settings settings)
    {
        // Existing ColumnizerMaskEntry rows pre-date the per-row Type field — they were regex-only.
        // Their default-loaded value would be Glob, which would silently change behaviour. Rewrite to Regex.
        if (settings.Preferences?.ColumnizerMaskList != null)
        {
            foreach (var entry in settings.Preferences.ColumnizerMaskList.Where(entry => entry != null))
            {
                entry.Type = MaskType.Regex;
            }
        }

        // Preserve the deprecated MaskPrio bool's intent on the new enum, but only if the enum is still
        // at its default — otherwise the user has already chosen.
#pragma warning disable CS0618 // Migrating away from MaskPrio
        if (settings.Preferences != null
            && settings.Preferences.ColumnizerSelectionPriority == ColumnizerSelectionPriority.HistoryThenMask
            && settings.Preferences.MaskPrio)
        {
            settings.Preferences.ColumnizerSelectionPriority = ColumnizerSelectionPriority.MaskThenHistory;
        }
#pragma warning restore CS0618
    }

}
