namespace LogExpert.Core.Interface;

public interface ILogExpertProxy
{
    #region Public methods

    /// <summary>
    /// Load the given files into the existing window.
    /// </summary>
    /// <param name="fileNames"></param>
    void LoadFiles(string[] fileNames);

    /// <summary>
    /// Open a new LogExpert window and load the given files.
    /// </summary>
    /// <param name="fileNames"></param>
    void NewWindow(string[] fileNames);

    /// <summary>
    /// load given files into the locked window or open a new window if no window is locked.
    /// </summary>
    /// <param name="fileNames"></param>
    void NewWindowOrLockedWindow(string[] fileNames);


    /// <summary>
    /// Called from LogTabWindow when the window is about to be closed.
    /// </summary>
    /// <param name="logWin"></param>
    void WindowClosed(ILogTabWindow logWin);

    int GetLogWindowCount();

    #endregion

    //void BroadcastSettingsChanged(Object cookie);
}