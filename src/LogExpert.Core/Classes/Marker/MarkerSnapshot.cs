namespace LogExpert.Core.Classes.Marker;

/// <summary>Immutable scan result. Match arrays are shared between append-only revisions.</summary>
public sealed class MarkerSnapshot
{
    internal MarkerSnapshot (MarkerLine[][] chunks, int scannedLineCount, Exception? error = null)
    {
        Chunks = chunks;
        ScannedLineCount = scannedLineCount;
        Error = error;
    }

    internal MarkerLine[][] Chunks { get; }

    public int ScannedLineCount { get; }

    public Exception? Error { get; }

    public IEnumerable<MarkerLine> Matches => Chunks.SelectMany(chunk => chunk);
}