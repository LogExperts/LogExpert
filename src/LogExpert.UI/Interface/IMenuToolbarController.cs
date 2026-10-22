using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Versioning;

using LogExpert.Core.EventArguments;
using LogExpert.Dialogs;
using LogExpert.UI.Services.MenuToolbarService;

namespace LogExpert.UI.Interface;

/// <summary>
/// Controls menu and toolbar state based on application state.
/// Thread-safe UI updates via SynchronizationContext.
/// </summary>
[SupportedOSPlatform("windows")]
internal interface IMenuToolbarController : IDisposable
{
    /// <summary>
    /// Initializes controller with menu, toolbar, and timestamp control references.
    /// Must be called on UI thread.
    /// </summary>
    void InitializeMenus (MenuStrip mainMenu, ToolStrip buttonToolbar, ToolStrip externalToolsToolStrip,
        DateTimeDragControl dragControlDateTime, CheckBox checkBoxFollowTail);

    /// <summary>
    /// Updates all menus, toolbars, encoding, highlight group, and timestamp control
    /// based on the GUI state event args from a LogWindow.
    /// </summary>
    /// <remarks>
    /// Consumes <see cref="GuiStateEventArgs"/> directly — no intermediate mapping needed.
    /// </remarks>
    void UpdateGuiState (GuiStateEventArgs state, bool timestampControlEnabled);

    /// <summary>
    /// Checks the encoding menu row the file is currently read with, and unchecks every other row.
    /// Leaves all rows unchecked when the encoding is null or is one the menu does not offer.
    /// </summary>
    void UpdateEncodingMenu (Encoding currentEncoding);

    /// <summary>
    /// Updates highlight groups combo box.
    /// </summary>
    void UpdateHighlightGroups (IEnumerable<string> groups, string selectedGroup);

    /// <summary>
    /// Populates file history menu with recent files.
    /// Includes right-click removal support.
    /// </summary>
    void PopulateFileHistory (IEnumerable<string> fileHistory);

    // Events
    event EventHandler<HistoryItemClickedEventArgs> HistoryItemClicked;
    event EventHandler<HistoryItemClickedEventArgs> HistoryItemRemoveRequested;
    event EventHandler<HighlightGroupSelectedEventArgs> HighlightGroupSelected;
}
