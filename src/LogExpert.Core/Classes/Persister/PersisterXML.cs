using System.Drawing;
using System.Globalization;
using System.Text;
using System.Xml;

using LogExpert.Core.Classes.Filter;
using LogExpert.Core.Entities;
using LogExpert.Core.Helpers;

using Newtonsoft.Json;

using NLog;

namespace LogExpert.Core.Classes.Persister;

/// <summary>
/// Persister for XML format persistence data.
/// </summary>
[Obsolete("XML persistence is deprecated and will be removed in future versions. This is a fallback for older Versions")]
public static class PersisterXML
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Reads all filter tab definitions from the given <paramref name="startNode"/>.
    /// </summary>
    /// <param name="startNode">
    /// The root <c>file</c> XmlElement which may contain a direct child element named <c>filterTabs</c>.
    /// Must not be <c>null</c>.
    /// </param>
    /// <returns>
    /// A list of <see cref="FilterTabData"/> instances. Returns an empty list if no <c>filterTabs</c> element exists.
    /// </returns>
    /// <remarks>
    /// Expected XML structure:
    /// <![CDATA[
    /// <filterTabs>
    ///   <filterTab fileName="..." lineCount="...">
    ///     ... (persistence related child nodes)
    ///     <tabFilter>
    ///       <filters>
    ///         <filter>
    ///           <params>BASE64(JSON FilterParams)</params>
    ///         </filter>
    ///       </filters>
    ///     </tabFilter>
    ///   </filterTab>
    /// </filterTabs>
    /// ]]>
    /// Processing steps:
    /// - Locates the <c>filterTabs</c> node under <paramref name="startNode"/>.
    /// - Iterates each child node (expected: <c>filterTab</c>).
    /// - For each node:
    ///   - Calls <see cref="ReadPersistenceDataFromNode(XmlNode)"/> to hydrate a nested <see cref="PersistenceData"/>.
    ///   - Locates <c>tabFilter</c> and deserializes its first (and historically only) <see cref="FilterParams"/> entry.
    ///   - Wraps both into a <see cref="FilterTabData"/> and adds it to the result list.
    /// - If JSON deserialization of filter parameters fails, the error is logged and the specific tab is skipped.
    /// Notes:
    /// - Only the first entry of the deserialized filter list is used because the persisted format supports
    ///   exactly one filter per tab.
    /// - Returns an empty list if the <c>filterTabs</c> node is absent.
    /// </remarks>
    private static List<FilterTabData> ReadFilterTabs (XmlElement startNode)
    {
        List<FilterTabData> dataList = [];
        XmlNode filterTabsNode = startNode.SelectSingleNode("filterTabs");
        if (filterTabsNode != null)
        {
            XmlNodeList filterTabNodeList = filterTabsNode.ChildNodes;

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
                        FilterParams = filterList[0]
                    };

                    dataList.Add(data);
                }
            }
        }

        return dataList;
    }

    /// <summary>
    /// Reads and deserializes all <see cref="FilterParams"/> entries from a given XML element which contains
    /// a child element named <c>filters</c>.
    /// </summary>
    /// <param name="startNode">
    /// The XML element expected to have a child element <c>filters</c>. This method is used both for
    /// global filter lists (root <c>file</c> element) and per-tab filters (<c>tabFilter</c> element).
    /// Structure example:
    /// <![CDATA[
    /// <tabFilter>
    ///   <filters>
    ///     <filter>
    ///       <params>BASE64(JSON FilterParams)</params>
    ///     </filter>
    ///     <filter>
    ///       <params>BASE64(JSON FilterParams)</params>
    ///     </filter>
    ///   </filters>
    /// </tabFilter>
    /// ]]>
    /// </param>
    /// <returns>
    /// A list of deserialized <see cref="FilterParams"/> instances. Returns an empty list if the
    /// <c>filters</c> element is missing or no valid filter entries are found.
    /// </returns>
    /// <remarks>
    /// Processing steps:
    /// 1. Locates the <c>filters</c> child node.
    /// 2. Iterates each <c>filter</c> node.
    /// 3. For each <c>params</c> child:
    ///    - Decodes its Base64 inner text to bytes.
    ///    - Deserializes JSON to <see cref="FilterParams"/>.
    ///    - Calls <see cref="FilterParams.Init"/> to finalize state.
    /// 4. Adds successfully deserialized instances to the result list.
    /// Errors:
    /// - <see cref="JsonException"/> during deserialization is logged (entry skipped).
    /// - Possible <see cref="FormatException"/> from invalid Base64 is not caught here.
    /// </remarks>
    private static List<FilterParams> ReadFilter (XmlElement startNode)
    {
        List<FilterParams> filterList = [];

        if (startNode == null)
        {
            return filterList;
        }

        XmlNode filtersNode = startNode.SelectSingleNode("filters");
        if (filtersNode != null)
        {
            XmlNodeList filterNodeList = filtersNode.ChildNodes;
            foreach (XmlNode node in filterNodeList)
            {
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    if (subNode.Name.Equals("params", StringComparison.OrdinalIgnoreCase))
                    {
                        var base64Text = subNode.InnerText;
                        var data = Convert.FromBase64String(base64Text);
                        using MemoryStream stream = new(data);
                        try
                        {
                            using StreamReader streamReader = new(stream);
                            using JsonTextReader jsonReader = new(streamReader);
                            JsonSerializer serializer = new();
                            var filterParams = serializer.Deserialize<FilterParams>(jsonReader);

                            if (filterParams == null)
                            {
                                continue;
                            }

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

    /// <summary>
    /// Loads persistence data from an XML file (internal implementation without exception filtering).
    /// </summary>
    /// <param name="fileName">Full path to the XML persistence file to read.</param>
    /// <returns>
    /// A populated <see cref="PersistenceData"/> instance. If the expected root node
    /// (<c>logexpert/file</c>) is missing an empty instance with default values is returned.
    /// </returns>
    /// <remarks>
    /// This method:<br></br>
    /// 1. Loads the XML document.<br></br>
    /// 2. Selects the node <c>logexpert/file</c>.<br></br>
    /// 3. Delegates hydration to <see cref="ReadPersistenceDataFromNode(XmlNode)"/>.<br></br>
    /// </remarks>
    private static PersistenceData LoadInternal (string fileName)
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

    /// <summary>
    /// Reads persistence-related information (bookmarks, row heights, filters, encoding, options, etc.)
    /// from a given <see cref="XmlNode"/> assumed to represent a <c>file</c> element.
    /// </summary>
    /// <param name="node">
    /// The XML node (ideally an <see cref="XmlElement"/>) containing child elements for bookmarks,
    /// options, filters, filter tabs, and encoding. Must not be <c>null</c>; if the cast to
    /// <see cref="XmlElement"/> fails an empty <see cref="PersistenceData"/> with default values is returned.
    /// </param>
    /// <returns>
    /// A fully populated <see cref="PersistenceData"/> instance. Collections are initialized to empty lists
    /// when corresponding XML sections are absent.
    /// </returns>
    /// <remarks>
    /// Processing order:<br></br>
    /// 1. Cast node to <see cref="XmlElement"/>.<br></br>
    /// 2. Bookmarks via <see cref="ReadBookmarks(XmlElement)"/>.<br></br>
    /// 3. Row heights via <see cref="ReadRowHeightList(XmlElement)"/>.<br></br>
    /// 4. Options via <see cref="ReadOptions(XmlElement, PersistenceData)"/>.<br></br>
    /// 5. File attributes: <c>fileName</c>, <c>lineCount</c>.<br></br>
    /// 6. Filters via <see cref="ReadFilter(XmlElement)"/>.<br></br>
    /// 7. Filter tabs via <see cref="ReadFilterTabs(XmlElement)"/>.<br></br>
    /// 8. Encoding via <see cref="ReadEncoding(XmlElement)"/>.<br></br>
    /// Invalid integers for <c>lineCount</c> will throw <see cref="FormatException"/>.
    /// </remarks>
    private static PersistenceData ReadPersistenceDataFromNode (XmlNode node)
    {
        PersistenceData persistenceData = new();
        if (node is not XmlElement fileElement)
        {
            // Documented above: a node that is not an element yields defaults. ReadFilterTabs walks every
            // child of filterTabs, so a comment or text node in a hand-edited .lxp arrives here.
            return persistenceData;
        }

        persistenceData.BookmarkList = ReadBookmarks(fileElement);
        persistenceData.RowHeightList = ReadRowHeightList(fileElement);
        ReadOptions(fileElement, persistenceData);
        persistenceData.FileName = fileElement.GetAttribute("fileName");
        var sLineCount = fileElement.GetAttribute("lineCount");
        if (sLineCount != null && sLineCount.Length > 0)
        {
            persistenceData.LineCount = int.Parse(sLineCount, CultureInfo.InvariantCulture);
        }

        persistenceData.FilterParamsList = ReadFilter(fileElement);
        persistenceData.FilterTabDataList = ReadFilterTabs(fileElement);
        persistenceData.Encoding = ReadEncoding(fileElement);
        return persistenceData;
    }

    /// <summary>
    /// Attempts to resolve the file text <see cref="Encoding"/> from the given XML element.
    /// </summary>
    /// <param name="fileElement">
    /// The root <c>file</c> element which may contain an <c>encoding</c> child element:
    /// <![CDATA[
    /// <encoding name="utf-8" />
    /// ]]>
    /// The element must not be null (no internal null check performed).
    /// </param>
    /// <returns>
    /// The resolved <see cref="Encoding"/> when the <c>encoding</c> element exists and its <c>name</c> attribute
    /// maps to a supported encoding; <c>null</c> if the <c>encoding</c> element is absent or the attribute is missing.
    /// If the specified name is invalid or not supported an error is logged and <see cref="Encoding.Default"/> is returned.
    /// </returns>
    /// <remarks>
    /// Processing rules:
    /// - Looks for a direct child element named <c>encoding</c>.
    /// - Reads its <c>name</c> attribute and calls <see cref="Encoding.GetEncoding(string)"/>.
    /// - Catches <see cref="ArgumentException"/> and <see cref="NotSupportedException"/>; logs and falls back to <see cref="Encoding.Default"/>.
    /// - Does not throw for missing node/attribute; returns <c>null</c> in that case.
    /// </remarks>
    private static Encoding ReadEncoding (XmlElement fileElement)
    {
        XmlNode encodingNode = fileElement.SelectSingleNode("encoding");
        if (encodingNode == null)
        {
            return null;
        }

        XmlAttribute encAttr = encodingNode.Attributes["name"];
        if (encAttr == null)
        {
            return null;
        }

        if (EncodingRegistry.TryGetEncoding(encAttr.Value, out var encoding))
        {
            return encoding;
        }

        _logger.Error($"Persisted encoding '{encAttr.Value}' is not supported, falling back to the default encoding");
        return Encoding.Default;
    }

    /// <summary>
    /// Reads bookmark entries from the given XML element and returns them as a sorted list keyed by line number.
    /// </summary>
    /// <remarks>
    /// Expected XML structure:
    /// <code>
    /// <bookmarks>
    ///   <bookmark line="42">
    ///     <text>User set bookmark</text>
    ///     <posX>10</posX>
    ///     <posY>25</posY>
    ///   </bookmark>
    ///   <bookmark line="43">
    ///     <posX>4</posX>
    ///     <posY>12</posY>
    ///   </bookmark>
    /// </bookmarks>
    /// </code>
    /// Processing details:
    /// - Each <c>bookmark</c> element must have a <c>line</c> attribute that parses to an integer.
    /// - Optional child element <c>text</c> provides the bookmark text/comment.
    /// - Required child elements <c>posX</c> and <c>posY</c> define the overlay offset (parsed as integers).
    /// - Invalid bookmark nodes (missing required data) are skipped and an error is logged.
    /// - Bookmarks are stored in a <see cref="SortedList{TKey,TValue}"/> keyed by their line number.
    /// </remarks>
    /// <param name="startNode">The XML element that contains (or has as descendant) the <c>bookmarks</c> element.</param>
    /// <returns>
    /// A sorted list of <see cref="Entities.Bookmark"/> instances keyed by line number. Returns an empty list if no
    /// <c>bookmarks</c> element exists.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown if a numeric value (line / posX / posY) cannot be parsed to an integer. This will abort processing of the current bookmark.
    /// </exception>
    private static SortedList<int, Entities.Bookmark> ReadBookmarks (XmlElement startNode)
    {
        SortedList<int, Entities.Bookmark> bookmarkList = [];
        XmlNode bookmarksNode = startNode.SelectSingleNode("bookmarks");
        if (bookmarksNode != null)
        {
            XmlNodeList bookmarkNodeList = bookmarksNode.ChildNodes;
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

                var lineNum = int.Parse(line, CultureInfo.InvariantCulture);

                Entities.Bookmark bookmark = new(lineNum)
                {
                    OverlayOffset = new Size(int.Parse(posX, CultureInfo.InvariantCulture), int.Parse(posY, CultureInfo.InvariantCulture))
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

    /// <summary>
    /// Reads row height entries from the given <see cref="XmlElement"/> and returns them
    /// as a sorted list keyed by line number.
    /// </summary>
    /// <remarks>
    /// Expected XML structure:
    /// <code>
    /// <rowheights>
    ///   <rowheight line="123" height="45" />
    ///   <rowheight line="124" height="30" />
    /// </rowheights>
    /// </code>
    /// Each <c>rowheight</c> element must contain a <c>line</c> attribute (the line number)
    /// and a <c>height</c> attribute (the row height value).
    /// Missing or invalid attributes will throw a <see cref="FormatException"/> during parsing.
    /// </remarks>
    /// <param name="startNode">The XML element to search within (usually the file element).</param>
    /// <returns>
    /// A <see cref="SortedList{TKey,TValue}"/> mapping line numbers to <see cref="RowHeightEntry"/> instances.
    /// Returns an empty list if no <c>rowheights</c> node is present.
    /// </returns>
    private static SortedList<int, RowHeightEntry> ReadRowHeightList (XmlElement startNode)
    {
        SortedList<int, RowHeightEntry> rowHeightList = [];
        XmlNode rowHeightsNode = startNode.SelectSingleNode("rowheights");
        if (rowHeightsNode != null)
        {
            XmlNodeList rowHeightNodeList = rowHeightsNode.ChildNodes;
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

                var lineNum = int.Parse(line, CultureInfo.InvariantCulture);
                var heightValue = int.Parse(height, CultureInfo.InvariantCulture);
                rowHeightList.Add(lineNum, new RowHeightEntry(lineNum, heightValue));
            }
        }

        return rowHeightList;
    }

    /// <summary>
    /// Reads the <c>options</c> section of a <c>file</c> element, plus the <c>tab</c>, <c>columnizer</c> and
    /// <c>highlightGroup</c> elements that sit beside it, into the provided <see cref="PersistenceData"/>.
    /// </summary>
    /// <remarks>
    /// The <c>options</c> section is delegated to <see cref="ReadOptionsNode"/> and is optional: a hand-edited
    /// or very old <c>.lxp</c> may omit it, in which case every setting it carries keeps its
    /// <see cref="PersistenceData"/> default. The three sibling elements are read either way, so a missing
    /// <c>options</c> section does not cost the user their tab name, columnizer or highlight group.
    /// </remarks>
    /// <param name="startNode">The <c>file</c> element to read from.</param>
    /// <param name="persistenceData">The <see cref="PersistenceData"/> object to populate with the settings extracted from the XML element.</param>
    private static void ReadOptions (XmlElement startNode, PersistenceData persistenceData)
    {
        XmlNode optionsNode = startNode.SelectSingleNode("options");
        if (optionsNode != null)
        {
            ReadOptionsNode(optionsNode, persistenceData);
        }

        // tab, columnizer and highlightGroup are siblings of options, not children, so they are read
        // even when a hand-edited or very old .lxp carries no options section at all.
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

    /// <summary>
    /// Reads the settings held under the <c>options</c> element into the given <see cref="PersistenceData"/>.
    /// </summary>
    /// <remarks>
    /// Boolean flags are written unconditionally, so an absent element or attribute reads as <c>false</c>.
    /// The remaining settings keep the value they already have. Not calling this at all — which is what
    /// <see cref="ReadOptions"/> does for a <c>file</c> element without an <c>options</c> child — therefore
    /// leaves the whole group at its <see cref="PersistenceData"/> default, <c>FollowTail</c> included.
    /// </remarks>
    /// <param name="optionsNode">The <c>options</c> element. Must not be <c>null</c>.</param>
    /// <param name="persistenceData">The <see cref="PersistenceData"/> object to populate.</param>
    private static void ReadOptionsNode (XmlNode optionsNode, PersistenceData persistenceData)
    {
        var value = GetOptionsAttribute(optionsNode, "multifile", "enabled");
        persistenceData.MultiFile = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);
        persistenceData.MultiFilePattern = GetOptionsAttribute(optionsNode, "multifile", "pattern");
        value = GetOptionsAttribute(optionsNode, "multifile", "maxDays");
        try
        {
            persistenceData.MultiFileMaxDays = value != null ? short.Parse(value, CultureInfo.InvariantCulture) : 0;
        }
        catch (Exception ex) when (ex is ArgumentNullException or
                                        FormatException or
                                        OverflowException)
        {
            persistenceData.MultiFileMaxDays = 0;
        }

        XmlNode multiFileNode = optionsNode.SelectSingleNode("multifile");
        if (multiFileNode != null)
        {
            XmlNodeList multiFileNodeList = multiFileNode.ChildNodes;
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
            persistenceData.CurrentLine = int.Parse(value, CultureInfo.InvariantCulture);
        }

        value = GetOptionsAttribute(optionsNode, "firstDisplayedLine", "line");
        if (value != null)
        {
            persistenceData.FirstDisplayedLine = int.Parse(value, CultureInfo.InvariantCulture);
        }

        value = GetOptionsAttribute(optionsNode, "filter", "visible");
        persistenceData.FilterVisible = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);
        value = GetOptionsAttribute(optionsNode, "filter", "advanced");
        persistenceData.FilterAdvanced = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);
        value = GetOptionsAttribute(optionsNode, "filter", "position");
        if (value != null)
        {
            persistenceData.FilterPosition = int.Parse(value, CultureInfo.InvariantCulture);
        }

        value = GetOptionsAttribute(optionsNode, "bookmarklist", "visible");
        persistenceData.BookmarkListVisible = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);
        value = GetOptionsAttribute(optionsNode, "bookmarklist", "position");
        if (value != null)
        {
            persistenceData.BookmarkListPosition = int.Parse(value, CultureInfo.InvariantCulture);
        }

        value = GetOptionsAttribute(optionsNode, "followTail", "enabled");
        persistenceData.FollowTail = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);

        value = GetOptionsAttribute(optionsNode, "bookmarkCommentColumn", "visible");
        persistenceData.ShowBookmarkCommentColumn = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);

        value = GetOptionsAttribute(optionsNode, "filterSaveList", "visible");
        persistenceData.FilterSaveListVisible = value != null && value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Retrieves the value of a specified attribute from a child element within the given <paramref name="optionsNode"/>.
    /// </summary>
    /// <param name="optionsNode">
    /// The parent XML node expected to contain the child element identified by <paramref name="elementName"/>.
    /// Must not be <c>null</c>; otherwise a <see cref="NullReferenceException"/> will occur before this method is called.
    /// </param>
    /// <param name="elementName">
    /// The name of the child element to search for (e.g. "multifile", "filter", "bookmarklist").
    /// </param>
    /// <param name="attrName">
    /// The name of the attribute whose value should be returned (e.g. "enabled", "pattern", "visible").
    /// </param>
    /// <returns>
    /// The attribute value as a string if the child element exists and is an <see cref="XmlElement"/> and the attribute is present;
    /// otherwise <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method performs a direct XPath child lookup using <see cref="XmlNode.SelectSingleNode(string)"/>.
    /// It does not perform any conversion of the returned value. Callers are responsible for parsing or validating the result.
    /// </remarks>
    private static string GetOptionsAttribute (XmlNode optionsNode, string elementName, string attrName)
    {
        XmlNode node = optionsNode.SelectSingleNode(elementName);
        if (node == null)
        {
            return null;
        }

        if (node is XmlElement element)
        {
            var valueAttr = element.GetAttribute(attrName);
            return valueAttr;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Loads persistence data from the specified XML file.
    /// </summary>
    /// <param name="fileName">Full path to the persistence XML file.</param>
    /// <returns>
    /// A populated <see cref="PersistenceData"/> instance if loading succeeds; otherwise <c>null</c>
    /// when the file cannot be read or parsed (XML/IO/security related issues are logged).
    /// </returns>
    /// <remarks>
    /// Only XML format is attempted. Unreadable files (<see cref="UnauthorizedAccessException"/>,
    /// <see cref="IOException"/>, <see cref="FileNotFoundException"/>) and unparseable content are caught
    /// and logged; in these cases <c>null</c> is returned.
    /// <para>
    /// "Unparseable" covers more than malformed markup: this is a read-only fallback for a format LogExpert
    /// no longer writes, so the readers below assume a well-formed document and let a hand-edited or truncated
    /// one throw <see cref="FormatException"/> (unparseable numbers, invalid Base64),
    /// <see cref="OverflowException"/> (numbers too large for their field), <see cref="ArgumentException"/>
    /// (missing attributes, duplicate bookmark lines, a filter tab with no filter) or
    /// <see cref="NullReferenceException"/> (attribute iteration over a comment or text node that stands where
    /// an element was expected). The caller reaches this synchronously from the file-open path, so every one of
    /// those has to degrade to "no persistence data" rather than escape.
    /// </para>
    /// </remarks>
    public static PersistenceData Load (string fileName)
    {
        try
        {
            return LoadInternal(fileName);
        }
        catch (Exception xmlParsingException) when (xmlParsingException is XmlException or
                                                                           UnauthorizedAccessException or
                                                                           IOException or
                                                                           FileNotFoundException or
                                                                           FormatException or
                                                                           OverflowException or
                                                                           ArgumentException or
                                                                           NullReferenceException)
        {
            _logger.Error(xmlParsingException, $"Error loading persistence data from {fileName}, unknown format, parsing xml or json was not possible");
            return null;
        }
    }

    #endregion
}