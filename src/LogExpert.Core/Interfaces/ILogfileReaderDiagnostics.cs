namespace LogExpert.Core.Interfaces;

/// <summary>
/// Capability interface for debug-only buffer diagnostics.
/// No caller should depend on this for correctness — it writes to the diagnostic log.
/// </summary>
public interface ILogfileReaderDiagnostics
{
#if DEBUG
    /// <summary>
    /// Logs detailed buffer information for the given line to the diagnostic output.
    /// Only functional in debug builds; no-op in release.
    /// </summary>
    void LogBufferInfoForLine(int lineNum);

    /// <summary>
    /// Logs overall buffer and LRU cache diagnostics.
    /// Only functional in debug builds; no-op in release.
    /// </summary>
    void LogBufferDiagnostic();
#endif
}
