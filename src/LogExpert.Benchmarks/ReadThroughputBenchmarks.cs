using System.Text;

using BenchmarkDotNet.Attributes;

using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;

namespace LogExpert.Benchmarks;

/// <summary>
/// Measures LogfileReader.ReadFiles() throughput with different progress reporters.
/// Uses real temp files to include actual I/O in the measurement.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ReadThroughputBenchmarks
{
    private string _tempFile = null!;

    [Params(10_000, 100_000, 1_000_000)]
    public int LineCount { get; set; }

    [GlobalSetup]
    public void Setup ()
    {
        _tempFile = Path.GetTempFileName();
        GenerateLogFile(_tempFile, LineCount);

        // Initialize PluginRegistry for local file system support
        // (or use NullPluginRegistry if constructor doesn't need it)
        _ = PluginRegistry.PluginRegistry.Create(Path.GetDirectoryName(_tempFile)!, 500);
    }

    /// <summary>
    /// Baseline: read with NullProgressReporter (zero event overhead).
    /// </summary>
    [Benchmark(Baseline = true)]
    public int ReadWithNullReporter ()
    {
        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 500,
            linesPerBuffer: 500,
            new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: Core.Classes.Log.ProgressReporters.NullProgressReporter.Instance);

        reader.ReadFiles();
        return reader.LineCount;
    }

    /// <summary>
    /// Production path: read with PeriodicProgressReporter (default, no subscribers).
    /// </summary>
    [Benchmark]
    public int ReadWithPeriodicReporter ()
    {
        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 500,
            linesPerBuffer: 500,
            new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500);
        // No progressReporter = default PeriodicProgressReporter

        reader.ReadFiles();
        return reader.LineCount;
    }

    /// <summary>
    /// Post-change: read with block-based allocation (System reader uses CharBlockAllocator).
    /// Compare Gen0/Gen1/Gen2 collections vs baseline to validate allocation reduction.
    /// This method is identical to ReadWithNullReporter — it exists solely for explicit
    /// before/after naming in benchmark reports.
    /// </summary>
    [Benchmark]
    public int ReadWithBlockAllocation ()
    {
        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 500,
            linesPerBuffer: 500,
            new MultiFileOptions(),
            ReaderType.System,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: Core.Classes.Log.ProgressReporters.NullProgressReporter.Instance);

        reader.ReadFiles();
        return reader.LineCount;
    }

    /// <summary>
    /// Direct-read: reads decoded chars directly into pooled blocks without per-line string allocation.
    /// Compare allocations and GC counts vs ReadWithBlockAllocation.
    /// </summary>
    [Benchmark]
    public int ReadWithDirectRead ()
    {
        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 500,
            linesPerBuffer: 500,
            new MultiFileOptions(),
            ReaderType.SystemDirect,
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: Core.Classes.Log.ProgressReporters.NullProgressReporter.Instance);

        reader.ReadFiles();
        return reader.LineCount;
    }

    [GlobalCleanup]
    public void Cleanup ()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Benchmark data generation")]
    private static void GenerateLogFile (string path, int lineCount)
    {
        var rng = new Random(42); // deterministic seed for reproducibility
        using var writer = new StreamWriter(path, false, Encoding.UTF8, bufferSize: 65536);
        for (int i = 0; i < lineCount; i++)
        {
            writer.Write("2026-04-23 12:00:00.");
            writer.Write(i % 1000);
            writer.Write(" [INFO] Thread-");
            writer.Write(rng.Next(1, 32));
            writer.Write(" SomeNamespace.SomeClass - Log message number ");
            writer.WriteLine(i);
        }
    }
}