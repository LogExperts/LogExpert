using LogExpert.Core.Classes.Persister;

namespace LogExpert.Core.Interfaces;

/// <summary>
/// A window whose persistable state can be captured as a Session Snapshot.
/// </summary>
/// <remarks>
/// This is the role of a Log Window that a <see cref="Classes.Filter.FilterPipe"/> keeps for the
/// window displaying its results (<c>OwnLogWindow</c>): saving a Session File recursively
/// gathers a child snapshot per filter-pipe tab.
/// </remarks>
public interface ISessionSnapshotSource
{
    /// <summary>
    /// Gathers a Session Snapshot of this window's persistable state, including recursive
    /// child snapshots for its filter pipe tabs.
    /// </summary>
    /// <returns>
    /// A <see cref="SessionSnapshot"/> capturing the current state of the window: current line,
    /// filters, columnizer configuration, and other settings.
    /// </returns>
    /// <remarks>
    /// The snapshot is mapped to the Session File's serialized form by
    /// <see cref="SessionFileComposer"/> and applied back in two phases when a session is
    /// loaded.
    /// </remarks>
    SessionSnapshot GatherSessionSnapshot ();
}
