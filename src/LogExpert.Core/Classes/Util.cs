using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

using ColumnizerLib;

using LogExpert.Core.Classes.Filter;

namespace LogExpert.Core.Classes;

public static class Util
{
    #region Public methods

    public static string GetNameFromPath (string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));

        var i = fileName.LastIndexOf('\\');

        if (i < 0)
        {
            i = fileName.LastIndexOf('/');
        }

        if (i < 0)
        {
            i = -1;
        }

        return fileName[(i + 1)..];
    }

    public static string StripExtension (string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));

        var i = fileName.LastIndexOf('.');

        if (i < 0)
        {
            i = fileName.Length - 1;
        }

        return fileName[..i];
    }

    public static string GetExtension (string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));

        var i = fileName.LastIndexOf('.');

        return i < 0 || i >= fileName.Length - 1
            ? string.Empty
            : fileName[(i + 1)..];
    }

    public static string GetFileSizeAsText (long size)
    {
        return size < 1024
            ? $"{size} bytes"
            : size < 1024 * 1024
                ? $"{size / 1024} KB"
                : $"{size / 1048576.0:0.00} MB";
    }

    public static bool TestFilterCondition (FilterParams filterParams, ILogLineMemory logLine, ILogLineMemoryColumnizerCallback columnizerCallback)
    {
        ArgumentNullException.ThrowIfNull(filterParams, nameof(filterParams));
        ArgumentNullException.ThrowIfNull(logLine, nameof(logLine));

        // TODO: Once FilterParams.LastLine is converted to ReadOnlyMemory<char>, this can be simplified to:
        // if (MemoryExtensions.Equals(filterParams.LastLine.Span, logLine.FullLine.Span, StringComparison.OrdinalIgnoreCase))
        if (MemoryExtensions.Equals(filterParams.LastLine.AsSpan(), logLine.FullLine.Span, StringComparison.OrdinalIgnoreCase))
        {
            return filterParams.LastResult;
        }

        var match = TestFilterMatch(filterParams, logLine, columnizerCallback);

        // TODO: This ToString() allocation will be eliminated when LastLine becomes ReadOnlyMemory<char>
        filterParams.LastLine = logLine.FullLine.ToString();

        if (filterParams.IsRangeSearch)
        {
            if (!filterParams.IsInRange)
            {
                if (match)
                {
                    filterParams.IsInRange = true;
                }
            }
            else
            {
                if (!match)
                {
                    match = true;
                }
                else
                {
                    filterParams.IsInRange = false;
                }
            }
        }

        if (filterParams.IsInvert)
        {
            match = !match;
        }

        filterParams.LastResult = match;
        return match;
    }

    public static int DamerauLevenshteinDistance (ReadOnlySpan<char> source, ReadOnlySpan<char> destination, bool ignoreCase = false)
    {
        var d = new int[source.Length + 1, destination.Length + 1];

        for (var i = 0; i <= source.Length; i++)
        {
            d[i, 0] = i;
        }

        for (var j = 0; j <= destination.Length; j++)
        {
            d[0, j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= destination.Length; j++)
            {
                var char1 = ignoreCase
                    ? char.ToUpperInvariant(source[i - 1])
                    : source[i - 1];

                var char2 = ignoreCase
                    ? char.ToUpperInvariant(destination[j - 1])
                    : destination[j - 1];

                var cost = char1 == char2
                    ? 0
                    : 1;

                d[i, j] = Math.Min
                    (
                        d[i - 1, j] + 1,        // Deletion
                        Math.Min
                        (
                            d[i, j - 1] + 1,    // Insertion
                            d[i - 1, j - 1] + cost // Substitution
                        )
                    );

                // Transposition
                if (i > 1 && j > 1)
                {
                    var prevChar1 = ignoreCase
                        ? char.ToUpperInvariant(source[i - 2])
                        : source[i - 2];

                    var prevChar2 = ignoreCase
                        ? char.ToUpperInvariant(destination[j - 2])
                        : destination[j - 2];

                    if (char1 == prevChar2 && prevChar1 == char2)
                    {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                    }
                }
            }
        }

        return d[source.Length, destination.Length];
    }

    public static unsafe int YetiLevenshtein (string s1, string s2)
    {
        ArgumentNullException.ThrowIfNull(s1, nameof(s1));
        ArgumentNullException.ThrowIfNull(s2, nameof(s2));

        if (ReferenceEquals(s1, s2))
        {
            return 0;
        }

        if (s1.Length == 0)
        {
            return s2.Length;
        }

        if (s2.Length == 0)
        {
            return s1.Length;
        }

        fixed (char* p1 = s1)
        fixed (char* p2 = s2)
        {
            return YetiLevenshtein(p1, s1.Length, p2, s2.Length, 0); // substitutionCost = 1
        }
    }

    public static unsafe int YetiLevenshtein (ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
    {
        int len1 = s1.Length;
        int len2 = s2.Length;

        if (len1 == 0)
        {
            return len2;
        }

        if (len2 == 0)
        {
            return len1;
        }

        fixed (char* p1 = s1)
        fixed (char* p2 = s2)
        {
            return YetiLevenshtein(p1, s1.Length, p2, s2.Length, 0);
        }
    }

    public static unsafe int YetiLevenshtein (string s1, string s2, int substitutionCost)
    {
        var xc = substitutionCost - 1;

        if (xc is < 0 or > 1)
        {
            throw new ArgumentException("", nameof(substitutionCost));
        }

        fixed (char* p1 = s1)
        fixed (char* p2 = s2)
        {
            return YetiLevenshtein(p1, s1.Length, p2, s2.Length, xc);
        }
    }

    /// <summary>
    /// Cetin Sert, David Necas
    /// <a href="http://webcleaner.svn.sourceforge.net/viewvc/webcleaner/trunk/webcleaner2/wc/levenshtein.c?revision=6015&amp;view=markup">
    /// Source Code</a>
    /// </summary>
    /// <param name="s1"></param>
    /// <param name="l1"></param>
    /// <param name="s2"></param>
    /// <param name="l2"></param>
    /// <param name="xcost"></param>
    /// <returns></returns>
    public static unsafe int YetiLevenshtein (char* s1, int l1, char* s2, int l2, int xcost)
    {
        int i;
        //int *row;  /* we only need to keep one row of costs */
        int* end;
        int half;

        /* strip common prefix */
        while (l1 > 0 && l2 > 0 && *s1 == *s2)
        {
            l1--;
            l2--;
            s1++;
            s2++;
        }

        /* strip common suffix */
        while (l1 > 0 && l2 > 0 && s1[l1 - 1] == s2[l2 - 1])
        {
            l1--;
            l2--;
        }

        /* catch trivial cases */
        if (l1 == 0)
        {
            return l2;
        }

        if (l2 == 0)
        {
            return l1;
        }

        /* make the inner cycle (i.e. string2) the longer one */
        if (l1 > l2)
        {
            var nx = l1;
            var sx = s1;
            l1 = l2;
            l2 = nx;
            s1 = s2;
            s2 = sx;
        }

        //check len1 == 1 separately
        if (l1 == 1)
        {
            //throw new NotImplementedException();
            return xcost > 0
                ? l2 + 1 - (2 * MemchrRPLC(s2, *s1, l2))
                : l2 - MemchrRPLC(s2, *s1, l2);
        }

        l1++;
        l2++;
        half = l1 >> 1;

        /* initalize first row */
        //row = (int*)malloc(l2*sizeof(int));
        var row = stackalloc int[l2];

        if (l2 < 0)
        //if (!row)
        {
            return -1;
        }

        end = row + l2 - 1;

        for (i = 0; i < l2 - (xcost > 0 ? 0 : half); i++)
        {
            row[i] = i;
        }

        /* go through the matrix and compute the costs.  yes, this is an extremely
       * obfuscated version, but also extremely memory-conservative and
       * relatively fast.
       */
        if (xcost > 0)
        {
            for (i = 1; i < l1; i++)
            {
                var p = row + 1;
                var char1 = s1[i - 1];
                var char2p = s2;
                var D = i;
                var x = i;

                while (p <= end)
                {
                    if (char1 == *char2p++)
                    {
                        x = --D;
                    }
                    else
                    {
                        x++;
                    }

                    D = *p;
                    D++;

                    if (x > D)
                    {
                        x = D;
                    }

                    *p++ = x;
                }
            }
        }
        else
        {
            /* in this case we don't have to scan two corner triangles (of size len1/2)
             * in the matrix because no best path can go throught them. note this
             * breaks when len1 == len2 == 2 so the memchr() special case above is
             * necessary */
            row[0] = l1 - half - 1;
            for (i = 1; i < l1; i++)
            {
                int* p;
                var char1 = s1[i - 1];
                char* char2p;
                int D, x;

                /* skip the upper triangle */
                if (i >= l1 - half)
                {
                    var offset = i - (l1 - half);
                    int c3;

                    char2p = s2 + offset;
                    p = row + offset;
                    c3 = *p++ + (char1 != *char2p++ ? 1 : 0);
                    x = *p;
                    x++;
                    D = x;

                    if (x > c3)
                    {
                        x = c3;
                    }

                    *p++ = x;
                }
                else
                {
                    p = row + 1;
                    char2p = s2;
                    D = x = i;
                }

                /* skip the lower triangle */
                if (i <= half + 1)
                {
                    end = row + l2 + i - half - 2;
                }

                /* main */
                while (p <= end)
                {
                    var c3 = --D + (char1 != *char2p++ ? 1 : 0);
                    x++;

                    if (x > c3)
                    {
                        x = c3;
                    }

                    D = *p;
                    D++;

                    if (x > D)
                    {
                        x = D;
                    }

                    *p++ = x;
                }

                /* lower triangle sentinel */
                if (i <= half)
                {
                    var c3 = --D + (char1 != *char2p ? 1 : 0);
                    x++;
                    if (x > c3)
                    {
                        x = c3;
                    }

                    *p = x;
                }
            }
        }

        i = *end;
        return i;
    }

    [Conditional("DEBUG")]
    public static void AssertTrue (bool condition, string msg)
    {
        if (!condition)
        {
            //Todo this should be done differently
            //MessageBox.Show("Assertion: " + msg);
            throw new Exception(msg);
        }
    }

    //TODO Add Null Check (https://github.com/LogExperts/LogExpert/issues/403)
    [SupportedOSPlatform("windows")]
    public static string? GetWordFromPos (int xPos, string text, Graphics g, Font font)
    {
        var words = text.Split([' ', '.', ':', ';']);

        var index = 0;

        List<CharacterRange> crList = [];

        for (var i = 0; i < words.Length; ++i)
        {
            crList.Add(new CharacterRange(index, words[i].Length));
            index += words[i].Length;
        }

        CharacterRange[] crArray = [.. crList];

        StringFormat stringFormat = new(StringFormat.GenericTypographic)
        {
            Trimming = StringTrimming.None,
            FormatFlags = StringFormatFlags.NoClip
        };

        stringFormat.SetMeasurableCharacterRanges(crArray);

        RectangleF rect = new(0, 0, 3000, 20);
        var stringRegions = g.MeasureCharacterRanges(text, font, rect, stringFormat);

        var found = false;

        var y = 0;

        foreach (var regio in stringRegions)
        {
            if (regio.IsVisible(xPos, 3, g))
            {
                found = true;
                break;
            }

            y++;
        }

        return found
            ? words[y]
            : null;
    }

    #endregion

    #region Private Methods

    private static bool TestFilterMatch (FilterParams filterParams, ILogLineMemory logLine, ILogLineMemoryColumnizerCallback columnizerCallback)
    {
        string normalizedSearchText;
        string searchText;
        Regex rex;

        if (filterParams.IsInRange)
        {
            normalizedSearchText = filterParams.NormalizedRangeSearchText;
            searchText = filterParams.RangeSearchText;
            rex = filterParams.RangeRex;
        }
        else
        {
            normalizedSearchText = filterParams.NormalizedSearchText;
            searchText = filterParams.SearchText;
            rex = filterParams.Regex;
        }

        if (string.IsNullOrEmpty(searchText))
        {
            return false;
        }

        if (filterParams.ColumnRestrict)
        {
            var columns = filterParams.CurrentColumnizer.SplitLine(columnizerCallback, logLine);
            var found = false;
            foreach (var colIndex in filterParams.ColumnList)
            {
                if (colIndex < columns.ColumnValues.Length) // just to be sure, maybe the columnizer has changed anyhow
                {
                    if (columns.ColumnValues[colIndex].FullValue.Trim().Length == 0)
                    {
                        if (filterParams.EmptyColumnUsePrev)
                        {
                            if (filterParams.LastNonEmptyCols.TryGetValue(colIndex, out var prevValue))
                            {
                                if (TestMatchSub(filterParams, prevValue, normalizedSearchText.AsSpan(), searchText.AsSpan(), rex, filterParams.ExactColumnMatch))
                                {
                                    found = true;
                                }
                            }
                        }
                        else if (filterParams.EmptyColumnHit)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        filterParams.LastNonEmptyCols[colIndex] = columns.ColumnValues[colIndex].FullValue;
                        if (TestMatchSub(filterParams, columns.ColumnValues[colIndex].FullValue, normalizedSearchText.AsSpan(), searchText.AsSpan(), rex, filterParams.ExactColumnMatch))
                        {
                            found = true;
                        }
                    }
                }
            }

            return found;
        }
        else
        {
            return TestMatchSub(filterParams, logLine.FullLine, normalizedSearchText.AsSpan(), searchText.AsSpan(), rex, false);
        }
    }

    private static bool TestMatchSub (
            FilterParams filterParams,
            ReadOnlySpan<char> line,
            ReadOnlySpan<char> normalizedSearchText,  // Pre-normalized (uppercase) // lowerSearchText
            ReadOnlySpan<char> searchText,
            Regex rex,
            bool exactMatch)
    {
        if (filterParams.IsRegex)
        {
            if (rex.IsMatch(line))
            {
                return true;
            }
        }
        else
        {
            if (!filterParams.IsCaseSensitive)
            {
                if (exactMatch)
                {
                    var trimmedLine = line.Trim();
                    if (MemoryExtensions.Equals(trimmedLine, normalizedSearchText, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                else
                {
                    if (line.Contains(normalizedSearchText, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (exactMatch)
                {
                    if (line.Equals(searchText, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                else
                {
                    if (line.Contains(searchText, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            if (filterParams.FuzzyValue > 0)
            {
                var range = line.Length - searchText.Length;
                if (range > 0)
                {
                    for (var i = 0; i <= range; ++i)
                    {
                        var src = line.Slice(i, searchText.Length);

                        var dist = DamerauLevenshteinDistance(src, searchText, !filterParams.IsCaseSensitive);

                        if ((searchText.Length + 1) / (float)(dist + 1) >= 11F / (float)(filterParams.FuzzyValue + 1F))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        return false;
    }

    private static bool TestMatchSub (
            FilterParams filterParams,
            ReadOnlyMemory<char> line,  // From ILogLineMemory.FullLine
            ReadOnlySpan<char> normalizedSearchText,
            ReadOnlySpan<char> searchText,
            Regex rex,
            bool exactMatch)
    {
        return TestMatchSub(filterParams, line.Span, normalizedSearchText, searchText, rex, exactMatch);
    }

    private static bool TestMatchSub (FilterParams filterParams, string line, string lowerSearchText, string searchText, Regex rex, bool exactMatch)
    {
        return TestMatchSub(filterParams, line.AsSpan(), lowerSearchText.AsSpan(), searchText.AsSpan(), rex, exactMatch);
    }

    private static unsafe int MemchrRPLC (char* buffer, char c, int count)
    {
        var p = buffer;
        var e = buffer + count;

        while (p++ < e)
        {
            if (*p == c)
            {
                return 1;
            }
        }

        return 0;
    }

    #endregion
}