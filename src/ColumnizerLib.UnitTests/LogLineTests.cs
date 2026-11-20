using System.Runtime.CompilerServices;

using ColumnizerLib;

using NUnit.Framework;

namespace LogExpert.ColumnizerLib.Tests;

[TestFixture]
public class LogLineTests
{
    [Test]
    public void LogLine_StoresTheLineOnce ()
    {
        // One ReadOnlyMemory<char> plus the line number; every resident line pays for this.
        Assert.That(Unsafe.SizeOf<LogLine>(), Is.LessThan(2 * Unsafe.SizeOf<ReadOnlyMemory<char>>()));
    }

    [Test]
    public void LogLine_TextIsTheFullLine ()
    {
        var memory = "2026-11-20 INFO started".AsMemory(11);
        var line = new LogLine(memory, 7);

        Assert.Multiple(() =>
        {
            Assert.That(line.Text.Equals(line.FullLine), Is.True);
            Assert.That(line.Text.ToString(), Is.EqualTo("INFO started"));
            Assert.That(line.LineNumber, Is.EqualTo(7));
            Assert.That(line, Is.EqualTo(new LogLine(memory, 7)));
        });
    }
}