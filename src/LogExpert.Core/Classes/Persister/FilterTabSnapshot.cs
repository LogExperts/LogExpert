using LogExpert.Core.Classes.Filter;

namespace LogExpert.Core.Classes.Persister;

/// <summary>
/// One Filter Pipe tab's persistable state: the filter that feeds the tab plus the tab's own
/// window state as a nested snapshot — the recursion point of <see cref="SessionSnapshot"/>.
/// Mapped to and from <see cref="FilterTabData"/> by <see cref="SessionFileComposer"/>.
/// </summary>
public class FilterTabSnapshot
{
    public FilterParams FilterParams { get; set; }

    public SessionSnapshot Snapshot { get; set; }
}
