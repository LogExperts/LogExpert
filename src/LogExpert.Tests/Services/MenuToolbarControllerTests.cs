using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;

using LogExpert.Core.EventArguments;
using LogExpert.Core.Helpers;
using LogExpert.Dialogs;
using LogExpert.UI.Controls;
using LogExpert.UI.Services;
using LogExpert.UI.Services.MenuToolbarService;

using NUnit.Framework;

namespace LogExpert.Tests.Services;

[TestFixture]
[SupportedOSPlatform("windows")]
[Apartment(ApartmentState.STA)]
internal class MenuToolbarControllerTests : IDisposable
{
    private MenuToolbarController _controller;
    private MenuStrip _mainMenu;
    private ToolStrip _toolbar;
    private DateTimeDragControl _dragControl;
    private CheckBox _followTailCheckBox;
    private ToolStripMenuItem _fileMenu;
    private ToolStripMenuItem _viewMenu;
    private ToolStripMenuItem _optionMenu;
    private ToolStripMenuItem _encodingMenu;
    private bool _disposed;
    private ApplicationContext? _appContext;
    private WindowsFormsSynchronizationContext? _syncContext;

    [SetUp]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit Tests")]
    public void Setup ()
    {
        // Ensure we have a WindowsFormsSynchronizationContext for the UI thread
        if (SynchronizationContext.Current == null)
        {
            _syncContext = new WindowsFormsSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(_syncContext);
        }

        // Create an application context to ensure we have a proper UI context
        _appContext = new ApplicationContext();

        _controller = new MenuToolbarController();

        _mainMenu = new MenuStrip();
        _toolbar = new ToolStrip();
        _dragControl = new DateTimeDragControl();
        _followTailCheckBox = new CheckBox { Name = "checkBoxFollowTail" };

        // Build realistic menu structure matching designer
        _fileMenu = new ToolStripMenuItem("File") { Name = "fileToolStripMenuItem" };
        _ = _fileMenu.DropDownItems.Add(new ToolStripMenuItem("Close") { Name = "closeFileToolStripMenuItem" });
        _ = _fileMenu.DropDownItems.Add(new ToolStripMenuItem("Last Used") { Name = "lastUsedToolStripMenuItem" });

        var multiFile = new ToolStripMenuItem("Multi-File") { Name = "multiFileToolStripMenuItem" };
        _ = multiFile.DropDownItems.Add(new ToolStripMenuItem("Enabled") { Name = "multiFileEnabledStripMenuItem" });
        _ = _fileMenu.DropDownItems.Add(multiFile);

        _viewMenu = new ToolStripMenuItem("View") { Name = "viewNavigateToolStripMenuItem" };
        _ = _viewMenu.DropDownItems.Add(new ToolStripMenuItem("Search") { Name = "searchToolStripMenuItem" });
        _ = _viewMenu.DropDownItems.Add(new ToolStripMenuItem("Filter") { Name = "filterToolStripMenuItem" });
        _ = _viewMenu.DropDownItems.Add(new ToolStripMenuItem("Column Finder") { Name = "columnFinderToolStripMenuItem" });

        // Filled by the same builder the Log Tab Window uses, so this fixture cannot describe a menu
        // the application does not actually build.
        _encodingMenu = new ToolStripMenuItem("Encoding") { Name = "encodingToolStripMenuItem" };
        EncodingMenuBuilder.Fill(_encodingMenu, (_, _) => { });
        _ = _viewMenu.DropDownItems.Add(_encodingMenu);

        var timeshiftMenu = new ToolStripMenuItem("Timeshift") { Name = "timeshiftToolStripMenuItem" };
        _ = timeshiftMenu.DropDownItems.Add(new ToolStripTextBox { Name = "timeshiftToolStripTextBox" });
        _ = _viewMenu.DropDownItems.Add(timeshiftMenu);

        _optionMenu = new ToolStripMenuItem("Options") { Name = "optionToolStripMenuItem" };
        _ = _optionMenu.DropDownItems.Add(new ToolStripMenuItem("Cell Select") { Name = "cellSelectModeToolStripMenuItem" });

        _mainMenu.Items.AddRange([_fileMenu, _viewMenu, _optionMenu]);

        // Toolbar
        _ = _toolbar.Items.Add(new ToolStripButton("Bubbles") { Name = "toolStripButtonBubbles" });
        _ = _toolbar.Items.Add(new ToolStripComboBox { Name = "highlightGroupsToolStripComboBox" });

        _controller.InitializeMenus(_mainMenu, _toolbar, null, _dragControl, _followTailCheckBox);
    }

