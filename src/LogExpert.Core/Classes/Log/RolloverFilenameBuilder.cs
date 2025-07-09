using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LogExpert.Core.Classes.Log;

/* Needed info:
 * - Date/time mask
 * - index counters
 * - counter direction (up/down)
 * - counter limit
 * - whether the files are shifted or not
 * - whether the indexes start with zero (or n/a) on a new date period
 *
 * Format:
 * *$D(yyyy-MM-dd)$I
 * *$J(.)
 *
 * *(yyyy-MM-dd)[I]
 *
 */

/// <summary>
/// This class is responsible for building file names for multifile.
/// </summary>
public class RolloverFilenameBuilder
{
    #region Fields

    private string _condContent;
    private Group _condGroup;
    private string _currentFileName;

    private Group _dateGroup;

    //private Regex regexCond;
    private DateTime _dateTime;

    //private DateTimeFormatInfo dateFormat;
    private string _dateTimeFormat;

    private bool _hideZeroIndex;
    private Group _indexGroup;
    private Regex _regex;

    #endregion

    #region cTor

    public RolloverFilenameBuilder (string formatString)
    {
        ParseFormatString(formatString);
    }

    #endregion

    #region Properties

    public int Index { get; set; }

    public bool IsDatePattern => _dateGroup != null && _dateGroup.Success;

    public bool IsIndexPattern => _indexGroup != null && _indexGroup.Success;

    #endregion

    #region Public methods

    public void SetFileName (string fileName)
    {
        _currentFileName = fileName;
        Match match = _regex.Match(fileName);
        if (match.Success)
        {
            _dateGroup = match.Groups["date"];
            if (_dateGroup.Success)
            {
                var date = fileName.Substring(_dateGroup.Index, _dateGroup.Length);
                if (DateTime.TryParseExact(date, _dateTimeFormat, DateTimeFormatInfo.InvariantInfo,
                    DateTimeStyles.None,
                    out _dateTime))
                {
                }
            }

            _indexGroup = match.Groups["index"];
            if (_indexGroup.Success)
            {
                Index = _indexGroup.Value.Length > 0 ? int.Parse(_indexGroup.Value) : 0;
            }

            _condGroup = match.Groups["cond"];
        }
    }

    public void IncrementDate ()
    {
        _dateTime = _dateTime.AddDays(1);
    }

    public void DecrementDate ()
    {
        _dateTime = _dateTime.AddDays(-1);
    }


    public string BuildFileName ()
    {
        var fileName = _currentFileName;
        if (_dateGroup != null && _dateGroup.Success)
        {
            var newDate = _dateTime.ToString(_dateTimeFormat, DateTimeFormatInfo.InvariantInfo);
            fileName = fileName.Remove(_dateGroup.Index, _dateGroup.Length);
            fileName = fileName.Insert(_dateGroup.Index, newDate);
        }

        if (_indexGroup != null && _indexGroup.Success)
        {
            fileName = fileName.Remove(_indexGroup.Index, _indexGroup.Length);

            if (!_hideZeroIndex || Index > 0)
            {
                var format = "D" + _indexGroup.Length;
                fileName = fileName.Insert(_indexGroup.Index, Index.ToString(format));
                if (_hideZeroIndex && _condContent != null)
                {
                    fileName = fileName.Insert(_indexGroup.Index, _condContent);
                }
            }
        }

        //      this.currentFileName = fileName;
        //      SetFileName(fileName);
        return fileName;
    }

    #endregion

    #region Private Methods

    private void ParseFormatString (string formatString)
    {
        var fmt = EscapeNonvarRegions(formatString);
        var datePos = formatString.IndexOf("$D(", StringComparison.Ordinal);

        if (datePos != -1)
        {
            var endPos = formatString.IndexOf(')', datePos);
            if (endPos != -1)
            {
                _dateTimeFormat = formatString.Substring(datePos + 3, endPos - datePos - 3)
                                              .ToUpperInvariant()
                                              .Replace('D', 'd')
                                              .Replace('Y', 'y');

                var dtf = _dateTimeFormat.ToUpper(CultureInfo.InvariantCulture)
                                         .Replace("D", "\\d", StringComparison.Ordinal)
                                         .Replace("Y", "\\d", StringComparison.Ordinal)
                                         .Replace("M", "\\d", StringComparison.Ordinal);

                fmt = fmt.Remove(datePos, 2) // remove $D
                         .Remove(datePos + 1, _dateTimeFormat.Length) // replace with regex version of format
                         .Insert(datePos + 1, dtf)
                         .Insert(datePos + 1, "?'date'"); // name the regex group
            }
        }

        var condPos = fmt.IndexOf("$J(", StringComparison.Ordinal);
        if (condPos != -1)
        {
            var endPos = fmt.IndexOf(')', condPos);
            if (endPos != -1)
            {
                _condContent = fmt.Substring(condPos + 3, endPos - condPos - 3);
                fmt = fmt.Remove(condPos + 2, endPos - condPos - 1);
            }
        }

        fmt = fmt.Replace("*", ".*", StringComparison.Ordinal);
        _hideZeroIndex = fmt.Contains("$J", StringComparison.Ordinal);
        fmt = fmt.Replace("$I", "(?'index'[\\d]+)", StringComparison.Ordinal);
        fmt = fmt.Replace("$J", "(?'index'[\\d]*)", StringComparison.Ordinal);

        _regex = new Regex(fmt);
    }

    private string EscapeNonvarRegions (string formatString)
    {
        var fmt = formatString.Replace('*', '\xFFFD');
        var state = 0;

        StringBuilder result = new();
        StringBuilder segment = new();

        for (var i = 0; i < fmt.Length; ++i)
        {
            switch (state)
            {
                case 0: // looking for $
                    if (fmt[i] == '$')
                    {
                        _ = result.Append(Regex.Escape(segment.ToString()));
                        segment = new StringBuilder();
                        state = 1;
                    }

                    _ = segment.Append(fmt[i]);
                    break;
                case 1: // the char behind $
                    _ = segment.Append(fmt[i]);
                    _ = result.Append(segment);
                    segment = new StringBuilder();
                    state = 2;
                    break;
                case 2: // checking if ( or other char
                    if (fmt[i] == '(')
                    {
                        _ = segment.Append(fmt[i]);
                        state = 3;
                    }
                    else
                    {
                        _ = segment.Append(fmt[i]);
                        state = 0;
                    }

                    break;
                case 3: // looking for )
                    _ = segment.Append(fmt[i]);
                    if (fmt[i] == ')')
                    {
                        _ = result.Append(segment);
                        segment = new StringBuilder();
                        state = 0;
                    }

                    break;
            }
        }

        fmt = result.ToString().Replace('\xFFFD', '*');
        return fmt;
    }

    #endregion
}