using System.Text;

using LogExpert.Core.Classes.Persister;

using NUnit.Framework;

namespace LogExpert.Tests.Encodings;

/// <summary>
/// The per-file encoding stored in a .lxp. It is read while a file is being opened, with no dialog
/// involved, so a legacy Windows code page has to resolve on its own here too — otherwise the encoding
/// the user chose for that specific file is silently replaced by <see cref="Encoding.Default"/>.
/// </summary>
[TestFixture]
#pragma warning disable CS0618 // PersisterXML is the deprecated fallback format, and still loads old .lxp files.
public class PersisterXmlEncodingTests
{
    private string _lxpFile = null!;

    [SetUp]
    public void Setup ()
    {
        _lxpFile = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid()}.lxp");
    }

    [TearDown]
    public void Cleanup ()
    {
        if (File.Exists(_lxpFile))
        {
            File.Delete(_lxpFile);
        }
    }

    [Test]
    [TestCase("windows-1250", 1250)]
    [TestCase("windows-1252", 1252)]
    [TestCase("utf-8", 65001)]
    [TestCase("gb2312", 936)]
    public void Load_PersistedEncoding_Resolves (string encodingName, int expectedCodePage)
    {
        WriteLxp($"<encoding name=\"{encodingName}\" />");

        var data = PersisterXML.Load(_lxpFile);

        Assert.Multiple(() =>
        {
            Assert.That(data.Encoding, Is.Not.Null, "the persisted encoding was discarded");
            Assert.That(data.Encoding.CodePage, Is.EqualTo(expectedCodePage));
        });
    }

    [Test]
    public void Load_NoEncodingElement_LeavesEncodingNull ()
    {
        WriteLxp(string.Empty);

        var data = PersisterXML.Load(_lxpFile);

        // Null means "nothing was persisted for this file", which lets the BOM and the Preferences
        // default decide. It must not be conflated with an unresolvable name.
        Assert.That(data.Encoding, Is.Null);
    }

    [Test]
    public void Load_UnresolvableEncodingName_FallsBackToDefault ()
    {
        WriteLxp("<encoding name=\"not-a-real-encoding-xxxxx\" />");

        var data = PersisterXML.Load(_lxpFile);

        Assert.That(data.Encoding, Is.EqualTo(Encoding.Default));
    }

    private void WriteLxp (string encodingElement)
    {
        File.WriteAllText(
            _lxpFile,
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <logexpert>
              <file fileName="test.log" lineCount="1">
                <options />
                {encodingElement}
              </file>
            </logexpert>
            """);
    }
}
#pragma warning restore CS0618
