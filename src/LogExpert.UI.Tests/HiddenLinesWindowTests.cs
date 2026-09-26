using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;

using ColumnizerLib;

using LogExpert.Core.Classes.Highlight;
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

    private string _directory = null!;
    private string _fileName = null!;
    private Settings _settings = null!;
    private Mock<IConfigManager> _config = null!;
    private LogTabWindow? _window;
    private Exception? _uiException;

    private HighlightEntry HideRule => _settings.Preferences.HighlightGroupList[0].HighlightEntryList[0];

    [SetUp]
    public void SetUp ()
    {
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
