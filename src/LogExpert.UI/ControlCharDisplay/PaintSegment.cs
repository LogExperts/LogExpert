using System.Drawing;

namespace LogExpert.UI.ControlCharDisplay;

internal readonly record struct PaintSegment(
    string RenderedText,
    Color ForeColor,
    Color BackColor,
    bool IsBold,
    bool IsItalic,
    bool NoBackground,
    bool IsSubstituted);
