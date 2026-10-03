using System.Reflection;

using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Config;
using LogExpert.Core.Entities;
using LogExpert.Core.Interfaces;
using LogExpert.Dialogs;
using LogExpert.UI.Dialogs.Highlight;

using Moq;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Dialogs;

[TestFixture]
[Apartment(ApartmentState.STA)]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class HighlightDialogEntryTests
{
    private Settings _settings;
    private HighlightDialog _dialog;
    private HighlightEntry _source;

    [SetUp]
    public void SetUp ()
    {
        _settings = new Settings();
        _settings.Preferences.HighlightCustomColors = [0x00102030];
        var config = new Mock<IConfigManager>();
        _ = config.SetupGet(c => c.Settings).Returns(_settings);

        _source = new HighlightEntry
        {
            SearchText = "ERR.*",
            IsRegex = true,
            IsCaseSensitive = true,
            ForegroundColor = Color.Red,
            BackgroundColor = Color.Yellow,
            IsBold = true,
            IsSetBookmark = true,
            BookmarkComment = "note",
            IsActionEntry = true,
            ActionEntry = new ActionEntry { PluginName = "plugin", ActionParam = "param" },
            AlertOnHit = true,
            CooldownSeconds = 7,
        };

        _dialog = new HighlightDialog(config.Object)
        {
            HighlightGroupList = [new HighlightGroup { GroupName = "G", HighlightEntryList = [_source] }],
            PreSelectedGroupName = "G",
        };
        _dialog.Show();
        // Shown, which fills the entry list, is posted rather than raised by Show().
        Application.DoEvents();
        // The dialog edits clones of the groups it is given.
        _source = Entries[0];
    }

    [TearDown]
    public void TearDown ()
    {
        _dialog.Dispose();
    }

    private List<HighlightEntry> Entries => _dialog.HighlightGroupList[0].HighlightEntryList;

    private ListBox List => Find<ListBox>("listBoxHighlight");

    [Test]
    public void Copy_IsDisabledWithoutASelectedEntry ()
    {
        List.SelectedIndex = -1;
        Assert.That(Find<Button>("btnCopy").Enabled, Is.False);

        List.SelectedIndex = 0;
        Assert.That(Find<Button>("btnCopy").Enabled, Is.True);
    }

    [Test]
    public void CopyAccepted_AppendsOneIndependentEntryAndSelectsIt ()
    {
        HighlightEntry edited = null;
        _dialog.ShowEntryDialog = editor =>
        {
            edited = EditedEntry(editor);
            return ClickOk(editor);
        };
        List.SelectedIndex = 0;

        Find<Button>("btnCopy").PerformClick();

        Assert.That(Entries, Has.Count.EqualTo(2));
        var copy = Entries[1];
        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.SameAs(edited));
            Assert.That(copy, Is.Not.SameAs(_source));
            Assert.That(List.SelectedItem, Is.SameAs(copy));
            Assert.That(copy.SearchText, Is.EqualTo("ERR.*"));
            Assert.That(copy.IsRegex, Is.True);
            Assert.That(copy.IsCaseSensitive, Is.True);
            Assert.That(copy.ForegroundColor.ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
            Assert.That(copy.BackgroundColor.ToArgb(), Is.EqualTo(Color.Yellow.ToArgb()));
            Assert.That(copy.IsBold, Is.True);
            Assert.That(copy.IsSetBookmark, Is.True);
            Assert.That(copy.BookmarkComment, Is.EqualTo("note"));
            Assert.That(copy.IsActionEntry, Is.True);
            Assert.That(copy.ActionEntry.PluginName, Is.EqualTo("plugin"));
            Assert.That(copy.ActionEntry.ActionParam, Is.EqualTo("param"));
            Assert.That(copy.AlertOnHit, Is.True);
            Assert.That(copy.CooldownSeconds, Is.EqualTo(7));
        });

        copy.ActionEntry.ActionParam = "changed";
        copy.SearchText = "changed";

        Assert.That(_source.ActionEntry.ActionParam, Is.EqualTo("param"));
        Assert.That(_source.SearchText, Is.EqualTo("ERR.*"));
    }

    [Test]
    public void CopyEditedInTheEditor_LeavesTheSourceUnchanged ()
    {
        _dialog.ShowEntryDialog = editor =>
        {
            editor.Controls.Find("_textBoxSearchString", true).Single().Text = "WARN";
            ((CheckBox)editor.Controls.Find("_checkBoxBold", true).Single()).Checked = false;
            return ClickOk(editor);
        };
        List.SelectedIndex = 0;

        Find<Button>("btnCopy").PerformClick();

        Assert.That(Entries[1].SearchText, Is.EqualTo("WARN"));
        Assert.That(Entries[1].IsBold, Is.False);
        Assert.That(_source.SearchText, Is.EqualTo("ERR.*"));
        Assert.That(_source.IsBold, Is.True);
    }

    [Test]
    public void CopyCancelled_AddsNothing ()
    {
        _dialog.ShowEntryDialog = _ => DialogResult.Cancel;
        List.SelectedIndex = 0;

        Find<Button>("btnCopy").PerformClick();

        Assert.That(Entries, Is.EqualTo(new[] { _source }));
        Assert.That(List.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public void Palette_StartsFromThePreferencesWithoutAliasingThem ()
    {
        Assert.That(_dialog.CustomColors, Is.EqualTo(new[] { 0x00102030 }));
        Assert.That(_dialog.CustomColors, Is.Not.SameAs(_settings.Preferences.HighlightCustomColors));
    }

    [Test]
    public void EditorOk_SharesItsPaletteWithTheNextEditor ()
    {
        int[] received = null;
        _dialog.ShowEntryDialog = editor =>
        {
            editor.CustomColors = [0x00ABCDEF];
            return DialogResult.OK;
        };
        List.SelectedIndex = 0;
        Find<Button>("btnEdit").PerformClick();

        _dialog.ShowEntryDialog = editor =>
        {
            received = editor.CustomColors;
            return DialogResult.Cancel;
        };
        Find<Button>("btnAdd").PerformClick();

        Assert.That(received, Is.EqualTo(new[] { 0x00ABCDEF }));
        Assert.That(_dialog.CustomColors, Is.EqualTo(new[] { 0x00ABCDEF }));
        Assert.That(_settings.Preferences.HighlightCustomColors, Is.EqualTo(new[] { 0x00102030 }));
    }

    [Test]
    public void EditorCancel_DiscardsItsPalette ()
    {
        _dialog.ShowEntryDialog = editor =>
        {
            editor.CustomColors = [0x00ABCDEF];
            return DialogResult.Cancel;
        };
        List.SelectedIndex = 0;

        Find<Button>("btnCopy").PerformClick();

        Assert.That(_dialog.CustomColors, Is.EqualTo(new[] { 0x00102030 }));
    }

    [Test]
    public void SelectionColorPickerOk_SharesThePaletteWithTheEntryEditors ()
    {
        int[] shown = null;
        int[] received = null;
        _dialog.ShowColorDialog = picker =>
        {
            shown = picker.CustomColors;
            picker.CustomColors = [0x00ABCDEF];
            return DialogResult.OK;
        };
        Find<Button>("btnSelectionColor").PerformClick();

        _dialog.ShowEntryDialog = editor =>
        {
            received = editor.CustomColors;
            return DialogResult.Cancel;
        };
        Find<Button>("btnAdd").PerformClick();

        Assert.That(shown[0], Is.EqualTo(0x00102030));
        Assert.That(received[0], Is.EqualTo(0x00ABCDEF));
    }

    [Test]
    public void SelectionColorPickerCancel_KeepsThePalette ()
    {
        _dialog.ShowColorDialog = picker =>
        {
            picker.CustomColors = [0x00ABCDEF];
            return DialogResult.Cancel;
        };

        Find<Button>("btnSelectionColor").PerformClick();

        Assert.That(_dialog.CustomColors, Is.EqualTo(new[] { 0x00102030 }));
    }

    private static HighlightEntry EditedEntry (HighlightEntryDialog editor)
    {
        return (HighlightEntry)typeof(HighlightEntryDialog).GetField("_entry", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor);
    }

    private static DialogResult ClickOk (HighlightEntryDialog editor)
    {
        _ = typeof(HighlightEntryDialog).GetMethod("OnOkClick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(editor, [editor, EventArgs.Empty]);
        return editor.DialogResult;
    }

    private T Find<T> (string name) where T : Control
    {
        return (T)_dialog.Controls.Find(name, true).Single();
    }
}
