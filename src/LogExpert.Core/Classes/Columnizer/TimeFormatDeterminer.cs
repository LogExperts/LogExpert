using System.Globalization;

namespace LogExpert.Core.Classes.Columnizer;

internal class TimeFormatDeterminer
{
    #region FormatInfo helper class

    public class FormatInfo (string dateFormat, string timeFormat, CultureInfo cultureInfo)
    {

        #region Properties

        public string DateFormat { get; } = dateFormat;

        public string TimeFormat { get; } = timeFormat;

        public CultureInfo CultureInfo { get; } = cultureInfo;

        public string DateTimeFormat => DateFormat + " " + TimeFormat;

        public bool IgnoreFirstChar { get; set; }

        #endregion
    }

    #endregion

    private readonly FormatInfo formatInfo1 = new("dd.MM.yyyy", "HH:mm:ss.fff", new CultureInfo("de-DE"));
    private readonly FormatInfo formatInfo2 = new("dd.MM.yyyy", "HH:mm:ss", new CultureInfo("de-DE"));
    private readonly FormatInfo formatInfo3 = new("yyyy/MM/dd", "HH:mm:ss.fff", new CultureInfo("en-US"));
    private readonly FormatInfo formatInfo4 = new("yyyy/MM/dd", "HH:mm:ss", new CultureInfo("en-US"));
    private readonly FormatInfo formatInfo5 = new("yyyy.MM.dd", "HH:mm:ss.fff", new CultureInfo("de-DE"));
    private readonly FormatInfo formatInfo6 = new("yyyy.MM.dd", "HH:mm:ss", new CultureInfo("de-DE"));
    private readonly FormatInfo formatInfo7 = new("dd.MM.yyyy", "HH:mm:ss,fff", new CultureInfo("de-DE"));
    private readonly FormatInfo formatInfo8 = new("yyyy/MM/dd", "HH:mm:ss,fff", new CultureInfo("en-US"));
    private readonly FormatInfo formatInfo9 = new("yyyy.MM.dd", "HH:mm:ss,fff", new CultureInfo("de-DE"));
    private readonly FormatInfo formatInfo10 = new("yyyy-MM-dd", "HH:mm:ss.fff", new CultureInfo("en-US"));
    private readonly FormatInfo formatInfo11 = new("yyyy-MM-dd", "HH:mm:ss,fff", new CultureInfo("en-US"));
    private readonly FormatInfo formatInfo12 = new("yyyy-MM-dd", "HH:mm:ss", new CultureInfo("en-US"));
    private readonly FormatInfo formatInfo13 = new("dd MMM yyyy", "HH:mm:ss,fff", new CultureInfo("de-DE"));
    private readonly FormatInfo formatInfo14 = new("dd MMM yyyy", "HH:mm:ss.fff", new CultureInfo("de-DE"));
    private readonly FormatInfo formatInfo15 = new("dd MMM yyyy", "HH:mm:ss", new CultureInfo("de-DE"));
    private readonly FormatInfo formatInfo16 = new("dd.MM.yy", "HH:mm:ss.fff", new CultureInfo("de-DE"));
    private readonly FormatInfo formatInfo17 = new("yyyy-MM-dd", "HH:mm:ss:ffff", new CultureInfo("en-US"));
    private readonly FormatInfo formatInfo18 = new("dd/MM/yyyy", "HH:mm:ss.fff", new CultureInfo("en-US"));
    private readonly FormatInfo formatInfo19 = new("dd/MM/yyyy", "HH:mm:ss:fff", new CultureInfo("en-US"));
    private readonly FormatInfo formatInfo20 = new("yyyy-MM-dd", "HH:mm:ss.ffff", new CultureInfo("en-US"));
    private readonly FormatInfo formatInfo21 = new("yyyy-MM-dd", "HH:mm:ss,ffff", new CultureInfo("en-US"));


