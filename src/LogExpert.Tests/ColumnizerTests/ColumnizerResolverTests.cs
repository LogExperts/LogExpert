using ColumnizerLib;

using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Config;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.ColumnizerTests;

[TestFixture]
public class ColumnizerResolverTests
{
    private static ILogLineMemoryColumnizer MakeColumnizer (string name)
    {
        var mock = new Mock<ILogLineMemoryColumnizer>();
        _ = mock.Setup(c => c.GetName()).Returns(name);
        return mock.Object;
    }

    private static ColumnizerMaskEntry Mask (string glob, string columnizerName) => new() { Mask = glob, Type = MaskType.Glob, ColumnizerName = columnizerName };

    #region HistoryThenMask (default)

    [Test]
    public void HistoryThenMask_HistoryPresent_HistoryWins ()
    {
        var history = MakeColumnizer("HistC");
        var mask = MakeColumnizer("MaskC");
        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.HistoryThenMask,
            FileName = "x.log",
            ShortFileName = "x.log",
            MaskList = [Mask("*.log", "MaskC")],
            HistoryLookup = _ => "HistC",
            Registered = [history, mask],
        });

        Assert.That(winner, Is.SameAs(history));
    }

    [Test]
    public void HistoryThenMask_HistoryAbsent_MaskWins ()
    {
        var mask = MakeColumnizer("MaskC");
        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.HistoryThenMask,
            FileName = "x.log",
            ShortFileName = "x.log",
            MaskList = [Mask("*.log", "MaskC")],
            HistoryLookup = _ => null,
            Registered = [mask],
        });

        Assert.That(winner, Is.SameAs(mask));
    }

    [Test]
    public void HistoryThenMask_BothAbsent_AutoPickFires ()
    {
        var auto = MakeColumnizer("AutoC");
        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.HistoryThenMask,
            FileName = "x.log",
            ShortFileName = "x.log",
            AutoPick = () => auto,
            Registered = [auto],
        });

        Assert.That(winner, Is.SameAs(auto));
    }

    [Test]
    public void HistoryThenMask_AllSourcesNull_ReturnsNull ()
    {
        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.HistoryThenMask,
            FileName = "x.log",
            ShortFileName = "x.log",
        });

        Assert.That(winner, Is.Null);
    }

    [Test]
    public void HistoryThenMask_PersistencePresent_BeatsHistoryAndMask ()
    {
        var pers = MakeColumnizer("PersC");
        var hist = MakeColumnizer("HistC");
        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.HistoryThenMask,
            FileName = "x.log",
            ShortFileName = "x.log",
            PersistenceColumnizerName = "PersC",
            HistoryLookup = _ => "HistC",
            Registered = [pers, hist],
        });

        Assert.That(winner, Is.SameAs(pers));
    }

    #endregion

    #region MaskThenHistory

    [Test]
    public void MaskThenHistory_MaskPresent_MaskBeatsHistory ()
    {
        var mask = MakeColumnizer("MaskC");
        var hist = MakeColumnizer("HistC");
        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.MaskThenHistory,
            FileName = "x.log",
            ShortFileName = "x.log",
            MaskList = [Mask("*.log", "MaskC")],
            HistoryLookup = _ => "HistC",
            Registered = [mask, hist],
        });

        Assert.That(winner, Is.SameAs(mask));
    }

    [Test]
    public void MaskThenHistory_PersistenceStillBeatsMask ()
    {
        var pers = MakeColumnizer("PersC");
        var mask = MakeColumnizer("MaskC");
        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.MaskThenHistory,
            FileName = "x.log",
            ShortFileName = "x.log",
            MaskList = [Mask("*.log", "MaskC")],
            PersistenceColumnizerName = "PersC",
            Registered = [pers, mask],
        });

        Assert.That(winner, Is.SameAs(pers));
    }

    #endregion

    #region MaskOverridesPersistence

    [Test]
    public void MaskOverridesPersistence_MaskPresent_BeatsPersistence ()
    {
        var pers = MakeColumnizer("PersC");
        var mask = MakeColumnizer("MaskC");
        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.MaskOverridesPersistence,
            FileName = "x.log",
            ShortFileName = "x.log",
            MaskList = [Mask("*.log", "MaskC")],
            PersistenceColumnizerName = "PersC",
            Registered = [pers, mask],
        });

        Assert.That(winner, Is.SameAs(mask));
    }

    [Test]
    public void MaskOverridesPersistence_MaskAbsent_PersistenceWins ()
    {
        var pers = MakeColumnizer("PersC");
        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.MaskOverridesPersistence,
            FileName = "x.log",
            ShortFileName = "x.log",
            MaskList = [Mask("*.txt", "Other")], // does not match
            PersistenceColumnizerName = "PersC",
            Registered = [pers],
        });

        Assert.That(winner, Is.SameAs(pers));
    }
    #endregion

    #region Stale handling

    [Test]
    public void StaleEntryFirst_ValidEntryWins_OnStaleInvokedOnce ()
    {
        var validC = MakeColumnizer("Valid");
        var stale = Mask("*.log", "MissingPlugin");
        var valid = Mask("*.log", "Valid");
        var staleCallbacks = new List<ColumnizerMaskEntry>();

        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.MaskThenHistory,
            FileName = "foo.log",
            ShortFileName = "foo.log",
            MaskList = [stale, valid],
            Registered = [validC],
            OnStaleMaskEntry = e => staleCallbacks.Add(e),
        });

        Assert.That(winner, Is.SameAs(validC));
        Assert.That(staleCallbacks, Has.Count.EqualTo(1));
        Assert.That(staleCallbacks[0], Is.SameAs(stale));
    }

    [Test]
    public void AllStale_MaskReturnsNull_FallsThroughToAutoPick ()
    {
        var auto = MakeColumnizer("AutoC");
        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.MaskThenHistory,
            FileName = "foo.log",
            ShortFileName = "foo.log",
            MaskList = [Mask("*.log", "Gone1"), Mask("*.log", "Gone2")],
            AutoPick = () => auto,
            Registered = [auto],
        });

        Assert.That(winner, Is.SameAs(auto));
    }

    [Test]
    public void AutoPickDoesNotOverrideExplicitMatch ()
    {
        var hist = MakeColumnizer("HistC");
        var auto = MakeColumnizer("AutoC");
        var autoFired = false;
        var winner = ColumnizerResolver.Resolve(new ResolveInputs
        {
            Priority = ColumnizerSelectionPriority.HistoryThenMask,
            FileName = "x.log",
            ShortFileName = "x.log",
            HistoryLookup = _ => "HistC",
            AutoPick = () =>
            {
                autoFired = true;
                return auto;
            },
            Registered = [hist, auto],
        });

        Assert.That(winner, Is.SameAs(hist));
        Assert.That(autoFired, Is.False);
    }

    #endregion
}