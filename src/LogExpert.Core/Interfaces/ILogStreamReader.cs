using System.Text;

namespace LogExpert.Core.Interfaces;

/// <summary>
/// Provides a position-aware stream reader interface for reading log files with support for character encoding
/// and position tracking.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts log file reading operations, providing a consistent API for different stream reading
/// implementations. All implementations must maintain accurate byte position tracking to support seeking and
/// re-reading specific portions of the log file.
/// </para>
/// <para>
/// Implementations include:
/// <list type="bullet">
/// <item><description><c>PositionAwareStreamReaderDirect</c> - Default. Reads decoded chars into pooled blocks and returns zero-copy slices; fastest, assumes <c>\n</c>/<c>\r\n</c> line endings</description></item>
/// <item><description><c>PositionAwareStreamReaderSystem</c> - Uses .NET's StreamReader.ReadLine(); also splits bare <c>\r</c> (classic-Mac) line endings</description></item>
/// <item><description><c>PositionAwareStreamReaderLegacy</c> - Character-by-character reading; exact byte position on mixed/pathological line endings, slowest</description></item>
/// <item><description><c>XmlLogReader</c> - Decorator for reading structured XML log blocks (e.g., Log4j XML format)</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ILogStreamReader : IDisposable
{
    #region Properties

    /// <summary>
    /// Gets or sets the current byte position in the stream.
    /// </summary>
    /// <value>
    /// The zero-based byte offset from the beginning of the stream, accounting for any byte order mark (BOM).
    /// </value>
    /// <remarks>
    /// <para>
    /// Setting the position causes the reader to seek to the specified byte offset in the underlying stream.
    /// This operation may be expensive as it requires resetting internal buffers and decoder state.
    /// </para>
    /// <para>
    /// The position should always represent a valid character boundary. Setting the position to the middle
    /// of a multi-byte character may result in decoding errors or incorrect output.
    /// </para>
    /// <para>
    /// After seeking, the next <see cref="ReadLine"/> or <see cref="ReadChar"/> operation will begin reading
    /// from the new position.
    /// </para>
    /// </remarks>
    long Position { get; set; }

    /// <summary>
    /// Gets a value indicating whether the internal buffer has been completely filled from the stream.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the buffer is complete and no additional data needs to be loaded;
    /// otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// This property is primarily used to determine if the reader is still waiting for data to become
    /// available in the stream. Most implementations return <see langword="true"/> as they read directly
    /// from the stream without pre-buffering.
    /// </remarks>
    bool IsBufferComplete { get; }

    /// <summary>
    /// Gets the character encoding used by the stream reader.
    /// </summary>
    /// <value>
    /// The <see cref="System.Text.Encoding"/> object representing the character encoding of the stream.
    /// </value>
    /// <remarks>
    /// <para>
    /// The encoding is determined during initialization and may be detected from a byte order mark (BOM)
    /// at the beginning of the stream, explicitly specified via <c>EncodingOptions</c>, or defaulted to
    /// the system default encoding.
    /// </para>
    /// <para>
    /// Supported BOM detection includes:
    /// <list type="bullet">
    /// <item><description>UTF-8 (EF BB BF)</description></item>
    /// <item><description>UTF-16 Little Endian (FF FE)</description></item>
    /// <item><description>UTF-16 Big Endian (FE FF)</description></item>
    /// <item><description>UTF-32 Little Endian (FF FE 00 00)</description></item>
    /// <item><description>UTF-32 Big Endian (00 00 FE FF)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Encoding Encoding { get; }

    #endregion

    #region Public methods

    /// <summary>
    /// Reads the next character from the stream and advances the position by the number of bytes consumed.
    /// </summary>
    /// <returns>
    /// The next character as an <see cref="int"/>, or -1 if the end of the stream has been reached.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The return value is an <see cref="int"/> rather than a <see cref="char"/> to allow returning -1
    /// for end-of-stream, following the convention established by <see cref="StreamReader.Read()"/>.
    /// </para>
    /// <para>
    /// After reading a character, the <see cref="Position"/> property is automatically advanced by the
    /// number of bytes consumed from the stream. For single-byte encodings this is always 1, for UTF-16
    /// this is always 2, but for variable-width encodings like UTF-8 this may be 1-4 bytes depending on
    /// the character.
    /// </para>
    /// <para>
    /// An implementation optimized purely for line-based reading may not support this method
    /// and will throw <see cref="NotSupportedException"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
    /// <exception cref="NotSupportedException">The implementation does not support character-level reading.</exception>
    /// <exception cref="IOException">An I/O error occurred while reading from the stream.</exception>
    int ReadChar ();

    /// <summary>
    /// Reads a line of characters from the stream and advances the position by the number of bytes consumed.
    /// </summary>
    /// <returns>
    /// A string containing the next line from the stream (excluding newline characters), or <see langword="null"/>
    /// if the end of the stream has been reached.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A line is defined as a sequence of characters followed by a line feed (\n), a carriage return (\r),
    /// or a carriage return followed by a line feed (\r\n). The returned string does not include the
    /// terminating newline character(s).
    /// </para>
    /// <para>
    /// The <see cref="Position"/> property is automatically advanced by the total number of bytes consumed,
    /// including the newline character(s).
    /// </para>
    /// <para>
    /// Implementations may enforce a maximum line length constraint. Lines exceeding this limit will be
    /// truncated to the maximum length. The specific limit is implementation-dependent and typically
    /// specified during construction.
    /// </para>
    /// <para>
    /// If the stream ends without a trailing newline, the remaining characters are returned as the last line.
    /// Subsequent calls will return <see langword="null"/> to indicate end-of-stream.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
    /// <exception cref="IOException">An I/O error occurred while reading from the stream.</exception>
    /// <exception cref="InvalidOperationException">
    /// The internal producer task encountered an error (specific to async implementations).
    /// </exception>
    string ReadLine ();

    #endregion
}