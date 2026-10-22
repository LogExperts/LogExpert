using System.Runtime.Versioning;

using WeifenLuo.WinFormsUI.Docking;

namespace LogExpert.UI.Theming;

/// <summary>
/// Installs the dark ToolStrip renderer for the whole process.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class ToolStripTheme
{
    private static readonly Lock _lock = new();

    private static bool _installed;

    /// <summary>
    /// Installs <see cref="DarkToolStripRenderer"/>, built from the same VS2015 palette the
    /// dock panel uses, so toolbar, status bar and menus share one set of colours.
    ///
    /// The renderer is deliberately process-wide: every ToolStrip, MenuStrip, StatusStrip and
    /// ContextMenuStrip that uses <see cref="ToolStripRenderMode.ManagerRenderMode"/> picks it
    /// up, which is what makes the tab menu, the LogWindow context menus and plugin menus dark
    /// as well. Strips inside a ToolStripContainer additionally have to be switched to
    /// ManagerRenderMode by their owner, because the container forces RenderMode = System.
    ///
    /// The setting is not reverted when the user switches back to light mode. The colour mode
    /// is read once by <c>Program.SetColorMode()</c> before the first window is created and
    /// changing it requires an application restart, so there is no supported runtime switch
    /// that would need to undo this.
    /// </summary>
    public static void Apply ()
    {
        if (!Application.IsDarkModeEnabled)
        {
            return;
        }

        lock (_lock)
        {
            if (_installed)
            {
                return;
            }

            var theme = new VS2015DarkTheme();
            ToolStripManager.Renderer = new DarkToolStripRenderer(theme.ColorPalette);
            _installed = true;
        }
    }
}
