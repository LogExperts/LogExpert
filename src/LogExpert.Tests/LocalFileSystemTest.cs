using LogExpert.PluginRegistry.FileSystem;

using NUnit.Framework;

using System;
using System.IO;

namespace LogExpert.Tests;

[TestFixture]
internal class LocalFileSystemTest : RolloverHandlerTestBase
{
    [TearDown]
    public void TearDown()
    {
        Cleanup();
    }

    [OneTimeSetUp]
    public void Boot()
    {
        Cleanup();
    }


    [Test]
    public void TestUriHandle()
    {
        LocalFileSystem fs = new();
        Assert.That(fs.CanHandleUri("file:///c:/logfile.txt"), Is.True);
        Assert.That(fs.CanHandleUri("file:///c:\\logfile.txt"), Is.True);
        Assert.That(fs.CanHandleUri("c:/logfile.txt"), Is.True);
        Assert.That(fs.CanHandleUri("c:\\logfile.txt"), Is.True);
    }

    [Test]
    public void TestUriToFileStream()
    {
        DirectoryInfo dInfo = Directory.CreateDirectory(RolloverHandlerTest.TEST_DIR_NAME);
        var fullName = CreateFile(dInfo, "test.log");

        LocalFileSystem fs = new();
        ILogFileInfo info = fs.GetLogfileInfo(fullName);
        Assert.That(info.Length > 0, Is.True);
        Assert.That(info.OriginalLength == info.Length, Is.True);
        Stream stream = info.OpenStream();
        Assert.That(stream.CanSeek, Is.True);
        StreamReader reader = new(stream);
        var line = reader.ReadLine();
        Assert.That(line.StartsWith("line number", StringComparison.InvariantCultureIgnoreCase), Is.True);
        reader.Close();
    }
}