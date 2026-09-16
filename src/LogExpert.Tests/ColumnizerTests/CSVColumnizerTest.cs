using System.Reflection;

using ColumnizerLib;

using CsvColumnizer;

using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.Core.Enums;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.ColumnizerTests;

[TestFixture]
public class CSVColumnizerTest
{
    [SetUp]
    public void Setup ()
    {
        // Reset singleton for testing (same pattern as PluginRegistryTests)
        ResetPluginRegistrySingleton();

        // Initialize plugin registry with proper test directory
        var testDataPath = Path.Join(Path.GetTempPath(), "LogExpertTests", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(testDataPath);

        var pluginRegistry = PluginRegistry.PluginRegistry.Create(testDataPath, 250);

        // Verify the local file system plugin is registered
        var localPlugin = pluginRegistry.FindFileSystemForUri(@"C:\test.txt");
        Assert.That(localPlugin, Is.Not.Null, "Local file system plugin not registered!");
    }

    [TearDown]
    public void TearDown ()
    {
        ResetPluginRegistrySingleton();
    }

    /// <summary>
    /// Uses reflection to reset the singleton instance for testing.
    /// This ensures each test starts with a fresh PluginRegistry state.
    /// </summary>
    private static void ResetPluginRegistrySingleton ()
    {
        var instanceField = typeof(PluginRegistry.PluginRegistry).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        instanceField?.SetValue(null, null);
    }

    /// <summary>
    /// Sets a private field on the CsvColumnizer via reflection.
    /// Used to reach edge-case states (e.g. _isValidCsv=true with _firstLine=null)
    /// that are not reachable through the public API alone.
    /// </summary>
    private static void SetPrivateField (CsvColumnizer.CsvColumnizer columnizer, string fieldName, object? value)
    {
        var field = typeof(CsvColumnizer.CsvColumnizer).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' not found on CsvColumnizer");
        field!.SetValue(columnizer, value);
    }

    [Test]
    public void Selected_HasFieldNames_FirstLineNull_FallsBackToCallback ()
    {
        // Arrange: _isValidCsv=true but _firstLine=null (edge case reachable via legacy adapter)
        CsvColumnizer.CsvColumnizer columnizer = new();
        SetPrivateField(columnizer, "_isValidCsv", true);

        var callbackLine = new CsvLogLine("name;age;city", 0);
        var mockCallback = new Mock<ILogLineMemoryColumnizerCallback>();
        _ = mockCallback.Setup(c => c.GetLogLineMemory(0)).Returns(callbackLine);

        // Act
        columnizer.Selected(mockCallback.Object);

        // Assert: columns detected from callback line
        Assert.That(columnizer.GetColumnCount(), Is.EqualTo(3));
        Assert.That(columnizer.GetColumnNames(), Is.EqualTo(["name", "age", "city"]));
    }

    [Test]
    public void Selected_HasFieldNames_NoLineAvailable_FallsBackToTextColumn ()
    {
        // Arrange: _isValidCsv=true, _firstLine=null, callback returns null
        CsvColumnizer.CsvColumnizer columnizer = new();
        SetPrivateField(columnizer, "_isValidCsv", true);

        var mockCallback = new Mock<ILogLineMemoryColumnizerCallback>();
        _ = mockCallback.Setup(c => c.GetLogLineMemory(0)).Returns((ILogLineMemory?)null);

        // Act
        columnizer.Selected(mockCallback.Object);

        // Assert: graceful fallback to single "Text" column
        Assert.That(columnizer.GetColumnCount(), Is.EqualTo(1));
        Assert.That(columnizer.GetColumnNames(), Is.EqualTo(["Text"]));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void SplitLine_EmptyFullLine_ReturnsSingleColumn ()
    {
        // Arrange: columnizer in valid-CSV mode with columns established
        CsvColumnizer.CsvColumnizer columnizer = new();
        _ = columnizer.PreProcessLine("a;b;c".AsMemory(), 0, 0);

        var mockCallback = new Mock<ILogLineMemoryColumnizerCallback>();
        columnizer.Selected(mockCallback.Object);

        // Act: split a line with empty FullLine
        var emptyLine = new CsvLogLine(ReadOnlyMemory<char>.Empty, 1);
        var result = columnizer.SplitLine(null, emptyLine);

        // Assert: single column with empty content, no crash
        Assert.That(result.ColumnValues, Has.Length.EqualTo(1));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void SplitLine_ValidCsvLine_ReturnsCorrectColumns ()
    {
        // Arrange: columnizer in valid-CSV mode
        CsvColumnizer.CsvColumnizer columnizer = new();
        _ = columnizer.PreProcessLine("name;age;city".AsMemory(), 0, 0);

        var mockCallback = new Mock<ILogLineMemoryColumnizerCallback>();
        columnizer.Selected(mockCallback.Object);

        // Act: split a normal data line
        var dataLine = new CsvLogLine("Alice;30;London", 1);
        var result = columnizer.SplitLine(null, dataLine);

        // Assert: three columns with correct values
        Assert.That(result.ColumnValues, Has.Length.EqualTo(3));
        Assert.That(result.ColumnValues[0].FullValue.ToString(), Is.EqualTo("Alice"));
        Assert.That(result.ColumnValues[1].FullValue.ToString(), Is.EqualTo("30"));
        Assert.That(result.ColumnValues[2].FullValue.ToString(), Is.EqualTo("London"));
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void SplitLine_BadCsvData_ReturnsSingleColumnInsteadOfCrash ()
    {
        // Arrange: columnizer with semicolon delimiter (default) but line uses commas with quoted fields
        // This triggers CsvHelper.BadDataException because quotes appear mid-field in semicolon mode
        CsvColumnizer.CsvColumnizer columnizer = new();
        _ = columnizer.PreProcessLine("header1;header2;header3".AsMemory(), 0, 0);

        var mockCallback = new Mock<ILogLineMemoryColumnizerCallback>();
        columnizer.Selected(mockCallback.Object);

        // Act: line with comma-separated data containing quoted fields — bad data for semicolon delimiter
        var badLine = new CsvLogLine("6,6774DC1dB00BD11,\"Farmer, Edwards and Andrade\",http://wolfe-boyd.com/,Norfolk Island,Virtual leadingedge benchmark,2003,Mental Health Care,3503", 1);
        var result = columnizer.SplitLine(null, badLine);

        // Assert: returns single-column fallback, no crash
        Assert.That(result.ColumnValues, Has.Length.EqualTo(1));
        Assert.That(result.ColumnValues[0].FullValue.ToString(), Does.Contain("6774DC1dB00BD11"));
    }

    [TestCase(@".\TestData\organizations-10000.csv", new[] { "Index", "Organization Id", "Name", "Website", "Country", "Description", "Founded", "Industry", "Number of employees" }, ReaderType.System)]
    [TestCase(@".\TestData\organizations-1000.csv", new[] { "Index", "Organization Id", "Name", "Website", "Country", "Description", "Founded", "Industry", "Number of employees" }, ReaderType.System)]
    [TestCase(@".\TestData\people-10000.csv", new[] { "Index", "User Id", "First Name", "Last Name", "Sex", "Email", "Phone", "Date of birth", "Job Title" }, ReaderType.System)]
    public void Instantiat_CSVFile_BuildCorrectColumnizer (string filename, string[] expectedHeaders, ReaderType readerType)
    {
        CsvColumnizer.CsvColumnizer csvColumnizer = new();
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, filename);
        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), readerType, PluginRegistry.PluginRegistry.Instance, 500);
        reader.ReadFiles();
        var line = reader.GetLogLineMemory(0);
        IColumnizedLogLineMemory logline = new ColumnizedLogLine();
        if (line != null)
        {
            logline = csvColumnizer.SplitLine(null, line);
        }

        var expectedResult = string.Join(",", expectedHeaders);
        Assert.That(logline.LogLine.FullLine.ToString(), Is.EqualTo(expectedResult));
    }

    #region Line reading tests for files with no trailing newline

    [Test]
    public void LogfileReader_SemicolonCsv_ReadsAllLines ()
    {
        // semicolon.csv: CRLF between lines, no trailing newline on last line
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\semicolon.csv");
        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.EqualTo(2), $"semicolon.csv should have 2 lines, got {reader.LineCount}");
    }

