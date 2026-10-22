using BenchmarkDotNet.Running;

namespace LogExpert.Benchmarks;

public static class Program
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Benchmarks")]
    public static void Main (string[] args)
    {
        if (args == null || args.Length == 0)
        {
            Console.WriteLine("No benchmarks specified. Running all benchmarks...");

            // Run all benchmarks if no arguments are provided
            _ = BenchmarkRunner.Run<StreamReaderBenchmarks>();
            _ = BenchmarkRunner.Run<BufferIndexBenchmarks>();
            _ = BenchmarkRunner.Run<ReadThroughputBenchmarks>();
            _ = BenchmarkRunner.Run<BufferIndexContentionBenchmarks>();
            _ = BenchmarkRunner.Run<CsvColumnizerBenchmarks>();
        }
        else
        {
            // Run specific benchmarks based on command-line arguments
            _ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }

        Console.WriteLine("Replace <benchmarkname> with the name of the benchmark you want to run, e.g. ");
        Console.WriteLine("StreamReaderBenchmarks: Benchmarks for stream readers");
        Console.WriteLine("ReadThroughputBenchmarks: Benchmarks for read throughput");
        Console.WriteLine("BufferIndexBenchmarks: Benchmarks for buffer index");
        Console.WriteLine("BufferIndexContentionBenchmarks: Benchmarks for buffer index contention");
        Console.WriteLine("CsvColumnizerBenchmarks: Benchmarks for CSV columnizer (SplitLine, PreProcess, Selected)");
        Console.WriteLine("Dry run:");
        Console.WriteLine("dotnet run -c Release -- --filter \"*<benchmarkname>*\" --job Dry --noOverwrite");
        Console.WriteLine("Short run:");
        Console.WriteLine("dotnet run -c Release -- --filter \"*<benchmarkname>*\" --job Short --noOverwrite");
        Console.WriteLine("Full baseline run:");
        Console.WriteLine("dotnet run -c Release -- --filter \"*<benchmarkname>*\" --noOverwrite");
    }
}

/*
 * Comment / Uncommen the benchmark to run, careful some can run longer
 * 1.) a dry run
 * dotnet run -c Release -- --filter "StreamReaderBenchmarks" --job Dry --noOverwrite
 * 2.) a short run
 * dotnet run -c Release -- --filter "StreamReaderBenchmarks" --job Short --noOverwrite
 * 3.) a full baseline run
 * dotnet run -c Release -- --filter "StreamReaderBenchmarks" --noOverwrite
 *
 * The full baseline run generates a MD file
 * BenchmarkDotNet.Artifacts/results/*-report-github.md
 *
 * If changes are made with the LogfileReader / BufferIndex, always do a Benchmark to
 * verify no performance regression is introduced, especially with large files.
 */
