using System.Text;

const string baseDir = "logs";
const string baseName = "engine.log";
var safeBaseName = Path.GetFileName(baseName);
const int maxBackups = 6;
const int linesPerFile = 50;
// When true, every generated line contains a rotating selection of C0 control
// characters (0x00..0x1F and 0x7F, excluding LF/CR which would break the line
// into multiple records). Useful for verifying the control-character display
// substitution feature in LogExpert. Toggle here to compare runs.
var includeControlChars = true;

// All displayable control codepoints minus LF (0x0A) and CR (0x0D).
var controlChars = Enumerable.Range(0x00, 0x20)
    .Where(c => c is not 0x0A and not 0x0D)
    .Append(0x7F)
    .Select(c => (char)c)
    .ToArray();

Directory.CreateDirectory(baseDir);

// Create initial set of files
Console.WriteLine($"Creating initial files in '{Path.GetFullPath(baseDir)}'...");
WriteLogFile(Path.Join(baseDir, safeBaseName), 0);

for (var i = 1; i <= maxBackups; i++)
{
    WriteLogFile(Path.Join(baseDir, $"{safeBaseName}.{i}"), i);
}

PrintFiles();
Console.WriteLine();
Console.WriteLine("Open the 'engine.log' file in LogExpert with MultiFile enabled (pattern: *$J(.))");
Console.WriteLine($"Control characters in output: {(includeControlChars ? "ENABLED" : "disabled")}");
Console.WriteLine("Press ENTER to perform a rotation (with oldest file deletion).");
Console.WriteLine("Press A to append a single live line (no rotation) for tail testing.");
Console.WriteLine("Press D to delete the log, wait, then recreate it AND start a 25 lines/s");
Console.WriteLine("  background writer (repro for issue #568). Press D again to delete mid-stream.");
Console.WriteLine("Press F to flicker: delete, wait, briefly recreate, delete again mid-reload,");
Console.WriteLine("  wait, then recreate + writer. Tries to land LogExpert's new reader's");
Console.WriteLine("  ReadFiles inside a deletion window.");
Console.WriteLine("Press Q to quit.");

var rotationCount = 0;
var delayedDeleteCount = 0;
var flickerCount = 0;
const int delayedDeleteSeconds = 5;
const int liveWriterDelayMs = 40; // ~25 lines/s
const int flickerInitialAbsentMs = 5000;
const int flickerBriefVisibleMs = 200;
const int flickerSecondAbsentMs = 2500;
CancellationTokenSource? liveWriterCts = null;
Task? liveWriterTask = null;

while (true)
{
    var key = Console.ReadKey(true);

    if (key.Key == ConsoleKey.Q)
    {
        StopLiveWriter();
        break;
    }

    if (key.Key == ConsoleKey.A)
    {
        AppendLiveLine(Path.Join(baseDir, safeBaseName));
        continue;
    }

    if (key.Key == ConsoleKey.D)
    {
        delayedDeleteCount++;
        DelayedDelete(Path.Join(baseDir, safeBaseName), delayedDeleteCount, delayedDeleteSeconds);
        continue;
    }

    if (key.Key == ConsoleKey.F)
    {
        flickerCount++;
        FlickerRepro(Path.Join(baseDir, safeBaseName), flickerCount);
        continue;
    }

    if (key.Key != ConsoleKey.Enter)
    {
        continue;
    }

    rotationCount++;
    Console.WriteLine($"\n--- Rotation #{rotationCount} ---");

    // Delete the oldest file (simulates maxBackups limit)
    var oldest = Path.Join(baseDir, $"{safeBaseName}.{maxBackups}");

    if (File.Exists(oldest))
    {
        File.Delete(oldest);
        Console.WriteLine($"  Deleted: {safeBaseName}.{maxBackups}");
    }

    // Shift all numbered files up by one
    for (var i = maxBackups - 1; i >= 1; i--)
    {
        var src = Path.Join(baseDir, $"{safeBaseName}.{i}");
        var dst = Path.Join(baseDir, $"{safeBaseName}.{i + 1}");

        if (File.Exists(src))
        {
            File.Move(src, dst);
            Console.WriteLine($"  Renamed: {safeBaseName}.{i} -> {safeBaseName}.{i + 1}");
        }
    }

    // Rename current log to .1
    var current = Path.Join(baseDir, safeBaseName);
    var first = Path.Join(baseDir, $"{safeBaseName}.1");

    if (File.Exists(current))
    {
        File.Move(current, first);
        Console.WriteLine($"  Renamed: {safeBaseName} -> {safeBaseName}.1");
    }

    // Create empty file first (like real log frameworks do), so LogExpert detects
    // newSize < oldSize and triggers ShiftBuffers()
    File.Create(current).Dispose();
    Console.WriteLine($"  Created: {safeBaseName} (empty - triggers rollover detection)");

    PrintFiles();

    // Wait for LogExpert's poll interval to detect the smaller file, then write content
    Console.WriteLine("  Waiting 2s for LogExpert to detect rollover...");
    Thread.Sleep(2000);

    WriteLogFile(current, maxBackups + rotationCount);
    Console.WriteLine($"  Wrote content to {safeBaseName}");

    PrintFiles();
    Console.WriteLine("\nPress ENTER for next rotation, A to append a live line, Q to quit.");
}

