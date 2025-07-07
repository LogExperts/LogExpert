using LogExpert.Core.Entities;

namespace LogExpert.UI.Extensions;

internal static class BookmarkExporter
{
    #region Fields

    private const string replacementForNewLine = @"\n";

    #endregion

    #region Public methods

    //TOOD: check if the callers are checking for null before calling
    public static void ExportBookmarkList (SortedList<int, Bookmark> bookmarkList, string logfileName, string fileName)
    {
        ArgumentNullException.ThrowIfNull(bookmarkList, nameof(bookmarkList));
        using FileStream fs = new(fileName, FileMode.Create, FileAccess.Write);
        using StreamWriter writer = new(fs);

        writer.WriteLine("Log file name;Line number;Comment");

        foreach (var bookmark in bookmarkList.Values)
        {
            var text = bookmark.Text ?? string.Empty;

            text = text
                .Replace(replacementForNewLine, @"\" + replacementForNewLine, StringComparison.OrdinalIgnoreCase)
                .Replace("\r\n", replacementForNewLine, StringComparison.OrdinalIgnoreCase);

            var line = $"{logfileName};{bookmark.LineNum};{text}";

            writer.WriteLine(line);
        }
    }

    public static void ImportBookmarkList (string logfileName, string fileName, SortedList<int, Bookmark> bookmarkList)
    {
        using FileStream fs = new(fileName, FileMode.Open, FileAccess.Read);
        using StreamReader reader = new(fs);
        if (!reader.EndOfStream)
        {
            _ = reader.ReadLine(); // skip "Log file name;Line number;Comment"
        }

        while (!reader.EndOfStream)
        {
            try
            {
                var line = reader.ReadLine();
                line = line
                    .Replace(replacementForNewLine, "\r\n", StringComparison.OrdinalIgnoreCase)
                    .Replace("\\\r\n", replacementForNewLine, StringComparison.OrdinalIgnoreCase);

                // Line is formatted: logfileName ";" bookmark.LineNum ";" bookmark.Text;
                var parts = line.Split(';', 3, StringSplitOptions.RemoveEmptyEntries);

                // parts[0] = fileStr
                // parts[1] = line number
                // parts[2] = comment
                if (int.TryParse(parts[1], out var lineNum) && parts[0] == logfileName)
                {
                    Bookmark bookmark = new(lineNum, parts[2]);
                    bookmarkList.Add(lineNum, bookmark);
                }
            }
            catch
            {
                //!!!
            }
        }
    }

    #endregion
}