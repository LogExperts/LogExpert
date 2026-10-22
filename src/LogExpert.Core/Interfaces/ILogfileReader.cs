using System.Text;

using ColumnizerLib;

using LogExpert.Core.Entities;
using LogExpert.Core.EventArguments;

namespace LogExpert.Core.Interfaces;

/// <summary>
/// Core seam for the Logfile Reader module — covers the 80% case:
/// reading Log Lines, getting the line count, reacting to file changes, and lifecycle.
///
/// <para><b>Adapters:</b> the file-backed <c>LogfileReader</c> (production) and an
/// in-memory adapter for tests. Future adapters may support network streams or
/// compressed files.</para>
///
/// <para><b>Capability interfaces:</b> specialized operations are on separate interfaces
/// that the production adapter also implements. Query with pattern matching when needed:</para>
/// <list type="bullet">
///   <item><see cref="IMultiFileNavigation"/> — navigation across physical file boundaries</item>
///   <item><see cref="ILogfileReaderConfiguration"/> — XML mode, pre-process columnizer, encoding changes</item>
///   <item><see cref="IBufferPinning"/> — pin buffer ranges during paint cycles (ColumnCache)</item>
///   <item><see cref="ILogfileReaderDiagnostics"/> — debug-only buffer logging</item>
/// </list>
///
/// <h3>Invariants</h3>
/// <list type="bullet">
///   <item>Line numbers are zero-based and dense: [0 .. LineCount-1].</item>
///   <item><see cref="LineCount"/> is eventually-consistent with the underlying source.
///         After a <see cref="FileSizeChanged"/> event, it reflects the new state.</item>
///   <item>All read methods (<see cref="IAutoLogLineMemoryColumnizerCallback.GetLogLineMemory"/>,
///         <see cref="GetLogLineMemoryWithWait"/>, <see cref="GetLogLineMemories"/>) are safe
///         to call from any thread. The implementation handles internal synchronization.</item>
///   <item>Events are raised on background threads. Callers must marshal to the
///         UI thread if needed.</item>
///   <item><see cref="StartMonitoring"/> must be called exactly once after construction
///         to begin producing events. Calling it again after <see cref="StopMonitoring"/>
///         is not supported — create a new instance instead.</item>
/// </list>
///
/// <h3>Ordering</h3>
/// <list type="number">
///   <item>Construct → set configuration (via <see cref="ILogfileReaderConfiguration"/>) → <see cref="StartMonitoring"/></item>
///   <item><see cref="LoadingStarted"/> fires once (initial file read begins)</item>
///   <item><see cref="LoadFile"/> fires repeatedly during initial read with progress</item>
///   <item><see cref="LoadingFinished"/> fires once (initial read complete; LineCount is valid)</item>
///   <item><see cref="FileSizeChanged"/> fires on each append/truncate detected during monitoring</item>
///   <item><see cref="StopMonitoring"/> or <see cref="StopMonitoringAsync"/> → Dispose</item>
/// </list>
///
/// <h3>Error Modes</h3>
/// <list type="bullet">
///   <item><see cref="FileNotFound"/>: the watched file was deleted or became inaccessible.
///         After this event, read methods return null for affected lines.</item>
///   <item><see cref="Respawned"/>: a previously-deleted file reappeared (e.g. log rotation).
///         The reader reloads content and fires <see cref="FileSizeChanged"/>.</item>
///   <item>Read methods return null for out-of-range lines or when the backing store is
///         temporarily unavailable (e.g. network timeout). They never throw.</item>
/// </list>
/// </summary>
public interface ILogfileReader : IAutoLogLineMemoryColumnizerCallback, IDisposable
{
    #region Events

    /// <summary>
    /// Raised when the file size and/or line count changes (content appended, truncated, or rotated).
    /// This is the primary event driving Tail Mode updates in the UI.
    /// </summary>
    event EventHandler<LogEventArgs> FileSizeChanged;

    /// <summary>
    /// Raised repeatedly during initial file load with progress information.
    /// </summary>
    event EventHandler<LoadFileEventArgs> LoadFile;

    /// <summary>
    /// Raised once when the initial file load begins.
    /// </summary>
    event EventHandler<LoadFileEventArgs> LoadingStarted;

    /// <summary>
    /// Raised once when the initial file load completes. After this event,
    /// <see cref="LineCount"/> reflects the full file content.
    /// </summary>
    event EventHandler<EventArgs> LoadingFinished;

    /// <summary>
    /// Raised when the monitored file is deleted or becomes inaccessible.
    /// </summary>
    event EventHandler<EventArgs> FileNotFound;

    /// <summary>
    /// Raised when a previously-missing file reappears (e.g. after log rotation).
    /// </summary>
    event EventHandler<EventArgs> Respawned;

    #endregion

    #region Core Read Properties

    /// <summary>
    /// Total number of Log Lines currently available. Thread-safe.
    /// Zero before <see cref="LoadingFinished"/> fires for the first time.
    /// </summary>
    int LineCount { get; }

    /// <summary>
    /// Whether this reader spans multiple physical files (Multi-File Mode).
    /// Immutable after construction.
    /// </summary>
    bool IsMultiFile { get; }

    /// <summary>
    /// The character encoding currently in use.
    /// </summary>
    Encoding CurrentEncoding { get; }

    /// <summary>
    /// Total size, in bytes, of the backing file(s). Updated on each monitoring cycle.
    /// </summary>
    long FileSize { get; }

    #endregion

    #region Line Access

    // GetLogLineMemory(int) is inherited from IAutoLogLineMemoryColumnizerCallback.
    // It is the synchronous single-line read. Returns null for out-of-range or unavailable lines.

    /// <summary>
    /// Reads a single Log Line with a bounded wait. If the backing store is slow
    /// (network share, deleted file), returns null after an internal timeout rather
    /// than blocking indefinitely. The implementation may enter a fast-fail mode
    /// after a timeout to prevent GUI freezes.
    /// </summary>
    /// <param name="lineNum">Zero-based line number.</param>
    /// <returns>The Log Line, or null if unavailable within the timeout.</returns>
    Task<ILogLineMemory> GetLogLineMemoryWithWait(int lineNum);

    /// <summary>
    /// Batch read for a contiguous range of Log Lines. Optimized for paint-cycle
    /// prefetch: acquires locks once for the entire range.
    /// </summary>
    /// <param name="startLine">Zero-based starting line number.</param>
    /// <param name="count">Number of lines to read. The returned array may be shorter
    /// if the end of the file is reached. Individual entries may be null if a buffer
    /// is temporarily unavailable.</param>
    /// <returns>Array of Log Lines. Never null; may be empty or shorter than <paramref name="count"/>.</returns>
    ILogLineMemory[] GetLogLineMemories(int startLine, int count);

    #endregion

    #region Lifecycle

    /// <summary>
    /// Begins background monitoring for file changes (Tail Mode). Triggers initial file read,
    /// producing <see cref="LoadingStarted"/>/<see cref="LoadFile"/>/<see cref="LoadingFinished"/>
    /// events, then monitors for appends.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Synchronously stops background monitoring and releases file handles. Blocks until
    /// background tasks complete (with a bounded timeout). After this call, no more events
    /// will be raised.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Asynchronously stops background monitoring. Returns immediately; cleanup happens
    /// on a background thread. Use when a fast-responding GUI is needed (e.g. closing a tab).
    /// </summary>
    void StopMonitoringAsync();

    /// <summary>
    /// Clears all buffered content and releases memory. Call only when the reader is about
    /// to be disposed. After this call, all read methods return null/empty.
    /// </summary>
    void DeleteAllContent();

    #endregion
}
