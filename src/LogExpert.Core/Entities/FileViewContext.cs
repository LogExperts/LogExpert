using LogExpert.Core.Interfaces;

namespace LogExpert.Core.Entities;

public class FileViewContext(ILogLineSource logPaintContext, ILogView logView) : IFileViewContext
{
    #region Properties

    public ILogLineSource LogPaintContext { get; } = logPaintContext;

    public ILogView LogView { get; } = logView;

    #endregion
}
