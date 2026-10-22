using LogExpert.Core.Classes.Persister;

namespace LogExpert.Persister.Tests;

/// <summary>
/// The deprecated XML .lxp format is a read-only fallback: it is only reached when a session file fails
/// to parse as JSON, and that happens on the file-open path, synchronously, with no handler above it.
/// A .lxp that is hand-edited, truncated, or written by a very old version must therefore degrade to
/// "no persistence data" instead of throwing into the open.
/// </summary>
[TestFixture]
#pragma warning disable CS0618 // PersisterXML is the deprecated fallback format, and still loads old .lxp files.
public class PersisterXmlMalformedTests
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
    public void Load_FileElementWithoutOptions_ReturnsDefaults ()
    {
        WriteLxp("""<file fileName="test.log" lineCount="1" />""");

        var data = PersisterXML.Load(_lxpFile);

        Assert.That(data, Is.Not.Null, "a <file> without <options> must still load");
        Assert.Multiple(() =>
        {
            Assert.That(data.FileName, Is.EqualTo("test.log"));
            Assert.That(data.LineCount, Is.EqualTo(1));
            Assert.That(data.MultiFile, Is.False);
            Assert.That(data.MultiFilePattern, Is.Null);
            Assert.That(data.MultiFileMaxDays, Is.Zero);
            Assert.That(data.FilterVisible, Is.False);

            // A missing <options> means "nothing was persisted", so every option keeps its
            // PersistenceData default — including the ones whose default is not the zero value.
            Assert.That(data.CurrentLine, Is.EqualTo(-1));
            Assert.That(data.FollowTail, Is.True);
        });
    }

    [Test]
    public void Load_FileElementWithoutOptions_StillReadsSiblingsOfOptions ()
    {
        // tab, columnizer and highlightGroup are children of <file>, not of <options>. A missing
        // <options> must not cost the user their columnizer and highlight group.
        WriteLxp("""
                 <file fileName="test.log" lineCount="1">
                   <tab name="My Tab" />
                   <columnizer name="CSV Columnizer" />
                   <highlightGroup name="My Group" />
                 </file>
                 """);

        var data = PersisterXML.Load(_lxpFile);

        Assert.That(data, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(data.TabName, Is.EqualTo("My Tab"));
            Assert.That(data.ColumnizerName, Is.EqualTo("CSV Columnizer"));
            Assert.That(data.HighlightGroupName, Is.EqualTo("My Group"));
        });
    }

    /// <summary>
    /// Load documents "null when the file cannot be read or parsed", and malformed content counts, not
    /// just malformed markup. One case per exception the readers let escape, so the widened catch in
    /// Load is pinned rather than merely asserted in prose.
    /// </summary>
    [Test]
    [TestCase("""<file fileName="test.log" lineCount="not-a-number"><options /></file>""", "FormatException")]
    [TestCase("""<file fileName="test.log" lineCount="99999999999999"><options /></file>""", "OverflowException")]
    [TestCase("""<file fileName="test.log" lineCount="1"><options /><bookmarks><bookmark line="5"><posX>1</posX><posY>2</posY></bookmark><bookmark line="5"><posX>3</posX><posY>4</posY></bookmark></bookmarks></file>""", "ArgumentException")]
    [TestCase("""<file fileName="test.log" lineCount="1"><options /><bookmarks><!-- hand-edited remark --></bookmarks></file>""", "NullReferenceException")]
    public void Load_MalformedContent_ReturnsNull (string fileElement, string escapingException)
    {
        WriteLxp(fileElement);

        Assert.That(PersisterXML.Load(_lxpFile), Is.Null, $"{escapingException} escaped Load");
    }

    [Test]
    public void Load_FilterTabsWithNonElementChild_SkipsIt ()
    {
        WriteLxp("""
                 <file fileName="test.log" lineCount="1">
                   <options />
                   <filterTabs><!-- hand-edited remark --></filterTabs>
                 </file>
                 """);

        var data = PersisterXML.Load(_lxpFile);

        // ReadPersistenceDataFromNode documents defaults for a node that is not an element, so the
        // remark costs the reader nothing beyond the tab it is standing in for.
        Assert.That(data, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(data.FileName, Is.EqualTo("test.log"));
            Assert.That(data.FilterTabDataList, Is.Empty);
        });
    }

    [Test]
    public void PersisterLoad_FileElementWithoutOptions_DoesNotThrowOnTheFileOpenPath ()
    {
        // Persister.Load tries JSON first and only falls back to XML from inside the catch block, so
        // anything PersisterXML throws replaces the original exception and escapes into AddFileTab.
        WriteLxp("""<file fileName="test.log" lineCount="1" />""");

        var data = Core.Classes.Persister.Persister.Load(_lxpFile);

        Assert.That(data, Is.Not.Null);
        Assert.That(data.FileName, Is.EqualTo("test.log"));
    }

    private void WriteLxp (string fileElement)
    {
        File.WriteAllText(
            _lxpFile,
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <logexpert>
              {fileElement}
            </logexpert>
            """);
    }
}
#pragma warning restore CS0618
