using System;
using System.Diagnostics;

namespace LogExpert;

internal class ProcessLauncher : IKeywordAction
{
    #region Properties

    public string Text => GetName();

    #endregion

    #region IKeywordAction Member

    private readonly object _callbackLock = new();

    public void Execute (string keyword, string param, ILogExpertCallback callback, ILogLineColumnizer columnizer)
    {
        var start = 0;
        int end;

        if (param.StartsWith('"'))
        {
            start = 1;
            end = param.IndexOf('"', start);
        }
        else
        {
            end = param.IndexOf(' ', StringComparison.Ordinal);
        }

        if (end == -1)
        {
            end = param.Length;
        }

        var procName = param[start..end];

        lock (_callbackLock)
        {
            var parameters = param[end..].Trim();
            parameters = parameters.Replace("%F", callback.GetFileName(), StringComparison.Ordinal);
            parameters = parameters.Replace("%K", keyword, StringComparison.Ordinal);

            var lineNumber = callback.LineNum; //Line Numbers start at 0, but are displayed (+1)
            var logline = callback.GetLogLine(lineNumber).FullLine;
            parameters = parameters.Replace("%L", string.Empty + lineNumber, System.StringComparison.Ordinal);
            parameters = parameters.Replace("%T", callback.GetTabTitle(), StringComparison.Ordinal);
            parameters = parameters.Replace("%C", logline, StringComparison.Ordinal);

            Process explorer = new();
            explorer.StartInfo.FileName = procName;
            explorer.StartInfo.Arguments = parameters;
            explorer.StartInfo.UseShellExecute = false;
            explorer.Start();
        }
    }

    public string GetName ()
    {
        return "ProcessLauncher keyword plugin";
    }


    public string GetDescription ()
    {
        return "Launches an external process. The plugin's parameter is the process name " +
               "and its (optional) command line.\r\n" +
               "Use the following variables for command line replacements:\r\n" +
               "%F = Log file name (full path)\r\n" +
               "%T = Tab title\r\n" +
               "%L = Line number of keyword hit\r\n" +
               "%K = Keyword\r\n" +
               "%C = Complete line content";
    }

    #endregion
}