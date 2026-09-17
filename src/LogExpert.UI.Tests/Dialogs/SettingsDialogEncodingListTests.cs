using System.Text;

using LogExpert.Core.Helpers;
using LogExpert.Dialogs;

using NUnit.Framework;

namespace LogExpert.UI.Tests.Dialogs;

/// <summary>
/// The Preferences encoding combo box, which is the only way to set <c>Preferences.DefaultEncoding</c>.
/// What it offers is <see cref="EncodingRegistry.OfferedEncodings"/> and is asserted in
/// <c>OfferedEncodingsTests</c>; what is left here is the part only a real
/// <see cref="ComboBox"/> can answer.
/// </summary>
[TestFixture]
public class SettingsDialogEncodingListTests
{
    /// <summary>
    /// The persist/restore round trip against a real <see cref="ComboBox"/>, configured the way the
    /// dialog configures it: the reselect goes through <c>Items.IndexOf</c>, and WinForms — not NUnit —
    /// has the final say on whether a row is reachable. Deliberately kept alongside the plain assertion
    /// in <c>OfferedEncodingsTests</c> rather than replacing it: this one needs an STA apartment and a
    /// WinForms control, and the invariant should still be pinned where that is unavailable.
    /// </summary>
    [Test]
    [Apartment(ApartmentState.STA)]
    public void EncodingComboBox_EveryOfferedRowIsReselectableAfterARestart ()
    {
        var encodings = EncodingRegistry.OfferedEncodings;

        using var comboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FormattingEnabled = true };
        comboBox.Items.AddRange([.. encodings]);

        Assert.Multiple(() =>
        {
            for (var row = 0; row < encodings.Count; row++)
            {
                comboBox.SelectedIndex = row;
                var saved = ((Encoding)comboBox.SelectedItem).HeaderName;

                comboBox.SelectedIndex = -1;
                comboBox.SelectedItem = EncodingRegistry.GetEncoding(saved, SettingsDialog.FallbackEncoding);

                Assert.That(comboBox.SelectedIndex, Is.EqualTo(row), $"row {row} ('{saved}') is unreachable after a restart");
            }
        });
    }

    /// <summary>
    /// The combo box has no <c>DisplayMember</c> — the dialog only sets <c>ValueMember</c>, and WinForms
    /// falls back to it for the display text. Pinned because without that fallback every row would render
    /// as <c>Encoding.ToString()</c>, i.e. a .NET type name, and the rows the user picks between would be
    /// indistinguishable for a different reason than the one issue #688 reported.
    /// </summary>
    [Test]
    [Apartment(ApartmentState.STA)]
    public void EncodingComboBox_RendersEachRowAsItsHeaderName ()
    {
        using var comboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FormattingEnabled = true };
        comboBox.ValueMember = "HeaderName";
        comboBox.Items.AddRange([.. EncodingRegistry.OfferedEncodings]);

        Assert.Multiple(() =>
        {
            foreach (Encoding encoding in comboBox.Items)
            {
                Assert.That(comboBox.GetItemText(encoding), Is.EqualTo(encoding.HeaderName));
            }
        });
    }
}
