namespace LogExpert;

/// <summary>
/// This interface declares the configuration data which is needed for XML log file parsing.
/// </summary>
public interface IXmlLogConfiguration
{
    #region Properties

    /// <summary>
    /// The opening XML tag for a log entry. Every log entry starts with this tag.<br></br>
    /// Example: &lt;log4j:event&gt;
    /// </summary>
    string XmlStartTag { get; }

    /// <summary>
    /// The closing tag for a log entry.<br></br>
    /// Example: &lt;/log4j:event&gt;
    /// </summary>
    string XmlEndTag { get; }

    /// <summary>
    /// A complete XSLT which is used to transform the XML fragments into text lines which can be
    /// processed by the Columnizer.
    /// </summary>
    string Stylesheet { get; }

    /// <summary>
    /// A namespace declaration. The returned array must contain 2 strings: The namespace and its declaration.<br></br>
    /// Example: {"log4j", "http://jakarta.apache.org/log4j"}
    ///
    /// </summary>
    string[] GetNamespaceDeclaration ();

    #endregion
}