namespace LogExpert.Core.Classes.FileDrop;

public sealed record DroppedFileDiscoveryResult (bool IncludesFolders, string[] Files, SkippedDropPath[] Skipped);

/// <param name="Reason">The filesystem error, or null for a deliberately skipped directory reparse point.</param>
public sealed record SkippedDropPath (string Path, string? Reason);
