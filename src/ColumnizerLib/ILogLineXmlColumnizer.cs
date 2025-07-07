namespace LogExpert;

/// <summary>
/// This is the interface for a Columnizer which supports XML log files. This interface extends
/// the <see cref="ILogLineColumnizer"/> interface.
/// LogExpert will automatically load a log file in XML mode if the current Columnizer implements
/// this interface.
/// </summary>
/// <remarks>
/// <para>
/// Note that the ILogLineXmlColumnizer interface is also a marker interface. If the user selects a
/// Columnizer that implements ILogLineXmlColumnizer then the log file will be treatet as XML file.
/// <br></br>
/// When in XML mode, LogExpert will scan for XML fragmets. These fragments are defined by opening
/// and closing tags (e.g. &lt;log4j:event&gt; and &lt;/log4j:event&gt;). Every fragment is
/// transformed by using a XSLT template. The result of the transformation (which may be multi-lined)
/// is splitted into single lines. These single lines are the lines you will see in LogExpert's display.
/// </para>
/// <para>
/// If you implement a XML Columnizer you have to provide the start tag and end tag and a
/// XSLT. Also you have to provide a namespace declaration, if your logfile uses name spaces.
/// All this stuff must be provided by returning a IXmlLogConfiguration in the <see cref="GetXmlLogConfiguration"/> method.
/// </para>
/// <para>
/// The processing of XML log files is done in the following steps:
/// <ol>
/// <li>LogExpert reads the file and separates it into fragments of XML content using the given
///     start/end tags (<see cref="GetXmlLogConfiguration"/>)</li>
/// <li>The fragments will be translated using the given XSLT (<see cref="GetXmlLogConfiguration"/>)
///     The result is one or more lines of text content. These lines will be the lines LogExpert will 'see'
///     in its internal buffer and line management. They will be handled like normal text lines in other
///     (non-XML) log files.
/// </li>
/// <li>The lines will be passed to the usual <see cref="ILogLineColumnizer"/> methods before displaying. So you can handle
///     field splitting in the way known from <see cref="ILogLineColumnizer"/>.
/// </li>
/// </ol>
/// </para>
/// </remarks>
public interface ILogLineXmlColumnizer : ILogLineColumnizer
{
    #region Public methods

    /// <summary>
    /// You have to implement this function to provide a configuration for LogExpert's XML reader.
    /// </summary>
    /// <returns></returns>
    IXmlLogConfiguration GetXmlLogConfiguration ();

    /// <summary>
    /// Returns the text which should be copied into the clipboard when the user want to copy selected
    /// lines to clipboard.
    /// </summary>
    /// <param name="logLine">The line as retrieved from the internal log reader. This is
    /// the result of the XSLT processing with your provided stylesheet.
    /// </param>
    /// <param name="callback">Callback which may be used by the Columnizer</param>
    /// <returns>A string which is placed into the clipboard</returns>
    /// <remarks>
    /// This function is intended to convert the representation of a log line produced by XSLT transformation into
    /// a format suitable for clipboard.
    /// The method can be used in the case that the XSLT transformation result is not very 'human readable'.
    /// <br></br>
    /// An example is the included Log4jXMLColumnizer. It uses special characters to separate the fields.
    /// The characters are added while XSLT transformation. The usual Columnizer functions (e.g. SplitLIne()) will
    /// use these markers for line splitting.
    /// When copying to clipboard, this method will remove the special characters and replace them with spaces.
    /// </remarks>
    ILogLine GetLineTextForClipboard (ILogLine logLine, ILogLineColumnizerCallback callback);

    #endregion
}