    [TearDown]
    public void TearDown ()
    {
        _controller?.Dispose();
        _mainMenu?.Dispose();
        _toolbar?.Dispose();
        _dragControl?.Dispose();
        _followTailCheckBox?.Dispose();
        _appContext?.Dispose();
        _syncContext?.Dispose();
    }

    [Test]
    public void UpdateGuiState_SetsFollowTailChecked ()
    {
        var state = new GuiStateEventArgs { FollowTail = true, MenuEnabled = true };
        _controller.UpdateGuiState(state, false);
        Assert.That(_followTailCheckBox.Checked, Is.True);
    }

    [Test]
    public void UpdateGuiState_SetsTimeshiftMenuState ()
    {
        var state = new GuiStateEventArgs
        {
            TimeshiftPossible = true,
            TimeshiftEnabled = true,
            TimeshiftText = "500",
            MenuEnabled = true
        };
        _controller.UpdateGuiState(state, false);

        var timeshiftItem = FindMenuItem("timeshiftToolStripMenuItem");
        Assert.That(timeshiftItem.Enabled, Is.True);
        Assert.That(timeshiftItem.Checked, Is.True);

        var timeshiftTextBox = FindItemRecursive<ToolStripTextBox>(_mainMenu.Items, "timeshiftToolStripTextBox");
        Assert.That(timeshiftTextBox.Text, Is.EqualTo("500"));
        Assert.That(timeshiftTextBox.Enabled, Is.True);
    }

    [Test]
    public void UpdateGuiState_ShowsTimestampControl_WhenTimeshiftAndConfigEnabled ()
    {
        var state = new GuiStateEventArgs
        {
            TimeshiftPossible = true,
            MinTimestamp = new DateTime(2025, 1, 1),
            MaxTimestamp = new DateTime(2025, 12, 31),
            Timestamp = new DateTime(2025, 6, 15),
            MenuEnabled = true
        };

        _controller.UpdateGuiState(state, timestampControlEnabled: true);
        Assert.That(_dragControl.Visible, Is.True);
        Assert.That(_dragControl.Enabled, Is.True);
    }

    [Test]
    public void UpdateGuiState_HidesTimestampControl_WhenTimeshiftNotPossible ()
    {
        var state = new GuiStateEventArgs { TimeshiftPossible = false, MenuEnabled = true };
        _controller.UpdateGuiState(state, timestampControlEnabled: true);
        Assert.That(_dragControl.Visible, Is.False);
    }

    /// <summary>
    /// Exactly one row is checked, and it is the row for the encoding passed in — asserted for every
    /// offered encoding, so no row can be unreachable.
    /// </summary>
    [Test]
    public void UpdateEncodingMenu_OfferedEncoding_ChecksOnlyThatRow ()
    {
        Assert.Multiple(() =>
        {
            foreach (var encoding in EncodingRegistry.OfferedEncodings)
            {
                _controller.UpdateEncodingMenu(encoding);

                Assert.That(
                    CheckedEncodingRowNames(),
                    Is.EqualTo([EncodingMenuBuilder.RowName(encoding)]).AsCollection,
                    $"'{encoding.HeaderName}' did not check exactly its own row");
            }
        });
    }

    [Test]
    public void UpdateEncodingMenu_NullEncoding_UnchecksAll ()
    {
        // First set one
        _controller.UpdateEncodingMenu(Encoding.UTF8);
        // Then clear
        _controller.UpdateEncodingMenu(null);

        Assert.That(CheckedEncodingRowNames(), Is.Empty);
    }

    /// <summary>
    /// A row is identified by its code page, so the UTF-8 instances the application passes around are the
    /// same row: <see cref="Encoding.UTF8"/> (emits a BOM), a no-BOM instance, and <c>Encoding.Default</c>
    /// — which is UTF-8 on .NET, and is the reason the "ANSI" row was dropped rather than kept as a
    /// second, indistinguishable UTF-8 row (issue #688).
    /// </summary>
    [Test]
    public void UpdateEncodingMenu_AnyUtf8Instance_ChecksTheUtf8Row ()
    {
        var utf8Row = new[] { EncodingMenuBuilder.RowName(Encoding.UTF8) };

        Assert.Multiple(() =>
        {
            foreach (var utf8 in new[] { Encoding.UTF8, new UTF8Encoding(false), Encoding.Default })
            {
                _controller.UpdateEncodingMenu(utf8);

                Assert.That(CheckedEncodingRowNames(), Is.EqualTo(utf8Row).AsCollection);
            }
        });
    }

