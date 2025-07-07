using LogExpert;

namespace Log4jXmlColumnizer;

/// <summary>
/// XMl configuration for parsing log4j XML files. The XSL will transform every block of log entries
/// into text lines. The fields in the text lines are separated by a special character (0xFFFD).
/// The special character will be used in the Split() function of the columnizer to split the line
/// into columns.
/// </summary>
internal class XmlConfig : IXmlLogConfiguration
{
    #region Properties

    public string XmlStartTag { get; } = "<log4j:event";

    public string XmlEndTag { get; } = "</log4j:event>";

    public string Stylesheet { get; } = "" +
                                        "<?xml version=\"1.0\" encoding=\"ISO-8859-1\" standalone=\"no\"?>" +
                                        "<xsl:stylesheet version=\"2.0\"" +
                                        "        xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"" +
                                        "        xmlns:log4j=\"http://jakarta.apache.org/log4j\">" +
                                        "<xsl:output method=\"text\" />" +
                                        "<xsl:template match=\"/log4j:event\"><xsl:value-of select=\"//@timestamp\"/>&#xFFFD;<xsl:value-of select=\"//@level\"/>&#xFFFD;<xsl:value-of select=\"//@logger\"/>&#xFFFD;<xsl:value-of select=\"//@thread\"/>&#xFFFD;<xsl:value-of select=\"//log4j:locationInfo/@class\"/>&#xFFFD;<xsl:value-of select=\"//log4j:locationInfo/@method\"/>&#xFFFD;<xsl:value-of select=\"//log4j:locationInfo/@file\"/>&#xFFFD;<xsl:value-of select=\"//log4j:locationInfo/@line\"/>&#xFFFD;<xsl:value-of select=\"//log4j:message\"/><xsl:value-of select=\"//log4j:throwable\"/>" +
                                        "</xsl:template>" +
                                        "</xsl:stylesheet>";

    public string[] GetNamespaceDeclaration () => ["log4j", "http://jakarta.apache.org/log4j"];

    #endregion
}