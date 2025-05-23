using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;

using NLog;

using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace LogExpert.Core.Classes.Persister
{
    public class PersistenceData
    {
        #region Fields

        public SortedList<int, Entities.Bookmark> bookmarkList = [];
        public int bookmarkListPosition = 300;
        public bool bookmarkListVisible = false;
        public string columnizerName;
        public int currentLine = -1;
        public Encoding encoding;
        public string fileName = null;
        public bool filterAdvanced = false;
        public List<FilterParams> filterParamsList = [];
        public int filterPosition = 222;
        public bool filterSaveListVisible = false;
        public List<FilterTabData> filterTabDataList = [];
        public bool filterVisible = false;
        public int firstDisplayedLine = -1;
        public bool followTail = true;
        public string highlightGroupName;
        public int lineCount;

        public bool multiFile = false;
        public int multiFileMaxDays;
        public List<string> multiFileNames = [];
        public string multiFilePattern;
        public SortedList<int, RowHeightEntry> rowHeightList = [];
        public string sessionFileName = null;
        public bool showBookmarkCommentColumn;
        public string tabName = null;

        public string settingsSaveLoadLocation;

        #endregion
    }

    //TODO Rewrite as json Persister, xml is outdated and difficult to parse and write
    public class Persister
    {
        #region Fields

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Public methods

        public static string SavePersistenceData(string logFileName, PersistenceData persistenceData, Preferences preferences)
        {
            string fileName;

            if (persistenceData.sessionFileName != null)
            {
                fileName = persistenceData.sessionFileName;
            }
            else
            {
                fileName = BuildPersisterFileName(logFileName, preferences);
            }
            if (preferences.saveLocation == SessionSaveLocation.SameDir)
            {
                // make to log file in .lxp file relative
                string filePart = Path.GetFileName(persistenceData.fileName);
                persistenceData.fileName = filePart;
            }

            Save(fileName, persistenceData);
            return fileName;
        }

        public static string SavePersistenceDataWithFixedName(string persistenceFileName,
            PersistenceData persistenceData)
        {
            Save(persistenceFileName, persistenceData);
            return persistenceFileName;
        }


        public static PersistenceData LoadPersistenceData(string logFileName, Preferences preferences)
        {
            string fileName = BuildPersisterFileName(logFileName, preferences);
            return Load(fileName);
        }

        public static PersistenceData LoadPersistenceDataOptionsOnly(string logFileName, Preferences preferences)
        {
            string fileName = BuildPersisterFileName(logFileName, preferences);
            return LoadOptionsOnly(fileName);
        }

        public static PersistenceData LoadPersistenceDataOptionsOnlyFromFixedFile(string persistenceFile)
        {
            return LoadOptionsOnly(persistenceFile);
        }

        public static PersistenceData LoadPersistenceDataFromFixedFile(string persistenceFile)
        {
            return Load(persistenceFile);
        }


        /// <summary>
        /// Loads the persistence options out of the given persistence file name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static PersistenceData LoadOptionsOnly(string fileName)
        {
            PersistenceData persistenceData = new();
            XmlDocument xmlDoc = new();
            try
            {
                xmlDoc.Load(fileName);
            }
            catch (IOException)
            {
                return null;
            }
            XmlNode fileNode = xmlDoc.SelectSingleNode("logexpert/file");
            if (fileNode != null)
            {
                XmlElement fileElement = fileNode as XmlElement;
                ReadOptions(fileElement, persistenceData);
                persistenceData.fileName = fileElement.GetAttribute("fileName");
                persistenceData.encoding = ReadEncoding(fileElement);
            }
            return persistenceData;
        }

        #endregion

        #region Private Methods

        private static string BuildPersisterFileName(string logFileName, Preferences preferences)
        {
            string dir;
            string file;

            switch (preferences.saveLocation)
            {
                case SessionSaveLocation.SameDir:
                default:
                    {
                        FileInfo fileInfo = new(logFileName);
                        dir = fileInfo.DirectoryName;
                        file = fileInfo.DirectoryName + Path.DirectorySeparatorChar + fileInfo.Name + ".lxp";
                        break;
                    }
                case SessionSaveLocation.DocumentsDir:
                    {
                        dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                              Path.DirectorySeparatorChar +
                              "LogExpert";
                        file = dir + Path.DirectorySeparatorChar + BuildSessionFileNameFromPath(logFileName);
                        break;
                    }
                case SessionSaveLocation.OwnDir:
                    {
                        dir = preferences.sessionSaveDirectory;
                        file = dir + Path.DirectorySeparatorChar + BuildSessionFileNameFromPath(logFileName);
                        break;
                    }
                case SessionSaveLocation.ApplicationStartupDir:
                    {
                        //TODO Add Application.StartupPath as Variable
                        dir = string.Empty;// Application.StartupPath + Path.DirectorySeparatorChar + "sessionfiles";
                        file = dir + Path.DirectorySeparatorChar + BuildSessionFileNameFromPath(logFileName);
                        break;
                    }
            }

            if (string.IsNullOrWhiteSpace(dir) == false && Directory.Exists(dir) == false)
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception e)
                {
                    //TODO this needs to be handled differently
                    //MessageBox.Show(e.Message, "LogExpert");
                }
            }
            return file;
        }

        private static string BuildSessionFileNameFromPath(string logFileName)
        {
            string result = logFileName;
            result = result.Replace(Path.DirectorySeparatorChar, '_');
            result = result.Replace(Path.AltDirectorySeparatorChar, '_');
            result = result.Replace(Path.VolumeSeparatorChar, '_');
            result += ".lxp";
            return result;
        }

        private static void Save(string fileName, PersistenceData persistenceData)
        {
            XmlDocument xmlDoc = new();
            XmlElement rootElement = xmlDoc.CreateElement("logexpert");
            xmlDoc.AppendChild(rootElement);
            XmlElement fileElement = xmlDoc.CreateElement("file");
            rootElement.AppendChild(fileElement);
            fileElement.SetAttribute("fileName", persistenceData.fileName);
            fileElement.SetAttribute("lineCount", "" + persistenceData.lineCount);
            WriteBookmarks(xmlDoc, fileElement, persistenceData.bookmarkList);
            WriteRowHeightList(xmlDoc, fileElement, persistenceData.rowHeightList);
            WriteOptions(xmlDoc, fileElement, persistenceData);
            WriteFilter(xmlDoc, fileElement, persistenceData.filterParamsList);
            WriteFilterTabs(xmlDoc, fileElement, persistenceData.filterTabDataList);
            WriteEncoding(xmlDoc, fileElement, persistenceData.encoding);
            if (xmlDoc.HasChildNodes)
            {
                xmlDoc.Save(fileName);
            }
        }

        private static void WriteEncoding(XmlDocument xmlDoc, XmlElement rootElement, Encoding encoding)
        {
            if (encoding != null)
            {
                XmlElement encodingElement = xmlDoc.CreateElement("encoding");
                rootElement.AppendChild(encodingElement);
                encodingElement.SetAttribute("name", encoding.WebName);
            }
        }

        private static void WriteFilterTabs(XmlDocument xmlDoc, XmlElement rootElement, List<FilterTabData> dataList)
        {
            if (dataList.Count > 0)
            {
                XmlElement filterTabsElement = xmlDoc.CreateElement("filterTabs");
                rootElement.AppendChild(filterTabsElement);
                foreach (FilterTabData data in dataList)
                {
                    PersistenceData persistenceData = data.PersistenceData;
                    XmlElement filterTabElement = xmlDoc.CreateElement("filterTab");
                    filterTabsElement.AppendChild(filterTabElement);
                    WriteBookmarks(xmlDoc, filterTabElement, persistenceData.bookmarkList);
                    WriteRowHeightList(xmlDoc, filterTabElement, persistenceData.rowHeightList);
                    WriteOptions(xmlDoc, filterTabElement, persistenceData);
                    WriteFilter(xmlDoc, filterTabElement, persistenceData.filterParamsList);
                    WriteFilterTabs(xmlDoc, filterTabElement, persistenceData.filterTabDataList);
                    XmlElement filterElement = xmlDoc.CreateElement("tabFilter");
                    filterTabElement.AppendChild(filterElement);
                    List<FilterParams> filterList = [data.FilterParams];
                    WriteFilter(xmlDoc, filterElement, filterList);
                }
            }
        }

        private static List<FilterTabData> ReadFilterTabs(XmlElement startNode)
        {
            List<FilterTabData> dataList = [];
            XmlNode filterTabsNode = startNode.SelectSingleNode("filterTabs");
            if (filterTabsNode != null)
            {
                XmlNodeList filterTabNodeList = filterTabsNode.ChildNodes; // all "filterTab" nodes

                foreach (XmlNode node in filterTabNodeList)
                {
                    PersistenceData persistenceData = ReadPersistenceDataFromNode(node);
                    XmlNode filterNode = node.SelectSingleNode("tabFilter");

                    if (filterNode != null)
                    {
                        List<FilterParams> filterList = ReadFilter(filterNode as XmlElement);
                        FilterTabData data = new()
                        {
                            PersistenceData = persistenceData,
                            FilterParams = filterList[0] // there's only 1
                        };

                        dataList.Add(data);
                    }
                }
            }
            return dataList;
        }


        private static void WriteFilter(XmlDocument xmlDoc, XmlElement rootElement, List<FilterParams> filterList)
        {
            XmlElement filtersElement = xmlDoc.CreateElement("filters");
            rootElement.AppendChild(filtersElement);
            foreach (FilterParams filterParams in filterList)
            {
                XmlElement filterElement = xmlDoc.CreateElement("filter");
                XmlElement paramsElement = xmlDoc.CreateElement("params");

                MemoryStream stream = new(capacity: 200);
                JsonSerializer.Serialize(stream, filterParams);
                string base64Data = Convert.ToBase64String(stream.ToArray());
                paramsElement.InnerText = base64Data;
                filterElement.AppendChild(paramsElement);
                filtersElement.AppendChild(filterElement);
            }
        }


        private static List<FilterParams> ReadFilter(XmlElement startNode)
        {
            List<FilterParams> filterList = [];
            XmlNode filtersNode = startNode.SelectSingleNode("filters");
            if (filtersNode != null)
            {
                XmlNodeList filterNodeList = filtersNode.ChildNodes; // all "filter" nodes
                foreach (XmlNode node in filterNodeList)
                {
                    foreach (XmlNode subNode in node.ChildNodes)
                    {
                        if (subNode.Name.Equals("params"))
                        {
                            string base64Text = subNode.InnerText;
                            byte[] data = Convert.FromBase64String(base64Text);
                            MemoryStream stream = new(data);

                            try
                            {
                                FilterParams filterParams = JsonSerializer.Deserialize<FilterParams>(stream);
                                filterParams.Init();
                                filterList.Add(filterParams);
                            }
                            catch (JsonException ex)
                            {
                                _logger.Error($"Error while deserializing filter params. Exception Message: {ex.Message}");
                            }
                        }
                    }
                }
            }
            return filterList;
        }


        private static void WriteBookmarks(XmlDocument xmlDoc, XmlElement rootElement,
            SortedList<int, Entities.Bookmark> bookmarkList)
        {
            XmlElement bookmarksElement = xmlDoc.CreateElement("bookmarks");
            rootElement.AppendChild(bookmarksElement);
            foreach (Entities.Bookmark bookmark in bookmarkList.Values)
            {
                XmlElement bookmarkElement = xmlDoc.CreateElement("bookmark");
                bookmarkElement.SetAttribute("line", "" + bookmark.LineNum);
                XmlElement textElement = xmlDoc.CreateElement("text");
                textElement.InnerText = bookmark.Text;
                XmlElement posXElement = xmlDoc.CreateElement("posX");
                XmlElement posYElement = xmlDoc.CreateElement("posY");
                posXElement.InnerText = "" + bookmark.OverlayOffset.Width;
                posYElement.InnerText = "" + bookmark.OverlayOffset.Height;
                bookmarkElement.AppendChild(textElement);
                bookmarkElement.AppendChild(posXElement);
                bookmarkElement.AppendChild(posYElement);
                bookmarksElement.AppendChild(bookmarkElement);
            }
        }


        private static PersistenceData Load(string fileName)
        {
            XmlDocument xmlDoc = new();
            xmlDoc.Load(fileName);
            XmlNode fileNode = xmlDoc.SelectSingleNode("logexpert/file");
            PersistenceData persistenceData = new();
            if (fileNode != null)
            {
                persistenceData = ReadPersistenceDataFromNode(fileNode);
            }
            return persistenceData;
        }

        private static PersistenceData ReadPersistenceDataFromNode(XmlNode node)
        {
            PersistenceData persistenceData = new();
            XmlElement fileElement = node as XmlElement;
            persistenceData.bookmarkList = ReadBookmarks(fileElement);
            persistenceData.rowHeightList = ReadRowHeightList(fileElement);
            ReadOptions(fileElement, persistenceData);
            persistenceData.fileName = fileElement.GetAttribute("fileName");
            string sLineCount = fileElement.GetAttribute("lineCount");
            if (sLineCount != null && sLineCount.Length > 0)
            {
                persistenceData.lineCount = int.Parse(sLineCount);
            }
            persistenceData.filterParamsList = ReadFilter(fileElement);
            persistenceData.filterTabDataList = ReadFilterTabs(fileElement);
            persistenceData.encoding = ReadEncoding(fileElement);
            return persistenceData;
        }


        private static Encoding ReadEncoding(XmlElement fileElement)
        {
            XmlNode encodingNode = fileElement.SelectSingleNode("encoding");
            if (encodingNode != null)
            {
                XmlAttribute encAttr = encodingNode.Attributes["name"];
                try
                {
                    return encAttr == null ? null : Encoding.GetEncoding(encAttr.Value);
                }
                catch (ArgumentException e)
                {
                    _logger.Error(e);
                    return Encoding.Default;
                }
                catch (NotSupportedException e)
                {
                    _logger.Error(e);
                    return Encoding.Default;
                }
            }
            return null;
        }


        private static SortedList<int, Entities.Bookmark> ReadBookmarks(XmlElement startNode)
        {
            SortedList<int, Entities.Bookmark> bookmarkList = [];
            XmlNode boomarksNode = startNode.SelectSingleNode("bookmarks");
            if (boomarksNode != null)
            {
                XmlNodeList bookmarkNodeList = boomarksNode.ChildNodes; // all "bookmark" nodes
                foreach (XmlNode node in bookmarkNodeList)
                {
                    string text = null;
                    string posX = null;
                    string posY = null;
                    string line = null;

                    foreach (XmlAttribute attr in node.Attributes)
                    {
                        if (attr.Name.Equals("line"))
                        {
                            line = attr.InnerText;
                        }
                    }
                    foreach (XmlNode subNode in node.ChildNodes)
                    {
                        if (subNode.Name.Equals("text"))
                        {
                            text = subNode.InnerText;
                        }
                        else if (subNode.Name.Equals("posX"))
                        {
                            posX = subNode.InnerText;
                        }
                        else if (subNode.Name.Equals("posY"))
                        {
                            posY = subNode.InnerText;
                        }
                    }
                    if (line == null || posX == null || posY == null)
                    {
                        _logger.Error("Invalid XML format for bookmark: {0}", node.InnerText);
                        continue;
                    }
                    int lineNum = int.Parse(line);

                    Entities.Bookmark bookmark = new(lineNum)
                    {
                        OverlayOffset = new Size(int.Parse(posX), int.Parse(posY))
                    };

                    if (text != null)
                    {
                        bookmark.Text = text;
                    }
                    bookmarkList.Add(lineNum, bookmark);
                }
            }
            return bookmarkList;
        }

        private static void WriteRowHeightList(XmlDocument xmlDoc, XmlElement rootElement, SortedList<int, RowHeightEntry> rowHeightList)
        {
            XmlElement rowheightElement = xmlDoc.CreateElement("rowheights");
            rootElement.AppendChild(rowheightElement);
            foreach (RowHeightEntry entry in rowHeightList.Values)
            {
                XmlElement entryElement = xmlDoc.CreateElement("rowheight");
                entryElement.SetAttribute("line", "" + entry.LineNum);
                entryElement.SetAttribute("height", "" + entry.Height);
                rowheightElement.AppendChild(entryElement);
            }
        }

        private static SortedList<int, RowHeightEntry> ReadRowHeightList(XmlElement startNode)
        {
            SortedList<int, RowHeightEntry> rowHeightList = [];
            XmlNode rowHeightsNode = startNode.SelectSingleNode("rowheights");
            if (rowHeightsNode != null)
            {
                XmlNodeList rowHeightNodeList = rowHeightsNode.ChildNodes; // all "rowheight" nodes
                foreach (XmlNode node in rowHeightNodeList)
                {
                    string height = null;
                    string line = null;
                    foreach (XmlAttribute attr in node.Attributes)
                    {
                        if (attr.Name.Equals("line"))
                        {
                            line = attr.InnerText;
                        }
                        else if (attr.Name.Equals("height"))
                        {
                            height = attr.InnerText;
                        }
                    }
                    int lineNum = int.Parse(line);
                    int heightValue = int.Parse(height);
                    rowHeightList.Add(lineNum, new RowHeightEntry(lineNum, heightValue));
                }
            }
            return rowHeightList;
        }


        private static void WriteOptions(XmlDocument xmlDoc, XmlElement rootElement, PersistenceData persistenceData)
        {
            XmlElement optionsElement = xmlDoc.CreateElement("options");
            rootElement.AppendChild(optionsElement);

            XmlElement element = xmlDoc.CreateElement("multifile");
            element.SetAttribute("enabled", persistenceData.multiFile ? "1" : "0");
            element.SetAttribute("pattern", persistenceData.multiFilePattern);
            element.SetAttribute("maxDays", "" + persistenceData.multiFileMaxDays);
            foreach (string fileName in persistenceData.multiFileNames)
            {
                XmlElement entryElement = xmlDoc.CreateElement("fileEntry");
                entryElement.SetAttribute("fileName", "" + fileName);
                element.AppendChild(entryElement);
            }
            optionsElement.AppendChild(element);

            element = xmlDoc.CreateElement("currentline");
            element.SetAttribute("line", "" + persistenceData.currentLine);
            optionsElement.AppendChild(element);

            element = xmlDoc.CreateElement("firstDisplayedLine");
            element.SetAttribute("line", "" + persistenceData.firstDisplayedLine);
            optionsElement.AppendChild(element);

            element = xmlDoc.CreateElement("filter");
            element.SetAttribute("visible", persistenceData.filterVisible ? "1" : "0");
            element.SetAttribute("advanced", persistenceData.filterAdvanced ? "1" : "0");
            element.SetAttribute("position", "" + persistenceData.filterPosition);
            optionsElement.AppendChild(element);

            element = xmlDoc.CreateElement("bookmarklist");
            element.SetAttribute("visible", persistenceData.bookmarkListVisible ? "1" : "0");
            element.SetAttribute("position", "" + persistenceData.bookmarkListPosition);
            optionsElement.AppendChild(element);

            element = xmlDoc.CreateElement("followTail");
            element.SetAttribute("enabled", persistenceData.followTail ? "1" : "0");
            optionsElement.AppendChild(element);

            element = xmlDoc.CreateElement("tab");
            element.SetAttribute("name", persistenceData.tabName);
            rootElement.AppendChild(element);

            element = xmlDoc.CreateElement("columnizer");
            element.SetAttribute("name", persistenceData.columnizerName);
            rootElement.AppendChild(element);

            element = xmlDoc.CreateElement("highlightGroup");
            element.SetAttribute("name", persistenceData.highlightGroupName);
            rootElement.AppendChild(element);

            element = xmlDoc.CreateElement("bookmarkCommentColumn");
            element.SetAttribute("visible", persistenceData.showBookmarkCommentColumn ? "1" : "0");
            optionsElement.AppendChild(element);

            element = xmlDoc.CreateElement("filterSaveList");
            element.SetAttribute("visible", persistenceData.filterSaveListVisible ? "1" : "0");
            optionsElement.AppendChild(element);
        }


        private static void ReadOptions(XmlElement startNode, PersistenceData persistenceData)
        {
            XmlNode optionsNode = startNode.SelectSingleNode("options");
            string value = GetOptionsAttribute(optionsNode, "multifile", "enabled");
            persistenceData.multiFile = value != null && value.Equals("1");
            persistenceData.multiFilePattern = GetOptionsAttribute(optionsNode, "multifile", "pattern");
            value = GetOptionsAttribute(optionsNode, "multifile", "maxDays");
            try
            {
                persistenceData.multiFileMaxDays = value != null ? short.Parse(value) : 0;
            }
            catch (Exception)
            {
                persistenceData.multiFileMaxDays = 0;
            }

            XmlNode multiFileNode = optionsNode.SelectSingleNode("multifile");
            if (multiFileNode != null)
            {
                XmlNodeList multiFileNodeList = multiFileNode.ChildNodes; // all "fileEntry" nodes
                foreach (XmlNode node in multiFileNodeList)
                {
                    string fileName = null;
                    foreach (XmlAttribute attr in node.Attributes)
                    {
                        if (attr.Name.Equals("fileName"))
                        {
                            fileName = attr.InnerText;
                        }
                    }
                    persistenceData.multiFileNames.Add(fileName);
                }
            }

            value = GetOptionsAttribute(optionsNode, "currentline", "line");
            if (value != null)
            {
                persistenceData.currentLine = int.Parse(value);
            }
            value = GetOptionsAttribute(optionsNode, "firstDisplayedLine", "line");
            if (value != null)
            {
                persistenceData.firstDisplayedLine = int.Parse(value);
            }

            value = GetOptionsAttribute(optionsNode, "filter", "visible");
            persistenceData.filterVisible = value != null && value.Equals("1");
            value = GetOptionsAttribute(optionsNode, "filter", "advanced");
            persistenceData.filterAdvanced = value != null && value.Equals("1");
            value = GetOptionsAttribute(optionsNode, "filter", "position");
            if (value != null)
            {
                persistenceData.filterPosition = int.Parse(value);
            }

            value = GetOptionsAttribute(optionsNode, "bookmarklist", "visible");
            persistenceData.bookmarkListVisible = value != null && value.Equals("1");
            value = GetOptionsAttribute(optionsNode, "bookmarklist", "position");
            if (value != null)
            {
                persistenceData.bookmarkListPosition = int.Parse(value);
            }

            value = GetOptionsAttribute(optionsNode, "followTail", "enabled");
            persistenceData.followTail = value != null && value.Equals("1");

            value = GetOptionsAttribute(optionsNode, "bookmarkCommentColumn", "visible");
            persistenceData.showBookmarkCommentColumn = value != null && value.Equals("1");

            value = GetOptionsAttribute(optionsNode, "filterSaveList", "visible");
            persistenceData.filterSaveListVisible = value != null && value.Equals("1");

            XmlNode tabNode = startNode.SelectSingleNode("tab");
            if (tabNode != null)
            {
                persistenceData.tabName = (tabNode as XmlElement).GetAttribute("name");
            }
            XmlNode columnizerNode = startNode.SelectSingleNode("columnizer");
            if (columnizerNode != null)
            {
                persistenceData.columnizerName = (columnizerNode as XmlElement).GetAttribute("name");
            }
            XmlNode highlightGroupNode = startNode.SelectSingleNode("highlightGroup");
            if (highlightGroupNode != null)
            {
                persistenceData.highlightGroupName = (highlightGroupNode as XmlElement).GetAttribute("name");
            }
        }


        private static string GetOptionsAttribute(XmlNode optionsNode, string elementName, string attrName)
        {
            XmlNode node = optionsNode.SelectSingleNode(elementName);
            if (node == null)
            {
                return null;
            }
            if (node is XmlElement)
            {
                string value = (node as XmlElement).GetAttribute(attrName);
                return value;
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}