    /// <summary>
    /// A file read with an encoding the menu does not offer leaves every row unchecked — checking one
    /// would claim a row reproduces the file's encoding when clicking it would change it.
    /// </summary>
    [Test]
    public void UpdateEncodingMenu_EncodingThatIsNotOffered_ChecksNothing ()
    {
        _controller.UpdateEncodingMenu(Encoding.BigEndianUnicode);

        Assert.That(CheckedEncodingRowNames(), Is.Empty);
    }

    private List<string> CheckedEncodingRowNames ()
    {
        return [.. _encodingMenu.DropDownItems
            .Cast<ToolStripItem>()
            .OfType<ToolStripMenuItem>()
            .Where(row => row.Checked)
            .Select(row => row.Name)];
    }

    [Test]
    public void UpdateHighlightGroups_PopulatesComboBox ()
    {
        var groups = new[] { "Default", "Errors", "Warnings" };
        _controller.UpdateHighlightGroups(groups, "Errors");

        var combo = _toolbar.Items["highlightGroupsToolStripComboBox"] as ToolStripComboBox;
        Assert.That(combo, Is.Not.Null);
        Assert.That(combo.Items, Has.Count.EqualTo(3));
        Assert.That(combo.Text, Is.EqualTo("Errors"));
    }

    [Test]
    public void PopulateFileHistory_CreatesMenuItems ()
    {
        var history = new[] { @"C:\log1.txt", @"C:\log2.txt" };
        _controller.PopulateFileHistory(history);

        var lastUsed = FindMenuItem("lastUsedToolStripMenuItem");
        Assert.That(lastUsed.DropDownItems, Has.Count.EqualTo(2));
        Assert.That(lastUsed.DropDownItems[0].Text, Is.EqualTo(@"C:\log1.txt"));
    }

    [Test]
    public void HistoryItemClicked_RaisesEvent ()
    {
        string clickedFile = null;
        _controller.HistoryItemClicked += (_, e) => clickedFile = e.FileName;

        _controller.PopulateFileHistory(["test.log"]);
        var lastUsed = FindMenuItem("lastUsedToolStripMenuItem");

        // Simulate click via reflection — OnItemClicked is protected on ToolStrip
        var args = new ToolStripItemClickedEventArgs(lastUsed.DropDownItems[0]);
        var onItemClicked = typeof(ToolStrip).GetMethod("OnItemClicked", BindingFlags.Instance | BindingFlags.NonPublic);
        _ = onItemClicked.Invoke(lastUsed.DropDown, [args]);

        Assert.That(clickedFile, Is.EqualTo("test.log"));
    }

    [Test]
    public void HighlightGroupSelected_RaisesEvent_OnComboChange ()
    {
        string selectedGroup = null;
        _controller.HighlightGroupSelected += (_, e) => selectedGroup = e.GroupName;

        // Populate first
        _controller.UpdateHighlightGroups(["Default", "Errors"], "Default");

        // Simulate user selecting "Errors"
        var combo = _toolbar.Items["highlightGroupsToolStripComboBox"] as ToolStripComboBox;
        Assert.That(combo, Is.Not.Null, "Expected highlightGroupsToolStripComboBox to be a ToolStripComboBox.");
        combo.SelectedIndex = 1; // This triggers SelectedIndexChanged

        Assert.That(selectedGroup, Is.EqualTo("Errors"));
    }

    [Test]
    public void Dispose_UnsubscribesEvents ()
    {
        string selectedGroup = null;
        _controller.HighlightGroupSelected += (_, e) => selectedGroup = e.GroupName;

        // Populate combo so we can change selection
        _controller.UpdateHighlightGroups(["Default", "Errors"], "Errors");

        _controller.Dispose();

        // Changing selection after dispose should not raise HighlightGroupSelected
        var combo = _toolbar.Items["highlightGroupsToolStripComboBox"] as ToolStripComboBox;
        Assert.That(combo, Is.Not.Null, "Expected highlightGroupsToolStripComboBox to be a ToolStripComboBox.");
        combo.SelectedIndex = 0;

        Assert.That(selectedGroup, Is.Null);
    }

    [Test]
    public void InitializeMenus_NullMainMenu_ThrowsArgumentNullException ()
    {
        var controller = new MenuToolbarController();
        _ = Assert.Throws<ArgumentNullException>(() =>
            controller.InitializeMenus(null, _toolbar, null, _dragControl, _followTailCheckBox));
    }

    [Test]
    public void InitializeMenus_NullButtonToolbar_ThrowsArgumentNullException ()
    {
        var controller = new MenuToolbarController();
        _ = Assert.Throws<ArgumentNullException>(() =>
            controller.InitializeMenus(_mainMenu, null, null, _dragControl, _followTailCheckBox));
    }

