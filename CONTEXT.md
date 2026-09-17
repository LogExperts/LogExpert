# LogExpert — Context & Ubiquitous Language

Glossary of domain terms used in this codebase. Terms here have a single, agreed
meaning; do not redefine them locally.

## Highlighting & triggers

- **Highlight Entry** (`HighlightEntry`) — One search-text rule plus the visual
  styling and trigger flags applied to lines that match it. Belongs to exactly
  one Highlight Group.
- **Highlight Group** (`HighlightGroup`) — A named, ordered list of Highlight
  Entries. The Log Window evaluates lines against the currently selected group.
- **Trigger** — A side-effecting action attached to a Highlight Entry that fires
  when a line matches. Current triggers: Set Bookmark, Stop Tail, Don't Dirty
  LED, Plugin (keyword action), and Audio Alert.
- **Action Entry** (`ActionEntry`) — Plugin name + parameters bound to the
  Plugin trigger of a Highlight Entry.
- **Tail trigger path** — The single code path in `LogWindow.CheckFilterAndHighlight`
  (one unified loop since the Filter Engine extraction) that evaluates
  highlight entries against *newly appended* lines. Triggers that have
  user-perceivable side effects (currently: Audio Alert) fire **only** on
  this path; bulk/scanner paths intentionally skip them — enforced by
  construction, since the bulk `HighlightBookmarkScanner` has no access to
  the side-effecting triggers.

## Marker Bar

- **Marker Bar** — A compact overview beside a Log Window that shows where
  highlights, bookmarks, Log Search hits, and Window Filter hits occur across
  the loaded file.
- **Marker** — An indicator in the Marker Bar representing one or more matching
  Log Lines. Markers are grouped by category; they do not create or replace
  Bookmarks.
- **Bookmark** — An annotation attached to a specific Log Line, with optional
  comment text. A Bookmark can be represented by a Marker in the bookmark
  category; the two terms are not synonyms.

*Avoid*: "marker" when referring to the underlying Bookmark annotation.

## Audio alerts

- **Audio Alert** — A sound played when a tail-only highlight match occurs.
  Toggled per Highlight Entry via `AlertOnHit`.
- **Sound File** (`SoundFilePath`) — Per-entry absolute path to an audio file
  (WAV/MP3/WMA/AIFF, anything NAudio can decode). Empty path means "default
  system beep". A missing or unreadable file falls back silently to the system
  beep and is logged at warn level.
- **Cooldown** (`CooldownSeconds`) — Per-entry minimum seconds between two
  audio alerts. `0` disables throttling.
- **Active Cooldown** — The cooldown value of the *most recently played* alert.
  It gates every subsequent attempt across the whole application until it
  expires, regardless of which entry's cooldown was passed in for the new
  attempt. See ADR 0001.
- **Audio Player** (`LogExpert.Audio.AudioPlayer`) — Static, process-wide
  fire-and-forget player. Owns the single global last-played timestamp and
  active-cooldown state.

## Sessions

- **Session** (`.lxj`) — Persisted workspace: a list of log file paths plus a
  tab/window layout XML blob. Loading a session reopens those files in their
  saved arrangement. Per-file state (bookmarks, filters, columnizer, …) is
  *not* in the session — it loads independently from each file's **Session
  File** (`.lxp`). A session is created or restored only by explicit user
  action ("Save Project" / "Load Project" menu items, to be renamed to
  Session terminology).
- **Session File** (`.lxp`) — JSON file saved beside (or in a central
  directory next to) a log file, restoring per-file state on reload:
  columnizer name, bookmarks, filter pipes, highlight group, multi-file
  state, follow-tail, encoding. Created automatically on save; one per log
  file. Composed by a Session via its `FileNames` list. The columnizer
  field of a Session File is one of several sources that can be selected
  by **Columnizer Selection Priority**; the other fields always load when
  the Session File exists.
- **Session Snapshot** (`SessionSnapshot`) — A neutral, UI-free capture of
  one Log Window's persistable state. Gathered by the Log Window when a
  Session File is saved and applied back in two phases when one is loaded
  (pre-load: options like encoding, multi-file, columnizer, panel layout;
  post-load: content-dependent state like bookmarks, scroll position,
  filters). Recursive: carries a child snapshot per filter-pipe tab.
- **Session File Composer** (`SessionFileComposer`) — The pure Core module
  beside `Persister` that owns the field mapping between a Session
  Snapshot and a Session File's serialized form, in both directions
  (compose on save, decompose on load), plus the Rollover staleness rule.
  It never does I/O, shows UI, or has side effects; the Log Window owns
  gathering, applying, timing, and error display.
