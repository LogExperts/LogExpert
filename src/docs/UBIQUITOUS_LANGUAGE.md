# Ubiquitous Language

## Log File & Lines

| Term | Definition | Aliases to avoid |
|------|-----------|-----------------|
| **Log Line** | A single textual entry read from a log file, identified by a zero-based line number | Row, record, entry |
| **Log Line Memory** (`ILogLineMemory`) | A memory-efficient representation of a Log Line's content, providing access to the full line text | LogLine, line string |
| **Log Buffer** | A fixed-size block of cached Log Lines held in memory to avoid repeated disk reads | Cache, page |
| **Logfile Reader** (`LogfileReader`) | The component that reads a log file from disk, manages Log Buffers, and tracks file position and encoding | File reader, stream reader |
| **Position-Aware Stream Reader** | A stream reader that tracks its byte position within the file to enable random access to Log Lines | StreamReader |
| **Timestamp** | The date/time parsed from a Log Line by a Columnizer, used for time-based navigation and sync | Date, time |

## Columnizer (Plugin System)

| Term | Definition | Aliases to avoid |
|------|-----------|-----------------|
| **Columnizer** | A plugin that splits a single Log Line into named columns for display in the data grid | Parser, splitter, formatter |
| **Column** | A single named field extracted from a Log Line by a Columnizer | Field, cell |
| **Auto Columnizer** | A Columnizer that automatically detects the log format and delegates to the appropriate Columnizer | Default parser |
| **File System Plugin** (`IFileSystemPlugin`) | A plugin that provides file data to LogExpert, selected by URI scheme (e.g., local file, SFTP) | File provider, data source |

## Viewing & Navigation

| Term | Definition | Aliases to avoid |
|------|-----------|-----------------|
| **Log Window** (`ILogWindow`) | A UI component that displays and manages a single log file (or multi-file set) in the data grid | View, panel, editor |
| **Log Tab Window** | The top-level MDI container that hosts multiple Log Windows as tabs | Main window, MDI parent |
| **Log Tab Page** | A single tab within the Log Tab Window, wrapping one Log Window | Tab |
| **Tail Mode** | A real-time monitoring mode where the view automatically scrolls to show newly appended Log Lines | Follow, live view, watch |
| **Multi-File Mode** | A mode where multiple log files are displayed together as one virtual file within a single Log Window | Merged view |

## Filtering

| Term | Definition | Aliases to avoid |
|------|-----------|-----------------|
| **Filter Params** | The set of criteria (text, regex, case sensitivity) that define which Log Lines match a filter | Filter settings, search criteria |
| **Filter Pipe** (`FilterPipe`) | A mechanism that writes matched Log Lines to a temp file and maintains a line-number mapping back to the original file | Filter stream, filter output |
| **Filter-to-Tab** | The action of piping filtered results into a new Log Tab Page for isolated viewing | Filter view |

## Highlighting & Bookmarks

For **Bookmark**, **Marker**, and **Marker Bar**, use the definitions in
[CONTEXT.md](../../CONTEXT.md#marker-bar).

| Term | Definition | Aliases to avoid |
|------|-----------|-----------------|
| **Highlight Entry** | A rule that matches Log Lines by text or regex and applies visual formatting (colors, bold) | Hilight entry, highlight rule |
| **Highlight Group** | A named collection of Highlight Entries that can be activated or deactivated together | Hilight group, color scheme |
| **Bookmark Overlay** | A draggable visual annotation displayed on the data grid at a Bookmark's position | Tooltip, popup |
| **Auto-Generated Bookmark** | A transient Bookmark created by scanning Highlight Entries with the "set bookmark" flag; not persisted | Scan bookmark |

## Relationships

- A **Logfile Reader** manages a collection of **Log Buffers**, each holding a block of **Log Lines**
- A **Columnizer** splits a **Log Line** into one or more **Columns** and optionally parses its **Timestamp**
- A **Log Window** uses exactly one **Logfile Reader** and one active **Columnizer**
- A **Log Tab Window** contains one or more **Log Tab Pages**, each wrapping one **Log Window**
- A **Filter Pipe** applies **Filter Params** to a **Log Window** and maps filtered lines back to original line numbers
- A **Highlight Group** contains one or more **Highlight Entries**
- A **Highlight Entry** with "set bookmark" enabled can produce **Auto-Generated Bookmarks**
- A **File System Plugin** provides the file stream consumed by a **Logfile Reader**

## Example dialogue

> **Dev:** "When a user switches the **Columnizer** on a **Log Window**, do the **Bookmarks** move?"
> **Domain expert:** "No — **Bookmarks** are tied to line numbers, not to **Column** content. The **Columnizer** only changes how each **Log Line** is split into **Columns** for display."
> **Dev:** "And the **Filter Pipe** — does it re-run when the **Columnizer** changes?"
> **Domain expert:** "Not automatically. The **Filter Pipe** operates on the raw **Log Line** text using **Filter Params**. Switching the **Columnizer** doesn't invalidate the filter results."
> **Dev:** "What about **Tail Mode**? Does new content flow through an active **Filter Pipe**?"
> **Domain expert:** "Yes. In **Tail Mode**, the **Logfile Reader** detects appended **Log Lines**, and any active **Filter Pipe** evaluates them against its **Filter Params** in real time."

## Flagged ambiguities

- "Hilight" vs "Highlight" — the codebase historically used the misspelling "hilight" (e.g., `HilightEntryList`). The canonical term is **Highlight Entry** and **Highlight Group**. Legacy property names with the typo exist only for backward compatibility with old settings files.
- "Account" is not used in this domain; "User" refers to the person operating LogExpert, not an authentication identity.
- "Entry" is overloaded — it can mean a **Log Line** (log entry), a **Highlight Entry** (highlight rule), or an **Action Entry** (trigger action). Always qualify: **Log Line**, **Highlight Entry**, or **Action Entry**.
