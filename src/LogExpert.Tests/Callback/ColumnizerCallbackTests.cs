using ColumnizerLib;

using LogExpert.Core.Callback;
using LogExpert.Core.Interfaces;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Callback;

/// <summary>
/// Pins the Log Line Source seam: a columnizer callback runs against the three-member
/// <see cref="ILogLineSource"/> role, so a hand-rolled fake is all a Core test needs —
/// no Log Window, no Logfile Reader.
/// </summary>
[TestFixture]
public class ColumnizerCallbackTests
{
    private sealed class FakeLineSource : ILogLineSource
    {
        public List<string> Lines { get; } = [];

        public string FileNamePrefix { get; init; } = "file-of-line-";

        public int LineCount => Lines.Count;

        public string GetCurrentFileName (int lineNum) => FileNamePrefix + lineNum;

        public ILogLineMemory GetLineMemory (int lineNum)
        {
            if (lineNum < 0 || lineNum >= Lines.Count)
            {
                return null;
            }

            var mock = new Mock<ILogLineMemory>();
            _ = mock.Setup(l => l.FullLine).Returns(Lines[lineNum].AsMemory());
            return mock.Object;
        }
    }

    [Test]
    public void GetLineCount_ReadsTheLineSourceDirectly ()
    {
        var source = new FakeLineSource { Lines = { "a", "b", "c" } };
        var callback = new ColumnizerCallback(source);

        Assert.That(callback.GetLineCount(), Is.EqualTo(3));

        source.Lines.Add("d");
        Assert.That(callback.GetLineCount(), Is.EqualTo(4), "line count must be read live, not captured");
    }

    [Test]
    public void GetFileName_UsesTheCurrentLineNum ()
    {
        var callback = new ColumnizerCallback(new FakeLineSource());

        callback.SetLineNum(7);

        Assert.That(callback.GetFileName(), Is.EqualTo("file-of-line-7"));
    }

    [Test]
    public void GetLogLineMemory_DelegatesToTheSource ()
    {
        var source = new FakeLineSource { Lines = { "first", "second" } };
        var callback = new ColumnizerCallback(source);

        Assert.Multiple(() =>
        {
            Assert.That(callback.GetLogLineMemory(1).FullLine.ToString(), Is.EqualTo("second"));
            Assert.That(callback.GetLogLineMemory(99), Is.Null);
        });
    }

    [Test]
    public void Clone_KeepsSourceAndLineNum ()
    {
        var source = new FakeLineSource { Lines = { "only" } };
        var callback = new ColumnizerCallback(source);
        callback.SetLineNum(0);

        var clone = callback.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(clone.LineNum, Is.EqualTo(0));
            Assert.That(clone.GetLogLineMemory(0).FullLine.ToString(), Is.EqualTo("only"));
        });
    }

    [Test]
    public void ColumnizerCallbackMemory_RunsAgainstTheSameThreeMemberRole ()
    {
        var source = new FakeLineSource { Lines = { "x", "y" } };
        var callback = new ColumnizerCallbackMemory(source);

        callback.SetLineNum(1);

        Assert.Multiple(() =>
        {
            Assert.That(callback.GetLineCount(), Is.EqualTo(2));
            Assert.That(callback.GetFileName(), Is.EqualTo("file-of-line-1"));
            Assert.That(callback.GetLogLineMemory(0).FullLine.ToString(), Is.EqualTo("x"));
        });
    }
}
