using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;

using ColumnizerLib;

using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Marker;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogTabWindow;
using LogExpert.UI.Controls.LogWindow;
using LogExpert.UI.Controls;

using Moq;

using NUnit.Framework;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.Tests.UI;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public sealed class MarkerWindowTests : IDisposable
{
    private string _directory = null!;
    private string _fileName = null!;
    private Settings _settings = null!;
    private Mock<IConfigManager> _config = null!;
    private LogTabWindow? _window;
    private Exception? _uiException;

    [SetUp]
    public void SetUp ()
    {
        _uiException = null;
        _directory = Path.Join(Path.GetTempPath(), "LogExpertMarkerTests", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(_directory);
        _fileName = Path.Join(_directory, "markers.log");
        File.WriteAllLines(_fileName, Enumerable.Range(0, 100).Select(line => line % 10 == 0 || line == 99 ? "ERROR" : "INFO"));
        _settings = new Settings();
        _settings.Preferences.MultiFileOptions = new MultiFileOptions();
        _settings.Preferences.FollowTail = true;
        _settings.Preferences.AskForClose = false;
        _settings.Preferences.AutoPick = false;
        _settings.Preferences.OpenLastFiles = false;
        _settings.Preferences.SaveSessions = false;
        _settings.Preferences.SaveLocation = SessionSaveLocation.SameDir;
        _settings.Preferences.ShowMarkerBar = true;
        _settings.Preferences.HighlightGroupList = [new HighlightGroup
        {
            GroupName = "markers",
            HighlightEntryList = [new HighlightEntry { SearchText = "ERROR", BackgroundColor = Color.Red }]
        }];
        _config = new Mock<IConfigManager>();
        _ = _config.Setup(config => config.Settings).Returns(_settings);
        _ = _config.Setup(config => config.ActiveConfigDir).Returns(_directory);
        _ = _config.Setup(config => config.ActiveSessionDir).Returns(_directory);
        _ = PluginRegistry.PluginRegistry.Create(_directory, 50);
        Application.ThreadException += OnUiException;
    }

    [TearDown]
    public void TearDown ()
    {
        _settings.Preferences.SaveSessions = false;
        try
        {
            if (_window != null)
            {
                _window.LogExpertProxy = null;
                _window.Close();
                _window.Dispose();
                _window = null;
            }
        }
        finally
        {
            Application.ThreadException -= OnUiException;
        }

        Directory.Delete(_directory, true);
    }

    [Test]
    public void Markers_NavigateStopTailAndRefreshRulesVisibilityAndBookmarks ()
    {
        var log = Open();
        var bar = Find<MarkerBar>(log, "markerBar");
        WaitForMarker(bar, MarkerCategory.Highlights, 20, 100, true);
        log.ToggleBookmark(99);
        WaitForMarker(bar, MarkerCategory.Bookmarks, 99, 100, true);
        log.FollowTailChanged(true, false);
        Click(bar, MarkerCategory.Bookmarks, 99, 100);
        Assert.That(log.CurrentLineNum, Is.EqualTo(99));
        Assert.That(log.GatherSessionSnapshot().FollowTail, Is.False, "Clicking the last line must stop tail too.");
        Click(bar, MarkerCategory.Highlights, 20, 100);
        Assert.That(log.CurrentLineNum, Is.EqualTo(20));
        Assert.That(log.GatherSessionSnapshot().FirstDisplayedLine, Is.LessThanOrEqualTo(20));
        log.ToggleBookmark(99);
        WaitForMarker(bar, MarkerCategory.Bookmarks, 99, 100, false);

        var rule = _settings.Preferences.HighlightGroupList[0].HighlightEntryList[0];
        rule.SearchText = "INFO";
        log.SetCurrentHighlightGroup("markers");
        WaitForMarker(bar, MarkerCategory.Highlights, 21, 100, true);
        WaitForMarker(bar, MarkerCategory.Highlights, 20, 100, false);

        var table = (TableLayoutPanel)bar.Parent!;
        log.ForceColumnizer(new TimestampColumnizer());
        _settings.Preferences.ShowTimeSpread = true;
        ApplyPreferences(log);
        table.PerformLayout();
        var timeSpread = table.GetControlFromPosition(1, 1);
        Assert.That(timeSpread, Is.Not.Null);
        Assert.That(timeSpread!.Visible, Is.True);
        Assert.That(bar.Visible, Is.True);
        Assert.That(timeSpread.Bounds.IntersectsWith(bar.Bounds), Is.False);
        var timeSpreadWidth = table.ColumnStyles[1].Width;
        Assert.That(timeSpreadWidth, Is.GreaterThan(0));
        _settings.Preferences.ShowMarkerBar = false;
        ApplyPreferences(log);
        Assert.That(table.ColumnStyles[2].Width, Is.Zero);
        Assert.That(table.ColumnStyles[1].Width, Is.EqualTo(timeSpreadWidth));
        _settings.Preferences.ShowMarkerBar = true;
        ApplyPreferences(log);
        WaitForMarker(bar, MarkerCategory.Highlights, 21, 100, true);

        _window!.Size = new Size(800, 600);
        WaitForMarker(bar, MarkerCategory.Highlights, 21, 100, true);
        using var image = new Bitmap(log.Width, log.Height);
        log.DrawToBitmap(image, log.ClientRectangle);
        var path = Path.Join(TestContext.CurrentContext.WorkDirectory, "marker-window.png");
        image.Save(path);
        TestContext.AddTestAttachment(path);
    }

    [Test]
    public void HiddenLogWindow_ProcessesAppendedMarkersAfterRestore ()
    {
        var log = Open();
        var bar = Find<MarkerBar>(log, "markerBar");
        WaitForMarker(bar, MarkerCategory.Highlights, 20, 100, true);

        log.Hide();
        Assert.Multiple(() =>
        {
            Assert.That(log.DockState, Is.EqualTo(DockState.Hidden));
            Assert.That(log.Visible, Is.False);
        });

        File.AppendAllText(_fileName, "ERROR\r\n");
        PumpUntil(() => log.GatherSessionSnapshot().LineCount == 101);

        log.Show(Find<DockPanel>(_window!, "dockPanel"));
        PumpUntil(() => log.Visible && log.DockState != DockState.Hidden);
        WaitForMarker(bar, MarkerCategory.Highlights, 100, 101, true);
        var expectedTooltip = string.Format(CultureInfo.CurrentCulture, Resources.MarkerBar_ToolTip,
            Resources.MarkerBar_Highlights, 101, 101, 1);
        // The previous final-line marker occupies the same pixel until the batched refresh publishes.
        PumpUntil(() => HoverMarker(bar, MarkerCategory.Highlights, 100, 101).StartsWith(expectedTooltip, StringComparison.Ordinal));
        Click(bar, MarkerCategory.Highlights, 100, 101);
        Assert.That(log.CurrentLineNum, Is.EqualTo(100));
    }

    [Test]
    public void SearchAndFilter_TrackExecutedCriteriaClearAndTailWithoutSpreadLines ()
    {
        var log = Open();
        var bar = Find<MarkerBar>(log, "markerBar");
        _window!.SearchParams.SearchText = "ERROR";
        log.StartSearch();
        WaitForMarker(bar, MarkerCategory.Search, 20, 100, true);
        ((ToolStripMenuItem)bar.ContextMenuStrip!.Items[0]).PerformClick();
        WaitForMarker(bar, MarkerCategory.Search, 20, 100, false);
        _window.SearchParams.IsFindNext = true;
        log.StartSearch();
        PumpFor(TimeSpan.FromMilliseconds(600));
        Assert.That(HasMarker(bar, MarkerCategory.Search, 20, 100), Is.False, "F3 must not restore a cleared marker search.");
        _window.SearchParams.IsFindNext = false;
        _window.SearchParams.SearchText = "INFO";
        log.StartSearch();
        WaitForMarker(bar, MarkerCategory.Search, 21, 100, true);
        WaitForMarker(bar, MarkerCategory.Search, 20, 100, false);

        if (!Find<Button>(log, "filterSearchButton").Visible)
        {
            log.ToggleFilterPanel();
        }
        PumpUntil(() => Find<Button>(log, "filterSearchButton").Visible && Find<Button>(log, "filterSearchButton").Enabled);
        Find<ComboBox>(log, "filterComboBox").Text = _settings.Preferences.HighlightGroupList[0].HighlightEntryList[0].SearchText;
        Find<KnobControl>(log, "knobControlFilterBackSpread").Value = 1;
        Find<KnobControl>(log, "knobControlFilterForeSpread").Value = 1;
        Find<CheckBox>(log, "filterTailCheckBox").Checked = true;
        Find<Button>(log, "filterSearchButton").PerformClick();
        WaitForMarker(bar, MarkerCategory.Filter, 20, 100, true);
        WaitForMarker(bar, MarkerCategory.Filter, 19, 100, false);
        File.AppendAllText(_fileName, "ERROR INFO\r\n");
        PumpUntil(() => log.GatherSessionSnapshot().LineCount == 101);
        WaitForMarker(bar, MarkerCategory.Highlights, 100, 101, true);
        WaitForMarker(bar, MarkerCategory.Search, 100, 101, true);
        WaitForMarker(bar, MarkerCategory.Filter, 100, 101, true);

        Find<ComboBox>(log, "filterComboBox").Text = string.Empty;
        Find<Button>(log, "filterSearchButton").PerformClick();
        WaitForMarker(bar, MarkerCategory.Filter, 20, 101, false);
    }

    [Test]
    public void SavedBookmarksAndTruncation_UseCurrentLogicalLines ()
    {
        _settings.Preferences.SaveSessions = true;
        _ = Persister.SavePersistenceData(_fileName, new PersistenceData
        {
            FileName = _fileName, LineCount = 100,
            BookmarkList = new SortedList<int, Core.Entities.Bookmark> { { 80, new Core.Entities.Bookmark(80) } }
        }, _settings.Preferences, _directory);
        var log = Open();
        var bar = Find<MarkerBar>(log, "markerBar");
        WaitForMarker(bar, MarkerCategory.Bookmarks, 80, 100, true);
        File.WriteAllText(_fileName, "INFO\r\nINFO\r\nERROR\r\n");
        PumpUntil(() => log.GatherSessionSnapshot().LineCount == 3);
        WaitForMarker(bar, MarkerCategory.Highlights, 2, 3, true);
        WaitForMarker(bar, MarkerCategory.Bookmarks, 2, 3, false);
        Click(bar, MarkerCategory.Highlights, 2, 3);
        Assert.That(log.CurrentLineNum, Is.EqualTo(2));
    }

    [Test]
    public void InactiveWindow_KeepsItsOwnSearchWhenAnotherWindowSearches ()
    {
        var first = Open();
        var bar = Find<MarkerBar>(first, "markerBar");
        _window!.SearchParams.SearchText = "ERROR";
        first.StartSearch();
        WaitForMarker(bar, MarkerCategory.Search, 20, 100, true);
        var secondFile = Path.Join(_directory, "second.log");
        File.WriteAllLines(secondFile, Enumerable.Repeat("INFO", 20));
        _window.LoadFiles([secondFile]);
        PumpUntil(() => _window.CurrentLogWindow != first && _window.CurrentLogWindow?.GatherSessionSnapshot().LineCount == 20);
        _window.SearchParams.SearchText = "INFO";
        _window.SearchParams.IsFindNext = false;
        _window.CurrentLogWindow.StartSearch();
        File.AppendAllText(_fileName, "ERROR\r\n");
        PumpUntil(() => first.GatherSessionSnapshot().LineCount == 101);
        _window.LoadFiles([_fileName]);
        PumpUntil(() => _window.CurrentLogWindow == first);
        WaitForMarker(bar, MarkerCategory.Search, 100, 101, true);
        Assert.That(HasMarker(bar, MarkerCategory.Search, 21, 101), Is.False);
    }

    [Test]
    public void JsonWordHighlights_DiscoveryMatchesOffscreenValuesWithoutMutatingTheGridParser ()
    {
        File.WriteAllLines(_fileName, Enumerable.Repeat("{\"message\":\"ok\"}", 999).Append("{\"extra\":\"error\"}"));
        _settings.Preferences.FollowTail = false;
        _settings.Preferences.HighlightGroupList[0].HighlightEntryList = [new HighlightEntry
        {
            SearchText = "^error$", IsRegex = true, IsWordMatch = true, BackgroundColor = Color.Red
        }];
        var log = Open();
        log.RequestGotoLine(1);
        log.ForceColumnizer(new JsonColumnizer.JsonColumnizer());
        var columnNames = log.CurrentColumnizer.GetColumnNames();
        Assert.That(columnNames, Does.Not.Contain("extra"));
        var bar = Find<MarkerBar>(log, "markerBar");
        WaitForMarker(bar, MarkerCategory.Highlights, 999, 1000, true);
        Assert.That(log.CurrentColumnizer.GetColumnNames(), Is.EqualTo(columnNames), "Overview parsing must not mutate the grid parser.");
        Click(bar, MarkerCategory.Highlights, 999, 1000);
        Assert.That(log.CurrentLineNum, Is.EqualTo(999));
    }

    [TestCase("2022-03-21 11:34:34.505[INFO][Worker]Message", "^11:34:34")]
    [TestCase("[one][two][three][four][five][six]Message", @"^\[six\]$")]
    public void SquareBracketColumns_DiscoveryPreservesDetectedLayout (string line, string expression)
    {
        File.WriteAllLines(_fileName, Enumerable.Repeat(line, 100));
        _settings.Preferences.HighlightGroupList[0].HighlightEntryList = [new HighlightEntry
        {
            SearchText = expression, IsRegex = true, IsWordMatch = true, BackgroundColor = Color.Red
        }];
        var log = Open();
        log.ForceColumnizer(new SquareBracketColumnizer());
        _ = ((SquareBracketColumnizer)log.CurrentColumnizer).GetPriority(_fileName, new ILogLineMemory[] { new LogLine(line, 0) });
        log.ColumnizerConfigChanged();
        var bar = Find<MarkerBar>(log, "markerBar");

        WaitForMarker(bar, MarkerCategory.Highlights, 20, 100, true);
        Click(bar, MarkerCategory.Highlights, 20, 100);
        Assert.That(log.CurrentLineNum, Is.EqualTo(20));
    }

    [Test]
    public void Timeshift_RebuildsDisplayedTimestampMatches ()
    {
        File.WriteAllLines(_fileName, Enumerable.Repeat("2022-03-21 11:34:34.505 message", 100));
        _settings.Preferences.HighlightGroupList[0].HighlightEntryList = [new HighlightEntry
        {
            SearchText = "^11:34:34", IsRegex = true, IsWordMatch = true, BackgroundColor = Color.Red
        }];
        var log = Open();
        log.ForceColumnizer(new TimestampColumnizer());
        var bar = Find<MarkerBar>(log, "markerBar");
        WaitForMarker(bar, MarkerCategory.Highlights, 20, 100, true);
        log.TimeshiftEnabled(true, "01:00:00");
        WaitForMarker(bar, MarkerCategory.Highlights, 20, 100, false);
        log.TimeshiftEnabled(false, "01:00:00");
        WaitForMarker(bar, MarkerCategory.Highlights, 20, 100, true);
    }

    [Test, Explicit("Real-file marker responsiveness and memory experiment")]
    public void DenseFile_ReportsDiscoveryAppendAndUiResponsiveness ()
    {
        const int LINE_COUNT = 500_000;
        File.WriteAllLines(_fileName, Enumerable.Repeat("2026-09-16 12:34:56 COMMON-123456 dense marker message", LINE_COUNT));
        _settings.Preferences.HighlightGroupList[0].HighlightEntryList = [
            new HighlightEntry { SearchText = "^absent-a$", IsRegex = true, BackgroundColor = Color.Red },
            new HighlightEntry { SearchText = "^absent-b$", IsRegex = true, BackgroundColor = Color.Blue },
            new HighlightEntry { SearchText = "\\babsent\\b", IsRegex = true, BackgroundColor = Color.Green },
            new HighlightEntry { SearchText = "COMMON-\\d+", IsRegex = true, BackgroundColor = Color.Yellow }
        ];
        _settings.Preferences.ShowMarkerBar = false;
        var log = Open();
        var bar = Find<MarkerBar>(log, "markerBar");
        var memoryBefore = GC.GetTotalMemory(true);
        var elapsed = Stopwatch.StartNew();
        _settings.Preferences.ShowMarkerBar = true;
        ApplyPreferences(log);
        var pump = Stopwatch.StartNew();
        var maximumPump = TimeSpan.Zero;
        var pumpCount = 0;
        while (!HasMarker(bar, MarkerCategory.Highlights, LINE_COUNT - 1, LINE_COUNT) && elapsed.Elapsed < TimeSpan.FromSeconds(90))
        {
            pump.Restart();
            PumpOnce();
            maximumPump = maximumPump > pump.Elapsed ? maximumPump : pump.Elapsed;
            pumpCount++;
            Thread.Sleep(1);
        }

        Assert.That(HasMarker(bar, MarkerCategory.Highlights, LINE_COUNT - 1, LINE_COUNT), Is.True);
        var scanElapsed = elapsed.Elapsed;
        var memoryAfter = GC.GetTotalMemory(true);
        elapsed.Restart();
        File.AppendAllLines(_fileName, Enumerable.Repeat("COMMON-123456 appended", 2_000));
        PumpUntil(() => log.GatherSessionSnapshot().LineCount == LINE_COUNT + 2_000);
        PumpUntil(() => HoverMarker(bar, MarkerCategory.Highlights, LINE_COUNT + 1_999, LINE_COUNT + 2_000).Contains("502000", StringComparison.Ordinal));
        TestContext.Progress.WriteLine($"Real file: {new FileInfo(_fileName).Length} bytes, {LINE_COUNT} + 2000 lines, 4 regex rules; discovery {scanElapsed.TotalMilliseconds:F0} ms; append + discovery {elapsed.Elapsed.TotalMilliseconds:F0} ms; managed delta {memoryAfter - memoryBefore} bytes; UI pumps {pumpCount}, maximum pump {maximumPump.TotalMilliseconds:F1} ms; DPI {log.DeviceDpi}.");
    }

    public void Dispose ()
    {
        _window?.Dispose();
    }

    private LogWindow Open ()
    {
        _window = new LogTabWindow([_fileName], 1, false, _config.Object) { ShowInTaskbar = false, Opacity = 0 };
        // The shell disposes LED icons that DockPanel can still cache; marker checks do not exercise document icons.
        Find<WeifenLuo.WinFormsUI.Docking.DockPanel>(_window, "dockPanel").ShowDocumentIcon = false;
        _window.Show();
        PumpUntil(() => _window.CurrentLogWindow != null);
        var log = _window.CurrentLogWindow;
        var loaded = Task.Run(log.WaitForLoadingFinished);
        PumpUntil(() => loaded.IsCompleted);
        loaded.GetAwaiter().GetResult();
        return log;
    }

    private static void ApplyPreferences (LogWindow log)
    {
        log.PreferencesChanged(log.Preferences.Font, false, 0, false, SettingsFlags.GuiOrColors);
    }

    private static TControl Find<TControl> (Control parent, string name) where TControl : Control
    {
        return (TControl)parent.Controls.Find(name, true).Single();
    }

    private static Point Position (MarkerBar bar, MarkerCategory category, int line, int count)
    {
        var laneIndex = category switch
        {
            MarkerCategory.Highlights => 0,
            MarkerCategory.Bookmarks => 1,
            MarkerCategory.Search => 2,
            MarkerCategory.Filter => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
        return new Point((laneIndex * 2 + 1) * bar.Width / 8, bar.TopInset + (count == 1 ? 0 : (int)((long)line * (bar.BucketHeight - 1) / (count - 1))));
    }

    private static bool HasMarker (MarkerBar bar, MarkerCategory category, int line, int count)
    {
        if (bar.Width <= 0 || bar.BucketHeight <= 0)
        {
            return false;
        }

        using var bitmap = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(bitmap, bar.ClientRectangle);
        var point = Position(bar, category, line, count);
        return bitmap.GetPixel(point.X, point.Y).ToArgb() != bar.BackColor.ToArgb();
    }

    private static void Click (MarkerBar bar, MarkerCategory category, int line, int count)
    {
        var point = Position(bar, category, line, count);
        _ = typeof(Control).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(bar,
            [new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0)]);
    }

    private static string HoverMarker (MarkerBar bar, MarkerCategory category, int line, int count)
    {
        var point = Position(bar, category, line, count);
        _ = typeof(Control).GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(bar,
            [new MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0)]);
        var tooltip = (ToolTip)typeof(MarkerBar).GetField("_toolTip", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(bar)!;
        return tooltip.GetToolTip(bar);
    }

    private void WaitForMarker (MarkerBar bar, MarkerCategory category, int line, int count, bool present)
    {
        PumpUntil(() => HasMarker(bar, category, line, count) == present);
    }

    private void PumpFor (TimeSpan duration)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < duration)
        {
            PumpOnce();
            Thread.Sleep(1);
        }
    }

    private void OnUiException (object sender, ThreadExceptionEventArgs eventArgs)
    {
        _uiException = eventArgs.Exception;
    }

    private void PumpOnce ()
    {
        Application.DoEvents();
        if (_uiException != null)
        {
            ExceptionDispatchInfo.Throw(_uiException);
        }
    }

    private void PumpUntil (Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (!condition() && timer.Elapsed < TimeSpan.FromSeconds(30))
        {
            PumpOnce();
            Thread.Sleep(1);
        }

        Assert.That(condition(), Is.True, "Timed out waiting for the marker UI.");
    }
}