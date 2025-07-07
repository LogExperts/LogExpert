namespace LogExpert.Core.Interface;

/// <summary>
/// Methods to control the LogWindow from other views.
/// </summary>
public interface ILogView
{
    #region Properties

    ILogLineColumnizer CurrentColumnizer { get; }
    string FileName { get; }

    #endregion

    #region Public methods

    void SelectLogLine(int lineNumber);
    void SelectAndEnsureVisible(int line, bool triggerSyncCall);
    void RefreshLogView();
    void DeleteBookmarks(List<int> lineNumList);

    #endregion
}