using ColumnizerLib;

namespace LogExpert.Core.Interfaces;

/// <summary>
/// A Columnizer callback whose current line number can be moved by the caller.
/// </summary>
/// <remarks>
/// <see cref="ILogLineMemoryColumnizerCallback.LineNum"/> is read-only on the plugin-facing
/// interface: a Columnizer may ask which line it is working on, but may not move it. The host,
/// however, must position the callback before every call it makes into a Columnizer, because
/// <see cref="ILogLineMemoryColumnizerCallback.GetFileName"/> resolves through the current line
/// number in Multi-File Mode.
/// </remarks>
public interface IPositionedColumnizerCallback : ILogLineMemoryColumnizerCallback
{
    /// <summary>
    /// Moves the callback to <paramref name="lineNum"/> so that subsequent Columnizer calls
    /// resolve their context against that line.
    /// </summary>
    /// <param name="lineNum">Zero-based line number.</param>
    void SetLineNum (int lineNum);
}
