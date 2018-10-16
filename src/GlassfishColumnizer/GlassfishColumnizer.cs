﻿using System;
using System.Globalization;
using System.Linq;
using LogExpert;

namespace GlassfishColumnizer
{
    internal class XmlConfig : IXmlLogConfiguration
    {
        #region Interface IXmlLogConfiguration

        public string[] Namespace => null;

        public string Stylesheet { get; } = null;

        public string XmlEndTag { get; } = "|#]";

        public string XmlStartTag { get; } = "[#|";

        #endregion
    }


    internal class GlassfishColumnizer : ILogLineXmlColumnizer
    {
        #region Static/Constants

        public const int COLUMN_COUNT = 2;
        protected const string DATETIME_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffzzzz";
        protected const string DATETIME_FORMAT_OUT = "yyyy-MM-dd HH:mm:ss.fff";

        private static readonly XmlConfig xmlConfig = new XmlConfig();

        #endregion

        #region Private Fields

        private readonly char separatorChar = '|';
        private readonly char[] trimChars = {'|'};
        protected CultureInfo cultureInfo = new CultureInfo("en-US");
        protected int timeOffset;

        #endregion

        #region Interface ILogLineXmlColumnizer

        public int GetColumnCount()
        {
            return COLUMN_COUNT;
        }

        public string[] GetColumnNames()
        {
            return new[] {"Date/Time", "Message"};
        }

        public string GetDescription()
        {
            return "Parse the timestamps in Glassfish logfiles.";
        }

        public ILogLine GetLineTextForClipboard(ILogLine logLine, ILogLineColumnizerCallback callback)
        {
            GlassFishLogLine line = new GlassFishLogLine
            {
                FullLine = logLine.FullLine.Replace(separatorChar, '|'),
                LineNumber = logLine.LineNumber
            };

            return line;
        }

        public string GetName()
        {
            return "Classfish";
        }

        public int GetTimeOffset()
        {
            return timeOffset;
        }

        public DateTime GetTimestamp(ILogLineColumnizerCallback callback, ILogLine logLine)
        {
            string temp = logLine.FullLine;

            // delete '[#|' and '|#]'
            if (temp.StartsWith("[#|"))
            {
                temp = temp.Substring(3);
            }

            if (temp.EndsWith("|#]"))
            {
                temp = temp.Substring(0, temp.Length - 3);
            }

            if (temp.Length < 28)
            {
                return DateTime.MinValue;
            }

            int endIndex = temp.IndexOf(separatorChar, 1);
            if (endIndex > 28 || endIndex < 0)
            {
                return DateTime.MinValue;
            }

            string value = temp.Substring(0, endIndex);

            try
            {
                // convert glassfish timestamp into a readable format:
                DateTime timestamp;
                if (DateTime.TryParseExact(value, DATETIME_FORMAT, cultureInfo,
                    DateTimeStyles.None, out timestamp))
                {
                    return timestamp.AddMilliseconds(timeOffset);
                }

                return DateTime.MinValue;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        public IXmlLogConfiguration GetXmlLogConfiguration()
        {
            return xmlConfig;
        }


        public bool IsTimeshiftImplemented()
        {
            return true;
        }

        public void PushValue(ILogLineColumnizerCallback callback, int column, string value, string oldValue)
        {
            if (column == 0)
            {
                try
                {
                    DateTime newDateTime = DateTime.ParseExact(value, DATETIME_FORMAT_OUT, cultureInfo);
                    DateTime oldDateTime = DateTime.ParseExact(oldValue, DATETIME_FORMAT_OUT, cultureInfo);
                    long mSecsOld = oldDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                    long mSecsNew = newDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                    timeOffset = (int)(mSecsNew - mSecsOld);
                }
                catch (FormatException)
                {
                }
            }
        }

        public void SetTimeOffset(int msecOffset)
        {
            timeOffset = msecOffset;
        }

        public IColumnizedLogLine SplitLine(ILogLineColumnizerCallback callback, ILogLine line)
        {
            ColumnizedLogLine cLogLine = new ColumnizedLogLine();
            cLogLine.LogLine = line;

            string temp = line.FullLine;

            Column[] columns = Column.CreateColumns(COLUMN_COUNT, cLogLine);
            cLogLine.ColumnValues = columns.Select(a => a as IColumn).ToArray();


            // delete '[#|' and '|#]'
            if (temp.StartsWith("[#|"))
            {
                temp = temp.Substring(3);
            }

            if (temp.EndsWith("|#]"))
            {
                temp = temp.Substring(0, temp.Length - 3);
            }

            // If the line is too short (i.e. does not follow the format for this columnizer) return the whole line content
            // in colum 8 (the log message column). Date and time column will be left blank.
            if (temp.Length < 28)
            {
                columns[1].FullValue = temp;
            }
            else
            {
                try
                {
                    DateTime dateTime = GetTimestamp(callback, line);
                    if (dateTime == DateTime.MinValue)
                    {
                        columns[1].FullValue = temp;
                    }

                    string newDate = dateTime.ToString(DATETIME_FORMAT_OUT);
                    columns[0].FullValue = newDate;
                }
                catch (Exception)
                {
                    columns[0].FullValue = "n/a";
                }

                Column timestmp = columns[0];

                string[] cols;
                cols = temp.Split(trimChars, COLUMN_COUNT, StringSplitOptions.None);

                if (cols.Length != COLUMN_COUNT)
                {
                    columns[0].FullValue = string.Empty;
                    columns[1].FullValue = temp;
                }
                else
                {
                    columns[0] = timestmp;
                    columns[1].FullValue = cols[1];
                }
            }

            return cLogLine;
        }

        #endregion

        #region Properties / Indexers

        public string Text => GetName();

        #endregion

        #region Nested type: GlassFishLogLine

        private class GlassFishLogLine : ILogLine
        {
            #region Interface ILogLine

            public string FullLine { get; set; }

            public int LineNumber { get; set; }

            string ITextValue.Text => FullLine;

            #endregion
        }

        #endregion
    }
}
