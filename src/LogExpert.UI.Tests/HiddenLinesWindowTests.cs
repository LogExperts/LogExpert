using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;

using ColumnizerLib;

using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogTabWindow;
using LogExpert.UI.Controls.LogWindow;

using Moq;

using NUnit.Framework;

namespace LogExpert.UI.Tests;

/// <summary>Hide-line highlight rules (#338) in a real Log Window: odd lines are "DEBUG" and hidden.</summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public sealed class HiddenLinesWindowTests : IDisposable
{
    private const int LINE_COUNT = 20;
    private static readonly DateTime SyncStart = new(2026, 1, 1, 10, 0, 0);

    private string _directory = null!;
    private string _fileName = null!;
    private Settings _settings = null!;
    private Mock<IConfigManager> _config = null!;
    private LogTabWindow? _window;
    private Exception? _uiException;
    private WinFormsSynchronizationScope? _synchronization;

    private HighlightEntry HideRule => _settings.Preferences.HighlightGroupList[0].HighlightEntryList[0];

    [SetUp]
    public void SetUp ()
    {
        _synchronization = new WinFormsSynchronizationScope();
        _uiException = null;
        _directory = Path.Join(Path.GetTempPath(), "LogExpertHiddenLinesTests", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(_directory);
        _fileName = Path.Join(_directory, "hidden.log");
        File.WriteAllLines(_fileName, Enumerable.Range(0, LINE_COUNT).Select(Text));
        _settings = new Settings();
        _settings.Preferences.MultiFileOptions = new MultiFileOptions();
        _settings.Preferences.FollowTail = false;
        _settings.Preferences.AskForClose = false;
        _settings.Preferences.AutoPick = false;
        _settings.Preferences.OpenLastFiles = false;
        _settings.Preferences.SaveSessions = false;
        _settings.Preferences.SaveLocation = SessionSaveLocation.SameDir;
        _settings.Preferences.HighlightGroupList = [new HighlightGroup
        {
            GroupName = "hide",
            HighlightEntryList =
            [
                new HighlightEntry { SearchText = "DEBUG", IsHideLine = true },
                new HighlightEntry { SearchText = "INFO", BackgroundColor = Color.LightGreen }
            ]
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
            _synchronization?.Dispose();
        }

        Directory.Delete(_directory, true);
    }

    public void Dispose ()
    {
        _window?.Dispose();
    }

    private static string Text (int line) => line % 2 == 1 ? $"DEBUG {line}" : $"INFO {line}";

    [Test]
    public void Load_HideRule_RemovesMatchingRows_AndKeepsOriginalLineNumbers ()
    {
        var log = Open();
        var grid = Grid(log);

        Assert.That(grid.RowCount, Is.EqualTo(LINE_COUNT / 2));
        Assert.That(DisplayedLineNumber(grid, 0), Is.EqualTo("1"));
        Assert.That(DisplayedLineNumber(grid, 1), Is.EqualTo("3"));
        Assert.That(log.HiddenLineCount, Is.EqualTo(LINE_COUNT / 2));
        Assert.That(Find<Label>(log, "hiddenLinesLabel").Text, Does.Contain("10"));
        Assert.That(Find<Panel>(log, "hiddenLinesBar").Visible, Is.True);
    }

    [Test]
    public void Load_ShowsNoRowsUntilTheFirstScanHasFinished ()
    {
        File.WriteAllLines(_fileName, Enumerable.Range(0, 20_000).Select(Text));
        _window = new LogTabWindow([_fileName], 1, false, _config.Object) { ShowInTaskbar = false, Opacity = 0 };
        Find<WeifenLuo.WinFormsUI.Docking.DockPanel>(_window, "dockPanel").ShowDocumentIcon = false;
        _window.Show();
        PumpUntil(() => _window.CurrentLogWindow != null);
        var log = _window.CurrentLogWindow;
        var grid = Grid(log);
        var largest = 0;
        grid.RowsAdded += (_, _) => largest = Math.Max(largest, grid.RowCount);

        PumpUntil(() => grid.RowCount == 10_000 && log.WhenLineVisibilityIdle().IsCompleted);

        Assert.That(largest, Is.EqualTo(10_000), "hidden lines must never be shown while the first scan runs");
    }

    [Test]
    public void GotoLine_BeforeTheFirstScanHasFinished_IsAppliedOnceItHas ()
    {
        var log = OpenBeforeTheFirstScan(Text);

        log.GotoLine(12_345);
        PumpUntil(() => log.CurrentLineNum == 12_345);

        Assert.That(log.ShowHiddenLines, Is.True);
    }

    [Test]
    public void BookmarkWindowNavigation_BeforeTheFirstScanHasFinished_IsAppliedOnceItHas ()
    {
        var log = OpenBeforeTheFirstScan(Text);

        log.SelectAndEnsureVisible(12_345, false);
        PumpUntil(() => log.CurrentLineNum == 12_345);

        Assert.That(log.ShowHiddenLines, Is.True);
    }

    [Test]
    public void TimestampNavigation_BeforeTheFirstScanHasFinished_IsAppliedOnceItHas ()
    {
        var log = OpenTimestampedBeforeTheFirstScan();

        var scrolled = log.ScrollToTimestamp(SyncStart.AddSeconds(12_345), false, true);
        PumpUntil(() => log.CurrentLineNum == 12_345);

        Assert.That(scrolled, Is.False, "the navigation was only queued");
        Assert.That(log.ShowHiddenLines, Is.True);
    }

    [Test]
    public void TimeSyncFollower_BeforeTheFirstScanHasFinished_SelectsTheNearestVisibleLineOnceItHas ()
    {
        var log = OpenTimestampedBeforeTheFirstScan();

        _ = log.ScrollToTimestamp(SyncStart.AddSeconds(12_345), false, false);
        PumpUntil(() => log.CurrentLineNum == 12_346);

        Assert.That(log.ShowHiddenLines, Is.False);
    }

    [TestCase(12, 12)]
    [TestCase(13, 14)]
    public void SavedPosition_IsRestoredOnceTheFirstScanHasFinished (int savedLine, int expectedLine)
    {
        _settings.Preferences.SaveSessions = true;
        _ = Persister.SavePersistenceData(_fileName, new PersistenceData
        {
            FileName = _fileName,
            CurrentLine = savedLine,
            FirstDisplayedLine = savedLine,
            FollowTail = false,
            HighlightGroupName = "hide",
            LineCount = LINE_COUNT
        }, _settings.Preferences, _directory);

        var log = Open();

        Assert.That(log.CurrentLineNum, Is.EqualTo(expectedLine));
        Assert.That(log.ShowHiddenLines, Is.False);
    }

    [Test]
    public void TimeSync_FollowerOnHiddenLine_SelectsTheNearestVisibleLine ()
    {
        var log = OpenTimestamped();

        _ = log.ScrollToTimestamp(new DateTime(2026, 1, 1, 10, 0, 5), false, false);

        Assert.That(log.ShowHiddenLines, Is.False);
        Assert.That(log.CurrentLineNum, Is.EqualTo(6));
    }

    [Test]
    public void TimestampNavigation_InTheOriginWindow_RevealsTheHiddenLine ()
    {
        var log = OpenTimestamped();

        _ = log.ScrollToTimestamp(new DateTime(2026, 1, 1, 10, 0, 5), false, true);

        Assert.That(log.ShowHiddenLines, Is.True);
        Assert.That(log.CurrentLineNum, Is.EqualTo(5));
    }

    [Test]
    public void NoticeBar_CentersTheCountBesideTheCheckBox ()
    {
        var log = Open();
        var bar = Find<Panel>(log, "hiddenLinesBar");
        var label = Find<Label>(log, "hiddenLinesLabel");
        var checkBox = Find<CheckBox>(log, "showHiddenLinesCheckBox");
        bar.PerformLayout();

        Assert.That(label.Height, Is.EqualTo(bar.ClientSize.Height - bar.Padding.Vertical));
        Assert.That(label.TextAlign, Is.EqualTo(ContentAlignment.MiddleLeft));
        Assert.That(label.Top + (label.Height / 2), Is.EqualTo(checkBox.Top + (checkBox.Height / 2)).Within(1));
        Assert.That(label.Right, Is.LessThanOrEqualTo(checkBox.Left));
        Assert.That(label.PreferredWidth, Is.LessThan(label.Width), "the count must not run into the check box");

        using var image = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(image, bar.ClientRectangle);
        var path = Path.Join(TestContext.CurrentContext.WorkDirectory, "hidden-lines-bar.png");
        image.Save(path);
        TestContext.AddTestAttachment(path);
    }

    [Test]
    public void ShowHiddenLines_ShowsEveryLine_WithoutChangingTheRules ()
    {
        var log = Open();
        var grid = Grid(log);

        Find<CheckBox>(log, "showHiddenLinesCheckBox").Checked = true;
        Assert.That(grid.RowCount, Is.EqualTo(LINE_COUNT));
        Assert.That(DisplayedLineNumber(grid, 1), Is.EqualTo("2"));
        Assert.That(HideRule.IsHideLine, Is.True);
        Assert.That(log.HiddenLineCount, Is.EqualTo(LINE_COUNT / 2));

        Find<CheckBox>(log, "showHiddenLinesCheckBox").Checked = false;
        Assert.That(grid.RowCount, Is.EqualTo(LINE_COUNT / 2));
    }

    [Test]
    public void GotoLine_HiddenTarget_RevealsAndSelectsThatExactLine ()
    {
        var log = Open();

        log.GotoLine(5);

        Assert.That(log.ShowHiddenLines, Is.True);
        Assert.That(log.CurrentLineNum, Is.EqualTo(5));
        Assert.That(Find<CheckBox>(log, "showHiddenLinesCheckBox").Checked, Is.True);
    }

    [Test]
    public void GotoLine_VisibleTarget_KeepsLinesHidden ()
    {
        var log = Open();

        log.GotoLine(6);

        Assert.That(log.ShowHiddenLines, Is.False);
        Assert.That(log.CurrentLineNum, Is.EqualTo(6));
        Assert.That(Grid(log).CurrentCellAddress.Y, Is.EqualTo(3));
    }

    [Test]
    public void TurningTheOverrideOff_WhileOnAHiddenLine_SelectsTheNextVisibleLine ()
    {
        var log = Open();
        log.GotoLine(5);

        log.ShowHiddenLines = false;

        Assert.That(log.CurrentLineNum, Is.EqualTo(6));
    }

    [Test]
    public void RuleChange_SelectedLineStaysVisible_KeepsItsSelection ()
    {
        var log = Open();
        log.GotoLine(8);

        HideRule.SearchText = "INFO 4";
        log.SetCurrentHighlightGroup("hide");
        WaitForVisibility(log);

        Assert.That(Grid(log).RowCount, Is.EqualTo(LINE_COUNT - 1));
        Assert.That(log.CurrentLineNum, Is.EqualTo(8));
    }

    [Test]
    public void RuleChange_SelectedLineBecomesHidden_SelectsTheNextVisibleLine ()
    {
        var log = Open();
        log.GotoLine(4);

        HideRule.SearchText = "INFO 4";
        log.SetCurrentHighlightGroup("hide");
        WaitForVisibility(log);

        Assert.That(log.CurrentLineNum, Is.EqualTo(5));
    }

    [Test]
    public void RuleChange_LastLineBecomesHidden_SelectsThePreviousVisibleLine ()
    {
        var log = Open();
        log.GotoLine(18);

        HideRule.SearchText = "8";
        log.SetCurrentHighlightGroup("hide");
        WaitForVisibility(log);

        Assert.That(log.CurrentLineNum, Is.EqualTo(19));
        HideRule.SearchText = "9";
        log.SetCurrentHighlightGroup("hide");
        WaitForVisibility(log);

        Assert.That(log.CurrentLineNum, Is.EqualTo(18));
    }

    [Test]
    public void RemovingTheHideFlag_ShowsEveryLine_AndHidesTheNoticeBar ()
    {
        var log = Open();

        HideRule.IsHideLine = false;
        log.SetCurrentHighlightGroup("hide");
        WaitForVisibility(log);

        Assert.That(Grid(log).RowCount, Is.EqualTo(LINE_COUNT));
        Assert.That(Find<Panel>(log, "hiddenLinesBar").Visible, Is.False);
    }

    [Test]
    public void AllLinesHidden_LeavesAnEmptySelection_AndNavigationStillReveals ()
    {
        var log = Open();

        HideRule.SearchText = " ";
        log.SetCurrentHighlightGroup("hide");
        WaitForVisibility(log);

        Assert.That(Grid(log).RowCount, Is.Zero);
        Assert.That(log.CurrentLineNum, Is.EqualTo(-1));
        Assert.That(log.HiddenLineCount, Is.EqualTo(LINE_COUNT));

        log.GotoLine(3);
        Assert.That(log.CurrentLineNum, Is.EqualTo(3));
    }

    [Test]
    public void FollowTail_TracksTheLastVisibleLine_WithoutRevealingAppendedHiddenLines ()
    {
        var log = Open();
        var grid = Grid(log);
        log.FollowTailChanged(true, false);

        File.AppendAllLines(_fileName, [Text(20), Text(21), Text(22), Text(23)]);
        PumpUntil(() => log.GatherSessionSnapshot().LineCount == 24);
        PumpUntil(() => grid.RowCount == 12);

        Assert.That(log.ShowHiddenLines, Is.False);
        Assert.That(DisplayedLineNumber(grid, 11), Is.EqualTo("23"));
        Assert.That(grid.FirstDisplayedScrollingRowIndex + grid.DisplayedRowCount(false), Is.GreaterThanOrEqualTo(grid.RowCount));
    }

    [Test]
    public void Reload_KeepsTheSelectedOriginalLine_AndHidesAgain ()
    {
        var log = Open();
        log.GotoLine(14);
        var finished = false;
        log.ProgressBarUpdate += (_, progress) => finished |= !progress.Visible;

        log.Reload();
        PumpUntil(() => finished);
        var loaded = Task.Run(log.WaitForLoadingFinished);
        PumpUntil(() => loaded.IsCompleted);
        WaitForVisibility(log);

        Assert.That(Grid(log).RowCount, Is.EqualTo(LINE_COUNT / 2));
        Assert.That(log.CurrentLineNum, Is.EqualTo(14));
    }

    [Test]
    public void TailTriggers_StillFireOnHiddenLines_WithoutRevealingThem ()
    {
        _settings.Preferences.HighlightGroupList[0].HighlightEntryList.Add(new HighlightEntry { SearchText = "DEBUG 21", IsStopTail = true, IsSetBookmark = true });
        var log = Open();
        log.FollowTailChanged(true, false);

        File.AppendAllLines(_fileName, [Text(20), Text(21), Text(22)]);
        PumpUntil(() => log.GatherSessionSnapshot().LineCount == 23);
        PumpUntil(() => log.BookmarkData.IsBookmarkAtLine(21));
        PumpFor(TimeSpan.FromMilliseconds(100));

        Assert.That(log.GatherSessionSnapshot().FollowTail, Is.False);
        Assert.That(log.ShowHiddenLines, Is.False);
        Assert.That(log.CurrentLineNum, Is.EqualTo(22), "stop-tail scrolls to the next visible line");
    }

    [Test]
    public void CopySelectedRows_CopiesOnlyTheSelectedVisibleLines ()
    {
        var log = Open();
        var grid = Grid(log);
        grid.ClearSelection();
        grid.Rows[0].Selected = true;
        grid.Rows[1].Selected = true;
        Clipboard.Clear();

        _ = typeof(LogWindow).GetMethod("CopyMarkedLinesToClipboard", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(log, null);

        var text = Clipboard.GetText();
        Assert.That(text, Does.Contain("INFO 0"));
        Assert.That(text, Does.Contain("INFO 2"));
        Assert.That(text, Does.Not.Contain("DEBUG 1"));
    }

    [Test]
    public void SessionSnapshot_StoresOriginalLines ()
    {
        var log = Open();

        log.GotoLine(12);
        var snapshot = log.GatherSessionSnapshot();

        Assert.That(snapshot.CurrentLine, Is.EqualTo(12));
        Assert.That(snapshot.FirstDisplayedLine, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(12));
        Assert.That(snapshot.FirstDisplayedLine % 2, Is.Zero, "first displayed line must be a visible original line");
    }

    [Test]
    public void LogSearch_HitOnHiddenLine_RevealsIt ()
    {
        var log = Open();

        _window!.SearchParams.SearchText = "DEBUG 7";
        _window.SearchParams.IsFindNext = false;
        log.StartSearch();
        PumpUntil(() => log.CurrentLineNum == 7);

        Assert.That(log.ShowHiddenLines, Is.True);
    }

    [Test]
    public void ToggleBookmark_BookmarksTheOriginalLineOfTheCurrentRow ()
    {
        var log = Open();
        log.GotoLine(8);

        log.ToggleBookmark();

        Assert.That(log.BookmarkData.IsBookmarkAtLine(8), Is.True);
        Assert.That(log.BookmarkData.IsBookmarkAtLine(4), Is.False);
    }

    [Test]
    public void JumpNextBookmark_ToHiddenLine_RevealsIt ()
    {
        var log = Open();
        log.ToggleBookmark(9);
        log.GotoLine(2);

        log.JumpNextBookmark();

        Assert.That(log.CurrentLineNum, Is.EqualTo(9));
        Assert.That(log.ShowHiddenLines, Is.True);
    }

    [Test]
    public void WindowFilter_StillFindsHiddenLines ()
    {
        var log = Open();
        if (!Find<Button>(log, "filterSearchButton").Visible)
        {
            log.ToggleFilterPanel();
        }

        PumpUntil(() => Find<Button>(log, "filterSearchButton").Visible && Find<Button>(log, "filterSearchButton").Enabled);
        Find<ComboBox>(log, "filterComboBox").Text = "DEBUG";
        Find<Button>(log, "filterSearchButton").PerformClick();

        var filterGrid = Find<DataGridView>(log, "filterGridView");
        PumpUntil(() => filterGrid.RowCount == LINE_COUNT / 2);
        Assert.That(Grid(log).RowCount, Is.EqualTo(LINE_COUNT / 2));
    }

    [Test]
    public void FilterTab_HidesLinesThroughItsOwnGroup_AndLocateRevealsTheHiddenOriginalLine ()
    {
        _settings.Preferences.HighlightGroupList.Add(new HighlightGroup
        {
            GroupName = "hide-some",
            HighlightEntryList = [new HighlightEntry { SearchText = "DEBUG (3|7|11|15|19)$", IsRegex = true, IsHideLine = true }]
        });
        var origin = Open();
        if (!Find<Button>(origin, "filterSearchButton").Visible)
        {
            origin.ToggleFilterPanel();
        }

        PumpUntil(() => Find<Button>(origin, "filterSearchButton").Visible && Find<Button>(origin, "filterSearchButton").Enabled);
        Find<ComboBox>(origin, "filterComboBox").Text = "DEBUG";
        Find<Button>(origin, "filterSearchButton").PerformClick();
        var filterGrid = Find<DataGridView>(origin, "filterGridView");
        PumpUntil(() => filterGrid.RowCount == LINE_COUNT / 2 && Find<Button>(origin, "filterSearchButton").Enabled);

        MenuItem(origin, "filterToTabToolStripMenuItem").PerformClick();
        PumpUntil(() => _window!.CurrentLogWindow is { FilterPipe: not null });
        var tab = _window!.CurrentLogWindow;
        var loaded = Task.Run(tab.WaitForLoadingFinished);
        PumpUntil(() => loaded.IsCompleted);
        tab.SetCurrentHighlightGroup("hide-some");
        WaitForVisibility(tab);

        var tabGrid = Grid(tab);
        Assert.That(tabGrid.RowCount, Is.EqualTo(5));
        Assert.That(DisplayedLineNumber(tabGrid, 0), Is.EqualTo("1"));
        Assert.That(DisplayedLineNumber(tabGrid, 1), Is.EqualTo("3"));
        Assert.That(tab.HiddenLineCount, Is.EqualTo(5));

        tab.GotoLine(2);
        var locate = MenuItem(tab, "locateLineInOriginalFileToolStripMenuItem");
        locate.Enabled = true;
        locate.PerformClick();
        PumpUntil(() => _window.CurrentLogWindow == origin);

        Assert.That(origin.ShowHiddenLines, Is.True);
        Assert.That(origin.CurrentLineNum, Is.EqualTo(5));
    }

    [Test]
    public void Truncation_WhileTailing_RecomputesHiddenLinesForTheNewContent ()
    {
        var log = Open();
        var grid = Grid(log);
        log.GotoLine(14);

        File.WriteAllLines(_fileName, ["DEBUG a", "INFO b", "INFO c", "DEBUG d", "INFO e", "DEBUG f", "DEBUG g"]);
        PumpUntil(() => log.GatherSessionSnapshot().LineCount == 7);
        WaitForVisibility(log);
        PumpUntil(() => grid.RowCount == 3);

        Assert.That(log.HiddenLineCount, Is.EqualTo(4));
        Assert.That(DisplayedLineNumber(grid, 0), Is.EqualTo("2"));
        Assert.That(DisplayedLineNumber(grid, 1), Is.EqualTo("3"));
        Assert.That(DisplayedLineNumber(grid, 2), Is.EqualTo("5"));
        Assert.That(log.ShowHiddenLines, Is.False);
        Assert.That(log.CurrentLineNum, Is.AnyOf(1, 2, 4));
        Assert.That(grid.CurrentCellAddress.Y, Is.LessThan(grid.RowCount));
    }

    [Test]
    public void MultiFileRollover_ShiftsHiddenLinesWithTheDroppedFile ()
    {
        File.WriteAllLines(_fileName + ".1", Enumerable.Range(0, 9).Select(Text));
        File.WriteAllLines(_fileName, Enumerable.Range(9, 10).Select(Text));
        var log = Open();
        var finished = false;
        log.ProgressBarUpdate += (_, progress) => finished |= !progress.Visible;
        log.SwitchMultiFile(true);
        PumpUntil(() => finished);
        var loaded = Task.Run(log.WaitForLoadingFinished);
        PumpUntil(() => loaded.IsCompleted);
        WaitForVisibility(log);
        var grid = Grid(log);
        PumpUntil(() => log.GatherSessionSnapshot().LineCount == 19 && grid.RowCount == 10);

        var next = Path.Join(_directory, "next.tmp");
        File.WriteAllLines(next, Enumerable.Range(19, 3).Select(Text));
        File.Delete(_fileName + ".1");
        File.Move(_fileName, _fileName + ".1");
        File.Move(next, _fileName);
        PumpUntil(() => log.GatherSessionSnapshot().LineCount == 13);
        WaitForVisibility(log);
        PumpUntil(() => grid.RowCount == 6);

        Assert.That(log.HiddenLineCount, Is.EqualTo(7));
        Assert.That(Enumerable.Range(0, 6).Select(row => DisplayedLineNumber(grid, row)), Is.EqualTo(new[] { "2", "4", "6", "8", "10", "12" }));
        Assert.That(DisplayedText(grid, 0), Is.EqualTo("INFO 10"));
        Assert.That(DisplayedText(grid, 5), Is.EqualTo("INFO 20"));
    }

    [Test]
    public void MarkerClick_OnHiddenLine_RevealsIt ()
    {
        _settings.Preferences.ShowMarkerBar = true;
        var log = Open();
        log.ToggleBookmark(11);
        PumpFor(TimeSpan.FromMilliseconds(600));
        var bar = Find<MarkerBar>(log, "markerBar");

        // The bar raises LineSelected with the original line of the clicked marker; 11 is beyond the 10 visible rows.
        var markers = typeof(LogWindow).GetField("_markerController", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(log)!;
        _ = typeof(MarkerBarController).GetMethod("OnLineSelected", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(markers, [bar, new Core.EventArguments.SelectLineEventArgs(11)]);

        Assert.That(log.CurrentLineNum, Is.EqualTo(11));
        Assert.That(log.ShowHiddenLines, Is.True);
    }

    [Test]
    public void CommandLineTarget_OnHiddenLine_RevealsItAfterVisibilityInitialization ()
    {
        _window = new LogTabWindow([_fileName], 1, false, _config.Object, 8) { ShowInTaskbar = false, Opacity = 0 };
        Find<WeifenLuo.WinFormsUI.Docking.DockPanel>(_window, "dockPanel").ShowDocumentIcon = false;
        _window.Show();
        PumpUntil(() => _window.CurrentLogWindow != null);
        var log = _window.CurrentLogWindow;

        PumpUntil(() => log.CurrentLineNum == 7);

        Assert.That(log.ShowHiddenLines, Is.True);
        Assert.That(log.GatherSessionSnapshot().FollowTail, Is.False);
    }

    [Test]
    public void TimeSync_BeforeTheFirstScanHasFinished_DoesNotReplaceTheUsersQueuedNavigation ()
    {
        var log = OpenTimestampedBeforeTheFirstScan();

        log.GotoLine(12_345);
        _ = log.ScrollToTimestamp(SyncStart.AddSeconds(100), false, false);
        PumpUntil(() => log.CurrentLineNum == 12_345);

        Assert.That(log.ShowHiddenLines, Is.True);
    }

    [Test]
    public void CommandLineTarget_OnVisibleLine_KeepsLinesHidden ()
    {
        _window = new LogTabWindow([_fileName], 1, false, _config.Object, 7) { ShowInTaskbar = false, Opacity = 0 };
        Find<WeifenLuo.WinFormsUI.Docking.DockPanel>(_window, "dockPanel").ShowDocumentIcon = false;
        _window.Show();
        PumpUntil(() => _window.CurrentLogWindow != null);
        var log = _window.CurrentLogWindow;

        PumpUntil(() => log.CurrentLineNum == 6);
        PumpUntil(() => Grid(log).RowCount == LINE_COUNT / 2);

        Assert.That(log.ShowHiddenLines, Is.False);
        Assert.That(log.CurrentLineNum, Is.EqualTo(6));
    }

    [Test, Explicit("Real-file hide-line scan duration, memory and UI responsiveness experiment")]
    public void LargeFile_ReportsScanDurationMemoryAndUiResponsiveness ()
    {
        const int LARGE_LINE_COUNT = 1_000_000;
        File.WriteAllLines(_fileName, Enumerable.Range(0, LARGE_LINE_COUNT)
            .Select(i => $"2026-09-26 12:34:56.{i % 1000:D3} {(i % 2 == 1 ? "DEBUG" : "INFO")} worker-{i % 16} message number {i}"));
        HideRule.IsHideLine = false;
        var log = Open();
        var grid = Grid(log);
        var memoryBefore = GC.GetTotalMemory(true);

        HideRule.IsHideLine = true;
        var elapsed = Stopwatch.StartNew();
        log.SetCurrentHighlightGroup("hide");
        var pump = Stopwatch.StartNew();
        var maximumPump = TimeSpan.Zero;
        while (grid.RowCount != LARGE_LINE_COUNT / 2 && elapsed.Elapsed < TimeSpan.FromSeconds(120))
        {
            pump.Restart();
            PumpOnce();
            maximumPump = maximumPump > pump.Elapsed ? maximumPump : pump.Elapsed;
            Thread.Sleep(1);
        }

        var scan = elapsed.Elapsed;
        Assert.That(grid.RowCount, Is.EqualTo(LARGE_LINE_COUNT / 2));
        var memoryAfter = GC.GetTotalMemory(true);

        elapsed.Restart();
        log.GotoLine(LARGE_LINE_COUNT - 1);
        var reveal = elapsed.Elapsed;
        elapsed.Restart();
        log.ShowHiddenLines = false;
        grid.FirstDisplayedScrollingRowIndex = grid.RowCount / 2;
        PumpOnce();
        var scroll = elapsed.Elapsed;

        TestContext.Progress.WriteLine($"Hide-line scan: {new FileInfo(_fileName).Length} bytes, {LARGE_LINE_COUNT} lines, {log.HiddenLineCount} hidden; " +
            $"scan {scan.TotalMilliseconds:F0} ms; managed delta {memoryAfter - memoryBefore} bytes; maximum UI pump during scan {maximumPump.TotalMilliseconds:F1} ms; " +
            $"reveal hidden last line {reveal.TotalMilliseconds:F0} ms; override off + mid-file scroll {scroll.TotalMilliseconds:F0} ms.");
    }

    private LogWindow Open ()
    {
        _window = new LogTabWindow([_fileName], 1, false, _config.Object) { ShowInTaskbar = false, Opacity = 0 };
        Find<WeifenLuo.WinFormsUI.Docking.DockPanel>(_window, "dockPanel").ShowDocumentIcon = false;
        _window.Show();
        PumpUntil(() => _window.CurrentLogWindow != null);
        var log = _window.CurrentLogWindow;
        var loaded = Task.Run(log.WaitForLoadingFinished);
        PumpUntil(() => loaded.IsCompleted);
        loaded.GetAwaiter().GetResult();
        log.SetCurrentHighlightGroup("hide");
        WaitForVisibility(log);
        return log;
    }

    /// <summary>Opens a 20,000-line file and returns as soon as the Log Window exists, while it still loads and scans.</summary>
    private LogWindow OpenBeforeTheFirstScan (Func<int, string> text)
    {
        File.WriteAllLines(_fileName, Enumerable.Range(0, 20_000).Select(text));
        _window = new LogTabWindow([_fileName], 1, false, _config.Object) { ShowInTaskbar = false, Opacity = 0 };
        Find<WeifenLuo.WinFormsUI.Docking.DockPanel>(_window, "dockPanel").ShowDocumentIcon = false;
        _window.Show();
        PumpUntil(() => _window.CurrentLogWindow != null);
        return _window.CurrentLogWindow;
    }

    private LogWindow OpenTimestampedBeforeTheFirstScan ()
    {
        _settings.Preferences.ColumnizerMaskList = [new ColumnizerMaskEntry { Mask = "hidden.log", ColumnizerName = new TimestampColumnizer().GetName() }];
        var log = OpenBeforeTheFirstScan(i => $"{SyncStart.AddSeconds(i):yyyy-MM-dd HH:mm:ss}.000 {Text(i)}");
        // Nothing is hidden until the first scan has published, so the window can't be navigable yet.
        Assume.That(log.HiddenLineCount, Is.Zero, "the first scan finished before the test could navigate");
        return log;
    }

    private LogWindow OpenTimestamped ()
    {
        File.WriteAllLines(_fileName, Enumerable.Range(0, LINE_COUNT).Select(i => $"2026-01-01 10:00:{i:D2}.000 {Text(i)}"));
        var log = Open();
        log.ForceColumnizer(new TimestampColumnizer());
        WaitForVisibility(log);
        return log;
    }

    private void WaitForVisibility (LogWindow log)
    {
        var idle = log.WhenLineVisibilityIdle();
        PumpUntil(() => idle.IsCompleted);
        PumpFor(TimeSpan.FromMilliseconds(50));
    }

    private static DataGridView Grid (LogWindow log) => Find<DataGridView>(log, "dataGridView");

    private static string DisplayedLineNumber (DataGridView grid, int row)
    {
        return ((IColumnMemory)grid.Rows[row].Cells[1].Value).FullValue.ToString();
    }

    private static ToolStripMenuItem MenuItem (LogWindow log, string name)
    {
        return (ToolStripMenuItem)typeof(LogWindow).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(log)!;
    }

    private static string DisplayedText (DataGridView grid, int row)
    {
        return ((IColumnMemory)grid.Rows[row].Cells[grid.ColumnCount - 1].Value).FullValue.ToString();
    }

    private static TControl Find<TControl> (Control parent, string name) where TControl : Control
    {
        return (TControl)parent.Controls.Find(name, true).Single();
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

    private void PumpFor (TimeSpan duration)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < duration)
        {
            PumpOnce();
            Thread.Sleep(1);
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

        Assert.That(condition(), Is.True, "Timed out waiting for the Log Window.");
    }
}