void WriteLogFile(string path, int fileId)
{
    using var writer = new StreamWriter(path, false, Encoding.UTF8);

    for (var i = 1; i <= linesPerFile; i++)
    {
        writer.WriteLine(BuildLine(fileId, i, Path.GetFileName(path)));
    }
}

void AppendLiveLine(string path)
{
    var name = Path.GetFileName(path);
    using var writer = new StreamWriter(path, append: true, Encoding.UTF8);
    var line = BuildLine(fileId: 999, lineIndex: (int)(DateTime.Now.Ticks & 0xFFF), fileName: name);
    writer.WriteLine(line);
    Console.WriteLine($"  Appended live line to {name} ({new FileInfo(path).Length} bytes total)");
}

// Repro path for issue #568: stop any background writer, delete the file and
// keep it absent long enough (> LogExpert's 1.25s OpenStream retry budget) for
// the watcher to enter FileNotFound state, then recreate it AND start a
// continuous background writer (~25 lines/s). The next D press will delete the
// file while the writer is actively appending — that mid-stream delete is the
// scenario the reporter describes.
void DelayedDelete(string path, int iteration, int delaySeconds)
{
    var name = Path.GetFileName(path);
    Console.WriteLine($"\n--- Delete + delay + recreate #{iteration} ---");

    StopLiveWriter();

    if (File.Exists(path))
    {
        File.Delete(path);
        Console.WriteLine($"  Deleted: {name}");
    }
    else
    {
        Console.WriteLine($"  {name} was already missing.");
    }

    Console.WriteLine($"  File absent. Waiting {delaySeconds}s so LogExpert enters FileNotFound state...");
    for (var i = delaySeconds; i > 0; i--)
    {
        Console.Write($"\r  Countdown: {i}s ");
        Thread.Sleep(1000);
    }
    Console.WriteLine("\r  Countdown: done.");

    WriteLogFile(path, fileId: 900 + iteration);
    Console.WriteLine($"  Recreated {name} with {linesPerFile} lines ({new FileInfo(path).Length} bytes).");
    StartLiveWriter(path, iteration);
    Console.WriteLine($"  Background writer started (~{1000 / liveWriterDelayMs} lines/s).");
    Console.WriteLine("  Watch LogExpert: lines should keep appearing.");
    Console.WriteLine("  If they do NOT, the bug is reproduced. Press D again to delete mid-stream.");
}

