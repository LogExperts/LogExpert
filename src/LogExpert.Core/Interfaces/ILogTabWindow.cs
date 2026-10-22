namespace LogExpert.Core.Interfaces;

/// <summary>
/// Represents a log tab window that can display and manage log files.
/// </summary>
public interface ILogTabWindow
{
    /// <summary>
    /// Gets or sets the proxy for communicating with the main LogExpert application.
    /// </summary>
    ILogExpertProxy LogExpertProxy { get; set; }

    /// <summary>
    /// Gets a value indicating whether this window has been disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Activates the window and brings it to the foreground.
    /// </summary>
    void Activate ();

    /// <summary>
    /// Invokes the specified delegate on the thread that owns the window's underlying handle.
    /// </summary>
    /// <param name="method">The delegate to invoke.</param>
    /// <param name="objects">Optional parameters to pass to the delegate.</param>
    /// <returns>The return value from the delegate being invoked.</returns>
    object Invoke (Delegate method, params object?[]? objects);

    /// <summary>
    /// Loads the specified log files into the window.
    /// </summary>
    /// <param name="fileNames">An array of file paths to load.</param>
    void LoadFiles (string[] fileNames, int? targetLine = null);

    /// <summary>
    /// Sets the window to the foreground and gives it focus.
    /// </summary>
    void SetForeground ();

    /// <summary>
    /// Shows the window.
    /// </summary>
    void Show ();
}