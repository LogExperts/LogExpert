namespace LogExpert.Core.Interfaces;

/// <summary>
/// Decouples file loading progress reporting from event dispatch.
/// The I/O path calls Report*() methods; the implementation decides
/// when and how to fire events.
/// </summary>
public interface ILoadProgressReporter : IDisposable
{
    /// <summary>
    /// Report intermediate loading progress. Called from I/O thread.
    /// Implementation may batch/coalesce/drop these.
    /// </summary>
    void ReportProgress (string fileName, long position, long fileLength);

    /// <summary>
    /// Report that loading of a file segment is complete.
    /// </summary>
    void ReportComplete (string fileName, long position, long fileLength);

    /// <summary>
    /// Report that a new file was detected (rollover).
    /// </summary>
    void ReportNewFile (string fileName, long position, long fileLength);

    /// <summary>
    /// Report that a loading operation has started.
    /// </summary>
    void ReportLoadingStarted (string fileName);

    /// <summary>
    /// Report that a loading operation has finished.
    /// </summary>
    void ReportLoadingFinished ();
}