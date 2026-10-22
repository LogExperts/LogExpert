using LogExpert.Core.Classes.Log.Buffers;

namespace LogExpert.Core.Interfaces;

/// <summary>
/// Capability interface for pinning buffer ranges so the GC-eviction thread cannot
/// reclaim them during a paint cycle. Used by ColumnCache for flicker-free rendering.
///
/// <para><b>Invariants:</b></para>
/// <list type="bullet">
///   <item>The returned <see cref="PinHandle"/> keeps buffers alive until disposed.</item>
///   <item>Callers MUST dispose the handle — leaked handles prevent buffer eviction.</item>
///   <item>Pin-before-read: call <see cref="PinRange"/> BEFORE reading lines
///         to prevent the GC thread from evicting between pin and read.</item>
/// </list>
/// </summary>
public interface IBufferPinning
{
    /// <summary>
    /// Pins all buffers that cover the line range [<paramref name="startLine"/>, <paramref name="endLine"/>].
    /// Internally acquires and releases the necessary read lock.
    /// </summary>
    /// <param name="startLine">First line of the range to pin (inclusive).</param>
    /// <param name="endLine">Last line of the range to pin (inclusive).</param>
    /// <returns>A <see cref="PinHandle"/> that must be disposed when the pinned range is no longer needed.</returns>
    PinHandle PinRange(int startLine, int endLine);
}