    [Test]
    public void InitializeMenus_NullDragControl_ThrowsArgumentNullException ()
    {
        var controller = new MenuToolbarController();
        _ = Assert.Throws<ArgumentNullException>(() =>
            controller.InitializeMenus(_mainMenu, _toolbar, null, null, _followTailCheckBox));
    }

    [Test]
    public void InitializeMenus_NullCheckBox_ThrowsArgumentNullException ()
    {
        var controller = new MenuToolbarController();
        _ = Assert.Throws<ArgumentNullException>(() =>
            controller.InitializeMenus(_mainMenu, _toolbar, null, _dragControl, null));
    }

    [Test]
    public void UpdateGuiState_NullState_ThrowsArgumentNullException ()
    {
        _ = Assert.Throws<ArgumentNullException>(() => _controller.UpdateGuiState(null, false));
    }

    [Test]
    public void UpdateGuiState_SetsMultiFileState ()
    {
        var state = new GuiStateEventArgs
        {
            MultiFileEnabled = true,
            IsMultiFileActive = true,
            MenuEnabled = true
        };
        _controller.UpdateGuiState(state, false);

        var multiFileItem = FindMenuItem("multiFileToolStripMenuItem");
        var multiFileEnabledItem = FindMenuItem("multiFileEnabledStripMenuItem");
        Assert.That(multiFileItem.Enabled, Is.True);
        Assert.That(multiFileItem.Checked, Is.True);
        Assert.That(multiFileEnabledItem.Checked, Is.True);
    }

    [Test]
    public void UpdateGuiState_SetsCellSelectMode ()
    {
        var state = new GuiStateEventArgs { CellSelectMode = true, MenuEnabled = true };
        _controller.UpdateGuiState(state, false);

        var cellSelectItem = FindMenuItem("cellSelectModeToolStripMenuItem");
        Assert.That(cellSelectItem.Checked, Is.True);
    }

    [Test]
    public void UpdateGuiState_SetsBookmarkBubblesButton ()
    {
        var state = new GuiStateEventArgs { ShowBookmarkBubbles = true, MenuEnabled = true };
        _controller.UpdateGuiState(state, false);

        var bubblesButton = _toolbar.Items["toolStripButtonBubbles"] as ToolStripButton;
        Assert.That(bubblesButton, Is.Not.Null, "Expected toolStripButtonBubbles to be a ToolStripButton.");
        Assert.That(bubblesButton.Checked, Is.True);
    }

    [Test]
    public void UpdateGuiState_SetsColumnFinderVisible ()
    {
        var state = new GuiStateEventArgs { ColumnFinderVisible = true, MenuEnabled = true };
        _controller.UpdateGuiState(state, false);

        var columnFinderItem = FindMenuItem("columnFinderToolStripMenuItem");
        Assert.That(columnFinderItem.Checked, Is.True);
    }

    [Test]
    public void UpdateGuiState_SetsHighlightGroupName ()
    {
        var state = new GuiStateEventArgs { HighlightGroupName = "Errors", MenuEnabled = true };
        _controller.UpdateGuiState(state, false);

        var combo = _toolbar.Items["highlightGroupsToolStripComboBox"] as ToolStripComboBox;
        Assert.That(combo.Text, Is.EqualTo("Errors"));
    }

    [Test]
    public void UpdateGuiState_SetsMenuEnabled ()
    {
        var state = new GuiStateEventArgs { MenuEnabled = false };
        _controller.UpdateGuiState(state, false);

        Assert.That(_mainMenu.Enabled, Is.False);
    }

    [Test]
    public void UpdateHighlightGroups_DoesNotRaiseEvent_DuringProgrammaticUpdate ()
    {
        string selectedGroup = null;
        _controller.HighlightGroupSelected += (_, e) => selectedGroup = e.GroupName;

        _controller.UpdateHighlightGroups(["Default", "Errors"], "Errors");

        Assert.That(selectedGroup, Is.Null);
    }

    private ToolStripMenuItem FindMenuItem (string name)
    {
        return FindItemRecursive<ToolStripMenuItem>(_mainMenu.Items, name);
    }

    private static T FindItemRecursive<T> (ToolStripItemCollection items, string name) where T : ToolStripItem
    {
        foreach (ToolStripItem item in items)
        {
            if (item.Name == name && item is T typed)
            {
                return typed;
            }

            if (item is ToolStripDropDownItem dropDown)
            {
                var found = FindItemRecursive<T>(dropDown.DropDownItems, name);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    public void Dispose ()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _controller?.Dispose();
            _mainMenu?.Dispose();
            _toolbar?.Dispose();
            _dragControl?.Dispose();
            _followTailCheckBox?.Dispose();
        }

        _disposed = true;
    }
}