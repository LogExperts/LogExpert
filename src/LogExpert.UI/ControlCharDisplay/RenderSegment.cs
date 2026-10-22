namespace LogExpert.UI.ControlCharDisplay;

internal readonly record struct RenderSegment(
    int SourceStart,
    int SourceLength,
    string RenderedText,
    bool IsSubstituted);
