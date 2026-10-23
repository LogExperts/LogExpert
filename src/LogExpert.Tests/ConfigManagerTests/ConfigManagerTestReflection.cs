using System.Reflection;

using LogExpert.Core.Config;

namespace LogExpert.Tests.ConfigManagerTests;

/// <summary>
/// Shared reflection helpers for fixtures that reset the ConfigManager singleton
/// between tests.
/// </summary>
internal static class ConfigManagerTestReflection
{
    /// <summary>
    /// Resets the ConfigManager singleton's initialization state so the next
    /// Initialize call reloads settings from its own directory.
    /// </summary>
    internal static void ResetConfigManagerInitialization (Configuration.ConfigManager configManager)
    {
        SetPrivateField(configManager, "_isInitialized", false);

        // Reset settings so they reload from the new path
        SetPrivateField(configManager, "_settings", null);
    }

    internal static object? GetPrivateField (Configuration.ConfigManager configManager, string fieldName)
    {
        return Field(fieldName).GetValue(configManager);
    }

    internal static void SetPrivateField (Configuration.ConfigManager configManager, string fieldName, object? value)
    {
        Field(fieldName).SetValue(configManager, value);
    }

    private static FieldInfo Field (string fieldName)
    {
        return typeof(Configuration.ConfigManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"ConfigManager.{fieldName} is gone; update the shared reset helper.");
    }
}
