using System.Text;

using BenchmarkDotNet.Attributes;

using ColumnizerLib;

using CsvColumnizer;

using Moq;

namespace LogExpert.Benchmarks;

/// <summary>
/// Benchmarks for CsvColumnizer covering PreProcessLine, Selected, and SplitLine operations
/// across varying line counts and column widths.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class CsvColumnizerBenchmarks
{
    private ILogLineMemory[] _dataLines = null!;
    private CsvColumnizer.CsvColumnizer _columnizer = null!;

    [Params(100, 1_000, 10_000)]
    public int LineCount { get; set; }

    [Params(5, 15)]
    public int ColumnCount { get; set; }

    [GlobalSetup]
    public void Setup ()
    {
        // Build header and data lines
        var headerParts = new string[ColumnCount];
        for (var i = 0; i < ColumnCount; i++)
        {
            headerParts[i] = $"Column{i}";
        }

        var header = string.Join(";", headerParts);

        // Initialize columnizer with header
        _columnizer = new CsvColumnizer.CsvColumnizer();
        _columnizer.PreProcessLine(header.AsMemory(), 0, 0);

        var mockCallback = new Mock<ILogLineMemoryColumnizerCallback>();
        _columnizer.Selected(mockCallback.Object);

        // Generate data lines
        _dataLines = new ILogLineMemory[LineCount];
        var random = new Random(42);

        for (var i = 0; i < LineCount; i++)
        {
            var parts = new string[ColumnCount];
            for (var j = 0; j < ColumnCount; j++)
            {
                parts[j] = GenerateFieldValue(random, j);
            }

            _dataLines[i] = new CsvLogLine(string.Join(";", parts), i + 1);
        }
    }

    [Benchmark(Description = "SplitLine: parse all lines")]
    public int SplitAllLines ()
    {
        var totalColumns = 0;
        for (var i = 0; i < _dataLines.Length; i++)
        {
            var result = _columnizer.SplitLine(null, _dataLines[i]);
            totalColumns += result.ColumnValues.Length;
        }

        return totalColumns;
    }

    [Benchmark(Description = "PreProcessLine: preprocess all lines")]
    public int PreProcessAllLines ()
    {
        var processed = 0;
        for (var i = 0; i < _dataLines.Length; i++)
        {
            var result = _columnizer.PreProcessLine(_dataLines[i].FullLine, i + 1, i + 1);
            if (!result.IsEmpty)
            {
                processed++;
            }
        }

        return processed;
    }

    [Benchmark(Description = "Selected: re-detect columns from header")]
    public int RedetectColumns ()
    {
        var mockCallback = new Mock<ILogLineMemoryColumnizerCallback>();
        _columnizer.Selected(mockCallback.Object);
        return _columnizer.GetColumnCount();
    }

    private static string GenerateFieldValue (Random random, int columnIndex)
    {
        // Mix of value types: numbers, short text, quoted text with commas
        return (columnIndex % 4) switch
        {
            0 => random.Next(1, 100000).ToString(),
            1 => $"text_{random.Next(1, 9999)}",
            2 => $"\"Value, with quotes {random.Next(1, 999)}\"",
            _ => new string((char)('A' + random.Next(0, 26)), random.Next(5, 20)),
        };
    }
}
