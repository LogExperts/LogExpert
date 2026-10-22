using System;
using System.Runtime.Versioning;
using System.Text;

using LogExpert.Core.Helpers;

namespace LogExpert.UI.Services.MenuToolbarService;

/// <summary>
/// Builds the View → Encoding dropdown from <see cref="EncodingRegistry.OfferedEncodings"/>, and reads
/// the encoding back off a row.
/// </summary>
/// <remarks>
/// The rows used to be hand-declared in the designer, one per encoding, each with its own click handler
/// and its own branch in the check-state lookup — so the menu could (and did) drift from the Preferences
/// list, and adding an encoding meant editing all three places. Here every row carries the
/// <see cref="Encoding"/> it stands for in its <c>Tag</c>: the handler applies whatever the clicked row
/// carries and the check-state lookup compares against it, so neither knows the list.
/// </remarks>
[SupportedOSPlatform("windows")]
internal static class EncodingMenuBuilder
{
    /// <summary>
    /// Replaces <paramref name="encodingMenu"/>'s rows with one per offered encoding, labelled with its
    /// <see cref="Encoding.HeaderName"/> — the same name the Preferences combo box shows.
    /// </summary>
    /// <param name="encodingMenu">The Encoding dropdown to fill.</param>
    /// <param name="onRowClick">Handler for a row click; read the encoding with <see cref="EncodingOf"/>.</param>
    internal static void Fill (ToolStripMenuItem encodingMenu, EventHandler onRowClick)
    {
        ArgumentNullException.ThrowIfNull(encodingMenu);

        encodingMenu.DropDownItems.Clear();

        foreach (var encoding in EncodingRegistry.OfferedEncodings)
        {
            var row = new ToolStripMenuItem(encoding.HeaderName)
            {
                Name = RowName(encoding),
                Tag = encoding,
                // Carried over from the designer-declared rows, which set both on every encoding row.
                BackColor = SystemColors.Control,
                ForeColor = SystemColors.ControlDarkDark
            };

            row.Click += onRowClick;

            _ = encodingMenu.DropDownItems.Add(row);
        }
    }

    /// <summary>
    /// The designer-style name of the row for <paramref name="encoding"/>. Keyed on the code page
    /// because that is what identifies a row; the header name is display text.
    /// </summary>
    internal static string RowName (Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        return $"encoding{encoding.CodePage}ToolStripMenuItem";
    }

    /// <summary>
    /// The encoding <paramref name="row"/> stands for, or null when it is not an encoding row.
    /// </summary>
    internal static Encoding EncodingOf (ToolStripItem row)
    {
        return row?.Tag as Encoding;
    }
}
