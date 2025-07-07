using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

using LogExpert.Core.Classes;
using LogExpert.Dialogs;

namespace LogExpert.UI.Entities;

internal class ArgParser (string argTemplate)
{
    #region Public methods

    [SupportedOSPlatform("windows")]
    public string BuildArgs (ILogLine logLine, int lineNum, ILogFileInfo logFileInfo, Form parent)
    {
        StringBuilder builder = new(argTemplate);

        _ = builder.Replace("%L", "" + lineNum);
        _ = builder.Replace("%P", logFileInfo.DirectoryName);
        _ = builder.Replace("%N", logFileInfo.FileName);
        _ = builder.Replace("%F", logFileInfo.FullName);
        _ = builder.Replace("%E", Util.GetExtension(logFileInfo.FileName));
        var stripped = Util.StripExtension(logFileInfo.FileName);
        _ = builder.Replace("%M", stripped);
        _ = builder.Replace("%URI", logFileInfo.Uri.AbsoluteUri);
        var user = logFileInfo.Uri.UserInfo;

        if (user.Contains(':', StringComparison.Ordinal))
        {
            user = user[..user.IndexOf(':', StringComparison.Ordinal)];
        }

        _ = builder.Replace("%S", user);
        _ = builder.Replace("%R", logFileInfo.Uri.PathAndQuery);
        _ = builder.Replace("%H", logFileInfo.Uri.Host);
        _ = builder.Replace("%T", logFileInfo.Uri.Port.ToString());

        var sPos = 0;
        string reg;
        string replace;
        do
        {
            reg = GetNextGroup(builder, ref sPos);
            replace = GetNextGroup(builder, ref sPos);
            if (reg != null && replace != null)
            {
                var result = Regex.Replace(logLine.FullLine, reg, replace);
                _ = builder.Insert(sPos, result);
            }
        } while (replace != null);

        var i = 0;
        while (i < builder.Length)
        {
            // ?"Pinpad-type?"(thales,dione)
            if (builder[i] == '?')
            {
                var end = i;
                var ask = "Parameter";
                if (builder[i + 1] == '"')
                {
                    end = builder.ToString().IndexOf('"', i + 2);
                    if (end == -1)
                    {
                        end = builder.Length - 1;
                    }

                    ask = builder.ToString().Substring(i + 2, end - i - 2);
                }

                string[] values = null;

                if (builder[end + 1] == '(')
                {
                    var end2 = builder.ToString().IndexOf(')', StringComparison.Ordinal);
                    if (end2 == -1)
                    {
                        end2 = builder.Length - 1;
                    }
                    var valueStr = builder.ToString().Substring(end + 2, end2 - end - 2);
                    values = valueStr.Split([','], StringSplitOptions.None);
                    end = end2;
                }

                ParamRequesterDialog dlg = new()
                {
                    ParamName = ask,
                    Values = values
                };

                DialogResult res = dlg.ShowDialog(parent);

                if (res is DialogResult.OK)
                {
                    _ = builder.Remove(i, end - i + 1);
                    _ = builder.Insert(i, dlg.ParamValue);
                }
                else if (res is DialogResult.Cancel or DialogResult.Abort)
                {
                    return null;
                }
            }

            ++i;
        }

        return builder.ToString();
    }

    #endregion

    #region Private Methods

    private string GetNextGroup (StringBuilder builder, ref int sPos)
    {
        int ePos;
        while (sPos < builder.Length)
        {
            if (builder[sPos] == '{')
            {
                ePos = sPos + 1;
                var count = 1;

                while (ePos < builder.Length)
                {
                    if (builder[ePos] == '{')
                    {
                        count++;
                    }

                    if (builder[ePos] == '}')
                    {
                        count--;
                    }

                    if (count == 0)
                    {
                        var reg = builder.ToString(sPos + 1, ePos - sPos - 1);
                        _ = builder.Remove(sPos, ePos - sPos + 1);
                        return reg;
                    }

                    ePos++;
                }
            }

            sPos++;
        }

        return null;
    }

    #endregion
}