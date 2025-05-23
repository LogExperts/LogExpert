using LogExpert.Controls.LogWindow;

using System.Collections.Generic;

namespace LogExpert.Classes.ILogLineColumnizerCallback
{
    public class ColumnizerCallback : LogExpert.ILogLineColumnizerCallback, IAutoLogLineColumnizerCallback
    {
        #region Fields

        protected LogWindow _logWindow;

        #endregion

        #region cTor

        public ColumnizerCallback(LogWindow logWindow)
        {
            _logWindow = logWindow;
        }

        private ColumnizerCallback(ColumnizerCallback original)
        {
            _logWindow = original._logWindow;
            LineNum = original.LineNum;
        }

        #endregion

        #region Properties

        public int LineNum { get; set; }

        #endregion

        #region Public methods

        public ColumnizerCallback CreateCopy()
        {
            return new ColumnizerCallback(this);
        }

        public int GetLineNum()
        {
            return LineNum;
        }

        public string GetFileName()
        {
            return _logWindow.GetCurrentFileName(LineNum);
        }

        public ILogLine GetLogLine(int lineNum)
        {
            return _logWindow.GetLine(lineNum);
        }

        public IList<ILogLineColumnizer> GetRegisteredColumnizers()
        {
            return PluginRegistry.PluginRegistry.Instance.RegisteredColumnizers;
        }

        public int GetLineCount()
        {
            return _logWindow._logFileReader.LineCount;
        }

        #endregion
    }
}
