using System.Diagnostics;
using System.Runtime.Versioning;

using LogExpert.Classes;
using LogExpert.Core.Classes.IPC;
using LogExpert.Core.Classes.Persister;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Controls.LogTabWindow;
using LogExpert.UI.Controls.LogWindow;

using Moq;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.CommandLine;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
[SupportedOSPlatform("windows")]
internal sealed class LineNavigationTests : IDisposable
{
    private string _directory;
    private string _fileName;
    private Mock<IConfigManager> _config;
    private Settings _settings;
    private LogTabWindow? _window;

    [SetUp]
    public void SetUp ()
    {
        _directory = Path.Join(Path.GetTempPath(), "LogExpertLineTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _fileName = Path.Join(_directory, "application.log");
        File.WriteAllLines(_fileName, Enumerable.Range(1, 100).Select(i => $"Log line {i}"));
        _settings = new Settings();
        _settings.Preferences.MultiFileOptions = new MultiFileOptions();
        _settings.Preferences.FollowTail = true;
        _settings.Preferences.AskForClose = false;
        _settings.Preferences.AutoPick = false;
        _settings.Preferences.OpenLastFiles = false;
        _settings.Preferences.SaveLocation = SessionSaveLocation.SameDir;
        _config = new Mock<IConfigManager>();
        _config.Setup(c => c.Settings).Returns(_settings);
        _config.Setup(c => c.ActiveConfigDir).Returns(_directory);
        _config.Setup(c => c.ActiveSessionDir).Returns(_directory);
        PluginRegistry.PluginRegistry.Create(_directory, 50);
    }

    [TearDown]
    public void TearDown ()
    {
        _settings.Preferences.SaveSessions = false;
        if (_window != null)
        {
            _window.LogExpertProxy = null;
        }
        _window?.Close();
        _window?.Dispose();
        _window = null;
        Directory.Delete(_directory, true);
    }

    [TestCase(1, 100, 0)]
    [TestCase(42, 100, 41)]
    [TestCase(100, 100, 99)]
    [TestCase(int.MaxValue, 100, 99)]
    [TestCase(1, 0, -1)]
    public void Startup_WithTarget_SelectsLineAfterLoadingAndDisablesTail (int target, int lineCount, int expected)
    {
        File.WriteAllLines(_fileName, Enumerable.Range(1, lineCount).Select(i => $"Log line {i}"));
        var logWindow = Open(target);
        WaitForLoad(logWindow);

        Assert.That(logWindow.CurrentLineNum, Is.EqualTo(expected));
        Assert.That(logWindow.GatherSessionSnapshot().FollowTail, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Startup_SavedPositionAndTail_ExplicitTargetWins (bool savedFollowTail)
    {
        _settings.Preferences.SaveSessions = true;
        Persister.SavePersistenceData(_fileName, new PersistenceData
        {
            FileName = _fileName,
            CurrentLine = 85,
            FirstDisplayedLine = 80,
            FollowTail = savedFollowTail,
            LineCount = 100
        }, _settings.Preferences, _directory);

        var logWindow = Open(3);
        WaitForLoad(logWindow);

        Assert.That(logWindow.CurrentLineNum, Is.EqualTo(2));
        var snapshot = logWindow.GatherSessionSnapshot();
        Assert.That(snapshot.FirstDisplayedLine, Is.LessThanOrEqualTo(2));
        Assert.That(snapshot.FollowTail, Is.False);
    }

    [Test]
    public void ForwardedRequest_AlreadyOpenFile_ReusesWindowAndNavigatesImmediately ()
    {
        var logWindow = Open(null);
        WaitForLoad(logWindow);
        var proxy = new LogExpertProxy(_window!);
        var message = JsonConvert.DeserializeObject<IpcMessage>(
            Program.SerializeCommandIntoNonFormattedJSON([_fileName], true, 70))!;

        Program.SendMessageToProxy(message, proxy);

        Assert.That(_window!.CurrentLogWindow, Is.SameAs(logWindow));
        Assert.That(logWindow.CurrentLineNum, Is.EqualTo(69));
        Assert.That(logWindow.GatherSessionSnapshot().FollowTail, Is.False);
    }

    [Test]
    public void RequestsDuringRestoration_LatestTargetWins ()
    {
        var logWindow = Open(42);
        bool requested = false;
        logWindow.ProgressBarUpdate += (_, progress) =>
        {
            if (!progress.Visible && !requested)
            {
                requested = true;
                _window!.LoadFiles([_fileName], 20);
                _window.LoadFiles([_fileName], 30);
            }
        };

        WaitForLoad(logWindow);

        Assert.That(requested, Is.True);
        Assert.That(logWindow.CurrentLineNum, Is.EqualTo(29));
        Assert.That(_window!.CurrentLogWindow, Is.SameAs(logWindow));
    }

    [Test]
    public void AppendAfterNavigation_KeepsPositionAndTailDisabled ()
    {
        var logWindow = Open(5);
        WaitForLoad(logWindow);

        File.AppendAllText(_fileName, "Appended line\r\n");
        PumpUntil(() => logWindow.GatherSessionSnapshot().LineCount == 101);
        Application.DoEvents();

        Assert.That(logWindow.CurrentLineNum, Is.EqualTo(4));
        Assert.That(logWindow.GatherSessionSnapshot().FollowTail, Is.False);
    }

    [Test]
    public void ReloadAfterUserNavigation_DoesNotReplayTarget ()
    {
        var logWindow = Open(5);
        WaitForLoad(logWindow);
        logWindow.GotoLine(75);
        var finished = false;
        logWindow.ProgressBarUpdate += (_, progress) => finished |= !progress.Visible;

        logWindow.Reload();
        PumpUntil(() => finished);
        WaitForLoad(logWindow);

        Assert.That(logWindow.CurrentLineNum, Is.EqualTo(75));
    }

    [Test]
    public void FailedInitialLoad_RespawnDoesNotReplayTarget ()
    {
        File.Delete(_fileName);
        var logWindow = Open(5);
        bool missing = false;
        logWindow.FileNotFound += (_, _) => missing = true;
        PumpUntil(() => missing);

        File.WriteAllLines(_fileName, Enumerable.Range(1, 100).Select(i => $"Restored line {i}"));
        WaitForLoad(logWindow);

        Assert.That(logWindow.GatherSessionSnapshot().FollowTail, Is.True);
        Assert.That(logWindow.CurrentLineNum, Is.Not.EqualTo(4));
    }

    [Test]
    public void TruncationAfterNavigation_DoesNotReplayTarget ()
    {
        var logWindow = Open(5);
        WaitForLoad(logWindow);
        logWindow.GotoLine(75);

        File.WriteAllLines(_fileName, Enumerable.Range(1, 30).Select(i => $"New line {i}"));
        PumpUntil(() => logWindow.GatherSessionSnapshot().LineCount == 30 && logWindow.CurrentLineNum >= 0);
        Application.DoEvents();

        Assert.That(logWindow.CurrentLineNum, Is.EqualTo(0));
    }

    [Test]
    public void ClosedWindow_DiscardsRequests ()
    {
        var logWindow = Open(5);
        WaitForLoad(logWindow);
        _window!.Close();

        Assert.DoesNotThrow(() => logWindow.RequestGotoLine(42));
    }

    public void Dispose ()
    {
        _window?.Dispose();
    }

    private LogWindow Open (int? targetLine)
    {
        _window = new LogTabWindow([_fileName], 1, false, _config.Object, targetLine)
        {
            ShowInTaskbar = false,
            Opacity = 0
        };
        _window.Show();
        PumpUntil(() => _window.CurrentLogWindow != null);
        return _window.CurrentLogWindow;
    }

    private static void WaitForLoad (LogWindow window)
    {
        var finished = Task.Run(window.WaitForLoadingFinished);
        PumpUntil(() => finished.IsCompleted);
        finished.GetAwaiter().GetResult();
        Application.DoEvents();
    }

    private static void PumpUntil (Func<bool> condition)
    {
        var timeout = Stopwatch.StartNew();
        while (!condition() && timeout.Elapsed < TimeSpan.FromSeconds(15))
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }
        Assert.That(condition(), Is.True, "The Log Window did not finish the requested operation.");
    }
}
