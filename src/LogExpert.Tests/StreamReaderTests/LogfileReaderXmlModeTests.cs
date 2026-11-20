using System.Text;

using ColumnizerLib;

using LogExpert.Core.Classes.Log;
using LogExpert.Core.Entities;

using NUnit.Framework;

namespace LogExpert.Tests.StreamReaderTests;

[TestFixture]
public class LogfileReaderXmlModeTests
{
    private string _tempFile;

    [SetUp]
    public void Setup ()
    {
        _tempFile = Path.GetTempFileName();
        _ = PluginRegistry.PluginRegistry.Create(Path.GetDirectoryName(_tempFile)!, 500);
    }

    [TearDown]
    public void Cleanup ()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Test]
    public void ReadFiles_XmlMode_ReadsOneLinePerEvent ()
    {
        File.WriteAllText(_tempFile,
            "<log4j:event logger=\"a\">first</log4j:event>\n<log4j:event logger=\"b\">second</log4j:event>\n",
            Encoding.UTF8);

        using var reader = new LogfileReader(
            _tempFile,
            new EncodingOptions { Encoding = Encoding.UTF8 },
            multiFile: false,
            bufferCount: 100,
            linesPerBuffer: 500,
            new MultiFileOptions(),
            PluginRegistry.PluginRegistry.Instance,
            maximumLineLength: 500,
            progressReporter: Core.Classes.Log.ProgressReporters.NullProgressReporter.Instance)
        {
            IsXmlMode = true,
            XmlLogConfig = new PlainXmlConfig()
        };

        reader.ReadFiles();

        Assert.That(reader.LineCount, Is.EqualTo(2));
        Assert.That(reader.GetLogLineMemory(0)?.FullLine.ToString(), Does.Contain("first"));
        Assert.That(reader.GetLogLineMemory(1)?.FullLine.ToString(), Does.Contain("second"));
    }

    private sealed class PlainXmlConfig : IXmlLogConfiguration
    {
        public string XmlStartTag => "<log4j:event";

        public string XmlEndTag => "</log4j:event>";

        public string Stylesheet => null;

        public string[] GetNamespaceDeclaration () => null;
    }
}