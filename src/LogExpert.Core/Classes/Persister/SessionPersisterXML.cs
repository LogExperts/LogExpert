using System.Xml;

using NLog;

namespace LogExpert.Core.Classes.Persister;

/// <summary>
/// Legacy XML loader for Session (.lxj) files written under the older XML format.
/// Used as a fallback when JSON deserialization fails. See <see cref="SessionPersister"/>.
/// </summary>
public static class SessionPersisterXML
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Loads Session data from a legacy XML-format .lxj file.
    /// </summary>
    /// <remarks>The method reads the XML file to extract file names and layout information. If the XML file
    /// contains a <c>layout</c> element, its inner XML is stored in the <c>TabLayoutXml</c> property of the returned
    /// <see cref="SessionData"/> object. If any exception occurs during loading, an error is logged and an
    /// empty <see cref="SessionData"/> object is returned.</remarks>
    /// <param name="sessionFileName">The path to the XML file containing the Session data.</param>
    /// <returns>A <see cref="SessionData"/> object populated with file names and layout information from the XML file. If an
    /// error occurs during loading, an empty <see cref="SessionData"/> object is returned.</returns>
    public static SessionData LoadSessionData (string sessionFileName)
    {
        var sessionData = new SessionData();
        var xmlDoc = new XmlDocument();
        try
        {
            xmlDoc.Load(sessionFileName);
            var fileList = xmlDoc.GetElementsByTagName("member");

            foreach (XmlNode fileNode in fileList)
            {
                var fileElement = fileNode as XmlElement;
                var fileName = fileElement.GetAttribute("fileName");
                sessionData.FileNames.Add(fileName);
            }

            var layoutElements = xmlDoc.GetElementsByTagName("layout");
            if (layoutElements.Count > 0)
            {
                sessionData.TabLayoutXml = layoutElements[0].InnerXml;
            }

            return sessionData;
        }
        catch (Exception xmlParsingException) when (xmlParsingException is XmlException or
                                                                           UnauthorizedAccessException or
                                                                           IOException)
        {
            _logger.Error(xmlParsingException, $"Error loading Session data from {sessionFileName}, unknown format, parsing xml or json was not possible");
            return new SessionData();
        }
    }
}
