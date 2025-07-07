using LogExpert.Core.Classes.Log;

using NUnit.Framework;

namespace LogExpert.Tests;

[TestFixture]
internal class RollingNameTest
{
    [Test]
    [TestCase("engine_2010-06-12_0.log", "*$D(yyyy-MM-dd)_$I.log")]
    [TestCase("engine_2010-06-12.log", "*$D(yyyy-MM-dd).log")]
    [TestCase("engine_0.log", "*_$I.log")]
    [TestCase("engine.log","*.log$J(.)")]
    [TestCase("engine.log","engine$J.log")]
    [TestCase("engine1.log","engine$J.log")]
    [TestCase("engine.log", "*$J(.)")]
    [TestCase("engine_2010-06-12.log", "*$D(yyyy-MM-dd).log$J(.)")]
    public void TestFilename1(string expectedResult, string formatString)
    {
        RolloverFilenameBuilder fnb = new(formatString);
        fnb.SetFileName(expectedResult);
        var name = fnb.BuildFileName();
        Assert.That(name, Is.EqualTo(expectedResult));
    }

    [Test]
    [TestCase("engine_2010-06-12_0.log", "engine_2010-06-12_1.log", "*$D(yyyy-MM-dd)_$I.log")]
    [TestCase("engine_2010-06-12.log", "engine_2010-06-12.log", "*$D(yyyy-MM-dd).log")]
    [TestCase("engine_0.log", "engine_1.log","*_$I.log")]
    [TestCase("engine.log", "engine.log.1","*.log$J(.)")]
    [TestCase("engine.log", "engine1.log","engine$J.log")]
    [TestCase("engine1.log", "engine2.log","engine$J.log")]
    [TestCase("engine.log", "engine.log.1","*$J(.)")]
    [TestCase("engine_2010-06-12.log", "engine_2010-06-12.log.1", "*$D(yyyy-MM-dd).log$J(.)")]
    public void TestFilenameAnd1(string fileName, string expectedResult, string formatString)
    {
        RolloverFilenameBuilder fnb = new(formatString);
        fnb.SetFileName(fileName);
        fnb.Index += 1;
        var name = fnb.BuildFileName();
        Assert.That(name, Is.EqualTo(expectedResult));
    }

    [Test]
    [TestCase("engine.log", "engine.log.2","*$J(.)")]
    [TestCase("engine.log", "engine.log.2","*.log$J(.)")]
    public void TestFilenameAnd2(string fileName, string expectedResult, string formatString)
    {
        RolloverFilenameBuilder fnb = new(formatString);
        fnb.SetFileName(fileName);
        fnb.Index += 2;
        var name = fnb.BuildFileName();
        Assert.That(name, Is.EqualTo(expectedResult));
    }


    [Test]
    [TestCase("engine1.log", "engine.log","engine$J.log")]
    public void TestFilenameMinus1(string fileName, string expectedResult, string formatString)
    {
        RolloverFilenameBuilder fnb = new(formatString);
        fnb.SetFileName(fileName);
        fnb.Index -= 1;
        var name = fnb.BuildFileName();
        Assert.That(name, Is.EqualTo("engine.log"));
    }
}