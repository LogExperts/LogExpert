using LogExpert.Core.Entities;

namespace LogExpert.Core.Interface;

public interface IFileViewContext
{
    ILogView LogView { get; }
    ILogPaintContext LogPaintContext { get; }
}