using System.Globalization;
using System.Text;

using LogExpert.Core.Classes.Persister;

using Newtonsoft.Json;

namespace LogExpert.Persister.Tests;

/// <summary>
/// Tests for recovering a Session's file list from its tab layout XML (issue #694).
/// v1.42.0 saved Sessions with an empty FileNames list; the DockPanel layout XML still
/// names every log window, so loading falls back to it.
/// </summary>
[TestFixture]
public class SessionFileResolverTests
{
    private string _testDirectory;

    [SetUp]
    public void Setup ()
    {
        _testDirectory = Path.Join(Path.GetTempPath(), "LogExpertTests", "SessionResolver", Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(_testDirectory);

        _ = PluginRegistry.PluginRegistry.Create(_testDirectory, 1000);
    }

    [TearDown]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Unit Test")]
    public void TearDown ()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch (Exception)
        {
            // Ignore cleanup failures
        }
    }

    private static string BuildLayoutXml (params string[] persistStrings)
    {
        StringBuilder contents = new();
        for (var i = 0; i < persistStrings.Length; i++)
        {
            _ = contents.Append(CultureInfo.InvariantCulture, $"    <Content ID=\"{i}\" PersistString=\"{persistStrings[i]}\" AutoHidePortion=\"0.25\" IsHidden=\"False\" IsFloat=\"False\" />\r\n");
        }

        StringBuilder paneRefs = new();
        for (var i = 0; i < persistStrings.Length; i++)
        {
            _ = paneRefs.Append(CultureInfo.InvariantCulture, $"        <Content ID=\"{i}\" RefID=\"{i}\" />\r\n");
        }

        // Shape of a real v1.42.0 layout: contents with PersistString, plus pane entries that
        // reference them through RefID only (those must not be picked up by the recovery).
        return "<!--DockPanel configuration file. Author: Weifen Luo, all rights reserved.-->\r\n" +
               "<DockPanel FormatVersion=\"1.0\" DockLeftPortion=\"0.25\" DockRightPortion=\"0.25\" DockTopPortion=\"0.25\" DockBottomPortion=\"0.25\" ActiveDocumentPane=\"0\" ActivePane=\"0\">\r\n" +
               $"  <Contents Count=\"{persistStrings.Length}\">\r\n" +
               contents +
               "  </Contents>\r\n" +
               "  <Panes Count=\"1\">\r\n" +
               "    <Pane ID=\"0\" DockState=\"Document\" ActiveContent=\"0\">\r\n" +
               $"      <Contents Count=\"{persistStrings.Length}\">\r\n" +
               paneRefs +
               "      </Contents>\r\n" +
               "    </Pane>\r\n" +
               "  </Panes>\r\n" +
               "  <FloatWindows Count=\"0\" />\r\n" +
               "</DockPanel>";
    }

    private string WriteSessionFile (string fileNamesJson, string tabLayoutXml)
    {
        var sessionFile = Path.Join(_testDirectory, "session.lxj");
        var layoutJson = JsonConvert.ToString(tabLayoutXml ?? string.Empty);
        var json = $"{{\r\n  \"FileNames\": {fileNamesJson},\r\n  \"TabLayoutXml\": {layoutJson},\r\n  \"SessionFilePath\": null\r\n}}";
        File.WriteAllText(sessionFile, json, Encoding.UTF8);
        return sessionFile;
    }

    #region RecoverFileNamesFromLayout

    [Test]
    public void RecoverFileNamesFromLayout_LayoutWithLogWindows_ReturnsPathsInLayoutOrder ()
    {
        var layout = BuildLayoutXml(@"LogWindow#C:\temp\test1.log", @"LogWindow#C:\temp\test2.log");

        var result = SessionFileResolver.RecoverFileNamesFromLayout(layout);

        Assert.That(result, Is.EqualTo(new[] { @"C:\temp\test1.log", @"C:\temp\test2.log" }));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void RecoverFileNamesFromLayout_NullOrWhitespace_ReturnsEmpty (string? layout)
    {
        var result = SessionFileResolver.RecoverFileNamesFromLayout(layout);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void RecoverFileNamesFromLayout_MalformedXml_ReturnsEmpty ()
    {
        var result = SessionFileResolver.RecoverFileNamesFromLayout("<DockPanel><Contents></DockPanel>");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void RecoverFileNamesFromLayout_NoLogWindowContents_ReturnsEmpty ()
    {
        var layout = BuildLayoutXml("BookmarkWindow");

        var result = SessionFileResolver.RecoverFileNamesFromLayout(layout);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void RecoverFileNamesFromLayout_PersistStringWithoutPath_IsSkipped ()
    {
        var layout = BuildLayoutXml("LogWindow#", @"LogWindow#C:\temp\test1.log");

        var result = SessionFileResolver.RecoverFileNamesFromLayout(layout);

        Assert.That(result, Is.EqualTo(new[] { @"C:\temp\test1.log" }));
    }

    #endregion

    #region SessionPersister.LoadSessionData recovery (issue #694)

    [Test]
    public void LoadSessionData_EmptyFileNamesWithLayout_RecoversFilesFromLayout ()
    {
        // Arrange - a Session as written by v1.42.0: empty FileNames, intact layout XML
        var log1 = Path.Join(_testDirectory, "test1.log");
        var log2 = Path.Join(_testDirectory, "test2.log");
        File.WriteAllText(log1, "line1\n");
        File.WriteAllText(log2, "line1\n");
        var sessionFile = WriteSessionFile("[]", BuildLayoutXml($"LogWindow#{log1}", $"LogWindow#{log2}"));

        // Act
        var result = SessionPersister.LoadSessionData(sessionFile, PluginRegistry.PluginRegistry.Instance);

        // Assert
        Assert.That(result.SessionData.FileNames, Is.EqualTo(new[] { log1, log2 }));
        Assert.That(result.ValidationResult.MissingFiles, Is.Empty);
        Assert.That(result.RequiresUserIntervention, Is.False);
    }

    [Test]
    public void LoadSessionData_NullFileNamesWithLayout_RecoversFilesFromLayout ()
    {
        var log1 = Path.Join(_testDirectory, "test1.log");
        File.WriteAllText(log1, "line1\n");
        var sessionFile = WriteSessionFile("null", BuildLayoutXml($"LogWindow#{log1}"));

        var result = SessionPersister.LoadSessionData(sessionFile, PluginRegistry.PluginRegistry.Instance);

        Assert.That(result.SessionData.FileNames, Is.EqualTo(new[] { log1 }));
    }

    [Test]
    public void LoadSessionData_EmptyFileNamesWithoutLayout_StaysEmpty ()
    {
        var sessionFile = WriteSessionFile("[]", string.Empty);

        var result = SessionPersister.LoadSessionData(sessionFile, PluginRegistry.PluginRegistry.Instance);

        Assert.That(result.SessionData.FileNames, Is.Empty);
    }

    [Test]
    public void LoadSessionData_FileNamesPresent_LayoutDoesNotOverrideThem ()
    {
        var log1 = Path.Join(_testDirectory, "listed.log");
        var log2 = Path.Join(_testDirectory, "layout-only.log");
        File.WriteAllText(log1, "line1\n");
        File.WriteAllText(log2, "line1\n");
        var sessionFile = WriteSessionFile(JsonConvert.SerializeObject(new[] { log1 }), BuildLayoutXml($"LogWindow#{log2}"));

        var result = SessionPersister.LoadSessionData(sessionFile, PluginRegistry.PluginRegistry.Instance);

        Assert.That(result.SessionData.FileNames, Is.EqualTo(new[] { log1 }));
    }

    #endregion
}