    public FormatInfo DetermineDateTimeFormatInfo (string line)
    {
        if (line.Length < 21)
        {
            return null;
        }

        var temp = line;
        var ignoreFirst = false;

        // determine if string starts with bracket and remove it
        if (temp[0] is '[' or '(' or '{')
        {
            temp = temp[1..];
            ignoreFirst = true;

        }

        // dirty hardcoded probing of date/time format (much faster than DateTime.ParseExact()
        if (temp[2] == '.' && temp[5] == '.' && temp[13] == ':' && temp[16] == ':')
        {
            if (temp[19] == '.')
            {
                formatInfo1.IgnoreFirstChar = ignoreFirst;
                return formatInfo1;
            }
            else if (temp[19] == ',')
            {
                formatInfo7.IgnoreFirstChar = ignoreFirst;
                return formatInfo7;
            }
            else
            {
                formatInfo2.IgnoreFirstChar = ignoreFirst;
                return formatInfo2;
            }
        }
        else if (temp[2] == '/' && temp[5] == '/' && temp[13] == ':' && temp[16] == ':')
        {
            if (temp[19] == '.')
            {
                formatInfo18.IgnoreFirstChar = ignoreFirst;
                return formatInfo18;
            }
            else if (temp[19] == ':')
            {
                formatInfo19.IgnoreFirstChar = ignoreFirst;
                return formatInfo19;
            }
        }
        else if (temp[4] == '/' && temp[7] == '/' && temp[13] == ':' && temp[16] == ':')
        {
            if (temp[19] == '.')
            {
                formatInfo3.IgnoreFirstChar = ignoreFirst;
                return formatInfo3;
            }
            else if (temp[19] == ',')
            {
                formatInfo8.IgnoreFirstChar = ignoreFirst;
                return formatInfo8;
            }
            else
            {
                formatInfo4.IgnoreFirstChar = ignoreFirst;
                return formatInfo4;
            }
        }
        else if (temp[4] == '.' && temp[7] == '.' && temp[13] == ':' && temp[16] == ':')
        {
            if (temp[19] == '.')
            {
                formatInfo5.IgnoreFirstChar = ignoreFirst;
                return formatInfo5;
            }
            else if (temp[19] == ',')
            {
                formatInfo9.IgnoreFirstChar = ignoreFirst;
                return formatInfo9;
            }
            else
            {
                formatInfo6.IgnoreFirstChar = ignoreFirst;
                return formatInfo6;
            }
        }
        else if (temp[4] == '-' && temp[7] == '-' && temp[13] == ':' && temp[16] == ':')
        {
            if (temp[19] == '.')
            {
                if (temp.Length > 23 && char.IsDigit(temp[23]))
                {
                    formatInfo20.IgnoreFirstChar = ignoreFirst;
                    return formatInfo20;
                }
                else
                {
                    formatInfo10.IgnoreFirstChar = ignoreFirst;
                    return formatInfo10;
                }
            }
            else if (temp[19] == ',')
            {
                if (temp.Length > 23 && char.IsDigit(temp[23]))
                {
                    formatInfo21.IgnoreFirstChar = ignoreFirst;
                    return formatInfo21;
                }
                else
                {
                    formatInfo11.IgnoreFirstChar = ignoreFirst;
                    return formatInfo11;
                }
            }
            else if (temp[19] == ':')
            {
                formatInfo17.IgnoreFirstChar = ignoreFirst;
                return formatInfo17;
            }
            else
            {
                formatInfo12.IgnoreFirstChar = ignoreFirst;
                return formatInfo12;
            }
        }
        else if (temp[2] == ' ' && temp[6] == ' ' && temp[14] == ':' && temp[17] == ':')
        {
            if (temp[20] == ',')
            {
                formatInfo13.IgnoreFirstChar = ignoreFirst;
                return formatInfo13;
            }
            else if (temp[20] == '.')
            {
                formatInfo14.IgnoreFirstChar = ignoreFirst;
                return formatInfo14;
            }
            else
            {
                formatInfo15.IgnoreFirstChar = ignoreFirst;
                return formatInfo15;
            }
        }
        //dd.MM.yy HH:mm:ss.fff
        else if (temp[2] == '.' && temp[5] == '.' && temp[11] == ':' && temp[14] == ':' && temp[17] == '.')
        {
            formatInfo16.IgnoreFirstChar = ignoreFirst;
            return formatInfo16;
        }

        return null;
    }

    public FormatInfo DetermineTimeFormatInfo (string field)
    {
        // dirty hardcoded probing of time format (much faster than DateTime.ParseExact()
        if (field[2] == ':' && field[5] == ':')
        {
            if (field.Length > 8)
            {
                if (field[8] == '.')
                {
                    return formatInfo1;
                }
                else if (field[8] == ',')
                {
                    return formatInfo7;
                }
            }
            else
            {
                return formatInfo2;
            }
        }

        return null;
    }
}