// Tighter race than DelayedDelete: after the file has been absent long enough
// for LogExpert to enter FileNotFound, we briefly recreate it (so the watcher
// fires OnRespawned and the LogWindow schedules a Reload), then delete it
// again before the new LogfileReader's first ReadFiles completes its
// OpenStream retries (5 x 250ms = 1.25s). If the hypothesis about issue #568
// is correct, the new reader's ReadFiles catches IOException, _isDeleted is
// set, ReportLoadingFinished is skipped, and FileSizeChanged never gets wired
// up. After we recreate the file for real and start the writer, those writes
// should fail to propagate.
void FlickerRepro(string path, int iteration)
{
    var name = Path.GetFileName(path);
    Console.WriteLine($"\n--- Flicker repro #{iteration} ---");

    StopLiveWriter();

    if (File.Exists(path))
    {
        File.Delete(path);
        Console.WriteLine($"  Deleted: {name}");
    }

    Console.WriteLine($"  Phase 1: file absent for {flickerInitialAbsentMs / 1000.0:0.0}s (LogExpert -> FileNotFound)");
    Thread.Sleep(flickerInitialAbsentMs);

    WriteLogFile(path, fileId: 700 + iteration);
    Console.WriteLine($"  Phase 2: briefly visible ({flickerBriefVisibleMs}ms) - LogExpert schedules a Reload");
    Thread.Sleep(flickerBriefVisibleMs);

    File.Delete(path);
    Console.WriteLine($"  Phase 3: deleted again, absent {flickerSecondAbsentMs / 1000.0:0.0}s");
    Console.WriteLine($"           (exceeds 1.25s OpenStream retry budget - new reader's ReadFiles should fail)");
    Thread.Sleep(flickerSecondAbsentMs);

    WriteLogFile(path, fileId: 750 + iteration);
    Console.WriteLine($"  Phase 4: recreated with {linesPerFile} lines, starting writer.");
    StartLiveWriter(path, iteration);
    Console.WriteLine("  Watch LogExpert. If row count freezes, bug reproduced.");
}

void StartLiveWriter(string path, int iteration)
{
    StopLiveWriter();
    liveWriterCts = new CancellationTokenSource();
    var token = liveWriterCts.Token;
    var fileId = 800 + iteration;
    liveWriterTask = Task.Run(() => LiveWriterLoop(path, fileId, token));
}

void StopLiveWriter()
{
    if (liveWriterCts == null)
    {
        return;
    }

    liveWriterCts.Cancel();
    try
    {
        liveWriterTask?.Wait(TimeSpan.FromSeconds(2));
    }
    catch (AggregateException)
    {
        // expected: task cancelled
    }

    liveWriterCts.Dispose();
    liveWriterCts = null;
    liveWriterTask = null;
}

void LiveWriterLoop(string path, int fileId, CancellationToken token)
{
    var name = Path.GetFileName(path);
    var lineIndex = 0;
    while (!token.IsCancellationRequested)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            using var writer = new StreamWriter(fs, Encoding.UTF8);
            writer.WriteLine(BuildLine(fileId, ++lineIndex, name));
        }
        catch (IOException)
        {
            // file may be momentarily inaccessible during a D-press; just keep
            // trying so writes resume once it reappears.
        }

        try
        {
            Task.Delay(liveWriterDelayMs, token).Wait(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (AggregateException)
        {
            return;
        }
    }
}

string BuildLine(int fileId, int lineIndex, string fileName)
{
    var baseText = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [INFO] File#{fileId:D3} Line {lineIndex:D3} - {fileName} - Sample log message";

    if (!includeControlChars)
    {
        return baseText;
    }

    // Embed three control characters that rotate through the full set as the
    // line index advances, so every line looks slightly different and over a
    // run all 31 displayable codepoints (LF/CR excluded) appear.
    var n = controlChars.Length;
    var c1 = controlChars[lineIndex % n];
    var c2 = controlChars[(lineIndex + 7) % n];
    var c3 = controlChars[(lineIndex + 13) % n];

    return $"{baseText} | ctrl:[{c1}{c2}{c3}] payload<{c1}field1{c2}field2{c3}field3>";
}

void PrintFiles()
{
    Console.WriteLine("\nCurrent files on disk:");

    foreach (var f in Directory.GetFiles(baseDir, $"{safeBaseName}*").OrderBy(f => f))
    {
        Console.WriteLine($"  {Path.GetFileName(f)} ({new FileInfo(f).Length} bytes)");
    }
}
