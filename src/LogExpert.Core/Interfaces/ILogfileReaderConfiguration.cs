using System.Text;

using ColumnizerLib;

namespace LogExpert.Core.Interfaces;

/// <summary>
/// Capability interface for mutable configuration that affects how the Logfile Reader
/// parses lines. Typically set once at load time or when the user changes the Columnizer.
///
/// <para><b>Ordering:</b></para>
/// <list type="bullet">
///   <item>Set <see cref="IsXmlMode"/> and <see cref="XmlLogConfig"/> together
///         before calling <see cref="ILogfileReader.StartMonitoring"/>
///         or immediately after stopping monitoring for a Columnizer switch.</item>
///   <item><see cref="ChangeEncoding"/> clears all buffers and triggers a full re-read
///         on the next monitoring cycle.</item>
/// </list>
/// </summary>
public interface ILogfileReaderConfiguration
{
    /// <summary>
    /// Enables or disables XML log parsing mode. Must be set before
    /// <see cref="ILogfileReader.StartMonitoring"/>. Changing after monitoring
    /// has started triggers a re-read of the file content.
    /// </summary>
    bool IsXmlMode { get; set; }

    /// <summary>
    /// The XML log configuration (start/end tags) used when <see cref="IsXmlMode"/> is true.
    /// Must be set before <see cref="ILogfileReader.StartMonitoring"/> if XML mode is enabled.
    /// </summary>
    IXmlLogConfiguration XmlLogConfig { get; set; }

    /// <summary>
    /// Optional pre-process Columnizer that transforms each line as it is loaded from disk.
    /// Set to null to disable pre-processing. Can be changed at any time; takes effect on
    /// the next buffer read.
    /// </summary>
    IPreProcessColumnizerMemory PreProcessColumnizer { get; set; }

    /// <summary>
    /// Changes the character encoding used to read the file. Invalidates all cached buffers;
    /// the next access will re-read from disk with the new encoding.
    /// </summary>
    void ChangeEncoding(Encoding encoding);
}
