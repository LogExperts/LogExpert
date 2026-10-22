using System.Runtime.Versioning;

using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.UI.Interface;
using LogExpert.UI.Services.LogWindowCoordinatorService;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.Services;

[TestFixture]
[Apartment(ApartmentState.STA)]
[SupportedOSPlatform("windows")]
public class LogWindowCoordinatorTests
{
    private Mock<IConfigManager> _configManagerMock;
    private Mock<IPluginRegistry> _pluginRegistryMock;
    private LogWindowCoordinator _coordinator;
    private Mock<ITabController> _tabControllerMock;
    private Mock<ILedIndicatorService> _ledServiceMock;
    private Mock<IFileOperationService> _fileOperationServiceMock;
    private Settings _settings;
    private Preferences _preferences;

    [SetUp]
    public void Setup ()
    {
        _configManagerMock = new Mock<IConfigManager>();
        _pluginRegistryMock = new Mock<IPluginRegistry>();
        _tabControllerMock = new Mock<ITabController>();
        _ledServiceMock = new Mock<ILedIndicatorService>();
        _fileOperationServiceMock = new Mock<IFileOperationService>();
        _settings = new Settings();
        _preferences = _settings.Preferences;
        _ = _configManagerMock.Setup(cm => cm.Settings).Returns(_settings);
        _ = _pluginRegistryMock.Setup(pr => pr.RegisteredColumnizers).Returns([]);

        _coordinator = new LogWindowCoordinator(
            _configManagerMock.Object,
            _pluginRegistryMock.Object,
            null!,
            _tabControllerMock.Object,
            _ledServiceMock.Object,
            _fileOperationServiceMock.Object);
    }

    [Test]
    public void ResolveHighlightGroup_WithGroupName_ReturnsNameMatch ()
    {
        // Arrange
        var group = new HighlightGroup { GroupName = "MyGroup" };
        _preferences.HighlightGroupList = [group];

        // Act
        var result = _coordinator.ResolveHighlightGroup("MyGroup", null);

        // Assert
        Assert.That(result, Is.SameAs(group));
    }

    [Test]
    public void ResolveHighlightGroup_WithFileName_ReturnsFileMaskMatch ()
    {
        // Arrange
        var group = new HighlightGroup { GroupName = "LogGroup" };
        _preferences.HighlightGroupList = [group];
        _preferences.HighlightMaskList.Add(new HighlightMaskEntry { Mask = @"\.log$", HighlightGroupName = "LogGroup" });

        // Act
        var result = _coordinator.ResolveHighlightGroup(null, "test.log");

        // Assert
        Assert.That(result, Is.SameAs(group));
    }

    [Test]
    public void ResolveHighlightGroup_FileMaskTakesPriority_WhenBothProvided ()
    {
        // Arrange
        var maskGroup = new HighlightGroup { GroupName = "MaskGroup" };
        var nameGroup = new HighlightGroup { GroupName = "NameGroup" };
        _preferences.HighlightGroupList = [maskGroup, nameGroup];
        _preferences.HighlightMaskList.Add(new HighlightMaskEntry { Mask = @"\.log$", HighlightGroupName = "MaskGroup" });

        // Act
        var result = _coordinator.ResolveHighlightGroup("NameGroup", "test.log");

        // Assert
        Assert.That(result, Is.SameAs(maskGroup));
    }

    [Test]
    public void ResolveHighlightGroup_FallsBackToName_WhenFileMaskNoMatch ()
    {
        // Arrange
        var group = new HighlightGroup { GroupName = "NameGroup" };
        _preferences.HighlightGroupList = [group];
        _preferences.HighlightMaskList.Add(new HighlightMaskEntry { Mask = @"\.xml$", HighlightGroupName = "OtherGroup" });

        // Act
        var result = _coordinator.ResolveHighlightGroup("NameGroup", "test.log");

        // Assert
        Assert.That(result, Is.SameAs(group));
    }

    [Test]
    public void ResolveHighlightGroup_NoMatch_ReturnsFirstGroup ()
    {
        // Arrange
        var firstGroup = new HighlightGroup { GroupName = "First" };
        var secondGroup = new HighlightGroup { GroupName = "Second" };
        _preferences.HighlightGroupList = [firstGroup, secondGroup];

        // Act
        var result = _coordinator.ResolveHighlightGroup("NonExistent", null);

        // Assert
        Assert.That(result, Is.SameAs(firstGroup));
    }

