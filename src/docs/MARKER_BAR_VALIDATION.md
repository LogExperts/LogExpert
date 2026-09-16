# Marker bar validation

Validation for [issue #27](https://github.com/LogExperts/LogExpert/issues/27#issuecomment-5574799680), performed on 2026-09-16. Commands below run from the repository root.

## Functional checks

The focused marker run passed 51 tests. Its coverage includes:

- [Core marker tests](../LogExpert.Tests/Marker): text and regex matching, case sensitivity, immutable criteria, cancellation, stale scan rejection, append-only updates, last-line edits, truncation, bucket counts, color priority, click targets, and bookmark snapshots.
- [Marker rendering](../LogExpert.Tests/UI/MarkerBarTests.cs): four lanes, tooltips, navigation events, odd widths, resize, light/dark background palettes, and geometry at 100%, 150%, and 200% sizes.
- [Log Window integration](../LogExpert.Tests/UI/MarkerWindowTests.cs): navigation stops follow-tail, visibility changes, latest executed search, filter hits without context lines, tail updates, saved bookmarks, truncation, independent windows, parsed word matches, timeshift, and Square Bracket layout snapshots.
- [Columnizer snapshots](../LogExpert.Tests/UI/MarkerColumnizerSnapshotTests.cs), [localized settings labels](../LogExpert.Tests/Dialogs/SettingsDialogMarkerBarTests.cs), and [preference serialization](../LogExpert.Tests/ConfigManagerTests/PreferencesMarkerBarTests.cs).

Two review regressions were reproduced before fixing them: a whole-line rule incorrectly matched text found only in a parsed column, and a bold-only rule rendered gray instead of the current foreground. Both failed with the old implementation and passed with the fixes. Word rules still match parsed column text.

The clean solution build and full test suite passed: **1,747 passed, 7 skipped, 0 failed**. The two explicit experiments below also passed when run separately. The clean build reported 241 warnings: 240 were present in the prior branch baseline, and the new nullable warning in the bold-only test was subsequently corrected. A final rebuild and focused run passed all 51 tests with no warnings beyond that baseline. Existing analyzer warnings, including the async UI error-boundary warning, remain; this is not a warning-free build.

```powershell
./build.ps1 --target Clean Compile Test
```

## Measured performance

Both explicit experiments passed when selected directly. Each used four regex rules, with a dense matching rule last, 500,000 initial lines, and 2,000 appended lines. These are measurements from one Debug run, not hardware-dependent timing assertions.

Environment: Windows 11 (NT 10.0.22631.0), x64, 14 logical processors available to the process, .NET SDK 10.0.106. The Log Window reported 96 DPI.

| Measurement | In-memory reader / marker index | Real file / Log Window |
| --- | ---: | ---: |
| Final line count | 502,000 | 502,000 |
| Initial byte count | 14,000,000 modeled UTF-8 bytes | Not recorded separately |
| Final byte count | 14,056,000 modeled UTF-8 bytes | 28,048,000 on disk |
| Initial discovery | 902.473 ms | 2,171 ms |
| Append processing | 5.836 ms | 698 ms, including file monitoring and discovery |
| Managed memory delta after initial discovery | 6,064,488 bytes | 6,097,816 bytes |
| Managed memory delta during append | 28,544 bytes | Not measured |
| Initial scan allocations | 82,130,480 bytes | Not measured |
| Append allocations | 388,408 bytes | Not measured |
| UI event pumps during discovery | Not applicable | 92 |
| Maximum measured event-pump duration | Not applicable | 57.9 ms |

The [index experiment](../LogExpert.Tests/Marker/MarkerPerformanceTests.cs) verified all 500,000 initial and 502,000 final matches and their aggregate counts across 2,000 pixel buckets. Its byte sizes model UTF-8 lines plus CRLF; it performs no file I/O. The [real-file experiment](../LogExpert.Tests/UI/MarkerWindowTests.cs) opened a Log Window, enabled discovery after loading, pumped UI events during discovery, appended to the file, and waited for the final marker tooltip to reflect line 502,000.

Memory deltas use `GC.GetTotalMemory(true)` and describe managed heap changes, not process working set. Allocation totals use `GC.GetTotalAllocatedBytes(true)`. The event-pump measurement shows that the UI processed events during discovery; it is not an input-latency guarantee. Physical monitor DPI changes and Windows theme switching were not exercised in this run; automated geometry and explicit palette checks cover those rendering inputs.

Reproduce the two opt-in experiments after building:

```powershell
dotnet test src/LogExpert.Tests/LogExpert.Tests.csproj --no-build --filter 'FullyQualifiedName=LogExpert.Tests.Marker.MarkerPerformanceTests.MarkerIndex_ScanAndAppend_DenseMatchesReportPerformanceAndCorrectness|FullyQualifiedName=LogExpert.Tests.UI.MarkerWindowTests.DenseFile_ReportsDiscoveryAppendAndUiResponsiveness' --logger 'console;verbosity=normal' --blame-hang-timeout 3m --blame-hang-dump-type none
```

## Review resolutions

| Review topic | Resolution and evidence |
| --- | --- |
| Whole-line matching | Removed the parsed-column fallback. [MarkerCriteria](../LogExpert.Core/Classes/Marker/MarkerCriteria.cs) now matches non-word rules against the raw line, matching the grid's whole-line painting. Word rules use parsed columns. |
| Bold-only color | Use a rule's background when supplied, otherwise its foreground. An empty foreground resolves to the control's current foreground in [MarkerBar](../LogExpert.UI/Controls/LogWindow/MarkerBar.cs); no hard-coded gray remains. |
| Blank settings labels | Not reproduced. [SettingsDialog](../LogExpert.UI/Dialogs/SettingsDialog.cs) runs `LoadResources` after constructing the marker controls; `ApplyTextResources` and [ResourceHelper](../LogExpert.UI/Extensions/ResourceHelper.cs) map their names to localized text. The label test passes. |
| Bookmark restoration | The production `SetBookmarks` path runs inside `LoadPersistenceData`. `OnLogFileReaderFinishedLoading` subsequently invalidates marker criteria. The saved-bookmark integration test passes. |
| Abandoned tail sink | `ITailFollowSink.IsAbandoned` becomes true during close/disposal, when markers are also disposed. Live tail dispatch decrements its pending count in `finally`. The reported permanently blank live-window path was not established. |
| Core test placement | Moved the five marker test files from `LogExpert.Persister.Tests` to `LogExpert.Tests/Marker`. |
| Unrelated files and formatting | Removed the feature branch's tracked `AGENTS.md` addition and the incidental final newline in `SettingsDialog.cs`. |
| Primitive sources and frame fields | Added named [MarkerSource](../LogExpert.UI/Controls/LogWindow/MarkerSource.cs) flags, source-keyed buckets, one rendering order, and a `MarkerFrame` record in [LogWindow.MarkerBar](../LogExpert.UI/Controls/LogWindow/LogWindow.MarkerBar.cs). |
| Criteria predicate, exceptions, bucket arithmetic, DPI | Reused the background predicate, added localized line/columnizer context to scan errors, explained inverse pixel mapping and midpoint selection, and scaled discovery dots by `DeviceDpi`. |
| Visibility default | Retained the existing opt-in default. The issue specifies saved independent visibility controls but does not prescribe a default. Serialization tests cover new and legacy preferences. |

The remaining structural choices were reviewed and retained. Generation, data revision, and unfinished-line revision protect different transitions. Highlight and search indexes keep separate progress and error state. A stable lock protects the mutable filter-hit list while background aggregation copies it; replacing it with the reassignable filter-result list's lock would not provide the same protection. Bookmark/filter data changes are batched on a 250 ms UI timer, so an existing rendered marker can remain briefly visible and clickable until the next refresh.

Columnizer clones intentionally copy each class's configuration and runtime layout. The [optional snapshot contract](PLUGIN_DEVELOPMENT_GUIDE.md#optional-marker-snapshots-icloneable) adds no required plugin-interface members. Plugins with a public parameterless constructor can use the configuration/initialization fallback; state that cannot be reconstructed this way requires the optional clone. A generic clone helper would not guarantee independent mutable state.

The final source reviews against Development found no remaining substantive standards or specification findings. Trigger execution remains confined to the existing tail path; marker discovery only evaluates visual matching.