- **Rollover staleness rule** — The predicate declaring a Session Snapshot
  stale because it was saved against a longer file than the one on disk
  (the saved line count exceeds the current one) — i.e. the log file has
  rolled over since the save. A stale snapshot's post-load state
  (bookmarks, scroll, filters) is discarded; the pre-load options still
  apply. Owned by the Session File Composer.

*Avoid*: "project" / "project file" / "workspace" (use **Session**),
"persistence file" / "per-file persistence" (use **Session File**),
bare "session file" when you mean the workspace (that's a **Session**),
"options-only load" (a Session File is always read whole; the options/full
split is two *apply phases* of the **Session Snapshot**, not a partial read).

## Control character display

- **Control Character Substitution** — Display-only replacement of selected
  control characters with visible glyphs, performed inside `LogWindow.PaintCell`.
  Never modifies raw column data, never affects search, filter, highlight
  regex, or columnizer parsing. Off by default. See ADR 0003.
- **Substitution Style** — One of five rendering modes applied globally to
  every enabled control character: Caret notation (`^G`), C escape (`\a` /
  `\xNN`), Abbreviation (`BEL`), Unicode Control Pictures (`␇`), ISO 2047
  (`⍾`). Default: Control Pictures.
- **Enabled Code Points** (`ControlCharSettings.EnabledCodepoints`) — The
  subset of C0 + DEL the user has opted in to substituting. First-time
  default is the "non-whitespace preset": all 33 characters except `HT`,
  `LF`, `CR`.
- **Substitution Style Fallback** — Per-style rule for characters the style
  has no defined glyph for. C escape falls back to `\xNN`; ISO 2047 falls
  back to the Control Pictures glyph; the other three styles cover all 33
  in-scope characters.
- **Copy Displayed Form** (`ControlCharSettings.CopyDisplayedForm`) —
  Opt-in setting that makes clipboard copy (and other "export selection"
  paths) use the substituted text instead of the raw bytes. Default: off,
  i.e. clipboard always carries raw data.

*Avoid*: "control char rendering" (use **Substitution**), "escape" alone
(ambiguous between the **C escape** style and the general concept).

## Filtering

- **Filter Engine** (`IFilterEngine`) — A Core module that executes one full
  **Filter Run** over a log file. Two engines implement the seam — the
  **Serial** and the **Parallel** engine (`SerialFilterEngine`,
  `ParallelFilterEngine`, the latter wrapping `FilterStarter`'s
  chunk-and-merge) — selected by the `MultiThreadFilter` preference and held
  to an identical, equivalence-test-pinned contract: sorted-ascending
  duplicate-free output, `FilterParams` and line count snapshotted at entry,
  failures and cancellation narrated as outcomes, never thrown or shown by
  the engine.
- **Filter Run** (`FilterRun`) — One execution of a Filter Engine and its
  result: result lines, hit lines, the spread history (handed to the tail
  path to continue from), and an outcome — Completed, Cancelled (partial
  lists stand), or Failed (carries the exception; the Log Window narrates it
  on the status line).
- **Filter Accumulator** (`FilterAccumulator`) — The single owner of the
  per-hit accumulation recipe (hit → spread expansion → append to
  results/history → trim history). Used inside the Serial engine, per line by
  the Log Window's tail filter path, and per hit by Filter Pipes — all three
  adopt existing lists, so a Filter Run's history is continued in place.
- **Window Filter** (`LogWindow._filterParams`) — The one filter a Log Window
  currently holds in its filter panel. Per-window state: it is what a **Session
  File** saves and restores, and it is not an entry of the **Saved Filter
  List**. A Filter Pipe tab gets its own, cloned when the tab is created.
- **Saved Filter List** (`Settings.FilterList`) — The application-wide list of
  filters the user explicitly saved from the filter panel, shown in the filter
  save list. Global, never per-window, and never restored from a Session File.
  (Session Files written before the Window Filter change stored a copy of it
  instead, of which only the first entry was ever restored.)
- **Filter Spread** — Context expansion around a filter hit: **Back Spread**
  (`FilterParams.SpreadBefore`) lines before and **Fore Spread**
  (`FilterParams.SpreadBehind`) lines after the hit are included in the
  filter result, deduplicated against recently emitted lines and clamped
  to the file's line range (line 0 is a valid context line). Owned by
  `LogExpert.Core.Classes.Filter.FilterSpread` — the single home of the
  expansion, history-trim, and rollover-shift rules; the serial filter,
  the parallel filter, and Filter Pipes all delegate to it. The maximum
  UI-configurable spread (99) and the duplicate-suppression window
  (2 × 99) derive from `FilterSpread.SPREAD_MAX`, which bounds the UI
  spread knobs (`FilterParams` itself does not clamp its values).

*Avoid*: "spread" alone when ambiguous (say **Back Spread** / **Fore
Spread**), "additional filter results" (the old internal name — use
**Filter Spread**), "multi-threaded filter" as a concept name (it is a
preference selecting the Parallel **Filter Engine**), "FilterFx" (legacy
delegate name, deleted), "the filter list" when the **Window Filter** is meant
(the list is the **Saved Filter List**).

## Log Search

- **Log Search** — The Ctrl+F / F3 / Shift+F3 search-in-file feature of a
  Log Window: finds the next or previous line matching a search text
  (plain or regex, case-sensitive or not), wrapping around the file
  boundary once before giving up. Executed by
  `LogExpert.Core.Classes.Search.LogSearcher`, the single owner of
  direction resolution (forward / find-next / Shift+F3), the
  wrap-around-once rule, and the matching; the Log Window narrates the
  returned `SearchResult` (status line, progress bar, scrolling).
  `Find` snapshots its `SearchParams` at entry, so mutating the shared
  instance (F3) never affects a run in flight.

*Avoid*: bare "search" when the filter panel is meant (that is
**filtering** — the `FilterSearch` methods in the code belong to the
filter path, not Log Search), "find dialog" (use **Search dialog**).

## Timestamp lookup

- **Timestamp Locator** (`LogExpert.Core.Classes.Timestamp.TimestampLocator`)
  — Timestamp lookup over a Logfile Reader and the active Columnizer: what
  time is the line at N (`FindBackward`, `FindForward`), and which line
  carries time T (`FindLine`, plus the raw binary-search primitive
  `FindNearestLine` used directly by the Time Spread calculator). Pure —
  no dependency on the Log Window control, only on `ITimestampSource`
  (below). Backs both time-sync (drag-to-scroll, "scroll all tabs to
  timestamp", the sync-group worker) and the Time Spread bar; the worker
  threads, grid selection, and cross-window coordination stay in the Log
  Window and `TimeSyncList` — only the lookup was extracted.
- **Timestamp Source** (`ITimestampSource`) — The narrow seam a Timestamp
  Locator runs against: a Logfile Reader, the active Columnizer, a
  positionable Columnizer callback, and the lock guarding Columnizer
  swaps. Every member is read live rather than captured, because a Log
  Window replaces its Logfile Reader on load/reload/rollover and swaps
  its Columnizer whenever the user picks a different one; a consumer like
  the Time Spread calculator outlives both events. Implemented by the Log
  Window itself.
- **Negated near-miss** — The miss convention of `FindNearestLine`, the raw
  binary-search primitive: when no line carries the exact timestamp searched
  for, it returns the nearest line's number *negated*, and callers flip the
  sign back themselves (`TimeSpreadCalculator` does). `FindLine`, the
  high-level lookup, does **not** expose this: it flips a miss back to a
  normal positive line number, so callers scroll to the nearest line instead
  of doing nothing — cross-window time-sync compares timestamps at
  millisecond precision, making the near-miss its *common* case. (Getting
  this wrong silently no-opped "scroll all tabs to timestamp" and time-sync;
  regression-pinned in `TimestampLocatorTests.FindLine_NoExactMatchMidFile_*`.)
  A near-miss that lands on line 0 is indistinguishable from a hit at line 0
  (`-0 == 0`) — a ported quirk of the original binary search, preserved
  rather than fixed; see
  `TimestampLocatorTests.FindLine_TimestampBeforeTheFirstLine_*`.

## Tail follow

- **Tail Follow Engine** (`LogExpert.Core.Classes.Tail.TailFollowEngine`) — The
  Core module that owns the tail-event queue and its single worker thread:
  file-size-changed events from the Logfile Reader are `Post()`ed in (any
  thread) and delivered in order as **Tail Follow Sink** callbacks on the
  worker thread. Owns the dispatch policies: rollover shift before content
  for the same event, the never-die guard (a failing dispatch is logged and
  the loop continues — the #634 contract), abandoned-sink exit, and the
  bounded (2 s) teardown join. It does **not** own the reader subscription:
  the Log Window swaps its Logfile Reader on load/reload/rollover, so it
  keeps forwarding `FileSizeChanged` into the engine. Watcher ownership is
  revisited with the LogfileReader decomposition.
- **Tail Follow Sink** (`ITailFollowSink`) — The rendering side of the seam,
  implemented by the Log Window: `OnRolloverShift` (shift bookmarks, row
  heights, filter pipes), `OnTailLines` (grid update + the tail trigger
  path), `OnLineCountChanged` (Time Spread bar), `IsAbandoned` (window
  disposed/closing — the engine exits). Callbacks arrive on the engine's
  worker thread; the sink marshals to the UI thread itself. Pinned quirks,
  preserved from the original worker: the rollover shift runs even for an
  abandoned sink, and `OnLineCountChanged` fires even when `OnTailLines`
  failed non-fatally.

*Avoid*: "LogEventWorker" (the pre-extraction thread and method name — the
module is the **Tail Follow Engine**), "tail thread" (say **the engine's
worker thread**).

*Avoid*: "GetTimestampForLine" / "GetTimestampForLineForward" /
"FindTimestampLineInternal" as concept names — those were the pre-extraction
Log Window method names (candidate 6 of
`docs/improve/logwindow-architecture-review.html`); the Core module is the
**Timestamp Locator**.

## Log Window roles

- **Log Line Source** (`ILogLineSource`) — Read access to a loaded log file's
  lines: line count, line memory, and the per-line physical file name
  (multi-file mode). The role of the Log Window that the columnizer callbacks
  (`ColumnizerCallback`, `ColumnizerCallbackMemory`) and the paint context
  consume — a three-member fake is all a Core test needs. Also the
  Core-visible face of the paint context: the UI's `ILogPaintContextUI`
  extends it with fonts, colors, and highlight lookup (the old empty
  `ILogPaintContext` marker is deleted).
- **Line Selectable** (`ILineSelectable`) — A Log Window in which a line can be
  selected programmatically (`SelectLine`). Held by a Filter Pipe for its
  origin window (`FilterPipe.OriginWindow`): "Locate line in original file".
- **Session Snapshot Source** (`ISessionSnapshotSource`) — A window whose
  persistable state can be gathered as a **Session Snapshot**. Held by a
  Filter Pipe for the window displaying its results (`FilterPipe.ResultWindow`);
  saving a Session File recurses through it.
- **`ILogWindow`** — Now purely the composition of the three roles above, with
  no members of its own. Kept for holders that need only the window's
  identity (the filter-list / highlight-group changed event args). New Core
  consumers take the single role they need, never the composition.

*Avoid*: adding members to `ILogWindow` directly (add to — or carve — a role
interface instead), "the ILogWindow seam" (name the specific role).

## Bookmark change notification

Two layers announce bookmark changes, and they deliberately differ in
granularity — do not "unify" them:

- **Bookmark Data Provider events** (`BookmarkDataProvider.BookmarkAdded` /
  `.BookmarkRemoved` / `.AllBookmarksRemoved`) — Core-level, and they *do* keep
  add and remove apart. Pinned by tests for fire-once and add-vs-remove
  semantics.
- **Bookmarks Changed** (`LogWindow.BookmarksChanged`) — The UI-level relay to
  the shell: this window's bookmark set changed, somehow. Add and remove are
  deliberately **not** distinguished, because every consumer only re-reads the
  set (the Tool Window Coordinator just calls `UpdateView()`). Bookmark *text*
  edits stay separate (`BookmarkTextChanged`) — they carry the bookmark.

*Avoid*: adding `BookmarkAdded` / `BookmarkRemoved` to the Log Window (that
granularity belongs to the provider), "bookmark event" unqualified (say which
layer).

## Columnizer selection

- **Columnizer** (`ILogLineMemoryColumnizer`) — A plugin that parses a log
  line into columns. Each loaded log window has exactly one active
  columnizer at a time. The set of available columnizers is owned by
  `PluginRegistry`.
- **Columnizer Mask Entry** (`ColumnizerMaskEntry`) — One user-configured
  row on the Settings → Columnizers tab. Pairs a **Mask**, a **Mask Type**,
  and a **Columnizer Name**. Stored in `Preferences.ColumnizerMaskList`.
- **Mask Type** — `Glob` or `Regex`. Glob uses `*` and `?` wildcards and
  matches against the short file name; Regex uses .NET regular-expression
  syntax. New rows default to `Glob`; rows that existed in `settings.json`
  before this field was introduced deserialize as `Regex` for backward
  compatibility. See ADR 0004.
- **Stale Mask Entry** — A Columnizer Mask Entry whose `ColumnizerName`
  does not resolve to any currently-registered columnizer. Stale entries
  are skipped at match time (the loop continues to the next entry), kept
  in the settings file (the plugin may return), and flagged in the
  Settings dialog with a leading warning icon.
- **Columnizer History** — Auto-maintained list (`Settings.ColumnizerHistoryList`,
  capped at 40 entries) recording the columnizer last used per absolute
  file path. Stale entries (columnizer no longer registered) are removed
  on lookup; this list, unlike the Mask list, is not user-curated.
- **Columnizer Selection Priority** (`ColumnizerSelectionPriority` enum) —
  The user-configured rule that orders the four sources of a "which
  columnizer should this file open with?" decision: Session File,
  Columnizer History, Columnizer Mask Entry, and AutoPick. Three modes,
  mutually exclusive:
  - `HistoryThenMask` *(default; today's behaviour)* — Session File →
    History → Mask → AutoPick → built-in default.
  - `MaskThenHistory` — Session File → Mask → History → AutoPick → default.
  - `MaskOverridesPersistence` — Mask → Session File → History → AutoPick
    → default. The only mode in which a matching mask outranks an
    existing Session File. Only the columnizer field of the Session File
    is overridden; bookmarks, filters, etc. still restore.

  See ADR 0005. Replaces the deprecated `Preferences.MaskPrio` bool.
  (Note: the enum member name `MaskOverridesPersistence` retains the old
  "Persistence" wording for backward compatibility of serialized settings;
  the user-facing concept is **Session File**.)
- **AutoPick** (`Preferences.AutoPick`) — When on, runs
  `ColumnizerPicker.FindBetterMemoryColumnizer` against the loaded file
  content to auto-detect a columnizer. Fires only when the Columnizer
  Selection Priority chain above produced no result — never overrides
  an explicit Mask, History, or Session File hit.

*Avoid*: "file mask" alone (ambiguous between glob and regex — say
**Glob Mask** or **Regex Mask**), "mask priority" (use **Columnizer
Selection Priority**).

## Flagged ambiguities

- "**session file**" was used by old UI resource keys (e.g.
  `LoadProject_UI_Message_Error_Title_FailedToUpdateSessionFile`) to mean
  the workspace (`.lxj`). Resolved: from now on **Session File** = `.lxp`
  (per-file state) and **Session** = `.lxj` (workspace). Existing resource
  keys named `*SessionFile*` today refer to the workspace and need
  renaming.
- "**project**" / "**project file**" historically referred to `.lxj`.
  Resolved: use **Session**. The `ProjectFileHandler` /
  `ProjectPersister` type names and `OnLoadProjectToolStripMenuItemClick`
  handler are scheduled for renaming.
- "**Per-file Persistence**" was the prior term for `.lxp`. Resolved:
  use **Session File**. The enum member
  `ColumnizerSelectionPriority.MaskOverridesPersistence` keeps its old
  name for serialization compatibility — internal name only, not a
  domain term.

### Log Viewing

**Log Window**: The control that displays, tails, and filters a single log file. One Log Window per open file.
_Avoid_: Tab, panel, view

**Session**: A named set of Log Windows and their layout, persisted to disk and restored together.
_Avoid_: Project, workspace, profile

**Columnizer**: A plugin that parses a raw log line into typed columns for structured display.
_Avoid_: Parser, formatter

**Highlight Group**: A named set of rules that colour-code log lines by content pattern.
_Avoid_: Colour scheme, filter rule

### External Tools

**External Tool**: A user-configured command-line program that LogExpert can launch from the Tool Launcher Bar, optionally with Sysout Capture.
_Avoid_: Plugin, script

**Sysout Capture**: A mode for an External Tool where the tool's stdout is redirected to a temp file and opened as a live Log Window.
_Avoid_: Stdout pipe, output redirect

**Tool Launcher Bar**: The toolbar strip populated from the configured External Tool entries.
_Avoid_: Toolbar, tool strip

### Sessions & Persistence

Defined in the **Sessions** section above: **Session** = `.lxj` (workspace),
**Session File** = `.lxp` (per-file state) — see also the flagged-ambiguity
resolution. (This section previously redefined both terms the pre-resolution
way; removed 2026-07-11.)
