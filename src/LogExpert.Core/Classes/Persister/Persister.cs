using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Xml;

using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;

using NLog;

namespace LogExpert.Core.Classes.Persister;

//TODO Rewrite as json Persister, xml is outdated and difficult to parse and write
public static class Persister
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Public methods

    public static string SavePersistenceData (string logFileName, PersistenceData persistenceData, Preferences preferences)
    {
        var fileName = persistenceData.SessionFileName ?? BuildPersisterFileName(logFileName, preferences);

        if (preferences.SaveLocation == SessionSaveLocation.SameDir)
        {
            // make to log file in .lxp file relative
            var filePart = Path.GetFileName(persistenceData.FileName);
            persistenceData.FileName = filePart;
        }

        Save(fileName, persistenceData);
        return fileName;
    }

    public static string SavePersistenceDataWithFixedName (string persistenceFileName,
        PersistenceData persistenceData)
    {
        Save(persistenceFileName, persistenceData);
        return persistenceFileName;
    }


    public static PersistenceData LoadPersistenceData (string logFileName, Preferences preferences)
    {
        var fileName = BuildPersisterFileName(logFileName, preferences);
        return Load(fileName);
    }

    public static PersistenceData LoadPersistenceDataOptionsOnly (string logFileName, Preferences preferences)
    {
        var fileName = BuildPersisterFileName(logFileName, preferences);
        return LoadOptionsOnly(fileName);
    }

    public static PersistenceData LoadPersistenceDataOptionsOnlyFromFixedFile (string persistenceFile)
    {
        return LoadOptionsOnly(persistenceFile);
    }

    public static PersistenceData LoadPersistenceDataFromFixedFile (string persistenceFile)
    {
        return Load(persistenceFile);
    }


    /// <summary>
    /// Loads the persistence options out of the given persistence file name.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static PersistenceData LoadOptionsOnly (string fileName)
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
            var fileElement = fileNode as XmlElement;
            ReadOptions(fileElement, persistenceData);
            persistenceData.FileName = fileElement.GetAttribute("fileName");
            persistenceData.Encoding = ReadEncoding(fileElement);
        }
        return persistenceData;
    }

    #endregion

    #region Private Methods

    private static string BuildPersisterFileName (string logFileName, Preferences preferences)
    {
        string dir;
        string file;

        switch (preferences.SaveLocation)
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
                    dir = preferences.SessionSaveDirectory;
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

    private static string BuildSessionFileNameFromPath (string logFileName)
    {
        var result = logFileName;
        result = result.Replace(Path.DirectorySeparatorChar, '_');
        result = result.Replace(Path.AltDirectorySeparatorChar, '_');
        result = result.Replace(Path.VolumeSeparatorChar, '_');
        result += ".lxp";
        return result;
    }

    private static void Save (string fileName, PersistenceData persistenceData)
    {
        XmlDocument xmlDoc = new();
        XmlElement rootElement = xmlDoc.CreateElement("logexpert");
        xmlDoc.AppendChild(rootElement);
        XmlElement fileElement = xmlDoc.CreateElement("file");
        rootElement.AppendChild(fileElement);
        fileElement.SetAttribute("fileName", persistenceData.FileName);
        fileElement.SetAttribute("lineCount", "" + persistenceData.LineCount);
        WriteBookmarks(xmlDoc, fileElement, persistenceData.BookmarkList);
        WriteRowHeightList(xmlDoc, fileElement, persistenceData.RowHeightList);
        WriteOptions(xmlDoc, fileElement, persistenceData);
        WriteFilter(xmlDoc, fileElement, persistenceData.FilterParamsList);
        WriteFilterTabs(xmlDoc, fileElement, persistenceData.FilterTabDataList);
        WriteEncoding(xmlDoc, fileElement, persistenceData.Encoding);
        if (xmlDoc.HasChildNodes)
        {
            xmlDoc.Save(fileName);
        }
    }

    private static void WriteEncoding (XmlDocument xmlDoc, XmlElement rootElement, Encoding encoding)
    {
        if (encoding != null)
        {
            XmlElement encodingElement = xmlDoc.CreateElement("encoding");
            rootElement.AppendChild(encodingElement);
            encodingElement.SetAttribute("name", encoding.WebName);
        }
    }

    private static void WriteFilterTabs (XmlDocument xmlDoc, XmlElement rootElement, List<FilterTabData> dataList)
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
                WriteBookmarks(xmlDoc, filterTabElement, persistenceData.BookmarkList);
                WriteRowHeightList(xmlDoc, filterTabElement, persistenceData.RowHeightList);
                WriteOptions(xmlDoc, filterTabElement, persistenceData);
                WriteFilter(xmlDoc, filterTabElement, persistenceData.FilterParamsList);
                WriteFilterTabs(xmlDoc, filterTabElement, persistenceData.FilterTabDataList);
                XmlElement filterElement = xmlDoc.CreateElement("tabFilter");
                filterTabElement.AppendChild(filterElement);
                List<FilterParams> filterList = [data.FilterParams];
                WriteFilter(xmlDoc, filterElement, filterList);
            }
        }
    }

    private static List<FilterTabData> ReadFilterTabs (XmlElement startNode)
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


    private static void WriteFilter (XmlDocument xmlDoc, XmlElement rootElement, List<FilterParams> filterList)
    {
        XmlElement filtersElement = xmlDoc.CreateElement("filters");
        rootElement.AppendChild(filtersElement);
        foreach (FilterParams filterParams in filterList)
        {
            XmlElement filterElement = xmlDoc.CreateElement("filter");
            XmlElement paramsElement = xmlDoc.CreateElement("params");

            MemoryStream stream = new(capacity: 200);
            JsonSerializer.Serialize(stream, filterParams);
            var base64Data = Convert.ToBase64String(stream.ToArray());
            paramsElement.InnerText = base64Data;
            filterElement.AppendChild(paramsElement);
            filtersElement.AppendChild(filterElement);
        }
    }


    private static List<FilterParams> ReadFilter (XmlElement startNode)
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
                    if (subNode.Name.Equals("params", StringComparison.OrdinalIgnoreCase))
                    {
                        var base64Text = subNode.InnerText;
                        var data = Convert.FromBase64String(base64Text);
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


    private static void WriteBookmarks (XmlDocument xmlDoc, XmlElement rootElement,
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


    private static PersistenceData Load (string fileName)
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

    private static PersistenceData ReadPersistenceDataFromNode (XmlNode node)
    {
        PersistenceData persistenceData = new();
        var fileElement = node as XmlElement;
        persistenceData.BookmarkList = ReadBookmarks(fileElement);
        persistenceData.RowHeightList = ReadRowHeightList(fileElement);
        ReadOptions(fileElement, persistenceData);
        persistenceData.FileName = fileElement.GetAttribute("fileName");
        var sLineCount = fileElement.GetAttribute("lineCount");
        if (sLineCount != null && sLineCount.Length > 0)
        {
            persistenceData.LineCount = int.Parse(sLineCount);
        }
        persistenceData.FilterParamsList = ReadFilter(fileElement);
        persistenceData.FilterTabDataList = ReadFilterTabs(fileElement);
        persistenceData.Encoding = ReadEncoding(fileElement);
        return persistenceData;
    }


    private static Encoding ReadEncoding (XmlElement fileElement)
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


    private static SortedList<int, Entities.Bookmark> ReadBookmarks (XmlElement startNode)
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
                    if (attr.Name.Equals("line", StringComparison.OrdinalIgnoreCase))
                    {
                        line = attr.InnerText;
                    }
                }
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    if (subNode.Name.Equals("text", StringComparison.OrdinalIgnoreCase))
                    {
                        text = subNode.InnerText;
                    }
                    else if (subNode.Name.Equals("posX", StringComparison.OrdinalIgnoreCase))
                    {
                        posX = subNode.InnerText;
                    }
                    else if (subNode.Name.Equals("posY", StringComparison.OrdinalIgnoreCase))
                    {
                        posY = subNode.InnerText;
                    }
                }
                if (line == null || posX == null || posY == null)
                {
                    _logger.Error($"Invalid XML format for bookmark: {node.InnerText}");
                    continue;
                }
                var lineNum = int.Parse(line);

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

    private static void WriteRowHeightList (XmlDocument xmlDoc, XmlElement rootElement, SortedList<int, RowHeightEntry> rowHeightList)
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

    private static SortedList<int, RowHeightEntry> ReadRowHeightList (XmlElement startNode)
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
                    if (attr.Name.Equals("line", StringComparison.OrdinalIgnoreCase))
                    {
                        line = attr.InnerText;
                    }
                    else if (attr.Name.Equals("height", StringComparison.OrdinalIgnoreCase))
                    {
                        height = attr.InnerText;
                    }
                }
                var lineNum = int.Parse(line);
                var heightValue = int.Parse(height);
                rowHeightList.Add(lineNum, new RowHeightEntry(lineNum, heightValue));
            }
        }
        return rowHeightList;
    }


    private static void WriteOptions (XmlDocument xmlDoc, XmlElement rootElement, PersistenceData persistenceData)
    {
        XmlElement optionsElement = xmlDoc.CreateElement("options");
        rootElement.AppendChild(optionsElement);

        XmlElement element = xmlDoc.CreateElement("multifile");
        element.SetAttribute("enabled", persistenceData.MultiFile ? "1" : "0");
        element.SetAttribute("pattern", persistenceData.MultiFilePattern);
        element.SetAttribute("maxDays", "" + persistenceData.MultiFileMaxDays);
        foreach (var fileName in persistenceData.MultiFileNames)
        {
            XmlElement entryElement = xmlDoc.CreateElement("fileEntry");
            entryElement.SetAttribute("fileName", "" + fileName);
            element.AppendChild(entryElement);
        }
        optionsElement.AppendChild(element);

        element = xmlDoc.CreateElement("currentline");
        element.SetAttribute("line", "" + persistenceData.CurrentLine);
        optionsElement.AppendChild(element);

        element = xmlDoc.CreateElement("firstDisplayedLine");
        element.SetAttribute("line", "" + persistenceData.FirstDisplayedLine);
        optionsElement.AppendChild(element);

        element = xmlDoc.CreateElement("filter");
        element.SetAttribute("visible", persistenceData.FilterVisible ? "1" : "0");
        element.SetAttribute("advanced", persistenceData.FilterAdvanced ? "1" : "0");
        element.SetAttribute("position", "" + persistenceData.FilterPosition);
        optionsElement.AppendChild(element);

        element = xmlDoc.CreateElement("bookmarklist");
        element.SetAttribute("visible", persistenceData.BookmarkListVisible ? "1" : "0");
        element.SetAttribute("position", "" + persistenceData.BookmarkListPosition);
        optionsElement.AppendChild(element);

        element = xmlDoc.CreateElement("followTail");
        element.SetAttribute("enabled", persistenceData.FollowTail ? "1" : "0");
        optionsElement.AppendChild(element);

        element = xmlDoc.CreateElement("tab");
        element.SetAttribute("name", persistenceData.TabName);
        rootElement.AppendChild(element);

        element = xmlDoc.CreateElement("columnizer");
        element.SetAttribute("name", persistenceData.ColumnizerName);
        rootElement.AppendChild(element);

        element = xmlDoc.CreateElement("highlightGroup");
        element.SetAttribute("name", persistenceData.HighlightGroupName);
        rootElement.AppendChild(element);

        element = xmlDoc.CreateElement("bookmarkCommentColumn");
        element.SetAttribute("visible", persistenceData.ShowBookmarkCommentColumn ? "1" : "0");
        optionsElement.AppendChild(element);

        element = xmlDoc.CreateElement("filterSaveList");
        element.SetAttribute("visible", persistenceData.FilterSaveListVisible ? "1" : "0");
        optionsElement.AppendChild(element);
    }


    private static void ReadOptions (XmlElement startNode, PersistenceData persistenceData)
    {
        XmlNode optionsNode = startNode.SelectSingleNode("options");
        var value = GetOptionsAttribute(optionsNode, "multifile", "enabled");
        persistenceData.MultiFile = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);
        persistenceData.MultiFilePattern = GetOptionsAttribute(optionsNode, "multifile", "pattern");
        value = GetOptionsAttribute(optionsNode, "multifile", "maxDays");
        try
        {
            persistenceData.MultiFileMaxDays = value != null ? short.Parse(value) : 0;
        }
        catch (Exception)
        {
            persistenceData.MultiFileMaxDays = 0;
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
                    if (attr.Name.Equals("fileName", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName = attr.InnerText;
                    }
                }
                persistenceData.MultiFileNames.Add(fileName);
            }
        }

        value = GetOptionsAttribute(optionsNode, "currentline", "line");
        if (value != null)
        {
            persistenceData.CurrentLine = int.Parse(value);
        }
        value = GetOptionsAttribute(optionsNode, "firstDisplayedLine", "line");
        if (value != null)
        {
            persistenceData.FirstDisplayedLine = int.Parse(value);
        }

        value = GetOptionsAttribute(optionsNode, "filter", "visible");
        persistenceData.FilterVisible = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);
        value = GetOptionsAttribute(optionsNode, "filter", "advanced");
        persistenceData.FilterAdvanced = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);
        value = GetOptionsAttribute(optionsNode, "filter", "position");
        if (value != null)
        {
            persistenceData.FilterPosition = int.Parse(value);
        }

        value = GetOptionsAttribute(optionsNode, "bookmarklist", "visible");
        persistenceData.BookmarkListVisible = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);
        value = GetOptionsAttribute(optionsNode, "bookmarklist", "position");
        if (value != null)
        {
            persistenceData.BookmarkListPosition = int.Parse(value);
        }

        value = GetOptionsAttribute(optionsNode, "followTail", "enabled");
        persistenceData.FollowTail = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);

        value = GetOptionsAttribute(optionsNode, "bookmarkCommentColumn", "visible");
        persistenceData.ShowBookmarkCommentColumn = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);

        value = GetOptionsAttribute(optionsNode, "filterSaveList", "visible");
        persistenceData.FilterSaveListVisible = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);

        XmlNode tabNode = startNode.SelectSingleNode("tab");
        if (tabNode != null)
        {
            persistenceData.TabName = (tabNode as XmlElement).GetAttribute("name");
        }
        XmlNode columnizerNode = startNode.SelectSingleNode("columnizer");
        if (columnizerNode != null)
        {
            persistenceData.ColumnizerName = (columnizerNode as XmlElement).GetAttribute("name");
        }
        XmlNode highlightGroupNode = startNode.SelectSingleNode("highlightGroup");
        if (highlightGroupNode != null)
        {
            persistenceData.HighlightGroupName = (highlightGroupNode as XmlElement).GetAttribute("name");
        }
    }


    private static string GetOptionsAttribute (XmlNode optionsNode, string elementName, string attrName)
    {
        XmlNode node = optionsNode.SelectSingleNode(elementName);
        if (node == null)
        {
            return null;
        }
        if (node is XmlElement)
        {
            var value = (node as XmlElement).GetAttribute(attrName);
            return value;
        }
        else
        {
            return null;
        }
    }

    #endregion
}