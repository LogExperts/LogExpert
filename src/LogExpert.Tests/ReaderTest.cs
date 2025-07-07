using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;
using LogExpert.Core.Interface;

using NUnit.Framework;

using System;
using System.IO;
using System.Text;

namespace LogExpert.Tests;

[TestFixture]
internal class ReaderTest
{
    [TearDown]
    public void TearDown()
    {
    }

    [OneTimeSetUp]
    public void Boot()
    {
    }

    private void CompareReaderImplementationsInternal(string fileName, Encoding enc, int maxPosition)
    {
        var path = Environment.CurrentDirectory + "\\data\\";
        EncodingOptions encOpts = new();
        encOpts.Encoding = enc;

        using Stream s1 = new FileStream(path + fileName, FileMode.Open, FileAccess.Read);
        using Stream s2 = new FileStream(path + fileName, FileMode.Open, FileAccess.Read);
        using ILogStreamReader r1 = new PositionAwareStreamReaderLegacy(s1, encOpts);
        using ILogStreamReader r2 = new PositionAwareStreamReaderSystem(s2, encOpts);
        for (var lineNum = 0; ; lineNum++)
        {
            var line1 = r1.ReadLine();
            var line2 = r2.ReadLine();
            if (line1 == null && line2 == null)
            {
                break;
            }

            Assert.That(line1, Is.EqualTo(line2), "File " + fileName);

            if (r1.Position != maxPosition)
            {
                Assert.That(r2.Position, Is.EqualTo(r1.Position), "Line " + lineNum + ", File: " + fileName);
            }
            else
            {
                //Its desired that the position of the new implementation is 2 bytes ahead to fix the problem of empty lines every time a new line is appended.
                Assert.That(r2.Position - 2, Is.EqualTo(r1.Position), "Line " + lineNum + ", File: " + fileName);
            }
        }
    }

    //TODO find out why it does not work with appveyor, but works fine with normal environment!
    //[Test]
    //[TestCase("50 MB.txt", "Windows-1252", 50000000)]
    //[TestCase("50 MB UTF16.txt", "Unicode", 49999998)]
    //[TestCase("50 MB UTF8.txt", "UTF-8", 50000000)]
    //public void CompareReaderImplementations(string fileName, string encoding, int maxPosition)
    //{
    //    CompareReaderImplementationsInternal(fileName, Encoding.GetEncoding(encoding), maxPosition);
    //}
}