    [Test]
    public void LogfileReader_TabCsv_ReadsAllLines ()
    {
        // tab.csv: header + 1 data line, CRLF between lines
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\tab.csv");
        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.EqualTo(2), $"tab.csv should have 2 lines, got {reader.LineCount}");
    }

    [Test]
    public void LogfileReader_CsvTest01_ReadsAllLines ()
    {
        // CsvTest_01.csv: CR line endings, no trailing newline on last line
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\CsvTest_01.csv");
        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.EqualTo(31), $"CsvTest_01.csv should have 31 lines (header + 30 data), got {reader.LineCount}");
    }

    [Test]
    public void LogfileReader_SemicolonCsv_WithPreProcess_DataLinesVisible ()
    {
        // With CsvColumnizer as PreProcessColumnizer, header should be dropped, data line should remain
        CsvColumnizer.CsvColumnizer csvColumnizer = new();
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\semicolon.csv");
        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.PreProcessColumnizer = csvColumnizer;
        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.GreaterThan(0), $"Should have data lines after header is dropped, got {reader.LineCount}");

        var line = reader.GetLogLineMemory(0);
        Assert.That(line, Is.Not.Null);
        Assert.That(line.FullLine.IsEmpty, Is.False);
        Assert.That(line.FullLine.ToString(), Does.Contain("2021-12-12"));
    }

    [Test]
    public void LogfileReader_TabCsv_WithPreProcess_DataLinesVisible ()
    {
        CsvColumnizer.CsvColumnizer csvColumnizer = new();
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\tab.csv");
        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.PreProcessColumnizer = csvColumnizer;
        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.GreaterThan(0), $"Should have data lines after header is dropped, got {reader.LineCount}");

        var line = reader.GetLogLineMemory(0);
        Assert.That(line, Is.Not.Null);
        Assert.That(line.FullLine.IsEmpty, Is.False);
        Assert.That(line.FullLine.ToString(), Does.Contain("2023-05-01"));
    }

    [Test]
    public void LogfileReader_SemicolonCsv_SimulateGuiReload_DataLinesVisible ()
    {
        // Simulate the GUI reload flow:
        // GUI creates a NEW reader with PreProcess set from the start (like Reload does)
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\semicolon.csv");

        CsvColumnizer.CsvColumnizer csvColumnizer = new();

        // The GUI Reload creates a brand new LogfileReader with PreProcess already set
        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.PreProcessColumnizer = csvColumnizer;
        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.EqualTo(1), $"After read with PreProcess, header dropped, 1 data line. Got {reader.LineCount}");

        var line = reader.GetLogLineMemory(0);
        Assert.That(line, Is.Not.Null, "First visible line should not be null");
        Assert.That(line.FullLine.IsEmpty, Is.False, "First visible line should not be empty");
        Assert.That(line.FullLine.ToString(), Does.Contain("2021-12-12"), "Should be the data line");

        // Simulate GUI calling Selected() (normally done in SetColumnizerInternal)
        var callbackMock = new Mock<ILogLineMemoryColumnizerCallback>();
        _ = callbackMock.Setup(c => c.GetLogLineMemory(0)).Returns(reader.GetLogLineMemory(0));
        csvColumnizer.Selected(callbackMock.Object);

        // Verify CsvColumnizer state after Selected()
        Assert.That(csvColumnizer.GetColumnCount(), Is.EqualTo(3), "Should have 3 columns (Date, Level, Message)");
        var names = csvColumnizer.GetColumnNames();
        Assert.That(names[0], Is.EqualTo("Date"));
        Assert.That(names[1], Is.EqualTo("Level"));
        Assert.That(names[2], Is.EqualTo("Message"));
    }

    [Test]
    public void LogfileReader_SemicolonCsv_StartMonitoring_LoadsCorrectly ()
    {
        // Test the actual StartMonitoring flow (async, like the GUI)
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\semicolon.csv");

        CsvColumnizer.CsvColumnizer csvColumnizer = new();

        using ManualResetEventSlim loadingDone = new(false);

        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.PreProcessColumnizer = csvColumnizer;
        reader.LoadingFinished += (_, _) => loadingDone.Set();
        reader.StartMonitoring();

        Assert.That(loadingDone.Wait(TimeSpan.FromSeconds(5)), Is.True, "Loading should finish within 5 seconds");

        Assert.That(reader.LineCount, Is.EqualTo(1), $"Header dropped, 1 data line expected. Got {reader.LineCount}");

        var line = reader.GetLogLineMemory(0);
        Assert.That(line, Is.Not.Null);
        Assert.That(line.FullLine.ToString(), Does.Contain("2021-12-12"));

        // Simulate GUI calling Selected()
        var callbackMock = new Mock<ILogLineMemoryColumnizerCallback>();
        _ = callbackMock.Setup(c => c.GetLogLineMemory(0)).Returns(reader.GetLogLineMemory(0));
        csvColumnizer.Selected(callbackMock.Object);

        // CsvColumnizer should have valid state
        Assert.That(csvColumnizer.GetColumnCount(), Is.EqualTo(3));

        reader.StopMonitoring();
    }

    [Test]
    public void LogfileReader_CsvTest01_StartMonitoring_LoadsCorrectly ()
    {
        // CsvTest_01.csv: CR line endings, comma delimiter, 31 lines
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\CsvTest_01.csv");

        CsvColumnizer.CsvColumnizer csvColumnizer = new();

        using ManualResetEventSlim loadingDone = new(false);

        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.PreProcessColumnizer = csvColumnizer;
        reader.LoadingFinished += (_, _) => loadingDone.Set();
        reader.StartMonitoring();

        Assert.That(loadingDone.Wait(TimeSpan.FromSeconds(5)), Is.True, "Loading should finish within 5 seconds");

        // CsvTest_01.csv uses comma delimiter but default config uses semicolon.
        // PreProcessLine still drops line 0 (regardless of delimiter match).
        Assert.That(reader.LineCount, Is.EqualTo(30), $"Header dropped, 30 data lines expected. Got {reader.LineCount}");

        // Verify _firstLine survived allocator block recycling (the CsvLogLine fix)
        var callbackMock = new Mock<ILogLineMemoryColumnizerCallback>();
        _ = callbackMock.Setup(c => c.GetLogLineMemory(0)).Returns(reader.GetLogLineMemory(0));
        csvColumnizer.Selected(callbackMock.Object);

        // With auto-detection, comma delimiter should be detected, giving 18 columns
        Assert.That(csvColumnizer.GetColumnCount(), Is.EqualTo(18), $"Should have 18 columns (auto-detected comma delimiter). Got {csvColumnizer.GetColumnCount()}");
        var names = csvColumnizer.GetColumnNames();
        Assert.That(names[0], Is.EqualTo("policyID"), "First column should be policyID");

        reader.StopMonitoring();
    }

    [Test]
    public void LogfileReader_TabCsv_StartMonitoring_LoadsCorrectly ()
    {
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\tab.csv");

        CsvColumnizer.CsvColumnizer csvColumnizer = new();

        using ManualResetEventSlim loadingDone = new(false);

        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.PreProcessColumnizer = csvColumnizer;
        reader.LoadingFinished += (_, _) => loadingDone.Set();
        reader.StartMonitoring();

        Assert.That(loadingDone.Wait(TimeSpan.FromSeconds(5)), Is.True);

        // tab.csv: header + 1 data line, header dropped
        Assert.That(reader.LineCount, Is.EqualTo(1), $"Header dropped, 1 data line expected. Got {reader.LineCount}");

        reader.StopMonitoring();
    }

    [Test]
    public void LogfileReader_CommaCsv_StartMonitoring_LoadsCorrectly ()
    {
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\comma.csv");

        CsvColumnizer.CsvColumnizer csvColumnizer = new();

        using ManualResetEventSlim loadingDone = new(false);

        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.PreProcessColumnizer = csvColumnizer;
        reader.LoadingFinished += (_, _) => loadingDone.Set();
        reader.StartMonitoring();

        Assert.That(loadingDone.Wait(TimeSpan.FromSeconds(5)), Is.True, "Loading should finish within 5 seconds");

        // comma.csv: header + 1 data line, comma delimiter auto-detected
        Assert.That(reader.LineCount, Is.EqualTo(1), $"Header dropped, 1 data line expected. Got {reader.LineCount}");

        var line = reader.GetLogLineMemory(0);
        Assert.That(line, Is.Not.Null);
        Assert.That(line.FullLine.ToString(), Does.Contain("comma file"));

        // Simulate GUI calling Selected()
        var callbackMock = new Mock<ILogLineMemoryColumnizerCallback>();
        _ = callbackMock.Setup(c => c.GetLogLineMemory(0)).Returns(reader.GetLogLineMemory(0));
        csvColumnizer.Selected(callbackMock.Object);

        Assert.That(csvColumnizer.GetColumnCount(), Is.EqualTo(3), "Should detect 3 columns with comma delimiter");
        var names = csvColumnizer.GetColumnNames();
        Assert.That(names[0], Is.EqualTo("Date"));
        Assert.That(names[1], Is.EqualTo("Level"));
        Assert.That(names[2], Is.EqualTo("Message"));

        // Simulate GUI calling SplitLine on the data line (what the DataGridView does)
        var splitResult = csvColumnizer.SplitLine(callbackMock.Object, line);
        Assert.That(splitResult, Is.Not.Null, "SplitLine should not return null");
        Assert.That(splitResult.ColumnValues.Length, Is.EqualTo(3), $"SplitLine should return 3 columns, got {splitResult.ColumnValues.Length}");
        Assert.That(splitResult.ColumnValues[0].FullValue.ToString(), Is.EqualTo("2021-01-01"), $"Column 0 should be '2021-01-01', got '{splitResult.ColumnValues[0].FullValue}'");
        Assert.That(splitResult.ColumnValues[1].FullValue.ToString(), Is.EqualTo("Error"), $"Column 1 should be 'Error', got '{splitResult.ColumnValues[1].FullValue}'");
        Assert.That(splitResult.ColumnValues[2].FullValue.ToString(), Is.EqualTo("comma file"), $"Column 2 should be 'comma file', got '{splitResult.ColumnValues[2].FullValue}'");

        reader.StopMonitoring();
    }

    [Test]
    public void LogfileReader_SemicolonCsv_SplitLine_ReturnsCorrectValues ()
    {
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\semicolon.csv");

        CsvColumnizer.CsvColumnizer csvColumnizer = new();

        using ManualResetEventSlim loadingDone = new(false);

        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.PreProcessColumnizer = csvColumnizer;
        reader.LoadingFinished += (_, _) => loadingDone.Set();
        reader.StartMonitoring();

        Assert.That(loadingDone.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(reader.LineCount, Is.EqualTo(1));

        var line = reader.GetLogLineMemory(0);
        Assert.That(line, Is.Not.Null);
        Assert.That(line.FullLine.ToString(), Does.Contain("2021-12-12"));

        var callbackMock = new Mock<ILogLineMemoryColumnizerCallback>();
        _ = callbackMock.Setup(c => c.GetLogLineMemory(0)).Returns(line);
        csvColumnizer.Selected(callbackMock.Object);

        Assert.That(csvColumnizer.GetColumnCount(), Is.EqualTo(3));

        // Call SplitLine - this is what the GUI grid does to render cells
        var splitResult = csvColumnizer.SplitLine(callbackMock.Object, line);
        Assert.That(splitResult, Is.Not.Null);
        Assert.That(splitResult.ColumnValues.Length, Is.EqualTo(3), $"Expected 3 columns, got {splitResult.ColumnValues.Length}");
        Assert.That(splitResult.ColumnValues[0].FullValue.ToString(), Is.EqualTo("2021-12-12"));
        Assert.That(splitResult.ColumnValues[1].FullValue.ToString(), Is.EqualTo("TRACE"));
        Assert.That(splitResult.ColumnValues[2].FullValue.ToString(), Is.EqualTo("semicolon file "));

        reader.StopMonitoring();
    }

    /// <summary>
    /// Simulates the exact GUI Reload flow:
    /// 1. First load happens WITHOUT PreProcessColumnizer (normal read)
    /// 2. Then the CsvColumnizer is assigned and Reload triggers a re-read with PreProcess active
    /// This is the "header + 1 data line" scenario the user reports as broken.
    /// </summary>
    [Test]
    public void LogfileReader_CommaCsv_ReloadWithPreProcess_DataLineNotEmpty ()
    {
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\comma.csv");

        // Step 1: Initial load WITHOUT PreProcessColumnizer (like GUI first load)
        CsvColumnizer.CsvColumnizer csvColumnizer = new();

        using ManualResetEventSlim loadingDone = new(false);

        LogfileReader reader = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        // NO PreProcessColumnizer set initially
        reader.LoadingFinished += (_, _) => loadingDone.Set();
        reader.StartMonitoring();

        Assert.That(loadingDone.Wait(TimeSpan.FromSeconds(5)), Is.True);
        // Without PreProcess, both lines are visible (header + data)
        Assert.That(reader.LineCount, Is.EqualTo(2), $"Without PreProcess, should see 2 lines. Got {reader.LineCount}");

        // Step 2: Simulate GUI Reload - GUI creates a NEW LogfileReader with PreProcess already set
        // (Reload() → LoadFile() creates a fresh LogfileReader, assigns PreProcess, then StartMonitoring)
        reader.StopMonitoring();

        using ManualResetEventSlim reloadDone = new(false);
        LogfileReader reader2 = new(path, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader2.PreProcessColumnizer = csvColumnizer;
        reader2.LoadingFinished += (_, _) => reloadDone.Set();
        reader2.StartMonitoring();

        Assert.That(reloadDone.Wait(TimeSpan.FromSeconds(5)), Is.True, "Reload should finish within 5 seconds");

        // Diagnostics
        var bufferCount = reader2.BufferIndex.BufferCount;
        Assert.That(bufferCount, Is.GreaterThan(0), "BufferIndex should have at least 1 buffer after ReadFiles");

        // After reload with PreProcess, header is dropped
        Assert.That(reader2.LineCount, Is.EqualTo(1), $"After reload with PreProcess, header dropped, 1 data line. Got {reader2.LineCount}. BufferCount={bufferCount}");

        var line = reader2.GetLogLineMemory(0);
        Assert.That(line, Is.Not.Null, "Data line should not be null after reload");
        Assert.That(line.FullLine.IsEmpty, Is.False, "Data line content should NOT be empty after reload");
        Assert.That(line.FullLine.ToString(), Does.Contain("comma file"), $"Data line should contain 'comma file', got: '{line.FullLine}'");

        // Step 3: Simulate GUI calling Selected + SplitLine
        var callbackMock = new Mock<ILogLineMemoryColumnizerCallback>();
        _ = callbackMock.Setup(c => c.GetLogLineMemory(0)).Returns(reader2.GetLogLineMemory(0));
        csvColumnizer.Selected(callbackMock.Object);

        Assert.That(csvColumnizer.GetColumnCount(), Is.EqualTo(3));

        var splitResult = csvColumnizer.SplitLine(callbackMock.Object, line);
        Assert.That(splitResult.ColumnValues.Length, Is.EqualTo(3));
        Assert.That(splitResult.ColumnValues[0].FullValue.ToString(), Is.EqualTo("2021-01-01"));
        Assert.That(splitResult.ColumnValues[1].FullValue.ToString(), Is.EqualTo("Error"));
        Assert.That(splitResult.ColumnValues[2].FullValue.ToString(), Is.EqualTo("comma file"));

        reader2.StopMonitoring();
    }

    /// <summary>
    /// Tests the exact GUI scenario: a single file opened with multiFile false, with a PreProcess
    /// columnizer that drops the header line.
    /// </summary>
    [Test]
    public void LogfileReader_CommaCsv_SingleFile_WithPreProcess_DataLineNotEmpty ()
    {
        var path = Path.Join(AppDomain.CurrentDomain.BaseDirectory, @".\TestData\comma.csv");

        CsvColumnizer.CsvColumnizer csvColumnizer = new();

        using ManualResetEventSlim loadingDone = new(false);

        // multiFile=FALSE, like the real GUI
        LogfileReader reader = new(path, new EncodingOptions(), false, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
        reader.PreProcessColumnizer = csvColumnizer;
        reader.LoadingFinished += (_, _) => loadingDone.Set();
        reader.StartMonitoring();

        Assert.That(loadingDone.Wait(TimeSpan.FromSeconds(5)), Is.True);

        // Header dropped, 1 data line
        Assert.That(reader.LineCount, Is.EqualTo(1), $"Expected 1 data line after header drop. Got {reader.LineCount}");

        var line = reader.GetLogLineMemory(0);
        Assert.That(line, Is.Not.Null, "Line 0 should not be null");
        Assert.That(line.FullLine.IsEmpty, Is.False, "Line 0 should not be empty");
        // This is the critical assertion: line 0 should be the DATA line, not the header
        Assert.That(line.FullLine.ToString(), Does.Contain("comma file"),
            $"Line 0 should be data line containing 'comma file', got: '{line.FullLine}'");
        Assert.That(line.FullLine.ToString(), Does.Not.Contain("Date"),
            $"Line 0 should NOT be the header. Got: '{line.FullLine}'");

        reader.StopMonitoring();
    }

    /// <summary>
    /// Tests a CSV file without trailing newline (header + 1 data line).
    /// Creates a temp file to control exact byte content.
    /// </summary>
    [Test]
    public void LogfileReader_CsvNoTrailingNewline_HeaderDropped_DataLineVisible ()
    {
        // Create temp file: header + 1 data line, NO trailing newline
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Name,Age,City\r\nAlice,30,Berlin");

            CsvColumnizer.CsvColumnizer csvColumnizer = new();

            LogfileReader reader = new(tempFile, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
            reader.PreProcessColumnizer = csvColumnizer;
            reader.ReadFiles();

            Assert.That(reader.LineCount, Is.EqualTo(1), $"Header dropped, 1 data line expected. Got {reader.LineCount}");

            var line = reader.GetLogLineMemory(0);
            Assert.That(line, Is.Not.Null, "Data line should not be null");
            Assert.That(line.FullLine.IsEmpty, Is.False, "Data line should not be empty");
            Assert.That(line.FullLine.ToString(), Is.EqualTo("Alice,30,Berlin"), $"Got: '{line.FullLine}'");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Tests a CSV file WITH trailing newline (header + 1 data line + trailing CRLF).
    /// This is the exact scenario described: 3 ReadLineMemory calls where 1st drops header.
    /// </summary>
    [Test]
    public void LogfileReader_CsvWithTrailingNewline_HeaderDropped_DataLineVisible ()
    {
        // Create temp file: header + 1 data line + trailing CRLF
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Name,Age,City\r\nAlice,30,Berlin\r\n");

            CsvColumnizer.CsvColumnizer csvColumnizer = new();

            LogfileReader reader = new(tempFile, new EncodingOptions(), true, 40, 50, new MultiFileOptions(), ReaderType.System, PluginRegistry.PluginRegistry.Instance, 500);
            reader.PreProcessColumnizer = csvColumnizer;
            reader.ReadFiles();

            Assert.That(reader.LineCount, Is.EqualTo(1), $"Header dropped, 1 data line expected. Got {reader.LineCount}");

            var line = reader.GetLogLineMemory(0);
            Assert.That(line, Is.Not.Null, "Data line should not be null");
            Assert.That(line.FullLine.IsEmpty, Is.False, "Data line should not be empty");
            Assert.That(line.FullLine.ToString(), Is.EqualTo("Alice,30,Berlin"), $"Got: '{line.FullLine}'");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion
}