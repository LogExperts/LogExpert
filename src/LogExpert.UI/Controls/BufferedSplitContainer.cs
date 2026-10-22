using System.Runtime.Versioning;

namespace LogExpert.UI.Controls;

/// <summary>
/// A <see cref="SplitContainer"/> that paints itself and all of its descendants composited (off-screen, bottom-to-top)
/// via the <c>WS_EX_COMPOSITED</c> extended window style.
///
/// <para>This removes the flicker that occurs while the splitter is dragged: anchored child controls (e.g. the
/// right-aligned filter-count label) are physically repositioned on every mouse move, and because they own their own
/// window handle, double-buffering the parent panel is not enough to stop them flickering. Compositing buffers the
/// entire control tree, so the drag is smooth (see issue #560). </para>
/// </summary>
/// <remarks>https://pinvoke.net/default.aspx/Enums/WindowStyles.html</remarks>
[SupportedOSPlatform("windows")]
internal sealed class BufferedSplitContainer : SplitContainer
{
    /// <summary>
    /// Specifies a window that paints all descendants in bottom-to-top painting order using double-buffering.
    /// This cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC. This style is not supported in Windows 2000.
    /// </summary>
    /// <remarks>
    /// With WS_EX_COMPOSITED set, all descendants of a window get bottom-to-top painting order using double-buffering.
    /// Bottom-to-top painting order allows a descendent window to have translucency (alpha) and transparency (color-key) effects,
    /// but only if the descendent window also has the WS_EX_TRANSPARENT bit set.
    /// Double-buffering allows the window and its descendents to be painted without flicker.
    /// </remarks>
    private const int WS_EX_COMPOSITED = 0x02000000;

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ExStyle |= WS_EX_COMPOSITED;
            return createParams;
        }
    }
}
