namespace LogExpert.Core.Interfaces;

public interface IFileViewContext
{
    ILogView LogView { get; }

    /// <summary>
    /// The view's paint context. Core sees only its line-source role; the UI layer extends it
    /// with the paint-specific members (fonts, colors, highlight lookup).
    /// </summary>
    ILogLineSource LogPaintContext { get; }
}
