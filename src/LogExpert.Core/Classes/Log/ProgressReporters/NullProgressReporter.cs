using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Classes.Log.ProgressReporters;

/// <summary>
/// No-op reporter for benchmarks and unit tests. Zero allocation, zero overhead.
/// </summary>
public sealed class NullProgressReporter : ILoadProgressReporter
{
    public static readonly NullProgressReporter Instance = new();

    public void ReportProgress (string fileName, long position, long fileLength) { }

    public void ReportComplete (string fileName, long position, long fileLength) { }

    public void ReportNewFile (string fileName, long position, long fileLength) { }

    public void ReportLoadingStarted (string fileName) { }

    public void ReportLoadingFinished () { }

    public void Dispose () { }
}