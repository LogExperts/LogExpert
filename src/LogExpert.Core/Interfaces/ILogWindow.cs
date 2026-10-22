namespace LogExpert.Core.Interfaces;

/// <summary>
/// The full Core-visible face of a Log Window, composed from its role interfaces.
/// Core consumers take the single role they need (<see cref="ILogLineSource"/> for the
/// columnizer callbacks, <see cref="ILineSelectable"/> and <see cref="ISessionSnapshotSource"/>
/// for a Filter Pipe's window references); this composition remains for holders that only
/// need the window's identity, such as <see cref="EventArguments.FilterListChangedEventArgs"/>.
/// </summary>
public interface ILogWindow : ILogLineSource, ILineSelectable, ISessionSnapshotSource
{
}