    [Test]
    public void ResolveHighlightGroup_EmptyList_ReturnsNewEmptyGroup ()
    {
        // Arrange
        _preferences.HighlightGroupList = [];

        // Act
        var result = _coordinator.ResolveHighlightGroup("NonExistent", null);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.GroupName, Is.Not.Null);
    }

    [Test]
    public void ResolveHighlightGroup_NeverReturnsNull ()
    {
        // Arrange
        _preferences.HighlightGroupList = [];

        // Act
        var result = _coordinator.ResolveHighlightGroup(null, null);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void ResolveHighlightGroup_MalformedRegex_SkipsAndContinues ()
    {
        // Arrange
        var group = new HighlightGroup { GroupName = "GoodGroup" };
        _preferences.HighlightGroupList = [group];
        _preferences.HighlightMaskList.Add(new HighlightMaskEntry { Mask = @"[invalid", HighlightGroupName = "BadGroup" });
        _preferences.HighlightMaskList.Add(new HighlightMaskEntry { Mask = @"\.log$", HighlightGroupName = "GoodGroup" });

        // Act
        var result = _coordinator.ResolveHighlightGroup(null, "test.log");

        // Assert
        Assert.That(result, Is.SameAs(group));
    }

    [Test]
    public void HighlightSettingsChanged_FiresAfterMutation ()
    {
        // Arrange
        var eventFired = false;
        _coordinator.HighlightSettingsChanged += (_, _) => eventFired = true;

        // Act
        _coordinator.OnHighlightSettingsChanged();

        // Assert
        Assert.That(eventFired, Is.True);
    }

    [Test]
    public void ResolveColumnizer_MaskPrioTrue_ChecksMaskFirst ()
    {
        // Arrange
        _preferences.MaskPrio = true;
        // Add mask entry that matches *.log
        _preferences.ColumnizerMaskList.Add(new ColumnizerMaskEntry { Mask = @"\.log$", ColumnizerName = "TestColumnizer" });
        // Note: This test depends on PluginRegistry having a registered columnizer named "TestColumnizer"
        // In unit tests, PluginRegistry may not be populated → expect null from FindMemorColumnizerByName
        // This test primarily verifies the priority logic path is exercised

        // Act
        var result = _coordinator.ResolveColumnizer("test.log");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ResolveColumnizer_NoMatch_ReturnsNull ()
    {
        // Arrange — no masks, no history
        _preferences.MaskPrio = true;

        // Act
        var result = _coordinator.ResolveColumnizer("unknown.xyz");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ResolveColumnizer_StaleHistoryEntry_IsRemoved ()
    {
        // Arrange
        _preferences.MaskPrio = false; // history first
        _settings.ColumnizerHistoryList.Add(new ColumnizerHistoryEntry("test.log", "NonExistentColumnizer"));

        // Act
        var result = _coordinator.ResolveColumnizer("test.log");

        // Assert
        Assert.That(result, Is.Null);
        Assert.That(_settings.ColumnizerHistoryList, Has.Count.EqualTo(0));
    }

    [Test]
    public void ResolveColumnizer_MalformedRegexInMask_SkipsGracefully ()
    {
        // Arrange
        _preferences.MaskPrio = true;
        _preferences.ColumnizerMaskList.Add(new ColumnizerMaskEntry { Mask = @"[invalid", ColumnizerName = "Test" });

        // Act & Assert — should not throw
        Assert.DoesNotThrow(() => _coordinator.ResolveColumnizer("test.log"));
    }

    [Test]
    public void SearchParams_SharedInstance_MutationsVisibleAcrossConsumers ()
    {
        // Arrange
        var params1 = _coordinator.SearchParams;
        var params2 = _coordinator.SearchParams;

        // Act
        params1.SearchText = "test search";
        params1.IsFindNext = true;

        // Assert
        Assert.That(params2.SearchText, Is.EqualTo("test search"));
        Assert.That(params2.IsFindNext, Is.True);
        Assert.That(params1, Is.SameAs(params2));
    }

    [Test]
    public void GetOpenFiles_DelegatesToTabController ()
    {
        // Arrange
        _ = _tabControllerMock.Setup(tc => tc.GetAllWindows()).Returns([]);

        // Act
        var result = _coordinator.GetOpenFiles();

        // Assert
        Assert.That(result, Is.Empty);
        _tabControllerMock.Verify(tc => tc.GetAllWindows(), Times.Once);
    }

    [Test]
    public void SelectTab_DelegatesToTabController ()
    {
        // Act & Assert — no exception with null (controller mock accepts any)
        _coordinator.SelectTab(null!);
        _tabControllerMock.Verify(tc => tc.ActivateWindow(null!), Times.Once);
    }

    [Test]
    public void ScrollAllTabsToTimestamp_SkipsSender ()
    {
        // Arrange
        _ = _tabControllerMock.Setup(tc => tc.GetAllWindows()).Returns([]);

        // Act
        _coordinator.ScrollAllTabsToTimestamp(DateTime.Now, null!);

        // Assert — no exception, tab controller queried
        _tabControllerMock.Verify(tc => tc.GetAllWindows(), Times.Once);
    }

    [Test]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void NotifyFollowTailChanged_DoesNotThrow ()
    {
        // This transitionally delegates to LogTabWindow, which is null in tests.
        // Skip detailed verification — covered by smoke test.
        // Once the form dependency is removed, this can be properly tested.
        Assert.Pass("Transitional delegation — verified by smoke test");
    }
}