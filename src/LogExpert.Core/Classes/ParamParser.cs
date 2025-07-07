using System.Text;
using System.Text.RegularExpressions;

namespace LogExpert.Core.Classes;

public class ParamParser (string argTemplate)
{
    #region Public methods

    public string ReplaceParams (ILogLine logLine, int lineNum, string fileName)
    {
        FileInfo fileInfo = new(fileName);
        StringBuilder builder = new(argTemplate);
        _ = builder.Replace("%L", "" + lineNum);
        _ = builder.Replace("%P", fileInfo.DirectoryName.Contains(' ', StringComparison.Ordinal)
                ? "\"" + fileInfo.DirectoryName + "\""
                : fileInfo.DirectoryName);
        _ = builder.Replace("%N", fileInfo.Name.Contains(' ', StringComparison.Ordinal)
                ? "\"" + fileInfo.Name + "\""
                : fileInfo.Name);
        _ = builder.Replace("%F",
            fileInfo.FullName.Contains(' ', StringComparison.Ordinal)
                ? "\"" + fileInfo.FullName + "\""
                : fileInfo.FullName);
        _ = builder.Replace("%E", fileInfo.Extension.Contains(' ', StringComparison.Ordinal)
                ? "\"" + fileInfo.Extension + "\""
                : fileInfo.Extension);
        var stripped = StripExtension(fileInfo.Name);
        _ = builder.Replace("%M", stripped.Contains(' ', StringComparison.Ordinal)
                ? "\"" + stripped + "\""
                : stripped);
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
                builder.Insert(sPos, result);
            }
        } while (replace != null);
        return builder.ToString();
    }

    public static string StripExtension (string fileName)
    {
        var i = fileName.LastIndexOf('.');

        if (i < 0)
        {
            i = fileName.Length - 1;
        }

        return fileName[..i];
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