namespace LogExpert.Core.EventArguments;

public record LoadFileEventArgs(string FileName, long ReadPos, bool Finished, long FileSize, bool NewFile);