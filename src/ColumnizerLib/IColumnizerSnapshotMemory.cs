namespace ColumnizerLib;

/// <summary>
/// Optional capability for capturing a columnizer's current parsing state for background use.
/// </summary>
/// <remarks>
/// The snapshot must be initialized with the current configuration and detected column layout,
/// without sharing mutable parsing state with the original columnizer. The Marker Bar captures
/// the snapshot on the UI thread and uses it on a worker thread without calling
/// <see cref="IInitColumnizerMemory.Selected"/>. Capture must be quick and perform no file I/O.
/// </remarks>
public interface IColumnizerSnapshotMemory
{
    /// <summary>
    /// Creates an independent, initialized columnizer with the current configuration and detected layout.
    /// </summary>
    /// <returns>A columnizer ready to parse log lines on a worker thread.</returns>
    ILogLineMemoryColumnizer CreateSnapshot ();
}