using System.Text;

using BenchmarkDotNet.Attributes;

using LogExpert.Core.Classes.Log.Streamreaders;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;

namespace LogExpert.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class StreamReaderBenchmarks
{
    private byte[] _smallTestData;
    private byte[] _mediumTestData;
    private byte[] _largeTestData;
    private byte[] _unicodeTestData;

    [GlobalSetup]
    public void Setup ()
    {
        // Small: 1000 lines, ~50 bytes each = ~50 KB
        _smallTestData = GenerateTestData(1000, 50);

        // Medium: 10000 lines, ~100 bytes each = ~1 MB
        _mediumTestData = GenerateTestData(10000, 100);

        // Large: 100000 lines, ~200 bytes each = ~20 MB
        _largeTestData = GenerateTestData(100000, 200);

        // Unicode: 5000 lines with mixed ASCII and Unicode
        _unicodeTestData = GenerateUnicodeTestData(5000);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Unit Test")]
    private static byte[] GenerateTestData (int lineCount, int avgLineLength)
    {
        var sb = new StringBuilder();
        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < lineCount; i++)
        {
            var lineLength = avgLineLength + random.Next(-10, 11); // Vary line length slightly
            var line = $"Line {i:D10} " + new string('X', Math.Max(0, lineLength - 20));
            _ = sb.AppendLine(line);
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Unit Test")]
    private static byte[] GenerateUnicodeTestData (int lineCount)
    {
        var sb = new StringBuilder();
        var random = new Random(42);

        for (int i = 0; i < lineCount; i++)
        {
            var lineType = random.Next(0, 3);
            var line = lineType switch
            {
                0 => $"Line {i}: ASCII text only",
                1 => $"Line {i}: Hello 世界 (Chinese)",
                _ => $"Line {i}: Спасибо большое (Russian)"
            };
            _ = sb.AppendLine(line);
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Legacy", "Small", "ReadAll")]
    public void Legacy_ReadAll_Small ()
    {
        using var stream = new MemoryStream(_smallTestData);
        using var reader = new PositionAwareStreamReaderLegacy(stream, new EncodingOptions(), 10000);
        ReadAllLines(reader);
    }

    [Benchmark]
    [BenchmarkCategory("System", "Small", "ReadAll")]
    public void System_ReadAll_Small ()
    {
        using var stream = new MemoryStream(_smallTestData);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 10000);
        ReadAllLines(reader);
    }

    [Benchmark]
    [BenchmarkCategory("Legacy", "Medium", "ReadAll")]
    public void Legacy_ReadAll_Medium ()
    {
        using var stream = new MemoryStream(_mediumTestData);
        using var reader = new PositionAwareStreamReaderLegacy(stream, new EncodingOptions(), 10000);
        ReadAllLines(reader);
    }

    [Benchmark]
    [BenchmarkCategory("System", "Medium", "ReadAll")]
    public void System_ReadAll_Medium ()
    {
        using var stream = new MemoryStream(_mediumTestData);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 10000);
        ReadAllLines(reader);
    }

    [Benchmark]
    [BenchmarkCategory("Legacy", "Large", "ReadAll")]
    public void Legacy_ReadAll_Large ()
    {
        using var stream = new MemoryStream(_largeTestData);
        using var reader = new PositionAwareStreamReaderLegacy(stream, new EncodingOptions(), 10000);
        ReadAllLines(reader);
    }

    [Benchmark]
    [BenchmarkCategory("System", "Large", "ReadAll")]
    public void System_ReadAll_Large ()
    {
        using var stream = new MemoryStream(_largeTestData);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 10000);
        ReadAllLines(reader);
    }

    [Benchmark]
    [BenchmarkCategory("System", "Unicode", "ReadAll")]
    public void System_ReadAll_Unicode ()
    {
        using var stream = new MemoryStream(_unicodeTestData);
        using var reader = new PositionAwareStreamReaderSystem(stream, new EncodingOptions(), 10000);
        ReadAllLines(reader);
    }

    [Benchmark]
    [BenchmarkCategory("Legacy", "Unicode", "ReadAll")]
    public void Legacy_ReadAll_Unicode ()
    {
        using var stream = new MemoryStream(_unicodeTestData);
        using var reader = new PositionAwareStreamReaderLegacy(stream, new EncodingOptions(), 10000);
        ReadAllLines(reader);
    }

    private static void ReadAllLines (ILogStreamReader reader)
    {
        while (reader.ReadLine() != null)
        {
            // Consume the line
        }
    }
}