using System.Buffers;
using System.Text;

using BenchmarkDotNet.Attributes;

using LogExpert.Core.Classes.Log.Streamreaders;
using LogExpert.Core.Entities;

namespace LogExpert.Benchmarks;

/// <summary>
/// Measures the default (Direct) stream reader the way <c>LogfileReader</c> drives it: lines come
/// from <c>TryReadLine</c>, and every <see cref="LINES_PER_BUFFER"/> lines the char blocks are
/// detached and returned to the pool, as a <c>LogBuffer</c> does when it is evicted.
/// The valid-UTF-8 workloads are the regression gate for reader changes; the invalid-bytes
/// workload covers files with binary blocks (issue #639).
/// </summary>
[MemoryDiagnoser]
public class DirectReaderBenchmarks
{
    private const int LINES_PER_BUFFER = 500;
    private const int MAX_LINE_LENGTH = 20_000;

    private byte[] _asciiMedium = null!;
    private byte[] _asciiLarge = null!;
    private byte[] _unicode = null!;
    private byte[] _mixedLineEndings = null!;
    private byte[] _longLines = null!;
    private byte[] _utf16 = null!;
    private byte[] _invalidBytes = null!;

    [GlobalSetup]
    public void Setup ()
    {
        _asciiMedium = Encoding.UTF8.GetBytes(GenerateAscii(10_000, 100, "\r\n"));
        _asciiLarge = Encoding.UTF8.GetBytes(GenerateAscii(200_000, 200, "\r\n"));
        _unicode = Encoding.UTF8.GetBytes(GenerateUnicode(100_000));
        _mixedLineEndings = Encoding.UTF8.GetBytes(GenerateMixedLineEndings(100_000));
        _longLines = Encoding.UTF8.GetBytes(GenerateAscii(500, 40_000, "\n"));
        _utf16 = Encoding.Unicode.GetBytes(GenerateAscii(100_000, 100, "\r\n"));
        _invalidBytes = GenerateInvalidBytes(100_000);
    }

    [Benchmark(Baseline = true)]
    public int AsciiMedium () => ReadAll(_asciiMedium, Encoding.UTF8);

    [Benchmark]
    public int AsciiLarge () => ReadAll(_asciiLarge, Encoding.UTF8);

    [Benchmark]
    public int Unicode () => ReadAll(_unicode, Encoding.UTF8);

    [Benchmark]
    public int MixedLineEndings () => ReadAll(_mixedLineEndings, Encoding.UTF8);

    [Benchmark]
    public int LongLines () => ReadAll(_longLines, Encoding.UTF8);

    [Benchmark]
    public int Utf16 () => ReadAll(_utf16, Encoding.Unicode);

    [Benchmark]
    public int InvalidBytes () => ReadAll(_invalidBytes, Encoding.UTF8);

    private static int ReadAll (byte[] data, Encoding encoding)
    {
        using var stream = new MemoryStream(data, writable: false);
        using var reader = new PositionAwareStreamReaderDirect(stream, new EncodingOptions { Encoding = encoding }, MAX_LINE_LENGTH);

        var lines = 0;
        while (reader.TryReadLine(out _))
        {
            if (++lines % LINES_PER_BUFFER == 0)
            {
                ReturnBlocks(reader.DetachCharBlocks());
            }
        }

        ReturnBlocks(reader.DetachCharBlocks());
        return lines;
    }

    private static void ReturnBlocks (List<char[]> blocks)
    {
        foreach (var block in blocks)
        {
            ArrayPool<char>.Shared.Return(block);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Benchmark data generation")]
    private static string GenerateAscii (int lineCount, int avgLineLength, string newLine)
    {
        var sb = new StringBuilder();
        var random = new Random(42);

        for (var i = 0; i < lineCount; i++)
        {
            var lineLength = avgLineLength + random.Next(-10, 11);
            _ = sb.Append("2026-04-23 12:00:00.").Append(i % 1000).Append(" [INFO] ")
                .Append('X', Math.Max(0, lineLength - 32))
                .Append(newLine);
        }

        return sb.ToString();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Benchmark data generation")]
    private static string GenerateUnicode (int lineCount)
    {
        var sb = new StringBuilder();
        var random = new Random(42);

        for (var i = 0; i < lineCount; i++)
        {
            _ = random.Next(0, 3) switch
            {
                0 => sb.Append("Line ").Append(i).Append(": ASCII text only, nothing special here\r\n"),
                1 => sb.Append("Line ").Append(i).Append(": Hello 世界, 日志文件 (Chinese)\r\n"),
                _ => sb.Append("Line ").Append(i).Append(": Спасибо большое 🙂 (Russian, emoji)\r\n")
            };
        }

        return sb.ToString();
    }

    private static string GenerateMixedLineEndings (int lineCount)
    {
        var sb = new StringBuilder();
        string[] endings = ["\r\n", "\n", "\r"];

        for (var i = 0; i < lineCount; i++)
        {
            _ = sb.Append("2026-04-23 12:00:00 [WARN] mixed ending line ").Append(i).Append(endings[i % endings.Length]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// CSV-like text with a binary block every 1000 lines: NUL runs, stray continuation bytes and
    /// truncated multi-byte sequences, the shape of the sample file from issue #639.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Benchmark data generation")]
    private static byte[] GenerateInvalidBytes (int lineCount)
    {
        var random = new Random(42);
        using var stream = new MemoryStream();

        for (var i = 0; i < lineCount; i++)
        {
            if (i % 1000 == 500)
            {
                var binary = new byte[2048];
                random.NextBytes(binary);
                stream.Write(binary);
            }

            stream.Write(Encoding.UTF8.GetBytes($"06/23/2026 00:00:00,MXS SEP26,3Minute,Info,OnVarUpdate(),\"line {i}\"\r\n"));
        }

        return stream.ToArray();
    }
}