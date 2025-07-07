using LogExpert.Core.Classes.Log;
using LogExpert.Core.Classes.Persister;

namespace LogExpert.Core.Interface;

//TODO: Add documentation
public interface ILogWindow
{
    string GetCurrentFileName (int lineNum);

    ILogLine GetLine (int lineNum);

    ILogLine GetLogLineWithWait (int lineNum);

    //TODO Find a way to not use a referenced int (https://github.com/LogExperts/LogExpert/issues/404)
    DateTime GetTimestampForLineForward (ref int lineNum, bool roundToSeconds);

    //TODO Find a way to not use a referenced int (https://github.com/LogExperts/LogExpert/issues/404)
    DateTime GetTimestampForLine (ref int lastLineNum, bool roundToSeconds);

    int FindTimestampLineInternal (int lineNum, int rangeStart, int rangeEnd, DateTime timestamp, bool roundToSeconds);

    void SelectLine (int lineNum, bool triggerSyncCall, bool shouldScroll);

    PersistenceData GetPersistenceData ();

    void AddTempFileTab (string fileName, string title);

    void WritePipeTab (IList<LineEntry> lineEntryList, string title);

    void Activate ();

    LogfileReader LogFileReader { get; }

    string Text { get; }

    string FileName { get; }

    //event EventHandler<LogEventArgs> FileSizeChanged; //TODO: All handlers should be moved to Core

    //event EventHandler TailFollowed;
    //LogExpert.UI.Controls.LogTabWindow.LogTabWindow.LogWindowData Tag { get; }
}