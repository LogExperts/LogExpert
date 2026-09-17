using System.Text;

using LogExpert.Core.Helpers;
using LogExpert.Dialogs;
using LogExpert.UI.Services.MenuToolbarService;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Encodings;

/// <summary>
/// <see cref="EncodingRegistry.OfferedEncodings"/> is the one list of encodings a user can pick: the
/// Preferences default-encoding combo box and the per-file View → Encoding menu are both built from it.
/// These tests pin the invariants that list has to hold for either UI to work, so they are asserted once
/// here rather than per UI.
/// </summary>
[TestFixture]
public class OfferedEncodingsTests
{
    [Test]
    [TestCase(1250, TestName = "OfferedEncodings_OffersWindows1250")]
    [TestCase(1252, TestName = "OfferedEncodings_OffersWindows1252")]
    [TestCase(936, TestName = "OfferedEncodings_OffersGb2312")]
    public void OfferedEncodings_OffersLegacyCodePage (int codePage)
    {
        Assert.That(EncodingRegistry.OfferedEncodings.Select(encoding => encoding.CodePage), Does.Contain(codePage));
    }

    /// <summary>
    /// Every offered encoding is saved as its name and resolved from that name on the next start, so a
    /// name that cannot be resolved again would silently degrade to a fallback.
    /// </summary>
    [Test]
    public void OfferedEncodings_EveryEntryResolvesByItsPersistedName ()
    {
        Assert.Multiple(() =>
        {
            foreach (var encoding in EncodingRegistry.OfferedEncodings)
            {
                Assert.That(
                    EncodingRegistry.TryGetEncoding(encoding.HeaderName, out _),
                    Is.True,
                    $"'{encoding.HeaderName}' cannot be resolved back from a saved preference");
            }
        });
    }

    /// <summary>
    /// Both UIs render an entry as its <see cref="Encoding.HeaderName"/>, so two entries sharing a code
    /// page are two rows the user cannot tell apart. <c>Encoding.Default</c> and
    /// <see cref="Encoding.UTF8"/> are both code page 65001 on .NET and both read <c>utf-8</c> — the
    /// duplicate reported in issue #688.
    /// </summary>
    [Test]
    public void OfferedEncodings_NoTwoEntriesShareACodePage ()
    {
        Assert.That(EncodingRegistry.OfferedEncodings.Select(encoding => encoding.CodePage), Is.Unique);
    }

    /// <summary>
    /// The same, stated as the property the reporter actually saw: no two rows carry the same label.
    /// </summary>
    [Test]
    public void OfferedEncodings_NoTwoEntriesShareAHeaderName ()
    {
        Assert.That(EncodingRegistry.OfferedEncodings.Select(encoding => encoding.HeaderName), Is.Unique);
    }

    /// <summary>
    /// The list is shared and handed out repeatedly (every Preferences open, every window's encoding
    /// menu), so it must be a stable snapshot rather than something a caller could append to.
    /// </summary>
    [Test]
    public void OfferedEncodings_IsTheSameListEveryTime ()
    {
        Assert.That(EncodingRegistry.OfferedEncodings, Is.SameAs(EncodingRegistry.OfferedEncodings));
    }

    /// <summary>
    /// The Preferences selection is persisted by name and restored with
    /// <c>comboBoxEncoding.SelectedItem = EncodingRegistry.GetEncoding(name, …)</c>. WinForms locates that
    /// item with <see cref="object.Equals(object)"/>, so an entry whose name resolves back to an instance
    /// that is not equal to it can never be reselected — the row goes dead after the first restart.
    /// </summary>
    [Test]
    public void OfferedEncodings_EveryEntryIsReselectableAfterARoundTrip ()
    {
        Assert.Multiple(() =>
        {
            foreach (var encoding in EncodingRegistry.OfferedEncodings)
            {
                var restored = EncodingRegistry.GetEncoding(encoding.HeaderName, SettingsDialog.FallbackEncoding);

                Assert.That(
                    restored,
                    Is.EqualTo(encoding),
                    $"picking '{encoding.HeaderName}' saves a name that reselects a different entry");
            }
        });
    }

    /// <summary>
    /// <c>FillDialog</c> and <c>SavePreferences</c> fall back to this encoding when the persisted name is
    /// unusable, so it has to be an offered entry — otherwise the dropdown shows no selection and OK
    /// rewrites the preference to something that was never on the list.
    /// </summary>
    [Test]
    public void OfferedEncodings_OffersTheSettingsDialogFallback ()
    {
        Assert.That(EncodingRegistry.OfferedEncodings, Does.Contain(SettingsDialog.FallbackEncoding));
    }

    /// <summary>
    /// The point of the shared list: the encoding menu offers exactly what Preferences offers, in the
    /// same order. Before this, the menu was hand-declared in the designer and had drifted — it lacked
    /// windows-1250 and windows-1252, and carried an "ANSI" row Preferences did not.
    /// </summary>
    [Test]
    [Apartment(ApartmentState.STA)]
    public void EncodingMenu_OffersExactlyTheOfferedEncodings ()
    {
        using var encodingMenu = new ToolStripMenuItem("Encoding");

        EncodingMenuBuilder.Fill(encodingMenu, (_, _) => { });

        var rows = encodingMenu.DropDownItems.Cast<ToolStripItem>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(
                rows.Select(EncodingMenuBuilder.EncodingOf),
                Is.EqualTo(EncodingRegistry.OfferedEncodings).AsCollection);

            Assert.That(
                rows.Select(row => row.Text),
                Is.EqualTo(EncodingRegistry.OfferedEncodings.Select(encoding => encoding.HeaderName)).AsCollection,
                "a row is labelled with its encoding's header name, the same text the Preferences combo shows");
        });
    }

    /// <summary>
    /// Filling twice — a second Log Tab Window, or a rebuild — must not stack a second set of rows.
    /// </summary>
    [Test]
    [Apartment(ApartmentState.STA)]
    public void EncodingMenu_FilledTwice_DoesNotDuplicateRows ()
    {
        using var encodingMenu = new ToolStripMenuItem("Encoding");

        EncodingMenuBuilder.Fill(encodingMenu, (_, _) => { });
        EncodingMenuBuilder.Fill(encodingMenu, (_, _) => { });

        Assert.That(encodingMenu.DropDownItems, Has.Count.EqualTo(EncodingRegistry.OfferedEncodings.Count));
    }

    /// <summary>
    /// Clicking a row has to hand the click handler that row's encoding — that is the whole mechanism by
    /// which one handler serves every row.
    /// </summary>
    [Test]
    [Apartment(ApartmentState.STA)]
    public void EncodingMenu_ClickingARow_YieldsThatRowsEncoding ()
    {
        using var encodingMenu = new ToolStripMenuItem("Encoding");
        var clicked = new List<Encoding>();

        EncodingMenuBuilder.Fill(encodingMenu, (sender, _) => clicked.Add(EncodingMenuBuilder.EncodingOf(sender as ToolStripItem)));

        foreach (var row in encodingMenu.DropDownItems.Cast<ToolStripItem>().ToList())
        {
            row.PerformClick();
        }

        Assert.That(clicked, Is.EqualTo(EncodingRegistry.OfferedEncodings).AsCollection);
    }
}
